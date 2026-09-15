using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace HayuNgaksara
{
    /// <summary>
    /// Offline speech recognition via whisper.unity (whisper.cpp).
    /// Setup:
    ///   1. Package sudah terinstall via manifest.json
    ///   2. Taruh ggml-tiny.bin di Assets/StreamingAssets/
    /// </summary>
    public class VoiceRecognitionService : MonoBehaviour
    {
        public static VoiceRecognitionService Instance { get; private set; }

        [SerializeField] private string modelFileName = "Whisper/ggml-tiny.bin";
        [SerializeField] private string language      = "id";

        private MonoBehaviour _whisperMgr;
        private MethodInfo    _initMethod;
        private MethodInfo    _getTextMethod;
        private FieldInfo     _resultField;   // WhisperResult.Result (string)
        private string        _micDevice;
        private AudioClip     _recordingClip;
        private bool          _isReady;

        public bool IsReady => _isReady && !string.IsNullOrEmpty(_micDevice);

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance != null) return;
            var go = new UnityEngine.GameObject("VoiceRecognitionService [Auto]");
            go.AddComponent<VoiceRecognitionService>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start() => StartCoroutine(InitRoutine());

        private IEnumerator InitRoutine()
        {
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
                yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);

            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                Debug.LogWarning("[Whisper] Izin mikrofon ditolak.");
                yield break;
            }

            _micDevice = PickMicDevice();
            if (_micDevice == null) { Debug.LogWarning("[Whisper] Tidak ada mikrofon."); yield break; }

            // Temukan WhisperManager via reflection
            Type whisperType = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                whisperType = asm.GetType("Whisper.WhisperManager");
                if (whisperType != null) break;
            }

            if (whisperType == null)
            {
                Debug.LogWarning("[Whisper] Package tidak ditemukan.");
                yield break;
            }

            _whisperMgr = (MonoBehaviour)gameObject.AddComponent(whisperType);

            // Set properties
            SetProperty(whisperType, "ModelPath",                   modelFileName);
            SetProperty(whisperType, "IsModelPathInStreamingAssets", true);

            // Set fields langsung
            SetField(whisperType, "language",          language);
            SetField(whisperType, "translateToEnglish", false);

            // Cache method & field references
            _initMethod    = whisperType.GetMethod("InitModel",    Type.EmptyTypes);
            _getTextMethod = whisperType.GetMethod("GetTextAsync", new[] { typeof(AudioClip) });

            Type resultType = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                resultType = asm.GetType("Whisper.WhisperResult");
                if (resultType != null) break;
            }
            _resultField = resultType?.GetField("Result",
                BindingFlags.Public | BindingFlags.Instance);

            if (_initMethod == null || _getTextMethod == null)
            {
                Debug.LogError("[Whisper] API tidak cocok.");
                yield break;
            }

            bool done = false, ok = false;
            InitAsync(b => { ok = b; done = true; });
            yield return new WaitUntil(() => done);

            _isReady = ok;
            if (ok) Debug.Log("[Whisper] Siap — model loaded.");
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Perangkat mic terpilih dari PlayerPrefs ("MicDevice"), fallback ke device pertama.</summary>
        private static string PickMicDevice()
        {
            if (Microphone.devices.Length == 0) return null;
            string saved = PlayerPrefs.GetString("MicDevice", "");
            if (!string.IsNullOrEmpty(saved) && Array.IndexOf(Microphone.devices, saved) >= 0)
                return saved;
            return Microphone.devices[0];
        }

        /// <summary>Ganti mic aktif saat runtime (dipanggil dari menu Pengaturan).</summary>
        public void SetMicDevice(string device)
        {
            if (string.IsNullOrEmpty(device)) return;
            if (Array.IndexOf(Microphone.devices, device) < 0) return;
            if (Microphone.IsRecording(_micDevice)) Microphone.End(_micDevice);
            _micDevice = device;
            PlayerPrefs.SetString("MicDevice", device);
            PlayerPrefs.Save();
        }

        public void StartRecording(int durationSec = 3)
        {
            if (!IsReady) { Debug.LogWarning("[Whisper] Belum siap."); return; }
            if (Microphone.IsRecording(_micDevice)) Microphone.End(_micDevice);
            _recordingClip = Microphone.Start(_micDevice, false, durationSec, 16000);
        }

        public IEnumerator StopAndRecognize(Action<string> onResult)
        {
            if (!IsReady || _recordingClip == null) { onResult?.Invoke(""); yield break; }

            Microphone.End(_micDevice);
            var clip = _recordingClip;
            _recordingClip = null;

            string result = "";
            bool   done   = false;
            RecognizeAsync(clip, r => { result = r; done = true; });
            yield return new WaitUntil(() => done);
            onResult?.Invoke(result);
        }

        // ── Internal ─────────────────────────────────────────────────────────

        private async void InitAsync(Action<bool> callback)
        {
            try   { await (Task)_initMethod.Invoke(_whisperMgr, null); callback(true); }
            catch (Exception e) { Debug.LogError($"[Whisper] Init: {e.Message}"); callback(false); }
        }

        private async void RecognizeAsync(AudioClip clip, Action<string> callback)
        {
            try
            {
                var task  = (Task)_getTextMethod.Invoke(_whisperMgr, new object[] { clip });
                await task;

                var taskResultProp = task.GetType().GetProperty("Result");
                var whisperResult  = taskResultProp?.GetValue(task);
                string text        = (_resultField?.GetValue(whisperResult) as string)?.Trim() ?? "";
                Debug.Log($"[Whisper] \"{text}\"");
                callback(text);
            }
            catch (Exception e) { Debug.LogError($"[Whisper] Recognize: {e.Message}"); callback(""); }
        }

        private void SetProperty(Type type, string name, object value)
        {
            var p = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (p != null && p.CanWrite) try { p.SetValue(_whisperMgr, value); } catch { }
        }

        private void SetField(Type type, string name, object value)
        {
            var f = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (f != null) try { f.SetValue(_whisperMgr, value); } catch { }
        }
    }
}
