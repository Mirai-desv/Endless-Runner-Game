using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Logic của 3 màn: Tap to play / Chọn số may mắn / Quay xúc xắc.
// Các màn chỉ ĐỌC UI từ GameFlow.ui (object thật trong scene, chỉnh được trong Unity),
// không tự tạo UI. Chỉ dùng thời gian "unscaled" vì game bị tạm dừng (Time.timeScale = 0).
// Việc bấm được xử lý bằng hit-test nên không phụ thuộc EventSystem / InputModule của scene.

// ============================================================================ Container

internal sealed class FlowScreens
{
    public MenuScreen menu;
    public LuckyPickerScreen luckyPicker;
    public DiceRollScreen diceRoll;

    private GameFlow flow;

    public static FlowScreens Build(GameFlow flow)
    {
        FlowScreens s = new FlowScreens();
        s.flow = flow;
        s.menu = new MenuScreen(flow, flow.ui);
        s.luckyPicker = new LuckyPickerScreen(flow, flow.ui);
        s.diceRoll = new DiceRollScreen(flow, flow.ui);

        s.menu.SetVisible(false);
        s.luckyPicker.SetVisible(false);
        s.diceRoll.SetVisible(false);
        return s;
    }

    public void Tick()
    {
        switch (flow.State)
        {
            case GameFlowState.Menu: menu.Tick(); break;
            case GameFlowState.PickLuckyNumber: luckyPicker.Tick(); break;
        }
    }
}

internal abstract class FlowScreen
{
    protected GameObject root;

    public void SetVisible(bool visible)
    {
        if (root.activeSelf == visible) return;
        root.SetActive(visible);
        if (visible) OnShow();
    }

    protected virtual void OnShow() { }
}

// ============================================================================ 1. Tap to play

internal sealed class MenuScreen : FlowScreen
{
    private readonly GameFlow flow;
    private readonly TextMeshProUGUI tapText;
    private readonly float baseAlpha;

    public MenuScreen(GameFlow flow, GameFlowUIRefs ui)
    {
        this.flow = flow;
        root = ui.menuPanel;
        tapText = ui.tapToPlayText;
        if (tapText != null) baseAlpha = tapText.color.a;
    }

    public void Tick()
    {
        // Chữ nhấp nháy nhẹ cho người chơi biết là có thể bấm.
        if (flow.pulseTapText && tapText != null)
        {
            Color c = tapText.color;
            c.a = baseAlpha * (0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 3f));
            tapText.color = c;
        }

        // Chạm / click BẤT KỲ ĐÂU trên màn hình (hoặc Enter khi test trên máy tính).
        if (FlowInput.PointerDown(out _) || FlowInput.ConfirmKeyDown())
            flow.OnTapToPlay();
    }
}

// ============================================================================ 2. Chọn số may mắn

internal sealed class LuckyPickerScreen : FlowScreen
{
    private readonly GameFlow flow;
    private readonly RectTransform[] dice;
    private readonly Graphic[] graphics;
    private readonly Vector3[] baseScale = new Vector3[6];
    private readonly Color[] baseColor = new Color[6];
    private bool picked;

    public LuckyPickerScreen(GameFlow flow, GameFlowUIRefs ui)
    {
        this.flow = flow;
        root = ui.luckyPanel;
        dice = ui.luckyDice;
        graphics = new Graphic[6];

        for (int i = 0; i < 6; i++)
        {
            baseScale[i] = dice[i].localScale;
            graphics[i] = dice[i].GetComponent<Graphic>();
            baseColor[i] = graphics[i] != null ? graphics[i].color : Color.white;
        }
    }

    protected override void OnShow()
    {
        picked = false;
        for (int i = 0; i < 6; i++)
        {
            dice[i].localScale = baseScale[i];
            if (graphics[i] != null) graphics[i].color = baseColor[i];
        }
    }

    public void Tick()
    {
        if (picked) return;
        if (!FlowInput.PointerDown(out Vector2 pos)) return;

        for (int i = 0; i < dice.Length; i++)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(dice[i], pos, CameraFor(dice[i])))
            {
                picked = true;
                flow.StartCoroutine(PickRoutine(i + 1));
                return;
            }
        }
    }

    // Canvas Overlay không cần camera; Screen Space - Camera / World Space thì cần.
    private static Camera CameraFor(RectTransform rt)
    {
        Canvas canvas = rt.GetComponentInParent<Canvas>();
        if (canvas == null) return null;
        canvas = canvas.rootCanvas;
        return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }

    private IEnumerator PickRoutine(int face)
    {
        // Mặt được chọn phóng to, các mặt còn lại mờ đi, rồi vào game.
        const float duration = 0.4f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            for (int i = 0; i < dice.Length; i++)
            {
                if (i == face - 1)
                {
                    dice[i].localScale = baseScale[i] * Mathf.Lerp(1f, 1.25f, k);
                }
                else if (graphics[i] != null)
                {
                    Color c = baseColor[i];
                    c.a = Mathf.Lerp(baseColor[i].a, baseColor[i].a * 0.25f, k);
                    graphics[i].color = c;
                }
            }
            yield return null;
        }

        flow.OnLuckyNumberChosen(face);
    }
}

// ============================================================================ 3. Quay xúc xắc

internal sealed class DiceRollScreen : FlowScreen
{
    private readonly GameFlow flow;
    private readonly Image die;
    private readonly Image luckyIcon;
    private readonly TextMeshProUGUI title;
    private readonly TextMeshProUGUI result;
    private readonly Vector3 dieBaseScale;
    private readonly Color dieBaseColor;

    public DiceRollScreen(GameFlow flow, GameFlowUIRefs ui)
    {
        this.flow = flow;
        root = ui.rollPanel;
        die = ui.rollDie;
        luckyIcon = ui.luckyIcon;
        title = ui.rollTitle;
        result = ui.rollResult;
        dieBaseScale = die.rectTransform.localScale;
        dieBaseColor = die.color;
    }

    // Chạy hiệu ứng quay; gọi onDone(mặt cuối cùng) sau khi đã hiện kết quả
    public void Play(int luckyNumber, int normalReward, Action<int> onDone)
    {
        flow.StartCoroutine(Run(luckyNumber, normalReward, onDone));
    }

    private IEnumerator Run(int luckyNumber, int normalReward, Action<int> onDone)
    {
        SetText(title, flow.rollingText, flow.normalColor);
        SetText(result, "", flow.normalColor);
        die.color = dieBaseColor;
        if (luckyIcon != null) luckyIcon.sprite = DiceFaces.Get(luckyNumber);

        int face = NextFace(0);
        Show(face);

        float fast = Mathf.Max(0.01f, flow.fastInterval);
        float interval = fast;
        float timer = 0f;
        float elapsed = 0f;

        while (true)
        {
            yield return null;

            float dt = Time.unscaledDeltaTime;
            elapsed += dt;
            timer += dt;

            // "Nảy" nhẹ mỗi khi đổi mặt rồi co về kích thước gốc.
            die.rectTransform.localScale =
                Vector3.Lerp(die.rectTransform.localScale, dieBaseScale, 15f * dt);

            if (timer < interval) continue;
            timer -= interval;

            // Hết giai đoạn quay nhanh: mỗi lần đổi mặt, khoảng chờ kế tiếp dài dần.
            if (elapsed > flow.spinDuration)
            {
                interval *= flow.slowdownGrowth;
                if (interval > flow.stopInterval) break; // dừng hẳn, mặt đang hiện là kết quả
            }

            face = NextFace(face);
            Show(face);
        }


        // Tutorial: ép mặt cuối trùng số may mắn.
        // Các mặt trong lúc quay vẫn ngẫu nhiên.
        if (flow.tutorialMode)
        {
            face = Mathf.Clamp(luckyNumber, 1, 6);
            Show(face);

            Debug.Log(
                "[Tutorial Dice] Forced lucky result: " + face
            );
        }


        bool lucky = face == luckyNumber;
        if (lucky)
        {
            SetText(title, flow.luckyTitleText, flow.luckyColor);
            SetText(result, flow.luckyResultText, flow.luckyColor);
            die.color = Color.Lerp(dieBaseColor, flow.luckyColor, 0.35f);
            die.rectTransform.localScale = dieBaseScale * 1.3f;
        }
        else
        {
            SetText(title, flow.notLuckyTitleText, flow.normalColor);
            SetText(result, string.Format(flow.notLuckyResultFormat, normalReward), flow.normalColor);
            die.rectTransform.localScale = dieBaseScale * 1.1f;
        }

        // Giữ màn kết quả một lúc, xúc xắc co dần về cỡ gốc.
        float hold = 0f;
        while (hold < flow.resultHoldTime)
        {
            float dt = Time.unscaledDeltaTime;
            hold += dt;
            die.rectTransform.localScale =
                Vector3.Lerp(die.rectTransform.localScale, dieBaseScale, 8f * dt);
            yield return null;
        }

        onDone?.Invoke(face);
    }

    private static void SetText(TextMeshProUGUI label, string text, Color color)
    {
        if (label == null) return;
        label.text = text;
        label.color = color;
    }

    private void Show(int face)
    {
        die.sprite = DiceFaces.Get(face);
        die.rectTransform.localScale = dieBaseScale * 1.12f;
    }

    // Chọn ngẫu nhiên 1-6, luôn khác mặt trước đó để mỗi lần đổi đều nhìn thấy được.
    // (Chọn trong 5 mặt còn lại nên vẫn đều nhau: mặt cuối cùng vẫn có xác suất 1/6.)
    private static int NextFace(int previous)
    {
        if (previous < 1) return UnityEngine.Random.Range(1, 7);
        int r = UnityEngine.Random.Range(1, 6);
        return r >= previous ? r + 1 : r;
    }
}

// ============================================================================ Input

internal static class FlowInput
{
    /// <summary>Có lượt nhấn chuột trái / chạm màn hình trong frame này không.</summary>
    public static bool PointerDown(out Vector2 position)
    {
        position = default;
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            position = mouse.position.ReadValue();
            return true;
        }

        Touchscreen touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
        {
            position = touch.primaryTouch.position.ReadValue();
            return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (UnityEngine.Input.GetMouseButtonDown(0))
        {
            position = UnityEngine.Input.mousePosition;
            return true;
        }

        if (UnityEngine.Input.touchCount > 0)
        {
            UnityEngine.Touch t = UnityEngine.Input.GetTouch(0);
            if (t.phase == UnityEngine.TouchPhase.Began)
            {
                position = t.position;
                return true;
            }
        }
#endif
        return false;
    }

    public static bool ConfirmKeyDown()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.enterKey.wasPressedThisFrame)
            return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (UnityEngine.Input.GetKeyDown(KeyCode.Return))
            return true;
#endif
        return false;
    }
}