using UnityEngine;

namespace PvZEcologyTranslator.Features.Dumpers
{
    public static class NGUIDumper
    {
        public static string Extract(GameObject obj)
        {
            // Mengekstrak teks dari UI lawas NGUI (UILabel)
            if (TextDumper.TryGetReflectionText(obj, "UILabel", out string ngui)) return ngui;
            return "";
        }
    }
}