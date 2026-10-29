using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;

namespace Glitched.EditorTools
{
    /// <summary>
    /// Glitched -> Build Everything: creates (only if missing) fonts, materials, URP quality tiers, all data assets
    /// (characters, enemies, bosses, arcs, cosmetics, achievements, sounds), animators and region kits, then
    /// (re)builds every scene and the Build Settings list. Safe to run again: existing data assets are kept.
    /// </summary>
    public static partial class GlitchedBuilder
    {
        public const string Root = "Assets/_Glitched";
        public const string Data = Root + "/Data";
        public const string Art = Root + "/Art";
        static GameDatabase db;

        [MenuItem("Glitched/Build Everything", priority = 0)]
        public static void BuildEverythingMenu()
        {
            BuildEverything();
            EditorUtility.DisplayDialog("GLITCHED", "Everything is built. Open Assets/_Glitched/Scenes/Boot.unity and press Play.", "OK");
        }

        [MenuItem("Glitched/Rebuild Scenes Only", priority = 1)]
        public static void RebuildScenesMenu()
        {
            db = AssetDatabase.LoadAssetAtPath<GameDatabase>(Root + "/Resources/GlitchedDatabase.asset");
            if (db == null) { BuildEverything(); return; }
            SceneFactory.BuildAll(db);
        }

        public static void RebuildScenesMenuBatch()
        {
            try
            {
                db = AssetDatabase.LoadAssetAtPath<GameDatabase>(Root + "/Resources/GlitchedDatabase.asset");
                lib = db.materials;
                MaterialCache.ClearCache();
                MaterialCache.OnCreated = SaveGeneratedMaterial;
                ProceduralMeshes.OnCreated = SaveGeneratedMesh;
                SceneFactory.BuildAll(db);
                AssetDatabase.SaveAssets();
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        /// <summary>Batch mode entry point: Unity.exe -batchmode -executeMethod Glitched.EditorTools.GlitchedBuilder.BuildEverythingBatch</summary>
        public static void BuildEverythingBatch()
        {
            try { BuildEverything(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static void BuildEverything()
        {
            Step("TextMeshPro essentials", ImportTMPEssentials);
            Step("Folders", Folders);
            Step("Reimport models if needed", ReimportModelsIfNeeded);
            Step("Fonts", BuildFonts);
            Step("Materials", BuildMaterials);
            Step("Render pipelines + quality tiers", BuildRenderPipelines);
            Step("Data assets", BuildData);
            Step("Characters (generated)", BuildCharacters);
            Step("Animators", BuildAnimators);
            Step("Environment kits", BuildKits);
            Step("Input + localisation", WriteTextAssets);
            Step("Voice script", () => VoiceScript.Generate(db));
            Step("Player settings", PlayerSettingsSetup);
            AssetDatabase.SaveAssets();
            Step("Scenes", () => SceneFactory.BuildAll(db));
            AssetDatabase.SaveAssets();
            Debug.Log("[GLITCHED] Build Everything finished.");
        }

        static void Step(string name, Action a)
        {
            Debug.Log("[GLITCHED] >> " + name);
            var t = DateTime.Now;
            a();
            Debug.Log($"[GLITCHED] << {name} ({(DateTime.Now - t).TotalSeconds:0.0}s)");
        }

        public static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void Folders()
        {
            foreach (var f in new[] { Data + "/Characters", Data + "/Enemies", Data + "/Bosses", Data + "/Arcs", Data + "/Themes", Data + "/GraphicsTiers", Data + "/Cosmetics", Data + "/Achievements", Data + "/Kits",
                Art + "/Materials/Generated", Art + "/Materials/Glitch", Art + "/Materials/Toon", Art + "/Meshes", Art + "/Animations/Controllers", Art + "/Fonts/TMP",
                Root + "/Scenes/Arcs", Root + "/Scenes/Test", Root + "/Resources/Localization", Root + "/Prefabs", Root + "/Settings" })
                EnsureFolder(f);
        }

        // ------------------------------------------------------------------ TMP
        static void ImportTMPEssentials()
        {
            if (AssetDatabase.FindAssets("t:TMP_Settings").Length > 0) return;
            string[] candidates =
            {
                "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage",
                "Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage",
            };
            foreach (var c in candidates)
            {
                string full = Path.GetFullPath(c);
                if (File.Exists(full)) { AssetDatabase.ImportPackage(full, false); AssetDatabase.Refresh(); Debug.Log("[GLITCHED] Imported " + c); return; }
            }
            Debug.LogWarning("[GLITCHED] TMP Essential Resources package not found.");
        }

        /// <summary>
        /// Remaps every imported model's materials to converted GLITCHED materials (Glitchable for environments,
        /// Toon for characters), keeping their textures and colours. Converted materials live in Art/Materials/Imported.
        /// </summary>
        /// <summary>
        /// Mixamo FBX files embed their textures. Unpack them once into a Textures_<model> folder next to the FBX, and
        /// drop any texture-less Toon materials made before the unpack so they are rebuilt with the albedo.
        /// </summary>
        static void ExtractEmbeddedTextures()
        {
            foreach (var folder in new[] { Art + "/Characters/Player", Art + "/Characters/Bosses", Art + "/Characters/MPFB" })
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { folder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(g);
                    string texDir = $"{Path.GetDirectoryName(path).Replace('\\', '/')}/Textures_{Path.GetFileNameWithoutExtension(path)}";
                    if (!(AssetImporter.GetAtPath(path) is ModelImporter importer)) continue;
                    bool extracted = AssetDatabase.IsValidFolder(texDir);
                    // redo only if never unpacked, or if a converted material is still missing its albedo
                    bool untextured = !importer.GetExternalObjectMap().Keys.Any(k => k.type == typeof(Material)) || importer.GetExternalObjectMap().Any(kv => kv.Value is Material mat && mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") == null && !mat.name.ToLowerInvariant().Contains("eye") && !mat.name.ToLowerInvariant().Contains("lash"));
                    if (extracted && (!untextured || AssetDatabase.FindAssets("t:Texture2D", new[] { texDir }).Length == 0)) continue;
                    EnsureFolder(texDir);
                    foreach (var kv in importer.GetExternalObjectMap())
                        if (kv.Key.type == typeof(Material))
                        {
                            importer.RemoveRemap(kv.Key);
                            var stale = AssetDatabase.GetAssetPath(kv.Value);
                            if (stale.StartsWith(Art + "/Materials/Imported/")) AssetDatabase.DeleteAsset(stale);
                        }
                    importer.SaveAndReimport();
                    bool ok = extracted || importer.ExtractTextures(texDir);
                    AssetDatabase.Refresh();
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    Debug.Log($"[GLITCHED] Extracted textures of {Path.GetFileName(path)}: {ok}");
                }
            }
        }

        /// <summary>
        /// Unity's humanoid auto-mapper fails on some Mixamo rigs (Mutant: "Required human bone 'LeftHand' not found").
        /// If a character's avatar is invalid, map the standard mixamorig bones explicitly and reimport.
        /// </summary>
        static void FixMixamoAvatars()
        {
            var map = new Dictionary<HumanBodyBones, string>
            {
                { HumanBodyBones.Hips, "Hips" }, { HumanBodyBones.Spine, "Spine" }, { HumanBodyBones.Chest, "Spine1" }, { HumanBodyBones.UpperChest, "Spine2" },
                { HumanBodyBones.Neck, "Neck" }, { HumanBodyBones.Head, "Head" },
                { HumanBodyBones.LeftShoulder, "LeftShoulder" }, { HumanBodyBones.LeftUpperArm, "LeftArm" }, { HumanBodyBones.LeftLowerArm, "LeftForeArm" }, { HumanBodyBones.LeftHand, "LeftHand" },
                { HumanBodyBones.RightShoulder, "RightShoulder" }, { HumanBodyBones.RightUpperArm, "RightArm" }, { HumanBodyBones.RightLowerArm, "RightForeArm" }, { HumanBodyBones.RightHand, "RightHand" },
                { HumanBodyBones.LeftUpperLeg, "LeftUpLeg" }, { HumanBodyBones.LeftLowerLeg, "LeftLeg" }, { HumanBodyBones.LeftFoot, "LeftFoot" }, { HumanBodyBones.LeftToes, "LeftToeBase" },
                { HumanBodyBones.RightUpperLeg, "RightUpLeg" }, { HumanBodyBones.RightLowerLeg, "RightLeg" }, { HumanBodyBones.RightFoot, "RightFoot" }, { HumanBodyBones.RightToes, "RightToeBase" },
            };
            foreach (var folder in new[] { Art + "/Characters/Player", Art + "/Characters/Bosses", Art + "/Animations/Mixamo", Art + "/Characters/MPFB", Art + "/Characters/Scan" })
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { folder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(g);
                    var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
                    if (avatar != null && avatar.isValid && avatar.isHuman) continue;
                    if (!(AssetImporter.GetAtPath(path) is ModelImporter mi)) continue;
                    var hd = mi.humanDescription;
                    var names = new HashSet<string>((hd.skeleton ?? new SkeletonBone[0]).Select(s => s.name));
                    string prefix = names.FirstOrDefault(n => n.EndsWith(":Hips"))?.Replace("Hips", "") ?? (names.Contains("Hips") ? "" : "mixamorig:");
                    hd.human = map.Where(kv => names.Contains(prefix + kv.Value))
                                  .Select(kv => new HumanBone { humanName = HumanTrait.BoneName[(int)kv.Key], boneName = prefix + kv.Value, limit = new HumanLimit { useDefaultValues = true } })
                                  .ToArray();
                    mi.humanDescription = hd;
                    mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    mi.SaveAndReimport();
                    avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
                    Debug.Log($"[GLITCHED] Mixamo avatar fix {Path.GetFileName(path)}: {hd.human.Length} bones, valid={avatar != null && avatar.isValid}");
                }
            }
            TPoseMakeHumans();
        }

        /// <summary>
        /// MakeHuman rigs rest in an A-pose (arms ~45 degrees down), but a humanoid avatar treats its reference pose as a
        /// T-pose (straight arms), so every animation bent the arms out of shape. Rotates the upper arms and forearms in the avatar's reference
        /// skeleton (ModelImporter.humanDescription.skeleton) until they are straight and horizontal, then reimports.
        /// </summary>
        static void TPoseMakeHumans()
        {
            var folders = new[] { Art + "/Characters/MPFB", Art + "/Characters/Scan" }.Where(AssetDatabase.IsValidFolder).ToArray();
            if (folders.Length == 0) return;
            foreach (var g in AssetDatabase.FindAssets("t:Model", folders))
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (!(AssetImporter.GetAtPath(path) is ModelImporter mi)) continue;
                var hd = mi.humanDescription;
                if (hd.skeleton == null || hd.skeleton.Length == 0) continue;
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var inst = UnityEngine.Object.Instantiate(model);
                var bones = inst.GetComponentsInChildren<Transform>(true);
                Transform B(string n) => bones.FirstOrDefault(t => t.name == n);
                // start from the reference pose currently stored in the importer
                foreach (var sb in hd.skeleton) { var t = B(sb.name); if (t != null && t != inst.transform) { t.localPosition = sb.position; t.localRotation = sb.rotation; t.localScale = sb.scale; } }
                float worst = 0f;
                foreach (var side in new[] { "Left", "Right" })
                {
                    var arm = B("mixamorig:" + side + "Arm");
                    var fore = B("mixamorig:" + side + "ForeArm");
                    var hand = B("mixamorig:" + side + "Hand");
                    if (arm == null || fore == null || hand == null) continue;
                    var dir = (fore.position - arm.position).normalized;
                    var target = new Vector3(Mathf.Sign(dir.x), 0, 0);
                    worst = Mathf.Max(worst, Vector3.Angle(dir, target));
                    arm.rotation = Quaternion.FromToRotation(dir, target) * arm.rotation;
                    var d2 = (hand.position - fore.position).normalized;   // straighten the elbow too (MakeHuman rests it bent ~45 degrees)
                    worst = Mathf.Max(worst, Vector3.Angle(d2, target));
                    fore.rotation = Quaternion.FromToRotation(d2, target) * fore.rotation;
                }
                if (worst > 3f)
                {
                    var sk = hd.skeleton;
                    for (int i = 0; i < sk.Length; i++) { var t = B(sk[i].name); if (t != null && t != inst.transform) sk[i].rotation = t.localRotation; }
                    hd.skeleton = sk;
                    mi.humanDescription = hd;
                    mi.SaveAndReimport();
                    var av = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
                    Debug.Log($"[GLITCHED] T-posed {Path.GetFileName(path)}: arms were {worst:0} deg off, avatar valid={av != null && av.isValid && av.isHuman}");
                }
                UnityEngine.Object.DestroyImmediate(inst);
            }
        }

        static Texture2D UnpackedTexture(string modelPath, string file)
        {
            string dir = $"{Path.GetDirectoryName(modelPath).Replace('\\', '/')}/Textures_{Path.GetFileNameWithoutExtension(modelPath)}";
            if (!AssetDatabase.IsValidFolder(dir)) return null;
            foreach (var g in AssetDatabase.FindAssets(file + " t:Texture2D", new[] { dir }))
            {
                var tp = AssetDatabase.GUIDToAssetPath(g);
                if (string.Equals(Path.GetFileNameWithoutExtension(tp), file, StringComparison.OrdinalIgnoreCase)) return AssetDatabase.LoadAssetAtPath<Texture2D>(tp);
            }
            return null;
        }

        /// <summary>MakeHuman materials: normal map from the matching _normal texture, alpha-cut + two-sided hair and lashes, skin roughness.</summary>
        static void SetupRealisticCharacterMaterial(Material conv, string modelPath, string matName, Texture tex)
        {
            string n = matName.ToLowerInvariant();
            string model = Path.GetFileNameWithoutExtension(modelPath).ToLowerInvariant();
            conv.SetFloat("_Glitchiness", 0.4f);
            // per-character colour: Shaurya has a warm brown complexion and black hair
            if (model == "shaurya")
            {
                if (n.EndsWith(".body")) conv.SetColor("_BaseColor", new Color(0.8f, 0.62f, 0.5f));
                if (n.Contains("short0") || n.Contains("brow")) conv.SetColor("_BaseColor", new Color(0.17f, 0.14f, 0.13f));
            }
            if (n.Contains("high-poly") || n.Contains("low-poly")) conv.SetFloat("_Roughness", 0.08f);
            conv.SetFloat("_Roughness", n.Contains("skin") || n.Contains("body") ? 0.6f : n.Contains("eye") && !n.Contains("brow") && !n.Contains("lash") ? 0.1f : 0.85f);
            if (tex != null)
            {
                string normalName = tex.name.Replace("_diffuse", "_normal").Replace("diffuse", "normal");
                var nt = UnpackedTexture(modelPath, normalName);
                if (nt != null && nt != tex) conv.SetTexture("_BumpMap", nt);
            }
            if (n.Contains("hair") || n.Contains("brow") || n.Contains("lash") || n.Contains("short0") || n.Contains("long0") || n.Contains("bob0") || n.Contains("braid") || n.Contains("ponytail") || n.Contains("afro"))
            {
                conv.SetFloat("_AlphaClip", 1); conv.EnableKeyword("_ALPHATEST_ON");
                // MakeHuman hair cards are thin strands: a high cutoff erases them up close (mips hid it from afar)
                conv.SetFloat("_Cutoff", n.Contains("brow") || n.Contains("lash") ? 0.3f : 0.12f);
                conv.SetFloat("_Cull", 0);
                conv.SetFloat("_Roughness", 0.55f);
            }
            // the high-poly eye has a transparent cornea shell over the iris: cut it out, or the eyes render blank white
            if (n.Contains("high-poly") || n.Contains("low-poly"))
            {
                conv.SetFloat("_AlphaClip", 1); conv.EnableKeyword("_ALPHATEST_ON");
                conv.SetFloat("_Cutoff", 0.5f);
            }
            // clothes are thin shells: draw their inside too (open collars, sleeve ends)
            if (n.Contains("suit") || n.Contains("shoes")) conv.SetFloat("_Cull", 0);
        }

        static void ReimportModelsIfNeeded()
        {
            EnsureFolder(Art + "/Materials/Imported");
            var glitch = Shader.Find("Glitched/Glitchable");
            var toon = Shader.Find("Glitched/Toon");
            if (glitch == null || toon == null) { Debug.LogError("[GLITCHED] GLITCHED shaders missing"); return; }
            ExtractEmbeddedTextures();
            FixMixamoAvatars();
            // the Quaternius animation libraries are oriented by the body (their root faces backwards): reimport old imports
            foreach (var ualPath in new[] { Art + "/Animations/UAL1/UAL1_Standard.fbx", Art + "/Animations/UAL2/UAL2_Standard.fbx" })
                if (AssetImporter.GetAtPath(ualPath) is ModelImporter ui && ui.clipAnimations.Length > 0 && ui.clipAnimations[0].keepOriginalOrientation)
                    AssetDatabase.ImportAsset(ualPath, ImportAssetOptions.ForceUpdate);
            int remapped = 0;
            var toImport = new List<string>();
            foreach (var folder in new[] { Art + "/Environments", Art + "/Characters" })
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { folder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(g);
                    var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                    if (importer == null) continue;
                    bool character = path.Contains("/Art/Characters/") || Path.GetFileName(path).StartsWith("Enemy_") || path.Contains("/Aliens/") || path.Contains("/Platformer/FBX/Enemies/");
                    var existing = importer.GetExternalObjectMap();
                    if (path.Contains("/Characters/MPFB/") && existing.Values.Any(o => o != null && !AssetDatabase.GetAssetPath(o).Contains("/MPFB_" + Path.GetFileNameWithoutExtension(path) + "_")))
                    {
                        // remapped to materials shared between all the MakeHuman people: drop them and convert again per model
                        foreach (var id in existing.Keys.ToList()) importer.RemoveRemap(id);
                        importer.SaveAndReimport();
                        existing = importer.GetExternalObjectMap();
                    }
                    bool changed = false;
                    foreach (var m in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())
                    {
                        var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name);
                        if (existing.ContainsKey(id)) continue;
                        string pack = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(path)));
                        // MakeHuman people all name their materials Human.*: give each model its own set
                        if (path.Contains("/Characters/MPFB/")) pack = "MPFB_" + Path.GetFileNameWithoutExtension(path);
                        string matPath = $"{Art}/Materials/Imported/{Safe(pack)}_{Safe(m.name)}{(character ? "_Toon" : "")}.mat";
                        var conv = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                        if (conv != null && path.Contains("/Characters/MPFB/"))
                        {
                            SetupRealisticCharacterMaterial(conv, path, m.name, conv.GetTexture("_BaseMap"));   // keep tuning in step with the code
                            EditorUtility.SetDirty(conv);
                        }
                        if (conv == null)
                        {
                            bool real = path.Contains("/Characters/MPFB/");
                            var realSh = Shader.Find("Glitched/Realistic");
                            conv = new Material(real && realSh != null ? realSh : character ? toon : glitch) { name = Path.GetFileNameWithoutExtension(matPath) };
                            Texture tex = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
                            if (tex == null && m.HasProperty("_MainTex")) tex = m.GetTexture("_MainTex");
                            if (tex == null && GlitchedImporter.DiffuseFile.TryGetValue(path + "|" + m.name, out var file)) tex = UnpackedTexture(path, file);
                            Color col = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : (m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white);
                            if (tex != null) conv.SetTexture("_BaseMap", tex);
                            conv.SetColor("_BaseColor", col);
                            if (real) SetupRealisticCharacterMaterial(conv, path, m.name, tex);
                            if (m.HasProperty("_EmissionColor") && m.IsKeywordEnabled("_EMISSION")) conv.SetColor("_EmissionColor", m.GetColor("_EmissionColor"));
                            if (!character) { conv.SetFloat("_Glitchiness", path.Contains("/Shared/") ? 1f : 0.8f); conv.SetFloat("_AlbedoContrast", 0.75f); }
                            conv.enableInstancing = true;
                            AssetDatabase.CreateAsset(conv, matPath);
                        }
                        else if (character && conv.HasProperty("_BaseMap") && conv.GetTexture("_BaseMap") == null
                                 && GlitchedImporter.DiffuseFile.TryGetValue(path + "|" + m.name, out var lateFile))
                        {
                            // a Toon material made before its textures were unpacked: give it the albedo now
                            var lateTex = UnpackedTexture(path, lateFile);
                            if (lateTex != null) { conv.SetTexture("_BaseMap", lateTex); conv.SetColor("_BaseColor", Color.white); EditorUtility.SetDirty(conv); }
                        }
                        importer.AddRemap(id, conv);
                        changed = true;
                        remapped++;
                    }
                    if (changed) toImport.Add(path);
                }
            }
            if (toImport.Count > 0)
            {
                AssetDatabase.StartAssetEditing();
                try { foreach (var p in toImport) { AssetImporter.GetAtPath(p).SaveAndReimport(); } }
                finally { AssetDatabase.StopAssetEditing(); }
            }
            Debug.Log($"[GLITCHED] Remapped {remapped} model materials on {toImport.Count} models to GLITCHED shaders.");
        }

        // ------------------------------------------------------------------ fonts
        static TMP_FontAsset Font(string file, string assetName, int size = 90, List<TMP_FontAsset> fallbacks = null)
        {
            string path = $"{Art}/Fonts/TMP/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;
            var font = AssetDatabase.LoadAssetAtPath<Font>($"{Art}/Fonts/{file}");
            if (font == null) { Debug.LogWarning("[GLITCHED] Missing font " + file); return null; }
            var fa = TMP_FontAsset.CreateFontAsset(font, size, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            fa.name = assetName;
            AssetDatabase.CreateAsset(fa, path);
            if (fa.atlasTextures != null)
                foreach (var t in fa.atlasTextures) { if (t != null) { t.name = assetName + " Atlas"; AssetDatabase.AddObjectToAsset(t, fa); } }
            if (fa.material != null) { fa.material.name = assetName + " Material"; AssetDatabase.AddObjectToAsset(fa.material, fa); }
            if (fallbacks != null) fa.fallbackFontAssetTable = fallbacks.Where(f => f != null).ToList();
            EditorUtility.SetDirty(fa);
            return fa;
        }

        static TMP_FontAsset fDisplay, fBody, fMono, fDys, fDeva, fSymbols;

        static void BuildFonts()
        {
            if (TMP_Settings.instance == null) { AssetDatabase.Refresh(); }
            if (TMP_Settings.instance == null) { Debug.LogWarning("[GLITCHED] TMP Settings not loaded yet: run Build Everything again."); return; }
            fSymbols = Font("DejaVuSans.ttf", "Glitched_Symbols", 80);
            var fb = new List<TMP_FontAsset> { fSymbols };
            fDeva = Font("Hind-Medium.ttf", "Glitched_Devanagari", 80, fb);
            var fb2 = new List<TMP_FontAsset> { fSymbols, fDeva };
            fDisplay = Font("RussoOne-Regular.ttf", "Glitched_Display", 90, fb2);
            fBody = Font("ChakraPetch-Medium.ttf", "Glitched_Body", 90, fb2);
            fMono = Font("JetBrainsMono.ttf", "Glitched_Mono", 90, fb2);
            fDys = Font("OpenDyslexic-Regular.otf", "Glitched_Dyslexic", 80, fb2);
            // TMP's default font falls back to ours too (so glyphs like ★ ◆ ⇈ render everywhere)
            var def = TMP_Settings.defaultFontAsset;
            if (def != null && fSymbols != null && (def.fallbackFontAssetTable == null || !def.fallbackFontAssetTable.Contains(fSymbols)))
            {
                if (def.fallbackFontAssetTable == null) def.fallbackFontAssetTable = new List<TMP_FontAsset>();
                def.fallbackFontAssetTable.Add(fSymbols);
                EditorUtility.SetDirty(def);
            }
            var globals = TMP_Settings.fallbackFontAssets;
            if (globals != null && fSymbols != null && !globals.Contains(fSymbols)) { globals.Add(fSymbols); EditorUtility.SetDirty(TMP_Settings.instance); }
        }

        // ------------------------------------------------------------------ materials
        /// <summary>One PBRSet per folder under Art/Textures/PBR (ambientCG naming: _Color, _NormalGL, _Roughness, _AmbientOcclusion, _Metalness).</summary>
        static List<PBRSet> CollectPBRSets()
        {
            var list = new List<PBRSet>();
            string root = Art + "/Textures/PBR";
            if (!AssetDatabase.IsValidFolder(root)) return list;
            foreach (var dir in AssetDatabase.GetSubFolders(root))
            {
                var set = new PBRSet { id = Path.GetFileName(dir) };
                foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { dir }))
                {
                    var p = AssetDatabase.GUIDToAssetPath(g);
                    var n = Path.GetFileNameWithoutExtension(p).ToLowerInvariant();
                    var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                    // prefer the 2K files when both resolutions exist
                    bool hi = n.Contains("_2k");
                    if (n.EndsWith("_color")) { if (set.albedo == null || hi) set.albedo = t; }
                    else if (n.EndsWith("_normalgl")) { if (set.normal == null || hi) set.normal = t; }
                    else if (n.EndsWith("_roughness")) { if (set.roughness == null || hi) set.roughness = t; }
                    else if (n.EndsWith("_ambientocclusion")) { if (set.occlusion == null || hi) set.occlusion = t; }
                    else if (n.EndsWith("_metalness")) { if (set.metalness == null || hi) set.metalness = t; }
                }
                if (set.albedo != null) list.Add(set);
            }
            Debug.Log($"[GLITCHED] PBR sets: {list.Count} ({string.Join(", ", list.Select(s => s.id))})");
            return list;
        }

        public static Material Mat(string name, string shader, Action<Material> setup = null, string folder = null)
        {
            if (folder != null) EnsureFolder(folder);
            string path = $"{folder ?? Art + "/Materials/Glitch"}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            var sh = Shader.Find(shader);
            if (sh == null) { Debug.LogError("[GLITCHED] Shader not found: " + shader); return null; }
            m = new Material(sh) { name = name };
            setup?.Invoke(m);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static MaterialLibrarySO lib;

        static void BuildMaterials()
        {
            string path = Data + "/MaterialLibrary.asset";
            lib = AssetDatabase.LoadAssetAtPath<MaterialLibrarySO>(path);
            if (lib == null) { lib = ScriptableObject.CreateInstance<MaterialLibrarySO>(); AssetDatabase.CreateAsset(lib, path); }
            lib.glitchable = Mat("Glitchable", "Glitched/Glitchable");
            lib.glitchableEmissive = Mat("GlitchableEmissive", "Glitched/Glitchable", m => { m.SetColor("_EmissionColor", new Color(0.5f, 1.5f, 2f)); m.EnableKeyword("_EMISSION"); });
            lib.toon = Mat("Toon", "Glitched/Toon", null, Art + "/Materials/Toon");
            lib.faceDecal = Mat("FaceDecal", "Glitched/FaceDecal", null, Art + "/Materials/Toon");
            lib.sky = Mat("Sky", "Glitched/Sky");
            lib.hologram = Mat("Hologram", "Glitched/Hologram");
            lib.impostor = Mat("Impostor", "Glitched/Impostor", m => m.enableInstancing = true);
            lib.flow = Mat("Flow", "Glitched/Flow");
            lib.particleAdditive = Mat("ParticleAdditive", "Glitched/Particle", m => { m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.One); });
            lib.particleAlpha = Mat("ParticleAlpha", "Glitched/Particle", m => { m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); });
            lib.uiGlitch = Mat("UIGlitch", "Glitched/UIGlitch");
            lib.fullscreenGlitch = Mat("FullscreenGlitch", "Glitched/FullscreenGlitch");
            lib.grid = Mat("Grid", "Glitched/Grid");
            lib.dither = Mat("ForegroundDither", "Glitched/ForegroundDither");
            lib.realistic = Mat("Realistic", "Glitched/Realistic", m => m.enableInstancing = true, Art + "/Materials/Realistic");
            lib.facade = Mat("Facade", "Glitched/Facade", m => m.enableInstancing = true, Art + "/Materials/Realistic");
            lib.terrain = Mat("Terrain", "Glitched/Terrain", null, Art + "/Materials/Realistic");
            lib.water = Mat("Water", "Glitched/Water", null, Art + "/Materials/Realistic");
            lib.pbr = CollectPBRSets();
            lib.leafAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/Textures/Foliage/leaf_atlas.png");
            lib.particleTextures = AssetDatabase.FindAssets("t:Texture2D", new[] { Art + "/Textures/VFX" }).Select(g => AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(g))).Where(t => t != null).ToArray();
            EditorUtility.SetDirty(lib);
            // the runtime material cache saves new variants as assets while baking
            MaterialCache.ClearCache();
            MaterialCache.OnCreated = SaveGeneratedMaterial;
            ProceduralMeshes.OnCreated = SaveGeneratedMesh;
        }

        // ------------------------------------------------------------------ generated characters
        /// <summary>
        /// Builds the original GLITCHED characters with HumanGen (Aarav + a cast of citizens) and saves each one as a
        /// prefab with its skinned mesh and humanoid avatar. They replace the imported Mixamo bodies.
        /// </summary>
        static void BuildCharacters()
        {
            string dir = Root + "/Prefabs/Characters";
            EnsureFolder(dir);
            CityMaterials.Library = db.materials;
            CityMaterials.ClearCache();
            var aarav = db.Character("aarav");
            var spec = HumanSpec.Aarav();
            // realistic MakeHuman people (Tools/mpfb_characters.py) replace the code-built ones when they exist
            var mpfbShaurya = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/Characters/MPFB/shaurya.fbx");
            // the real Shaurya (a 3D scan, rigged by Mixamo) wins over everything else
            var scanShaurya = MakeScanCharacter("Shaurya", dir);
            var prefab = scanShaurya != null ? scanShaurya : mpfbShaurya != null ? mpfbShaurya : MakeHuman(spec, dir);
            if (aarav != null && prefab != null)
            {
                aarav.modelPrefab = prefab;
                aarav.height = scanShaurya != null ? 1.75f : spec.height;
                // the real Shaurya talks with his real face (Resources/Portraits/aarav.png, Tools/render_portrait.py)
                if (scanShaurya != null && Resources.Load<Texture2D>("Portraits/aarav") != null) { aarav.faceFolder = ""; aarav.useFaceDecal = false; }
                aarav.displayName = "Shaurya";
                aarav.catchphrase = "I was GAMING twenty minutes ago.";
                aarav.animator = null;   // re-linked to the humanoid controller by the Animators step
                EditorUtility.SetDirty(aarav);
            }
            // Kanha the Dard, the main antagonist (a 3D scan, rigged on Shaurya's skeleton: Tools/rig_from_template.py)
            var kanha = db.Character("kanha");
            var scanKanha = MakeScanCharacter("Kanha", dir);
            if (kanha != null && scanKanha != null)
            {
                kanha.modelPrefab = scanKanha;
                kanha.height = 1.68f;
                kanha.humanoid = true;
                kanha.animator = null;
                EditorUtility.SetDirty(kanha);
            }
            db.citizenModels.Clear();
            db.citizenHeights.Clear();
            for (int i = 0; i < 10; i++)
            {
                var cs = HumanSpec.Citizen(1009 + i * 7919);
                var mp = AssetDatabase.LoadAssetAtPath<GameObject>($"{Art}/Characters/MPFB/citizen_{i}.fbx");
                var cp = mp != null ? mp : MakeHuman(cs, dir);
                if (cp == null) continue;
                db.citizenModels.Add(cp);
                db.citizenHeights.Add(cs.height);
            }
            // original robots for the realistic Code City (crypto cracker, Sergeant Syntax, Lord Infinite Loop)
            void Assign(EnemyDataSO e, HumanSpec rs)
            {
                if (e == null) return;
                var rp = MakeHuman(rs, dir);
                if (rp == null) return;
                e.modelPrefab = rp; e.height = rs.height;
                EditorUtility.SetDirty(e);
            }
            var cc = db.arcs.Find(a => a != null && a.id == "CodeCity");
            Assign(db.enemies.Cast<EnemyDataSO>().FirstOrDefault(x => x != null && x.id == "crypto_cracker"), HumanSpec.SecurityBot());
            if (cc != null) { Assign(cc.miniBoss, HumanSpec.SergeantBot()); Assign(cc.boss, HumanSpec.LoopMech()); }
            void AssignMachine(string enemyId, MachineGen.Kind kind, float height)
            {
                var e = db.enemies.FirstOrDefault(x => x != null && x.id == enemyId);
                if (e == null) return;
                var mp = MakeMachine(kind, dir, enemyId);
                if (mp == null) return;
                e.modelPrefab = mp; e.height = height;
                EditorUtility.SetDirty(e);
            }
            AssignMachine("loop_drone", MachineGen.Kind.Drone, 0.55f);
            AssignMachine("bug_monster", MachineGen.Kind.Bug, 0.7f);
            AssignMachine("syntax_slime", MachineGen.Kind.Slime, 0.8f);
            AssignMachine("off_by_one", MachineGen.Kind.Delivery, 1.25f);
            // Physics Valley: lab machines gone wrong, a copper robot and a judge-mech
            AssignMachine("gravity_goon", MachineGen.Kind.Slime, 0.9f);
            AssignMachine("static_spark", MachineGen.Kind.Drone, 0.55f);
            AssignMachine("momentum_mite", MachineGen.Kind.Bug, 0.6f);
            AssignMachine("tesla_wraith", MachineGen.Kind.Drone, 0.8f);
            // Chemical Chaos: plant maintenance machines and two robots
            AssignMachine("hydrogen_hopper", MachineGen.Kind.Drone, 0.5f);
            AssignMachine("oxy_brute", MachineGen.Kind.Bug, 0.9f);
            AssignMachine("carbon_clump", MachineGen.Kind.Slime, 0.85f);
            AssignMachine("sodium_sprinter", MachineGen.Kind.Delivery, 1.1f);
            AssignMachine("acid_alchemist", MachineGen.Kind.Drone, 0.75f);
            var ch = db.arcs.Find(a => a != null && a.id == "ChemicalChaos");
            if (ch != null)
            {
                Assign(ch.miniBoss, new HumanSpec { id = "count_catalyst", height = 2.6f, robot = true, bulk = 0.5f, plate = new Color(0.85f, 0.86f, 0.84f), plateDark = new Color(0.12f, 0.2f, 0.14f), glow = new Color(0.4f, 1f, 0.45f), visor = true });
                Assign(ch.boss, new HumanSpec { id = "lord_of_the_lab", height = 4f, robot = true, bulk = 0.8f, plate = new Color(0.9f, 0.9f, 0.88f), plateDark = new Color(0.1f, 0.25f, 0.2f), glow = new Color(0.3f, 1f, 0.4f), visor = false, antenna = true });
            }
            // Bio-Zone: lab containment machines; The Core: corrupted fragments of the city's systems
            AssignMachine("virus_swarmling", MachineGen.Kind.Drone, 0.45f);
            AssignMachine("rogue_cell", MachineGen.Kind.Slime, 0.8f);
            AssignMachine("plaque_puff", MachineGen.Kind.Slime, 0.7f);
            AssignMachine("antibody_imposter", MachineGen.Kind.Bug, 0.8f);
            AssignMachine("glitch_fragment", MachineGen.Kind.Drone, 0.6f);
            var bz = db.arcs.Find(a => a != null && a.id == "BioZone");
            if (bz != null)
            {
                Assign(bz.miniBoss, new HumanSpec { id = "professor_plaque", height = 2.4f, robot = true, bulk = 0.55f, plate = new Color(0.92f, 0.9f, 0.86f), plateDark = new Color(0.4f, 0.32f, 0.2f), glow = new Color(1f, 0.85f, 0.3f), visor = true });
                Assign(bz.boss, new HumanSpec { id = "lord_within", height = 4f, robot = true, bulk = 0.7f, plate = new Color(0.55f, 0.12f, 0.16f), plateDark = new Color(0.16f, 0.05f, 0.06f), glow = new Color(1f, 0.3f, 0.4f), visor = true });
            }
            var tc = db.arcs.Find(a => a != null && a.id == "TheCore");
            if (tc != null) Assign(tc.boss, new HumanSpec { id = "the_source", height = 6f, robot = true, bulk = 0.85f, plate = new Color(0.9f, 0.92f, 0.95f), plateDark = new Color(0.2f, 0.18f, 0.28f), glow = new Color(0.85f, 0.5f, 1f), visor = true, antenna = true });
            Assign(db.enemies.FirstOrDefault(x => x != null && x.id == "source_shard"), new HumanSpec { id = "source_shard", height = 2.1f, robot = true, bulk = 0.35f, plate = new Color(0.82f, 0.84f, 0.9f), plateDark = new Color(0.18f, 0.16f, 0.26f), glow = new Color(0.85f, 0.5f, 1f), visor = true });
            // unique creature bodies for three Glitch Lords (assigned after the robot versions below)
            void AssignCreature(EnemyDataSO e, CreatureGen.Kind kind, float height)
            {
                if (e == null) return;
                var cp = MakeGenerated(() => CreatureGen.Build(kind), dir, e.id);
                if (cp == null) return;
                e.modelPrefab = cp; e.height = height;
                EditorUtility.SetDirty(e);
            }
            var pv = db.arcs.Find(a => a != null && a.id == "PhysicsValley");
            if (pv != null)
            {
                Assign(pv.miniBoss, new HumanSpec { id = "baron_friction", height = 2.4f, robot = true, bulk = 0.75f, plate = new Color(0.62f, 0.38f, 0.22f), plateDark = new Color(0.14f, 0.11f, 0.09f), glow = new Color(1f, 0.6f, 0.2f), visor = false });
                Assign(pv.boss, new HumanSpec { id = "lord_broken_laws", height = 4.2f, robot = true, bulk = 0.9f, plate = new Color(0.3f, 0.24f, 0.42f), plateDark = new Color(0.1f, 0.08f, 0.13f), glow = new Color(0.75f, 0.45f, 1f), visor = true, antenna = true });
            }
            if (bz != null) AssignCreature(bz.boss, CreatureGen.Kind.VirusTitan, 5.5f);
            if (pv != null) AssignCreature(pv.boss, CreatureGen.Kind.CrystalJudge, 5f);
            if (tc != null) AssignCreature(tc.boss, CreatureGen.Kind.ShardTitan, 7f);
            BuildHQBosses();   // the realistic Sketchfab bodies replace the generated ones where they are installed
            EditorUtility.SetDirty(db);
            Debug.Log($"[GLITCHED] Characters: aarav + {db.citizenModels.Count} citizens + robots + creature bosses");
        }

        /// <summary>Generated machine enemy saved as a prefab; its meshes go into one asset file next to it.</summary>
        static GameObject MakeMachine(MachineGen.Kind kind, string dir, string id) => MakeGenerated(() => MachineGen.Build(kind), dir, id);

        static GameObject MakeGenerated(Func<GameObject> build, string dir, string id)
        {
            var meshes = new List<Mesh>();
            var prev = SceneMeshes.OnCreated;
            SceneMeshes.OnCreated = m => meshes.Add(m);
            GameObject go;
            try { go = build(); }
            finally { SceneMeshes.OnCreated = prev; }
            go.name = id;
            string meshPath = $"{dir}/{id}_Meshes.asset";
            if (AssetDatabase.LoadMainAssetAtPath(meshPath) != null) AssetDatabase.DeleteAsset(meshPath);
            if (meshes.Count > 0)
            {
                AssetDatabase.CreateAsset(meshes[0], meshPath);
                for (int i = 1; i < meshes.Count; i++) AssetDatabase.AddObjectToAsset(meshes[i], meshPath);
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{dir}/{id}.prefab");
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>
        /// A scanned person (Art/Characters/Scan/&lt;name&gt;.fbx + &lt;name&gt;_albedo.png/jpg) as a prefab with a realistic
        /// material: scans carry their lighting in the colour map, so they get a soft, mostly rough surface.
        /// </summary>
        static GameObject MakeScanCharacter(string name, string dir)
        {
            string fbx = $"{Art}/Characters/Scan/{name}.fbx";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            if (model == null) return null;
            Texture2D albedo = null;
            foreach (var ext in new[] { ".png", ".jpg" })
                albedo = albedo ?? AssetDatabase.LoadAssetAtPath<Texture2D>($"{Art}/Characters/Scan/{name}_albedo{ext}");
            EnsureFolder(Art + "/Materials/Imported");
            string matPath = $"{Art}/Materials/Imported/Scan_{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            var sh = Shader.Find("Glitched/Realistic");
            if (mat == null) { mat = new Material(sh) { name = "Scan_" + name }; AssetDatabase.CreateAsset(mat, matPath); }
            else if (sh != null) mat.shader = sh;
            if (albedo != null) mat.SetTexture("_BaseMap", albedo);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Roughness", 0.78f);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Glitchiness", 0.3f);
            mat.SetFloat("_Cull", 0);   // scans have thin, open edges (cuffs, collar)
            // GPU-baked detail (Tools/bake_scan_maps.py): fabric and hair relief, and contact darkening in the folds
            var nrm = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Art}/Characters/Scan/{name}_normal.png");
            var ao = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Art}/Characters/Scan/{name}_ao.png");
            mat.SetTexture("_BumpMap", nrm); mat.SetFloat("_BumpScale", nrm != null ? 0.7f : 1f);
            mat.SetTexture("_OcclusionMap", ao); mat.SetFloat("_OcclusionStrength", ao != null ? 0.85f : 0f);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true)) r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();
            var prefab = PrefabUtility.SaveAsPrefabAsset(inst, $"{dir}/Scan_{name}.prefab");
            UnityEngine.Object.DestroyImmediate(inst);
            var av = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<Avatar>().FirstOrDefault();
            Debug.Log($"[GLITCHED] Scan character {name}: avatar {(av != null && av.isValid && av.isHuman ? "humanoid OK" : "MISSING/INVALID")}, texture {(albedo != null ? albedo.name : "none")}");
            return prefab;
        }

        static GameObject MakeHuman(HumanSpec spec, string dir)
        {
            var res = new HumanGen(spec).Build(null, HumanGen.Materials(spec));
            if (res.avatar == null || !res.avatar.isValid) Debug.LogWarning("[GLITCHED] Invalid avatar for " + spec.id);
            string meshPath = $"{dir}/{spec.id}_Body.asset";
            if (AssetDatabase.LoadMainAssetAtPath(meshPath) != null) AssetDatabase.DeleteAsset(meshPath);
            AssetDatabase.CreateAsset(res.mesh, meshPath);
            AssetDatabase.AddObjectToAsset(res.avatar, meshPath);
            var prefab = PrefabUtility.SaveAsPrefabAsset(res.root, $"{dir}/{spec.id}.prefab");
            UnityEngine.Object.DestroyImmediate(res.root);
            return prefab;
        }

        static readonly Dictionary<string, Material> generatedMats = new Dictionary<string, Material>();

        static Material SaveGeneratedMaterial(Material m)
        {
            if (m == null || AssetDatabase.Contains(m)) return m;
            string safe = string.Join("_", m.name.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_").Replace("(", "").Replace(")", "");
            if (safe.Length > 60) safe = safe.Substring(0, 60);
            string path = $"{Art}/Materials/Generated/{safe}.mat";
            int n = 1;
            while (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            {
                var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (SameMaterial(existing, m)) return existing;
                path = $"{Art}/Materials/Generated/{safe}_{n++}.mat";
            }
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static bool SameMaterial(Material a, Material b)
        {
            if (a.shader != b.shader) return false;
            foreach (var p in new[] { "_BaseColor", "_EmissionColor", "_TopColor", "_WindowColor" })
                if (a.HasProperty(p) && b.HasProperty(p) && a.GetColor(p) != b.GetColor(p)) return false;
            foreach (var p in new[] { "_Glitchiness", "_WindowFlicker", "_OutlineWidth", "_TriplanarScale", "_Taper", "_Seed", "_Alpha" })
                if (a.HasProperty(p) && b.HasProperty(p) && !Mathf.Approximately(a.GetFloat(p), b.GetFloat(p))) return false;
            if (a.HasProperty("_BaseMap") && b.HasProperty("_BaseMap") && a.GetTexture("_BaseMap") != b.GetTexture("_BaseMap")) return false;
            if (a.HasProperty("_Flow") && b.HasProperty("_Flow") && a.GetVector("_Flow") != b.GetVector("_Flow")) return false;
            return a.shaderKeywords.SequenceEqual(b.shaderKeywords);
        }

        static Mesh SaveGeneratedMesh(Mesh mesh)
        {
            string path = $"{Art}/Meshes/{mesh.name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        // ------------------------------------------------------------------ URP + quality
        static UniversalRenderPipelineAsset mobileLite, balanced, pcHigh;

        static UniversalRenderPipelineAsset CopyRP(string from, string to, Action<UniversalRenderPipelineAsset> setup)
        {
            var a = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(to);
            if (a == null)
            {
                if (!AssetDatabase.CopyAsset(from, to)) { Debug.LogError("[GLITCHED] Could not copy " + from); return null; }
                a = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(to);
            }
            setup(a);
            EditorUtility.SetDirty(a);
            return a;
        }

        static void BuildRenderPipelines()
        {
            mobileLite = CopyRP("Assets/Settings/Mobile_RPAsset.asset", Root + "/Settings/URP_MobileLite.asset", a =>
            {
                a.renderScale = 0.75f; a.msaaSampleCount = 1; a.shadowDistance = 0; a.supportsHDR = false;
                a.supportsCameraDepthTexture = false; a.supportsCameraOpaqueTexture = false;
            });
            balanced = CopyRP("Assets/Settings/PC_RPAsset.asset", Root + "/Settings/URP_Balanced.asset", a =>
            {
                a.renderScale = 0.9f; a.msaaSampleCount = 2; a.shadowDistance = 35; a.shadowCascadeCount = 1; a.mainLightShadowmapResolution = 512; a.supportsHDR = true;
                a.supportsCameraDepthTexture = false; a.supportsCameraOpaqueTexture = false;
            });
            pcHigh = CopyRP("Assets/Settings/PC_RPAsset.asset", Root + "/Settings/URP_PCHigh.asset", a =>
            {
                a.renderScale = 1f; a.msaaSampleCount = 4; a.shadowDistance = 60; a.shadowCascadeCount = 2; a.mainLightShadowmapResolution = 2048; a.supportsHDR = true;
                a.supportsCameraDepthTexture = true;
                // grade in HDR (no banding in skies and fog, highlights keep their colour) with a fine LUT
                a.colorGradingMode = ColorGradingMode.HighDynamicRange; a.colorGradingLutSize = 64;
                a.hdrColorBufferPrecision = HDRColorBufferPrecision._64Bits;
                a.maxAdditionalLightsCount = 8;
            });
            AddFullscreenGlitchFeature();

            // five quality levels: Potato, Low, Medium, High, Ultra
            var qsAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset").FirstOrDefault();
            if (qsAsset != null)
            {
                var so = new SerializedObject(qsAsset);
                var levels = so.FindProperty("m_QualitySettings");
                string[] names = { "Potato", "Low", "Medium", "High", "Ultra" };
                RenderPipelineAsset[] rps = { mobileLite, mobileLite, balanced, pcHigh, pcHigh };
                float[] lod = { 0.4f, 0.6f, 1f, 1.5f, 2f };
                int[] vsync = { 0, 0, 0, 0, 1 };
                int[] tex = { 2, 1, 0, 0, 0 };
                if (levels.arraySize == 0) levels.InsertArrayElementAtIndex(0);
                while (levels.arraySize < 5) levels.InsertArrayElementAtIndex(levels.arraySize - 1);
                while (levels.arraySize > 5) levels.DeleteArrayElementAtIndex(levels.arraySize - 1);
                for (int i = 0; i < 5; i++)
                {
                    var l = levels.GetArrayElementAtIndex(i);
                    l.FindPropertyRelative("name").stringValue = names[i];
                    var rp = l.FindPropertyRelative("customRenderPipeline"); if (rp != null) rp.objectReferenceValue = rps[i];
                    var lb = l.FindPropertyRelative("lodBias"); if (lb != null) lb.floatValue = lod[i];
                    var vs = l.FindPropertyRelative("vSyncCount"); if (vs != null) vs.intValue = vsync[i];
                    var gt = l.FindPropertyRelative("globalTextureMipmapLimit"); if (gt != null) gt.intValue = tex[i];
                    var sp = l.FindPropertyRelative("softParticles"); if (sp != null) sp.boolValue = i >= 3;
                    var ar = l.FindPropertyRelative("antiAliasing"); if (ar != null) ar.intValue = 0;
                }
                so.FindProperty("m_CurrentQuality").intValue = 2;
                var per = so.FindProperty("m_PerPlatformDefaultQuality");
                if (per != null)
                    for (int i = 0; i < per.arraySize; i++)
                    {
                        var e = per.GetArrayElementAtIndex(i);
                        var first = e.FindPropertyRelative("first"); var second = e.FindPropertyRelative("second");
                        if (first != null && second != null) second.intValue = first.stringValue == "Android" ? 1 : 2;
                    }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            GraphicsSettings.defaultRenderPipeline = balanced;
        }

        static void AddFullscreenGlitchFeature()
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
            if (data == null) return;
            var existing = data.rendererFeatures.FirstOrDefault(f => f != null && f.name == "FullscreenGlitch");
            if (existing == null)
            {
                var feat = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                feat.name = "FullscreenGlitch";
                feat.passMaterial = lib.fullscreenGlitch;
                feat.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
                feat.fetchColorBuffer = true;
                feat.requirements = ScriptableRenderPassInput.Color;
                AssetDatabase.AddObjectToAsset(feat, data);
                data.rendererFeatures.Add(feat);
                // keep the renderer's internal feature map in sync
                var mapField = typeof(ScriptableRendererData).GetField("m_RendererFeatureMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (mapField != null && mapField.GetValue(data) is List<long> map && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feat, out string _, out long localId))
                    map.Add(localId);
                EditorUtility.SetDirty(data);
                existing = feat;
            }
            existing.SetActive(false);
            fullscreenFeature = existing;

            // ambient occlusion for the realistic world (switched per tier at runtime)
            var ao = data.rendererFeatures.FirstOrDefault(f => f is ScreenSpaceAmbientOcclusion);
            if (ao == null)
            {
                ao = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
                ao.name = "SSAO";
                AssetDatabase.AddObjectToAsset(ao, data);
                data.rendererFeatures.Add(ao);
                var mapField = typeof(ScriptableRendererData).GetField("m_RendererFeatureMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (mapField != null && mapField.GetValue(data) is List<long> map && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ao, out string _, out long localId))
                    map.Add(localId);
                EditorUtility.SetDirty(data);
            }
            // grounded, soft contact shadows: wider radius, stronger, high sample count, normals from the depth-normals pass
            var aoSo = new SerializedObject(ao);
            void AO(string f, float v) { var pr = aoSo.FindProperty("m_Settings." + f); if (pr == null) return; if (pr.propertyType == SerializedPropertyType.Float) pr.floatValue = v; else if (pr.propertyType == SerializedPropertyType.Boolean) pr.boolValue = v > 0; else pr.intValue = (int)v; }
            AO("Intensity", 0.85f); AO("Radius", 0.45f); AO("DirectLightingStrength", 0.3f); AO("Falloff", 60f);
            AO("Samples", 0); AO("BlurQuality", 0); AO("Downsample", 0); AO("Source", 1); AO("NormalSamples", 1); AO("AOMethod", 0);
            aoSo.ApplyModifiedPropertiesWithoutUndo();
            ssaoFeature = ao;
        }

        static ScriptableRendererFeature ssaoFeature;

        static ScriptableRendererFeature fullscreenFeature;

        // ------------------------------------------------------------------ data
        static T Asset<T>(string path, Func<T> create) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = create();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        static string Safe(string s) => string.Join("_", s.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");

        static GameObject Model(string relPath) => AssetDatabase.LoadAssetAtPath<GameObject>($"{Art}/{relPath}");
        static Texture2D Tex(string relPath) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{Art}/{relPath}");

        static Texture2D Pbr(string id, bool normal = false)
        {
            var guids = AssetDatabase.FindAssets(id + (normal ? "_NormalGL" : "_Color") + " t:Texture2D", new[] { Art + "/Textures/PBR/" + id });
            return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
        }

        static Texture Sky(string name) => AssetDatabase.LoadAssetAtPath<Texture>($"{Art}/Textures/Skies/{name}_2k.hdr");

        static void BuildData()
        {
            string dbPath = Root + "/Resources/GlitchedDatabase.asset";
            db = AssetDatabase.LoadAssetAtPath<GameDatabase>(dbPath);
            if (db == null) { db = ScriptableObject.CreateInstance<GameDatabase>(); AssetDatabase.CreateAsset(db, dbPath); }
            db.materials = lib;
            db.fullscreenGlitchFeature = fullscreenFeature;
            db.ssaoFeature = ssaoFeature;

            // graphics tiers
            db.graphicsTiers.Clear();
            foreach (var t in ContentDefaults.CreateTiers())
            {
                var a = Asset($"{Data}/GraphicsTiers/{t.name}.asset", () => t);
                if (a.urpAsset == null) a.urpAsset = a.tier <= GraphicsTier.Low ? mobileLite : a.tier == GraphicsTier.Medium ? (RenderPipelineAsset)balanced : pcHigh;
                EditorUtility.SetDirty(a);
                db.graphicsTiers.Add(a);
            }

            // characters
            db.characters.Clear();
            foreach (var c in ContentDefaults.CreateCharacters())
            {
                var a = Asset($"{Data}/Characters/{c.name}.asset", () => c);
                db.characters.Add(a);
            }
            var npc = db.Character("npc");
            if (npc != null && npc.modelPrefab == null)
            {
                npc.modelPrefab = Model("Characters/NPCs/UniversalBase/Superhero_Male_FullBody.fbx");
                EditorUtility.SetDirty(npc);
            }
            AutoAssignMixamo();

            // enemies with model mapping
            db.enemies.Clear();
            var enemyModels = new Dictionary<string, (string path, float h)>
            {
                { "bug_monster", ("Environments/Shared/SciFiEssentials/FBX/Enemy_Trilobite.fbx", 1.1f) },
                { "loop_drone", ("Environments/Shared/SciFiEssentials/FBX/Enemy_EyeDrone.fbx", 1.2f) },
                { "off_by_one", ("Environments/Shared/SciFiEssentials/FBX/Enemy_QuadShell.fbx", 1.6f) },
                { "crypto_cracker", ("Characters/Minions/Robot/Robot.fbx", 1.9f) },
                { "syntax_slime", ("Characters/Minions/Monsters/Slime.fbx", 1.0f) },
                { "momentum_mite", ("Characters/Minions/Monsters/Slime.fbx", 0.9f) },
                { "tesla_wraith", ("Characters/Minions/Monsters/Bat.fbx", 1.6f) },
                { "hydrogen_hopper", ("Characters/Minions/Monsters/Slime.fbx", 0.8f) },
                { "oxy_brute", ("Characters/Minions/Monsters/Slime.fbx", 1.3f) },
                { "carbon_clump", ("Characters/Minions/Monsters/Slime.fbx", 1.2f) },
                { "sodium_sprinter", ("Characters/Minions/Monsters/Slime.fbx", 0.9f) },
                { "acid_alchemist", ("Environments/ChemicalChaos/SciFiMegaKit/FBX/Aliens/Alien_Oculichrysalis.fbx", 1.9f) },
                { "plaque_puff", ("Characters/Minions/Monsters/Slime.fbx", 1.0f) },
                { "antibody_imposter", ("Environments/ChemicalChaos/SciFiMegaKit/FBX/Aliens/Alien_Scolitex.fbx", 1.8f) },
                { "source_shard", ("Characters/Minions/Robot/Robot.fbx", 2.2f) },
            };
            foreach (var e in ContentDefaults.CreateEnemies())
            {
                var a = Asset($"{Data}/Enemies/{e.name}.asset", () => e);
                if (a.modelPrefab == null && enemyModels.TryGetValue(a.id, out var m)) { a.modelPrefab = Model(m.path); a.height = m.h; EditorUtility.SetDirty(a); }
                db.enemies.Add(a);
            }

            // arcs, themes and bosses
            db.arcs.Clear();
            var skies = new Dictionary<string, (string c, string r)>
            {
                { "CodeCity", ("kloppenheim_02_puresky", "kloofendal_48d_partly_cloudy_puresky") },
                { "PhysicsValley", ("qwantani_dusk_2_puresky", "qwantani_mid_morning_puresky") },
                { "ChemicalChaos", ("industrial_sunset_02_puresky", "aerodynamics_workshop") },
                { "BioZone", ("industrial_sunset_02_puresky", "industrial_sunset_02_puresky") },
                { "TheCore", ("kloppenheim_02_puresky", "kloofendal_43d_clear_puresky") },
            };
            var bossModels = new Dictionary<string, (string path, float h)>
            {
                { "sergeant_syntax", ("Characters/Minions/Robot/Robot.fbx", 2.6f) },
                { "count_catalyst", ("Environments/ChemicalChaos/SciFiMegaKit/FBX/Aliens/Alien_Cyclop.fbx", 2.6f) },
                { "professor_plaque", ("Characters/Minions/Monsters/Slime.fbx", 2.4f) },
            };
            var mixamoBosses = new Dictionary<string, string>
            {
                { "sergeant_syntax", "Characters/Bosses/Exo_Red.fbx" },
                { "lord_broken_laws", "Characters/Bosses/Warrok.fbx" },
                { "count_catalyst", "Characters/Bosses/Brute.fbx" },
                { "lord_of_the_lab", "Characters/Bosses/Mutant.fbx" },
                { "lord_within", "Characters/Bosses/Parasite.fbx" },
                { "the_source", "Characters/Bosses/X_Bot.fbx" },
            };
            var tmp = ScriptableObject.CreateInstance<GameDatabase>();
            tmp.characters = db.characters;
            foreach (var arc in ContentDefaults.CreateArcs(tmp))
            {
                string arcPath = $"{Data}/Arcs/{arc.name}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<ArcDataSO>(arcPath);
                if (existing == null)
                {
                    arc.theme = Asset($"{Data}/Themes/{arc.theme.name}.asset", () => arc.theme);
                    if (arc.miniBoss != null) arc.miniBoss = Asset($"{Data}/Bosses/{arc.miniBoss.name}.asset", () => arc.miniBoss);
                    if (arc.boss != null) arc.boss = Asset($"{Data}/Bosses/{arc.boss.name}.asset", () => arc.boss);
                    AssetDatabase.CreateAsset(arc, arcPath);
                    existing = arc;
                }
                existing.levels = arc.levels;   // level order always follows the code (prototype = no second level)
                existing.concepts = arc.concepts;   // so do the concepts (the Class XI topics come first)
                EditorUtility.SetDirty(existing);
                var th = existing.theme;
                if (th != null && skies.TryGetValue(existing.id, out var s))
                {
                    if (th.skyCorrupted == null) th.skyCorrupted = Sky(s.c);
                    if (th.skyRestored == null) th.skyRestored = Sky(s.r);
                    if (existing.id == "BioZone") { th.skyTintRestored = new Color(1f, 0.6f, 0.7f); th.skyTintCorrupted = new Color(0.9f, 0.2f, 0.3f); }
                    if (existing.id == "ChemicalChaos") th.skyTintCorrupted = new Color(0.35f, 0.9f, 0.75f);
                    if (LevelBuilder.Realistic(existing.id))
                    {
                        // realistic look: hazy dusk while corrupted, clear daylight once restored (overwrites old values)
                        ContentDefaults.ApplyRealisticTheme(th, existing.id);
                        bool valleySky = existing.id == "PhysicsValley";
                        th.skyCorrupted = Sky(valleySky ? "qwantani_dusk_2_puresky" : existing.id == "TheCore" ? "kloppenheim_02_puresky" : "industrial_sunset_02_puresky") ?? th.skyCorrupted;
                        th.skyRestored = Sky(valleySky ? "qwantani_mid_morning_puresky" : existing.id == "ChemicalChaos" || existing.id == "BioZone" ? "kloofendal_43d_clear_puresky" : existing.id == "TheCore" ? "qwantani_dusk_2_puresky" : "kloofendal_48d_partly_cloudy_puresky") ?? th.skyRestored;
                        th.skyTintCorrupted = existing.id == "ChemicalChaos" ? new Color(0.8f, 0.9f, 0.75f) : th.skyTintCorrupted;
                    }
                    EditorUtility.SetDirty(th);
                }
                foreach (var b in new[] { existing.miniBoss, existing.boss })
                {
                    if (b == null) continue;
                    if (b.profile == null) b.profile = db.characters.Find(c => c.id == ProfileFor(b.id));
                    if (mixamoBosses.TryGetValue(b.id, out var mx) && Model(mx) != null)
                    {
                        // Mixamo body (from the asset prompt) wins over the placeholder once the FBX is in Art/Characters/Bosses
                        var cur = b.modelPrefab != null ? AssetDatabase.GetAssetPath(b.modelPrefab) : "";
                        if (!cur.Contains("/Characters/Bosses/") && !cur.EndsWith(Safe(System.IO.Path.GetFileNameWithoutExtension(mx)) + "_Animated.prefab")) b.modelPrefab = Model(mx);
                    }
                    else if (b.modelPrefab == null && bossModels.TryGetValue(b.id, out var bm)) { b.modelPrefab = Model(bm.path); b.height = bm.h; }
                    EditorUtility.SetDirty(b);
                }
                db.arcs.Add(existing);
            }
            UnityEngine.Object.DestroyImmediate(tmp);

            db.cosmetics.Clear();
            foreach (var c in ContentDefaults.CreateCosmetics()) db.cosmetics.Add(Asset($"{Data}/Cosmetics/{c.name}.asset", () => c));
            db.achievements.Clear();
            foreach (var a in ContentDefaults.CreateAchievements()) db.achievements.Add(Asset($"{Data}/Achievements/{a.name}.asset", () => a));

            // UI theme
            db.uiTheme = Asset($"{Data}/UITheme.asset", () => ScriptableObject.CreateInstance<UIThemeSO>());
            if (db.uiTheme.displayFont == null) db.uiTheme.displayFont = fDisplay;
            if (db.uiTheme.bodyFont == null) db.uiTheme.bodyFont = fBody;
            if (db.uiTheme.monoFont == null) db.uiTheme.monoFont = fMono;
            if (db.uiTheme.dyslexicFont == null) db.uiTheme.dyslexicFont = fDys;
            if (db.uiTheme.devanagariFont == null) db.uiTheme.devanagariFont = fDeva;
            EditorUtility.SetDirty(db.uiTheme);

            BuildSfx();
            EditorUtility.SetDirty(db);
            LoadHolograms();
        }

        static string ProfileFor(string bossId)
        {
            switch (bossId)
            {
                case "lord_infinite_loop": return "lord_loop";
                case "lord_broken_laws": return "lord_laws";
                case "lord_of_the_lab": return "lord_lab";
                case "lord_within": return "lord_within";
                case "the_source": return "the_source";
                default: return null;
            }
        }

        /// <summary>
        /// When Mixamo FBX files are dropped into Art/Characters/Mixamo (Ty.fbx, Exo_Red.fbx, Warrok.fbx...),
        /// assign them to the right characters automatically.
        /// </summary>
        static void AutoAssignMixamo()
        {
            var map = new Dictionary<string, string> { { "ty", "aarav" }, { "timmy", "aarav" } };
            foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { Art + "/Characters/Player" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var prof = db.Character("aarav");
                if (prof != null && prof.modelPrefab == null) { prof.modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path); EditorUtility.SetDirty(prof); }
            }
        }

        public static readonly Dictionary<string, string[]> SfxMap = new Dictionary<string, string[]>
        {
            { "click", new[] { "ui-audio/click1", "ui-audio/click3" } }, { "hover", new[] { "ui-audio/rollover2", "ui-audio/rollover3" } },
            { "select", new[] { "interface-sounds/select_001", "interface-sounds/select_002" } }, { "pause", new[] { "interface-sounds/minimize_003" } },
            { "transition", new[] { "generated/transition" } }, { "whoosh", new[] { "digital-audio/phaserDown2" } },
            { "levelup", new[] { "digital-audio/powerUp7" } }, { "achievement", new[] { "jingles/jingles_NES05" } },
            { "dialogue_next", new[] { "interface-sounds/tick_002" } },
            { "blip", new[] { "generated/blip_0", "generated/blip_1", "generated/blip_2", "generated/blip_3", "generated/blip_4", "generated/blip_5" } },
            { "blip_pixel", new[] { "generated/blip_pixel_0", "generated/blip_pixel_1", "generated/blip_pixel_2", "generated/blip_pixel_3" } },
            { "hit", new[] { "impact-sounds/impactPunch_medium_000", "impact-sounds/impactPunch_medium_001", "impact-sounds/impactPunch_medium_002" } },
            { "crit", new[] { "generated/crit" } }, { "combo", new[] { "digital-audio/pepSound3" } },
            { "correct", new[] { "generated/correct" } }, { "wrong", new[] { "generated/wrong" } },
            { "comedy_fail", new[] { "generated/comedy_fail" } }, { "enemy_attack", new[] { "impact-sounds/impactPunch_heavy_000", "impact-sounds/impactPunch_heavy_002" } },
            { "player_hurt", new[] { "impact-sounds/impactSoft_heavy_001", "impact-sounds/impactSoft_heavy_002" } },
            { "enemy_defeat", new[] { "digital-audio/zapThreeToneDown", "interface-sounds/glitch_003" } },
            { "jump", new[] { "digital-audio/phaseJump1" } }, { "double_jump", new[] { "digital-audio/phaseJump3" } },
            { "land", new[] { "impact-sounds/footstep_concrete_001" } },
            { "footstep", new[] { "impact-sounds/footstep_concrete_000", "impact-sounds/footstep_concrete_002", "impact-sounds/footstep_concrete_003", "impact-sounds/footstep_concrete_004" } },
            { "dash", new[] { "sci-fi-sounds/forceField_001" } }, { "coin", new[] { "digital-audio/pepSound1", "digital-audio/pepSound2" } },
            { "checkpoint", new[] { "digital-audio/powerUp2" } }, { "door_open", new[] { "sci-fi-sounds/doorOpen_001" } },
            { "door_locked", new[] { "interface-sounds/error_004" } }, { "door_slam", new[] { "sci-fi-sounds/doorClose_002" } },
            { "glitch", new[] { "interface-sounds/glitch_001", "interface-sounds/glitch_002", "interface-sounds/glitch_004" } },
            { "glitch_big", new[] { "generated/glitch_big" } }, { "boom", new[] { "generated/boom" } },
            { "explosion", new[] { "sci-fi-sounds/explosionCrunch_002", "sci-fi-sounds/lowFrequency_explosion_000" } },
            { "powerup", new[] { "digital-audio/powerUp11" } }, { "restore", new[] { "generated/restore" } },
            { "restore_tick", new[] { "digital-audio/tone1", "digital-audio/twoTone1" } },
            { "buy", new[] { "interface-sounds/confirmation_002" } }, { "equip", new[] { "interface-sounds/switch_004" } },
            { "timer_tick", new[] { "generated/timer_tick" } }, { "drop", new[] { "interface-sounds/drop_002" } }, { "pickup", new[] { "interface-sounds/scratch_002" } },
            { "type", new[] { "interface-sounds/tick_001" } }, { "error", new[] { "interface-sounds/error_006" } },
            { "shrink", new[] { "generated/shrink" } }, { "grow", new[] { "generated/grow" } }, { "gravity_flip", new[] { "generated/gravity_flip" } },
            { "fall", new[] { "generated/fall" } }, { "boing", new[] { "generated/boing" } }, { "heal", new[] { "digital-audio/powerUp5" } },
            { "boss_roar", new[] { "generated/boss_roar" } }, { "glass_break", new[] { "impact-sounds/impactGlass_heavy_000", "impact-sounds/impactGlass_heavy_002", "impact-sounds/impactGlass_heavy_004" } }, { "boss_regen", new[] { "digital-audio/phaserUp6" } },
            { "mastered", new[] { "generated/mastered" } }, { "rewards", new[] { "jingles/jingles_NES11" } },
            { "panel_in", new[] { "interface-sounds/maximize_004" } }, { "terminal", new[] { "sci-fi-sounds/computerNoise_001" } },
            { "lab_open", new[] { "interface-sounds/open_002" } }, { "lab_close", new[] { "interface-sounds/close_002" } },
            { "launch", new[] { "sci-fi-sounds/thrusterFire_001" } }, { "bond", new[] { "generated/bond" } },
            { "sky_tear", new[] { "sci-fi-sounds/spaceEngineLow_002" } }, { "thunder", new[] { "sci-fi-sounds/lowFrequency_explosion_001" } },
            { "heartbeat", new[] { "generated/heartbeat" } },
        };

        static AudioClip Clip(string rel)
        {
            foreach (var ext in new[] { ".ogg", ".wav" })
            {
                var c = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Root}/Audio/SFX/{rel}{ext}");
                if (c != null) return c;
            }
            return null;
        }

        static void BuildSfx()
        {
            db.sfx = Asset($"{Data}/SfxLibrary.asset", () => ScriptableObject.CreateInstance<SfxLibrarySO>());
            var lib = db.sfx;
            foreach (var kv in SfxMap)
            {
                var e = lib.sounds.Find(s => s.key == kv.Key);
                if (e == null) { e = new SfxEntry { key = kv.Key }; lib.sounds.Add(e); }
                if (e.clips.Count == 0) foreach (var r in kv.Value) { var c = Clip(r); if (c != null) e.clips.Add(c); }
                if (kv.Key.StartsWith("click") || kv.Key == "hover" || kv.Key == "select" || kv.Key == "pause" || kv.Key == "dialogue_next" || kv.Key == "panel_in") e.bus = SoundBus.UI;
                if (kv.Key.StartsWith("blip")) { e.bus = SoundBus.Voice; e.volume = 0.5f; }
                if (kv.Key == "footstep") e.volume = 0.35f;
                if (kv.Key == "hover") e.volume = 0.4f;
            }
            foreach (var g in AssetDatabase.FindAssets("t:AudioClip", new[] { Root + "/Audio/Music" }))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g));
                if (clip != null && lib.music.Find(m => m.key == clip.name) == null) lib.music.Add(new MusicEntry { key = clip.name, clip = clip });
            }
            EditorUtility.SetDirty(lib);
        }

        public static void LoadHolograms()
        {
            LevelBuilder.holograms = new Dictionary<string, Texture2D>();
            foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { Art + "/Textures/Holograms" }))
            {
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(g));
                if (t != null) LevelBuilder.holograms[t.name] = t;
            }
        }

        // ------------------------------------------------------------------ animators
        static AnimationClip FindClip(IEnumerable<AnimationClip> clips, params string[] keys)
        {
            foreach (var k in keys)
                foreach (var c in clips)
                    if (c.name.StartsWith("Mixamo_") && c.name.ToLowerInvariant().Contains(k)) return c;
            foreach (var k in keys)
                foreach (var c in clips)
                    if (!c.name.StartsWith("__preview__") && c.name.ToLowerInvariant().Contains(k)) return c;
            return null;
        }

        static List<AnimationClip> ClipsAt(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToList();

        static AnimatorController BuildController(string name, List<AnimationClip> clips, bool humanoid)
        {
            string path = $"{Art}/Animations/Controllers/{name}.controller";
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(path);
            ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ac.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            ac.AddParameter("VSpeed", AnimatorControllerParameterType.Float);
            foreach (var t in new[] { "Hit", "Victory", "Defeat", "Cast", "Attack", "Land", "Jump", "Dash" }) ac.AddParameter(t, AnimatorControllerParameterType.Trigger);
            ac.AddParameter("Talk", AnimatorControllerParameterType.Bool);
            ac.AddParameter("Sit", AnimatorControllerParameterType.Bool);
            ac.AddParameter("Slide", AnimatorControllerParameterType.Bool);
            ac.AddParameter(new AnimatorControllerParameter { name = "ActSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
            var sm = ac.layers[0].stateMachine;
            var idle = FindClip(clips.Where(c => !c.name.ToLowerInvariant().Contains("sit")), "breath", "idle_loop", "idle");   // never a seated idle for standing
            var walk = FindClip(clips, "walk_loop", "walk");
            var run = FindClip(clips, "jog_fwd", "run", "sprint", "jog", "fly", "walk");
            var tree = new BlendTree { name = "Locomotion", blendParameter = "Speed", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(tree, ac);
            // Speed is metres per second / CharacterVisual.RunReference: each clip sits at the speed it really moves
            // at, and plays faster when the character moves faster than the clip (no ice-skating feet)
            float ClipSpeed(AnimationClip c, float fallback) { float v = c != null ? c.averageSpeed.magnitude : 0f; return v > 0.4f && v < 8f ? v : fallback; }
            float runRef = CharacterVisual.RunReference;
            float walkV = ClipSpeed(walk, 1.4f), runV = ClipSpeed(run, 3.9f);
            float sprint = Mathf.Clamp(runRef / runV, 1f, 1.65f);
            var kids = new List<ChildMotion>();
            if (idle != null) kids.Add(new ChildMotion { motion = idle, threshold = 0f, timeScale = 1f });
            // humanoids (Shaurya, the citizens): walk -> jog -> sprint, each at the speed it really covers on the rig
            // (UAL1 jog 5.06 and sprint 7.8 m/s from their root-motion versions; the Mixamo walk 1.5 m/s)
            var jog = humanoid ? FindClip(clips.Where(c => !c.name.StartsWith("Mixamo_")), "jog_fwd") : null;
            var sprintClip = humanoid ? FindClip(clips.Where(c => !c.name.StartsWith("Mixamo_")), "sprint_loop") : null;
            if (jog != null && sprintClip != null)
            {
                if (walk != null) kids.Add(new ChildMotion { motion = walk, threshold = 1.5f / runRef, timeScale = 1f });
                kids.Add(new ChildMotion { motion = jog, threshold = 5.06f / runRef, timeScale = 1f });
                kids.Add(new ChildMotion { motion = sprintClip, threshold = 7.8f / runRef, timeScale = 1f });
                Debug.Log($"[GLITCHED] Locomotion {name}: walk {walk?.name} 1.5, jog {jog.name} 5.06, sprint {sprintClip.name} 7.8 m/s");
            }
            else
            {
                if (walk != null && walk != run) kids.Add(new ChildMotion { motion = walk, threshold = walkV / runRef, timeScale = 1f });
                if (run != null)
                {
                    kids.Add(new ChildMotion { motion = run, threshold = runV / runRef, timeScale = 1f });
                    kids.Add(new ChildMotion { motion = run, threshold = 1f, timeScale = sprint });   // full sprint
                }
                Debug.Log($"[GLITCHED] Locomotion {name}: walk {walkV:0.0} m/s, run {runV:0.0} m/s, sprint x{sprint:0.00}");
            }
            tree.children = kids.ToArray();
            var loco = sm.AddState("Locomotion");
            loco.motion = tree;
            sm.defaultState = loco;
            AnimatorState Add(string stateName, AnimationClip clip, string trigger, bool returnToLoco = true)
            {
                if (clip == null) return null;
                var s = sm.AddState(stateName);
                s.motion = clip;
                var tr = sm.AddAnyStateTransition(s);
                tr.AddCondition(AnimatorConditionMode.If, 0, trigger);
                tr.duration = 0.1f; tr.canTransitionToSelf = false;
                if (returnToLoco) { var back = s.AddTransition(loco); back.hasExitTime = true; back.exitTime = 0.9f; back.duration = 0.15f; }
                return s;
            }
            Add("Hit", FindClip(clips, "hit", "hurt", "damage", "reaction"), "Hit");
            Add("Attack", humanoid ? FindClip(clips, "punch_cross", "punch") : FindClip(clips, "attack", "punch", "bite", "slash", "shoot"), "Attack");
            Add("Victory", FindClip(clips, "victory", "cheer", "dance", "celebrat", "yes"), "Victory");
            Add("Defeat", FindClip(clips, "death", "die", "defeat"), "Defeat", false);
            Add("Cast", FindClip(clips, "spell", "cast", "interact", "punch"), "Cast");
            var jumpClip = FindClip(clips, "jump_start", "jump");
            var fallClip = FindClip(clips, "jump_loop", "fall", "jump");
            if (fallClip != null)
            {
                var air = sm.AddState("Air");
                air.motion = fallClip;
                var toAir = loco.AddTransition(air); toAir.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded"); toAir.duration = 0.1f; toAir.hasExitTime = false;
                var toGround = air.AddTransition(loco); toGround.AddCondition(AnimatorConditionMode.If, 0, "Grounded"); toGround.duration = 0.1f; toGround.hasExitTime = false;
            }
            // parkour (UAL2 / UAL1): slide under gaps, mantle, vault, landing roll; CharacterVisual.PlayAct times them
            var slideIn = humanoid ? FindClip(clips, "slide_start") : null;
            var slideLoop = humanoid ? FindClip(clips, "slide_loop") : null;
            var slideOut = humanoid ? FindClip(clips, "slide_exit") : null;
            if (slideIn != null && slideLoop != null && slideOut != null)
            {
                var sIn = sm.AddState("SlideStart"); sIn.motion = slideIn;
                var sLoop = sm.AddState("SlideLoop"); sLoop.motion = slideLoop;
                var sOut = sm.AddState("SlideExit"); sOut.motion = slideOut; sOut.speed = 1.6f;
                foreach (var from in sm.states.Select(cs => cs.state).Where(st => st.name == "Locomotion" || st.name == "Air").ToList())
                { var t = from.AddTransition(sIn); t.AddCondition(AnimatorConditionMode.If, 0, "Slide"); t.duration = 0.08f; t.hasExitTime = false; }
                var a = sIn.AddTransition(sLoop); a.hasExitTime = true; a.exitTime = 0.85f; a.duration = 0.1f;
                foreach (var st in new[] { sIn, sLoop }) { var t = st.AddTransition(sOut); t.AddCondition(AnimatorConditionMode.IfNot, 0, "Slide"); t.duration = 0.08f; t.hasExitTime = false; }
                var back = sOut.AddTransition(loco); back.hasExitTime = true; back.exitTime = 0.6f; back.duration = 0.15f;
            }
            AnimatorState Act(string stateName, AnimationClip clip)
            {
                if (clip == null) return null;
                var st = sm.AddState(stateName);
                st.motion = clip;
                st.speedParameterActive = true; st.speedParameter = "ActSpeed";
                var back = st.AddTransition(loco); back.hasExitTime = true; back.exitTime = 0.92f; back.duration = 0.12f;
                return st;
            }
            if (humanoid)
            {
                Act("Climb", FindClip(clips, "climbup"));
                Act("Vault", FindClip(clips, "ninjajump_start"));
                Act("Roll", FindClip(clips.Where(c => !c.name.StartsWith("Mixamo_")), "|roll"));
            }
            var sit = FindClip(clips, "sitting_idle", "sit");
            if (sit != null)
            {
                var s = sm.AddState("Sit"); s.motion = sit;
                var to = sm.AddAnyStateTransition(s); to.AddCondition(AnimatorConditionMode.If, 0, "Sit"); to.canTransitionToSelf = false;
                var back = s.AddTransition(loco); back.AddCondition(AnimatorConditionMode.IfNot, 0, "Sit");
            }
            // every other Mixamo clip becomes a named state cutscenes can play (CharacterVisual.PlayState("Typing"))
            var used = new HashSet<Motion>();
            foreach (var cs in sm.states) { if (cs.state.motion != null) used.Add(cs.state.motion); }
            var sitState = sm.states.FirstOrDefault(cs => cs.state.name == "Sit").state;
            foreach (var clip in clips)
            {
                if (!clip.name.StartsWith("Mixamo_") || used.Contains(clip)) continue;
                var st = sm.AddState(clip.name.Substring(7));
                st.motion = clip;
                // one-shot moves (a taunt, a seated yell) flow back to standing, or to sitting for the seated ones;
                // looping ones (typing, floating, terrified) hold until the next PlayState
                bool loops = clip.isLooping || AnimationUtility.GetAnimationClipSettings(clip).loopTime;
                if (loops || clip.name.Contains("Dying") || clip.name.Contains("Defeated")) continue;
                bool seated = clip.name.Contains("Sitting");
                var back = st.AddTransition(seated && sitState != null ? sitState : loco);
                back.hasExitTime = true; back.exitTime = 0.92f; back.duration = 0.25f;
            }
            EditorUtility.SetDirty(ac);
            return ac;
        }

        static void BuildAnimators()
        {
            // humanoid controller from the Universal Animation Library 2 (works on any Humanoid, incl. Mixamo)
            var ualPath = $"{Art}/Animations/UAL2/UAL2_Standard.fbx";
            var ualClips = ClipsAt(ualPath);
            ualClips.AddRange(ClipsAt($"{Art}/Animations/UAL1/UAL1_Standard.fbx"));   // jog, sprint, roll
            var mixamo = AssetDatabase.FindAssets("t:Model", new[] { Art + "/Animations/Mixamo" }).SelectMany(g => ClipsAt(AssetDatabase.GUIDToAssetPath(g))).ToList();
            ualClips.InsertRange(0, mixamo); // FindClip prefers Mixamo_ clips (see FindClip)
            AnimatorController humanoidAc = null;
            if (ualClips.Count > 0)
            {
                Debug.Log("[GLITCHED] Humanoid clips: " + string.Join(", ", ualClips.Select(c => c.name).Take(60)));
                var ac = BuildController("Humanoid", ualClips, true);
                humanoidAc = ac;
                foreach (var c in db.characters)
                    if (c != null && c.humanoid && c.modelPrefab != null && c.animator == null) { c.animator = ac; EditorUtility.SetDirty(c); }
            }
            // generic controllers for each animated minion model
            var done = new Dictionary<GameObject, RuntimeAnimatorController>();
            foreach (var e in db.enemies.Cast<EnemyDataSO>().Concat(db.arcs.SelectMany(a => new EnemyDataSO[] { a.miniBoss, a.boss })))
            {
                if (e == null || e.modelPrefab == null) continue;
                if (!done.TryGetValue(e.modelPrefab, out var ctrl))
                {
                    var mpath = AssetDatabase.GetAssetPath(e.modelPrefab);
                    var clips = ClipsAt(mpath);
                    var modelAnim = e.modelPrefab.GetComponentInChildren<Animator>();
                    bool humanoidModel = modelAnim != null && modelAnim.avatar != null && modelAnim.avatar.isHuman;
                    if (e.modelPrefab.GetComponentInChildren<MachineMotion>() != null) ctrl = null;              // procedural machines animate themselves
                    else if (humanoidModel || mpath.Contains("/Characters/Bosses/")) ctrl = humanoidAc;           // Mixamo + generated humanoids // Mixamo T-pose bodies share the humanoid set
                    else ctrl = clips.Count > 0 ? BuildController("Enemy_" + Safe(e.modelPrefab.name), clips, false) : null;
                    done[e.modelPrefab] = ctrl;
                }
                if (ctrl != null)
                {
                    // the enemy's model prefab gets its controller through a small wrapper prefab
                    var wrapperPath = $"{Root}/Prefabs/{Safe(e.modelPrefab.name)}_Animated.prefab";
                    var wrapper = AssetDatabase.LoadAssetAtPath<GameObject>(wrapperPath);
                    // a wrapper left over from a different model of the same name (the procedural robots before the HQ bosses)
                    if (wrapper != null && wrapper != e.modelPrefab && PrefabUtility.GetCorrespondingObjectFromOriginalSource(wrapper) != e.modelPrefab)
                    {
                        AssetDatabase.DeleteAsset(wrapperPath);
                        wrapper = null;
                    }
                    if (wrapper == null)
                    {
                        var inst = (GameObject)PrefabUtility.InstantiatePrefab(e.modelPrefab);
                        var anim = inst.GetComponentInChildren<Animator>() ?? inst.AddComponent<Animator>();
                        anim.runtimeAnimatorController = ctrl;
                        anim.applyRootMotion = false;
                        wrapper = PrefabUtility.SaveAsPrefabAsset(inst, wrapperPath);
                        UnityEngine.Object.DestroyImmediate(inst);
                    }
                    else
                    {
                        // controllers are recreated every build, so re-link the existing wrapper to the new one
                        var root = PrefabUtility.LoadPrefabContents(wrapperPath);
                        var anim = root.GetComponentInChildren<Animator>() ?? root.AddComponent<Animator>();
                        anim.runtimeAnimatorController = ctrl;
                        anim.applyRootMotion = false;
                        PrefabUtility.SaveAsPrefabAsset(root, wrapperPath);
                        PrefabUtility.UnloadPrefabContents(root);
                        wrapper = AssetDatabase.LoadAssetAtPath<GameObject>(wrapperPath);
                    }
                    e.modelPrefab = wrapper;
                    EditorUtility.SetDirty(e);
                }
            }
            // animated NPC wrapper (Universal Base Character + humanoid controller)
            var npc = db.Character("npc");
            var humanoid = AssetDatabase.LoadAssetAtPath<AnimatorController>($"{Art}/Animations/Controllers/Humanoid.controller");
            if (npc != null && humanoid != null) { npc.animator = humanoid; EditorUtility.SetDirty(npc); }
        }

        // ------------------------------------------------------------------ kits
        static List<GameObject> Models(string folder, Func<string, bool> filter = null)
        {
            var list = new List<GameObject>();
            if (!AssetDatabase.IsValidFolder(folder)) return list;
            foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { folder }))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                string n = Path.GetFileNameWithoutExtension(p);
                if (filter != null && !filter(n)) continue;
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (go != null) list.Add(go);
            }
            return list;
        }

        static Material SurfaceMat(string name, string pbr, Color tint, float glitch, float uv = 0.3f)
        {
            return Mat(name, "Glitched/Glitchable", m =>
            {
                var t = Pbr(pbr);
                if (t != null) m.SetTexture("_BaseMap", t);
                m.SetColor("_BaseColor", tint);
                m.SetFloat("_Glitchiness", glitch);
                m.SetFloat("_Triplanar", 1); m.EnableKeyword("_TRIPLANAR"); m.SetFloat("_TriplanarScale", uv);
                m.SetFloat("_AlbedoContrast", 0.6f);
                m.enableInstancing = true;
            });
        }

        static void BuildKits()
        {
            EnvironmentKitSO Kit(string id)
            {
                var k = Asset($"{Data}/Kits/Kit_{id}.asset", () => { var s = ScriptableObject.CreateInstance<EnvironmentKitSO>(); s.id = id; return s; });
                return k;
            }
            string dt = Art + "/Environments/CodeCity/Downtown/FBX";
            string nat = Art + "/Environments/PhysicsValley/Nature/FBX";
            string sci = Art + "/Environments/ChemicalChaos/SciFiMegaKit/FBX";
            string ess = Art + "/Environments/Shared/SciFiEssentials/FBX";
            string fan = Art + "/Environments/Shared/FantasyProps/FBX";
            string space = Art + "/Environments/FinalVoid/SpaceKit/FBX";

            var cc = Kit("CodeCity");
            if (cc.buildings.Count == 0) cc.buildings = Models(dt, n => n.StartsWith("Building_"));
            if (cc.props.Count == 0) cc.props = Models(dt, n => n.StartsWith("Prop_") || n == "Sidewalk_Planter");
            if (cc.vehicles.Count == 0) cc.vehicles = Models(space, n => n.StartsWith("craft_"));
            if (cc.facadeTextures.Count == 0) cc.facadeTextures = new[] { Pbr("Facade001"), Pbr("Facade006"), Pbr("MetalPlates017A") }.Where(t => t != null).ToList();
            if (cc.surfaceTextures.Count == 0) cc.surfaceTextures = new[] { Pbr("Road012A"), Pbr("Asphalt033") }.Where(t => t != null).ToList();
            if (cc.groundMaterial == null) cc.groundMaterial = SurfaceMat("Ground_CodeCity_Top", "PavingStones151", new Color(0.55f, 0.6f, 0.75f), 0.35f, 0.4f);
            if (cc.groundSideMaterial == null) cc.groundSideMaterial = SurfaceMat("Ground_CodeCity_Side", "MetalPlates017A", new Color(0.35f, 0.38f, 0.5f), 0.5f, 0.3f);
            if (cc.wallMaterial == null) cc.wallMaterial = SurfaceMat("Wall_CodeCity", "Concrete034", new Color(0.4f, 0.42f, 0.55f), 0.6f, 0.3f);

            var pv = Kit("PhysicsValley");
            if (pv.nature.Count == 0) pv.nature = Models(nat, n => n.Contains("Tree") || n.StartsWith("Pine") || n.StartsWith("Rock_") || n.StartsWith("Bush") || n.StartsWith("Plant"));
            if (pv.props.Count == 0) pv.props = Models(nat, n => n.StartsWith("Grass") || n.StartsWith("Flower") || n.StartsWith("Mushroom") || n.StartsWith("Pebble_Round") || n.StartsWith("Fern"));
            if (pv.surfaceTextures.Count == 0) pv.surfaceTextures = new[] { Pbr("Rock064"), Pbr("Grass005"), Pbr("Rock058") }.Where(t => t != null).ToList();
            if (pv.groundMaterial == null) pv.groundMaterial = SurfaceMat("Ground_Physics_Top", "Grass005", new Color(0.75f, 0.95f, 0.65f), 0.3f, 0.35f);
            if (pv.groundSideMaterial == null) pv.groundSideMaterial = SurfaceMat("Ground_Physics_Side", "Rock064", new Color(0.85f, 0.75f, 0.7f), 0.5f, 0.25f);
            if (pv.wallMaterial == null) pv.wallMaterial = SurfaceMat("Wall_Physics", "Rock058", new Color(0.8f, 0.72f, 0.68f), 0.6f, 0.25f);

            var ch = Kit("ChemicalChaos");
            if (ch.props.Count == 0) { ch.props = Models(sci + "/Props"); ch.props.AddRange(Models(ess, n => n.StartsWith("Prop_") && !n.Contains("Gun"))); }
            if (ch.platforms.Count == 0) ch.platforms = Models(sci + "/Platforms");
            if (ch.creatures.Count == 0) ch.creatures = Models(sci + "/Aliens");
            if (ch.facadeTextures.Count == 0) ch.facadeTextures = new[] { Pbr("Tiles141"), Pbr("CorrugatedSteel009"), Pbr("Metal063") }.Where(t => t != null).ToList();
            if (ch.surfaceTextures.Count == 0) ch.surfaceTextures = new[] { Pbr("Tiles141"), Pbr("DiamondPlate009") }.Where(t => t != null).ToList();
            if (ch.groundMaterial == null) ch.groundMaterial = SurfaceMat("Ground_Chem_Top", "DiamondPlate009", new Color(0.85f, 0.92f, 0.95f), 0.35f, 0.4f);
            if (ch.groundSideMaterial == null) ch.groundSideMaterial = SurfaceMat("Ground_Chem_Side", "Tiles141", new Color(0.7f, 0.85f, 0.85f), 0.5f, 0.3f);
            if (ch.wallMaterial == null) ch.wallMaterial = SurfaceMat("Wall_Chem", "Metal063", new Color(0.7f, 0.8f, 0.85f), 0.6f, 0.3f);
            if (ch.liquidMaterial == null) ch.liquidMaterial = Mat("Liquid_Acid", "Glitched/Flow", m => { var t = Pbr("Lava004"); if (t != null) m.SetTexture("_BaseMap", t); m.SetColor("_BaseColor", new Color(0.4f, 1f, 0.3f)); });

            var bz = Kit("BioZone");
            if (bz.nature.Count == 0) bz.nature = Models(nat, n => n.StartsWith("Plant") || n.StartsWith("Fern") || n.StartsWith("Grass_Wispy") || n.StartsWith("Mushroom") || n.StartsWith("Bush"));
            if (bz.creatures.Count == 0) bz.creatures = Models(sci + "/Aliens");
            if (bz.surfaceTextures.Count == 0) bz.surfaceTextures = new[] { Pbr("Sponge001"), Pbr("Leather037") }.Where(t => t != null).ToList();
            if (bz.groundMaterial == null) bz.groundMaterial = SurfaceMat("Ground_Bio_Top", "Leather037", new Color(1f, 0.55f, 0.6f), 0.35f, 0.35f);
            if (bz.groundSideMaterial == null) bz.groundSideMaterial = SurfaceMat("Ground_Bio_Side", "Sponge001", new Color(0.95f, 0.45f, 0.55f), 0.5f, 0.3f);
            if (bz.wallMaterial == null) bz.wallMaterial = SurfaceMat("Wall_Bio", "Sponge001", new Color(0.85f, 0.35f, 0.45f), 0.6f, 0.3f);
            if (bz.liquidMaterial == null) bz.liquidMaterial = Mat("Liquid_Blood", "Glitched/Flow", m => { var t = Pbr("Lava001"); if (t != null) m.SetTexture("_BaseMap", t); m.SetColor("_BaseColor", new Color(0.8f, 0.1f, 0.15f)); });

            var core = Kit("TheCore");
            if (core.props.Count == 0) core.props = Models(space, n => n.StartsWith("rock") || n.StartsWith("satellite") || n.StartsWith("meteor") || n.StartsWith("structure") || n.StartsWith("rocket"));
            if (core.facadeTextures.Count == 0) core.facadeTextures = new[] { Pbr("MetalPlates017A"), Pbr("Facade006") }.Where(t => t != null).ToList();
            if (core.groundMaterial == null) core.groundMaterial = SurfaceMat("Ground_Core_Top", "MetalPlates017A", new Color(0.35f, 0.35f, 0.45f), 0.6f, 0.4f);
            if (core.groundSideMaterial == null) core.groundSideMaterial = SurfaceMat("Ground_Core_Side", "Concrete048", new Color(0.2f, 0.2f, 0.28f), 0.8f, 0.3f);

            var room = Kit("Bedroom");
            if (room.groundMaterial == null) room.groundMaterial = SurfaceMat("Room_Floor", "WoodFloor064", Color.white, 1f, 0.5f);
            if (room.wallMaterial == null) room.wallMaterial = SurfaceMat("Room_Wall", "Bricks113A", new Color(0.95f, 0.9f, 0.88f), 1f, 0.4f);
            if (room.props.Count == 0) room.props = Models(fan, n => n.StartsWith("Book") || n.StartsWith("Potion") || n.StartsWith("Candle"));

            db.kits = new List<EnvironmentKitSO> { cc, pv, ch, bz, core, room };
            foreach (var k in db.kits) EditorUtility.SetDirty(k);
            EditorUtility.SetDirty(db);
        }

        // ------------------------------------------------------------------ text assets
        static void WriteTextAssets()
        {
            string ia = Root + "/Resources/PlayerControls.inputactions";
            if (!File.Exists(ia)) { File.WriteAllText(ia, InputHub.DefaultJson); AssetDatabase.ImportAsset(ia); }
            string loc = Root + "/Resources/Localization/strings.csv";
            if (!File.Exists(loc))
            {
                File.WriteAllText(loc,
                    "key,en,hi\n" +
                    "pause.resume,RESUME,जारी रखें\n" +
                    "pause.mastery,MASTERY,महारत\n" +
                    "pause.achievements,ACHIEVEMENTS,उपलब्धियाँ\n" +
                    "pause.settings,SETTINGS,सेटिंग्स\n" +
                    "pause.checkpoint,RESPAWN AT CHECKPOINT,चेकपॉइंट पर लौटें\n" +
                    "pause.worldmap,WORLD MAP,विश्व मानचित्र\n" +
                    "pause.title,QUIT TO TITLE,मुख्य मेनू\n" +
                    "settings.title,SETTINGS,सेटिंग्स\n" +
                    "settings.graphics,GRAPHICS,ग्राफ़िक्स\n" +
                    "settings.audio,AUDIO,ध्वनि\n" +
                    "settings.controls,CONTROLS,नियंत्रण\n" +
                    "settings.accessibility,ACCESSIBILITY,सुलभता\n" +
                    "settings.language,LANGUAGE,भाषा\n" +
                    "mastery.title,MASTERY,महारत\n" +
                    "achievements.title,ACHIEVEMENTS,उपलब्धियाँ\n" +
                    "shop.title,COSTUME TERMINAL,पोशाक टर्मिनल\n");
                AssetDatabase.ImportAsset(loc);
            }
        }

        static void PlayerSettingsSetup()
        {
            PlayerSettings.companyName = "DAV Public School Dehradun";
            PlayerSettings.productName = "GLITCHED";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.davdehradun.glitched");
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "com.davdehradun.glitched");
            PlayerSettings.bundleVersion = "1.0";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.runInBackground = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/Textures/Icon/glitched_icon.png");
            if (icon != null) PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            // keep the Glitched shaders in builds even when only made at runtime
            var gs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset").FirstOrDefault();
            if (gs != null)
            {
                var so = new SerializedObject(gs);
                var arr = so.FindProperty("m_AlwaysIncludedShaders");
                foreach (var name in new[] { "Glitched/UIGlitch", "Glitched/Particle", "Glitched/Hologram", "Glitched/Flow", "Glitched/Toon", "Glitched/Glitchable", "Glitched/FaceDecal", "Glitched/Impostor", "Glitched/MonitorScreen" })
                {
                    var sh = Shader.Find(name);
                    if (sh == null) continue;
                    bool found = false;
                    for (int i = 0; i < arr.arraySize; i++) if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) found = true;
                    if (!found) { arr.InsertArrayElementAtIndex(arr.arraySize); arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh; }
                }
                // the realistic shaders come in through their saved materials; forcing them would include every variant
                foreach (var name in new[] { "Glitched/Realistic", "Glitched/Facade", "Glitched/Terrain", "Glitched/Water" })
                {
                    var sh = Shader.Find(name);
                    for (int i = arr.arraySize - 1; i >= 0; i--)
                        if (sh != null && arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) arr.DeleteArrayElementAtIndex(i);
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
