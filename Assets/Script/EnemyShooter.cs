
using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public Transform player;
    public Transform firePoint;
    public GameObject bulletPrefab;

    [Header("Attack Settings")]
    public float attackRange = 10f;
    public float shootInterval = 2f;

    private float timer;
    private bool isAttacking;

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (player == null)
        {
            GameObject target =
                GameObject.FindGameObjectWithTag("Player");

            if (target != null)
                player = target.transform;
        }
    }

    void Update()
    {
        if (player == null || animator == null ||
            bulletPrefab == null || firePoint == null)
            return;

        if (GameFlow.InputLocked)
            return;

        if (isAttacking)
            return;

        float distance = Vector2.Distance(
            transform.position,
            player.position
        );

        float horizontalDistance =
        transform.position.x - player.position.x;

        // Player phải ở bên trái và trong tầm tấn công
        if (horizontalDistance < 0f ||
            horizontalDistance > attackRange)
            return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            StartAttack();
        }
    }

    void StartAttack()
    {
        isAttacking = true;

        animator.ResetTrigger("Attack");
        animator.SetTrigger("Attack");

        Debug.Log("[ENEMY] Start Attack");
    }

    // Gọi từ Animation Event tại frame bắn.

    public void FireProjectile()
    {
        if (!isAttacking || bulletPrefab == null || firePoint == null)
            return;

        Debug.Log("[ENEMY] FireProjectile Event called!");

        GameObject bullet = Instantiate(
            bulletPrefab,
            firePoint.position,
            Quaternion.identity
        );

        EnemyProjectile projectile =
            bullet.GetComponent<EnemyProjectile>();

        if (projectile != null)
        {
            // Luôn bắn thẳng về bên trái
            projectile.SetDirection(Vector2.left);
        }
    }


    // Gọi từ Animation Event cuối animation.
    public void EndAttack()
    {
        isAttacking = false;
        timer = shootInterval;

        Debug.Log("[ENEMY] End Attack");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );
    }
}
