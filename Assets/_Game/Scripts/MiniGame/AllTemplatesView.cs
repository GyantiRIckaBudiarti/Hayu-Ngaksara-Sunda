using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HayuNgaksara
{
    /// <summary>
    /// Overlay yang menampilkan semua template aksara dari file JSON QTemplates,
    /// dirender sebagai gambar stroke (bukan Unicode) — persis seperti program referensi.
    /// </summary>
    public class AllTemplatesView : MonoBehaviour
    {
        private static AllTemplatesView _instance;

        private GameObject    _overlay;
        private RectTransform _grid;

        // Template data cache: label → list of strokes (first template only, for display)
        private static Dictionary<string, List<List<Vector2>>> _templateCache;

        private const int   COLS      = 4;
        private const float CELL_W    = 160f;
        private const float CELL_H    = 190f;   // taller to fit preview + label
        private const float CELL_GAP  = 12f;
        private const float PAD_H     = 16f;
        private const float PAD_B     = 20f;
        private const int   TEX_SIZE  = 128;    // preview texture resolution
        private const int   BRUSH_R   = 3;      // stroke brush radius (px)

        // ── public API ────────────────────────────────────────────────────────

        public static void Show(List<AksaraCard> cards)
        {
            EnsureTemplateCache();
            EnsureInstance();
            if (_instance == null) return;
            _instance.transform.SetAsLastSibling();
            _instance._overlay.SetActive(true);
            _instance.StartCoroutine(_instance.PopulateNextFrame(cards));
        }

        public static void Hide()
        {
            if (_instance != null && _instance._overlay != null)
                _instance._overlay.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // ── template cache ────────────────────────────────────────────────────

        private static void EnsureTemplateCache()
        {
            if (_templateCache != null) return;
            _templateCache = new Dictionary<string, List<List<Vector2>>>();

            var files = Resources.LoadAll<TextAsset>("QTemplates");
            foreach (var ta in files)
            {
                var file = JsonUtility.FromJson<QTemplateFile>(ta.text);
                if (file == null || file.templates == null || file.templates.Count == 0) continue;
                if (_templateCache.ContainsKey(file.label)) continue;

                // Store only the FIRST template for preview
                var strokes = DecodeStrokes(file.templates[0].strokes);
                _templateCache[file.label] = strokes;
            }
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

        // ── setup ─────────────────────────────────────────────────────────────

        private static void EnsureInstance()
        {
            if (_instance != null) return;

            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            var go = new GameObject("AllTemplatesView", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            _instance = go.AddComponent<AllTemplatesView>();
            _instance.BuildUI();
        }

        private static RectTransform Child(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private void BuildUI()
        {
            // Backdrop
            var overlayRT = Child(transform, "Overlay");
            overlayRT.anchorMin = Vector2.zero;
            overlayRT.anchorMax = Vector2.one;
            overlayRT.offsetMin = overlayRT.offsetMax = Vector2.zero;
            _overlay = overlayRT.gameObject;
            _overlay.AddComponent<Image>().color = new Color(0f, 0f, 0.05f, 0.93f);

            // Card panel
            var cardRT = Child(overlayRT, "Card");
            cardRT.anchorMin = new Vector2(0.02f, 0.03f);
            cardRT.anchorMax = new Vector2(0.98f, 0.97f);
            cardRT.offsetMin = cardRT.offsetMax = Vector2.zero;
            cardRT.gameObject.AddComponent<Image>().color = new Color(0.10f, 0.15f, 0.26f, 1f);

            // Header
            var headerRT = Child(cardRT, "Header");
            headerRT.anchorMin = new Vector2(0f, 1f);
            headerRT.anchorMax = new Vector2(1f, 1f);
            headerRT.pivot     = new Vector2(0.5f, 1f);
            headerRT.offsetMin = headerRT.offsetMax = Vector2.zero;
            headerRT.sizeDelta = new Vector2(0f, 52f);
            headerRT.gameObject.AddComponent<Image>().color = new Color(0.07f, 0.11f, 0.20f, 1f);

            var titleRT = Child(headerRT, "Title");
            titleRT.anchorMin = new Vector2(0.02f, 0f);
            titleRT.anchorMax = new Vector2(0.78f, 1f);
            titleRT.offsetMin = titleRT.offsetMax = Vector2.zero;
            var title = titleRT.gameObject.AddComponent<TextMeshProUGUI>();
            title.text      = "Semua Aksara — Contoh Coretan";
            title.fontSize  = 18f;
            title.fontStyle = FontStyles.Bold;
            title.color     = Color.white;
            title.alignment = TextAlignmentOptions.MidlineLeft;

            var closeBtnRT = Child(headerRT, "BtnClose");
            closeBtnRT.anchorMin = new Vector2(1f, 0.1f);
            closeBtnRT.anchorMax = new Vector2(1f, 0.9f);
            closeBtnRT.pivot     = new Vector2(1f, 0.5f);
            closeBtnRT.anchoredPosition = new Vector2(-10f, 0f);
            closeBtnRT.sizeDelta = new Vector2(110f, 0f);
            var closeImg = closeBtnRT.gameObject.AddComponent<Image>();
            closeImg.color = new Color(0.80f, 0.15f, 0.15f, 1f);
            var closeBtn = closeBtnRT.gameObject.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            closeBtn.onClick.AddListener(Hide);
            var closeLblRT = Child(closeBtnRT, "Lbl");
            closeLblRT.anchorMin = Vector2.zero; closeLblRT.anchorMax = Vector2.one;
            closeLblRT.offsetMin = closeLblRT.offsetMax = Vector2.zero;
            var closeLbl = closeLblRT.gameObject.AddComponent<TextMeshProUGUI>();
            closeLbl.text      = "Tutup";
            closeLbl.fontSize  = 15f;
            closeLbl.fontStyle = FontStyles.Bold;
            closeLbl.color     = Color.white;
            closeLbl.alignment = TextAlignmentOptions.Center;

            // Scroll view
            var scrollRT = Child(cardRT, "Scroll");
            scrollRT.anchorMin = Vector2.zero;
            scrollRT.anchorMax = Vector2.one;
            scrollRT.offsetMin = Vector2.zero;
            scrollRT.offsetMax = new Vector2(0f, -52f);
            scrollRT.gameObject.AddComponent<Image>().color = Color.clear;
            var scroll = scrollRT.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal        = false;
            scroll.vertical          = true;
            scroll.scrollSensitivity = 35f;
            scroll.movementType      = ScrollRect.MovementType.Clamped;

            // Viewport
            var vpRT = Child(scrollRT, "Viewport");
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
            vpRT.gameObject.AddComponent<Image>().color = Color.white;
            vpRT.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = vpRT;

            // Content / grid (no ContentSizeFitter — height set manually in Populate)
            var contentRT = Child(vpRT, "Content");
            contentRT.anchorMin        = new Vector2(0f, 1f);
            contentRT.anchorMax        = new Vector2(1f, 1f);
            contentRT.pivot            = new Vector2(0.5f, 1f);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta        = Vector2.zero;
            scroll.content = contentRT;
            _grid = contentRT;

            var glg = contentRT.gameObject.AddComponent<GridLayoutGroup>();
            glg.padding         = new RectOffset((int)PAD_H, (int)PAD_H, (int)PAD_H, (int)PAD_B);
            glg.cellSize        = new Vector2(CELL_W, CELL_H);
            glg.spacing         = new Vector2(CELL_GAP, CELL_GAP);
            glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = COLS;
            glg.childAlignment  = TextAnchor.UpperLeft;

            _overlay.SetActive(false);
        }

        // ── populate ──────────────────────────────────────────────────────────

        private IEnumerator PopulateNextFrame(List<AksaraCard> cards)
        {
            yield return null;
            Populate(cards);
        }

        private void Populate(List<AksaraCard> cards)
        {
            while (_grid.childCount > 0)
                DestroyImmediate(_grid.GetChild(0).gameObject);

            if (cards == null || cards.Count == 0) return;

            foreach (var card in cards)
                CreateCell(card);

            // Set content height manually
            int   rows   = Mathf.CeilToInt((float)cards.Count / COLS);
            float height = PAD_H + rows * CELL_H + Mathf.Max(0, rows - 1) * CELL_GAP + PAD_B;
            _grid.sizeDelta = new Vector2(0f, height);
        }

        private void CreateCell(AksaraCard card)
        {
            var cellRT = Child(_grid, "Cell_" + card.latinName);
            cellRT.gameObject.AddComponent<Image>().color = new Color(0.14f, 0.20f, 0.36f, 1f);

            var vlg = cellRT.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment         = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = true;
            vlg.padding                = new RectOffset(6, 6, 8, 6);
            vlg.spacing                = 4f;

            // ── stroke preview texture ────────────────────────────────────────
            var previewRT = Child(cellRT, "Preview");
            var previewLE = previewRT.gameObject.AddComponent<LayoutElement>();
            previewLE.preferredHeight = CELL_H * 0.72f;
            previewLE.flexibleWidth   = 1f;
            var rawImg = previewRT.gameObject.AddComponent<RawImage>();
            rawImg.color   = Color.white;
            rawImg.texture = BuildStrokeTexture(card);

            // ── latin name label ──────────────────────────────────────────────
            var nameRT = Child(cellRT, "Name");
            var nameLE = nameRT.gameObject.AddComponent<LayoutElement>();
            nameLE.preferredHeight = CELL_H * 0.20f;
            nameLE.flexibleWidth   = 1f;
            var nameTxt = nameRT.gameObject.AddComponent<TextMeshProUGUI>();
            nameTxt.text      = card.latinName.ToUpper();
            nameTxt.fontSize  = 13f;
            nameTxt.color     = new Color(0.85f, 0.95f, 1f, 1f);
            nameTxt.alignment = TextAlignmentOptions.Center;
            nameTxt.fontStyle = FontStyles.Bold;
        }

        // ── stroke rendering ──────────────────────────────────────────────────

        private static Texture2D BuildStrokeTexture(AksaraCard card)
        {
            // Look up template by label (same mapping as GetStrokeLabel in TracePanel)
            string label = GetTemplateLabel(card.latinName);

            List<List<Vector2>> strokes = null;
            if (_templateCache != null)
                _templateCache.TryGetValue(label, out strokes);

            int    size   = TEX_SIZE;
            var    tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            // Background — dark navy
            var bg = new Color32(28, 40, 70, 255);
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = bg;
            tex.SetPixels32(pixels);

            if (strokes != null && strokes.Count > 0)
            {
                // Compute tight bounding box of all stroke points
                float minX = float.MaxValue, maxX = float.MinValue;
                float minY = float.MaxValue, maxY = float.MinValue;
                foreach (var stroke in strokes)
                    foreach (var p in stroke)
                    {
                        if (p.x < minX) minX = p.x; if (p.x > maxX) maxX = p.x;
                        if (p.y < minY) minY = p.y; if (p.y > maxY) maxY = p.y;
                    }
                float rangeX = Mathf.Max(maxX - minX, 0.01f);
                float rangeY = Mathf.Max(maxY - minY, 0.01f);
                float scale  = Mathf.Min(
                    (size - BRUSH_R * 4) / rangeX,
                    (size - BRUSH_R * 4) / rangeY);
                float offX   = (size - rangeX * scale) * 0.5f;
                float offY   = (size - rangeY * scale) * 0.5f;

                // Color per stroke (up to 4 distinct hues)
                Color32[] strokeColors =
                {
                    new Color32(255, 80,  80,  255),   // stroke 1 — red
                    new Color32(80,  200, 120, 255),   // stroke 2 — green
                    new Color32(80,  160, 255, 255),   // stroke 3 — blue
                    new Color32(255, 200, 60,  255),   // stroke 4 — yellow
                };

                for (int si = 0; si < strokes.Count; si++)
                {
                    var stroke = strokes[si];
                    var col    = strokeColors[si % strokeColors.Length];

                    for (int pi = 0; pi < stroke.Count; pi++)
                    {
                        var p   = stroke[pi];
                        // JSON: y=0=top → flip for texture (y=0=bottom)
                        int tx  = Mathf.RoundToInt((p.x - minX) * scale + offX);
                        int ty  = Mathf.RoundToInt((1f - p.y - (1f - maxY)) * scale + offY);
                        // Equivalent: ty = size - 1 - RoundToInt((p.y - minY) * scale + offY)
                        ty = size - 1 - Mathf.RoundToInt((p.y - minY) * scale + offY);

                        // Draw line to previous point
                        if (pi > 0)
                        {
                            var prev = stroke[pi - 1];
                            int px0  = Mathf.RoundToInt((prev.x - minX) * scale + offX);
                            int py0  = size - 1 - Mathf.RoundToInt((prev.y - minY) * scale + offY);
                            DrawLine(tex, px0, py0, tx, ty, col);
                        }
                        else
                        {
                            // Draw start dot (slightly larger)
                            DrawDisk(tex, tx, ty, BRUSH_R + 1, new Color32(255, 255, 255, 200));
                        }

                        DrawDisk(tex, tx, ty, BRUSH_R, col);
                    }
                }

                // Stroke order numbers (1, 2, ...) at stroke start points
                // (kept simple — just a bright dot at start of each stroke, already drawn above)
            }

            tex.Apply(false);
            return tex;
        }

        private static void DrawDisk(Texture2D tex, int cx, int cy, int r, Color32 col)
        {
            int size = tex.width;
            for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
            {
                if (dx * dx + dy * dy > r * r) continue;
                int nx = cx + dx, ny = cy + dy;
                if ((uint)nx >= (uint)size || (uint)ny >= (uint)size) continue;
                tex.SetPixel(nx, ny, col);
            }
        }

        private static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color32 col)
        {
            int dx  = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
            int sx  = x0 < x1 ? 1 : -1,   sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            while (true)
            {
                DrawDisk(tex, x0, y0, BRUSH_R, col);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 <  dx) { err += dx; y0 += sy; }
            }
        }

        private static string GetTemplateLabel(string latinName)
        {
            string key = latinName.ToLower().Trim();
            if (key == "é") return "e_panelenng";
            if (key == "e") return "e_pamepet";
            int paren = key.IndexOf('(');
            if (paren > 0) key = key.Substring(0, paren).Trim();
            return key.Replace(' ', '_');
        }
    }
}
