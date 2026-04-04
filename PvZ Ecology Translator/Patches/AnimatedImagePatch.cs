#pragma warning disable IDE0051, IDE0079 

using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using PvZEcologyTranslator.Features;

namespace PvZEcologyTranslator.Patches
{
    public static class AnimatedImagePatch
    {
        public static void PatchAll(Harmony harmony)
        {
            try
            {
                var onEnableImg = AccessTools.Method(typeof(Image), "OnEnable");
                if (onEnableImg != null) harmony.Patch(onEnableImg, postfix: new HarmonyMethod(typeof(AnimatedImagePatch), nameof(Image_OnEnable_Postfix)));

                var onEnableSR = AccessTools.Method(typeof(SpriteRenderer), "OnEnable");
                if (onEnableSR != null) harmony.Patch(onEnableSR, postfix: new HarmonyMethod(typeof(AnimatedImagePatch), nameof(SpriteRenderer_OnEnable_Postfix)));

                Main.Log.LogInfo("[Hook] Universal Animated Image Watchers applied successfully.");
            }
            catch (System.Exception ex)
            {
                Main.Log.LogWarning($"[Hook] Universal Animated Image Watcher failed to patch: {ex.Message}");
            }
        }

        private static void Image_OnEnable_Postfix(Image __instance)
        {
            AttachWatcherIfNeeded(__instance.gameObject);
        }

        private static void SpriteRenderer_OnEnable_Postfix(SpriteRenderer __instance)
        {
            AttachWatcherIfNeeded(__instance.gameObject);
        }

        private static void AttachWatcherIfNeeded(GameObject obj)
        {
            if (obj == null) return;

            // [FIX 0.14.2] Pasang mata-mata ke SEMUA objek gambar tanpa pandang bulu! 
            // Karena banyak efek ledakan/partikel di game ini yang dianimasikan menggunakan 
            // script khusus (bukan komponen Animator bawaan Unity).
            if (obj.GetComponent<AnimationWatcher>() == null)
            {
                obj.AddComponent<AnimationWatcher>();
            }
        }
    }
}