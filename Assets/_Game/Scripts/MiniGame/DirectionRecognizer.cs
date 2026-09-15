using System.Collections.Generic;
using UnityEngine;

namespace HayuNgaksara
{
    /// <summary>
    /// Direction-sequence recognizer: resamples stroke to N points,
    /// extracts N-1 direction angles, compares via DTW with wrapped angular distance.
    /// Much more discriminative than $Q point-cloud for similar-looking characters.
    /// Uses the same JSON template format as QDollarRecognizer.
    /// </summary>
    public class DirectionRecognizer
    {
        private const int N = 64; // resample → N points → N-1 direction angles

        private static readonly HashSet<string> _swaraSet =
            new HashSet<string> { "a","i","u","e_pamepet","e_panelenng","o","eu" };
        private static readonly HashSet<string> _rarangkenSet =
            new HashSet<string> { "panghulu","pamepet_r","paneuleung","panglayar","pangecek","panguku" };
        private static float GetThreshold(string label)
        {
            if (_swaraSet.Contains(label))     return 0.60f;
            if (_rarangkenSet.Contains(label)) return 0.55f;
            return 0.50f;
        }

        private class DTemplate
        {
            public string  Label;
            public float[] Angles; // N-1 direction angles in radians
        }

        private readonly List<DTemplate> _templates = new List<DTemplate>();

        public int TemplateCount => _templates.Count;

        // ── public API (drop-in replacement for QDollarRecognizer) ───────────────

        public void LoadFromResources(string folder = "QTemplates")
        {
            _templates.Clear();
            var files = Resources.LoadAll<TextAsset>(folder);
            foreach (var ta in files)
            {
                var file = JsonUtility.FromJson<QTemplateFile>(ta.text);
                if (file == null || file.templates == null) continue;
                foreach (var entry in file.templates)
                {
                    var strokes = DecodeStrokes(entry.strokes);
                    AddTemplateInternal(file.label, strokes);
                }
            }
            Debug.Log($"[Direction] Loaded {_templates.Count} templates from Resources/{folder}");
        }

        public void AddTemplate(string label, List<List<Vector2>> strokes) =>
            AddTemplateInternal(label, strokes);

        public string Recognize(List<List<Vector2>> strokes, out float score)
        {
            score = 0f;
            if (_templates.Count == 0 || strokes == null || strokes.Count == 0) return "";

            var angles = ExtractAngles(strokes);
            if (angles == null) return "";

            float minDist = float.MaxValue;
            string best   = "";
            foreach (var t in _templates)
            {
                float d = AngleDTW(angles, t.Angles);
                if (d < minDist) { minDist = d; best = t.Label; }
            }

            // Normalized DTW distance per step ∈ [0, π]; score ∈ [0, 1]
            score = Mathf.Clamp01(1f - minDist / Mathf.PI);
            return best;
        }

        public bool IsMatch(string expectedLabel, List<List<Vector2>> strokes, out float score)
        {
            string recognized = Recognize(strokes, out score);
            return recognized == expectedLabel && score >= GetThreshold(expectedLabel);
        }

        public string RecognizeForDisplay(List<List<Vector2>> strokes, out float score) =>
            Recognize(strokes, out score);

        public float ScoreFor(string label, List<List<Vector2>> strokes)
        {
            if (strokes == null || strokes.Count == 0) return 0f;
            var angles = ExtractAngles(strokes);
            if (angles == null) return 0f;

            float bestScore = 0f;
            foreach (var t in _templates)
            {
                if (t.Label != label) continue;
                float d = AngleDTW(angles, t.Angles);
                float s = Mathf.Clamp01(1f - d / Mathf.PI);
                if (s > bestScore) bestScore = s;
            }
            return bestScore;
        }

        public bool HasTemplateFor(string label) =>
            _templates.Exists(t => t.Label == label);

        // ── internal ─────────────────────────────────────────────────────────────

        private void AddTemplateInternal(string label, List<List<Vector2>> strokes)
        {
            var angles = ExtractAngles(strokes);
            if (angles == null) return;
            _templates.Add(new DTemplate { Label = label, Angles = angles });
        }

        // Resample → compute N-1 direction angles (no scale/centroid needed for angles)
        private static float[] ExtractAngles(List<List<Vector2>> strokes)
        {
            var all = new List<Vector2>();
            foreach (var s in strokes) if (s != null) all.AddRange(s);
            if (all.Count < 2) return null;

            var pts = Resample(all, N);
            if (pts.Count < 2) return null;

            var angles = new float[pts.Count - 1];
            for (int i = 0; i < pts.Count - 1; i++)
                angles[i] = Mathf.Atan2(pts[i + 1].y - pts[i].y, pts[i + 1].x - pts[i].x);

            return angles;
        }

        // DTW with wrapped angular distance, normalized by (n+m) so score is per-step
        private static float AngleDTW(float[] a, float[] b)
        {
            int n = a.Length, m = b.Length;
            var dp = new float[n + 1, m + 1];
            for (int i = 0; i <= n; i++) dp[i, 0] = float.MaxValue / 2;
            for (int j = 0; j <= m; j++) dp[0, j] = float.MaxValue / 2;
            dp[0, 0] = 0f;

            for (int i = 1; i <= n; i++)
            for (int j = 1; j <= m; j++)
            {
                float cost = AngleDist(a[i - 1], b[j - 1]);
                float prev = Mathf.Min(dp[i - 1, j - 1], Mathf.Min(dp[i - 1, j], dp[i, j - 1]));
                dp[i, j]   = cost + prev;
            }

            return dp[n, m] / (n + m); // average per-step angular distance ∈ [0, π]
        }

        // Shortest angular distance between two angles, result ∈ [0, π]
        private static float AngleDist(float a, float b)
        {
            return Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, b * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
        }

        // ── resampling (identical to QDollarRecognizer) ───────────────────────────

        private static List<Vector2> Resample(List<Vector2> pts, int n)
        {
            if (pts.Count < 2 || n < 2) return new List<Vector2>(pts);

            var cum = new float[pts.Count];
            for (int i = 1; i < pts.Count; i++)
                cum[i] = cum[i - 1] + Vector2.Distance(pts[i - 1], pts[i]);
            float totalLen = cum[pts.Count - 1];
            if (totalLen < 1e-6f)
            {
                var flat = new List<Vector2>(n);
                for (int i = 0; i < n; i++) flat.Add(pts[0]);
                return flat;
            }

            var result = new List<Vector2>(n);
            for (int k = 0; k < n; k++)
            {
                float d  = (float)k / (n - 1) * totalLen;
                int lo = 0, hi = pts.Count - 2;
                while (lo < hi) { int mid = (lo + hi) / 2; if (cum[mid + 1] < d) lo = mid + 1; else hi = mid; }
                float segLen = cum[lo + 1] - cum[lo];
                float t      = segLen < 1e-6f ? 0f : (d - cum[lo]) / segLen;
                result.Add(Vector2.Lerp(pts[lo], pts[lo + 1], Mathf.Clamp01(t)));
            }
            return result;
        }

        private static List<List<Vector2>> DecodeStrokes(List<QStroke> raw)
        {
            var result = new List<List<Vector2>>();
            if (raw == null) return result;
            foreach (var s in raw)
            {
                var stroke = new List<Vector2>();
                for (int i = 0; i + 1 < s.pts.Count; i += 2)
                    stroke.Add(new Vector2(s.pts[i], s.pts[i + 1]));
                if (stroke.Count > 0) result.Add(stroke);
            }
            return result;
        }
    }
}
