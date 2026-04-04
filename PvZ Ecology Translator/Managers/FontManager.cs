using UnityEngine;
using System.IO;
using PvZEcologyTranslator.Features;
using HarmonyLib;

namespace PvZEcologyTranslator.Managers
{
    public static class FontManager
    {
        public static Font CustomFont;
        public static Object CustomTMPFont;
        public static bool IsFontLoaded = false;

        private static AssetBundle currentBundle;

        public static void LoadCustomFont()
        {
            CustomFont = null;
            CustomTMPFont = null;
            IsFontLoaded = false;

            currentBundle?.Unload(true);
            currentBundle = null;

            string lang = LanguageMenu.CurrentLanguage;
            if (string.IsNullOrEmpty(lang)) return;

            string folder = FileManager.CustomFontsFolder;
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string bundlePath = Path.Combine(folder, lang + ".bundle");
            string ttfPath = Path.Combine(folder, lang + ".ttf");
            string otfPath = Path.Combine(folder, lang + ".otf");

            // 1. Prioritaskan memuat AssetBundle jika ada (Cara Resmi Unity - Mensupport Semua Teks)
            if (File.Exists(bundlePath))
            {
                currentBundle = AssetBundle.LoadFromFile(bundlePath);
                if (currentBundle != null)
                {
                    Object[] allAssets = currentBundle.LoadAllAssets();
                    bool foundAny = false;

                    foreach (Object asset in allAssets)
                    {
                        if (asset is Font f && CustomFont == null)
                        {
                            CustomFont = f;
                            foundAny = true;
                        }
                        else if (asset.GetType().Name == "TMP_FontAsset" && CustomTMPFont == null)
                        {
                            CustomTMPFont = asset;
                            foundAny = true;
                        }
                    }

                    if (foundAny)
                    {
                        IsFontLoaded = true;
                        string tmpStatus = CustomTMPFont != null ? "(& TMPro Asset)" : "";
                        Main.Log.LogInfo($"[FontManager] Custom font {tmpStatus} successfully loaded from '{lang}.bundle'.");
                    }
                }
            }
            // 2. Bypass Limitasi! Memuat langsung file mentah .ttf / .otf
            else if (File.Exists(ttfPath) || File.Exists(otfPath))
            {
                string fontPath = File.Exists(ttfPath) ? ttfPath : otfPath;
                try
                {
                    Font rawFont = new Font(Path.GetFileNameWithoutExtension(fontPath));
                    var internalCreate = AccessTools.Method(typeof(Font), "Internal_CreateFontFromPath");

                    if (internalCreate != null)
                    {
                        internalCreate.Invoke(null, new object[] { rawFont, fontPath });

                        // [FIX 0.12.4] KITA TIDAK MENYUNTIKKAN rawFont ke CustomFont (UGUI)!
                        // rawFont akan menjadi static/non-dynamic sehingga UGUI Text akan crash dan menghilang jika ini dipakai.
                        // rawFont HANYA akan digunakan sebagai bahan baku untuk TextMeshPro!
                        CustomFont = null;

                        System.Type tmpFontAssetType = AccessTools.TypeByName("TMPro.TMP_FontAsset");
                        if (tmpFontAssetType != null)
                        {
                            var createMethod = AccessTools.Method(tmpFontAssetType, "CreateFontAsset", new System.Type[] { typeof(Font) });
                            if (createMethod != null)
                            {
                                CustomTMPFont = (Object)createMethod.Invoke(null, new object[] { rawFont });
                                if (CustomTMPFont != null)
                                {
                                    var hideFlagsProp = CustomTMPFont.GetType().GetProperty("hideFlags");
                                    hideFlagsProp?.SetValue(CustomTMPFont, HideFlags.HideAndDontSave, null);

                                    // Tandai sukses HANYA jika font TMPro berhasil dibuat
                                    IsFontLoaded = true;
                                }
                            }
                        }

                        if (IsFontLoaded)
                        {
                            Main.Log.LogInfo($"[FontManager] Bypass success! Raw file {Path.GetFileName(fontPath)} successfully generated for TMPro.");
                            Main.Log.LogWarning($"[FontManager] Note: Raw .ttf/.otf only applies to TextMeshPro. Standard UGUI texts will use their original fonts to prevent disappearing. Use .bundle for full support.");
                        }
                    }
                    else
                    {
                        Main.Log.LogWarning($"[FontManager] Internal_CreateFontFromPath function not found in this Unity version.");
                    }
                }
                catch (System.Exception ex)
                {
                    Main.Log.LogError($"[FontManager] Failed to load raw font: {ex.Message}");
                }
            }
            else
            {
                Main.Log.LogInfo($"[FontManager] No custom font found for language: {lang}");
            }
        }
    }
}