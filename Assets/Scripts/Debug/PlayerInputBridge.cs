using UnityEngine;
using UnityEngine.InputSystem;

namespace EndlessRunner.Run.Controls
{
    /// Cửa duy nhất để điều khiển Player. PlayerController không tự đọc input.
    ///
    /// Có 2 nguồn input, cùng gọi chung các hàm RequestXxx():
    ///   1. Bàn phím: đọc trong Update(), đổi phím ngay trong Inspector.
    ///   2. Nút UI:   gán trong Inspector (Button > On Click, hoặc Event Trigger > Pointer Down)
    ///                rồi chọn PlayerInputBridge > RequestJump / RequestDrop.
    ///
    /// Tất cả đều bị khóa khi GameFlow.InputLocked = true.

    [DisallowMultipleComponent]
    public class PlayerInputBridge : MonoBehaviour
    {
        [Header("Tham chiếu")]
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerCombat playerCombat;

        [Header("Phím điều khiển")]
        [SerializeField] private Key[] jumpKeys = { Key.W, Key.UpArrow };
        [SerializeField] private Key[] dropKeys = { Key.S, Key.DownArrow };
        [SerializeField] private Key[] skillKeys = { Key.Space };

        [Header("Debug")]
        [Tooltip("In log mỗi lần nhận lệnh (từ phím hoặc từ nút).")]
        [SerializeField] private bool logInput = false;

        private void Reset()
        {
            player = GetComponent<PlayerController>();
        }

        private void Awake()
        {
            if (player == null)
                player = GetComponent<PlayerController>();

            if (player == null)
                Debug.LogError("[PlayerInputBridge] Không tìm thấy PlayerController.", this);
        }

        // ------------------------------------------------------------------
        // NGUỒN 1: BÀN PHÍM
        // ------------------------------------------------------------------
        private void Update()
        {
            if (WasPressed(jumpKeys)) RequestJump();
            if (WasPressed(dropKeys)) RequestDrop();
            if (WasPressed(skillKeys)) RequestSkill();
        }

        // ------------------------------------------------------------------
        // NGUỒN 2: NÚT UI (gọi các hàm public này từ Inspector)
        // ------------------------------------------------------------------
        public void RequestJump()
        {
            if (!CanControl()) return;

            Log("Nhảy");
            player.Jump();
        }

        public void RequestDrop()
        {
            if (!CanControl()) return;

            Log("Xuống");
            player.DropDown();
        }

        public void RequestSkill()
        {
            if (!CanControl()) return;
            Log("Bắn");
            playerCombat.Shoot();
        }

        // THÊM HÀNH ĐỘNG MỚI (ví dụ lướt), làm 3 việc:
        //   1. Viết hàm Dash() public trong PlayerController.
        //   2. Thêm hàm ở đây:
        //        public void RequestDash()
        //        {
        //            if (!CanControl()) return;
        //            Log("Lướt");
        //            player.Dash();
        //        }
        //   3. Khai báo phím (nếu cần) và thêm một dòng trong Update():
        //        [SerializeField] private Key[] dashKeys = { Key.LeftShift };
        //        if (WasPressed(dashKeys)) RequestDash();
        //   Muốn có nút UI cho Dash: gán RequestDash vào Button trong Inspector.

        // ------------------------------------------------------------------
        // HÀM PHỤ
        // ------------------------------------------------------------------

        // Player có đang được phép nhận lệnh không (menu, chọn số, xúc xắc thì không)
        private bool CanControl()
        {
            return player != null && !GameFlow.InputLocked;
        }

        // True nếu có bất kỳ phím nào trong danh sách vừa được bấm trong frame này
        private static bool WasPressed(Key[] keys)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || keys == null) return false;

            foreach (Key key in keys)
            {
                // Key.None là giá trị mặc định khi thêm ô mới trong Inspector, bỏ qua.
                if (key == Key.None) continue;

                if (keyboard[key].wasPressedThisFrame)
                    return true;
            }

            return false;
        }

        private void Log(string action)
        {
            if (logInput)
                Debug.Log($"[PlayerInputBridge] {action}", this);
        }
    }
}