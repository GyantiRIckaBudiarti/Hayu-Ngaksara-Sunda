using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HayuNgaksara
{
    public enum KategoriSoal { Membaca, Menulis, Pelafalan }

    [System.Serializable]
    public class KuisQuestion
    {
        public string       pertanyaan;
        public Sprite       gambarSoal;
        public List<string> pilihan = new List<string>();
        public int          indexJawaban;
        public KategoriSoal kategori;
    }

    public class KuisManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI  textPertanyaan;
        [SerializeField] private Image            imageSoal;
        [SerializeField] private Button[]         tombolPilihan;
        [SerializeField] private TextMeshProUGUI[] labelPilihan;
        [SerializeField] private Image[]          imagePilihan;
        [SerializeField] private TextMeshProUGUI  hudSoal;
        [SerializeField] private Image            timerBar;
        [SerializeField] private GameObject       panelResult;
        [SerializeField] private KuisResultPanel  resultPanel;
        [SerializeField] private Button           btnUlangi;
        [SerializeField] private Button           btnMenu;

        [Header("Settings")]
        [SerializeField] private int   jumlahSoal   = 10;
        [SerializeField] private float timerPerSoal = 20f;

        [Header("Audio")]
        [SerializeField] private AudioClip sfxBenar;
        [SerializeField] private AudioClip sfxSalah;

        [SerializeField] private List<KuisQuestion> bankSoal;

        private List<KuisQuestion> _soalAktif;
        private int  _soalIndex;
        private int  _skor;
        private int  _benarCount;
        private bool _locked;
        private Coroutine _timerCo;

        private void Start()
        {
            for (int i = 0; i < tombolPilihan.Length; i++)
            {
                int idx = i;
                tombolPilihan[i].onClick.AddListener(() => Jawab(idx));
            }
            btnUlangi.onClick.AddListener(Restart);
            btnMenu.onClick.AddListener(() => SceneTransitionManager.Instance?.LoadScene("01_MainMenu"));

            GenerateSoal();
            StartCoroutine(ShowSoal());
        }

        private void GenerateSoal()
        {
            var pool = new List<KuisQuestion>(bankSoal);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
            _soalAktif = pool.GetRange(0, Mathf.Min(jumlahSoal, pool.Count));
        }

        private IEnumerator ShowSoal()
        {
            if (_soalIndex >= _soalAktif.Count)
            {
                ShowResult();
                yield break;
            }

            _locked = false;
            var soal = _soalAktif[_soalIndex];
            hudSoal.text = $"Soal {_soalIndex + 1}/{_soalAktif.Count}";
            textPertanyaan.text = soal.pertanyaan;

            bool hasImage = soal.gambarSoal != null;
            imageSoal.gameObject.SetActive(hasImage);
            if (hasImage) imageSoal.sprite = soal.gambarSoal;

            for (int i = 0; i < tombolPilihan.Length && i < soal.pilihan.Count; i++)
            {
                if (labelPilihan != null && i < labelPilihan.Length)
                    labelPilihan[i].text = soal.pilihan[i];
            }
            ResetWarna();

            if (_timerCo != null) StopCoroutine(_timerCo);
            _timerCo = StartCoroutine(TimerLoop());
        }

        private IEnumerator TimerLoop()
        {
            float t = timerPerSoal;
            while (t > 0 && !_locked)
            {
                t -= Time.deltaTime;
                if (timerBar != null) timerBar.fillAmount = t / timerPerSoal;
                yield return null;
            }
            if (!_locked) Timeout();
        }

        private void Jawab(int idx)
        {
            if (_locked) return;
            _locked = true;
            if (_timerCo != null) StopCoroutine(_timerCo);

            bool benar = idx == _soalAktif[_soalIndex].indexJawaban;
            if (benar)
            {
                _skor += 10;
                _benarCount++;
                AudioManager.Instance?.PlaySFX(sfxBenar);
                if (imagePilihan != null && idx < imagePilihan.Length)
                    imagePilihan[idx].color = Color.green;
            }
            else
            {
                AudioManager.Instance?.PlaySFX(sfxSalah);
                if (imagePilihan != null && idx < imagePilihan.Length)
                    imagePilihan[idx].color = Color.red;
                // Tunjukkan jawaban benar
                int correct = _soalAktif[_soalIndex].indexJawaban;
                if (imagePilihan != null && correct < imagePilihan.Length)
                    imagePilihan[correct].color = Color.green;
            }

            StartCoroutine(NextAfterDelay(benar ? 1f : 2f));
        }

        private void Timeout()
        {
            _locked = true;
            StartCoroutine(NextAfterDelay(1f));
        }

        private IEnumerator NextAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ResetWarna();
            _soalIndex++;
            yield return StartCoroutine(ShowSoal());
        }

        private void ShowResult()
        {
            int bintang = _skor >= 90 ? 3 : _skor >= 70 ? 2 : 1;
            GameManager.Instance?.SaveHighScore(_skor);
            panelResult.SetActive(true);
            resultPanel.Show(_skor, _benarCount, _soalAktif.Count, bintang);
        }

        private void Restart()
        {
            _soalIndex = 0; _skor = 0; _benarCount = 0;
            panelResult.SetActive(false);
            GenerateSoal();
            StartCoroutine(ShowSoal());
        }

        private void ResetWarna()
        {
            if (imagePilihan == null) return;
            foreach (var img in imagePilihan)
                if (img != null) img.color = Color.white;
        }
    }
}
