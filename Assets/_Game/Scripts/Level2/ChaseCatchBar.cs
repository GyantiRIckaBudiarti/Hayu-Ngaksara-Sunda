using UnityEngine;
using UnityEngine.UI;

namespace HayuNgaksara
{
    public class ChaseCatchBar : MonoBehaviour
    {
        [SerializeField] private Image fillBar;
        [SerializeField] private float fillSpeed = 0.05f;
        [SerializeField] private float losePenalty = 0.15f;

        private float _progress;

        public bool IsFull => _progress >= 1f;

        public void AddProgress()
        {
            _progress = Mathf.Min(1f, _progress + fillSpeed);
            UpdateBar();
        }

        public void LoseProgress()
        {
            _progress = Mathf.Max(0f, _progress - losePenalty);
            UpdateBar();
        }

        public void Reset()
        {
            _progress = 0f;
            UpdateBar();
        }

        private void UpdateBar()
        {
            if (fillBar != null) fillBar.fillAmount = _progress;
        }
    }
}
