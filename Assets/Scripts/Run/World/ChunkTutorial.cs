using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessRunner.Run.World
{
    /// Sinh chunk theo ĐÚNG thứ tự đã sắp sẵn trong ChunkTutLib (dùng cho màn tutorial).
    /// Sinh hết danh sách thì dừng, không sinh thêm chunk mới. Chunk đã bỏ lại phía sau vẫn được thu hồi.
    public class ChunkTutorial : MonoBehaviour
    {
        [Header("Dữ liệu")]
        [Tooltip("Danh sách chunk theo thứ tự sẽ xuất hiện (phần tử 0 sinh đầu tiên)")]
        [SerializeField] private ChunkTutLib library;

        [Header("Theo dõi")]
        [Tooltip("Thường là Player. Dùng để biết cần sinh/thu hồi chunk ở đâu")]
        [SerializeField] private Transform target;
        [SerializeField] private float spawnAhead = 40f;
        [SerializeField] private float despawnBehind = 25f;

        [Header("Pool")]
        [SerializeField, Min(0)] private int prewarmPerChunk = 1;

        [Header("Player Spawn")]
        [SerializeField] private float initialPlayerOffsetX;
        [SerializeField] private float initialPlayerOffsetY;

        public event Action<ChunkView, ChunkData> ChunkSpawned;
        public event Action<ChunkView> ChunkRecycled;
        // Bắn ra đúng 1 lần khi chunk cuối cùng trong danh sách đã được sinh
        public event Action AllChunksSpawned;

        public int SpawnedCount { get; private set; }
        public bool Finished { get; private set; }
        public Transform Target { get => target; set => target = value; }

        private ChunkPool pool;
        private Transform activeRoot;

        private readonly List<ChunkView> active = new();
        private Vector3 lastExit;

        private void Awake()
        {
            var poolRoot = new GameObject("ChunkPool").transform;
            poolRoot.SetParent(transform, false);
            activeRoot = new GameObject("ActiveChunks").transform;
            activeRoot.SetParent(transform, false);

            pool = new ChunkPool(poolRoot);
            lastExit = ComputeStartExit();

            if (library == null) return;

            // Mỗi prefab chỉ prewarm một lần dù xuất hiện nhiều lần trong danh sách
            var done = new HashSet<ChunkView>();
            foreach (var d in library.chunks)
            {
                if (d == null || d.prefab == null || !done.Add(d.prefab)) continue;

                // Chống đệ quy vô hạn: prefab chunk chứa chính generator thì Instantiate lại chạy Awake() -> ...
                if (d.prefab.GetComponentInChildren<ChunkTutorial>(true) != null ||
                    d.prefab.GetComponentInChildren<ChunkGenerator>(true) != null)
                {
                    Debug.LogError($"ChunkTutorial: prefab '{d.prefab.name}' (trong ChunkData '{d.name}') đang chứa " +
                                   "ChunkTutorial/ChunkGenerator → sẽ đệ quy vô hạn. Hãy gỡ nó khỏi prefab chunk.", d);
                    continue;
                }

                pool.Prewarm(d.prefab, prewarmPerChunk);
            }
        }

        private void Start()
        {
            if (library == null) Debug.LogWarning("ChunkTutorial: chưa gán ChunkTutLib.", this);
            if (target == null) Debug.LogWarning("ChunkTutorial: chưa gán Target (Player).", this);
        }

        private void LateUpdate()
        {
            if (library == null || target == null) return;

            // Sinh thêm khi chunk cuối còn nằm gần player, hết danh sách thì thôi
            int guard = 0;
            while (!Finished && lastExit.x < target.position.x + spawnAhead && guard++ < 16)
                if (!SpawnNext()) break;

            RecycleBehind();
        }

        // Sinh / thu hồi -----------------------------

        private bool SpawnNext()
        {
            // Bỏ qua các ô trống / thiếu prefab, sinh chunk hợp lệ kế tiếp theo thứ tự
            ChunkData data = null;
            while (SpawnedCount < library.chunks.Count)
            {
                var d = library.chunks[SpawnedCount];
                if (d != null && d.prefab != null) { data = d; break; }

                Debug.LogWarning($"ChunkTutorial: phần tử {SpawnedCount} trong ChunkTutLib bị trống/thiếu prefab, bỏ qua.", this);
                SpawnedCount++;
            }

            if (data == null)
            {
                MarkFinished();
                return false;
            }

            ChunkView view = pool.Get(data.prefab, activeRoot);
            view.AlignTo(lastExit);

            // Chỉ đặt vị trí Player khi sinh chunk đầu tiên
            if (SpawnedCount == 0 && active.Count == 0 && target != null)
            {
                Vector3 p = target.position;
                p.x = view.EntryPoint.position.x + initialPlayerOffsetX;
                p.y = view.EntryPoint.position.y + initialPlayerOffsetY;
                target.position = p;
            }

            lastExit = view.ExitPoint.position;

            active.Add(view);
            SpawnedCount++;

            ChunkSpawned?.Invoke(view, data);

            if (SpawnedCount >= library.chunks.Count) MarkFinished();
            return true;
        }

        private void MarkFinished()
        {
            if (Finished) return;
            Finished = true;
            AllChunksSpawned?.Invoke();
        }

        private void RecycleBehind()
        {
            float limit = target.position.x - despawnBehind;
            while (active.Count > 1 && active[0].ExitPoint.position.x < limit)
            {
                ChunkView v = active[0];
                active.RemoveAt(0);
                pool.Release(v);
                ChunkRecycled?.Invoke(v);
            }
        }

        // Xóa sạch chunk đang có và chạy lại tutorial từ chunk đầu (dùng khi Retry)
        public void Restart()
        {
            foreach (var v in active) pool.Release(v);
            active.Clear();
            SpawnedCount = 0;
            Finished = false;
            lastExit = ComputeStartExit();
        }

        private Vector3 ComputeStartExit()
        {
            if (target != null && Camera.main != null)
            {
                Camera cam = Camera.main;
                float halfWidth = cam.orthographicSize * cam.aspect;
                return new Vector3(target.position.x - halfWidth, transform.position.y, transform.position.z);
            }
            return transform.position;
        }
    }
}