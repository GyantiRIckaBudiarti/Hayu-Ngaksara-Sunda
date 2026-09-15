using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace HayuNgaksara
{
    [Serializable]
    public class DialogLine
    {
        public string speakerName;
        [TextArea(2, 4)] public string text;
        public Sprite portrait;
        public float autoAdvanceAfter = 0f;
    }

    public class DialogSystem : MonoBehaviour
    {
        public static DialogSystem Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject       panelDialog;
        [SerializeField] private TextMeshProUGUI  txtSpeaker;
        [SerializeField] private TextMeshProUGUI  txtBody;
        [SerializeField] private Image            imgPortrait;
        [SerializeField] private GameObject       iconPressNext;
        [SerializeField] private float            typeSpeed = 0.03f;

        [Header("Choice UI (legacy — disembunyikan, diganti daftar dinamis)")]
        [SerializeField] private GameObject       panelChoices;
        [SerializeField] private Button           btnChoice0;
        [SerializeField] private Button           btnChoice1;
        [SerializeField] private TextMeshProUGUI  txtChoice0;
        [SerializeField] private TextMeshProUGUI  txtChoice1;

        [Header("Choice UI (dinamis, gaya Skyrim — teks kecil, banyak opsi)")]
        [SerializeField] private float choiceFontSize   = 26f;
        [SerializeField] private Color choiceColorNormal = new Color(0.72f, 0.72f, 0.72f, 1f);
        [SerializeField] private Color choiceColorHover  = new Color(1f, 0.97f, 0.85f, 1f);
        [SerializeField] private Color choiceColorPress  = new Color(1f, 0.85f, 0.3f, 1f);

        public bool IsOpen      { get; private set; }
        public bool ChoiceOpen  { get; private set; }

        private Queue<DialogLine> _queue = new Queue<DialogLine>();
        private Coroutine         _typingCoroutine;
        private bool              _lineComplete;
        private Action            _onFinished;

        private string[]   _pendingChoices;
        private Action<int> _onChosen;

        // Daftar pilihan dinamis (runtime)
        private Canvas                 _choiceCanvas;
        private RectTransform          _choiceRoot;
        private readonly List<GameObject> _choiceEntries = new List<GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            panelDialog?.SetActive(false);
            panelChoices?.SetActive(false);
            if (imgPortrait != null) imgPortrait.gameObject.SetActive(false);

            // Tombol pilihan lama tidak dipakai lagi — sembunyikan
            btnChoice0?.gameObject.SetActive(false);
            btnChoice1?.gameObject.SetActive(false);
        }

        // ── Normal dialog ──────────────────────────────────────────────────
        public void Show(IEnumerable<DialogLine> lines, Action onFinished = null)
        {
            if (panelDialog == null) { Debug.LogError("[DialogSystem] panelDialog null!"); return; }
            _pendingChoices = null;
            _onChosen       = null;
            HideChoices();
            _queue.Clear();
            foreach (var l in lines) _queue.Enqueue(l);
            _onFinished = onFinished;
            IsOpen = true;
            ChoiceOpen = false;
            PlayerTopDown.Instance?.SetCanMove(false);
            panelDialog.SetActive(true);
            if (imgPortrait != null) imgPortrait.gameObject.SetActive(true);
            Next();
        }

        // ── Dialog + pilihan di akhir (2 opsi — kompatibilitas lama) ───────
        public void ShowWithChoice(IEnumerable<DialogLine> lines, string[] choices, Action<int> onChosen)
        {
            Show(lines, null);
            _pendingChoices = choices;
            _onChosen       = onChosen;
        }

        // ── Dialog + banyak pilihan (daftar dinamis) ───────────────────────
        public void ShowWithChoices(IEnumerable<DialogLine> lines, IList<string> choices, Action<int> onChosen)
        {
            Show(lines, null);
            _pendingChoices = new List<string>(choices).ToArray();
            _onChosen       = onChosen;
        }

        // ── Input handling ─────────────────────────────────────────────────
        private void Update()
        {
            var kb = Keyboard.current;

            // Saat daftar pilihan tampil → dukung tombol angka 1..9
            if (IsOpen && ChoiceOpen)
            {
                if (kb == null || _pendingChoices == null) return;
                for (int i = 0; i < _pendingChoices.Length && i < 9; i++)
                {
                    if (kb[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame)
                    {
                        SelectChoice(i);
                        return;
                    }
                }
                return;
            }

            if (!IsOpen) return;
            bool pressed = kb != null && (kb.zKey.wasPressedThisFrame ||
                           kb.eKey.wasPressedThisFrame ||
                           kb.spaceKey.wasPressedThisFrame ||
                           kb.enterKey.wasPressedThisFrame);
            if (!pressed) return;

            if (!_lineComplete)
            {
                if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
                txtBody.maxVisibleCharacters = int.MaxValue;
                _lineComplete = true;
                if (iconPressNext) iconPressNext.SetActive(true);
            }
            else
            {
                Next();
            }
        }

        private void Next()
        {
            if (_queue.Count == 0) { Close(); return; }
            var line = _queue.Dequeue();
            txtSpeaker.text = line.speakerName;
            if (imgPortrait != null)
            {
                imgPortrait.sprite  = line.portrait;
                imgPortrait.enabled = line.portrait != null;
            }
            _lineComplete = false;
            if (iconPressNext) iconPressNext.SetActive(false);
            if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
            _typingCoroutine = StartCoroutine(TypeLine(line));
        }

        private IEnumerator TypeLine(DialogLine line)
        {
            txtBody.text = line.text;
            txtBody.maxVisibleCharacters = 0;
            int total = line.text.Length;
            for (int i = 0; i <= total; i++)
            {
                txtBody.maxVisibleCharacters = i;
                yield return new WaitForSecondsRealtime(typeSpeed);
            }
            _lineComplete = true;
            if (iconPressNext) iconPressNext.SetActive(true);

            if (line.autoAdvanceAfter > 0f)
            {
                yield return new WaitForSecondsRealtime(line.autoAdvanceAfter);
                Next();
            }
        }

        private void Close()
        {
            if (_pendingChoices != null && _pendingChoices.Length > 0)
            {
                ShowChoiceList();
                return;
            }
            CloseAll();
        }

        // ── Daftar pilihan dinamis ─────────────────────────────────────────
        // Layout MANUAL (tanpa LayoutGroup/ContentSizeFitter) agar deterministik &
        // selalu punya area klik — layout-group runtime rawan tinggi 0 / graphic mati.
        private const float ChPad = 18f, ChRowH = 58f, ChGap = 8f, ChRowW = 680f;

        private void ShowChoiceList()
        {
            ChoiceOpen = true;
            if (iconPressNext) iconPressNext.SetActive(false);
            EnsureEventSystem();          // pastikan input UI aktif (Input System baru)
            EnsureChoiceRoot();
            ClearChoiceEntries();
            _choiceRoot.gameObject.SetActive(true);
            // Bawa ke depan (child paling akhir → render/raycast paling atas di canvas dialog)
            _choiceRoot.SetAsLastSibling();

            int n = _pendingChoices.Length;
            float h = ChPad * 2f + n * ChRowH + Mathf.Max(0, n - 1) * ChGap;
            _choiceRoot.sizeDelta = new Vector2(ChRowW + ChPad * 2f, h);

            for (int i = 0; i < n; i++)
                _choiceEntries.Add(CreateChoiceEntry(i, _pendingChoices[i]));
        }

        // Jamin ada EventSystem dengan modul Input System baru — kalau tidak, klik UI mati.
        private void EnsureEventSystem()
        {
            if (EventSystem.current != null &&
                EventSystem.current.GetComponent<InputSystemUIInputModule>() != null) return;

            if (EventSystem.current == null)
            {
                var go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<InputSystemUIInputModule>();
                return;
            }
            // EventSystem ada tapi modulnya bukan Input System baru → perbaiki
            var es = EventSystem.current.gameObject;
            var legacy = es.GetComponent<StandaloneInputModule>();
            if (legacy != null) Destroy(legacy);
            if (es.GetComponent<InputSystemUIInputModule>() == null)
                es.AddComponent<InputSystemUIInputModule>();
        }

        private void EnsureChoiceRoot()
        {
            if (_choiceRoot != null) return;

            // Pakai canvas dialog bawaan (sudah dikonfigurasi Unity dengan benar). Membuat
            // canvas + CanvasScaler sendiri saat runtime terbukti membuat raycast klik meleset
            // di sebagian resolusi. Cukup jadikan child paling atas + hilangkan blocker.
            Canvas canvas = panelDialog != null ? panelDialog.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
                foreach (var c in FindObjectsOfType<Canvas>())
                    if (c.renderMode == RenderMode.ScreenSpaceOverlay) { canvas = c; break; }
            if (canvas == null) return;
            _choiceCanvas = canvas;

            var rootGO = new GameObject("DialogChoiceList");
            rootGO.transform.SetParent(canvas.transform, false);
            var rt = rootGO.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 120f);  // di atas kotak dialog
            rt.sizeDelta = new Vector2(ChRowW + ChPad * 2f, 100f); // tinggi diatur ulang saat show

            // Panel solid dari design system (kontras tinggi, tidak "hilang" di background)
            var bg = rootGO.AddComponent<Image>();
            bg.color  = UITheme.Surface;
            bg.sprite = UITheme.UISprite();
            bg.type   = bg.sprite != null ? Image.Type.Sliced : Image.Type.Simple; // build-safe
            bg.raycastTarget = true;
            var outline = rootGO.AddComponent<Outline>();
            outline.effectColor    = UITheme.WithAlpha(UITheme.Primary, 0.55f);
            outline.effectDistance = new Vector2(2f, 2f);

            _choiceRoot = rt;
        }

        private GameObject CreateChoiceEntry(int idx, string label)
        {
            // Baris ber-latar — POSISI & UKURAN MANUAL (deterministik, pasti bisa diklik)
            var go = new GameObject($"Choice_{idx}");
            go.transform.SetParent(_choiceRoot, false);

            var rrt = go.AddComponent<RectTransform>();
            rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 1f); // jangkar atas-tengah root
            rrt.pivot     = new Vector2(0.5f, 1f);
            rrt.sizeDelta = new Vector2(ChRowW, ChRowH);
            rrt.anchoredPosition = new Vector2(0f, -(ChPad + idx * (ChRowH + ChGap)));

            var rowImg = go.AddComponent<Image>();
            rowImg.enabled = true;
            rowImg.color   = UITheme.SurfaceRaised;
            rowImg.sprite  = UITheme.UISprite();
            rowImg.type    = rowImg.sprite != null ? Image.Type.Sliced : Image.Type.Simple; // build-safe
            rowImg.raycastTarget = true;   // area klik = seluruh baris

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = rowImg;
            btn.transition = Selectable.Transition.ColorTint;
            var cb = btn.colors;
            cb.normalColor      = Color.white;                              // = SurfaceRaised
            cb.highlightedColor = UITheme.WithAlpha(UITheme.Primary, 1f);   // sorot emas
            cb.pressedColor     = UITheme.Gold600;
            cb.selectedColor    = Color.white;
            cb.fadeDuration     = 0.08f;
            btn.colors = cb;

            // nomor (badge kiri)
            var num = UITheme.Label(go.transform, (idx < 9 ? (idx + 1).ToString() : "•"),
                choiceFontSize, UITheme.Primary, TMPro.FontStyles.Bold, TMPro.TextAlignmentOptions.Center);
            var nrt = num.rectTransform;
            nrt.anchorMin = new Vector2(0f, 0f); nrt.anchorMax = new Vector2(0f, 1f);
            nrt.pivot = new Vector2(0f, 0.5f); nrt.sizeDelta = new Vector2(48f, 0f);
            nrt.anchoredPosition = new Vector2(20f, 0f);
            num.raycastTarget = false;

            // teks pilihan
            var tmp = UITheme.Label(go.transform, label, choiceFontSize, UITheme.Text,
                TMPro.FontStyles.Normal, TMPro.TextAlignmentOptions.Left);
            var trt = tmp.rectTransform;
            trt.anchorMin = new Vector2(0f, 0f); trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(72f, 0f); trt.offsetMax = new Vector2(-16f, 0f);
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TMPro.TextOverflowModes.Ellipsis;

            int captured = idx;
            btn.onClick.AddListener(() => SelectChoice(captured));
            return go;
        }

        private void ClearChoiceEntries()
        {
            foreach (var e in _choiceEntries) if (e != null) Destroy(e);
            _choiceEntries.Clear();
        }

        private void HideChoices()
        {
            panelChoices?.SetActive(false);
            ClearChoiceEntries();
            if (_choiceRoot != null) _choiceRoot.gameObject.SetActive(false);
        }

        public void SelectChoice(int idx)
        {
            HideChoices();
            ChoiceOpen = false;
            var cb = _onChosen;
            _pendingChoices = null;
            _onChosen       = null;
            CloseAll();
            cb?.Invoke(idx);
        }

        private void CloseAll()
        {
            IsOpen = false;
            ChoiceOpen = false;
            panelDialog?.SetActive(false);
            HideChoices();
            if (imgPortrait != null) imgPortrait.gameObject.SetActive(false);
            PlayerTopDown.Instance?.SetCanMove(true);
            _onFinished?.Invoke();
            _onFinished = null;
        }
    }
}
