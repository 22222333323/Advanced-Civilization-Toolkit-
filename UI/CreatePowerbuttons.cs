using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace WorldBoxMod
{
    public class CreatePowerbuttons
    {
        public static IEnumerator CreateUIButtonsDelayed(List<GodPower> powers)
        {
            yield return new WaitForSeconds(0.3f);

            try
            {
                // Find the main PowersTab in the game UI
                PowersTab mainPowersTab = GameObject.FindObjectOfType<PowersTab>();
                if (mainPowersTab == null)
                {
                    Debug.LogError("[WorldBoxMod] PowersTab not found - buttons could not be created");
                    yield break;
                }

                Debug.Log("[WorldBoxMod] PowersTab found, creating " + powers.Count + " buttons...");

                Transform parentTransform = mainPowersTab.transform;

                // Create a PowerButton for each mod power
                int successCount = 0;
                foreach (GodPower power in powers)
                {
                    try
                    {
                        CreateModPowerButton(power, parentTransform);
                        successCount++;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[WorldBoxMod] Failed to create button for {power.id}: {e}");
                    }
                }

                Debug.Log($"[WorldBoxMod] Successfully created {successCount}/{powers.Count} power buttons");
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error creating power buttons: {e}");
            }
        }

        /// <summary>
        /// Create a single PowerButton GameObject for mod power
        /// </summary>
        public static void CreateModPowerButton(GodPower power, Transform parentTransform)
        {
            try
            {
                Debug.Log($"[WorldBoxMod] Creating PowerButton for: {power.id}");
                
                // Verify power exists in library BEFORE creating button
                GodPower verifyPower = AssetManager.powers.get(power.id);
                if (verifyPower == null)
                {
                    Debug.LogError($"[WorldBoxMod] ERROR: GodPower '{power.id}' not found in AssetManager.powers!");
                    // Use reflection to get available powers for debugging
                    try
                    {
                        var libField = typeof(PowerLibrary).GetField("lib", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (libField != null)
                        {
                            var libList = libField.GetValue(AssetManager.powers) as System.Collections.IEnumerable;
                            if (libList != null)
                            {
                                var powerIds = new System.Collections.Generic.List<string>();
                                foreach (var p in libList)
                                {
                                    var idProp = p.GetType().GetProperty("id");
                                    if (idProp != null)
                                        powerIds.Add((string)idProp.GetValue(p));
                                }
                                Debug.LogError($"[WorldBoxMod] Available powers: {string.Join(", ", powerIds)}");
                            }
                        }
                    }
                    catch { }
                    return;
                }
                Debug.Log($"[WorldBoxMod] ✓ Verified power exists in library: {power.id}");

                // Create root GameObject with exact name matching GodPower.id
                GameObject buttonObj = new GameObject(power.id);
                Debug.Log($"[WorldBoxMod] Created GameObject with name: '{buttonObj.name}'");
                
                buttonObj.transform.SetParent(parentTransform, worldPositionStays: false);

                // Add RectTransform
                RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(32, 32);

                // Add Image (background)
                Image bgImage = buttonObj.AddComponent<Image>();
                bgImage.color = Color.white;

                // Add Button component
                Button buttonComponent = buttonObj.AddComponent<Button>();

                // Add LayoutElement for proper sizing in layout
                LayoutElement layoutElement = buttonObj.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = 32;
                layoutElement.preferredHeight = 32;
                layoutElement.flexibleWidth = 0;
                layoutElement.flexibleHeight = 0;

                // Create Icon child GameObject
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(buttonObj.transform, worldPositionStays: false);

                RectTransform iconRect = iconObj.AddComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;

                Image iconImage = iconObj.AddComponent<Image>();
                iconImage.raycastTarget = false;

                // Load icon sprite from embedded resources
                try
                {
                    Sprite iconSprite = WorldBoxMod.GetIconForPower(power.id);
                    if (iconSprite != null)
                    {
                        iconImage.sprite = iconSprite;
                        Debug.Log($"[WorldBoxMod] ✓ Set icon for {power.id}");
                    }
                    else
                    {
                        // Fallback to game icon if embedded resource not found
                        Sprite fallbackSprite = SpriteTextureLoader.getSprite(power.path_icon);
                        if (fallbackSprite != null)
                        {
                            iconImage.sprite = fallbackSprite;
                            Debug.LogWarning($"[WorldBoxMod] Using fallback icon for {power.id}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[WorldBoxMod] Could not load icon for {power.id}: {e.Message}");
                }

                // Add Button tooltip following game UI patterns
                try
                {
                    TipButton tipButton = buttonObj.GetComponent<TipButton>();
                    if (tipButton == null)
                    {
                        tipButton = buttonObj.AddComponent<TipButton>();
                    }
                    
                    // Use game's localization system (we register translations at runtime)
                    string displayName = verifyPower.getTranslatedName();
                    string description = verifyPower.getTranslatedDescription();
                    // Set tooltip text like the game does
                    tipButton.textOnClick = displayName;
                    tipButton.textOnClickDescription = description;
                    tipButton.text_description_2 = "";
                    
                    // Add click listener
                    UnityAction clickAction = () =>
                    {
                        Debug.Log($"[WorldBoxMod] {displayName} power clicked!");
                    };
                    buttonComponent.onClick.AddListener(clickAction);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[WorldBoxMod] Could not add tooltip for {power.id}: {e.Message}");
                }

                // Add PowerButton component BEFORE SetActive
                PowerButton powerButton = buttonObj.AddComponent<PowerButton>();
                powerButton.type = PowerButtonType.Active;
                powerButton.icon = iconImage;  // Set icon BEFORE activation
                powerButton.drag_power_bar = false;
                powerButton.open_window_id = "";
                powerButton.block_same_window = false;

                Debug.Log($"[WorldBoxMod] PowerButton component added, before SetActive");

                // SetActive triggers OnEnable() -> init() which tries to link godPower
                buttonObj.SetActive(true);
                
                Debug.Log($"[WorldBoxMod] SetActive(true) called, init() should have run");
                
                // Manually set godPower via reflection because init() lookup fails
                var godPowerField = typeof(PowerButton).GetField("godPower", BindingFlags.NonPublic | BindingFlags.Instance);
                if (godPowerField != null)
                {
                    // Verify it's null first
                    var currentGodPower = godPowerField.GetValue(powerButton) as GodPower;
                    if (currentGodPower == null)
                    {
                        Debug.LogWarning($"[WorldBoxMod] init() didn't find godPower, setting manually...");
                        godPowerField.SetValue(powerButton, verifyPower);
                        Debug.Log($"[WorldBoxMod] ✓ Manually set godPower to: {verifyPower.id}");
                        
                        // Also need to manually call GodPower.addPower via reflection since it's internal
                        try
                        {
                            var addPowerMethod = typeof(GodPower).GetMethod("addPower", BindingFlags.NonPublic | BindingFlags.Static);
                            if (addPowerMethod != null)
                            {
                                addPowerMethod.Invoke(null, new object[] { verifyPower, powerButton });
                                Debug.Log($"[WorldBoxMod] ✓ Called GodPower.addPower() via reflection");
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"[WorldBoxMod] Could not call GodPower.addPower: {e.Message}");
                        }
                    }
                    else
                    {
                        Debug.Log($"[WorldBoxMod] ✓ init() successfully linked godPower: {currentGodPower.id}");
                    }
                }
                
                Debug.Log($"[WorldBoxMod] ✓ Created PowerButton: {power.id}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error creating PowerButton for {power.id}: {e}\n{e.StackTrace}");
            }
        }

        // Helper to load embedded icon; stubbed to return null if not implemented
        

    }

}
