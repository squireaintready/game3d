using UnityEngine;
using UnityEngine.AI;

namespace TowerDefense
{
    public enum EnemyState
    {
        Moving,
        Blocked,
        Attacking
    }

    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyMovement : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform targetDestination;

        [Header("Settings")]
        [SerializeField] private float arrivalThreshold = 0.5f;
        [SerializeField] private float pathRecalculateInterval = 0.1f; // More aggressive
        [SerializeField] private float stuckThreshold = 0.05f;
        [SerializeField] private float stuckTimeLimit = 0.5f; // Faster stuck detection
        [SerializeField] private float attackRange = 1.2f;
        [SerializeField] private float attackDamagePerSecond = 50f; // 2 seconds to destroy 100hp tower
        [SerializeField] private float vulnerableTowerScanRange = 1.5f; // Range to scan for vulnerable towers while moving

        public float MoveSpeed { get; private set; }
        public EnemyState CurrentState { get; private set; } = EnemyState.Moving;
        public Tower AttackTarget { get; private set; }

        private NavMeshAgent agent;
        private Enemy enemy;
        private bool hasReachedGoal;
        private float pathRecalculateTimer;
        private Vector3 lastCheckedPosition;
        private float stuckTimer;
        private float blockedTimer;
        private int failedPathAttempts;
        private const float BLOCKED_ATTACK_DELAY = 1.0f; // Time before attacking when blocked
        private const int REQUIRED_FAILED_ATTEMPTS = 5; // Must fail this many times before attacking

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            enemy = GetComponent<Enemy>();

            // Configure agent for isometric movement
            agent.updateRotation = false;
            agent.updateUpAxis = false;

            Debug.Log($"[EnemyMovement] Awake at position {transform.position}, agent={agent != null}");
        }

        private void Start()
        {
            Debug.Log($"[EnemyMovement] Start - isOnNavMesh={agent?.isOnNavMesh ?? false}, position={transform.position}");
            FindDestination();
        }

        public void Initialize(float speed)
        {
            MoveSpeed = speed;
            if (agent != null)
            {
                agent.speed = speed;
            }
            lastCheckedPosition = transform.position;
            stuckTimer = 0f;
            pathRecalculateTimer = pathRecalculateInterval;
        }

        private void FindDestination()
        {
            if (targetDestination == null)
            {
                // Try multiple tags to find the goal
                string[] goalTags = { "GoalPoint", "SafeZone", "Goal", "Finish" };
                foreach (string tag in goalTags)
                {
                    try
                    {
                        var goal = GameObject.FindGameObjectWithTag(tag);
                        if (goal != null)
                        {
                            targetDestination = goal.transform;
                            break;
                        }
                    }
                    catch { }
                }

                // Fallback: find by name
                if (targetDestination == null)
                {
                    var goal = GameObject.Find("GoalPoint") ?? GameObject.Find("Goal") ?? GameObject.Find("SafeZone");
                    if (goal != null)
                    {
                        targetDestination = goal.transform;
                    }
                }
            }

            if (targetDestination != null && agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(targetDestination.position);
                Debug.Log($"[EnemyMovement] Set destination to {targetDestination.name} at {targetDestination.position}");
            }
            else
            {
                Debug.LogWarning($"[EnemyMovement] Could not set destination. Target={targetDestination != null}, Agent={agent != null}, OnNavMesh={agent?.isOnNavMesh ?? false}");
            }
        }

        private void Update()
        {
            if (hasReachedGoal || enemy == null || enemy.IsDead) return;

            ClampToGridBounds();
            CheckArrival();

            switch (CurrentState)
            {
                case EnemyState.Moving:
                    CheckPathValidity();
                    CheckIfStuck();
                    // Check for nearby vulnerable towers to attack while moving
                    CheckForVulnerableTowers();
                    break;

                case EnemyState.Blocked:
                    HandleBlockedState();
                    break;

                case EnemyState.Attacking:
                    HandleAttackingState();
                    break;
            }
        }

        private void ClampToGridBounds()
        {
            // Keep enemy within grid boundaries
            var grid = TowerDefense.Core.GridManager.Instance;
            if (grid == null) return;

            Vector3 pos = transform.position;
            float minX = 0f;
            float maxX = grid.GridWidth * grid.CellSize;
            float minZ = 0f;
            float maxZ = grid.GridHeight * grid.CellSize;

            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.z = Mathf.Clamp(pos.z, minZ, maxZ);

            if (pos != transform.position)
            {
                transform.position = pos;
            }
        }

        private void CheckArrival()
        {
            if (targetDestination == null) return;

            float distance = Vector3.Distance(transform.position, targetDestination.position);

            if (distance <= arrivalThreshold)
            {
                hasReachedGoal = true;
                enemy?.ReachedGoal();
            }
        }

        private void CheckPathValidity()
        {
            if (agent == null || !agent.isOnNavMesh || targetDestination == null) return;

            pathRecalculateTimer -= Time.deltaTime;

            // Periodically recalculate path to handle dynamic obstacles
            if (pathRecalculateTimer <= 0f)
            {
                pathRecalculateTimer = pathRecalculateInterval;

                // Check if current path is invalid or incomplete
                if (!agent.hasPath || agent.pathStatus == NavMeshPathStatus.PathInvalid || agent.pathStatus == NavMeshPathStatus.PathPartial)
                {
                    RecalculatePath();
                }
                else
                {
                    // Even if path seems valid, recalculate to adapt to new obstacles
                    agent.SetDestination(targetDestination.position);
                }
            }
        }

        private void CheckIfStuck()
        {
            if (agent == null || !agent.isOnNavMesh) return;

            // Check if enemy is barely moving
            float distanceMoved = Vector3.Distance(transform.position, lastCheckedPosition);

            if (distanceMoved < stuckThreshold)
            {
                stuckTimer += Time.deltaTime;

                if (stuckTimer >= stuckTimeLimit)
                {
                    // Try to recalculate path first
                    RecalculatePath();

                    // Wait a frame for NavMesh to update, then check path
                    if (!agent.hasPath || agent.pathStatus == NavMeshPathStatus.PathInvalid)
                    {
                        failedPathAttempts++;
                        Debug.Log($"[EnemyMovement] Path attempt failed ({failedPathAttempts}/{REQUIRED_FAILED_ATTEMPTS})");

                        // Only transition to blocked after multiple failed attempts
                        if (failedPathAttempts >= REQUIRED_FAILED_ATTEMPTS)
                        {
                            TransitionToBlocked();
                        }
                    }
                    else
                    {
                        // Path found, reset counter
                        failedPathAttempts = 0;
                    }
                    stuckTimer = 0f;
                }
            }
            else
            {
                // Moving fine, reset timers and counters
                stuckTimer = 0f;
                blockedTimer = 0f;
                failedPathAttempts = 0;
            }

            lastCheckedPosition = transform.position;
        }

        /// <summary>
        /// Check for nearby vulnerable (ground-placed) towers to attack while moving
        /// </summary>
        private void CheckForVulnerableTowers()
        {
            Tower vulnerableTower = FindNearestVulnerableTower();
            if (vulnerableTower != null)
            {
                float dist = Vector3.Distance(transform.position, vulnerableTower.transform.position);
                if (dist <= attackRange)
                {
                    // Close enough to attack
                    AttackTarget = vulnerableTower;
                    TransitionToAttacking();
                    Debug.Log($"[EnemyMovement] Enemy attacking nearby vulnerable tower: {vulnerableTower.name}");
                }
            }
        }

        /// <summary>
        /// Find the nearest vulnerable (ground-placed, unprotected) tower
        /// </summary>
        private Tower FindNearestVulnerableTower()
        {
            var allTowers = Core.GridManager.Instance?.GetAllTowers();
            if (allTowers == null || allTowers.Count == 0) return null;

            Tower nearest = null;
            float nearestDist = vulnerableTowerScanRange;

            foreach (var tower in allTowers)
            {
                if (tower == null) continue;

                // Only target vulnerable (ground-placed) attack towers
                if (!tower.IsVulnerable) continue;

                float dist = Vector3.Distance(transform.position, tower.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = tower;
                }
            }

            return nearest;
        }

        private void TransitionToBlocked()
        {
            CurrentState = EnemyState.Blocked;
            blockedTimer = 0f;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }

            Debug.Log($"[EnemyMovement] Enemy blocked at {transform.position}");
        }

        private void HandleBlockedState()
        {
            blockedTimer += Time.deltaTime;

            // Keep trying to find a path
            RecalculatePath();

            // Check if path is now valid
            if (agent.hasPath && agent.pathStatus == NavMeshPathStatus.PathComplete)
            {
                // Path found! Resume moving
                Debug.Log("[EnemyMovement] Path found while blocked, resuming movement");
                TransitionToMoving();
                return;
            }

            // Only attack after significant delay and confirmed no path
            if (blockedTimer >= BLOCKED_ATTACK_DELAY)
            {
                // Final verification - try to calculate path manually
                NavMeshPath testPath = new NavMeshPath();
                bool canPath = agent.CalculatePath(targetDestination.position, testPath);

                if (canPath && testPath.status == NavMeshPathStatus.PathComplete)
                {
                    // Actually we CAN path - just use this path
                    agent.SetPath(testPath);
                    TransitionToMoving();
                    return;
                }

                // Truly blocked - find nearest tower to attack
                Tower nearestTower = FindNearestBlockingTower();
                if (nearestTower != null)
                {
                    AttackTarget = nearestTower;
                    TransitionToAttacking();
                }
                else
                {
                    // No tower blocking, keep trying to find path
                    blockedTimer = 0f;
                }
            }
        }

        private void HandleAttackingState()
        {
            if (AttackTarget == null || AttackTarget.gameObject == null || AttackTarget.IsDestroyed)
            {
                // Target destroyed, try to move again
                AttackTarget = null;
                RecalculatePath();
                TransitionToMoving();
                return;
            }

            // If target became protected (wall added), stop attacking and find another target or move
            if (AttackTarget.IsProtected)
            {
                Debug.Log($"[EnemyMovement] Target {AttackTarget.name} became protected, stopping attack");
                AttackTarget = null;
                RecalculatePath();
                TransitionToMoving();
                return;
            }

            // Face the target
            Vector3 dirToTarget = (AttackTarget.transform.position - transform.position).normalized;
            if (dirToTarget != Vector3.zero)
            {
                transform.forward = new Vector3(dirToTarget.x, 0, dirToTarget.z);
            }

            // Deal damage to the tower (only if vulnerable)
            if (AttackTarget.IsVulnerable)
            {
                float damage = attackDamagePerSecond * Time.deltaTime;
                AttackTarget.TakeDamage(damage);
            }

            // Notify enemy for visual effect
            enemy?.SetAttacking(true);

            // Check if we can path again (tower might be destroyed or moved)
            pathRecalculateTimer -= Time.deltaTime;
            if (pathRecalculateTimer <= 0f)
            {
                pathRecalculateTimer = pathRecalculateInterval;
                RecalculatePath();

                if (agent.hasPath && agent.pathStatus == NavMeshPathStatus.PathComplete)
                {
                    // Path is clear now
                    AttackTarget = null;
                    enemy?.SetAttacking(false);
                    TransitionToMoving();
                }
            }
        }

        private void TransitionToMoving()
        {
            CurrentState = EnemyState.Moving;
            stuckTimer = 0f;
            blockedTimer = 0f;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }

            enemy?.SetAttacking(false);
            Debug.Log($"[EnemyMovement] Enemy resumed moving");
        }

        private void TransitionToAttacking()
        {
            CurrentState = EnemyState.Attacking;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }

            enemy?.SetAttacking(true);
            Debug.Log($"[EnemyMovement] Enemy attacking tower: {AttackTarget?.name}");
        }

        private Tower FindNearestTower()
        {
            var allTowers = Core.GridManager.Instance?.GetAllTowers();
            if (allTowers == null || allTowers.Count == 0) return null;

            Tower nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var tower in allTowers)
            {
                if (tower == null) continue;

                float dist = Vector3.Distance(transform.position, tower.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = tower;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Find a tower that's likely blocking the path to the goal
        /// </summary>
        private Tower FindNearestBlockingTower()
        {
            var allTowers = Core.GridManager.Instance?.GetAllTowers();
            if (allTowers == null || allTowers.Count == 0) return null;

            Tower bestTarget = null;
            float bestScore = float.MaxValue;

            Vector3 dirToGoal = (targetDestination.position - transform.position).normalized;

            foreach (var tower in allTowers)
            {
                if (tower == null) continue;

                Vector3 toTower = tower.transform.position - transform.position;
                float dist = toTower.magnitude;

                // Only consider towers within reasonable range
                if (dist > 5f) continue;

                // Check if tower is roughly in the direction of the goal
                float dotProduct = Vector3.Dot(toTower.normalized, dirToGoal);

                // Tower should be somewhat in front of us (toward goal)
                if (dotProduct < 0.3f) continue;

                // Score based on distance and alignment (lower is better)
                float score = dist * (2f - dotProduct);

                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = tower;
                }
            }

            // If no tower in path direction, fall back to nearest
            if (bestTarget == null)
            {
                return FindNearestTower();
            }

            Debug.Log($"[EnemyMovement] Found blocking tower: {bestTarget.name} at distance {bestScore:F2}");
            return bestTarget;
        }

        private void RecalculatePath()
        {
            if (agent == null || !agent.isOnNavMesh || targetDestination == null) return;

            // Clear current path and recalculate
            agent.ResetPath();
            agent.SetDestination(targetDestination.position);

            // If path is still invalid, try to find nearest valid position
            if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(targetDestination.position, out hit, 5f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
            }
        }

        public void SetDestination(Transform destination)
        {
            targetDestination = destination;
            if (agent != null && agent.isOnNavMesh && destination != null)
            {
                agent.SetDestination(destination.position);
            }
        }

        public void Stop()
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
        }

        public void Resume()
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }
        }

        public bool HasPath()
        {
            return agent != null && agent.hasPath;
        }

        public float GetRemainingDistance()
        {
            if (agent == null || !agent.hasPath) return float.MaxValue;
            return agent.remainingDistance;
        }

        /// <summary>
        /// Force immediate path recalculation (call when obstacles change)
        /// </summary>
        public void ForceRecalculatePath()
        {
            RecalculatePath();
        }

        /// <summary>
        /// Force all enemies to recalculate their paths (call when towers are placed/removed)
        /// </summary>
        public static void RecalculateAllEnemyPaths()
        {
            var allMovements = FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None);
            foreach (var movement in allMovements)
            {
                if (movement != null && movement.gameObject.activeInHierarchy)
                {
                    movement.ForceRecalculatePath();
                }
            }
            Debug.Log($"[EnemyMovement] Recalculated paths for {allMovements.Length} enemies");
        }
    }
}
