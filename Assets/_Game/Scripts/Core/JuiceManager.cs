using System.Collections;
using UnityEngine;
using TMPro;

namespace HayuNgaksara
{
    public class JuiceManager : MonoBehaviour
    {
        public static JuiceManager Instance { get; private set; }

        [SerializeField] private bool   reduceEffects = false;
        [SerializeField] private GameObject scorePopupPrefab;

        private Camera _mainCamera;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _mainCamera = Camera.main;
        }

        public void ScreenShake(float duration, float magnitude)
        {
            if (reduceEffects || _mainCamera == null) return;
            StartCoroutine(ShakeCoroutine(duration, magnitude));
        }

        private IEnumerator ShakeCoroutine(float duration, float magnitude)
        {
            Vector3 original = _mainCamera.transform.localPosition;
            float   t        = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float remaining = 1f - t / duration;
                _mainCamera.transform.localPosition = original + (Vector3)Random.insideUnitCircle * magnitude * remaining;
                yield return null;
            }
            _mainCamera.transform.localPosition = original;
        }

        public void ScorePopup(Vector3 worldPos, int amount)
        {
            if (reduceEffects || scorePopupPrefab == null) return;
            var go  = Instantiate(scorePopupPrefab, worldPos, Quaternion.identity);
            var tmp = go.GetComponentInChildren<TextMeshPro>();
            if (tmp != null) tmp.text = $"+{amount}";
            StartCoroutine(FloatAndFade(go));
        }

        private IEnumerator FloatAndFade(GameObject go)
        {
            var tmp  = go.GetComponentInChildren<TextMeshPro>();
            float t  = 0f;
            float dur = 1f;
            Vector3 startPos = go.transform.position;
            while (t < dur)
            {
                t += Time.deltaTime;
                float progress = t / dur;
                go.transform.position = startPos + Vector3.up * progress;
                if (tmp != null)
                {
                    var c = tmp.color;
                    c.a = 1f - progress;
                    tmp.color = c;
                }
                yield return null;
            }
            Destroy(go);
        }

        public void CorrectFlash(SpriteRenderer sr)
        {
            if (sr == null) return;
            StartCoroutine(FlashCoroutine(sr, Color.yellow));
        }

        public void WrongShake(Transform target)
        {
            if (target == null) return;
            StartCoroutine(ShakeObject(target));
        }

        private IEnumerator FlashCoroutine(SpriteRenderer sr, Color flashColor)
        {
            Color original = sr.color;
            for (int i = 0; i < 2; i++)
            {
                sr.color = flashColor;
                yield return new WaitForSeconds(0.07f);
                sr.color = original;
                yield return new WaitForSeconds(0.07f);
            }
        }

        private IEnumerator ShakeObject(Transform target)
        {
            Vector3 origin = target.localPosition;
            float   dur    = 0.3f;
            float   t      = 0f;
            float   str    = 0.1f;
            while (t < dur)
            {
                t += Time.deltaTime;
                target.localPosition = origin + (Vector3)Random.insideUnitCircle * str * (1f - t / dur);
                yield return null;
            }
            target.localPosition = origin;
        }

        public void LetterReveal(SpriteRenderer sr)
        {
            if (sr == null) return;
            StartCoroutine(RevealCoroutine(sr));
        }

        private IEnumerator RevealCoroutine(SpriteRenderer sr)
        {
            Color c = sr.color;
            c.a = 0f;
            sr.color = c;

            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                c.a = t / 0.3f;
                sr.color = c;
                yield return null;
            }
            c.a = 1f;
            sr.color = c;

            // Scale punch
            Vector3 orig = sr.transform.localScale;
            t = 0f;
            while (t < 0.15f)
            {
                t += Time.deltaTime;
                float s = 1f + Mathf.Sin(t / 0.15f * Mathf.PI) * 0.2f;
                sr.transform.localScale = orig * s;
                yield return null;
            }
            sr.transform.localScale = orig;
        }
    }
}
