using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Background NPCs (simple animated figures) walking along a path in the near city. Frozen mid-step when
    /// corrupted; they walk and bob happily once the region is restored.
    /// </summary>
    public class CrowdLoop : MonoBehaviour
    {
        public int maxCount = 16;
        public float length = 80f;
        public float speed = 1.2f;
        public Color[] shirts = { new Color(0.9f, 0.4f, 0.4f), new Color(0.4f, 0.7f, 1f), new Color(1f, 0.85f, 0.3f), new Color(0.5f, 1f, 0.6f), new Color(0.8f, 0.5f, 1f) };
        [Tooltip("people, or blobs (Chemical Chaos element creatures: H white, O red, C black, Na yellow)")]
        public string style = "people";
        readonly List<Transform> people = new List<Transform>();
        readonly List<float> pos = new List<float>(), dir = new List<float>(), phase = new List<float>();

        void Start()
        {
            int n = Mathf.Max(2, Mathf.RoundToInt(maxCount * Services.Graphics.DensityMultiplier));
            for (int i = 0; i < n; i++)
            {
                var p = new GameObject("Walker").transform;
                p.SetParent(transform, false);
                if (style == "blobs")
                {
                    Color[] elements = { new Color(0.95f, 0.95f, 1f), new Color(1f, 0.25f, 0.25f), new Color(0.15f, 0.15f, 0.18f), new Color(1f, 0.85f, 0.2f) };
                    var body = MaterialCache.Glitch(elements[i % elements.Length], 0.6f);
                    PlaceholderFactory.Part(PrimitiveType.Sphere, p, new Vector3(0, 0.45f, 0), new Vector3(0.9f, 0.8f, 0.8f), body, "Blob");
                    PlaceholderFactory.Part(PrimitiveType.Sphere, p, new Vector3(0.14f, 0.6f, 0.34f), Vector3.one * 0.18f, MaterialCache.Glitch(Color.white, 0.2f), "EyeR");
                    PlaceholderFactory.Part(PrimitiveType.Sphere, p, new Vector3(-0.14f, 0.6f, 0.34f), Vector3.one * 0.18f, MaterialCache.Glitch(Color.white, 0.2f), "EyeL");
                }
                else
                {
                    var shirt = MaterialCache.Glitch(shirts[i % shirts.Length], 0.6f);
                    var skin = MaterialCache.Glitch(Color.HSVToRGB(0.07f, Random.Range(0.2f, 0.5f), Random.Range(0.5f, 0.95f)), 0.6f);
                    PlaceholderFactory.Part(PrimitiveType.Capsule, p, new Vector3(0, 0.75f, 0), new Vector3(0.45f, 0.5f, 0.35f), shirt, "Body");
                    PlaceholderFactory.Part(PrimitiveType.Sphere, p, new Vector3(0, 1.45f, 0), Vector3.one * 0.38f, skin, "Head");
                }
                foreach (var r in p.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                p.localScale = Vector3.one * Random.Range(0.85f, 1.1f);
                people.Add(p);
                pos.Add(Random.Range(0, length));
                dir.Add(Random.value < 0.5f ? -1 : 1);
                phase.Add(Random.value * 10);
            }
        }

        void Update()
        {
            float c = Services.Corruption.Value;
            bool frozen = c > 0.5f;
            for (int i = 0; i < people.Count; i++)
            {
                var p = people[i];
                if (!frozen)
                {
                    pos[i] += dir[i] * speed * Time.deltaTime;
                    if (pos[i] < 0 || pos[i] > length) dir[i] = -dir[i];
                }
                float bob = frozen ? 0 : Mathf.Abs(Mathf.Sin((Time.time + phase[i]) * 7f)) * 0.08f;
                p.localPosition = new Vector3(pos[i], bob, (i % 3) * 1.2f);
                p.localRotation = Quaternion.Euler(0, dir[i] > 0 ? 90 : -90, frozen ? Mathf.Sin(phase[i]) * 12f : 0);
            }
        }
    }
}
