using Unity;
using UnityEngine;
using System;
using System.Collections.Generic;
using ai.behaviours;
using ai;

namespace WorldBoxMod
{
    public class Actions
    {
        /// <summary>
        /// GodPower action for force citizenship
        /// </summary>
        public static bool action_force_citizenship(WorldTile pTile, string pPowerID)
        {
            if (pTile == null)
                return false;

            try
            {
                WorldBoxMod.ForceCitizenship(pTile);
                EffectsLibrary.spawnAtTile("fx_positive_effect", pTile, 0.5f);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error in action_force_citizenship: {e}");
                return false;
            }
        }

        public static bool action_force_capital(WorldTile pTile, string pPowerID)
        {
            if (pTile == null)
                return false;

            try
            {
                bool success = WorldBoxMod.ForceCapital(pTile);
                if (success)
                {
                    EffectsLibrary.spawnAtTile("fx_positive_effect", pTile, 1.0f);
                }
                else
                {
                    EffectsLibrary.spawnAtTile("fx_bad_place", pTile, 0.5f);
                }
                return success;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error in action_force_capital: {e}");
                return false;
            }
        }

              /// <summary>
        /// GodPower action for force settle (create city)
        /// </summary>
        public static bool action_force_settle(WorldTile pTile, string pPowerID)
        {
            if (pTile == null)
                return false;

            try
            {
                bool success = WorldBoxMod.ForceSettle(pTile);
                if (success)
                {
                    EffectsLibrary.spawnAtTile("fx_positive_effect", pTile, 1.0f);
                }
                else
                {
                    EffectsLibrary.spawnAtTile("fx_bad_place", pTile, 0.5f);
                }
                return success;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error in action_force_settle: {e}");
                return false;
            }
        }

         /// <summary>
        /// Quick spawn: place last selected building on clicked tile, or open selector if none selected
        /// </summary>
        public static bool action_quick_spawn_building(WorldTile pTile, string pPowerID)
        {
            if (pTile == null)
                return false;

            try
            {
                if (string.IsNullOrEmpty(WorldBoxMod._lastSelectedBuildingID))
                {
                    // No building selected yet - open selector and store this tile as last
                    WorldBoxMod._lastTile = pTile;
                    BuildingSelectionUI.ShowBuildingSelection(pTile);
                    return true;
                }

                bool success = WorldBoxMod.SpawnAnyBuilding(pTile, WorldBoxMod._lastSelectedBuildingID);
                if (success)
                {
                    Debug.Log($"[WorldBoxMod] Quick spawned {WorldBoxMod._lastSelectedBuildingID} at tile");
                    EffectsLibrary.spawnAtTile("fx_positive_effect", pTile, 0.5f);
                }
                else
                {
                    Debug.LogError($"[WorldBoxMod] Quick spawn failed for {WorldBoxMod._lastSelectedBuildingID}");
                    EffectsLibrary.spawnAtTile("fx_bad_place", pTile, 0.25f);
                }
                return success;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error in action_quick_spawn_building: {e}");
                return false;
            }
        }

                /// <summary>
        /// GodPower action for spawning buildings
        /// </summary>
        public static bool action_spawn_building_power(WorldTile pTile, string pPowerID)
        {
            if (pTile == null)
                return false;

            try
            {
                // Show building selection UI to choose a building (does NOT place it)
                WorldBoxMod._lastTile = pTile;
                BuildingSelectionUI.ShowBuildingSelection(pTile);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error in action_spawn_building_power: {e}");
                return false;
            }
        }

        /// <summary>
        /// GodPower action for changing unit to civilian
        /// </summary>
        public static bool action_change_civilian(WorldTile pTile, string pPowerID)
        {
            if (pTile == null)
                return false;

            try
            {
                WorldBoxMod.ChangeUnitToCivilian(pTile);
                EffectsLibrary.spawnAtTile("fx_positive_effect", pTile, 0.5f);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error in action_change_civilian: {e}");
                return false;
            }
        }

        /// <summary>
        /// GodPower action for changing unit to king
        /// </summary>
        public static bool action_change_king(WorldTile pTile, string pPowerID)
        {
            if (pTile == null)
                return false;

            try
            {
                WorldBoxMod.ChangeUnitToKing(pTile);
                EffectsLibrary.spawnAtTile("fx_positive_effect", pTile, 0.75f);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error in action_change_king: {e}");
                return false;
            }
        }

        /// <summary>
        /// GodPower action for changing unit to warrior
        /// </summary>
        public static bool action_change_warrior(WorldTile pTile, string pPowerID)
        {
            if (pTile == null)
                return false;

            try
            {
                WorldBoxMod.ChangeUnitToWarrior(pTile);
                EffectsLibrary.spawnAtTile("fx_positive_effect", pTile, 0.5f);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error in action_change_warrior: {e}");
                return false;
            }
        }

        
    }
}