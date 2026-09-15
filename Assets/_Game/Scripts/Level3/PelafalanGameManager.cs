using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HayuNgaksara
{
    public enum QuestionMode { AudioToSprite, SpriteToText }

    [System.Serializable]
    public struct QuestionData
    {
        public AksaraData       aksaraJawaban;
        public List<AksaraData> distractors;
        public QuestionMode     mode;
    }

    public class PelafalanGameManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GuruController guru;

        [Header("UI")]
        [SerializeField] private Image           gambarSoal;
        [SerializeField] private TextMeshProUGUI textSoal;
        [SerializeField] private Button[]        tombolJawaban;
        [SerializeField] private TextMeshProUGUI[] labelJawaban;
        [SerializeField] private Image[]         imageJawaban;
        [SerializeField] private Button          btnReplay;
        [SerializeField] private Image           timerBar;
        [SerializeField] private TextMeshProUGUI hudSoal;
        [SerializeField] private GameObject      panelResult;
        [SerializeField] private TextMeshProUGUI textResult;
        [SerializeField] private Button          tombolKembali;

        [Header("Soal")]
        [SerializeField] private AksaraDatabase aksaraDatabase;
        [SerializeField] private int            jumlahSoal   = 10;
        [SerializeField] private float          timerPerSoal = 15f;

        [Header("Audio")]
        [SerializeField] private AudioClip sfxBenar;
        [SerializeField] private AudioClip sfxSalah;

        private List<QuestionData> _soalList;
        private int                _soalIndex;
        private int                _benarCount;
        private int                _replayCount;
        private float              _timer;
        private bool               _answerLocked;
        private Coroutine          _timerCoroutine;

        private void Start()
        {
            for (int i = 0; i < tombolJawaban.Length; i++)
            {
                int idx = i;
                tombolJawaban[i].onClick.AddListener(() => OnJawab(idx));
            }
            btnReplay.onClick.AddListener(ReplayAudio);
            tombolKembali.onClick.AddListener(() =>
                SceneTransitionManager.Instance?.LoadScene("01_MainMenu"));

            GenerateSoal();
            StartCoroutine(ShowSoal());
        }

        private void GenerateSoal()
        {
            _soalList = new List<QuestionData>();
            var pool = aksaraDatabase.GetRandom(jumlahSoal);

            for (int i = 0; i < pool.Count; i++)
            {
                var distractors = GetDistractors(pool[i], 3);
                _soalList.Add(new QuestionData
                {
                    aksaraJawaban = pool[i],
                    distractors   = distractors,
                    mode          = (i % 2 == 0) ? QuestionMode.AudioToSprite : QuestionMode.SpriteToText
                });
            }
        }

        private List<AksaraData> GetDistractors(AksaraData jawaban, int count)
        {
            var all    = aksaraDatabase.GetRandom(count + 5);
            var result = new List<AksaraData>();
            foreach (var a in all)
            {
                if (a.namaHuruf != jawaban.namaHuruf && result.Count < count)
                    result.Add(a);
            }
            return result;
        }

        private IEnumerator ShowSoal()
        {
            if (_soalIndex >= _soalList.Count)
            {
                ShowResult();
                yield break;
            }

            var soal = _soalList[_soalIndex];
            _answerLocked = false;
            _replayCount  = 0;

            hudSoal.text = $"Soal {_soalIndex + 1}/{_soalList.Count}";

            // Setup mode
            gambarSoal.gameObject.SetActive(soal.mode == QuestionMode.AudioToSprite);
            textSoal.gameObject.SetActive(soal.mode == QuestionMode.SpriteToText);

            if (soal.mode == QuestionMode.AudioToSprite)
            {
                textSoal.text = "";
                AudioManager.Instance?.PlayPelafalan(soal.aksaraJawaban.audioPelafalan);
                SetupTombolSprite(soal);
            }
            else
            {
                if (gambarSoal.sprite != null) {}
                gambarSoal.sprite = soal.aksaraJawaban.spriteHuruf;
                gambarSoal.gameObject.SetActive(true);
                SetupTombolTeks(soal);
            }

            guru?.GiveInstruction("Sekarang coba baca aksara ini!");
            ResetTombolWarna();

            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            _timerCoroutine = StartCoroutine(TimerCountdown());
        }

        private void SetupTombolSprite(QuestionData soal)
        {
            var opsi = MixOptions(soal.aksaraJawaban, soal.distractors);
            for (int i = 0; i < tombolJawaban.Length && i < opsi.Count; i++)
            {
                if (imageJawaban != null && i < imageJawaban.Length && opsi[i].spriteHuruf != null)
                    imageJawaban[i].sprite = opsi[i].spriteHuruf;
                if (labelJawaban != null && i < labelJawaban.Length)
                    labelJawaban[i].text = "";
                tombolJawaban[i].name = opsi[i].namaHuruf;
            }
        }

        private void SetupTombolTeks(QuestionData soal)
        {
            var opsi = MixOptions(soal.aksaraJawaban, soal.distractors);
            for (int i = 0; i < tombolJawaban.Length && i < opsi.Count; i++)
            {
                if (labelJawaban != null && i < labelJawaban.Length)
                    labelJawaban[i].text = opsi[i].fonetik;
                tombolJawaban[i].name = opsi[i].namaHuruf;
            }
        }

        private List<AksaraData> MixOptions(AksaraData jawaban, List<AksaraData> distractors)
        {
            var list = new List<AksaraData> { jawaban };
            list.AddRange(distractors);
            // Fisher-Yates
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }

        private IEnumerator TimerCountdown()
        {
            float t = timerPerSoal;
            while (t > 0f && !_answerLocked)
            {
                t -= Time.deltaTime;
                if (timerBar != null) timerBar.fillAmount = t / timerPerSoal;
                yield return null;
            }
            if (!_answerLocked)
                OnTimeout();
        }

        private void OnJawab(int idx)
        {
            if (_answerLocked) return;
            _answerLocked = true;
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);

            bool benar = tombolJawaban[idx].name == _soalList[_soalIndex].aksaraJawaban.namaHuruf;
            if (benar)
            {
                _benarCount++;
                if (imageJawaban != null && idx < imageJawaban.Length) imageJawaban[idx].color = Color.green;
                AudioManager.Instance?.PlaySFX(sfxBenar);
                guru?.ReactToCorrect();
            }
            else
            {
                if (imageJawaban != null && idx < imageJawaban.Length) imageJawaban[idx].color = Color.red;
                AudioManager.Instance?.PlaySFX(sfxSalah);
                guru?.ReactToWrong();
            }

            StartCoroutine(NextSoalAfterDelay(benar ? 1f : 2f));
        }

        private void OnTimeout()
        {
            _answerLocked = true;
            StartCoroutine(NextSoalAfterDelay(1f));
        }

        private IEnumerator NextSoalAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ResetTombolWarna();
            _soalIndex++;
            yield return StartCoroutine(ShowSoal());
        }

        private void ReplayAudio()
        {
            if (_replayCount >= 2) return;
            _replayCount++;
            AudioManager.Instance?.PlayPelafalan(_soalList[_soalIndex].aksaraJawaban.audioPelafalan);
        }

        private void ShowResult()
        {
            int skor    = _benarCount * 10;
            int bintang = skor >= 90 ? 3 : skor >= 70 ? 2 : 1;
            GameManager.Instance?.SaveProgress("level3", bintang);
            GameManager.Instance?.SaveHighScore(skor);

            if (panelResult != null)
            {
                panelResult.SetActive(true);
                textResult.text = $"Jawaban benar: {_benarCount}/{_soalList.Count}\nSkor: {skor}\n{new string('★', bintang)}{new string('☆', 3 - bintang)}";
            }
        }

        private void ResetTombolWarna()
        {
            if (imageJawaban == null) return;
            foreach (var img in imageJawaban)
                if (img != null) img.color = Color.white;
        }
    }
}
