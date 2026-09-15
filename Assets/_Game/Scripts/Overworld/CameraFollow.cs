using UnityEngine;

namespace HayuNgaksara
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);

        private void LateUpdate()
        {
            if (target == null)
            {
                if (PlayerTopDown.Instance != null)
                    target = PlayerTopDown.Instance.transform;
                return;
            }
            Vector3 desired = target.position + offset;
            desired.z = offset.z;
            transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
        }
    }
}
