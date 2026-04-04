using System.Collections.Generic;
using System.IO;
using BepInEx;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Diagnostics;

namespace PvZEcologyTranslator.Managers
{
    public static class TranslationManager
    {
        public static Dictionary<string, string> Translations = new Dictionary<string, string>();
        public static Dictionary<string, string> RegexTranslations = new Dictionary<string, string>();
        public static Dictionary<string, string> UIOverrides = new Dictionary<string, string>();

        private const string LangFileName = "translation.txt";

        private class AlmanacEntry
        {
            public string Name;
            public string Desc;
            public string Cost;
            public string Recharge;
        }

        public static void LoadTranslations()
        {
            Stopwatch sw = Stopwatch.StartNew();

            Translations.Clear();
            RegexTranslations.Clear();
            UIOverrides.Clear();

            // Memuat file lama (Legacy) terlebih dahulu
            LoadLegacyTranslations();

            // File JSON sekarang akan dimuat SETELAH file legacy dan akan MENIMPA isinya jika ada yang bentrok!
            LoadJsonStrings();
            LoadRegexTranslations();
            LoadUIOverrides();

            LoadAlmanacTranslations();

            sw.Stop();
            Main.Log.LogInfo($"[Translation] Loaded: {Translations.Count} Strings, {RegexTranslations.Count} Regex, {UIOverrides.Count} UI Overrides in {sw.ElapsedMilliseconds} ms.");
        }

        private static void LoadLegacyTranslations()
        {
            string path = Path.Combine(Paths.PluginPath, LangFileName);
            if (!File.Exists(path)) return;

            string[] lines = File.ReadAllLines(path);
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;

                string[] parts = line.Split(new char[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    string original = parts[0].Trim();
                    string translated = parts[1].Trim();
                    if (!Translations.ContainsKey(original)) Translations.Add(original, translated);
                }
            }
        }

        private static string UnescapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\\"", "\"").Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\\\", "\\");
        }

        private static void LoadJsonStrings()
        {
            string langFolder = PvZEcologyTranslator.Features.LanguageMenu.CurrentLanguage;
            string jsonPath = Path.Combine(FileManager.LocalizationFolder, langFolder, "Strings", "translation_strings.json");

            if (!File.Exists(jsonPath)) return;

            try
            {
                string jsonContent = File.ReadAllText(jsonPath, System.Text.Encoding.UTF8);
                var matches = Regex.Matches(jsonContent, @"""((?:[^""\\]|\\.)*)""\s*:\s*""((?:[^""\\]|\\.)*)""");

                foreach (Match match in matches)
                {
                    if (match.Groups.Count == 3)
                    {
                        string original = UnescapeJson(match.Groups[1].Value);
                        string translation = UnescapeJson(match.Groups[2].Value);

                        if (original == translation) continue;

                        // [FIX 0.14.1] JSON Menang Mutlak! Menggunakan [ ] = akan memaksa 
                        // dictionary untuk menimpa teks lama jika kuncinya sudah ada sebelumnya.
                        if (!string.IsNullOrEmpty(translation))
                        {
                            Translations[original] = translation;
                        }
                    }
                }
            }
            catch (System.Exception ex) { Main.Log.LogError($"[Translation] Failed to load json: {ex.Message}"); }
        }

        private static void LoadRegexTranslations()
        {
            string langFolder = PvZEcologyTranslator.Features.LanguageMenu.CurrentLanguage;
            string regexPath = Path.Combine(FileManager.LocalizationFolder, langFolder, "Strings", "translation_regexs.json");

            if (!File.Exists(regexPath)) return;

            try
            {
                string jsonContent = File.ReadAllText(regexPath, System.Text.Encoding.UTF8);
                var matches = Regex.Matches(jsonContent, @"""((?:[^""\\]|\\.)*)""\s*:\s*""((?:[^""\\]|\\.)*)""");

                foreach (Match match in matches)
                {
                    if (match.Groups.Count == 3)
                    {
                        string pattern = UnescapeJson(match.Groups[1].Value);
                        string translation = UnescapeJson(match.Groups[2].Value);

                        translation = translation.Replace("{0}", "$1").Replace("{1}", "$2").Replace("{2}", "$3").Replace("{3}", "$4").Replace("{4}", "$5");

                        if (pattern == translation) continue;

                        // [FIX 0.14.1] Kekuasaan Mutlak JSON
                        if (!string.IsNullOrEmpty(translation))
                        {
                            RegexTranslations[pattern] = translation;
                        }
                    }
                }
            }
            catch (System.Exception ex) { Main.Log.LogError($"[Regex] Failed to load regex: {ex.Message}"); }
        }

        private static void LoadUIOverrides()
        {
            string langFolder = PvZEcologyTranslator.Features.LanguageMenu.CurrentLanguage;
            string overridePath = Path.Combine(FileManager.LocalizationFolder, langFolder, "Strings", "ui_overrides.json");

            if (!File.Exists(overridePath)) return;

            try
            {
                string jsonContent = File.ReadAllText(overridePath, System.Text.Encoding.UTF8);
                var matches = Regex.Matches(jsonContent, @"""((?:[^""\\]|\\.)*)""\s*:\s*""((?:[^""\\]|\\.)*)""");

                foreach (Match match in matches)
                {
                    if (match.Groups.Count == 3)
                    {
                        string uiPath = match.Groups[1].Value.Trim();
                        string properties = match.Groups[2].Value.Trim();

                        // [FIX 0.14.1] Kekuasaan Mutlak JSON
                        if (!string.IsNullOrEmpty(properties))
                        {
                            UIOverrides[uiPath] = properties;
                        }
                    }
                }
            }
            catch (System.Exception ex) { Main.Log.LogError($"[UI Overrides] Failed to load ui_overrides.json: {ex.Message}"); }
        }

        private static Dictionary<string, AlmanacEntry> ParseAlmanacJson(string json)
        {
            var result = new Dictionary<string, AlmanacEntry>();
            var blocks = Regex.Matches(json, @"\{\s*""id"".*?(?=\s*,\s*\{\s*""id""|\s*\]\s*\})", RegexOptions.Singleline);

            foreach (Match block in blocks)
            {
                string b = block.Value;
                string id = Regex.Match(b, @"""id""\s*:\s*(\d+)").Groups[1].Value;

                string name = Regex.Match(b, @"""name""\s*:\s*""((?:[^""\\]|\\.)*)""", RegexOptions.Singleline).Groups[1].Value;
                string desc = Regex.Match(b, @"""description""\s*:\s*""((?:[^""\\]|\\.)*)""", RegexOptions.Singleline).Groups[1].Value;
                string cost = Regex.Match(b, @"""cost""\s*:\s*""((?:[^""\\]|\\.)*)""", RegexOptions.Singleline).Groups[1].Value;
                string recharge = Regex.Match(b, @"""recharge""\s*:\s*""((?:[^""\\]|\\.)*)""", RegexOptions.Singleline).Groups[1].Value;

                if (!string.IsNullOrEmpty(id))
                {
                    result[id] = new AlmanacEntry
                    {
                        Name = UnescapeJson(name),
                        Desc = UnescapeJson(desc),
                        Cost = UnescapeJson(cost),
                        Recharge = UnescapeJson(recharge)
                    };
                }
            }
            return result;
        }

        private static string ApplyAutoIndent(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            text = text.Replace("\t", " ");

            string[] lines = text.Split('\n');
            int maxChars = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int colonIdx = line.IndexOf(':');
                if (colonIdx == -1) colonIdx = line.IndexOf('：');

                if (colonIdx > 1 && colonIdx <= 30)
                {
                    string preColon = line.Substring(0, colonIdx).TrimEnd();
                    string cleanPreColon = System.Text.RegularExpressions.Regex.Replace(preColon, "<.*?>", "");
                    if (cleanPreColon.Length > maxChars) maxChars = cleanPreColon.Length;
                }
            }

            if (maxChars > 0)
            {
                float multiplier = PvZEcologyTranslator.Features.DeveloperMenu.AutoIndentMultiplier;

                float maxPreColonEm = (maxChars + 1f) * multiplier;
                string maxEmStr = maxPreColonEm.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

                float totalIndent = maxPreColonEm + (2.5f * multiplier);
                string indentStr = totalIndent.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    int colonIdx = line.IndexOf(':');
                    if (colonIdx == -1) colonIdx = line.IndexOf('：');

                    if (colonIdx > 1 && colonIdx <= 30)
                    {
                        string preColon = line.Substring(0, colonIdx).TrimEnd();
                        char colonChar = line[colonIdx];
                        string content = line.Substring(colonIdx + 1).TrimStart();

                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            lines[i] = $"{preColon}<pos={maxEmStr}em>{colonChar} <indent={indentStr}em>{content}</indent>";
                        }
                    }
                }
            }
            return string.Join("\n", lines);
        }

        private static void LoadAlmanacTranslations()
        {
            if (!PvZEcologyTranslator.Features.DeveloperMenu.EnableAlmanacTranslation) return;

            string langFolder = PvZEcologyTranslator.Features.LanguageMenu.CurrentLanguage;
            string[] types = { "lawnstrings", "zombiesstrings" };

            foreach (string type in types)
            {
                string origPath = Path.Combine(FileManager.DumpsFolder, $"untranslation_{type}.json");
                string transPath = Path.Combine(FileManager.LocalizationFolder, langFolder, "Strings", "Almanac", $"translation_{type}.json");

                if (File.Exists(origPath) && File.Exists(transPath))
                {
                    try
                    {
                        var origData = ParseAlmanacJson(File.ReadAllText(origPath, System.Text.Encoding.UTF8));
                        var transData = ParseAlmanacJson(File.ReadAllText(transPath, System.Text.Encoding.UTF8));

                        foreach (var kvp in origData)
                        {
                            if (transData.TryGetValue(kvp.Key, out var trans))
                            {
                                if (!string.IsNullOrEmpty(kvp.Value.Name) && !string.IsNullOrEmpty(trans.Name) && kvp.Value.Name != trans.Name)
                                    Translations[kvp.Value.Name] = trans.Name;

                                if (!string.IsNullOrEmpty(kvp.Value.Desc) && !string.IsNullOrEmpty(trans.Desc) && kvp.Value.Desc != trans.Desc)
                                {
                                    string finalDesc = trans.Desc;

                                    if (PvZEcologyTranslator.Features.DeveloperMenu.AutoIndentAlmanac)
                                    {
                                        finalDesc = ApplyAutoIndent(finalDesc);
                                    }

                                    Translations[kvp.Value.Desc] = finalDesc;
                                }

                                if (!string.IsNullOrEmpty(kvp.Value.Cost) && !string.IsNullOrEmpty(trans.Cost) && kvp.Value.Cost != trans.Cost)
                                    Translations[kvp.Value.Cost] = trans.Cost;

                                if (!string.IsNullOrEmpty(kvp.Value.Recharge) && !string.IsNullOrEmpty(trans.Recharge) && kvp.Value.Recharge != trans.Recharge)
                                    Translations[kvp.Value.Recharge] = trans.Recharge;
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Main.Log.LogError($"[Almanac] Failed to load {type}: {ex.Message}");
                    }
                }
            }
        }
    }
}