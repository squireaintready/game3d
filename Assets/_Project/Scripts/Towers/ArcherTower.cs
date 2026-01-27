using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Data;

namespace TowerDefense
{
    public class ArcherTower : Tower
    {
        [Header("Archer Settings")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;

        [Header("Rotation Tracking")]
        [SerializeField] private float rotationSpeed = 10f;

        private float attackCooldown;
        private Enemy currentTarget;

        // Default values if towerData is null
        private const float DEFAULT_RANGE = 3f;
        private const float DEFAULT_DAMAGE = 25f;
        private const float DEFAULT_ATTACK_SPEED = 1f;
        private const float DEFAULT_PROJECTILE_SPEED = 8f;

        protected override void Update()
        {
            base.Update();

            if (IsDestroyed) return;

            var gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                Debug.LogWarning("[ArcherTower] GameManager.Instance is null!");
                return;
            }

            if (gameManager.CurrentState != GameState.WaveActive) return;

            attackCooldown -= Time.deltaTime;

            if (currentTarget == null || !IsTargetInRange(currentTarget))
            {
                FindNewTarget();
            }

            // Rotate tower to face current target
            if (currentTarget != null)
            {
                RotateTowardsTarget();
            }

            if (currentTarget != null && attackCooldown <= 0f)
            {
                Attack();
            }
        }

        private void RotateTowardsTarget()
        {
            if (currentTarget == null) return;

            // Rotate units toward target (for composite models)
            if (unitTransforms != null && unitTransforms.Length > 0)
            {
                RotateUnitsToward(currentTarget.transform.position, rotationSpeed);
            }
            else if (modelHolder != null)
            {
                // Fallback: rotate the model holder for simple models
                Vector3 directionToTarget = currentTarget.transform.position - transform.position;
                directionToTarget.y = 0f;

                if (directionToTarget.sqrMagnitude < 0.001f) return;

                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                modelHolder.rotation = Quaternion.Slerp(
                    modelHolder.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }

        private void FindNewTarget()
        {
            currentTarget = null;
            float closestDistance = float.MaxValue;

            float range = CurrentRange > 0 ? CurrentRange : DEFAULT_RANGE;

            var waveManager = WaveManager.Instance;
            if (waveManager == null)
            {
                Debug.LogWarning("[ArcherTower] WaveManager.Instance is null!");
                return;
            }

            var enemies = waveManager.GetActiveEnemies();
            if (enemies == null || enemies.Count == 0) return;

            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.IsDead) continue;

                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance <= range && distance < closestDistance)
                {
                    closestDistance = distance;
                    currentTarget = enemy;
                }
            }

            if (currentTarget != null)
            {
                Debug.Log($"[ArcherTower] Found target at distance {closestDistance:F2}, range={range}");
            }
        }

        private bool IsTargetInRange(Enemy target)
        {
            if (target == null) return false;
            float range = CurrentRange > 0 ? CurrentRange : DEFAULT_RANGE;
            return Vector3.Distance(transform.position, target.transform.position) <= range;
        }

        private void Attack()
        {
            if (currentTarget == null) return;

            float damage = CurrentDamage > 0 ? CurrentDamage : DEFAULT_DAMAGE;
            float attackSpeed = towerData != null ? towerData.attackSpeed : DEFAULT_ATTACK_SPEED;
            float projectileSpeed = towerData != null ? towerData.projectileSpeed : DEFAULT_PROJECTILE_SPEED;

            Debug.Log($"[ArcherTower] Attacking {currentTarget.name}, damage={damage}, speed={attackSpeed}");
            attackCooldown = 1f / attackSpeed;

            // Play attack animation
            PlayAttackAnimation();

            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1f;

            GameObject projectileObj;
            if (projectilePrefab != null)
            {
                projectileObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                projectileObj = CreateRuntimeProjectile(spawnPos);
            }

            Projectile projectile = projectileObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(currentTarget, damage, projectileSpeed);
            }
        }

        private GameObject CreateRuntimeProjectile(Vector3 position)
        {
            GameObject projectileObj = new GameObject("ArrowProjectile");
            projectileObj.transform.position = position;

            projectileObj.AddComponent<Projectile>();

            // Create a 3D arrow visual (elongated capsule)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "ArrowVisual";
            visual.transform.SetParent(projectileObj.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.1f, 0.3f, 0.1f);
            visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Point forward

            // Remove collider
            var collider = visual.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            // Set yellow color
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = CreateProjectileMaterial(new Color(1f, 0.9f, 0.2f));
            }

            return projectileObj;
        }

        private Material CreateProjectileMaterial(Color color)
        {
            string[] shaderNames = new string[]
            {
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Simple Lit",
                "Standard"
            };

            foreach (string shaderName in shaderNames)
            {
                Shader shader = Shader.Find(shaderName);
                if (shader != null)
                {
                    Material mat = new Material(shader);
                    mat.color = color;
                    if (mat.HasProperty("_BaseColor"))
                    {
                        mat.SetColor("_BaseColor", color);
                    }
                    return mat;
                }
            }

            Material fallback = new Material(Shader.Find("Sprites/Default"));
            fallback.color = color;
            return fallback;
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            if (currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, currentTarget.transform.position);
            }
        }
    }
}
