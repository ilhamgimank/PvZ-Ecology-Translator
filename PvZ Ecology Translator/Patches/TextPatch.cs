#pragma warning disable IDE0079, IDE0270, IDE0060, IDE0018, CS0252

using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using PvZEcologyTranslator.Managers;
using PvZEcologyTranslator.Features;
using System.Collections.Generic;
using System.Globalization;

namespace PvZEcologyTranslator.Patches
{
    public static class TextPatch
    {
        public static bool IsApplyingTranslation = false;

        public static Dictionary<Component, string> OriginalTextCache = new Dictionary<Component, string>();
        public static Dictionary<string, string> ReverseTranslationCache = new Dictionary<string, string>();
        public static HashSet<string> TranslatedTextCache = new HashSet<string>();

        private static int lastDictCount = -1;

        private static string lastAchievementNotif = "";
        private static float lastAchievementTime = 0f;

        private static void UpdateReverseCache()
        {
            if (TranslationManager.Translations != null && lastDictCount != TranslationManager.Translations.Count)
            {
                RebuildReverseCache();
            }
        }

        public static void RebuildReverseCache()
        {
            ReverseTranslationCache.Clear();
            if (TranslationManager.Translations != null)
            {
                foreach (var kvp in TranslationManager.Translations)
                {
                    if (!string.IsNullOrWhiteSpace(kvp.Value))
                    {
                        ReverseTranslationCache[kvp.Value] = kvp.Key;
                    }
                }
            }
            lastDictCount = TranslationManager.Translations?.Count ?? 0;
        }

        public static void PatchAll(Harmony harmony)
        {
            try
            {
                var setter = AccessTools.PropertySetter(typeof(Text), "text");
                harmony.Patch(setter, prefix: new HarmonyMethod(typeof(TextPatch), nameof(UGUI_Prefix)));

                var onEnable = AccessTools.Method(typeof(Text), "OnEnable");
                if (onEnable == null)
                {
                    onEnable = AccessTools.Method(typeof(Graphic), "OnEnable");
                }

                if (onEnable != null)
                {
                    harmony.Patch(onEnable, postfix: new HarmonyMethod(typeof(TextPatch), nameof(UGUI_OnEnable_Postfix)));
                }
                Main.Log.LogInfo("[Hook] UGUI Text hooks applied.");
            }
            catch { Main.Log.LogWarning("[Hook] UGUI Text failed to patch."); }

            try
            {
                var setter = AccessTools.PropertySetter(typeof(TextMesh), "text");
                harmony.Patch(setter, prefix: new HarmonyMethod(typeof(TextPatch), nameof(TextMesh_Prefix)));
                Main.Log.LogInfo("[Hook] TextMesh hook applied.");
            }
            catch { }

            try
            {
                System.Type tmpType = AccessTools.TypeByName("TMPro.TMP_Text");
                if (tmpType != null)
                {
                    var setter = AccessTools.PropertySetter(tmpType, "text");
                    harmony.Patch(setter, prefix: new HarmonyMethod(typeof(TextPatch), nameof(TMP_Prefix)));

                    var onEnable = AccessTools.Method(tmpType, "OnEnable");
                    if (onEnable != null)
                    {
                        harmony.Patch(onEnable, postfix: new HarmonyMethod(typeof(TextPatch), nameof(TMP_OnEnable_Postfix)));
                    }
                    Main.Log.LogInfo("[Hook] TextMeshPro hooks applied.");
                }
            }
            catch { }
        }

        private static void UGUI_Prefix(Text __instance, ref string value) => ProcessGeneric(__instance, ref value, "UGUI");
        private static void TextMesh_Prefix(TextMesh __instance, ref string value) => ProcessGeneric(__instance, ref value, "TextMesh");
        private static void TMP_Prefix(object __instance, ref string value) => ProcessGeneric(__instance as Component, ref value, "TextMeshPro");
        private static void NGUI_Prefix(object __instance, ref string value) => ProcessGeneric(__instance as Component, ref value, "NGUI");

        private static void UGUI_OnEnable_Postfix(Text __instance)
        {
            if (__instance == null || string.IsNullOrEmpty(__instance.text)) return;
            string val = __instance.text;

            if (OriginalTextCache.TryGetValue(__instance, out string cached))
            {
                val = cached;
            }

            ProcessGeneric(__instance, ref val, "UGUI", false);

            if (__instance.text != val)
            {
                IsApplyingTranslation = true;
                __instance.text = val;
                IsApplyingTranslation = false;
            }
        }

        private static void TMP_OnEnable_Postfix(Component __instance)
        {
            if (__instance == null) return;
            var prop = __instance.GetType().GetProperty("text");
            if (prop != null)
            {
                string val = prop.GetValue(__instance, null) as string;
                if (string.IsNullOrEmpty(val)) return;

                if (OriginalTextCache.TryGetValue(__instance, out string cached))
                {
                    val = cached;
                }

                ProcessGeneric(__instance, ref val, "TextMeshPro", false);

                if (val != (prop.GetValue(__instance, null) as string))
                {
                    IsApplyingTranslation = true;
                    prop.SetValue(__instance, val, null);
                    IsApplyingTranslation = false;
                }
            }
        }

        private static void ProcessGeneric(Component comp, ref string value, string typeLabel, bool isRefresh = false)
        {
            if (string.IsNullOrWhiteSpace(value) || IsApplyingTranslation) return;

            if (comp != null && comp.gameObject.name == "NotifText") return;

            if (value.Contains("Enable ") || value.Contains("Disabled") || value.Contains("Enabled") ||
                value.Contains("Mod Version") || value.Contains("PvZ Ecology") || value.Contains("Active Language"))
            {
                return;
            }

            UpdateReverseCache();

            if (ReverseTranslationCache.TryGetValue(value, out string trueOriginal))
            {
                value = trueOriginal;
            }

            if (!isRefresh && comp != null)
            {
                OriginalTextCache[comp] = value;
            }

            bool isAlmanac = false;
            if (comp != null)
            {
                Transform t = comp.transform;
                while (t != null)
                {
                    string tName = t.name.ToLower();
                    if (tName.Contains("almanac") || tName.Contains("cardpage") || tName.Contains("seedbank"))
                    {
                        isAlmanac = true;
                        break;
                    }
                    t = t.parent;
                }
                if (!isAlmanac && comp.gameObject.scene.name != null && comp.gameObject.scene.name.ToLower().Contains("almanac")) isAlmanac = true;
            }

            if (DeveloperMenu.EnableDumpedText && !isAlmanac)
            {
                TextDumper.DumpText(value, typeLabel);
            }

            if (!LanguageToggle.IsTranslationEnabled) return;

            string originalTextForReverse = value;
            bool wasTranslated = false;

            if (TranslationManager.Translations.ContainsKey(value))
            {
                value = TranslationManager.Translations[value];
                wasTranslated = true;
            }
            else if (DeveloperMenu.EnableRegex && TranslationManager.RegexTranslations.Count > 0)
            {
                foreach (var regexEntry in TranslationManager.RegexTranslations)
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(value, regexEntry.Key))
                    {
                        value = System.Text.RegularExpressions.Regex.Replace(value, regexEntry.Key, m =>
                        {
                            string result = regexEntry.Value;
                            for (int i = 1; i < m.Groups.Count; i++)
                            {
                                string groupVal = m.Groups[i].Value;

                                if (TranslationManager.Translations.TryGetValue(groupVal, out string translatedGroupVal))
                                {
                                    groupVal = translatedGroupVal;
                                }

                                result = result.Replace("{" + (i - 1).ToString() + "}", groupVal);
                                result = result.Replace("$" + i.ToString(), groupVal);
                            }

                            if (!IsApplyingTranslation && !isRefresh && regexEntry.Key.Contains("成就"))
                            {
                                if (result != lastAchievementNotif || Time.unscaledTime - lastAchievementTime > 2f)
                                {
                                    lastAchievementNotif = result;
                                    lastAchievementTime = Time.unscaledTime;

                                    Features.LanguageMenu.CreateNotificationUI("🏆 " + result, new Color(1f, 0.84f, 0f));
                                }
                            }

                            return result;
                        });
                        wasTranslated = true;
                        break;
                    }
                }
            }
            else if (DeveloperMenu.EnableAutoTranslate)
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(value.Trim(), @"^\d+\s*(fps|FPS)?$") &&
                    !System.Text.RegularExpressions.Regex.IsMatch(value.Trim(), @"^[0-9./\\]+$"))
                {
                    GoogleTranslator.TranslateText(value);
                }
            }

            if (CurrencyConverter.EnableConversion && value.Contains("$"))
            {
                string convertedCurrency = CurrencyConverter.ConvertString(value, LanguageMenu.CurrentLanguage);
                if (convertedCurrency != value)
                {
                    value = convertedCurrency;
                    wasTranslated = true;
                }
            }

            if (FontManager.IsFontLoaded)
            {
                if (FontManager.CustomFont != null)
                {
                    if (comp is Text uiText && uiText.font != FontManager.CustomFont)
                    {
                        uiText.font = FontManager.CustomFont;
                    }
                    else if (comp is TextMesh txtMesh && txtMesh.font != FontManager.CustomFont)
                    {
                        txtMesh.font = FontManager.CustomFont;
                        Renderer r = txtMesh.GetComponent<Renderer>();
                        if (r != null && FontManager.CustomFont.material != null)
                        {
                            r.sharedMaterial = FontManager.CustomFont.material;
                        }
                    }
                }

                if (FontManager.CustomTMPFont != null && comp.GetType().Name.Contains("TextMeshPro"))
                {
                    var type = comp.GetType();
                    var fontProp = type.GetProperty("font");
                    if (fontProp != null)
                    {
                        var currentFont = fontProp.GetValue(comp, null);
                        if (currentFont as UnityEngine.Object != FontManager.CustomTMPFont)
                        {
                            fontProp.SetValue(comp, FontManager.CustomTMPFont, null);
                        }
                    }
                }
            }

            if (DeveloperMenu.EnableUIOverrides && comp != null)
            {
                string objPath = PathDetector.GetPath(comp.transform);
                if (TranslationManager.UIOverrides.TryGetValue(objPath, out string overrideData))
                {
                    // [UPDATE 0.14.5] Sekarang kita lempar "ref value" ke fungsi ini agar teksnya bisa dimanipulasi!
                    ApplyUIOverrides(comp, overrideData, ref value);
                    wasTranslated = true;
                }
            }

            if (wasTranslated)
            {
                ReverseTranslationCache[value] = originalTextForReverse;
            }
        }

        private static void ApplyUIOverrides(Component comp, string overrideData, ref string textValue)
        {
            string[] props = overrideData.Split(';');
            foreach (string prop in props)
            {
                if (string.IsNullOrWhiteSpace(prop)) continue;

                string[] kvp = prop.Split('=');
                if (kvp.Length != 2) continue;

                string key = kvp[0].Trim().ToLower();
                string val = kvp[1].Trim().ToLower();

                // [FITUR BARU 0.14.5] Ultimate Hack OneLine! (Mengganti Spasi dengan Non-Breaking Space)
                if (key == "oneline" && bool.TryParse(val, out bool ol) && ol)
                {
                    if (textValue != null)
                    {
                        textValue = textValue.Replace(" ", "\u00A0");
                    }
                    continue;
                }
                // [FITUR BARU 0.14.7] Pengatur Ukuran Tab (\t) menjadi spasi
                else if (key == "tabsize" && int.TryParse(val, out int tSize))
                {
                    if (textValue != null && tSize >= 0)
                    {
                        textValue = textValue.Replace("\t", new string(' ', tSize));
                    }
                    continue;
                }
                else if (key == "rotation" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float rot))
                {
                    comp.transform.localEulerAngles = new Vector3(comp.transform.localEulerAngles.x, comp.transform.localEulerAngles.y, rot);
                    continue;
                }
                else if (key == "posx" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float px))
                {
                    if (comp.transform is RectTransform rt) rt.anchoredPosition = new Vector2(px, rt.anchoredPosition.y);
                    else comp.transform.localPosition = new Vector3(px, comp.transform.localPosition.y, comp.transform.localPosition.z);
                    continue;
                }
                else if (key == "posy" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float py))
                {
                    if (comp.transform is RectTransform rt) rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, py);
                    else comp.transform.localPosition = new Vector3(comp.transform.localPosition.x, py, comp.transform.localPosition.z);
                    continue;
                }
                // [UPDATE 0.14.5] Fix Absolute Width: Kini tidak peduli apa Anchor-nya, ukurannya pasti melar secara presisi!
                else if (key == "width" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float w))
                {
                    if (comp.transform is RectTransform rt) rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
                    continue;
                }
                else if (key == "height" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float h))
                {
                    if (comp.transform is RectTransform rt) rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
                    continue;
                }
                // [FITUR BARU] Scale Override: Senjata pamungkas penembus Layout Group & Mask
                else if (key == "scalex" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float sx))
                {
                    comp.transform.localScale = new Vector3(sx, comp.transform.localScale.y, comp.transform.localScale.z);
                    continue;
                }
                else if (key == "scaley" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float sy))
                {
                    comp.transform.localScale = new Vector3(comp.transform.localScale.x, sy, comp.transform.localScale.z);
                    continue;
                }

                if (comp is Text uiText)
                {
                    if (key == "size" && int.TryParse(val, out int size)) uiText.fontSize = size;
                    else if (key == "bestfit" && bool.TryParse(val, out bool bf)) uiText.resizeTextForBestFit = bf;
                    else if (key == "maxsize" && int.TryParse(val, out int maxs)) uiText.resizeTextMaxSize = maxs;
                    else if (key == "minsize" && int.TryParse(val, out int mins)) uiText.resizeTextMinSize = mins;

                    // [FITUR BARU] Vertical Overflow: Mencegah teks terpotong secara vertikal
                    else if (key == "voverflow" && bool.TryParse(val, out bool vo)) uiText.verticalOverflow = vo ? VerticalWrapMode.Overflow : VerticalWrapMode.Truncate;

                    else if (key == "nowrap" && bool.TryParse(val, out bool nw))
                    {
                        uiText.horizontalOverflow = nw ? HorizontalWrapMode.Overflow : HorizontalWrapMode.Wrap;
                        if (nw)
                        {
                            uiText.verticalOverflow = VerticalWrapMode.Overflow;
                            uiText.resizeTextForBestFit = false;
                        }
                    }
                    else if (key == "wrap" && bool.TryParse(val, out bool w)) uiText.horizontalOverflow = w ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;

                    else if (key == "linespacing" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float ls))
                    {
                        uiText.lineSpacing = ls;
                        if (uiText.resizeTextForBestFit)
                        {
                            Main.Log.LogWarning($"[UI Overrides] Unity Bug: 'linespacing' on {comp.name} will not work if 'bestfit' is active!");
                        }
                    }
                    else if (key == "color" && ColorUtility.TryParseHtmlString(val, out Color c)) uiText.color = c;
                    else if (key == "align")
                    {
                        if (val == "center") uiText.alignment = TextAnchor.MiddleCenter;
                        else if (val == "left") uiText.alignment = TextAnchor.MiddleLeft;
                        else if (val == "right") uiText.alignment = TextAnchor.MiddleRight;
                    }
                }
                else if (comp is TextMesh legacy3DText)
                {
                    if (key == "size" && int.TryParse(val, out int size)) legacy3DText.fontSize = size;
                    else if (key == "charsize" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float cs)) legacy3DText.characterSize = cs;
                    else if (key == "linespacing" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float ls)) legacy3DText.lineSpacing = ls;
                    else if (key == "color" && ColorUtility.TryParseHtmlString(val, out Color c)) legacy3DText.color = c;
                    else if (key == "align")
                    {
                        if (val == "center") legacy3DText.alignment = TextAlignment.Center;
                        else if (val == "left") legacy3DText.alignment = TextAlignment.Left;
                        else if (val == "right") legacy3DText.alignment = TextAlignment.Right;
                    }
                }
                else if (comp.GetType().Name.Contains("TextMeshPro"))
                {
                    var type = comp.GetType();
                    if (key == "size" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float sizeTMP)) type.GetProperty("fontSize")?.SetValue(comp, sizeTMP, null);
                    else if (key == "bestfit" && bool.TryParse(val, out bool bfTMP)) type.GetProperty("enableAutoSizing")?.SetValue(comp, bfTMP, null);
                    else if (key == "maxsize" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float maxsTMP)) type.GetProperty("fontSizeMax")?.SetValue(comp, maxsTMP, null);
                    else if (key == "minsize" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float minsTMP)) type.GetProperty("fontSizeMin")?.SetValue(comp, minsTMP, null);

                    else if (key == "nowrap" && bool.TryParse(val, out bool nwTMP)) type.GetProperty("enableWordWrapping")?.SetValue(comp, !nwTMP, null);
                    else if (key == "wrap" && bool.TryParse(val, out bool wTMP)) type.GetProperty("enableWordWrapping")?.SetValue(comp, wTMP, null);

                    else if (key == "color" && ColorUtility.TryParseHtmlString(val, out Color cTMP)) type.GetProperty("color")?.SetValue(comp, cTMP, null);
                    else if (key == "linespacing" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float lsTMP))
                    {
                        float additiveSpacing = (lsTMP - 1f) * 100f;
                        type.GetProperty("lineSpacing")?.SetValue(comp, additiveSpacing, null);
                    }
                }
            }
        }

        public static void RefreshAllTexts()
        {
            try
            {
                List<Component> deadKeys = new List<Component>();
                foreach (var key in OriginalTextCache.Keys)
                {
                    if (key == null) deadKeys.Add(key);
                }
                foreach (var k in deadKeys) OriginalTextCache.Remove(k);

                Text[] allTexts = Resources.FindObjectsOfTypeAll<Text>();
                foreach (Text t in allTexts)
                {
                    if (t == null || t.gameObject.scene.name == null) continue;

                    string orig = "";
                    if (!OriginalTextCache.TryGetValue(t, out orig))
                    {
                        orig = t.text;
                        if (!string.IsNullOrEmpty(orig)) OriginalTextCache[t] = orig;
                    }

                    if (!string.IsNullOrEmpty(orig))
                    {
                        string translated = orig;
                        ProcessGeneric(t, ref translated, "UGUI", true);
                        if (t.text != translated)
                        {
                            IsApplyingTranslation = true;
                            t.text = translated;
                            IsApplyingTranslation = false;
                        }
                    }
                }

                TextMesh[] allTMs = Resources.FindObjectsOfTypeAll<TextMesh>();
                foreach (TextMesh tm in allTMs)
                {
                    if (tm == null || tm.gameObject.scene.name == null) continue;

                    string orig = "";
                    if (!OriginalTextCache.TryGetValue(tm, out orig))
                    {
                        orig = tm.text;
                        if (!string.IsNullOrEmpty(orig)) OriginalTextCache[tm] = orig;
                    }

                    if (!string.IsNullOrEmpty(orig))
                    {
                        string translated = orig;
                        ProcessGeneric(tm, ref translated, "TextMesh", true);
                        if (tm.text != translated)
                        {
                            IsApplyingTranslation = true;
                            tm.text = translated;
                            IsApplyingTranslation = false;
                        }
                    }
                }

                System.Type tmpType = AccessTools.TypeByName("TMPro.TMP_Text");
                if (tmpType != null)
                {
                    UnityEngine.Object[] allTMPs = Resources.FindObjectsOfTypeAll(tmpType);
                    foreach (UnityEngine.Object tmpObj in allTMPs)
                    {
                        Component tmp = tmpObj as Component;
                        if (tmp == null || tmp.gameObject.scene.name == null) continue;

                        if (!tmp.gameObject.activeInHierarchy) continue;

                        var prop = tmpType.GetProperty("text");
                        if (prop == null) continue;

                        string orig = "";
                        if (!OriginalTextCache.TryGetValue(tmp, out orig))
                        {
                            orig = prop.GetValue(tmp, null) as string;
                            if (!string.IsNullOrEmpty(orig)) OriginalTextCache[tmp] = orig;
                        }

                        if (!string.IsNullOrEmpty(orig))
                        {
                            string translated = orig;
                            ProcessGeneric(tmp, ref translated, "TextMeshPro", true);

                            string currentText = prop.GetValue(tmp, null) as string;
                            if (currentText != translated)
                            {
                                IsApplyingTranslation = true;
                                prop.SetValue(tmp, translated, null);
                                IsApplyingTranslation = false;
                            }
                        }
                    }
                }

                Main.Log.LogInfo("[TextPatch] Refresh Texts Complete. Reverse Caching Active.");
            }
            catch (System.Exception ex)
            {
                Main.Log.LogWarning($"[TextPatch] Error during RefreshAllTexts: {ex.Message}");
            }
        }
    }
}