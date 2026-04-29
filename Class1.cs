using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ai.behaviours;
using UnityEngine.UI;
using UnityEngine.EventSystems;


namespace WorldBoxMod
{
    public class WorldBoxMod : MonoBehaviour
    {
        private static MethodInfo _setProfessionMethod;
        private static MethodInfo _addBuildingMethod;
        private static bool _initialized = false;
        private static bool _AreIconsReady = true; //if no icons drawn. When loading should use placeholder.png
        private static WorldTile _lastTile = null;
        public static string _lastSelectedBuildingID = null;
        // Localization storage for this mod: language -> (key -> translation)
        private static Dictionary<string, Dictionary<string, string>> _modTranslations = null;
        private static string _modFileName = "WorldBoxMod_mod"; // used as pseudo-file name in LocalizedTextManager

        void Update()
        {
            if (!_initialized)
            {
                _initialized = true;
                Debug.Log("[WorldBoxMod] Initializing Advanced Civilization Toolkit(CCT) Mod functions...");
                Init();
            }
        }

        void Init()
        {
            InitializeReflection();      
            RegisterGodPowers();
            Debug.Log("[WorldBoxMod] Advanced Civilization Toolkit(CCT) Mod loaded successfully!");
            LocalizationFix.Apply();
        }


        

 

        private static void InitializeReflection()
        {
            if (_setProfessionMethod == null)
            {
                _setProfessionMethod = typeof(Actor).GetMethod("setProfession", 
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null, new Type[] { typeof(UnitProfession), typeof(bool) }, null);
            }

            if (_addBuildingMethod == null)
            {
                _addBuildingMethod = typeof(BuildingManager).GetMethod("addBuilding",
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null, new Type[] { typeof(string), typeof(WorldTile), typeof(bool), typeof(bool), typeof(BuildPlacingType) }, null);
            }
        }

        /// <summary>
        /// Load icon from embedded resources. NOTe: if _AreIconsReady is false, load placeholder.png. if icon not found load placeholder.png and log warning. If error occurs, log error and return null (no icon).
        /// </summary>
        private static Sprite LoadIconFromResources(string iconFileName)
        {
            try
            {
                var assembly = typeof(WorldBoxMod).Assembly;
                if (!_AreIconsReady)
                {
                    iconFileName = "placeholder.png";
                }
                // Try Resources first (Res/icons/<name> without extension)
                string baseName = System.IO.Path.GetFileNameWithoutExtension(iconFileName);
                try
                {
                    Texture2D tex = Resources.Load<Texture2D>("Res/icons/" + baseName);
                    if (tex != null)
                    {
                        tex.name = iconFileName;
                        Sprite spriteRes = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
                        spriteRes.name = iconFileName;
                        Debug.Log($"[WorldBoxMod] ✓ Loaded icon from Resources: {iconFileName}");
                        return spriteRes;
                    }
                }
                catch (Exception) { }

                // Fallback: try embedded manifest resource
                string resourceName = $"WorldBoxMod.icons.{iconFileName}";
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        Debug.LogWarning($"[WorldBoxMod] Icon resource not found: {resourceName}");
                        return null;
                    }

                    byte[] textureData = new byte[stream.Length];
                    stream.Read(textureData, 0, textureData.Length);

                    Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    bool success = UnityEngine.ImageConversion.LoadImage(texture, textureData);
                    if (!success)
                    {
                        Debug.LogWarning($"[WorldBoxMod] Failed to load texture from bytes: {iconFileName}");
                        return null;
                    }
                    texture.name = iconFileName;

                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                    sprite.name = iconFileName;

                    Debug.Log($"[WorldBoxMod] ✓ Loaded icon: {iconFileName}");

                    return sprite;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error loading icon {iconFileName}: {e.Message}");
                return null;
            }
        }

        

        /// <summary>
        /// Add localization and tooltip to GodPower
        /// </summary>
        private static void SetupLocalizationAndTooltip(GodPower power, string displayName, string description)
        {
            if (power == null) return;

            try
            {
                // Don't overwrite power.name (we use id-based keys for localization).
                // Optionally register runtime translations if needed.
                // Keep this method safe in case external callers pass display strings.
                if (string.IsNullOrEmpty(power.name))
                {
                    power.name = displayName;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[WorldBoxMod] Could not set localization for {power.id}: {e.Message}");
            }
        }

        /// <summary>
        /// Get localized text for power
        /// </summary>
        private static string GetLocalizedText(string key)
        {
            try
            {
                return LocalizedTextManager.getText(key);
            }
            catch
            {
                return key;  // Fallback to key if localization fails
            }
        }

        /// <summary>
        /// Get icon for power by ID. if Id is unknown, return placeholder. 
        /// </summary>
        private static Sprite GetIconForPower(string powerId)
        {
            if (powerId == "CCT_mod_warrior") return LoadIconFromResources("changetowarrior.png");
            if (powerId == "CCT_mod_civilian") return LoadIconFromResources("changetocivilain.png");
            if (powerId == "CCT_mod_king") return LoadIconFromResources("changetoking.png");
            if (powerId == "CCT_mod_spawn_building") return LoadIconFromResources("spawnbuilding.png");
            if (powerId == "CCT_mod_spawn_building_quick") return LoadIconFromResources("spawnbuilding.png");
            if (powerId == "CCT_mod_settle") return LoadIconFromResources("ForceSettle.png");
            if (powerId == "CCT_mod_capital") return LoadIconFromResources("ForceCapital.png");
            if (powerId == "CCT_mod_citizenship") return LoadIconFromResources("ForceCitizenship.png");
            return LoadIconFromResources("placeholder.png");
        }

        /// <summary>
        /// Get display name for power
        /// </summary>
        private static string GetPowerDisplayName(string powerId)
        {
            if (powerId == "CCT_mod_warrior") return "Warrior";
            if (powerId == "CCT_mod_civilian") return "Civilian";
            if (powerId == "CCT_mod_king") return "King";
            if (powerId == "CCT_mod_spawn_building") return "Select Building";
            if (powerId == "CCT_mod_spawn_building_quick") return "Place Building";
            if (powerId == "CCT_mod_settle") return "Force Settle";
            if (powerId == "CCT_mod_capital") return "Force Capital";
            if (powerId == "CCT_mod_citizenship") return "Force Citizenship";
            return "Unknown Power";
        }

        /// <summary>
        /// Get description for power
        /// </summary>
        private static string GetPowerDescription(string powerId)
        {
            if (powerId == "CCT_mod_warrior") return "Click on units to turn them into warriors";
            if (powerId == "CCT_mod_civilian") return "Click on units to turn them into civilians";
            if (powerId == "CCT_mod_king") return "Click on units to turn them into kings";
            if (powerId == "CCT_mod_spawn_building") return "Open selector to choose building (doesn't place)";
            if (powerId == "CCT_mod_spawn_building_quick") return "Click tiles to place last selected building";
            if (powerId == "CCT_mod_settle") return "Click on tiles to force settlement";
            if (powerId == "CCT_mod_capital") return "Click on cities to make them capitals";
            if (powerId == "CCT_mod_citizenship") return "Click on units to force citizenship";
            return "No description";
        }

        /// <summary>
        /// Initialize mod translations (in-memory). Adds English and Russian entries.
        /// </summary>
        private static void InitModLocalization()
        {
            if (_modTranslations != null) return;

            _modTranslations = new Dictionary<string, Dictionary<string, string>>();

            // English
            var en = new Dictionary<string, string>();
            en["CCT_mod_warrior".Underscore()] = "Change to Warrior";
            en["CCT_mod_warrior_description".Underscore()] = "Click on units to turn them into warriors";

            en["CCT_mod_civilian".Underscore()] = "Change to Civilian";
            en["CCT_mod_civilian_description".Underscore()] = "Click on units to turn them into civilians";

            en["CCT_mod_king".Underscore()] = "Change to King";
            en["CCT_mod_king_description".Underscore()] = "Click on units to turn them into kings";

            en["CCT_mod_spawn_building".Underscore()] = "Select Building";
            en["CCT_mod_spawn_building_description".Underscore()] = "Open selector to choose building (doesn't place)";

            en["CCT_mod_spawn_building_quick".Underscore()] = "Place Building";
            en["CCT_mod_spawn_building_quick_description".Underscore()] = "Click tiles to place last selected building";

            en["CCT_mod_settle".Underscore()] = "Force Settle";
            en["CCT_mod_settle_description".Underscore()] = "Click on tiles to force settlement";

            en["CCT_mod_capital".Underscore()] = "Force Capital";
            en["CCT_mod_capital_description".Underscore()] = "Click on cities to make them capitals";

            en["CCT_mod_citizenship".Underscore()] = "Force Citizenship";
            en["CCT_mod_citizenship_description".Underscore()] = "Click on units to force citizenship";

            _modTranslations["en"] = en;

            // Russian
            var ru = new Dictionary<string, string>();
            ru["CCT_mod_warrior".Underscore()] = "Сделать воином";
            ru["CCT_mod_warrior_description".Underscore()] = "Кликните по юниту, чтобы сделать его воином";

            ru["CCT_mod_civilian".Underscore()] = "Сделать гражданином";
            ru["CCT_mod_civilian_description".Underscore()] = "Кликните по юниту, чтобы сделать его гражданином";

            ru["CCT_mod_king".Underscore()] = "Сделать королём";
            ru["CCT_mod_king_description".Underscore()] = "Кликните по юниту, чтобы сделать его королём";

            ru["CCT_mod_spawn_building".Underscore()] = "Выбрать здание";
            ru["CCT_mod_spawn_building_description".Underscore()] = "Откройте селектор, чтобы выбрать здание (не размещает)";

            ru["CCT_mod_spawn_building_quick".Underscore()] = "Разместить здание";
            ru["CCT_mod_spawn_building_quick_description".Underscore()] = "Кликните по тайлу, чтобы разместить последнее выбранное здание";

            ru["CCT_mod_settle".Underscore()] = "Принудительное поселение";
            ru["CCT_mod_settle_description".Underscore()] = "Кликните по тайлу, чтобы принудительно основать город";

            ru["CCT_mod_capital".Underscore()] = "Сделать столицей";
            ru["CCT_mod_capital_description".Underscore()] = "Кликните по городу, чтобы сделать его столицей";

            ru["CCT_mod_citizenship".Underscore()] = "Принудительное гражданство";
            ru["CCT_mod_citizenship_description".Underscore()] = "Кликните по юниту, чтобы присоединить его к городу";

            _modTranslations["ru"] = ru;
        }

        private static void ApplyModLocalization(string pLanguage)
        {
            try
            {
                if (LocalizedTextManager.instance == null) LocalizedTextManager.init();
                if (_modTranslations == null) InitModLocalization();

                string lang = pLanguage ?? (PlayerConfig.dict.ContainsKey("language") ? PlayerConfig.dict["language"].stringVal : "en") ?? "en";
                if (!_modTranslations.ContainsKey(lang)) lang = "en";

                var dict = _modTranslations[lang];
                foreach (var kv in dict)
                {
                    // Add with replace = true so switching languages updates entries
                    LocalizedTextManager.add(kv.Key, kv.Value, pReplace: true, pFileName: _modFileName, pCheckForCharacters: false);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[WorldBoxMod] ApplyModLocalization failed: {e.Message}");
            }
        }

        private static System.Collections.IEnumerator MonitorLanguageChanges()
        {
            string last = PlayerConfig.dict.ContainsKey("language") ? PlayerConfig.dict["language"].stringVal : "";
            while (true)
            {
                yield return new WaitForSeconds(0.5f);
                try
                {
                    string cur = PlayerConfig.dict.ContainsKey("language") ? PlayerConfig.dict["language"].stringVal : "";
                    if (cur != last)
                    {
                        last = cur;
                        ApplyModLocalization(cur);
                        LocalizedTextManager.updateTexts();
                        Debug.Log($"[WorldBoxMod] Reapplied mod translations for: {cur}");
                    }
                }
                catch {}
            }
        }

        /// <summary>
        /// Get all actors in tiles around target tile
        /// </summary>
        private static void GetActorsAroundTile(WorldTile centerTile, int radius, List<Actor> resultList)
        {
            if (centerTile == null || resultList == null)
                return;

            resultList.Clear();

            // Get all tiles in range
            List<WorldTile> tilesToCheck = new List<WorldTile>();
            tilesToCheck.Add(centerTile);

            // Add neighboring tiles iteratively
            for (int r = 0; r < radius; r++)
            {
                List<WorldTile> nextTiles = new List<WorldTile>();
                foreach (WorldTile tile in tilesToCheck)
                {
                    if (tile.neighboursAll != null)
                    {
                        foreach (WorldTile neighbor in tile.neighboursAll)
                        {
                            if (neighbor != null && !tilesToCheck.Contains(neighbor) && !nextTiles.Contains(neighbor))
                            {
                                nextTiles.Add(neighbor);
                            }
                        }
                    }
                }
                tilesToCheck.AddRange(nextTiles);
            }

            // Collect all actors from these tiles
            foreach (WorldTile tile in tilesToCheck)
            {
                tile.doUnits(actor => resultList.Add(actor));
            }
        }

        // ==================== CHANGING UNIT JOBS ====================
        /// <summary>
        /// Changes unit profession to Warrior
        /// </summary>
        public static void ChangeUnitToWarrior(WorldTile pTile)
        {
            if (pTile == null)
                return;

            InitializeReflection();

            // Target only the first actor on the clicked tile
            Actor target = null;
            pTile.doUnits(a =>
            {
                if (target == null && a.asset != null)
                    target = a;
                return true;
            });

            if (target == null || target.city == null)
                return;

            if (target.isBaby())
            {
                target.removeTrait("peaceful");
            }

            if (target.is_profession_warrior)
                return;

            _setProfessionMethod?.Invoke(target, new object[] { UnitProfession.Warrior, true });

            if (target.equipment != null && target.equipment.weapon.isEmpty())
            {
                City.giveItem(target, target.city.getEquipmentList(EquipmentType.Weapon), target.city);
            }

            if (target.city.getArmy() == null && target.city.army == null)
            {
                Army army = MapBox.instance.armies.newArmy(target, target.city);
                target.city.army = army;
            }

            target.city.status.warriors_current++;
            target.setStatsDirty();
            target.startShake(0.3f, 0.1f, true, true);
            target.startColorEffect(ActorColorEffect.White);
        }

        /// <summary>
        /// Changes unit profession to Civilian
        /// </summary>
        public static void ChangeUnitToCivilian(WorldTile pTile)
        {
            if (pTile == null)
                return;

            InitializeReflection();

            Actor target = null;
            pTile.doUnits(a =>
            {
                if (target == null && a.asset != null)
                    target = a;
                return true;
            });

            if (target == null || target.city == null || target.isBaby())
                return;

            if (target.is_profession_warrior && target.city != null)
            {
                if (target.army != null)
                {
                    FieldInfo unitsField = typeof(Army).BaseType.GetField("units", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (unitsField != null)
                    {
                        List<Actor> units = unitsField.GetValue(target.army) as List<Actor>;
                        units?.Remove(target);
                    }
                    target.army = null;
                }
                target.city.status.warriors_current--;
            }

            if (target.is_profession_leader && target.city != null)
            {
                target.city.removeLeader();
            }

            _setProfessionMethod?.Invoke(target, new object[] { UnitProfession.Unit, true });
            target.setStatsDirty();
            target.startShake(0.3f, 0.1f, true, true);
            target.startColorEffect(ActorColorEffect.White);
        }

        /// <summary>
        /// Changes unit profession to King
        /// </summary>
        public static void ChangeUnitToKing(WorldTile pTile)
        {
            if (pTile == null)
                return;

            InitializeReflection();

            Actor target = null;
            pTile.doUnits(a =>
            {
                if (target == null && a.asset != null)
                    target = a;
                return true;
            });

            if (target == null || target.kingdom == null || target.city == null || target.isBaby())
                return;

            if (target.is_profession_leader && target.city != null)
            {
                target.city.removeLeader();
            }

            if (target.army != null)
            {
                FieldInfo unitsField = typeof(Army).BaseType.GetField("units", BindingFlags.NonPublic | BindingFlags.Instance);
                if (unitsField != null)
                {
                    List<Actor> units = unitsField.GetValue(target.army) as List<Actor>;
                    units?.Remove(target);
                }
                target.army = null;
            }

            if (target.kingdom.king != null)
            {
                target.kingdom.king = null;
            }

            target.kingdom.setKing(target);

            if (target.equipment != null && target.equipment.weapon.isEmpty())
            {
                City.giveItem(target, target.city.getEquipmentList(EquipmentType.Weapon), target.city);
            }

            target.setStatsDirty();
            target.startShake(0.3f, 0.1f, true, true);
            target.startColorEffect(ActorColorEffect.White);
        }

        // ==================== SPAWN BUILDINGS ====================
        /// <summary>
        /// Spawn any building type at tile
        /// </summary>
        public static bool SpawnAnyBuilding(WorldTile pTile, string buildingID)
        {
            if (pTile == null || string.IsNullOrEmpty(buildingID))
                return false;

            InitializeReflection();

            // Use reflection to call internal addBuilding method
            Building newBuilding = _addBuildingMethod?.Invoke(MapBox.instance.buildings, 
                new object[] { buildingID, pTile, false, false, BuildPlacingType.New }) as Building;

            if (newBuilding == null)
            {
                EffectsLibrary.spawnAtTile("fx_bad_place", pTile, 0.25f);
                return false;
            }

            // Add to city if building is in a city
            if (pTile.zone.city != null)
            {
                pTile.zone.city.listBuilding(newBuilding);
            }

            return true;
        }

        // ==================== FORCE SETTLE (CREATE CITIES) ====================
        /// <summary>
        /// Force settle - create new city at tile with proper inhabitants
        /// </summary>
        public static bool ForceSettle(WorldTile pTile)
        {
            if (pTile == null || pTile.zone == null) {
                Debug.Log("[WorldBoxMod] ForceSettle failed: tile or zone is null");
                return false;
            }
            try
            {

                Actor actor = null;
                TileZone zone = pTile.zone;

                pTile.doUnits(a =>
                {
                    if (actor == null && a.asset != null)
                        actor = a;
                   
                    return true;
                });

                if (actor == null)
                {
                    Debug.LogWarning("[WorldBoxMod] No unit on tile or nearby to create city from!");
                    return false;
                }
                if (actor.kingdom == null)
                {
                    return false;
                }

                // Use game's city building method (buildNewCity handles validation internally)
                City newCity = World.world.cities.buildNewCity(actor, zone);
                actor.joinCity(newCity);
                if (newCity == null)
                {
                    Debug.LogError("[WorldBoxMod] Failed to build new city - buildNewCity returned null");
                    return false;
                }

                Debug.Log($"[WorldBoxMod] ✓ Created city: {newCity.name} at zone");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] ForceSettle error: {e.Message}");
                return false;
            }
        }

        // ==================== FORCE CAPITAL ====================
        /// <summary>
        /// Force a city to become the capital of its kingdom
        /// </summary>
        public static bool ForceCapital(WorldTile pTile)
        {
            if (pTile == null || pTile.zone == null)
                return false;

            City city = pTile.zone.city;
            if (city == null)
            {
                Debug.Log("[WorldBoxMod] No city at this tile!");
                return false;
            }

            // Get kingdom - it's an internal field but we can access it through reflection or try to find it
            // City has a reference through leadership/armies
            if (city.army != null && city.army.getCaptain() != null && city.army.getCaptain().kingdom != null)
            {
                Kingdom kingdom = city.army.getCaptain().kingdom;
                kingdom.setCapital(city);
                Debug.Log($"[WorldBoxMod] {city.name} is now the capital of {kingdom.name}");
                return true;
            }

            // Try through leader
            if (city.leader != null && city.leader.kingdom != null)
            {
                Kingdom kingdom = city.leader.kingdom;
                kingdom.setCapital(city);
                Debug.Log($"[WorldBoxMod] {city.name} is now the capital of {kingdom.name}");
                return true;
            }

            Debug.Log("[WorldBoxMod] Could not find kingdom for city!");
            return false;
        }

        // ==================== FORCE CITIZENSHIP ====================
        /// <summary>
        /// Force all units in area to join city they stand on
        /// </summary>
        public static void ForceCitizenship(WorldTile pTile)
        {
            if (pTile == null || pTile.zone == null)
                return;

            City newCity = pTile.zone.city;
            if (newCity == null)
            {
                Debug.Log("[WorldBoxMod] No city at this tile!");
                return;
            }

            List<Actor> actors = new List<Actor>();
            GetActorsAroundTile(pTile, 3, actors);

            foreach (Actor pActor in actors)
            {
                if (pActor.asset == null)
                    continue;

                // Make actor citizen of city
                pActor.joinCity(newCity);
                Debug.Log($"[WorldBoxMod] {pActor.getName()} has joined {newCity.name}");
            }
        }

        // ==================== GOD POWER ACTIONS ====================
        /// <summary>
        /// GodPower action for changing unit to warrior
        /// </summary>
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

        /// <summary>
        /// Button select action for Warrior power - enables tile selection mode
        /// </summary>
        public static bool OnWarriorButtonSelect(string pPowerID)
        {
            try
            {
                Debug.Log("[WorldBoxMod] Warrior power selected - click on units to convert");
                return false;  // Return false to let PowerButtonSelector set the power normally
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error in OnWarriorButtonSelect: {e}");
            }
            return false;
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
                ChangeUnitToCivilian(pTile);
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
        /// Button select action for Civilian power - enables tile selection mode
        /// </summary>
        public static bool OnCivilianButtonSelect(string pPowerID)
        {
            try
            {
                Debug.Log("[WorldBoxMod] Civilian power selected - click on units to convert");
                return false;  // Return false to let PowerButtonSelector set the power normally
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error in OnCivilianButtonSelect: {e}");
            }
            return false;
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
                ChangeUnitToKing(pTile);
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
        /// Button select action for King power - enables tile selection mode
        /// </summary>
        public static bool OnKingButtonSelect(string pPowerID)
        {
            try
            {
                Debug.Log("[WorldBoxMod] King power selected - click on units to make king");
                return false;  // Return false to let PowerButtonSelector set the power normally
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error in OnKingButtonSelect: {e}");
            }
            return false;
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
                _lastTile = pTile;
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
        /// Quick spawn: place last selected building on clicked tile, or open selector if none selected
        /// </summary>
        public static bool action_quick_spawn_building(WorldTile pTile, string pPowerID)
        {
            if (pTile == null)
                return false;

            try
            {
                if (string.IsNullOrEmpty(_lastSelectedBuildingID))
                {
                    // No building selected yet - open selector and store this tile as last
                    _lastTile = pTile;
                    BuildingSelectionUI.ShowBuildingSelection(pTile);
                    return true;
                }

                bool success = SpawnAnyBuilding(pTile, _lastSelectedBuildingID);
                if (success)
                {
                    Debug.Log($"[WorldBoxMod] Quick spawned {_lastSelectedBuildingID} at tile");
                    EffectsLibrary.spawnAtTile("fx_positive_effect", pTile, 0.5f);
                }
                else
                {
                    Debug.LogError($"[WorldBoxMod] Quick spawn failed for {_lastSelectedBuildingID}");
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
        /// Button select action for Spawn Building power - shows UI without tile selection
        /// </summary>
        public static bool OnSpawnBuildingButtonSelect(string pPowerID)
        {
            try
            {
                Debug.Log("[WorldBoxMod] Spawn Building power selected - click on tiles to spawn");
                return false;  // Return false to let PowerButtonSelector set the power normally
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error in OnSpawnBuildingButtonSelect: {e}");
            }
            return false;
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
                bool success = ForceSettle(pTile);
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
        /// Button select action for Settle power
        /// </summary>
        public static bool OnSettleButtonSelect(string pPowerID)
        {
            try
            {
                Debug.Log("[WorldBoxMod] Settle power selected - click to create city");
                return false;  // Return false to let PowerButtonSelector set the power normally
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error in OnSettleButtonSelect: {e}");
            }
            return false;
        }

        /// <summary>
        /// GodPower action for force capital
        /// </summary>
        public static bool action_force_capital(WorldTile pTile, string pPowerID)
        {
            if (pTile == null)
                return false;

            try
            {
                bool success = ForceCapital(pTile);
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
        /// Button select action for Capital power
        /// </summary>
        public static bool OnCapitalButtonSelect(string pPowerID)
        {
            try
            {
                Debug.Log("[WorldBoxMod] Capital power selected - click on city to make capital");
                return false;  // Return false to let PowerButtonSelector set the power normally
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error in OnCapitalButtonSelect: {e}");
            }
            return false;
        }

        /// <summary>
        /// GodPower action for force citizenship
        /// </summary>
        public static bool action_force_citizenship(WorldTile pTile, string pPowerID)
        {
            if (pTile == null)
                return false;

            try
            {
                ForceCitizenship(pTile);
                EffectsLibrary.spawnAtTile("fx_positive_effect", pTile, 0.5f);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error in action_force_citizenship: {e}");
                return false;
            }
        }

        /// <summary>
        /// Button select action for Citizenship power
        /// </summary>
        public static bool OnCitizenshipButtonSelect(string pPowerID)
        {
            try
            {
                Debug.Log("[WorldBoxMod] Citizenship power selected - click to convert nearby units");
                return false;  // Return false to let PowerButtonSelector set the power normally
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] Error in OnCitizenshipButtonSelect: {e}");
            }
            return false;
        }

        // ==================== GODPOWER REGISTRATION ====================
        /// <summary>
        /// Register all Advanced Civilization Toolkit(CCT) mod powers with the game
        /// </summary>
        private static void RegisterGodPowers()
        {
            try
            {
                // Get PowerLibrary instance
                PowerLibrary powerLib = AssetManager.powers as PowerLibrary;
                if (powerLib == null)
                {
                    Debug.LogError("[WorldBoxMod] Could not access PowerLibrary!");
                    return;
                }

                Debug.Log("[WorldBoxMod] Starting GodPower registration...");

                // Create GodPower assets (these will be stored in the library)
                List<GodPower> CCTPowers = new List<GodPower>();

                // Create and add Change to Warrior power
                GodPower power_warrior = new GodPower
                {
                    id = "CCT_mod_warrior",
                    name = "CCT_mod_warrior",
                    path_icon = "ui/Icons/culture_military",
                    rank = PowerRank.Rank0_free,
                    type = PowerActionType.PowerSpecial,
                    click_action = new PowerActionWithID(action_change_warrior),
                    select_button_action = new PowerButtonClickAction(OnWarriorButtonSelect),
                    requires_premium = false,
                    tester_enabled = true,
                    track_activity = true
                };
                powerLib.add(power_warrior);
                CCTPowers.Add(power_warrior);
                Debug.Log("[WorldBoxMod] ✓ Registered power: Change to Warrior (id: " + power_warrior.id + ")");

                // Create and add Change to Civilian power
                GodPower power_civilian = new GodPower
                {
                    id = "CCT_mod_civilian",
                    name = "CCT_mod_civilian",
                    path_icon = "ui/Icons/job_citizen",
                    rank = PowerRank.Rank0_free,
                    type = PowerActionType.PowerSpecial,
                    click_action = new PowerActionWithID(action_change_civilian),
                    select_button_action = new PowerButtonClickAction(OnCivilianButtonSelect),
                    requires_premium = false,
                    tester_enabled = true,
                    track_activity = true
                };
                powerLib.add(power_civilian);
                CCTPowers.Add(power_civilian);
                Debug.Log("[WorldBoxMod] ✓ Registered power: Change to Civilian (id: " + power_civilian.id + ")");

                // Create and add Change to King power
                GodPower power_king = new GodPower
                {
                    id = "CCT_mod_king",
                    name = "CCT_mod_king",
                    path_icon = "ui/Icons/trait_leader",
                    rank = PowerRank.Rank1_common,
                    type = PowerActionType.PowerSpecial,
                    click_action = new PowerActionWithID(action_change_king),
                    select_button_action = new PowerButtonClickAction(OnKingButtonSelect),
                    requires_premium = false,
                    tester_enabled = true,
                    track_activity = true
                };
                powerLib.add(power_king);
                CCTPowers.Add(power_king);
                Debug.Log("[WorldBoxMod] ✓ Registered power: Change to King (id: " + power_king.id + ")");

                // Create and add Select Building power (opens selector)
                GodPower power_spawn_building = new GodPower
                {
                    id = "CCT_mod_spawn_building",
                    name = "CCT_mod_spawn_building",
                    path_icon = "ui/Icons/buildings_house",
                    rank = PowerRank.Rank1_common,
                    type = PowerActionType.PowerSpecial,
                    click_action = new PowerActionWithID(action_spawn_building_power),
                    select_button_action = new PowerButtonClickAction(OnSpawnBuildingButtonSelect),
                    requires_premium = false,
                    tester_enabled = true,
                    track_activity = true
                };
                powerLib.add(power_spawn_building);
                CCTPowers.Add(power_spawn_building);
                Debug.Log("[WorldBoxMod] ✓ Registered power: Select Building (id: " + power_spawn_building.id + ")");

                // Create and add Place Building (quick spawn) power
                GodPower power_quick_spawn = new GodPower
                {
                    id = "CCT_mod_spawn_building_quick",
                    name = "CCT_mod_spawn_building_quick",
                    path_icon = "ui/Icons/buildings_house",
                    rank = PowerRank.Rank1_common,
                    type = PowerActionType.PowerSpecial,
                    click_action = new PowerActionWithID(action_quick_spawn_building),
                    select_button_action = new PowerButtonClickAction(OnSpawnBuildingButtonSelect),
                    requires_premium = false,
                    tester_enabled = true,
                    track_activity = true
                };
                powerLib.add(power_quick_spawn);
                CCTPowers.Add(power_quick_spawn);
                Debug.Log("[WorldBoxMod] ✓ Registered power: Place Building (id: " + power_quick_spawn.id + ")");

                // Create and add Force Settle power
                GodPower power_settle = new GodPower
                {
                    id = "CCT_mod_settle",
                    name = "CCT_mod_settle",
                    path_icon = "ui/Icons/city_expand",
                    rank = PowerRank.Rank2_normal,
                    type = PowerActionType.PowerSpecial,
                    click_action = new PowerActionWithID(action_force_settle),
                    select_button_action = new PowerButtonClickAction(OnSettleButtonSelect),
                    requires_premium = false,
                    tester_enabled = true,
                    track_activity = true
                };
                powerLib.add(power_settle);
                CCTPowers.Add(power_settle);
                Debug.Log("[WorldBoxMod] ✓ Registered power: Force Settle (id: " + power_settle.id + ")");

                // Create and add Force Capital power
                GodPower power_capital = new GodPower
                {
                    id = "CCT_mod_capital",
                    name = "CCT_mod_capital",
                    path_icon = "ui/Icons/kingdom_castle",
                    rank = PowerRank.Rank2_normal,
                    type = PowerActionType.PowerSpecial,
                    click_action = new PowerActionWithID(action_force_capital),
                    select_button_action = new PowerButtonClickAction(OnCapitalButtonSelect),
                    requires_premium = false,
                    tester_enabled = true,
                    track_activity = true
                };
                powerLib.add(power_capital);
                CCTPowers.Add(power_capital);
                Debug.Log("[WorldBoxMod] ✓ Registered power: Force Capital (id: " + power_capital.id + ")");

                // Create and add Force Citizenship power
                GodPower power_citizenship = new GodPower
                {
                    id = "CCT_mod_citizenship",
                    name = "CCT_mod_citizenship",
                    path_icon = "ui/Icons/culture_diplomat",
                    rank = PowerRank.Rank1_common,
                    type = PowerActionType.PowerSpecial,
                    click_action = new PowerActionWithID(action_force_citizenship),
                    select_button_action = new PowerButtonClickAction(OnCitizenshipButtonSelect),
                    requires_premium = false,
                    tester_enabled = true,
                    track_activity = true
                };
                powerLib.add(power_citizenship);
                CCTPowers.Add(power_citizenship);
                Debug.Log("[WorldBoxMod] ✓ Registered power: Force Citizenship (id: " + power_citizenship.id + ")");

                Debug.Log("[WorldBoxMod] All " + CCTPowers.Count + " Advanced Civilization Toolkit(CCT) Mod powers registered successfully!");
                // Initialize and apply mod-localizations (in-memory). Will also monitor language changes.
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
                
                // Verify all powers are in library
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

                // Create UI buttons after short delay to allow PowersTab initialization
                if (modInstance != null)
                {
                    modInstance.StartCoroutine(CreateUIButtonsDelayed(CCTPowers));
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

        /// <summary>
        /// Create PowerButton UI elements after scene initialization
        /// </summary>
        private static System.Collections.IEnumerator CreateUIButtonsDelayed(List<GodPower> powers)
        {
            yield return new WaitForSeconds(0.3f);

            try
            {
                // Find the main PowersTab in the game UI
                PowersTab mainPowersTab = FindObjectOfType<PowersTab>();
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
        private static void CreateModPowerButton(GodPower power, Transform parentTransform)
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
                        var libField = typeof(PowerLibrary).GetField("lib", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
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
                    Sprite iconSprite = GetIconForPower(power.id);
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
                    UnityEngine.Events.UnityAction clickAction = () =>
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
                var godPowerField = typeof(PowerButton).GetField("godPower", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
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
                            var addPowerMethod = typeof(GodPower).GetMethod("addPower", 
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
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


    }

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
