using System;
using System.Collections;
using UnityEngine;

namespace HayuNgaksara
{
    public class ChaseManager : MonoBehaviour
    {
        public static event Action<bool> OnChaseEnd;

        [Header("References")]
        [SerializeField] private ChaseBackground       background;
        [SerializeField] private ChaseObstacleSpawner  spawner;
        [SerializeField] private ChaseJajang           jajang;
        [SerializeField] private ChaseCatchBar         catchBar;
        [SerializeField] private AndreController       andre;

        [SerializeField] private int maxLives = 3;

        private int  _lives;
        private bool _running;

        private void Start()
        {
            jajang.OnHitObstacle += HandleHit;
            StartChase();
        }

        private void OnDestroy()
        {
            jajang.OnHitObstacle -= HandleHit;
        }

        private void StartChase()
        {
            _lives   = maxLives;
            _running = true;
            spawner.StartSpawning();
            StartCoroutine(ProgressLoop());
        }

        private IEnumerator ProgressLoop()
        {
            while (_running)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                if (!_running) break;
                catchBar.AddProgress();
                background.ScrollSpeed = spawner.CurrentSpeed;

                if (catchBar.IsFull)
                    EndChase(true);
            }
        }

        private void HandleHit()
        {
            if (!_running) return;
            _lives--;
            catchBar.LoseProgress();
            GameManager.Instance?.LoseLife();

            if (_lives <= 0)
                EndChase(false);
        }

        private void EndChase(bool won)
        {
            _running = false;
            spawner.StopSpawning();

            if (won)
                andre?.StopAndTurnSheepish();

            StartCoroutine(ShowResult(won));
        }

        private IEnumerator ShowResult(bool won)
        {
            yield return new WaitForSecondsRealtime(1.5f);
            OnChaseEnd?.Invoke(won);
            SceneTransitionManager.Instance?.LoadScene("10_Level2_Game");
        }
    }
}
