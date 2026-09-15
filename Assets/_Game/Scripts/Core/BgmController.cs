using UnityEngine;

namespace HayuNgaksara
{
    /// <summary>
    /// Pemutar BGM global (bebas-royalti, loop). Self-bootstrap — tak perlu setup scene.
    /// Musik ada di Resources/Audio/bgm_loop. Volume mengikuti PlayerPrefs "MusicVolume"
    /// (dikontrol dari menu Pengaturan) dikali basis lembut agar tidak mengganggu.
    /// </summary>
    public class BgmController : MonoBehaviour
    {
        public static BgmController Instance { get; private set; }

        private AudioSource _src;
        private const float BaseVol = 0.5f;   // basis agar BGM lembut di latar

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("BgmController");
            go.AddComponent<BgmController>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            var clip = Resources.Load<AudioClip>("Audio/bgm_loop");
            _src = gameObject.AddComponent<AudioSource>();
            _src.clip        = clip;
            _src.loop        = true;
            _src.playOnAwake = false;
            _src.volume      = CurrentVol();
            if (clip != null) _src.Play();
        }

        private static float CurrentVol()
            => Mathf.Clamp01(PlayerPrefs.GetFloat("MusicVolume", 0.8f)) * BaseVol;

        private void Update()
        {
            // Sinkron dengan slider Musik di menu Pengaturan (murah: satu GetFloat).
            float target = CurrentVol();
            if (!Mathf.Approximately(_src.volume, target))
                _src.volume = Mathf.MoveTowards(_src.volume, target, Time.unscaledDeltaTime * 1.5f);
        }
    }
}
