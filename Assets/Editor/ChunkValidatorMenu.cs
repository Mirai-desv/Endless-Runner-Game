using EndlessRunner.Run.World;
using UnityEditor;
using UnityEngine;

namespace EndlessRunner.EditorTools
{
    // Menu EndlessRunner > Validate All Chunks: kiểm tra mọi ChunkData trong project chỉ với một cú bấm
    public static class ChunkValidatorMenu
    {
        [MenuItem("EndlessRunner/Validate All Chunks")]
        private static void ValidateAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:ChunkData");
            int total = 0, bad = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<ChunkData>(path);
                if (data == null) continue;
                total++;

                if (data.prefab == null)
                {
                    Debug.LogWarning($"[{data.name}] ChunkData chưa gán prefab.", data);
                    bad++;
                    continue;
                }

                // Dựng tạm một bản của prefab ở xa gốc tọa độ để đo, xong thì xóa
                var inst = (ChunkView)PrefabUtility.InstantiatePrefab(data.prefab);
                inst.transform.position = new Vector3(0f, -1000f, 0f);

                var issues = inst.Validate();
                if (issues.Count > 0)
                {
                    bad++;
                    foreach (var s in issues)
                        Debug.LogWarning($"[{data.prefab.name}] {s}", data.prefab);
                }

                Object.DestroyImmediate(inst.gameObject);
            }

            Debug.Log($"Validate All Chunks: {total - bad}/{total} chunk đạt chuẩn.");
        }
    }
}