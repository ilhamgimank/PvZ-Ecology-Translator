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

        private int lastImgSpriteId = 0;
        private int lastSrSpriteId = 0;
        private int lastPsrTexId = 0;
        private int lastMrTexId = 0;

        void Awake()
        {
            img = GetComponent<Image>();
            sr = GetComponent<SpriteRenderer>();
            psr = GetComponent<ParticleSystemRenderer>();
            mr = GetComponent<MeshRenderer>();
        }

        // [FITUR BARU 1] Eksekusi instan seketika saat objek didaur ulang dari kolam memori (Object Pool)!
        void OnEnable() { ForceReplaceSprite(); }

        // [FITUR BARU 2] Eksekusi di awal frame untuk mengimbangi kecepatan Animator game!
        void Update() { ForceReplaceSprite(); }

        // Eksekusi pelapis di akhir frame 
        void LateUpdate() { ForceReplaceSprite(); }

        private void ForceReplaceSprite()
        {
            // 1. Cek UI Image standar
            if (img != null && img.sprite != null)
            {
                int currentId = img.sprite.GetInstanceID();
                if (currentId != lastImgSpriteId)
                {
                    lastImgSpriteId = currentId;
                    string sName = img.sprite.name;

                    if (TextureManager.EnableTextureDumper && !sName.EndsWith("_Translated"))
                    {
                        TextureManager.DumpSprite(img.sprite);
                    }

                    if (!sName.EndsWith("_Translated"))
                    {
                        string cleanName = sName.Replace("(Clone)", "").Replace("(Instance)", "").Trim();
                        Sprite translated = TextureManager.GetTranslatedSprite(img.sprite, cleanName);
                        if (translated != null)
                        {
                            img.sprite = translated;
                            lastImgSpriteId = translated.GetInstanceID();
                        }
                    }
                }
            }

            // 2. Cek SpriteRenderer (Objek 2D Dunia / Frame Ledakan)
            if (sr != null && sr.sprite != null)
            {
                int currentId = sr.sprite.GetInstanceID();
                if (currentId != lastSrSpriteId)
                {
                    lastSrSpriteId = currentId;
                    string sName = sr.sprite.name;

                    if (TextureManager.EnableTextureDumper && !sName.EndsWith("_Translated"))
                    {
                        TextureManager.DumpSprite(sr.sprite);
                    }

                    if (!sName.EndsWith("_Translated"))
                    {
                        string cleanName = sName.Replace("(Clone)", "").Replace("(Instance)", "").Trim();
                        Sprite translated = TextureManager.GetTranslatedSprite(sr.sprite, cleanName);
                        if (translated != null)
                        {
                            sr.sprite = translated;
                            lastSrSpriteId = translated.GetInstanceID();
                        }
                    }
                }
            }

            // 3. Cek Particle System (Efek Ledakan Doom, Spudow, Sproing!)
            if (psr != null && psr.sharedMaterial != null && psr.sharedMaterial.mainTexture != null)
            {
                int currentId = psr.sharedMaterial.mainTexture.GetInstanceID();
                if (currentId != lastPsrTexId)
                {
                    lastPsrTexId = currentId;
                    string texName = psr.sharedMaterial.mainTexture.name;
                    string cleanName = texName.Replace("(Clone)", "").Replace("(Instance)", "").Trim();

                    if (TextureManager.EnableTextureDumper && !texName.EndsWith("_Translated"))
                    {
                        TextureManager.DumpTexture2D(psr.sharedMaterial.mainTexture, cleanName);
                    }

                    if (!texName.EndsWith("_Translated"))
                    {
                        if (TextureManager.CustomTextures.TryGetValue(cleanName, out Texture2D customTex))
                        {
                            customTex.name = cleanName + "_Translated";
                            psr.sharedMaterial.mainTexture = customTex;
                            lastPsrTexId = customTex.GetInstanceID();
                        }
                    }
                }
            }

            // 4. Cek MeshRenderer (Untuk objek 3D datar klasik)
            if (mr != null && mr.sharedMaterial != null && mr.sharedMaterial.mainTexture != null)
            {
                int currentId = mr.sharedMaterial.mainTexture.GetInstanceID();
                if (currentId != lastMrTexId)
                {
                    lastMrTexId = currentId;
                    string texName = mr.sharedMaterial.mainTexture.name;
                    string cleanName = texName.Replace("(Clone)", "").Replace("(Instance)", "").Trim();

                    if (TextureManager.EnableTextureDumper && !texName.EndsWith("_Translated"))
                    {
                        TextureManager.DumpTexture2D(mr.sharedMaterial.mainTexture, cleanName);
                    }

                    if (!texName.EndsWith("_Translated"))
                    {
                        if (TextureManager.CustomTextures.TryGetValue(cleanName, out Texture2D customTex))
                        {
                            customTex.name = cleanName + "_Translated";
                            mr.sharedMaterial.mainTexture = customTex;
                            lastMrTexId = customTex.GetInstanceID();
                        }
                    }
                }
            }
        }
    }
}