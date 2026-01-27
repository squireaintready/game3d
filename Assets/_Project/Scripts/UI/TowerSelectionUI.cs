using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Placement;

namespace TowerDefense.UI
{
    public class TowerSelectionUI : MonoBehaviour
    {
        [Header("Tower Buttons")]
        [SerializeField] private Button archerButton;
        [SerializeField] private Button wallButton;

        [Header("Tower Data")]
        [SerializeField] private TowerData archerTowerData;
        [SerializeField] private TowerData wallTowerData;

        [Header("Cost Labels")]
        [SerializeField] private Text archerCostText;
        [SerializeField] private Text wallCostText;

        [Header("Selection Indicator")]
        [SerializeField] private Image archerHighlight;
        [SerializeField] private Image wallHighlight;
        [SerializeField] private Color selectedColor = new Color(1f, 1f, 0f, 0.5f);
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0f);

        [Header("Undo")]
        [SerializeField] private Button undoButton;
        [SerializeField] private Text undoButtonText;

        private void Start()
        {
            if (archerButton != null)
            {
                archerButton.onClick.AddListener(OnArcherSelected);
            }

            if (wallButton != null)
            {
                wallButton.onClick.AddListener(OnWallSelected);
            }

            if (undoButton != null)
            {
                undoButton.onClick.AddListener(OnUndoClicked);
            }

            UpdateCostLabels();
            UpdateHighlights();
            UpdateUndoButton(UndoManager.Instance?.CanUndo ?? false);
        }

        private void OnEnable()
        {
            GameEvents.OnCurrencyChanged += OnCurrencyChanged;
            GameEvents.OnTowerPlaced += OnTowerPlaced;
            GameEvents.OnUndoStateChanged += UpdateUndoButton;
            GameEvents.OnGameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnCurrencyChanged -= OnCurrencyChanged;
            GameEvents.OnTowerPlaced -= OnTowerPlaced;
            GameEvents.OnUndoStateChanged -= UpdateUndoButton;
            GameEvents.OnGameStateChanged -= OnGameStateChanged;
        }

        private void OnGameStateChanged(GameState state)
        {
            UpdateUndoButton(UndoManager.Instance?.CanUndo ?? false);
        }

        private void Update()
        {
            // Keyboard shortcuts
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                OnArcherSelected();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                OnWallSelected();
            }
            // Ctrl+Z or Cmd+Z for undo
            else if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
                      Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)) &&
                     Input.GetKeyDown(KeyCode.Z))
            {
                OnUndoClicked();
            }
        }

        private void OnArcherSelected()
        {
            TowerPlacementSystem.Instance?.SelectArcherTower();
            UpdateHighlights();
        }

        private void OnWallSelected()
        {
            TowerPlacementSystem.Instance?.SelectWallTower();
            UpdateHighlights();
        }

        private void OnCurrencyChanged(int amount)
        {
            UpdateButtonInteractable();
        }

        private void OnTowerPlaced(Tower tower)
        {
            UpdateButtonInteractable();
        }

        private void UpdateCostLabels()
        {
            if (archerCostText != null && archerTowerData != null)
            {
                archerCostText.text = $"{archerTowerData.cost}g";
            }

            if (wallCostText != null && wallTowerData != null)
            {
                wallCostText.text = $"{wallTowerData.cost}g";
            }
        }

        private void UpdateButtonInteractable()
        {
            int currency = EconomyManager.Instance?.CurrentCurrency ?? 0;

            if (archerButton != null && archerTowerData != null)
            {
                archerButton.interactable = currency >= archerTowerData.cost;
            }

            if (wallButton != null && wallTowerData != null)
            {
                wallButton.interactable = currency >= wallTowerData.cost;
            }
        }

        private void UpdateHighlights()
        {
            var selected = TowerPlacementSystem.Instance?.SelectedTowerData;

            if (archerHighlight != null)
            {
                archerHighlight.color = (selected == archerTowerData) ? selectedColor : normalColor;
            }

            if (wallHighlight != null)
            {
                wallHighlight.color = (selected == wallTowerData) ? selectedColor : normalColor;
            }
        }

        private void OnUndoClicked()
        {
            UndoManager.Instance?.Undo();
        }

        private void UpdateUndoButton(bool canUndo)
        {
            // Undo only allowed during build phase
            var state = GameManager.Instance?.CurrentState;
            bool isBuildPhase = state == GameState.Building;
            bool canActuallyUndo = canUndo && isBuildPhase;

            if (undoButton != null)
            {
                undoButton.interactable = canActuallyUndo;
            }

            if (undoButtonText != null)
            {
                int count = UndoManager.Instance?.HistoryCount ?? 0;
                if (!isBuildPhase)
                {
                    undoButtonText.text = "Undo";
                }
                else if (canUndo)
                {
                    undoButtonText.text = $"Undo ({count})";
                }
                else
                {
                    undoButtonText.text = "Undo";
                }
            }
        }
    }
}
