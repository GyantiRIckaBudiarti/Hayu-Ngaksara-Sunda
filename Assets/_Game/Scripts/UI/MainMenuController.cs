using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HayuNgaksara
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button btnMulai;
        [SerializeField] private Button btnLanjutkan;
        [SerializeField] private Button btnPengaturan;
        [SerializeField] private Button btnKeluar;

        [Header("Pengaturan Panel")]
        [SerializeField] private GameObject panelPengaturan;
        [SerializeField] private Slider     sliderMusic;
        [SerializeField] private Slider     sliderSFX;
        [SerializeField] private Button     btnTutupPengaturan;

        [Header("Opening Animation")]
        [SerializeField] private RectTransform judulRect;
        [SerializeField] private CanvasGroup   jajangGroup;
        [SerializeField] private Button[]      tombolUrut;

        private void Awake()
        {
            btnMulai?.onClick.AddListener(() => LoadScene("00a_NamaKarakter"));
            btnLanjutkan?.onClick.AddListener(() => LoadScene("00_Sekolah"));
            // Pengaturan = menu penuh sendiri (SettingsPanel), bukan panel modal kosong di scene
            btnPengaturan?.onClick.AddListener(() => SettingsPanel.Open());
            btnKeluar?.onClick.AddListener(() => Application.Quit());
            btnTutupPengaturan?.onClick.AddListener(() => { if (panelPengaturan) panelPengaturan.SetActive(false); });

            if (sliderMusic != null) sliderMusic.onValueChanged.AddListener(v => AudioManager.Instance?.SetMusicVolume(v));
            if (sliderSFX   != null) sliderSFX.onValueChanged.AddListener(v => AudioManager.Instance?.SetSFXVolume(v));
        }

        private void Start()
        {
            if (panelPengaturan) panelPengaturan.SetActive(false);

            RefreshContinueButton();

            StartCoroutine(OpeningAnimation());
        }

        private IEnumerator OpeningAnimation()
        {
            if (judulRect != null)
            {
                Vector2 endPos   = new Vector2(0f, 175f);
                Vector2 startPos = new Vector2(0f, 500f);
                float   t        = 0f;
                float   dur      = 0.6f;
                judulRect.anchoredPosition = startPos;
                while (t < dur)
                {
                    t += Time.unscaledDeltaTime;
                    float ease = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / dur), 3f);
                    judulRect.anchoredPosition = Vector2.Lerp(startPos, endPos, ease);
                    yield return null;
                }
                judulRect.anchoredPosition = endPos;
            }

            if (jajangGroup != null)
            {
                jajangGroup.alpha = 0f;
                float t = 0f;
                while (t < 0.3f)
                {
                    t += Time.deltaTime;
                    jajangGroup.alpha = t / 0.3f;
                    yield return null;
                }
                jajangGroup.alpha = 1f;
            }

            foreach (var btn in tombolUrut)
            {
                if (btn == null) continue;
                var cg = btn.GetComponent<CanvasGroup>();
                if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                float t = 0f;
                while (t < 0.15f)
                {
                    t += Time.deltaTime;
                    cg.alpha = t / 0.15f;
                    yield return null;
                }
                cg.alpha = 1f;
                yield return new WaitForSecondsRealtime(0.1f);
            }
        }

        // Tampilkan "Lanjutkan" hanya bila ada save (nama terisi & tutorial selesai).
        // Public agar bisa dipanggil SettingsPanel setelah reset simpanan.
        public void RefreshContinueButton()
        {
            bool hasSave = !string.IsNullOrEmpty(PlayerPrefs.GetString("PlayerName", ""))
                           && PlayerPrefs.GetInt("tutorial_done", 0) == 1;
            if (btnLanjutkan != null) btnLanjutkan.gameObject.SetActive(hasSave);
        }

        private void LoadScene(string sceneName)
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.LoadScene(sceneName);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }
}
