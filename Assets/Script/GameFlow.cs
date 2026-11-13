using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameFlowState
{
    Menu,            // Màn hình "Tap to play"
    PickLuckyNumber, // Màn hình chọn số may mắn
    Playing,         // Đang chơi
    DiceRoll         // Đang quay xúc xắc (game tạm dừng)
}

// Điều phối luồng: Tap to play -> chọn số may mắn -> chơi -> (chạm Dice) quay xúc xắc -> chơi tiếp.
// Cách dùng khuyên dùng: menu Tools > Endless Runner > Create Game Flow UI in Scene
// -> sinh ra Canvas "GameFlowUI" + object "GameFlow" đã nối sẵn. Sau đó chỉnh UI thoải mái trong Unity.
// Nếu scene KHÔNG có GameFlow, component này vẫn tự được tạo khi scene có PlayerController
// và dựng UI mặc định bằng code (xem Bootstrap) nên game vẫn chạy bình thường.

[DisallowMultipleComponent]
public class GameFlow : MonoBehaviour
{
    public static GameFlow Instance { get; private set; }

    // True khi người chơi chưa được điều khiển nhân vật (menu, chọn số, quay xúc xắc).
    // Các script input (PlayerInputBridge, PlayerCombat) kiểm tra cờ này.
    // Khóa thêm 1 frame sau khi vào Playing để cú chạm "Tap to play" không bị tính là cú nhảy.
    public static bool InputLocked =>
        SettingsPanel.IsOpen ||
        (Instance != null &&
        (Instance.State != GameFlowState.Playing || Time.frameCount <= Instance.unlockFrame));

    [Header("UI (object thật trong scene - chỉnh thoải mái trong Unity)")]
    [Tooltip("Tạo sẵn bằng menu Tools > Endless Runner > Create Game Flow UI in Scene. " +
             "Nếu để trống, lúc chạy game sẽ tự dựng UI mặc định bằng code.")]
    public GameFlowUIRefs ui = new GameFlowUIRefs();

    [Header("Ảnh xúc xắc riêng (không bắt buộc)")]
    [Tooltip("Mặt 1..6 dùng cho xúc xắc lớn khi quay. Ô trống sẽ lấy ảnh đang gán trên " +
             "ô chọn số tương ứng (ui.luckyDice), rồi tới Resources/DiceFaces/dice_N, cuối cùng là ảnh tự sinh.")]
    public Sprite[] diceFaceSprites = new Sprite[6];

    [Header("Menu (chỉ dùng khi UI được tự dựng bằng code)")]
    public string gameTitle = "ENDLESS RUNNER";
    public string tapToPlayText = "TAP TO PLAY";
    [Tooltip("Chữ Tap to play nhấp nháy.")]
    public bool pulseTapText = true;

    [Header("Dice roll - thời gian (tất cả đều chỉnh được)")]
    [Tooltip("Thời gian (giây) xúc xắc đổi mặt liên tục ở tốc độ nhanh.")]
    [Min(0f)] public float spinDuration = 3f;
    [Tooltip("Khoảng cách (giây) giữa 2 lần đổi mặt ở giai đoạn nhanh.")]
    [Min(0.01f)] public float fastInterval = 0.06f;
    [Tooltip("Sau giai đoạn nhanh, mỗi lần đổi mặt khoảng cách được nhân với số này (>1 = chậm dần).")]
    [Min(1.01f)] public float slowdownGrowth = 1.25f;
    [Tooltip("Khi khoảng cách giữa 2 lần đổi mặt vượt mức này (giây) thì dừng hẳn.")]
    [Min(0.05f)] public float stopInterval = 0.7f;
    [Tooltip("Thời gian (giây) giữ màn hình kết quả trước khi quay lại game.")]
    [Min(0f)] public float resultHoldTime = 1.5f;

    [Header("Dice roll - chữ & màu hiển thị")]

    public string rollingText = "ROLLING...";
    public string luckyTitleText = "LUCKY!";
    public string luckyResultText = "FULL ENERGY!";
    public string notLuckyTitleText = "NOT LUCKY";
    [Tooltip("{0} = số điểm năng lượng được cộng.")]
    public string notLuckyResultFormat = "+{0} ENERGY";
    public Color luckyColor = new Color(1f, 0.85f, 0.25f);
    public Color normalColor = Color.white;

    [Header("Tutorial Mode")]
    public bool tutorialMode = false;

    public event Action<int> OnTutorialLuckyNumberChosen;

    private bool tutorialPickingNumber;
    private float timeScaleBeforeTutorialPick = 1f;

    public GameFlowState State { get; private set; } = GameFlowState.Menu;

    // Số may mắn người chơi chọn ở đầu game (1-6). 0 = chưa chọn
    public int LuckyNumber { get; private set; }

    private int unlockFrame;
    private float timeScaleBeforeRoll = 1f;
    private bool started;

    private FlowScreens screens;

    // ------------------------------------------------------------------ Bootstrap

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterBootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // tránh đăng ký trùng khi tắt Domain Reload
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (FindAnyObjectByType<PlayerController>() == null) return; // không phải scene chơi
        if (FindAnyObjectByType<GameFlow>() != null) return;         // đã có sẵn trong scene

        new GameObject("GameFlow").AddComponent<GameFlow>();
    }

    // ------------------------------------------------------------------ Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DiceFaces.SetInspectorSprites(diceFaceSprites);
        if (ui == null || !ui.IsComplete) BuildFallbackUI();
        DiceFaces.SetInspectorSprites(ResolveFaceSprites());

        screens = FlowScreens.Build(this);
    }

    // Scene chưa có UI (hoặc thiếu tham chiếu) -> tự dựng bản mặc định bằng code.
    private void BuildFallbackUI()
    {
        if (ui != null && (ui.menuPanel != null || ui.luckyPanel != null || ui.rollPanel != null))
            Debug.LogWarning("[GameFlow] UI chưa gán đủ tham chiếu (xem mục UI trong Inspector) -> dùng UI mặc định dựng bằng code.", this);

        GameObject canvas = GameFlowLayout.CreateCanvas(transform, "GameFlowCanvas");
        ui = GameFlowLayout.Build(canvas.transform, gameTitle, tapToPlayText);
    }

    // Ưu tiên: ảnh trong diceFaceSprites -> ảnh đang gán trên ô chọn số -> (DiceFaces tự xử lý phần còn lại).
    private Sprite[] ResolveFaceSprites()
    {
        Sprite[] result = new Sprite[6];
        for (int i = 0; i < 6; i++)
        {
            Sprite s = diceFaceSprites != null && i < diceFaceSprites.Length ? diceFaceSprites[i] : null;
            if (s == null && ui != null && ui.luckyDice != null && i < ui.luckyDice.Length && ui.luckyDice[i] != null)
            {
                UnityEngine.UI.Image img = ui.luckyDice[i].GetComponent<UnityEngine.UI.Image>();
                if (img != null) s = img.sprite;
            }
            result[i] = s;
        }
        return result;
    }

    // Dùng Start (không phải Awake) vì GameManager.Awake đặt timeScale = 1,
    // Start chạy sau mọi Awake nên chắc chắn đè được.
    private void Start()
    {
        if (Instance != this || started) return;
        started = true;

        if (tutorialMode)
        {
            Time.timeScale = 1f;
            SetState(GameFlowState.Playing);
        }
        else
        {
            Time.timeScale = 0f;
            SetState(GameFlowState.Menu);
        }
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;

        // Thoát giữa chừng (đổi scene lúc đang menu/quay) thì trả lại timeScale.
        if (State != GameFlowState.Playing) Time.timeScale = 1f;
    }

    private void Update()
    {
        if (Instance != this) return;
        screens.Tick();
    }

    // ------------------------------------------------------------------ Flow

    private void SetState(GameFlowState next)
    {
        State = next;
        if (next == GameFlowState.Playing) unlockFrame = Time.frameCount;

        screens.menu.SetVisible(next == GameFlowState.Menu);
        screens.luckyPicker.SetVisible(next == GameFlowState.PickLuckyNumber);
        screens.diceRoll.SetVisible(next == GameFlowState.DiceRoll);

        // Gameplay UI chỉ xuất hiện khi đang chơi
        bool showGameplayUI = next == GameFlowState.Playing;

        if (ui.scoreText != null) ui.scoreText.SetActive(showGameplayUI);
        if (ui.energyBar != null) ui.energyBar.SetActive(showGameplayUI);
        if (ui.upButton != null) ui.upButton.SetActive(showGameplayUI);
        if (ui.downButton != null) ui.downButton.SetActive(showGameplayUI);
        if (ui.skillButton != null) ui.skillButton.SetActive(showGameplayUI);
    }

    // Người chơi chạm màn hình ở menu -> sang chọn số may mắn
    internal void OnTapToPlay()
    {
        if (State != GameFlowState.Menu) return;
        SetState(GameFlowState.PickLuckyNumber);
    }

    // Người chơi chọn xong số may mắn -> bắt đầu chạy
    internal void OnLuckyNumberChosen(int number)
    {
        if (State != GameFlowState.PickLuckyNumber) return;

        LuckyNumber = Mathf.Clamp(number, 1, 6);

        if (tutorialMode && tutorialPickingNumber)
        {
            tutorialPickingNumber = false;

            Time.timeScale = timeScaleBeforeTutorialPick;
            SetState(GameFlowState.Playing);

            OnTutorialLuckyNumberChosen?.Invoke(LuckyNumber);
            return;
        }

        Time.timeScale = 1f;
        SetState(GameFlowState.Playing);
    }

    /// Mở màn hình quay xúc xắc, tạm dừng game. Khi dừng hẳn sẽ gọi onFinished(mặt cuối cùng)
    /// rồi game chạy tiếp. normalReward chỉ dùng để hiện chữ "+N ENERGY" khi không trúng.
    /// Trả về false nếu không mở được (chưa vào game / đang quay).
    public bool StartDiceRoll(int normalReward, Action<int> onFinished)
    {
        if (State != GameFlowState.Playing) return false;

        timeScaleBeforeRoll = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        SetState(GameFlowState.DiceRoll);

        screens.diceRoll.Play(
            LuckyNumber,
            normalReward,
            result =>
            {
                onFinished?.Invoke(result);
                Time.timeScale = timeScaleBeforeRoll;
                SetState(GameFlowState.Playing);
            });
        return true;
    }

    public bool OpenTutorialLuckyPicker()
    {
        if (!tutorialMode)
            return false;

        if (State != GameFlowState.Playing)
            return false;

        tutorialPickingNumber = true;

        timeScaleBeforeTutorialPick =
            Time.timeScale > 0f ? Time.timeScale : 1f;

        Time.timeScale = 0f;

        SetState(GameFlowState.PickLuckyNumber);

        return true;
    }

}
