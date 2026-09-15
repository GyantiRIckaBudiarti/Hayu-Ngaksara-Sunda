using UnityEngine;
using UnityEngine.SceneManagement;

namespace HayuNgaksara
{
    /// <summary>
    /// Panah dinamis yang mengikuti player dan menunjuk ke NPC objektif berikutnya.
    /// Auto-spawn saat scene 00_Sekolah dimuat. DDOL.
    /// </summary>
    public class ObjectiveArrow : MonoBehaviour
    {
        public static ObjectiveArrow Instance { get; private set; }

        // ── Auto-bootstrap ────────────────────────────────────────────────
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            TrySpawn(SceneManager.GetActiveScene().name);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode _) => TrySpawn(scene.name);

        private static void TrySpawn(string sceneName)
        {
            if (Instance != null) { Instance.OnSceneChanged(sceneName); return; }
            if (sceneName == "00_Sekolah")
                new GameObject("ObjectiveArrow").AddComponent<ObjectiveArrow>();
        }

        // ── Tuning ────────────────────────────────────────────────────────
        private const float OffsetRadius  = 0.75f;  // jarak panah dari center player
        private const float BobSpeed      = 2.8f;   // kecepatan naik-turun
        private const float BobAmplitude  = 0.10f;  // amplitudo naik-turun
        private const float RotSmooth     = 7f;     // kecepatan smooth rotate (deg/s lerp)
        private const float FadeStartDist = 2.8f;   // mulai fade saat sedekat ini
        private const float HideDist      = 1.3f;   // sembunyikan total saat sedekat ini
        private const float PulseSpeed    = 3.5f;   // kecepatan pulse scale
        private const float PulseAmt      = 0.08f;  // amplitudo pulse scale

        // ── State ─────────────────────────────────────────────────────────
        private SpriteRenderer _sr;
        private Transform _target;
        private string _lastTargetName = "";
        private float _angle;
        private float _bobTime;
        private bool _active;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite           = BuildArrowSprite();
            _sr.sortingLayerName = "Characters";
            _sr.sortingOrder     = 20;
            _sr.color            = new Color(1f, 0.90f, 0.15f, 0f); // mulai transparan
            transform.localScale = Vector3.one * 0.45f;

            _active = SceneManager.GetActiveScene().name == "00_Sekolah";
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneChanged(string sceneName)
        {
            _active = sceneName == "00_Sekolah";
            if (!_active)
            {
                SetAlpha(0f);
                _target = null;
                _lastTargetName = "";
            }
        }

        // ── Update ────────────────────────────────────────────────────────
        private void Update()
        {
            if (!_active || PlayerTopDown.Instance == null) { SetAlpha(0f); return; }

            // Refresh target jika objektif berubah
            string targetName = ChapterManager.Instance?.GetObjectiveTargetNPC() ?? "";
            if (targetName != _lastTargetName)
            {
                _lastTargetName = targetName;
                _target = null;
                if (!string.IsNullOrEmpty(targetName) && NPCController.Registry.TryGetValue(targetName, out var t))
                    _target = t;
            }

            if (_target == null || string.IsNullOrEmpty(_lastTargetName))
            {
                SetAlpha(Mathf.MoveTowards(GetAlpha(), 0f, Time.deltaTime * 4f));
                return;
            }

            Vector2 playerPos = PlayerTopDown.Instance.transform.position;
            Vector2 targetPos = _target.position;
            float dist = Vector2.Distance(playerPos, targetPos);

            // Sembunyikan total saat sangat dekat
            if (dist < HideDist)
            {
                SetAlpha(Mathf.MoveTowards(GetAlpha(), 0f, Time.deltaTime * 6f));
                return;
            }

            // Arah menuju target
            Vector2 dir = (targetPos - playerPos).normalized;

            // Target angle: sprite menghadap atas (0°) = arah Y+
            float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

            // Smooth rotation — tidak kaku
            _angle = Mathf.LerpAngle(_angle, targetAngle, Time.deltaTime * RotSmooth);
            transform.rotation = Quaternion.Euler(0f, 0f, _angle);

            // Bob animation (gerak naik-turun di arah radial)
            _bobTime += Time.deltaTime * BobSpeed;
            float bob = Mathf.Sin(_bobTime) * BobAmplitude;

            // Posisi: dekat player, offset ke arah target
            Vector2 arrowPos = playerPos + dir * (OffsetRadius + bob);
            transform.position = new Vector3(arrowPos.x, arrowPos.y,
                PlayerTopDown.Instance.transform.position.z - 0.1f);

            // Pulse scale
            float pulse = 1f + Mathf.Sin(_bobTime * PulseSpeed) * PulseAmt;
            transform.localScale = Vector3.one * 0.45f * pulse;

            // Alpha: full di kejauhan, fade saat dekat
            float alpha = dist < FadeStartDist
                ? Mathf.InverseLerp(HideDist, FadeStartDist, dist) * 0.92f
                : 0.92f;
            SetAlpha(Mathf.MoveTowards(GetAlpha(), alpha, Time.deltaTime * 5f));
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private float GetAlpha() => _sr.color.a;
        private void SetAlpha(float a)
        {
            var c = _sr.color;
            c.a = a;
            _sr.color = c;
        }

        // ── Buat sprite panah secara prosedural (32×48 px, putih) ─────────
        private static Sprite BuildArrowSprite()
        {
            const int W = 32, H = 48;
            var tex    = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var pixels = new Color32[W * H];

            // Transparent background
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 0, 0, 0);

            Color32 white = new Color32(255, 255, 255, 255);
            int cx = W / 2;

            // Shaft: 8px lebar, bagian bawah (y = 0..H/2-1)
            const int shaftW = 8;
            for (int y = 0; y < H / 2; y++)
                for (int x = cx - shaftW / 2; x < cx + shaftW / 2; x++)
                    if (x >= 0 && x < W) pixels[y * W + x] = white;

            // Arrowhead: segitiga melebar ke bawah, ujung di atas (y = H/2..H-1)
            for (int y = H / 2; y < H; y++)
            {
                float frac    = (float)(y - H / 2) / (H / 2);  // 0 di dasar kepala, 1 di ujung
                int halfSpan  = Mathf.RoundToInt((W / 2f - 1f) * (1f - frac));
                for (int x = cx - halfSpan; x <= cx + halfSpan; x++)
                    if (x >= 0 && x < W) pixels[y * W + x] = white;
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            // Pivot di tengah-bawah shaft agar rotasi dari titik dekat player
            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.18f), 50f);
        }
    }
}
