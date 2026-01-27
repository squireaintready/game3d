using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using Unity.AI.Navigation;
using TowerDefense.Data;
using TowerDefense.Placement;
using TowerDefense.UI;

namespace TowerDefense.Core
{
    [DefaultExecutionOrder(-50)] // Run before other scripts but after GameSceneSetup
    public class TestSetup : MonoBehaviour
    {
        [Header("Auto Start")]
        [SerializeField] private bool autoStartGame = true;
        [SerializeField] private float startDelay = 0.5f;
        [SerializeField] private bool skipBuildPhase = false;

        [Header("UI")]
        [SerializeField] private bool autoCreateUI = true;

        [Header("NavMesh")]
        [SerializeField] private bool autoBakeNavMesh = true;

        [Header("Test Towers")]
        [SerializeField] private bool placeTestTowers = false;
        [SerializeField] private GameObject archerTowerPrefab;
        [SerializeField] private GameObject wallTowerPrefab;
        [SerializeField] private TowerData archerTowerData;
        [SerializeField] private TowerData wallTowerData;

        [Header("Test Tower Positions (Grid Coords)")]
        [SerializeField] private Vector2Int[] archerPositions = new Vector2Int[]
        {
            new Vector2Int(1, 3),
            new Vector2Int(5, 3),
            new Vector2Int(3, 6),
            new Vector2Int(1, 9),
            new Vector2Int(5, 9)
        };

        [SerializeField] private Vector2Int[] wallPositions = new Vector2Int[]
        {
            new Vector2Int(2, 5),
            new Vector2Int(4, 5)
        };

        private void Awake()
        {
            // Create spawn and goal points early so other managers can find them
            EnsureSpawnAndGoalPoints();
        }

        private void Start()
        {
            if (autoCreateUI)
            {
                SetupUI();
            }

            if (autoStartGame)
            {
                Invoke(nameof(AutoStart), startDelay);
            }
        }

        private void SetupUI()
        {
            // Find or create Canvas
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                Debug.Log("[TestSetup] Created new Canvas");
            }

            // Add EventSystem if not present
            var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                var eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Debug.Log("[TestSetup] Created EventSystem");
            }

            // Add GameUIAutoSetup if not already present
            if (canvas.GetComponent<GameUIAutoSetup>() == null)
            {
                canvas.gameObject.AddComponent<GameUIAutoSetup>();
                Debug.Log("[TestSetup] Added GameUIAutoSetup to Canvas");
            }
        }

        private void AutoStart()
        {
            Debug.Log("[TestSetup] AutoStart beginning");

            if (placeTestTowers)
            {
                PlaceTestTowers();
            }

            // Bake NavMesh after placing towers so obstacles are included
            if (autoBakeNavMesh)
            {
                BakeNavMesh();
            }

            Debug.Log($"[TestSetup] Starting game, GameManager.Instance={GameManager.Instance != null}");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame(Difficulty.Normal);
            }
            else
            {
                Debug.LogError("[TestSetup] GameManager.Instance is null!");
                return;
            }

            // Skip build phase for faster testing
            if (skipBuildPhase)
            {
                Debug.Log("[TestSetup] Scheduling SkipBuildPhase in 0.1s");
                Invoke(nameof(SkipBuildPhase), 0.1f);
            }
        }

        private void EnsureSpawnAndGoalPoints()
        {
            float gridWidth = 7f;
            float gridHeight = 12f;
            float cellSize = 1f;

            // Try to get actual values from GridManager
            if (GridManager.Instance != null)
            {
                gridWidth = GridManager.Instance.GridWidth;
                gridHeight = GridManager.Instance.GridHeight;
                cellSize = GridManager.Instance.CellSize;
            }

            // Create SpawnPoint if missing
            GameObject spawnPoint = GameObject.Find("SpawnPoint");
            if (spawnPoint == null)
            {
                spawnPoint = new GameObject("SpawnPoint");
                // Place at top center of the map
                spawnPoint.transform.position = new Vector3(gridWidth * cellSize / 2f, 0f, gridHeight * cellSize);
                Debug.Log($"[TestSetup] Created SpawnPoint at {spawnPoint.transform.position}");
            }

            // Create GoalPoint if missing
            GameObject goalPoint = GameObject.Find("GoalPoint") ?? GameObject.Find("Goal");
            if (goalPoint == null)
            {
                goalPoint = new GameObject("GoalPoint");
                // Place at bottom center of the map
                goalPoint.transform.position = new Vector3(gridWidth * cellSize / 2f, 0f, 0f);
                Debug.Log($"[TestSetup] Created GoalPoint at {goalPoint.transform.position}");
            }
        }

        private void SkipBuildPhase()
        {
            Debug.Log($"[TestSetup] SkipBuildPhase called, WaveManager.Instance={WaveManager.Instance != null}");
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.StartWaveEarly();
            }
            else
            {
                Debug.LogError("[TestSetup] WaveManager.Instance is null!");
            }
        }

        private void BakeNavMesh()
        {
            // Find or create NavMeshSurface on the ground
            GameObject ground = GameObject.Find("Ground") ?? GameObject.Find("GroundPlane") ?? GameObject.Find("Plane");

            if (ground != null)
            {
                // Disable any existing ground renderer - tile sprites handle visuals
                var existingRenderer = ground.GetComponent<MeshRenderer>();
                if (existingRenderer != null)
                {
                    existingRenderer.enabled = false;
                    Debug.Log("[TestSetup] Disabled existing ground renderer");
                }
            }
            else
            {
                // Create a simple ground plane for NavMesh
                ground = new GameObject("Ground");
                var meshFilter = ground.AddComponent<MeshFilter>();
                var meshRenderer = ground.AddComponent<MeshRenderer>();

                // Create a large flat mesh
                Mesh mesh = new Mesh();
                float size = 50f;
                mesh.vertices = new Vector3[]
                {
                    new Vector3(-5, 0, -5),
                    new Vector3(size, 0, -5),
                    new Vector3(-5, 0, size),
                    new Vector3(size, 0, size)
                };
                mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
                mesh.normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
                meshFilter.mesh = mesh;

                // Disable renderer - this is only for NavMesh, not visuals
                meshRenderer.enabled = false;

                // Add collider
                var collider = ground.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;

                Debug.Log("[TestSetup] Created ground for NavMesh (invisible)");
            }

            // Get or add NavMeshSurface
            NavMeshSurface navMeshSurface = ground.GetComponent<NavMeshSurface>();
            if (navMeshSurface == null)
            {
                navMeshSurface = ground.AddComponent<NavMeshSurface>();
            }

            // Configure NavMeshSurface - use PhysicsColliders since renderer is disabled
            navMeshSurface.collectObjects = CollectObjects.All;
            navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

            // Build the NavMesh
            navMeshSurface.BuildNavMesh();
            Debug.Log("[TestSetup] NavMesh baked successfully");
        }

        private void PlaceTestTowers()
        {
            if (GridManager.Instance == null)
            {
                Debug.LogWarning("GridManager not found, skipping test towers");
                return;
            }

            // Place archer towers
            if (archerTowerPrefab != null && archerTowerData != null)
            {
                foreach (var pos in archerPositions)
                {
                    PlaceTower(pos, archerTowerPrefab, archerTowerData);
                }
            }

            // Place wall towers
            if (wallTowerPrefab != null && wallTowerData != null)
            {
                foreach (var pos in wallPositions)
                {
                    PlaceTower(pos, wallTowerPrefab, wallTowerData);
                }
            }
        }

        private void PlaceTower(Vector2Int gridPos, GameObject prefab, TowerData data)
        {
            if (!GridManager.Instance.IsValidCell(gridPos))
            {
                Debug.LogWarning($"Invalid grid position: {gridPos}");
                return;
            }

            if (!GridManager.Instance.IsCellEmpty(gridPos))
            {
                Debug.LogWarning($"Cell already occupied: {gridPos}");
                return;
            }

            Vector3 worldPos = GridManager.Instance.GridToWorld(gridPos);
            GameObject towerObj = Instantiate(prefab, worldPos, Quaternion.identity);
            Tower tower = towerObj.GetComponent<Tower>();

            if (tower != null)
            {
                tower.Initialize(data, gridPos);
                GridManager.Instance.PlaceTower(gridPos, tower);
                Debug.Log($"Placed {data.towerName} at {gridPos}");
            }
        }
    }
}
