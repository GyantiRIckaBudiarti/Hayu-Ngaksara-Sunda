using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace HayuNgaksara
{
    public class LevelSummaryController : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TextMeshProUGUI txtLevelTitle;
        [SerializeField] private TextMeshProUGUI txtLevelSubtitle;

        [Header("Card — Belajar (Sinta)")]
        [SerializeField] private TextMeshProUGUI txtBacaStars;
        [SerializeField] private TextMeshProUGUI txtBacaScore;
        [SerializeField] private Image           imgBacaBg;

        [Header("Card — Menulis (Ucup)")]
        [SerializeField] private TextMeshProUGUI txtTulisStars;
        [SerializeField] private TextMeshProUGUI txtTulisScore;
        [SerializeField] private Image           imgTulisBg;

        [Header("Card — Pelafalan (Nabila)")]
        [SerializeField] private TextMeshProUGUI txtPelafalanStars;
        [SerializeField] private TextMeshProUGUI txtPelafalanScore;
        [SerializeField] private Image           imgPelafalanBg;

        [Header("Card — Refleksi (Guru)")]
        [SerializeField] private TextMeshProUGUI txtRefleksiStars;
        [SerializeField] private TextMeshProUGUI txtRefleksiScore;
        [SerializeField] private Image           imgRefleksiBg;

        [Header("Overall Grade")]
        [SerializeField] private TextMeshProUGUI txtGrade;
        [SerializeField] private TextMeshProUGUI txtGradeDesc;
        [SerializeField] private TextMeshProUGUI txtTotalStars;

        [Header("Buttons")]
        [SerializeField] private Button          btnLanjut;
        [SerializeField] private Button          btnKembali;
        [SerializeField] private TextMeshProUGUI txtBtnLanjut;

        // ── colours ──────────────────────────────────────────────────────────
        private static readonly Color C3Star = new Color(1f,   0.85f, 0.1f);   // gold
        private static readonly Color C2Star = new Color(0.4f, 0.85f, 0.4f);   // green
        private static readonly Color C1Star = new Color(0.4f, 0.7f,  1f);     // blue
        private static readonly Color C0Star = new Color(0.55f,0.55f, 0.55f);  // grey
        private static readonly Color CGradeA = new Color(1f,   0.84f, 0f);
        private static readonly Color CGradeB = new Color(0.2f, 0.9f,  0.2f);
        private static readonly Color CGradeC = new Color(0.2f, 0.8f,  1f);
        private static readonly Color CGradeD = new Color(1f,   1f,    1f);
        private static readonly Color CGradeE = new Color(1f,   0.3f,  0.3f);

        private ChapterID _chapterID;

        private void Start()
        {
            _chapterID = (ChapterID)PlayerPrefs.GetInt("SummaryChapterID", 1);

            btnLanjut?.onClick.AddListener(OnLanjut);
            btnKembali?.onClick.AddListener(OnKembali);

            Refresh();
        }

        private void Refresh()
        {
            // ── chapter info ─────────────────────────────────────────────────
            string levelName = ChapterName(_chapterID);
            if (txtLevelTitle)    txtLevelTitle.text    = $"Level {(int)_chapterID} Selesai!";
            if (txtLevelSubtitle) txtLevelSubtitle.text = levelName;

            // ── per-activity data ─────────────────────────────────────────────
            int idInt = (int)_chapterID;
            int bacaSt  = PlayerPrefs.GetInt($"ch_{idInt}_baca_stars",      0);
            int tulisSt = PlayerPrefs.GetInt($"ch_{idInt}_tulis_stars",     0);
            int pelSt   = PlayerPrefs.GetInt($"ch_{idInt}_pelafalan_stars", 0);
            int refSt   = PlayerPrefs.GetInt($"ch_{idInt}_refleksi_stars",  0);

            int bacaS  = PlayerPrefs.GetInt($"ch_{idInt}_baca_score",      0);
            int bacaT  = PlayerPrefs.GetInt($"ch_{idInt}_baca_total",      0);
            int tulisS = PlayerPrefs.GetInt($"ch_{idInt}_tulis_score",     0);
            int tulisT = PlayerPrefs.GetInt($"ch_{idInt}_tulis_total",     0);
            int pelS   = PlayerPrefs.GetInt($"ch_{idInt}_pelafalan_score", 0);
            int pelT   = PlayerPrefs.GetInt($"ch_{idInt}_pelafalan_total", 0);
            int refS   = PlayerPrefs.GetInt($"ch_{idInt}_refleksi_score",  0);
            int refT   = PlayerPrefs.GetInt($"ch_{idInt}_refleksi_total",  0);

            SetCard(txtBacaStars,      txtBacaScore,      imgBacaBg,      bacaSt,  bacaS,  bacaT);
            SetCard(txtTulisStars,     txtTulisScore,     imgTulisBg,     tulisSt, tulisS, tulisT);
            SetCard(txtPelafalanStars, txtPelafalanScore, imgPelafalanBg, pelSt,   pelS,   pelT);
            SetCard(txtRefleksiStars,  txtRefleksiScore,  imgRefleksiBg,  refSt,   refS,   refT);

            // ── overall grade (persentase) ────────────────────────────────────
            int total = bacaSt + tulisSt + pelSt + refSt;

            // Persentase dari skor benar seluruh aktivitas; fallback ke bintang (dari 12)
            int scoreSum = bacaS + tulisS + pelS + refS;
            int totalSum = bacaT + tulisT + pelT + refT;
            int pct = totalSum > 0
                ? Mathf.RoundToInt(100f * scoreSum / totalSum)
                : Mathf.RoundToInt(100f * total / 12f);
            pct = Mathf.Clamp(pct, 0, 100);

            if (txtGrade)      { txtGrade.text = $"{pct}%";       txtGrade.color = PctColor(pct); }
            if (txtGradeDesc)  txtGradeDesc.text  = PctDesc(pct);
            if (txtTotalStars) txtTotalStars.text = $"Total: {total} / 12 Bintang";

            // ── lanjut button ─────────────────────────────────────────────────
            // Ujian Akhir DIHAPUS → Rarangken level terakhir. Tak ada "lanjut" setelahnya.
            ChapterID next = _chapterID + 1;
            bool hasNext = System.Enum.IsDefined(typeof(ChapterID), (int)next)
                           && (int)next <= (int)ChapterID.Rarangken;
            if (txtBtnLanjut && hasNext)
                txtBtnLanjut.text = $"Lanjut ke {ChapterName(next)} →";
            if (btnLanjut) btnLanjut.gameObject.SetActive(hasNext);

            // Level terakhir (Rarangken) selesai → tampilkan status TAMAT
            if (!hasNext)
            {
                if (txtLevelTitle)    txtLevelTitle.text    = "Selamat! Semua Level Selesai! 🎉";
                if (txtLevelSubtitle) txtLevelSubtitle.text = "Kamu telah menguasai Aksara Sunda!";
            }
        }

        private void SetCard(TextMeshProUGUI starsTxt, TextMeshProUGUI scoreTxt,
                             Image bg, int stars, int score, int total)
        {
            if (starsTxt) { starsTxt.text = StarsStr(stars); starsTxt.color = StarColor(stars); }
            if (scoreTxt) scoreTxt.text = total > 0 ? $"{score} / {total} benar" : "Selesai ✓";
            if (bg) bg.color = StarBgColor(stars);
        }

        // ── helpers ───────────────────────────────────────────────────────────

        private static string StarsStr(int s) => s switch
        {
            3 => "★★★",
            2 => "★★☆",
            1 => "★☆☆",
            _ => "☆☆☆"
        };

        private static Color StarColor(int s) => s switch
        {
            3 => C3Star,
            2 => C2Star,
            1 => C1Star,
            _ => C0Star
        };

        private static Color StarBgColor(int s) => s switch
        {
            3 => new Color(0.2f, 0.18f, 0.05f, 0.9f),
            2 => new Color(0.05f, 0.2f, 0.05f, 0.9f),
            1 => new Color(0.05f, 0.12f, 0.25f, 0.9f),
            _ => new Color(0.1f,  0.1f,  0.1f,  0.9f)
        };

        private static char CalcGrade(int total) => total switch
        {
            >= 11 => 'A',
            >= 9  => 'B',
            >= 7  => 'C',
            >= 5  => 'D',
            _     => 'E'
        };

        private static Color GradeColor(char g) => g switch
        {
            'A' => CGradeA,
            'B' => CGradeB,
            'C' => CGradeC,
            'D' => CGradeD,
            _   => CGradeE
        };

        private static string GradeDesc(char g) => g switch
        {
            'A' => "Luar Biasa! Kamu menguasai semua aksara Sunda!",
            'B' => "Bagus! Terus semangat belajar aksara Sunda!",
            'C' => "Cukup baik! Latihan lebih sering ya!",
            'D' => "Perlu lebih banyak latihan, semangat!",
            _   => "Jangan menyerah, terus berlatih!"
        };

        // ── persentase (pengganti grade A–E) ──────────────────────────────────
        private static Color PctColor(int pct) => pct switch
        {
            >= 90 => CGradeA,
            >= 75 => CGradeB,
            >= 60 => CGradeC,
            >= 40 => CGradeD,
            _     => CGradeE
        };

        private static string PctDesc(int pct) => pct switch
        {
            >= 90 => "Luar Biasa! Kamu menguasai semua aksara Sunda!",
            >= 75 => "Bagus! Terus semangat belajar aksara Sunda!",
            >= 60 => "Cukup baik! Latihan lebih sering ya!",
            >= 40 => "Perlu lebih banyak latihan, semangat!",
            _     => "Jangan menyerah, terus berlatih!"
        };

        private static string ChapterName(ChapterID id) => id switch
        {
            ChapterID.Swara      => "Aksara Swara",
            ChapterID.Ngalagena1 => "Ngalagena I",
            ChapterID.Ngalagena2 => "Ngalagena II",
            ChapterID.Rarangken  => "Rarangken",
            ChapterID.UjianFinal => "Ujian Akhir",
            _                    => "Tutorial"
        };

        // ── button handlers ───────────────────────────────────────────────────

        private void OnLanjut()
        {
            ChapterID next = _chapterID + 1;
            PlayerPrefs.SetInt($"ch_{(int)next}_unlock", 1);
            PlayerPrefs.SetInt("SummaryChapterID", (int)next);
            PlayerPrefs.Save();
            SceneManager.LoadScene("00_Sekolah");
        }

        private void OnKembali()
        {
            SceneManager.LoadScene("00_Sekolah");
        }
    }
}
