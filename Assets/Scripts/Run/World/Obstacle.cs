using UnityEngine;

namespace EndlessRunner.Run.World
{
    /// <summary>
    /// Gắn vào mỗi prefab vật cản. Người 1 có thể dùng component này để nhận biết "đã chạm vật cản".
    /// Trong Scene View, khung bao của vật cản hiện XANH nếu vượt được, ĐỎ nếu không vượt được
    /// (tính ở tốc độ chạy thấp nhất, dựa trên ChunkRules của chunk chứa nó).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Obstacle : MonoBehaviour
    {
        [Tooltip("Chạm vào có chết không (để Người 1 đọc)")]
        public bool lethal = true;

        private void OnDrawGizmos()
        {
            var col = GetComponent<Collider2D>();
            if (col == null) return;

            Bounds b = col.bounds;
            Color color = Color.yellow;

            var chunk = GetComponentInParent<ChunkView>();
            if (chunk != null && chunk.Rules != null)
            {
                var r = chunk.Rules;
                float baseY = chunk.transform.position.y + r.groundY;
                float h = b.max.y - baseY;
                float w = b.size.x;
                bool ok = h <= r.SafeJumpHeight && w <= r.ClearWidth(h, r.minRunSpeed);
                color = ok ? new Color(0.2f, 0.9f, 0.3f) : new Color(1f, 0.2f, 0.2f);
            }

            Gizmos.color = color;
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
}