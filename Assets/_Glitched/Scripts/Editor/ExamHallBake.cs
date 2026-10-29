using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Glitched.EditorTools
{
    /// <summary>
    /// Bakes real global illumination into Kanha's Examination Hall on the GPU: sunlight through the windows and the
    /// tube lights bounce off the wooden floor and plaster walls into lightmaps (Baked Indirect: direct light and
    /// shadows stay realtime), a light-probe grid lights Shaurya, Kanha and the flying papers with the same bounce,
    /// and a baked box-projected reflection probe gives the floor and glass the hall's own reflections.
    /// </summary>
    public static class ExamHallBake
    {
        const string ScenePath = "Assets/_Glitched/Scenes/Kanha_Final.unity";

        [MenuItem("Glitched/Bake Exam Hall Lighting", priority = 30)]
        public static void Bake()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var refs = Object.FindFirstObjectByType<ExamHallRefs>();
            if (refs == null) { Debug.LogError("[GLITCHED] Exam hall bake: no ExamHallRefs in " + ScenePath); return; }
            var root = refs.transform;

            // static geometry: second UV set for the lightmaps, contributes to and receives GI
            int meshes = 0;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || mf.sharedMesh == null) continue;
                string n = mf.gameObject.name;
                bool staticPart = n == "Shell" || n == "Desks" || n == "Stage" || n == "Windows" || n == "TubeLights" || n == "Face";
                if (!staticPart || mf.GetComponentInParent<FloatingPaper>() != null) continue;
                Unwrapping.GenerateSecondaryUVSet(mf.sharedMesh, new UnwrapParam { angleError = 0.08f, areaError = 0.15f, hardAngle = 88f, packMargin = 0.004f });
                var flags = GameObjectUtility.GetStaticEditorFlags(mf.gameObject) | StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic;
                GameObjectUtility.SetStaticEditorFlags(mf.gameObject, flags);
                mr.receiveGI = ReceiveGI.Lightmaps;
                mr.scaleInLightmap = n == "Desks" ? 0.6f : n == "Shell" ? 1f : 0.8f;
                foreach (var m in mr.sharedMaterials)
                    if (m != null && m.HasProperty("_EmissionColor") && m.GetColor("_EmissionColor").maxColorComponent > 0.05f)
                    {
                        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;   // the sky behind the windows and the tubes light the room
                        EditorUtility.SetDirty(m);
                    }
                meshes++;
            }

            // lights: the sun stays realtime direct + baked bounce; the tubes and stage lights are fully baked (direct too),
            // so the hall can have as many as it needs (URP drops realtime lights past 8 per object) and characters get them via the probes
            foreach (var l in root.GetComponentsInChildren<Light>(true))
            {
                l.lightmapBakeType = l.type == LightType.Directional ? LightmapBakeType.Mixed : LightmapBakeType.Baked;
                if (l.type == LightType.Directional) l.bounceIntensity = 1.3f;   // late sun: a strong warm bounce off the floor
            }

            // light probes through the open part of the hall (dynamic things: the players, Kanha, papers)
            var b = ExamHall.Interior;
            var probes = new System.Collections.Generic.List<Vector3>();
            for (float x = b.min.x + 1f; x <= b.max.x - 1f; x += 3f)
                foreach (float y in new[] { 0.4f, 1.4f, 2.6f, 4.5f, 7.5f })
                    for (float z = b.min.z + 1f; z <= b.max.z - 1f; z += 3f)
                    {
                        float floorY = x > 23f ? 0.6f : 0f;   // the stage
                        probes.Add(new Vector3(x, floorY + y, z));
                    }
            var pgo = GameObject.Find("HallProbes"); if (pgo == null) pgo = new GameObject("HallProbes");
            pgo.transform.SetParent(root, false); pgo.transform.localPosition = Vector3.zero;
            var group = pgo.GetComponent<LightProbeGroup>(); if (group == null) group = pgo.AddComponent<LightProbeGroup>();
            group.probePositions = probes.ToArray();

            // reflections: one baked, box-projected probe that fits the room
            var rgo = GameObject.Find("HallReflection"); if (rgo == null) rgo = new GameObject("HallReflection");
            rgo.transform.SetParent(root, false);
            rgo.transform.position = new Vector3(12f, 2.2f, 0f);   // the gameplay line, at eye height
            var rp = rgo.GetComponent<ReflectionProbe>(); if (rp == null) rp = rgo.AddComponent<ReflectionProbe>();
            rp.mode = ReflectionProbeMode.Baked;
            rp.boxProjection = true;
            rp.center = b.center - rgo.transform.position;
            rp.size = b.size + new Vector3(0.6f, 0.4f, 0.6f);
            rp.resolution = 512; rp.hdr = true; rp.importance = 2;

            // lighting settings for this scene only (the city levels stay fully realtime: their light changes as they are restored)
            const string lsPath = GlitchedBuilder.Root + "/Settings/Lighting_ExamHall.lighting";
            var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(lsPath);
            if (ls == null) { ls = new LightingSettings { name = "Lighting_ExamHall" }; AssetDatabase.CreateAsset(ls, lsPath); }
            ls.bakedGI = true; ls.realtimeGI = false;
            ls.mixedBakeMode = MixedLightingMode.IndirectOnly;
            ls.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            ls.lightmapResolution = 14f; ls.lightmapPadding = 3; ls.lightmapMaxSize = 2048;
            ls.directSampleCount = 32; ls.indirectSampleCount = 384; ls.environmentSampleCount = 128; ls.maxBounces = 3;
            ls.filteringMode = LightingSettings.FilterMode.Auto;
            ls.indirectScale = 1.2f;   // a sunlit room full of pale plaster and wood is bright with bounce light
            ls.directionalityMode = LightmapsMode.NonDirectional;
            ls.lightmapCompression = LightmapCompression.HighQuality; ls.ao = false;
            EditorUtility.SetDirty(ls);
            Lightmapping.lightingSettings = ls;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            var t0 = System.DateTime.Now;
            bool ok = Lightmapping.Bake();
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[GLITCHED] Exam hall bake: {(ok ? "done" : "FAILED")} in {(System.DateTime.Now - t0).TotalSeconds:0}s, {meshes} meshes, {probes.Count} probes, {LightmapSettings.lightmaps.Length} lightmaps");
        }

        public static void BakeBatch() { Bake(); EditorApplication.Exit(0); }
    }
}
