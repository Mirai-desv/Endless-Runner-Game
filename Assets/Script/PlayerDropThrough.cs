
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDropThrough : MonoBehaviour
{
    public LayerMask platformLayer;

    private Collider2D playerCollider;
    private Rigidbody2D rb;
    private bool isDropping;

    void Awake()
    {
        playerCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void Drop()
    {
        if (isDropping) return;

        Collider2D platform = FindPlatformBelow();

        if (platform != null)
            StartCoroutine(DropRoutine(platform));
    }

    private Collider2D FindPlatformBelow()
    {
        Bounds bounds = playerCollider.bounds;

        RaycastHit2D[] hits = Physics2D.RaycastAll(
            bounds.center,
            Vector2.down,
            bounds.extents.y + 0.35f,
            platformLayer
        );

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == playerCollider)
                continue;

            if (hit.collider.GetComponent<
                PlatformEffector2D>() != null)
            {
                return hit.collider;
            }
        }

        return null;
    }

    private IEnumerator DropRoutine(Collider2D platform)
    {
        isDropping = true;

        Physics2D.IgnoreCollision(
            playerCollider, platform, true
        );

        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x, -4f
        );

        // Chờ toàn bộ nhân vật xuống dưới platform.
        // Giới hạn thời gian để tránh kẹt vô hạn.
        float elapsed = 0f;

        while (platform != null &&
               playerCollider.bounds.max.y >=
               platform.bounds.min.y - 0.05f &&
               elapsed < 3f)
        {
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // Nếu chưa thoát khỏi platform thì tiếp tục chờ.
        while (platform != null &&
               playerCollider.bounds.Intersects(
                   platform.bounds))
        {
            yield return new WaitForFixedUpdate();
        }

        if (platform != null)
        {
            Physics2D.IgnoreCollision(
                playerCollider, platform, false
            );
        }

        isDropping = false;
    }
}
