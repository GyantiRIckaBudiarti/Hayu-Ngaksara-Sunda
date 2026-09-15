using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HayuNgaksara
{
    public class ChaseJajang : MonoBehaviour
    {
        public event Action OnHitObstacle;

        [SerializeField] private float jumpForce    = 8f;
        [SerializeField] private float gravity      = -20f;
        [SerializeField] private float groundY      = -1.5f;
        [SerializeField] private LayerMask obstacleLayer;

        private float   _velocityY;
        private bool    _isGrounded;
        private bool    _isDead;
        private Collider2D _col;

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
        }

        private void Update()
        {
            if (_isDead) return;

            var mouse = Mouse.current;
            if (_isGrounded && mouse != null && mouse.leftButton.wasPressedThisFrame)
                Jump();

            _velocityY += gravity * Time.deltaTime;
            var pos = transform.position;
            pos.y += _velocityY * Time.deltaTime;

            if (pos.y <= groundY)
            {
                pos.y      = groundY;
                _velocityY = 0f;
                _isGrounded = true;
            }
            else
            {
                _isGrounded = false;
            }

            transform.position = pos;
        }

        private void Jump()
        {
            _velocityY  = jumpForce;
            _isGrounded = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isDead) return;
            if (((1 << other.gameObject.layer) & obstacleLayer) != 0)
            {
                _isDead = true;
                StartCoroutine(HitSequence());
            }
        }

        private IEnumerator HitSequence()
        {
            OnHitObstacle?.Invoke();
            // Animasi jatuh sederhana
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                transform.eulerAngles = new Vector3(0, 0, Mathf.Lerp(0, -90f, t / 0.5f));
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.5f);
            transform.eulerAngles = Vector3.zero;
            _isDead = false;
        }
    }
}
