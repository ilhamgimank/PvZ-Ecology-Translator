using UnityEngine;

namespace PvZEcologyTranslator.Features.Dumpers
{
    public static class TextMeshDumper
    {
        public static string Extract(GameObject obj)
        {
            // Mengekstrak teks dari komponen TextMesh 3D lawas
            if (obj.TryGetComponent<TextMesh>(out var txtMesh)) return txtMesh.text;
            return "";
        }
    }
}