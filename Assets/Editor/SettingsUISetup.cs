using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;

// Menu Tools > Endless Runner > Create Settings UI in Scene.
// Tạo trong Canvas HUD: nút "SETTINGS" (góc phải trên) + Setting Panel mới
// (nền mờ, cửa sổ, tiêu đề, nút CLOSE) đã nối sẵn OnClick. Sau đó chỉnh UI thoải mái trong Unity.
public static class SettingsUISetup
{
    [MenuItem("Tools/Endless Runner/Create Settings UI in Scene")]
    public static void Create()
    {
        if (Object.FindFirstObjectByType<SettingsPanel>() != null)
        {
            EditorUtility.DisplayDialog(
                "Settings UI",
                "Scene này đã có SettingsPanel rồi. Hãy xóa nó nếu muốn tạo lại từ đầu.",
                "OK");
            return;
        }

        Canvas canvas = FindHudCanvas();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog(
                "Settings UI",
                "Không tìm thấy Canvas HUD (Canvas dùng cho UpButton/DownButton/SkillButton) trong scene.",
                "OK");
            return;
        }

        Transform parent = canvas.transform;

        // ---------- Nút mở Setting (luôn hiện) ----------
        Button openButton = AddButton(parent, "SettingsButton", "SETTINGS",
                                      new Vector2(220f, 80f), new Color(0.15f, 0.15f, 0.22f, 0.9f), 30f);
        RectTransform openRt = (RectTransform)openButton.transform;
        openRt.anchorMin = openRt.anchorMax = openRt.pivot = new Vector2(1f, 1f);
        openRt.anchoredPosition = new Vector2(-30f, -30f);

        // ---------- SettingsPanel (object luôn bật, chứa script) ----------
        RectTransform root = NewUI("SettingsPanel", parent);
        Stretch(root);
        SettingsPanel settings = root.gameObject.AddComponent<SettingsPanel>();

        // ---------- Panel mới: nền mờ phủ kín màn hình (chặn bấm xuyên xuống HUD) ----------
        RectTransform panel = NewUI("Panel", root);
        Stretch(panel);
        Image dim = panel.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.65f);

        // ---------- Cửa sổ ở giữa ----------
        RectTransform window = NewUI("Window", panel);
        window.anchorMin = window.anchorMax = window.pivot = new Vector2(0.5f, 0.5f);
        window.sizeDelta = new Vector2(700f, 450f);
        window.anchoredPosition = Vector2.zero;
        Image windowBg = window.gameObject.AddComponent<Image>();
        windowBg.color = new Color(0.12f, 0.12f, 0.18f, 0.97f);

        TextMeshProUGUI title = AddText(window, "Title", "SETTINGS", 56f, Color.white);
        RectTransform titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.sizeDelta = new Vector2(0f, 100f);
        titleRt.anchoredPosition = new Vector2(0f, -20f);

        Button closeButton = AddButton(window, "CloseButton", "CLOSE",
                                       new Vector2(260f, 80f), new Color(0.75f, 0.25f, 0.25f, 1f), 34f);
        RectTransform closeRt = (RectTransform)closeButton.transform;
        closeRt.anchorMin = closeRt.anchorMax = new Vector2(0.5f, 0f);
        closeRt.pivot = new Vector2(0.5f, 0f);
        closeRt.anchoredPosition = new Vector2(0f, 30f);

        // ---------- Nối tham chiếu & OnClick ----------
        settings.content = panel.gameObject;
        UnityEventTools.AddPersistentListener(openButton.onClick, settings.Open);
        UnityEventTools.AddPersistentListener(closeButton.onClick, settings.Close);

        // Panel ẩn sẵn (lúc chạy game cũng tự ẩn ở Start). Muốn sửa UI thì bật lại ô tick của 'Panel'.
        panel.gameObject.SetActive(false);

        Undo.RegisterCreatedObjectUndo(openButton.gameObject, "Create Settings UI");
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Create Settings UI");
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        Selection.activeGameObject = root.gameObject;

        Debug.Log("[Settings] Đã tạo 'SettingsButton' và 'SettingsPanel' trong Canvas '" + canvas.name +
                  "'. Kéo/chỉnh vị trí nút trong Hierarchy. Nhớ lưu scene (Ctrl+S).", root.gameObject);
    }

    // Canvas HUD: ưu tiên object tên "Canvas"; nếu không có thì Canvas Overlay đầu tiên không phải GameFlowUI.
    private static Canvas FindHudCanvas()
    {
        Canvas fallback = null;

        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.transform.parent != null) continue;   // chỉ lấy Canvas gốc
            if (c.name == "GameFlowUI") continue;
            if (c.name == "Canvas") return c;
            if (fallback == null) fallback = c;
        }

        return fallback;
    }

    private static RectTransform NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static TextMeshProUGUI AddText(Transform parent, string name, string text, float size, Color color)
    {
        RectTransform rt = NewUI(name, parent);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    private static Button AddButton(Transform parent, string name, string label, Vector2 size, Color bg, float fontSize)
    {
        RectTransform rt = NewUI(name, parent);
        rt.sizeDelta = size;

        Image img = rt.gameObject.AddComponent<Image>();
        img.color = bg;

        Button btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;

        TextMeshProUGUI text = AddText(rt, "Text (TMP)", label, fontSize, Color.white);
        Stretch(text.rectTransform);

        return btn;
    }
}