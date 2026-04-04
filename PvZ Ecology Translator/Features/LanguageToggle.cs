using UnityEngine;
using PvZEcologyTranslator.Patches;

namespace PvZEcologyTranslator.Features
{
    public static class LanguageToggle
    {
        public static bool IsTranslationEnabled = true;

        public static void HandleInput()
        {
            if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetKeyDown(KeyCode.T))
            {
                IsTranslationEnabled = !IsTranslationEnabled;

                // Menggunakan Main.Log
                Main.Log.LogInfo($"[Language Toggle] Translation Enabled: {IsTranslationEnabled}");

                TextPatch.RefreshAllTexts();
            }
        }
    }
}