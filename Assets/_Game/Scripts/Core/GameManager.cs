using System;
using UnityEngine;
// namespace HayuNgaksara (single namespace)

namespace HayuNgaksara
{
    public enum GameState
    {
        MainMenu,
        Cutscene,
        Playing,
        Paused,
        LevelComplete,
        GameOver
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public static event Action<GameState> OnStateChanged;
        public static event Action<int> OnScoreChanged;
        public static event Action<int> OnLivesChanged;
        public static event Action<string> OnLevelComplete;

        [Header("Database")]
        [SerializeField] private AksaraDatabase aksaraDatabase;

        [Header("Settings")]
        [SerializeField] private int defaultLives = 3;

        private GameState _currentState;
        private int _currentScore;
        private int _currentLives;

        public GameState CurrentState => _currentState;
        public int CurrentScore => _currentScore;
        public int CurrentLives => _currentLives;
        public AksaraDatabase AksaraDatabase => aksaraDatabase;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            _currentLives = defaultLives;
        }

        public void SetState(GameState newState)
        {
            _currentState = newState;
            OnStateChanged?.Invoke(newState);
        }

        public void AddScore(int amount)
        {
            _currentScore += amount;
            OnScoreChanged?.Invoke(_currentScore);
        }

        public void LoseLife()
        {
            _currentLives = Mathf.Max(0, _currentLives - 1);
            OnLivesChanged?.Invoke(_currentLives);

            if (_currentLives <= 0)
                SetState(GameState.GameOver);
        }

        public void ResetLevel()
        {
            _currentLives = defaultLives;
            _currentScore = 0;
            OnLivesChanged?.Invoke(_currentLives);
            OnScoreChanged?.Invoke(_currentScore);
        }

        public void SaveProgress(string levelKey, int bintang)
        {
            int existing = PlayerPrefs.GetInt(levelKey + "_bintang", 0);
            if (bintang > existing)
                PlayerPrefs.SetInt(levelKey + "_bintang", bintang);

            PlayerPrefs.SetInt(levelKey + "_complete", 1);
            PlayerPrefs.Save();

            OnLevelComplete?.Invoke(levelKey);
        }

        public int LoadProgress(string levelKey)
        {
            return PlayerPrefs.GetInt(levelKey + "_bintang", 0);
        }

        public bool IsLevelComplete(string levelKey)
        {
            return PlayerPrefs.GetInt(levelKey + "_complete", 0) == 1;
        }

        public void SaveHighScore(int score)
        {
            if (score > LoadHighScore())
            {
                PlayerPrefs.SetInt("HighScore", score);
                PlayerPrefs.Save();
            }
        }

        public int LoadHighScore()
        {
            return PlayerPrefs.GetInt("HighScore", 0);
        }

        public int HitungBintang(int jumlahKesalahan)
        {
            if (jumlahKesalahan == 0) return 3;
            if (jumlahKesalahan <= 2) return 2;
            return 1;
        }
    }
}
