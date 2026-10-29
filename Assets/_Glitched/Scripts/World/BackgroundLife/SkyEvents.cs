using System.Collections;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Occasional big sky moments (L5): a Glitch tear opening, a giant data whale or blimp drifting past, lightning.
    /// Only on tiers with L5 events enabled.
    /// </summary>
    public class SkyEvents : MonoBehaviour
    {
        public float minInterval = 18f, maxInterval = 40f;
        public bool whales = true, blimps = true, lightning;
        public Texture2D blimpAd;
        float timer;
        bool busy;

        void Start() => timer = Random.Range(6f, 14f);

        void Update()
        {
            if (busy || !Services.Graphics.LayerActive(DepthLayer.L5_SkyEvents)) return;
            timer -= Time.deltaTime;
            if (timer > 0) return;
            timer = Random.Range(minInterval, maxInterval);
            float c = Services.Corruption.Value;
            float r = Random.value;
            if (c > 0.4f && r < 0.4f) StartCoroutine(Tear());
            else if (lightning && r < 0.65f) StartCoroutine(Lightning());
            else if (whales && r < 0.85f) StartCoroutine(Fly(BuildWhale(), 40f));
            else if (blimps) StartCoroutine(Fly(BuildBlimp(), 50f));
        }

        Vector3 CamPos => CameraRig.Instance != null ? CameraRig.Instance.Cam.transform.position : Vector3.zero;

        IEnumerator Tear()
        {
            busy = true;
            var sky = RenderSettings.skybox;
            Services.Audio.Play("sky_tear", 0.6f);
            GlitchFX.Pulse(0.3f, 0.6f);
            for (float t = 0; t < 2.5f; t += Time.deltaTime)
            {
                if (sky != null) sky.SetFloat("_Tears", 1f + Mathf.Sin(t / 2.5f * Mathf.PI) * 3f);
                yield return null;
            }
            if (sky != null) sky.SetFloat("_Tears", 1f);
            busy = false;
        }

        IEnumerator Lightning()
        {
            busy = true;
            var go = new GameObject("Lightning");
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 12;
            lr.widthMultiplier = 1.4f;
            lr.material = MaterialCache.Instance("padd");
            lr.startColor = lr.endColor = new Color(0.8f, 0.9f, 1.5f);
            Vector3 a = CamPos + new Vector3(Random.Range(-40f, 40f), 60f, 140f);
            Vector3 b = a + new Vector3(Random.Range(-20f, 20f), -70f, 0);
            for (int i = 0; i < 12; i++) lr.SetPosition(i, Vector3.Lerp(a, b, i / 11f) + new Vector3(Random.Range(-4f, 4f), 0, 0));
            var sun = RenderSettings.sun;
            float baseI = sun != null ? sun.intensity : 1f;
            Services.Audio.Play("thunder", 0.7f);
            for (int k = 0; k < 3; k++)
            {
                if (sun != null) sun.intensity = baseI * 3f;
                lr.enabled = true;
                yield return new WaitForSeconds(0.06f);
                if (sun != null) sun.intensity = baseI;
                lr.enabled = false;
                yield return new WaitForSeconds(0.08f);
            }
            Destroy(go);
            busy = false;
        }

        IEnumerator Fly(Transform thing, float seconds)
        {
            busy = true;
            float y = Random.Range(35f, 60f), z = Random.Range(160f, 240f);
            float dir = Random.value < 0.5f ? 1 : -1;
            Vector3 start = CamPos + new Vector3(-dir * 200f, y, z);
            Vector3 end = CamPos + new Vector3(dir * 200f, y + Random.Range(-10f, 10f), z);
            thing.rotation = Quaternion.Euler(0, dir > 0 ? 0 : 180, 0);
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                float k = t / seconds;
                thing.position = Vector3.Lerp(start, end, k) + Vector3.up * Mathf.Sin(t * 0.6f) * 3f;
                if (thing.childCount > 1) thing.GetChild(1).localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.4f) * 18f);
                yield return null;
            }
            Destroy(thing.gameObject);
            busy = false;
        }

        Transform BuildWhale()
        {
            var root = new GameObject("DataWhale").transform;
            var body = MaterialCache.Emissive(new Color(0.05f, 0.1f, 0.2f), new Color(0.2f, 0.9f, 1.4f), 0.7f, 1f);
            PlaceholderFactory.Part(PrimitiveType.Sphere, root, Vector3.zero, new Vector3(60f, 18f, 18f), body, "Body");
            var tail = new GameObject("Tail").transform; tail.SetParent(root, false); tail.localPosition = new Vector3(-30f, 0, 0);
            PlaceholderFactory.Part(PrimitiveType.Cube, tail, new Vector3(-6f, 0, 0), new Vector3(10f, 2f, 18f), body, "Fluke");
            PlaceholderFactory.Part(PrimitiveType.Cube, root, new Vector3(5f, -8f, 8f), new Vector3(12f, 1.5f, 6f), body, "Fin");
            foreach (var r in root.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return root;
        }

        Transform BuildBlimp()
        {
            var root = new GameObject("Blimp").transform;
            PlaceholderFactory.Part(PrimitiveType.Capsule, root, Vector3.zero, new Vector3(14f, 30f, 14f), MaterialCache.Glitch(new Color(0.85f, 0.85f, 0.95f), 0.5f), "Body", new Vector3(0, 0, 90));
            var screen = PlaceholderFactory.MeshPart(ProceduralMeshes.Quad(), root, new Vector3(0, -2f, -7.2f), new Vector3(24f, 8f, 1f), MaterialCache.Get("holo"), "Ad");
            var hb = screen.gameObject.AddComponent<HologramBillboard>();
            hb.content = blimpAd;
            hb.scroll = new Vector2(0.05f, 0);
            foreach (var r in root.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return root;
        }
    }
}
