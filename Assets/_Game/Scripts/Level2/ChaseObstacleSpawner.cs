using System.Collections;
using UnityEngine;

namespace HayuNgaksara
{
    public class ChaseObstacleSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject[] obstaclePrefabs;
        [SerializeField] private float        spawnX        = 12f;
        [SerializeField] private float        spawnY        = -1.5f;
        [SerializeField] private float        initialSpeed  = 3f;
        [SerializeField] private float        maxSpeed      = 6f;
        [SerializeField] private float        speedIncrement = 0.1f;
        [SerializeField] private float        speedInterval  = 5f;
        [SerializeField] private float        initialInterval = 2.5f;

        private float _currentSpeed;
        private float _spawnInterval;
        private bool  _running;

        public float CurrentSpeed => _currentSpeed;

        public void StartSpawning()
        {
            _currentSpeed   = initialSpeed;
            _spawnInterval  = initialInterval;
            _running        = true;
            StartCoroutine(SpawnLoop());
            StartCoroutine(SpeedUpLoop());
        }

        public void StopSpawning()
        {
            _running = false;
            StopAllCoroutines();
        }

        private IEnumerator SpawnLoop()
        {
            while (_running)
            {
                yield return new WaitForSecondsRealtime(_spawnInterval);
                if (!_running) break;

                int idx = Random.Range(0, obstaclePrefabs.Length);
                var go  = Instantiate(obstaclePrefabs[idx], new Vector3(spawnX, spawnY, 0), Quaternion.identity);
                go.GetComponent<ChaseObstacle>()?.Init(_currentSpeed);
            }
        }

        private IEnumerator SpeedUpLoop()
        {
            while (_running)
            {
                yield return new WaitForSecondsRealtime(speedInterval);
                _currentSpeed  = Mathf.Min(_currentSpeed + speedIncrement, maxSpeed);
                _spawnInterval = Mathf.Max(_spawnInterval - 0.05f, 1f);
            }
        }
    }
}
