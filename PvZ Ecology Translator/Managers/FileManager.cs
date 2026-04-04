using System.IO;
using BepInEx;

namespace PvZEcologyTranslator.Managers
{
    public static class FileManager
    {
        public static string RootFolder;
        public static string DefaultTexturesFolder;
        public static string CustomFontsFolder;
        public static string DumpsFolder;
        // DumpsTexturesFolder dihapus karena sekarang kita menggunakan DefaultTexturesFolder
        public static string LocalizationFolder;

        public static void Initialize()
        {
            RootFolder = Path.Combine(Paths.PluginPath, "PvZ Ecology Translator");

            DefaultTexturesFolder = Path.Combine(RootFolder, "[Default Textures]");
            CustomFontsFolder = Path.Combine(RootFolder, "[Custom Fonts]");
            DumpsFolder = Path.Combine(RootFolder, "Dumps");
            LocalizationFolder = Path.Combine(RootFolder, "Localization");

            CreateDirectorySafe(RootFolder);
            CreateDirectorySafe(DefaultTexturesFolder);
            CreateDirectorySafe(CustomFontsFolder);
            CreateDirectorySafe(DumpsFolder);
            CreateDirectorySafe(LocalizationFolder);

            InitializeDumpFiles();
            CreateLanguageTemplate("English");

            Main.Log.LogInfo("File and Folder structure initialized successfully.");
        }

        private static void CreateDirectorySafe(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Main.Log.LogInfo($"Created directory: {path}");
            }
        }

        private static void InitializeDumpFiles()
        {
            CreateJsonFileSafe(Path.Combine(DumpsFolder, "untranslation_strings.json"), "{\n\n}");
            CreateJsonFileSafe(Path.Combine(DumpsFolder, "untranslation_regexs.json"), "{\n\n}");
            CreateJsonFileSafe(Path.Combine(DumpsFolder, "untranslation_lawnstrings.json"), "{\"plants\":[]}");
            CreateJsonFileSafe(Path.Combine(DumpsFolder, "untranslation_zombiesstrings.json"), "{\"zombies\":[]}");
        }

        public static void CreateLanguageTemplate(string langName)
        {
            string langDir = Path.Combine(LocalizationFolder, langName);
            CreateDirectorySafe(langDir);

            CreateDirectorySafe(Path.Combine(langDir, "Textures"));

            string stringsDir = Path.Combine(langDir, "Strings");
            CreateDirectorySafe(stringsDir);
            CreateDirectorySafe(Path.Combine(stringsDir, "Almanac"));

            CreateJsonFileSafe(Path.Combine(stringsDir, "translation_strings.json"), "{\n\n}");
            CreateJsonFileSafe(Path.Combine(stringsDir, "translation_regexs.json"), "{\n\n}");

            CreateJsonFileSafe(Path.Combine(stringsDir, "ui_overrides.json"), "{\n  \"/Canvas/SetPanel/MoreOptionBtn/Text\": \"size=32; bestfit=false;\"\n}");

            CreateJsonFileSafe(Path.Combine(stringsDir, "Almanac", "translation_lawnstrings.json"), "{\"plants\":[]}");
            CreateJsonFileSafe(Path.Combine(stringsDir, "Almanac", "translation_zombiesstrings.json"), "{\"zombies\":[]}");
        }

        private static void CreateJsonFileSafe(string path, string defaultContent)
        {
            if (!File.Exists(path))
            {
                File.WriteAllText(path, defaultContent, System.Text.Encoding.UTF8);
                Main.Log.LogInfo($"Created JSON file: {path}");
            }
        }
    }
}