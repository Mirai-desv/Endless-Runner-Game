using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessRunner.Run.World
{
    // Hàng thử nghiệm thôi: nối một dãy chunk trong Scene để kiểm tra bằng mắt, đồng thời in cảnh báo nếu chunk nào sai chuẩn. Sau này sẽ thay bằng ChunkGenerator.
    public class ChunkPreviewSpawner : MonoBehaviour
    {
        [SerializeField] private List<ChunkView> chunkPrefabs = new();
        [SerializeField] private int count = 10;
        [SerializeField] private bool randomize = true;
        [SerializeField] private bool logValidation = true;

        private readonly List<ChunkView> spawned = new();

        private void Start() => Build();

        [ContextMenu("Rebuild")]
        public void Build()
        {
            Clear();

            if (chunkPrefabs.Count == 0)
            {
                Debug.LogWarning("Chưa gán chunk prefab nào.", this);
                return;
            }

            Vector3 cursor = transform.position;

            for (int i = 0; i < count; i++)
            {
                ChunkView prefab = randomize
                    ? chunkPrefabs[Random.Range(0, chunkPrefabs.Count)]
                    : chunkPrefabs[i % chunkPrefabs.Count];

                ChunkView inst = Instantiate(prefab, transform);
                inst.name = prefab.name;
                inst.AlignTo(cursor);
                cursor = inst.ExitPoint.position;
                spawned.Add(inst);
            }

            if (logValidation) StartCoroutine(ValidateAfterPhysics());
        }

        [ContextMenu("Clear")]
        public void Clear()
        {
            foreach (var c in spawned)
                if (c != null) Destroy(c.gameObject);
            spawned.Clear();
        }

        // Chờ 1 bước physics để Composite Collider kịp sinh hình trước khi kiểm tra
        private IEnumerator ValidateAfterPhysics()
        {
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            var reported = new HashSet<string>();
            foreach (var chunk in spawned)
            {
                if (chunk == null || !reported.Add(chunk.name)) continue;
                foreach (var issue in chunk.Validate())
                    Debug.LogWarning($"[{chunk.name}] {issue}", chunk);
            }
        }
    }
}