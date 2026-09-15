using System.Collections;
using UnityEngine;
using TMPro;

namespace HayuNgaksara
{
    // Popup "Wah nemu aksara KA!" tipe reaksi kecil
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private TextMeshPro tmp;
        [SerializeField] private float riseSpeed  = 1f;
        [SerializeField] private float lifetime   = 1.5f;
        [SerializeField] private AnimationCurve alphaCurve;

        public static FloatingText Spawn(string text, Vector3 worldPos, Color color)
        {
            var go = new GameObject("FloatingText");
            go.transform.position = worldPos + Vector3.up * 0.3f;
            var ft  = go.AddComponent<FloatingText>();
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text      = text;
            tmp.color     = color;
            tmp.fontSize  = 3f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.sortingOrder = 20;
            ft.tmp = tmp;
            ft.alphaCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
            return ft;
        }

        private IEnumerator Start()
        {
            float t = 0f;
            var startPos = transform.position;
            while (t < lifetime)
            {
                t += Time.deltaTime;
                float frac = t / lifetime;
                transform.position = startPos + Vector3.up * riseSpeed * t;
                if (tmp != null)
                {
                    var c = tmp.color;
                    c.a = alphaCurve.Evaluate(frac);
                    tmp.color = c;
                }
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
