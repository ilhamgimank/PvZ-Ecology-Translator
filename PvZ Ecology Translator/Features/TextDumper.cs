using UnityEngine;
using PvZEcologyTranslator.Features.Dumpers;
using System.Text.RegularExpressions;

namespace PvZEcologyTranslator.Features
{
    public static class TextDumper
    {
        public static bool EnableIMGUI = true;
        public static bool EnableUGUI = true;
        public static bool EnableNGUI = true;
        public static bool EnableTextMesh = true;
        public static bool EnableTMP = true;

        public static string ExtractText(GameObject obj)
        {
            if (obj == null) return "";
            string txt = "";

            if (EnableUGUI && string.IsNullOrEmpty(txt)) txt = UGUIDumper.Extract(obj);
            if (EnableTextMesh && string.IsNullOrEmpty(txt)) txt = TextMeshDumper.Extract(obj);
            if (EnableTMP && string.IsNullOrEmpty(txt)) txt = TMPDumper.Extract(obj);
            if (EnableNGUI && string.IsNullOrEmpty(txt)) txt = NGUIDumper.Extract(obj);
            if (EnableIMGUI && string.IsNullOrEmpty(txt)) txt = IMGUIDumper.Extract(obj);

            if (string.IsNullOrEmpty(txt))
            {
                foreach (Transform child in obj.transform)
                {
                    txt = ExtractText(child.gameObject);
                    if (!string.IsNullOrEmpty(txt)) break;
                }
            }

            return txt;
        }

        public static bool TryGetReflectionText(GameObject obj, string componentName, out string result)
        {
            result = "";
            Component comp = obj.GetComponent(componentName);
            if (comp != null)
            {
                var prop = comp.GetType().GetProperty("text");
                if (prop != null)
                {
                    result = prop.GetValue(comp, null) as string;
                    return true;
                }
            }
            return false;
        }

        public static string EscapeForJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        public static void DumpText(string text, string sourceType = "Text")
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            // 1. Filter Wajib: HANYA proses teks yang memiliki karakter Chinese/Mandarin (Hanzi)
            if (!Regex.IsMatch(text, @"\p{IsCJKUnifiedIdeographs}"))
            {
                return;
            }

            // 2. Filter Kata Kunci UI Mod kita sendiri
            string[] blockedKeywords = {
                "Enable ", "Disable", "Mod Version", "Game Version", "Active Language",
                "PvZ Ecology", "----", "Reload\n", "Manual\nHook", "Language", "Bahasa", "语言",
                "Queue Text", "Text Translated", "Plants Dumped", "Zombies Dumped"
            };

            foreach (string keyword in blockedKeywords)
            {
                if (text.Contains(keyword)) return;
            }

            // Abaikan jika teks sudah ada di kamus terjemahan biasa
            if (PvZEcologyTranslator.Managers.TranslationManager.Translations.ContainsKey(text)) return;

            // =========================================================================
            // SMART REGEX GENERATOR
            // Jika teks Mandarin ini mengandung angka, ubah otomatis jadi pola Regex!
            // =========================================================================
            if (Regex.IsMatch(text, @"\d+"))
            {
                // Mengubah semua angka menjadi grup regex penangkap (\d+)
                string regexPattern = Regex.Replace(text, @"\d+", "(\\d+)");

                // Melempar teks ini ke dumper khusus Regex
                DumpRegexText(regexPattern, sourceType);
                return; // Hentikan eksekusi di sini agar tidak masuk ke file strings biasa!
            }

            // =========================================================================
            // DUMP STRINGS BIASA (Jika tidak ada angka)
            // =========================================================================
            try
            {
                string path = System.IO.Path.Combine(PvZEcologyTranslator.Managers.FileManager.DumpsFolder, "untranslation_strings.json");
                if (!System.IO.File.Exists(path)) return;

                string cleanText = EscapeForJson(text);
                string jsonContent = System.IO.File.ReadAllText(path, System.Text.Encoding.UTF8);

                if (jsonContent.Contains($"\"{cleanText}\":")) return;

                int insertPos = jsonContent.IndexOf('{') + 1;
                if (insertPos > 0)
                {
                    bool isFirstEntry = jsonContent.IndexOf('}', insertPos) < jsonContent.IndexOf('"', insertPos) || jsonContent.IndexOf('"', insertPos) == -1;
                    string comma = isFirstEntry ? "" : ",";

                    string newEntry = $"\n  \"{cleanText}\": \"{cleanText}\"{comma}";

                    jsonContent = jsonContent.Insert(insertPos, newEntry);
                    System.IO.File.WriteAllText(path, jsonContent, System.Text.Encoding.UTF8);

                    Main.Log.LogInfo($"[{sourceType} Added] {text}, successfully added to untranslation_strings.json");
                }
            }
            catch (System.Exception ex)
            {
                Main.Log.LogError($"[Text Dumper] Failed to save text: {ex.Message}");
            }
        }

        // Fungsi khusus untuk mendump pola Regex secara otomatis
        private static void DumpRegexText(string pattern, string sourceType)
        {
            try
            {
                string path = System.IO.Path.Combine(PvZEcologyTranslator.Managers.FileManager.DumpsFolder, "untranslation_regexs.json");
                if (!System.IO.File.Exists(path)) return;

                string cleanPattern = EscapeForJson(pattern);
                string jsonContent = System.IO.File.ReadAllText(path, System.Text.Encoding.UTF8);

                // Cek agar tidak mendump pola regex yang sama berulang kali
                if (jsonContent.Contains($"\"{cleanPattern}\":")) return;

                // [UPDATE] Membuat template value otomatis dengan format {0}, {1} (Misal: 关卡{0}-{1})
                int groupIndex = 0;
                string templateValue = Regex.Replace(pattern, @"\(\\d\+\)", match => $"{{{groupIndex++}}}");
                string cleanTemplate = EscapeForJson(templateValue);

                int insertPos = jsonContent.IndexOf('{') + 1;
                if (insertPos > 0)
                {
                    bool isFirstEntry = jsonContent.IndexOf('}', insertPos) < jsonContent.IndexOf('"', insertPos) || jsonContent.IndexOf('"', insertPos) == -1;
                    string comma = isFirstEntry ? "" : ",";

                    // Menulis format: "PolaRegex": "TemplateValue"
                    string newEntry = $"\n  \"{cleanPattern}\": \"{cleanTemplate}\"{comma}";

                    jsonContent = jsonContent.Insert(insertPos, newEntry);
                    System.IO.File.WriteAllText(path, jsonContent, System.Text.Encoding.UTF8);

                    Main.Log.LogInfo($"[Smart Regex Added] Pattern {pattern} generated and saved to untranslation_regexs.json");
                }
            }
            catch (System.Exception ex)
            {
                Main.Log.LogError($"[Smart Regex Dumper] Failed to save regex: {ex.Message}");
            }
        }
    }
}