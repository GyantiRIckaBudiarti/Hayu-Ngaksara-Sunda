using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HayuNgaksara
{
    public enum TransitionType
    {
        Fade,
        SlideLeft,
        SlideRight,
        IrisWipe
    }

    public class SceneTransitionManager : MonoBehaviour
    {
        public static SceneTransitionManager Instance { get; private set; }

        public static event Action OnTransitionStart;
        public static event Action OnTransitionComplete;

        [SerializeField] private float transitionDuration = 0.5f;

        private Canvas _canvas;
        private Image _overlay;
        private bool _isTransitioning;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            SetupCanvas();
        }

        private void SetupCanvas()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9999;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            gameObject.AddComponent<GraphicRaycaster>();

            var overlayGO = new GameObject("Overlay");
            overlayGO.transform.SetParent(_canvas.transform, false);
            _overlay = overlayGO.AddComponent<Image>();
            _overlay.color = Color.black;
            _overlay.raycastTarget = false;

            var rect = _overlay.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            SetOverlayAlpha(0f);
        }

        public void LoadScene(string sceneName, TransitionType type = TransitionType.Fade)
        {
            if (_isTransitioning) return;
            StartCoroutine(TransitionCoroutine(sceneName, -1, type));
        }

        public void LoadScene(int sceneIndex, TransitionType type = TransitionType.Fade)
        {
            if (_isTransitioning) return;
            StartCoroutine(TransitionCoroutine(null, sceneIndex, type));
        }

        public void ReloadCurrentScene()
        {
            LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private IEnumerator TransitionCoroutine(string sceneName, int sceneIndex, TransitionType type)
        {
            _isTransitioning = true;
            OnTransitionStart?.Invoke();

            yield return StartCoroutine(TransitionOut(type));

            AsyncOperation asyncLoad = sceneName != null
                ? SceneManager.LoadSceneAsync(sceneName)
                : SceneManager.LoadSceneAsync(sceneIndex);

            asyncLoad.allowSceneActivation = false;
            while (asyncLoad.progress < 0.9f)
                yield return null;
            asyncLoad.allowSceneActivation = true;
            yield return asyncLoad;

            yield return StartCoroutine(TransitionIn(type));

            _isTransitioning = false;
            OnTransitionComplete?.Invoke();
        }

        private IEnumerator TransitionOut(TransitionType type)
        {
            float t = 0f;
            if (type == TransitionType.Fade)
            {
                while (t < transitionDuration)
                {
                    t += Time.deltaTime;
                    SetOverlayAlpha(Mathf.Clamp01(t / transitionDuration));
                    yield return null;
                }
                SetOverlayAlpha(1f);
            }
            else if (type == TransitionType.SlideLeft)
            {
                var rect = _overlay.rectTransform;
                Vector2 startPos = new Vector2(-Screen.width, 0);
                Vector2 endPos   = Vector2.zero;
                SetOverlayAlpha(1f);
                while (t < transitionDuration)
                {
                    t += Time.deltaTime;
                    rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t / transitionDuration);
                    yield return null;
                }
                rect.anchoredPosition = endPos;
            }
        }

        private IEnumerator TransitionIn(TransitionType type)
        {
            float t = 0f;
            if (type == TransitionType.Fade)
            {
                while (t < transitionDuration)
                {
                    t += Time.deltaTime;
                    SetOverlayAlpha(1f - Mathf.Clamp01(t / transitionDuration));
                    yield return null;
                }
                SetOverlayAlpha(0f);
            }
            else if (type == TransitionType.SlideLeft)
            {
                var rect = _overlay.rectTransform;
                Vector2 startPos = Vector2.zero;
                Vector2 endPos   = new Vector2(Screen.width, 0);
                while (t < transitionDuration)
                {
                    t += Time.deltaTime;
                    rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t / transitionDuration);
                    yield return null;
                }
                rect.anchoredPosition = startPos;
                SetOverlayAlpha(0f);
            }
        }

        private void SetOverlayAlpha(float alpha)
        {
            var c = _overlay.color;
            c.a = alpha;
            _overlay.color = c;
        }
    }
}
