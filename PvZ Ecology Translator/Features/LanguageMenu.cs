#pragma warning disable IDE0031 // Menyembunyikan pesan "Null check can be simplified"

using UnityEngine;
using UnityEngine.UI;
using System.IO;
using BepInEx.Configuration; // Wajib ditambahkan untuk fitur Config
using PvZEcologyTranslator.Managers;
using PvZEcologyTranslator.Patches;

namespace PvZEcologyTranslator.Features
{
    // Class statis untuk menangani menu pemilihan bahasa di dalam game
    public static class LanguageMenu
    {
        // Variabel untuk menyimpan referensi objek UI buatan kita
        private static GameObject customDropdownBtn;
        private static GameObject dropdownListPanel;
        private static GameObject languageLabelObj;
        private static GameObject languageConfirmPanel;

        // Status apakah menu dropdown sedang terbuka atau tertutup
        public static bool isDropdownOpen = false;

        // Menyimpan bahasa yang sedang aktif
        public static string CurrentLanguage = "Chinese";

        // ==========================================================
        // VARIABEL KONFIGURASI (CONFIG)
        // ==========================================================
        public static ConfigFile ModConfig;
        public static ConfigEntry<string> ConfigLanguage;
        public static ConfigEntry<bool> ConfigUseCustomFont;
        public static ConfigEntry<int> ConfigLabelFontSize;
        public static ConfigEntry<float> ConfigLabelOffsetY;
        public static ConfigEntry<int> ConfigDialogTitleFontSize;

        // Fungsi untuk menginisialisasi dan membaca file config.ini
        public static void InitConfig()
        {
            // Menentukan lokasi file config.ini di dalam folder PvZ Ecology Translator
            string configPath = Path.Combine(FileManager.RootFolder, "config.ini");
            ModConfig = new ConfigFile(configPath, true);

            // Membuat kategori [General]
            ConfigLanguage = ModConfig.Bind("General", "Language", "Chinese", "Bahasa yang aktif dan digunakan saat ini.");
            ConfigUseCustomFont = ModConfig.Bind("General", "UseCustomFont", true, "Aktifkan (true) untuk menggunakan font kustom dari folder [Custom Fonts].");

            // Membuat kategori [UI Settings] untuk pengaturan tampilan mod
            ConfigLabelFontSize = ModConfig.Bind("UI Settings", "LabelFontSize", 40, "Ukuran teks label 'Language' di atas tombol menu.");
            ConfigLabelOffsetY = ModConfig.Bind("UI Settings", "LabelOffsetY", 70f, "Posisi vertikal (naik/turun) teks label 'Language'.");
            ConfigDialogTitleFontSize = ModConfig.Bind("UI Settings", "DialogTitleFontSize", 20, "Ukuran teks judul pada dialog konfirmasi bahasa.");

            // Mengatur bahasa saat ini sesuai dengan yang ada di file config
            CurrentLanguage = ConfigLanguage.Value;

            Main.Log.LogInfo($"[Language Menu] Config loaded successfully. Current Language: {CurrentLanguage}");
        }

        // Fungsi ini dipanggil setiap frame (Update) dari Main.cs
        public static void Update()
        {
            // Memuat Config jika belum dimuat (dijalankan sekali saat game mulai)
            if (ModConfig == null) InitConfig();

            // Mencari objek tombol Back bawaan game untuk dijadikan patokan posisi
            GameObject backButton = GameObject.Find("Canvas/MoreOption/Back");

            // 1. Jika tombol Back ada di layar dan menu kita belum dibuat, maka buat menunya
            if (backButton != null && backButton.activeInHierarchy)
            {
                if (customDropdownBtn == null) CreateDropdownMenu(backButton);
            }
            // 2. Jika menu opsi game ditutup, sembunyikan juga semua UI mod kita
            else if (backButton != null && !backButton.activeInHierarchy)
            {
                if (customDropdownBtn != null)
                {
                    customDropdownBtn.SetActive(false);
                    if (languageLabelObj != null) languageLabelObj.SetActive(false);
                    if (dropdownListPanel != null) dropdownListPanel.SetActive(false);
                    if (languageConfirmPanel != null) languageConfirmPanel.SetActive(false);
                    isDropdownOpen = false;
                }
            }
            // 3. Jika menu opsi game dibuka kembali, tampilkan lagi tombol dropdown kita
            else if (backButton != null && backButton.activeInHierarchy)
            {
                if (customDropdownBtn != null && !customDropdownBtn.activeSelf)
                {
                    customDropdownBtn.SetActive(true);
                    if (languageLabelObj != null)
                    {
                        languageLabelObj.SetActive(true);
                        languageLabelObj.transform.SetAsLastSibling();
                    }
                }
            }
        }

        // ==========================================================
        // LOGIKA MENU BAHASA (LANGUAGE MENU)
        // ==========================================================
        private static void CreateDropdownMenu(GameObject backButton)
        {
            // Menduplikat tombol Back asli
            customDropdownBtn = Object.Instantiate(backButton, backButton.transform.parent);
            customDropdownBtn.name = "LanguageDropdownMenu";

            // Menggeser posisi tombol duplikat ke sebelah kanan sejauh lebar tombol + 20 pixel
            RectTransform rt = customDropdownBtn.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x + rt.rect.width + 20, rt.anchoredPosition.y);

            // Menghapus fungsi klik bawaan game dan menggantinya dengan fungsi ToggleDropdown kita
            Button btn = customDropdownBtn.GetComponent<Button>();
            btn.onClick = new Button.ButtonClickedEvent();
            btn.onClick.AddListener(ToggleDropdown);

            // Mengambil komponen Text dari tombol asli untuk dijadikan referensi font
            Text referenceText = customDropdownBtn.GetComponentInChildren<Text>();

            // Membuat teks label ("Language") di atas tombol
            CreateLanguageLabel(rt, referenceText);

            // Memperbarui teks pada tombol (misal: "Chinese ▼")
            UpdateButtonText();

            Main.Log.LogInfo("[Language Menu] Dropdown button and label created successfully.");
        }

        private static void UpdateButtonText()
        {
            if (customDropdownBtn != null)
            {
                Text btnText = customDropdownBtn.GetComponentInChildren<Text>();
                if (btnText != null)
                {
                    string arrow = isDropdownOpen ? " ▼" : " ▲";
                    btnText.text = CurrentLanguage + arrow;
                }
            }
        }

        private static void CreateLanguageLabel(RectTransform buttonRt, Text referenceText)
        {
            languageLabelObj = new GameObject("LanguageLabelText");
            languageLabelObj.transform.SetParent(customDropdownBtn.transform.parent, false);

            Text labelTxt = languageLabelObj.AddComponent<Text>();

            int customFontSize = ConfigLabelFontSize.Value;
            float yOffset = ConfigLabelOffsetY.Value;
            Color textColor = Color.white;
            Color outlineColor = Color.black;

            if (referenceText != null)
            {
                labelTxt.font = referenceText.font;
                labelTxt.fontSize = customFontSize;
                labelTxt.color = textColor;
                labelTxt.alignment = TextAnchor.MiddleCenter;
                labelTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
                labelTxt.verticalOverflow = VerticalWrapMode.Overflow;
            }

            Outline outline = languageLabelObj.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(1, -1);

            RectTransform labelRt = languageLabelObj.GetComponent<RectTransform>();
            labelRt.sizeDelta = new Vector2(buttonRt.rect.width, 30);
            labelRt.anchoredPosition = new Vector2(buttonRt.anchoredPosition.x, buttonRt.anchoredPosition.y + yOffset);

            UpdateLanguageLabelText();
            languageLabelObj.transform.SetAsLastSibling();
        }

        private static void UpdateLanguageLabelText()
        {
            if (languageLabelObj == null) return;
            Text txt = languageLabelObj.GetComponent<Text>();

            if (CurrentLanguage == "Chinese") txt.text = "语言";
            else if (CurrentLanguage == "Indonesian") txt.text = "Bahasa:";
            else txt.text = "Language:";
        }

        public static void ToggleDropdown()
        {
            isDropdownOpen = !isDropdownOpen;

            if (dropdownListPanel == null && isDropdownOpen)
            {
                GenerateLanguageList();
            }

            if (dropdownListPanel != null)
            {
                dropdownListPanel.SetActive(isDropdownOpen);
                if (isDropdownOpen)
                {
                    dropdownListPanel.transform.SetAsLastSibling();
                }
            }

            UpdateButtonText();
        }

        private static void GenerateLanguageList()
        {
            dropdownListPanel = new GameObject("LanguageListPanel");
            dropdownListPanel.transform.SetParent(customDropdownBtn.transform, false);

            RectTransform listRt = dropdownListPanel.AddComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0.5f, 1);
            listRt.anchorMax = new Vector2(0.5f, 1);
            listRt.pivot = new Vector2(0.5f, 0);
            listRt.anchoredPosition = new Vector2(0, 10);

            Image bg = dropdownListPanel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0f);

            VerticalLayoutGroup vlg = dropdownListPanel.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.spacing = 8;

            CreateLanguageItem("Chinese");
            CreateLanguageItem("English");
            CreateLanguageItem("Indonesian");

            GameObject backButton = GameObject.Find("Canvas/MoreOption/Back");
            if (backButton != null)
            {
                float itemHeight = backButton.GetComponent<RectTransform>().rect.height;
                float itemWidth = backButton.GetComponent<RectTransform>().rect.width;
                listRt.sizeDelta = new Vector2(itemWidth + 20, (vlg.transform.childCount * (itemHeight + vlg.spacing)) + 10);
            }
        }

        private static void CreateLanguageItem(string langName)
        {
            GameObject backButton = GameObject.Find("Canvas/MoreOption/Back");
            if (backButton == null) return;

            GameObject item = Object.Instantiate(backButton, dropdownListPanel.transform);
            item.name = $"Item_{langName}";

            Text txt = item.GetComponentInChildren<Text>();
            if (txt != null) txt.text = langName;

            Button btn = item.GetComponent<Button>();
            btn.onClick = new Button.ButtonClickedEvent();
            btn.onClick.AddListener(() => OnLanguageSelected(langName));
        }

        private static void OnLanguageSelected(string langName)
        {
            if (CurrentLanguage == langName)
            {
                string msg = langName == "Indonesian" ? "Bahasa ini sudah digunakan!" : (langName == "Chinese" ? "该语言已在使用！" : "This language is already in use!");
                CreateNotificationUI(msg, Color.yellow);

                ToggleDropdown();
                return;
            }

            isDropdownOpen = false;
            dropdownListPanel.SetActive(false);
            UpdateButtonText();

            ShowConfirmDialog(langName);
        }

        private static void ShowConfirmDialog(string targetLang)
        {
            Main.Log.LogInfo($"[Language Menu] Memanggil dialog konfirmasi untuk bahasa: {targetLang}");

            GameObject backButton = GameObject.Find("Canvas/MoreOption/Back");
            if (backButton == null) return;

            Transform canvasTransform = backButton.transform.parent.parent;

            if (languageConfirmPanel == null)
            {
                Transform originalPanel = canvasTransform.Find("ConfirmPanel");
                if (originalPanel == null)
                {
                    Main.Log.LogWarning("[Language Menu] ConfirmPanel not found! Applying language directly.");
                    ApplyLanguage(targetLang);
                    return;
                }

                languageConfirmPanel = Object.Instantiate(originalPanel.gameObject, canvasTransform);
                languageConfirmPanel.name = "LanguageConfirmPanel";

                MonoBehaviour[] scripts = languageConfirmPanel.GetComponents<MonoBehaviour>();
                foreach (var script in scripts)
                {
                    if (script != null && script.GetType().Assembly.GetName().Name == "Assembly-CSharp")
                    {
                        Object.Destroy(script);
                    }
                }

                if (languageConfirmPanel.TryGetComponent<Animator>(out var anim))
                {
                    Object.Destroy(anim);
                }

                Transform titleInitTr = languageConfirmPanel.transform.Find("TitleText");
                if (titleInitTr != null && titleInitTr.TryGetComponent<RectTransform>(out var titleRect))
                {
                    float customTitleHeight = 110f;
                    float customTitleOffsetY = 35f;
                    int customFontSize = ConfigDialogTitleFontSize.Value;

                    titleRect.sizeDelta = new Vector2(titleRect.sizeDelta.x, customTitleHeight);
                    titleRect.anchoredPosition = new Vector2(titleRect.anchoredPosition.x, customTitleOffsetY);

                    Text[] initTexts = titleInitTr.GetComponentsInChildren<Text>(true);
                    foreach (Text t in initTexts)
                    {
                        t.alignment = TextAnchor.MiddleCenter;
                        t.horizontalOverflow = HorizontalWrapMode.Wrap;
                        t.verticalOverflow = VerticalWrapMode.Overflow;
                        t.fontSize = customFontSize;
                    }
                }
            }

            languageConfirmPanel.transform.localScale = Vector3.one;
            languageConfirmPanel.transform.localPosition = Vector3.zero;
            if (languageConfirmPanel.TryGetComponent<CanvasGroup>(out var cg))
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }

            string titleText = "Are you sure you want to change the language?";
            string confirmText = "Confirm";
            string cancelText = "Cancel";

            if (targetLang == "Chinese")
            {
                titleText = "确定要更改语言吗？";
                confirmText = "确定";
                cancelText = "取消";
            }
            else if (targetLang == "Indonesian")
            {
                titleText = "Apakah kamu yakin ingin mengubah bahasa?";
                confirmText = "Iya";
                cancelText = "Ga";
            }

            Transform titleTr = languageConfirmPanel.transform.Find("TitleText");
            Transform confirmTr = languageConfirmPanel.transform.Find("Confirm");
            Transform cancelTr = languageConfirmPanel.transform.Find("Cancel");

            Text[] allPanelTexts = languageConfirmPanel.GetComponentsInChildren<Text>(true);
            foreach (Text t in allPanelTexts)
            {
                if (titleTr != null && (t.transform == titleTr || t.transform.IsChildOf(titleTr)))
                {
                    t.text = titleText;
                }
                else if (confirmTr != null && (t.transform == confirmTr || t.transform.IsChildOf(confirmTr)))
                {
                    t.text = confirmText;
                }
                else if (cancelTr != null && (t.transform == cancelTr || t.transform.IsChildOf(cancelTr)))
                {
                    t.text = cancelText;
                }
                else
                {
                    t.text = "";
                }
            }

            if (confirmTr != null && confirmTr.TryGetComponent<Button>(out var cBtn))
            {
                cBtn.onClick = new Button.ButtonClickedEvent();
                cBtn.onClick.AddListener(() => ApplyLanguage(targetLang));
            }

            if (cancelTr != null && cancelTr.TryGetComponent<Button>(out var cBtn2))
            {
                cBtn2.onClick = new Button.ButtonClickedEvent();
                cBtn2.onClick.AddListener(() => languageConfirmPanel.SetActive(false));
            }

            languageConfirmPanel.SetActive(true);
            languageConfirmPanel.transform.SetAsLastSibling();
        }

        private static void ApplyLanguage(string langName)
        {
            Main.Log.LogInfo($"[Language Menu] Language changed from {CurrentLanguage} to {langName}");
            CurrentLanguage = langName;

            if (ModConfig != null)
            {
                ConfigLanguage.Value = langName;
                ModConfig.Save();
            }

            TranslationManager.LoadTranslations();
            TextureManager.LoadCustomSprites();

            if (languageConfirmPanel != null) languageConfirmPanel.SetActive(false);

            UpdateLanguageLabelText();
            UpdateButtonText();

            // [FIX 0.9.1] Memanggil pembaruan label bahasa pada TextureMenu yang baru dibuat
            TextureMenu.UpdateTextureLabelText();

            string msg = langName == "Indonesian" ? "Bahasa berhasil diubah!" : (langName == "Chinese" ? "语言更改成功！" : "Language changed successfully!");
            CreateNotificationUI(msg, Color.green);

            TextPatch.RefreshAllTexts();
            ImagePatch.RefreshAllImages();
        }

        // ==========================================================
        // FITUR: SISTEM NOTIFIKASI ELEGAN (FLOATING TEXT)
        // ==========================================================
        public static void CreateNotificationUI(string message, Color textColor)
        {
            GameObject notifCanvasObj = GameObject.Find("TranslatorNotifCanvas");
            if (notifCanvasObj == null)
            {
                notifCanvasObj = new GameObject("TranslatorNotifCanvas");
                Canvas canvas = notifCanvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 9999;

                CanvasScaler scaler = notifCanvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                Object.DontDestroyOnLoad(notifCanvasObj);
            }

            GameObject notifObj = new GameObject("NotifPanel");
            notifObj.transform.SetParent(notifCanvasObj.transform, false);

            Image bg = notifObj.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.85f);

            HorizontalLayoutGroup hlg = notifObj.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.padding = new RectOffset(30, 30, 15, 15);

            ContentSizeFitter csf = notifObj.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject textObj = new GameObject("NotifText");
            textObj.transform.SetParent(notifObj.transform, false);

            Text txt = textObj.AddComponent<Text>();
            txt.text = message;
            txt.color = textColor;
            txt.fontSize = 35;
            txt.alignment = TextAnchor.MiddleCenter;

            if (customDropdownBtn != null)
            {
                Text btnText = customDropdownBtn.GetComponentInChildren<Text>();
                if (btnText != null) txt.font = btnText.font;
            }
            if (txt.font == null) txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            Outline outline = textObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);

            RectTransform notifRt = notifObj.GetComponent<RectTransform>();
            notifRt.pivot = new Vector2(0.5f, 0.5f);
            notifRt.anchorMin = new Vector2(0.5f, 0.5f);
            notifRt.anchorMax = new Vector2(0.5f, 0.5f);
            notifRt.anchoredPosition = new Vector2(0, -150);

            notifObj.AddComponent<NotificationFader>();
        }
    }

    public class NotificationFader : MonoBehaviour
    {
        private float stayTimer = 1.5f;
        private CanvasGroup cg;
        private RectTransform rt;

        void Start()
        {
            cg = gameObject.AddComponent<CanvasGroup>();
            rt = GetComponent<RectTransform>();
        }

        void Update()
        {
            if (stayTimer > 0)
            {
                stayTimer -= Time.unscaledDeltaTime;
                rt.anchoredPosition += new Vector2(0, 10f * Time.unscaledDeltaTime);
            }
            else
            {
                cg.alpha -= Time.unscaledDeltaTime * 2f;
                rt.anchoredPosition += new Vector2(0, 25f * Time.unscaledDeltaTime);

                if (cg.alpha <= 0)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}