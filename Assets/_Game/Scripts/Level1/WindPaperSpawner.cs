using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HayuNgaksara
{
    [System.Serializable]
    public class SpawnerSettings
    {
        public float spawnAreaXMin   = -7f;
        public float spawnAreaXMax   =  7f;
        public float spawnAreaYMin   = -3f;
        public float spawnAreaYMax   =  3f;
        public float delayAntarSpawn = 0.3f;
        public bool  spawnBertahap   = true;
    }

    public class WindPaperSpawner : MonoBehaviour
    {
        public static event Action<AksaraData> OnPaperPickedUp;
        public static event Action             OnAllPapersPickedUp;

        [SerializeField] private GameObject         paperPrefab;
        [SerializeField] private SpawnerSettings    settings;

        private List<AksaraData>    _aksaraList;
        private List<PaperBehavior> _spawnedPapers = new List<PaperBehavior>();
        private int                 _pickedCount;

        public void Setup(List<AksaraData> aksara)
        {
            _aksaraList = aksara;
        }

        public void SpawnAll()
        {
            _pickedCount = 0;
            _spawnedPapers.Clear();

            if (settings.spawnBertahap)
                StartCoroutine(SpawnBertahap());
            else
                foreach (var aksara in _aksaraList)
                    SpawnOne(aksara);
        }

        private IEnumerator SpawnBertahap()
        {
            foreach (var aksara in _aksaraList)
            {
                SpawnOne(aksara);
                yield return new WaitForSecondsRealtime(settings.delayAntarSpawn);
            }
        }

        private void SpawnOne(AksaraData aksara)
        {
            float x  = UnityEngine.Random.Range(settings.spawnAreaXMin, settings.spawnAreaXMax);
            float y  = UnityEngine.Random.Range(settings.spawnAreaYMin, settings.spawnAreaYMax);
            var   go = Instantiate(paperPrefab, new Vector3(x, y, 0f), Quaternion.identity);

            var paper = go.GetComponent<PaperBehavior>();
            if (paper != null)
            {
                paper.Init(aksara);
                _spawnedPapers.Add(paper);
            }

            var drag = go.GetComponent<DraggablePaper>();
            if (drag != null)
                drag.OnPickedUp += HandlePickedUp;
        }

        private void HandlePickedUp(AksaraData aksara)
        {
            _pickedCount++;
            OnPaperPickedUp?.Invoke(aksara);

            if (_pickedCount >= _aksaraList.Count)
                OnAllPapersPickedUp?.Invoke();
        }
    }
}
