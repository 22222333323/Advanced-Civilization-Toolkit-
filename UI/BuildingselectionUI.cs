using ai.behaviours;
using System.Collections;
using System.Collections.Generic; 
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
namespace WorldBoxMod
{
    
    /// <summary>
    /// UI window for selecting buildings to spawn
    /// </summary>
    public class BuildingSelectionUI : MonoBehaviour
    {
        private static BuildingSelectionUI _instance;
        private GameObject _window;
        private Transform _buttonContainer;
        private GameObject _buttonPrefab;
        private WorldTile _selectedTile;

        public static void ShowBuildingSelection(WorldTile pTile)
        {
            // Prevent spam - if window is already open, don't create another
            if (_instance != null && _instance._window != null && _instance._window.activeSelf)
            {
                Debug.LogWarning("[WorldBoxMod] BuildingSelectionUI already open!");
                return;
            }

            if (_instance == null)
            {
                GameObject uiObject = new GameObject("BuildingSelectionUI");
                _instance = uiObject.AddComponent<BuildingSelectionUI>();
            }

            _instance.Display(pTile);
        }

        private void Display(WorldTile pTile)
        {
            try
            {
                _selectedTile = pTile;

                // Ensure EventSystem exists
                if (EventSystem.current == null)
                {
                    Debug.LogWarning("[WorldBoxMod] Creating EventSystem");
                    GameObject eventSystemObj = new GameObject("EventSystem");
                    eventSystemObj.AddComponent<EventSystem>();
                    eventSystemObj.AddComponent<StandaloneInputModule>();
                }

                // Create or clear window
                if (_window == null)
                {
                    CreateWindow();
                }

                if (_window == null || _buttonContainer == null || _buttonPrefab == null)
                {
                        Debug.LogError("[WorldBoxMod] Failed to create window or components! _window=" + (_window != null) + " _buttonContainer=" + (_buttonContainer != null) + " _buttonPrefab=" + (_buttonPrefab != null));
                    return;
                }

                ClearButtons();

                // Check if buildings are loaded
                if (AssetManager.buildings == null || AssetManager.buildings.list == null)
                {
                    Debug.LogError("[WorldBoxMod] AssetManager.buildings not loaded yet!");
                    _window.SetActive(true);
                    return;
                }

                // Populate with buildings
                PopulateBuildings();
                
                // Show window
                _window.SetActive(true);
                Debug.Log("[WorldBoxMod] BuildingSelectionUI displayed");
            }
            catch (Exception e)
            {
                Debug.LogError("[WorldBoxMod] Error in Display: " + e.ToString());
            }
        }

        private void CreateWindow()
        {
            try
            {
                Debug.Log("[WorldBoxMod] CreateWindow: start");

                // Find main canvas or create one
                Canvas canvas = FindObjectOfType<Canvas>();
                Debug.Log("[WorldBoxMod] CreateWindow: found Canvas = " + (canvas != null));
                if (canvas == null)
                {
                    Debug.LogError("[WorldBoxMod] No Canvas found!");
                    return;
                }

                // Ensure canvas has GraphicRaycaster
                if (canvas.GetComponent<GraphicRaycaster>() == null)
                {
                    Debug.Log("[WorldBoxMod] Adding GraphicRaycaster to canvas");
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
                }

                // Create window panel with dark background
                Debug.Log("[WorldBoxMod] Creating window GameObject");
                _window = new GameObject("BuildingSelectionWindow");
                _window.transform.SetParent(canvas.transform, false);

                Debug.Log("[WorldBoxMod] Adding Image to window");
                Image panelImage = _window.AddComponent<Image>();
                panelImage.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

                RectTransform rectTransform = _window.GetComponent<RectTransform>();
                if (rectTransform == null)
                {
                    Debug.LogError("[WorldBoxMod] Window RectTransform is null");
                    return;
                }
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                // Clamp window size so it never exceeds screen size
                float winW = Mathf.Min(900f, Screen.width - 100f);
                float winH = Mathf.Min(600f, Screen.height - 100f);
                winW = Mathf.Max(200f, winW);
                winH = Mathf.Max(150f, winH);
                rectTransform.sizeDelta = new Vector2(winW, winH);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.localScale = Vector3.one;
                rectTransform.localPosition = Vector3.zero;
                // Prevent parent LayoutGroups from moving/resizing this window
                LayoutElement windowLayoutElem = _window.AddComponent<LayoutElement>();
                windowLayoutElem.ignoreLayout = true;
                windowLayoutElem.preferredWidth = winW;
                windowLayoutElem.preferredHeight = winH;
                windowLayoutElem.flexibleWidth = 0;
                windowLayoutElem.flexibleHeight = 0;

                // Add CanvasGroup to block raycasts to UI below
                Debug.Log("[WorldBoxMod] Adding CanvasGroup");
                CanvasGroup canvasGroup = _window.AddComponent<CanvasGroup>();
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;

                // Create title bar
                GameObject titleBarObj = new GameObject("TitleBar");
                titleBarObj.transform.SetParent(_window.transform, false);

                Debug.Log("[WorldBoxMod] Adding Image to titleBar");
                Image titleBarImage = titleBarObj.AddComponent<Image>();
                titleBarImage.color = new Color(0.2f, 0.3f, 0.4f, 1);

                RectTransform titleBarRect = titleBarObj.GetComponent<RectTransform>();
                // Stretch title bar across window width so it won't shift relative to parent
                titleBarRect.anchorMin = new Vector2(0f, 1f);
                titleBarRect.anchorMax = new Vector2(1f, 1f);
                titleBarRect.pivot = new Vector2(0.5f, 1f);
                titleBarRect.sizeDelta = new Vector2(0, 60);
                titleBarRect.anchoredPosition = new Vector2(0, -30);

                // Create title text
                GameObject titleObj = new GameObject("Title");
                titleObj.transform.SetParent(titleBarObj.transform, false);
                Text titleText = titleObj.AddComponent<Text>();
                titleText.text = "Select Building to Spawn";
                titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                titleText.fontSize = 28;
                titleText.fontStyle = FontStyle.Bold;
                titleText.alignment = TextAnchor.MiddleCenter;
                titleText.color = new Color(1, 1, 1, 1);

                RectTransform titleRect = titleObj.GetComponent<RectTransform>();
                titleRect.anchorMin = Vector2.zero;
                titleRect.anchorMax = Vector2.one;
                titleRect.offsetMin = Vector2.zero;
                titleRect.offsetMax = Vector2.zero;

                // Create scroll view for buttons with a proper viewport so scrolling works
                Debug.Log("[WorldBoxMod] Creating ScrollView");
                GameObject scrollViewObj = new GameObject("ScrollView");
                scrollViewObj.transform.SetParent(_window.transform, false);

                RectTransform scrollRect_rt = scrollViewObj.AddComponent<RectTransform>();
                // Anchor stretch to window so it won't pin to screen edges
                scrollRect_rt.anchorMin = new Vector2(0f, 0f);
                scrollRect_rt.anchorMax = new Vector2(1f, 1f);
                // Apply padding inside window
                float padX = 30f;
                float padYTop = 80f; // leave space for title
                float padYBottom = 40f;
                scrollRect_rt.offsetMin = new Vector2(padX, padYBottom);
                scrollRect_rt.offsetMax = new Vector2(-padX, -padYTop);
                scrollRect_rt.anchoredPosition = Vector2.zero;

                ScrollRect scrollRect = scrollViewObj.AddComponent<ScrollRect>();
                Image scrollBg = scrollViewObj.AddComponent<Image>();
                scrollBg.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);

                // Create viewport (required for ScrollRect) and add a RectMask2D
                GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform));
                viewportObj.transform.SetParent(scrollViewObj.transform, false);
                RectTransform viewportRect = viewportObj.GetComponent<RectTransform>();
                viewportRect.anchorMin = new Vector2(0f, 0f);
                viewportRect.anchorMax = new Vector2(1f, 1f);
                viewportRect.offsetMin = Vector2.zero;
                viewportRect.offsetMax = Vector2.zero;
                Image viewportImage = viewportObj.AddComponent<Image>();
                viewportImage.color = new Color(0f, 0f, 0f, 0f);
                viewportObj.AddComponent<RectMask2D>();

                // Create content container with grid layout and place it under viewport
                Debug.Log("[WorldBoxMod] Creating Content object");
                GameObject contentObj = new GameObject("Content", typeof(RectTransform));
                contentObj.transform.SetParent(viewportObj.transform, false);

                RectTransform contentRect = contentObj.GetComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0, 1);
                contentRect.anchorMax = new Vector2(1, 1);
                contentRect.pivot = new Vector2(0.5f, 1);
                contentRect.offsetMin = new Vector2(0, 0);
                contentRect.offsetMax = new Vector2(0, 0);
                contentRect.sizeDelta = new Vector2(0, 200);

                GridLayoutGroup gridLayout = contentObj.AddComponent<GridLayoutGroup>();
                // Responsive multi-column layout: compute columns from window width
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.spacing = new Vector2(8, 8);
                gridLayout.padding = new RectOffset(10, 10, 5, 5);

                // Determine available width inside the window (use winW computed above)
                float minCellWidth = 220f; // desired minimum column width
                float availableWidth = winW - (padX * 2) - (gridLayout.padding.left + gridLayout.padding.right);
                int columns = Mathf.Max(1, Mathf.FloorToInt((availableWidth + gridLayout.spacing.x) / (minCellWidth + gridLayout.spacing.x)));
                gridLayout.constraintCount = columns;

                // Compute actual cell width to evenly fill available space
                float cellWidth = Mathf.Floor((availableWidth - (columns - 1) * gridLayout.spacing.x) / columns);
                float cellHeight = 70f;
                gridLayout.cellSize = new Vector2(Mathf.Max(100f, cellWidth), cellHeight);

                ContentSizeFitter contentSizeFitter = contentObj.AddComponent<ContentSizeFitter>();
                contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                // Wire up scrollRect and tune sensitivity for snappier scrolling
                scrollRect.content = contentRect;
                scrollRect.viewport = viewportRect;
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.scrollSensitivity = 30f;
                scrollRect.decelerationRate = 0.05f;
                scrollRect.inertia = true;

                // Optional: add a visible vertical scrollbar for clarity
                try
                {
                    GameObject scrollbarObj = new GameObject("ScrollbarVertical", typeof(RectTransform));
                    scrollbarObj.transform.SetParent(scrollViewObj.transform, false);
                    RectTransform sbRect = scrollbarObj.GetComponent<RectTransform>();
                    sbRect.anchorMin = new Vector2(1f, 0f);
                    sbRect.anchorMax = new Vector2(1f, 1f);
                    sbRect.sizeDelta = new Vector2(14f, 0f);
                    sbRect.anchoredPosition = new Vector2(-10f, 0f);

                    Image sbImage = scrollbarObj.AddComponent<Image>();
                    sbImage.color = new Color(0.12f, 0.12f, 0.12f, 0.6f);
                    Scrollbar scrollbar = scrollbarObj.AddComponent<Scrollbar>();
                    scrollbar.direction = Scrollbar.Direction.BottomToTop;

                    // Create sliding handle
                    GameObject handle = new GameObject("Handle", typeof(RectTransform));
                    handle.transform.SetParent(scrollbarObj.transform, false);
                    Image handleImage = handle.AddComponent<Image>();
                    handleImage.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);
                    RectTransform handleRect = handle.GetComponent<RectTransform>();
                    handleRect.anchorMin = new Vector2(0f, 0f);
                    handleRect.anchorMax = new Vector2(1f, 1f);
                    handleRect.offsetMin = Vector2.zero;
                    handleRect.offsetMax = Vector2.zero;
                    scrollbar.targetGraphic = handleImage;
                    scrollbar.handleRect = handleRect;

                    scrollRect.verticalScrollbar = scrollbar;
                    scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
                }
                catch {}

                _buttonContainer = contentObj.transform;
                Debug.Log("[WorldBoxMod] _buttonContainer assigned = " + (_buttonContainer != null));

                // Create a plain Button prefab (use Unity Button rather than PowerButton)
                Debug.Log("[WorldBoxMod] Creating building button prefab object (Unity Button)");
                GameObject buttonObj = new GameObject("BuildingButton", typeof(RectTransform));
                buttonObj.transform.SetParent(_buttonContainer, false);

                // Button background image
                Image btnImage = buttonObj.AddComponent<Image>();
                btnImage.color = new Color(0.3f, 0.3f, 0.4f, 1);

                // Button component
                Button btn = buttonObj.AddComponent<Button>();
                btn.targetGraphic = btnImage;

                // TipButton for tooltip (keeps game tooltip behavior)
                TipButton tipButton = buttonObj.AddComponent<TipButton>();
                tipButton.textOnClick = "Building";
                tipButton.textOnClickDescription = "Click to select";

                // Create icon as separate child
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(buttonObj.transform, false);
                Image iconImage = iconObj.AddComponent<Image>();
                iconImage.color = Color.white;

                RectTransform iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = new Vector2(0, 1);
                iconRect.offsetMin = new Vector2(5, 5);
                iconRect.offsetMax = new Vector2(65, -5);

                Debug.Log("[WorldBoxMod] Created building button prefab, assigning to _buttonPrefab");
                // Create text label as separate child
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(buttonObj.transform, false);
                Text btnText = textObj.AddComponent<Text>();
                btnText.text = "Building";
                btnText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                btnText.fontSize = 14;
                btnText.fontStyle = FontStyle.Bold;
                btnText.alignment = TextAnchor.MiddleLeft;
                btnText.color = Color.white;

                RectTransform btnTextRect = textObj.GetComponent<RectTransform>();
                btnTextRect.anchorMin = Vector2.zero;
                btnTextRect.anchorMax = Vector2.one;
                btnTextRect.offsetMin = new Vector2(70, 0);
                btnTextRect.offsetMax = new Vector2(-10, 0);

                // Set button layout
                RectTransform btnRect = buttonObj.GetComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(0, 1);
                btnRect.anchorMax = new Vector2(1, 1);
                btnRect.sizeDelta = new Vector2(0, 70);

                _buttonPrefab = buttonObj;
                Debug.Log("[WorldBoxMod] _buttonPrefab assigned = " + (_buttonPrefab != null));
                buttonObj.SetActive(false);

                // Create close button
                GameObject closeButtonObj = new GameObject("CloseButton");
                closeButtonObj.transform.SetParent(_window.transform, false);

                Button closeButton = closeButtonObj.AddComponent<Button>();
                Image closeImage = closeButtonObj.AddComponent<Image>();
                closeImage.color = new Color(0.8f, 0.2f, 0.2f, 1);
                closeButton.targetGraphic = closeImage;

                // Create text as child to avoid two Graphic components on same GameObject
                GameObject closeTextObj = new GameObject("CloseText", typeof(RectTransform));
                closeTextObj.transform.SetParent(closeButtonObj.transform, false);
                Text closeText = closeTextObj.AddComponent<Text>();
                closeText.text = "✕";
                closeText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                closeText.fontSize = 20;
                closeText.alignment = TextAnchor.MiddleCenter;
                closeText.color = Color.white;

                RectTransform closeTextRect = closeTextObj.GetComponent<RectTransform>();
                closeTextRect.anchorMin = Vector2.zero;
                closeTextRect.anchorMax = Vector2.one;
                closeTextRect.offsetMin = Vector2.zero;
                closeTextRect.offsetMax = Vector2.zero;

                RectTransform closeRect = closeButtonObj.GetComponent<RectTransform>();
                closeRect.anchorMin = new Vector2(1, 1);
                closeRect.anchorMax = new Vector2(1, 1);
                closeRect.sizeDelta = new Vector2(45, 45);
                closeRect.anchoredPosition = new Vector2(-22, -22);

                closeButton.onClick.AddListener(() => CloseWindow());

                Debug.Log("[WorldBoxMod] BuildingSelectionUI window created successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error creating window: {e}\n{e.StackTrace}");
                _window = null;
            }
        }

        /// <summary>
        /// Загрузить спрайт для иконки (используется для кастомных зданий/иконок)
        /// Размещай файлы: C:\mods\WorldBoxMod\icons\[name].png
        /// </summary>
        private Sprite LoadIconSprite(string iconName)
        {
            try
            {
                // Пример: для загрузки Custom иконок
                // string iconPath = System.IO.Path.Combine(
                //     System.IO.Directory.GetCurrentDirectory(), 
                //     "mods", "WorldBoxMod", "icons", $"{iconName}.png");
                // 
                // if (System.IO.File.Exists(iconPath))
                // {
                //     byte[] fileData = System.IO.File.ReadAllBytes(iconPath);
                //     Texture2D tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                //     tex.LoadImage(fileData);
                //     return Sprite.Create(tex, new Rect(0, 0, 64, 64), Vector2.zero);
                // }
                
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Failed to load icon {iconName}: {e}");
                return null;
            }
        }

        private void PopulateBuildings()
        {
            if (_buttonContainer == null)
            {
                Debug.LogError("[WorldBoxMod] _buttonContainer is null!");
                return;
            }

            if (_buttonPrefab == null)
            {
                Debug.LogError("[WorldBoxMod] _buttonPrefab is null!");
                return;
            }

            List<BuildingAsset> buildings = new List<BuildingAsset>();
            
            // Get all buildings
            if (AssetManager.buildings != null && AssetManager.buildings.list != null)
            {
                Debug.Log("[WorldBoxMod] AssetManager.buildings.list found: " + AssetManager.buildings.list.Count);
                
                foreach (BuildingAsset building in AssetManager.buildings.list)
                {
                    if (building != null && !building.id.StartsWith("$"))
                    {
                        buildings.Add(building);
                    }
                }
            }
            else
            {
                Debug.LogError("[WorldBoxMod] AssetManager.buildings or list is NULL!");
                return;
            }

            if (buildings.Count == 0)
            {
                Debug.LogError("[WorldBoxMod] No buildings found after filtering!");
                return;
            }

            Debug.Log("[WorldBoxMod] Creating " + buildings.Count + " building buttons");

            // Create buttons for each building
            foreach (BuildingAsset building in buildings)
            {
                try
                {
                    GameObject buttonObj = Instantiate(_buttonPrefab, _buttonContainer);
                    buttonObj.SetActive(true);

                    // Update TipButton for tooltip
                    TipButton tipButton = buttonObj.GetComponent<TipButton>();
                    if (tipButton != null)
                    {
                        tipButton.textOnClick = building.id;
                        tipButton.textOnClickDescription = "Click to select";
                    }

                    // Update text label
                    Text btnText = buttonObj.transform.Find("Text")?.GetComponent<Text>();
                    if (btnText != null)
                    {
                        btnText.text = building.id;
                    }

                    // Set icon image
                    Image iconImage = buttonObj.transform.Find("Icon")?.GetComponent<Image>();
                    if (iconImage != null)
                    {
                        Sprite buildingIcon = null;
                        if (building.building_sprites != null && building.building_sprites.construction != null)
                        {
                            buildingIcon = building.building_sprites.construction;
                        }

                        if (buildingIcon != null)
                        {
                            iconImage.sprite = buildingIcon;
                        }
                        else
                        {
                            // Use color if no icon
                            float hue = (building.id.GetHashCode() % 360) / 360f;
                            iconImage.color = Color.HSVToRGB(hue, 0.7f, 0.8f);
                        }
                    }

                    // Add click handler
                    Button btn = buttonObj.GetComponent<Button>();
                    if (btn != null)
                    {
                        BuildingAsset buildingRef = building;
                        UnityEngine.Events.UnityAction clickAction = () => SelectBuilding(buildingRef);
                        btn.onClick.AddListener(clickAction);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("[WorldBoxMod] Failed to create button for " + building.id + ": " + e.Message);
                }
            }

            // Adjust content size
            if (_buttonContainer != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_buttonContainer.GetComponent<RectTransform>());
            }
        }

        private void SelectBuilding(BuildingAsset building)
        {
            try
            {
                if (_selectedTile == null)
                {
                    Debug.LogWarning("[WorldBoxMod] Selected tile is null!");
                    return;
                }

                if (building == null)
                {
                    Debug.LogWarning("[WorldBoxMod] Building asset is null!");
                    return;
                }

                // Save last selected building ID for quick placement
                WorldBoxMod._lastSelectedBuildingID = building.id;
                Debug.Log($"[WorldBoxMod] Selected building for quick spawn: {building.id}");
                // Small feedback effect
                try { EffectsLibrary.spawnAtTile("fx_positive_effect", _selectedTile, 0.25f); } catch {}
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error in SelectBuilding: {e}");
            }

            CloseWindow();
        }

        private void ClearButtons()
        {
            if (_buttonContainer == null)
            {
                Debug.LogWarning("[WorldBoxMod] _buttonContainer is null, cannot clear buttons");
                return;
            }
            
            // Destroy all children except prefab
            foreach (Transform child in _buttonContainer)
            {
                if (_buttonPrefab != null && child.gameObject != _buttonPrefab)
                {
                    Destroy(child.gameObject);
                }
                else if (_buttonPrefab == null)
                {
                    // If no prefab set, destroy all children
                    Destroy(child.gameObject);
                }
            }
        }

        private void CloseWindow()
        {
            if (_window != null)
            {
                _window.SetActive(false);
            }
        }
    }
}