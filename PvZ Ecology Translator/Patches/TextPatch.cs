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
    // Kelas utama yang bertugas mencegat (hook) semua teks yang akan muncul di layar game
    public static class TextPatch
    {
        // Penanda agar sistem tidak melakukan looping terjemahan secara terus-menerus
        public static bool IsApplyingTranslation = false;

        // --- SISTEM CACHE MEMORI ---
        // Menyimpan teks asli bawaan game agar bisa dikembalikan saat refresh (F5)
        public static Dictionary<Component, string> OriginalTextCache = new Dictionary<Component, string>();
        // Menyimpan kamus kebalikan (Terjemahan -> Asli) untuk keperluan Almanac Dumper
        public static Dictionary<string, string> ReverseTranslationCache = new Dictionary<string, string>();
        // Menyimpan teks yang sudah diterjemahkan agar tidak perlu diproses ulang (menghemat CPU)
        public static HashSet<string> TranslatedTextCache = new HashSet<string>();

        private static int lastDictCount = -1;

        // Variabel untuk menahan spam notifikasi "Achievement" agar tidak muncul berkali-kali
        private static string lastAchievementNotif = "";
        private static float lastAchievementTime = 0f;

        // Memperbarui kamus kebalikan jika ada perubahan jumlah kosakata di TranslationManager
        private static void UpdateReverseCache()
        {
            if (TranslationManager.Translations != null && lastDictCount != TranslationManager.Translations.Count)
            {
                RebuildReverseCache();
            }
        }

        // Membangun ulang kamus kebalikan (Terjemahan sebagai Key, Asli sebagai Value)
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

        // Fungsi yang dipanggil oleh Main.cs untuk memasang "Alat Sadap" (Patch) ke dalam mesin Unity
        public static void PatchAll(Harmony harmony)
        {
            try
            {
                // 1. Membajak UGUI Text bawaan Unity lawas
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
                // 2. Membajak TextMesh 3D lawas (biasanya dipakai untuk teks di dunia 3D/Zombies)
                var setter = AccessTools.PropertySetter(typeof(TextMesh), "text");
                harmony.Patch(setter, prefix: new HarmonyMethod(typeof(TextPatch), nameof(TextMesh_Prefix)));
                Main.Log.LogInfo("[Hook] TextMesh hook applied.");
            }
            catch { }

            try
            {
                // 3. Membajak TextMeshPro (TMP) menggunakan metode Reflection agar tidak error jika game tidak punya TMP
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

        // --- KUMPULAN PREFIX (Fungsi yang berjalan tepat sebelum teks muncul di layar) ---
        private static void UGUI_Prefix(Text __instance, ref string value) => ProcessGeneric(__instance, ref value, "UGUI");
        private static void TextMesh_Prefix(TextMesh __instance, ref string value) => ProcessGeneric(__instance, ref value, "TextMesh");
        private static void TMP_Prefix(object __instance, ref string value) => ProcessGeneric(__instance as Component, ref value, "TextMeshPro");
        private static void NGUI_Prefix(object __instance, ref string value) => ProcessGeneric(__instance as Component, ref value, "NGUI");

        // --- KUMPULAN POSTFIX (Fungsi yang berjalan setelah komponen diaktifkan / Enable) ---
        private static void UGUI_OnEnable_Postfix(Text __instance)
        {
            if (__instance == null || string.IsNullOrEmpty(__instance.text)) return;
            string val = __instance.text;

            // Memanggil teks asli dari cache jika objek ini pernah direkam sebelumnya
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

        // =========================================================================
        // JANTUNG UTAMA MOD: Fungsi yang memproses semua logika terjemahan teks
        // =========================================================================
        private static void ProcessGeneric(Component comp, ref string value, string typeLabel, bool isRefresh = false)
        {
            // Abaikan teks kosong atau teks yang sedang kita manipulasi
            if (string.IsNullOrWhiteSpace(value) || IsApplyingTranslation) return;

            // Abaikan UI Notifikasi bawaan mod kita agar tidak ikut diterjemahkan
            if (comp != null && comp.gameObject.name == "NotifText") return;

            // [FIX] Filter pemblokir kata "Enabled", "Disabled", "Mod Version" dsb telah dihapus 
            // agar tidak memblokir UI tombol game bawaan secara tidak sengaja!

            UpdateReverseCache();

            // Jika teks yang masuk ternyata adalah teks hasil terjemahan, kembalikan ke teks aslinya
            // Ini sangat penting agar Almanak Dumper tetap bisa mengekstrak nama Mandarin aslinya
            if (ReverseTranslationCache.TryGetValue(value, out string trueOriginal))
            {
                value = trueOriginal;
            }

            // Simpan teks asli ke dalam memori Cache
            if (!isRefresh && comp != null)
            {
                OriginalTextCache[comp] = value;
            }

            // --- DETEKSI BUKU ALMANAK ---
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

            // Buang teks yang belum terjemah ke Dumps Folder (Kecuali Almanak, karena punya Dumper sendiri)
            if (DeveloperMenu.EnableDumpedText && !isAlmanac)
            {
                TextDumper.DumpText(value, typeLabel);
            }

            // Jika toggle terjemahan dimatikan (Alt+T), hentikan proses di sini
            if (!LanguageToggle.IsTranslationEnabled) return;

            string originalTextForReverse = value;
            bool wasTranslated = false;

            // 1. Cek di kamus utama (translation_strings.json)
            if (TranslationManager.Translations.ContainsKey(value))
            {
                value = TranslationManager.Translations[value];
                wasTranslated = true;
            }
            // 2. Cek di kamus dinamis Regex (translation_regexs.json)
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

                                // Terjemahkan variabel grup jika ada di kamus utama
                                if (TranslationManager.Translations.TryGetValue(groupVal, out string translatedGroupVal))
                                {
                                    groupVal = translatedGroupVal;
                                }

                                // Format {0} atau $1
                                result = result.Replace("{" + (i - 1).ToString() + "}", groupVal);
                                result = result.Replace("$" + i.ToString(), groupVal);
                            }

                            // Sistem Notifikasi Achievement Otomatis
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
            // 3. Terjemahan Otomatis via Google Translate API
            else if (DeveloperMenu.EnableAutoTranslate)
            {
                // Abaikan jika isinya cuma angka atau simbol agar Google tidak capek
                if (!System.Text.RegularExpressions.Regex.IsMatch(value.Trim(), @"^\d+\s*(fps|FPS)?$") &&
                    !System.Text.RegularExpressions.Regex.IsMatch(value.Trim(), @"^[0-9./\\]+$"))
                {
                    GoogleTranslator.TranslateText(value);
                }
            }

            // 4. Konversi Mata Uang Cerdas
            if (CurrencyConverter.EnableConversion && value.Contains("$"))
            {
                string convertedCurrency = CurrencyConverter.ConvertString(value, LanguageMenu.CurrentLanguage);
                if (convertedCurrency != value)
                {
                    value = convertedCurrency;
                    wasTranslated = true;
                }
            }

            // --- SISTEM PEMUAT FONT KUSTOM ---
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

            // --- EKSEKUSI UI OVERRIDES ---
            if (DeveloperMenu.EnableUIOverrides && comp != null)
            {
                string objPath = PathDetector.GetPath(comp.transform);

                // 1. Coba cari jalur aslinya persis (termasuk '(Clone)' jika ada)
                if (TranslationManager.UIOverrides.TryGetValue(objPath, out string overrideData))
                {
                    ApplyUIOverrides(comp, overrideData, ref value);
                    wasTranslated = true; // Anggap sebagai terjemahan agar masuk ke ReverseCache
                }
                else
                {
                    // 2. FALLBACK (SMART CHECK): Jika tidak ketemu, coba hapus (Clone) dan cari lagi!
                    string cleanPath = objPath.Replace("(Clone)", "").Trim();
                    if (cleanPath != objPath && TranslationManager.UIOverrides.TryGetValue(cleanPath, out overrideData))
                    {
                        ApplyUIOverrides(comp, overrideData, ref value);
                        wasTranslated = true;
                    }
                }
            }

            // Menyimpan teks ke Reverse Cache jika berhasil diproses
            if (wasTranslated)
            {
                ReverseTranslationCache[value] = originalTextForReverse;
            }
        }

        // =========================================================================
        // EKSEKUTOR UI OVERRIDES & OPSI NUKLIR (Senjata Modifikasi Layout)
        // =========================================================================
        private static void ApplyUIOverrides(Component comp, string overrideData, ref string textValue)
        {
            string[] props = overrideData.Split(';');
            foreach (string prop in props)
            {
                if (string.IsNullOrWhiteSpace(prop)) continue;

                // [FIX CRASH] Gunakan IndexOf agar spasi atau karakter '=' di teks replace tidak error
                int eqIndex = prop.IndexOf('=');
                if (eqIndex == -1) continue;

                string key = prop.Substring(0, eqIndex).Trim().ToLower();
                string valOriginal = prop.Substring(eqIndex + 1); // JANGAN DI-TRIM! Spasi utuh dipertahankan.
                string val = valOriginal.Trim().ToLower(); // Trim khusus untuk angka/boolean

                // -------------------------------------------------------------
                // [FITUR BARU] Manipulasi / Menimpa Teks secara Langsung
                // -------------------------------------------------------------
                if (key == "text")
                {
                    if (textValue != null) textValue = valOriginal.Replace("\\n", "\n");
                    continue;
                }
                else if (key == "replace" && valOriginal.Contains("|"))
                {
                    string[] rep = valOriginal.Split('|');
                    if (rep.Length == 2 && textValue != null)
                    {
                        // [FIX CRASH] Mencegah error "String cannot be of zero length"
                        if (!string.IsNullOrEmpty(rep[0]))
                        {
                            textValue = textValue.Replace(rep[0], rep[1].Replace("\\n", "\n"));
                        }
                    }
                    continue;
                }

                // 💣 [OPSI NUKLIR 1] Menghancurkan ContentSizeFitter!
                // Jika komponen ini ada, ia akan terus melawan pengaturan width/height kita. Eksekusi mati di tempat!
                if (key == "width" || key == "height" || key == "ignorelayout")
                {
                    ContentSizeFitter csf = comp.GetComponent<ContentSizeFitter>();
                    if (csf != null) Object.DestroyImmediate(csf);
                }

                // 💣 [OPSI NUKLIR 2] Melepaskan diri dari rantai Layout Group (ignoreLayout)
                // Memaksa tombol agar bisa dibesarkan bebas walau dikunci oleh Parent-nya.
                if (key == "ignorelayout" && bool.TryParse(val, out bool ignore))
                {
                    LayoutElement le = comp.gameObject.GetComponent<LayoutElement>();
                    if (le == null) le = comp.gameObject.AddComponent<LayoutElement>();
                    le.ignoreLayout = ignore;
                    continue;
                }

                // [Hack OneLine] Mencegah teks turun ke bawah dengan Non-Breaking Space
                if (key == "oneline" && bool.TryParse(val, out bool ol) && ol)
                {
                    if (textValue != null)
                    {
                        textValue = textValue.Replace(" ", "\u00A0");
                    }
                    continue;
                }
                // [Tab Size] Mengubah karakter tab \t menjadi rentetan spasi biasa
                else if (key == "tabsize" && int.TryParse(val, out int tSize))
                {
                    if (textValue != null && tSize >= 0)
                    {
                        textValue = textValue.Replace("\t", new string(' ', tSize));
                    }
                    continue;
                }
                // Memutar objek
                else if (key == "rotation" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float rot))
                {
                    comp.transform.localEulerAngles = new Vector3(comp.transform.localEulerAngles.x, comp.transform.localEulerAngles.y, rot);
                    continue;
                }
                // Menggeser objek secara fisik
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
                // Melebarkan/Meninggikan objek secara HARDCORE
                else if (key == "width" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float w))
                {
                    if (comp.transform is RectTransform rt)
                    {
                        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
                        rt.sizeDelta = new Vector2(w, rt.sizeDelta.y); // Paksa modifikasi di akar delta
                    }
                    continue;
                }
                else if (key == "height" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float h))
                {
                    if (comp.transform is RectTransform rt)
                    {
                        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
                        rt.sizeDelta = new Vector2(rt.sizeDelta.x, h); // Paksa modifikasi di akar delta
                    }
                    continue;
                }
                // [Opsi Nuklir 3] Manipulasi ukuran visual secara brutal menembus batas Layout & RectMask2D
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

                // -------------------------------------------------------------
                // PENGATURAN KHUSUS KOMPONEN TEKS (Size, BestFit, Wrap, dll)
                // -------------------------------------------------------------
                if (comp is Text uiText)
                {
                    if (key == "size" && int.TryParse(val, out int size)) uiText.fontSize = size;
                    else if (key == "bestfit" && bool.TryParse(val, out bool bf)) uiText.resizeTextForBestFit = bf;
                    else if (key == "maxsize" && int.TryParse(val, out int maxs)) uiText.resizeTextMaxSize = maxs;
                    else if (key == "minsize" && int.TryParse(val, out int mins)) uiText.resizeTextMinSize = mins;

                    // Mencegah teks terpotong sebagian di bagian atas/bawah (Truncate Killer)
                    else if (key == "voverflow" && bool.TryParse(val, out bool vo)) uiText.verticalOverflow = vo ? VerticalWrapMode.Overflow : VerticalWrapMode.Truncate;

                    // Mencegah teks turun ke baris baru
                    else if (key == "nowrap" && bool.TryParse(val, out bool nw))
                    {
                        uiText.horizontalOverflow = nw ? HorizontalWrapMode.Overflow : HorizontalWrapMode.Wrap;
                        // Jika nowrap nyala, kita harus mematikan bestfit dan menyalakan vertical overflow
                        // agar teks bisa melintas bebas keluar batas tanpa disensor
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
                            Main.Log.LogWarning($"[UI Overrides] Unity Bug: 'linespacing' pada {comp.name} tidak akan berefek jika 'bestfit' menyala!");
                        }
                    }
                    else if (key == "color" && ColorUtility.TryParseHtmlString(valOriginal, out Color c)) uiText.color = c;
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
                    else if (key == "color" && ColorUtility.TryParseHtmlString(valOriginal, out Color c)) legacy3DText.color = c;
                    else if (key == "align")
                    {
                        if (val == "center") legacy3DText.alignment = TextAlignment.Center;
                        else if (val == "left") legacy3DText.alignment = TextAlignment.Left;
                        else if (val == "right") legacy3DText.alignment = TextAlignment.Right;
                    }
                }
                else if (comp.GetType().Name.Contains("TextMeshPro"))
                {
                    // TMPro diproses dengan Reflection karena DLL-nya seringkali terpisah
                    var type = comp.GetType();
                    if (key == "size" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float sizeTMP)) type.GetProperty("fontSize")?.SetValue(comp, sizeTMP, null);
                    else if (key == "bestfit" && bool.TryParse(val, out bool bfTMP)) type.GetProperty("enableAutoSizing")?.SetValue(comp, bfTMP, null);
                    else if (key == "maxsize" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float maxsTMP)) type.GetProperty("fontSizeMax")?.SetValue(comp, maxsTMP, null);
                    else if (key == "minsize" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float minsTMP)) type.GetProperty("fontSizeMin")?.SetValue(comp, minsTMP, null);

                    else if (key == "nowrap" && bool.TryParse(val, out bool nwTMP)) type.GetProperty("enableWordWrapping")?.SetValue(comp, !nwTMP, null);
                    else if (key == "wrap" && bool.TryParse(val, out bool wTMP)) type.GetProperty("enableWordWrapping")?.SetValue(comp, wTMP, null);

                    else if (key == "color" && ColorUtility.TryParseHtmlString(valOriginal, out Color cTMP)) type.GetProperty("color")?.SetValue(comp, cTMP, null);
                    else if (key == "linespacing" && float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float lsTMP))
                    {
                        // TMP menggunakan perhitungan lineSpacing yang berbeda dari UGUI
                        float additiveSpacing = (lsTMP - 1f) * 100f;
                        type.GetProperty("lineSpacing")?.SetValue(comp, additiveSpacing, null);
                    }
                }
            }
        }

        // =========================================================================
        // REFRESHER TEKS GLOBAL (DIPANGGIL SAAT TEKAN F5)
        // =========================================================================
        public static void RefreshAllTexts()
        {
            try
            {
                // 1. Membersihkan cache teks dari objek yang sudah hancur/hilang dari layar
                List<Component> deadKeys = new List<Component>();
                foreach (var key in OriginalTextCache.Keys)
                {
                    if (key == null) deadKeys.Add(key);
                }
                foreach (var k in deadKeys) OriginalTextCache.Remove(k);

                // 2. Refresh semua UGUI Text
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
                        else
                        {
                            // [UPDATE PENTING] Force Hard Refresh: Paksa Unity merefresh ukuran dan layout meski teks tidak berubah!
                            t.SetAllDirty();
                            if (t.transform is RectTransform rt) LayoutRebuilder.MarkLayoutForRebuild(rt);
                        }
                    }
                }

                // 3. Refresh semua TextMesh 3D
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

                // 4. Refresh semua TextMeshPro
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
                            else
                            {
                                // Force TMPro Hard Refresh
                                IsApplyingTranslation = true;
                                prop.SetValue(tmp, translated, null);
                                IsApplyingTranslation = false;
                            }
                        }
                    }
                }

                Main.Log.LogInfo("[TextPatch] Refresh Texts Complete. Hard Refresh & Layout Rebuild executed.");
            }
            catch (System.Exception ex)
            {
                Main.Log.LogWarning($"[TextPatch] Error during RefreshAllTexts: {ex.Message}");
            }
        }
    }
}