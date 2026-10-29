using System.Collections;
using UnityEngine;

namespace Glitched
{
    public enum BlastStyle { Fire, Chemical, Glitch, Electric, Bio }

    /// <summary>
    /// A proper explosion, built from layers that land together (the "stacked feedback" of good game feel): a flash of
    /// light, a fireball with flame licks, real 3D debris chunks that bounce off the ground, sparks and embers, a smoke
    /// plume that rises and lingers, a shockwave ring, a scorch mark, and camera shake + sound that fall off with
    /// distance. Each region has its own colour (fire, toxic green, glitch magenta, electric blue, bio pink).
    /// </summary>
    public static class Blast
    {
        struct Look { public Color core, flame, smoke, spark, light; }

        static Look LookOf(BlastStyle s)
        {
            switch (s)
            {
                case BlastStyle.Chemical: return new Look { core = new Color(0.8f, 1f, 0.5f), flame = new Color(0.35f, 1f, 0.25f), smoke = new Color(0.25f, 0.32f, 0.22f), spark = new Color(0.7f, 1f, 0.3f), light = new Color(0.4f, 1f, 0.3f) };
                case BlastStyle.Glitch: return new Look { core = new Color(1f, 0.85f, 1f), flame = new Color(1f, 0.25f, 0.8f), smoke = new Color(0.22f, 0.15f, 0.28f), spark = new Color(0.3f, 0.95f, 1f), light = new Color(1f, 0.3f, 0.9f) };
                case BlastStyle.Electric: return new Look { core = new Color(0.85f, 0.95f, 1f), flame = new Color(0.35f, 0.65f, 1f), smoke = new Color(0.22f, 0.24f, 0.3f), spark = new Color(0.6f, 0.85f, 1f), light = new Color(0.5f, 0.7f, 1f) };
                case BlastStyle.Bio: return new Look { core = new Color(1f, 0.85f, 0.85f), flame = new Color(1f, 0.35f, 0.5f), smoke = new Color(0.4f, 0.22f, 0.26f), spark = new Color(1f, 0.6f, 0.7f), light = new Color(1f, 0.4f, 0.5f) };
                default: return new Look { core = new Color(1f, 0.92f, 0.7f), flame = new Color(1f, 0.5f, 0.15f), smoke = new Color(0.2f, 0.19f, 0.18f), spark = new Color(1f, 0.75f, 0.35f), light = new Color(1f, 0.6f, 0.3f) };
            }
        }

        public static BlastStyle StyleFor(string arcId)
        {
            switch (arcId)
            {
                case "ChemicalChaos": return BlastStyle.Chemical;
                case "PhysicsValley": return BlastStyle.Electric;
                case "BioZone": return BlastStyle.Bio;
                case "TheCore": return BlastStyle.Glitch;
                default: return BlastStyle.Fire;
            }
        }

        static float Mult => Services.Graphics != null ? Mathf.Max(0.25f, Services.Graphics.ParticleMultiplier) : 1f;
        static int N(float n) => Mathf.Max(1, Mathf.RoundToInt(n * Mult));

        /// <summary>One explosion. scale 1 = a car; 3 = a building floor; 6+ = a distant tower.</summary>
        public static void At(Vector3 pos, float scale = 1f, BlastStyle style = BlastStyle.Fire, bool debris = true, bool scorch = false, bool quiet = false)
        {
            var look = LookOf(style);
            var root = new GameObject("Blast");
            root.transform.position = pos;
            float life = 9f;

            // fireball: a white-hot core that swells, cools to orange and dark red
            var core = System(root, "Core", "fire", true, N(26 * Mathf.Sqrt(scale)));
            {
                var m = core.main;
                m.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.9f);
                m.startSpeed = new ParticleSystem.MinMaxCurve(1.5f * scale, 5f * scale);
                m.startSize = new ParticleSystem.MinMaxCurve(1.2f * scale, 2.8f * scale);
                m.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
                m.gravityModifier = -0.15f;
                var sh = core.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.4f * scale;
                Colour(core, new[] { (look.core * 3.2f, 0f), (look.flame * 2.2f, 0.25f), (look.flame * 0.6f, 0.6f), (look.smoke, 1f) }, 0.9f);
                var sz = core.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.4f), new Keyframe(0.2f, 1f), new Keyframe(1, 1.4f)));
                var vl = core.limitVelocityOverLifetime; vl.enabled = true; vl.dampen = 0.25f; vl.limit = 0.5f * scale;
                var rot = core.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
            }
            // flame licks shooting out
            var licks = System(root, "Flames", "flame", true, N(14 * Mathf.Sqrt(scale)));
            {
                var m = licks.main;
                m.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
                m.startSpeed = new ParticleSystem.MinMaxCurve(6f * scale, 12f * scale);
                m.startSize = new ParticleSystem.MinMaxCurve(0.6f * scale, 1.4f * scale);
                var sh = licks.shape; sh.shapeType = ParticleSystemShapeType.Hemisphere; sh.radius = 0.2f * scale; sh.rotation = new Vector3(-90, 0, 0);
                Colour(licks, new[] { (look.flame * 3f, 0f), (look.flame, 0.5f), (look.smoke * 0.5f, 1f) }, 0.8f);
                var vl = licks.limitVelocityOverLifetime; vl.enabled = true; vl.dampen = 0.35f; vl.limit = scale;
                var r = licks.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.06f; r.lengthScale = 1.2f;
            }
            // sparks and embers: fast streaks, then slow glowing embers that drift down
            var sparks = System(root, "Sparks", "spark", true, N(45 * Mathf.Sqrt(scale)));
            {
                var m = sparks.main;
                m.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.8f);
                m.startSpeed = new ParticleSystem.MinMaxCurve(6f * scale, 18f * scale);
                m.startSize = new ParticleSystem.MinMaxCurve(0.06f * scale + 0.04f, 0.16f * scale + 0.06f);
                m.gravityModifier = 0.8f;
                var sh = sparks.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.3f * scale;
                Colour(sparks, new[] { (Color.white, 0f), (Color.white, 0.6f), (new Color(1, 1, 1, 0), 1f) }, 1f, false);
                Glow(sparks, look.spark * 3.5f);
                var r = sparks.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.04f; r.lengthScale = 2f;
                var drag = sparks.limitVelocityOverLifetime; drag.enabled = true; drag.dampen = 0.08f; drag.limit = 2f;
            }
            // the smoke plume: rises, spreads, lingers, catches the light a little
            var smoke = System(root, "Smoke", "smoke", false, 0);
            {
                var m = smoke.main;
                m.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 6.5f);
                m.startSpeed = new ParticleSystem.MinMaxCurve(0.6f * scale, 2.5f * scale);
                m.startSize = new ParticleSystem.MinMaxCurve(1.8f * scale, 3.6f * scale);
                m.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
                m.gravityModifier = -0.06f;
                var fog = RenderSettings.fogColor;
                Color sc = Color.Lerp(look.smoke, fog * 0.6f, 0.3f);
                var sh = smoke.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.8f * scale;
                Colour(smoke, new[] { (new Color(sc.r * 1.6f, sc.g * 1.4f, sc.b * 1.2f, 0f), 0f), (new Color(sc.r, sc.g, sc.b, 0.85f), 0.12f), (new Color(sc.r, sc.g, sc.b, 0.55f), 0.6f), (new Color(sc.r, sc.g, sc.b, 0f), 1f) }, 1f, false);
                var sz = smoke.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 2.4f));
                var rot = smoke.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
                var vl = smoke.limitVelocityOverLifetime; vl.enabled = true; vl.dampen = 0.12f; vl.limit = 0.4f * scale;
                var em = smoke.emission;
                em.SetBursts(new[] { new ParticleSystem.Burst(0.05f, (short)N(14 * Mathf.Sqrt(scale))), new ParticleSystem.Burst(0.35f, (short)N(8 * Mathf.Sqrt(scale))) });
                var wind = smoke.forceOverLifetime; wind.enabled = true; wind.x = new ParticleSystem.MinMaxCurve(0.4f); wind.y = new ParticleSystem.MinMaxCurve(0.2f); wind.z = new ParticleSystem.MinMaxCurve(0f);
                Glow(smoke, new Color(2.8f, 2.8f, 2.8f, 1f));   // the smoke texture is mid-grey (≈0.23 linear)
                smoke.Play();
            }
            if (debris) Debris(root, scale, style);
            Ring(root, look.flame * 1.6f, scale);
            if (scorch) Scorch(pos, scale);

            // the flash: a real light for a moment
            var lg = new GameObject("Flash");
            lg.transform.SetParent(root.transform, false);
            var light = lg.AddComponent<Light>();
            light.type = LightType.Point; light.color = look.light; light.range = 16f * scale; light.intensity = 0f; light.shadows = LightShadows.None;
            root.AddComponent<BlastFlash>().Init(light, 40f * scale, 0.45f + 0.05f * scale);

            // camera + sound by distance
            var cam = CameraRig.Instance != null ? CameraRig.Instance.Cam : Camera.main;
            float dist = cam != null ? Vector3.Distance(cam.transform.position, pos) : 20f;
            float k = Mathf.Clamp01((14f * Mathf.Sqrt(scale)) / Mathf.Max(1f, dist));
            if (CameraRig.Instance != null && k > 0.05f) { CameraRig.Instance.Shake(Mathf.Lerp(0.15f, 1.6f, k * k) * Mathf.Min(2f, Mathf.Sqrt(scale))); if (k > 0.4f) CameraRig.Instance.PunchIn(3f * k); }
            if (!quiet && Services.Audio != null) Services.Audio.Play(scale >= 2.5f ? "boom" : "explosion", Mathf.Clamp(0.25f + k, 0.2f, 1f));
            if (style == BlastStyle.Glitch) GlitchFX.Pulse(0.25f * k + 0.05f, 0.3f);
            Object.Destroy(root, life);
        }

        /// <summary>A tall column of smoke that keeps billowing up for a while (after a demolition, a distant blast).</summary>
        public static GameObject SmokeColumn(Vector3 pos, float scale, float seconds, Color tint)
        {
            var root = new GameObject("SmokeColumn");
            root.transform.position = pos;
            var ps = System(root, "Smoke", "smoke", false, 0);
            var m = ps.main;
            m.loop = true; m.duration = Mathf.Max(1f, seconds);
            m.startLifetime = new ParticleSystem.MinMaxCurve(7f, 11f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(1f * Mathf.Sqrt(scale), 2.5f * Mathf.Sqrt(scale));
            m.startSize = new ParticleSystem.MinMaxCurve(2.5f * scale, 5f * scale);
            m.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            m.gravityModifier = -0.04f;
            var c = Color.Lerp(tint, RenderSettings.fogColor, 0.3f);
            Colour(ps, new[] { (new Color(c.r, c.g, c.b, 0f), 0f), (new Color(c.r, c.g, c.b, 0.7f), 0.15f), (new Color(c.r, c.g, c.b, 0.45f), 0.6f), (new Color(c.r, c.g, c.b, 0f), 1f) }, 1f, false);
            var em = ps.emission; em.rateOverTime = 3.5f * Mathf.Sqrt(scale) * Mult;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = scale;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.5f, 1, 3f));
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
            var wind = ps.forceOverLifetime; wind.enabled = true; wind.x = new ParticleSystem.MinMaxCurve(0.5f); wind.y = new ParticleSystem.MinMaxCurve(0f); wind.z = new ParticleSystem.MinMaxCurve(0f);
            Glow(ps, new Color(2.8f, 2.8f, 2.8f, 1f));
            ps.Play();
            root.AddComponent<EmitFor>().seconds = seconds;
            return root;
        }

        /// <summary>A fire that keeps burning on a wreck for a while.</summary>
        public static GameObject Fire(Vector3 pos, float scale, BlastStyle style, float seconds)
        {
            var look = LookOf(style);
            var root = new GameObject("Fire");
            root.transform.position = pos;
            var f = System(root, "Flames", "flame", true, 0);
            var m = f.main;
            m.loop = true; m.duration = Mathf.Max(1f, seconds);
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(0.8f * scale, 2f * scale);
            m.startSize = new ParticleSystem.MinMaxCurve(0.5f * scale, 1.1f * scale);
            m.gravityModifier = -0.3f;
            var sh = f.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.4f * scale;
            Colour(f, new[] { (look.core * 2.5f, 0f), (look.flame * 1.8f, 0.35f), (look.flame * 0.4f, 1f) }, 0.6f);
            var em = f.emission; em.rateOverTime = 26f * scale * Mult;
            var sz = f.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1f, 1, 0.3f));
            f.Play();
            var smoke = SmokeColumn(pos + Vector3.up * 1.2f, 0.6f * scale, seconds, look.smoke);
            smoke.transform.SetParent(root.transform, true);
            var lg = new GameObject("Glow");
            lg.transform.SetParent(root.transform, false);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point; l.color = look.light; l.range = 7f * scale; l.intensity = 3f; l.shadows = LightShadows.None;
            lg.AddComponent<FireFlicker>();
            root.AddComponent<EmitFor>().seconds = seconds;
            return root;
        }

        /// <summary>A chain of blasts (a building's charges going off, a boss blowing apart).</summary>
        public static IEnumerator Chain(Vector3 center, Vector3 spread, int count, float interval, float scale, BlastStyle style)
        {
            for (int i = 0; i < count; i++)
            {
                At(center + Vector3.Scale(Random.insideUnitSphere, spread), scale * Random.Range(0.7f, 1.2f), style, i % 2 == 0, false, i % 2 == 1);
                yield return new WaitForSeconds(interval * Random.Range(0.6f, 1.4f));
            }
        }

        // ------------------------------------------------------------------ pieces
        static ParticleSystem System(GameObject root, string name, string tex, bool additive, int burst)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false; main.loop = false; main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;
            var em = ps.emission; em.rateOverTime = 0;
            if (burst > 0) em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burst) });
            var sh = ps.shape; sh.enabled = true;
            var r = go.GetComponent<ParticleSystemRenderer>();
            var mat = MaterialCache.Instance(additive ? "padd" : "palpha");
            var lib = Services.Db != null ? Services.Db.materials : null;
            var t = lib != null ? lib.Particle(tex) : null;
            if (t != null) mat.SetTexture("_BaseMap", t);
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sortingFudge = additive ? -20 : 0;
            if (burst > 0) ps.Play();
            return ps;
        }

        static void Colour(ParticleSystem ps, (Color c, float t)[] keys, float alphaEnd, bool fadeAlpha = true)
        {
            // particle colours are 8-bit: the overall glow (HDR, for bloom) goes on the material, the ramp in the gradient
            float hdr = 1f;
            foreach (var k in keys) hdr = Mathf.Max(hdr, Mathf.Max(k.c.r, Mathf.Max(k.c.g, k.c.b)));
            var g = new Gradient();
            var ck = new GradientColorKey[keys.Length];
            var ak = new GradientAlphaKey[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                var c = keys[i].c;
                ck[i] = new GradientColorKey(new Color(c.r / hdr, c.g / hdr, c.b / hdr), keys[i].t);
                ak[i] = new GradientAlphaKey(fadeAlpha ? (i == keys.Length - 1 ? 0f : (i == 0 ? 1f : Mathf.Lerp(1f, alphaEnd, keys[i].t))) : c.a, keys[i].t);
            }
            g.SetKeys(ck, ak);
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(g);
            Glow(ps, new Color(hdr, hdr, hdr, 1f));
        }

        static void Glow(ParticleSystem ps, Color c)
        {
            var r = ps.GetComponent<ParticleSystemRenderer>();
            if (r != null && r.sharedMaterial != null) r.sharedMaterial.SetColor("_BaseColor", c);
        }

        static Mesh cube;
        static Material debrisMat;

        /// <summary>Chunks of concrete / metal: lit 3D meshes that tumble, bounce and settle.</summary>
        static void Debris(GameObject root, float scale, BlastStyle style)
        {
            if (cube == null) { var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube); cube = tmp.GetComponent<MeshFilter>().sharedMesh; Object.Destroy(tmp); }
            if (debrisMat == null) debrisMat = CityMaterials.PBR("Concrete046", new Color(0.55f, 0.53f, 0.5f), 1f);
            var go = new GameObject("Debris");
            go.transform.SetParent(root.transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main;
            m.playOnAwake = false; m.loop = false; m.duration = 1f;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 5f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(4f * Mathf.Sqrt(scale), 13f * Mathf.Sqrt(scale));
            m.startSize3D = true;
            m.startSizeX = new ParticleSystem.MinMaxCurve(0.12f * scale, 0.45f * scale);
            m.startSizeY = new ParticleSystem.MinMaxCurve(0.08f * scale, 0.3f * scale);
            m.startSizeZ = new ParticleSystem.MinMaxCurve(0.12f * scale, 0.4f * scale);
            m.startRotation3D = true;
            m.startRotationX = m.startRotationY = m.startRotationZ = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            m.gravityModifier = 1.6f;
            m.startColor = Color.white;
            var em = ps.emission; em.rateOverTime = 0; em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)N(16 * Mathf.Sqrt(scale))) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Hemisphere; sh.radius = 0.3f * scale; sh.rotation = new Vector3(-90, 0, 0);
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.separateAxes = true;
            rot.x = rot.y = rot.z = new ParticleSystem.MinMaxCurve(-8f, 8f);
            var col = ps.collision; col.enabled = true; col.type = ParticleSystemCollisionType.World; col.mode = ParticleSystemCollisionMode.Collision3D;
            col.bounce = 0.3f; col.dampen = 0.45f; col.lifetimeLoss = 0f; col.quality = ParticleSystemCollisionQuality.Medium; col.radiusScale = 0.6f;
            col.enableDynamicColliders = false;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 1), new Keyframe(0.85f, 1), new Keyframe(1, 0)));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Mesh;
            r.mesh = cube;
            r.sharedMaterial = debrisMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            r.alignment = ParticleSystemRenderSpace.World;
            ps.Play();
        }

        static void Ring(GameObject root, Color c, float scale)
        {
            var ps = System(root, "Shockwave", "circle", true, 1);
            var m = ps.main;
            m.startLifetime = 0.4f; m.startSpeed = 0f; m.startSize = 0.8f * scale;
            var sh = ps.shape; sh.enabled = false;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(10f, AnimationCurve.EaseInOut(0, 0.1f, 1, 1));
            Colour(ps, new[] { (Color.white, 0f), (new Color(0.5f, 0.5f, 0.5f), 0.5f), (new Color(0.2f, 0.2f, 0.2f), 1f) }, 0f);
            Glow(ps, c * 1.5f);
        }

        /// <summary>A burnt patch on the ground under the blast (stays a while).</summary>
        static void Scorch(Vector3 pos, float scale)
        {
            if (!Physics.Raycast(pos + Vector3.up, Vector3.down, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore)) return;
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.name = "Scorch";
            q.transform.position = hit.point + hit.normal * 0.02f;
            q.transform.rotation = Quaternion.LookRotation(-hit.normal) * Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            q.transform.localScale = Vector3.one * 3.5f * scale;
            var mat = MaterialCache.Instance("palpha");
            var lib = Services.Db != null ? Services.Db.materials : null;
            var t = lib != null ? lib.Particle("scorch") : null;
            if (t != null) mat.SetTexture("_BaseMap", t);
            mat.SetColor("_BaseColor", new Color(0.05f, 0.04f, 0.04f, 0.85f));
            q.GetComponent<Renderer>().sharedMaterial = mat;
            q.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Object.Destroy(q, 40f);
        }
    }

    /// <summary>Stops a looping effect's emission after a while, lets the last particles fade, then cleans up.</summary>
    public class EmitFor : MonoBehaviour
    {
        public float seconds = 10f;
        float t;
        void Update()
        {
            t += Time.deltaTime;
            if (t < seconds) return;
            foreach (var ps in GetComponentsInChildren<ParticleSystem>()) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            foreach (var l in GetComponentsInChildren<Light>()) l.enabled = false;
            Destroy(gameObject, 12f);
            enabled = false;
        }
    }

    /// <summary>Firelight that flickers.</summary>
    public class FireFlicker : MonoBehaviour
    {
        Light l; float baseI, seed;
        void Start() { l = GetComponent<Light>(); baseI = l.intensity; seed = Random.value * 10f; }
        void Update() { if (l != null) l.intensity = baseI * (0.7f + Mathf.PerlinNoise(Time.time * 7f, seed) * 0.6f); }
    }

    /// <summary>The explosion's light: snaps on, then fades with a flicker.</summary>
    public class BlastFlash : MonoBehaviour
    {
        Light l; float peak, dur, t;
        public void Init(Light light, float intensity, float duration) { l = light; peak = intensity; dur = duration; }
        void Update()
        {
            if (l == null) return;
            t += Time.deltaTime;
            float k = t < 0.04f ? t / 0.04f : Mathf.Clamp01(1f - (t - 0.04f) / dur);
            l.intensity = peak * k * k * (0.85f + Random.value * 0.3f);
            if (t > dur + 0.1f) { Destroy(l.gameObject); enabled = false; }
        }
    }
}
