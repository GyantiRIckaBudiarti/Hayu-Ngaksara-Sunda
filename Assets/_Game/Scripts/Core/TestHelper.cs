#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HayuNgaksara
{
    public static class TestHelper
    {
        private const string MenuRoot = "Tools/Hayu Ngaksara/";

        [MenuItem(MenuRoot + "Skip to Level 1")]
        public static void SkipToLevel1()
        {
            EditorApplication.isPlaying = true;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode)
                    SceneManager.LoadScene("08_Level1_Game");
            };
        }

        [MenuItem(MenuRoot + "Skip to Level 2")]
        public static void SkipToLevel2()
        {
            EditorApplication.isPlaying = true;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode)
                    SceneManager.LoadScene("10_Level2_Game");
            };
        }

        [MenuItem(MenuRoot + "Skip to Level 3")]
        public static void SkipToLevel3()
        {
            EditorApplication.isPlaying = true;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode)
                    SceneManager.LoadScene("11_Level3_Game");
            };
        }

        [MenuItem(MenuRoot + "Skip to Kuis")]
        public static void SkipToKuis()
        {
            EditorApplication.isPlaying = true;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode)
                    SceneManager.LoadScene("06_Kuis");
            };
        }

        [MenuItem(MenuRoot + "Reset All Progress")]
        public static void ResetAllProgress()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[TestHelper] Semua progress di-reset.");
        }

        [MenuItem(MenuRoot + "Set Max Score (Bintang 3 Semua)")]
        public static void SetMaxScore()
        {
            string[] levels = { "level1", "level2", "level3" };
            foreach (var lv in levels)
            {
                PlayerPrefs.SetInt(lv + "_bintang", 3);
                PlayerPrefs.SetInt(lv + "_complete", 1);
            }
            PlayerPrefs.SetInt("HighScore", 100);
            PlayerPrefs.Save();
            Debug.Log("[TestHelper] Semua level di-set bintang 3.");
        }
    }

    // Debug overlay aktif saat play mode (F1 toggle)
    [InitializeOnLoad]
    public static class DebugOverlay
    {
        static DebugOverlay()
        {
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            // Overlay hanya di play mode, via OnGUI di MonoBehaviour
        }
    }

    // Runtime debug overlay (F1 toggle)
    public class RuntimeDebugOverlay : MonoBehaviour
    {
        private bool _visible;

        private void Update()
        {
            // Input System baru (project ini activeInputHandler = New) — Input lama melempar exception.
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.f1Key.wasPressedThisFrame)
                _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible) return;
            GUI.Box(new Rect(10, 10, 300, 120), "Debug Info");
            int y = 35;
            if (GameManager.Instance != null)
            {
                GUI.Label(new Rect(20, y, 280, 20), $"State : {GameManager.Instance.CurrentState}"); y += 20;
                GUI.Label(new Rect(20, y, 280, 20), $"Score : {GameManager.Instance.CurrentScore}"); y += 20;
                GUI.Label(new Rect(20, y, 280, 20), $"Lives : {GameManager.Instance.CurrentLives}"); y += 20;
            }
            GUI.Label(new Rect(20, y, 280, 20), $"Scene : {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        }
    }
}
#endif
