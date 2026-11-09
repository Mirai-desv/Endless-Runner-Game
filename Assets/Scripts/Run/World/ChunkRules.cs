using UnityEngine;

namespace EndlessRunner.Run.World
{
    /// "Luật chung" cho mọi chunk. Chỉ cần 1 asset, gán vào tất cả ChunkView.
    /// Các thông số nhảy/tốc độ do Người 1 (Player) cung cấp; từ đó tự suy ra
    /// khe hố rộng nhất, vật cản cao/rộng nhất, khoảng cách tối thiểu giữa các chướng ngại.
    [CreateAssetMenu(menuName = "EndlessRunner/Chunk Rules", fileName = "ChunkRules")]
    public class ChunkRules : ScriptableObject
    {
        [Header("Chuẩn ghép nối")]
        [Tooltip("Cao độ mặt đất ở hai đầu chunk (trục Y cục bộ của chunk)")]
        public float groundY = 0f;

        [Tooltip("Chiều dài chunk phải là bội số của giá trị này")]
        public float lengthUnit = 4f;

        [Tooltip("Sai số cho phép khi so sánh")]
        public float tolerance = 0.05f;

        [Header("Thông số nhảy (lấy từ Người 1)")]
        [Tooltip("Vận tốc dọc ngay khi bật nhảy (đơn vị unit/giây). Nếu dùng AddForce Impulse thì = lực / khối lượng")]
        public float jumpVelocity = 12f;

        [Tooltip("Gravity Scale của Rigidbody2D nhân vật")]
        public float gravityScale = 3f;

        [Header("Tốc độ chạy (unit/giây)")]
        public float minRunSpeed = 6f;
        [Tooltip("Tốc độ khi bật boost")]
        public float maxRunSpeed = 10f;

        [Header("Người chơi")]
        [Tooltip("Thời gian người chơi cần để nhìn thấy và phản ứng với chướng ngại tiếp theo")]
        public float reactionTime = 0.3f;

        [Range(0.5f, 1f), Tooltip("Hệ số an toàn: 0.85 nghĩa là chỉ cho dùng 85% khả năng nhảy thực tế")]
        public float safetyMargin = 0.85f;

        // Giá trị suy ra

        public float Gravity => Mathf.Abs(Physics2D.gravity.y) * gravityScale;

        // Tổng thời gian trên không của một cú nhảy (đáp xuống cùng độ cao)
        public float AirTime => Gravity > 0.0001f ? 2f * jumpVelocity / Gravity : 0f;

        // Độ cao nhảy tối đa lý thuyết
        public float MaxJumpHeight => Gravity > 0.0001f ? jumpVelocity * jumpVelocity / (2f * Gravity) : 0f;

        // Độ cao tối đa cho phép của vật cản/platform (đã nhân hệ số an toàn)
        public float SafeJumpHeight => MaxJumpHeight * safetyMargin;

        // Bề rộng tối đa của chướng ngại cao h mà nhân vật chạy với tốc độ speed vẫn nhảy qua được: bằng quãng đường đi được trong khoảng thời gian nhân vật ở cao hơn h.
        // h = 0 là khe hố rộng nhất.
        public float ClearWidth(float h, float speed)
        {
            float g = Gravity;
            if (g <= 0.0001f) return 0f;
            float disc = jumpVelocity * jumpVelocity - 2f * g * h;
            if (disc <= 0f) return 0f;
            float timeAbove = 2f * Mathf.Sqrt(disc) / g;
            return speed * timeAbove * safetyMargin;
        }

        public float MaxGapWidth => ClearWidth(0f, minRunSpeed);

        // Khoảng cách tối thiểu giữa hai chướng ngại liên tiếp (cao hA và hB) ở tốc độ speed: phải đủ để đáp xuống rồi còn kịp phản ứng cho cú nhảy tiếp theo.
        public float RequiredSpacing(float hA, float hB, float speed)
        {
            float airDistance = speed * AirTime;
            float avgClear = (ClearWidth(hA, speed) + ClearWidth(hB, speed)) * 0.5f;
            return Mathf.Max(0f, airDistance - avgClear + speed * reactionTime);
        }
    }
}