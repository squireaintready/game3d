using System.Collections;
using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Data;

namespace TowerDefense
{
    public class CannonTower : Tower
    {
        [Header("Cannon Settings")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;

        [Header("Rotation Tracking")]
        [SerializeField] private float rotationSpeed = 8f;

        [Header("Recoil Animation")]
        [SerializeField] private float recoilDistance = 0.15f;
        [SerializeField] private float recoilDuration = 0.15f;

        private float attackCooldown;
        private Enemy currentTarget;
        private bool isRecoiling;

        // Default values if towerData is null
        private const float DEFAULT_RANGE = 4f;
        private const float DEFAULT_DAMAGE = 60f;
        private const float DEFAULT_ATTACK_SPEED = 0.5f;
        private const float DEFAULT_PROJECTILE_SPEED = 10f;

        protected override void Update()
        {
            // Skip base Update (we'll handle idle animation differently for cannon)
            if (IsDestroyed) return;

            var gameManager = GameManager.Instance;
            if (gameManager == null) return;

            if (gameManager.CurrentState != GameState.WaveActive) return;

            attackCooldown -= Time.deltaTime;

            if (currentTarget == null || !IsTargetInRange(currentTarget))
            {
                FindNewTarget();
            }

            // Rotate cannon (unit) to face current target
            if (currentTarget != null && !isRecoiling)
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

            // Rotate units toward target (the cannon itself)
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
            if (waveManager == null) return;

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

            Debug.Log($"[CannonTower] Attacking {currentTarget.name}, damage={damage}, speed={attackSpeed}");
            attackCooldown = 1f / attackSpeed;

            // Play recoil animation instead of scale pulse
            PlayRecoilAnimation();

            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1f;

            GameObject projectileObj;
            if (projectilePrefab != null)
            {
                projectileObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                projectileObj = CreateCannonballProjectile(spawnPos);
            }

            Projectile projectile = projectileObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(currentTarget, damage, projectileSpeed);
            }
        }

        private void PlayRecoilAnimation()
        {
            if (unitTransforms == null || unitTransforms.Length == 0) return;

            foreach (var unitTransform in unitTransforms)
            {
                if (unitTransform == null) continue;
                StartCoroutine(RecoilCoroutine(unitTransform));
            }
        }

        private IEnumerator RecoilCoroutine(Transform cannon)
        {
            isRecoiling = true;

            Vector3 originalLocalPos = cannon.localPosition;
            // Recoil backward (opposite of forward direction)
            Vector3 recoilDir = -cannon.forward;
            Vector3 recoilPos = originalLocalPos + recoilDir * recoilDistance;

            // Quick recoil back
            float elapsed = 0f;
            float recoilTime = recoilDuration * 0.3f;
            while (elapsed < recoilTime)
            {
                elapsed += Time.deltaTime;
                cannon.localPosition = Vector3.Lerp(originalLocalPos, recoilPos, elapsed / recoilTime);
                yield return null;
            }

            // Slower return
            elapsed = 0f;
            float returnTime = recoilDuration * 0.7f;
            while (elapsed < returnTime)
            {
                elapsed += Time.deltaTime;
                cannon.localPosition = Vector3.Lerp(recoilPos, originalLocalPos, elapsed / returnTime);
                yield return null;
            }

            cannon.localPosition = originalLocalPos;
            isRecoiling = false;
        }

        private GameObject CreateCannonballProjectile(Vector3 position)
        {
            GameObject projectileObj = new GameObject("CannonballProjectile");
            projectileObj.transform.position = position;

            projectileObj.AddComponent<Projectile>();

            // Create a cannonball visual (dark sphere)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "CannonballVisual";
            visual.transform.SetParent(projectileObj.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

            // Remove collider
            var collider = visual.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            // Set dark gray/iron color
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = CreateProjectileMaterial(new Color(0.2f, 0.2f, 0.2f));
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
                Gizmos.color = Color.black;
                Gizmos.DrawLine(transform.position, currentTarget.transform.position);
            }
        }
    }
}
