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
    private readonly ContactPoint2D[] groundContacts =
    new ContactPoint2D[16];

    [SerializeField] private bool debugGround = false;
    [Header("Ground Detection")]
    [SerializeField] private float groundCastDistance = 0.08f;
    [SerializeField] private float groundNormalThreshold = 0.5f;

    private readonly ContactPoint2D[] contacts =
    new ContactPoint2D[16];

    public bool IsGrounded => isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        dropThrough = GetComponent<PlayerDropThrough>();
    }

    private void FixedUpdate()
    {
        if (!isRunning)
            return;

        bool dropping =
            dropThrough != null && dropThrough.IsDropping;

        bool groundDetected = CheckGround();

        bool groundContact = HasGroundContact();
        bool groundNearby = CheckGround();
        bool fallingOrStanding = rb.linearVelocity.y <= 0.1f;

        // Tiếp xúc thực tế được ưu tiên.
        // Trong khi DropThrough, vẫn không nhận mặt đất
        // nếu chưa có tiếp xúc hợp lệ.
        isGrounded = fallingOrStanding &&
            (groundContact || (!dropping && groundNearby));

        bool previousGrounded = isGrounded;

        if (previousGrounded != isGrounded)
        {
            Debug.Log(
                $"[GROUND] {previousGrounded} -> {isGrounded} | " +
                $"Detected={groundDetected} | " +
                $"Dropping={dropping} | " +
                $"VelocityY={rb.linearVelocity.y:F2}"
            );
        }

        coyoteRemaining = dropping
            ? 0f
            : (isGrounded
                ? coyoteTime
                : Mathf.Max(
                    0f,
                    coyoteRemaining - Time.fixedDeltaTime
                ));

        if (jumpBufferRemaining > 0f)
        {
            jumpBufferRemaining = Mathf.Max(
                0f,
                jumpBufferRemaining - Time.fixedDeltaTime
            );

            if (!dropping &&
                (isGrounded || coyoteRemaining > 0f))
            {
                rb.linearVelocity = new Vector2(
                    runSpeed,
                    jumpForce
                );

                jumpBufferRemaining = 0f;
                coyoteRemaining = 0f;
                isGrounded = false;
                return;
            }
        }

        rb.linearVelocity = new Vector2(
            runSpeed,
            rb.linearVelocity.y
        );
    }

    private bool CheckGround()
    {
        if (bodyCollider == null)
            return false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(groundLayer);
        filter.useTriggers = false;

        int count = bodyCollider.Cast(
            Vector2.down,
            filter,
            groundHits,
            groundCastDistance
        );

        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = groundHits[i];

            if (hit.collider == null)
                continue;

            // Bỏ qua chính Player.
            if (hit.collider.transform == transform ||
                hit.collider.transform.IsChildOf(transform))
                continue;

            // Bỏ qua platform đang bị IgnoreCollision.
            if (Physics2D.GetIgnoreCollision(
                    bodyCollider,
                    hit.collider))
                continue;

            // Chỉ nhận bề mặt có pháp tuyến hướng lên.
            if (hit.normal.y < groundNormalThreshold)
                continue;

            return true;
        }

        return false;
    }

    private bool HasGroundContact()
    {
        int count = bodyCollider.GetContacts(contacts);

        for (int i = 0; i < count; i++)
        {
            ContactPoint2D contact = contacts[i];

            Collider2D other =
                contact.collider == bodyCollider
                    ? contact.otherCollider
                    : contact.collider;

            if (other == null || other.isTrigger)
                continue;

            if ((groundLayer.value & (1 << other.gameObject.layer)) == 0)
                continue;

            if (Physics2D.GetIgnoreCollision(bodyCollider, other))
                continue;

            // Xác nhận tiếp xúc từ phía dưới chân.
            // Dựa vào vị trí tiếp xúc để tránh phụ thuộc
            // vào chiều normal của từng collider.
            if (contact.point.y <= bodyCollider.bounds.center.y)
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
