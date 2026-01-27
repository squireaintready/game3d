using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Core;
using TowerDefense.Placement;
using System.Collections;

namespace TowerDefense.UI
{
    /// <summary>
    /// Auto-creates and manages UI elements for the game HUD.
    /// Attach to Canvas - creates all necessary UI at runtime.
    /// </summary>
    public class GameUIAutoSetup : MonoBehaviour
    {
        // HUD elements
        private Text goldText;
        private Text livesText;
        private Text waveText;
        private Text timerText;
        private GameObject timerPanel;

        // Wave announcement
        private Text waveAnnouncementText;
        private CanvasGroup waveAnnouncementGroup;

        // Tower buttons
        private Button archerButton;
        private Button wallButton;
        private Button mageButton;
        private Button cannonButton;
        private Image archerBtnImage;
        private Image wallBtnImage;
        private Image mageBtnImage;
        private Image cannonBtnImage;

        // Start wave button
        private Button startWaveButton;
        private GameObject startWavePanel;
        private Text startWaveButtonText;
        private GameObject startButtonBtcIcon;
        private Image startWaveButtonImage;

        // Speed button
        private Button speedButton;
        private Text speedButtonText;
        private bool isDoubleSpeed = false;

        // Undo button
        private Button undoButton;
        private Text undoButtonText;

        // Tower selection UI is handled by TowerInfoWorldUI (world-space)

        // Bottom tower bar
        private GameObject rightSidePanel;

        // Colors
        private Color panelColor = new Color(0.1f, 0.1f, 0.15f, 0.9f);
        private Color accentColor = new Color(0.3f, 0.7f, 0.4f, 1f);
        private Color goldColor = new Color(1f, 0.85f, 0.3f, 1f);
        private Color dangerColor = new Color(0.9f, 0.3f, 0.3f, 1f);
        private Color selectedColor = new Color(0.4f, 0.8f, 0.5f, 1f);
        private Color normalBtnColor = new Color(0.25f, 0.25f, 0.3f, 1f);
        private Color disabledBtnColor = new Color(0.15f, 0.15f, 0.18f, 0.7f); // Darker, more transparent when can't afford

        // Bitcoin icon sprite (created at runtime)
        private Sprite btcIconSprite;

        private void Start()
        {
            Debug.Log("[GameUIAutoSetup] Starting UI creation");
            EnsureManagersExist();
            CreateBitcoinIcon();
            SetupCanvas();
            CreateTopLeftTimer();
            CreateTopRightPanel();
            CreateBottomTowerBar();
            CreateWaveAnnouncement();
            SubscribeToEvents();
            UpdateAllDisplays();
        }

        private void EnsureManagersExist()
        {
            // Auto-create TowerSelectionManager if not found
            if (TowerSelectionManager.Instance == null)
            {
                var obj = new GameObject("TowerSelectionManager");
                obj.AddComponent<TowerSelectionManager>();
                Debug.Log("[GameUIAutoSetup] Created TowerSelectionManager");
            }

            // Auto-create TowerInfoWorldUI if not found
            if (TowerInfoWorldUI.Instance == null)
            {
                var obj = new GameObject("TowerInfoWorldUI");
                obj.AddComponent<TowerInfoWorldUI>();
                Debug.Log("[GameUIAutoSetup] Created TowerInfoWorldUI");
            }
        }

        private void CreateBitcoinIcon()
        {
            int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            Color clear = new Color(0, 0, 0, 0);
            Color gold = new Color(1f, 0.85f, 0.3f, 1f);

            // Clear all pixels
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    texture.SetPixel(x, y, clear);

            // Simple, clean B shape - no fancy curves, just clear rectangles
            int stroke = 8;
            int left = 12;
            int innerLeft = left + stroke;
            int bumpRight = 48;
            int bottom = 8;
            int top = 56;
            int mid = 32;
            int gap = 3; // gap at middle to show two separate bumps

            // LEFT VERTICAL BAR
            for (int x = left; x < left + stroke; x++)
                for (int y = bottom; y <= top; y++)
                    texture.SetPixel(x, y, gold);

            // TOP BUMP (upper half of B)
            // Top horizontal
            for (int x = left; x <= bumpRight - 6; x++)
                for (int y = top - stroke; y <= top; y++)
                    texture.SetPixel(x, y, gold);

            // Upper bump right side
            for (int x = bumpRight - stroke; x <= bumpRight; x++)
                for (int y = mid + gap; y <= top - stroke; y++)
                    texture.SetPixel(x, y, gold);

            // Upper bump bottom (connects to middle)
            for (int x = innerLeft; x <= bumpRight - 6; x++)
                for (int y = mid + gap; y <= mid + gap + stroke; y++)
                    texture.SetPixel(x, y, gold);

            // BOTTOM BUMP (lower half of B)
            // Bottom horizontal
            for (int x = left; x <= bumpRight - 6; x++)
                for (int y = bottom; y < bottom + stroke; y++)
                    texture.SetPixel(x, y, gold);

            // Lower bump right side
            for (int x = bumpRight - stroke; x <= bumpRight; x++)
                for (int y = bottom + stroke; y <= mid - gap; y++)
                    texture.SetPixel(x, y, gold);

            // Lower bump top (connects to middle)
            for (int x = innerLeft; x <= bumpRight - 6; x++)
                for (int y = mid - gap - stroke; y <= mid - gap; y++)
                    texture.SetPixel(x, y, gold);

            // TWO VERTICAL LINES (Bitcoin signature)
            int line1 = 24;
            int line2 = 38;
            int lineW = 5;

            // Lines above B
            for (int x = line1; x < line1 + lineW; x++)
                for (int y = top + 1; y <= 62; y++)
                    texture.SetPixel(x, y, gold);
            for (int x = line2; x < line2 + lineW; x++)
                for (int y = top + 1; y <= 62; y++)
                    texture.SetPixel(x, y, gold);

            // Lines below B
            for (int x = line1; x < line1 + lineW; x++)
                for (int y = 2; y < bottom; y++)
                    texture.SetPixel(x, y, gold);
            for (int x = line2; x < line2 + lineW; x++)
                for (int y = 2; y < bottom; y++)
                    texture.SetPixel(x, y, gold);

            texture.Apply();

            btcIconSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            Debug.Log("[GameUIAutoSetup] Bitcoin icon created");
        }

        private void SetupCanvas()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                // Ensure pixel perfect rendering
                canvas.pixelPerfect = true;

                var scaler = GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    // Lower reference resolution = larger UI elements = sharper text
                    scaler.referenceResolution = new Vector2(800, 600);
                    scaler.matchWidthOrHeight = 0.5f;
                }
            }
        }

        private void CreateTopLeftTimer()
        {
            // Timer panel anchored to top-left
            timerPanel = new GameObject("TimerPanel");
            timerPanel.transform.SetParent(transform, false);

            RectTransform rect = timerPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(10, -10);
            rect.sizeDelta = new Vector2(80, 50);

            Image bg = timerPanel.AddComponent<Image>();
            bg.color = panelColor;

            timerText = CreateText(timerPanel.transform, "TimerText", "", 28, TextAnchor.MiddleCenter);
            timerText.fontStyle = FontStyle.Bold;
            timerText.color = goldColor;
            RectTransform timerRect = timerText.GetComponent<RectTransform>();
            timerRect.anchorMin = Vector2.zero;
            timerRect.anchorMax = Vector2.one;
            timerRect.sizeDelta = Vector2.zero;
        }

        private void CreateTopRightPanel()
        {
            // Container for stats + actions, anchored to top-right
            GameObject topRightContainer = new GameObject("TopRightPanel");
            topRightContainer.transform.SetParent(transform, false);

            RectTransform containerRect = topRightContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(1, 1);
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.pivot = new Vector2(1, 1);
            containerRect.anchoredPosition = new Vector2(-10, -10);

            VerticalLayoutGroup vLayout = topRightContainer.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 8; // Gap between stats and actions
            vLayout.childAlignment = TextAnchor.UpperRight;
            vLayout.childControlWidth = false;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = false;
            vLayout.childForceExpandHeight = false;

            // Stats panel (on top)
            CreateStatsPanel(topRightContainer.transform);

            // Actions panel (below stats)
            CreateActionsPanel(topRightContainer.transform);

            // Size the container based on content
            containerRect.sizeDelta = new Vector2(130, 220);
        }

        private void CreateStatsPanel(Transform parent)
        {
            GameObject statsPanel = new GameObject("StatsPanel");
            statsPanel.transform.SetParent(parent, false);

            RectTransform rect = statsPanel.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(130, 110);

            Image bg = statsPanel.AddComponent<Image>();
            bg.color = panelColor;

            VerticalLayoutGroup vLayout = statsPanel.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(8, 8, 6, 6);
            vLayout.spacing = 4;
            vLayout.childAlignment = TextAnchor.MiddleCenter;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true;

            // BTC
            GameObject goldRow = CreateStatRow(statsPanel.transform, "BTC", goldColor);
            goldText = goldRow.transform.Find("Value").GetComponent<Text>();

            // Lives
            GameObject livesRow = CreateStatRow(statsPanel.transform, "LIVES", dangerColor);
            livesText = livesRow.transform.Find("Value").GetComponent<Text>();

            // Wave
            GameObject waveRow = CreateStatRow(statsPanel.transform, "WAVE", accentColor);
            waveText = waveRow.transform.Find("Value").GetComponent<Text>();
        }

        private GameObject CreateStatRow(Transform parent, string label, Color labelColor)
        {
            GameObject row = new GameObject(label + "Row");
            row.transform.SetParent(parent, false);

            RectTransform rect = row.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 30);

            HorizontalLayoutGroup hLayout = row.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 8;
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = false;

            // Label
            Text labelText = CreateText(row.transform, "Label", label, 16, TextAnchor.MiddleRight);
            labelText.color = labelColor;
            labelText.fontStyle = FontStyle.Bold;
            RectTransform labelRect = labelText.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(55, 28);

            // Value
            Text valueText = CreateText(row.transform, "Value", "0", 22, TextAnchor.MiddleLeft);
            valueText.fontStyle = FontStyle.Bold;
            RectTransform valueRect = valueText.GetComponent<RectTransform>();
            valueRect.sizeDelta = new Vector2(55, 28);

            return row;
        }

        private void CreateActionsPanel(Transform parent)
        {
            GameObject actionsPanel = new GameObject("ActionsPanel");
            actionsPanel.transform.SetParent(parent, false);

            RectTransform rect = actionsPanel.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(130, 100);

            Image bg = actionsPanel.AddComponent<Image>();
            bg.color = panelColor;

            // Use Grid layout for 2x2 button arrangement
            GridLayoutGroup gridLayout = actionsPanel.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(58, 44);
            gridLayout.spacing = new Vector2(4, 4);
            gridLayout.padding = new RectOffset(4, 4, 4, 4);
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;

            // Start button (top-left)
            CreateStartButton(actionsPanel.transform);

            // Speed button (top-right)
            CreateSpeedButton(actionsPanel.transform);

            // Undo button (bottom-left)
            CreateUndoButton(actionsPanel.transform);

            // Reset button (bottom-right)
            CreateResetButton(actionsPanel.transform);
        }

        private void CreateBottomTowerBar()
        {
            // Bottom bar for tower buttons only
            rightSidePanel = new GameObject("BottomTowerBar");
            rightSidePanel.transform.SetParent(transform, false);

            RectTransform panelRect = rightSidePanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0);
            panelRect.anchorMax = new Vector2(0.5f, 0);
            panelRect.pivot = new Vector2(0.5f, 0);
            panelRect.anchoredPosition = new Vector2(0, 5);
            panelRect.sizeDelta = new Vector2(340, 70);

            Image bg = rightSidePanel.AddComponent<Image>();
            bg.color = panelColor;

            HorizontalLayoutGroup hLayout = rightSidePanel.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 6;
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = false;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;
            hLayout.padding = new RectOffset(6, 6, 4, 4);

            // Tower buttons - Wall first, then attack towers
            wallButton = CreateTowerButton(rightSidePanel.transform, "WallBtn", "WALL", 25, OnWallClicked);
            wallBtnImage = wallButton.GetComponent<Image>();

            archerButton = CreateTowerButton(rightSidePanel.transform, "ArcherBtn", "ARCHER", 50, OnArcherClicked);
            archerBtnImage = archerButton.GetComponent<Image>();

            mageButton = CreateTowerButton(rightSidePanel.transform, "MageBtn", "MAGE", 100, OnMageClicked);
            mageBtnImage = mageButton.GetComponent<Image>();

            cannonButton = CreateTowerButton(rightSidePanel.transform, "CannonBtn", "CANNON", 150, OnCannonClicked);
            cannonBtnImage = cannonButton.GetComponent<Image>();
        }

        private void CreateStartButton(Transform parent)
        {
            startWavePanel = new GameObject("StartBtn");
            startWavePanel.transform.SetParent(parent, false);

            RectTransform rect = startWavePanel.AddComponent<RectTransform>();

            startWaveButtonImage = startWavePanel.AddComponent<Image>();
            startWaveButtonImage.color = accentColor;

            startWaveButton = startWavePanel.AddComponent<Button>();
            startWaveButton.targetGraphic = startWaveButtonImage;
            startWaveButton.onClick.AddListener(OnStartWaveClicked);

            startWaveButtonText = CreateText(startWavePanel.transform, "Label", "START", 12, TextAnchor.MiddleCenter);
            startWaveButtonText.fontStyle = FontStyle.Bold;
            RectTransform textRect = startWaveButtonText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            // Bitcoin icon for bonus (hidden by default)
            startButtonBtcIcon = new GameObject("BtcIcon");
            startButtonBtcIcon.transform.SetParent(startWavePanel.transform, false);
            RectTransform iconRect = startButtonBtcIcon.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(1, 0.5f);
            iconRect.anchorMax = new Vector2(1, 0.5f);
            iconRect.pivot = new Vector2(1, 0.5f);
            iconRect.anchoredPosition = new Vector2(-2, 0);
            iconRect.sizeDelta = new Vector2(10, 10);
            Image iconImage = startButtonBtcIcon.AddComponent<Image>();
            iconImage.sprite = btcIconSprite;
            startButtonBtcIcon.SetActive(false);
        }

        private void CreateResetButton(Transform parent)
        {
            GameObject resetPanel = new GameObject("ResetBtn");
            resetPanel.transform.SetParent(parent, false);

            RectTransform rect = resetPanel.AddComponent<RectTransform>();

            Image bg = resetPanel.AddComponent<Image>();
            bg.color = dangerColor;

            Button resetButton = resetPanel.AddComponent<Button>();
            resetButton.targetGraphic = bg;
            resetButton.onClick.AddListener(OnResetClicked);

            Text btnText = CreateText(resetPanel.transform, "Label", "RESET", 12, TextAnchor.MiddleCenter);
            btnText.fontStyle = FontStyle.Bold;
            RectTransform textRect = btnText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
        }

        private void CreateSpeedButton(Transform parent)
        {
            GameObject speedPanel = new GameObject("SpeedBtn");
            speedPanel.transform.SetParent(parent, false);

            RectTransform rect = speedPanel.AddComponent<RectTransform>();

            Image bg = speedPanel.AddComponent<Image>();
            bg.color = new Color(0.3f, 0.5f, 0.7f, 1f);

            speedButton = speedPanel.AddComponent<Button>();
            speedButton.targetGraphic = bg;
            speedButton.onClick.AddListener(OnSpeedClicked);

            speedButtonText = CreateText(speedPanel.transform, "Label", "1x", 14, TextAnchor.MiddleCenter);
            speedButtonText.fontStyle = FontStyle.Bold;
            RectTransform textRect = speedButtonText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
        }

        private void CreateUndoButton(Transform parent)
        {
            GameObject undoPanel = new GameObject("UndoBtn");
            undoPanel.transform.SetParent(parent, false);

            RectTransform rect = undoPanel.AddComponent<RectTransform>();

            Image bg = undoPanel.AddComponent<Image>();
            bg.color = new Color(0.5f, 0.4f, 0.6f, 1f);

            undoButton = undoPanel.AddComponent<Button>();
            undoButton.targetGraphic = bg;
            undoButton.onClick.AddListener(OnUndoClicked);

            undoButtonText = CreateText(undoPanel.transform, "Label", "UNDO", 12, TextAnchor.MiddleCenter);
            undoButtonText.fontStyle = FontStyle.Bold;
            RectTransform textRect = undoButtonText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            UpdateUndoButton(UndoManager.Instance?.CanUndo ?? false);
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

            // Get button image for color change
            Image undoBtnImage = undoButton?.GetComponent<Image>();
            if (undoBtnImage != null)
            {
                if (!isBuildPhase)
                {
                    // During wave - muted shadowy disabled look
                    undoBtnImage.color = new Color(0.2f, 0.2f, 0.25f, 0.5f);
                }
                else if (canUndo)
                {
                    // Build phase with undo available - normal purple
                    undoBtnImage.color = new Color(0.5f, 0.4f, 0.6f, 1f);
                }
                else
                {
                    // Build phase but nothing to undo - slightly muted
                    undoBtnImage.color = new Color(0.35f, 0.3f, 0.4f, 0.7f);
                }
            }

            if (undoButtonText != null)
            {
                int count = UndoManager.Instance?.HistoryCount ?? 0;
                if (!isBuildPhase)
                {
                    undoButtonText.text = "UNDO";
                    undoButtonText.color = new Color(1f, 1f, 1f, 0.4f); // Faded text
                }
                else if (canUndo)
                {
                    undoButtonText.text = $"UNDO({count})";
                    undoButtonText.color = Color.white;
                }
                else
                {
                    undoButtonText.text = "UNDO";
                    undoButtonText.color = new Color(1f, 1f, 1f, 0.6f);
                }
            }
        }

        private Button CreateTowerButton(Transform parent, string name, string title, int cost, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(78, 60);

            Image bg = btnObj.AddComponent<Image>();
            bg.color = normalBtnColor;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(onClick);

            VerticalLayoutGroup layout = btnObj.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.spacing = 2;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;

            // Title - original font size 14
            Text titleText = CreateText(btnObj.transform, "Title", title, 14, TextAnchor.MiddleCenter);
            titleText.fontStyle = FontStyle.Bold;
            RectTransform titleRect = titleText.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(0, 20);

            // Cost row
            GameObject costContainer = new GameObject("CostContainer");
            costContainer.transform.SetParent(btnObj.transform, false);
            RectTransform costContainerRect = costContainer.AddComponent<RectTransform>();
            costContainerRect.sizeDelta = new Vector2(0, 22);

            HorizontalLayoutGroup costLayout = costContainer.AddComponent<HorizontalLayoutGroup>();
            costLayout.spacing = 2;
            costLayout.childAlignment = TextAnchor.MiddleCenter;
            costLayout.childControlWidth = false;
            costLayout.childControlHeight = false;

            Text costText = CreateText(costContainer.transform, "CostText", cost.ToString(), 18, TextAnchor.MiddleRight);
            costText.color = goldColor;
            costText.fontStyle = FontStyle.Bold;
            RectTransform costTextRect = costText.GetComponent<RectTransform>();
            costTextRect.sizeDelta = new Vector2(40, 22);

            GameObject iconObj = new GameObject("BtcIcon");
            iconObj.transform.SetParent(costContainer.transform, false);
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(16, 16);
            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.sprite = btcIconSprite;
            iconImage.color = Color.white;

            return btn;
        }

        private void CreateWaveAnnouncement()
        {
            // Create wave announcement overlay (centered on screen)
            GameObject announcementObj = new GameObject("WaveAnnouncement");
            announcementObj.transform.SetParent(transform, false);

            RectTransform rect = announcementObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(400, 150);

            waveAnnouncementGroup = announcementObj.AddComponent<CanvasGroup>();
            waveAnnouncementGroup.alpha = 0f;

            // Large wave text - gold color to match UI theme
            waveAnnouncementText = CreateText(announcementObj.transform, "WaveText", "WAVE 1", 90, TextAnchor.MiddleCenter);
            waveAnnouncementText.fontStyle = FontStyle.Bold;
            waveAnnouncementText.color = goldColor;
            RectTransform textRect = waveAnnouncementText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            // Add stronger outline
            var outline = waveAnnouncementText.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = new Color(0, 0, 0, 0.9f);
                outline.effectDistance = new Vector2(3, -3);
            }
        }

        private void UpdateStartButtonIcon(bool showIcon)
        {
            if (startButtonBtcIcon != null)
            {
                startButtonBtcIcon.SetActive(showIcon);
            }
        }

        private void OnSpeedClicked()
        {
            isDoubleSpeed = !isDoubleSpeed;
            Time.timeScale = isDoubleSpeed ? 2f : 1f;
            speedButtonText.text = isDoubleSpeed ? "2x" : "1x";
        }

        private GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 position, Vector2 size)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image bg = panel.AddComponent<Image>();
            bg.color = panelColor;

            return panel;
        }

        private Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor alignment)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            Text text = textObj.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;

            // Prevent text clipping
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            // Use LegacyRuntime (Unity's default UI font)
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Sharp outline for crisp readability
            Outline outline = textObj.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 1f);
            outline.effectDistance = new Vector2(1, -1);

            return text;
        }

        private void Update()
        {
            // Update start button state and bonus display frequently
            UpdateStartWaveButton();

            // Keyboard shortcuts (matches UI order: Wall first, then upgrades)
            if (Input.GetKeyDown(KeyCode.Alpha1))
                OnWallClicked();
            else if (Input.GetKeyDown(KeyCode.Alpha2))
                OnArcherClicked();
            else if (Input.GetKeyDown(KeyCode.Alpha3))
                OnMageClicked();
            else if (Input.GetKeyDown(KeyCode.Alpha4))
                OnCannonClicked();
            else if (Input.GetKeyDown(KeyCode.Space))
                OnStartWaveClicked();
            else if (Input.GetKeyDown(KeyCode.R))
                OnResetClicked();
            // Ctrl+Z or Cmd+Z for undo
            else if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
                      Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)) &&
                     Input.GetKeyDown(KeyCode.Z))
                OnUndoClicked();
        }

        private void OnArcherClicked()
        {
            TowerPlacementSystem.Instance?.SelectArcherTower();
            UpdateTowerHighlights();
        }

        private void OnMageClicked()
        {
            TowerPlacementSystem.Instance?.SelectMageTower();
            UpdateTowerHighlights();
        }

        private void OnCannonClicked()
        {
            TowerPlacementSystem.Instance?.SelectCannonTower();
            UpdateTowerHighlights();
        }

        private void OnWallClicked()
        {
            TowerPlacementSystem.Instance?.SelectWallTower();
            UpdateTowerHighlights();
        }

        private void OnStartWaveClicked()
        {
            var state = GameManager.Instance?.CurrentState;
            if (state == GameState.Building || state == GameState.WaveActive)
            {
                WaveManager.Instance?.StartWaveEarly();
            }
        }

        private void OnResetClicked()
        {
            // Reset speed to normal
            isDoubleSpeed = false;
            Time.timeScale = 1f;
            if (speedButtonText != null)
                speedButtonText.text = "1x";

            // Clear tower selection (world-space UI handles hide via event)
            TowerSelectionManager.Instance?.DeselectTower();

            GameManager.Instance?.RestartGame();
        }

        private void UpdateTowerHighlights()
        {
            if (TowerPlacementSystem.Instance == null) return;

            var selected = TowerPlacementSystem.Instance.SelectedTowerData;
            bool archerSelected = selected != null && selected.name.Contains("Archer");
            bool mageSelected = selected != null && selected.name.Contains("Mage");
            bool cannonSelected = selected != null && selected.name.Contains("Cannon");
            bool wallSelected = selected != null && selected.name.Contains("Wall");

            // Get affordability
            int currency = EconomyManager.Instance?.CurrentCurrency ?? 0;
            var tps = TowerPlacementSystem.Instance;
            bool canAffordArcher = tps?.ArcherTowerData != null && currency >= tps.ArcherTowerData.cost;
            bool canAffordMage = tps?.MageTowerData != null && currency >= tps.MageTowerData.cost;
            bool canAffordCannon = tps?.CannonTowerData != null && currency >= tps.CannonTowerData.cost;
            bool canAffordWall = tps?.WallTowerData != null && currency >= tps.WallTowerData.cost;

            // Update button colors and interactability
            if (archerButton != null)
            {
                archerButton.interactable = canAffordArcher;
                if (archerBtnImage != null)
                    archerBtnImage.color = !canAffordArcher ? disabledBtnColor : (archerSelected ? selectedColor : normalBtnColor);
            }
            if (mageButton != null)
            {
                mageButton.interactable = canAffordMage;
                if (mageBtnImage != null)
                    mageBtnImage.color = !canAffordMage ? disabledBtnColor : (mageSelected ? selectedColor : normalBtnColor);
            }
            if (cannonButton != null)
            {
                cannonButton.interactable = canAffordCannon;
                if (cannonBtnImage != null)
                    cannonBtnImage.color = !canAffordCannon ? disabledBtnColor : (cannonSelected ? selectedColor : normalBtnColor);
            }
            if (wallButton != null)
            {
                wallButton.interactable = canAffordWall;
                if (wallBtnImage != null)
                    wallBtnImage.color = !canAffordWall ? disabledBtnColor : (wallSelected ? selectedColor : normalBtnColor);
            }
        }

        private void SubscribeToEvents()
        {
            GameEvents.OnCurrencyChanged += UpdateGold;
            GameEvents.OnLivesChanged += UpdateLives;
            GameEvents.OnWaveStart += UpdateWave;
            GameEvents.OnBuildPhaseTimerUpdate += UpdateTimer;
            GameEvents.OnBuildPhaseStart += OnBuildPhaseStart;
            GameEvents.OnWaveComplete += OnWaveComplete;
            GameEvents.OnGameStateChanged += OnGameStateChanged;
            GameEvents.OnTowerPlaced += OnTowerPlaced;
            GameEvents.OnUndoStateChanged += UpdateUndoButton;
            // Tower selection UI is handled by TowerInfoWorldUI (world-space)
        }

        private void OnDestroy()
        {
            GameEvents.OnCurrencyChanged -= UpdateGold;
            GameEvents.OnLivesChanged -= UpdateLives;
            GameEvents.OnWaveStart -= UpdateWave;
            GameEvents.OnBuildPhaseTimerUpdate -= UpdateTimer;
            GameEvents.OnBuildPhaseStart -= OnBuildPhaseStart;
            GameEvents.OnWaveComplete -= OnWaveComplete;
            GameEvents.OnGameStateChanged -= OnGameStateChanged;
            GameEvents.OnTowerPlaced -= OnTowerPlaced;
            GameEvents.OnUndoStateChanged -= UpdateUndoButton;
        }

        private void OnTowerPlaced(Tower tower)
        {
            // Update tower button highlights after placement (selection is cleared)
            UpdateTowerHighlights();
        }

        private void UpdateAllDisplays()
        {
            if (EconomyManager.Instance != null)
                UpdateGold(EconomyManager.Instance.CurrentCurrency);
            if (GameManager.Instance != null)
                UpdateLives(GameManager.Instance.CurrentLives);
            if (WaveManager.Instance != null)
                UpdateWave(WaveManager.Instance.CurrentWave);
        }

        private void UpdateGold(int amount)
        {
            if (goldText != null)
                goldText.text = amount.ToString();

            // Update tower button affordability when currency changes
            UpdateTowerHighlights();
        }

        private void UpdateLives(int lives)
        {
            if (livesText != null)
                livesText.text = lives.ToString();
        }

        private void UpdateWave(int wave)
        {
            if (waveText != null)
            {
                int total = GameManager.Instance?.WaveConfig?.totalWaves ?? 15;
                waveText.text = $"{wave}/{total}";
            }

            // Trigger wave announcement animation
            if (wave > 0)
            {
                StartCoroutine(PlayWaveAnnouncement(wave));
            }
        }

        private IEnumerator PlayWaveAnnouncement(int wave)
        {
            if (waveAnnouncementText == null || waveAnnouncementGroup == null) yield break;

            // Set text
            waveAnnouncementText.text = $"WAVE {wave}";

            // Get the transform for scaling
            RectTransform rect = waveAnnouncementText.GetComponent<RectTransform>();
            if (rect == null) yield break;

            // Animation settings
            float zoomInDuration = 0.3f;
            float holdDuration = 0.8f;
            float zoomOutDuration = 0.3f;

            // Start small and transparent
            rect.localScale = Vector3.one * 0.3f;
            waveAnnouncementGroup.alpha = 0f;

            // Zoom in and fade in
            float elapsed = 0f;
            while (elapsed < zoomInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / zoomInDuration;
                float easeOut = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic

                rect.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.2f, easeOut);
                waveAnnouncementGroup.alpha = Mathf.Lerp(0f, 1f, easeOut);
                yield return null;
            }

            // Settle to normal size
            elapsed = 0f;
            float settleDuration = 0.1f;
            while (elapsed < settleDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / settleDuration;
                rect.localScale = Vector3.one * Mathf.Lerp(1.2f, 1f, t);
                yield return null;
            }

            // Hold
            yield return new WaitForSeconds(holdDuration);

            // Zoom out and fade out
            elapsed = 0f;
            while (elapsed < zoomOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / zoomOutDuration;
                float easeIn = t * t; // Ease in quadratic

                rect.localScale = Vector3.one * Mathf.Lerp(1f, 1.5f, easeIn);
                waveAnnouncementGroup.alpha = Mathf.Lerp(1f, 0f, easeIn);
                yield return null;
            }

            // Ensure fully hidden
            waveAnnouncementGroup.alpha = 0f;
        }

        private void UpdateTimer(float time)
        {
            if (timerText == null) return;

            var waveManager = WaveManager.Instance;
            var state = GameManager.Instance?.CurrentState;

            if (state == GameState.Building)
            {
                // During build phase - show countdown
                if (time > 0)
                    timerText.text = $"{Mathf.CeilToInt(time)}s";
                else
                    timerText.text = "";
            }
            else if (state == GameState.WaveActive && waveManager != null)
            {
                // During wave - show countdown to next wave
                if (time > 0)
                {
                    timerText.text = $"{Mathf.CeilToInt(time)}s";
                }
                else
                {
                    timerText.text = "";
                }
            }
            else
            {
                timerText.text = "";
            }
        }

        private void OnBuildPhaseStart()
        {
            UpdateStartWaveButton();
        }

        private void OnWaveComplete(int wave)
        {
            if (timerText != null)
                timerText.text = "";
            UpdateStartWaveButton();
        }

        private void OnGameStateChanged(GameState newState)
        {
            UpdateStartWaveButton();
            UpdateTowerHighlights();
            UpdateUndoButton(UndoManager.Instance?.CanUndo ?? false);
        }

        private void UpdateStartWaveButton()
        {
            if (startWavePanel == null) return;

            var state = GameManager.Instance?.CurrentState;
            var waveManager = WaveManager.Instance;

            bool canStartDuringBuild = state == GameState.Building;
            bool canSendDuringWave = state == GameState.WaveActive && waveManager != null && waveManager.CanSendNextWave;
            bool isWaveActive = state == GameState.WaveActive;
            bool canPress = canStartDuringBuild || canSendDuringWave;

            // Button always visible, but color and interactability change
            startWaveButton.interactable = canPress;

            if (startWaveButtonImage != null)
            {
                // Green when available, red when on cooldown
                startWaveButtonImage.color = canPress ? accentColor : dangerColor;
            }

            // Update button text
            if (startWaveButtonText != null)
            {
                if (canStartDuringBuild)
                {
                    // Build phase - can start
                    int bonus = waveManager?.GetCurrentEarlyBonus() ?? 0;
                    startWaveButtonText.text = bonus > 0 ? $"START\n+{bonus}" : "START";
                    UpdateStartButtonIcon(bonus > 0);
                }
                else if (canSendDuringWave)
                {
                    // Wave active - can send next wave early
                    int bonus = waveManager?.GetCurrentEarlyBonus() ?? 0;
                    startWaveButtonText.text = bonus > 0 ? $"SEND\n+{bonus}" : "SEND";
                    UpdateStartButtonIcon(bonus > 0);
                }
                else if (isWaveActive && waveManager != null)
                {
                    // Wave active but on cooldown - show timer
                    float waitTime = waveManager.TimeUntilCanSendNext;
                    if (waitTime > 0)
                    {
                        startWaveButtonText.text = $"WAIT\n{Mathf.CeilToInt(waitTime)}s";
                    }
                    else
                    {
                        startWaveButtonText.text = "WAIT";
                    }
                    UpdateStartButtonIcon(false);
                }
                else
                {
                    // Game over or other state
                    startWaveButtonText.text = "---";
                    UpdateStartButtonIcon(false);
                }
            }
        }
    }
}
