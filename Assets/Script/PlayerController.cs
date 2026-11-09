using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float runSpeed = 5f;
    public float jumpForce = 11f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float checkRadius = 0.15f;

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool isRunning = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            checkRadius,
            groundLayer
         ) != null && rb.linearVelocity.y <= 0.1f; // Thêm && rb.linearVelocity.y <= 0.1f

        if (!isRunning) return;

        if (Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.UpArrow))
        {
            Jump();
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) ||
            Input.GetKeyDown(KeyCode.S))
        {
            DropDown();
        }
    }

    void FixedUpdate()
    {
        if (!isRunning) return;

        rb.linearVelocity = new Vector2(
            runSpeed,
            rb.linearVelocity.y
        );
    }

    public void Jump()
    {
        if (!isRunning || !isGrounded) return;

        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            jumpForce
        );
    }
    public void DropDown()
    {
        if (!isRunning || !isGrounded) return;

        PlayerDropThrough drop =
            GetComponent<PlayerDropThrough>();

        if (drop != null)
        {
            drop.Drop();
        }
    }


    public void StopRunning()
    {
        isRunning = false;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
    }

    public float GetDistance()
    {
        return transform.position.x;
    }
}
