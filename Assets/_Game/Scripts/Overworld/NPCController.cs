using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace HayuNgaksara
{
    public class NPCController : MonoBehaviour, IInteractable
    {
        [Header("Identity")]
        [SerializeField] private string    npcName      = "Jajang";
        [SerializeField] private string    npcTitle     = "";   // "Ibu" / "Bapak" (kosong = tanpa gelar)
        [SerializeField] private ChapterID chapterOwned = ChapterID.Tutorial;
        [SerializeField] private Sprite    portrait;

        [Header("Name Label")]
        [SerializeField] private bool  showNameLabel  = true;
        [SerializeField] private float labelOffsetY   = 0.75f;
        [SerializeField] private float labelFontSize  = 2f;

        [Header("Dialog")]
        [SerializeField] private List<DialogLine> dialogLocked;
        [SerializeField] private List<DialogLine> dialogIdle;
        [SerializeField] private List<DialogLine> dialogPostComplete;

        [Header("Skill NPC (abaikan chapterOwned, gunakan chapter aktif)")]
        [SerializeField] private bool         isSkillNPC = false;
        [SerializeField] private MiniGameMode skillMode  = MiniGameMode.ReadBaca;

        [Header("NPC Naik Level (Jajang) — trigger naik level setelah 3 latihan selesai")]
        [SerializeField] private bool         isLevelUpNPC = false;

        [Header("Pilihan Mulai Belajar")]
        [SerializeField] private string choiceYes    = "Oke, ayo kita mulai!";
        [SerializeField] private string choiceNo     = "Maaf, aku belum siap. Nanti ya.";
        [SerializeField] private string choiceRepeat = "Ya, ayo ulangi lagi!";
        [SerializeField] private string choiceNoRepeat = "Tidak, terima kasih.";

        [Header("Face Player")]
        [SerializeField] private bool  facePlayer = true;
        [SerializeField] private float faceRadius = 2f;

        // Registry global: npcName → Transform — dipakai ObjectiveArrow
        public static readonly Dictionary<string, Transform> Registry = new Dictionary<string, Transform>();

        private Animator _anim;
        private static readonly int HashFaceX = Animator.StringToHash("FaceX");
        private static readonly int HashFaceY = Animator.StringToHash("FaceY");

        // Nama tampil dengan gelar (mis. "Ibu Sinta"); kalau gelar kosong pakai nama saja.
        private string DisplayName =>
            string.IsNullOrEmpty(npcTitle) ? npcName : $"{npcTitle} {npcName}";

        public string InteractLabel => $"Bicara dengan {DisplayName}";

        private SpriteRenderer _sr;

        private void Awake()
        {
            _anim = GetComponent<Animator>();
            _sr   = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = GetComponentInChildren<SpriteRenderer>();
            if (showNameLabel) CreateNameLabel();
            if (!string.IsNullOrEmpty(npcName))
                Registry[npcName] = transform;
        }

        // Hadapkan NPC ke pemain. Pakai animator (FaceX/FaceY) bila ada controller
        // berarah; kalau sprite statis (tanpa controller), balik horizontal via flipX.
        private void FacePlayerVisual(Transform player)
        {
            if (player == null) return;
            Vector2 d = ((Vector2)(player.position - transform.position)).normalized;
            if (_anim != null && _anim.runtimeAnimatorController != null)
            {
                _anim.SetFloat(HashFaceX, d.x);
                _anim.SetFloat(HashFaceY, d.y);
            }
            else if (_sr != null)
            {
                // sprite default menghadap depan → balik ke sisi pemain
                if (Mathf.Abs(d.x) > 0.15f) _sr.flipX = d.x < 0f;
            }
        }

        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(npcName) && Registry.TryGetValue(npcName, out var t) && t == transform)
                Registry.Remove(npcName);
        }

        private void CreateNameLabel()
        {
            // NPC bisa punya localScale berbeda (mis. 2.0 vs 4.5). Label adalah child,
            // jadi normalnya ikut mengecil/membesar → ukuran nama tak konsisten.
            // Kompensasi: counter-scale label agar world-size selalu sama untuk semua NPC.
            float parentScale = Mathf.Abs(transform.lossyScale.y);
            if (parentScale < 0.0001f) parentScale = 1f;
            float inv         = 1f / parentScale;
            // Ukuran world-efektif TETAP & seragam untuk semua NPC (kebal skala induk).
            // Nilai serialized lama sangat kecil (0.18-0.4) → nama tampak mungil; pakai konstan.
            float worldFont   = 3.6f;

            // Layer shadow: teks gelap sedikit offset → simulasi outline tanpa bergantung shader variant.
            // Offset dinyatakan dalam world (×inv) supaya tetap rapat walau induk berskala besar,
            // jika tidak nama akan terlihat "dobel".
            float shOff = 0.03f * inv; // ~0.03 world
            var goShadow = new GameObject("NPC_NameLabel_Shadow");
            goShadow.transform.SetParent(transform);
            goShadow.transform.localPosition = new Vector3(shOff, labelOffsetY - shOff, 0f);
            goShadow.transform.localRotation = Quaternion.identity;
            goShadow.transform.localScale    = Vector3.one * inv;

            var tmpShadow       = goShadow.AddComponent<TextMeshPro>();
            tmpShadow.text      = DisplayName;
            tmpShadow.fontSize  = worldFont;
            tmpShadow.fontStyle = FontStyles.Bold;
            tmpShadow.alignment = TextAlignmentOptions.Center;
            tmpShadow.color     = new Color(0f, 0f, 0f, 0.85f);

            var mrShadow = goShadow.GetComponent<MeshRenderer>();
            if (mrShadow != null)
            {
                mrShadow.sortingLayerName = "Characters";
                mrShadow.sortingOrder     = 9;
            }

            // Layer utama: teks putih di atas shadow
            var go = new GameObject("NPC_NameLabel");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, labelOffsetY, 0f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale    = Vector3.one * inv;

            var tmp       = go.AddComponent<TextMeshPro>();
            tmp.text      = DisplayName;
            tmp.fontSize  = worldFont;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Color.white;

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerName = "Characters";
                mr.sortingOrder     = 10;
            }
        }

        private void Update()
        {
            if (!facePlayer || PlayerTopDown.Instance == null) return;
            float dist = Vector2.Distance(transform.position, PlayerTopDown.Instance.transform.position);
            if (dist > faceRadius) return;
            FacePlayerVisual(PlayerTopDown.Instance.transform);
        }

        public void Interact(PlayerTopDown player)
        {
            if (DialogSystem.Instance == null) return;

            // Saling menghadap saat ngobrol: MC menghadap NPC, NPC menghadap MC
            player?.FaceTowards(transform.position);
            FacePlayerVisual(player != null ? player.transform : null);

            if (isSkillNPC)
            {
                HandleSkillInteract();
                return;
            }

            // Jajang = pemandu NAIK LEVEL. Kalau 3 latihan level aktif sudah selesai → trigger naik level.
            if (isLevelUpNPC && (ChapterManager.Instance?.IsCompleted(ChapterID.Tutorial) ?? false))
            {
                HandleLevelUpNPC();
                return;
            }

            var chap  = ChapterManager.Instance?.GetProgress(chapterOwned);
            var lines = SelectLines(chap);
            if (lines.Count == 0) return;

            foreach (var l in lines)
                if (l.portrait == null) l.portrait = portrait;

            // Chapter sudah selesai → tawarkan untuk mengulang
            bool offerRepeat = chap != null && chap.completed && chapterOwned != ChapterID.Tutorial;
            if (offerRepeat)
            {
                // Sisipkan sapaan dengan nama MC sebelum dialog post-complete
                string mcName = PlayerTopDown.Instance?.PlayerName ?? "kamu";
                var repeatLines = new System.Collections.Generic.List<DialogLine>(lines);
                repeatLines.Add(new DialogLine
                {
                    speakerName = DisplayName,
                    text        = UITheme.Bi(
                        $"Oh enya, {mcName}... hoyong ngulang deui latihan ieu?",
                        $"Oh iya, {mcName}... apakah kamu ingin mengulang latihan ini lagi?"),
                    portrait    = portrait
                });
                DialogSystem.Instance.ShowWithChoice(repeatLines,
                    new[] { choiceRepeat, choiceNoRepeat },
                    OnRepeatChoiceSelected);
                return;
            }

            // Chapter unlocked, belum selesai → tampilkan pilihan mulai
            bool offerChoice = chap != null && chap.unlocked && !chap.completed
                               && chapterOwned != ChapterID.Tutorial;

            if (offerChoice)
                DialogSystem.Instance.ShowWithChoice(lines,
                    new[] { choiceYes, choiceNo },
                    OnChoiceSelected);
            else
                DialogSystem.Instance.Show(lines, OnDialogFinished);
        }

        // ── Skill NPC logic ───────────────────────────────────────────────
        private void HandleSkillInteract()
        {
            ChapterID activeChapter = ChapterManager.Instance?.GetCurrentActiveChapter() ?? ChapterID.Swara;
            var lines = BuildSkillDialogLines(activeChapter);

            // ── Guru: REFLEKSI DIHILANGKAN ───────────────────────────────
            // Naik level sekarang lewat Jajang setelah 3 latihan (baca/tulis/pelafalan) selesai.
            // (Kode refleksi lama di HandleGuruRefleksi/HandleGuruAllDone dinonaktifkan/di-comment.)
            if (skillMode == MiniGameMode.Refleksi)
            {
                DialogSystem.Instance.Show(new List<DialogLine> { new DialogLine
                {
                    speakerName = DisplayName,
                    text = UITheme.Bi(
                        "Diajar heula Maca, Nulis, jeung Ngucapkeun jeung Ibu Sinta, Pa Ucup, sareng Ibu Nabila. Lamun geus, temui Pa Jajang pikeun naek tingkat!",
                        "Belajar dulu Membaca, Menulis, dan Pelafalan dengan Ibu Sinta, Pak Ucup, dan Ibu Nabila. Kalau sudah, temui Pak Jajang untuk naik level!"),
                    portrait = portrait
                } });
                return;
            }

            // ── Regular skill NPC (Sinta / Ucup / Nabila) ────────────────
            string activityKey = GetActivityKey(skillMode);
            bool activityDone = ChapterManager.Instance?.IsSubActivityDone(activeChapter, activityKey) ?? false;
            string actLabel   = ActivityLabel(skillMode);
            string mcName     = PlayerTopDown.Instance?.PlayerName ?? "kamu";

            lines.Add(new DialogLine
            {
                speakerName = DisplayName,
                text = activityDone
                    ? UITheme.Bi(
                        $"Anjeun parantos réngsé latihan ieu, {mcName}. Bade ngulang, atawa latihan tingkat séjén?",
                        $"Kamu sudah menyelesaikan latihan ini, {mcName}. Mau ulangi, atau berlatih level lain?")
                    : UITheme.Bi(
                        $"Siap latihan {actLabel} {GetChapterDisplayName(activeChapter)}, {mcName}?",
                        $"Siap berlatih {actLabel} {GetChapterDisplayName(activeChapter)}, {mcName}?"),
                portrait = portrait
            });

            var labels  = new List<string>();
            var actions = new List<System.Action>();

            labels.Add((activityDone ? "Ulangi " : "Mulai ") + actLabel + " " + GetChapterDisplayName(activeChapter));
            var curCh = activeChapter;
            actions.Add(() => OpenSkillActivity(curCh));

            foreach (var prev in GetCompletedChaptersBefore(activeChapter))
            {
                var p = prev;
                labels.Add($"Ulang {actLabel} {GetChapterDisplayName(p)}");
                actions.Add(() => OpenSkillActivity(p));
            }

            labels.Add("Tidak sekarang");
            actions.Add(null);

            DialogSystem.Instance.ShowWithChoices(lines, labels,
                (idx) => { if (idx >= 0 && idx < actions.Count) actions[idx]?.Invoke(); });
        }

        // ── NAIK LEVEL via Jajang ─────────────────────────────────────────
        // Dipanggil saat pemain menemui Jajang. Kalau 3 latihan level aktif sudah selesai,
        // munculkan dialog naik level + opsi ke level berikutnya.
        private void HandleLevelUpNPC()
        {
            string mc = PlayerTopDown.Instance?.PlayerName ?? "kamu";
            var cm = ChapterManager.Instance;
            ChapterID active = cm != null ? cm.GetCurrentActiveChapter() : ChapterID.Swara;

            // Semua level sudah selesai
            if (active == ChapterID.UjianFinal)
            {
                DialogSystem.Instance.Show(new List<DialogLine> { new DialogLine {
                    speakerName = DisplayName,
                    text = UITheme.Bi(
                        $"Wilujeng, {mc}! Anjeun parantos ngaréngsékeun sadaya tingkatan Aksara Sunda! Éndah pisan!",
                        $"Selamat, {mc}! Kamu sudah menyelesaikan semua level Aksara Sunda! Hebat sekali!"),
                    portrait = portrait } });
                return;
            }

            string curName = GetChapterDisplayName(active);
            bool ready = cm != null && cm.AllThreeLatihanDone(active);

            if (!ready)
            {
                // Belum siap → arahkan menyelesaikan 3 latihan
                var lines = new List<DialogLine>(dialogIdle);
                foreach (var l in lines) if (l.portrait == null) l.portrait = portrait;
                lines.Add(new DialogLine {
                    speakerName = DisplayName,
                    text = UITheme.Bi(
                        $"Réngsékeun heula Maca (Ibu Sinta), Nulis (Pa Ucup), jeung Ngucapkeun (Ibu Nabila) {curName}, nya {mc}!",
                        $"Selesaikan dulu Membaca (Ibu Sinta), Menulis (Pak Ucup), dan Pelafalan (Ibu Nabila) {curName} ya, {mc}!"),
                    portrait = portrait });
                DialogSystem.Instance.Show(lines);
                return;
            }

            // Siap naik level → dialog naik level + opsi ke level berikutnya
            var upLines = new List<DialogLine> { new DialogLine {
                speakerName = DisplayName,
                text = UITheme.Bi(
                    $"Hébat, {mc}! Anjeun geus tamat Maca, Nulis, jeung Ngucapkeun {curName}! Siap naek tingkat?",
                    $"Hebat, {mc}! Kamu sudah menyelesaikan Membaca, Menulis, dan Pelafalan {curName}! Siap naik level?"),
                portrait = portrait } };

            ChapterID next = active + 1;
            var acCur = active;
            if ((int)next <= (int)ChapterID.Rarangken)
            {
                string nextName = GetChapterDisplayName(next);
                DialogSystem.Instance.ShowWithChoices(upLines,
                    new List<string> { $"Naik ke {nextName}! →", "Nanti dulu" },
                    idx => { if (idx == 0) DoLevelUp(acCur); });
            }
            else
            {
                // Rarangken = level terakhir
                DialogSystem.Instance.ShowWithChoices(upLines,
                    new List<string> { "Selesaikan! 🎉", "Nanti dulu" },
                    idx => { if (idx == 0) DoLevelUp(acCur); });
            }
        }

        private void DoLevelUp(ChapterID chapter)
        {
            ChapterManager.Instance?.CompleteChapter(chapter, 3);
            // Jajang mengumumkan naik level LANGSUNG di sini → cegah LevelUpAnnouncer dobel.
            PlayerPrefs.DeleteKey("pending_levelup");
            PlayerPrefs.Save();

            string mc = PlayerTopDown.Instance?.PlayerName ?? "kamu";
            ChapterID next = chapter + 1;
            if ((int)next <= (int)ChapterID.Rarangken)
            {
                string nextName = GetChapterDisplayName(next);
                DialogSystem.Instance.Show(new List<DialogLine> { new DialogLine {
                    speakerName = DisplayName,
                    text = UITheme.Bi(
                        $"Wilujeng naek ka {nextName}! Ayeuna temui deui Ibu Sinta pikeun mimiti diajar.",
                        $"Selamat naik ke {nextName}! Sekarang temui lagi Ibu Sinta untuk mulai belajar."),
                    portrait = portrait } });
            }
            else
            {
                DialogSystem.Instance.Show(new List<DialogLine> { new DialogLine {
                    speakerName = DisplayName,
                    text = UITheme.Bi(
                        $"Wilujeng, {mc}! Anjeun parantos TAMAT sadaya tingkatan Aksara Sunda! 🎉",
                        $"Selamat, {mc}! Kamu sudah menyelesaikan SEMUA level Aksara Sunda! 🎉"),
                    portrait = portrait } });
            }
        }

        private void HandleGuruRefleksi(List<DialogLine> lines, ChapterID activeChapter)
        {
            string mcName = PlayerTopDown.Instance?.PlayerName ?? "kamu";
            bool bacaDone = ChapterManager.Instance?.IsSubActivityDone(activeChapter, "baca") ?? false;
            bool tulisDone = ChapterManager.Instance?.IsSubActivityDone(activeChapter, "tulis") ?? false;
            bool pelafalanDone = ChapterManager.Instance?.IsSubActivityDone(activeChapter, "pelafalan") ?? false;

            if (!bacaDone || !tulisDone || !pelafalanDone)
            {
                lines.Add(new DialogLine
                {
                    speakerName = DisplayName,
                    text = UITheme.Bi(
                        "Alusna réngsékeun heula diajar jeung Bu Sinta, Pa Ucup, jeung Bu Nabila samemeh Refleksi. Tapi lamun geus siap...",
                        "Sebaiknya selesaikan dulu belajar dengan Sinta, Ucup, dan Nabila sebelum Refleksi. Tapi kalau sudah siap..."),
                    portrait = portrait
                });
            }

            lines.Add(new DialogLine
            {
                speakerName = DisplayName,
                text = UITheme.Bi($"Bade ngalakukeun naon, {mcName}?", $"Mau melakukan apa, {mcName}?"),
                portrait = portrait
            });

            string curName = GetChapterDisplayName(activeChapter);
            var labels  = new List<string>();
            var actions = new List<System.Action>();

            var acCur = activeChapter;
            labels.Add($"Hafal dulu, lalu Refleksi {curName}");
            actions.Add(() => OnGuruChoiceSelected(0, acCur));
            labels.Add($"Langsung Refleksi {curName}");
            actions.Add(() => OnGuruChoiceSelected(1, acCur));
            // (opsi "Ulang Refleksi level sebelumnya" disembunyikan — dialog Guru lebih ringkas)

            labels.Add("Tidak sekarang");
            actions.Add(null);

            DialogSystem.Instance.ShowWithChoices(lines, labels,
                (idx) => { if (idx >= 0 && idx < actions.Count) actions[idx]?.Invoke(); });
        }

        // Ulang ujian (Refleksi) level sebelumnya — langsung ke kuis, lewati sesi hafalan
        private void StartPreviousExam(ChapterID chapter)
        {
            PlayerPrefs.SetInt("RefleksiSkipReview", 1);
            PlayerPrefs.Save();
            OpenSkillActivity(chapter);
        }

        // Semua level (sampai Rarangken) sudah tuntas. Ujian Akhir dihapus → tawarkan
        // mengulang Refleksi level mana pun, atau tidak sekarang.
        private void HandleGuruAllDone(List<DialogLine> lines)
        {
            string mcName = PlayerTopDown.Instance?.PlayerName ?? "kamu";
            lines.Add(new DialogLine
            {
                speakerName = DisplayName,
                text = UITheme.Bi(
                    $"Wilujeng, {mcName}! Anjeun parantos ngaréngsékeun sadaya tingkatan. Éndah pisan!",
                    $"Selamat, {mcName}! Kamu sudah menyelesaikan semua level. Hebat sekali!"),
                portrait = portrait
            });
            lines.Add(new DialogLine
            {
                speakerName = DisplayName,
                text = UITheme.Bi(
                    "Teruskeun latihan sangkan beuki jago. Wilujeng ngaksara Sunda!",
                    "Teruslah berlatih agar semakin mahir. Selamat ngaksara Sunda!"),
                portrait = portrait
            });
            // Opsi "Ulang Refleksi level" disembunyikan → cukup tampilkan dialog penutup.
            DialogSystem.Instance.Show(lines);
        }

        private void OnGuruChoiceSelected(int idx, ChapterID activeChapter)
        {
            PlayerPrefs.SetInt("RefleksiSkipReview", idx == 1 ? 1 : 0);
            PlayerPrefs.Save();
            OpenSkillActivity(activeChapter);
        }

        private void OpenSkillActivity(ChapterID chapter)
        {
            PlayerPrefs.SetString("ActiveNPCTitle", npcTitle);
            PlayerPrefs.SetString("ActiveNPCName",  npcName);
            PlayerPrefs.Save();
            LearningPanelManager.Instance?.OpenChapterWithMode(chapter, skillMode);
        }

        private void OpenSkillActivityWithMode(ChapterID chapter, MiniGameMode modeOverride)
        {
            PlayerPrefs.SetString("ActiveNPCTitle", npcTitle);
            PlayerPrefs.SetString("ActiveNPCName",  npcName);
            PlayerPrefs.Save();
            LearningPanelManager.Instance?.OpenChapterWithMode(chapter, modeOverride);
        }

        // Dialog dengan baris kontekstual di awal (menyebut chapter & aktivitas saat ini)
        private List<DialogLine> BuildSkillDialogLines(ChapterID chapter)
        {
            var lines = new List<DialogLine>(dialogIdle);
            if (lines.Count == 0)
                lines.Add(new DialogLine { speakerName = DisplayName,
                    text = UITheme.Bi("Hayu urang mimitian latihan!", "Ayo mulai latihan!"), portrait = portrait });
            foreach (var l in lines) if (l.portrait == null) l.portrait = portrait;

            string chName = GetChapterDisplayName(chapter);
            string ctx;
            if (skillMode == MiniGameMode.ReadBaca)
                ctx = UITheme.Bi($"Ayeuna urang diajar MACA {chName}!", $"Sekarang kita belajar MEMBACA {chName}!");
            else if (skillMode == MiniGameMode.TraceOnly)
                ctx = UITheme.Bi($"Ayeuna urang latihan NULIS {chName}!", $"Sekarang kita latihan MENULIS {chName}!");
            else if (skillMode == MiniGameMode.Pelafalan)
                ctx = UITheme.Bi($"Ayeuna urang latihan NGUCAPKEUN {chName}!", $"Sekarang kita latihan PELAFALAN {chName}!");
            else if (skillMode == MiniGameMode.Refleksi)
                ctx = UITheme.Bi($"Waktosna RÉFLÉKSI {chName}!", $"Waktunya REFLEKSI {chName}!");
            else
                ctx = UITheme.Bi($"Diajar {chName} babarengan!", $"Belajar {chName} bersama-sama!");

            lines.Insert(0, new DialogLine { speakerName = DisplayName, text = ctx, portrait = portrait });
            return lines;
        }

        // Kembalikan chapter selesai terakhir sebelum 'current' (untuk fitur ulang level sebelumnya)
        private static ChapterID? GetPreviousCompletedChapter(ChapterID current)
        {
            var order = new[] { ChapterID.Swara, ChapterID.Ngalagena1, ChapterID.Ngalagena2, ChapterID.Rarangken };
            int curIdx = System.Array.IndexOf(order, current);
            if (curIdx <= 0) return null;
            for (int i = curIdx - 1; i >= 0; i--)
                if (ChapterManager.Instance?.IsCompleted(order[i]) == true)
                    return order[i];
            return null;
        }

        // Semua chapter yang sudah SELESAI sebelum 'current' (untuk daftar akses level lama)
        private static List<ChapterID> GetCompletedChaptersBefore(ChapterID current)
        {
            var order = new[] { ChapterID.Swara, ChapterID.Ngalagena1, ChapterID.Ngalagena2, ChapterID.Rarangken };
            int curIdx = System.Array.IndexOf(order, current);
            if (curIdx < 0) curIdx = order.Length; // current di luar daftar (UjianFinal) → cek semua
            var result = new List<ChapterID>();
            for (int i = 0; i < curIdx; i++)
                if (ChapterManager.Instance?.IsCompleted(order[i]) == true)
                    result.Add(order[i]);
            return result;
        }

        private static string ActivityLabel(MiniGameMode mode)
        {
            if (mode == MiniGameMode.ReadBaca)  return "Baca";
            if (mode == MiniGameMode.TraceOnly) return "Tulis";
            if (mode == MiniGameMode.Pelafalan) return "Pelafalan";
            return "Latihan";
        }

        private static string GetChapterDisplayName(ChapterID id)
        {
            switch (id)
            {
                case ChapterID.Swara:      return "Aksara Swara";
                case ChapterID.Ngalagena1: return "Ngalagena I";
                case ChapterID.Ngalagena2: return "Ngalagena II";
                case ChapterID.Rarangken:  return "Rarangken";
                case ChapterID.UjianFinal: return "Ujian Akhir";
                default: return id.ToString();
            }
        }

        private string GetActivityKey(MiniGameMode mode)
        {
            if (mode == MiniGameMode.ReadBaca)     return "baca";
            if (mode == MiniGameMode.TraceOnly)    return "tulis";
            if (mode == MiniGameMode.Pelafalan)    return "pelafalan";
            if (mode == MiniGameMode.Refleksi)     return "refleksi";
            if (mode == MiniGameMode.CombineLetters) return "combine";
            return "baca";
        }

        // ── Dipanggil saat pilihan mulai belajar dipilih ─────────────────
        private void OnChoiceSelected(int idx)
        {
            if (idx == 0) // Ya, mulai
                LearningPanelManager.Instance?.OpenChapter(chapterOwned);
            // idx == 1 atau -1: tutup dialog, tidak melakukan apa-apa
        }

        // ── Dipanggil saat pilihan ulang latihan dipilih ─────────────────
        private void OnRepeatChoiceSelected(int idx)
        {
            if (idx == 0) // Ya, ulangi
                LearningPanelManager.Instance?.OpenChapter(chapterOwned);
            // idx == 1: tidak ingin mengulang
        }

        // ── Dipanggil setelah dialog normal selesai (Tutorial) ─────────────
        private void OnDialogFinished()
        {
            if (chapterOwned != ChapterID.Tutorial) return;
            var chap = ChapterManager.Instance?.GetProgress(chapterOwned);
            if (chap != null && chap.unlocked && !chap.completed)
                LearningPanelManager.Instance?.OpenChapter(chapterOwned);
        }

        private List<DialogLine> SelectLines(ChapterProgress chap)
        {
            if (chap == null || !chap.unlocked)
                return dialogLocked.Count > 0 ? dialogLocked : dialogIdle;
            if (chap.completed)
                return dialogPostComplete.Count > 0 ? dialogPostComplete : dialogIdle;
            return dialogIdle;
        }
    }
}
