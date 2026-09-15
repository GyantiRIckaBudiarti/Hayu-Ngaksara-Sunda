using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace HayuNgaksara
{
    /// <summary>
    /// Text-to-Speech fallback via Google Translate TTS (gratis, tanpa API key, Bahasa Indonesia).
    /// Hanya aktif jika pre-recorded AudioClip tidak ditemukan di Resources/Audio/Pelafalan/.
    /// Audio di-cache ke persistentDataPath agar tidak butuh internet setelah pertama kali.
    /// </summary>
    public class TTSService : MonoBehaviour
    {
        public static TTSService Instance { get; private set; }

        [SerializeField] private string language   = "id";
        [SerializeField] private float  speakRate  = 0.85f;
        [SerializeField] private int    timeoutSec = 8;

        private string _cacheDir;

        // Google Translate TTS — gratis, tanpa API key
        private const string TtsUrl = "https://translate.google.com/translate_tts";

        public bool IsOnline { get; private set; } = true;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _cacheDir = Path.Combine(Application.persistentDataPath, "tts_cache");
            if (!Directory.Exists(_cacheDir)) Directory.CreateDirectory(_cacheDir);
        }

        // ── Public API ───────────────────────────────────────────────────────

        public IEnumerator Speak(string text, Action<AudioClip> callback, string lang = null)
        {
            if (string.IsNullOrWhiteSpace(text)) { callback?.Invoke(null); yield break; }

            string useLang   = lang ?? language;
            string cacheKey  = Hash(text + "|" + useLang);
            string cachePath = Path.Combine(_cacheDir, cacheKey + ".mp3");

            if (File.Exists(cachePath))
            {
                yield return LoadFromFile(cachePath, callback);
                yield break;
            }

            // Download raw bytes (DownloadHandlerBuffer agar bisa disimpan ke file)
            string url = BuildUrl(text, useLang);
            using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120 Safari/537.36");
            req.SetRequestHeader("Referer", "https://translate.google.com/");
            req.timeout = timeoutSec;

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success || req.downloadHandler.data == null)
            {
                Debug.LogWarning($"[TTS] Gagal: {req.error}");
                IsOnline = false;
                callback?.Invoke(null);
                yield break;
            }

            IsOnline = true;
            byte[] bytes = req.downloadHandler.data;

            try { File.WriteAllBytes(cachePath, bytes); }
            catch (Exception e) { Debug.LogWarning($"[TTS] Cache write gagal: {e.Message}"); }

            yield return LoadFromFile(cachePath, callback);
        }

        public void ClearCache()
        {
            if (!Directory.Exists(_cacheDir)) return;
            foreach (var f in Directory.GetFiles(_cacheDir, "*.mp3"))
                try { File.Delete(f); } catch { }
        }

        // ── Internal ─────────────────────────────────────────────────────────

        private string BuildUrl(string text, string lang)
        {
            if (text.Length > 200) text = text.Substring(0, 200);
            string rate = speakRate.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            return $"{TtsUrl}?ie=UTF-8&q={UnityWebRequest.EscapeURL(text)}&tl={lang}&client=tw-ob&ttsspeed={rate}";
        }

        private IEnumerator LoadFromFile(string path, Action<AudioClip> callback)
        {
            string uri = new Uri(path).AbsoluteUri;
            using var req = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.MPEG);
            yield return req.SendWebRequest();
            callback?.Invoke(req.result == UnityWebRequest.Result.Success
                ? DownloadHandlerAudioClip.GetContent(req)
                : null);
        }

        private static string Hash(string input)
        {
            using var md5 = MD5.Create();
            byte[] b = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder(32);
            foreach (byte x in b) sb.Append(x.ToString("x2"));
            return sb.ToString();
        }
    }
}
