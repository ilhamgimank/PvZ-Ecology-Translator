#pragma warning disable IDE0051 

using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using PvZEcologyTranslator.Managers;
using PvZEcologyTranslator.Features;
using System.Collections.Generic;
using System.Globalization;

namespace PvZEcologyTranslator.Patches
{
    public static class ImagePatch
    {
        public static Dictionary<Component, Sprite> OriginalSpriteCache = new Dictionary<Component, Sprite>();
        private static bool isApplyingTranslation = false;

        public static void PatchAll(Harmony harmony)
        {
            try
            {
                var setterImg = AccessTools.PropertySetter(typeof(Image), "sprite");
                harmony.Patch(setterImg, prefix: new HarmonyMethod(typeof(ImagePatch), nameof(Image_Prefix)));
                var onEnableImg = AccessTools.Method(typeof(Image), "OnEnable");
                if (onEnableImg != null) harmony.Patch(onEnableImg, postfix: new HarmonyMethod(typeof(ImagePatch), nameof(Image_OnEnable_Postfix)));

                Main.Log.LogInfo("[Hook] UGUI Image hooks applied.");
            }
            catch { Main.Log.LogWarning("[Hook] UGUI Image failed to patch."); }

            try
            {
                var setterSR = AccessTools.PropertySetter(typeof(SpriteRenderer), "sprite");
                harmony.Patch(setterSR, prefix: new HarmonyMethod(typeof(ImagePatch), nameof(SpriteRenderer_Prefix)));
                var onEnableSR = AccessTools.Method(typeof(SpriteRenderer), "OnEnable");
                if (onEnableSR != null) harmony.Patch(onEnableSR, postfix: new HarmonyMethod(typeof(ImagePatch), nameof(SpriteRenderer_OnEnable_Postfix)));

                Main.Log.LogInfo("[Hook] SpriteRenderer hooks applied.");
            }
            catch { }
        }

        private static void Image_Prefix(Image __instance, ref Sprite value) => ProcessImage(__instance, ref value);
        private static void SpriteRenderer_Prefix(SpriteRenderer __instance, ref Sprite value) => ProcessImage(__instance, ref value);

        private static void Image_OnEnable_Postfix(Image __instance)
        {
            if (__instance == null || __instance.sprite == null) return;
            Sprite val = __instance.sprite;

            if (OriginalSpriteCache.TryGetValue(__instance, out Sprite cached))
            {
                val = cached;
            }

            ProcessImage(__instance, ref val, true);

            if (__instance.sprite != val)
            {
                isApplyingTranslation = true;
                __instance.sprite = val;
                isApplyingTranslation = false;
            }
        }

        private static void SpriteRenderer_OnEnable_Postfix(SpriteRenderer __instance)
        {
            if (__instance == null || __instance.sprite == null) return;
            Sprite val = __instance.sprite;

            if (OriginalSpriteCache.TryGetValue(__instance, out Sprite cached))
            {
                val = cached;
            }

            ProcessImage(__instance, ref val, true);

            if (__instance.sprite != val)
            {
                isApplyingTranslation = true;
                __instance.sprite = val;
                isApplyingTranslation = false;
            }
        }

        private static void ProcessImage(Component comp, ref Sprite value, bool isRefresh = false)
        {
            if (value == null || isApplyingTranslation) return;

            if (!isRefresh && comp != null)
            {
                if (!value.name.EndsWith("_Translated"))
                {
                    OriginalSpriteCache[comp] = value;
                }
            }

            if (TextureManager.EnableTextureDumper && !value.name.EndsWith("_Translated"))
            {
                TextureManager.DumpSprite(value);
            }

            // [UPDATE] Hapus batasan if (TextureManager.EnableImageTranslation)
            // Karena sekarang kita selalu memuat tekstur ke memori sesuai mode yang dipilih (Modded/Original)
            string lookupName = value.name;
            Sprite refSprite = value;

            if (isRefresh && comp != null && OriginalSpriteCache.TryGetValue(comp, out Sprite orig) && orig != null)
            {
                lookupName = orig.name;
                refSprite = orig;
            }

            if (lookupName.EndsWith("_Translated")) lookupName = lookupName.Replace("_Translated", "");
            string cleanName = lookupName.Replace("(Clone)", "").Trim();

            Sprite customSprite = TextureManager.GetTranslatedSprite(refSprite, cleanName);
            if (customSprite != null)
            {
                value = customSprite;
            }

            if (DeveloperMenu.EnableUIOverrides && comp != null)
            {
                string objPath = PathDetector.GetPath(comp.transform);
                if (TranslationManager.UIOverrides.TryGetValue(objPath, out string overrideData))
                {
                    ApplyUIOverrides(comp, overrideData);
                }
            }
        }

        private static void ApplyUIOverrides(Component comp, string overrideData)
        {
            string[] props = overrideData.Split(';');
            foreach (string prop in props)
            {
                if (string.IsNullOrWhiteSpace(prop)) continue;

                string[] kvp = prop.Split('=');
                if (kvp.Length != 2) continue;

                string key = kvp[0].Trim().ToLower();
                string val = kvp[1].Trim().ToLower();

                if (key == "rotation" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float rot))
                {
                    comp.transform.localEulerAngles = new Vector3(comp.transform.localEulerAngles.x, comp.transform.localEulerAngles.y, rot);
                }
                else if (key == "posx" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float px))
                {
                    if (comp.transform is RectTransform rt) rt.anchoredPosition = new Vector2(px, rt.anchoredPosition.y);
                    else comp.transform.localPosition = new Vector3(px, comp.transform.localPosition.y, comp.transform.localPosition.z);
                }
                else if (key == "posy" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float py))
                {
                    if (comp.transform is RectTransform rt) rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, py);
                    else comp.transform.localPosition = new Vector3(comp.transform.localPosition.x, py, comp.transform.localPosition.z);
                }
                // [UPDATE 0.14.5] Fix Absolute Width untuk gambar
                else if (key == "width" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float w))
                {
                    if (comp.transform is RectTransform rt) rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
                }
                else if (key == "height" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float h))
                {
                    if (comp.transform is RectTransform rt) rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
                }
                else if (key == "color" && ColorUtility.TryParseHtmlString(val, out Color c))
                {
                    if (comp is Image img) img.color = c;
                    else if (comp is SpriteRenderer sr) sr.color = c;
                }
                else if (key == "nativesize" && bool.TryParse(val, out bool ns) && ns)
                {
                    if (comp is Image img) img.SetNativeSize();
                }
            }
        }

        public static void RefreshAllImages()
        {
            try
            {
                List<Component> deadKeys = new List<Component>();
                foreach (var key in OriginalSpriteCache.Keys)
                {
                    if (key == null) deadKeys.Add(key);
                }
                foreach (var k in deadKeys) OriginalSpriteCache.Remove(k);

                Image[] allImages = Resources.FindObjectsOfTypeAll<Image>();
                foreach (Image img in allImages)
                {
                    if (img == null || img.gameObject.scene.name == null) continue;
                    if (img.gameObject.hideFlags == HideFlags.NotEditable || img.gameObject.hideFlags == HideFlags.HideAndDontSave) continue;

                    Sprite original = null;
                    if (OriginalSpriteCache.TryGetValue(img, out Sprite cached))
                    {
                        original = cached;
                    }
                    else
                    {
                        original = img.sprite;
                        if (original != null && !original.name.EndsWith("_Translated"))
                        {
                            OriginalSpriteCache[img] = original;
                        }
                    }

                    if (original == null) continue;
                    Sprite val = original;
                    ProcessImage(img, ref val, true);
                    if (img.sprite != val)
                    {
                        isApplyingTranslation = true;
                        img.sprite = val;
                        isApplyingTranslation = false;
                    }
                }

                SpriteRenderer[] allSRs = Resources.FindObjectsOfTypeAll<SpriteRenderer>();
                foreach (SpriteRenderer sr in allSRs)
                {
                    if (sr == null || sr.gameObject.scene.name == null) continue;

                    Sprite original = null;
                    if (OriginalSpriteCache.TryGetValue(sr, out Sprite cached))
                    {
                        original = cached;
                    }
                    else
                    {
                        original = sr.sprite;
                        if (original != null && !original.name.EndsWith("_Translated"))
                        {
                            OriginalSpriteCache[sr] = original;
                        }
                    }

                    if (original == null) continue;
                    Sprite val = original;
                    ProcessImage(sr, ref val, true);
                    if (sr.sprite != val)
                    {
                        isApplyingTranslation = true;
                        sr.sprite = val;
                        isApplyingTranslation = false;
                    }
                }
                Main.Log.LogInfo("[ImagePatch] All images & textures successfully refreshed (F6).");
            }
            catch (System.Exception ex)
            {
                Main.Log.LogWarning($"[ImagePatch] Error during RefreshAllImages: {ex.Message}");
            }
        }
    }
}