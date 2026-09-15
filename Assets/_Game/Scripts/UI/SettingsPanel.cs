using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace HayuNgaksara
{
    /// <summary>
    /// Menu PENGATURAN full-screen — dibangun dengan design system <see cref="UITheme"/>.
    /// Berisi: volume Musik &amp; Efek Suara, dan bagian Mikrofon (deteksi + pilih perangkat
    /// + bar level suara langsung). Self-building: panggil <see cref="Open"/>.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        public static SettingsPanel Instance { get; private set; }

        private GameObject _panel;
        private Slider _sliderMusic, _sliderSfx;
        private TextMeshProUGUI _valMusic, _valSfx;

        // Mic
        private string[] _mics = new string[0];
        private int      _micIndex;
        private TextMeshProUGUI _micLabel, _micHint;
        private RectTransform   _levelFill;
        private AudioClip       _monitorClip;
        private string          _monitorDevice;
        private const int       MonitorFreq = 16000;

        // Reset simpanan
        private Button          _resetBtn;
        private bool            _resetArmed;

        public static void Open()
        {
            if (Instance == null)
                Instance = new GameObject("SettingsPanel").AddComponent<SettingsPanel>();
            Instance.Show();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildUI();
        }

        public void Show()
        {
            float m = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
            float s = PlayerPrefs.GetFloat("SFXVolume",   0.8f);
            _sliderMusic.SetValueWithoutNotify(m);
            _sliderSfx.SetValueWithoutNotify(s);
            if (_valMusic != null) _valMusic.text = Pct(m);
            if (_valSfx   != null) _valSfx.text   = Pct(s);

            RefreshMicList();
            _resetArmed = false; SetResetLabel("Reset Simpanan");
            _panel.SetActive(true);
            _panel.transform.SetAsLastSibling();
            StartMonitor();
        }

        public void Hide() { StopMonitor(); _panel.SetActive(false); }

        private void Update()
        {
            if (_panel == null || !_panel.activeSelf) return;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            { Hide(); return; }

            UpdateMicLevel();
        }

        // ── BUILD ────────────────────────────────────────────────────────────
        private void BuildUI()
        {
            var canvas = UITheme.CreateCanvas("Canvas_Settings", 500);
            DontDestroyOnLoad(canvas.gameObject);

            _panel = new GameObject("Panel_Settings");
            _panel.transform.SetParent(canvas.transform, false);
            var rt = _panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            _panel.AddComponent<Image>().color = UITheme.Scrim;

            var card = UITheme.Panel(_panel.transform, "Panel_Center", new Vector2(940f, 680f), UITheme.Surface);
            var ct = card.transform;

            var title = UITheme.Label(ct, "PENGATURAN", UITheme.FH1, UITheme.Primary, FontStyles.Bold);
            title.enableWordWrapping = false;
            var titleRt = title.rectTransform;
            titleRt.anchorMin = titleRt.anchorMax = titleRt.pivot = new Vector2(0.5f, 0.5f);
            titleRt.sizeDelta = new Vector2(800f, 60f);
            titleRt.anchoredPosition = new Vector2(0f, 285f);

            SectionHeader(ct, "AUDIO", 225f);
            _sliderMusic = SliderRow(ct, "Musik", 165f,
                PlayerPrefs.GetFloat("MusicVolume", 0.8f),
                v => { if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(v); }, out _valMusic);
            _sliderSfx = SliderRow(ct, "Efek Suara", 85f,
                PlayerPrefs.GetFloat("SFXVolume", 0.8f),
                v => { if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(v); }, out _valSfx);

            SectionHeader(ct, "MIKROFON", 5f);
            BuildMicSelector(ct, -55f);
            BuildLevelMeter(ct, -140f);

            // Reset simpanan (mulai benar-benar dari awal) — konfirmasi 2-klik
            _resetBtn = UITheme.Button(ct, "Reset Simpanan", UITheme.BtnVariant.Danger, new Vector2(340f, 54f),
                OnResetClicked, UITheme.FBody);
            _resetBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -212f);

            UITheme.Button(ct, "Kembali", UITheme.BtnVariant.Primary, new Vector2(340f, 70f),
                Hide, UITheme.FTitle).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -295f);

            _panel.SetActive(false);
        }

        // ── Reset simpanan ────────────────────────────────────────────────────
        private void OnResetClicked()
        {
            if (!_resetArmed) { _resetArmed = true; SetResetLabel("Yakin? Klik lagi untuk reset"); return; }
            _resetArmed = false;

            PlayerPrefs.DeleteKey("PlayerName");
            PlayerPrefs.DeleteKey("PlayerGender");
            PlayerPrefs.SetInt("tutorial_done", 0);
            string[] acts = { "baca", "tulis", "pelafalan", "refleksi", "combine" };
            for (int i = 0; i <= 5; i++)
            {
                PlayerPrefs.DeleteKey($"ch_{i}_unlock");
                PlayerPrefs.DeleteKey($"ch_{i}_done");
                PlayerPrefs.DeleteKey($"ch_{i}_stars");
                foreach (var a in acts)
                {
                    PlayerPrefs.DeleteKey($"ch_{i}_{a}_done");
                    PlayerPrefs.DeleteKey($"ch_{i}_{a}_stars");
                    PlayerPrefs.DeleteKey($"ch_{i}_{a}_score");
                    PlayerPrefs.DeleteKey($"ch_{i}_{a}_total");
                }
            }
            PlayerPrefs.DeleteKey("pending_levelup");
            PlayerPrefs.DeleteKey("SummaryChapterID");
            PlayerPrefs.Save();
            ChapterManager.Instance?.ResetAll();

            // Perbarui Main Menu bila sedang terbuka (sembunyikan "Lanjutkan")
            var mmc = FindObjectOfType<MainMenuController>();
            if (mmc != null) mmc.RefreshContinueButton();

            SetResetLabel("Simpanan direset ✓");
        }

        private void SetResetLabel(string text)
        {
            if (_resetBtn == null) return;
            var lbl = _resetBtn.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (lbl != null) lbl.text = text;
        }

        private void SectionHeader(Transform card, string text, float y)
        {
            var lbl = UITheme.Label(card, text, UITheme.FSmall, UITheme.Primary, FontStyles.Bold, TextAlignmentOptions.Left);
            var rt = lbl.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(760f, 26f);
            rt.anchoredPosition = new Vector2(0f, y);
            // garis tipis di bawah header
            var line = new GameObject("HeaderLine");
            line.transform.SetParent(card, false);
            var lrt = line.AddComponent<RectTransform>();
            lrt.anchorMin = lrt.anchorMax = lrt.pivot = new Vector2(0.5f, 0.5f);
            lrt.sizeDelta = new Vector2(760f, 2f);
            lrt.anchoredPosition = new Vector2(0f, y - 18f);
            line.AddComponent<Image>().color = UITheme.WithAlpha(UITheme.Border, 0.7f);
        }

        private void BuildMicSelector(Transform card, float y)
        {
            // tombol ◀
            var prev = UITheme.Button(card, "◀", UITheme.BtnVariant.Ghost, new Vector2(64f, 56f),
                () => CycleMic(-1), UITheme.FTitle);
            prev.GetComponent<RectTransform>().anchoredPosition = new Vector2(-330f, y);
            // tombol ▶
            var next = UITheme.Button(card, "▶", UITheme.BtnVariant.Ghost, new Vector2(64f, 56f),
                () => CycleMic(1), UITheme.FTitle);
            next.GetComponent<RectTransform>().anchoredPosition = new Vector2(330f, y);
            // nama mic (tengah)
            var box = UITheme.Panel(card, "MicBox", new Vector2(560f, 56f), UITheme.SurfaceRaised);
            box.rectTransform.anchoredPosition = new Vector2(0f, y);
            _micLabel = UITheme.Label(box.transform, "—", UITheme.FBody, UITheme.Text, FontStyles.Normal);
            var mrt = _micLabel.rectTransform; mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one;
            mrt.offsetMin = new Vector2(16f, 0f); mrt.offsetMax = new Vector2(-16f, 0f);
            _micLabel.enableWordWrapping = false; _micLabel.overflowMode = TextOverflowModes.Ellipsis;
        }

        private void BuildLevelMeter(Transform card, float y)
        {
            var lbl = UITheme.Label(card, "Level suara", UITheme.FSmall, UITheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.Left);
            var lrt = lbl.rectTransform; lrt.anchorMin = lrt.anchorMax = lrt.pivot = new Vector2(0.5f, 0.5f);
            lrt.sizeDelta = new Vector2(760f, 22f); lrt.anchoredPosition = new Vector2(0f, y + 26f);

            Image fill;
            var fillRt = UITheme.Bar(card, UITheme.WithAlpha(UITheme.Ink900, 0.6f), UITheme.Success, out fill);
            var bar = fill.transform.parent.gameObject.GetComponent<RectTransform>();
            bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0.5f, 0.5f);
            bar.sizeDelta = new Vector2(760f, 24f);
            bar.anchoredPosition = new Vector2(0f, y);
            _levelFill = fillRt;

            _micHint = UITheme.Label(card, "", UITheme.FTiny, UITheme.TextMuted, FontStyles.Italic, TextAlignmentOptions.Left);
            var hrt = _micHint.rectTransform; hrt.anchorMin = hrt.anchorMax = hrt.pivot = new Vector2(0.5f, 0.5f);
            hrt.sizeDelta = new Vector2(760f, 20f); hrt.anchoredPosition = new Vector2(0f, y - 26f);
        }

        // Satu baris slider lengkap (label + track + fill + handle + nilai %).
        private Slider SliderRow(Transform card, string label, float centerY, float init,
                                 Action<float> onChanged, out TextMeshProUGUI valText)
        {
            var lbl = UITheme.Label(card, label, UITheme.FBody, UITheme.Text, FontStyles.Normal, TextAlignmentOptions.Left);
            var lrt = lbl.rectTransform; lrt.anchorMin = lrt.anchorMax = lrt.pivot = new Vector2(0.5f, 0.5f);
            lrt.sizeDelta = new Vector2(260f, 34f); lrt.anchoredPosition = new Vector2(-350f, centerY);

            valText = UITheme.Label(card, Pct(init), UITheme.FBody, UITheme.Primary, FontStyles.Bold, TextAlignmentOptions.Right);
            var vrt = valText.rectTransform; vrt.anchorMin = vrt.anchorMax = vrt.pivot = new Vector2(0.5f, 0.5f);
            vrt.sizeDelta = new Vector2(120f, 34f); vrt.anchoredPosition = new Vector2(360f, centerY);

            var sGO = new GameObject("Slider_" + label.Replace(" ", ""));
            sGO.transform.SetParent(card, false);
            var rtS = sGO.AddComponent<RectTransform>();
            rtS.anchorMin = rtS.anchorMax = rtS.pivot = new Vector2(0.5f, 0.5f);
            rtS.sizeDelta = new Vector2(560f, 22f); rtS.anchoredPosition = new Vector2(-30f, centerY);
            var bgImg = sGO.AddComponent<Image>(); bgImg.color = UITheme.WithAlpha(UITheme.Ink900, 0.6f);
            bgImg.sprite = UITheme.UISprite(); bgImg.type = Image.Type.Sliced;
            var slider = sGO.AddComponent<Slider>();

            var fillArea = new GameObject("Fill Area"); fillArea.transform.SetParent(sGO.transform, false);
            var rtFA = fillArea.AddComponent<RectTransform>();
            rtFA.anchorMin = new Vector2(0f, 0.5f); rtFA.anchorMax = new Vector2(1f, 0.5f);
            rtFA.offsetMin = new Vector2(0f, -11f); rtFA.offsetMax = new Vector2(0f, 11f);
            var fill = new GameObject("Fill"); fill.transform.SetParent(fillArea.transform, false);
            var rtFill = fill.AddComponent<RectTransform>();
            rtFill.anchorMin = new Vector2(0f, 0f); rtFill.anchorMax = new Vector2(0f, 1f); rtFill.sizeDelta = new Vector2(10f, 0f);
            var fillImg = fill.AddComponent<Image>(); fillImg.color = UITheme.Primary; fillImg.sprite = UITheme.UISprite(); fillImg.type = Image.Type.Sliced;

            var handleArea = new GameObject("Handle Slide Area"); handleArea.transform.SetParent(sGO.transform, false);
            var rtHA = handleArea.AddComponent<RectTransform>();
            rtHA.anchorMin = new Vector2(0f, 0f); rtHA.anchorMax = new Vector2(1f, 1f);
            rtHA.offsetMin = new Vector2(11f, 0f); rtHA.offsetMax = new Vector2(-11f, 0f);
            var handle = new GameObject("Handle"); handle.transform.SetParent(handleArea.transform, false);
            var rtH = handle.AddComponent<RectTransform>(); rtH.sizeDelta = new Vector2(28f, 28f);
            var handleImg = handle.AddComponent<Image>(); handleImg.color = UITheme.Cream50; handleImg.sprite = UITheme.UISprite(); handleImg.type = Image.Type.Sliced;

            slider.fillRect = rtFill; slider.handleRect = rtH; slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight; slider.minValue = 0f; slider.maxValue = 1f; slider.value = init;
            var vt = valText;
            slider.onValueChanged.AddListener(v => { onChanged?.Invoke(v); vt.text = Pct(v); });
            return slider;
        }

        // ── MIC ──────────────────────────────────────────────────────────────
        private void RefreshMicList()
        {
            _mics = Microphone.devices;
            if (_mics.Length == 0)
            {
                _micIndex = 0;
                if (_micLabel != null) _micLabel.text = "Tidak ada mikrofon";
                if (_micHint  != null) _micHint.text  = "Colokkan mikrofon lalu buka lagi menu ini.";
                return;
            }
            string saved = PlayerPrefs.GetString("MicDevice", "");
            _micIndex = Array.IndexOf(_mics, saved);
            if (_micIndex < 0) _micIndex = 0;
            ApplyMicSelection(false);
        }

        private void CycleMic(int dir)
        {
            if (_mics.Length == 0) return;
            _micIndex = (_micIndex + dir + _mics.Length) % _mics.Length;
            ApplyMicSelection(true);
            StopMonitor(); StartMonitor(); // pindah monitor ke perangkat baru
        }

        private void ApplyMicSelection(bool save)
        {
            string dev = _mics[_micIndex];
            if (_micLabel != null) _micLabel.text = dev;
            if (_micHint  != null) _micHint.text  = "Bicara untuk menguji — bar akan bergerak.";
            if (save)
            {
                PlayerPrefs.SetString("MicDevice", dev);
                PlayerPrefs.Save();
                if (VoiceRecognitionService.Instance != null)
                    VoiceRecognitionService.Instance.SetMicDevice(dev);
            }
        }

        private void StartMonitor()
        {
            if (_mics.Length == 0) return;
            _monitorDevice = _mics[Mathf.Clamp(_micIndex, 0, _mics.Length - 1)];
            try { _monitorClip = Microphone.Start(_monitorDevice, true, 1, MonitorFreq); }
            catch { _monitorClip = null; }
        }

        private void StopMonitor()
        {
            if (!string.IsNullOrEmpty(_monitorDevice) && Microphone.IsRecording(_monitorDevice))
                Microphone.End(_monitorDevice);
            _monitorClip = null; _monitorDevice = null;
            if (_levelFill != null) _levelFill.anchorMax = new Vector2(0f, 1f);
        }

        private readonly float[] _sampleBuf = new float[512];
        private float _levelSmoothed;

        private void UpdateMicLevel()
        {
            if (_levelFill == null) return;
            float level = 0f;
            if (_monitorClip != null && !string.IsNullOrEmpty(_monitorDevice) && Microphone.IsRecording(_monitorDevice))
            {
                int pos = Microphone.GetPosition(_monitorDevice) - _sampleBuf.Length;
                if (pos >= 0)
                {
                    _monitorClip.GetData(_sampleBuf, pos);
                    float sum = 0f;
                    for (int i = 0; i < _sampleBuf.Length; i++) sum += _sampleBuf[i] * _sampleBuf[i];
                    float rms = Mathf.Sqrt(sum / _sampleBuf.Length);
                    level = Mathf.Clamp01(rms * 6f); // gain agar mudah terlihat
                }
            }
            _levelSmoothed = Mathf.Lerp(_levelSmoothed, level, 0.35f);
            _levelFill.anchorMax = new Vector2(_levelSmoothed, 1f);
            // warna: hijau → kuning → merah saat keras
            var img = _levelFill.GetComponent<Image>();
            if (img != null)
                img.color = _levelSmoothed < 0.6f ? UITheme.Success
                          : _levelSmoothed < 0.85f ? UITheme.Primary : UITheme.Danger;
        }

        private static string Pct(float v) => Mathf.RoundToInt(v * 100f) + "%";
    }
}
