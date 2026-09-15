using UnityEngine;

namespace HayuNgaksara
{
    public class ChaseObstacle : MonoBehaviour
    {
        private float _speed;

        public void Init(float speed)
        {
            _speed = speed;
        }

        private void Update()
        {
            transform.position += Vector3.left * _speed * Time.deltaTime;
            if (transform.position.x < -14f)
                Destroy(gameObject);
        }
    }
}
