using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace HayuNgaksara
{
    /// <summary>
    /// Project ini memakai Input System BARU (Player Settings → Active Input Handling = Input System).
    /// EventSystem yang masih memakai <c>StandaloneInputModule</c> (Input lama) akan membaca
    /// <c>UnityEngine.Input.mousePosition</c> TIAP FRAME → melempar InvalidOperationException terus-
    /// menerus (spam konsol → editor freeze / terasa "crash") DAN membuat klik UI mati.
    ///
    /// Fixer ini otomatis mengganti modul lama → <c>InputSystemUIInputModule</c> di setiap scene,
    /// sehingga tak perlu memperbaiki EventSystem satu per satu di banyak scene.
    /// </summary>
    public static class InputModuleFixer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            FixAll();
            SceneManager.sceneLoaded += (scene, mode) => FixAll();
        }

        private static void FixAll()
        {
            foreach (var es in Object.FindObjectsOfType<EventSystem>())
            {
                var legacy = es.GetComponent<StandaloneInputModule>();
                if (legacy != null)
                {
                    legacy.enabled = false;   // hentikan segera agar tak melempar exception frame ini
                    Object.Destroy(legacy);
                }
                if (es.GetComponent<InputSystemUIInputModule>() == null)
                    es.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }
    }
}
