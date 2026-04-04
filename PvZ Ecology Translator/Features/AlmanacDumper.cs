#pragma warning disable IDE0270, IDE0079 // Menambahkan IDE0079 untuk membungkam Unnecessary Suppression

using UnityEngine;
using System;
using System.IO;
using System.Text;
using PvZEcologyTranslator.Managers;
using PvZEcologyTranslator.Patches;

namespace PvZEcologyTranslator.Features
{
    public static class AlmanacDumper
    {
        public static bool IsZombieMode = false;
        private static string lastScannedName = "";
        // [FITUR BARU] Menyimpan ingatan teks deskripsi untuk mengecek animasi mesin tik
        private static string lastDescText = "";
        private static string pendingDumpName = "";
        private static float dumpTimer = 0f;

        private static GameObject cachedNameObj;
        private static GameObject cachedDescObj;
        private static GameObject cachedCostLObj;
        private static GameObject cachedCostRObj;

        public static void Update()
        {
            if (!DeveloperMenu.EnableAlmanacDumper) return;

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.M))
            {
                DeveloperMenu.AlmanacZombieMode = !DeveloperMenu.AlmanacZombieMode;
                Main.SaveConfigs();

                string mode = DeveloperMenu.AlmanacZombieMode ? "ZOMBIES" : "PLANTS";
                Color notifColor = DeveloperMenu.AlmanacZombieMode ? new Color(1f, 0.4f, 0.4f) : Color.green;
                LanguageMenu.CreateNotificationUI($"Almanac Target: {mode}", notifColor);
                Main.Log.LogInfo($"[Almanac Dumper] Target dumping diubah ke: {mode}");
            }

            FindAlmanacObjects();

            if (cachedNameObj != null && cachedNameObj.activeInHierarchy)
            {
                string currentName = TextDumper.ExtractText(cachedNameObj);
                string currentDesc = TextDumper.ExtractText(cachedDescObj);

                if (!string.IsNullOrEmpty(currentName) && currentName != "NameText")
                {
                    // 1. Jika kita mengklik tanaman baru
                    if (currentName != lastScannedName)
                    {
                        lastScannedName = currentName;
                        pendingDumpName = currentName;
                        lastDescText = currentDesc;
                        dumpTimer = 1.0f; // Beri waktu 1 detik
                    }
                    // 2. [FIX 0.13.3] Jika nama sama, tapi deskripsi masih ngetik (berubah-ubah)!
                    else if (currentDesc != lastDescText)
                    {
                        lastDescText = currentDesc;
                        dumpTimer = 1.0f; // Tahan terus timernya sampai efek ngetiknya berhenti!
                    }
                }
            }
            else
            {
                lastScannedName = "";
                pendingDumpName = "";
                lastDescText = "";
                dumpTimer = 0f;
            }

            if (dumpTimer > 0)
            {
                dumpTimer -= Time.unscaledDeltaTime;
                if (dumpTimer <= 0 && !string.IsNullOrEmpty(pendingDumpName))
                {
                    DumpCurrentAlmanac(pendingDumpName);
                    pendingDumpName = "";
                }
            }
        }

        private static void FindAlmanacObjects()
        {
            if (cachedNameObj != null && cachedNameObj.activeInHierarchy) return;

            cachedNameObj = null; cachedDescObj = null; cachedCostLObj = null; cachedCostRObj = null;

            GameObject loader = GameObject.Find("Almanac_Loader");
            if (loader != null && loader.activeInHierarchy)
            {
                Transform tName = loader.transform.Find("NameText");
                if (tName != null) cachedNameObj = tName.gameObject;

                Transform tDesc = loader.transform.Find("Text1");
                if (tDesc != null) cachedDescObj = tDesc.gameObject;

                Transform tCostL = loader.transform.Find("Text2");
                if (tCostL != null) cachedCostLObj = tCostL.gameObject;

                Transform tCostR = loader.transform.Find("Text3");
                if (tCostR != null) cachedCostRObj = tCostR.gameObject;
            }
        }

        private static string GetOriginalText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            if (TextPatch.ReverseTranslationCache.TryGetValue(text, out string orig))
            {
                return orig;
            }
            return text;
        }

        private static void DumpCurrentAlmanac(string nameText)
        {
            string desc = TextDumper.ExtractText(cachedDescObj);
            string costL = TextDumper.ExtractText(cachedCostLObj);
            string costR = TextDumper.ExtractText(cachedCostRObj);

            nameText = GetOriginalText(nameText);
            desc = GetOriginalText(desc);
            costL = GetOriginalText(costL);
            costR = GetOriginalText(costR);

            SaveToJsonManual(nameText, desc, costL, costR);
        }

        private static void SaveToJsonManual(string name, string desc, string cost, string recharge)
        {
            try
            {
                string fileName = DeveloperMenu.AlmanacZombieMode ? "untranslation_zombiesstrings.json" : "untranslation_lawnstrings.json";
                string path = Path.Combine(FileManager.DumpsFolder, fileName);
                if (!File.Exists(path)) return;

                string jsonContent = File.ReadAllText(path, Encoding.UTF8);
                string cleanName = TextDumper.EscapeForJson(name);

                if (jsonContent.Contains($"\"name\": \"{cleanName}\"")) return;

                int newId = 1;
                int idIndex = 0;
                while ((idIndex = jsonContent.IndexOf("\"id\":", idIndex)) != -1) { newId++; idIndex += 5; }

                string newObj = $"\n    {{\n      \"id\": {newId},\n      \"name\": \"{cleanName}\",\n      \"description\": \"{TextDumper.EscapeForJson(desc)}\",\n      \"cost\": \"{TextDumper.EscapeForJson(cost)}\",\n      \"recharge\": \"{TextDumper.EscapeForJson(recharge)}\"\n    }}";

                int insertPos = jsonContent.LastIndexOf("]");
                if (insertPos != -1)
                {
                    if (jsonContent.Substring(jsonContent.LastIndexOf("[", insertPos) + 1, insertPos - jsonContent.LastIndexOf("[", insertPos) - 1).Trim().Length > 0)
                        newObj = "," + newObj;

                    jsonContent = jsonContent.Insert(insertPos, newObj + "\n  ");
                    File.WriteAllText(path, jsonContent, Encoding.UTF8);

                    string typeLabel = DeveloperMenu.AlmanacZombieMode ? "AlmanacZombie" : "AlmanacPlant";
                    Main.Log.LogInfo($"[{typeLabel} Added] {name}, successfully added to {fileName}");
                    LanguageMenu.CreateNotificationUI($"{typeLabel} Dumped: {name}", Color.green);
                }
            }
            catch (Exception ex) { Main.Log.LogError($"[Almanac Dumper] Gagal: {ex.Message}"); }
        }
    }
}