using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HayuNgaksara
{
    public class InteractHintUI : MonoBehaviour
    {
        public static InteractHintUI Instance { get; private set; }

        [SerializeField] private GameObject      panel;
        [SerializeField] private TextMeshProUGUI txtLabel;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            panel?.SetActive(false);

            // Hint hanya informatif → JANGAN pernah menghalangi klik (mis. ke opsi dialog).
            if (panel != null)
                foreach (var g in panel.GetComponentsInChildren<Graphic>(true))
                    g.raycastTarget = false;
        }

        public void Show(bool visible, string label = null)
        {
            // Saat dialog/daftar pilihan terbuka, sembunyikan hint (biar tak menutupi & tak salah fokus).
            if (DialogSystem.Instance != null &&
                (DialogSystem.Instance.IsOpen || DialogSystem.Instance.ChoiceOpen))
                visible = false;

            panel?.SetActive(visible);
            if (txtLabel != null && label != null)
                txtLabel.text = $"[E] {label}";
        }
    }
}
