using UnityEngine;

/// <summary>
/// Hiệu ứng khói khi quái / tường bị hạ.
/// Gắn CÙNG object với EnemyHealth hoặc DestructibleObstacle.
/// Khi bị hạ, các script đó gọi Play() -> sinh vài cụm khói ở phía trước vật,
/// bay nhẹ lên, to dần rồi mờ dần và tự biến mất.
/// Khói là object độc lập (không phải con của chunk) nên không bị ảnh hưởng
/// khi chunk bị pool/tắt đi hay khi vật gốc bị Destroy.
/// </summary>
public class DeathSmoke : MonoBehaviour
{
    [Header("Hình khói")]
    [Tooltip("Sprite khói (vd: 1 ô cắt từ Art/dust.png). Để trống = tự vẽ khói tròn mềm.")]
    public Sprite smokeSprite;

    [Tooltip("Màu + độ đục ban đầu của khói (A = độ đục lớn nhất).")]
    public Color color = new Color(0.85f, 0.85f, 0.9f, 0.9f);

    [Tooltip("Để trống = dùng material mặc định của pipeline. Nếu khói bị tối do ánh sáng 2D, kéo material Sprite-Unlit-Default vào đây.")]
    public Material materialOverride;

    [Header("Số lượng & kích thước")]
    [Min(1)] public int puffCount = 4;

    [Tooltip("Đường kính ban đầu của mỗi cụm khói (đơn vị world).")]
    [Min(0.05f)] public float startSize = 0.9f;

    [Tooltip("Cuối vòng đời khói to gấp bao nhiêu lần ban đầu.")]
    [Min(1f)] public float endScaleMultiplier = 1.8f;

    [Header("Chuyển động")]
    [Tooltip("Thời gian từ lúc hiện đến lúc biến mất hoàn toàn (giây).")]
    [Min(0.1f)] public float duration = 0.7f;

    [Tooltip("Tốc độ bay lên (đơn vị/giây).")]
    public float riseSpeed = 0.8f;

    [Tooltip("Tốc độ trôi ngang ngẫu nhiên tối đa (đơn vị/giây).")]
    public float driftSpeed = 0.4f;

    [Tooltip("Tốc độ xoay ngẫu nhiên tối đa (độ/giây).")]
    public float spinSpeed = 40f;

    [Header("Vị trí")]
    [Tooltip("Lệch so với tâm vật. Chạy sang phải nên X âm = lệch về phía người chơi.")]
    public Vector2 positionOffset = Vector2.zero;

    [Tooltip("Mức khói tản ngẫu nhiên quanh tâm, theo tỉ lệ kích thước vật (0 = đúng tâm).")]
    [Range(0f, 1f)] public float spread = 0.4f;

    [Header("Thứ tự vẽ")]
    [Tooltip("Cộng thêm vào Order in Layer của vật để khói luôn vẽ ĐÈ LÊN phía trước vật.")]
    public int sortingOrderBoost = 5;

    private static Sprite softSprite;

    /// <summary>Gọi ngay trước khi Destroy vật.</summary>
    public void Play()
    {
        // 1) Lấy vùng bao + sorting của vật từ các SpriteRenderer.
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();

        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        bool hasBounds = false;
        bool hasTop = false;
        int topOrder = 0;
        int layerId = 0;

        foreach (SpriteRenderer sr in renderers)
        {
            if (sr == null || sr.sprite == null) continue;

            if (!hasBounds) { bounds = sr.bounds; hasBounds = true; }
            else bounds.Encapsulate(sr.bounds);

            if (!hasTop || sr.sortingOrder > topOrder)
            {
                topOrder = sr.sortingOrder;
                layerId = sr.sortingLayerID;
                hasTop = true;
            }
        }

        Sprite sprite = smokeSprite != null ? smokeSprite : GetSoftSprite();
        float spriteWidth = Mathf.Max(0.0001f, sprite.bounds.size.x);
        float baseScale = startSize / spriteWidth;

        // 2) Sinh các cụm khói.
        for (int i = 0; i < puffCount; i++)
        {
            Vector3 pos = bounds.center + (Vector3)positionOffset + new Vector3(
                Random.Range(-bounds.extents.x, bounds.extents.x) * spread,
                Random.Range(-bounds.extents.y, bounds.extents.y) * spread,
                0f);
            pos.z = transform.position.z;

            GameObject go = new GameObject("DeathSmokePuff");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingLayerID = layerId;
            sr.sortingOrder = topOrder + sortingOrderBoost + i;
            if (materialOverride != null) sr.sharedMaterial = materialOverride;

            float scaleJitter = Random.Range(0.8f, 1.2f);
            float life = duration * Random.Range(0.85f, 1.15f);

            SmokePuff puff = go.AddComponent<SmokePuff>();
            puff.Init(
                sr,
                life,
                baseScale * scaleJitter,
                baseScale * scaleJitter * endScaleMultiplier,
                riseSpeed * Random.Range(0.7f, 1.3f),
                Random.Range(-driftSpeed, driftSpeed),
                Random.Range(-spinSpeed, spinSpeed));
        }
    }

    // Khói tròn mềm tự vẽ (64x64), tạo 1 lần dùng lại cho mọi hiệu ứng.
    private static Sprite GetSoftSprite()
    {
        if (softSprite != null) return softSprite;

        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "SoftSmokeTexture",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color32[] px = new Color32[size * size];
        float r = size * 0.5f;
        Vector2 c = new Vector2(r, r);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) / r;
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a); // smoothstep -> rìa mờ dần
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }

        tex.SetPixels32(px);
        tex.Apply(false, true);

        // pixelsPerUnit = size -> sprite rộng đúng 1 đơn vị world.
        softSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        softSprite.name = "SoftSmokeSprite";
        return softSprite;
    }
}

/// <summary>Điều khiển 1 cụm khói: hiện nhanh, to dần, bay lên, mờ dần rồi tự huỷ.</summary>
public class SmokePuff : MonoBehaviour
{
    private const float FadeInTime = 0.1f; // 10% đầu: hiện ra

    private SpriteRenderer sr;
    private float life, elapsed, startAlpha;
    private float startScale, endScale;
    private float rise, drift, spin;

    public void Init(SpriteRenderer renderer, float lifeTime, float startScl, float endScl,
                     float riseSpeed, float driftSpeed, float spinSpeed)
    {
        sr = renderer;
        life = Mathf.Max(0.05f, lifeTime);
        startScale = startScl;
        endScale = endScl;
        rise = riseSpeed;
        drift = driftSpeed;
        spin = spinSpeed;
        startAlpha = sr.color.a;

        transform.localScale = Vector3.one * startScale;
        SetAlpha(0f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / life);

        // To dần (ease-out: nở nhanh lúc đầu, chậm lúc cuối).
        float eased = 1f - (1f - t) * (1f - t);
        transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, eased);

        transform.position += new Vector3(drift, rise, 0f) * Time.deltaTime;
        transform.Rotate(0f, 0f, spin * Time.deltaTime);

        // Hiện nhanh rồi mờ dần về 0.
        float a;
        if (t < FadeInTime) a = t / FadeInTime;
        else a = Mathf.Pow(1f - (t - FadeInTime) / (1f - FadeInTime), 1.5f);
        SetAlpha(a * startAlpha);

        if (t >= 1f) Destroy(gameObject);
    }

    private void SetAlpha(float a)
    {
        Color c = sr.color;
        c.a = a;
        sr.color = c;
    }
}