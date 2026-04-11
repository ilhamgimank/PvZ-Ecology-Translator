#pragma warning disable IDE0017, IDE0270

using UnityEngine;
using UnityEngine.UI;
using PvZEcologyTranslator.Managers;

namespace PvZEcologyTranslator.Features
{
    public class AnimationWatcher : MonoBehaviour
    {
        private Image img;
        private SpriteRenderer sr;
        private ParticleSystemRenderer psr;
        private MeshRenderer mr;

        void Awake()
        {
            // Cache komponen di awal agar performa game tetap lancar
            img = GetComponent<Image>();
            sr = GetComponent<SpriteRenderer>();
            psr = GetComponent<ParticleSystemRenderer>();
            mr = GetComponent<MeshRenderer>();
        }

        // Jalankan pelacakan di setiap tahap penggambaran frame
        void Update() { ForceReplaceSprite(); }
        void LateUpdate() { ForceReplaceSprite(); }
        void OnWillRenderObject() { ForceReplaceSprite(); }

        private void ForceReplaceSprite()
        {
            // Mencari ulang komponen jaga-jaga jika skrip game menambahkannya belakangan
            if (img == null) img = GetComponent<Image>();
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (psr == null) psr = GetComponent<ParticleSystemRenderer>();
            if (mr == null) mr = GetComponent<MeshRenderer>();

            // 1. Cek UI Image standar
            if (img != null && img.sprite != null)
            {
                Sprite s = img.sprite;
                if (!s.name.EndsWith("_Translated"))
                {
                    string cleanName = s.name.Replace("(Clone)", "").Replace("(Instance)", "").Trim();
                    Sprite translated = TextureManager.GetTranslatedSprite(s, cleanName);
                    if (translated != null) img.sprite = translated;
                }
            }

            // 2. Cek SpriteRenderer
            if (sr != null && sr.sprite != null)
            {
                Sprite s = sr.sprite;
                if (!s.name.EndsWith("_Translated"))
                {
                    string cleanName = s.name.Replace("(Clone)", "").Replace("(Instance)", "").Trim();
                    Sprite translated = TextureManager.GetTranslatedSprite(s, cleanName);
                    if (translated != null) sr.sprite = translated;
                }
            }

            // 3. [FITUR BARU] Cek Particle System (Efek Ledakan Doom, Spudow, Sproing!)
            if (psr != null && psr.material != null && psr.material.mainTexture != null)
            {
                Texture tex = psr.material.mainTexture;
                if (!tex.name.EndsWith("_Translated"))
                {
                    string cleanName = tex.name.Replace("(Clone)", "").Replace("(Instance)", "").Trim();

                    // Particle System menggunakan Texture2D murni, bukan Sprite.
                    if (TextureManager.CustomTextures.TryGetValue(cleanName, out Texture2D customTex))
                    {
                        customTex.name = cleanName + "_Translated";
                        psr.material.mainTexture = customTex; // Timpa material partikel!
                    }
                }
            }

            // 4. [FITUR BARU] Cek MeshRenderer (Untuk objek 3D datar klasik)
            if (mr != null && mr.material != null && mr.material.mainTexture != null)
            {
                Texture tex = mr.material.mainTexture;
                if (!tex.name.EndsWith("_Translated"))
                {
                    string cleanName = tex.name.Replace("(Clone)", "").Replace("(Instance)", "").Trim();
                    if (TextureManager.CustomTextures.TryGetValue(cleanName, out Texture2D customTex))
                    {
                        customTex.name = cleanName + "_Translated";
                        mr.material.mainTexture = customTex;
                    }
                }
            }
        }
    }
}