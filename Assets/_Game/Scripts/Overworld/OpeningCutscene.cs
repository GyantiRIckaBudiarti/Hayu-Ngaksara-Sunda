using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HayuNgaksara
{
    // Cutscene pembuka: player jalan ke Jajang, Jajang menyapa
    // Dijalankan otomatis jika tutorial_done == 0
    public class OpeningCutscene : MonoBehaviour
    {
        public static OpeningCutscene Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Transform jajangTransform;
        [SerializeField] private float walkSpeed = 2.5f;
        [SerializeField] private float stopDistance = 1.5f;

        private bool _played;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            bool done = PlayerPrefs.GetInt("tutorial_done", 0) == 1;
            if (!done && !_played)
                StartCoroutine(PlayCutscene());
        }

        private IEnumerator PlayCutscene()
        {
            _played = true;

            yield return new WaitForSecondsRealtime(0.5f);

            var player = PlayerTopDown.Instance;
            if (player == null) yield break;

            // Auto-find Jajang jika tidak di-assign di Inspector
            if (jajangTransform == null)
            {
                foreach (var go in FindObjectsOfType<GameObject>())
                {
                    if (go.name.ToLower().Contains("jajang"))
                    {
                        jajangTransform = go.transform;
                        break;
                    }
                }
            }

            Vector3 targetPos = jajangTransform != null
                ? jajangTransform.position
                : new Vector3(2f, -3f, 0f);

            // Jika sudah cukup dekat, skip walk
            if (Vector2.Distance(player.transform.position, targetPos) <= stopDistance)
            {
                player.SetCanMove(true);
                TutorialManager.Instance?.StartTutorial();
                yield break;
            }

            player.SetCanMove(false);
            var rb = player.GetComponent<Rigidbody2D>();

            // Walk dengan timeout 5 detik agar tidak stuck selamanya
            float elapsed = 0f;
            float timeout  = 5f;
            while (Vector2.Distance(player.transform.position, targetPos) > stopDistance && elapsed < timeout)
            {
                Vector2 dir = ((Vector2)targetPos - (Vector2)player.transform.position).normalized;
                if (rb != null) rb.linearVelocity = dir * walkSpeed;
                yield return new WaitForSecondsRealtime(0.02f); // ~50fps tick
                elapsed += 0.02f;
            }
            if (rb != null) rb.linearVelocity = Vector2.zero;

            yield return new WaitForSecondsRealtime(0.3f);

            // SELALU restore CanMove
            player.SetCanMove(true);
            TutorialManager.Instance?.StartTutorial();
        }
    }
}
