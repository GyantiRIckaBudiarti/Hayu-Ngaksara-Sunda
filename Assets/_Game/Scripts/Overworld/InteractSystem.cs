using System.Collections.Generic;
using UnityEngine;

namespace HayuNgaksara
{
    public interface IInteractable
    {
        string InteractLabel { get; }
        void Interact(PlayerTopDown player);
    }

    public class InteractSystem : MonoBehaviour
    {
        public static InteractSystem Instance { get; private set; }

        [SerializeField] private float interactRadius = 1.2f;
        [SerializeField] private LayerMask interactLayer;

        private IInteractable _nearest;
        private readonly Collider2D[] _hits = new Collider2D[8];

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (PlayerTopDown.Instance == null) return;

            int count = Physics2D.OverlapCircleNonAlloc(
                PlayerTopDown.Instance.transform.position,
                interactRadius, _hits, interactLayer);

            IInteractable best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var interactable = _hits[i].GetComponent<IInteractable>();
                if (interactable == null) continue;
                float d = (_hits[i].transform.position - PlayerTopDown.Instance.transform.position).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = interactable; }
            }
            _nearest = best;

            // Show/hide interact hint UI
            InteractHintUI.Instance?.Show(_nearest != null, _nearest?.InteractLabel);
        }

        public void TryInteract()
        {
            _nearest?.Interact(PlayerTopDown.Instance);
        }
    }
}
