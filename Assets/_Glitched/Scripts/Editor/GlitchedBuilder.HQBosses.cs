using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Glitched.EditorTools
{
    public static partial class GlitchedBuilder
    {
        /// <summary>
        /// Realistic bosses: rigged, animated Sketchfab models (CC BY, see CreditsScreen) converted by
        /// Tools/blender_glb_to_fbx.py into Art/Characters/BossesHQ/&lt;bossId&gt;/ (FBX + Textures/ + manifest json).
        /// Each material becomes a Glitched/Realistic PBR material (albedo, normal, roughness/metal, AO, emission,
        /// plus hit flash and dissolve), remapped onto the FBX; the boss then uses the model with a generic
        /// controller built from its own clips (BuildAnimators).
        /// </summary>
        static readonly Dictionary<string, float> HQBossHeights = new Dictionary<string, float>
        {
            { "sergeant_syntax", 2.9f }, { "lord_infinite_loop", 5.2f },
            { "baron_friction", 2.5f }, { "lord_broken_laws", 4.4f },
            { "count_catalyst", 3f }, { "lord_of_the_lab", 3.4f },
            { "professor_plaque", 2.2f }, { "lord_within", 3.6f },
            { "the_source", 5f },
        };

        [System.Serializable] class HQMat { public string @base, normal, mr, emission, occlusion; public float[] color; public float metallic = 0f, roughness = 0.7f, emissive; }

        static void BuildHQBosses()
        {
            string root = Art + "/Characters/BossesHQ";
            if (!AssetDatabase.IsValidFolder(root)) return;
            var sh = Shader.Find("Glitched/Realistic");
            if (sh == null) { Debug.LogWarning("[GLITCHED] HQ bosses: Glitched/Realistic missing"); return; }
            EnsureFolder(Art + "/Materials/BossesHQ");
            var bosses = db.arcs.Where(a => a != null).SelectMany(a => new[] { a.miniBoss, a.boss }).Where(b => b != null).ToList();
            int n = 0;
            foreach (var kv in HQBossHeights)
            {
                string id = kv.Key, dir = $"{root}/{id}", fbx = $"{dir}/{id}.fbx", manifest = $"{dir}/{id}.json";
                var boss = bosses.Find(b => b.id == id);
                if (boss == null || !File.Exists(fbx) || !File.Exists(manifest)) continue;
                var importer = AssetImporter.GetAtPath(fbx) as ModelImporter;
                if (importer == null) continue;
                var mats = ParseMaterials(File.ReadAllText(manifest));
                bool changed = false;
                foreach (var m in AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<Material>().Select(x => x.name).Concat(mats.Keys).Distinct().ToList())
                {
                    if (!mats.TryGetValue(m, out var d)) d = new HQMat();
                    string matPath = $"{Art}/Materials/BossesHQ/{id}_{Safe(m)}.mat";
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (mat == null) { mat = new Material(sh) { name = Path.GetFileNameWithoutExtension(matPath) }; AssetDatabase.CreateAsset(mat, matPath); }
                    mat.shader = sh;
                    Texture2D T(string file) => string.IsNullOrEmpty(file) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/Textures/{file}");
                    var albedo = T(d.@base);
                    mat.SetTexture("_BaseMap", albedo);
                    mat.SetColor("_BaseColor", albedo == null && d.color != null && d.color.Length >= 3 ? new Color(d.color[0], d.color[1], d.color[2], 1f) : Color.white);
                    var nrm = T(d.normal);
                    mat.SetTexture("_BumpMap", nrm);
                    mat.SetFloat("_BumpScale", nrm != null ? 1f : 0f);
                    var mr = T(d.mr);
                    mat.SetTexture("_RoughnessMap", mr);
                    mat.SetFloat("_Roughness", mr != null ? 1f : Mathf.Clamp(d.roughness, 0.05f, 1f));
                    mat.SetFloat("_Metallic", mr != null ? 1f : Mathf.Clamp01(d.metallic));
                    mat.SetFloat("_MetalFromMap", mr != null ? 1f : 0f);
                    var ao = T(d.occlusion);
                    mat.SetTexture("_OcclusionMap", ao);
                    mat.SetFloat("_OcclusionStrength", ao != null ? 1f : 0f);
                    var emi = T(d.emission);
                    float e = Mathf.Clamp(d.emissive, 0f, 4f);
                    if (emi != null) { mat.SetTexture("_EmissionMap", emi); mat.SetColor("_EmissionColor", Color.white * Mathf.Max(1f, e * 0.5f)); }
                    else if (e > 0.01f && d.color != null) { mat.SetTexture("_EmissionMap", Texture2D.whiteTexture); mat.SetColor("_EmissionColor", new Color(d.color[0], d.color[1], d.color[2]) * e); }
                    else mat.SetColor("_EmissionColor", Color.black);
                    mat.SetFloat("_Glitchiness", 0.35f);
                    mat.SetFloat("_Seed", Mathf.Abs(id.GetHashCode() % 97));
                    mat.enableInstancing = true;
                    EditorUtility.SetDirty(mat);
                    var sid = new AssetImporter.SourceAssetIdentifier(typeof(Material), m);
                    if (!importer.GetExternalObjectMap().TryGetValue(sid, out var cur) || cur != mat) { importer.AddRemap(sid, mat); changed = true; }
                }
                if (changed) importer.SaveAndReimport();
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
                if (model == null) continue;
                boss.modelPrefab = model;
                boss.height = kv.Value;
                EditorUtility.SetDirty(boss);
                n++;
            }
            Debug.Log($"[GLITCHED] HQ bosses: {n} realistic boss models assigned");
        }

        /// <summary>The "materials" object of a converter manifest (JsonUtility cannot read dictionaries).</summary>
        static Dictionary<string, HQMat> ParseMaterials(string json)
        {
            var result = new Dictionary<string, HQMat>();
            int at = json.IndexOf("\"materials\"");
            if (at < 0) return result;
            int i = json.IndexOf('{', at) + 1;
            while (i > 0 && i < json.Length)
            {
                int q0 = json.IndexOf('"', i);
                int close = json.IndexOf('}', i);
                if (q0 < 0 || (close >= 0 && close < q0)) break;
                int q1 = json.IndexOf('"', q0 + 1);
                string name = json.Substring(q0 + 1, q1 - q0 - 1);
                int b0 = json.IndexOf('{', q1), depth = 0, b1 = b0;
                for (; b1 < json.Length; b1++) { if (json[b1] == '{') depth++; else if (json[b1] == '}' && --depth == 0) break; }
                var obj = json.Substring(b0, b1 - b0 + 1).Replace("null", "\"\"").Replace("true", "1").Replace("false", "0");
                result[name] = JsonUtility.FromJson<HQMat>(obj);
                i = b1 + 1;
            }
            return result;
        }
    }
}
