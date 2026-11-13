using UnityEngine;

// Quản lý vòng đời đạn do quái sinh ra

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 7f;
    public float lifetime = 4f;

    private Vector2 direction = Vector2.left;
    private Rigidbody2D rb;
    private bool hasHit;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        GetComponent<Collider2D>().isTrigger = true;
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void SetDirection(Vector2 newDirection)
    {
        direction = newDirection.normalized;
    }

    void FixedUpdate()
    {
        rb.MovePosition(
            rb.position + direction * speed * Time.fixedDeltaTime
        );
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        PlayerHitReceiver player =
            other.GetComponentInParent<PlayerHitReceiver>();

        if (player != null)
        {
            hasHit = true;
            player.Hit();
            Destroy(gameObject);
            return;
        }

        // Không va chạm vật phẩm hoặc quái khác.
        if (other.GetComponentInParent<EnemyHealth>() != null ||
            other.GetComponentInParent<MoonPickup>() != null ||
            other.GetComponentInParent<DicePickup>() != null)
            return;

        // Đạn va chạm địa hình hoặc vật cản.
        if (!other.isTrigger)
        {
            hasHit = true;
            Destroy(gameObject);
        }
    }
}