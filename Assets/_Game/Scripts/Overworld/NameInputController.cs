using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

namespace HayuNgaksara
{
    public class NameInputController : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button         btnMulai;
        [SerializeField] private string         defaultName = "Raka";
        [SerializeField] private string         nextScene   = "00_Sekolah";

        [Header("Gender Selection (opsional — dibuat otomatis jika kosong)")]
        [SerializeField] private Button btnPria;
        [SerializeField] private Button btnWanita;

        private string _selectedGender = "pria";

        private static readonly Color ColSelected   = new Color(0.20f, 0.45f, 0.80f, 1f);
        private static readonly Color ColUnselected = new Color(0.55f, 0.55f, 0.55f, 0.6f);

        private void Start()
        {
            EnsureGenderUI();
            btnMulai?.onClick.AddListener(OnMulai);
            if (inputField != null)
                inputField.onSubmit.AddListener(_ => OnMulai());
        }

        private void EnsureGenderUI()
        {
            if (btnPria != null && btnWanita != null)
            {
                btnPria.onClick.AddListener(()   => SelectGender("pria"));
                btnWanita.onClick.AddListener(() => SelectGender("wanita"));
                RefreshGenderColors();
                return;
            }

            // Taruh di Canvas root DALAM SCENE ini (bukan DDOL canvas dari scene lain)
            Canvas cv = null;
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach (var c in FindObjectsOfType<Canvas>())
                if (c.gameObject.scene == activeScene) { cv = c; break; }
            if (cv == null) return;
            Transform parent = cv.transform;

            // InputBox bottom at 40%, Btn_Mulai top now at 22% (moved down in scene)
            // Gap center: (40+22)/2 = 31% → offset from center: -19% × 651 ≈ -120
            const float yCenter = -120f;
            btnPria   = CreateGenderButton(parent, "Laki-laki", new Vector2(-90f, yCenter));
            btnWanita = CreateGenderButton(parent, "Perempuan", new Vector2( 90f, yCenter));

            btnPria.onClick.AddListener(()   => SelectGender("pria"));
            btnWanita.onClick.AddListener(() => SelectGender("wanita"));
            RefreshGenderColors();
        }

        private static Button CreateGenderButton(Transform parent, string label, Vector2 pos)
        {
            var go = new GameObject($"Btn_Gender_{label}");
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(160f, 46f);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.55f, 0.55f, 0.55f, 0.6f);

            var btn = go.AddComponent<Button>();

            var txtGO = new GameObject("Label");
            txtGO.transform.SetParent(go.transform, false);
            var txtRT = txtGO.AddComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;
            var tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = 16f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Color.white;

            return btn;
        }

        private void SelectGender(string gender)
        {
            _selectedGender = gender;
            RefreshGenderColors();
        }

        private void RefreshGenderColors()
        {
            SetButtonColor(btnPria,   _selectedGender == "pria");
            SetButtonColor(btnWanita, _selectedGender == "wanita");
        }

        private static void SetButtonColor(Button btn, bool selected)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img) img.color = selected
                ? new Color(0.20f, 0.45f, 0.80f, 1f)
                : new Color(0.55f, 0.55f, 0.55f, 0.6f);
        }

        private void OnMulai()
        {
            string nama = inputField != null ? inputField.text.Trim() : "";
            if (string.IsNullOrEmpty(nama)) nama = defaultName;

            PlayerPrefs.SetString("PlayerName",   nama);
            PlayerPrefs.SetString("PlayerGender", _selectedGender);
            PlayerPrefs.SetInt("tutorial_done", 0);
            // 1. Bersihkan PlayerPrefs langsung (tidak bergantung pada Instance)
            ResetAllProgressDirect();
            // 2. Jika ChapterManager DDOL masih hidup (dari sesi sebelumnya via BackToMenu),
            //    paksa reload in-memory state agar sinkron dengan PlayerPrefs yang sudah bersih
            ChapterManager.Instance?.ResetAll();

            // Pakai SceneTransitionManager jika tersedia agar ada efek fade
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.LoadScene(nextScene);
            else
                SceneManager.LoadScene(nextScene);
        }

        // Reset langsung via PlayerPrefs — tidak bergantung pada ChapterManager.Instance
        // (ChapterManager belum spawn saat scene ini berjalan dari MainMenu)
        private static void ResetAllProgressDirect()
        {
            string[] activities = { "baca", "tulis", "pelafalan", "refleksi", "combine" };
            // ChapterID: Tutorial=0, Swara=1, Ngalagena1=2, Ngalagena2=3, Rarangken=4, UjianFinal=5
            for (int i = 0; i <= 5; i++)
            {
                PlayerPrefs.DeleteKey($"ch_{i}_unlock");
                PlayerPrefs.DeleteKey($"ch_{i}_done");
                PlayerPrefs.DeleteKey($"ch_{i}_stars");
                foreach (var act in activities)
                    PlayerPrefs.DeleteKey($"ch_{i}_{act}_done");
            }
            // Bersihkan juga key sementara mini-game
            PlayerPrefs.DeleteKey("ActiveMode");
            PlayerPrefs.DeleteKey("ActiveNPCTitle");
            PlayerPrefs.DeleteKey("ActiveNPCName");
            PlayerPrefs.DeleteKey("RefleksiSkipReview");
            PlayerPrefs.DeleteKey("mg_pending_feedback");
            PlayerPrefs.Save();
        }
    }
}
