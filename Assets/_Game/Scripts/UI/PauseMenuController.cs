using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace HayuNgaksara
{
    public class PauseMenuController : MonoBehaviour
    {
        public static PauseMenuController Instance { get; private set; }

        [Header("Panel")]
        [SerializeField] private GameObject panelPause;

        [Header("Buttons")]
        [SerializeField] private Button btnResume;
        [SerializeField] private Button btnRestart;
        [SerializeField] private Button btnBackToMenu;
        [SerializeField] private Button btnQuit;
        [SerializeField] private Button btnWritingProgress;
        [SerializeField] private Button btnSettings;

        public bool IsPaused { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            btnResume.onClick.AddListener(Resume);
            btnRestart.onClick.AddListener(Restart);
            btnBackToMenu.onClick.AddListener(BackToMenu);
            btnQuit.onClick.AddListener(Quit);
            btnWritingProgress?.onClick.AddListener(() =>
            {
                Resume(); // unpause dulu agar panel bisa interaktif
                WritingProgressPanel.Instance?.Show();
            });
            btnSettings?.onClick.AddListener(() => SettingsPanel.Open());
        }

        private void Start()
        {
            panelPause.SetActive(false);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Toggle();
        }

        public void Toggle()
        {
            if (IsPaused) Resume();
            else          Pause();
        }

        public void Pause()
        {
            IsPaused = true;
            Time.timeScale = 0f;
            panelPause.SetActive(true);
            if (PlayerTopDown.Instance != null) PlayerTopDown.Instance.CanMove = false;
        }

        public void Resume()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            panelPause.SetActive(false);
            if (PlayerTopDown.Instance != null) PlayerTopDown.Instance.CanMove = true;
        }

        private void Restart()
        {
            Time.timeScale = 1f;
            IsPaused = false;
            SceneTransitionManager.Instance?.LoadScene("00_Sekolah");
        }

        private void BackToMenu()
        {
            Time.timeScale = 1f;
            IsPaused = false;
            SceneTransitionManager.Instance?.LoadScene("01_MainMenu");
        }

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
