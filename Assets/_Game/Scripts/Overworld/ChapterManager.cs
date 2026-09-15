using System;
using System.Collections.Generic;
using UnityEngine;

namespace HayuNgaksara
{
    public enum ChapterID { Tutorial = 0, Swara = 1, Ngalagena1 = 2, Ngalagena2 = 3, Rarangken = 4, UjianFinal = 5 }

    [Serializable]
    public class ChapterProgress
    {
        public ChapterID id;
        public bool      unlocked;
        public bool      completed;
        public int       stars;        // 0-3
        public int       highScore;
    }

    public class ChapterManager : MonoBehaviour
    {
        public static ChapterManager Instance { get; private set; }

        public event Action<ChapterID> OnChapterCompleted;
        public event Action<ChapterID> OnChapterUnlocked;
        public event Action<ChapterID> OnSubActivityCompleted;

        private Dictionary<ChapterID, ChapterProgress> _chapters;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            transform.SetParent(null); // must be root for DDOL
            DontDestroyOnLoad(gameObject);
            LoadProgress();
        }

        private void LoadProgress()
        {
            _chapters = new Dictionary<ChapterID, ChapterProgress>();
            foreach (ChapterID id in Enum.GetValues(typeof(ChapterID)))
            {
                var p = new ChapterProgress { id = id };
                p.unlocked  = PlayerPrefs.GetInt($"ch_{(int)id}_unlock", id == ChapterID.Tutorial ? 1 : 0) == 1;
                p.completed = PlayerPrefs.GetInt($"ch_{(int)id}_done", 0) == 1;
                p.stars     = PlayerPrefs.GetInt($"ch_{(int)id}_stars", 0);
                _chapters[id] = p;
            }
        }

        public ChapterProgress GetProgress(ChapterID id)
            => _chapters.TryGetValue(id, out var p) ? p : null;

        public bool IsUnlocked(ChapterID id)
            => _chapters.TryGetValue(id, out var p) && p.unlocked;

        public bool IsCompleted(ChapterID id)
            => _chapters.TryGetValue(id, out var p) && p.completed;

        public void CompleteChapter(ChapterID id, int stars)
        {
            if (!_chapters.TryGetValue(id, out var p)) return;
            p.completed = true;
            if (stars > p.stars) p.stars = stars;
            SaveChapter(p);
            OnChapterCompleted?.Invoke(id);

            // Auto-unlock next chapter. Ujian Akhir DIHAPUS → Rarangken adalah level terakhir.
            ChapterID next = id + 1;
            if (next == ChapterID.UjianFinal) return; // tidak ada level setelah Rarangken
            if (_chapters.TryGetValue(next, out var np) && !np.unlocked)
            {
                np.unlocked = true;
                SaveChapter(np);
                OnChapterUnlocked?.Invoke(next);

                // Tandai agar overworld menampilkan dialog naik level saat kembali
                PlayerPrefs.SetInt("pending_levelup", (int)next);
                PlayerPrefs.Save();
            }
        }

        private void SaveChapter(ChapterProgress p)
        {
            PlayerPrefs.SetInt($"ch_{(int)p.id}_unlock", p.unlocked  ? 1 : 0);
            PlayerPrefs.SetInt($"ch_{(int)p.id}_done",   p.completed ? 1 : 0);
            PlayerPrefs.SetInt($"ch_{(int)p.id}_stars",  p.stars);
            PlayerPrefs.Save();
        }

        public void CompleteSubActivity(ChapterID id, string activity, int stars = 0, int score = 0, int total = 0)
        {
            PlayerPrefs.SetInt($"ch_{(int)id}_{activity}_done", 1);
            // Simpan bintang terbaik (tidak turun jika mengulang)
            int prev = PlayerPrefs.GetInt($"ch_{(int)id}_{activity}_stars", 0);
            if (stars > prev) PlayerPrefs.SetInt($"ch_{(int)id}_{activity}_stars", stars);
            if (score > 0)   PlayerPrefs.SetInt($"ch_{(int)id}_{activity}_score", score);
            if (total > 0)   PlayerPrefs.SetInt($"ch_{(int)id}_{activity}_total", total);
            PlayerPrefs.Save();
            OnSubActivityCompleted?.Invoke(id);
        }

        public bool IsSubActivityDone(ChapterID id, string activity)
            => PlayerPrefs.GetInt($"ch_{(int)id}_{activity}_done", 0) == 1;

        public int GetSubActivityStars(ChapterID id, string activity)
            => PlayerPrefs.GetInt($"ch_{(int)id}_{activity}_stars", 0);

        public int GetSubActivityScore(ChapterID id, string activity)
            => PlayerPrefs.GetInt($"ch_{(int)id}_{activity}_score", 0);

        public int GetSubActivityTotal(ChapterID id, string activity)
            => PlayerPrefs.GetInt($"ch_{(int)id}_{activity}_total", 0);

        public bool AllSubActivitiesDone(ChapterID id)
            => IsSubActivityDone(id, "baca") && IsSubActivityDone(id, "tulis")
            && IsSubActivityDone(id, "pelafalan") && IsSubActivityDone(id, "refleksi");

        public int GetTotalStars(ChapterID id)
            => GetSubActivityStars(id, "baca")
             + GetSubActivityStars(id, "tulis")
             + GetSubActivityStars(id, "pelafalan")
             + GetSubActivityStars(id, "refleksi");

        public char GetGrade(ChapterID id)
        {
            int t = GetTotalStars(id);
            if (t >= 11) return 'A';
            if (t >= 9)  return 'B';
            if (t >= 7)  return 'C';
            if (t >= 5)  return 'D';
            return 'E';
        }

        // Chapter aktif = chapter terbuka pertama yang BELUM selesai.
        // (Refleksi dihapus — naik level lewat Jajang setelah 3 latihan selesai.)
        public ChapterID GetCurrentActiveChapter()
        {
            var order = new[] { ChapterID.Swara, ChapterID.Ngalagena1, ChapterID.Ngalagena2, ChapterID.Rarangken };
            foreach (var id in order)
                if (IsUnlocked(id) && !IsCompleted(id))
                    return id;
            return ChapterID.UjianFinal; // sinyal: semua level selesai
        }

        // 3 latihan (baca/tulis/pelafalan) chapter ini sudah selesai → siap naik level via Jajang.
        public bool AllThreeLatihanDone(ChapterID id)
            => IsSubActivityDone(id, "baca")
            && IsSubActivityDone(id, "tulis")
            && IsSubActivityDone(id, "pelafalan");

        // Kembalikan nama NPC yang harus dikunjungi sesuai objektif saat ini
        public string GetObjectiveTargetNPC()
        {
            if (!IsCompleted(ChapterID.Tutorial)) return "Jajang";

            var order = new[] { ChapterID.Swara, ChapterID.Ngalagena1, ChapterID.Ngalagena2, ChapterID.Rarangken };
            foreach (var id in order)
            {
                if (!IsUnlocked(id) || IsCompleted(id)) continue;
                if (!IsSubActivityDone(id, "baca"))      return "Sinta";
                if (!IsSubActivityDone(id, "tulis"))     return "Ucup";
                if (!IsSubActivityDone(id, "pelafalan")) return "Nabila";
                return "Jajang"; // 3 latihan beres → temui Jajang untuk NAIK LEVEL
            }
            return "";
        }

        public void ResetAll()
        {
            string[] activities = { "baca", "tulis", "pelafalan", "refleksi", "combine" };
            foreach (ChapterID id in Enum.GetValues(typeof(ChapterID)))
            {
                PlayerPrefs.DeleteKey($"ch_{(int)id}_unlock");
                PlayerPrefs.DeleteKey($"ch_{(int)id}_done");
                PlayerPrefs.DeleteKey($"ch_{(int)id}_stars");
                foreach (var act in activities)
                {
                    PlayerPrefs.DeleteKey($"ch_{(int)id}_{act}_done");
                    PlayerPrefs.DeleteKey($"ch_{(int)id}_{act}_stars");
                    PlayerPrefs.DeleteKey($"ch_{(int)id}_{act}_score");
                    PlayerPrefs.DeleteKey($"ch_{(int)id}_{act}_total");
                }
            }
            PlayerPrefs.Save();
            LoadProgress();
        }
    }
}
