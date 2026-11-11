using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Các object UI mà GameFlow cần. Kéo thả trong Inspector (hoặc để menu
// Tools > Endless Runner > Create Game Flow UI in Scene tự điền).
// Bạn có thể tự do đổi vị trí, kích thước, ảnh, font, màu của các object này;
// chỉ cần giữ đúng các tham chiếu bên dưới.
[Serializable]
public class GameFlowUIRefs
{
    [Header("1. Tap to play")]
    [Tooltip("Panel của màn Tap to play (toàn màn hình). Chạm bất kỳ đâu là bắt đầu.")]
    public GameObject menuPanel;
    [Tooltip("Không bắt buộc. Chữ 'Tap to play' sẽ nhấp nháy nếu gán.")]
    public TextMeshProUGUI tapToPlayText;

    [Header("2. Chọn số may mắn")]
    [Tooltip("Panel của màn chọn số may mắn.")]
    public GameObject luckyPanel;
    [Tooltip("6 ô xúc xắc để chạm chọn. Phần tử 0 = mặt 1, ..., phần tử 5 = mặt 6. Nên có Image.")]
    public RectTransform[] luckyDice = new RectTransform[6];

    [Header("3. Quay xúc xắc")]
    [Tooltip("Panel của màn quay xúc xắc.")]
    public GameObject rollPanel;
    [Tooltip("Image xúc xắc lớn ở giữa, đổi sprite liên tục khi quay.")]
    public Image rollDie;
    [Tooltip("Không bắt buộc. Image nhỏ hiện số may mắn của người chơi.")]
    public Image luckyIcon;
    [Tooltip("Không bắt buộc. Chữ tiêu đề (ROLLING... / LUCKY! / NOT LUCKY).")]
    public TextMeshProUGUI rollTitle;
    [Tooltip("Không bắt buộc. Chữ kết quả (FULL ENERGY! / +25 ENERGY).")]
    public TextMeshProUGUI rollResult;

    [Header("4. Gameplay UI")]
    public GameObject scoreText;
    public GameObject energyBar;
    public GameObject upButton;
    public GameObject downButton;
    public GameObject skillButton;

    public bool IsComplete
    {
        get
        {
            if (menuPanel == null || luckyPanel == null || rollPanel == null || rollDie == null) return false;
            if (luckyDice == null || luckyDice.Length != 6) return false;
            foreach (RectTransform r in luckyDice)
                if (r == null) return false;
            return true;
        }
    }
}


// Dựng bố cục mặc định của 3 màn. Dùng cho (a) menu Editor để tạo UI trong scene,
// (b) dự phòng lúc chạy nếu scene chưa có UI.

public static class GameFlowLayout
{
    private static readonly Color Gold = new Color(1f, 0.85f, 0.25f);

    public static GameObject CreateCanvas(Transform parent, string name = "GameFlowUI")
    {
        GameObject canvasGo = new GameObject(name, typeof(Canvas), typeof(CanvasScaler));
        if (parent != null) canvasGo.transform.SetParent(parent, false);

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // luôn nằm trên mọi UI khác

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f); // theo README: 1080 x 1920
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvasGo;
    }

    // Tạo 3 panel dưới canvasRoot và trả về các tham chiếu. Sprite xúc xắc lấy từ DiceFaces.Get
    public static GameFlowUIRefs Build(Transform canvasRoot, string gameTitle, string tapToPlay)
    {
        GameFlowUIRefs ui = new GameFlowUIRefs();

        // ---------------------------------------------------------------- 1. Tap to play
        RectTransform menu = FlowUI.NewRect("MenuPanel", canvasRoot);
        FlowUI.Stretch(menu);
        FlowUI.Stretch(FlowUI.NewImage("Dim", menu, null, new Color(0f, 0f, 0f, 0.35f)).rectTransform);

        TextMeshProUGUI title = FlowUI.NewText("GameTitle", menu, gameTitle, 130f, Gold);
        FlowUI.Anchor(title.rectTransform, new Vector2(0.5f, 0.78f), new Vector2(1000f, 220f));

        TextMeshProUGUI tap = FlowUI.NewText("TapToPlayText", menu, tapToPlay, 80f, Color.white);
        FlowUI.Anchor(tap.rectTransform, new Vector2(0.5f, 0.22f), new Vector2(1000f, 140f));

        ui.menuPanel = menu.gameObject;
        ui.tapToPlayText = tap;

        // ---------------------------------------------------------------- 2. Chọn số may mắn
        RectTransform lucky = FlowUI.NewRect("LuckyNumberPanel", canvasRoot);
        FlowUI.Stretch(lucky);
        FlowUI.Stretch(FlowUI.NewImage("Dim", lucky, null, new Color(0.04f, 0.02f, 0.12f, 0.88f)).rectTransform);

        TextMeshProUGUI header = FlowUI.NewText("Header", lucky, "LUCKY NUMBER", 60f, Gold);
        FlowUI.Anchor(header.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(900f, 100f), new Vector2(0f, 480f));

        TextMeshProUGUI hint = FlowUI.NewText("TapToChoose", lucky, "Tap to choose", 70f, Color.white);
        FlowUI.Anchor(hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(900f, 110f));

        // Hình 2: hàng trên 1-3-5, hàng dưới 2-4-6.
        int[] layout = { 1, 3, 5, 2, 4, 6 };
        const float size = 260f, gapX = 330f, gapY = 230f;
        ui.luckyDice = new RectTransform[6];
        for (int i = 0; i < 6; i++)
        {
            int face = layout[i];
            int col = i % 3, row = i / 3;
            Image img = FlowUI.NewImage("Dice_" + face, lucky, DiceFaces.Get(face), Color.white);
            FlowUI.Anchor(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(size, size),
                new Vector2((col - 1) * gapX, row == 0 ? gapY : -gapY));
            ui.luckyDice[face - 1] = img.rectTransform;
        }
        ui.luckyPanel = lucky.gameObject;

        // ---------------------------------------------------------------- 3. Quay xúc xắc
        RectTransform roll = FlowUI.NewRect("DiceRollPanel", canvasRoot);
        FlowUI.Stretch(roll);
        FlowUI.Stretch(FlowUI.NewImage("Dim", roll, null, new Color(0.04f, 0.02f, 0.12f, 0.9f)).rectTransform);

        TextMeshProUGUI rollTitle = FlowUI.NewText("RollTitle", roll, "ROLLING...", 90f, Color.white);
        FlowUI.Anchor(rollTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(900f, 140f), new Vector2(0f, 460f));

        Image bigDie = FlowUI.NewImage("RollDie", roll, DiceFaces.Get(1), Color.white);
        FlowUI.Anchor(bigDie.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(420f, 420f), new Vector2(0f, 80f));

        TextMeshProUGUI luckyLabel = FlowUI.NewText("LuckyLabel", roll, "YOUR LUCKY NUMBER", 48f, new Color(1f, 1f, 1f, 0.8f));
        FlowUI.Anchor(luckyLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(700f, 80f), new Vector2(-90f, -240f));

        Image luckyIcon = FlowUI.NewImage("LuckyIcon", roll, DiceFaces.Get(1), Color.white);
        FlowUI.Anchor(luckyIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(110f, 110f), new Vector2(330f, -240f));

        TextMeshProUGUI rollResult = FlowUI.NewText("RollResult", roll, "", 80f, Gold);
        FlowUI.Anchor(rollResult.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1000f, 240f), new Vector2(0f, -430f));

        ui.rollPanel = roll.gameObject;
        ui.rollDie = bigDie;
        ui.luckyIcon = luckyIcon;
        ui.rollTitle = rollTitle;
        ui.rollResult = rollResult;

        return ui;
    }
}

public static class FlowUI
{
    public static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // Đặt rect theo điểm neo (0..1 theo màn hình), kích thước và độ lệch (pixel theo canvas)
    public static void Anchor(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 offset = default)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = offset;
    }

    public static Image NewImage(string name, Transform parent, Sprite sprite, Color color)
    {
        RectTransform rt = NewRect(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    public static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, Color color)
    {
        RectTransform rt = NewRect(name, parent);
        TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.enableAutoSizing = true; // chữ tự co lại nếu quá rộng, không bị xuống dòng
        tmp.fontSizeMax = size;
        tmp.fontSizeMin = size * 0.4f;
        tmp.fontSize = size;
        return tmp;
    }
}

public static class DiceFaces
{
    private const int Size = 256;
    private static readonly Sprite[] cache = new Sprite[6];

    private static Sprite[] inspectorSprites;
    private static readonly Sprite[] resourceSprites = new Sprite[6];
    private static readonly bool[] resourceTried = new bool[6];

    // GameFlow truyền 6 sprite người dùng chọn (có thể null / có ô null)
    public static void SetInspectorSprites(Sprite[] sprites)
    {
        inspectorSprites = sprites;
    }

    public static Sprite Get(int face)
    {
        int i = Mathf.Clamp(face, 1, 6) - 1;

        if (inspectorSprites != null && i < inspectorSprites.Length && inspectorSprites[i] != null)
            return inspectorSprites[i];

        if (!resourceTried[i])
        {
            resourceTried[i] = true;
            resourceSprites[i] = Resources.Load<Sprite>("DiceFaces/dice_" + (i + 1));
        }
        if (resourceSprites[i] != null) return resourceSprites[i];

        if (cache[i] == null)
        {
            Texture2D tex = BuildTexture(i + 1, false);
            cache[i] = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        }
        return cache[i];
    }

    // Vẽ texture 256x256 của 1 mặt xúc xắc. readable = true nếu cần EncodeToPNG (Editor)
    public static Texture2D BuildTexture(int face, bool readable)
    {
        Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            name = "DiceFace" + face,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Vector2[] pips = PipPositions(face);
        Color body = new Color(0.36f, 0.22f, 0.88f, 1f);
        Color edge = new Color(0.78f, 0.68f, 1f, 1f);

        float half = Size * 0.5f;
        float boxHalf = half - 1.5f;
        float corner = Size * 0.2f;
        float border = Size * 0.045f;
        float pipRadius = Size * 0.075f;

        Color32[] px = new Color32[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f - half, y + 0.5f - half);

                float sd = RoundedBoxDistance(p, boxHalf, corner); // < 0 là bên trong
                float shape = Mathf.Clamp01(0.5f - sd);
                if (shape <= 0f)
                {
                    px[y * Size + x] = new Color32(0, 0, 0, 0);
                    continue;
                }

                float inner = Mathf.Clamp01(0.5f - (sd + border));
                Color c = Color.Lerp(edge, body, inner);

                float pipCoverage = 0f;
                for (int k = 0; k < pips.Length; k++)
                {
                    Vector2 centre = (pips[k] - new Vector2(0.5f, 0.5f)) * Size;
                    float d = (p - centre).magnitude;
                    pipCoverage = Mathf.Max(pipCoverage, Mathf.Clamp01(pipRadius - d + 0.5f));
                }

                c = Color.Lerp(c, Color.white, pipCoverage);
                c.a = shape;
                px[y * Size + x] = c;
            }
        }

        tex.SetPixels32(px);
        tex.Apply(false, !readable);
        return tex;
    }

    private static float RoundedBoxDistance(Vector2 p, float halfExtent, float radius)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - new Vector2(halfExtent - radius, halfExtent - radius);
        float outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude;
        float inside = Mathf.Min(Mathf.Max(q.x, q.y), 0f);
        return outside + inside - radius;
    }

    private static Vector2[] PipPositions(int face)
    {
        const float a = 0.27f, b = 0.5f, c = 0.73f;
        switch (face)
        {
            case 1: return new[] { new Vector2(b, b) };
            case 2: return new[] { new Vector2(a, c), new Vector2(c, a) };
            case 3: return new[] { new Vector2(a, c), new Vector2(b, b), new Vector2(c, a) };
            case 4: return new[] { new Vector2(a, a), new Vector2(a, c), new Vector2(c, a), new Vector2(c, c) };
            case 5: return new[] { new Vector2(a, a), new Vector2(a, c), new Vector2(c, a), new Vector2(c, c), new Vector2(b, b) };
            default: return new[] { new Vector2(a, a), new Vector2(a, b), new Vector2(a, c), new Vector2(c, a), new Vector2(c, b), new Vector2(c, c) };
        }
    }
}
