using System.Collections;
using TMPro;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// The way on to the next chapter: after a region's Glitch Lord falls and the region heals, a portal tears open
    /// in the arena (Code City -> Physics Valley -> Chemical Chaos -> Bio-Zone), and after the Bio-Zone a red exam
    /// rift opens into Kanha the Dard's Examination Hall. A glowing ring, a spiralling vortex, swirling sparks and a
    /// label; walk into it and it pulls you through a hyperspace streak to the next chapter.
    /// </summary>
    public class ChapterPortal : MonoBehaviour
    {
        public string targetScene, destination;
        public Color color = Color.cyan;
        public bool exam;
        Transform vortexA, vortexB, ring;
        Light glow;
        bool entering;
        float born;

        public static ChapterPortal Open(Vector3 ground, string targetScene, string destination, Color color, bool exam)
        {
            var go = new GameObject(exam ? "ExamRift" : "ChapterPortal");
            go.transform.position = ground;
            var cp = go.AddComponent<ChapterPortal>();
            cp.targetScene = targetScene; cp.destination = destination; cp.color = color; cp.exam = exam;
            cp.Build();
            return cp;
        }

        /// <summary>A spiral-arm texture for the vortex (generated once).</summary>
        static Texture2D spiral;
        static Texture2D Spiral()
        {
            if (spiral != null) return spiral;
            const int N = 256;
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);
                    float a = Mathf.Atan2(v, u);
                    float arms = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(a * 4f + Mathf.Log(r + 0.02f) * 7f), 3f);
                    float fall = Mathf.Clamp01(1f - r) * Mathf.Clamp01(r * 3f);
                    float core = Mathf.Clamp01(1f - r * 4f);
                    float k = Mathf.Clamp01(arms * fall + core);
                    byte b = (byte)(k * 255);
                    px[y * N + x] = new Color32(b, b, b, b);
                }
            spiral = new Texture2D(N, N, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "PortalSpiral" };
            spiral.SetPixels32(px); spiral.Apply();
            return spiral;
        }

        void Build()
        {
            born = Time.time;
            var c = color;
            float h = 2.3f;   // vortex centre height
            // ring: a thick glowing ellipse of tube segments
            var mb = new MeshBuilder(2);
            const int seg = 48;
            Vector3 P(int i) { float a = i / (float)seg * Mathf.PI * 2f; return new Vector3(Mathf.Cos(a) * 1.55f, Mathf.Sin(a) * 2.05f, 0f); }
            for (int i = 0; i < seg; i++) mb.Tube(P(i), P(i + 1), 0.12f, 0.12f, 8, i % 6 == 0 ? 1 : 0);
            for (int i = 0; i < seg; i += 4) mb.BoxC(P(i) * 1.1f, new Vector3(0.22f, 0.22f, 0.3f), 1);
            ring = new GameObject("Ring").transform;
            ring.SetParent(transform, false);
            ring.localPosition = new Vector3(0, h, 0);
            mb.ToObject("RingMesh", ring, new[] { CityMaterials.Emissive(c * 0.3f, c * 3.2f), CityMaterials.Solid(new Color(0.1f, 0.1f, 0.12f), 0.3f, 0.9f) });
            // two counter-rotating spiral layers
            vortexA = Disc("VortexA", h, c * 1.6f, new Vector3(3.2f, 4.2f, 1f), 0.02f);
            vortexB = Disc("VortexB", h, Color.Lerp(c, Color.white, 0.5f) * 1.1f, new Vector3(2.6f, 3.5f, 1f), 0.04f);
            // sparks spiralling into the middle
            var ps = new GameObject("Swirl").AddComponent<ParticleSystem>();
            ps.transform.SetParent(transform, false);
            ps.transform.localPosition = new Vector3(0, h, -0.1f);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.startLifetime = 1.6f; main.startSpeed = 0f; main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(c, Color.white); main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var em = ps.emission; em.rateOverTime = 120f * (Services.Graphics != null ? Services.Graphics.ParticleMultiplier : 1f);
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 1.9f; sh.radiusThickness = 0.1f;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
            vel.orbitalZ = new ParticleSystem.MinMaxCurve(2.4f); vel.radial = new ParticleSystem.MinMaxCurve(-1.1f);
            vel.x = new ParticleSystem.MinMaxCurve(0f); vel.y = new ParticleSystem.MinMaxCurve(0f); vel.z = new ParticleSystem.MinMaxCurve(0f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var pr = ps.GetComponent<ParticleSystemRenderer>();
            var pm = MaterialCache.Instance("padd");
            var lib = Services.Db != null ? Services.Db.materials : null;
            var tex = lib != null ? lib.Particle("spark") : null;
            if (tex != null) pm.SetTexture("_BaseMap", tex);
            pr.sharedMaterial = pm;
            ps.Play();
            // light
            glow = new GameObject("Glow").AddComponent<Light>();
            glow.transform.SetParent(transform, false);
            glow.transform.localPosition = new Vector3(0, h, -1.2f);
            glow.type = LightType.Point; glow.range = 12f; glow.intensity = 6f; glow.color = c;
            // label
            var lt = new GameObject("Label").AddComponent<TextMeshPro>();
            lt.transform.SetParent(transform, false);
            lt.transform.localPosition = new Vector3(0, h + 2.9f, 0);
            var font = Services.Db != null && Services.Db.uiTheme != null ? Services.Db.uiTheme.displayFont : null;
            if (font != null) lt.font = font;
            lt.text = (exam ? "THE FINAL EXAM ▸ " : "NEXT CHAPTER ▸ ") + destination;
            lt.fontSize = 5f; lt.alignment = TextAlignmentOptions.Center; lt.color = Color.Lerp(c, Color.white, 0.4f) * 1.3f;
            lt.rectTransform.sizeDelta = new Vector2(16, 2); lt.textWrappingMode = TextWrappingModes.NoWrap;
            transform.localScale = Vector3.one * 0.01f;
            StartCoroutine(OpenUp());
        }

        Transform Disc(string name, float h, Color c, Vector3 scale, float z)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = name;
            q.transform.SetParent(transform, false);
            q.transform.localPosition = new Vector3(0, h, z);
            q.transform.localScale = scale;
            var m = MaterialCache.Instance("padd");
            m.SetTexture("_BaseMap", Spiral());
            m.SetColor("_BaseColor", c);
            q.GetComponent<Renderer>().sharedMaterial = m;
            return q.transform;
        }

        IEnumerator OpenUp()
        {
            Services.Audio.Play("powerup");
            Services.Audio.Play("glitch_big");
            VFX.Ring(transform.position + Vector3.up * 2.3f, color, 4f);
            CameraRig.Instance?.Shake(0.8f);
            GlitchFX.Pulse(0.4f, 0.3f);
            for (float t = 0; t < 0.9f; t += Time.deltaTime)
            {
                transform.localScale = Vector3.one * Easing.Evaluate(Ease.OutBack, t / 0.9f);
                yield return null;
            }
            transform.localScale = Vector3.one;
        }

        void Update()
        {
            float t = Time.time - born;
            if (vortexA != null) vortexA.localRotation = Quaternion.Euler(0, 0, -t * 140f);
            if (vortexB != null) vortexB.localRotation = Quaternion.Euler(0, 0, t * 90f);
            if (ring != null) ring.localScale = Vector3.one * (1f + Mathf.Sin(t * 3f) * 0.02f);
            if (glow != null) glow.intensity = 5f + Mathf.Sin(t * 5f) * 1.5f;
            var p = PlayerController.Instance;
            if (entering || p == null || t < 1.2f) return;
            var d = p.transform.position - transform.position;
            if ((Mathf.Abs(d.x) < 1.1f && d.y > -1f && d.y < 3f) || (ParkourBot.Active && t > 4f)) StartCoroutine(Enter(p));   // the test bot is taken through
        }

        IEnumerator Enter(PlayerController p)
        {
            entering = true;
            p.Lock(this);
            Services.Audio.Play("transition");
            Services.Audio.Play("whoosh");
            CameraRig.Instance?.PunchIn(10f);
            // pulled in: into the middle, spinning, shrinking
            Vector3 from = p.transform.position, to = transform.position + Vector3.up * 2.3f;
            var model = p.Visual != null ? p.Visual.transform : p.transform;
            for (float t = 0; t < 0.9f; t += Time.deltaTime)
            {
                float k = Easing.Evaluate(Ease.InCubic, t / 0.9f);
                p.Teleport(Vector3.Lerp(from, to, k));
                model.localScale = Vector3.one * Mathf.Lerp(1f, 0.05f, k);
                model.Rotate(0, 0, 500f * Time.deltaTime, Space.Self);
                GlitchFX.Pulse(0.2f + k * 0.4f, 0.1f);
                yield return null;
            }
            yield return Transit(destination, color, exam);
            if (exam) Services.Game.Data.SetFlag("exam_transfer");
            Services.Save.Write();
            Services.Scenes.Load(targetScene, "");
        }

        /// <summary>Hyperspace: streaks of light rushing out of the middle of a black screen, the chapter title.</summary>
        public static IEnumerator Transit(string destination, Color color, bool exam)
        {
            var root = UIFactory.Stretch(UIFactory.Rect("PortalTransit", Services.UI.OverlayLayer));
            root.gameObject.AddComponent<DestroyOnSceneChange>();
            var bg = UIFactory.Image(root, new Color(0, 0, 0, 0), null, "Black");
            UIFactory.Stretch(bg.rectTransform);
            var streaks = new RectTransform[70];
            var dirs = new Vector2[streaks.Length];
            var speeds = new float[streaks.Length];
            for (int i = 0; i < streaks.Length; i++)
            {
                var s = UIFactory.Image(root, Color.Lerp(color, Color.white, Random.value * 0.7f), null, "Streak");
                streaks[i] = s.rectTransform;
                float a = Random.value * Mathf.PI * 2f;
                dirs[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                speeds[i] = Random.Range(0.6f, 1.4f);
                s.rectTransform.anchorMin = s.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                s.rectTransform.pivot = new Vector2(0f, 0.5f);
                s.rectTransform.localRotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg);
            }
            var title = UIFactory.Text(root, (exam ? "<size=50%>THE FINAL EXAM</size>\n" : "<size=50%>NEXT CHAPTER</size>\n") + destination, 96, Color.white, TextAlignmentOptions.Center, true);
            UIFactory.Stretch(title.rectTransform);
            title.alpha = 0f;
            title.outlineWidth = 0.15f; title.outlineColor = new Color32(0, 0, 0, 255);
            for (float t = 0; t < 2.4f; t += Time.unscaledDeltaTime)
            {
                bg.color = new Color(0, 0, 0, Mathf.Clamp01(t * 3f));
                for (int i = 0; i < streaks.Length; i++)
                {
                    float d = Mathf.Repeat(t * 900f * speeds[i] + i * 37f, 1300f);
                    streaks[i].anchoredPosition = dirs[i] * d;
                    streaks[i].sizeDelta = new Vector2(20f + d * 0.35f * speeds[i], 3f + d * 0.004f);
                }
                title.alpha = Mathf.Clamp01((t - 0.6f) * 2f);
                yield return null;
            }
        }
    }
}
