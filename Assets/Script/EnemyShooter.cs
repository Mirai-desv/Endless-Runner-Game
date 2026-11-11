using UnityEngine;

// Điều khiển hành vi tấn công của quái

public class EnemyShooter : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Animator animator;
    public Transform firePoint;
    public GameObject bulletPrefab;

    [Header("Shooting")]
    public float detectionRange = 6f;
    public float shootCooldown = 1.5f;

    [Header("Direction")]
    public bool faceLeft = true;

    private float nextShootTime;

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (player == null)
        {
            PlayerHitReceiver target =
                FindAnyObjectByType<PlayerHitReceiver>();

            if (target != null)
                player = target.transform;
        }
    }

    void Update()
    {
        if (player == null ||
            firePoint == null ||
            bulletPrefab == null)
            return;

        if (Time.time < nextShootTime)
            return;

        Vector2 offset = player.position - transform.position;

        bool inFront = faceLeft
            ? offset.x < 0f
            : offset.x > 0f;

        if (!inFront)
            return;

        if (offset.magnitude > detectionRange)
            return;

        Shoot();
        nextShootTime = Time.time + shootCooldown;
    }

    void Shoot()
    {
        if (animator != null)
            animator.SetTrigger("Attack");

        GameObject bullet = Instantiate(
            bulletPrefab,
            firePoint.position,
            Quaternion.identity
        );

        EnemyProjectile projectile =
            bullet.GetComponent<EnemyProjectile>();

        if (projectile != null)
            projectile.SetDirection(faceLeft ? Vector2.left : Vector2.right);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange
        );
    }
}