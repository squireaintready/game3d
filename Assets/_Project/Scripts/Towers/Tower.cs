using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using TowerDefense.Core;
using TowerDefense.Data;

namespace TowerDefense
{
    public class Tower : MonoBehaviour
    {
        [Header("Tower Data")]
        [SerializeField] protected TowerData towerData;

        [Header("Visual")]
        [SerializeField] protected GameObject rangeIndicator;
        [SerializeField] protected float modelYOffset = 0f;

        public TowerData Data => towerData;
        public TowerType TowerType => towerData?.towerType ?? TowerType.None;
        public Vector2Int GridPosition { get; private set; }
        public int Cost => towerData?.cost ?? 0;
        public int SellValue => towerData?.GetSellValue(CurrentLevel) ?? 0;

        // Upgrade system
        public int CurrentLevel { get; protected set; } = 1;
        public int MaxLevel => TowerData.MaxLevel;
        public bool CanUpgrade => towerData != null && towerData.CanUpgrade && CurrentLevel < MaxLevel;
        public int UpgradeCost => towerData?.GetUpgradeCost(CurrentLevel) ?? 0;
        public float CurrentDamage => towerData?.GetDamageForLevel(CurrentLevel) ?? 0f;
        public float CurrentRange => towerData?.GetRangeForLevel(CurrentLevel) ?? 0f;

        [Header("Health")]
        [SerializeField] protected float maxHealth = 100f;
        public float CurrentHealth { get; protected set; }
        public float MaxHealth => maxHealth;
        public bool IsDestroyed { get; protected set; }

        // Protection status - ground towers are vulnerable, wall-protected towers are not
        public bool IsProtected { get; protected set; } = true;
        public bool IsVulnerable => !IsProtected && towerData != null && towerData.IsAttackTower;

        // HP constants
        protected const float GROUND_TOWER_HP = 30f;
        protected const float PROTECTED_TOWER_HP = 999999f; // Effectively invulnerable

        protected bool isSelected;
        protected Transform modelHolder;
        protected GameObject modelInstance;
        protected Renderer[] modelRenderers;

        // Composite model components (wall + units)
        protected GameObject wallInstance;
        protected GameObject[] unitInstances;
        protected Transform[] unitTransforms;

        // Placeholder colors
        protected static readonly Color ARCHER_COLOR = new Color(0.2f, 0.4f, 0.8f);
        protected static readonly Color WALL_COLOR = new Color(0.5f, 0.5f, 0.5f);
        protected static readonly Color MAGE_COLOR = new Color(0.6f, 0.2f, 0.8f);
        protected static readonly Color CANNON_COLOR = new Color(0.4f, 0.4f, 0.4f);

        protected virtual void Awake()
        {
            // Add NavMeshObstacle so enemies path around towers
            SetupNavMeshObstacle();
            // Add BoxCollider for raycast selection
            SetupSelectionCollider();
        }

        private void SetupSelectionCollider()
        {
            BoxCollider collider = GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<BoxCollider>();
            }

            // Size collider to fit the tile
            float cellSize = Core.GridManager.Instance?.CellSize ?? 1f;
            collider.size = new Vector3(cellSize * 0.9f, cellSize, cellSize * 0.9f);
            collider.center = new Vector3(0, cellSize * 0.5f, 0);
        }

        private void SetupNavMeshObstacle()
        {
            NavMeshObstacle obstacle = GetComponent<NavMeshObstacle>();
            if (obstacle == null)
            {
                obstacle = gameObject.AddComponent<NavMeshObstacle>();
            }

            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = new Vector3(0.9f, 1f, 0.9f);
            obstacle.center = new Vector3(0, 0.5f, 0);

            Debug.Log($"[Tower] NavMeshObstacle configured for {gameObject.name}");
        }

        public virtual void Initialize(TowerData data, Vector2Int gridPos)
        {
            Initialize(data, gridPos, true); // Default to protected
        }

        public virtual void Initialize(TowerData data, Vector2Int gridPos, bool isProtected)
        {
            towerData = data;
            GridPosition = gridPos;
            IsProtected = isProtected;
            IsDestroyed = false;

            // Set HP based on protection status
            if (data.IsAttackTower)
            {
                maxHealth = isProtected ? PROTECTED_TOWER_HP : GROUND_TOWER_HP;
            }
            else
            {
                maxHealth = PROTECTED_TOWER_HP; // Walls are always protected
            }
            CurrentHealth = maxHealth;

            // Create the 3D model
            SetupModel();

            ShowRangeIndicator(false);

            string protectionStatus = IsProtected ? "protected" : "vulnerable";
            Debug.Log($"[Tower] Initialized {data?.towerName ?? "Tower"} at {gridPos} ({protectionStatus}, HP: {maxHealth})");
        }

        protected virtual void SetupModel()
        {
            // Create holder for the model
            modelHolder = new GameObject("ModelHolder").transform;
            modelHolder.SetParent(transform);
            modelHolder.localPosition = new Vector3(0, modelYOffset, 0);

            // Debug info
            Debug.Log($"[Tower.SetupModel] {towerData?.towerName}: IsAttackTower={towerData?.IsAttackTower}, " +
                      $"unitPrefab={(towerData?.unitPrefab != null ? towerData.unitPrefab.name : "NULL")}, " +
                      $"wallPrefab={(towerData?.wallPrefab != null ? towerData.wallPrefab.name : "NULL")}, " +
                      $"modelPrefab={(towerData?.modelPrefab != null ? towerData.modelPrefab.name : "NULL")}, " +
                      $"IsProtected={IsProtected}");

            // For attack towers, check if protected (wall+unit) or ground (unit only)
            if (towerData?.IsAttackTower == true && towerData?.unitPrefab != null)
            {
                // Check for wall prefab - prefer modelPrefab since wallPrefab reference can be broken
                bool hasWallPrefab = towerData?.modelPrefab != null || towerData?.wallPrefab != null;

                if (IsProtected && hasWallPrefab)
                {
                    // Wall-protected: show wall + unit
                    Debug.Log($"[Tower.SetupModel] -> SetupCompositeModel (protected attack tower)");
                    SetupCompositeModel();
                }
                else
                {
                    // Ground placement: show unit only (no wall)
                    Debug.Log($"[Tower.SetupModel] -> SetupUnitOnlyModel (ground placement)");
                    SetupUnitOnlyModel();
                }
            }
            else if (towerData?.modelPrefab != null)
            {
                // Simple single-prefab model (for walls and other towers)
                Debug.Log($"[Tower.SetupModel] -> SetupSimpleModel");
                SetupSimpleModel();
            }
            else
            {
                // Create placeholder cube
                Debug.Log($"[Tower.SetupModel] -> SetupPlaceholderModel (no prefabs found)");
                SetupPlaceholderModel();
            }

            // Cache renderers for effects (from all model instances)
            CacheRenderers();
        }

        protected virtual void SetupSimpleModel()
        {
            // Scale relative to cell size so model fits tile
            float cellSize = Core.GridManager.Instance?.CellSize ?? 1f;

            // Check for non-uniform scale vector override
            Vector3 scaleVector;
            if (towerData != null && towerData.modelScaleVector != Vector3.zero)
            {
                // Use non-uniform scale (allows different width/height/depth)
                scaleVector = towerData.modelScaleVector * cellSize;
            }
            else
            {
                // Use uniform scale
                float scale = (towerData?.modelScale ?? 1f) * cellSize;
                scaleVector = Vector3.one * scale;
            }

            // Instantiate model prefab
            if (towerData.modelPrefab == null)
            {
                Debug.LogError($"[Tower] modelPrefab is NULL in TowerData: {towerData?.name}");
                SetupPlaceholderModel();
                return;
            }
            modelInstance = UnityEngine.Object.Instantiate(towerData.modelPrefab, modelHolder);
            if (modelInstance == null)
            {
                Debug.LogError($"[Tower] Failed to instantiate model prefab: {towerData.modelPrefab?.name}");
                SetupPlaceholderModel();
                return;
            }
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localScale = scaleVector;

            // Apply rotation offset if specified
            if (towerData.modelRotationOffset != 0)
            {
                modelInstance.transform.localRotation = Quaternion.Euler(0, towerData.modelRotationOffset, 0);
            }

            Debug.Log($"[Tower] Instantiated model prefab: {towerData.modelPrefab.name}");
        }

        protected virtual void SetupCompositeModel()
        {
            Debug.Log($"[Tower.SetupCompositeModel] Starting for {towerData?.towerName}");

            // Scale relative to cell size so walls fit tiles
            float cellSize = Core.GridManager.Instance?.CellSize ?? 1f;

            // Check for non-uniform wall scale vector override
            Vector3 wallScaleVec;
            if (towerData != null && towerData.wallScaleVector != Vector3.zero)
            {
                wallScaleVec = towerData.wallScaleVector * cellSize;
            }
            else
            {
                float wallScale = (towerData?.wallScale ?? 1f) * cellSize;
                wallScaleVec = Vector3.one * wallScale;
            }

            float unitScale = (towerData?.unitScale ?? 1f) * cellSize;
            int unitCount = towerData?.unitCount ?? 1;
            Vector3 unitOffset = (towerData?.unitOffset ?? new Vector3(0f, 0.5f, 0f)) * cellSize;

            Debug.Log($"[Tower.SetupCompositeModel] cellSize={cellSize}, wallScaleVec={wallScaleVec}, unitScale={unitScale}, unitOffset={unitOffset}");

            // WALL BASE: Use modelPrefab (which works reliably) instead of wallPrefab
            // This is the same prefab that works for standalone walls
            GameObject wallPrefabToUse = towerData.modelPrefab ?? towerData.wallPrefab;

            Debug.Log($"[Tower.SetupCompositeModel] Wall prefab: modelPrefab={towerData.modelPrefab != null}, wallPrefab={towerData.wallPrefab != null}, using={(wallPrefabToUse != null ? wallPrefabToUse.name : "NULL")}");

            if (wallPrefabToUse != null)
            {
                wallInstance = TryInstantiatePrefab(wallPrefabToUse, "wall_base");
            }

            if (wallInstance == null)
            {
                Debug.LogWarning($"[Tower.SetupCompositeModel] Wall prefab instantiation failed, creating placeholder");
                wallInstance = CreatePlaceholderCube();
            }

            wallInstance.transform.SetParent(modelHolder);
            wallInstance.transform.localPosition = Vector3.zero;
            wallInstance.transform.localScale = wallScaleVec;
            wallInstance.name = "WallBase";

            Debug.Log($"[Tower.SetupCompositeModel] Wall created: localPos={wallInstance.transform.localPosition}, scale={wallInstance.transform.localScale}");

            // UNITS ON TOP: Use unitPrefab (which works for ground towers)
            unitInstances = new GameObject[unitCount];
            unitTransforms = new Transform[unitCount];

            Debug.Log($"[Tower.SetupCompositeModel] Creating {unitCount} units from prefab: {(towerData.unitPrefab != null ? towerData.unitPrefab.name : "NULL")}");

            for (int i = 0; i < unitCount; i++)
            {
                GameObject unit = TryInstantiatePrefab(towerData.unitPrefab, $"unit_{i}");

                if (unit == null)
                {
                    Debug.LogWarning($"[Tower.SetupCompositeModel] Unit {i} prefab failed, creating placeholder");
                    unit = CreateUnitPlaceholder(towerData?.towerType ?? TowerType.Archer);
                }

                unit.transform.SetParent(modelHolder);
                unit.name = $"Unit_{i}";
                unit.transform.localScale = Vector3.one * unitScale;

                // Position units ON TOP of the wall with offset
                Vector3 pos = unitOffset;
                if (unitCount > 1)
                {
                    float spread = 0.3f * cellSize;
                    float xOffset = (i - (unitCount - 1) / 2f) * spread;
                    pos += new Vector3(xOffset, 0f, 0f);
                }
                unit.transform.localPosition = pos;
                unit.SetActive(true);

                unitInstances[i] = unit;
                unitTransforms[i] = unit.transform;

                Debug.Log($"[Tower.SetupCompositeModel] Unit {i}: localPos={pos}, scale={unitScale}");
            }

            modelInstance = wallInstance;
            Debug.Log($"[Tower.SetupCompositeModel] COMPLETE: wall at scale {wallScaleVec} + {unitCount} units at Y={unitOffset.y}");
        }

        /// <summary>
        /// Try multiple approaches to instantiate a prefab
        /// </summary>
        private GameObject TryInstantiatePrefab(GameObject prefab, string debugName)
        {
            if (prefab == null)
            {
                Debug.LogError($"[Tower] Prefab is null for {debugName}");
                return null;
            }

            // Check if it's a "fake null" (Unity's destroyed object that still != null)
            try
            {
                string testName = prefab.name; // This will throw if it's fake null
                Debug.Log($"[Tower] Prefab {debugName} name='{testName}', type={prefab.GetType().Name}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Tower] Prefab {debugName} appears to be destroyed/invalid: {e.Message}");
                return null;
            }

            // Approach 1: Standard Instantiate
            try
            {
                GameObject instance = UnityEngine.Object.Instantiate(prefab);
                if (instance != null)
                {
                    Debug.Log($"[Tower] Successfully instantiated {debugName} using standard Instantiate");
                    return instance;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Tower] Standard Instantiate failed for {debugName}: {e.Message}");
            }

            // Approach 2: Cast to Object first
            try
            {
                UnityEngine.Object prefabObj = prefab as UnityEngine.Object;
                UnityEngine.Object instanceObj = UnityEngine.Object.Instantiate(prefabObj);
                GameObject instance = instanceObj as GameObject;
                if (instance != null)
                {
                    Debug.Log($"[Tower] Successfully instantiated {debugName} using Object cast approach");
                    return instance;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Tower] Object cast Instantiate failed for {debugName}: {e.Message}");
            }

            Debug.LogError($"[Tower] All instantiation approaches failed for {debugName}");
            return null;
        }

        /// <summary>
        /// Create a colored placeholder for units when prefab loading fails
        /// </summary>
        private GameObject CreateUnitPlaceholder(TowerType towerType)
        {
            GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            placeholder.name = "UnitPlaceholder";

            // Remove collider
            var collider = placeholder.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            // Set color based on tower type
            var renderer = placeholder.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color color = towerType switch
                {
                    TowerType.Archer => new Color(0.2f, 0.6f, 0.2f), // Green for archer
                    TowerType.Mage => new Color(0.6f, 0.2f, 0.8f),   // Purple for mage
                    TowerType.Cannon => new Color(0.3f, 0.3f, 0.3f), // Gray for cannon
                    _ => Color.white
                };
                renderer.material = CreatePlaceholderMaterial(color);
            }

            return placeholder;
        }

        /// <summary>
        /// Setup model for ground-placed attack towers (unit only, no wall base)
        /// </summary>
        protected virtual void SetupUnitOnlyModel()
        {
            // Scale relative to cell size
            float cellSize = Core.GridManager.Instance?.CellSize ?? 1f;
            float unitScale = (towerData?.unitScale ?? 1f) * cellSize;
            int unitCount = towerData?.unitCount ?? 1;

            // Units are placed directly on ground (no wall offset)
            Vector3 groundOffset = new Vector3(0f, 0f, 0f);

            unitInstances = new GameObject[unitCount];
            unitTransforms = new Transform[unitCount];

            Debug.Log($"[Tower.SetupUnitOnlyModel] Creating {unitCount} ground units from prefab: {(towerData?.unitPrefab != null ? towerData.unitPrefab.name : "NULL")}");

            for (int i = 0; i < unitCount; i++)
            {
                GameObject unit = TryInstantiatePrefab(towerData?.unitPrefab, $"ground_unit_{i}");

                if (unit == null)
                {
                    Debug.LogWarning($"[Tower] Ground unit {i} prefab failed, creating placeholder");
                    unit = CreateUnitPlaceholder(towerData?.towerType ?? TowerType.Archer);
                }

                unit.transform.SetParent(modelHolder);
                unit.name = $"Unit_{i}";
                unit.transform.localScale = Vector3.one * unitScale;

                // Position units on ground, spread them out if multiple
                Vector3 pos = groundOffset;
                if (unitCount > 1)
                {
                    float spread = 0.3f * cellSize;
                    float xOffset = (i - (unitCount - 1) / 2f) * spread;
                    pos += new Vector3(xOffset, 0f, 0f);
                }
                unit.transform.localPosition = pos;
                unit.SetActive(true);

                unitInstances[i] = unit;
                unitTransforms[i] = unit.transform;
            }

            // Use first unit as main model instance
            if (unitInstances.Length > 0 && unitInstances[0] != null)
            {
                modelInstance = unitInstances[0];
            }

            Debug.Log($"[Tower] SetupUnitOnlyModel complete: {unitCount} ground unit(s) (vulnerable)");
        }

        protected virtual void SetupPlaceholderModel()
        {
            float scale = towerData?.modelScale ?? 1f;

            modelInstance = CreatePlaceholderCube();
            modelInstance.transform.SetParent(modelHolder);
            modelInstance.transform.localPosition = new Vector3(0, 0.5f, 0);
            modelInstance.transform.localScale = Vector3.one * scale;

            Debug.Log($"[Tower] Created placeholder cube for {towerData?.towerName ?? "Tower"}");
        }

        protected virtual void CacheRenderers()
        {
            var rendererList = new System.Collections.Generic.List<Renderer>();

            if (modelInstance != null)
            {
                rendererList.AddRange(modelInstance.GetComponentsInChildren<Renderer>());
            }

            if (unitInstances != null)
            {
                foreach (var unit in unitInstances)
                {
                    if (unit != null)
                    {
                        rendererList.AddRange(unit.GetComponentsInChildren<Renderer>());
                    }
                }
            }

            modelRenderers = rendererList.ToArray();
        }

        protected virtual GameObject CreatePlaceholderCube()
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "PlaceholderModel";

            // Remove collider (NavMeshObstacle handles collision)
            var collider = cube.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            // Set color based on tower type
            var renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color color = GetPlaceholderColor();
                renderer.material = CreatePlaceholderMaterial(color);
            }

            return cube;
        }

        protected Color GetPlaceholderColor()
        {
            if (towerData == null) return WALL_COLOR;

            return towerData.towerType switch
            {
                TowerType.Archer => ARCHER_COLOR,
                TowerType.Mage => MAGE_COLOR,
                TowerType.Cannon => CANNON_COLOR,
                TowerType.Wall => WALL_COLOR,
                _ => WALL_COLOR
            };
        }

        protected Material CreatePlaceholderMaterial(Color color)
        {
            // Try to find a URP-compatible shader
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

            // Fallback
            Material fallback = new Material(Shader.Find("Sprites/Default"));
            fallback.color = color;
            return fallback;
        }

        protected virtual void Update()
        {
            // Apply idle animation to units (subtle bob)
            UpdateIdleAnimation();
        }

        protected virtual void UpdateIdleAnimation()
        {
            if (unitTransforms == null || unitTransforms.Length == 0) return;

            // Scale relative to cell size
            float cellSize = Core.GridManager.Instance?.CellSize ?? 1f;

            // Subtle idle bob for units
            float bobAmount = 0.02f * cellSize;
            float bobSpeed = 2f;
            float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmount;

            // Ground-placed units have no Y offset, wall-placed units use unitOffset (scaled)
            Vector3 baseOffset = IsProtected
                ? (towerData?.unitOffset ?? new Vector3(0f, 0.5f, 0f)) * cellSize
                : Vector3.zero;
            int unitCount = unitTransforms.Length;

            for (int i = 0; i < unitCount; i++)
            {
                if (unitTransforms[i] == null) continue;

                Vector3 pos = baseOffset + new Vector3(0f, bob, 0f);
                if (unitCount > 1)
                {
                    float spread = 0.3f * cellSize;
                    float xOffset = (i - (unitCount - 1) / 2f) * spread;
                    pos += new Vector3(xOffset, 0f, 0f);
                }
                unitTransforms[i].localPosition = pos;
            }
        }

        /// <summary>
        /// Rotate units to face a target position. Used for attack animations.
        /// </summary>
        protected virtual void RotateUnitsToward(Vector3 targetPosition, float rotationSpeed)
        {
            if (unitTransforms == null) return;

            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);

            foreach (var unitTransform in unitTransforms)
            {
                if (unitTransform == null) continue;
                unitTransform.rotation = Quaternion.Slerp(unitTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// Play attack animation (scale pulse) on units.
        /// </summary>
        protected virtual void PlayAttackAnimation()
        {
            if (unitTransforms == null || unitTransforms.Length == 0) return;

            foreach (var unitTransform in unitTransforms)
            {
                if (unitTransform == null) continue;
                StartCoroutine(AttackPulse(unitTransform));
            }
        }

        private IEnumerator AttackPulse(Transform unit)
        {
            // Scale relative to cell size
            float cellSize = Core.GridManager.Instance?.CellSize ?? 1f;
            float unitScale = (towerData?.unitScale ?? 1f) * cellSize;
            Vector3 originalScale = Vector3.one * unitScale;
            Vector3 pulseScale = originalScale * 1.15f;

            // Scale up
            float duration = 0.1f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                unit.localScale = Vector3.Lerp(originalScale, pulseScale, elapsed / duration);
                yield return null;
            }

            // Scale back down
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                unit.localScale = Vector3.Lerp(pulseScale, originalScale, elapsed / duration);
                yield return null;
            }

            unit.localScale = originalScale;
        }

        public virtual void Select()
        {
            isSelected = true;
            Debug.Log($"[Tower] Select called on {towerData?.towerName}, IsAttackTower={towerData?.IsAttackTower}, CurrentRange={CurrentRange}");
            // Only show range indicator for attack towers (Archer, Mage, Cannon)
            if (towerData != null && towerData.IsAttackTower)
            {
                ShowRangeIndicator(true);
            }
            GameEvents.InvokeTowerSelected(this);
        }

        public virtual void Deselect()
        {
            isSelected = false;
            ShowRangeIndicator(false);
        }

        public void ShowRangeIndicator(bool show)
        {
            Debug.Log($"[Tower] ShowRangeIndicator({show}) - rangeIndicator exists: {rangeIndicator != null}, CurrentRange: {CurrentRange}");

            // Create range indicator if it doesn't exist
            if (rangeIndicator == null && show)
            {
                CreateRangeIndicator();
            }

            if (rangeIndicator != null)
            {
                rangeIndicator.SetActive(show);
                if (show && towerData != null)
                {
                    // Use CurrentRange which accounts for level upgrades
                    float diameter = CurrentRange * 2f;
                    // Cylinder scale: X and Z control diameter, Y controls height
                    // Use 0.05f for Y to make it visible but still flat
                    rangeIndicator.transform.localScale = new Vector3(diameter, 0.05f, diameter);
                    Debug.Log($"[Tower] Range indicator scale set to: ({diameter}, 0.05, {diameter}), worldPos: {rangeIndicator.transform.position}");
                }
            }
            else if (show)
            {
                Debug.LogError("[Tower] Range indicator is null after CreateRangeIndicator! This should not happen.");
            }
        }

        protected void CreateRangeIndicator()
        {
            // Create a cylinder for the range indicator
            rangeIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rangeIndicator.name = "RangeIndicator";
            rangeIndicator.transform.SetParent(transform);
            rangeIndicator.transform.localPosition = new Vector3(0, 0.02f, 0); // Slightly above ground

            // Remove collider so it doesn't interfere with raycasts
            var collider = rangeIndicator.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            // Setup material - need a URP-compatible transparent shader
            var renderer = rangeIndicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                // For URP, we need to use a proper transparent material
                // Try URP shaders first, then fallback to others
                Shader shader = null;
                Material mat = null;

                // Approach 1: URP Unlit with transparency
                shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader != null)
                {
                    mat = new Material(shader);
                    // Enable transparency for URP Unlit
                    mat.SetFloat("_Surface", 1); // 0 = Opaque, 1 = Transparent
                    mat.SetFloat("_Blend", 0);   // 0 = Alpha, 1 = Premultiply, 2 = Additive, 3 = Multiply
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.renderQueue = 3000; // Transparent queue
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                }

                // Approach 2: Standard shader with transparency
                if (mat == null)
                {
                    shader = Shader.Find("Standard");
                    if (shader != null)
                    {
                        mat = new Material(shader);
                        mat.SetFloat("_Mode", 3); // Transparent mode
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.SetInt("_ZWrite", 0);
                        mat.DisableKeyword("_ALPHATEST_ON");
                        mat.EnableKeyword("_ALPHABLEND_ON");
                        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        mat.renderQueue = 3000;
                    }
                }

                // Approach 3: Sprites/Default (works for simple cases)
                if (mat == null)
                {
                    shader = Shader.Find("Sprites/Default");
                    if (shader != null)
                    {
                        mat = new Material(shader);
                    }
                }

                if (mat != null)
                {
                    Color rangeColor = new Color(0.4f, 1f, 0.5f, 0.15f); // Light green, more transparent
                    mat.color = rangeColor;

                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", rangeColor);

                    renderer.material = mat;
                    Debug.Log($"[Tower] Range indicator created with shader: {mat.shader?.name ?? "NULL"}, color: {rangeColor}");
                }
                else
                {
                    Debug.LogError("[Tower] Failed to create range indicator material - no compatible shader found!");
                }
            }

            rangeIndicator.SetActive(false);
            Debug.Log($"[Tower] Range indicator GameObject created: {rangeIndicator != null}, active: {rangeIndicator?.activeSelf}");
        }

        public void Sell()
        {
            // Refund based on total invested at current level
            int refund = SellValue;
            EconomyManager.Instance?.AddCurrency(refund);
            GridManager.Instance?.RemoveTower(GridPosition);
            GameEvents.InvokeTowerSold(this);
            Debug.Log($"[Tower] Sold {towerData?.towerName} Level {CurrentLevel} for {refund}");
            Destroy(gameObject);
        }

        public bool TryUpgrade()
        {
            if (!CanUpgrade)
            {
                Debug.Log($"[Tower] Cannot upgrade {towerData?.towerName} - max level or not upgradeable");
                return false;
            }

            int cost = UpgradeCost;
            if (EconomyManager.Instance == null || EconomyManager.Instance.CurrentCurrency < cost)
            {
                Debug.Log($"[Tower] Cannot afford upgrade - need {cost}, have {EconomyManager.Instance?.CurrentCurrency ?? 0}");
                return false;
            }

            // Spend the currency
            EconomyManager.Instance.SpendCurrency(cost);

            // Increase level
            CurrentLevel++;

            // Update range indicator if shown
            if (rangeIndicator != null && rangeIndicator.activeSelf)
            {
                float diameter = CurrentRange * 2f;
                rangeIndicator.transform.localScale = new Vector3(diameter, 0.01f, diameter);
            }

            Debug.Log($"[Tower] Upgraded {towerData?.towerName} to Level {CurrentLevel} - Damage: {CurrentDamage:F1}, Range: {CurrentRange:F1}");
            GameEvents.InvokeTowerUpgraded(this);
            return true;
        }

        public virtual void TakeDamage(float damage)
        {
            if (IsDestroyed) return;

            // Protected towers (on walls) cannot be damaged
            if (IsProtected)
            {
                return;
            }

            CurrentHealth -= damage;

            // Visual feedback - flash red
            StartCoroutine(DamageFlash());

            if (CurrentHealth <= 0)
            {
                CurrentHealth = 0;
                DestroyTower();
            }
        }

        /// <summary>
        /// Upgrade a ground tower to protected status by adding a wall base
        /// </summary>
        public virtual void UpgradeToProtected()
        {
            Debug.Log($"[Tower.UpgradeToProtected] Starting upgrade for {towerData?.towerName}, IsProtected={IsProtected}");

            if (IsProtected)
            {
                Debug.Log($"[Tower.UpgradeToProtected] Already protected, skipping");
                return; // Already protected
            }

            // Use modelPrefab (which works reliably) for the wall base
            GameObject wallPrefabToUse = towerData?.modelPrefab ?? towerData?.wallPrefab;

            Debug.Log($"[Tower.UpgradeToProtected] wallPrefabToUse: modelPrefab={towerData?.modelPrefab != null}, wallPrefab={towerData?.wallPrefab != null}");

            if (wallPrefabToUse == null)
            {
                Debug.LogError($"[Tower.UpgradeToProtected] No wall prefab available, cannot upgrade");
                return; // No wall to add
            }

            IsProtected = true;
            maxHealth = PROTECTED_TOWER_HP;
            CurrentHealth = maxHealth;

            // Scale relative to cell size
            float cellSize = Core.GridManager.Instance?.CellSize ?? 1f;

            // Check for non-uniform wall scale vector override
            Vector3 wallScaleVec;
            if (towerData != null && towerData.wallScaleVector != Vector3.zero)
            {
                wallScaleVec = towerData.wallScaleVector * cellSize;
            }
            else
            {
                float wallScale = (towerData?.wallScale ?? 1f) * cellSize;
                wallScaleVec = Vector3.one * wallScale;
            }

            Vector3 unitOffset = (towerData?.unitOffset ?? new Vector3(0f, 0.5f, 0f)) * cellSize;

            Debug.Log($"[Tower.UpgradeToProtected] cellSize={cellSize}, wallScaleVec={wallScaleVec}, unitOffset={unitOffset}");

            // Ensure modelHolder exists
            if (modelHolder == null)
            {
                Debug.LogWarning($"[Tower.UpgradeToProtected] modelHolder is null, creating new one");
                modelHolder = new GameObject("ModelHolder").transform;
                modelHolder.SetParent(transform);
                modelHolder.localPosition = new Vector3(0, modelYOffset, 0);
            }

            // Instantiate wall using the reliable prefab
            wallInstance = TryInstantiatePrefab(wallPrefabToUse, "upgrade_wall");

            if (wallInstance == null)
            {
                Debug.LogWarning($"[Tower.UpgradeToProtected] Wall prefab instantiation failed, creating placeholder");
                wallInstance = CreatePlaceholderCube();
            }

            wallInstance.transform.SetParent(modelHolder);
            wallInstance.transform.localPosition = Vector3.zero;
            wallInstance.transform.localScale = wallScaleVec;
            wallInstance.name = "WallBase";

            Debug.Log($"[Tower.UpgradeToProtected] Wall created at localPos={wallInstance.transform.localPosition}, scale={wallInstance.transform.localScale}");

            // Move units up onto wall
            if (unitTransforms != null && unitTransforms.Length > 0)
            {
                int unitCount = unitTransforms.Length;
                Debug.Log($"[Tower.UpgradeToProtected] Moving {unitCount} units to offset {unitOffset}");

                for (int i = 0; i < unitCount; i++)
                {
                    if (unitTransforms[i] == null)
                    {
                        Debug.LogWarning($"[Tower.UpgradeToProtected] unitTransforms[{i}] is null");
                        continue;
                    }

                    Vector3 pos = unitOffset;
                    if (unitCount > 1)
                    {
                        float spread = 0.3f * cellSize;
                        float xOffset = (i - (unitCount - 1) / 2f) * spread;
                        pos += new Vector3(xOffset, 0f, 0f);
                    }
                    unitTransforms[i].localPosition = pos;
                    Debug.Log($"[Tower.UpgradeToProtected] Unit {i} moved to localPos={pos}");
                }
            }
            else
            {
                Debug.LogWarning($"[Tower.UpgradeToProtected] No unit transforms found! unitTransforms={unitTransforms != null}, Length={unitTransforms?.Length ?? 0}");
            }

            // Re-cache renderers
            CacheRenderers();

            Debug.Log($"[Tower.UpgradeToProtected] COMPLETE - {towerData?.towerName} upgraded to protected status");
        }

        /// <summary>
        /// Downgrade a protected tower back to vulnerable status (remove wall base)
        /// Used by undo system
        /// </summary>
        public virtual void DowngradeToVulnerable()
        {
            Debug.Log($"[Tower.DowngradeToVulnerable] Starting downgrade for {towerData?.towerName}, IsProtected={IsProtected}");

            if (!IsProtected)
            {
                Debug.Log($"[Tower.DowngradeToVulnerable] Already vulnerable, skipping");
                return;
            }

            // Only attack towers can be downgraded
            if (!towerData.IsAttackTower)
            {
                Debug.LogWarning($"[Tower.DowngradeToVulnerable] Cannot downgrade non-attack tower");
                return;
            }

            IsProtected = false;
            maxHealth = GROUND_TOWER_HP;
            CurrentHealth = maxHealth;

            // Remove wall instance
            if (wallInstance != null)
            {
                Destroy(wallInstance);
                wallInstance = null;
                Debug.Log($"[Tower.DowngradeToVulnerable] Destroyed wall instance");
            }

            // Move units back to ground level
            float cellSize = Core.GridManager.Instance?.CellSize ?? 1f;

            if (unitTransforms != null && unitTransforms.Length > 0)
            {
                int unitCount = unitTransforms.Length;
                for (int i = 0; i < unitCount; i++)
                {
                    if (unitTransforms[i] == null) continue;

                    Vector3 pos = Vector3.zero;
                    if (unitCount > 1)
                    {
                        float spread = 0.3f * cellSize;
                        float xOffset = (i - (unitCount - 1) / 2f) * spread;
                        pos += new Vector3(xOffset, 0f, 0f);
                    }
                    unitTransforms[i].localPosition = pos;
                }
                Debug.Log($"[Tower.DowngradeToVulnerable] Moved {unitCount} units to ground level");
            }

            // Update model instance reference
            if (unitInstances != null && unitInstances.Length > 0 && unitInstances[0] != null)
            {
                modelInstance = unitInstances[0];
            }

            // Re-cache renderers
            CacheRenderers();

            Debug.Log($"[Tower.DowngradeToVulnerable] COMPLETE - {towerData?.towerName} downgraded to vulnerable status");
        }

        private IEnumerator DamageFlash()
        {
            if (modelRenderers == null || modelRenderers.Length == 0) yield break;

            // Store original colors
            Color[] originalColors = new Color[modelRenderers.Length];
            for (int i = 0; i < modelRenderers.Length; i++)
            {
                if (modelRenderers[i] != null && modelRenderers[i].material != null)
                {
                    originalColors[i] = modelRenderers[i].material.color;
                    modelRenderers[i].material.color = new Color(1f, 0.3f, 0.3f, 1f);
                }
            }

            yield return new WaitForSeconds(0.1f);

            // Restore original colors
            for (int i = 0; i < modelRenderers.Length; i++)
            {
                if (modelRenderers[i] != null && modelRenderers[i].material != null)
                {
                    modelRenderers[i].material.color = originalColors[i];
                }
            }
        }

        protected virtual void DestroyTower()
        {
            if (IsDestroyed) return;

            IsDestroyed = true;
            Debug.Log($"[Tower] {towerData?.towerName ?? "Tower"} destroyed at {GridPosition}");

            GridManager.Instance?.RemoveTower(GridPosition);
            GameEvents.InvokeTowerDestroyed(this);
            EnemyMovement.RecalculateAllEnemyPaths();

            Destroy(gameObject);
        }

        protected virtual void OnDrawGizmosSelected()
        {
            if (towerData != null && towerData.IsAttackTower)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, towerData.range);
            }
        }
    }
}
