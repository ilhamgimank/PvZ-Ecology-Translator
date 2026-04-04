#pragma warning disable IDE0031, IDE0044

using UnityEngine;
using UnityEngine.UI;
using PvZEcologyTranslator.Managers;
using PvZEcologyTranslator.Patches;

namespace PvZEcologyTranslator.Features
{
    public static class TextureMenu
    {
        private static GameObject customTextureBtn;
        private static GameObject textureListPanel;
        private static GameObject textureLabelObj;
        private static bool isTextureDropdownOpen = false;

        public static void Update()
        {
            GameObject backButton = GameObject.Find("Canvas/MoreOption/Back");

            if (backButton != null && backButton.activeInHierarchy)
            {
                if (customTextureBtn == null) CreateTextureDropdownMenu(backButton);
            }
            else if (backButton != null && !backButton.activeInHierarchy)
            {
                if (customTextureBtn != null)
                {
                    customTextureBtn.SetActive(false);
                    if (textureLabelObj != null) textureLabelObj.SetActive(false);
                    if (textureListPanel != null) textureListPanel.SetActive(false);
                    isTextureDropdownOpen = false;
                }
            }
            else if (backButton != null && backButton.activeInHierarchy)
            {
                if (customTextureBtn != null && !customTextureBtn.activeSelf)
                {
                    customTextureBtn.SetActive(true);
                    if (textureLabelObj != null)
                    {
                        textureLabelObj.SetActive(true);
                        textureLabelObj.transform.SetAsLastSibling();
                    }
                }
            }
        }

        private static void CreateTextureDropdownMenu(GameObject backButton)
        {
            customTextureBtn = Object.Instantiate(backButton, backButton.transform.parent);
            customTextureBtn.name = "TextureDropdownMenu";

            RectTransform rt = customTextureBtn.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x - rt.rect.width - 20, rt.anchoredPosition.y);

            Button btn = customTextureBtn.GetComponent<Button>();
            btn.onClick = new Button.ButtonClickedEvent();
            btn.onClick.AddListener(ToggleTextureDropdown);

            Text referenceText = customTextureBtn.GetComponentInChildren<Text>();

            CreateTextureLabel(rt, referenceText);
            UpdateTextureButtonText();

            Main.Log.LogInfo("[Texture Menu] Texture Dropdown button and label created successfully.");
        }

        public static void UpdateTextureButtonText()
        {
            if (customTextureBtn != null)
            {
                Text btnText = customTextureBtn.GetComponentInChildren<Text>();
                if (btnText != null)
                {
                    string stateName = TextureManager.EnableImageTranslation ? "Modded Textures" : "Original Textures";
                    string arrow = isTextureDropdownOpen ? " ▼" : " ▲";
                    btnText.text = stateName + arrow;
                }
            }
        }

        private static void CreateTextureLabel(RectTransform buttonRt, Text referenceText)
        {
            textureLabelObj = new GameObject("TextureLabelText");
            textureLabelObj.transform.SetParent(customTextureBtn.transform.parent, false);

            Text labelTxt = textureLabelObj.AddComponent<Text>();

            if (referenceText != null)
            {
                labelTxt.font = referenceText.font;
                labelTxt.fontSize = LanguageMenu.ConfigLabelFontSize.Value;
                labelTxt.color = Color.white;
                labelTxt.alignment = TextAnchor.MiddleCenter;
                labelTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
                labelTxt.verticalOverflow = VerticalWrapMode.Overflow;
            }

            Outline outline = textureLabelObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1, -1);

            RectTransform labelRt = textureLabelObj.GetComponent<RectTransform>();
            labelRt.sizeDelta = new Vector2(buttonRt.rect.width, 30);
            labelRt.anchoredPosition = new Vector2(buttonRt.anchoredPosition.x, buttonRt.anchoredPosition.y + LanguageMenu.ConfigLabelOffsetY.Value);

            UpdateTextureLabelText();
            textureLabelObj.transform.SetAsLastSibling();
        }

        public static void UpdateTextureLabelText()
        {
            if (textureLabelObj == null) return;
            Text txt = textureLabelObj.GetComponent<Text>();

            if (LanguageMenu.CurrentLanguage == "Indonesian") txt.text = "Ganti Tekstur:";
            else if (LanguageMenu.CurrentLanguage == "Chinese") txt.text = "纹理更换器:";
            else txt.text = "Textures Changer:";
        }

        private static void ToggleTextureDropdown()
        {
            isTextureDropdownOpen = !isTextureDropdownOpen;

            if (textureListPanel == null && isTextureDropdownOpen)
            {
                GenerateTextureList();
            }

            if (textureListPanel != null)
            {
                textureListPanel.SetActive(isTextureDropdownOpen);
                if (isTextureDropdownOpen)
                {
                    textureListPanel.transform.SetAsLastSibling();
                }
            }

            UpdateTextureButtonText();
        }

        private static void GenerateTextureList()
        {
            textureListPanel = new GameObject("TextureListPanel");
            textureListPanel.transform.SetParent(customTextureBtn.transform, false);

            RectTransform listRt = textureListPanel.AddComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0.5f, 1);
            listRt.anchorMax = new Vector2(0.5f, 1);
            listRt.pivot = new Vector2(0.5f, 0);
            listRt.anchoredPosition = new Vector2(0, 10);

            Image bg = textureListPanel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0f);

            VerticalLayoutGroup vlg = textureListPanel.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.spacing = 8;

            CreateTextureItem("Original Textures", false);
            CreateTextureItem("Modded Textures", true);

            GameObject backButton = GameObject.Find("Canvas/MoreOption/Back");
            if (backButton != null)
            {
                float itemHeight = backButton.GetComponent<RectTransform>().rect.height;
                float itemWidth = backButton.GetComponent<RectTransform>().rect.width;
                listRt.sizeDelta = new Vector2(itemWidth + 20, (vlg.transform.childCount * (itemHeight + vlg.spacing)) + 10);
            }
        }

        private static void CreateTextureItem(string btnName, bool isModded)
        {
            GameObject backButton = GameObject.Find("Canvas/MoreOption/Back");
            if (backButton == null) return;

            GameObject item = Object.Instantiate(backButton, textureListPanel.transform);
            item.name = $"Item_{btnName.Replace(" ", "")}";

            Text txt = item.GetComponentInChildren<Text>();
            if (txt != null) txt.text = btnName;

            Button btn = item.GetComponent<Button>();
            btn.onClick = new Button.ButtonClickedEvent();
            btn.onClick.AddListener(() => OnTextureSelected(isModded));
        }

        private static void OnTextureSelected(bool isModded)
        {
            if (TextureManager.EnableImageTranslation == isModded)
            {
                string msg = LanguageMenu.CurrentLanguage == "Indonesian" ? "Opsi tekstur ini sudah aktif!" : (LanguageMenu.CurrentLanguage == "Chinese" ? "此纹理选项已处于活动状态！" : "This texture option is already active!");
                LanguageMenu.CreateNotificationUI(msg, Color.yellow);
                ToggleTextureDropdown();
                return;
            }

            isTextureDropdownOpen = false;
            textureListPanel.SetActive(false);

            TextureManager.EnableImageTranslation = isModded;
            Main.SaveConfigs();

            // [FITUR BARU 0.10.0] Memuat ulang (reload) tekstur dari disk SECARA OTOMATIS saat digeser ke Modded!
            // Kini kamu tidak perlu pencet F6 lagi setelah menaruh gambar baru ke folder terjemahan.
            if (isModded)
            {
                TextureManager.LoadCustomSprites();
            }

            UpdateTextureButtonText();
            ImagePatch.RefreshAllImages();

            string successMsg = LanguageMenu.CurrentLanguage == "Indonesian" ? "Tekstur berhasil diterapkan!" : (LanguageMenu.CurrentLanguage == "Chinese" ? "纹理应用成功！" : "Textures applied successfully!");
            LanguageMenu.CreateNotificationUI(successMsg, Color.green);
        }
    }
}