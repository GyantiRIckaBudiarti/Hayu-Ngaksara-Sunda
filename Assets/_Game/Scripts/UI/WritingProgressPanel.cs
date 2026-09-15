using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace HayuNgaksara
{
    /// <summary>
    /// Panel progres menulis — menampilkan persentase aksara yang sudah dipraktikkan trace.
    /// Auto-spawn di scene 00_Sekolah. Buka via WritingProgressPanel.Instance.Show().
    /// </summary>
    public class WritingProgressPanel : MonoBehaviour
    {
        public static WritingProgressPanel Instance { get; private set; }

        // ── Auto-spawn ────────────────────────────────────────────────────────
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded += (scene, _) => TrySpawn(scene.name);
            TrySpawn(SceneManager.GetActiveScene().name);
        }

        private static void TrySpawn(string sceneName)
        {
            if (sceneName != "00_Sekolah") return;
            if (FindObjectOfType<WritingProgressPanel>() != null) return;
            new GameObject("WritingProgressPanel").AddComponent<WritingProgressPanel>();
        }

        // ── Runtime UI refs (dibuat di BuildUI) ──────────────────────────────
        private GameObject         _panel;
        private TextMeshProUGUI    _txtPercent;
        private TextMeshProUGUI    _txtDetail;
        private Transform          _gridParent;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            BuildUI();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Show()
        {
            Refresh();
            _panel?.SetActive(true);
        }

        public void Hide() => _panel?.SetActive(false);

        // ── Logic ─────────────────────────────────────────────────────────────

        private void Refresh()
        {
            var db = GameManager.Instance?.AksaraDatabase;
            if (db == null || db.semuaAksara == null) return;

            var aksaraList = db.semuaAksara;
            int total     = aksaraList.Count;
            int practiced = 0;

            // Bersihkan grid lama
            if (_gridParent != null)
                foreach (Transform c in _gridParent) Destroy(c.gameObject);

            foreach (var aksara in aksaraList)
            {
                bool done = PlayerPrefs.GetInt($"trace_{aksara.namaHuruf.ToLower()}_practiced", 0) == 1;
                if (done) practiced++;
                AddGridItem(aksara.namaHuruf, done);
            }

            float pct = total > 0 ? (float)practiced / total * 100f : 0f;
            if (_txtPercent) _txtPercent.text = $"{Mathf.RoundToInt(pct)}%";
            if (_txtDetail)  _txtDetail.text  = $"{practiced} dari {total} aksara sudah dilatih";
        }

        private void AddGridItem(string name, bool done)
        {
            if (_gridParent == null) return;

            var go = new GameObject(name);
            go.transform.SetParent(_gridParent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(60f, 60f);

            var bg = go.AddComponent<Image>();
            bg.color = done ? new Color(0.2f, 0.75f, 0.3f, 0.9f) : new Color(0.3f, 0.3f, 0.3f, 0.5f);

            var lblGO = new GameObject("Lbl");
            lblGO.transform.SetParent(go.transform, false);
            var lblRT = lblGO.AddComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
            var tmp = lblGO.AddComponent<TextMeshProUGUI>();
            tmp.text      = (done ? "✓\n" : "○\n") + name.ToUpper();
            tmp.fontSize  = 9f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Color.white;
        }

        // ── UI Builder ────────────────────────────────────────────────────────

        private void BuildUI()
        {
            Canvas canvas = null;
            foreach (var c in FindObjectsOfType<Canvas>())
                if (c.renderMode == RenderMode.ScreenSpaceOverlay) { canvas = c; break; }
            if (canvas == null)
            {
                var cvGO = new GameObject("Canvas_WritingProgress");
                canvas = cvGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = cvGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                cvGO.AddComponent<GraphicRaycaster>();
            }

            // ── Overlay gelap ──────────────────────────────────────────────
            _panel = new GameObject("Panel_WritingProgress");
            _panel.transform.SetParent(canvas.transform, false);

            var rtOv = _panel.AddComponent<RectTransform>();
            rtOv.anchorMin = Vector2.zero; rtOv.anchorMax = Vector2.one;
            rtOv.offsetMin = rtOv.offsetMax = Vector2.zero;

            var imgOv = _panel.AddComponent<Image>();
            imgOv.color = new Color(0f, 0f, 0f, 0.75f);

            // Tombol tutup saat klik overlay
            var btnOv = _panel.AddComponent<Button>();
            btnOv.onClick.AddListener(Hide);

            // ── Card tengah ────────────────────────────────────────────────
            var card = new GameObject("Card");
            card.transform.SetParent(_panel.transform, false);
            var rtCard = card.AddComponent<RectTransform>();
            rtCard.anchorMin = rtCard.anchorMax = rtCard.pivot = new Vector2(0.5f, 0.5f);
            rtCard.sizeDelta = new Vector2(560f, 460f);
            rtCard.anchoredPosition = Vector2.zero;

            var imgCard = card.AddComponent<Image>();
            imgCard.color = new Color(0.08f, 0.10f, 0.18f, 1f);

            // Blok click-through ke overlay
            var btnCard = card.AddComponent<Button>();
            btnCard.onClick.AddListener(() => { }); // kosong — cegah overlay tertutup saat klik card

            // ── Judul ──────────────────────────────────────────────────────
            AddText(card.transform, "PROGRES MENULIS", 22f, FontStyles.Bold,
                    new Color(1f, 0.85f, 0.2f), new Vector2(0f, 195f), new Vector2(500f, 36f));

            // ── Persentase besar ───────────────────────────────────────────
            var goP = new GameObject("Txt_Percent");
            goP.transform.SetParent(card.transform, false);
            var rtP = goP.AddComponent<RectTransform>();
            rtP.anchorMin = rtP.anchorMax = rtP.pivot = new Vector2(0.5f, 1f);
            rtP.anchoredPosition = new Vector2(0f, -40f);
            rtP.sizeDelta = new Vector2(200f, 64f);
            _txtPercent = goP.AddComponent<TextMeshProUGUI>();
            _txtPercent.fontSize  = 52f;
            _txtPercent.fontStyle = FontStyles.Bold;
            _txtPercent.alignment = TextAlignmentOptions.Center;
            _txtPercent.color     = Color.white;

            // ── Detail teks ────────────────────────────────────────────────
            var goD = new GameObject("Txt_Detail");
            goD.transform.SetParent(card.transform, false);
            var rtD = goD.AddComponent<RectTransform>();
            rtD.anchorMin = rtD.anchorMax = rtD.pivot = new Vector2(0.5f, 1f);
            rtD.anchoredPosition = new Vector2(0f, -108f);
            rtD.sizeDelta = new Vector2(480f, 30f);
            _txtDetail = goD.AddComponent<TextMeshProUGUI>();
            _txtDetail.fontSize  = 14f;
            _txtDetail.alignment = TextAlignmentOptions.Center;
            _txtDetail.color     = new Color(0.8f, 0.8f, 0.8f);

            // ── Grid aksara ────────────────────────────────────────────────
            var gridGO = new GameObject("Grid");
            gridGO.transform.SetParent(card.transform, false);
            var rtGrid = gridGO.AddComponent<RectTransform>();
            rtGrid.anchorMin = rtGrid.anchorMax = rtGrid.pivot = new Vector2(0.5f, 1f);
            rtGrid.anchoredPosition = new Vector2(0f, -145f);
            rtGrid.sizeDelta = new Vector2(520f, 270f);

            var glg = gridGO.AddComponent<GridLayoutGroup>();
            glg.cellSize        = new Vector2(60f, 60f);
            glg.spacing         = new Vector2(8f, 8f);
            glg.startCorner     = GridLayoutGroup.Corner.UpperLeft;
            glg.childAlignment  = TextAnchor.UpperCenter;
            _gridParent = gridGO.transform;

            // ── Tombol tutup ───────────────────────────────────────────────
            var closeGO = new GameObject("Btn_Tutup");
            closeGO.transform.SetParent(card.transform, false);
            var rtClose = closeGO.AddComponent<RectTransform>();
            rtClose.anchorMin = rtClose.anchorMax = rtClose.pivot = new Vector2(0.5f, 0f);
            rtClose.anchoredPosition = new Vector2(0f, 18f);
            rtClose.sizeDelta = new Vector2(160f, 40f);
            var imgClose = closeGO.AddComponent<Image>();
            imgClose.color = new Color(0.7f, 0.15f, 0.15f);
            var btnClose = closeGO.AddComponent<Button>();
            btnClose.onClick.AddListener(Hide);
            AddText(closeGO.transform, "Tutup", 15f, FontStyles.Normal, Color.white, Vector2.zero, new Vector2(160f, 40f));

            _panel.SetActive(false);
        }

        private static void AddText(Transform parent, string text, float size, FontStyles style,
                                    Color color, Vector2 pos, Vector2 sizeDelta)
        {
            var go = new GameObject("Txt");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = sizeDelta;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center; tmp.color = color;
        }
    }
}
