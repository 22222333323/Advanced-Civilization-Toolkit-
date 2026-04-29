using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorldBoxMod
{
    public partial class WorldBoxMod : MonoBehaviour
    {
        public static bool action_change_warrior(WorldTile pTile, string pPowerID)
        {
            if (pTile == null)
                return false;

            try
            {
                ChangeUnitToWarrior(pTile);
                EffectsLibrary.spawnAtTile("fx_positive_effect", pTile, 0.5f);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error in action_change_warrior: {e}");
                return false;
            }
        }

        public static bool OnWarriorButtonSelect(string pPowerID)
        {
            try
            {
                Debug.Log("[WorldBoxMod] Warrior power selected - click on units to convert");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error in OnWarriorButtonSelect: {e}");
            }
            return false;
        }

        public static bool OnCivilianButtonSelect(string pPowerID)
        {
            try
            {
                Debug.Log("[WorldBoxMod] Civilian power selected - click on units to convert");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error in OnCivilianButtonSelect: {e}");
            }
            return false;
        }

        public static bool OnKingButtonSelect(string pPowerID)
        {
            try
            {
                Debug.Log("[WorldBoxMod] King power selected - click on units to make king");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error in OnKingButtonSelect: {e}");
            }
            return false;
        }

        public static bool OnSpawnBuildingButtonSelect(string pPowerID)
        {
            try
            {
                Debug.Log("[WorldBoxMod] Spawn Building power selected - click on tiles to spawn");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error in OnSpawnBuildingButtonSelect: {e}");
            }
            return false;
        }

        public static bool OnSettleButtonSelect(string pPowerID)
        {
            try
            {
                Debug.Log("[WorldBoxMod] Settle power selected - click to create city");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error in OnSettleButtonSelect: {e}");
            }
            return false;
        }

        public static bool OnCapitalButtonSelect(string pPowerID)
        {
            try
            {
                Debug.Log("[WorldBoxMod] Capital power selected - click on city to make capital");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error in OnCapitalButtonSelect: {e}");
            }
            return false;
        }

        public static bool OnCitizenshipButtonSelect(string pPowerID)
        {
            try
            {
                Debug.Log("[WorldBoxMod] Citizenship power selected - click to convert nearby units");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error in OnCitizenshipButtonSelect: {e}");
            }
            return false;
        }

        public static void RegisterGodPowers()
        {
            try
            {
                PowerLibrary powerLib = AssetManager.powers as PowerLibrary;
                if (powerLib == null)
                {
                    Debug.LogError("[WorldBoxMod] Could not access PowerLibrary!");
                    return;
                }

                Debug.Log("[WorldBoxMod] Starting GodPower registration...");

                List<GodPower> CCTPowers = new List<GodPower>();

                GodPower power_warrior = new GodPower
                {
                    id = "CCT_mod_warrior",
                    name = "CCT_mod_warrior",
                    path_icon = "ui/Icons/culture_military",
                    rank = PowerRank.Rank0_free,
                    type = PowerActionType.PowerSpecial,
                    click_action = new PowerActionWithID(Actions.action_change_warrior),
                    select_button_action = new PowerButtonClickAction(OnWarriorButtonSelect),
                    requires_premium = false,
                    tester_enabled = true,
                    track_activity = true
                };
                powerLib.add(power_warrior);
                CCTPowers.Add(power_warrior);
                Debug.Log("[WorldBoxMod] ✓ Registered power: Change to Warrior (id: " + power_warrior.id + ")");

                GodPower power_civilian = new GodPower
                {
                    id = "CCT_mod_civilian",
                    name = "CCT_mod_civilian",
                    path_icon = "ui/Icons/job_citizen",
                    rank = PowerRank.Rank0_free,
                    type = PowerActionType.PowerSpecial,
                    click_action = new PowerActionWithID(Actions.action_change_civilian),
                    select_button_action = new PowerButtonClickAction(OnCivilianButtonSelect),
                    requires_premium = false,
                    tester_enabled = true,
                    track_activity = true
                };
                powerLib.add(power_civilian);
                CCTPowers.Add(power_civilian);
                Debug.Log("[WorldBoxMod] ✓ Registered power: Change to Civilian (id: " + power_civilian.id + ")");

                GodPower power_king = new GodPower
                {
                    id = "CCT_mod_king",
                    name = "CCT_mod_king",
                    path_icon = "ui/Icons/trait_leader",
                    rank = PowerRank.Rank1_common,
                    type = PowerActionType.PowerSpecial,
                    click_action = new PowerActionWithID(Actions.action_change_king),
                    select_button_action = new PowerButtonClickAction(OnKingButtonSelect),
                    requires_premium = false,
                    tester_enabled = true,
                    track_activity = true
                };
                powerLib.add(power_king);
                CCTPowers.Add(power_king);
                Debug.Log("[WorldBoxMod] ✓ Registered power: Change to King (id: " + power_king.id + ")");

                GodPower power_spawn_building = new GodPower
                {
                    id = "CCT_mod_spawn_building",
                    name = "CCT_mod_spawn_building",
                    path_icon = "ui/Icons/buildings_house",
                    rank = PowerRank.Rank1_common,
                    type = PowerActionType.PowerSpecial,
                    click_action = new PowerActionWithID(Actions.action_spawn_building_power),
                    select_button_action = new PowerButtonClickAction(OnSpawnBuildingButtonSelect),
                    requires_premium = false,
                    tester_enabled = true,
                    track_activity = true
                };
                powerLib.add(power_spawn_building);
                CCTPowers.Add(power_spawn_building);
                Debug.Log("[WorldBoxMod] ✓ Registered power: Select Building (id: " + power_spawn_building.id + ")");

                GodPower power_quick_spawn = new GodPower
                {
                    id = "CCT_mod_spawn_building_quick",
                    name = "CCT_mod_spawn_building_quick",
                    path_icon = "ui/Icons/buildings_house",
                    rank = PowerRank.Rank1_common,
                    type = PowerActionType.PowerSpecial,
                    click_action = new PowerActionWithID(Actions.action_quick_spawn_building),
                    select_button_action = new PowerButtonClickAction(OnSpawnBuildingButtonSelect),
                    requires_premium = false,
                    tester_enabled = true,
                    track_activity = true
                };
                powerLib.add(power_quick_spawn);
                CCTPowers.Add(power_quick_spawn);
                Debug.Log("[WorldBoxMod] ✓ Registered power: Place Building (id: " + power_quick_spawn.id + ")");

                GodPower power_settle = new GodPower
                {
                    id = "CCT_mod_settle",
                    name = "CCT_mod_settle",
                    path_icon = "ui/Icons/city_expand",
                    rank = PowerRank.Rank2_normal,
                    type = PowerActionType.PowerSpecial,
                    click_action = new PowerActionWithID(Actions.action_force_settle),
                    select_button_action = new PowerButtonClickAction(OnSettleButtonSelect),
                    requires_premium = false,
                    tester_enabled = true,
                    track_activity = true
                };
                powerLib.add(power_settle);
                CCTPowers.Add(power_settle);
                Debug.Log("[WorldBoxMod] ✓ Registered power: Force Settle (id: " + power_settle.id + ")");

                GodPower power_capital = new GodPower
                {
                    id = "CCT_mod_capital",
                    name = "CCT_mod_capital",
                    path_icon = "ui/Icons/kingdom_castle",
                    rank = PowerRank.Rank2_normal,
                    type = PowerActionType.PowerSpecial,
                    click_action = new PowerActionWithID(Actions.action_force_capital),
                    select_button_action = new PowerButtonClickAction(OnCapitalButtonSelect),
                    requires_premium = false,
                    tester_enabled = true,
                    track_activity = true
                };
                powerLib.add(power_capital);
                CCTPowers.Add(power_capital);
                Debug.Log("[WorldBoxMod] ✓ Registered power: Force Capital (id: " + power_capital.id + ")");

                GodPower power_citizenship = new GodPower
                {
                    id = "CCT_mod_citizenship",
                    name = "CCT_mod_citizenship",
                    path_icon = "ui/Icons/culture_diplomat",
                    rank = PowerRank.Rank1_common,
                    type = PowerActionType.PowerSpecial,
                    click_action = new PowerActionWithID(Actions.action_force_citizenship),
                    select_button_action = new PowerButtonClickAction(OnCitizenshipButtonSelect),
                    requires_premium = false,
                    tester_enabled = true,
                    track_activity = true
                };
                powerLib.add(power_citizenship);
                CCTPowers.Add(power_citizenship);
                Debug.Log("[WorldBoxMod] ✓ Registered power: Force Citizenship (id: " + power_citizenship.id + ")");

                Debug.Log("[WorldBoxMod] All " + CCTPowers.Count + " Advanced Civilization Toolkit(CCT) Mod powers registered successfully!");

                WorldBoxMod modInstance = FindObjectOfType<WorldBoxMod>();
                try
                {
                    InitModLocalization();
                    string curLang = PlayerConfig.dict.ContainsKey("language") ? PlayerConfig.dict["language"].stringVal : "en";
                    ApplyModLocalization(curLang ?? "en");
                    if (modInstance != null)
                    {
                        modInstance.StartCoroutine(MonitorLanguageChanges());
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[WorldBoxMod] Localization init failed: {e.Message}");
                }

                foreach (GodPower power in CCTPowers)
                {
                    GodPower verify = AssetManager.powers.get(power.id);
                    if (verify != null)
                    {
                        Debug.Log("[WorldBoxMod] ✓ Verified: " + power.id + " is in AssetManager.powers");
                    }
                    else
                    {
                        Debug.LogError("[WorldBoxMod] ✗ FAILED: " + power.id + " NOT found in AssetManager.powers!");
                    }
                }

                if (modInstance != null)
                {
                    modInstance.StartCoroutine(CreatePowerbuttons.CreateUIButtonsDelayed(CCTPowers));
                }
                else
                {
                    Debug.LogError("[WorldBoxMod] Could not find WorldBoxMod instance for coroutine!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error registering GodPowers: {e.Message}\n{e.StackTrace}");
            }
        }
    }
}
