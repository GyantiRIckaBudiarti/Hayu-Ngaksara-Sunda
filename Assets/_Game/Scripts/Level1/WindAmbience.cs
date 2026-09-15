using System.Collections;
using UnityEngine;

namespace HayuNgaksara
{
    public class WindAmbience : MonoBehaviour
    {
        [Header("Partikel Daun (ParticleSystem di Inspector)")]
        [SerializeField] private ParticleSystem leafParticles;

        [Header("Awan")]
        [SerializeField] private GameObject[] cloudPrefabs;
        [SerializeField] private float        cloudSpawnX   = -12f;
        [SerializeField] private float        cloudExitX    =  14f;
        [SerializeField] private float        cloudMinY     =  1f;
        [SerializeField] private float        cloudMaxY     =  4f;
        [SerializeField] private float        cloudMinSpeed = 0.2f;
        [SerializeField] private float        cloudMaxSpeed = 0.5f;
        [SerializeField] private float        cloudInterval = 4f;

        private void Start()
        {
            if (leafParticles != null) leafParticles.Play();
            StartCoroutine(SpawnClouds());
        }

        private IEnumerator SpawnClouds()
        {
            while (true)
            {
                yield return new WaitForSeconds(cloudInterval);
                SpawnCloud();
            }
        }

        private void SpawnCloud()
        {
            if (cloudPrefabs == null || cloudPrefabs.Length == 0) return;
            int     idx   = Random.Range(0, cloudPrefabs.Length);
            float   y     = Random.Range(cloudMinY, cloudMaxY);
            float   speed = Random.Range(cloudMinSpeed, cloudMaxSpeed);
            var     go    = Instantiate(cloudPrefabs[idx], new Vector3(cloudSpawnX, y, 5f), Quaternion.identity);
            StartCoroutine(MoveCloud(go, speed));
        }

        private IEnumerator MoveCloud(GameObject cloud, float speed)
        {
            while (cloud != null && cloud.transform.position.x < cloudExitX)
            {
                cloud.transform.position += Vector3.right * speed * Time.deltaTime;
                yield return null;
            }
            if (cloud != null) Destroy(cloud);
        }
    }
}
