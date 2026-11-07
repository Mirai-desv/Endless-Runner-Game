using UnityEngine;

namespace EndlessRunner.Run.World
{
    // Luật chung cho mọi chunk. Tạo 1 asset duy nhất và gán vào tất cả ChunkView.
    // Các con số này lấy từ thông số di chuyển của nhân vật
    [CreateAssetMenu(menuName = "EndlessRunner/Chunk Rules", fileName = "ChunkRules")]
    public class ChunkRules : ScriptableObject
    {
        [Header("Chuẩn ghép nối")]
        [Tooltip("Cao độ mặt đất ở hai đầu chunk (theo trục Y cục bộ của chunk)")]
        public float groundY = 0f;

        [Tooltip("Chiều dài chunk phải là bội số của giá trị này")]
        public float lengthUnit = 4f;

        [Tooltip("Sai số cho phép khi so sánh")]
        public float tolerance = 0.01f;

        [Header("Giới hạn theo khả năng nhảy của nhân vật")]
        [Tooltip("Khe hố rộng nhất mà nhân vật nhảy qua được (ở tốc độ thấp nhất và cao nhất)")]
        public float maxGapWidth = 4f;

        [Tooltip("Platform cao nhất so với mặt đất mà nhân vật nhảy lên được")]
        public float maxPlatformHeight = 2f;
    }
}