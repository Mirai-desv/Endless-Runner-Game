using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Bật/tắt Setting Panel khi bấm Button.
// Gắn lên 1 object LUÔN ĐANG BẬT (không phải chính panel), kéo panel vào ô "Content".
// Nối Button như sau:
//   SettingsButton  -> OnClick -> SettingsPanel.Open
//   CloseButton     -> OnClick -> SettingsPanel.Close
// Hoặc dùng menu Tools > Endless Runner > Create Settings UI in Scene để tạo sẵn tất cả.
public class SettingsPanel : MonoBehaviour
{
    /// True khi panel đang mở. GameFlow.InputLocked đọc cờ này để khóa nhảy/bắn.
    public static bool IsOpen { get; private set; }

    [Header("Tham chiếu")]
    [Tooltip("Object panel sẽ được bật/tắt (ví dụ object 'Panel').")]
    public GameObject content;

    [Header("Hành vi")]
    [Tooltip("Tạm dừng game (Time.timeScale = 0) khi panel đang mở.")]
    public bool pauseGameWhileOpen = true;

    [Tooltip("Cho phép phím Esc đóng/mở panel (khi test trên máy tính).")]
    public bool toggleWithEscape = true;

    private bool pausedByThis;
    private float timeScaleBefore = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => IsOpen = false;

    private void Start()
    {
        // Luôn bắt đầu ở trạng thái đóng.
        if (content != null) content.SetActive(false);
        IsOpen = false;
    }

    private void Update()
    {
        if (toggleWithEscape && EscapePressed())
            Toggle();
    }

    public void Open()
    {
        if (IsOpen || content == null) return;

        content.SetActive(true);
        IsOpen = true;

        if (pauseGameWhileOpen)
        {
            timeScaleBefore = Time.timeScale;
            Time.timeScale = 0f;
            pausedByThis = true;
        }
    }

    public void Close()
    {
        if (!IsOpen) return;

        if (content != null) content.SetActive(false);
        IsOpen = false;

        // Chỉ trả lại timeScale nếu chính panel này đã tạm dừng game.
        if (pausedByThis)
        {
            Time.timeScale = timeScaleBefore;
            pausedByThis = false;
        }
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    private void OnDestroy()
    {
        // Đổi scene / thoát khi đang mở: trả lại trạng thái bình thường.
        if (IsOpen) Close();
    }

    private static bool EscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape)) return true;
#endif
        return false;
    }
}