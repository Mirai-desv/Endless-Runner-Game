using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EndlessRunner.Run.Controls
{
    /// <summary>
    /// Đọc phím và gọi PlayerController.Jump() / DropDown() (có sẵn, public).
    /// Hoạt động với cả Input System mới lẫn Input Manager cũ, không phụ thuộc
    /// cấu hình "Active Input Handling" trên máy.
    /// Gắn vào GameObject Player. Không cần sửa file nào trong Assets/Script.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerInputBridge : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [Tooltip("In log mỗi lần nhận phím và log cấu hình input lúc bắt đầu chơi")]
        [SerializeField] private bool logInput = true;

        private bool warnedNoKeyboard;

        private void Reset()
        {
            player = GetComponent<PlayerController>();
        }

        private void Awake()
        {
            if (player == null) player = GetComponent<PlayerController>();
        }

        private void Start()
        {
            if (!logInput) return;

            string newInput =
#if ENABLE_INPUT_SYSTEM
                "BẬT";
#else
                "TẮT";
#endif
            string oldInput =
#if ENABLE_LEGACY_INPUT_MANAGER
                "BẬT";
#else
                "TẮT";
#endif
            Debug.Log($"[PlayerInputBridge] Input System mới: {newInput} | Input Manager cũ: {oldInput} | player: {(player != null ? "OK" : "THIẾU")}", this);
        }

        private void Update()
        {
            if (player == null) return;

            bool jump = false;
            bool drop = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                jump |= kb.spaceKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;
                drop |= kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame;
            }
            else if (logInput && !warnedNoKeyboard)
            {
                warnedNoKeyboard = true;
                Debug.LogWarning("[PlayerInputBridge] Keyboard.current = null: Unity chưa nhận bàn phím (Game view chưa focus, hoặc bộ gõ/IME đang chặn phím).", this);
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            jump |= UnityEngine.Input.GetKeyDown(KeyCode.Space) || UnityEngine.Input.GetKeyDown(KeyCode.UpArrow);
            drop |= UnityEngine.Input.GetKeyDown(KeyCode.DownArrow) || UnityEngine.Input.GetKeyDown(KeyCode.S);
#endif

            // Jump() và DropDown() tự kiểm tra isGrounded / isDropping,
            // nên gọi trùng với PlayerController trong cùng frame cũng không sao.
            if (jump)
            {
                if (logInput) Debug.Log("[PlayerInputBridge] Nhận phím NHẢY", this);
                player.Jump();
            }

            if (drop)
            {
                if (logInput) Debug.Log("[PlayerInputBridge] Nhận phím XUỐNG", this);
                player.DropDown();
            }
        }
    }
}