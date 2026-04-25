#pragma warning disable IDE0270 

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

namespace PvZEcologyTranslator.Features
{
    public static class PathDetector
    {
        public static bool IsEnabled = true;
        public static bool IsAdvanced = false;

        public static string LastScannedSpriteName = "";

        // Menambahkan variabel penampung untuk rangkuman hasil akhir
        public static string PickedSpriteName = "";
        public static string PickedPath = "";
        public static string PickedObjectName = "";

        public static void HandleInput()
        {
            if (!IsEnabled) return;

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButtonDown(1))
            {
                ScanObjectUnderMouse();
            }

            if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetMouseButtonDown(1))
            {
                PickTextureUnderMouse();
            }
        }

        // [UPDATE] Pemindai Tekstur/Gambar kini dibekali Filter Area & Daftar Hitam (Blacklist)!
        private static void PickTextureUnderMouse()
        {
            PickedSpriteName = "";
            PickedPath = "";
            PickedObjectName = "";
            int foundCount = 0;
            Vector2 mousePos = Input.mousePosition;

            Debug.Log("---------------------------------------------");
            Debug.Log("Texture Picker - Scanning for Sprites/Images");
            Debug.Log("---------------------------------------------");

            // [FITUR BARU] Menyimpan ukuran area terkecil untuk memfilter background raksasa
            float smallestArea = float.MaxValue;

            // 1. ABSOLUTE UI SCANNER (Memaksa scan tanpa mempedulikan Raycast Target!)
            Image[] allImages = Object.FindObjectsOfType<Image>();
            foreach (Image img in allImages)
            {
                if (img.gameObject.activeInHierarchy && img.sprite != null)
                {
                    if (IsPointInsideRectTransform(img.rectTransform, mousePos))
                    {
                        string cleanName = img.sprite.name.Replace("_Translated", "").Replace("(Clone)", "").Trim();

                        // [BLACKLIST MUTLAK] Abaikan layer transparan raksasa pelindung klik bawaan game!
                        if (cleanName == "BlockSprite" || cleanName == "UIMask") continue;

                        string path = GetPath(img.transform);
                        Debug.Log($"[Texture Picker] (Absolute UI) Found: {cleanName} on {img.gameObject.name}\nPath: {path}\nFile Name: {cleanName}.png");

                        // Kalkulasi luas gambar. Tombol/Ikon pasti areanya lebih kecil dari Background!
                        float area = img.rectTransform.rect.width * img.rectTransform.rect.height;
                        if (area < smallestArea)
                        {
                            smallestArea = area;
                            PickedSpriteName = cleanName;
                            PickedPath = path;
                            PickedObjectName = img.gameObject.name;
                        }
                        foundCount++;
                    }
                }
            }

            // 2. RAYCAST SCANNER (Jalankan SELALU, jangan diskip. Ini scanner paling akurat untuk UI!)
            if (EventSystem.current != null)
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = mousePos };
                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                foreach (RaycastResult result in results)
                {
                    if (TryGetSpriteName(result.gameObject, out string rawName))
                    {
                        string cleanName = rawName.Replace("_Translated", "").Replace("(Clone)", "").Trim();

                        // [BLACKLIST MUTLAK] 
                        if (cleanName == "BlockSprite" || cleanName == "UIMask") continue;

                        string path = GetPath(result.gameObject.transform);
                        Debug.Log($"[Texture Picker] (Raycast UI) Found: {cleanName} on {result.gameObject.name}\nPath: {path}\nFile Name: {cleanName}.png");

                        float area = float.MaxValue;
                        if (result.gameObject.TryGetComponent<RectTransform>(out var rt))
                        {
                            area = rt.rect.width * rt.rect.height;
                        }

                        if (area < smallestArea)
                        {
                            smallestArea = area;
                            PickedSpriteName = cleanName;
                            PickedPath = path;
                            PickedObjectName = result.gameObject.name;
                        }
                        foundCount++;
                    }
                }
            }

            // 3. PHYSICS SCANNER (Untuk objek dunia 3D/Physics yang punya Collider)
            if (foundCount == 0)
            {
                Ray ray = Camera.main.ScreenPointToRay(mousePos);
                RaycastHit[] hits = Physics.RaycastAll(ray);

                foreach (var hit in hits)
                {
                    if (TryGetSpriteName(hit.collider.gameObject, out string rawName))
                    {
                        string cleanName = rawName.Replace("_Translated", "").Replace("(Clone)", "").Trim();

                        if (cleanName == "BlockSprite" || cleanName == "UIMask") continue;

                        string path = GetPath(hit.collider.transform);
                        Debug.Log($"[Texture Picker] (Physics 3D) Found: {cleanName} on {hit.collider.gameObject.name}\nPath: {path}\nFile Name: {cleanName}.png");

                        // Untuk 3D kita ambil yang teratas (pertama kali nembrak Collider)
                        PickedSpriteName = cleanName;
                        PickedPath = path;
                        PickedObjectName = hit.collider.gameObject.name;
                        foundCount++;
                        break;
                    }
                }
            }

            // 4. ABSOLUTE 2D SCANNER (Untuk objek Sprite di dunia yang tidak punya Collider)
            if (foundCount == 0)
            {
                smallestArea = float.MaxValue;
                SpriteRenderer[] allSRs = Object.FindObjectsOfType<SpriteRenderer>();
                foreach (SpriteRenderer sr in allSRs)
                {
                    if (sr.gameObject.activeInHierarchy && sr.sprite != null && IsObjectUnderMouse(sr.gameObject))
                    {
                        string cleanName = sr.sprite.name.Replace("_Translated", "").Replace("(Clone)", "").Trim();

                        if (cleanName == "BlockSprite" || cleanName == "UIMask") continue;

                        string path = GetPath(sr.transform);
                        Debug.Log($"[Texture Picker] (Absolute 2D) Found: {cleanName} on {sr.gameObject.name}\nPath: {path}\nFile Name: {cleanName}.png");

                        float area = sr.bounds.size.x * sr.bounds.size.y;
                        if (area < smallestArea)
                        {
                            smallestArea = area;
                            PickedSpriteName = cleanName;
                            PickedPath = path;
                            PickedObjectName = sr.gameObject.name;
                        }
                        foundCount++;
                    }
                }
            }

            // Hasil Akhir
            if (foundCount == 0)
            {
                Debug.Log("[Texture Picker] No Sprite Found!");
            }
            else
            {
                Debug.Log("---------------------------------------------");
                Debug.Log($"[Texture Picker] Selected Final : {PickedSpriteName}.png");
                Debug.Log($"[Texture Picker] Object Name    : {PickedObjectName}");
                Debug.Log($"[Texture Picker] Exact Path     : {PickedPath}");
                Debug.Log("---------------------------------------------");
            }
        }

        // [UPDATE] Menggunakan parameter 'out' agar variabel utama tidak kotor sebelum waktunya
        private static bool TryGetSpriteName(GameObject obj, out string spriteName)
        {
            spriteName = "";
            if (obj == null) return false;

            if (obj.TryGetComponent<Image>(out var img) && img.sprite != null)
            {
                spriteName = img.sprite.name;
                return true;
            }

            if (obj.TryGetComponent<SpriteRenderer>(out var sr) && sr.sprite != null)
            {
                spriteName = sr.sprite.name;
                return true;
            }

            return false;
        }

        private static void ScanObjectUnderMouse()
        {
            Debug.Log("---------------------------------------------");
            string mode = IsAdvanced ? "Advanced Scanner" : "Absolute Text Scanner";
            Debug.Log($"Path Detector - {mode} Active");
            Debug.Log("---------------------------------------------");

            int foundCount = 0;
            Vector2 mousePos = Input.mousePosition;
            LastScannedSpriteName = "";

            // A. Pengecekan UGUI Text
            Text[] allTexts = Object.FindObjectsOfType<Text>();
            foreach (Text t in allTexts)
            {
                if (t.gameObject.activeInHierarchy && IsPointInsideRectTransform(t.rectTransform, mousePos))
                {
                    PrintLog(t.gameObject, t.text, "UI.Text");
                    foundCount++;
                }
            }

            // B. Pengecekan TextMeshPro UI
            System.Type tmpType = HarmonyLib.AccessTools.TypeByName("TMPro.TextMeshProUGUI");
            if (tmpType != null)
            {
                Object[] allTMPs = Object.FindObjectsOfType(tmpType);
                foreach (Component tmp in allTMPs)
                {
                    if (tmp.gameObject.activeInHierarchy)
                    {
                        RectTransform rt = tmp.GetComponent<RectTransform>();
                        if (rt != null && IsPointInsideRectTransform(rt, mousePos))
                        {
                            var prop = tmpType.GetProperty("text");
                            string txtVal = prop?.GetValue(tmp, null) as string;
                            PrintLog(tmp.gameObject, txtVal, tmpType.Name);
                            foundCount++;
                        }
                    }
                }
            }

            // C. Pengecekan TextMesh 3D
            TextMesh[] allMesh = Object.FindObjectsOfType<TextMesh>();
            foreach (TextMesh tm in allMesh)
            {
                if (tm.gameObject.activeInHierarchy && IsObjectUnderMouse(tm.gameObject))
                {
                    PrintLog(tm.gameObject, tm.text, "UnityEngine.TextMesh");
                    foundCount++;
                }
            }

            if (foundCount == 0 || IsAdvanced)
            {
                if (EventSystem.current != null)
                {
                    PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = mousePos };
                    List<RaycastResult> results = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(pointerData, results);

                    foreach (RaycastResult result in results)
                    {
                        if (result.gameObject.GetComponent<Text>() == null && result.gameObject.GetComponent("TMPro.TMP_Text") == null)
                        {
                            if (CheckAndLog(result.gameObject)) foundCount++;
                        }
                    }
                }
            }

            if (foundCount == 0) Debug.Log("No relevant text or component found under mouse.");

            Debug.Log("---------------------------------------------");
            Debug.Log($"Scan Complete. Found {foundCount} object(s).");
            Debug.Log("---------------------------------------------");
        }

        private static bool IsPointInsideRectTransform(RectTransform rectTransform, Vector2 screenPoint)
        {
            Camera cam = null;
            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = canvas.worldCamera ?? Camera.main;
            }
            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, cam);
        }

        private static bool IsObjectUnderMouse(GameObject obj)
        {
            if (!obj.TryGetComponent<Renderer>(out var r)) return false;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Bounds bounds = r.bounds;
            return bounds.IntersectRay(ray);
        }

        private static bool CheckAndLog(GameObject obj)
        {
            if (obj == null) return false;

            string textContent = "N/A";
            string type = "Unknown";
            bool isTarget = false;

            if (obj.TryGetComponent<InputField>(out var inputField))
            {
                textContent = inputField.text;
                type = "UI.InputField";
                isTarget = true;
            }
            else if (obj.GetComponent("TMPro.TMP_InputField") != null)
            {
                Component tmp = obj.GetComponent("TMPro.TMP_InputField");
                var prop = tmp.GetType().GetProperty("text");
                if (prop != null)
                {
                    textContent = prop.GetValue(tmp, null) as string;
                    type = "TMPro.TMP_InputField";
                    isTarget = true;
                }
            }

            if (obj.TryGetComponent<Image>(out var img) && img.sprite != null)
            {
                LastScannedSpriteName = img.sprite.name;
                if (!isTarget) { type = "UI.Image"; textContent = img.sprite.name; isTarget = true; }
            }
            else if (obj.TryGetComponent<SpriteRenderer>(out var sr) && sr.sprite != null)
            {
                LastScannedSpriteName = sr.sprite.name;
                if (!isTarget) { type = "SpriteRenderer"; textContent = sr.sprite.name; isTarget = true; }
            }

            if (!string.IsNullOrEmpty(LastScannedSpriteName))
            {
                LastScannedSpriteName = LastScannedSpriteName.Replace("_Translated", "").Replace("(Clone)", "").Trim();
            }

            if (isTarget || (IsAdvanced && obj.GetComponent<RectTransform>() != null))
            {
                PrintLog(obj, textContent, type);
                return true;
            }

            return false;
        }

        private static void PrintLog(GameObject obj, string text, string type)
        {
            string path = GetPath(obj.transform);
            string jsonKey = EscapeForJson(text);

            if (IsAdvanced)
            {
                string posInfo = "";
                if (obj.TryGetComponent<RectTransform>(out var rect))
                {
                    posInfo += $"\nPos (X, Y)  : {rect.anchoredPosition.x:F1}, {rect.anchoredPosition.y:F1}";
                    posInfo += $"\nSize (W, H) : {rect.rect.width:F0}, {rect.rect.height:F0}";
                    posInfo += $"\nPivot       : {rect.pivot}";
                }

                string layoutInfo = "None";
                if (obj.transform.parent != null)
                {
                    if (obj.transform.parent.GetComponent<VerticalLayoutGroup>()) layoutInfo = "VerticalLayoutGroup";
                    else if (obj.transform.parent.GetComponent<HorizontalLayoutGroup>()) layoutInfo = "HorizontalLayoutGroup";
                    else if (obj.transform.parent.GetComponent<GridLayoutGroup>()) layoutInfo = "GridLayoutGroup";
                }

                string spriteInfo = string.IsNullOrEmpty(LastScannedSpriteName) ? "" : $"\nSprite Name : {LastScannedSpriteName}\nFile Name   : {LastScannedSpriteName}.png";

                Debug.Log($"Text       : {text}\n" +
                          $"JSON Key   : {jsonKey}\n" +
                          $"Path       : {path}\n" +
                          $"Type       : {type}\n" +
                          $"Parent Lay : {layoutInfo}" +
                          spriteInfo +
                          posInfo);
            }
            else
            {
                Debug.Log($"Text : {text}\nJSON Key : {jsonKey}\nPath : {path}\nType : {type}");
                if (!string.IsNullOrEmpty(LastScannedSpriteName) && (type.Contains("Image") || type.Contains("Sprite")))
                    Debug.Log($"Sprite Name: {LastScannedSpriteName}\nFile Name: {LastScannedSpriteName}.png");
            }
        }

        private static string EscapeForJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        public static string GetPath(Transform current)
        {
            if (current.parent == null)
                return "/" + current.name;
            return GetPath(current.parent) + "/" + current.name;
        }
    }
}