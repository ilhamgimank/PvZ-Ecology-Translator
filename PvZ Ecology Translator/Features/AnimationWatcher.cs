#pragma warning disable IDE0017, IDE0270, IDE0079 // Membungkam pesan Object Initialization dan Null Check

using UnityEngine;
using UnityEngine.UI;
using PvZEcologyTranslator.Managers;

namespace PvZEcologyTranslator.Features
{
    public class AnimationWatcher : MonoBehaviour
    {
        private Image img;
        private SpriteRenderer sr;

        private Sprite lastOriginalSprite;
        private Sprite lastTranslatedSprite;

        void Awake()
        {
            // Cache komponen sekali di awal agar ringan dan tidak membebani performa game
            img = GetComponent<Image>();
            sr = GetComponent<SpriteRenderer>();
        }

        // Berjalan di detik-detik terakhir sebelum frame digambar ke layar (monitor)
        void LateUpdate()
        {
            // [UPDATE] Dihapus if (!TextureManager.EnableImageTranslation) return; 
            // agar efek animasi ledakan juga membaca tekstur dari folder Original ([Default Textures])

            if (img != null && img.sprite != null)
            {
                Sprite s = img.sprite;

                // Jika sprite berubah, dan sprite itu BUKAN hasil terjemahan mod kita
                if (s != lastTranslatedSprite && !s.name.EndsWith("_Translated"))
                {
                    lastOriginalSprite = s;
                    string cleanName = lastOriginalSprite.name.Replace("(Clone)", "").Trim();

                    // Coba cari terjemahannya di kamus
                    Sprite translated = TextureManager.GetTranslatedSprite(lastOriginalSprite, cleanName);
                    if (translated != null)
                    {
                        lastTranslatedSprite = translated;
                        img.sprite = translated; // Timpa seketika!
                    }
                    else
                    {
                        // Tidak ada file terjemahan di folder, anggap original sebagai yang terakhir agar tidak spam proses
                        lastTranslatedSprite = s;
                    }
                }
                // Jika sprite yang tampil sekarang ADALAH hasil terjemahan, update catatan kita
                else if (s != lastTranslatedSprite && s.name.EndsWith("_Translated"))
                {
                    lastTranslatedSprite = s;
                }
            }

            if (sr != null && sr.sprite != null)
            {
                Sprite s = sr.sprite;

                if (s != lastTranslatedSprite && !s.name.EndsWith("_Translated"))
                {
                    lastOriginalSprite = s;
                    string cleanName = lastOriginalSprite.name.Replace("(Clone)", "").Trim();

                    Sprite translated = TextureManager.GetTranslatedSprite(lastOriginalSprite, cleanName);
                    if (translated != null)
                    {
                        lastTranslatedSprite = translated;
                        sr.sprite = translated;
                    }
                    else
                    {
                        lastTranslatedSprite = s;
                    }
                }
                else if (s != lastTranslatedSprite && s.name.EndsWith("_Translated"))
                {
                    lastTranslatedSprite = s;
                }
            }
        }
    }
}