using System.Collections.Generic;
using UnityEngine;

namespace EndlessRunner.Run.World
{
    // Gắn vào root của mỗi chunk prefab.
    // Biết điểm vào/ra, tự căn chỉnh để nối chunk, và kiểm tra chunk có đúng chuẩn không.
    // Lưu ý: root của chunk phải có Scale = (1,1,1) và không xoay.
    public class ChunkView : MonoBehaviour
    {
        [Header("Điểm nối")]
        [SerializeField] private Transform entryPoint;
        [SerializeField] private Transform exitPoint;

        [Header("Kiểm tra")]
        [Tooltip("Object cha chứa toàn bộ collider mặt đất/platform (dùng để dò khe hố)")]
        [SerializeField] private Transform groundRoot;
        [SerializeField] private ChunkRules rules;

        public Transform EntryPoint => entryPoint;
        public Transform ExitPoint => exitPoint;

        /// <summary>Chiều dài chunk, tính theo toạ độ cục bộ nên đúng cả khi chunk đã bị dịch chuyển.</summary>
        public float Length => Local(exitPoint).x - Local(entryPoint).x;

        private Vector3 Local(Transform t) => transform.InverseTransformPoint(t.position);

        /// <summary>Dịch chunk sao cho EntryPoint trùng với điểm ExitPoint của chunk trước.</summary>
        public void AlignTo(Vector3 previousExitWorld)
        {
            transform.position += previousExitWorld - entryPoint.position;
        }

        // Test

        [ContextMenu("Validate Chunk")]
        private void ValidateAndLog()
        {
            var issues = Validate();
            if (issues.Count == 0)
                Debug.Log($"[{name}] Chunk hợp lệ. Dài {Length:0.##}.", this);
            else
                foreach (var s in issues) Debug.LogWarning($"[{name}] {s}", this);
        }

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

            // 2. Cao độ hai đầu
            if (Mathf.Abs(Local(entryPoint).y - rules.groundY) > tol)
                issues.Add($"EntryPoint lệch cao độ chuẩn (y = {Local(entryPoint).y:0.###}, chuẩn {rules.groundY}).");
            if (Mathf.Abs(Local(exitPoint).y - rules.groundY) > tol)
                issues.Add($"ExitPoint lệch cao độ chuẩn (y = {Local(exitPoint).y:0.###}, chuẩn {rules.groundY}).");

            // 3. Khe hố và độ cao platform
            if (groundRoot != null) CheckGround(issues);
            else issues.Add("Chưa gán GroundRoot nên chưa kiểm tra được khe hố.");

            return issues;
        }

        private struct Span { public float minX, maxX, maxY; }

        private void CheckGround(List<string> issues)
        {
            var spans = new List<Span>();
            foreach (var c in groundRoot.GetComponentsInChildren<Collider2D>())
                CollectSpans(c, spans);

            if (spans.Count == 0)
            {
                issues.Add("Không tìm thấy collider nào dưới GroundRoot (nếu dùng Composite thì thử lại sau khi nó đã sinh hình).");
                return;
            }

            spans.Sort((a, b) => a.minX.CompareTo(b.minX));

            float cursor = entryPoint.position.x;
            float baseY = transform.position.y + rules.groundY;

            foreach (var s in spans)
            {
                float gap = s.minX - cursor;
                if (gap > rules.maxGapWidth + rules.tolerance)
                    issues.Add($"Khe hố rộng {gap:0.##} tại x≈{cursor:0.##} (tối đa {rules.maxGapWidth}).");

                float height = s.maxY - baseY;
                if (height > rules.maxPlatformHeight + rules.tolerance)
                    issues.Add($"Platform cao {height:0.##} tại x≈{s.minX:0.##} (tối đa {rules.maxPlatformHeight}).");

                cursor = Mathf.Max(cursor, s.maxX);
            }

            float tail = exitPoint.position.x - cursor;
            if (tail > rules.maxGapWidth + rules.tolerance)
                issues.Add($"Khoảng trống cuối chunk rộng {tail:0.##} (tối đa {rules.maxGapWidth}).");
        }

        private static void CollectSpans(Collider2D c, List<Span> spans)
        {
            if (c.isTrigger) return;
            // Collider đã được gộp vào Composite thì để Composite đại diện
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