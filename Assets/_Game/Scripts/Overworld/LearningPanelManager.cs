using UnityEngine;

namespace HayuNgaksara
{
    // Jembatan antara overworld dan mini-game scenes
    // Saat chapter dipanggil → load scene mini-game yg sesuai
    public class LearningPanelManager : MonoBehaviour
    {
        public static LearningPanelManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void OpenChapterWithMode(ChapterID id, MiniGameMode mode)
        {
            PlayerPrefs.SetInt("ActiveMode", (int)mode);
            PlayerPrefs.Save();
            OpenChapter(id);
        }

        public void OpenChapter(ChapterID id)
        {
            switch (id)
            {
                case ChapterID.Tutorial:    StartTutorial();                break;
                case ChapterID.Swara:       LoadMiniGame("MG_Swara");       break;
                case ChapterID.Ngalagena1:  LoadMiniGame("MG_Ngalagena1");  break;
                case ChapterID.Ngalagena2:  LoadMiniGame("MG_Ngalagena2");  break;
                case ChapterID.Rarangken:   LoadMiniGame("MG_Rarangken");   break;
                case ChapterID.UjianFinal:  LoadMiniGame("MG_UjianFinal");  break;
            }
        }

        private void StartTutorial()
        {
            // Tutorial inline di overworld — gerak, interact, dst
            TutorialManager.Instance?.StartTutorial();
        }

        private void LoadMiniGame(string sceneName)
        {
            // Simpan chapter yang sedang dikerjakan
            PlayerPrefs.SetString("ActiveChapter", sceneName);
            PlayerPrefs.Save();
            SceneTransitionManager.Instance?.LoadScene(sceneName);
        }
    }
}
