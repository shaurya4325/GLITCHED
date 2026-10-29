using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Glitched.EditorTools
{
    /// <summary>
    /// Import rules for every GLITCHED asset (asset prompt, Step 5):
    /// - textures: 1K for environment tiles, 2K for characters and skies, normal maps detected, mipmaps on
    /// - environment models: no animation, materials converted to the Glitchable shader (stylised, corruption-aware)
    /// - characters: Humanoid (Mixamo / Quaternius Universal) or Generic (monsters), materials converted to Toon
    /// - audio: music streamed, short SFX decompressed on load
    /// Drop new models into the matching Art/ folder and they are set up automatically.
    /// </summary>
    public class GlitchedImporter : AssetPostprocessor
    {
        static bool In(string path, string folder) => path.Replace('\\', '/').Contains(folder);
        static bool IsCharacter(string p) => In(p, "/Art/Characters/") || In(p, "/Art/Animations/") || (In(p, "/SciFiEssentials/") && System.IO.Path.GetFileName(p).StartsWith("Enemy_")) || In(p, "/Platformer/FBX/Enemies/") || In(p, "/SciFiMegaKit/FBX/Aliens/");
        static bool IsHumanoid(string p) => In(p, "/Characters/NPCs/UniversalBase/") || In(p, "/Art/Animations/") || In(p, "/Characters/Player/") || In(p, "/Characters/Bosses/") || In(p, "/Characters/Mixamo/") || In(p, "/Characters/MPFB/") || In(p, "/Characters/Scan/");

        void OnPreprocessTexture()
        {
            var ti = (TextureImporter)assetImporter;
            string p = assetPath;
            string name = System.IO.Path.GetFileNameWithoutExtension(p).ToLowerInvariant();
            if (In(p, "/Art/Textures/Skies/"))
            {
                ti.maxTextureSize = 2048;
                ti.mipmapEnabled = false;
                ti.wrapModeU = TextureWrapMode.Repeat; ti.wrapModeV = TextureWrapMode.Clamp;
                ti.textureCompression = TextureImporterCompression.Compressed;
                return;
            }
            if (In(p, "/Resources/Faces/"))
            {
                ti.alphaIsTransparency = true; ti.maxTextureSize = 512; ti.mipmapEnabled = true; ti.wrapMode = TextureWrapMode.Clamp;
                ti.textureType = TextureImporterType.Default;
                return;
            }
            if (In(p, "/Art/Textures/VFX/")) { ti.alphaIsTransparency = true; ti.maxTextureSize = 256; return; }
            if (In(p, "/Art/Textures/Holograms/")) { ti.maxTextureSize = 1024; ti.wrapMode = TextureWrapMode.Repeat; ti.sRGBTexture = true; return; }
            bool normal = name.Contains("normal") && !name.Contains("normals unity");
            if (normal) ti.textureType = TextureImporterType.NormalMap;
            if (name.Contains("_orm") || name.Contains("roughness") || name.Contains("ambientocclusion") || name.Contains("metalness")) ti.sRGBTexture = false;
            if (In(p, "/Art/Textures/Foliage/")) { ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.maxTextureSize = 2048; ti.mipMapsPreserveCoverage = true; ti.alphaTestReferenceValue = 0.45f; }
            if (In(p, "/Art/Characters/")) ti.maxTextureSize = 2048;
            // Sketchfab bosses (Tools/blender_glb_to_fbx.py): roughness/metal and AO maps are linear data
            if (In(p, "/Art/Characters/BossesHQ/") && (name.EndsWith("_mr") || name.EndsWith("_ao"))) ti.sRGBTexture = false;
            // 3D scans of real people: the face is a small part of the atlas, keep every pixel (Polycam scenes are 4K)
            if (In(p, "/Art/Characters/Scan/"))
            {
                ti.maxTextureSize = 4096; ti.textureCompression = TextureImporterCompression.CompressedHQ; ti.wrapMode = TextureWrapMode.Clamp;
                // baked detail maps (Tools/bake_scan_maps.py): the normal map is a normal map, the AO is linear data
                if (name.EndsWith("_normal")) ti.textureType = TextureImporterType.NormalMap;
                if (name.EndsWith("_ao")) { ti.sRGBTexture = false; ti.maxTextureSize = 2048; }
            }
            else if (In(p, "/Art/Textures/PBR/")) ti.maxTextureSize = 2048;   // full detail on Ultra; low tiers drop mips
            else if (In(p, "/Art/Environments/")) ti.maxTextureSize = 1024;
            ti.mipmapEnabled = true;
        }

        void OnPreprocessModel()
        {
            var mi = (ModelImporter)assetImporter;
            string p = assetPath;
            if (!In(p, "/_Glitched/Art/")) return;
            mi.importCameras = false;
            mi.importLights = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
            mi.isReadable = false;
            mi.addCollider = false;
            if (IsCharacter(p))
            {
                mi.animationType = IsHumanoid(p) ? ModelImporterAnimationType.Human : ModelImporterAnimationType.Generic;
                mi.importAnimation = true;
                mi.optimizeGameObjects = false;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            }
            else
            {
                mi.animationType = ModelImporterAnimationType.None;
                mi.importAnimation = false;
                mi.importBlendShapes = false;
                mi.meshCompression = ModelImporterMeshCompression.Low;
            }
        }

        void OnPreprocessAnimation()
        {
            var mi = (ModelImporter)assetImporter;
            if (!IsCharacter(assetPath)) return;
            var clips = mi.defaultClipAnimations;
            if (clips == null || clips.Length == 0) return;
            bool mixamo = assetPath.Contains("/Art/Animations/Mixamo/");
            bool ual = assetPath.Contains("/Art/Animations/UAL");   // Quaternius rigs face the other way: orient by the body
            foreach (var c in clips)
            {
                if (mixamo) c.name = "Mixamo_" + System.IO.Path.GetFileNameWithoutExtension(assetPath); // Mixamo names every clip "mixamo.com"
                string n = c.name.ToLowerInvariant();
                c.loopTime = n.Contains("idle") || n.Contains("walk") || n.Contains("run") || n.Contains("fly") || n.Contains("sit") || n.Contains("talk") || n.Contains("dance") || n.Contains("loop") || n.Contains("swim") || n.Contains("fall") || n.Contains("float") || n.Contains("typing") || n.Contains("terrified");
                c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = true;
                c.keepOriginalOrientation = !ual; c.keepOriginalPositionY = true; c.keepOriginalPositionXZ = true;
            }
            mi.clipAnimations = clips;
        }

        /// <summary>
        /// Mixamo FBX textures are unpacked into Textures_<model>/ (see GlitchedBuilder.ExtractEmbeddedTextures), which
        /// Unity's own texture search does not look in; resolve the diffuse map there by file name.
        /// </summary>
        /// <summary>"modelPath|materialName" -> diffuse texture file name (no extension), filled during import.</summary>
        public static readonly System.Collections.Generic.Dictionary<string, string> DiffuseFile = new System.Collections.Generic.Dictionary<string, string>();

        void OnPreprocessMaterialDescription(MaterialDescription d, Material material, AnimationClip[] clips)
        {
            if (!In(assetPath, "/Art/Characters/Player/") && !In(assetPath, "/Art/Characters/Bosses/") && !In(assetPath, "/Art/Characters/MPFB/")) return;
            if (!d.TryGetProperty("DiffuseColor", out TexturePropertyDescription t) && !d.TryGetProperty("diffuse", out t)) return;
            // Mixamo writes an absolute server path (/home/app/mixamo-mini/tmp/....fbm/X_diffuse.png) and no relative path
            string src = !string.IsNullOrEmpty(t.relativePath) ? t.relativePath : t.path ?? "";
            string file = System.IO.Path.GetFileNameWithoutExtension(src.Replace('\\', '/'));
            if (string.IsNullOrEmpty(file)) return;
            DiffuseFile[assetPath + "|" + d.materialName] = file;
            if (t.texture != null) return;
            string dir = $"{System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/')}/Textures_{System.IO.Path.GetFileNameWithoutExtension(assetPath)}";
            if (!AssetDatabase.IsValidFolder(dir)) return;
            foreach (var g in AssetDatabase.FindAssets(file + " t:Texture2D", new[] { dir }))
            {
                var tp = AssetDatabase.GUIDToAssetPath(g);
                if (System.IO.Path.GetFileNameWithoutExtension(tp) != file) continue;
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(tp);
                if (tex == null) continue;
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", tex);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", tex);
                return;
            }
        }

        void OnPostprocessMaterial(Material m)
        {
            string p = assetPath;
            if (!In(p, "/_Glitched/Art/")) return;
            if (In(p, "/Art/Characters/BossesHQ/")) return;   // GlitchedBuilder.BuildHQBosses gives these PBR materials
            bool character = IsCharacter(p);
            var shader = Shader.Find(character ? "Glitched/Toon" : "Glitched/Glitchable");
            if (shader == null) return;
            Texture tex = null;
            if (m.HasProperty("_BaseMap")) tex = m.GetTexture("_BaseMap");
            if (tex == null && m.HasProperty("_MainTex")) tex = m.GetTexture("_MainTex");
            Color col = Color.white;
            if (m.HasProperty("_BaseColor")) col = m.GetColor("_BaseColor");
            else if (m.HasProperty("_Color")) col = m.GetColor("_Color");
            Color emi = Color.black;
            if (m.HasProperty("_EmissionColor")) emi = m.GetColor("_EmissionColor");
            m.shader = shader;
            if (tex != null) m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", col);
            if (emi.maxColorComponent > 0.01f) m.SetColor("_EmissionColor", emi);
            if (!character)
            {
                m.SetFloat("_Glitchiness", In(p, "/Environments/Shared/") ? 1f : 0.8f);
                m.SetFloat("_AlbedoContrast", 0.7f);
            }
            m.enableInstancing = true;
        }

        void OnPreprocessAudio()
        {
            var ai = (AudioImporter)assetImporter;
            var s = ai.defaultSampleSettings;
            if (In(assetPath, "/Audio/Music/"))
            {
                s.loadType = AudioClipLoadType.Streaming;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.55f;
                ai.loadInBackground = true;
            }
            else
            {
                s.loadType = AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.6f;
                ai.forceToMono = true;
            }
            ai.defaultSampleSettings = s;
        }
    }
}
