
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerDropThrough : MonoBehaviour
{
    [Header("Platform Only")]
    [Tooltip("Only Platform. Never include Ground.")]
    public LayerMask platformLayer;

    [Header("Drop Settings")]
    [SerializeField, Min(0.05f)]
    private float footProbeDistance = 0.35f;

    [SerializeField, Min(0.1f)]
    private float downwardSpeed = 10f;

    [SerializeField, Min(0f)]
    private float clearMargin = 0.03f;

    [SerializeField, Min(0.1f)]
    private float maxDropSeconds = 1.2f;

    [SerializeField]
    private bool debugDrop = false;

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private Collider2D[] playerColliders;

    private Coroutine dropRoutine;
    private Coroutine restoreRoutine;

    private readonly List<Collider2D> ignoredPlayer =
        new List<Collider2D>();

    private readonly List<Collider2D> ignoredSurface =
        new List<Collider2D>();

    private readonly ContactPoint2D[] contactBuffer =
        new ContactPoint2D[32];

    public bool IsDropping { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        playerColliders = GetComponentsInChildren<Collider2D>();
    }

    public bool HasPlatformBelow()
    {
        return FindPlatformBelow() != null;
    }

    private bool IsValidPlatform(Collider2D c)
    {
        if (c == null || !c.enabled ||
            c.isTrigger || !c.gameObject.activeInHierarchy)
            return false;

        if ((platformLayer.value &
            (1 << c.gameObject.layer)) == 0)
            return false;

        if (c.transform == transform ||
            c.transform.IsChildOf(transform))
            return false;

        // Chỉ cho xuyên Platform có Effector.
        return c.GetComponent<PlatformEffector2D>() != null ||
               c.GetComponentInParent<PlatformEffector2D>() != null;
    }

    private Collider2D FindPlatformBelow()
    {
        if (bodyCollider == null || platformLayer.value == 0)
            return null;

        Bounds feet = bodyCollider.bounds;

        float inset = Mathf.Min(
            0.08f,
            feet.extents.x * 0.25f
        );

        Vector2 center = new Vector2(
            feet.center.x,
            feet.min.y - footProbeDistance * 0.25f
        );

        Vector2 size = new Vector2(
            Mathf.Max(0.05f, feet.size.x - inset * 2f),
            footProbeDistance * 1.5f + 0.12f
        );

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            center,
            size,
            0f,
            platformLayer
        );

        Collider2D nearest = null;
        float nearestGap = float.PositiveInfinity;

        foreach (Collider2D c in hits)
        {
            if (!IsValidPlatform(c))
                continue;

            if (c.bounds.max.x <= feet.min.x + inset ||
                c.bounds.min.x >= feet.max.x - inset)
                continue;

            float gap = feet.min.y - c.bounds.max.y;

            if (gap < -0.18f ||
                gap > footProbeDistance)
                continue;

            if (Mathf.Abs(gap) < nearestGap)
            {
                nearestGap = Mathf.Abs(gap);
                nearest = c;
            }
        }

        return nearest;
    }

    private bool AlreadyIgnored(
        Collider2D player,
        Collider2D surface)
    {
        for (int i = 0; i < ignoredPlayer.Count; i++)
        {
            if (ignoredPlayer[i] == player &&
                ignoredSurface[i] == surface)
                return true;
        }

        return false;
    }

    private void IgnorePair(
        Collider2D player,
        Collider2D surface)
    {
        if (player == null || surface == null)
            return;

        if (!player.enabled || player.isTrigger)
            return;

        if (!IsValidPlatform(surface))
            return;

        if (AlreadyIgnored(player, surface))
            return;

        // Không can thiệp những collision đã được
        // script khác thiết lập Ignore từ trước.
        if (Physics2D.GetIgnoreCollision(player, surface))
            return;

        Physics2D.IgnoreCollision(
            player,
            surface,
            true
        );

        ignoredPlayer.Add(player);
        ignoredSurface.Add(surface);
    }

    private void IgnoreSupportsAtSameHeight(
        Collider2D selected)
    {
        if (bodyCollider == null || selected == null)
            return;

        Bounds feet = bodyCollider.bounds;

        Vector2 center = new Vector2(
            feet.center.x,
            feet.min.y
        );

        Vector2 size = new Vector2(
            feet.size.x + 0.1f,
            0.4f
        );

        Collider2D[] nearby = Physics2D.OverlapBoxAll(
            center,
            size,
            0f,
            platformLayer
        );

        foreach (Collider2D surface in nearby)
        {
            if (!IsValidPlatform(surface))
                continue;

            if (Mathf.Abs(
                surface.bounds.max.y -
                selected.bounds.max.y) > 0.12f)
                continue;

            foreach (Collider2D pc in playerColliders)
                IgnorePair(pc, surface);
        }

        foreach (Collider2D pc in playerColliders)
        {
            if (pc == null || !pc.enabled || pc.isTrigger)
                continue;

            int count = pc.GetContacts(contactBuffer);

            for (int i = 0; i < count; i++)
            {
                ContactPoint2D cp = contactBuffer[i];

                Collider2D surface =
                    cp.collider == pc
                        ? cp.otherCollider
                        : cp.collider;

                if (!IsValidPlatform(surface))
                    continue;

                if (Mathf.Abs(
                    surface.bounds.max.y -
                    selected.bounds.max.y) > 0.12f)
                    continue;

                IgnorePair(pc, surface);
            }
        }
    }

    // Giữ hàm cũ để tương thích.
    public void Drop()
    {
        TryDrop();
    }

    // Trả về true nếu thực sự bắt đầu Drop.
    public bool TryDrop()
    {
        if (IsDropping || rb == null || !rb.simulated)
            return false;

        // Chưa khôi phục collision của lần Drop cũ.
        if (ignoredPlayer.Count > 0)
            return false;

        Collider2D platform = FindPlatformBelow();

        if (platform == null)
        {
            if (debugDrop)
                Debug.LogWarning(
                    "[DROP] No valid Platform below.",
                    this
                );

            return false;
        }

        if (restoreRoutine != null)
        {
            StopCoroutine(restoreRoutine);
            restoreRoutine = null;
        }

        dropRoutine = StartCoroutine(
            DropRoutine(platform)
        );

        return true;
    }

    private IEnumerator DropRoutine(Collider2D platform)
    {
        IsDropping = true;

        IgnoreSupportsAtSameHeight(platform);

        foreach (Collider2D pc in playerColliders)
            IgnorePair(pc, platform);

        // Bắt đầu rơi xuống.
        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            Mathf.Min(rb.linearVelocity.y, -downwardSpeed)
        );

        float elapsed = 0f;

        while (elapsed < maxDropSeconds &&
               rb != null &&
               rb.simulated &&
               bodyCollider != null &&
               platform != null)
        {
            // Player đã nằm hoàn toàn bên dưới Platform.
            if (bodyCollider.bounds.max.y <
                platform.bounds.min.y - clearMargin)
            {
                break;
            }

            yield return new WaitForFixedUpdate();

            elapsed += Time.fixedDeltaTime;

            if (platform != null)
                IgnoreSupportsAtSameHeight(platform);
        }

        IsDropping = false;
        dropRoutine = null;

        BeginRestore();
    }

    // Được gọi khi Jump trong lúc đang Drop.
    public void CancelDrop()
    {
        if (!IsDropping)
            return;

        if (dropRoutine != null)
        {
            StopCoroutine(dropRoutine);
            dropRoutine = null;
        }

        IsDropping = false;

        if (debugDrop)
            Debug.Log("[DROP] Cancelled by Jump.", this);

        // Không bật collision ngay nếu còn chồng lấn.
        BeginRestore();
    }

    private void BeginRestore()
    {
        if (restoreRoutine != null)
        {
            StopCoroutine(restoreRoutine);
            restoreRoutine = null;
        }

        restoreRoutine = StartCoroutine(
            RestoreWhenClear()
        );
    }

    private bool IsClearOfIgnoredSurfaces()
    {
        for (int i = 0; i < ignoredPlayer.Count; i++)
        {
            Collider2D pc = ignoredPlayer[i];
            Collider2D surface = ignoredSurface[i];

            if (pc == null || surface == null)
                continue;

            if (!pc.enabled || !surface.enabled)
                continue;

            Bounds a = pc.bounds;
            Bounds b = surface.bounds;

            bool separated =
                a.max.x + clearMargin < b.min.x ||
                a.min.x - clearMargin > b.max.x ||
                a.max.y + clearMargin < b.min.y ||
                a.min.y - clearMargin > b.max.y;

            if (!separated)
                return false;
        }

        return true;
    }

    private IEnumerator RestoreWhenClear()
    {
        yield return new WaitForFixedUpdate();

        float elapsed = 0f;
        bool warned = false;

        while (rb != null &&
               rb.simulated &&
               !IsClearOfIgnoredSurfaces())
        {
            yield return new WaitForFixedUpdate();

            elapsed += Time.fixedDeltaTime;

            if (!warned && elapsed >= 2f)
            {
                warned = true;

                if (debugDrop)
                {
                    Debug.LogWarning(
                        "[DROP] Waiting to clear Platform.",
                        this
                    );
                }
            }
        }

        RestoreCollision();
        restoreRoutine = null;
    }

    private void RestoreCollision()
    {
        for (int i = 0; i < ignoredPlayer.Count; i++)
        {
            Collider2D pc = ignoredPlayer[i];
            Collider2D surface = ignoredSurface[i];

            if (pc != null && surface != null)
            {
                Physics2D.IgnoreCollision(
                    pc,
                    surface,
                    false
                );
            }
        }

        ignoredPlayer.Clear();
        ignoredSurface.Clear();

        if (debugDrop)
            Debug.Log("[DROP] Collision restored.", this);
    }

    private void OnDisable()
    {
        if (dropRoutine != null)
            StopCoroutine(dropRoutine);

        if (restoreRoutine != null)
            StopCoroutine(restoreRoutine);

        RestoreCollision();

        IsDropping = false;
        dropRoutine = null;
        restoreRoutine = null;
    }
}
