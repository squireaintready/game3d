using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Data;
using TowerDefense.Placement;

namespace TowerDefense.Core
{
    public class UndoManager : MonoBehaviour
    {
        public static UndoManager Instance { get; private set; }

        // Stack of undoable actions
        private Stack<UndoAction> actionHistory = new Stack<UndoAction>();

        // Maximum history size to prevent memory issues
        private const int MAX_HISTORY = 50;

        public bool CanUndo => actionHistory.Count > 0;
        public int HistoryCount => actionHistory.Count;

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

        /// <summary>
        /// Record a new tower placement action
        /// </summary>
        public void RecordPlacement(Tower tower, int cost, bool wasGroundPlacement)
        {
            var action = new UndoAction
            {
                type = UndoActionType.NewTower,
                tower = tower,
                gridPosition = tower.GridPosition,
                cost = cost,
                wasGroundPlacement = wasGroundPlacement,
                towerData = tower.Data
            };

            PushAction(action);
            Debug.Log($"[UndoManager] Recorded new tower: {tower.Data?.towerName} at {tower.GridPosition}, cost={cost}");
        }

        /// <summary>
        /// Record a wall upgrade action (wall added to vulnerable tower)
        /// </summary>
        public void RecordWallUpgrade(Tower tower, int wallCost)
        {
            var action = new UndoAction
            {
                type = UndoActionType.WallUpgrade,
                tower = tower,
                gridPosition = tower.GridPosition,
                cost = wallCost,
                towerData = tower.Data
            };

            PushAction(action);
            Debug.Log($"[UndoManager] Recorded wall upgrade on: {tower.Data?.towerName} at {tower.GridPosition}");
        }

        /// <summary>
        /// Record an attack tower replacing a wall
        /// </summary>
        public void RecordWallReplacement(Tower newTower, TowerData wallData, int attackTowerCost)
        {
            var action = new UndoAction
            {
                type = UndoActionType.WallReplacement,
                tower = newTower,
                gridPosition = newTower.GridPosition,
                cost = attackTowerCost,
                replacedWallData = wallData,
                towerData = newTower.Data
            };

            PushAction(action);
            Debug.Log($"[UndoManager] Recorded wall replacement: {newTower.Data?.towerName} replaced wall at {newTower.GridPosition}");
        }

        private void PushAction(UndoAction action)
        {
            actionHistory.Push(action);

            // Limit history size
            if (actionHistory.Count > MAX_HISTORY)
            {
                // Convert to array, remove oldest, convert back
                var actions = actionHistory.ToArray();
                actionHistory.Clear();
                for (int i = 0; i < MAX_HISTORY; i++)
                {
                    actionHistory.Push(actions[MAX_HISTORY - 1 - i]);
                }
            }

            // Notify UI
            GameEvents.InvokeUndoStateChanged(CanUndo);
        }

        /// <summary>
        /// Undo the most recent action
        /// </summary>
        public void Undo()
        {
            if (!CanUndo)
            {
                Debug.Log("[UndoManager] Nothing to undo");
                return;
            }

            UndoAction action = actionHistory.Pop();

            switch (action.type)
            {
                case UndoActionType.NewTower:
                    UndoNewTower(action);
                    break;

                case UndoActionType.WallUpgrade:
                    UndoWallUpgrade(action);
                    break;

                case UndoActionType.WallReplacement:
                    UndoWallReplacement(action);
                    break;
            }

            // Recalculate enemy paths after undo
            EnemyMovement.RecalculateAllEnemyPaths();

            // Notify UI
            GameEvents.InvokeUndoStateChanged(CanUndo);
        }

        private void UndoNewTower(UndoAction action)
        {
            if (action.tower == null)
            {
                Debug.LogWarning("[UndoManager] Tower reference lost, cannot undo");
                return;
            }

            Vector2Int pos = action.gridPosition;

            // Remove from grid
            GridManager.Instance.RemoveTower(pos);

            // Destroy the game object
            Destroy(action.tower.gameObject);

            // Full refund
            EconomyManager.Instance.AddCurrency(action.cost);

            Debug.Log($"[UndoManager] Undid new tower at {pos}, refunded {action.cost}");
        }

        private void UndoWallUpgrade(UndoAction action)
        {
            if (action.tower == null)
            {
                Debug.LogWarning("[UndoManager] Tower reference lost, cannot undo wall upgrade");
                return;
            }

            // Downgrade tower back to vulnerable (remove wall protection)
            action.tower.DowngradeToVulnerable();

            // Refund wall cost
            EconomyManager.Instance.AddCurrency(action.cost);

            Debug.Log($"[UndoManager] Undid wall upgrade on {action.tower.Data?.towerName}, refunded {action.cost}");
        }

        private void UndoWallReplacement(UndoAction action)
        {
            if (action.tower == null)
            {
                Debug.LogWarning("[UndoManager] Tower reference lost, cannot undo wall replacement");
                return;
            }

            Vector2Int pos = action.gridPosition;

            // Remove the attack tower
            GridManager.Instance.RemoveTower(pos);
            Destroy(action.tower.gameObject);

            // Restore the wall
            if (action.replacedWallData != null)
            {
                Vector3 worldPos = GridManager.Instance.GridToWorld(pos);
                GameObject wallObj = new GameObject("Wall");
                wallObj.transform.position = worldPos;
                WallTower wall = wallObj.AddComponent<WallTower>();
                wall.Initialize(action.replacedWallData, pos, true);
                GridManager.Instance.PlaceTower(pos, wall);

                Debug.Log($"[UndoManager] Restored wall at {pos}");
            }

            // Refund only the attack tower cost (wall wasn't refunded originally)
            EconomyManager.Instance.AddCurrency(action.cost);

            Debug.Log($"[UndoManager] Undid wall replacement at {pos}, refunded {action.cost}");
        }

        /// <summary>
        /// Clear all undo history (e.g., when starting a new wave)
        /// </summary>
        public void ClearHistory()
        {
            actionHistory.Clear();
            GameEvents.InvokeUndoStateChanged(CanUndo);
            Debug.Log("[UndoManager] History cleared");
        }
    }

    public enum UndoActionType
    {
        NewTower,       // New tower placed on empty cell
        WallUpgrade,    // Wall added to vulnerable attack tower
        WallReplacement // Attack tower replaced existing wall
    }

    public struct UndoAction
    {
        public UndoActionType type;
        public Tower tower;
        public Vector2Int gridPosition;
        public int cost;
        public bool wasGroundPlacement;
        public TowerData towerData;
        public TowerData replacedWallData;
    }
}
