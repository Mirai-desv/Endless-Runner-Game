using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class MagicProjectile : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 12f;
    public float lifetime = 4f;

    [Header("Combat")]
    public int damage = 1;
    public bool isSpecial = false;

    private Rigidbody2D rb;
    private Camera mainCamera;
    private bool hasHit;

    private readonly HashSet<EnemyHealth> hitEnemies = new();
    private readonly HashSet<DestructibleObstacle> hitObstacles = new();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        GetComponent<Collider2D>().isTrigger = true;
    }

    void Start()
    {
        mainCamera = Camera.main;
        Destroy(gameObject, lifetime);
    }

    void FixedUpdate()
    {
        rb.MovePosition(
            rb.position + Vector2.right * speed * Time.fixedDeltaTime
        );
    }

    void LateUpdate()
    {
        // Đạn đặc biệt bay ra khỏi cạnh phải màn hình thì biến mất.
        if (!isSpecial || mainCamera == null)
            return;

        Vector3 viewportPoint =
            mainCamera.WorldToViewportPoint(transform.position);

        if (viewportPoint.z > 0f && viewportPoint.x > 1.15f)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        // Không bắn trúng Player.
        if (other.GetComponentInParent<PlayerHitReceiver>() != null)
            return;

        // Bỏ qua vật phẩm.
        if (other.GetComponentInParent<MoonPickup>() != null ||
            other.GetComponentInParent<DicePickup>() != null)
            return;

        // 1. Bắn trúng quái.
        EnemyHealth enemy =
            other.GetComponentInParent<EnemyHealth>();

        if (enemy != null)
        {
            if (hitEnemies.Add(enemy))
            {
                enemy.TakeDamage(damage);

                if (!isSpecial)
                    DestroyBullet();
            }

            return;
        }

        // 2. Bắn trúng vật cản phá được.
        DestructibleObstacle obstacle =
            other.GetComponentInParent<DestructibleObstacle>();

        if (obstacle != null)
        {
            if (hitObstacles.Add(obstacle))
            {
                obstacle.TakeDamage(damage);

                if (!isSpecial)
                    DestroyBullet();
            }

            return;
        }

        // 3. Bắn trúng địa hình thường.
        if (!other.isTrigger)
        {
            // Đạn thường bị chặn, đạn đặc biệt xuyên qua.
            if (!isSpecial)
                DestroyBullet();
        }
    }

    void DestroyBullet()
    {
        if (hasHit) return;

        hasHit = true;
        Destroy(gameObject);
    }
}