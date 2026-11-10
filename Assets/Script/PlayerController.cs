using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float runSpeed = 5f;
    public float jumpForce = 11f;

    [Header("Ground Check (include both Ground and Platform)")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float checkRadius = 0.15f;

    [Header("Jump Forgiveness")]
    [SerializeField, Min(0f)] private float coyoteTime = 0.08f;
    [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private PlayerDropThrough dropThrough;
    private bool isGrounded;
    private bool isRunning = true;
    private float coyoteRemaining;
    private float jumpBufferRemaining;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[12];

    public bool IsGrounded => isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        dropThrough = GetComponent<PlayerDropThrough>();
    }

    private void FixedUpdate()
    {
        if (!isRunning) return;

        bool dropping = dropThrough != null && dropThrough.IsDropping;
        isGrounded = !dropping && rb.linearVelocity.y <= 0.1f && CheckGround();
        coyoteRemaining = dropping ? 0f : (isGrounded ? coyoteTime : Mathf.Max(0f, coyoteRemaining - Time.fixedDeltaTime));

        if (jumpBufferRemaining > 0f)
        {
            jumpBufferRemaining = Mathf.Max(0f, jumpBufferRemaining - Time.fixedDeltaTime);
            if (!dropping && (isGrounded || coyoteRemaining > 0f))
            {
                rb.linearVelocity = new Vector2(runSpeed, jumpForce);
                jumpBufferRemaining = 0f;
                coyoteRemaining = 0f;
                isGrounded = false;
                return;
            }
        }

        rb.linearVelocity = new Vector2(runSpeed, rb.linearVelocity.y);
    }

    private bool CheckGround()
    {
        // Keep the original GroundCheck method for compatibility with existing scenes.
        if (groundCheck != null)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(groundCheck.position, checkRadius, groundLayer);
            foreach (Collider2D hit in hits)
            {
                if (hit != null && !hit.isTrigger && hit != bodyCollider &&
                    !hit.transform.IsChildOf(transform))
                    return true;
            }
        }

        // Fallback: check beneath the actual body collider (less sensitive to
        // a misplaced GroundCheck, especially on a narrow platform edge).
        if (bodyCollider == null) return false;
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(groundLayer);
        filter.useTriggers = false;
        int count = bodyCollider.Cast(Vector2.down, filter, groundHits, 0.12f);
        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = groundHits[i];
            if (hit.collider == null || hit.collider == bodyCollider ||
                hit.collider.transform.IsChildOf(transform) || hit.normal.y < 0.5f)
                continue;
            return true;
        }
        return false;
    }

    public void Jump()
    {
        if (!isRunning || (dropThrough != null && dropThrough.IsDropping)) return;
        jumpBufferRemaining = jumpBufferTime > 0f ? jumpBufferTime : Time.fixedDeltaTime;
    }

    public void DropDown()
    {
        Debug.Log(
            $"[PlayerController] DropDown | " +
            $"isRunning={isRunning}, " +
            $"isGrounded={isGrounded}, " +
            $"dropThrough={(dropThrough != null)}"
        );

        if (!isRunning || dropThrough == null)
            return;

        bool hasPlatform = dropThrough.HasPlatformBelow();

        Debug.Log(
            $"[PlayerController] HasPlatformBelow={hasPlatform}, " +
            $"IsDropping={dropThrough.IsDropping}"
        );

        if (!isGrounded && !hasPlatform)
            return;

        jumpBufferRemaining = 0f;
        coyoteRemaining = 0f;

        Debug.Log("[PlayerController] Calling Drop()");
        dropThrough.Drop();
    }

    public void StopRunning()
    {
        isRunning = false;
        jumpBufferRemaining = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
    }

    public float GetDistance() => transform.position.x;

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
    }
}
