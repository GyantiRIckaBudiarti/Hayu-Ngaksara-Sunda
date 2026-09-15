using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HayuNgaksara
{
    /// <summary>
    /// Menampilkan dialog interaktif "naik level" saat pemain kembali ke overworld
    /// setelah lulus Refleksi sebuah chapter. Dipicu oleh PlayerPrefs "pending_levelup"
    /// yang di-set ChapterManager.CompleteChapter. Self-bootstrap di scene 00_Sekolah.
    /// </summary>
    public class LevelUpAnnouncer : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded += (scene, _) => TrySpawn(scene.name);
            TrySpawn(SceneManager.GetActiveScene().name);
        }

        private static void TrySpawn(string sceneName)
        {
            if (sceneName != "00_Sekolah") return;
            if (PlayerPrefs.GetInt("pending_levelup", -1) < 0) return;
            if (FindObjectOfType<LevelUpAnnouncer>() != null) return;
            new GameObject("LevelUpAnnouncer").AddComponent<LevelUpAnnouncer>();
        }

        private void Start()
        {
            StartCoroutine(AnnounceWhenReady());
        }

        private IEnumerator AnnounceWhenReady()
        {
            int nextInt = PlayerPrefs.GetInt("pending_levelup", -1);
            if (nextInt < 0) { Destroy(gameObject); yield break; }
            PlayerPrefs.DeleteKey("pending_levelup");
            PlayerPrefs.Save();

            // Tunggu sistem dialog & pemain siap (maks ~5 detik)
            float t = 0f;
            while ((DialogSystem.Instance == null || PlayerTopDown.Instance == null) && t < 5f)
            {
                t += 0.1f;
                yield return new WaitForSecondsRealtime(0.1f);
            }
            if (DialogSystem.Instance == null) { Destroy(gameObject); yield break; }

            // Jangan bentrok dengan tutorial/cutscene pembuka
            while (DialogSystem.Instance.IsOpen)
                yield return new WaitForSecondsRealtime(0.2f);
            yield return new WaitForSecondsRealtime(0.4f);

            var next     = (ChapterID)nextInt;
            var prev     = (ChapterID)(nextInt - 1);
            string mc    = PlayerTopDown.Instance?.PlayerName ?? "kamu";
            string prevN = DisplayName(prev);
            string nextN = DisplayName(next);

            var lines = new List<DialogLine>();

            if (next == ChapterID.UjianFinal)
            {
                lines.Add(Line($"Hebat, {mc}! Anjeun geus ngawasa {prevN}!",
                               $"Hebat, {mc}! Kamu sudah menaklukkan {prevN}!"));
                lines.Add(Line("Sadaya aksara dasar geus dikawasa. Kari hiji tangtangan panungtung...",
                               "Semua aksara dasar sudah kamu kuasai. Kini tinggal satu tantangan terakhir..."));
                lines.Add(Line("UJIAN AKHIR! Panggih Bu Guru pikeun ngagabungkeun konsonan jeung rarangkén. Sumanget!",
                               "UJIAN AKHIR! Temui Bu Guru untuk menggabungkan konsonan dengan rarangken. Semangat!"));
            }
            else
            {
                lines.Add(Line($"Wilujeng, {mc}! Anjeun lulus Réfléksi {prevN}!",
                               $"Selamat, {mc}! Kamu lulus Refleksi {prevN}!"));
                lines.Add(Line($"Anjeun naek ka tingkat satuluyna: {nextN}. Aya aksara-aksara anyar nu ngantosan.",
                               $"Kamu naik ke level berikutnya: {nextN}. Ada aksara-aksara baru yang menantimu."));
                lines.Add(Line($"Panggih deui Ibu Sinta, Bapak Ucup, jeung Ibu Nabila pikeun diajar {nextN}, terus Réfléksi jeung Bu Guru. Hayu terus ngaksara!",
                               $"Temui lagi Ibu Sinta, Bapak Ucup, dan Ibu Nabila untuk belajar {nextN}, lalu Refleksi dengan Bu Guru. Ayo terus ngaksara!"));
            }

            DialogSystem.Instance.Show(lines, () => Destroy(gameObject));
        }

        private DialogLine Line(string sunda, string indo)
            => new DialogLine { speakerName = "Bapak Jajang", text = UITheme.Bi(sunda, indo) };

        private static string DisplayName(ChapterID id)
        {
            switch (id)
            {
                case ChapterID.Swara:      return "Aksara Swara";
                case ChapterID.Ngalagena1: return "Ngalagena I";
                case ChapterID.Ngalagena2: return "Ngalagena II";
                case ChapterID.Rarangken:  return "Rarangken";
                case ChapterID.UjianFinal: return "Ujian Akhir";
                default:                   return id.ToString();
            }
        }
    }
}
