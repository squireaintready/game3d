using System;
using UnityEngine;

namespace TowerDefense.Core
{
    /// <summary>
    /// Central event system for game-wide communication
    /// </summary>
    public static class GameEvents
    {
        // Game State Events
        public static event Action<GameState> OnGameStateChanged;
        public static event Action OnGameWin;
        public static event Action OnGameLose;

        // Wave Events
        public static event Action<int> OnWaveStart;
        public static event Action<int> OnWaveComplete;
        public static event Action<float> OnBuildPhaseTimerUpdate;
        public static event Action OnBuildPhaseStart;

        // Economy Events
        public static event Action<int> OnCurrencyChanged;
        public static event Action<int> OnCurrencyEarned;
        public static event Action<int> OnCurrencySpent;

        // Lives Events
        public static event Action<int> OnLivesChanged;
        public static event Action OnLifeLost;

        // Tower Events
        public static event Action<Tower> OnTowerPlaced;
        public static event Action<Tower> OnTowerSold;
        public static event Action<Tower> OnTowerSelected;
        public static event Action OnTowerDeselected;
        public static event Action<Tower> OnTowerDestroyed;
        public static event Action<Tower> OnTowerUpgraded;

        // Enemy Events
        public static event Action<Enemy> OnEnemySpawned;
        public static event Action<Enemy> OnEnemyKilled;
        public static event Action<Enemy> OnEnemyReachedGoal;
        public static event Action OnAllEnemiesDefeated;

        // Placement Events
        public static event Action<TowerType> OnTowerTypeSelected;
        public static event Action OnPlacementCancelled;
        public static event Action<Vector2Int, bool> OnPlacementValidation;

        // Undo Events
        public static event Action<bool> OnUndoStateChanged; // true = can undo

        // Invoke methods
        public static void InvokeGameStateChanged(GameState state) => OnGameStateChanged?.Invoke(state);
        public static void InvokeGameWin() => OnGameWin?.Invoke();
        public static void InvokeGameLose() => OnGameLose?.Invoke();

        public static void InvokeWaveStart(int wave) => OnWaveStart?.Invoke(wave);
        public static void InvokeWaveComplete(int wave) => OnWaveComplete?.Invoke(wave);
        public static void InvokeBuildPhaseTimerUpdate(float time) => OnBuildPhaseTimerUpdate?.Invoke(time);
        public static void InvokeBuildPhaseStart() => OnBuildPhaseStart?.Invoke();

        public static void InvokeCurrencyChanged(int amount) => OnCurrencyChanged?.Invoke(amount);
        public static void InvokeCurrencyEarned(int amount) => OnCurrencyEarned?.Invoke(amount);
        public static void InvokeCurrencySpent(int amount) => OnCurrencySpent?.Invoke(amount);

        public static void InvokeLivesChanged(int lives) => OnLivesChanged?.Invoke(lives);
        public static void InvokeLifeLost() => OnLifeLost?.Invoke();

        public static void InvokeTowerPlaced(Tower tower) => OnTowerPlaced?.Invoke(tower);
        public static void InvokeTowerSold(Tower tower) => OnTowerSold?.Invoke(tower);
        public static void InvokeTowerSelected(Tower tower) => OnTowerSelected?.Invoke(tower);
        public static void InvokeTowerDeselected() => OnTowerDeselected?.Invoke();
        public static void InvokeTowerDestroyed(Tower tower) => OnTowerDestroyed?.Invoke(tower);
        public static void InvokeTowerUpgraded(Tower tower) => OnTowerUpgraded?.Invoke(tower);

        public static void InvokeEnemySpawned(Enemy enemy) => OnEnemySpawned?.Invoke(enemy);
        public static void InvokeEnemyKilled(Enemy enemy) => OnEnemyKilled?.Invoke(enemy);
        public static void InvokeEnemyReachedGoal(Enemy enemy) => OnEnemyReachedGoal?.Invoke(enemy);
        public static void InvokeAllEnemiesDefeated() => OnAllEnemiesDefeated?.Invoke();

        public static void InvokeTowerTypeSelected(TowerType type) => OnTowerTypeSelected?.Invoke(type);
        public static void InvokePlacementCancelled() => OnPlacementCancelled?.Invoke();
        public static void InvokePlacementValidation(Vector2Int cell, bool valid) => OnPlacementValidation?.Invoke(cell, valid);

        public static void InvokeUndoStateChanged(bool canUndo) => OnUndoStateChanged?.Invoke(canUndo);
    }

    public enum GameState
    {
        MainMenu,
        Building,
        WaveActive,
        Paused,
        GameOver
    }

    public enum TowerType
    {
        None,
        Wall,
        Archer,
        Mage,
        Cannon
    }

    public enum Difficulty
    {
        Easy,
        Normal,
        Hard
    }
}
