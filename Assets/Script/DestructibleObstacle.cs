/*
using UnityEngine;

public class DestructibleObstacle : MonoBehaviour
{
    [Min(1)] public int maxHealth = 1;

    private int currentHealth;
    private bool destroyed;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (destroyed || damage <= 0) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
            DestroyObstacle();
    }

    public void DestroyObstacle()
    {
        if (destroyed) return;

        destroyed = true;
        Destroy(gameObject);
    }
}
*/
using UnityEngine;

public class DestructibleObstacle : MonoBehaviour
{
    [Min(1)] public int maxHealth = 1;

    [Header("Hiệu ứng phá hủy")]
    [Tooltip("Sprite hiển thị ngay trước khi biến mất. Để trống = biến mất luôn.")]
    public Sprite brokenSprite;

    [Tooltip("Thời gian hiển thị brokenSprite (giây) trước khi bị xoá.")]
    [Min(0f)] public float brokenDuration = 0.01f;

    [Tooltip("Để trống sẽ tự tìm SpriteRenderer trên object hoặc object con.")]
    public SpriteRenderer spriteRenderer;

    private int currentHealth;
    private bool destroyed;

    void Awake()
    {
        currentHealth = maxHealth;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void TakeDamage(int damage)
    {
        if (destroyed || damage <= 0) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
            DestroyObstacle();
    }

    public void DestroyObstacle()
    {
        if (destroyed) return;
        destroyed = true;

        // Hiệu ứng khói (chỉ chạy nếu object có gắn DeathSmoke).
        DeathSmoke smoke = GetComponent<DeathSmoke>();
        if (smoke != null) smoke.Play();

        // Tắt collider NGAY để trong khoảng chờ tường không còn chặn đạn
        // và không giết Player (tag "Obstacle").
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        // Đổi sang sprite vỡ.
        if (brokenSprite != null && spriteRenderer != null)
            spriteRenderer.sprite = brokenSprite;

        Destroy(gameObject, brokenDuration);
    }
}