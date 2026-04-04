#pragma warning disable IDE0270, IDE0220 

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
        public static string PickedSpriteName = "";

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

        private static void PickTextureUnderMouse()
        {
            PickedSpriteName = "";
            bool found = false;

            if (EventSystem.current != null)
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition
                };
                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                foreach (RaycastResult result in results)
                {
                    if (TryGetSpriteName(result.gameObject))
                    {
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit[] hits = Physics.RaycastAll(ray);
                foreach (var hit in hits)
                {
                    if (TryGetSpriteName(hit.collider.gameObject))
                    {
                        found = true;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(PickedSpriteName))
            {
                Debug.Log($"[Texture Picker] Selected: {PickedSpriteName}");
            }
            else
            {
                Debug.Log("[Texture Picker] No Sprite Found!");
            }
        }

        private static bool TryGetSpriteName(GameObject obj)
        {
            if (obj == null) return false;

            if (obj.TryGetComponent<Image>(out var img) && img.sprite != null)
            {
                PickedSpriteName = img.sprite.name;
                return true;
            }

            if (obj.TryGetComponent<SpriteRenderer>(out var sr) && sr.sprite != null)
            {
                PickedSpriteName = sr.sprite.name;
                return true;
            }

            return false;
        }

        // [UPDATE 0.14.6] Menggunakan Absolute Screen Scanner untuk menembus batasan Raycast Target
        private static void ScanObjectUnderMouse()
        {
            Debug.Log("---------------------------------------------");
            string mode = IsAdvanced ? "Advanced Scanner" : "Absolute Text Scanner";
            Debug.Log($"Path Detector - {mode} Active");
            Debug.Log("---------------------------------------------");

            int foundCount = 0;
            Vector2 mousePos = Input.mousePosition;
            LastScannedSpriteName = "";

            // 1. ABSOLUTE TEXT SCANNER (Memaksa scan tanpa peduli Raycast Target nyala atau mati)

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

            // 2. RAYCAST SCANNER (Sebagai cadangan untuk mendeteksi Gambar jika tidak ada teks, atau mode Advanced)
            if (foundCount == 0 || IsAdvanced)
            {
                if (EventSystem.current != null)
                {
                    PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = mousePos };
                    List<RaycastResult> results = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(pointerData, results);

                    foreach (RaycastResult result in results)
                    {
                        // Hindari log ganda jika objek tersebut adalah teks yang sudah dilog di atas
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

        // [FITUR BARU] Mengecek langsung posisi mouse dengan kotak RectTransform UI di layar
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

                string spriteInfo = string.IsNullOrEmpty(LastScannedSpriteName) ? "" : $"\nSprite Name : {LastScannedSpriteName}";

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
                    Debug.Log($"Sprite Name: {LastScannedSpriteName}");
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