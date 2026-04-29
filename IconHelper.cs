using System;
using System.IO;
using UnityEngine;

namespace WorldBoxMod
{
    public partial class WorldBoxMod : MonoBehaviour
    {
        /// <summary>
        /// Load icon from embedded resources. If _AreIconsReady is false, load placeholder.png.
        /// Returns null on failure.
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

                string baseName = Path.GetFileNameWithoutExtension(iconFileName);
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
                catch { }

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

        public static Sprite GetIconForPower(string powerId)
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
    }
}
