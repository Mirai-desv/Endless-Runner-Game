using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace EndlessRunner.Run.World
{
    // Thành phần nào trong chunk cần khôi phục trạng thái khi chunk được tái sử dụng (coin đã nhặt...) thì implement interface này
    public interface IChunkResettable
    {
        void ResetForReuse();
    }

    /// Gắn vào root của mỗi chunk prefab.
    /// Lo: điểm vào/ra, căn chỉnh để nối chunk, và kiểm tra chunk có đúng chuẩn + có vượt được không.
    /// Lưu ý: root của chunk phải có Scale = (1,1,1) và không xoay.

    public class ChunkView : MonoBehaviour
    {
        [Header("Điểm nối")]
        [SerializeField] private Transform entryPoint;
        [SerializeField] private Transform exitPoint;

        [Header("Kiểm tra")]
        [Tooltip("Object cha chứa Tilemap/collider mặt đất và platform")]
        [SerializeField] private Transform groundRoot;
        [Tooltip("Object cha chứa các vật cản (mỗi vật cản có collider)")]
        [SerializeField] private Transform obstaclesRoot;
        [SerializeField] private ChunkRules rules;

        public Transform EntryPoint => entryPoint;
        public Transform ExitPoint => exitPoint;
        public ChunkRules Rules => rules;

        public float Length => Local(exitPoint).x - Local(entryPoint).x;

        private IChunkResettable[] resettables;

        private Vector3 Local(Transform t) => transform.InverseTransformPoint(t.position);

        public void AlignTo(Vector3 previousExitWorld)
        {
            transform.position += previousExitWorld - entryPoint.position;
        }

        // Được pool gọi mỗi khi chunk được lấy ra dùng lại
        public void ResetForReuse()
        {
            resettables ??= GetComponentsInChildren<IChunkResettable>(true);
            foreach (var r in resettables) r.ResetForReuse();
        }

        // ================================================================ Kiểm tra

        [ContextMenu("Validate Chunk")]
        private void ValidateAndLog()
        {
            var issues = Validate();
            if (issues.Count == 0)
                Debug.Log($"[{name}] Chunk hợp lệ. Dài {Length:0.##}.", this);
            else
                foreach (var s in issues) Debug.LogWarning($"[{name}] {s}", this);
        }

        private struct Span { public float minX, maxX, maxY; }
        private struct Hazard { public float minX, maxX, height; public string label; }

        // Vật cản được coi là "đứng trên" mặt đất nếu đáy nó chìm tối đa chừng này vào nền
        private const float EmbedTolerance = 0.5f;

        public List<string> Validate()
        {
            var issues = new List<string>();

            if (entryPoint == null || exitPoint == null)
            {
                issues.Add("Thiếu EntryPoint hoặc ExitPoint.");
                return issues;
            }
            if (rules == null)
            {
                issues.Add("Chưa gán ChunkRules.");
                return issues;
            }

            Physics2D.SyncTransforms();
            float tol = rules.tolerance;

            // 1. Chiều dài
            float len = Length;
            if (len <= 0f)
            {
                issues.Add("ExitPoint phải nằm bên phải EntryPoint.");
            }
            else
            {
                float units = len / rules.lengthUnit;
                if (Mathf.Abs(units - Mathf.Round(units)) * rules.lengthUnit > tol)
                    issues.Add($"Chiều dài {len:0.###} không phải bội số của {rules.lengthUnit}.");
            }

            // 2. Cao độ hai điểm nối
            if (Mathf.Abs(Local(entryPoint).y - rules.groundY) > tol)
                issues.Add($"EntryPoint lệch cao độ chuẩn (y = {Local(entryPoint).y:0.###}, chuẩn {rules.groundY}).");
            if (Mathf.Abs(Local(exitPoint).y - rules.groundY) > tol)
                issues.Add($"ExitPoint lệch cao độ chuẩn (y = {Local(exitPoint).y:0.###}, chuẩn {rules.groundY}).");

            // 3. Mặt đất
            if (groundRoot == null)
            {
                issues.Add("Chưa gán GroundRoot nên chưa kiểm tra được mặt đất.");
                return issues;
            }

            var spans = CollectGroundSpans();
            if (spans.Count == 0)
            {
                issues.Add("Không tìm thấy mặt đất nào dưới GroundRoot. " + Diagnose());
                return issues;
            }

            var hazards = new List<Hazard>();
            CheckGroundCoverage(issues, spans, hazards);

            // 4. Vật cản
            if (obstaclesRoot != null) CollectObstacles(issues, spans, hazards);

            // 5. Khả năng vượt qua mọi chướng ngại (khe hố + vật cản)
            CheckHazards(issues, hazards);

            return issues;
        }

        // ---------------------------------------------------------------- Mặt đất

        private List<Span> CollectGroundSpans()
        {
            var spans = new List<Span>();

            foreach (var tm in groundRoot.GetComponentsInChildren<Tilemap>())
                if (tm.GetComponent<TilemapCollider2D>() != null)
                    CollectTilemapSpans(tm, spans);

            foreach (var c in groundRoot.GetComponentsInChildren<Collider2D>())
                CollectColliderSpans(c, spans);

            spans.Sort((a, b) => a.minX.CompareTo(b.minX));
            return spans;
        }

        private static bool TryTopAt(List<Span> spans, float x, out float top)
        {
            top = float.NegativeInfinity;
            bool found = false;
            foreach (var s in spans)
            {
                if (s.minX <= x && s.maxX >= x)
                {
                    found = true;
                    if (s.maxY > top) top = s.maxY;
                }
            }
            return found;
        }

        private void CheckGroundCoverage(List<string> issues, List<Span> spans, List<Hazard> hazards)
        {
            float tol = rules.tolerance;
            float entryX = entryPoint.position.x;
            float exitX = exitPoint.position.x;
            float baseY = transform.position.y + rules.groundY;

            // Hai đầu chunk phải có mặt đất, đúng cao độ chuẩn
            float probeIn = entryX + tol * 2f;
            float probeOut = exitX - tol * 2f;

            bool entryOk = TryTopAt(spans, probeIn, out float entryTop);
            if (!entryOk) issues.Add("Không có mặt đất ngay tại EntryPoint.");
            else if (Mathf.Abs(entryTop - baseY) > tol)
                issues.Add($"Mặt đất tại EntryPoint không đúng cao độ chuẩn (lệch {entryTop - baseY:0.##}).");

            bool exitOk = TryTopAt(spans, probeOut, out float exitTop);
            if (!exitOk) issues.Add("Không có mặt đất ngay tại ExitPoint.");
            else if (Mathf.Abs(exitTop - baseY) > tol)
                issues.Add($"Mặt đất tại ExitPoint không đúng cao độ chuẩn (lệch {exitTop - baseY:0.##}).");

            // Bậc/platform quá cao
            float maxTop = float.NegativeInfinity;
            foreach (var s in spans) if (s.maxY > maxTop) maxTop = s.maxY;
            if (maxTop - baseY > rules.SafeJumpHeight + tol)
                issues.Add($"Có bậc/platform cao {maxTop - baseY:0.##} so với mặt đất, vượt độ cao nhảy an toàn {rules.SafeJumpHeight:0.##}.");

            // Khe hố = khoảng trống giữa các đoạn mặt đất
            float cursor = entryOk ? entryX : Mathf.Max(entryX, spans[0].minX);
            foreach (var s in spans)
            {
                if (s.minX >= exitX) break;
                if (s.minX - cursor > tol)
                {
                    hazards.Add(new Hazard
                    {
                        minX = cursor,
                        maxX = s.minX,
                        height = 0f,
                        label = $"Khe hố tại x≈{cursor:0.#}"
                    });
                }
                cursor = Mathf.Max(cursor, s.maxX);
            }
        }

        // ---------------------------------------------------------------- Vật cản

        private void CollectObstacles(List<string> issues, List<Span> spans, List<Hazard> hazards)
        {
            foreach (var c in obstaclesRoot.GetComponentsInChildren<Collider2D>())
            {
                if (c is TilemapCollider2D || c is CompositeCollider2D) continue;

                Bounds b = c.bounds;

                // Tìm mặt đất mà vật cản đang đứng lên
                float surface = float.NegativeInfinity;
                foreach (var s in spans)
                {
                    if (s.maxX > b.min.x && s.minX < b.max.x && s.maxY <= b.min.y + EmbedTolerance)
                        if (s.maxY > surface) surface = s.maxY;
                }

                if (float.IsNegativeInfinity(surface))
                {
                    issues.Add($"Vật cản '{c.name}' không đứng trên mặt đất nào (đang lơ lửng hoặc nằm trên khe hố).");
                    continue;
                }

                hazards.Add(new Hazard
                {
                    minX = b.min.x,
                    maxX = b.max.x,
                    height = b.max.y - surface,
                    label = $"Vật cản '{c.name}'"
                });
            }
        }

        // ---------------------------------------------------------------- Khả năng vượt

        private void CheckHazards(List<string> issues, List<Hazard> hazards)
        {
            if (hazards.Count == 0) return;

            hazards.Sort((a, b) => a.minX.CompareTo(b.minX));

            float minS = rules.minRunSpeed;
            float maxS = rules.maxRunSpeed;
            float entryX = entryPoint.position.x;
            float exitX = exitPoint.position.x;

            // a. Từng chướng ngại có nhảy qua được không (kiểm tra ở tốc độ THẤP nhất)
            foreach (var h in hazards)
            {
                float width = h.maxX - h.minX;
                if (h.height > rules.SafeJumpHeight)
                {
                    issues.Add($"{h.label} cao {h.height:0.##}, vượt độ cao nhảy an toàn {rules.SafeJumpHeight:0.##}.");
                    continue;
                }
                float clear = rules.ClearWidth(h.height, minS);
                if (width > clear)
                    issues.Add($"{h.label} rộng {width:0.##} nhưng ở tốc độ thấp nhất ({minS}) chỉ vượt được tối đa {clear:0.##}.");
            }

            // b. Khoảng cách giữa hai chướng ngại liên tiếp (kiểm tra ở tốc độ CAO nhất)
            for (int i = 1; i < hazards.Count; i++)
            {
                var a = hazards[i - 1];
                var b = hazards[i];
                float spacing = b.minX - a.maxX;
                float required = rules.RequiredSpacing(a.height, b.height, maxS);
                if (spacing < required)
                    issues.Add($"{a.label} và {b.label} cách nhau {spacing:0.##}, cần tối thiểu {required:0.##} (ở tốc độ {maxS}).");
            }

            // c. Chừa khoảng trống ở hai đầu để ghép với bất kỳ chunk nào cũng vẫn vượt được
            var first = hazards[0];
            float leading = first.minX - entryX;
            float needLead = rules.RequiredSpacing(first.height, first.height, maxS) * 0.5f;
            if (leading < needLead)
                issues.Add($"{first.label} quá gần đầu chunk: còn {leading:0.##}, cần tối thiểu {needLead:0.##}.");

            var last = hazards[hazards.Count - 1];
            float trailing = exitX - last.maxX;
            float needTrail = rules.RequiredSpacing(last.height, last.height, maxS) * 0.5f;
            if (trailing < needTrail)
                issues.Add($"{last.label} quá gần cuối chunk: còn {trailing:0.##}, cần tối thiểu {needTrail:0.##}.");
        }

        // ---------------------------------------------------------------- Thu thập hình học

        private string Diagnose()
        {
            int tilemaps = 0, tilemapsWithCollider = 0, tiles = 0, tilesWithCollider = 0;
            foreach (var tm in groundRoot.GetComponentsInChildren<Tilemap>(true))
            {
                tilemaps++;
                if (tm.GetComponent<TilemapCollider2D>() != null) tilemapsWithCollider++;
                foreach (var p in tm.cellBounds.allPositionsWithin)
                {
                    if (!tm.HasTile(p)) continue;
                    tiles++;
                    if (tm.GetColliderType(p) != Tile.ColliderType.None) tilesWithCollider++;
                }
            }

            int colliders = 0, triggers = 0, inactive = 0;
            foreach (var c in groundRoot.GetComponentsInChildren<Collider2D>(true))
            {
                colliders++;
                if (c.isTrigger) triggers++;
                if (!c.gameObject.activeInHierarchy) inactive++;
            }

            return $"[Chẩn đoán] GroundRoot = '{groundRoot.name}' ({groundRoot.childCount} object con) | " +
                   $"Tilemap: {tilemaps} (có TilemapCollider2D: {tilemapsWithCollider}) | " +
                   $"Tile đã vẽ: {tiles} (có Collider Type khác None: {tilesWithCollider}) | " +
                   $"Collider2D: {colliders} (Is Trigger: {triggers}, bị tắt: {inactive})";
        }

        private static void CollectTilemapSpans(Tilemap tm, List<Span> spans)
        {
            foreach (var p in tm.cellBounds.allPositionsWithin)
            {
                if (!tm.HasTile(p)) continue;
                if (tm.GetColliderType(p) == Tile.ColliderType.None) continue;

                Vector3 origin = tm.CellToWorld(p);
                Vector3 right = tm.CellToWorld(p + Vector3Int.right);
                Vector3 up = tm.CellToWorld(p + Vector3Int.up);
                spans.Add(new Span { minX = origin.x, maxX = right.x, maxY = up.y });
            }
        }

        private static void CollectColliderSpans(Collider2D c, List<Span> spans)
        {
            if (c.isTrigger) return;
            if (c is TilemapCollider2D) return;
            if (c is CompositeCollider2D && c.GetComponent<Tilemap>() != null) return;
            if (c.compositeOperation != Collider2D.CompositeOperation.None) return;

            if (c is CompositeCollider2D comp)
            {
                for (int i = 0; i < comp.pathCount; i++)
                {
                    var pts = new Vector2[comp.GetPathPointCount(i)];
                    comp.GetPath(i, pts);
                    if (pts.Length == 0) continue;

                    var s = new Span { minX = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue };
                    foreach (var p in pts)
                    {
                        Vector3 w = comp.transform.TransformPoint(p);
                        if (w.x < s.minX) s.minX = w.x;
                        if (w.x > s.maxX) s.maxX = w.x;
                        if (w.y > s.maxY) s.maxY = w.y;
                    }
                    spans.Add(s);
                }
            }
            else
            {
                Bounds b = c.bounds;
                spans.Add(new Span { minX = b.min.x, maxX = b.max.x, maxY = b.max.y });
            }
        }

        // ================================================================ Gizmo

        private void OnDrawGizmos()
        {
            if (entryPoint == null || exitPoint == null) return;

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(entryPoint.position, 0.2f);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(exitPoint.position, 0.2f);

            if (rules != null)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.6f);
                Vector3 a = transform.TransformPoint(new Vector3(Local(entryPoint).x, rules.groundY, 0f));
                Vector3 b = transform.TransformPoint(new Vector3(Local(exitPoint).x, rules.groundY, 0f));
                Gizmos.DrawLine(a, b);
            }
        }
    }
}