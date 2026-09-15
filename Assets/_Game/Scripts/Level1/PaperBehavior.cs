using System;
using System.Collections;
using UnityEngine;

namespace HayuNgaksara
{
    public class PaperBehavior : MonoBehaviour
    {
        public AksaraData AksaraData { get; private set; }
        public bool IsPlaced { get; set; }

        [SerializeField] private float floatAmplitude = 0.3f;
        [SerializeField] private float rotateAngle    = 5f;

        private SpriteRenderer _sr;
        private float _floatSpeed;
        private Vector3 _startPos;
        private bool _isDragging;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        public void Init(AksaraData data)
        {
            AksaraData  = data;
            _floatSpeed = UnityEngine.Random.Range(0.8f, 1.2f);
            _startPos   = transform.position;

            if (data.spriteHuruf != null)
                _sr.sprite = data.spriteHuruf;
        }

        private void Update()
        {
            if (_isDragging || IsPlaced) return;

            float y   = Mathf.Sin(Time.time * _floatSpeed) * floatAmplitude;
            float rot = Mathf.Sin(Time.time * _floatSpeed * 0.7f) * rotateAngle;
            transform.position    = _startPos + new Vector3(0, y, 0);
            transform.eulerAngles = new Vector3(0, 0, rot);
        }

        public void SetDragging(bool isDragging)
        {
            _isDragging = isDragging;
            if (!isDragging)
                _startPos = transform.position;
        }

        public void SetHover(bool hovered)
        {
            StartCoroutine(ScaleTo(hovered ? 1.1f : 1f));
        }

        private IEnumerator ScaleTo(float target)
        {
            Vector3 from = transform.localScale;
            Vector3 to   = Vector3.one * target;
            float   t    = 0f;
            while (t < 0.1f)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(from, to, t / 0.1f);
                yield return null;
            }
            transform.localScale = to;
        }
    }
}
