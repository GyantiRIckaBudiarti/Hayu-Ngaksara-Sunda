using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace HayuNgaksara
{
    /// <summary>
    /// HUD objektif gaya RPG (pojok kanan atas). Menampilkan level aktif + checklist
    /// 4 aktivitas (Membaca/Menulis/Pelafalan/Refleksi) beserta skornya. Aktivitas
    /// selesai → ✓ + skor (mis. 7/7); aktivitas berjalan → ➤ (disorot); berikutnya → •.
    /// Hanya tampil di scene 00_Sekolah. Self-bootstrap (tak butuh setup scene).
    /// </summary>
    public class ObjectiveHUD : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded += (scene, _) => TrySpawn(scene.name);
            TrySpawn(SceneManager.GetActiveScene().name);
        }

        private static void TrySpawn(string sceneName)
        {
            var existing = FindObjectOfType<ObjectiveHUD>();
            if (existing != null) { existing.OnSceneChanged(sceneName); return; }
            if (sceneName == "00_Sekolah")
                new GameObject("ObjectiveHUD").AddComponent<ObjectiveHUD>();
        }

        // ── warna (hex rich-text) ─────────────────────────────────────────────
        private const string CDone    = "#4FE04F"; // hijau — selesai
        private const string CScore   = "#66CCFF"; // biru  — skor
        private const string CCurrent = "#FFD23A"; // kuning— sedang dikerjakan
        private const string CNext    = "#7A7A7A"; // abu   — berikutnya
        private const string CNpc     = "#B0B0B0"; // abu terang — nama NPC

        [Header("UI (dibuat otomatis)")]
        [SerializeField] private GameObject      panelObjective;
        [SerializeField] private TextMeshProUGUI txtLabel;
        [SerializeField] private TextMeshProUGUI txtBody;

        private string _lastText;

        private void Awake()
        {
            if (FindObjectsOfType<ObjectiveHUD>().Length > 1) { Destroy(gameObject); return; }
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            EnsureUI();
            Subscribe();
            Refresh();
        }

        private void OnDestroy()
        {
            if (ChapterManager.Instance == null) return;
            ChapterManager.Instance.OnChapterCompleted     -= OnChapterEvent;
            ChapterManager.Instance.OnChapterUnlocked      -= OnChapterEvent;
            ChapterManager.Instance.OnSubActivityCompleted -= OnChapterEvent;
        }

        private void OnSceneChanged(string sceneName)
        {
            bool inOverworld = sceneName == "00_Sekolah";
            if (panelObjective != null)
                panelObjective.SetActive(inOverworld && !string.IsNullOrEmpty(_lastText));
            if (inOverworld) { Subscribe(); Refresh(); }
        }

        private void Subscribe()
        {
            if (ChapterManager.Instance == null) return;
            ChapterManager.Instance.OnChapterCompleted     -= OnChapterEvent;
            ChapterManager.Instance.OnChapterUnlocked      -= OnChapterEvent;
            ChapterManager.Instance.OnSubActivityCompleted -= OnChapterEvent;
            ChapterManager.Instance.OnChapterCompleted     += OnChapterEvent;
            ChapterManager.Instance.OnChapterUnlocked      += OnChapterEvent;
            ChapterManager.Instance.OnSubActivityCompleted += OnChapterEvent;
        }

        private void OnChapterEvent(ChapterID _) => Refresh();

        // ─── logika ─────────────────────────────────────────────────────────
        private void Refresh()
        {
            BuildQuest(out string header, out string body);

            string key = header + "" + body;
            if (key == _lastText) return;
            _lastText = key;

            bool show = !string.IsNullOrEmpty(body)
                        && SceneManager.GetActiveScene().name == "00_Sekolah";
            if (panelObjective != null) panelObjective.SetActive(show);
            if (txtLabel != null) txtLabel.text = header;
            if (txtBody  != null) txtBody.text  = body;
        }

        private void BuildQuest(out string header, out string body)
        {
            header = "[ OBJEKTIF ]";
            body   = "";

            var cm = ChapterManager.Instance;
            if (cm == null) return;

            // Tutorial dulu
            if (!cm.IsCompleted(ChapterID.Tutorial))
            {
                body = $"<color={CCurrent}>➤ Temui <b>Jajang</b> untuk memulai!</color>";
                return;
            }

            ChapterID chapter = cm.GetCurrentActiveChapter();

            // Ujian Akhir DIHAPUS → chapter aktif == UjianFinal berarti SEMUA level tuntas.
            if (chapter == ChapterID.UjianFinal)
            {
                header = "[ SELESAI ]";
                body   = $"<color={CDone}>Semua level selesai. Hebat! 🎉</color>";
                return;
            }

            header = "[ OBJEKTIF ]  " + GetChapterName(chapter);

            // Refleksi DIHAPUS → hanya 3 latihan; setelah beres, naik level lewat Jajang.
            string[] act   = { "baca",    "tulis",   "pelafalan" };
            string[] label = { "Membaca", "Menulis", "Pelafalan" };
            string[] npc   = { "Sinta",   "Ucup",    "Nabila"    };

            var sb = new StringBuilder();
            bool currentMarked = false;
            for (int i = 0; i < act.Length; i++)
            {
                if (cm.IsSubActivityDone(chapter, act[i]))
                {
                    int sc = cm.GetSubActivityScore(chapter, act[i]);
                    int tt = cm.GetSubActivityTotal(chapter, act[i]);
                    string score = tt > 0
                        ? $"  <color={CScore}>{sc}/{tt}</color>"
                        : $"  <color={CScore}>✓</color>";
                    sb.AppendLine($"<color={CDone}>✓ {label[i]}</color> <color={CNpc}>({npc[i]})</color>{score}");
                }
                else if (!currentMarked)
                {
                    currentMarked = true;
                    sb.AppendLine($"<color={CCurrent}>➤ {label[i]} <color={CNpc}>({npc[i]})</color></color>");
                }
                else
                {
                    sb.AppendLine($"<color={CNext}>•  {label[i]} ({npc[i]})</color>");
                }
            }
            // 3 latihan beres → langkah terakhir: naik level via Jajang
            if (!currentMarked)
                sb.AppendLine($"<color={CCurrent}>➤ Temui <b>Pak Jajang</b> untuk NAIK LEVEL!</color>");
            body = sb.ToString().TrimEnd();
        }

        private static string GetChapterName(ChapterID id)
        {
            switch (id)
            {
                case ChapterID.Swara:      return "Aksara Swara";
                case ChapterID.Ngalagena1: return "Ngalagena I";
                case ChapterID.Ngalagena2: return "Ngalagena II";
                case ChapterID.Rarangken:  return "Rarangken";
                case ChapterID.UjianFinal: return "Ujian Akhir";
                default: return id.ToString();
            }
        }

        // ─── auto-UI ────────────────────────────────────────────────────────
        private void EnsureUI()
        {
            if (panelObjective != null && txtBody != null) return;

            var cvGO = new GameObject("Canvas_ObjectiveHUD");
            DontDestroyOnLoad(cvGO);
            var canvas = cvGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = cvGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            cvGO.AddComponent<GraphicRaycaster>();

            // Panel latar
            var panel = new GameObject("Panel_Objective");
            panel.transform.SetParent(canvas.transform, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-16f, -16f);
            rt.sizeDelta        = new Vector2(540f, 200f);
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.68f);

            // Header
            var goLabel = new GameObject("Txt_Label");
            goLabel.transform.SetParent(panel.transform, false);
            var rtL = goLabel.AddComponent<RectTransform>();
            rtL.anchorMin = Vector2.zero; rtL.anchorMax = Vector2.one;
            rtL.offsetMin = new Vector2(14f, 160f);
            rtL.offsetMax = new Vector2(-14f, -10f);
            var tmpL = goLabel.AddComponent<TextMeshProUGUI>();
            tmpL.text      = "[ OBJEKTIF ]";
            tmpL.fontSize  = 17f;
            tmpL.fontStyle = FontStyles.Bold;
            tmpL.color     = new Color(1f, 0.85f, 0.2f, 1f);
            tmpL.alignment = TextAlignmentOptions.TopLeft;
            tmpL.enableWordWrapping = false;

            // Body (checklist)
            var goBody = new GameObject("Txt_Body");
            goBody.transform.SetParent(panel.transform, false);
            var rtB = goBody.AddComponent<RectTransform>();
            rtB.anchorMin = Vector2.zero; rtB.anchorMax = Vector2.one;
            rtB.offsetMin = new Vector2(16f, 10f);
            rtB.offsetMax = new Vector2(-14f, -40f);
            var tmpB = goBody.AddComponent<TextMeshProUGUI>();
            tmpB.fontSize          = 16f;
            tmpB.color             = Color.white;
            tmpB.richText          = true;
            tmpB.alignment         = TextAlignmentOptions.TopLeft;
            tmpB.enableWordWrapping = true;
            tmpB.lineSpacing        = 6f;

            panelObjective = panel;
            txtLabel       = tmpL;
            txtBody        = tmpB;
        }
    }
}
