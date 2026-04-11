using UnityEngine;
using PvZEcologyTranslator.Features.Dumpers;
using System.Text.RegularExpressions;

namespace PvZEcologyTranslator.Features
{
    // Kelas utilitas statis yang bertugas "mencuri" (ekstrak) dan menyimpan teks dari game ke file JSON
    public static class TextDumper
    {
        // Kumpulan sakelar (toggle) untuk menyalakan/mematikan ekstraktor berdasarkan jenis teks
        // Nilainya diatur dari DeveloperMenu (F12)
        public static bool EnableIMGUI = true;
        public static bool EnableUGUI = true;
        public static bool EnableNGUI = true;
        public static bool EnableTextMesh = true;
        public static bool EnableTMP = true;

        // =========================================================================
        // FUNGSI EKSTRAKSI TEKS
        // Mengecek objek game dan mencoba mengambil teks apa pun yang ada padanya
        // =========================================================================
        public static string ExtractText(GameObject obj)
        {
            if (obj == null) return "";
            string txt = "";

            // Mencoba mengekstrak menggunakan berbagai Dumper spesifik jika fiturnya menyala
            if (EnableUGUI && string.IsNullOrEmpty(txt)) txt = UGUIDumper.Extract(obj);
            if (EnableTextMesh && string.IsNullOrEmpty(txt)) txt = TextMeshDumper.Extract(obj);
            if (EnableTMP && string.IsNullOrEmpty(txt)) txt = TMPDumper.Extract(obj);
            if (EnableNGUI && string.IsNullOrEmpty(txt)) txt = NGUIDumper.Extract(obj);
            if (EnableIMGUI && string.IsNullOrEmpty(txt)) txt = IMGUIDumper.Extract(obj);

            // Jika objek ini tidak punya teks, cari ke dalam anak-anaknya (child objects) secara rekursif
            if (string.IsNullOrEmpty(txt))
            {
                foreach (Transform child in obj.transform)
                {
                    txt = ExtractText(child.gameObject);
                    if (!string.IsNullOrEmpty(txt)) break; // Berhenti mencari jika sudah ketemu teks
                }
            }

            return txt;
        }

        // =========================================================================
        // FUNGSI BANTUAN REFLECTION
        // Berguna untuk mengambil teks dari komponen yang DLL-nya tidak kita referensikan langsung (Misal: TMPro)
        // =========================================================================
        public static bool TryGetReflectionText(GameObject obj, string componentName, out string result)
        {
            result = "";
            Component comp = obj.GetComponent(componentName);
            if (comp != null)
            {
                // Mencari properti bernama "text" secara paksa
                var prop = comp.GetType().GetProperty("text");
                if (prop != null)
                {
                    result = prop.GetValue(comp, null) as string;
                    return true;
                }
            }
            return false;
        }

        // =========================================================================
        // FUNGSI KEAMANAN JSON
        // Membersihkan karakter spesial bawaan game agar tidak merusak struktur file JSON
        // =========================================================================
        public static string EscapeForJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            // Mengubah enter, tab, dan kutip menjadi format teks aman
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        // =========================================================================
        // FUNGSI UTAMA DUMP TEKS
        // Memfilter teks yang masuk dan menyimpannya ke untranslation_strings.json
        // =========================================================================
        public static void DumpText(string text, string sourceType = "Text")
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            // 1. Filter Wajib: HANYA proses teks yang memiliki karakter Chinese/Mandarin (Hanzi)
            // Ini mencegah huruf bahasa Inggris atau angka biasa menumpuk di file dump
            if (!Regex.IsMatch(text, @"\p{IsCJKUnifiedIdeographs}"))
            {
                return;
            }

            // 2. Filter Kata Kunci UI Mod kita sendiri
            // Mencegah menu developer mod kita (F12) ikut tercatat ke dalam file dump
            string[] blockedKeywords = {
                "Enable ", "Disable", "Mod Version", "Game Version", "Active Language",
                "PvZ Ecology", "----", "Reload\n", "Manual\nHook", "Language", "Bahasa", "语言",
                "Queue Text", "Text Translated", "Plants Dumped", "Zombies Dumped"
            };

            foreach (string keyword in blockedKeywords)
            {
                if (text.Contains(keyword)) return;
            }

            // Abaikan jika teks ini ternyata sudah ada di kamus terjemahan biasa
            if (PvZEcologyTranslator.Managers.TranslationManager.Translations.ContainsKey(text)) return;

            // =========================================================================
            // SMART REGEX GENERATOR
            // Jika teks Mandarin ini mengandung angka, ubah otomatis jadi pola Regex!
            // Contoh: "Wave 1" -> "Wave (\d+)"
            // =========================================================================
            if (Regex.IsMatch(text, @"\d+"))
            {
                // [FIX] Escape tanda kurung literal bawaan teks agar tidak merusak format Regex!
                // Misalnya teks aslinya "(1)" akan diubah menjadi "\(\d+\)"
                string safePattern = text.Replace("(", "\\(").Replace(")", "\\)").Replace("[", "\\[").Replace("]", "\\]");

                // Mengubah semua angka yang ada di teks menjadi grup regex penangkap (\d+)
                string regexPattern = Regex.Replace(safePattern, @"\d+", "(\\d+)");

                // Melempar teks yang sudah jadi pola ini ke dumper khusus Regex
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

                // Cek lagi untuk memastikan teks ini belum pernah di-dump sebelumnya
                if (jsonContent.Contains($"\"{cleanText}\":")) return;

                // Mencari tanda kurung kurawal pembuka { pertama di dalam file JSON
                int insertPos = jsonContent.IndexOf('{') + 1;
                if (insertPos > 0)
                {
                    // Cek apakah kita butuh menambahkan tanda koma (,) sebelum menambahkan baris baru
                    bool isFirstEntry = jsonContent.IndexOf('}', insertPos) < jsonContent.IndexOf('"', insertPos) || jsonContent.IndexOf('"', insertPos) == -1;
                    string comma = isFirstEntry ? "" : ",";

                    // Membuat format baris baru: "TeksMandarin": "TeksMandarin",
                    string newEntry = $"\n  \"{cleanText}\": \"{cleanText}\"{comma}";

                    // Menyisipkan dan menyimpan teks ke hardisk
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

        // =========================================================================
        // FUNGSI DUMP REGEX (Menyimpan teks yang mengandung angka variabel)
        // =========================================================================
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

                // Membuat template value otomatis dengan format {0}, {1}
                // Jika polanya "Level (\d+) Wave (\d+)", hasil template-nya "Level {0} Wave {1}"
                int groupIndex = 0;
                string templateValue = Regex.Replace(pattern, @"\(\\d\+\)", match => $"{{{groupIndex++}}}");

                // Membuang escape literal kurung di hasil akhir terjemahan (karena tidak dibutuhkan lagi di string hasil)
                templateValue = templateValue.Replace("\\(", "(").Replace("\\)", ")").Replace("\\[", "[").Replace("\\]", "]");
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