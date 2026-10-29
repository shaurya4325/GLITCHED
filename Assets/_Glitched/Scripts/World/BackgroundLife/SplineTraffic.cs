using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using Random = UnityEngine.Random;

namespace Glitched
{
    /// <summary>
    /// Flying cars, drones, trains, blood cells... moving along a Spline at L2-L3 depth, pooled, with speed variation.
    /// The count follows the graphics tier. When corrupted, vehicles stutter, jitter and occasionally reverse.
    /// </summary>
    [RequireComponent(typeof(SplineContainer))]
    public class SplineTraffic : MonoBehaviour
    {
        public List<GameObject> vehiclePrefabs = new List<GameObject>();
        public int maxCount = 14;
        public float speed = 0.03f;            // spline fraction per second
        public float speedVariation = 0.5f;
        public float scale = 1f;
        public bool faceAlong = true;
        public Color placeholderColor = new Color(0.3f, 0.9f, 1f);
        public string placeholderShape = "car";  // car, drone, cell, bubble, debris

        SplineContainer spline;
        readonly List<Transform> vehicles = new List<Transform>();
        readonly List<float> t = new List<float>(), spd = new List<float>(), lane = new List<float>();

        void Start()
        {
            spline = GetComponent<SplineContainer>();
            Rebuild();
            GameEvents.TierChanged += OnTier;
        }

        void OnDestroy() => GameEvents.TierChanged -= OnTier;
        void OnTier(GraphicsTier tier) => Rebuild();

        void Rebuild()
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(maxCount * Services.Graphics.DensityMultiplier));
            while (vehicles.Count > count) { Destroy(vehicles[vehicles.Count - 1].gameObject); vehicles.RemoveAt(vehicles.Count - 1); t.RemoveAt(t.Count - 1); spd.RemoveAt(spd.Count - 1); lane.RemoveAt(lane.Count - 1); }
            while (vehicles.Count < count)
            {
                Transform v;
                if (vehiclePrefabs.Count > 0 && vehiclePrefabs[0] != null)
                {
                    v = Instantiate(vehiclePrefabs[Random.Range(0, vehiclePrefabs.Count)], transform).transform;
                    foreach (var c in v.GetComponentsInChildren<Collider>()) Destroy(c);
                }
                else v = BuildPlaceholder();
                v.localScale *= scale * Random.Range(0.8f, 1.2f);
                foreach (var r in v.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                vehicles.Add(v);
                t.Add(Random.value);
                spd.Add(speed * (1f + Random.Range(-speedVariation, speedVariation)) * (Random.value < 0.5f ? 1 : -1));
                lane.Add(Random.Range(-1.5f, 1.5f));
            }
        }

        Transform BuildPlaceholder()
        {
            var root = new GameObject("Vehicle").transform;
            root.SetParent(transform, false);
            var body = MaterialCache.Emissive(placeholderColor * 0.3f, placeholderColor * 1.4f, 0f, 0.6f);
            var dark = MaterialCache.Glitch(new Color(0.1f, 0.12f, 0.18f), 0.6f);
            switch (placeholderShape)
            {
                case "cell":
                    PlaceholderFactory.Part(PrimitiveType.Sphere, root, Vector3.zero, new Vector3(1.4f, 1.4f, 0.5f), MaterialCache.Glitch(placeholderColor, 0.4f), "Cell");
                    PlaceholderFactory.Part(PrimitiveType.Sphere, root, new Vector3(0, 0, -0.1f), new Vector3(0.7f, 0.7f, 0.4f), MaterialCache.Glitch(placeholderColor * 0.7f, 0.4f), "Dip");
                    break;
                case "bubble":
                    PlaceholderFactory.Part(PrimitiveType.Sphere, root, Vector3.zero, Vector3.one * 0.8f, MaterialCache.Get("holo"), "Bubble");
                    break;
                case "debris":
                    PlaceholderFactory.MeshPart(ProceduralMeshes.Crystal(), root, Vector3.zero, new Vector3(1f, 1.5f, 1f), MaterialCache.Glitch(placeholderColor, 1f), "Rock");
                    root.gameObject.AddComponent<Spinner>().speed = new Vector3(Random.Range(-40, 40), Random.Range(-60, 60), Random.Range(-30, 30));
                    break;
                case "drone":
                    PlaceholderFactory.Part(PrimitiveType.Cube, root, Vector3.zero, new Vector3(0.8f, 0.25f, 0.8f), dark, "Body");
                    PlaceholderFactory.Part(PrimitiveType.Sphere, root, new Vector3(0, -0.15f, 0), Vector3.one * 0.25f, body, "Eye");
                    foreach (var x in new[] { -0.5f, 0.5f }) foreach (var z in new[] { -0.5f, 0.5f })
                            PlaceholderFactory.Part(PrimitiveType.Cylinder, root, new Vector3(x, 0.15f, z), new Vector3(0.45f, 0.02f, 0.45f), body, "Rotor");
                    break;
                default:
                    PlaceholderFactory.Part(PrimitiveType.Cube, root, Vector3.zero, new Vector3(2.4f, 0.6f, 1.1f), dark, "Body");
                    PlaceholderFactory.Part(PrimitiveType.Cube, root, new Vector3(-0.1f, 0.42f, 0), new Vector3(1.2f, 0.4f, 0.9f), MaterialCache.Glitch(new Color(0.2f, 0.3f, 0.45f), 0.6f), "Cabin");
                    PlaceholderFactory.Part(PrimitiveType.Cube, root, new Vector3(1.2f, 0, 0), new Vector3(0.05f, 0.2f, 0.9f), body, "Light");
                    PlaceholderFactory.Part(PrimitiveType.Cube, root, new Vector3(0, -0.32f, 0), new Vector3(2.2f, 0.05f, 1.0f), body, "Underglow");
                    break;
            }
            return root;
        }

        void Update()
        {
            if (spline == null || spline.Spline == null) return;
            float c = Services.Corruption.Value;
            float dt = Time.deltaTime;
            for (int i = 0; i < vehicles.Count; i++)
            {
                float s = spd[i];
                // corruption: stutter and occasionally run backwards for a moment
                if (c > 0.4f && Mathf.Repeat(Time.time * 0.37f + i * 0.61f, 1f) < 0.06f * c) s = -s * 0.5f;
                t[i] = Mathf.Repeat(t[i] + s * dt, 1f);
                float3 p = spline.EvaluatePosition(t[i]);
                float3 tan = spline.EvaluateTangent(t[i]);
                Vector3 pos = (Vector3)p + new Vector3(0, lane[i] * 0.8f, lane[i]);
                if (c > 0.5f && Random.value < 0.01f * c) pos += Random.insideUnitSphere * 1.5f;
                var v = vehicles[i];
                v.position = pos;
                if (faceAlong)
                {
                    Vector3 dir = ((Vector3)tan).normalized * Mathf.Sign(s);
                    if (dir.sqrMagnitude > 0.001f) v.rotation = Quaternion.Slerp(v.rotation, Quaternion.LookRotation(dir) * Quaternion.Euler(0, -90, 0), dt * 6f);
                }
            }
        }
    }
}
