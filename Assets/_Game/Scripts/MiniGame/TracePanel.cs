using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace HayuNgaksara
{
    // Komponen Panel Latihan Menulis (Trace).
    // Tempelkan pada root Panel_Trace. Menerima pointer down/drag untuk menggambar.
    public class TracePanel : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Guide & Canvas")]
        [SerializeField] private Image          imgGuide;        // aksara semi-transparan (sprite)
        [SerializeField] private TextMeshProUGUI txtGuideAksara;  // teks aksara dengan Sundanese font
        [SerializeField] private TMPro.TMP_FontAsset sundaneseFont; // assign sama dengan MiniGameManager
        [SerializeField] private RawImage       rawDraw;         // kanvas putih tempat menggambar
        [SerializeField] private RectTransform  strokeContainer; // parent untuk ikon panah stroke order

        [Header("Stroke Order Guide")]
        [SerializeField] private Image          imgStrokeOrderGuide; // gambar urutan coretan dari PDF

        [Header("Mascot")]
        [SerializeField] private Image          imgMascot;       // karakter maskot di samping papan panduan

        [Header("Info")]
        [SerializeField] private TextMeshProUGUI txtCardName;   // "PANGHULU (I)"
        [SerializeField] private TextMeshProUGUI txtProgress;   // "1 / 6"
        [SerializeField] private TextMeshProUGUI txtHint;

        [Header("Buttons")]
        [SerializeField] private Button          btnReset;
        [SerializeField] private Button          btnNext;
        [SerializeField] private TextMeshProUGUI txtBtnNext;    // label tombol

        [Header("Drawing Settings")]
        [SerializeField] private Color brushColor = Color.black;
        [SerializeField, Range(2, 30)] private int brushSize = 6;
        [SerializeField] private int texSize = 512;

        // ── private state ────────────────────────────────
        private Texture2D _tex;
        private Color32[] _clearPixels;
        private Action    _onNext;
        private Action    _onStartQuiz;
        private bool      _isLastCard;
        private readonly List<GameObject> _strokeMarkers = new List<GameObject>();
        private Coroutine _pulseCoroutine;
        private Vector2   _lastPaintPos = Vector2.negativeInfinity;
        private int       _strokeCount;

        // ── template overview button ──────────────────────
        private List<AksaraCard>      _sessionCards;
        private TMP_FontAsset         _sessionFont;
        private GameObject            _btnAllTemplates;

        // ── recognizer (Direction-DTW; QDollarRecognizer kept as fallback) ──────
        private static DirectionRecognizer _recognizer; // shared, load sekali
        private List<List<Vector2>> _qStrokes = new List<List<Vector2>>();
        private List<Vector2>       _qCurrent;
        private string              _qLabel;            // label aksara saat ini

        private static readonly System.Collections.Generic.HashSet<string> _swaraQ =
            new System.Collections.Generic.HashSet<string> { "a","i","u","e_pamepet","e_panelenng","o","eu" };
        private static readonly System.Collections.Generic.HashSet<string> _rarangkenQ =
            new System.Collections.Generic.HashSet<string> { "panghulu","pamepet_r","paneuleung","panglayar","pangecek","panguku" };
        private static float GetQThreshold(string label)
        {
            if (_swaraQ.Contains(label))     return 0.60f;
            if (_rarangkenQ.Contains(label)) return 0.55f;
            return 0.50f;
        }

        // Jumlah kartu yang dikenali SEMPURNA (best-match == expected) dalam sesi ini
        public int BestMatchCount { get; private set; }
        public int TotalTracedCount { get; private set; }

        // ── debug save folder (editor only) ──────────────
        // Hasil disimpan ke <ProjectRoot>/TraceDebug/<label>/
        private static string DebugDir =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "TraceDebug");

        // Per-aksara tracking untuk summary akhir sesi
        private class LabelResult
        {
            public int    attempts;          // total kali klik "Berikutnya"
            public int    rejects;           // berapa kali REJECT sebelum akhirnya diterima
            public bool   finalAccepted;
            public float  bestScoreBest;     // score_best tertinggi selama sesi ini
            public float  bestScoreDirect;   // score_direct tertinggi
            public string lastRecognized;    // hasil recognize terakhir
        }
        private static readonly Dictionary<string, LabelResult> _labelResults
            = new Dictionary<string, LabelResult>();

        // ── lifecycle ────────────────────────────────────
        private void Awake()
        {
            btnReset?.onClick.AddListener(ResetDraw);
            btnNext?.onClick.AddListener(OnNextClicked);

            if (_recognizer == null)
            {
                _recognizer = new DirectionRecognizer();
                _recognizer.LoadFromResources("QTemplates");
            }
        }

        private void OnEnable()
        {
            if (_tex == null) InitTexture();
            EnsureGuideText();
        }

        // ── public API ───────────────────────────────────
        /// <summary>Call saat panel dibuka untuk setiap kartu.</summary>
        public void Setup(AksaraCard card, int cardIdx, int totalCards,
                          Action onNext, Action onStartQuiz)
        {
            _onNext      = onNext;
            _onStartQuiz = onStartQuiz;
            _isLastCard  = (cardIdx == totalCards - 1);
            _lastPaintPos = Vector2.negativeInfinity;

            if (cardIdx == 0) { BestMatchCount = 0; TotalTracedCount = 0; _labelResults.Clear(); InitDebugSession(); }

            if (_tex == null) InitTexture();
            ResetDraw();

            // Selalu gunakan font Unicode Sundanese — sembunyikan sprite
            if (imgGuide != null) imgGuide.gameObject.SetActive(false);
            if (_pulseCoroutine != null) { StopCoroutine(_pulseCoroutine); _pulseCoroutine = null; }

            // Tampilkan karakter dengan font Sundanese
            EnsureGuideText();
            if (txtGuideAksara != null)
            {
                txtGuideAksara.text = card.aksaraChar;
                txtGuideAksara.gameObject.SetActive(true);
                txtGuideAksara.enabled = true;
                txtGuideAksara.color = new Color(0.2f, 0.2f, 0.2f, 0.65f);
                if (sundaneseFont != null) txtGuideAksara.font = sundaneseFont;
                txtGuideAksara.fontSize = 100f;
                txtGuideAksara.ForceMeshUpdate(true, true);
            }

            if (txtCardName) txtCardName.text = card.latinName.ToUpper();
            if (txtProgress) txtProgress.text = $"{cardIdx + 1} / {totalCards}";
            if (txtHint)     txtHint.text      = $"Tiru aksara '{card.latinName.ToUpper()}' di kiri → gambar di kotak putih!";
            if (txtBtnNext)  txtBtnNext.text   = _isLastCard ? "Selesai →" : "Berikutnya →";

            _qLabel = GetStrokeLabel(card.latinName);

            LoadStrokeOrderImage(card.latinName);
            BuildStrokeMarkers(card.strokeOrder);
        }

        /// <summary>
        /// Dipanggil sekali saat sesi trace dimulai.
        /// Menyimpan daftar kartu dan membuat/menampilkan tombol "Lihat Semua Template".
        /// </summary>
        public void SetSessionCards(List<AksaraCard> cards, TMP_FontAsset font)
        {
            _sessionCards = cards;
            _sessionFont  = font;   // kept for possible future use
            EnsureAllTemplatesButton();
        }

        private void EnsureAllTemplatesButton()
        {
            if (_btnAllTemplates != null) { _btnAllTemplates.SetActive(true); return; }

            // Cari canvas root
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            // Buat button — gunakan typeof(RectTransform) di konstruktor agar tidak ada Transform→RT replacement bug
            _btnAllTemplates = new GameObject("BtnAllTemplates", typeof(RectTransform));
            _btnAllTemplates.transform.SetParent(transform, false);

            var rt = (RectTransform)_btnAllTemplates.transform;
            rt.anchorMin        = new Vector2(0f, 0f);   // kiri-BAWAH agar tak menutupi judul
            rt.anchorMax        = new Vector2(0f, 0f);
            rt.pivot            = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(14f, 14f);
            rt.sizeDelta        = new Vector2(190f, 46f);

            var img = _btnAllTemplates.AddComponent<Image>();
            img.color = new Color(0.18f, 0.22f, 0.31f, 1f);

            var btn = _btnAllTemplates.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => AllTemplatesView.Show(_sessionCards));

            var lblGO = new GameObject("Lbl", typeof(RectTransform));
            lblGO.transform.SetParent(_btnAllTemplates.transform, false);
            var lblRT = (RectTransform)lblGO.transform;
            lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
            var lbl = lblGO.AddComponent<TextMeshProUGUI>();
            lbl.text      = "Lihat Semua Aksara";
            lbl.fontSize  = 13f;
            lbl.fontStyle = FontStyles.Bold;
            lbl.color     = Color.white;
            lbl.alignment = TextAlignmentOptions.Center;
        }

        public void ResetDraw()
        {
            if (_tex == null || _clearPixels == null) return;
            _tex.SetPixels32(_clearPixels);
            _tex.Apply(false);
            _lastPaintPos = Vector2.negativeInfinity;
            _strokeCount  = 0;
            _qStrokes.Clear();
            _qCurrent = null;
        }

        // Hitung pixel coverage — dipakai oleh validasi dan pesan error
        private float GetPixelCoverage()
        {
            if (_tex == null) return 0f;
            var px = _tex.GetPixels32();
            int dark = 0;
            foreach (var p in px) if (p.r < 128) dark++;
            return (float)dark / px.Length;
        }

        // Total panjang goresan dalam koordinat [0,1] — spiral panjangnya jauh melebihi aksara normal
        private float GetTotalPathLength()
        {
            float total = 0f;
            foreach (var stroke in _qStrokes)
                for (int i = 1; i < stroke.Count; i++)
                    total += Vector2.Distance(stroke[i], stroke[i - 1]);
            return total;
        }

        // Validasi: batas bawah, batas atas, path length, lalu $Q recognition
        private bool IsDrawingAcceptable()
        {
            if (_tex == null) return true;
            float coverage = GetPixelCoverage();
            if (coverage < 0.01f) return false;
            if (coverage > 0.35f) return false;

            // Tolak coretan asal yang sangat panjang (spiral, coretan bolak-balik berlebihan)
            // Aksara normal: total path 0.5–3.5 unit; spiral/scribble: >> 4.0
            float pathLen = GetTotalPathLength();
            if (pathLen > 4.0f) return false;

            if (_recognizer != null && _qStrokes.Count > 0
                && !string.IsNullOrEmpty(_qLabel)
                && _recognizer.HasTemplateFor(_qLabel))
            {
                float sc;
                string rec = _recognizer.RecognizeForDisplay(_qStrokes, out sc);
                float directScore = _recognizer.ScoreFor(_qLabel, _qStrokes);

                // Accept jika: best-match benar ATAU skor langsung ke label yang diharapkan >= 0.82
                bool bestMatchOk  = rec == _qLabel && sc >= GetQThreshold(_qLabel);
                bool directScoreOk = directScore >= 0.82f;
                bool result = bestMatchOk || directScoreOk;

                float cov = GetPixelCoverage();
                float pth = GetTotalPathLength();
                Debug.Log($"[QDollar] expected={_qLabel} | best={rec}({sc:F3}) | direct={directScore:F3} | bestOk={bestMatchOk} | directOk={directScoreOk} | cov={cov:F3} | path={pth:F2} | ACCEPT={result}");
                return result;
            }

            return coverage >= 0.03f;
        }

        // ── drawing ──────────────────────────────────────
        public void OnPointerDown(PointerEventData e)
        {
            _strokeCount++;
            _lastPaintPos = e.position;
            _qCurrent     = new List<Vector2>();
            RecordQPoint(e.position);
            PaintAt(e.position);
        }

        public void OnDrag(PointerEventData e)
        {
            if (_lastPaintPos == Vector2.negativeInfinity) { PaintAt(e.position); _lastPaintPos = e.position; return; }
            RecordQPoint(e.position);
            float dist = Vector2.Distance(_lastPaintPos, e.position);
            int steps = Mathf.Max(1, Mathf.CeilToInt(dist / (brushSize * 0.5f)));
            for (int i = 1; i <= steps; i++)
                PaintAt(Vector2.Lerp(_lastPaintPos, e.position, (float)i / steps));
            _lastPaintPos = e.position;
        }

        public void OnPointerUp(PointerEventData e)
        {
            _lastPaintPos = Vector2.negativeInfinity;
            if (_qCurrent != null && _qCurrent.Count > 0)
                _qStrokes.Add(_qCurrent);
            _qCurrent = null;
        }

        private void RecordQPoint(Vector2 screenPos)
        {
            if (rawDraw == null || _qCurrent == null) return;
            Camera cam = rawDraw.canvas?.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rawDraw.rectTransform, screenPos, cam, out Vector2 local)) return;
            Rect r = rawDraw.rectTransform.rect;
            _qCurrent.Add(new Vector2(
                Mathf.Clamp01((local.x - r.xMin) / r.width),
                1f - Mathf.Clamp01((local.y - r.yMin) / r.height)));
        }

        private void PaintAt(Vector2 screenPos)
        {
            if (rawDraw == null || _tex == null) return;

            Camera cam = rawDraw.canvas != null ? rawDraw.canvas.worldCamera : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rawDraw.rectTransform, screenPos, cam, out Vector2 local)) return;

            Rect r = rawDraw.rectTransform.rect;
            float u = (local.x - r.xMin) / r.width;
            float v = (local.y - r.yMin) / r.height;

            int cx = Mathf.RoundToInt(u * (texSize - 1));
            int cy = Mathf.RoundToInt(v * (texSize - 1));

            Color32 bc = brushColor;
            int bs = brushSize;
            for (int dx = -bs; dx <= bs; dx++)
            for (int dy = -bs; dy <= bs; dy++)
            {
                if (dx * dx + dy * dy > bs * bs) continue;
                int tx = cx + dx, ty = cy + dy;
                if ((uint)tx >= (uint)texSize || (uint)ty >= (uint)texSize) continue;
                _tex.SetPixel(tx, ty, bc);
            }
            _tex.Apply(false);
        }

        // ── private helpers ──────────────────────────────
        private void InitTexture()
        {
            _tex         = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            _clearPixels = new Color32[texSize * texSize];
            for (int i = 0; i < _clearPixels.Length; i++)
                _clearPixels[i] = new Color32(255, 255, 255, 255);
            _tex.SetPixels32(_clearPixels);
            _tex.Apply(false);
            if (rawDraw != null) rawDraw.texture = _tex;
        }

        // Buat TMP text overlay di posisi imgGuide jika belum di-wire di Inspector
        private void EnsureGuideText()
        {
            if (txtGuideAksara != null || imgGuide == null) return;

            var go = new GameObject("TxtGuideAksara");
            go.transform.SetParent(imgGuide.transform.parent, false);

            var rt    = go.AddComponent<RectTransform>();
            var srcRT = imgGuide.GetComponent<RectTransform>();
            rt.anchorMin        = srcRT.anchorMin;
            rt.anchorMax        = srcRT.anchorMax;
            rt.pivot            = srcRT.pivot;
            rt.anchoredPosition = srcRT.anchoredPosition;
            rt.sizeDelta        = srcRT.sizeDelta;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize           = 80f;
            tmp.color              = new Color(0.3f, 0.3f, 0.3f, 0.55f);
            tmp.alignment          = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;

            if (sundaneseFont != null) tmp.font = sundaneseFont;

            txtGuideAksara = tmp;
        }

        private void OnNextClicked()
        {
            if (!IsDrawingAcceptable())
            {
                float coverage = GetPixelCoverage();
                string msg;

                if (coverage < 0.01f || _qStrokes.Count == 0)
                    msg = "Gambar aksaranya dulu di kotak putih!";
                else if (coverage > 0.35f)
                    msg = "Terlalu banyak coretan! Hapus dan gambar sesuai contoh panduan.";
                else if (GetTotalPathLength() > 4.0f)
                    msg = "Terlalu banyak coretan! Gambar aksara sekali dengan rapi sesuai panduan.";
                else if (_recognizer != null && !string.IsNullOrEmpty(_qLabel)
                         && _recognizer.HasTemplateFor(_qLabel) && _qStrokes.Count > 0)
                {
                    string recognized = _recognizer.RecognizeForDisplay(_qStrokes, out float score);
                    int pct = Mathf.RoundToInt(score * 100f);
                    msg = recognized == _qLabel
                        ? $"Hampir tepat! Lebih teliti lagi ({pct}% mirip)."
                        : "Bentuk tidak sesuai. Ikuti contoh aksara di panduan kiri!";
                }
                else
                    msg = "Gambar terlalu tipis! Ikuti bentuk aksara di panduan kiri.";

                if (txtHint) txtHint.text = msg;

                // Simpan juga yang DITOLAK — penting untuk analisis kenapa gagal
                if (_qStrokes.Count > 0)
                {
                    string recRej = "";
                    float  scRej  = 0f;
                    if (_recognizer != null && !string.IsNullOrEmpty(_qLabel) && _recognizer.HasTemplateFor(_qLabel))
                        recRej = _recognizer.RecognizeForDisplay(_qStrokes, out scRej);
                    SaveDebugTrace(recRej, scRej, accepted: false);
                }
                return;
            }
            // Tracking hasil: apakah best-match sempurna?
            TotalTracedCount++;
            string rec2 = "";
            float  sc2  = 0f;
            if (_recognizer != null && _qStrokes.Count > 0
                && !string.IsNullOrEmpty(_qLabel)
                && _recognizer.HasTemplateFor(_qLabel))
            {
                rec2 = _recognizer.RecognizeForDisplay(_qStrokes, out sc2);
                if (rec2 == _qLabel) BestMatchCount++;
            }

            SaveDebugTrace(rec2, sc2, accepted: true);

            if (_isLastCard)
            {
                SaveSessionSummary(BestMatchCount, TotalTracedCount);
                _onStartQuiz?.Invoke();
            }
            else _onNext?.Invoke();
        }

        // ── Debug trace export ───────────────────────────────────────────

        private static void InitDebugSession() { } // tiap attempt = file sendiri

        /// <summary>
        /// Dipanggil di akhir sesi (kartu terakhir diterima).
        /// Tulis summary.json: score akhir + breakdown per aksara.
        /// </summary>
        private static void SaveSessionSummary(int bestMatchTotal, int totalTraced)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_labelResults.Count == 0) return;

            var ic  = System.Globalization.CultureInfo.InvariantCulture;
            float pct = totalTraced > 0 ? (float)bestMatchTotal / totalTraced * 100f : 0f;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"session_time\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",");
            sb.AppendLine($"  \"total_aksara\": {totalTraced},");
            sb.AppendLine($"  \"correct_first_recognize\": {bestMatchTotal},");
            sb.AppendLine($"  \"score_pct\": {pct.ToString("F1", ic)},");
            sb.AppendLine($"  \"verdict\": \"{(pct >= 70 ? "BAIK" : pct >= 50 ? "CUKUP" : "PERLU PERBAIKAN")}\",");
            sb.AppendLine("  \"per_aksara\": [");

            var labels = new List<string>(_labelResults.Keys);
            labels.Sort();
            for (int i = 0; i < labels.Count; i++)
            {
                var lb = labels[i];
                var lr = _labelResults[lb];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"label\": \"{lb}\",");
                sb.AppendLine($"      \"attempts\": {lr.attempts},");
                sb.AppendLine($"      \"rejects_before_pass\": {lr.rejects},");
                sb.AppendLine($"      \"final_accepted\": {(lr.finalAccepted ? "true" : "false")},");
                sb.AppendLine($"      \"best_score_best\": {lr.bestScoreBest.ToString("F4", ic)},");
                sb.AppendLine($"      \"best_score_direct\": {lr.bestScoreDirect.ToString("F4", ic)},");
                sb.AppendLine($"      \"last_recognized_as\": \"{lr.lastRecognized}\",");
                sb.AppendLine($"      \"misidentified\": {(lr.lastRecognized != lb ? "true" : "false")}");
                sb.Append("    }");
                sb.AppendLine(i < labels.Count - 1 ? "," : "");
            }

            sb.AppendLine("  ]");
            sb.Append("}");

            string dir  = DebugDir;
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"summary_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            File.WriteAllText(path, sb.ToString());
            Debug.Log($"[TraceDebug] Summary → {path}  ({pct:F1}% correct)");
#endif
        }

        /// <summary>
        /// Simpan satu percobaan menggambar ke TraceDebug/{label}/{label}_{OK|REJECT}_{ms}.json
        /// di dalam folder project Unity — langsung bisa dibaca untuk analisis.
        /// </summary>
        private void SaveDebugTrace(string recognized, float score, bool accepted)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_qStrokes == null || _qStrokes.Count == 0 || string.IsNullOrEmpty(_qLabel)) return;

            float directScore = (_recognizer != null)
                ? _recognizer.ScoreFor(_qLabel, _qStrokes) : 0f;
            float coverage = GetPixelCoverage();
            float pathLen  = GetTotalPathLength();

            // Encode strokes — format sama persis dengan QTemplates JSON
            var sb = new System.Text.StringBuilder();
            sb.Append("[");
            for (int si = 0; si < _qStrokes.Count; si++)
            {
                sb.Append("{\"pts\":[");
                var s = _qStrokes[si];
                for (int pi = 0; pi < s.Count; pi++)
                {
                    sb.Append(s[pi].x.ToString("F5", System.Globalization.CultureInfo.InvariantCulture));
                    sb.Append(",");
                    sb.Append(s[pi].y.ToString("F5", System.Globalization.CultureInfo.InvariantCulture));
                    if (pi < s.Count - 1) sb.Append(",");
                }
                sb.Append("]}");
                if (si < _qStrokes.Count - 1) sb.Append(",");
            }
            sb.Append("]");

            var ic  = System.Globalization.CultureInfo.InvariantCulture;
            string tag = accepted ? "OK" : "REJECT";
            string json = "{\n"
                + $"  \"label\": \"{_qLabel}\",\n"
                + $"  \"recognized\": \"{recognized}\",\n"
                + $"  \"correct\": {(recognized == _qLabel ? "true" : "false")},\n"
                + $"  \"accepted\": {(accepted ? "true" : "false")},\n"
                + $"  \"score_best\": {score.ToString("F4", ic)},\n"
                + $"  \"score_direct\": {directScore.ToString("F4", ic)},\n"
                + $"  \"coverage\": {coverage.ToString("F4", ic)},\n"
                + $"  \"path_len\": {pathLen.ToString("F4", ic)},\n"
                + $"  \"num_strokes\": {_qStrokes.Count},\n"
                + $"  \"strokes\": {sb}\n"
                + "}";

            string dir  = Path.Combine(DebugDir, _qLabel);
            Directory.CreateDirectory(dir);
            string file = Path.Combine(dir, $"{_qLabel}_{tag}_{DateTime.Now:HHmmss_fff}.json");
            File.WriteAllText(file, json);
            Debug.Log($"[TraceDebug] {tag} → {file}");

            // Update per-label tracker
            if (!_labelResults.ContainsKey(_qLabel))
                _labelResults[_qLabel] = new LabelResult();
            var lr = _labelResults[_qLabel];
            lr.attempts++;
            if (!accepted) lr.rejects++;
            if (accepted)  lr.finalAccepted = true;
            if (score        > lr.bestScoreBest)   lr.bestScoreBest   = score;
            if (directScore  > lr.bestScoreDirect)  lr.bestScoreDirect = directScore;
            lr.lastRecognized = recognized;
#endif
        }

        // ── Stroke order image ───────────────────────────────────────────
        private static readonly Dictionary<string, string> _strokeOverrides =
            new Dictionary<string, string>
            {
                { "é", "e_panelenng" },
                { "e", "e_pamepet"   },
            };

        // Dipakai oleh LoadStrokeOrderImage dan $Q label lookup
        private string GetStrokeLabel(string latinName)
        {
            string key = latinName.ToLower().Trim();
            if (_strokeOverrides.TryGetValue(key, out string mapped)) return mapped;
            int paren = key.IndexOf('(');
            if (paren > 0) key = key.Substring(0, paren).Trim();
            return key.Replace(' ', '_');
        }

        private void LoadStrokeOrderImage(string latinName)
        {
            if (imgStrokeOrderGuide == null) return;
            string fileName = GetStrokeLabel(latinName);
            var sprite = Resources.Load<Sprite>($"StrokeOrder/stroke_{fileName}");
            imgStrokeOrderGuide.sprite = sprite;
            imgStrokeOrderGuide.gameObject.SetActive(sprite != null);
        }

        // ── Stroke order markers ─────────────────────────────────────────
        private void BuildStrokeMarkers(List<StrokePoint> points)
        {
            foreach (var m in _strokeMarkers) if (m) Destroy(m);
            _strokeMarkers.Clear();

            if (strokeContainer == null || points == null || points.Count == 0) return;

            for (int i = 0; i < points.Count; i++)
            {
                var go = new GameObject($"Stroke_{i+1}");
                go.transform.SetParent(strokeContainer, false);

                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(28, 28);
                Rect r = strokeContainer.rect;
                rt.anchoredPosition = new Vector2(points[i].x * r.width, points[i].y * r.height);

                var bg = go.AddComponent<Image>();
                bg.color = new Color(1f, 0.25f, 0.25f, 0.85f);

                var labelGO = new GameObject("Label");
                labelGO.transform.SetParent(go.transform, false);
                var labelRT = labelGO.AddComponent<RectTransform>();
                labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
                labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
                var txt = labelGO.AddComponent<TMPro.TextMeshProUGUI>();
                txt.text      = (i + 1).ToString();
                txt.fontSize  = 14;
                txt.fontStyle = TMPro.FontStyles.Bold;
                txt.color     = Color.white;
                txt.alignment = TMPro.TextAlignmentOptions.Center;

                _strokeMarkers.Add(go);
            }
        }
    }
}
