using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TowerDefense.Core;
using TowerDefense.Data;

namespace TowerDefense.UI
{
    /// <summary>
    /// World-space UI panel that floats above selected towers.
    /// Shows tower stats, sell button, and upgrade button.
    /// </summary>
    [DefaultExecutionOrder(-50)] // Run before TowerSelectionManager to handle button clicks first
    public class TowerInfoWorldUI : MonoBehaviour
    {
        public static TowerInfoWorldUI Instance { get; private set; }

        // UI Elements
        private GameObject panelContainer;
        private Canvas worldCanvas;
        private Text towerNameText;
        private Text towerStatsText;
        private Text levelText;
        private Button sellButton;
        private Text sellButtonText;
        private Button upgradeButton;
        private Text upgradeButtonText;
        private Image upgradeButtonImage;

        // Colors
        private Color panelColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        private Color goldColor = new Color(1f, 0.85f, 0.3f, 1f);
        private Color dangerColor = new Color(0.9f, 0.3f, 0.3f, 1f);
        private Color accentColor = new Color(0.3f, 0.7f, 0.4f, 1f);
        private Color disabledColor = new Color(0.3f, 0.3f, 0.35f, 0.8f);

        // Tracking
        private Tower currentTower;
        private float panelYOffset = 2.5f;

        // Flag to prevent tower deselection when clicking UI buttons
        public static bool WasButtonClickedThisFrame { get; private set; }

        // Bitcoin icon
        private Sprite btcIconSprite;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Debug.Log("[TowerInfoWorldUI] Instance created");
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            CreateBitcoinIcon();
            CreateWorldSpaceUI();
            Debug.Log($"[TowerInfoWorldUI] Awake complete, panelContainer={panelContainer != null}");
        }

        private void Start()
        {
            // Ensure world canvas has camera reference (Camera.main may be null in Awake)
            if (worldCanvas != null)
            {
                worldCanvas.worldCamera = Camera.main;
                Debug.Log($"[TowerInfoWorldUI] Start - Set worldCamera to {worldCanvas.worldCamera?.name ?? "NULL"}");
            }
        }

        private void Update()
        {
            // Reset the flag at the start of each frame
            WasButtonClickedThisFrame = false;

            // Handle clicks when panel is visible
            if (panelContainer != null && panelContainer.activeSelf && Input.GetMouseButtonDown(0))
            {
                HandlePanelClick();
            }
        }

        private void HandlePanelClick()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector2 mousePos = Input.mousePosition;

            // Check if click is on sell button (screen-space)
            if (sellButton != null && sellButton.gameObject.activeInHierarchy)
            {
                if (IsMouseOverRect(sellButton.GetComponent<RectTransform>(), cam, mousePos))
                {
                    Debug.Log("[TowerInfoWorldUI] Sell button clicked!");
                    WasButtonClickedThisFrame = true;
                    OnSellClicked();
                    return;
                }
            }

            // Check if click is on upgrade button (screen-space)
            if (upgradeButton != null && upgradeButton.gameObject.activeInHierarchy && upgradeButton.interactable)
            {
                if (IsMouseOverRect(upgradeButton.GetComponent<RectTransform>(), cam, mousePos))
                {
                    Debug.Log("[TowerInfoWorldUI] Upgrade button clicked!");
                    WasButtonClickedThisFrame = true;
                    OnUpgradeClicked();
                    return;
                }
            }

            // Check if click is within the panel bounds (screen-space)
            RectTransform panelRect = panelContainer.GetComponent<RectTransform>();
            if (IsMouseOverRect(panelRect, cam, mousePos))
            {
                // Clicked on panel but not on a button - do nothing, keep selection
                Debug.Log("[TowerInfoWorldUI] Clicked on panel background - keeping selection");
                WasButtonClickedThisFrame = true;
                return;
            }

            // Click is OUTSIDE the panel - deselect tower to close UI
            Debug.Log("[TowerInfoWorldUI] Clicked outside panel - deselecting tower");
            WasButtonClickedThisFrame = true; // Prevent TowerSelectionManager from also processing

            if (TowerSelectionManager.Instance != null)
            {
                TowerSelectionManager.Instance.DeselectTower();
            }
        }

        /// <summary>
        /// Check if mouse position is over a RectTransform using screen-space projection
        /// </summary>
        private bool IsMouseOverRect(RectTransform rect, Camera cam, Vector2 mousePos)
        {
            if (rect == null) return false;

            // Get world corners and project to screen space
            Vector3[] worldCorners = new Vector3[4];
            rect.GetWorldCorners(worldCorners);

            // Project corners to screen space
            Vector2[] screenCorners = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                Vector3 screenPoint = cam.WorldToScreenPoint(worldCorners[i]);
                screenCorners[i] = new Vector2(screenPoint.x, screenPoint.y);
            }

            // Get bounding box in screen space
            float minX = Mathf.Min(screenCorners[0].x, screenCorners[1].x, screenCorners[2].x, screenCorners[3].x);
            float maxX = Mathf.Max(screenCorners[0].x, screenCorners[1].x, screenCorners[2].x, screenCorners[3].x);
            float minY = Mathf.Min(screenCorners[0].y, screenCorners[1].y, screenCorners[2].y, screenCorners[3].y);
            float maxY = Mathf.Max(screenCorners[0].y, screenCorners[1].y, screenCorners[2].y, screenCorners[3].y);

            // Check if mouse is within bounds
            bool isOver = mousePos.x >= minX && mousePos.x <= maxX && mousePos.y >= minY && mousePos.y <= maxY;

            return isOver;
        }

        private void OnEnable()
        {
            GameEvents.OnTowerSelected += OnTowerSelected;
            GameEvents.OnTowerDeselected += OnTowerDeselected;
            GameEvents.OnTowerUpgraded += OnTowerUpgraded;
            GameEvents.OnCurrencyChanged += OnCurrencyChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnTowerSelected -= OnTowerSelected;
            GameEvents.OnTowerDeselected -= OnTowerDeselected;
            GameEvents.OnTowerUpgraded -= OnTowerUpgraded;
            GameEvents.OnCurrencyChanged -= OnCurrencyChanged;
        }

        private void LateUpdate()
        {
            if (panelContainer == null || !panelContainer.activeSelf || currentTower == null)
                return;

            // Position panel above tower
            Vector3 worldPos = currentTower.transform.position + Vector3.up * panelYOffset;
            panelContainer.transform.position = worldPos;

            // Face the camera
            var cam = Camera.main;
            if (cam != null)
            {
                panelContainer.transform.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);

                // Ensure world canvas has camera reference
                if (worldCanvas != null && worldCanvas.worldCamera == null)
                {
                    worldCanvas.worldCamera = cam;
                }
            }
        }

        private void CreateBitcoinIcon()
        {
            int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            Color clear = new Color(0, 0, 0, 0);
            Color gold = new Color(1f, 0.85f, 0.3f, 1f);

            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    texture.SetPixel(x, y, clear);

            int stroke = 8;
            int left = 12;
            int innerLeft = left + stroke;
            int bumpRight = 48;
            int bottom = 8;
            int top = 56;
            int mid = 32;
            int gap = 3;

            for (int x = left; x < left + stroke; x++)
                for (int y = bottom; y <= top; y++)
                    texture.SetPixel(x, y, gold);

            for (int x = left; x <= bumpRight - 6; x++)
                for (int y = top - stroke; y <= top; y++)
                    texture.SetPixel(x, y, gold);

            for (int x = bumpRight - stroke; x <= bumpRight; x++)
                for (int y = mid + gap; y <= top - stroke; y++)
                    texture.SetPixel(x, y, gold);

            for (int x = innerLeft; x <= bumpRight - 6; x++)
                for (int y = mid + gap; y <= mid + gap + stroke; y++)
                    texture.SetPixel(x, y, gold);

            for (int x = left; x <= bumpRight - 6; x++)
                for (int y = bottom; y < bottom + stroke; y++)
                    texture.SetPixel(x, y, gold);

            for (int x = bumpRight - stroke; x <= bumpRight; x++)
                for (int y = bottom + stroke; y <= mid - gap; y++)
                    texture.SetPixel(x, y, gold);

            for (int x = innerLeft; x <= bumpRight - 6; x++)
                for (int y = mid - gap - stroke; y <= mid - gap; y++)
                    texture.SetPixel(x, y, gold);

            int line1 = 24;
            int line2 = 38;
            int lineW = 5;

            for (int x = line1; x < line1 + lineW; x++)
                for (int y = top + 1; y <= 62; y++)
                    texture.SetPixel(x, y, gold);
            for (int x = line2; x < line2 + lineW; x++)
                for (int y = top + 1; y <= 62; y++)
                    texture.SetPixel(x, y, gold);

            for (int x = line1; x < line1 + lineW; x++)
                for (int y = 2; y < bottom; y++)
                    texture.SetPixel(x, y, gold);
            for (int x = line2; x < line2 + lineW; x++)
                for (int y = 2; y < bottom; y++)
                    texture.SetPixel(x, y, gold);

            texture.Apply();
            btcIconSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private void CreateWorldSpaceUI()
        {
            // Create container
            panelContainer = new GameObject("TowerInfoWorldUI");
            panelContainer.transform.SetParent(transform);

            // Create world-space canvas
            worldCanvas = panelContainer.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.worldCamera = Camera.main;
            worldCanvas.sortingOrder = 100; // High sorting order to be on top

            RectTransform canvasRect = panelContainer.GetComponent<RectTransform>();
            // Use pixel-sized dimensions, then scale down to world units
            // Larger canvas = better text resolution
            canvasRect.sizeDelta = new Vector2(300f, 180f);
            canvasRect.localScale = Vector3.one * 0.008f; // 300 * 0.008 = 2.4 world units wide

            // NOTE: Do NOT add CanvasScaler to WorldSpace canvases - it doesn't work correctly
            // and can cause text rendering issues

            // Add graphic raycaster for button clicks - critical for world-space UI
            var raycaster = panelContainer.AddComponent<GraphicRaycaster>();
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None; // Don't block on 3D objects

            Debug.Log($"[TowerInfoWorldUI] Canvas created: sizeDelta={canvasRect.sizeDelta}, scale={canvasRect.localScale}");

            // Create panel background
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(panelContainer.transform, false);

            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            Image panelBg = panel.AddComponent<Image>();
            panelBg.color = panelColor;
            panelBg.raycastTarget = false; // Don't intercept clicks - let buttons handle them

            // Vertical layout
            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 10, 10);
            layout.spacing = 6;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;

            // Tower name + level
            GameObject nameRow = new GameObject("NameRow");
            nameRow.transform.SetParent(panel.transform, false);
            RectTransform nameRowRect = nameRow.AddComponent<RectTransform>();
            nameRowRect.sizeDelta = new Vector2(0, 45); // Increased height for larger font

            HorizontalLayoutGroup nameLayout = nameRow.AddComponent<HorizontalLayoutGroup>();
            nameLayout.spacing = 8;
            nameLayout.childAlignment = TextAnchor.MiddleCenter;
            nameLayout.childControlWidth = false;
            nameLayout.childControlHeight = false;

            towerNameText = CreateText(nameRow.transform, "TowerName", "Tower", 28, TextAnchor.MiddleCenter);
            towerNameText.fontStyle = FontStyle.Bold;
            towerNameText.color = goldColor;
            RectTransform nameRect = towerNameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.5f, 0.5f);
            nameRect.anchorMax = new Vector2(0.5f, 0.5f);
            nameRect.sizeDelta = new Vector2(180, 45);

            levelText = CreateText(nameRow.transform, "Level", "Lv.1", 22, TextAnchor.MiddleLeft);
            levelText.color = accentColor;
            levelText.fontStyle = FontStyle.Bold;
            RectTransform levelRect = levelText.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0.5f, 0.5f);
            levelRect.anchorMax = new Vector2(0.5f, 0.5f);
            levelRect.sizeDelta = new Vector2(60, 45);

            // Stats
            towerStatsText = CreateText(panel.transform, "Stats", "DMG: 0 | RNG: 0", 20, TextAnchor.MiddleCenter);
            RectTransform statsRect = towerStatsText.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.5f, 0.5f);
            statsRect.anchorMax = new Vector2(0.5f, 0.5f);
            statsRect.sizeDelta = new Vector2(280, 30);

            // Button row
            GameObject buttonRow = new GameObject("ButtonRow");
            buttonRow.transform.SetParent(panel.transform, false);
            RectTransform buttonRowRect = buttonRow.AddComponent<RectTransform>();
            buttonRowRect.sizeDelta = new Vector2(0, 45); // Increased height for buttons

            HorizontalLayoutGroup buttonLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 12;
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.childControlWidth = false;
            buttonLayout.childControlHeight = false;

            // Sell button - dark background, red text
            sellButton = CreateButton(buttonRow.transform, "SellBtn", "+$0", panelColor, 90, 40);
            sellButtonText = sellButton.GetComponentInChildren<Text>();
            sellButtonText.color = dangerColor;
            sellButton.onClick.AddListener(OnSellClicked);

            // Upgrade button - dark background, green text
            upgradeButton = CreateButton(buttonRow.transform, "UpgradeBtn", "-$0", panelColor, 90, 40);
            upgradeButtonText = upgradeButton.GetComponentInChildren<Text>();
            upgradeButtonText.color = accentColor;
            upgradeButtonImage = upgradeButton.GetComponent<Image>();
            upgradeButton.onClick.AddListener(OnUpgradeClicked);

            // Hide initially
            panelContainer.SetActive(false);
        }

        private Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor alignment)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(280, fontSize + 20);

            Text text = textObj.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;

            // Try multiple font loading approaches
            Font loadedFont = null;

            // Approach 1: LegacyRuntime.ttf (Unity 6+)
            loadedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Approach 2: Arial.ttf (older Unity)
            if (loadedFont == null)
            {
                loadedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            // Approach 3: Find any font in the project
            if (loadedFont == null)
            {
                loadedFont = Resources.Load<Font>("Fonts/Arial");
            }

            // Approach 4: Use Font.CreateDynamicFontFromOSFont
            if (loadedFont == null)
            {
                string[] fontNames = Font.GetOSInstalledFontNames();
                if (fontNames != null && fontNames.Length > 0)
                {
                    // Try to find Arial or a common font
                    string fontToUse = System.Array.Find(fontNames, f => f.ToLower().Contains("arial"));
                    if (fontToUse == null) fontToUse = fontNames[0];

                    loadedFont = Font.CreateDynamicFontFromOSFont(fontToUse, fontSize);
                    Debug.Log($"[TowerInfoWorldUI] Created dynamic font from OS: {fontToUse}");
                }
            }

            text.font = loadedFont;

            if (text.font != null)
            {
                Debug.Log($"[TowerInfoWorldUI] Font loaded successfully: {text.font.name} for '{name}'");
            }
            else
            {
                Debug.LogError($"[TowerInfoWorldUI] CRITICAL: NO FONT LOADED for '{name}'! Text will not render.");
            }

            // Prevent text clipping
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            // Text should not block button clicks
            text.raycastTarget = false;

            // NOTE: Removed Outline component - can cause rendering issues in URP world-space canvas
            // Use shadow instead which is more compatible
            Shadow shadow = textObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.8f);
            shadow.effectDistance = new Vector2(2f, -2f);

            return text;
        }

        private Button CreateButton(Transform parent, string name, string label, Color bgColor, float width, float height)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);

            Image bg = btnObj.AddComponent<Image>();
            bg.color = bgColor;
            bg.raycastTarget = true; // Explicitly enable raycast for button clicks

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.interactable = true;

            // Set up button colors for visual feedback
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            btn.colors = colors;

            Text btnText = CreateText(btnObj.transform, "Label", label, 22, TextAnchor.MiddleCenter);
            btnText.fontStyle = FontStyle.Bold;
            RectTransform textRect = btnText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.sizeDelta = Vector2.zero; // Fill parent when using anchors

            Debug.Log($"[TowerInfoWorldUI] Button '{name}' created with text '{label}', font={btnText.font?.name ?? "NULL"}, raycastTarget={bg.raycastTarget}");

            return btn;
        }

        private void OnTowerSelected(Tower tower)
        {
            Debug.Log($"[TowerInfoWorldUI] OnTowerSelected called, tower={tower?.Data?.towerName}, panelContainer={panelContainer != null}");

            if (tower == null || tower.Data == null)
            {
                Debug.LogWarning("[TowerInfoWorldUI] Tower or tower.Data is null, hiding panel");
                Hide();
                return;
            }

            currentTower = tower;
            UpdatePanel();
            panelContainer.SetActive(true);

            // Debug: Log all text components and their font status
            var allTexts = panelContainer.GetComponentsInChildren<Text>(true);
            Debug.Log($"[TowerInfoWorldUI] Panel activated. Found {allTexts.Length} Text components:");
            foreach (var text in allTexts)
            {
                Debug.Log($"  - '{text.name}': text='{text.text}', font={text.font?.name ?? "NULL"}, fontSize={text.fontSize}, color={text.color}");
            }

            Debug.Log($"[TowerInfoWorldUI] Panel position: {panelContainer.transform.position}, scale: {panelContainer.transform.localScale}");
        }

        private void OnTowerDeselected()
        {
            Hide();
        }

        private void OnTowerUpgraded(Tower tower)
        {
            if (tower == currentTower)
            {
                UpdatePanel();
            }
        }

        private void OnCurrencyChanged(int amount)
        {
            if (currentTower != null)
            {
                UpdateUpgradeButton();
            }
        }

        private void Hide()
        {
            currentTower = null;
            if (panelContainer != null)
            {
                panelContainer.SetActive(false);
            }
        }

        private void UpdatePanel()
        {
            if (currentTower == null || currentTower.Data == null) return;

            // Name
            if (towerNameText != null)
            {
                towerNameText.text = currentTower.Data.towerName ?? "Tower";
            }

            // Level
            if (levelText != null)
            {
                if (currentTower.Data.CanUpgrade)
                {
                    levelText.text = $"Lv.{currentTower.CurrentLevel}";
                    levelText.gameObject.SetActive(true);
                }
                else
                {
                    levelText.gameObject.SetActive(false);
                }
            }

            // Stats
            if (towerStatsText != null)
            {
                if (currentTower.Data.IsAttackTower)
                {
                    float dps = currentTower.CurrentDamage / currentTower.Data.attackSpeed;
                    towerStatsText.text = $"DMG: {currentTower.CurrentDamage:F0} | RNG: {currentTower.CurrentRange:F1}";
                }
                else
                {
                    towerStatsText.text = "Defensive Structure";
                }
            }

            // Sell button - red text with +$XX format
            if (sellButtonText != null)
            {
                sellButtonText.text = $"+${currentTower.SellValue}";
                sellButtonText.color = dangerColor;
            }

            // Upgrade button
            UpdateUpgradeButton();
        }

        private void UpdateUpgradeButton()
        {
            if (currentTower == null) return;

            bool canUpgrade = currentTower.CanUpgrade;
            int upgradeCost = currentTower.UpgradeCost;
            int currency = EconomyManager.Instance?.CurrentCurrency ?? 0;
            bool canAfford = currency >= upgradeCost;

            // Show/hide upgrade button based on whether tower can upgrade
            if (upgradeButton != null)
            {
                upgradeButton.gameObject.SetActive(canUpgrade);

                if (canUpgrade)
                {
                    upgradeButton.interactable = canAfford;

                    // Keep dark background, just dim when can't afford
                    if (upgradeButtonImage != null)
                    {
                        upgradeButtonImage.color = canAfford ? panelColor : disabledColor;
                    }

                    // Green text, dimmed when can't afford
                    if (upgradeButtonText != null)
                    {
                        upgradeButtonText.text = $"-${upgradeCost}";
                        upgradeButtonText.color = canAfford ? accentColor : new Color(0.3f, 0.7f, 0.4f, 0.5f);
                    }
                }
            }
        }

        private void OnSellClicked()
        {
            Debug.Log($"[TowerInfoWorldUI] Sell button clicked! currentTower={currentTower?.Data?.towerName}, TowerSelectionManager exists={TowerSelectionManager.Instance != null}");
            if (TowerSelectionManager.Instance != null)
            {
                TowerSelectionManager.Instance.SellSelectedTower();
            }
            else
            {
                Debug.LogWarning("[TowerInfoWorldUI] TowerSelectionManager.Instance is null!");
            }
        }

        private void OnUpgradeClicked()
        {
            Debug.Log($"[TowerInfoWorldUI] Upgrade button clicked! currentTower={currentTower?.Data?.towerName}");
            if (currentTower != null)
            {
                bool success = currentTower.TryUpgrade();
                Debug.Log($"[TowerInfoWorldUI] Upgrade result: {success}");
            }
            else
            {
                Debug.LogWarning("[TowerInfoWorldUI] currentTower is null!");
            }
        }
    }
}
