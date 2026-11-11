using UnityEngine;

/// Gắn lên prefab NormalBullet / SpecialBullet để chưởng "sống" hơn:
///  - Chạy animation theo danh sách sprite (nếu có Frames)
///  - Nhấp nháy to nhỏ (pulse) và/hoặc xoay
///  - Hiệu ứng xuất hiện (scale từ nhỏ lên)
[RequireComponent(typeof(SpriteRenderer))]
public class ProjectileVisual : MonoBehaviour
{
    [Header("Animation theo frame (tuỳ chọn)")]
    public Sprite[] frames;
    public float fps = 12f;

    [Header("Chuyển động")]
    public float pulseAmount = 0.12f;
    public float pulseSpeed = 14f;
    public float spinSpeed = 0f;       // độ/giây, 0 = không xoay

    [Header("Xuất hiện")]
    public float spawnGrowTime = 0.08f;

    private SpriteRenderer sr;
    private Vector3 baseScale;
    private float age;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        transform.localScale = baseScale * 0.2f;
    }

    private void Update()
    {
        age += Time.deltaTime;

        if (frames != null && frames.Length > 0 && fps > 0f)
            sr.sprite = frames[(int)(age * fps) % frames.Length];

        float grow = spawnGrowTime > 0f ? Mathf.Clamp01(age / spawnGrowTime) : 1f;
        float pulse = 1f + Mathf.Sin(age * pulseSpeed) * pulseAmount;
        transform.localScale = baseScale * Mathf.Lerp(0.2f, 1f, grow) * pulse;

        if (spinSpeed != 0f)
            transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
    }
}