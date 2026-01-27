using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TowerDefense.Core;

namespace TowerDefense.Placement
{
    public class PlacementValidator : MonoBehaviour
    {
        [Header("Path Validation")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private Transform safeZone;

        [Header("Temporary Obstacle")]
        [SerializeField] private GameObject tempObstaclePrefab;

        private NavMeshPath testPath;

        private void Awake()
        {
            testPath = new NavMeshPath();
        }

        private void Start()
        {
            FindSpawnAndGoalPoints();
        }

        private void FindSpawnAndGoalPoints()
        {
            // Find spawn points
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                var spawns = new List<GameObject>();

                // Try multiple methods to find spawns
                try
                {
                    var taggedSpawns = GameObject.FindGameObjectsWithTag("SpawnPoint");
                    spawns.AddRange(taggedSpawns);
                }
                catch { }

                // Also search by name
                var leftSpawn = GameObject.Find("SpawnPoint_Left");
                var rightSpawn = GameObject.Find("SpawnPoint_Right");
                if (leftSpawn != null && !spawns.Contains(leftSpawn)) spawns.Add(leftSpawn);
                if (rightSpawn != null && !spawns.Contains(rightSpawn)) spawns.Add(rightSpawn);

                spawnPoints = new Transform[spawns.Count];
                for (int i = 0; i < spawns.Count; i++)
                {
                    spawnPoints[i] = spawns[i].transform;
                }

                Debug.Log($"[PlacementValidator] Found {spawnPoints.Length} spawn points");
            }

            // Find goal (safe zone) - check multiple tags/names
            if (safeZone == null)
            {
                string[] goalTags = { "GoalPoint", "SafeZone", "Goal", "Finish" };
                foreach (string tag in goalTags)
                {
                    try
                    {
                        var zone = GameObject.FindGameObjectWithTag(tag);
                        if (zone != null)
                        {
                            safeZone = zone.transform;
                            Debug.Log($"[PlacementValidator] Found goal with tag '{tag}' at {safeZone.position}");
                            break;
                        }
                    }
                    catch { }
                }

                // Fallback: search by name
                if (safeZone == null)
                {
                    var goal = GameObject.Find("GoalPoint") ?? GameObject.Find("Goal") ?? GameObject.Find("SafeZone");
                    if (goal != null)
                    {
                        safeZone = goal.transform;
                        Debug.Log($"[PlacementValidator] Found goal by name at {safeZone.position}");
                    }
                }

                if (safeZone == null)
                {
                    Debug.LogWarning("[PlacementValidator] Could not find goal/safe zone! Path validation will be disabled.");
                }
            }
        }

        /// <summary>
        /// Returns true if placing at this position would block ANY path
        /// (meaning placement should be denied)
        /// </summary>
        public bool WouldBlockPath(Vector2Int gridPos)
        {
            // Re-find points if needed (they may not exist at Start)
            if (safeZone == null || spawnPoints == null || spawnPoints.Length == 0)
            {
                FindSpawnAndGoalPoints();
            }

            if (safeZone == null || spawnPoints == null || spawnPoints.Length == 0)
            {
                // Still can't validate, allow placement
                Debug.LogWarning("[PlacementValidator] Cannot validate - missing spawn/goal references");
                return false;
            }

            var grid = GridManager.Instance;
            if (grid == null) return false;

            Vector2Int goalGridPos = grid.WorldToGrid(safeZone.position);

            // Use grid-based BFS pathfinding (more reliable than NavMesh carving)
            // Require ALL spawn points to have valid paths to goal
            foreach (var spawn in spawnPoints)
            {
                if (spawn == null) continue;

                Vector2Int spawnGridPos = grid.WorldToGrid(spawn.position);

                // Check if path exists with the proposed cell blocked
                bool pathExists = GridPathExists(spawnGridPos, goalGridPos, gridPos, grid);

                if (!pathExists)
                {
                    // This spawn would be blocked - deny placement
                    Debug.Log($"[PlacementValidator] BLOCKING placement at {gridPos} - would block path from spawn {spawnGridPos} to goal {goalGridPos}");
                    return true;
                }
            }

            // All spawns have valid paths - allow placement
            return false;
        }

        /// <summary>
        /// BFS pathfinding on the grid to check if a path exists
        /// </summary>
        private bool GridPathExists(Vector2Int start, Vector2Int goal, Vector2Int blockedCell, GridManager grid)
        {
            if (start == goal) return true;

            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();

            queue.Enqueue(start);
            visited.Add(start);
            visited.Add(blockedCell); // Treat proposed cell as blocked

            // 4-directional movement
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(0, 1),  // up
                new Vector2Int(0, -1), // down
                new Vector2Int(1, 0),  // right
                new Vector2Int(-1, 0)  // left
            };

            int iterations = 0;
            int maxIterations = grid.GridWidth * grid.GridHeight * 2; // Safety limit

            while (queue.Count > 0 && iterations < maxIterations)
            {
                iterations++;
                Vector2Int current = queue.Dequeue();

                foreach (var dir in directions)
                {
                    Vector2Int neighbor = current + dir;

                    // Check if we reached the goal
                    if (neighbor == goal)
                    {
                        return true; // Found path to goal
                    }

                    // Skip if already visited (includes proposed blocked cell)
                    if (visited.Contains(neighbor)) continue;

                    // Skip if out of grid bounds
                    if (!grid.IsValidCell(neighbor)) continue;

                    // Skip blocked cells (spawn, goal, water, etc.) - but NOT the goal itself
                    // Note: goal check happens before this, so goal won't be skipped
                    if (grid.IsCellBlocked(neighbor)) continue;

                    // Skip cells with towers
                    if (grid.IsCellOccupied(neighbor)) continue;

                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }

            return false; // No path found
        }

        /// <summary>
        /// Check if any valid path exists from spawn points to safe zone
        /// </summary>
        public bool AnyPathExists()
        {
            if (safeZone == null || spawnPoints == null || spawnPoints.Length == 0)
            {
                return true;
            }

            foreach (var spawn in spawnPoints)
            {
                if (spawn == null) continue;

                bool canReach = NavMesh.CalculatePath(spawn.position, safeZone.position, NavMesh.AllAreas, testPath);

                if (canReach && testPath.status == NavMeshPathStatus.PathComplete)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the path length from spawn to safe zone
        /// </summary>
        public float GetPathLength(Transform spawn)
        {
            if (safeZone == null || spawn == null) return float.MaxValue;

            bool canReach = NavMesh.CalculatePath(spawn.position, safeZone.position, NavMesh.AllAreas, testPath);

            if (!canReach || testPath.status != NavMeshPathStatus.PathComplete)
            {
                return float.MaxValue;
            }

            float length = 0f;
            for (int i = 0; i < testPath.corners.Length - 1; i++)
            {
                length += Vector3.Distance(testPath.corners[i], testPath.corners[i + 1]);
            }

            return length;
        }
    }
}
