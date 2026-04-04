#pragma warning disable IDE0031, IDE0079 // Membungkam pesan Null Check dan Unnecessary Suppression

using UnityEngine;
using PvZEcologyTranslator.Managers;
using PvZEcologyTranslator.Patches;

namespace PvZEcologyTranslator.Features
{
    public class DeveloperMenu : MonoBehaviour
    {
        public static bool IsOpen = false;
        public static bool EnableUIOverrides = true;
        public static bool EnableDumpedText = false;
        public static bool EnableRegex = true;
        public static bool EnableAutoTranslate = false;

        public static bool EnableAlmanacTranslation = true;
        public static bool AutoIndentAlmanac = true;
        // [FITUR BARU 0.13.7] Variabel UI untuk pengatur jarak Auto Indent
        public static float AutoIndentMultiplier = 0.40f;

        public static bool EnableAlmanacDumper = false;
        public static bool AlmanacZombieMode = false;

        public static string GoogleTargetLang = "en";
        private readonly string[] targetLangCodes = { "ar", "en", "id", "ja", "ko", "ms", "ru", "es", "su" };
        private readonly string[] targetLangNames = { "Arabic", "English", "Indonesian", "Japanese", "Korean", "Malay", "Russian", "Spanish", "Sundanese" };

        private readonly string[] currencyCodes = { "USD", "IDR", "EUR", "CNY", "JPY", "GBP" };
        private readonly string[] currencyNames = { "USD ($)", "IDR (Rp)", "EUR (€)", "CNY (¥)", "JPY (¥)", "GBP (£)" };

        private Rect windowRect;
        private bool isWindowInitialized = false;
        private Vector2 scrollPosition = Vector2.zero;

        private bool showFileBrowser = false;
        private Rect browserRect;
        private string currentDirectory = "C:\\";
        private Vector2 fileScroll = Vector2.zero;

        private int queueTextCount = 0;
        private int translatedTextCount = 0;
        private int plantDumpCount = 0;
        private int zombieDumpCount = 0;
        private float statusUpdateTimer = 0f;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F12))
            {
                IsOpen = !IsOpen;
                if (IsOpen) UpdateStatusCounts();
                else showFileBrowser = false;
            }

            if (Input.GetKeyDown(KeyCode.F5)) ReloadTranslationsCommand();
            if (Input.GetKeyDown(KeyCode.F6)) ReloadTexturesCommand();

            if (IsOpen)
            {
                statusUpdateTimer -= Time.unscaledDeltaTime;
                if (statusUpdateTimer <= 0)
                {
                    statusUpdateTimer = 2f;
                    UpdateStatusCounts();
                }
            }
        }

        private void ReloadTranslationsCommand()
        {
            Main.Log.LogInfo("[Command] Reloading translation dictionary from JSON files...");
            TranslationManager.LoadTranslations();
            FontManager.LoadCustomFont();
            if (TextPatch.TranslatedTextCache != null) TextPatch.TranslatedTextCache.Clear();
            TextPatch.RefreshAllTexts();
            UpdateStatusCounts();
            LanguageMenu.CreateNotificationUI("Translation Reloaded!", Color.green);
        }

        private void ReloadTexturesCommand()
        {
            Main.Log.LogInfo("[Command] Memuat ulang tekstur kustom dari file PNG...");
            TextureManager.LoadCustomSprites();
            ImagePatch.RefreshAllImages();
            LanguageMenu.CreateNotificationUI("Texture Reloaded!", Color.green);
        }

        private void UpdateStatusCounts()
        {
            queueTextCount = GetJsonEntryCount(System.IO.Path.Combine(FileManager.DumpsFolder, "untranslation_strings.json"), "\": \"");
            plantDumpCount = GetJsonEntryCount(System.IO.Path.Combine(FileManager.DumpsFolder, "untranslation_lawnstrings.json"), "\"id\":");
            zombieDumpCount = GetJsonEntryCount(System.IO.Path.Combine(FileManager.DumpsFolder, "untranslation_zombiesstrings.json"), "\"id\":");
            string translationPath = System.IO.Path.Combine(FileManager.LocalizationFolder, LanguageMenu.CurrentLanguage, "Strings", "translation_strings.json");
            translatedTextCount = GetJsonEntryCount(translationPath, "\": \"");
        }

        private int GetJsonEntryCount(string filePath, string keyToCount)
        {
            if (!System.IO.File.Exists(filePath)) return 0;
            try
            {
                string content = System.IO.File.ReadAllText(filePath);
                int count = 0, index = 0;
                while ((index = content.IndexOf(keyToCount, index)) != -1)
                {
                    count++;
                    index += keyToCount.Length;
                }
                return count;
            }
            catch { return 0; }
        }

        void OnGUI()
        {
            if (!IsOpen) return;

            if (!isWindowInitialized)
            {
                float windowHeight = Mathf.Min(800f, Screen.height - 40f);
                windowRect = new Rect(Screen.width - 380, 20, 360, windowHeight);
                isWindowInitialized = true;
            }

            GUI.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            windowRect = GUI.Window(999, windowRect, DrawWindow, "PvZ Ecology Translator");

            if (showFileBrowser)
            {
                if (browserRect.width == 0) browserRect = new Rect(windowRect.x - 410, windowRect.y, 400, 500);
                GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
                browserRect = GUI.Window(998, browserRect, DrawFileBrowser, "Select Custom Font (.ttf / .otf / .bundle)");
            }
        }

        void DrawWindow(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, windowRect.width - 35, 20));
            if (GUI.Button(new Rect(windowRect.width - 30, 2, 25, 20), "X")) IsOpen = false;

            GUILayout.Space(10);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false);

            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Mod Version: {Main.ModVersion}", GUILayout.Width(150));
            GUILayout.FlexibleSpace();
            GUILayout.Label("Game Version: " + Application.version, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight });
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUIStyle langStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            langStyle.normal.textColor = Color.green;
            GUILayout.Label("Active Language : " + LanguageMenu.CurrentLanguage, langStyle);
            GUILayout.EndVertical();
            GUILayout.Space(10);

            GUILayout.BeginVertical("box");
            DrawSectionHeader("---- Custom Font Settings ----");
            if (GUILayout.Button("Browse Custom Font...", GUILayout.Height(35)))
            {
                showFileBrowser = !showFileBrowser;
                if (showFileBrowser) currentDirectory = System.IO.Directory.GetCurrentDirectory();
            }
            if (FontManager.IsFontLoaded)
            {
                string fontName = FontManager.CustomFont != null ? FontManager.CustomFont.name : "None";
                string tmpStatus = FontManager.CustomTMPFont != null ? " (+TMPro)" : "";
                GUILayout.Label($"Active Font: {fontName} {tmpStatus}", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, normal = new GUIStyleState() { textColor = Color.green } });
            }
            else
            {
                GUILayout.Label("No custom font loaded.", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, normal = new GUIStyleState() { textColor = Color.gray } });
            }
            GUILayout.EndVertical();
            GUILayout.Space(10);

            GUILayout.BeginVertical("box");
            DrawSectionHeader("---- Command Panel ----");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload\nTranslation [F5]", GUILayout.Height(40))) ReloadTranslationsCommand();
            if (GUILayout.Button("Reload\nTexture [F6]", GUILayout.Height(40))) ReloadTexturesCommand();
            GUI.enabled = false;
            GUILayout.Button("Manual\nHook Text [F7]", GUILayout.Height(40));
            GUILayout.EndHorizontal();
            GUI.enabled = true;
            GUILayout.EndVertical();
            GUILayout.Space(10);

            GUILayout.BeginVertical("box");
            DrawSectionHeader("---- Text Translation Settings ----");
            bool prevTranslationState = LanguageToggle.IsTranslationEnabled;
            LanguageToggle.IsTranslationEnabled = DrawToggle("Enable Text Translation", LanguageToggle.IsTranslationEnabled);
            if (LanguageToggle.IsTranslationEnabled != prevTranslationState) { Main.SaveConfigs(); TextPatch.RefreshAllTexts(); }

            bool prevAlmanacTransState = EnableAlmanacTranslation;
            EnableAlmanacTranslation = DrawToggle("Enable Almanac Translation", EnableAlmanacTranslation);
            if (EnableAlmanacTranslation != prevAlmanacTransState)
            {
                Main.SaveConfigs();
                TranslationManager.LoadTranslations();
                TextPatch.RefreshAllTexts();
            }

            bool prevAutoIndentState = AutoIndentAlmanac;
            AutoIndentAlmanac = DrawToggle("Auto Indent Almanac Text", AutoIndentAlmanac);
            if (AutoIndentAlmanac != prevAutoIndentState)
            {
                Main.SaveConfigs();
                TranslationManager.LoadTranslations();
                TextPatch.RefreshAllTexts();
            }

            // [FITUR BARU 0.13.7] Tombol plus-minus untuk mengatur jarak indentasi
            if (AutoIndentAlmanac)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("   ↳ Indent Space Width", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Normal }, GUILayout.Width(170));
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("-", GUILayout.Width(30)))
                {
                    AutoIndentMultiplier -= 0.02f;
                    if (AutoIndentMultiplier < 0.1f) AutoIndentMultiplier = 0.1f;
                    Main.SaveConfigs(); TranslationManager.LoadTranslations(); TextPatch.RefreshAllTexts();
                }

                GUILayout.Label(AutoIndentMultiplier.ToString("0.00"), new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter }, GUILayout.Width(45));

                if (GUILayout.Button("+", GUILayout.Width(30)))
                {
                    AutoIndentMultiplier += 0.02f;
                    if (AutoIndentMultiplier > 1.5f) AutoIndentMultiplier = 1.5f;
                    Main.SaveConfigs(); TranslationManager.LoadTranslations(); TextPatch.RefreshAllTexts();
                }
                GUILayout.EndHorizontal();
            }

            bool prevOverrideState = EnableUIOverrides;
            EnableUIOverrides = DrawToggle("Enable UI Overrides", EnableUIOverrides);
            if (EnableUIOverrides != prevOverrideState) { Main.SaveConfigs(); TextPatch.RefreshAllTexts(); }

            GUILayout.EndVertical();
            GUILayout.Space(10);

            GUILayout.BeginVertical("box");
            DrawSectionHeader("---- Image & Texture Settings ----");
            bool prevImageState = TextureManager.EnableImageTranslation;
            TextureManager.EnableImageTranslation = DrawToggle("1. Texture Translation", TextureManager.EnableImageTranslation);
            if (TextureManager.EnableImageTranslation != prevImageState)
            {
                Main.SaveConfigs();
                if (TextureManager.EnableImageTranslation) TextureManager.LoadCustomSprites();
                ImagePatch.RefreshAllImages();
            }
            GUILayout.Space(5);
            GUILayout.Label("2. Texture Dumper:", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            bool prevTextureDumpState = TextureManager.EnableTextureDumper;
            TextureManager.EnableTextureDumper = DrawToggle("   ↳ Normal Scanning (Auto)", TextureManager.EnableTextureDumper);
            if (TextureManager.EnableTextureDumper != prevTextureDumpState)
            {
                Main.SaveConfigs();
                if (TextureManager.EnableTextureDumper) ImagePatch.RefreshAllImages();
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("   ↳ Advanced Scanning (Mass)", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold }, GUILayout.Width(220));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("DUMP NOW", new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold }, GUILayout.Width(90))) TextureManager.DumpAllLoadedSprites();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(10);

            GUILayout.BeginVertical("box");
            DrawSectionHeader("---- Almanac Dumping ----");

            bool prevAlmanacState = EnableAlmanacDumper;
            EnableAlmanacDumper = DrawToggle("Enable Almanac Dumper", EnableAlmanacDumper);
            if (EnableAlmanacDumper != prevAlmanacState) Main.SaveConfigs();

            if (EnableAlmanacDumper)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("   ↳ Dump Target", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Normal }, GUILayout.Width(160));
                GUILayout.FlexibleSpace();

                string targetLabel = AlmanacZombieMode ? "ZOMBIES ►" : "PLANTS ►";
                if (GUILayout.Button(targetLabel, new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter }, GUILayout.Width(100)))
                {
                    AlmanacZombieMode = !AlmanacZombieMode;
                    Main.SaveConfigs();

                    Color notifColor = AlmanacZombieMode ? new Color(1f, 0.4f, 0.4f) : Color.green;
                    LanguageMenu.CreateNotificationUI($"Almanac Target: {(AlmanacZombieMode ? "ZOMBIES" : "PLANTS")}", notifColor);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.Space(10);

            GUILayout.BeginVertical("box");
            DrawSectionHeader("---- Text Dumping ----");
            bool prevDumpState = EnableDumpedText;
            EnableDumpedText = DrawToggle("Enable Dumped Text (General)", EnableDumpedText);
            if (EnableDumpedText != prevDumpState)
            {
                TextDumper.EnableIMGUI = EnableDumpedText; TextDumper.EnableUGUI = EnableDumpedText;
                TextDumper.EnableNGUI = EnableDumpedText; TextDumper.EnableTextMesh = EnableDumpedText;
                TextDumper.EnableTMP = EnableDumpedText;
                Main.SaveConfigs();
                if (EnableDumpedText) TextPatch.RefreshAllTexts();
            }
            bool prevPathState = PathDetector.IsEnabled;
            PathDetector.IsEnabled = DrawToggle("Enable Text Path Logging", PathDetector.IsEnabled);
            if (PathDetector.IsEnabled != prevPathState) Main.SaveConfigs();
            bool prevAdvPathState = PathDetector.IsAdvanced;
            PathDetector.IsAdvanced = DrawToggle("Enable Adv. Path Logging", PathDetector.IsAdvanced);
            if (PathDetector.IsAdvanced != prevAdvPathState) Main.SaveConfigs();
            bool prevRegexState = EnableRegex;
            EnableRegex = DrawToggle("Enable Regex Text Detection", EnableRegex);
            if (EnableRegex != prevRegexState) Main.SaveConfigs();
            GUILayout.EndVertical();
            GUILayout.Space(10);

            GUILayout.BeginVertical("box");
            DrawSectionHeader("---- Text Detection ----");
            TextDumper.EnableIMGUI = DrawToggle("Enable IMGUI", TextDumper.EnableIMGUI);
            TextDumper.EnableUGUI = DrawToggle("Enable UGUI", TextDumper.EnableUGUI);
            TextDumper.EnableNGUI = DrawToggle("Enable NGUI", TextDumper.EnableNGUI);
            TextDumper.EnableTextMesh = DrawToggle("Enable TextMesh", TextDumper.EnableTextMesh);
            TextDumper.EnableTMP = DrawToggle("Enable TextMeshPro", TextDumper.EnableTMP);
            DrawNAToggle("Enable FairyGUI");
            GUILayout.EndVertical();
            GUILayout.Space(10);

            GUILayout.BeginVertical("box");
            DrawSectionHeader("---- Special Features ----");
            bool prevAutoTranslateState = EnableAutoTranslate;
            EnableAutoTranslate = DrawToggle("Enable Auto Translate (Google)", EnableAutoTranslate);
            if (EnableAutoTranslate != prevAutoTranslateState) { Main.SaveConfigs(); if (EnableAutoTranslate) TextPatch.RefreshAllTexts(); }

            GUILayout.BeginHorizontal();
            GUILayout.Label("   ↳ Target Language", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Normal }, GUILayout.Width(160));
            GUILayout.FlexibleSpace();
            int currentIndex = System.Array.IndexOf(targetLangCodes, GoogleTargetLang);
            if (currentIndex == -1) currentIndex = 0;
            if (GUILayout.Button(targetLangNames[currentIndex] + " ►", new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter }, GUILayout.Width(100)))
            {
                currentIndex = (currentIndex + 1) % targetLangCodes.Length;
                GoogleTargetLang = targetLangCodes[currentIndex];
                Main.SaveConfigs();
                if (EnableAutoTranslate) TextPatch.RefreshAllTexts();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            bool prevCurrencyState = CurrencyConverter.EnableConversion;
            CurrencyConverter.EnableConversion = DrawToggle("Enable Currency Conversion", CurrencyConverter.EnableConversion);
            if (CurrencyConverter.EnableConversion != prevCurrencyState) { Main.SaveConfigs(); TextPatch.RefreshAllTexts(); }

            GUILayout.BeginHorizontal();
            GUILayout.Label("   ↳ Target Currency", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Normal }, GUILayout.Width(160));
            GUILayout.FlexibleSpace();
            int currIndex = System.Array.IndexOf(currencyCodes, CurrencyConverter.TargetCurrency);
            if (currIndex == -1) currIndex = 0;
            if (GUILayout.Button(currencyNames[currIndex] + " ►", new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter }, GUILayout.Width(100)))
            {
                currIndex = (currIndex + 1) % currencyCodes.Length;
                CurrencyConverter.TargetCurrency = currencyCodes[currIndex];
                Main.SaveConfigs();
                if (CurrencyConverter.EnableConversion) TextPatch.RefreshAllTexts();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(10);

            GUILayout.BeginVertical("box");
            DrawSectionHeader("---- Status ----");
            DrawStatusRow("Queue Text", queueTextCount.ToString());
            DrawStatusRow("Text Translated", translatedTextCount.ToString());
            DrawStatusRow("Plants Dumped", $"{plantDumpCount} / 57", 70f);
            DrawStatusRow("Zombies Dumped", $"{zombieDumpCount} / 44", 70f);
            GUILayout.EndVertical();

            GUILayout.EndScrollView();
        }

        void DrawFileBrowser(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, browserRect.width - 35, 20));
            if (GUI.Button(new Rect(browserRect.width - 30, 2, 25, 20), "X")) showFileBrowser = false;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Up", GUILayout.Width(50)))
            {
                try
                {
                    System.IO.DirectoryInfo parent = System.IO.Directory.GetParent(currentDirectory);
                    if (parent != null) currentDirectory = parent.FullName;
                }
                catch { }
            }
            GUILayout.Label(currentDirectory, new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.EndHorizontal();

            fileScroll = GUILayout.BeginScrollView(fileScroll, "box");

            try
            {
                if (string.IsNullOrEmpty(currentDirectory))
                {
                    foreach (string drive in System.IO.Directory.GetLogicalDrives())
                    {
                        if (GUILayout.Button(drive, GUILayout.Height(30))) currentDirectory = drive;
                    }
                }
                else
                {
                    string[] dirs = System.IO.Directory.GetDirectories(currentDirectory);
                    foreach (string dir in dirs)
                    {
                        if (GUILayout.Button($"[FOLDER] {System.IO.Path.GetFileName(dir)}", GUILayout.Height(30)))
                        {
                            currentDirectory = dir;
                            fileScroll = Vector2.zero;
                        }
                    }

                    string[] files = System.IO.Directory.GetFiles(currentDirectory);
                    foreach (string file in files)
                    {
                        string ext = System.IO.Path.GetExtension(file).ToLower();
                        if (ext == ".ttf" || ext == ".otf" || ext == ".bundle" || ext == ".unity3d")
                        {
                            if (GUILayout.Button(System.IO.Path.GetFileName(file), GUILayout.Height(30)))
                            {
                                string targetName = LanguageMenu.CurrentLanguage + ext;
                                string dest = System.IO.Path.Combine(FileManager.CustomFontsFolder, targetName);
                                System.IO.File.Copy(file, dest, true);

                                showFileBrowser = false;
                                FontManager.LoadCustomFont();
                                TextPatch.RefreshAllTexts();

                                string notifMsg = ext.Contains("bundle") ? $"Font '{targetName}' Applied (Full)!" : $"Font '{targetName}' Applied (TMPro Only)!";
                                LanguageMenu.CreateNotificationUI(notifMsg, Color.green);
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                GUILayout.Label($"Access Denied: {ex.Message}");
            }

            GUILayout.EndScrollView();
        }

        private void DrawSectionHeader(string title)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            GUILayout.Label(title, style);
            GUILayout.Space(5);
        }

        private bool DrawToggle(string label, bool value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold }, GUILayout.Width(240));
            GUILayout.FlexibleSpace();

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
            btnStyle.normal.textColor = value ? Color.green : Color.red;

            if (GUILayout.Button(value ? "ON" : "OFF", btnStyle, GUILayout.Width(50)))
            {
                value = !value;
                LanguageMenu.CreateNotificationUI($"{label} {(value ? "Enabled" : "Disabled")}", value ? Color.green : new Color(1f, 0.4f, 0.4f));
            }
            GUILayout.EndHorizontal();
            return value;
        }

        private void DrawNAToggle(string label)
        {
            GUILayout.BeginHorizontal();
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            labelStyle.normal.textColor = Color.gray;
            GUILayout.Label(label, labelStyle, GUILayout.Width(240));
            GUILayout.FlexibleSpace();
            GUIStyle naStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            naStyle.normal.textColor = Color.gray;
            GUILayout.Label("N/A", naStyle, GUILayout.Width(50));
            GUILayout.EndHorizontal();
        }

        private void DrawStatusRow(string label, string value, float width = 50f)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.FlexibleSpace();
            GUILayout.Label(value, new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold }, GUILayout.Width(width));
            GUILayout.EndHorizontal();
        }
    }
}