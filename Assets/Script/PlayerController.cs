using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float runSpeed = 10f;
    public float RunSpeed
    {
        get => runSpeed;
        set => runSpeed = Mathf.Max(0f, value);
    }
    public float jumpForce = 16f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float checkRadius = 0.15f;

    [SerializeField, Min(0.01f)]
    private float groundCastDistance = 0.08f;

    [SerializeField, Range(0f, 1f)]
    private float groundNormalThreshold = 0.5f;

    [Header("Jump Forgiveness")]
    [SerializeField, Min(0f)]
    private float coyoteTime = 0.08f;

    [SerializeField, Min(0f)]
    private float jumpBufferTime = 0.12f;

    [Header("Air Control")]
    [SerializeField] private bool allowAirDrop = true;
    [SerializeField] private bool allowDropJump = true;
    [SerializeField] private float airDropSpeed = 4f;

    [Header("Debug")]
    [SerializeField] private bool debugGround = false;

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private PlayerDropThrough dropThrough;

    private bool isRunning = true;
    private bool isGrounded;

    // Mỗi chu kỳ trên không có 1 Bonus Jump và 1 Bonus Down.
    private bool isBonusJumpUsed;
    private bool isBonusDownUsed;

    private float coyoteRemaining;
    private float jumpBufferRemaining;

    private readonly RaycastHit2D[] groundHits =
        new RaycastHit2D[12];

    private readonly ContactPoint2D[] contacts =
        new ContactPoint2D[16];

    public bool IsGrounded => isGrounded;

    public event Action OnJumpPerformed;
    public event Action OnDropPerformed;

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

        bool previousGrounded = isGrounded;

        bool dropping =
            dropThrough != null && dropThrough.IsDropping;

        bool groundContact = HasGroundContact(groundLayer);
        bool groundNearby = CheckGround(groundLayer);

        bool fallingOrStanding =
            rb.linearVelocity.y <= 0.1f;

        // Khi đang xuyên Platform, chỉ tiếp xúc vật lý
        // hợp lệ mới được tính là tiếp đất.
        isGrounded = fallingOrStanding &&
            (groundContact || (!dropping && groundNearby));

        if (isGrounded)
        {
            // Chỉ reset khi thực sự tiếp đất hợp lệ.
            isBonusJumpUsed = false;
            isBonusDownUsed = false;
        }

        if (debugGround && previousGrounded != isGrounded)
        {
            Debug.Log(
                $"[GROUND] {previousGrounded} -> {isGrounded} " +
                $"dropping={dropping} " +
                $"vy={rb.linearVelocity.y:F2}",
                this
            );
        }

        // Coyote Time
        if (dropping)
        {
            coyoteRemaining = 0f;
        }
        else if (isGrounded)
        {
            coyoteRemaining = coyoteTime;
        }
        else
        {
            coyoteRemaining = Mathf.Max(
                0f,
                coyoteRemaining - Time.fixedDeltaTime
            );
        }

        // Jump thường khi tiếp đất/coyote; Jump trên không dùng Bonus Jump.
        if (jumpBufferRemaining > 0f)
        {
            bool canGroundJump =
                !dropping && (isGrounded || coyoteRemaining > 0f);

            bool canBonusJump =
                allowDropJump && !isGrounded &&
                !canGroundJump && !isBonusJumpUsed;

            if (canGroundJump || canBonusJump)
            {
                if (canBonusJump)
                {
                    isBonusJumpUsed = true;

                    // Nếu đang xuyên Platform: dừng Drop,
                    // collision chỉ khôi phục khi không còn chồng lấn.
                    if (dropping && dropThrough != null)
                        dropThrough.CancelDrop();
                }

                rb.linearVelocity = new Vector2(runSpeed, jumpForce);
                jumpBufferRemaining = 0f;
                coyoteRemaining = 0f;
                isGrounded = false;

                OnJumpPerformed?.Invoke();

                if (debugGround)
                    Debug.Log(canBonusJump
                        ? "[JUMP] Bonus Jump used"
                        : "[JUMP] Ground Jump", this);

                return;
            }

            jumpBufferRemaining = Mathf.Max(
                0f, jumpBufferRemaining - Time.fixedDeltaTime);
        }

        // Auto-run
        rb.linearVelocity = new Vector2(
            runSpeed,
            rb.linearVelocity.y
        );
    }

    private bool CheckGround(LayerMask mask)
    {
        if (bodyCollider == null || mask.value == 0)
            return false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(mask);
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

            if (hit.collider.transform == transform ||
                hit.collider.transform.IsChildOf(transform))
                continue;

            if (Physics2D.GetIgnoreCollision(
                bodyCollider, hit.collider))
                continue;

            if (hit.normal.y < groundNormalThreshold)
                continue;

            return true;
        }

        return false;
    }

    private bool HasGroundContact(LayerMask mask)
    {
        if (bodyCollider == null || mask.value == 0)
            return false;

        int count = bodyCollider.GetContacts(contacts);
        Bounds playerBounds = bodyCollider.bounds;
        float feetY = playerBounds.min.y;

        for (int i = 0; i < count; i++)
        {
            ContactPoint2D contact = contacts[i];

            Collider2D other =
                contact.collider == bodyCollider
                    ? contact.otherCollider
                    : contact.collider;

            if (other == null || other.isTrigger)
                continue;

            if ((mask.value &
                (1 << other.gameObject.layer)) == 0)
                continue;

            if (Physics2D.GetIgnoreCollision(
                bodyCollider, other))
                continue;

            // Bỏ va chạm với tường / cạnh dọc.
            // Dùng trị tuyệt đối do hướng normal của contact có thể
            // phụ thuộc thứ tự collider trong cặp tiếp xúc.
            if (Mathf.Abs(contact.normal.y) < groundNormalThreshold)
                continue;

            // Tiếp xúc phải nằm gần chân Player.
            if (contact.point.y > feetY + 0.12f)
                continue;

            // Bề mặt đỡ Player phải gần độ cao chân.
            // Tránh nhận va chạm với cạnh tường.
            float surfaceTop = other.bounds.max.y;

            if (Mathf.Abs(surfaceTop - feetY) > 0.18f)
                continue;

            return true;
        }

        return false;
    }

    private bool IsOnSolidGround()
    {
        // Ground đặc = groundLayer trừ platformLayer.
        int platformMask = dropThrough != null
            ? dropThrough.platformLayer.value
            : 0;

        LayerMask solidMask =
            groundLayer.value & ~platformMask;

        return HasGroundContact(solidMask) ||
               CheckGround(solidMask);
    }

    public void Jump()
    {
        if (!isRunning)
            return;

        // Input được xử lý ở FixedUpdate.
        // Nếu chưa thể nhảy, buffer có thể giúp bắt
        // thao tác ngay trước lúc tiếp đất.
        jumpBufferRemaining = Mathf.Max(
            jumpBufferTime,
            Time.fixedDeltaTime
        );
    }

    public void DropDown()
    {
        if (!isRunning || rb == null || !rb.simulated)
            return;

        // Không cho xuyên Ground đặc khi đang đứng trên đó
        if (isGrounded && IsOnSolidGround())
            return;

        bool onPlatform =
            dropThrough != null && dropThrough.HasPlatformBelow();

        // Đang đứng trên Platform
        if (isGrounded)
        {
            if (dropThrough == null ||
                !onPlatform ||
                !dropThrough.TryDrop())
                return;

            jumpBufferRemaining = 0f;
            coyoteRemaining = 0f;
            isGrounded = false;

            OnDropPerformed?.Invoke();
            return;
        }

        // Đang trên không
        if (!allowAirDrop)
            return;

        jumpBufferRemaining = 0f;
        coyoteRemaining = 0f;

        // Cho phép xuyên Platform nếu có thể
        if (dropThrough != null &&
            !dropThrough.IsDropping &&
            onPlatform)
        {
            dropThrough.TryDrop();
        }

        // Chỉ tăng tốc rơi nếu đang rơi chậm hơn tốc độ mong muốn.
        // Không làm nhân vật bị chậm lại nếu đã rơi nhanh.
        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            Mathf.Min(rb.linearVelocity.y, -airDropSpeed)
        );

        OnDropPerformed?.Invoke();

        if (debugGround)
            Debug.Log("[DROP] Air Drop", this);
    }

    public void StopRunning()
    {
        isRunning = false;
        jumpBufferRemaining = 0f;

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
    }

    public float GetDistance()
    {
        return transform.position.x;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            groundCheck.position,
            checkRadius
        );
    }
}
