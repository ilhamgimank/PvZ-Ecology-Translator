using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using PvZEcologyTranslator.Managers;
using PvZEcologyTranslator.Patches;

namespace PvZEcologyTranslator.Features
{
    // Class ini di-attach ke game object untuk menjalankan Coroutine (Proses Asynchronous)
    public class GoogleTranslator : MonoBehaviour
    {
        // Antrean teks yang akan diterjemahkan agar tidak spam request ke Google sekaligus
        private static readonly Queue<string> translationQueue = new Queue<string>();
        private static bool isTranslating = false;

        public static void TranslateText(string originalText)
        {
            if (string.IsNullOrWhiteSpace(originalText)) return;

            // Masukkan ke antrean jika belum ada di antrean dan belum diterjemahkan
            if (!translationQueue.Contains(originalText) && !TranslationManager.Translations.ContainsKey(originalText))
            {
                translationQueue.Enqueue(originalText);
            }
        }

        void Update()
        {
            // Menjalankan antrean satu per satu jika fitur menyala
            if (DeveloperMenu.EnableAutoTranslate && translationQueue.Count > 0 && !isTranslating)
            {
                StartCoroutine(ProcessTranslation());
            }
        }

        private IEnumerator ProcessTranslation()
        {
            isTranslating = true;
            string targetText = translationQueue.Dequeue();

            // Cek lagi untuk memastikan
            if (TranslationManager.Translations.ContainsKey(targetText))
            {
                isTranslating = false;
                yield break;
            }

            // [FITUR BARU] Menggunakan bahasa target yang dipilih di menu Developer (tl={DeveloperMenu.GoogleTargetLang})
            // Sumber teks (Source) di-lock ke Chinese (sl=zh-CN)
            string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=zh-CN&tl={DeveloperMenu.GoogleTargetLang}&dt=t&q={UnityWebRequest.EscapeURL(targetText)}";

            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                yield return webRequest.SendWebRequest();

                // Menggunakan UnityWebRequest.Result untuk menghindari warning CS0618 (Obsolete)
                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    // Log Masalah Koneksi
                    Main.Log.LogError($"[Google Translate] Connection Issue: {webRequest.error}. Failed to translate text.");
                }
                else
                {
                    try
                    {
                        string response = webRequest.downloadHandler.text;

                        // Mengekstrak teks terjemahan dari format JSON balasan Google menggunakan Regex
                        Match match = Regex.Match(response, @"\[\[\[\""(.*?)\"",""");
                        if (match.Success)
                        {
                            string translated = match.Groups[1].Value;

                            // Membersihkan karakter escape JSON (seperti \n, \", dll)
                            translated = translated.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\");

                            // Masukkan ke kamus memori (agar langsung terganti di layar)
                            TranslationManager.Translations[targetText] = translated;

                            // Log Sukses
                            Main.Log.LogInfo($"[Google Translate] Success ({DeveloperMenu.GoogleTargetLang}): '{targetText}' -> '{translated}'");

                            // Menyimpan teks yang berhasil diterjemahkan ke file translation_strings.json
                            SaveTranslationToJson(targetText, translated);

                            // Meminta UI game menyegarkan teks agar hasil terjemahan langsung muncul
                            TextPatch.RefreshAllTexts();

                            // Otomatis mencatat ke file JSON Dump agar kamu bisa ngedit/permanenkan nanti
                            if (DeveloperMenu.EnableDumpedText)
                            {
                                TextDumper.DumpText(targetText, "GoogleAuto");
                            }
                        }
                        else
                        {
                            // Log Gagal jika format JSON dari Google berubah atau tidak cocok
                            Main.Log.LogWarning($"[Google Translate] Failed: Could not extract translation for '{targetText}'. Invalid response format.");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        // Log Gagal karena masalah internal sistem parsing
                        Main.Log.LogError($"[Google Translate] Failed (Parsing Error): {ex.Message}");
                    }
                }
            }

            // Jeda 0.5 detik agar IP kita tidak diblokir sementara oleh Google (Rate Limit protection)
            yield return new WaitForSeconds(0.5f);
            isTranslating = false;
        }

        // Fungsi baru untuk menyuntikkan teks terjemahan langsung ke dalam file JSON bahasa aktif
        private static void SaveTranslationToJson(string original, string translated)
        {
            try
            {
                // Mengambil bahasa yang saat ini sedang digunakan (misal: "Indonesian")
                string langFolder = LanguageMenu.CurrentLanguage;
                string path = System.IO.Path.Combine(FileManager.LocalizationFolder, langFolder, "Strings", "translation_strings.json");

                if (!System.IO.File.Exists(path)) return;

                string jsonContent = System.IO.File.ReadAllText(path, System.Text.Encoding.UTF8);
                string cleanOriginal = TextDumper.EscapeForJson(original);
                string cleanTranslated = TextDumper.EscapeForJson(translated);

                // Hindari duplikasi jika ternyata teks sudah ada di dalam file
                if (jsonContent.Contains($"\"{cleanOriginal}\":")) return;

                // Mencari posisi kurung kurawal pembuka '{' untuk menyisipkan teks di bagian atas
                int insertPos = jsonContent.IndexOf('{') + 1;
                if (insertPos > 0)
                {
                    // Cek apakah ini entri pertama di dalam JSON
                    bool isFirstEntry = jsonContent.IndexOf('}', insertPos) < jsonContent.IndexOf('"', insertPos) || jsonContent.IndexOf('"', insertPos) == -1;

                    // Beri koma jika sudah ada item lain di dalam JSON
                    string comma = isFirstEntry ? "" : ",";

                    // Format JSON {"Teks Asli": "Teks Terjemahan"}
                    string newEntry = $"\n  \"{cleanOriginal}\": \"{cleanTranslated}\"{comma}";

                    jsonContent = jsonContent.Insert(insertPos, newEntry);
                    System.IO.File.WriteAllText(path, jsonContent, System.Text.Encoding.UTF8);

                    Main.Log.LogInfo($"[Google Translate] Successfully saved '{cleanOriginal}' to {langFolder}/Strings/translation_strings.json");
                }
            }
            catch (System.Exception ex)
            {
                Main.Log.LogError($"[Google Translate] Failed to save to JSON file: {ex.Message}");
            }
        }
    }
}