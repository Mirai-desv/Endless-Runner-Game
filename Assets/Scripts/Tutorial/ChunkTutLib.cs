using System.Collections.Generic;
using UnityEngine;

namespace EndlessRunner.Run.World
{
    // Danh sách tất cả ChunkData mà generator được phép dùng
    [CreateAssetMenu(menuName = "EndlessRunner/Chunk Tut Lib", fileName = "ChunkTutLib")]
    public class ChunkTutLib : ScriptableObject
    {
        public List<ChunkData> chunks = new();

#if UNITY_EDITOR
        [ContextMenu("Tìm tất cả ChunkData trong project")]
        private void FindAll()
        {
            chunks.Clear();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:ChunkData"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var d = UnityEditor.AssetDatabase.LoadAssetAtPath<ChunkData>(path);
                if (d != null) chunks.Add(d);
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}