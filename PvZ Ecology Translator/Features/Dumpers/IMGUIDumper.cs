using UnityEngine;

namespace PvZEcologyTranslator.Features.Dumpers
{
    public static class IMGUIDumper
    {
        public static string Extract(GameObject obj)
        {
            // Mengekstrak teks dari UI sangat lawas IMGUI (GUIText)
            if (TextDumper.TryGetReflectionText(obj, "UnityEngine.GUIText", out string guiTxt)) return guiTxt;
            return "";
        }
    }
}