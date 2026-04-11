using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using UnityEngine.SceneManagement;
using PvZEcologyTranslator.Managers;
using PvZEcologyTranslator.Patches;
using PvZEcologyTranslator.Features;

namespace PvZEcologyTranslator
{
    // [UPDATE VERSI 0.2.0] Major UI Overrides Update & Smart Path
    // Penulisan versi di-hardcode di atribut untuk mencegah error compiler C# pada Unity lawas
    [BepInPlugin("com.ilhamgimank.pvzecology.translator", "PvZ Ecology Translator", "0.2.0")]
    public class Main : BaseUnityPlugin
    {
        // Variabel versi mod yang dipanggil oleh DeveloperMenu.cs (Ini yang menyelesaikan error ModVersion)
        public const string ModVersion = "0.2.0";

        internal static Main Instance;
        public static ManualLogSource Log;
        public static ConfigFile CustomConfig;

        public static ConfigEntry<bool> ConfigEnableTranslation;
        public static ConfigEntry<bool> ConfigEnableImageTranslation;
        public static ConfigEntry<bool> ConfigEnableUIOverrides;
        public static ConfigEntry<bool> ConfigEnableCurrency;
        public static ConfigEntry<string> ConfigTargetCurrency;

        public static ConfigEntry<bool> ConfigEnableAlmanacTranslation;
        public static ConfigEntry<bool> ConfigAutoIndentAlmanac;
        public static ConfigEntry<float> ConfigAutoIndentMultiplier;

        public static ConfigEntry<bool> ConfigEnableDump;
        public static ConfigEntry<bool> ConfigEnableTextureDumper;
        public static ConfigEntry<bool> ConfigEnableRegex;
        public static ConfigEntry<bool> ConfigPathLogging;
        public static ConfigEntry<bool> ConfigAdvPathLogging;
        public static ConfigEntry<bool> ConfigAutoTranslate;
        public static ConfigEntry<string> ConfigGoogleTargetLang;

        public static ConfigEntry<bool> ConfigEnableAlmanacDumper;
        public static ConfigEntry<bool> ConfigAlmanacZombieMode;

        private void Awake()
        {
            Instance = this;
            Log = base.Logger;

            FileManager.Initialize();
            LanguageMenu.InitConfig();
            InitConfig();

            TranslationManager.LoadTranslations();
            TextureManager.LoadCustomSprites();

            FontManager.LoadCustomFont();

            Harmony harmony = new Harmony("com.ilhamgimank.pvzecology.translator");
            TextPatch.PatchAll(harmony);
            ImagePatch.PatchAll(harmony);
            AnimatedImagePatch.PatchAll(harmony);

            gameObject.AddComponent<DeveloperMenu>();
            gameObject.AddComponent<GoogleTranslator>();

            SceneManager.sceneLoaded += OnSceneLoaded;

            Log.LogInfo($"Mod initialized successfully! Version {ModVersion} by Ilham Gimank.");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Log.LogInfo($"[Scene] {scene.name} loaded. Scanning static texts and images...");
            TextPatch.RefreshAllTexts();

            if (TextureManager.EnableImageTranslation)
            {
                ImagePatch.RefreshAllImages();
            }
        }

        private void InitConfig()
        {
            string configPath = Path.Combine(FileManager.RootFolder, "config.ini");
            CustomConfig = new ConfigFile(configPath, true);

            ConfigEnableTranslation = CustomConfig.Bind("General Settings", "EnableTranslation", true, "Aktifkan/Matikan fungsi terjemahan dari file");
            ConfigEnableImageTranslation = CustomConfig.Bind("General Settings", "EnableImageTranslation", true, "Aktifkan/Matikan fungsi penerapan terjemahan gambar/tekstur");
            ConfigEnableUIOverrides = CustomConfig.Bind("General Settings", "EnableUIOverrides", true, "Aktifkan/Matikan fitur penyesuaian UI otomatis berdasarkan path (ui_overrides.json)");
            ConfigEnableCurrency = CustomConfig.Bind("General Settings", "EnableCurrencyConversion", true, "Aktifkan Konversi Mata Uang Otomatis");
            ConfigTargetCurrency = CustomConfig.Bind("General Settings", "TargetCurrency", "IDR", "Mata uang target (USD, IDR, EUR, CNY, JPY, GBP)");

            ConfigEnableAlmanacTranslation = CustomConfig.Bind("General Settings", "EnableAlmanacTranslation", true, "Aktifkan/Matikan fungsi pembacaan file terjemahan Almanac");
            ConfigAutoIndentAlmanac = CustomConfig.Bind("General Settings", "AutoIndentAlmanac", true, "Otomatis merapikan baris baru pada teks Almanac yang memiliki titik dua (Hanging Indent)");
            ConfigAutoIndentMultiplier = CustomConfig.Bind("General Settings", "AutoIndentMultiplier", 0.40f, "Pengali jarak spasi untuk Auto Indent. Turunkan jika jaraknya terlalu lebar.");

            ConfigEnableDump = CustomConfig.Bind("Developer Settings", "EnableDumpedText", false, "Simpan status dumper teks (Auto Dump ke JSON)");
            ConfigEnableTextureDumper = CustomConfig.Bind("Developer Settings", "EnableTextureDumper", false, "Otomatis curi gambar saat mengunjungi scene ke [Default Textures]/[Bahasa]");
            ConfigEnableRegex = CustomConfig.Bind("Developer Settings", "EnableRegex", true, "Simpan status deteksi teks dinamis/Regex");
            ConfigPathLogging = CustomConfig.Bind("Developer Settings", "EnablePathLogging", true, "Simpan status logging path objek UI");
            ConfigAdvPathLogging = CustomConfig.Bind("Developer Settings", "EnableAdvPathLogging", false, "Simpan status logging path tingkat lanjut (Advanced)");
            ConfigAutoTranslate = CustomConfig.Bind("Developer Settings", "EnableAutoTranslate", false, "Aktifkan terjemahan otomatis menggunakan Google Translate API");
            ConfigGoogleTargetLang = CustomConfig.Bind("Developer Settings", "GoogleTargetLang", "id", "Kode bahasa target untuk Google Translate (id, en, dll)");

            ConfigEnableAlmanacDumper = CustomConfig.Bind("Developer Settings", "EnableAlmanacDumper", false, "Aktifkan fitur Auto-Dump khusus untuk halaman Almanac (Tanaman/Zombie)");
            ConfigAlmanacZombieMode = CustomConfig.Bind("Developer Settings", "AlmanacZombieMode", false, "Target dump Almanac: true = Zombie, false = Plant");

            LanguageToggle.IsTranslationEnabled = ConfigEnableTranslation.Value;
            TextureManager.EnableImageTranslation = ConfigEnableImageTranslation.Value;
            DeveloperMenu.EnableUIOverrides = ConfigEnableUIOverrides.Value;
            CurrencyConverter.EnableConversion = ConfigEnableCurrency.Value;
            CurrencyConverter.TargetCurrency = ConfigTargetCurrency.Value;

            DeveloperMenu.EnableAlmanacTranslation = ConfigEnableAlmanacTranslation.Value;
            DeveloperMenu.AutoIndentAlmanac = ConfigAutoIndentAlmanac.Value;
            DeveloperMenu.AutoIndentMultiplier = ConfigAutoIndentMultiplier.Value;

            DeveloperMenu.EnableDumpedText = ConfigEnableDump.Value;
            TextureManager.EnableTextureDumper = ConfigEnableTextureDumper.Value;
            DeveloperMenu.EnableRegex = ConfigEnableRegex.Value;
            PathDetector.IsEnabled = ConfigPathLogging.Value;
            PathDetector.IsAdvanced = ConfigAdvPathLogging.Value;
            DeveloperMenu.EnableAutoTranslate = ConfigAutoTranslate.Value;
            DeveloperMenu.GoogleTargetLang = ConfigGoogleTargetLang.Value;

            DeveloperMenu.EnableAlmanacDumper = ConfigEnableAlmanacDumper.Value;
            DeveloperMenu.AlmanacZombieMode = ConfigAlmanacZombieMode.Value;

            if (DeveloperMenu.EnableDumpedText)
            {
                TextDumper.EnableIMGUI = true;
                TextDumper.EnableUGUI = true;
                TextDumper.EnableNGUI = true;
                TextDumper.EnableTextMesh = true;
                TextDumper.EnableTMP = true;
            }

            Log.LogInfo($"Custom config loaded from: {configPath}");
        }

        private void Update()
        {
            PathDetector.HandleInput();
            LanguageToggle.HandleInput();
            LanguageMenu.Update();
            TextureMenu.Update();

            AlmanacDumper.Update();
        }

        // FUNGSI INI YANG SEBELUMNYA TERHAPUS (Ini yang menyelesaikan error SaveConfigs)
        public static void SaveConfigs()
        {
            if (CustomConfig == null) return;

            ConfigEnableTranslation.Value = LanguageToggle.IsTranslationEnabled;
            ConfigEnableImageTranslation.Value = TextureManager.EnableImageTranslation;
            ConfigEnableUIOverrides.Value = DeveloperMenu.EnableUIOverrides;
            ConfigEnableCurrency.Value = CurrencyConverter.EnableConversion;
            ConfigTargetCurrency.Value = CurrencyConverter.TargetCurrency;

            ConfigEnableAlmanacTranslation.Value = DeveloperMenu.EnableAlmanacTranslation;
            ConfigAutoIndentAlmanac.Value = DeveloperMenu.AutoIndentAlmanac;
            ConfigAutoIndentMultiplier.Value = DeveloperMenu.AutoIndentMultiplier;

            ConfigEnableDump.Value = DeveloperMenu.EnableDumpedText;
            ConfigEnableTextureDumper.Value = TextureManager.EnableTextureDumper;
            ConfigEnableRegex.Value = DeveloperMenu.EnableRegex;
            ConfigPathLogging.Value = PathDetector.IsEnabled;
            ConfigAdvPathLogging.Value = PathDetector.IsAdvanced;
            ConfigAutoTranslate.Value = DeveloperMenu.EnableAutoTranslate;
            ConfigGoogleTargetLang.Value = DeveloperMenu.GoogleTargetLang;

            ConfigEnableAlmanacDumper.Value = DeveloperMenu.EnableAlmanacDumper;
            ConfigAlmanacZombieMode.Value = DeveloperMenu.AlmanacZombieMode;

            CustomConfig.Save();
        }
    }
}