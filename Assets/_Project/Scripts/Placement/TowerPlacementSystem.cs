using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense;
using System.Collections.Generic;

namespace TowerDefense.Placement
{
    public class TowerPlacementSystem : MonoBehaviour
    {
        public static TowerPlacementSystem Instance { get; private set; }

        [Header("Tower Prefabs")]
        [SerializeField] private GameObject archerTowerPrefab;
        [SerializeField] private GameObject wallTowerPrefab;
        [SerializeField] private GameObject mageTowerPrefab;
        [SerializeField] private GameObject cannonTowerPrefab;

        [Header("Tower Data")]
        [SerializeField] private TowerData archerTowerData;
        [SerializeField] private TowerData wallTowerData;
        [SerializeField] private TowerData mageTowerData;
        [SerializeField] private TowerData cannonTowerData;

        [Header("Preview")]
        [SerializeField] private GameObject previewPrefab;
        [SerializeField] private Color validColor = new Color(0f, 1f, 0f, 0.5f);
        [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.5f);
        [SerializeField] private Color cantAffordColor = new Color(1f, 0.5f, 0.5f, 0.6f); // Light reddish tint

        private TowerData selectedTowerData;
        private GameObject selectedPrefab;
        private GameObject previewObject;
        private SpriteRenderer previewRenderer;
        private UnityEngine.Camera mainCamera;
        private PlacementValidator validator;
        private float lastPlacementTime;
        private const float PLACEMENT_COOLDOWN = 0.2f; // Prevent double-clicks

        // 3D preview system
        private GameObject preview3DObject;
        private TowerData currentPreviewData;
        private Material previewMaterial;
        private List<Renderer> previewRenderers = new List<Renderer>();

        public TowerData SelectedTowerData => selectedTowerData;
        public bool HasSelection => selectedTowerData != null;

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
            mainCamera = UnityEngine.Camera.main;
            validator = FindFirstObjectByType<PlacementValidator>();

            // Auto-create validator if not found
            if (validator == null)
            {
                Debug.Log("[TowerPlacementSystem] Creating PlacementValidator");
                var validatorObj = new GameObject("PlacementValidator");
                validator = validatorObj.AddComponent<PlacementValidator>();
            }
            else
            {
                Debug.Log("[TowerPlacementSystem] Found existing PlacementValidator");
            }

            // Auto-create UndoManager if not found
            if (UndoManager.Instance == null)
            {
                Debug.Log("[TowerPlacementSystem] Creating UndoManager");
                var undoObj = new GameObject("UndoManager");
                undoObj.AddComponent<UndoManager>();
            }
            else
            {
                Debug.Log("[TowerPlacementSystem] Found existing UndoManager");
            }

            AutoWireReferences();
            CreatePreviewObject();
        }

        private void AutoWireReferences()
        {
            // Auto-load tower data
            if (archerTowerData == null)
            {
                archerTowerData = Resources.Load<TowerData>("ArcherTowerData");
                if (archerTowerData == null)
                {
                    var allData = Resources.FindObjectsOfTypeAll<TowerData>();
                    foreach (var data in allData)
                    {
                        if (data.name.Contains("Archer"))
                        {
                            archerTowerData = data;
                            break;
                        }
                    }
                }
            }

            if (wallTowerData == null)
            {
                wallTowerData = Resources.Load<TowerData>("WallTowerData");
                if (wallTowerData == null)
                {
                    var allData = Resources.FindObjectsOfTypeAll<TowerData>();
                    foreach (var data in allData)
                    {
                        if (data.name.Contains("Wall"))
                        {
                            wallTowerData = data;
                            break;
                        }
                    }
                }
            }

            // Auto-load tower prefabs
            if (archerTowerPrefab == null)
            {
                archerTowerPrefab = Resources.Load<GameObject>("ArcherTower");
                if (archerTowerPrefab == null)
                {
                    var allPrefabs = Resources.FindObjectsOfTypeAll<GameObject>();
                    foreach (var prefab in allPrefabs)
                    {
                        if (prefab.name == "ArcherTower" && prefab.GetComponent<ArcherTower>() != null)
                        {
                            archerTowerPrefab = prefab;
                            break;
                        }
                    }
                }
            }

            if (wallTowerPrefab == null)
            {
                wallTowerPrefab = Resources.Load<GameObject>("WallTower");
                if (wallTowerPrefab == null)
                {
                    var allPrefabs = Resources.FindObjectsOfTypeAll<GameObject>();
                    foreach (var prefab in allPrefabs)
                    {
                        if (prefab.name == "WallTower" && prefab.GetComponent<WallTower>() != null)
                        {
                            wallTowerPrefab = prefab;
                            break;
                        }
                    }
                }
            }

            // Auto-load mage tower data
            if (mageTowerData == null)
            {
                mageTowerData = Resources.Load<TowerData>("MageTowerData");
                if (mageTowerData == null)
                {
                    var allData = Resources.FindObjectsOfTypeAll<TowerData>();
                    foreach (var data in allData)
                    {
                        if (data.name.Contains("Mage"))
                        {
                            mageTowerData = data;
                            break;
                        }
                    }
                }
            }

            if (mageTowerPrefab == null)
            {
                mageTowerPrefab = Resources.Load<GameObject>("MageTower");
                if (mageTowerPrefab == null)
                {
                    var allPrefabs = Resources.FindObjectsOfTypeAll<GameObject>();
                    foreach (var prefab in allPrefabs)
                    {
                        if (prefab.name == "MageTower" && prefab.GetComponent<MageTower>() != null)
                        {
                            mageTowerPrefab = prefab;
                            break;
                        }
                    }
                }
            }

            // Auto-load cannon tower data
            if (cannonTowerData == null)
            {
                cannonTowerData = Resources.Load<TowerData>("CannonTowerData");
                if (cannonTowerData == null)
                {
                    var allData = Resources.FindObjectsOfTypeAll<TowerData>();
                    foreach (var data in allData)
                    {
                        if (data.name.Contains("Cannon"))
                        {
                            cannonTowerData = data;
                            break;
                        }
                    }
                }
            }

            if (cannonTowerPrefab == null)
            {
                cannonTowerPrefab = Resources.Load<GameObject>("CannonTower");
                if (cannonTowerPrefab == null)
                {
                    var allPrefabs = Resources.FindObjectsOfTypeAll<GameObject>();
                    foreach (var prefab in allPrefabs)
                    {
                        if (prefab.name == "CannonTower" && prefab.GetComponent<CannonTower>() != null)
                        {
                            cannonTowerPrefab = prefab;
                            break;
                        }
                    }
                }
            }

            Debug.Log($"[TowerPlacementSystem] AutoWire: archerData={archerTowerData != null}, wallData={wallTowerData != null}, mageData={mageTowerData != null}, cannonData={cannonTowerData != null}");
        }

        private void CreatePreviewObject()
        {
            // Create transparent preview material for 3D models
            CreatePreviewMaterial();

            // Legacy 2D preview as fallback
            previewObject = new GameObject("TowerPreview2D");
            previewRenderer = previewObject.AddComponent<SpriteRenderer>();
            previewRenderer.color = validColor;
            previewRenderer.sortingOrder = 200;

            float cellSize = GridManager.Instance?.CellSize ?? 1f;
            float previewScale = cellSize * 0.85f;
            previewObject.transform.localScale = Vector3.one * previewScale;
            previewObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            previewObject.SetActive(false);
        }

        private void CreatePreviewMaterial()
        {
            // Try to find a transparent/ghost shader for the preview
            string[] shaderNames = new string[]
            {
                "Universal Render Pipeline/Lit",
                "Standard",
                "Sprites/Default"
            };

            Shader shader = null;
            foreach (string shaderName in shaderNames)
            {
                shader = Shader.Find(shaderName);
                if (shader != null) break;
            }

            if (shader != null)
            {
                previewMaterial = new Material(shader);
                // Make it transparent
                previewMaterial.SetFloat("_Surface", 1); // Transparent
                previewMaterial.SetFloat("_Blend", 0); // Alpha
                previewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                previewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                previewMaterial.SetInt("_ZWrite", 0);
                previewMaterial.DisableKeyword("_ALPHATEST_ON");
                previewMaterial.EnableKeyword("_ALPHABLEND_ON");
                previewMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                previewMaterial.renderQueue = 3000;
                previewMaterial.color = validColor;
            }
        }

        private void Create3DPreview(TowerData data)
        {
            // Destroy old preview if different tower type
            if (preview3DObject != null && currentPreviewData != data)
            {
                Destroy(preview3DObject);
                preview3DObject = null;
                previewRenderers.Clear();
            }

            if (preview3DObject != null) return; // Already have correct preview

            currentPreviewData = data;
            preview3DObject = new GameObject("TowerPreview3D");

            float cellSize = GridManager.Instance?.CellSize ?? 1f;
            bool isAttackTower = IsUpgradeTower(data.towerType);
            bool isWall = data.towerType == TowerType.Wall;

            // Create wall/base model
            if (data.modelPrefab != null)
            {
                GameObject wallModel = Instantiate(data.modelPrefab, preview3DObject.transform);
                wallModel.transform.localPosition = Vector3.zero;

                // Use appropriate scale vector based on tower type
                Vector3 scaleVec;
                if (isWall)
                {
                    // Standalone wall uses modelScaleVector
                    scaleVec = data.modelScaleVector != Vector3.zero ? data.modelScaleVector : Vector3.one * data.modelScale;
                }
                else
                {
                    // Attack towers use wallScaleVector for their wall base
                    scaleVec = data.wallScaleVector != Vector3.zero ? data.wallScaleVector : Vector3.one * data.wallScale;
                }
                wallModel.transform.localScale = scaleVec * cellSize;
                CollectRenderers(wallModel);
            }

            // Create unit models for attack towers
            if (isAttackTower && data.unitPrefab != null)
            {
                int unitCount = Mathf.Max(1, data.unitCount);
                float spacing = 0.3f * cellSize;

                for (int i = 0; i < unitCount; i++)
                {
                    GameObject unitModel = Instantiate(data.unitPrefab, preview3DObject.transform);

                    Vector3 unitPos = data.unitOffset * cellSize;
                    if (unitCount > 1)
                    {
                        float offset = (i - (unitCount - 1) / 2f) * spacing;
                        unitPos.x += offset;
                    }
                    unitModel.transform.localPosition = unitPos;
                    unitModel.transform.localScale = Vector3.one * data.unitScale * cellSize;
                    CollectRenderers(unitModel);
                }
            }

            // Apply preview material to all renderers
            ApplyPreviewMaterial(validColor);

            // Disable any scripts/components that might interfere
            DisablePreviewComponents(preview3DObject);
        }

        private void CollectRenderers(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            previewRenderers.AddRange(renderers);
        }

        private void ApplyPreviewMaterial(Color color)
        {
            foreach (var renderer in previewRenderers)
            {
                if (renderer == null) continue;

                // Create instance materials for each renderer
                Material[] mats = renderer.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material mat = new Material(mats[i]);
                    // Make semi-transparent with tint
                    mat.color = new Color(color.r, color.g, color.b, 0.5f);

                    // Try to set transparency properties
                    if (mat.HasProperty("_Surface"))
                        mat.SetFloat("_Surface", 1);
                    if (mat.HasProperty("_Blend"))
                        mat.SetFloat("_Blend", 0);

                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.renderQueue = 3000;

                    mats[i] = mat;
                }
                renderer.materials = mats;
            }
        }

        private void UpdatePreviewColor(Color color)
        {
            foreach (var renderer in previewRenderers)
            {
                if (renderer == null) continue;

                foreach (var mat in renderer.materials)
                {
                    mat.color = new Color(color.r, color.g, color.b, 0.5f);
                }
            }
        }

        private void DisablePreviewComponents(GameObject obj)
        {
            // Disable all MonoBehaviours except Transform
            var behaviours = obj.GetComponentsInChildren<MonoBehaviour>();
            foreach (var b in behaviours)
            {
                b.enabled = false;
            }

            // Disable colliders
            var colliders = obj.GetComponentsInChildren<Collider>();
            foreach (var c in colliders)
            {
                c.enabled = false;
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null)
            {
                HidePreview();
                return;
            }

            // Allow building during both Build phase and Wave phase
            var state = GameManager.Instance.CurrentState;
            if (state != GameState.Building && state != GameState.WaveActive)
            {
                HidePreview();
                return;
            }

            if (selectedTowerData == null)
            {
                HidePreview();
                return;
            }

            UpdatePreview();
            HandleInput();
        }

        private void UpdatePreview()
        {
            Vector3 mouseWorld = GetMouseWorldPosition();
            Vector2Int gridPos = GridManager.Instance.WorldToGrid(mouseWorld);

            if (!GridManager.Instance.IsValidCell(gridPos))
            {
                HidePreview();
                return;
            }

            Vector3 snappedPos = GridManager.Instance.GridToWorld(gridPos);

            // Determine preview color based on placement validity and affordability
            bool canPlace = CanPlaceAt(gridPos);
            bool canAfford = EconomyManager.Instance != null && EconomyManager.Instance.CanAfford(selectedTowerData.cost);

            Color previewColor;
            if (!canAfford)
            {
                previewColor = cantAffordColor;
            }
            else if (canPlace)
            {
                previewColor = validColor;
            }
            else
            {
                previewColor = invalidColor;
            }

            // Use 3D preview if tower has model data
            if (selectedTowerData.modelPrefab != null || selectedTowerData.unitPrefab != null)
            {
                // Create/update 3D preview
                Create3DPreview(selectedTowerData);

                if (preview3DObject != null)
                {
                    preview3DObject.transform.position = snappedPos;
                    preview3DObject.SetActive(true);
                    UpdatePreviewColor(previewColor);
                }

                // Hide 2D fallback
                if (previewObject != null)
                    previewObject.SetActive(false);
            }
            else
            {
                // Fallback to 2D preview
                bool isUpgrade = IsUpgradeTower(selectedTowerData.towerType);
                float yOffset = isUpgrade ? 0.8f : 0.3f;
                previewObject.transform.position = snappedPos + Vector3.up * yOffset;
                previewObject.SetActive(true);

                // Billboard to face camera
                if (mainCamera != null)
                {
                    Vector3 lookDir = mainCamera.transform.forward;
                    previewObject.transform.rotation = Quaternion.LookRotation(lookDir, mainCamera.transform.up);
                }

                previewRenderer.color = previewColor;
                if (previewRenderer.material != null)
                {
                    previewRenderer.material.color = previewColor;
                }

                // Hide 3D preview
                if (preview3DObject != null)
                    preview3DObject.SetActive(false);
            }
        }

        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                TryPlaceTower();
            }

            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                ClearSelection();
            }
        }

        private void TryPlaceTower()
        {
            // Prevent rapid double-clicks from placing multiple towers
            if (Time.time - lastPlacementTime < PLACEMENT_COOLDOWN) return;

            Vector3 mouseWorld = GetMouseWorldPosition();
            Vector2Int gridPos = GridManager.Instance.WorldToGrid(mouseWorld);

            if (!CanPlaceAt(gridPos)) return;

            if (!EconomyManager.Instance.CanAfford(selectedTowerData.cost))
            {
                Debug.Log("Not enough BTC!");
                return;
            }

            lastPlacementTime = Time.time;
            PlaceTower(gridPos);
        }

        private bool CanPlaceAt(Vector2Int gridPos)
        {
            if (selectedTowerData == null) return false;

            Tower existingTower = GridManager.Instance.GetTowerAt(gridPos);
            bool isAttackTower = IsUpgradeTower(selectedTowerData.towerType);
            bool isWall = selectedTowerData.towerType == TowerType.Wall;

            // Both walls and attack towers can only be placed on empty cells
            // Attack towers come with wall included, so can't place on existing wall
            if (existingTower != null)
            {
                Debug.Log($"[TowerPlacement] Cannot place at {gridPos} - cell occupied by {existingTower.TowerType}");
                return false;
            }

            // Check path blocking
            if (validator != null)
            {
                bool wouldBlock = validator.WouldBlockPath(gridPos);
                if (wouldBlock)
                {
                    Debug.Log($"[TowerPlacement] Cannot place at {gridPos} - would block path");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns true if the tower type is an upgrade (requires existing wall)
        /// </summary>
        private bool IsUpgradeTower(TowerType type)
        {
            return type == TowerType.Archer || type == TowerType.Mage || type == TowerType.Cannon;
        }

        private void PlaceTower(Vector2Int gridPos)
        {
            Vector3 worldPos = GridManager.Instance.GridToWorld(gridPos);
            bool isAttackTower = IsUpgradeTower(selectedTowerData.towerType);
            bool isWall = selectedTowerData.towerType == TowerType.Wall;

            // All towers are always protected (attack towers come with wall included)
            bool isProtected = true;

            if (isAttackTower)
            {
                Debug.Log($"[TowerPlacement] Placing {selectedTowerData.towerName} at {gridPos} (with wall included)");
            }
            else if (isWall)
            {
                Debug.Log($"[TowerPlacement] Placing wall at {gridPos}");
            }

            GameObject towerObj;
            Tower tower;

            // Check if prefab is valid (not destroyed)
            bool prefabValid = !ReferenceEquals(selectedPrefab, null) && selectedPrefab != null;

            // Use prefab if available and valid, otherwise create runtime tower
            if (prefabValid)
            {
                towerObj = Instantiate(selectedPrefab, worldPos, Quaternion.identity);
                tower = towerObj.GetComponent<Tower>();
            }
            else
            {
                // Create runtime tower with proper component
                Debug.Log($"[TowerPlacement] Prefab invalid or null, creating runtime tower");
                towerObj = CreateRuntimeTower(worldPos, selectedTowerData);
                tower = towerObj.GetComponent<Tower>();
            }

            if (tower != null)
            {
                // Initialize with protection status
                tower.Initialize(selectedTowerData, gridPos, isProtected);

                // Verify placement succeeded
                if (GridManager.Instance.PlaceTower(gridPos, tower))
                {
                    EconomyManager.Instance.SpendCurrency(selectedTowerData.cost);

                    // Record for undo - all towers get full refund
                    Debug.Log($"[TowerPlacement] Recording undo: NewTower");
                    UndoManager.Instance?.RecordPlacement(tower, selectedTowerData.cost, false);

                    // Verify undo was recorded
                    Debug.Log($"[TowerPlacement] UndoManager exists: {UndoManager.Instance != null}, CanUndo: {UndoManager.Instance?.CanUndo}, History: {UndoManager.Instance?.HistoryCount}");

                    string protectionStatus = isProtected ? "protected" : "vulnerable";
                    Debug.Log($"[TowerPlacement] Placed {selectedTowerData.towerName} at {gridPos} ({protectionStatus})");

                    // Force all enemies to recalculate their paths
                    EnemyMovement.RecalculateAllEnemyPaths();

                    // Deselect tower after placement
                    ClearSelection();

                    // Fire event
                    GameEvents.InvokeTowerPlaced(tower);
                }
                else
                {
                    // Placement failed - cell was occupied
                    Debug.LogWarning($"[TowerPlacement] Failed to place at {gridPos} - cell occupied");
                    Destroy(towerObj);
                }
            }
            else
            {
                Debug.LogError($"[TowerPlacement] Created tower has no Tower component!");
                Destroy(towerObj);
            }
        }

        private GameObject CreateRuntimeTower(Vector3 position, TowerData data)
        {
            GameObject towerObj = new GameObject(data.towerName);
            towerObj.transform.position = position;

            // Add the correct tower component based on type
            switch (data.towerType)
            {
                case TowerType.Archer:
                    towerObj.AddComponent<ArcherTower>();
                    Debug.Log("[TowerPlacement] Created runtime ArcherTower");
                    break;
                case TowerType.Wall:
                    towerObj.AddComponent<WallTower>();
                    Debug.Log("[TowerPlacement] Created runtime WallTower");
                    break;
                case TowerType.Mage:
                    towerObj.AddComponent<MageTower>();
                    Debug.Log("[TowerPlacement] Created runtime MageTower");
                    break;
                case TowerType.Cannon:
                    towerObj.AddComponent<CannonTower>();
                    Debug.Log("[TowerPlacement] Created runtime CannonTower");
                    break;
                default:
                    towerObj.AddComponent<Tower>();
                    Debug.Log("[TowerPlacement] Created runtime generic Tower");
                    break;
            }

            return towerObj;
        }

        public void SelectTower(TowerData data, GameObject prefab)
        {
            selectedTowerData = data;
            selectedPrefab = prefab;

            // For 3D mode, use a simple colored preview quad
            // The actual 3D model preview would require instantiating the model
            if (data.icon != null)
            {
                previewRenderer.sprite = data.icon;
            }
        }

        public void SelectArcherTower()
        {
            SelectTower(archerTowerData, archerTowerPrefab);
        }

        public void SelectWallTower()
        {
            SelectTower(wallTowerData, wallTowerPrefab);
        }

        public void SelectMageTower()
        {
            SelectTower(mageTowerData, mageTowerPrefab);
        }

        public void SelectCannonTower()
        {
            SelectTower(cannonTowerData, cannonTowerPrefab);
        }

        // Expose tower data for UI cost display
        public TowerData ArcherTowerData => archerTowerData;
        public TowerData WallTowerData => wallTowerData;
        public TowerData MageTowerData => mageTowerData;
        public TowerData CannonTowerData => cannonTowerData;

        public void ClearSelection()
        {
            selectedTowerData = null;
            selectedPrefab = null;
            HidePreview();
        }

        private void HidePreview()
        {
            if (previewObject != null)
            {
                previewObject.SetActive(false);
            }
            if (preview3DObject != null)
            {
                preview3DObject.SetActive(false);
            }
        }

        private Vector3 GetMouseWorldPosition()
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = mainCamera.transform.position.y;
            Ray ray = mainCamera.ScreenPointToRay(mousePos);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }

            return Vector3.zero;
        }
    }
}
