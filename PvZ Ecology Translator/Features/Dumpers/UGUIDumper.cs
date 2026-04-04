using UnityEngine;
using UnityEngine.UI;

namespace PvZEcologyTranslator.Features.Dumpers
{
    public static class UGUIDumper
    {
        public static string Extract(GameObject obj)
        {
            // Mengekstrak teks dari UI Text standar bawaan Unity (UGUI)
            if (obj.TryGetComponent<Text>(out var uiText)) return uiText.text;
            return "";
        }
    }
}