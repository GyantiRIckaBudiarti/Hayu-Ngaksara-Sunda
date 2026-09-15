using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HayuNgaksara
{
    // Diletakkan di 00_Sekolah.
    // Mengecek PlayerPrefs apakah ada feedback pending setelah mini-game,
    // lalu menampilkan dialog dari Jajang berdasarkan skor.
    public class ReturnFeedback : MonoBehaviour
    {
        [SerializeField] private Sprite jajangPortrait;

        private static readonly string KEY_PENDING  = "mg_pending_feedback";
        private static readonly string KEY_STARS    = "mg_last_stars";
        private static readonly string KEY_SCORE    = "mg_last_score";
        private static readonly string KEY_TOTAL    = "mg_last_total";
        private static readonly string KEY_CHAPTER  = "mg_last_chapter";
        private static readonly string KEY_EXITED   = "mg_exited_early";

        private IEnumerator Start()
        {
            if (PlayerPrefs.GetInt(KEY_PENDING, 0) != 1) yield break;

            // Tunggu DialogSystem dan PlayerTopDown siap
            yield return new WaitForSecondsRealtime(0.8f);
            while (DialogSystem.Instance == null || PlayerTopDown.Instance == null)
                yield return new WaitForSecondsRealtime(0.2f);

            int  stars      = PlayerPrefs.GetInt(KEY_STARS,   0);
            int  score      = PlayerPrefs.GetInt(KEY_SCORE,   0);
            int  total      = PlayerPrefs.GetInt(KEY_TOTAL,   0);
            bool exitedEarly = PlayerPrefs.GetInt(KEY_EXITED, 0) == 1;
            string chapter  = PlayerPrefs.GetString(KEY_CHAPTER, "");

            // Hapus flag agar tidak muncul lagi
            PlayerPrefs.DeleteKey(KEY_PENDING);
            PlayerPrefs.DeleteKey(KEY_STARS);
            PlayerPrefs.DeleteKey(KEY_SCORE);
            PlayerPrefs.DeleteKey(KEY_TOTAL);
            PlayerPrefs.DeleteKey(KEY_EXITED);
            PlayerPrefs.Save();

            var lines = BuildFeedbackLines(stars, score, total, exitedEarly, chapter);
            DialogSystem.Instance.Show(lines);
        }

        private List<DialogLine> BuildFeedbackLines(int stars, int score, int total,
                                                     bool exitedEarly, string chapter)
        {
            var lines = new List<DialogLine>();

            if (exitedEarly)
            {
                lines.Add(Line("Éh, geus balik? Teu nanaon, reureuh heula. Lamun geus siap, ajak abdi deui nya — abdi pasti nemenan anjeun diajar!",
                               "Eh, sudah balik? Tidak apa-apa, istirahat dulu. Kalau sudah siap, ajak aku lagi ya — aku pasti menemani kamu belajar!"));
                return lines;
            }

            string chapterLabel = chapter switch {
                "Swara"      => "Aksara Swara",
                "Ngalagena1" => "Aksara Ngalagena (bagian 1)",
                "Ngalagena2" => "Aksara Ngalagena (bagian 2)",
                "Rarangken"  => "Rarangken",
                "UjianFinal" => "Ujian Final",
                _            => "materi tadi"
            };

            if (total == 0)
            {
                lines.Add(Line($"Héy, kumaha {chapterLabel}-na? Abdi ngadagoan carita anjeun!",
                               $"Hei, bagaimana {chapterLabel}-nya? Aku tunggu ceritamu!"));
                return lines;
            }

            string scoreText = total > 0 ? $"{score}/{total}" : "";

            switch (stars)
            {
                case 3:
                    lines.Add(Line($"Wah, hébat pisan! Anjeun meunang peunteun {scoreText} pikeun {chapterLabel}! Anjeun bener-bener boga bakat! Terus sumanget nya!",
                                   $"Wah, luar biasa! Kamu dapat nilai {scoreText} untuk {chapterLabel}! Kamu benar-benar berbakat! Terus semangat ya!"));
                    lines.Add(Line("Abdi reueus ka anjeun. Hayu teraskeun ka palajaran satuluyna lamun geus siap!",
                                   "Aku bangga sama kamu. Yuk lanjut ke pelajaran berikutnya kalau sudah siap!"));
                    break;

                case 2:
                    lines.Add(Line($"Alus! Anjeun meunang peunteun {scoreText} pikeun {chapterLabel}. Geus ampir sampurna euy!",
                                   $"Bagus! Kamu dapat nilai {scoreText} untuk {chapterLabel}. Sudah hampir sempurna nih!"));
                    lines.Add(Line("Latihan saeutik deui pasti langsung mahér. Abdi percaya anjeun tiasa!",
                                   "Latihan sedikit lagi pasti langsung mahir. Aku percaya kamu bisa!"));
                    break;

                case 1:
                    lines.Add(Line($"Peunteun {scoreText} pikeun {chapterLabel}... Aya kamajuan! Ulah nyerah nya, cobian sakali deui.",
                                   $"Nilai {scoreText} untuk {chapterLabel}... Ada kemajuan! Jangan menyerah ya, coba sekali lagi."));
                    lines.Add(Line("Abdi tiasa ngabantu review deui lamun hoyong. Ajak abdi iraha waé!",
                                   "Aku bisa bantu kamu review lagi kalau mau. Ajak aku kapanpun!"));
                    break;

                default:
                    lines.Add(Line($"Hmm, peunteun {scoreText} pikeun {chapterLabel}. Rada hésé nya materina?",
                                   $"Hmm, nilai {scoreText} untuk {chapterLabel}. Agak susah ya materinya?"));
                    lines.Add(Line("Teu nanaon, diajar téh butuh prosés. Hayu cobian deui ti awal bareng abdi!",
                                   "Tidak apa-apa, belajar itu butuh proses. Yuk coba lagi dari awal bareng aku!"));
                    break;
            }

            return lines;
        }

        private DialogLine Line(string sunda, string indo) =>
            new DialogLine { speakerName = "Bapak Jajang", text = UITheme.Bi(sunda, indo), portrait = jajangPortrait };
    }
}
