using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorldBoxMod
{
    public partial class WorldBoxMod : MonoBehaviour
    {
        // Localization storage for this mod: language -> (key -> translation)
        private static Dictionary<string, Dictionary<string, string>> _modTranslations = null;
        private static string _modFileName = "WorldBoxMod_mod"; // used as pseudo-file name in LocalizedTextManager

        private static void SetupLocalizationAndTooltip(GodPower power, string displayName, string description)
        {
            if (power == null) return;

            try
            {
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

        private static string GetLocalizedText(string key)
        {
            try
            {
                return LocalizedTextManager.getText(key);
            }
            catch
            {
                return key;
            }
        }

        public static void InitModLocalization()
        {
            if (_modTranslations != null) return;

            _modTranslations = new Dictionary<string, Dictionary<string, string>>();

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
                catch { }
            }
        }
    }
}
