using UnityEngine;
using UnityEngine.EventSystems;
using TowerDefense.Placement;
using TowerDefense.UI;

namespace TowerDefense.Core
{
    public class TowerSelectionManager : MonoBehaviour
    {
        public static TowerSelectionManager Instance { get; private set; }

        private Tower selectedTower;
        private Camera mainCamera;

        public Tower SelectedTower => selectedTower;
        public bool HasSelection => selectedTower != null;

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
            mainCamera = Camera.main;
        }

        private void Update()
        {
            // Only process selection clicks when not placing a tower
            if (TowerPlacementSystem.Instance != null && TowerPlacementSystem.Instance.HasSelection)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                // Check if TowerInfoWorldUI handled a button click this frame
                if (TowerInfoWorldUI.WasButtonClickedThisFrame)
                {
                    Debug.Log("[TowerSelectionManager] UI button was clicked - skipping tower selection");
                    return;
                }

                // Always try to select a tower first - tower selection takes priority
                // This allows clicking on towers even when UI panel is visible nearby
                bool foundTower = TrySelectTower();

                // If we didn't find a tower but we're over UI, don't deselect
                // (let UI buttons handle their own clicks)
                if (!foundTower)
                {
                    bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                    if (isOverUI && selectedTower != null)
                    {
                        // Over UI (probably buttons) with tower selected - don't deselect
                        Debug.Log("[TowerSelectionManager] Click on UI - letting UI handle it");
                    }
                    // Note: deselection on empty space is handled in TrySelectTower
                }
            }

            // Right-click or Escape to deselect
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                DeselectTower();
            }
        }

        /// <summary>
        /// Attempts to select a tower at the mouse position.
        /// Returns true if a tower was found (regardless of whether it was already selected).
        /// </summary>
        private bool TrySelectTower()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogError("[TowerSelectionManager] No main camera found!");
                    return false;
                }
            }

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Debug.Log($"[TowerSelectionManager] TrySelectTower - mouse pos: {Input.mousePosition}");

            // First check if we hit a tower collider
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                Debug.Log($"[TowerSelectionManager] Physics hit: {hit.collider.name} on {hit.collider.gameObject.name}");
                Tower tower = hit.collider.GetComponentInParent<Tower>();
                if (tower != null)
                {
                    Debug.Log($"[TowerSelectionManager] Found tower via collider: {tower.Data?.towerName}");
                    SelectTower(tower);
                    return true;
                }
            }

            // Check grid position for tower
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPos = ray.GetPoint(distance);
                Vector2Int gridPos = GridManager.Instance.WorldToGrid(worldPos);
                Debug.Log($"[TowerSelectionManager] Grid check - worldPos: {worldPos}, gridPos: {gridPos}");

                if (GridManager.Instance.IsValidCell(gridPos))
                {
                    Tower tower = GridManager.Instance.GetTowerAt(gridPos);
                    if (tower != null)
                    {
                        Debug.Log($"[TowerSelectionManager] Found tower via grid: {tower.Data?.towerName}");
                        SelectTower(tower);
                        return true;
                    }
                    else
                    {
                        Debug.Log($"[TowerSelectionManager] No tower at grid position {gridPos}");
                    }
                }
            }

            // No tower found - deselect only if not over UI
            // (UI check is done in Update, but we also deselect here for empty space clicks)
            bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (!isOverUI)
            {
                Debug.Log("[TowerSelectionManager] No tower found, deselecting");
                DeselectTower();
            }
            return false;
        }

        public void SelectTower(Tower tower)
        {
            if (tower == null) return;

            // Deselect previous tower
            if (selectedTower != null && selectedTower != tower)
            {
                selectedTower.Deselect();
            }

            selectedTower = tower;
            selectedTower.Select();

            Debug.Log($"[TowerSelectionManager] Selected: {tower.Data?.towerName} at {tower.GridPosition}");
        }

        public void DeselectTower()
        {
            if (selectedTower != null)
            {
                selectedTower.Deselect();
                GameEvents.InvokeTowerDeselected();
                Debug.Log("[TowerSelectionManager] Tower deselected");
            }
            selectedTower = null;
        }

        public void SellSelectedTower()
        {
            if (selectedTower == null) return;

            Tower towerToSell = selectedTower;
            DeselectTower();
            towerToSell.Sell();

            // Recalculate paths after selling
            EnemyMovement.RecalculateAllEnemyPaths();
        }
    }
}
