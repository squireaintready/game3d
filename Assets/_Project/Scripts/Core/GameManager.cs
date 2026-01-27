using UnityEngine;
using TowerDefense.Data;

// For Projectile class
using TowerDefense;

namespace TowerDefense.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private WaveConfig waveConfig;
        [SerializeField] private int startingCurrency = 500;
        [SerializeField] private int startingLives = 20;

        [Header("References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private EconomyManager economyManager;

        public GameState CurrentState { get; private set; } = GameState.MainMenu;
        public Difficulty CurrentDifficulty { get; private set; } = Difficulty.Normal;
        public int CurrentLives { get; private set; }
        public int CurrentWave => waveManager?.CurrentWave ?? 0;
        public WaveConfig WaveConfig => waveConfig;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void OnEnable()
        {
            GameEvents.OnEnemyReachedGoal += HandleEnemyReachedGoal;
            GameEvents.OnWaveComplete += HandleWaveComplete;
        }

        private void OnDisable()
        {
            GameEvents.OnEnemyReachedGoal -= HandleEnemyReachedGoal;
            GameEvents.OnWaveComplete -= HandleWaveComplete;
        }

        private void Start()
        {
            AutoWireReferences();
            InitializeGame();
        }

        private void AutoWireReferences()
        {
            if (gridManager == null)
                gridManager = FindFirstObjectByType<GridManager>();
            if (waveManager == null)
                waveManager = FindFirstObjectByType<WaveManager>();
            if (economyManager == null)
                economyManager = FindFirstObjectByType<EconomyManager>();
            if (waveConfig == null)
                waveConfig = Resources.Load<WaveConfig>("DefaultWaveConfig");
        }

        public void InitializeGame()
        {
            CurrentLives = startingLives;
            economyManager?.SetCurrency(startingCurrency);
            GameEvents.InvokeLivesChanged(CurrentLives);
        }

        public void StartGame(Difficulty difficulty = Difficulty.Normal)
        {
            Debug.Log($"[GameManager] StartGame called, difficulty={difficulty}, waveManager={waveManager != null}");
            CurrentDifficulty = difficulty;
            CurrentLives = startingLives;
            economyManager?.SetCurrency(startingCurrency);
            GameEvents.InvokeLivesChanged(CurrentLives);
            SetGameState(GameState.Building);
            if (waveManager != null)
            {
                waveManager.StartBuildPhase();
            }
            else
            {
                Debug.LogError("[GameManager] waveManager is null!");
            }
        }

        public void SetGameState(GameState newState)
        {
            if (CurrentState == newState) return;

            CurrentState = newState;
            GameEvents.InvokeGameStateChanged(newState);

            switch (newState)
            {
                case GameState.Building:
                    Time.timeScale = 1f;
                    break;
                case GameState.WaveActive:
                    Time.timeScale = 1f;
                    break;
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                case GameState.GameOver:
                    Time.timeScale = 0f;
                    break;
            }
        }

        public void TogglePause()
        {
            if (CurrentState == GameState.Paused)
            {
                SetGameState(GameState.Building);
            }
            else if (CurrentState == GameState.Building || CurrentState == GameState.WaveActive)
            {
                SetGameState(GameState.Paused);
            }
        }

        public void LoseLife()
        {
            CurrentLives--;
            GameEvents.InvokeLivesChanged(CurrentLives);
            GameEvents.InvokeLifeLost();

            if (CurrentLives <= 0)
            {
                GameOver(false);
            }
        }

        private void HandleEnemyReachedGoal(Enemy enemy)
        {
            LoseLife();
        }

        private void HandleWaveComplete(int wave)
        {
            int bonus = waveConfig.GetRoundSurvivalBonus(wave);
            economyManager?.AddCurrency(bonus);

            if (wave >= waveConfig.totalWaves)
            {
                GameOver(true);
            }
            else
            {
                SetGameState(GameState.Building);
                waveManager?.StartBuildPhase();
            }
        }

        private void GameOver(bool victory)
        {
            SetGameState(GameState.GameOver);

            if (victory)
            {
                GameEvents.InvokeGameWin();
            }
            else
            {
                GameEvents.InvokeGameLose();
            }
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;

            // Reset wave manager (clears enemies, resets wave count)
            waveManager?.ResetState();

            // Clear all towers from the grid
            ClearAllTowers();

            // Clear any projectiles in flight
            ClearAllProjectiles();

            // Reset currency and lives
            CurrentLives = startingLives;
            economyManager?.SetCurrency(startingCurrency);
            GameEvents.InvokeLivesChanged(CurrentLives);

            // Start fresh
            SetGameState(GameState.Building);
            waveManager?.StartBuildPhase();

            Debug.Log("[GameManager] Game reset complete");
        }

        private void ClearAllProjectiles()
        {
            var projectiles = FindObjectsByType<Projectile>(FindObjectsSortMode.None);
            foreach (var proj in projectiles)
            {
                if (proj != null)
                {
                    Destroy(proj.gameObject);
                }
            }
            Debug.Log($"[GameManager] Cleared {projectiles.Length} projectiles");
        }

        private void ClearAllTowers()
        {
            if (gridManager == null) return;

            var allTowers = gridManager.GetAllTowers();
            foreach (var tower in allTowers)
            {
                if (tower != null)
                {
                    // Remove from grid
                    var gridPos = gridManager.WorldToGrid(tower.transform.position);
                    gridManager.RemoveTower(gridPos);

                    // Destroy the tower object
                    Destroy(tower.gameObject);
                }
            }

            Debug.Log($"[GameManager] Cleared {allTowers.Count} towers");
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        public DifficultySettings GetCurrentDifficultySettings()
        {
            return waveConfig.GetDifficultySettings(CurrentDifficulty);
        }
    }
}
