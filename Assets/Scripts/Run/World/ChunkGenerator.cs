using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessRunner.Run.World
{
    /// <summary>
    /// Sinh chunk liên tục phía trước player, thu hồi chunk đã bị bỏ lại phía sau.
    /// Chọn chunk ngẫu nhiên có trọng số, theo độ khó cho phép, tránh lặp lại chunk vừa dùng.
    /// </summary>
    public class ChunkGenerator : MonoBehaviour
    {
        [Header("Dữ liệu")]
        [SerializeField] private ChunkLibrary library;
        [Tooltip("Chunk mở đầu. Để trống thì lấy chunk có tag 'safe'")]
        [SerializeField] private ChunkData startChunk;

        [Header("Theo dõi")]
        [Tooltip("Thường là Player. Dùng để biết cần sinh/thu hồi chunk ở đâu")]
        [SerializeField] private Transform target;
        [SerializeField] private float spawnAhead = 40f;
        [SerializeField] private float despawnBehind = 25f;

        [Header("Chọn chunk")]
        [SerializeField, Min(0)] private int safeChunksAtStart = 2;
        [SerializeField, Min(0)] private int avoidRepeatCount = 2;
        [SerializeField, Range(1, 5)] private int maxDifficulty = 1;
        [Tooltip("0 = ngẫu nhiên mỗi lần chơi; khác 0 = cố định (để tái hiện lỗi / daily challenge)")]
        [SerializeField] private int seed = 0;

        [Header("Pool")]
        [SerializeField, Min(0)] private int prewarmPerChunk = 1;

        [Header("Player Spawn")]
        [SerializeField] private float initialPlayerOffsetX;
        [SerializeField] private float initialPlayerOffsetY;

        public event Action<ChunkView, ChunkData> ChunkSpawned;
        public event Action<ChunkView> ChunkRecycled;

        // Độ khó tối đa được phép bốc. Ngày 5 DifficultyManager sẽ gán giá trị này theo quãng đường
        public int MaxDifficulty
        {
            get => maxDifficulty;
            set => maxDifficulty = Mathf.Clamp(value, 1, 5);
        }

        public int SpawnedCount { get; private set; }
        public Transform Target { get => target; set => target = value; }

        private ChunkPool pool;
        private Transform activeRoot;
        private System.Random rng;

        private readonly List<ChunkView> active = new();
        private readonly Queue<ChunkData> recent = new();
        private readonly List<ChunkData> candidates = new();
        private Vector3 lastExit;

        private void Awake()
        {
            var poolRoot = new GameObject("ChunkPool").transform;
            poolRoot.SetParent(transform, false);
            activeRoot = new GameObject("ActiveChunks").transform;
            activeRoot.SetParent(transform, false);

            pool = new ChunkPool(poolRoot);
            rng = seed != 0 ? new System.Random(seed) : new System.Random();
            //lastExit = transform.position;

            if (target != null && Camera.main != null)
            {
                Camera cam = Camera.main;

                float halfWidth = cam.orthographicSize * cam.aspect;

                lastExit = new Vector3(target.position.x - halfWidth, transform.position.y, transform.position.z);
            }
            else
            {
                lastExit = transform.position;
            }

            if (library != null)
                foreach (var d in library.chunks)
                {
                    if (d == null || d.prefab == null) continue;

                    // Chống đệ quy vô hạn: prefab chunk mà chứa chính ChunkGenerator thì mỗi lần Instantiate
                    // lại chạy Awake() -> Prewarm() -> Instantiate() ... cho tới khi tràn stack.
                    if (d.prefab.GetComponentInChildren<ChunkGenerator>(true) != null)
                    {
                        Debug.LogError($"ChunkGenerator: prefab '{d.prefab.name}' (trong ChunkData '{d.name}') đang chứa ChunkGenerator → " +
                                       "sẽ đệ quy vô hạn. Hãy gỡ ChunkGenerator khỏi prefab chunk; chỉ đặt nó trên một GameObject riêng trong Scene.", d);
                        continue;
                    }

                    pool.Prewarm(d.prefab, prewarmPerChunk);
                }
        }

        private void Start()
        {
            if (library == null) Debug.LogWarning("ChunkGenerator: chưa gán ChunkLibrary.", this);
            if (target == null) Debug.LogWarning("ChunkGenerator: chưa gán Target (Player).", this);
        }

        private void LateUpdate()
        {
            if (library == null || target == null) return;

            // Sinh thêm khi chunk cuối còn nằm gần player
            int guard = 0;
            while (lastExit.x < target.position.x + spawnAhead && guard++ < 16)
                if (!SpawnNext()) break;

            RecycleBehind();
        }

        // Sinh / thu hồi -----------------------------

        private bool SpawnNext()
        {
            ChunkData data = PickNext();
            if (data == null)
            {
                Debug.LogWarning("ChunkGenerator: không có chunk nào hợp lệ để sinh.", this);
                return false;
            }

            ChunkView view = pool.Get(data.prefab, activeRoot);
            view.AlignTo(lastExit);
            
            // Chỉ đặt vị trí Player khi sinh chunk đầu tiên
            if (SpawnedCount == 0 && target != null)
            {
                Vector3 spawnPosition = target.position;
                spawnPosition.x = view.EntryPoint.position.x + initialPlayerOffsetX;
                spawnPosition.y = view.EntryPoint.position.y + initialPlayerOffsetY;
                target.position = spawnPosition;
            }

            lastExit = view.ExitPoint.position;

            active.Add(view);
            SpawnedCount++;

            recent.Enqueue(data);
            while (recent.Count > avoidRepeatCount) recent.Dequeue();

            ChunkSpawned?.Invoke(view, data);
            return true;
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

        // Xóa sạch chunk đang có và bắt đầu lại (dùng khi Retry)
        public void Restart(int newSeed = 0)
        {
            foreach (var v in active) pool.Release(v);
            active.Clear();
            recent.Clear();
            SpawnedCount = 0;
            lastExit = transform.position;
            seed = newSeed;
            rng = seed != 0 ? new System.Random(seed) : new System.Random();
        }

        // ---------------------------------------------------------------- Chọn chunk

        private static bool Usable(ChunkData d) => d != null && d.prefab != null;

        private ChunkData PickNext()
        {
            candidates.Clear();

            // Những chunk đầu tiên: chunk an toàn
            if (SpawnedCount < safeChunksAtStart)
            {
                if (Usable(startChunk)) return startChunk;

                foreach (var d in library.chunks)
                    if (Usable(d) && d.HasTag("safe")) candidates.Add(d);
                if (candidates.Count > 0) return WeightedPick(candidates);
            }

            // Bình thường: đúng độ khó, không trùng chunk vừa dùng
            foreach (var d in library.chunks)
                if (Usable(d) && d.difficulty <= maxDifficulty && !recent.Contains(d))
                    candidates.Add(d);

            // Không còn lựa chọn thì cho phép lặp
            if (candidates.Count == 0)
                foreach (var d in library.chunks)
                    if (Usable(d) && d.difficulty <= maxDifficulty) candidates.Add(d);

            // Vẫn không có thì lấy bất kỳ chunk nào dùng được
            if (candidates.Count == 0)
                foreach (var d in library.chunks)
                    if (Usable(d)) candidates.Add(d);

            return candidates.Count > 0 ? WeightedPick(candidates) : null;
        }

        private ChunkData WeightedPick(List<ChunkData> list)
        {
            float total = 0f;
            foreach (var c in list) total += Mathf.Max(0f, c.weight);
            if (total <= 0f) return list[rng.Next(list.Count)];

            double roll = rng.NextDouble() * total;
            foreach (var c in list)
            {
                roll -= Mathf.Max(0f, c.weight);
                if (roll <= 0) return c;
            }
            return list[list.Count - 1];
        }
    }
}