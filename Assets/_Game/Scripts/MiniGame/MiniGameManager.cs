using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace HayuNgaksara
{
    public class MiniGameManager : MonoBehaviour
    {
        public static MiniGameManager Instance { get; private set; }

        [Header("Data")]
        [SerializeField] private MiniGameData data;
        [Tooltip("Data pengganti untuk mode CombineLetters (berisi konsonan + rarangken)")]
        [SerializeField] private MiniGameData comboDataOverride;
        [Tooltip("Font Noto Sans Sundanese SDF — assign di Inspector agar aksara Unicode Sunda tampil")]
        [SerializeField] private TMPro.TMP_FontAsset sundaneseFont;

        [Header("Panels")]
        [SerializeField] private GameObject panelIntro;
        [SerializeField] private GameObject panelLearn;
        [SerializeField] private GameObject panelTrace;
        [SerializeField] private GameObject panelQuiz;
        [SerializeField] private GameObject panelResult;

        [Header("Intro UI")]
        [SerializeField] private TextMeshProUGUI txtIntroNPC;
        [SerializeField] private TextMeshProUGUI txtIntroText;
        [SerializeField] private Button          btnStartLearn;

        [Header("Learn UI")]
        [SerializeField] private TextMeshProUGUI txtLearnChapter;
        [SerializeField] private TextMeshProUGUI txtAksaraChar;   // Noto Sans Sundanese font (fallback)
        [SerializeField] private Image            imgAksaraSprite; // PNG sprite aksara (prioritas)
        [SerializeField] private TextMeshProUGUI txtAksaraLatin;
        [SerializeField] private TextMeshProUGUI txtAksaraDesc;
        [SerializeField] private TextMeshProUGUI txtCardProgress; // "3 / 6"
        [SerializeField] private Button          btnPrev;
        [SerializeField] private Button          btnNext;
        [SerializeField] private Button          btnAudio;
        [SerializeField] private Button          btnStartQuiz;

        [Header("Quiz UI")]
        [SerializeField] private TextMeshProUGUI txtQuizQuestion;
        [SerializeField] private TextMeshProUGUI txtQuizAksara;   // Noto font (fallback)
        [SerializeField] private Image            imgQuizSprite;   // PNG sprite aksara
        [SerializeField] private Button[]        answerButtons;   // 4 buttons
        [SerializeField] private TextMeshProUGUI txtQuizScore;
        [SerializeField] private TextMeshProUGUI txtQuizProgress;

        [Header("Result UI")]
        [SerializeField] private TextMeshProUGUI txtResultTitle;
        [SerializeField] private TextMeshProUGUI txtResultScore;
        [SerializeField] private TextMeshProUGUI txtResultStars;
        [SerializeField] private Button          btnRetry;
        [SerializeField] private Button          btnBack;

        [Header("Trace UI")]
        [SerializeField] private TracePanel tracePanel;
        [SerializeField] private Button     btnKeluarTrace;

        [Header("Voice Quiz UI")]
        [SerializeField] private GameObject   panelVoiceQuiz;
        [SerializeField] private VoiceQuizPanel voiceQuizPanel;
        [SerializeField] private Button       btnKeluarVoice;

        [Header("Combine Letters UI (Ujian Final)")]
        [SerializeField] private GameObject       panelCombine;
        [SerializeField] private Image            imgCombineA;       // komponen 1 (konsonan)
        [SerializeField] private TextMeshProUGUI  txtCombineA;
        [SerializeField] private Image            imgCombineB;       // komponen 2 (rarangken)
        [SerializeField] private TextMeshProUGUI  txtCombineB;
        [SerializeField] private Button[]         combineAnswerBtns; // 4 tombol jawaban
        [SerializeField] private TextMeshProUGUI  txtCombineScore;
        [SerializeField] private TextMeshProUGUI  txtCombineProgress;
        [SerializeField] private Button           btnKeluarCombine;

[Header("Keluar")]
        [SerializeField] private Button          btnKeluarLearn;
        [SerializeField] private Button          btnKeluarQuiz;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;

        // State
        private int   _cardIndex;
        private int   _quizIndex;
        private int   _traceIndex;
        private int   _score;
        private List<QuizQuestion> _questions;

        // Voice Quiz state
        private int  _voiceIndex;
        private int  _voiceScore;

        // Mode state
        private MiniGameMode _effectiveMode = MiniGameMode.Standard;
        private int          _retryCount    = 0;

        // Combine Letters state
        private int   _combineIndex;
        private int   _combineScore;
        private List<CombineQuestion> _combineQuestions;

        private struct QuizQuestion
        {
            public AksaraCard correct;
            public List<AksaraCard> options;
            public bool isReverse; // true = tampilkan latinName, pilih aksara
        }

        private struct CombineQuestion
        {
            public AksaraCard partA;      // konsonan dasar, misal "ka"
            public AksaraCard rarangken;  // tanda vokal, misal panghulu (i)
            public string     combined;   // hasil gabungan latin, misal "ki"
            public List<string> wrongOptions;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            ShowPanel(panelIntro);

            btnPrev?.onClick.AddListener(PrevCard);
            btnNext?.onClick.AddListener(NextCard);
            btnAudio?.onClick.AddListener(PlayCurrentAudio);

            // Baca mode override dari PlayerPrefs (di-set oleh LearningPanelManager)
            int modeOverride = PlayerPrefs.GetInt("ActiveMode", -1);
            _effectiveMode = modeOverride >= 0 ? (MiniGameMode)modeOverride : (data?.gameMode ?? MiniGameMode.Standard);
            if (modeOverride >= 0) PlayerPrefs.DeleteKey("ActiveMode");

            // Jika mode CombineLetters dan ada comboDataOverride, ganti data aktif
            if (_effectiveMode == MiniGameMode.CombineLetters && comboDataOverride != null)
                data = comboDataOverride;

            // Update intro text setelah kemungkinan data diganti
            if (data != null)
            {
                string npcTitle    = PlayerPrefs.GetString("ActiveNPCTitle", "");
                string npcNameKey  = PlayerPrefs.GetString("ActiveNPCName",  "");
                PlayerPrefs.DeleteKey("ActiveNPCTitle");
                PlayerPrefs.DeleteKey("ActiveNPCName");
                string baseName    = string.IsNullOrEmpty(npcNameKey) ? data.npcName : npcNameKey;
                string displayName = string.IsNullOrEmpty(npcTitle) ? baseName : npcTitle + " " + baseName;
                if (txtIntroNPC)     txtIntroNPC.text     = displayName;
                if (txtIntroText)    txtIntroText.text    = data.introText;
                if (txtLearnChapter) txtLearnChapter.text = data.chapterTitle;
            }

            ApplySundaneseFont();
            SetupAnswerButtonDualLabel();
            SetupModeListeners();
            btnKeluarTrace?.onClick.AddListener(ExitEarly);
            btnRetry?.onClick.AddListener(RetryQuiz);
            btnBack?.onClick.AddListener(BackToOverworld);
            btnKeluarLearn?.onClick.AddListener(ExitEarly);
            btnKeluarQuiz?.onClick.AddListener(ExitEarly);
            btnKeluarCombine?.onClick.AddListener(ExitEarly);
            btnKeluarVoice?.onClick.AddListener(ExitEarly);

            for (int i = 0; i < answerButtons.Length; i++)
            {
                int idx = i;
                answerButtons[i]?.onClick.AddListener(() => OnAnswerSelected(idx));
            }

            for (int i = 0; i < combineAnswerBtns.Length; i++)
            {
                int idx = i;
                combineAnswerBtns[i]?.onClick.AddListener(() => OnCombineAnswerSelected(idx));
            }
        }

        // ── FONT SETUP ────────────────────────────────────
        private void ApplySundaneseFont()
        {
            // Fallback: cari font di runtime jika SerializedField tidak ter-assign
            if (sundaneseFont == null)
            {
                foreach (var f in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
                    if (f.name.Contains("NotoSansSundanese") || f.name.Contains("Sundanese"))
                    { sundaneseFont = f; break; }
            }
            if (sundaneseFont == null) { Debug.LogError("[MGM] NotoSansSundanese font not found!"); return; }

            // Rebuild lookup tables so characters are found on first render
            sundaneseFont.ReadFontAssetDefinition();

            void Apply(TextMeshProUGUI t)
            {
                if (t == null) return;
                t.enabled = true;
                t.font = sundaneseFont;
                t.overflowMode = TextOverflowModes.Overflow;
                t.enableWordWrapping = false;
                t.ForceMeshUpdate(true, true);
            }
            Apply(txtAksaraChar);
            Apply(txtQuizAksara);
            Apply(txtCombineA);
            Apply(txtCombineB);
        }

        // ── ANSWER BUTTON — aksara only, no Latin hint ────
        private void SetupAnswerButtonDualLabel()
        {
            if (sundaneseFont == null) return;
            foreach (var btn in answerButtons)
            {
                if (btn == null) continue;
                // Sembunyikan label Latin bawaan agar tidak jadi kunci jawaban
                var latinLbl = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (latinLbl != null)
                {
                    latinLbl.name = "Lbl_Latin";
                    latinLbl.gameObject.SetActive(false);
                }
                // Buat label aksara yang mengisi seluruh tombol
                if (btn.transform.Find("Lbl_Aksara") != null) continue;
                var aksaraGO = new GameObject("Lbl_Aksara");
                aksaraGO.transform.SetParent(btn.transform, false);
                var rt = aksaraGO.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                var tmp = aksaraGO.AddComponent<TextMeshProUGUI>();
                tmp.font = sundaneseFont;
                tmp.fontSize = 32f;
                tmp.color = Color.white;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.enableWordWrapping = false;
            }
        }

        // showLatin=true  → soal aksara→baca: tampilkan NAMA LATIN di tombol jawaban
        // showLatin=false → soal latin→aksara (reverse): tampilkan AKSARA di tombol jawaban
        private void SetAnswerLabel(Button btn, AksaraCard card, bool showLatin = false)
        {
            if (btn == null) return;
            var aksaraLbl = btn.transform.Find("Lbl_Aksara")?.GetComponent<TextMeshProUGUI>();
            var latinLbl  = btn.transform.Find("Lbl_Latin")?.GetComponent<TextMeshProUGUI>();

            // Fallback jika struktur label belum dibuat
            if (aksaraLbl == null && latinLbl == null)
            {
                var any = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (any != null) any.text = showLatin ? card.latinName.ToUpper() : card.aksaraChar;
                return;
            }

            if (showLatin)
            {
                // Jawaban berupa nama Latin — tampilkan teks baca
                if (latinLbl != null)
                {
                    latinLbl.text      = card.latinName.ToUpper();
                    latinLbl.fontSize  = 28f;
                    latinLbl.alignment = TextAlignmentOptions.Center;
                    latinLbl.gameObject.SetActive(true);
                }
                if (aksaraLbl != null) aksaraLbl.gameObject.SetActive(false);
            }
            else
            {
                // Jawaban berupa aksara Sunda
                if (aksaraLbl != null)
                {
                    aksaraLbl.text = card.aksaraChar;
                    aksaraLbl.gameObject.SetActive(true);
                    if (sundaneseFont != null) aksaraLbl.font = sundaneseFont;
                    aksaraLbl.ForceMeshUpdate(true, true);
                }
                if (latinLbl != null) latinLbl.gameObject.SetActive(false);
            }
        }

        // ── MODE SETUP ────────────────────────────────────
        private void SetupModeListeners()
        {
            switch (_effectiveMode)
            {
                case MiniGameMode.TraceOnly:
                    SetBtnLabel(btnStartLearn, "Mulai Latihan Menulis");
                    btnStartLearn?.onClick.AddListener(StartTraceFromIntro);
                    btnStartQuiz?.onClick.AddListener(StartTrace);
                    break;

                case MiniGameMode.ReadBaca:
                    SetBtnLabel(btnStartLearn, "Mulai Belajar");
                    SetBtnLabel(btnStartQuiz, "Mulai Kuis →");
                    btnStartLearn?.onClick.AddListener(StartLearning);
                    btnStartQuiz?.onClick.AddListener(StartQuiz);
                    break;

                case MiniGameMode.Pelafalan:
                    SetBtnLabel(btnStartLearn, "Mulai Mendengar");
                    SetBtnLabel(btnStartQuiz, "Mulai Kuis Suara →");
                    btnStartLearn?.onClick.AddListener(StartLearning);
                    btnStartQuiz?.onClick.AddListener(StartVoiceQuiz);
                    break;

                case MiniGameMode.Refleksi:
                    bool skipReview = PlayerPrefs.GetInt("RefleksiSkipReview", 0) == 1;
                    if (skipReview)
                    {
                        SetBtnLabel(btnStartLearn, "Mulai Refleksi");
                        btnStartLearn?.onClick.AddListener(StartQuizHardMode);
                    }
                    else
                    {
                        SetBtnLabel(btnStartLearn, "Hafal Dulu");
                        btnStartLearn?.onClick.AddListener(StartLearning);
                    }
                    SetBtnLabel(btnStartQuiz, "Mulai Refleksi →");
                    btnStartQuiz?.onClick.AddListener(StartQuizHardMode);
                    break;

                case MiniGameMode.CombineLetters:
                    SetBtnLabel(btnStartLearn, "Mulai Belajar");
                    SetBtnLabel(btnStartQuiz, "Mulai Gabung Aksara →");
                    btnStartLearn?.onClick.AddListener(StartLearning);
                    btnStartQuiz?.onClick.AddListener(StartTrace);
                    break;

                default:
                    btnStartLearn?.onClick.AddListener(StartLearning);
                    btnStartQuiz?.onClick.AddListener(StartTrace);
                    break;
            }
        }

        private void SetBtnLabel(Button btn, string text)
        {
            if (btn == null) return;
            var lbl = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (lbl) lbl.text = text;
        }

        private void StartTraceFromIntro()
        {
            _traceIndex = 0;
            ShowPanel(panelTrace);
            tracePanel?.SetSessionCards(data?.cards, sundaneseFont);
            ShowTraceCard(_traceIndex);
        }

        private void StartQuizHardMode()
        {
            _score = 0;
            _questions = BuildQuestions(hardMode: true);
            _quizIndex = 0;
            ShowPanel(panelQuiz);
            ShowQuestion(_quizIndex);
        }

        // ── LEARNING ──────────────────────────────────────
        private void StartLearning()
        {
            _cardIndex = 0;
            ShowPanel(panelLearn);
            ShowCard(_cardIndex);
        }

        private void ShowCard(int idx)
        {
            if (data == null || data.cards.Count == 0) return;
            var card = data.cards[idx];

            // Selalu gunakan font Sundanese — sprite disembunyikan
            if (imgAksaraSprite != null) imgAksaraSprite.gameObject.SetActive(false);
            if (txtAksaraChar != null)
            {
                txtAksaraChar.gameObject.SetActive(true);
                txtAksaraChar.enabled = true;
                txtAksaraChar.text = card.aksaraChar;
                txtAksaraChar.color = Color.white;
                if (sundaneseFont != null) txtAksaraChar.font = sundaneseFont;
                txtAksaraChar.ForceMeshUpdate(true, true);
            }
            if (txtAksaraLatin) txtAksaraLatin.text = card.latinName.ToUpper();
            if (txtAksaraDesc)  txtAksaraDesc.text  = card.description;
            if (txtCardProgress) txtCardProgress.text = $"{idx + 1} / {data.cards.Count}";

            if (btnPrev) btnPrev.interactable = idx > 0;
            if (btnStartQuiz)
            {
                bool isLast = idx == data.cards.Count - 1;
                btnStartQuiz.gameObject.SetActive(isLast);
                if (isLast)
                {
                    var lbl = btnStartQuiz.GetComponentInChildren<TextMeshProUGUI>();
                    switch (_effectiveMode)
                    {
                        case MiniGameMode.ReadBaca:
                            if (lbl) lbl.text = "Mulai Kuis Baca →";
                            break;
                        case MiniGameMode.Pelafalan:
                            if (lbl) lbl.text = "Mulai Kuis Pelafalan →";
                            break;
                        case MiniGameMode.Refleksi:
                            if (lbl) lbl.text = "Mulai Refleksi →";
                            break;
                        case MiniGameMode.CombineLetters:
                            if (lbl) lbl.text = "Mulai Gabung Aksara →";
                            break;
                        default:
                            if (lbl) lbl.text = "Latihan Menulis →";
                            break;
                    }
                }
            }

            StartCoroutine(AutoPlayAudio(card.audioFile, card.latinName));
        }

        private void PrevCard()
        {
            if (_cardIndex > 0) { _cardIndex--; ShowCard(_cardIndex); }
        }

        private void NextCard()
        {
            if (data == null) return;
            if (_cardIndex < data.cards.Count - 1) { _cardIndex++; ShowCard(_cardIndex); }
        }

        private void PlayCurrentAudio()
        {
            if (data == null) return;
            var card = data.cards[_cardIndex];
            StartCoroutine(PlayAudio(card.audioFile, card.latinName));
        }

        private IEnumerator AutoPlayAudio(string filename, string fallbackText = "")
        {
            yield return new WaitForSecondsRealtime(0.3f);
            yield return StartCoroutine(PlayAudio(filename, fallbackText));
        }

        private IEnumerator PlayAudio(string filename, string fallbackText = "")
        {
            if (audioSource == null) yield break;

            // 1. Coba pre-recorded clip dari Resources
            if (!string.IsNullOrEmpty(filename))
            {
                var clip = Resources.Load<AudioClip>($"Audio/Pelafalan/{filename}");
                if (clip != null)
                {
                    audioSource.clip = clip;
                    audioSource.Play();
                    yield return new WaitForSeconds(clip.length);
                    yield break;
                }
            }

            // 2. Fallback ke TTS jika tersedia
            if (!string.IsNullOrEmpty(fallbackText) && TTSService.Instance != null)
            {
                AudioClip ttsClip = null;
                yield return TTSService.Instance.Speak(fallbackText, c => ttsClip = c);
                if (ttsClip != null)
                {
                    audioSource.clip = ttsClip;
                    audioSource.Play();
                    yield return new WaitForSeconds(ttsClip.length);
                }
            }
        }

        // ── QUIZ ──────────────────────────────────────────
        private void StartQuiz()
        {
            _score = 0;
            _questions = BuildQuestions(hardMode: false);
            _quizIndex = 0;
            ShowPanel(panelQuiz);
            ShowQuestion(_quizIndex);
        }

        private List<QuizQuestion> BuildQuestions(bool hardMode = false)
        {
            var qs = new List<QuizQuestion>();
            var shuffled = new List<AksaraCard>(data.cards);
            // Simple shuffle
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = shuffled[i]; shuffled[i] = shuffled[j]; shuffled[j] = tmp;
            }

            foreach (var card in shuffled)
            {
                var opts = new List<AksaraCard> { card };
                var pool = new List<AksaraCard>(data.cards);
                pool.Remove(card);
                for (int i = pool.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    var tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp;
                }
                for (int k = 0; k < Mathf.Min(3, pool.Count); k++) opts.Add(pool[k]);
                for (int i = opts.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    var tmp = opts[i]; opts[i] = opts[j]; opts[j] = tmp;
                }
                qs.Add(new QuizQuestion { correct = card, options = opts, isReverse = false });

                // Hard mode: tambah soal terbalik (nama → pilih aksara) selang-seling
                if (hardMode)
                {
                    var revOpts = new List<AksaraCard> { card };
                    var revPool = new List<AksaraCard>(data.cards);
                    revPool.Remove(card);
                    for (int i = revPool.Count - 1; i > 0; i--)
                    {
                        int j = Random.Range(0, i + 1);
                        var tmp = revPool[i]; revPool[i] = revPool[j]; revPool[j] = tmp;
                    }
                    for (int k = 0; k < Mathf.Min(3, revPool.Count); k++) revOpts.Add(revPool[k]);
                    for (int i = revOpts.Count - 1; i > 0; i--)
                    {
                        int j = Random.Range(0, i + 1);
                        var tmp = revOpts[i]; revOpts[i] = revOpts[j]; revOpts[j] = tmp;
                    }
                    qs.Add(new QuizQuestion { correct = card, options = revOpts, isReverse = true });
                }
            }
            return qs;
        }

        private void ShowQuestion(int idx)
        {
            if (idx >= _questions.Count) { ShowResult(); return; }
            var q = _questions[idx];
            if (txtQuizScore)    txtQuizScore.text    = $"Skor: {_score}";
            if (txtQuizProgress) txtQuizProgress.text = $"{idx + 1} / {_questions.Count}";

            if (q.isReverse)
            {
                // Soal terbalik: tampilkan nama latin, pilih aksara yang sesuai
                if (txtQuizQuestion) txtQuizQuestion.text = $"Aksara \"{q.correct.latinName.ToUpper()}\" yang mana?";
                if (imgQuizSprite != null) imgQuizSprite.gameObject.SetActive(false);
                if (txtQuizAksara != null) { txtQuizAksara.gameObject.SetActive(true); txtQuizAksara.text = "?"; }

                for (int i = 0; i < answerButtons.Length; i++)
                {
                    if (answerButtons[i] == null) continue;
                    bool hasOpt = i < q.options.Count;
                    answerButtons[i].gameObject.SetActive(hasOpt);
                    if (hasOpt)
                    {
                        // Reverse: soal latin→aksara — tampilkan AKSARA di jawaban
                        SetAnswerLabel(answerButtons[i], q.options[i], showLatin: false);
                        answerButtons[i].interactable = true;
                        var img = answerButtons[i].GetComponent<Image>();
                        if (img) img.color = new Color(0.2f, 0.4f, 0.7f);
                    }
                }
            }
            else
            {
                // Soal normal: tampilkan aksara, jawaban berupa NAMA LATIN
                if (txtQuizQuestion) txtQuizQuestion.text = "Aksara ini dibaca...?";
                if (imgQuizSprite != null) imgQuizSprite.gameObject.SetActive(false);
                if (txtQuizAksara != null)
                {
                    txtQuizAksara.gameObject.SetActive(true);
                    txtQuizAksara.enabled = true;
                    txtQuizAksara.text = q.correct.aksaraChar;
                    if (sundaneseFont != null) txtQuizAksara.font = sundaneseFont;
                    txtQuizAksara.ForceMeshUpdate(true, true);
                }

                for (int i = 0; i < answerButtons.Length; i++)
                {
                    if (answerButtons[i] == null) continue;
                    bool hasOpt = i < q.options.Count;
                    answerButtons[i].gameObject.SetActive(hasOpt);
                    if (hasOpt)
                    {
                        // showLatin: true — jawaban harus nama Latin bukan aksara lagi
                        SetAnswerLabel(answerButtons[i], q.options[i], showLatin: true);
                        answerButtons[i].interactable = true;
                        var img = answerButtons[i].GetComponent<Image>();
                        if (img) img.color = new Color(0.2f, 0.4f, 0.7f);
                    }
                }

                // Pelafalan: putar audio soal agar murid bisa dengar
                if (_effectiveMode == MiniGameMode.Pelafalan)
                    StartCoroutine(AutoPlayAudio(q.correct.audioFile, q.correct.latinName));
            }
        }

        private void OnAnswerSelected(int optionIdx)
        {
            var q = _questions[_quizIndex];
            if (optionIdx >= q.options.Count) return;

            bool correct = q.options[optionIdx].latinName == q.correct.latinName;
            if (correct) _score++;

            StartCoroutine(FlashAnswer(optionIdx, correct));
        }

        private IEnumerator FlashAnswer(int optionIdx, bool correct)
        {
            for (int i = 0; i < answerButtons.Length; i++)
                if (answerButtons[i] != null) answerButtons[i].interactable = false;

            var img = answerButtons[optionIdx]?.GetComponent<Image>();
            if (img) img.color = correct ? Color.green : Color.red;

            // Also highlight correct answer if wrong
            if (!correct)
            {
                var q = _questions[_quizIndex];
                for (int i = 0; i < answerButtons.Length; i++)
                {
                    if (i < q.options.Count && q.options[i].latinName == q.correct.latinName)
                    {
                        var ci = answerButtons[i]?.GetComponent<Image>();
                        if (ci) ci.color = Color.green;
                    }
                }
            }

            float delay = _effectiveMode == MiniGameMode.Refleksi ? 0.8f : 1.2f;
            yield return new WaitForSecondsRealtime(delay);
            _quizIndex++;
            ShowQuestion(_quizIndex);
        }

        // ── RESULT ────────────────────────────────────────
        private void ShowResult()
        {
            ShowPanel(panelResult);
            int total = _questions.Count;
            float pct = total > 0 ? (float)_score / total : 0f;

            // Threshold berbeda untuk Refleksi (hard mode)
            int stars;
            if (_effectiveMode == MiniGameMode.Refleksi)
                stars = pct >= 0.95f ? 3 : pct >= 0.8f ? 2 : pct >= 0.6f ? 1 : 0;
            else
                stars = pct >= 0.9f ? 3 : pct >= 0.6f ? 2 : pct >= 0.4f ? 1 : 0;

            if (txtResultTitle) txtResultTitle.text = stars >= 2 ? "Bagus sekali!" : stars == 1 ? "Lumayan!" : "Coba lagi ya!";
            if (txtResultScore) txtResultScore.text = $"{_score} / {total} benar";
            if (txtResultStars) txtResultStars.text = stars >= 3 ? "Bintang 3" : stars == 2 ? "Bintang 2" : stars == 1 ? "Bintang 1" : "Belum lulus";

            if (data != null)
            {
                switch (_effectiveMode)
                {
                    case MiniGameMode.ReadBaca:
                        if (stars > 0) ChapterManager.Instance?.CompleteSubActivity(data.chapterID, "baca", stars, _score, total);
                        break;
                    case MiniGameMode.Pelafalan:
                        // Tandai selesai tanpa syarat bintang (mic mungkin tidak tersedia)
                        ChapterManager.Instance?.CompleteSubActivity(data.chapterID, "pelafalan", Mathf.Max(1, stars), _score, total);
                        break;
                    case MiniGameMode.Refleksi:
                        if (stars > 0)
                        {
                            ChapterManager.Instance?.CompleteSubActivity(data.chapterID, "refleksi", stars, _score, total);
                            ChapterManager.Instance?.CompleteChapter(data.chapterID, stars);
                        }
                        break;
                    default:
                        // Mode Standard (MG_Swara lama): anggap semua sub-aktivitas selesai
                        if (stars > 0)
                        {
                            ChapterManager.Instance?.CompleteSubActivity(data.chapterID, "baca",      stars, _score, total);
                            ChapterManager.Instance?.CompleteSubActivity(data.chapterID, "tulis",     stars, _score, total);
                            ChapterManager.Instance?.CompleteSubActivity(data.chapterID, "pelafalan", stars, _score, total);
                            ChapterManager.Instance?.CompleteSubActivity(data.chapterID, "refleksi",  stars, _score, total);
                            ChapterManager.Instance?.CompleteChapter(data.chapterID, stars);
                        }
                        break;
                }
            }

            SaveFeedbackData(stars);

            // Refleksi: max 2x retry
            bool canRetry = stars == 0;
            if (_effectiveMode == MiniGameMode.Refleksi)
                canRetry = canRetry && _retryCount < 2;
            btnRetry?.gameObject.SetActive(canRetry);

            // Kalau tombol Retry disembunyikan, tengahkan tombol Kembali (biar tak mepet kanan)
            if (btnBack != null)
            {
                var rtb = btnBack.GetComponent<RectTransform>();
                if (rtb != null)
                {
                    if (canRetry) { rtb.anchorMin = new Vector2(0.55f, 0.22f); rtb.anchorMax = new Vector2(0.90f, 0.35f); }
                    else          { rtb.anchorMin = new Vector2(0.32f, 0.20f); rtb.anchorMax = new Vector2(0.68f, 0.35f); }
                    rtb.offsetMin = Vector2.zero; rtb.offsetMax = Vector2.zero;
                }
            }

            // Setelah Refleksi berhasil → tombol Kembali menuju LevelSummary
            if (_effectiveMode == MiniGameMode.Refleksi && stars > 0 && data != null)
            {
                SetBtnLabel(btnBack, "Lihat Ringkasan Level →");
                btnBack?.onClick.RemoveAllListeners();
                btnBack?.onClick.AddListener(GoToLevelSummary);
            }
        }

        private void GoToLevelSummary()
        {
            if (data != null)
                PlayerPrefs.SetInt("SummaryChapterID", (int)data.chapterID);
            PlayerPrefs.Save();
            SceneManager.LoadScene("LevelSummary");
        }

        private void RetryQuiz()
        {
            _retryCount++;
            switch (_effectiveMode)
            {
                case MiniGameMode.Refleksi:   StartQuizHardMode();    break;
                case MiniGameMode.TraceOnly:  StartTraceFromIntro();  break;
                case MiniGameMode.Pelafalan:  StartVoiceQuiz();       break;
                default:                      StartQuiz();             break;
            }
        }

        private void ExitEarly()
        {
            SaveFeedbackData(stars: -1, exitedEarly: true);
            SceneManager.LoadScene("00_Sekolah");
        }

        private void BackToOverworld()
        {
            // Skor sudah disimpan di ShowResult via CompleteChapter
            SceneManager.LoadScene("00_Sekolah");
        }

        private void SaveFeedbackData(int stars, bool exitedEarly = false)
        {
            PlayerPrefs.SetInt("mg_pending_feedback", 1);
            PlayerPrefs.SetInt("mg_last_stars",  stars);
            PlayerPrefs.SetInt("mg_last_score",  _score);
            PlayerPrefs.SetInt("mg_last_total",  _questions != null ? _questions.Count : 0);
            PlayerPrefs.SetInt("mg_exited_early", exitedEarly ? 1 : 0);
            PlayerPrefs.SetString("mg_last_chapter", data != null ? data.chapterID.ToString() : "");
            PlayerPrefs.Save();
        }

        // ── COMBINE LETTERS (Ujian Final) ─────────────────
        private void StartCombine()
        {
            _combineScore = 0;
            _combineIndex = 0;
            _combineQuestions = BuildCombineQuestions();
            ShowPanel(panelCombine);
            ShowCombineQuestion(_combineIndex);
        }

        private List<CombineQuestion> BuildCombineQuestions()
        {
            // Cari pasangan konsonan + rarangken dari cards
            // Rarangken: card dengan aksaraChar panjang > 1 (gabungan 2 karakter unicode)
            var consonants  = new List<AksaraCard>();
            var rarangkens  = new List<AksaraCard>();

            // Aksara Swara (vokal mandiri) tidak boleh dijadikan "konsonan" dalam soal combine
            var swaraNames = new System.Collections.Generic.HashSet<string>
                { "a", "i", "u", "e", "eu", "é", "o" };

            foreach (var c in data.cards)
            {
                // Komposit (konsonan + rarangken) = panjang > 1 karakter unicode
                if (c.aksaraChar.Length > 1)
                    rarangkens.Add(c);
                else if (!swaraNames.Contains(c.latinName.ToLower()))
                    consonants.Add(c);
            }

            // Jika tidak ada rarangken spesifik, gunakan semua kartu untuk quiz biasa
            if (rarangkens.Count == 0 || consonants.Count == 0)
            {
                // Fallback: quiz standar (aksara → baca)
                var fallback = new List<CombineQuestion>();
                foreach (var c in data.cards)
                {
                    var q = new CombineQuestion { partA = c, rarangken = null, combined = c.latinName };
                    q.wrongOptions = new List<string>();
                    foreach (var other in data.cards)
                        if (other.latinName != c.latinName && q.wrongOptions.Count < 3)
                            q.wrongOptions.Add(other.latinName);
                    fallback.Add(q);
                }
                return fallback;
            }

            var qs = new List<CombineQuestion>();
            // Buat soal: konsonan + rarangken = ?
            foreach (var cons in consonants)
            {
                foreach (var rang in rarangkens)
                {
                    // Vokal rarangken diambil dari audioFile (sudah berisi "i", "u", "e", "o", "eu")
                    string vowel = !string.IsNullOrEmpty(rang.audioFile) ? rang.audioFile : ExtractVowel(rang.latinName);
                    // Hasil gabungan: "ka" + "i" = "ki" (ganti akhir 'a')
                    string combined = cons.latinName.Length > 0
                        ? cons.latinName.Substring(0, cons.latinName.Length - 1) + vowel
                        : cons.latinName + vowel;

                    var q = new CombineQuestion
                    {
                        partA    = cons,
                        rarangken = rang,
                        combined  = combined,
                        wrongOptions = new List<string>()
                    };

                    // Generate opsi salah
                    foreach (var other in consonants)
                    {
                        if (other.latinName != cons.latinName && q.wrongOptions.Count < 3)
                        {
                            string w = other.latinName.Length > 0
                                ? other.latinName.Substring(0, other.latinName.Length - 1) + vowel
                                : other.latinName + vowel;
                            q.wrongOptions.Add(w);
                        }
                    }
                    qs.Add(q);

                    if (qs.Count >= 12) goto Done; // batasi 12 soal
                }
            }
            Done:
            // Shuffle
            for (int i = qs.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (qs[i], qs[j]) = (qs[j], qs[i]);
            }
            return qs;
        }

        private string ExtractVowel(string rarangkenName)
        {
            // "panghulu (i)" → "i", "panyuku (u)" → "u", dst.
            int open  = rarangkenName.IndexOf('(');
            int close = rarangkenName.IndexOf(')');
            if (open >= 0 && close > open)
                return rarangkenName.Substring(open + 1, close - open - 1).Trim();
            // Fallback: ambil bagian setelah spasi terakhir
            var parts = rarangkenName.Split(' ');
            return parts[parts.Length - 1];
        }

        private void ShowCombineQuestion(int idx)
        {
            if (_combineQuestions == null || idx >= _combineQuestions.Count)
            {
                ShowCombineResult();
                return;
            }
            var q = _combineQuestions[idx];

            // Tampilkan komponen A (konsonan) — selalu gunakan font
            if (imgCombineA != null) imgCombineA.gameObject.SetActive(false);
            if (txtCombineA != null) { txtCombineA.gameObject.SetActive(true); txtCombineA.text = q.partA.aksaraChar; }

            // Tampilkan komponen B (rarangken) — selalu gunakan font
            if (q.rarangken != null)
            {
                if (imgCombineB != null) imgCombineB.gameObject.SetActive(false);
                if (txtCombineB != null) { txtCombineB.gameObject.SetActive(true); txtCombineB.text = q.rarangken.aksaraChar; }
            }

            if (txtCombineScore)    txtCombineScore.text    = $"Skor: {_combineScore}";
            if (txtCombineProgress) txtCombineProgress.text = $"{idx + 1} / {_combineQuestions.Count}";

            // Susun pilihan jawaban (1 benar + max 3 salah)
            var allOptions = new List<string> { q.combined };
            allOptions.AddRange(q.wrongOptions);
            // Shuffle opsi
            for (int i = allOptions.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (allOptions[i], allOptions[j]) = (allOptions[j], allOptions[i]);
            }

            for (int i = 0; i < combineAnswerBtns.Length; i++)
            {
                if (combineAnswerBtns[i] == null) continue;
                bool show = i < allOptions.Count;
                combineAnswerBtns[i].gameObject.SetActive(show);
                if (show)
                {
                    var lbl = combineAnswerBtns[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (lbl) lbl.text = allOptions[i].ToUpper();
                    combineAnswerBtns[i].interactable = true;
                    var img = combineAnswerBtns[i].GetComponent<Image>();
                    if (img) img.color = new Color(0.2f, 0.5f, 0.8f);
                }
            }
        }

        private void OnCombineAnswerSelected(int btnIdx)
        {
            if (_combineQuestions == null || _combineIndex >= _combineQuestions.Count) return;
            var q = _combineQuestions[_combineIndex];

            // Ambil teks jawaban yang dipilih
            var lbl = combineAnswerBtns[btnIdx]?.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl == null) return;
            bool correct = lbl.text.ToLower() == q.combined.ToLower();

            foreach (var b in combineAnswerBtns) if (b) b.interactable = false;

            var img = combineAnswerBtns[btnIdx]?.GetComponent<Image>();
            if (img) img.color = correct ? Color.green : Color.red;

            if (correct) _combineScore++;
            else
            {
                // Highlight jawaban benar
                foreach (var b in combineAnswerBtns)
                {
                    var bl = b?.GetComponentInChildren<TextMeshProUGUI>();
                    if (bl != null && bl.text.ToLower() == q.combined.ToLower())
                    {
                        var bi = b.GetComponent<Image>();
                        if (bi) bi.color = Color.green;
                    }
                }
            }

            StartCoroutine(NextCombineAfterDelay(correct ? 1f : 2f));
        }

        private System.Collections.IEnumerator NextCombineAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            _combineIndex++;
            ShowCombineQuestion(_combineIndex);
        }

        private void ShowCombineResult()
        {
            ShowPanel(panelResult);
            int total = _combineQuestions?.Count ?? 1;
            float pct = (float)_combineScore / total;
            int stars = pct >= 0.9f ? 3 : pct >= 0.6f ? 2 : pct >= 0.4f ? 1 : 0;

            if (txtResultTitle) txtResultTitle.text = stars >= 2 ? "Luar biasa!" : stars == 1 ? "Cukup baik!" : "Latihan lagi ya!";
            if (txtResultScore) txtResultScore.text = $"{_combineScore} / {total} benar";
            if (txtResultStars) txtResultStars.text = stars >= 3 ? "Bintang 3" : stars == 2 ? "Bintang 2" : stars == 1 ? "Bintang 1" : "Belum lulus";

            if (stars > 0 && data != null)
                ChapterManager.Instance?.CompleteChapter(data.chapterID, stars);

            _score = _combineScore;
            // Sync _questions agar SaveFeedbackData bisa melaporkan total yang benar
            _questions = new List<QuizQuestion>();
            for (int i = 0; i < (_combineQuestions?.Count ?? 0); i++)
                _questions.Add(new QuizQuestion());
            SaveFeedbackData(stars);
            btnRetry?.gameObject.SetActive(stars == 0);
        }

        // ── HELPERS ───────────────────────────────────────
private void ShowPanel(GameObject target)
        {
            foreach (var p in new[] { panelIntro, panelLearn, panelTrace, panelQuiz, panelResult, panelCombine, panelVoiceQuiz })
                if (p != null) p.SetActive(p == target);
        }
    

private void StartTrace()
        {
            // Mode CombineLetters langsung ke panel gabung setelah belajar
            if (_effectiveMode == MiniGameMode.CombineLetters || (data != null && data.gameMode == MiniGameMode.CombineLetters))
            {
                StartCombine();
                return;
            }
            _traceIndex = 0;
            ShowPanel(panelTrace);
            tracePanel?.SetSessionCards(data?.cards, sundaneseFont);
            ShowTraceCard(_traceIndex);
        }

        private void ShowTraceCard(int idx)
        {
            if (data == null || data.cards.Count == 0) return;
            tracePanel?.Setup(data.cards[idx], idx, data.cards.Count,
                NextTrace, TrackLastCardAndStartQuiz);
        }

        private void NextTrace()
        {
            if (data == null) return;
            // Track aksara yang baru saja selesai di-trace
            if (_traceIndex < data.cards.Count)
            {
                PlayerPrefs.SetInt($"trace_{data.cards[_traceIndex].latinName}_practiced", 1);
                PlayerPrefs.Save();
            }
            if (_traceIndex < data.cards.Count - 1)
            {
                _traceIndex++;
                ShowTraceCard(_traceIndex);
            }
        }

        private void TrackLastCardAndStartQuiz()
        {
            if (data != null && _traceIndex < data.cards.Count)
            {
                PlayerPrefs.SetInt($"trace_{data.cards[_traceIndex].latinName}_practiced", 1);
                PlayerPrefs.Save();
            }

            if (_effectiveMode == MiniGameMode.TraceOnly)
            {
                ShowTraceOnlyResult();
                return;
            }
            if (_effectiveMode == MiniGameMode.VoiceQuiz || (data != null && data.gameMode == MiniGameMode.VoiceQuiz))
                StartVoiceQuiz();
            else
                StartQuiz();
        }

        private void ShowTraceOnlyResult()
        {
            int count    = data?.cards.Count ?? 0;
            int exact    = tracePanel != null ? tracePanel.BestMatchCount    : 0; // dikenali sempurna
            int accepted = tracePanel != null ? tracePanel.TotalTracedCount  : count;
            int approx   = accepted - exact; // diterima tapi bukan best-match

            _questions = new List<QuizQuestion>();
            for (int i = 0; i < count; i++) _questions.Add(new QuizQuestion());
            _score = exact;

            // Stars: berdasarkan jumlah yang dikenali sempurna
            float pct   = count > 0 ? (float)exact / count : 0f;
            int   stars = pct >= 0.7f ? 3 : pct >= 0.4f ? 2 : exact > 0 ? 1 : 0;
            // Bonus: jika semua selesai (ditulis semua) minimal 1 bintang
            if (stars == 0 && accepted == count) stars = 1;

            ShowPanel(panelResult);

            string title = stars >= 3 ? "Latihan Menulis Selesai!" : stars >= 2 ? "Hampir Sempurna!" : "Terus Berlatih!";
            if (txtResultTitle) txtResultTitle.text = title;

            string scoreText = $"Sempurna: {exact}/{count}  |  Cukup Baik: {approx}/{count}";
            if (txtResultScore) txtResultScore.text = scoreText;

            if (txtResultStars) txtResultStars.text = stars >= 3 ? "Bintang 3" : stars == 2 ? "Bintang 2" : stars == 1 ? "Bintang 1" : "Belum lulus";
            if (btnRetry != null) btnRetry.gameObject.SetActive(stars == 0);

            if (data != null)
                ChapterManager.Instance?.CompleteSubActivity(data.chapterID, "tulis", stars, exact, count);

            SaveFeedbackData(stars);
        }

        // ── VOICE QUIZ ────────────────────────────────────────────────────────
        private void StartVoiceQuiz()
        {
            _voiceIndex = 0;
            _voiceScore = 0;
            if (data == null || data.cards.Count == 0) { ShowResult(); return; }
            if (panelVoiceQuiz != null) ShowPanel(panelVoiceQuiz);
            ShowVoiceQuestion(_voiceIndex);
        }

        private void ShowVoiceQuestion(int idx)
        {
            if (data == null || idx >= data.cards.Count) { ShowVoiceResult(); return; }
            voiceQuizPanel?.Setup(data.cards[idx], idx, data.cards.Count, OnVoiceAnswered);
        }

        private void OnVoiceAnswered(bool correct)
        {
            if (correct) _voiceScore++;
            _voiceIndex++;
            ShowVoiceQuestion(_voiceIndex);
        }

        private void ShowVoiceResult()
        {
            _score = _voiceScore;
            _questions = new System.Collections.Generic.List<QuizQuestion>();
            // Tambahkan dummy questions agar ShowResult bisa hitung total
            for (int i = 0; i < (data?.cards.Count ?? 0); i++)
                _questions.Add(new QuizQuestion());
            ShowResult();
        }
}
}
