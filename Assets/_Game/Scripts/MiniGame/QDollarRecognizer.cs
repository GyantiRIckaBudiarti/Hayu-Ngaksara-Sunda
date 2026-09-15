using System;
using System.Collections.Generic;
using UnityEngine;

namespace HayuNgaksara
{
    [Serializable] public class QTemplateFile  { public string label; public List<QTemplateEntry> templates = new List<QTemplateEntry>(); }
    [Serializable] public class QTemplateEntry { public List<QStroke> strokes = new List<QStroke>(); }
    [Serializable] public class QStroke        { public List<float> pts = new List<float>(); } // interleaved x,y

    /// <summary>
    /// $Q Super-Quick point-cloud recognizer — Vatavu et al. 2012.
    /// Reimplemented to match reference program (Assembly-CSharp.dll):
    ///   N=64, Resample→Scale→TranslateCentroid, LUT stores nearest-point INDEX,
    ///   CloudDistance computes actual integer-space Euclidean distance via LUT.
    /// </summary>
    public class QDollarRecognizer
    {
        private const int N                  = 64;
        private const int LUT_SIZE           = 64;
        private const int MAX_INT_COORD      = 1024;  // integer coord range [0, MAX_INT_COORD-1]
        private const int LUT_SCALE_FACTOR   = 16;    // = MAX_INT_COORD / LUT_SIZE

        private static readonly HashSet<string> _swaraSet =
            new HashSet<string> { "a","i","u","e_pamepet","e_panelenng","o","eu" };
        private static readonly HashSet<string> _rarangkenSet =
            new HashSet<string> { "panghulu","pamepet_r","paneuleung","panglayar","pangecek","panguku" };

        private static float GetThreshold(string label)
        {
            if (_swaraSet.Contains(label))     return 0.40f;
            if (_rarangkenSet.Contains(label)) return 0.35f;
            return 0.30f;
        }

        private readonly List<QDTemplate> _templates = new List<QDTemplate>();

        private class QDTemplate
        {
            public string Label;
            public int[]  IntX;   // integer coords after normalization [0, MAX_INT_COORD-1]
            public int[]  IntY;
            public int[,] LUT;    // LUT_SIZE × LUT_SIZE, each cell = index of nearest template point
        }

        public int TemplateCount => _templates.Count;

        // ── public API ────────────────────────────────────────────────────────────

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
            Debug.Log($"[QDollar] Loaded {_templates.Count} templates from Resources/{folder}");
        }

        public void AddTemplate(string label, List<List<Vector2>> strokes) =>
            AddTemplateInternal(label, strokes);

        public string Recognize(List<List<Vector2>> strokes, out float score)
        {
            score = 0f;
            if (_templates.Count == 0 || strokes == null || strokes.Count == 0) return "";

            var pts  = NormalizeStrokes(strokes);
            var intX = new int[N];
            var intY = new int[N];
            ToIntCoords(pts, intX, intY);
            var candidateLUT = BuildLUT(intX, intY);

            float minD  = float.MaxValue;
            string best = "";

            foreach (var tmpl in _templates)
            {
                float d = GreedyCloudMatch(intX, intY, candidateLUT, tmpl.IntX, tmpl.IntY, tmpl.LUT);
                if (d < minD) { minD = d; best = tmpl.Label; }
            }

            float maxDist = 0.5f * Mathf.Sqrt(2f) * MAX_INT_COORD * N;
            score = Mathf.Clamp01(1f - minD / maxDist);
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
            var pts  = NormalizeStrokes(strokes);
            var intX = new int[N]; var intY = new int[N];
            ToIntCoords(pts, intX, intY);
            var candidateLUT = BuildLUT(intX, intY);

            float maxDist   = 0.5f * Mathf.Sqrt(2f) * MAX_INT_COORD * N;
            float bestScore = 0f;
            foreach (var tmpl in _templates)
            {
                if (tmpl.Label != label) continue;
                float d = GreedyCloudMatch(intX, intY, candidateLUT, tmpl.IntX, tmpl.IntY, tmpl.LUT);
                float s = Mathf.Clamp01(1f - d / maxDist);
                if (s > bestScore) bestScore = s;
            }
            return bestScore;
        }

        public bool HasTemplateFor(string label) =>
            _templates.Exists(t => t.Label == label);

        // ── internal ──────────────────────────────────────────────────────────────

        private void AddTemplateInternal(string label, List<List<Vector2>> strokes)
        {
            var pts = NormalizeStrokes(strokes);
            if (pts.Length < 2) return;

            var intX = new int[N];
            var intY = new int[N];
            ToIntCoords(pts, intX, intY);

            _templates.Add(new QDTemplate
            {
                Label = label,
                IntX  = intX,
                IntY  = intY,
                LUT   = BuildLUT(intX, intY)
            });
        }

        // ── normalization (Resample → Scale → TranslateCentroid) ──────────────────

        private static Vector2[] NormalizeStrokes(List<List<Vector2>> strokes)
        {
            var all = new List<Vector2>();
            foreach (var s in strokes) if (s != null) all.AddRange(s);
            if (all.Count < 2) return all.ToArray();

            var resampled = Resample(all, N);         // resample FIRST (matches reference)
            var scaled    = Scale(resampled);          // then scale to unit square
            var centered  = TranslateCentroid(scaled); // then centroid to origin
            return centered.ToArray();
        }

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
                float d   = (float)k / (n - 1) * totalLen;
                int lo = 0, hi = pts.Count - 2;
                while (lo < hi) { int mid = (lo + hi) / 2; if (cum[mid + 1] < d) lo = mid + 1; else hi = mid; }
                float segLen = cum[lo + 1] - cum[lo];
                float t = segLen < 1e-6f ? 0f : (d - cum[lo]) / segLen;
                result.Add(Vector2.Lerp(pts[lo], pts[lo + 1], Mathf.Clamp01(t)));
            }
            return result;
        }

        private static List<Vector2> Scale(List<Vector2> pts)
        {
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var p in pts)
            {
                if (p.x < minX) minX = p.x; if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y; if (p.y > maxY) maxY = p.y;
            }
            float scale = Mathf.Max(maxX - minX, maxY - minY);
            if (scale < 1e-6f) return pts;
            var r = new List<Vector2>(pts.Count);
            foreach (var p in pts)
                r.Add(new Vector2((p.x - minX) / scale, (p.y - minY) / scale));
            return r;
        }

        private static List<Vector2> TranslateCentroid(List<Vector2> pts)
        {
            var c = Vector2.zero;
            foreach (var p in pts) c += p;
            c /= pts.Count;
            var r = new List<Vector2>(pts.Count);
            foreach (var p in pts) r.Add(p - c);
            return r;
        }

        // ── integer coordinates — rescale per-gesture ke full [0, MAX_INT_COORD-1] ──
        // Rescale per-gesture (bukan fixed mapping) agar seluruh range LUT terpakai
        // dan jarak antar aksara yang berbeda menjadi discriminative.
        // Fixed mapping (x+1)/2*1023 hanya mengisi 50% range → semua score ≥ 0.96, tidak bisa dibedakan.

        private static void ToIntCoords(Vector2[] pts, int[] intX, int[] intY)
        {
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var p in pts)
            {
                if (p.x < minX) minX = p.x; if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y; if (p.y > maxY) maxY = p.y;
            }
            float range = Mathf.Max(maxX - minX, maxY - minY);
            if (range < 1e-6f) range = 1f;
            // Scale uniform + center tiap dimensi agar aspect ratio dipertahankan
            float scale = (MAX_INT_COORD - 1) / range;
            float offX  = ((MAX_INT_COORD - 1) - (maxX - minX) * scale) * 0.5f;
            float offY  = ((MAX_INT_COORD - 1) - (maxY - minY) * scale) * 0.5f;
            for (int i = 0; i < pts.Length; i++)
            {
                intX[i] = Mathf.Clamp((int)((pts[i].x - minX) * scale + offX), 0, MAX_INT_COORD - 1);
                intY[i] = Mathf.Clamp((int)((pts[i].y - minY) * scale + offY), 0, MAX_INT_COORD - 1);
            }
        }

        // ── LUT construction — stores index of nearest template point ─────────────
        // LUT[lx][ly] = index k of template point nearest to LUT cell center (lx, ly)
        // Distance computed in integer coord space; LUT_SCALE_FACTOR=16 converts int→LUT cell

        private static int[,] BuildLUT(int[] intX, int[] intY)
        {
            var lut = new int[LUT_SIZE, LUT_SIZE];
            for (int i = 0; i < LUT_SIZE; i++)
            for (int j = 0; j < LUT_SIZE; j++)
            {
                int minDist = int.MaxValue;
                int minIdx  = 0;
                for (int k = 0; k < intX.Length; k++)
                {
                    int px = intX[k] / LUT_SCALE_FACTOR;
                    int py = intY[k] / LUT_SCALE_FACTOR;
                    int dx = px - i, dy = py - j;
                    int d  = dx * dx + dy * dy;
                    if (d < minDist) { minDist = d; minIdx = k; }
                }
                lut[i, j] = minIdx;
            }
            return lut;
        }

        // ── GreedyCloudMatch — both directions, step = sqrt(N) ───────────────────

        private static float GreedyCloudMatch(
            int[] qIntX, int[] qIntY, int[,] qLUT,
            int[] tIntX, int[] tIntY, int[,] tLUT)
        {
            int step = Mathf.Max(1, Mathf.FloorToInt(Mathf.Sqrt(N)));
            float minSoFar = float.MaxValue;

            for (int i = 0; i < N; i += step)
            {
                // candidate → template (uses template LUT)
                float d1 = CloudDistance(qIntX, qIntY, tIntX, tIntY, tLUT, i, minSoFar);
                if (d1 < minSoFar) minSoFar = d1;

                // template → candidate (uses candidate LUT)
                float d2 = CloudDistance(tIntX, tIntY, qIntX, qIntY, qLUT, i, minSoFar);
                if (d2 < minSoFar) minSoFar = d2;
            }
            return minSoFar;
        }

        // ── CloudDistance — LUT lookup for nearest point index, then exact distance

        private static float CloudDistance(
            int[] intX1, int[] intY1,
            int[] intX2, int[] intY2,
            int[,] lut2, int start, float minSoFar)
        {
            float sum = 0f;
            for (int idx = 0; idx < N; idx++)
            {
                int i  = (start + idx) % N;
                int lx = Mathf.Clamp(intX1[i] / LUT_SCALE_FACTOR, 0, LUT_SIZE - 1);
                int ly = Mathf.Clamp(intY1[i] / LUT_SCALE_FACTOR, 0, LUT_SIZE - 1);
                int j  = lut2[lx, ly]; // index of nearest point in pts2
                float dx = intX1[i] - intX2[j];
                float dy = intY1[i] - intY2[j];
                float weight = 1f - (float)idx / N;
                sum += weight * Mathf.Sqrt(dx * dx + dy * dy);
                if (sum >= minSoFar) return sum; // early abandoning
            }
            return sum;
        }

        // ── decode helper ─────────────────────────────────────────────────────────

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
