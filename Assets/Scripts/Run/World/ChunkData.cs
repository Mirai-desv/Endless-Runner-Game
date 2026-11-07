using UnityEngine;

namespace EndlessRunner.Run.World
{
    /// Metadata của một chunk. Mỗi chunk prefab có 1 asset ChunkData đi kèm.
    // ChunkGenerator sẽ đọc các asset này để chọn chunk theo độ khó.
    [CreateAssetMenu(menuName = "EndlessRunner/Chunk Data", fileName = "ChunkData_")]
    public class ChunkData : ScriptableObject
    {
        public ChunkView prefab;

        [Range(1, 5)] public int difficulty = 1;

        [Tooltip("Ví dụ: safe, gap, platform, rest")]
        public string[] tags;

        [Header("Tốc độ hợp lệ (0 = không giới hạn)")]
        public float minSpeed = 0f;
        public float maxSpeed = 0f;

        [Min(0f), Tooltip("Trọng số khi bốc ngẫu nhiên")]
        public float weight = 1f;

        public float Length => prefab != null ? prefab.Length : 0f;

        public bool HasTag(string tag)
        {
            if (tags == null) return false;
            foreach (var t in tags)
                if (t == tag) return true;
            return false;
        }
    }
}