using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Data;

namespace TowerDefense.Core
{
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private WaveConfig waveConfig;
        [SerializeField] private EnemyData defaultEnemyData;

        [Header("Enemy Prefab")]
        [SerializeField] private GameObject enemyPrefab;

        public int CurrentWave { get; private set; } = 0;
        public float BuildPhaseTimeRemaining { get; private set; }
        public bool IsWaveActive { get; private set; }
        public int EnemiesRemaining => activeEnemies.Count + enemiesToSpawn;

        // Mid-wave start tracking
        private float waveStartTime;
        private const float MIN_TIME_BEFORE_NEXT_WAVE = 5f; // Must wait 5 seconds into wave before sending next
        private const float AUTO_WAVE_INTERVAL = 30f; // Auto-send next wave after 30 seconds

        public bool CanSendNextWave => IsWaveActive && (Time.time - waveStartTime) >= MIN_TIME_BEFORE_NEXT_WAVE;
        public float TimeUntilCanSendNext => IsWaveActive ? Mathf.Max(0, MIN_TIME_BEFORE_NEXT_WAVE - (Time.time - waveStartTime)) : 0;
        public float TimeInCurrentWave => IsWaveActive ? Time.time - waveStartTime : 0;

        private List<Enemy> activeEnemies = new List<Enemy>();
        private int enemiesToSpawn;
        private Coroutine spawnCoroutine;
        private Coroutine buildPhaseCoroutine;
        private Coroutine autoWaveCoroutine;
        private Vector3[] spawnPositions;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            AutoWireReferences();
        }

        private void AutoWireReferences()
        {
            // Get spawn positions from GridManager
            if (GridManager.Instance != null)
            {
                spawnPositions = GridManager.Instance.GetSpawnPositions();
                Debug.Log($"[WaveManager] Got {spawnPositions.Length} spawn positions from GridManager");
            }
            else
            {
                // Fallback: find spawn points by name
                var leftSpawn = GameObject.Find("SpawnPoint_Left");
                var rightSpawn = GameObject.Find("SpawnPoint_Right");
                var oldSpawn = GameObject.Find("SpawnPoint");

                List<Vector3> positions = new List<Vector3>();
                if (leftSpawn != null) positions.Add(leftSpawn.transform.position);
                if (rightSpawn != null) positions.Add(rightSpawn.transform.position);
                if (positions.Count == 0 && oldSpawn != null) positions.Add(oldSpawn.transform.position);

                spawnPositions = positions.ToArray();
            }

            // Load wave config if not assigned
            if (waveConfig == null)
            {
                waveConfig = Resources.Load<WaveConfig>("DefaultWaveConfig");
                if (waveConfig == null)
                {
                    // Try loading from Data folder
                    var configs = Resources.FindObjectsOfTypeAll<WaveConfig>();
                    if (configs.Length > 0)
                        waveConfig = configs[0];
                }
            }

            // Load default enemy data if not assigned
            if (defaultEnemyData == null)
            {
                defaultEnemyData = Resources.Load<EnemyData>("SoldierEnemyData");
                if (defaultEnemyData == null)
                {
                    var enemies = Resources.FindObjectsOfTypeAll<EnemyData>();
                    if (enemies.Length > 0)
                        defaultEnemyData = enemies[0];
                }
            }

            // Find enemy prefab if not assigned
            if (enemyPrefab == null)
            {
                enemyPrefab = Resources.Load<GameObject>("Enemy");
                if (enemyPrefab == null)
                {
                    // Try to find it in the project
                    var prefabs = Resources.FindObjectsOfTypeAll<GameObject>();
                    foreach (var prefab in prefabs)
                    {
                        if (prefab.name == "Enemy" && prefab.GetComponent<Enemy>() != null)
                        {
                            enemyPrefab = prefab;
                            break;
                        }
                    }
                }
            }

            Debug.Log($"[WaveManager] AutoWire: spawnPositions={spawnPositions?.Length ?? 0}, waveConfig={waveConfig != null}, enemyData={defaultEnemyData != null}, enemyPrefab={enemyPrefab != null}");
        }

        private void OnEnable()
        {
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
            GameEvents.OnEnemyReachedGoal += HandleEnemyReachedGoal;
        }

        private void OnDisable()
        {
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
            GameEvents.OnEnemyReachedGoal -= HandleEnemyReachedGoal;
        }

        public void StartBuildPhase()
        {
            if (buildPhaseCoroutine != null)
            {
                StopCoroutine(buildPhaseCoroutine);
            }

            BuildPhaseTimeRemaining = waveConfig != null ? waveConfig.buildPhaseDuration : 5f;
            Debug.Log($"[WaveManager] StartBuildPhase called, duration={BuildPhaseTimeRemaining}s");
            GameEvents.InvokeBuildPhaseStart();
            buildPhaseCoroutine = StartCoroutine(BuildPhaseCountdown());
        }

        private IEnumerator BuildPhaseCountdown()
        {
            Debug.Log($"[WaveManager] BuildPhaseCountdown started, waiting {BuildPhaseTimeRemaining}s");
            while (BuildPhaseTimeRemaining > 0)
            {
                yield return null;
                BuildPhaseTimeRemaining -= Time.deltaTime;
                GameEvents.InvokeBuildPhaseTimerUpdate(BuildPhaseTimeRemaining);
            }

            Debug.Log("[WaveManager] BuildPhaseCountdown completed, starting wave");
            StartWave();
        }

        private IEnumerator AutoWaveTimer()
        {
            // Wait for auto-wave interval, then send next wave automatically
            float elapsed = 0f;
            while (elapsed < AUTO_WAVE_INTERVAL)
            {
                yield return null;
                elapsed += Time.deltaTime;

                // Broadcast timer update for UI (time until next wave)
                float timeRemaining = AUTO_WAVE_INTERVAL - elapsed;
                GameEvents.InvokeBuildPhaseTimerUpdate(timeRemaining);
            }

            // Auto-start next wave (no bonus since they waited)
            Debug.Log("[WaveManager] Auto-wave timer completed, starting next wave");
            if (IsWaveActive)
            {
                StartWave();
            }
        }

        public void StartWaveEarly()
        {
            Debug.Log($"[WaveManager] StartWaveEarly called, currentState={GameManager.Instance?.CurrentState}");

            // Handle mid-wave start (sending next wave during current wave)
            if (GameManager.Instance?.CurrentState == GameState.WaveActive)
            {
                if (CanSendNextWave)
                {
                    // Calculate bonus for sending next wave early (based on time remaining in auto-wave interval)
                    float timeInWave = Time.time - waveStartTime;
                    float timeRemaining = Mathf.Max(0, AUTO_WAVE_INTERVAL - timeInWave);
                    int bonus = Mathf.FloorToInt((timeRemaining / AUTO_WAVE_INTERVAL) * (waveConfig?.maxEarlyStartBonus ?? 50f));
                    if (bonus > 0)
                    {
                        EconomyManager.Instance?.AddCurrency(bonus);
                        Debug.Log($"[WaveManager] Early wave bonus: +{bonus} BTC");
                    }

                    // Stop auto-wave timer and start next wave immediately
                    if (autoWaveCoroutine != null)
                    {
                        StopCoroutine(autoWaveCoroutine);
                    }
                    StartWave();
                }
                else
                {
                    Debug.Log($"[WaveManager] Cannot send next wave yet, wait {TimeUntilCanSendNext:F1}s");
                }
                return;
            }

            // Handle build phase start
            if (GameManager.Instance?.CurrentState != GameState.Building)
            {
                Debug.LogWarning($"[WaveManager] StartWaveEarly skipped - not in building or wave state");
                return;
            }

            int buildBonus = waveConfig != null ? waveConfig.GetEarlyStartBonus(BuildPhaseTimeRemaining) : 0;
            if (buildBonus > 0)
            {
                EconomyManager.Instance?.AddCurrency(buildBonus);
                Debug.Log($"[WaveManager] Early start bonus: +{buildBonus} BTC");
            }

            if (buildPhaseCoroutine != null)
            {
                StopCoroutine(buildPhaseCoroutine);
            }

            StartWave();
        }

        public void StartWave()
        {
            CurrentWave++;
            IsWaveActive = true;
            waveStartTime = Time.time;
            Debug.Log($"[WaveManager] StartWave called, wave={CurrentWave}");
            GameManager.Instance.SetGameState(GameState.WaveActive);
            GameEvents.InvokeWaveStart(CurrentWave);

            // Start auto-wave timer for sending next wave
            if (autoWaveCoroutine != null)
            {
                StopCoroutine(autoWaveCoroutine);
            }
            autoWaveCoroutine = StartCoroutine(AutoWaveTimer());

            if (waveConfig == null)
            {
                Debug.LogError("[WaveManager] waveConfig is null, cannot start wave!");
                return;
            }

            var waveDef = waveConfig.GetWaveDefinition(CurrentWave);
            var difficulty = GameManager.Instance.GetCurrentDifficultySettings();

            int enemyCount = Mathf.RoundToInt(waveDef.enemyCount * difficulty.enemyCountMultiplier);
            float spawnRate = waveDef.spawnRate;

            // Use wave-specific enemy type if defined, otherwise fall back to default
            EnemyData enemyData = waveDef.enemyType != null ? waveDef.enemyType : defaultEnemyData;

            Debug.Log($"[WaveManager] Wave {CurrentWave}: enemyCount={enemyCount}, spawnRate={spawnRate}, enemyData={enemyData?.name ?? "null"}");

            spawnCoroutine = StartCoroutine(SpawnWave(enemyCount, spawnRate, enemyData));
        }

        private IEnumerator SpawnWave(int count, float spawnRate, EnemyData enemyData)
        {
            Debug.Log($"[WaveManager] SpawnWave started: count={count}, spawnRate={spawnRate}, enemyData={enemyData?.name ?? "null"}");

            if (spawnPositions == null || spawnPositions.Length == 0)
            {
                Debug.LogError("[WaveManager] No spawn positions available!");
                yield break;
            }

            enemiesToSpawn = count;
            float spawnInterval = 1f / spawnRate;
            int spawnIndex = 0;

            while (enemiesToSpawn > 0)
            {
                // Alternate between spawn positions
                Vector3 spawnPos = spawnPositions[spawnIndex % spawnPositions.Length];
                SpawnEnemy(spawnPos, enemyData);

                enemiesToSpawn--;
                spawnIndex++;

                yield return new WaitForSeconds(spawnInterval);
            }

            Debug.Log("[WaveManager] SpawnWave completed");
        }

        private void SpawnEnemy(Vector3 position, EnemyData data)
        {
            if (enemyPrefab == null)
            {
                Debug.LogError("[WaveManager] Cannot spawn enemy: enemyPrefab is null");
                return;
            }
            if (data == null)
            {
                Debug.LogError("[WaveManager] Cannot spawn enemy: data is null");
                return;
            }

            Debug.Log($"[WaveManager] Spawning enemy at {position}");
            GameObject enemyObj = Instantiate(enemyPrefab, position, Quaternion.identity);
            Enemy enemy = enemyObj.GetComponent<Enemy>();

            if (enemy != null)
            {
                var difficulty = GameManager.Instance.GetCurrentDifficultySettings();

                float health = data.GetHealthForWave(CurrentWave) * difficulty.enemyHealthMultiplier;
                float speed = data.GetSpeedForWave(CurrentWave) * difficulty.enemySpeedMultiplier;
                int reward = data.GetKillRewardForWave(CurrentWave);

                enemy.Initialize(health, speed, reward);

                // Setup 3D model from enemy data
                enemy.SetEnemyData(data);

                activeEnemies.Add(enemy);
                GameEvents.InvokeEnemySpawned(enemy);
            }
        }

        private void HandleEnemyKilled(Enemy enemy)
        {
            activeEnemies.Remove(enemy);
            CheckWaveComplete();
        }

        private void HandleEnemyReachedGoal(Enemy enemy)
        {
            activeEnemies.Remove(enemy);
            CheckWaveComplete();
        }

        private void CheckWaveComplete()
        {
            if (IsWaveActive && enemiesToSpawn <= 0 && activeEnemies.Count <= 0)
            {
                IsWaveActive = false;
                GameEvents.InvokeWaveComplete(CurrentWave);
                GameEvents.InvokeAllEnemiesDefeated();
            }
        }

        public void ClearAllEnemies()
        {
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
            }

            foreach (var enemy in activeEnemies)
            {
                if (enemy != null)
                {
                    Destroy(enemy.gameObject);
                }
            }

            activeEnemies.Clear();
            enemiesToSpawn = 0;
        }

        public List<Enemy> GetActiveEnemies()
        {
            activeEnemies.RemoveAll(e => e == null);
            return new List<Enemy>(activeEnemies);
        }

        public void ResetState()
        {
            // Stop any active coroutines
            if (spawnCoroutine != null)
                StopCoroutine(spawnCoroutine);
            if (buildPhaseCoroutine != null)
                StopCoroutine(buildPhaseCoroutine);
            if (autoWaveCoroutine != null)
                StopCoroutine(autoWaveCoroutine);

            // Clear enemies
            ClearAllEnemies();

            // Reset wave count
            CurrentWave = 0;
            IsWaveActive = false;
            BuildPhaseTimeRemaining = 0f;
            enemiesToSpawn = 0;
            waveStartTime = 0f;

            Debug.Log("[WaveManager] State reset complete");
        }

        /// <summary>
        /// Gets the current early start bonus based on remaining time.
        /// Works for both build phase and mid-wave scenarios.
        /// </summary>
        public int GetCurrentEarlyBonus()
        {
            if (GameManager.Instance?.CurrentState == GameState.Building)
            {
                return waveConfig?.GetEarlyStartBonus(BuildPhaseTimeRemaining) ?? 0;
            }
            else if (IsWaveActive && CanSendNextWave)
            {
                float timeInWave = Time.time - waveStartTime;
                float timeRemaining = Mathf.Max(0, AUTO_WAVE_INTERVAL - timeInWave);
                return Mathf.FloorToInt((timeRemaining / AUTO_WAVE_INTERVAL) * (waveConfig?.maxEarlyStartBonus ?? 50f));
            }
            return 0;
        }
    }
}
