
using UnityEngine;
using EndlessRunner.Run.World;

public class EnemyShooter : MonoBehaviour, IChunkResettable
{
    [Header("References")]
    public Animator animator;
    public Transform player;
    public Transform firePoint;
    public GameObject bulletPrefab;

    [Header("Attack Settings")]
    public float attackRange = 10f;
    public float shootInterval = 2f;
    public float firstAttackDelay = 1.5f;

    private bool hasDetectedPlayer = false;

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

    private bool needsAnimatorReset;

    void OnDisable()
    {
        needsAnimatorReset = true;
    }

    void OnEnable()
    {
        // Được bật lại sau khi chunk tái sử dụng: đưa animator về trạng thái ban đầu.
        if (!needsAnimatorReset) return;
        needsAnimatorReset = false;

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    // Được ChunkView gọi khi chunk tái sử dụng: reset trạng thái tấn công.
    public void ResetForReuse()
    {
        hasDetectedPlayer = false;
        isAttacking = false;
        timer = 0f;

        if (animator != null)
            animator.ResetTrigger("Attack");
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
        // Lần đầu phát hiện Player
        if (!hasDetectedPlayer)
        {
            hasDetectedPlayer = true;
            timer = firstAttackDelay;
        }

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
