using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Data;

namespace TowerDefense
{
    public class Enemy : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private EnemyMovement movement;
        [SerializeField] private EnemyHealth health;

        [Header("3D Model")]
        [SerializeField] private float modelYOffset = 0f;

        public float MaxHealth => health?.MaxHealth ?? 0f;
        public float CurrentHealth => health?.CurrentHealth ?? 0f;
        public float MoveSpeed => movement?.MoveSpeed ?? 0f;
        public int KillReward { get; private set; }
        public bool IsDead { get; private set; }

        private Transform modelHolder;
        private GameObject modelInstance;
        private Renderer[] modelRenderers;
        private Vector3 lastPosition;
        private bool isAttacking;
        private EnemyData enemyData;

        // Health bar
        private GameObject healthBarContainer;
        private Transform healthBarFill;
        private bool hasBeenDamaged;
        private const float HEALTH_BAR_WIDTH = 0.6f;
        private const float HEALTH_BAR_HEIGHT = 0.08f;
        private const float HEALTH_BAR_Y_OFFSET = 1.5f;

        // Placeholder colors
        private static readonly Color SOLDIER_COLOR = new Color(0.8f, 0.2f, 0.2f);
        private static readonly Color BOSS_COLOR = new Color(0.5f, 0.1f, 0.1f);

        // Walking animation
        private float walkBobAmount = 0.1f;
        private float walkBobSpeed = 8f;
        private float walkBobPhase;

        private void Awake()
        {
            if (movement == null)
            {
                movement = GetComponent<EnemyMovement>();
            }
            if (health == null)
            {
                health = GetComponent<EnemyHealth>();
            }
        }

        public void Initialize(float maxHealth, float speed, int killReward)
        {
            KillReward = killReward;
            lastPosition = transform.position;

            if (health != null)
            {
                health.Initialize(maxHealth);
            }

            if (movement != null)
            {
                movement.Initialize(speed);
            }
        }

        public void SetEnemyData(EnemyData data)
        {
            enemyData = data;
            SetupModel();
        }

        private void SetupModel()
        {
            // Create holder for the model
            modelHolder = new GameObject("ModelHolder").transform;
            modelHolder.SetParent(transform);
            modelHolder.localPosition = new Vector3(0, modelYOffset, 0);

            float scale = enemyData?.modelScale ?? 0.8f;

            if (enemyData?.modelPrefab != null)
            {
                // Instantiate the 3D model
                modelInstance = UnityEngine.Object.Instantiate(enemyData.modelPrefab, modelHolder);
                if (modelInstance == null)
                {
                    Debug.LogError($"[Enemy] Failed to instantiate model prefab: {enemyData.modelPrefab?.name}");
                    modelInstance = CreatePlaceholderCube();
                    modelInstance.transform.SetParent(modelHolder);
                    modelInstance.transform.localPosition = new Vector3(0, 0.4f, 0);
                    modelInstance.transform.localScale = new Vector3(0.6f, 0.8f, 0.6f) * scale;
                }
                else
                {
                    modelInstance.transform.localPosition = Vector3.zero;
                    modelInstance.transform.localScale = Vector3.one * scale;
                    Debug.Log($"[Enemy] Instantiated model prefab: {enemyData.modelPrefab.name}");
                }
            }
            else
            {
                // Create placeholder cube
                modelInstance = CreatePlaceholderCube();
                modelInstance.transform.SetParent(modelHolder);
                modelInstance.transform.localPosition = new Vector3(0, 0.4f, 0);
                modelInstance.transform.localScale = new Vector3(0.6f, 0.8f, 0.6f) * scale;

                Debug.Log($"[Enemy] Created placeholder cube for {enemyData?.enemyName ?? "Enemy"}");
            }

            // Cache renderers for effects
            modelRenderers = modelInstance.GetComponentsInChildren<Renderer>();
        }

        private GameObject CreatePlaceholderCube()
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "PlaceholderModel";

            // Remove collider
            var collider = cube.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            // Set color - darker red for boss
            var renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                bool isBoss = enemyData != null && enemyData.enemyName.ToLower().Contains("boss");
                Color color = isBoss ? BOSS_COLOR : SOLDIER_COLOR;
                renderer.material = CreatePlaceholderMaterial(color);
            }

            return cube;
        }

        private Material CreatePlaceholderMaterial(Color color)
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

        private void Update()
        {
            if (IsDead) return;

            UpdateFacingDirection();
            UpdateWalkingAnimation();
        }

        private void UpdateWalkingAnimation()
        {
            if (modelHolder == null) return;

            // Calculate movement speed to scale bob intensity
            Vector3 velocity = transform.position - lastPosition;
            float speed = velocity.magnitude / Time.deltaTime;

            // Only bob while moving
            if (speed > 0.1f)
            {
                walkBobPhase += Time.deltaTime * walkBobSpeed;
                float bob = Mathf.Sin(walkBobPhase) * walkBobAmount;
                modelHolder.localPosition = new Vector3(0, modelYOffset + bob, 0);
            }
            else
            {
                // Smoothly return to rest position when stopped
                Vector3 currentPos = modelHolder.localPosition;
                Vector3 restPos = new Vector3(0, modelYOffset, 0);
                modelHolder.localPosition = Vector3.Lerp(currentPos, restPos, Time.deltaTime * 5f);
            }
        }

        private void UpdateFacingDirection()
        {
            if (modelHolder == null) return;

            // Calculate movement direction
            Vector3 movementDir = transform.position - lastPosition;
            lastPosition = transform.position;

            // Only rotate if there's significant movement
            if (movementDir.sqrMagnitude > 0.0001f)
            {
                // Rotate model to face movement direction
                movementDir.y = 0; // Keep rotation on XZ plane
                if (movementDir != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(movementDir);
                    modelHolder.rotation = Quaternion.Slerp(modelHolder.rotation, targetRotation, Time.deltaTime * 10f);
                }
            }
        }

        private void LateUpdate()
        {
            if (IsDead) return;

            UpdateHealthBarPosition();
        }

        private void UpdateHealthBarPosition()
        {
            if (healthBarContainer == null) return;

            // Position health bar above enemy in world space
            Vector3 worldPos = transform.position + Vector3.up * HEALTH_BAR_Y_OFFSET;
            healthBarContainer.transform.position = worldPos;

            // Make health bar face the camera
            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                healthBarContainer.transform.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);
            }
        }

        public void TakeDamage(float damage)
        {
            if (IsDead) return;

            health?.TakeDamage(damage);

            // Show health bar on first damage
            if (!hasBeenDamaged)
            {
                hasBeenDamaged = true;
                CreateHealthBar();
            }

            UpdateHealthBar();

            // Flash red on damage
            StartCoroutine(DamageFlash());

            if (health != null && health.CurrentHealth <= 0)
            {
                Die();
            }
        }

        private System.Collections.IEnumerator DamageFlash()
        {
            if (modelRenderers == null || modelRenderers.Length == 0) yield break;

            Color[] originalColors = new Color[modelRenderers.Length];
            for (int i = 0; i < modelRenderers.Length; i++)
            {
                if (modelRenderers[i] != null && modelRenderers[i].material != null)
                {
                    originalColors[i] = modelRenderers[i].material.color;
                    modelRenderers[i].material.color = Color.white;
                }
            }

            yield return new WaitForSeconds(0.05f);

            for (int i = 0; i < modelRenderers.Length; i++)
            {
                if (modelRenderers[i] != null && modelRenderers[i].material != null)
                {
                    modelRenderers[i].material.color = originalColors[i];
                }
            }
        }

        public void ReachedGoal()
        {
            if (IsDead) return;

            IsDead = true;

            if (healthBarContainer != null)
            {
                Destroy(healthBarContainer);
            }

            GameEvents.InvokeEnemyReachedGoal(this);
            Destroy(gameObject);
        }

        private void Die()
        {
            if (IsDead) return;

            IsDead = true;

            if (healthBarContainer != null)
            {
                Destroy(healthBarContainer);
            }

            GameEvents.InvokeEnemyKilled(this);

            // Play death animation
            StartCoroutine(DeathAnimation());
        }

        private System.Collections.IEnumerator DeathAnimation()
        {
            float duration = 0.5f;
            float elapsed = 0f;

            Vector3 originalScale = modelHolder != null ? modelHolder.localScale : Vector3.one;
            Vector3 originalPos = transform.position;

            // Disable movement during death
            if (movement != null)
            {
                movement.enabled = false;
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Scale down
                if (modelHolder != null)
                {
                    modelHolder.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
                }

                // Sink into ground
                transform.position = originalPos + Vector3.down * (t * 0.3f);

                yield return null;
            }

            Destroy(gameObject);
        }

        public void SetAttacking(bool attacking)
        {
            if (isAttacking == attacking) return;
            isAttacking = attacking;

            // Could trigger attack animation here when animator is added
        }

        private void CreateHealthBar()
        {
            healthBarContainer = new GameObject("HealthBar");
            healthBarContainer.transform.localScale = Vector3.one;

            // Background (dark gray)
            GameObject background = GameObject.CreatePrimitive(PrimitiveType.Quad);
            background.name = "Background";
            background.transform.SetParent(healthBarContainer.transform);
            background.transform.localPosition = Vector3.zero;
            background.transform.localScale = new Vector3(HEALTH_BAR_WIDTH, HEALTH_BAR_HEIGHT, 1f);

            var bgCollider = background.GetComponent<Collider>();
            if (bgCollider != null) Destroy(bgCollider);

            var bgRenderer = background.GetComponent<Renderer>();
            if (bgRenderer != null)
            {
                bgRenderer.material = CreatePlaceholderMaterial(new Color(0.2f, 0.2f, 0.2f, 0.95f));
            }

            // Fill bar (green to red based on health)
            GameObject fill = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fill.name = "Fill";
            fill.transform.SetParent(healthBarContainer.transform);
            fill.transform.localPosition = new Vector3(0, 0, -0.01f);
            fill.transform.localScale = new Vector3(HEALTH_BAR_WIDTH * 0.95f, HEALTH_BAR_HEIGHT * 0.7f, 1f);

            var fillCollider = fill.GetComponent<Collider>();
            if (fillCollider != null) Destroy(fillCollider);

            var fillRenderer = fill.GetComponent<Renderer>();
            if (fillRenderer != null)
            {
                fillRenderer.material = CreatePlaceholderMaterial(Color.green);
            }

            healthBarFill = fill.transform;

            Debug.Log($"[Enemy] Health bar created");
        }

        private void UpdateHealthBar()
        {
            if (healthBarFill == null || health == null) return;

            float healthPercent = health.CurrentHealth / health.MaxHealth;
            healthPercent = Mathf.Clamp01(healthPercent);

            // Scale the fill bar
            float fillWidth = HEALTH_BAR_WIDTH * 0.95f * healthPercent;
            Vector3 scale = healthBarFill.localScale;
            scale.x = fillWidth;
            healthBarFill.localScale = scale;

            // Offset position so it fills from left
            float offset = (HEALTH_BAR_WIDTH * 0.95f - fillWidth) / 2f;
            healthBarFill.localPosition = new Vector3(-offset, 0, -0.01f);

            // Update color (green -> yellow -> red)
            var fillRenderer = healthBarFill.GetComponent<Renderer>();
            if (fillRenderer != null)
            {
                Color healthColor;
                if (healthPercent > 0.5f)
                {
                    float t = (healthPercent - 0.5f) * 2f;
                    healthColor = Color.Lerp(Color.yellow, Color.green, t);
                }
                else
                {
                    float t = healthPercent * 2f;
                    healthColor = Color.Lerp(Color.red, Color.yellow, t);
                }
                fillRenderer.material.color = healthColor;
                if (fillRenderer.material.HasProperty("_BaseColor"))
                {
                    fillRenderer.material.SetColor("_BaseColor", healthColor);
                }
            }
        }
    }
}
