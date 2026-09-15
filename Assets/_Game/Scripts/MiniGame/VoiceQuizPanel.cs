using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HayuNgaksara
{
    /// <summary>
    /// Panel kuis suara — player mengucapkan nama aksara yang ditampilkan.
    /// Assign referensi UI di Inspector, lalu panggil Setup() dari MiniGameManager.
    /// </summary>
    public class VoiceQuizPanel : MonoBehaviour
    {
        [Header("Display Aksara")]
        [SerializeField] private Image            imgAksara;
        [SerializeField] private TextMeshProUGUI  txtAksaraChar;

        [Header("Status & Kontrol")]
        [SerializeField] private TextMeshProUGUI  txtQuestion;
        [SerializeField] private TextMeshProUGUI  txtResult;
        [SerializeField] private TextMeshProUGUI  txtProgress;
        [SerializeField] private Button           btnMic;
        [SerializeField] private Slider           sliderTimer;
        [SerializeField] private GameObject       spinnerIcon;

        [Header("Mic Indicator")]
        [SerializeField] private TextMeshProUGUI txtMicName;       // nama device mic
        [SerializeField] private Slider          sliderVoiceLevel; // level suara realtime
        [SerializeField] private GameObject      panelMicReady;    // panel "mic siap" saat idle

        [Header("Fallback")]
        [SerializeField] private Button btnSkip; // "Lewati →" — muncul jika mic tidak tersedia

        [Header("Settings")]
        [SerializeField] private float recordDuration = 3f;
        [SerializeField, Range(64, 1024)] private int sampleWindow = 256;

        private AksaraCard   _card;
        private Action<bool> _onResult;
        private bool         _busy;
        private AudioClip    _monitorClip;
        private bool         _monitoring;

        private void Awake()
        {
            btnMic?.onClick.AddListener(OnMicPressed);
            btnSkip?.onClick.AddListener(() => _onResult?.Invoke(false));
        }

        private void OnEnable()  => StartMonitoring();
        private void OnDisable() => StopMonitoring();

        private void StartMonitoring()
        {
            if (_monitoring) return;
            string[] devices = Microphone.devices;
            if (devices == null || devices.Length == 0) return;
            string device = devices[0];
            if (txtMicName) txtMicName.text = "Mic: " + device;
            _monitorClip = Microphone.Start(device, true, 1, 44100);
            _monitoring  = true;
            StartCoroutine(MonitorLevel(device));
        }

        private void StopMonitoring()
        {
            if (!_monitoring) return;
            Microphone.End(null);
            _monitoring = false;
            if (sliderVoiceLevel) sliderVoiceLevel.value = 0f;
        }

        private IEnumerator MonitorLevel(string device)
        {
            var samples = new float[sampleWindow];
            while (_monitoring && !_busy)
            {
                int pos = Microphone.GetPosition(device);
                if (_monitorClip != null && pos >= sampleWindow)
                {
                    _monitorClip.GetData(samples, pos - sampleWindow);
                    float rms = 0f;
                    foreach (var s in samples) rms += s * s;
                    float level = Mathf.Clamp01(Mathf.Sqrt(rms / sampleWindow) * 8f);
                    if (sliderVoiceLevel) sliderVoiceLevel.value = level;
                }
                yield return new WaitForSecondsRealtime(0.05f);
            }
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Siapkan panel untuk menampilkan satu aksara.</summary>
        public void Setup(AksaraCard card, int idx, int total, Action<bool> onResult)
        {
            _card     = card;
            _onResult = onResult;
            _busy     = false;

            // Selalu gunakan font Sundanese — sprite disembunyikan
            if (imgAksara != null) imgAksara.gameObject.SetActive(false);
            if (txtAksaraChar != null)
            {
                txtAksaraChar.gameObject.SetActive(true);
                txtAksaraChar.enabled = true;
                txtAksaraChar.text = card.aksaraChar;
            }

            if (txtQuestion) txtQuestion.text = "Ucapkan nama aksara ini!";
            if (txtProgress) txtProgress.text = $"{idx + 1} / {total}";
            if (txtResult  ) { txtResult.gameObject.SetActive(false); txtResult.text = ""; }
            if (sliderTimer) { sliderTimer.value = 0f; sliderTimer.gameObject.SetActive(false); }
            if (spinnerIcon) spinnerIcon.SetActive(false);
            if (panelMicReady) panelMicReady.SetActive(true);

            bool ready = VoiceRecognitionService.Instance != null && VoiceRecognitionService.Instance.IsReady;
            if (btnMic)  btnMic.interactable  = ready;
            if (btnSkip) btnSkip.gameObject.SetActive(!ready);

            if (!_monitoring) StartMonitoring();

            if (!ready)
                SetStatus("Mikrofon tidak tersedia — tekan Lewati untuk lanjut", new Color(0.8f, 0.4f, 0f));
        }

        // ── Internal ─────────────────────────────────────────────────────────

        private void OnMicPressed()
        {
            if (_busy) return;
            StartCoroutine(RecordRoutine());
        }

        private IEnumerator RecordRoutine()
        {
            _busy = true;
            StopMonitoring();
            if (btnMic) btnMic.interactable = false;
            if (txtResult) txtResult.gameObject.SetActive(false);
            // Tetap tampilkan panel mic (nama mic masih terlihat), hanya sembunyikan slider level
            if (sliderVoiceLevel) sliderVoiceLevel.gameObject.SetActive(false);

            // Rekam — timer menggantikan slider level
            if (sliderTimer) sliderTimer.gameObject.SetActive(true);
            VoiceRecognitionService.Instance.StartRecording((int)recordDuration);

            float elapsed = 0f;
            while (elapsed < recordDuration)
            {
                elapsed += Time.deltaTime;
                if (sliderTimer) sliderTimer.value = elapsed / recordDuration;
                yield return null;
            }
            if (sliderTimer) sliderTimer.gameObject.SetActive(false);

            // Proses
            if (spinnerIcon) spinnerIcon.SetActive(true);
            string recognized = "";
            yield return StartCoroutine(
                VoiceRecognitionService.Instance.StopAndRecognize(r => recognized = r));
            if (spinnerIcon) spinnerIcon.SetActive(false);

            // Evaluasi
            bool correct = IsMatch(recognized, _card.latinName);
            string msg = correct
                ? $"✓ Benar! \"{_card.latinName.ToUpper()}\""
                : $"✗ Jawaban: \"{_card.latinName.ToUpper()}\"  (ucapan: \"{recognized}\")";

            SetStatus(msg, correct ? new Color(0.1f, 0.75f, 0.2f) : new Color(0.9f, 0.2f, 0.2f));

            // Sembunyikan panel mic saat hasil tampil agar tidak overlap
            if (panelMicReady) panelMicReady.SetActive(false);

            yield return new WaitForSecondsRealtime(2f);
            _busy = false;

            // Kembalikan semua state
            if (sliderVoiceLevel) sliderVoiceLevel.gameObject.SetActive(true);
            if (panelMicReady) panelMicReady.SetActive(true);
            StartMonitoring();
            _onResult?.Invoke(correct);
        }

        private void SetStatus(string msg, Color color)
        {
            if (txtResult == null) return;
            txtResult.gameObject.SetActive(true);
            txtResult.text  = msg;
            txtResult.color = color;
        }

        // ── String matching ──────────────────────────────────────────────────

        private static bool IsMatch(string recognized, string expected)
        {
            if (string.IsNullOrEmpty(recognized)) return false;
            recognized = recognized.ToLower().Trim();
            expected   = expected.ToLower().Trim();
            return recognized == expected
                || recognized.Contains(expected)
                || Levenshtein(recognized, expected) <= 1;
        }

        private static int Levenshtein(string a, string b)
        {
            int n = a.Length, m = b.Length;
            int[,] d = new int[n + 1, m + 1];
            for (int i = 0; i <= n; i++) d[i, 0] = i;
            for (int j = 0; j <= m; j++) d[0, j] = j;
            for (int i = 1; i <= n; i++)
                for (int j = 1; j <= m; j++)
                    d[i, j] = a[i - 1] == b[j - 1]
                        ? d[i - 1, j - 1]
                        : 1 + Mathf.Min(d[i - 1, j], Mathf.Min(d[i, j - 1], d[i - 1, j - 1]));
            return d[n, m];
        }
    }
}
