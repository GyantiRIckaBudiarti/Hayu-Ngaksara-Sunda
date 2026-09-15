using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HayuNgaksara
{
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        [SerializeField] private List<DialogLine> tutorialLines;
        [SerializeField] private GameObject       arrowIndicator;

        private bool _done;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            bool tutorialDone = PlayerPrefs.GetInt("tutorial_done", 0) == 1;
            // Jika OpeningCutscene ada, ia yang akan memanggil StartTutorial() setelah cutscene selesai
            if (!tutorialDone && OpeningCutscene.Instance == null)
                StartCoroutine(DelayedStart());
        }

        private IEnumerator DelayedStart()
        {
            yield return new WaitForSecondsRealtime(0.8f);
            StartTutorial();
        }

        public void StartTutorial()
        {
            if (_done) return;

            var lines = tutorialLines.Count > 0 ? tutorialLines : DefaultLines();
            DialogSystem.Instance?.Show(lines, OnTutorialDone);
        }

        private void OnTutorialDone()
        {
            _done = true;
            PlayerPrefs.SetInt("tutorial_done", 1);
            PlayerPrefs.Save();
            ChapterManager.Instance?.CompleteChapter(ChapterID.Tutorial, 3);

            // Kecil reaksi: Jajang kasih "toss"
            FloatingText.Spawn("Ayo jelajahi sekolah!",
                PlayerTopDown.Instance?.transform.position ?? Vector3.zero,
                new Color(0.2f, 0.8f, 0.3f));
        }

        private List<DialogLine> DefaultLines()
        {
            string playerName = PlayerTopDown.Instance?.PlayerName ?? "kamu";
            return new List<DialogLine>
            {
                new DialogLine { speakerName = "Bapak Jajang",
                    text = UITheme.Bi("Halo! Nami abdi Jajang. Wilujeng sumping di Sakola Aksara Sunda!",
                                      "Halo! Nama saya Jajang. Selamat datang di Sekolah Aksara Sunda!") },
                new DialogLine { speakerName = "Bapak Jajang",
                    text = UITheme.Bi("Apal teu, Aksara Sunda téh warisan budaya karuhun urang nu geus aya ti abad ka-14?",
                                      "Tahukah kamu, Aksara Sunda adalah warisan budaya leluhur kita yang sudah ada sejak abad ke-14?") },
                new DialogLine { speakerName = "Bapak Jajang",
                    text = UITheme.Bi("Baheula, karuhun urang nyeratkeun carita, sair, jeung pesen penting maké aksara ieu.",
                                      "Dulu, nenek moyang kita menuliskan cerita, syair, dan pesan penting menggunakan aksara ini.") },
                new DialogLine { speakerName = "Bapak Jajang",
                    text = UITheme.Bi("Hanjakalna, ayeuna loba nu can wanoh kana aksara éndah ieu. Éta sababna urang aya di dieu!",
                                      "Sayangnya, sekarang banyak yang belum mengenal aksara indah ini. Itulah mengapa kita ada di sini!") },
                new DialogLine { speakerName = "Bapak Jajang",
                    text = UITheme.Bi($"Anjeun tangtu {playerName}, sanés? Bungah tepang! Abdi bakal ngabantu ngamimitian lalampahan anjeun.",
                                      $"Kamu pasti {playerName}, kan? Senang berkenalan! Aku akan membantumu memulai perjalananmu.") },
                new DialogLine { speakerName = "Bapak Jajang",
                    text = UITheme.Bi($"Nah {playerName}, di sakola ieu aya sababaraha réncang nu tiasa ngabantu anjeun diajar:",
                                      $"Nah {playerName}, di sekolah ini ada beberapa teman yang bisa membantumu belajar:") },
                new DialogLine { speakerName = "Bapak Jajang",
                    text = UITheme.Bi("Panggih Ibu SINTA — anjeunna bakal ngajar cara MACA Aksara Sunda kalayan bener.",
                                      "Temui Ibu SINTA — dia akan mengajarimu cara MEMBACA Aksara Sunda dengan baik dan benar.") },
                new DialogLine { speakerName = "Bapak Jajang",
                    text = UITheme.Bi("Panggih Bapak UCUP — ahli NULIS aksara, bakal ngalatih leungeun anjeun nulis unggal aksara.",
                                      "Temui Bapak UCUP — dia ahli MENULIS aksara, dia akan melatih tanganmu menulis setiap aksara.") },
                new DialogLine { speakerName = "Bapak Jajang",
                    text = UITheme.Bi("Panggih Ibu NABILA — bakal ngabantu latihan NGUCAPKEUN sangkan lafal anjeun pas.",
                                      "Temui Ibu NABILA — dia akan membantumu melatih PELAFALAN agar pengucapanmu tepat.") },
                new DialogLine { speakerName = "Bapak Jajang",
                    text = UITheme.Bi("Sanggeus diajar ti aranjeunna, panggih BU GURU pikeun RÉFLÉKSI — uji sabaraha jauh kamajuan anjeun!",
                                      "Setelah belajar dari mereka, temui BU GURU untuk REFLEKSI — uji seberapa jauh kemajuanmu!") },
                new DialogLine { speakerName = "Bapak Jajang",
                    text = UITheme.Bi("Hayu urang mimitian lalampahan diajar anjeun! Hayu ngaksara Sunda!",
                                      "Ayo mulai petualangan belajarmu! Hayu ngaksara Sunda!"),
                    autoAdvanceAfter = 0f },
            };
        }
    }
}
