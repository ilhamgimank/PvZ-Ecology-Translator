#pragma warning disable IDE0051, IDE0270 

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
                Main.Log.LogInfo("[Hook] SpriteRenderer hooks applied.");
            }
            catch { }

            try
            {
                var inst1 = AccessTools.Method(typeof(Object), "Instantiate", new System.Type[] { typeof(Object) });
                var inst2 = AccessTools.Method(typeof(Object), "Instantiate", new System.Type[] { typeof(Object), typeof(Transform) });
                var inst3 = AccessTools.Method(typeof(Object), "Instantiate", new System.Type[] { typeof(Object), typeof(Transform), typeof(bool) });
                var inst4 = AccessTools.Method(typeof(Object), "Instantiate", new System.Type[] { typeof(Object), typeof(Vector3), typeof(Quaternion) });
                var inst5 = AccessTools.Method(typeof(Object), "Instantiate", new System.Type[] { typeof(Object), typeof(Vector3), typeof(Quaternion), typeof(Transform) });

                var postfixInst = new HarmonyMethod(typeof(ImagePatch), nameof(Instantiate_Postfix));

                if (inst1 != null) harmony.Patch(inst1, postfix: postfixInst);
                if (inst2 != null) harmony.Patch(inst2, postfix: postfixInst);
                if (inst3 != null) harmony.Patch(inst3, postfix: postfixInst);
                if (inst4 != null) harmony.Patch(inst4, postfix: postfixInst);
                if (inst5 != null) harmony.Patch(inst5, postfix: postfixInst);

                Main.Log.LogInfo("[Hook] Dynamic Object Spawner (Instantiate) hooks applied.");
            }
            catch { Main.Log.LogWarning("[Hook] Dynamic Object Spawner failed to patch."); }

            GameObject scanner = new GameObject("PvZ_ImageScanner");
            scanner.AddComponent<DynamicImageScanner>();
            Object.DontDestroyOnLoad(scanner);
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

        // [UPDATE] Pasang mata-mata ke SEMUA objek tanpa terkecuali!
        private static void Instantiate_Postfix(Object __result)
        {
            if (!TextureManager.EnableImageTranslation || __result == null) return;

            GameObject go = __result as GameObject;
            if (go == null)
            {
                Component comp = __result as Component;
                if (comp != null) go = comp.gameObject;
            }
            if (go == null) return;

            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                if (r is SpriteRenderer sr)
                {
                    if (sr.gameObject.GetComponent<AnimationWatcher>() == null) sr.gameObject.AddComponent<AnimationWatcher>();
                    if (sr.sprite != null)
                    {
                        Sprite val = sr.sprite;
                        ProcessImage(sr, ref val, true);
                        if (sr.sprite != val) { isApplyingTranslation = true; sr.sprite = val; isApplyingTranslation = false; }
                    }
                }
                else if (r is ParticleSystemRenderer psr)
                {
                    if (psr.gameObject.GetComponent<AnimationWatcher>() == null) psr.gameObject.AddComponent<AnimationWatcher>();
                    if (psr.sharedMaterial != null && psr.sharedMaterial.mainTexture != null)
                    {
                        Texture tex = psr.sharedMaterial.mainTexture;
                        if (!tex.name.EndsWith("_Translated"))
                        {
                            string cleanName = tex.name.Replace("(Clone)", "").Replace("(Instance)", "").Trim();
                            if (TextureManager.CustomTextures.TryGetValue(cleanName, out Texture2D customTex))
                            {
                                customTex.name = cleanName + "_Translated";
                                psr.sharedMaterial.mainTexture = customTex;
                            }
                        }
                    }
                }
                else if (r is MeshRenderer mr)
                {
                    if (mr.gameObject.GetComponent<AnimationWatcher>() == null) mr.gameObject.AddComponent<AnimationWatcher>();
                    if (mr.sharedMaterial != null && mr.sharedMaterial.mainTexture != null)
                    {
                        Texture tex = mr.sharedMaterial.mainTexture;
                        if (!tex.name.EndsWith("_Translated"))
                        {
                            string cleanName = tex.name.Replace("(Clone)", "").Replace("(Instance)", "").Trim();
                            if (TextureManager.CustomTextures.TryGetValue(cleanName, out Texture2D customTex))
                            {
                                customTex.name = cleanName + "_Translated";
                                mr.sharedMaterial.mainTexture = customTex;
                            }
                        }
                    }
                }
            }

            Image[] images = go.GetComponentsInChildren<Image>(true);
            foreach (Image img in images)
            {
                if (img.gameObject.GetComponent<AnimationWatcher>() == null) img.gameObject.AddComponent<AnimationWatcher>();
                if (img.sprite != null)
                {
                    Sprite val = img.sprite;
                    ProcessImage(img, ref val, true);
                    if (img.sprite != val) { isApplyingTranslation = true; img.sprite = val; isApplyingTranslation = false; }
                }
            }
        }

        private class DynamicImageScanner : MonoBehaviour
        {
            private float scanTimer = 0f;

            void Update()
            {
                if (!TextureManager.EnableImageTranslation) return;

                scanTimer -= Time.unscaledDeltaTime;
                if (scanTimer <= 0)
                {
                    // Scan pelapis santai setiap 2 detik untuk menangkap objek yang bersembunyi
                    scanTimer = 2.0f;

                    SpriteRenderer[] allSRs = FindObjectsOfType<SpriteRenderer>();
                    foreach (SpriteRenderer sr in allSRs)
                        if (sr.gameObject.GetComponent<AnimationWatcher>() == null) sr.gameObject.AddComponent<AnimationWatcher>();

                    ParticleSystemRenderer[] allPSRs = FindObjectsOfType<ParticleSystemRenderer>();
                    foreach (ParticleSystemRenderer psr in allPSRs)
                        if (psr.gameObject.GetComponent<AnimationWatcher>() == null) psr.gameObject.AddComponent<AnimationWatcher>();

                    Image[] allImgs = FindObjectsOfType<Image>();
                    foreach (Image img in allImgs)
                        if (img.gameObject.GetComponent<AnimationWatcher>() == null) img.gameObject.AddComponent<AnimationWatcher>();
                }
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
                else if (key == "width" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float w))
                {
                    if (comp.transform is RectTransform rt) rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
                }
                else if (key == "height" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float h))
                {
                    if (comp.transform is RectTransform rt) rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
                }
                else if (key == "scalex" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float sx))
                {
                    comp.transform.localScale = new Vector3(sx, comp.transform.localScale.y, comp.transform.localScale.z);
                }
                else if (key == "scaley" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float sy))
                {
                    comp.transform.localScale = new Vector3(comp.transform.localScale.x, sy, comp.transform.localScale.z);
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
                    if (img.gameObject.GetComponent<AnimationWatcher>() == null) img.gameObject.AddComponent<AnimationWatcher>();

                    if (img.gameObject.hideFlags == HideFlags.NotEditable || img.gameObject.hideFlags == HideFlags.HideAndDontSave) continue;

                    Sprite original = null;
                    if (OriginalSpriteCache.TryGetValue(img, out Sprite cached)) { original = cached; }
                    else
                    {
                        original = img.sprite;
                        if (original != null && !original.name.EndsWith("_Translated")) { OriginalSpriteCache[img] = original; }
                    }

                    if (original == null) continue;
                    Sprite val = original;
                    ProcessImage(img, ref val, true);
                    if (img.sprite != val) { isApplyingTranslation = true; img.sprite = val; isApplyingTranslation = false; }
                }

                SpriteRenderer[] allSRs = Resources.FindObjectsOfTypeAll<SpriteRenderer>();
                foreach (SpriteRenderer sr in allSRs)
                {
                    if (sr == null || sr.gameObject.scene.name == null) continue;
                    if (sr.gameObject.GetComponent<AnimationWatcher>() == null) sr.gameObject.AddComponent<AnimationWatcher>();

                    Sprite original = null;
                    if (OriginalSpriteCache.TryGetValue(sr, out Sprite cached)) { original = cached; }
                    else
                    {
                        original = sr.sprite;
                        if (original != null && !original.name.EndsWith("_Translated")) { OriginalSpriteCache[sr] = original; }
                    }

                    if (original == null) continue;
                    Sprite val = original;
                    ProcessImage(sr, ref val, true);
                    if (sr.sprite != val) { isApplyingTranslation = true; sr.sprite = val; isApplyingTranslation = false; }
                }

                ParticleSystemRenderer[] allPSRs = Resources.FindObjectsOfTypeAll<ParticleSystemRenderer>();
                foreach (ParticleSystemRenderer psr in allPSRs)
                {
                    if (psr == null || psr.gameObject.scene.name == null) continue;
                    if (psr.gameObject.GetComponent<AnimationWatcher>() == null) psr.gameObject.AddComponent<AnimationWatcher>();

                    if (psr.sharedMaterial != null && psr.sharedMaterial.mainTexture != null)
                    {
                        Texture tex = psr.sharedMaterial.mainTexture;
                        if (!tex.name.EndsWith("_Translated"))
                        {
                            string cleanName = tex.name.Replace("(Clone)", "").Replace("(Instance)", "").Trim();
                            if (TextureManager.CustomTextures.TryGetValue(cleanName, out Texture2D customTex))
                            {
                                customTex.name = cleanName + "_Translated";
                                psr.sharedMaterial.mainTexture = customTex;
                            }
                        }
                    }
                }

                Main.Log.LogInfo("[ImagePatch] All images, sprites, and particles successfully refreshed (F6).");
            }
            catch (System.Exception ex)
            {
                Main.Log.LogWarning($"[ImagePatch] Error during RefreshAllImages: {ex.Message}");
            }
        }
    }
}