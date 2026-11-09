using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerDropThrough : MonoBehaviour
{
    [Tooltip("Only the Platform layer; never include solid Ground.")]
    public LayerMask platformLayer;

    [Header("Drop settings")]
    [SerializeField, Min(0.05f)] private float footProbeDistance = 0.35f;
    [SerializeField, Min(0.1f)] private float downwardSpeed = 5f;
    [SerializeField, Min(0f)] private float clearMargin = 0.03f;
    [SerializeField, Min(0.1f)] private float maxDropSeconds = 1.2f;
    [SerializeField] private bool debugDrop = true;

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private Collider2D[] playerColliders;
    private Coroutine dropRoutine;

    // Store every pair we ignore, so restoration is guaranteed.
    private readonly List<Collider2D> ignoredPlayer = new List<Collider2D>();
    private readonly List<Collider2D> ignoredSurface = new List<Collider2D>();
    private readonly ContactPoint2D[] contactBuffer = new ContactPoint2D[32];

    public bool IsDropping { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        playerColliders = GetComponentsInChildren<Collider2D>();
    }

    public bool HasPlatformBelow() => FindPlatformBelow() != null;

    public void Drop()
    {
        if (IsDropping || rb == null || !rb.simulated) return;

        Collider2D platform = FindPlatformBelow();
        if (platform == null)
        {
            if (debugDrop) Debug.LogWarning("[DROP] No Platform collider beneath the player. Check PlayerDropThrough.Platform Layer.", this);
            return;
        }

        if (debugDrop)
            Debug.Log($"[DROP] Found {platform.name} id={platform.GetEntityId()} feetY={bodyCollider.bounds.min.y:F3} topY={platform.bounds.max.y:F3}", this);

        dropRoutine = StartCoroutine(DropRoutine(platform));
    }

    private bool IsValidPlatform(Collider2D c)
    {
        if (c == null || !c.enabled || c.isTrigger || !c.gameObject.activeInHierarchy) return false;
        if ((platformLayer.value & (1 << c.gameObject.layer)) == 0) return false;
        if (c.transform == transform || c.transform.IsChildOf(transform)) return false;
        return c.GetComponent<PlatformEffector2D>() != null ||
               c.GetComponentInParent<PlatformEffector2D>() != null;
    }

    private Collider2D FindPlatformBelow()
    {
        if (bodyCollider == null || platformLayer.value == 0) return null;

        Bounds feet = bodyCollider.bounds;
        float inset = Mathf.Min(0.08f, feet.extents.x * 0.25f);
        Vector2 center = new Vector2(feet.center.x, feet.min.y - footProbeDistance * 0.25f);
        Vector2 size = new Vector2(Mathf.Max(0.05f, feet.size.x - inset * 2f),
                                   footProbeDistance * 1.5f + 0.12f);
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, platformLayer);

        Collider2D nearest = null;
        float nearestGap = float.PositiveInfinity;
        foreach (Collider2D c in hits)
        {
            if (!IsValidPlatform(c)) continue;
            if (c.bounds.max.x <= feet.min.x + inset ||
                c.bounds.min.x >= feet.max.x - inset) continue;

            float gap = feet.min.y - c.bounds.max.y;
            if (gap < -0.18f || gap > footProbeDistance) continue;

            if (Mathf.Abs(gap) < nearestGap)
            {
                nearestGap = Mathf.Abs(gap);
                nearest = c;
            }
        }
        return nearest;
    }

    private bool AlreadyIgnored(Collider2D player, Collider2D surface)
    {
        for (int i = 0; i < ignoredPlayer.Count; i++)
            if (ignoredPlayer[i] == player && ignoredSurface[i] == surface) return true;
        return false;
    }

    private void IgnorePair(Collider2D player, Collider2D surface)
    {
        if (player == null || surface == null || player.isTrigger || !IsValidPlatform(surface)) return;
        if (AlreadyIgnored(player, surface)) return;
        Physics2D.IgnoreCollision(player, surface, true);
        ignoredPlayer.Add(player);
        ignoredSurface.Add(surface);
        if (debugDrop)
            Debug.Log($"[DROP] Ignore pair: player={player.name}#{player.GetEntityId()} surface={surface.name}#{surface.GetEntityId()} active={Physics2D.GetIgnoreCollision(player, surface)}", this);
    }

    // Some chunks contain several touching platform colliders. Ignore only surfaces
    // at the SAME height as the platform being dropped through, not lower floors.
    private void IgnoreOtherSupportsAtSameHeight(Collider2D selected)
    {
        if (bodyCollider == null || selected == null) return;
        Bounds feet = bodyCollider.bounds;
        Vector2 areaCenter = new Vector2(feet.center.x, feet.min.y);
        Vector2 areaSize = new Vector2(feet.size.x + 0.10f, 0.40f);
        Collider2D[] nearby = Physics2D.OverlapBoxAll(areaCenter, areaSize, 0f, platformLayer);

        foreach (Collider2D surface in nearby)
        {
            if (!IsValidPlatform(surface)) continue;
            if (Mathf.Abs(surface.bounds.max.y - selected.bounds.max.y) > 0.12f) continue;
            foreach (Collider2D pc in playerColliders) IgnorePair(pc, surface);
        }

        // Also examine actual contacts: their collider instance IDs are useful
        // when two scene objects share the same name (e.g. pooled chunks).
        foreach (Collider2D pc in playerColliders)
        {
            if (pc == null || !pc.enabled || pc.isTrigger) continue;
            int count = pc.GetContacts(contactBuffer);
            for (int i = 0; i < count; i++)
            {
                ContactPoint2D contact = contactBuffer[i];
                Collider2D surface = contact.collider == pc ? contact.otherCollider : contact.collider;
                if (!IsValidPlatform(surface)) continue;
                if (Mathf.Abs(surface.bounds.max.y - selected.bounds.max.y) > 0.12f) continue;
                IgnorePair(pc, surface);
            }
        }
    }

    private IEnumerator DropRoutine(Collider2D platform)
    {
        IsDropping = true;
        IgnoreOtherSupportsAtSameHeight(platform);
        foreach (Collider2D pc in playerColliders) IgnorePair(pc, platform);

        rb.linearVelocity = new Vector2(rb.linearVelocity.x,
            Mathf.Min(rb.linearVelocity.y, -downwardSpeed));

        float elapsed = 0f;
        float nextLog = 0.20f;
        float initialY = rb.position.y;
        bool cleared = false;
        bool warnedStuck = false;

        while (elapsed < maxDropSeconds && rb != null && rb.simulated &&
               bodyCollider != null && platform != null)
        {
            // Prevent the lower platform from being ignored: only check clearance
            // from the originally selected surface.
            if (bodyCollider.bounds.max.y < platform.bounds.min.y - clearMargin)
            {
                cleared = true;
                break;
            }

            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;

            // Support colliders might change as the player advances horizontally.
            IgnoreOtherSupportsAtSameHeight(platform);

            if (!warnedStuck && elapsed >= 0.18f && Mathf.Abs(rb.position.y - initialY) < 0.03f)
            {
                warnedStuck = true;
                if (debugDrop) Debug.LogWarning("[DROP] Player is still stationary after 0.18s: inspect contacts below. Another collider, movement script or effector may be holding the body.", this);
            }

            if (debugDrop && elapsed >= nextLog)
            {
                nextLog += 0.2f;
                string contacts = "";
                int n = rb.GetContacts(contactBuffer);
                for (int i = 0; i < n; i++)
                {
                    var cp = contactBuffer[i];
                    var other = (cp.collider != null && cp.collider.transform.IsChildOf(transform))
                        ? cp.otherCollider : cp.collider;
                    if (other != null)
                        contacts += $" {other.name}#{other.GetEntityId()} ignored=" +
                            (cp.otherCollider != null && cp.collider != null && Physics2D.GetIgnoreCollision(cp.collider, cp.otherCollider));
                }
                Debug.Log($"[DROP] t={elapsed:F2} y={rb.position.y:F3} vy={rb.linearVelocity.y:F2} contacts=[{contacts}]", this);
            }
        }

        if (debugDrop)
            Debug.Log($"[DROP] Finished cleared={cleared} y={rb.position.y:F3}; ignoredPairs={ignoredPlayer.Count}", this);

        RestoreCollision();
        IsDropping = false;
        dropRoutine = null;
    }

    private void RestoreCollision()
    {
        for (int i = 0; i < ignoredPlayer.Count; i++)
            if (ignoredPlayer[i] != null && ignoredSurface[i] != null)
                Physics2D.IgnoreCollision(ignoredPlayer[i], ignoredSurface[i], false);
        ignoredPlayer.Clear();
        ignoredSurface.Clear();
    }

    private void OnDisable()
    {
        if (dropRoutine != null) StopCoroutine(dropRoutine);
        RestoreCollision();
        IsDropping = false;
        dropRoutine = null;
    }
}
