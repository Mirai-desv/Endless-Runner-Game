using UnityEngine;
using UnityEngine.InputSystem;

namespace EndlessRunner.Dev
{
    /// <summary>
    /// CHỈ DÙNG ĐỂ THỬ NGHIỆM khi Player của Người 1 chưa xong:
    /// cho một object tự trôi sang phải để generator có "người chơi" mà bám theo.
    /// Giữ phím Space (hoặc giữ chuột/chạm màn hình) để chạy nhanh gấp đôi.
    /// Dùng Input System mới.
    /// </summary>
    public class SimpleTargetMover : MonoBehaviour
    {
        [SerializeField] private float speed = 8f;

        private void Update()
        {
            bool boost = false;

            var kb = Keyboard.current;
            if (kb != null && kb.spaceKey.isPressed) boost = true;

            var pointer = Pointer.current;
            if (pointer != null && pointer.press.isPressed) boost = true;

            float s = boost ? speed * 2f : speed;
            transform.position += Vector3.right * (s * Time.deltaTime);
        }
    }
}