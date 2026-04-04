using UnityEngine;

namespace PvZEcologyTranslator.Features.Dumpers
{
    public static class TMPDumper
    {
        public static string Extract(GameObject obj)
        {
            // Mengekstrak dari TextMeshPro UI Baru
            if (TextDumper.TryGetReflectionText(obj, "TMPro.TextMeshProUGUI", out string tmpUI)) return tmpUI;

            // Mengekstrak dari TextMeshPro 3D Baru
            if (TextDumper.TryGetReflectionText(obj, "TMPro.TextMeshPro", out string tmp3D)) return tmp3D;

            return "";
        }
    }
}