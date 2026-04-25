using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace PvZEcologyTranslator.Managers
{
    public static class TextureManager
    {
        public static bool EnableImageTranslation = true;
        public static bool EnableTextureDumper = false;

        public static Dictionary<string, Texture2D> CustomTextures = new Dictionary<string, Texture2D>();
        public static Dictionary<string, Sprite> CachedTranslatedSprites = new Dictionary<string, Sprite>();

        private static HashSet<string> DumpedSprites = new HashSet<string>();

        public static void LoadCustomSprites()
        {
            Stopwatch sw = Stopwatch.StartNew();

            CustomTextures.Clear();
            CachedTranslatedSprites.Clear();

            string langFolder = PvZEcologyTranslator.Features.LanguageMenu.CurrentLanguage;
            string texturesPath = EnableImageTranslation
                ? Path.Combine(FileManager.LocalizationFolder, langFolder, "Textures")
                : Path.Combine(FileManager.DefaultTexturesFolder, langFolder);

            if (Directory.Exists(texturesPath))
            {
                string[] files = Directory.GetFiles(texturesPath, "*.png");
                foreach (string file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    byte[] fileData = File.ReadAllBytes(file);

                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                    if (tex.LoadImage(fileData))
                    {
                        tex.name = fileName;
                        tex.wrapMode = TextureWrapMode.Clamp;
                        tex.filterMode = FilterMode.Bilinear;

                        CustomTextures[fileName] = tex;
                    }
                }
            }

            sw.Stop();
            string modeName = EnableImageTranslation ? "Modded" : "Original";
            Main.Log.LogInfo($"[Texture] Loaded: {CustomTextures.Count} {modeName} Textures in {sw.ElapsedMilliseconds} ms from {texturesPath}.");
        }

        public static Sprite GetTranslatedSprite(Sprite originalSprite, string cleanName)
        {
            if (CachedTranslatedSprites.TryGetValue(cleanName, out Sprite cached))
                return cached;

            if (CustomTextures.TryGetValue(cleanName, out Texture2D tex))
            {
                Vector2 pivot = new Vector2(0.5f, 0.5f);
                float ppu = 100f;
                Vector4 border = Vector4.zero;

                if (originalSprite != null)
                {
                    if (originalSprite.rect.width > 0 && originalSprite.rect.height > 0)
                    {
                        pivot = new Vector2(originalSprite.pivot.x / originalSprite.rect.width, originalSprite.pivot.y / originalSprite.rect.height);
                    }
                    ppu = originalSprite.pixelsPerUnit;
                    border = originalSprite.border;
                }

                if (border.x + border.z >= tex.width) { border.x = 0; border.z = 0; }
                if (border.y + border.w >= tex.height) { border.y = 0; border.w = 0; }

                Sprite newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, ppu, 0, SpriteMeshType.FullRect, border);
                newSprite.name = cleanName + "_Translated";

                CachedTranslatedSprites[cleanName] = newSprite;
                return newSprite;
            }

            return null;
        }

        public static void DumpAllLoadedSprites()
        {
            Main.Log.LogInfo("[Texture Dumper] Starting Advanced Scanning (Dump All Loaded Sprites)...");

            string langFolder = Features.LanguageMenu.CurrentLanguage;
            string targetFolder = Path.Combine(FileManager.DefaultTexturesFolder, langFolder);
            if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);

            Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            int dumpedCount = 0;

            foreach (Sprite spr in allSprites)
            {
                if (spr == null || string.IsNullOrEmpty(spr.name)) continue;

                string cleanName = Features.TextDumper.EscapeForJson(spr.name).Replace("_Translated", "").Replace("(Clone)", "").Trim();

                if (DumpedSprites.Contains(cleanName)) continue;

                string dumpPath = Path.Combine(targetFolder, cleanName + ".png");
                if (File.Exists(dumpPath))
                {
                    DumpedSprites.Add(cleanName);
                    continue;
                }

                bool success = DumpSpriteSilent(spr, cleanName, dumpPath);
                if (success) dumpedCount++;
            }

            Main.Log.LogInfo($"[Texture Dumper] Advanced Scanning Completed! {dumpedCount} new images dumped into {langFolder}.");
            Features.LanguageMenu.CreateNotificationUI($"Advanced Dump: {dumpedCount} Images Saved!", Color.green);
        }

        public static void DumpSprite(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return;

            string spriteName = sprite.name;
            if (string.IsNullOrEmpty(spriteName) || spriteName.Contains("UI_Mask") || spriteName == "Background") return;

            string cleanName = Features.TextDumper.EscapeForJson(spriteName).Replace("_Translated", "").Replace("(Clone)", "").Trim();

            if (DumpedSprites.Contains(cleanName)) return;

            string langFolder = Features.LanguageMenu.CurrentLanguage;
            string targetFolder = Path.Combine(FileManager.DefaultTexturesFolder, langFolder);
            if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);

            string dumpPath = Path.Combine(targetFolder, cleanName + ".png");
            if (File.Exists(dumpPath))
            {
                DumpedSprites.Add(cleanName);
                return;
            }

            bool success = DumpSpriteSilent(sprite, cleanName, dumpPath);
            if (success)
            {
                Main.Log.LogInfo($"[Texture Dumper] (Auto) Image '{cleanName}.png' successfully saved to [Default Textures].");
            }
        }

        // [FITUR BARU] Fungsi Dumper khusus untuk Tekstur utuh (Dipakai oleh Particle System / Material 3D)
        public static void DumpTexture2D(Texture texture, string cleanName)
        {
            if (texture == null || string.IsNullOrEmpty(cleanName)) return;
            if (DumpedSprites.Contains(cleanName)) return;

            string langFolder = Features.LanguageMenu.CurrentLanguage;
            string targetFolder = Path.Combine(FileManager.DefaultTexturesFolder, langFolder);
            if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);

            string dumpPath = Path.Combine(targetFolder, cleanName + ".png");
            if (File.Exists(dumpPath))
            {
                DumpedSprites.Add(cleanName);
                return;
            }

            try
            {
                RenderTexture tmp = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
                Graphics.Blit(texture, tmp);
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = tmp;

                Texture2D myTexture2D = new Texture2D(texture.width, texture.height);
                myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                myTexture2D.Apply();

                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(tmp);

                byte[] bytes = myTexture2D.EncodeToPNG();
                File.WriteAllBytes(dumpPath, bytes);

                DumpedSprites.Add(cleanName);
                Main.Log.LogInfo($"[Texture Dumper] (Particle/Material) '{cleanName}.png' successfully saved to [Default Textures].");
            }
            catch (System.Exception ex)
            {
                Main.Log.LogWarning($"[Texture Dumper] Failed to save Texture2D '{cleanName}': {ex.Message}");
            }
        }

        private static bool DumpSpriteSilent(Sprite sprite, string cleanName, string dumpPath)
        {
            try
            {
                RenderTexture tmp = RenderTexture.GetTemporary(
                                    sprite.texture.width,
                                    sprite.texture.height,
                                    0,
                                    RenderTextureFormat.Default,
                                    RenderTextureReadWrite.Linear);

                Graphics.Blit(sprite.texture, tmp);
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = tmp;

                Texture2D myTexture2D = new Texture2D(sprite.texture.width, sprite.texture.height);
                myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                myTexture2D.Apply();

                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(tmp);

                int x = Mathf.FloorToInt(sprite.rect.x);
                int y = Mathf.FloorToInt(sprite.rect.y);
                int width = Mathf.FloorToInt(sprite.rect.width);
                int height = Mathf.FloorToInt(sprite.rect.height);

                if (x < 0) x = 0;
                if (y < 0) y = 0;
                if (x + width > myTexture2D.width) width = myTexture2D.width - x;
                if (y + height > myTexture2D.height) height = myTexture2D.height - y;

                Texture2D croppedTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                Color[] pixels = myTexture2D.GetPixels(x, y, width, height);
                croppedTex.SetPixels(pixels);
                croppedTex.Apply();

                byte[] bytes = croppedTex.EncodeToPNG();
                File.WriteAllBytes(dumpPath, bytes);

                DumpedSprites.Add(cleanName);
                return true;
            }
            catch (System.Exception ex)
            {
                Main.Log.LogError($"[Texture Dumper] Failed to save '{cleanName}': {ex.Message}");
                return false;
            }
        }
    }
}