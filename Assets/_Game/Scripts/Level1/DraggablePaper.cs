using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HayuNgaksara
{
    [RequireComponent(typeof(PaperBehavior))]
    public class DraggablePaper : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        public event Action<AksaraData>           OnDroppedOnSlot;
        public event Action<AksaraData>           OnPickedUp;

        private PaperBehavior  _paper;
        private SpriteRenderer _sr;
        private Vector3        _originalPosition;
        private int            _originalSortOrder;
        private bool           _isDragging;
        private Coroutine      _returnCoroutine;

        private void Awake()
        {
            _paper             = GetComponent<PaperBehavior>();
            _sr                = GetComponent<SpriteRenderer>();
            _originalPosition  = transform.position;
            _originalSortOrder = _sr != null ? _sr.sortingOrder : 0;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_paper.IsPlaced)
                _paper.SetHover(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_paper.IsPlaced)
                _paper.SetHover(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_paper.IsPlaced) return;

            _isDragging = true;
            _paper.SetDragging(true);
            _paper.SetHover(false);

            if (_sr != null) _sr.sortingOrder = 10;
            if (_returnCoroutine != null) StopCoroutine(_returnCoroutine);

            OnPickedUp?.Invoke(_paper.AksaraData);
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayPelafalan(_paper.AksaraData.audioPelafalan);

            StartCoroutine(ScaleTo(1.15f));
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(eventData.position);
            worldPos.z = -1f;
            transform.position = worldPos;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isDragging) return;
            _isDragging = false;
            _paper.SetDragging(false);

            if (_sr != null) _sr.sortingOrder = _originalSortOrder;
            StartCoroutine(ScaleTo(1f));

            Collider2D hit = Physics2D.OverlapCircle(transform.position, 1f);
            if (hit != null)
            {
                var slot = hit.GetComponent<Slot>();
                if (slot != null && !slot.IsOccupied)
                {
                    OnDroppedOnSlot?.Invoke(_paper.AksaraData);
                    return;
                }
            }

            ReturnToOrigin();
        }

        public void ReturnToOrigin()
        {
            if (_returnCoroutine != null) StopCoroutine(_returnCoroutine);
            _returnCoroutine = StartCoroutine(MoveToOrigin());
        }

        private IEnumerator MoveToOrigin()
        {
            Vector3 from = transform.position;
            float   t    = 0f;
            float   dur  = 0.4f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float eased = 1f - Mathf.Pow(1f - t / dur, 3f); // ease out cubic
                transform.position = Vector3.Lerp(from, _originalPosition, eased);
                yield return null;
            }
            transform.position = _originalPosition;
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
