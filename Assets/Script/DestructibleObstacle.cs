using UnityEngine;
using EndlessRunner.Run.World;
using System.Collections;

public class DestructibleObstacle : MonoBehaviour, IChunkResettable
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

    private Collider2D[] colliders;
    private bool[] colliderEnabled;
    private Sprite originalSprite;

    void Awake()
    {
        currentHealth = maxHealth;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
            originalSprite = spriteRenderer.sprite;

        colliders = GetComponentsInChildren<Collider2D>(true);
        colliderEnabled = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
            colliderEnabled[i] = colliders[i].enabled;
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

        StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(brokenDuration);
        gameObject.SetActive(false);
    }

    // Được ChunkView gọi mỗi khi chunk được lấy ra dùng lại: tường nguyên vẹn trở lại.
    public void ResetForReuse()
    {
        StopAllCoroutines();

        currentHealth = maxHealth;
        destroyed = false;

        if (spriteRenderer != null && originalSprite != null)
            spriteRenderer.sprite = originalSprite;

        if (colliders != null)
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i] != null) colliders[i].enabled = colliderEnabled[i];

        gameObject.SetActive(true);
    }
}