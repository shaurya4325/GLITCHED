using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Pooled particle effects built in code with the Kenney particle textures: bursts, dust, hit sparks,
    /// critical explosions, dash trails, confetti and shockwave rings. Counts follow the graphics tier.
    /// </summary>
    public static class VFX
    {
        static ParticleSystem Make(string key, string tex, bool additive)
        {
            var go = new GameObject("VFX_" + key);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 200;
            var em = ps.emission; em.rateOverTime = 0;
            var r = go.GetComponent<ParticleSystemRenderer>();
            var mat = MaterialCache.Instance(additive ? "padd" : "palpha");
            var lib = Services.Db.materials;
            var t = lib != null ? lib.Particle(tex) : null;
            if (t != null) mat.SetTexture("_BaseMap", t);
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sortingFudge = -10;
            return ps;
        }

        static ParticleSystem Get(string key, string tex, bool additive, Vector3 pos)
        {
            var go = Pool.Get("vfx_" + key + tex, () => Make(key, tex, additive).gameObject, pos, Quaternion.identity);
            return go.GetComponent<ParticleSystem>();
        }

        static int Count(int n) => Mathf.Max(1, Mathf.RoundToInt(n * (Services.Graphics != null ? Services.Graphics.ParticleMultiplier : 1f)));

        static void Emit(ParticleSystem ps, int n, float life)
        {
            ps.Emit(Count(n));
            Pool.ReleaseAfter(ps.gameObject, life + 0.2f);
        }

        public static void Burst(Vector3 pos, Color color, int count = 20, string tex = "spark", float speed = 6f, float size = 0.25f, float life = 0.6f)
        {
            var ps = Get("burst", tex, true, pos);
            var m = ps.main;
            m.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.5f));
            m.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            m.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            m.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            m.gravityModifier = 0.6f;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.1f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(Fade());
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));
            Emit(ps, count, life);
        }

        public static void Dust(Vector3 pos)
        {
            var ps = Get("dust", "smoke", false, pos + Vector3.up * 0.05f);
            var m = ps.main;
            m.startColor = new Color(0.9f, 0.9f, 1f, 0.35f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.6f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            m.startLifetime = 0.45f;
            m.gravityModifier = -0.05f;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.25f; sh.rotation = new Vector3(90, 0, 0);
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Fade());
            Emit(ps, 6, 0.5f);
        }

        /// <summary>Correct answer hit: sparks + flash ring.</summary>
        public static void Hit(Vector3 pos, Color color, bool big)
        {
            Burst(pos, color, big ? 60 : 26, "spark", big ? 12f : 7f, big ? 0.45f : 0.3f, big ? 0.9f : 0.6f);
            Burst(pos, Color.white, big ? 14 : 6, "flare", 1f, big ? 1.8f : 1.0f, 0.25f);
            Ring(pos, color, big ? 4f : 2.2f);
            if (big) Burst(pos, UIFactory.Theme.magenta, 30, "star", 9f, 0.35f, 1f);
        }

        public static void Ring(Vector3 pos, Color color, float radius)
        {
            var ps = Get("ring", "circle", true, pos);
            var m = ps.main;
            m.startColor = color;
            m.startSpeed = 0;
            m.startSize = radius * 0.2f;
            m.startLifetime = 0.35f;
            m.gravityModifier = 0;
            var sh = ps.shape; sh.enabled = false;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(radius * 5f, AnimationCurve.EaseInOut(0, 0.2f, 1, 1));
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Fade());
            ps.Emit(1);
            Pool.ReleaseAfter(ps.gameObject, 0.5f);
        }

        public static void Explosion(Vector3 pos, Color color, float scale = 1f)
        {
            Burst(pos, color, 80, "spark", 14f * scale, 0.5f * scale, 1.1f);
            Burst(pos, new Color(1f, 0.6f, 0.2f), 30, "fire", 5f * scale, 1.2f * scale, 0.8f);
            Burst(pos, new Color(0.3f, 0.3f, 0.35f, 0.6f), 20, "smoke", 3f * scale, 1.8f * scale, 1.4f);
            Ring(pos, Color.white, 6f * scale);
        }

        public static void Confetti(Vector3 pos)
        {
            var ps = Get("confetti", "square", false, pos);
            var m = ps.main;
            m.startColor = new ParticleSystem.MinMaxGradient(new Gradient
            {
                colorKeys = new[] { new GradientColorKey(UIFactory.Theme.cyan, 0), new GradientColorKey(UIFactory.Theme.magenta, 0.33f), new GradientColorKey(UIFactory.Theme.yellow, 0.66f), new GradientColorKey(UIFactory.Theme.green, 1) },
                alphaKeys = new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) }
            }) { mode = ParticleSystemGradientMode.RandomColor };
            m.startSpeed = new ParticleSystem.MinMaxCurve(5f, 12f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            m.startLifetime = 2.2f;
            m.gravityModifier = 0.8f;
            m.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 35; sh.rotation = new Vector3(-90, 0, 0);
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-6, 6);
            Emit(ps, 120, 2.4f);
        }

        /// <summary>A trail of afterimages following a transform (dash).</summary>
        public static void Trail(Transform t, Color color, float seconds)
        {
            int n = Mathf.Max(3, Mathf.RoundToInt(seconds / 0.03f));
            for (int i = 0; i < n; i++)
            {
                float delay = i * 0.03f;
                Tween.After(delay, () => { if (t) Burst(t.position + Vector3.up * 0.8f, color, 3, "spark", 0.5f, 0.6f, 0.25f); }, false);
            }
        }

        /// <summary>Glitchy pixel debris when something "stops existing".</summary>
        public static void GlitchApart(Vector3 pos, float size)
        {
            Burst(pos, UIFactory.Theme.magenta, 40, "square", 6f * size, 0.25f * size, 1.0f);
            Burst(pos, UIFactory.Theme.cyan, 40, "square", 6f * size, 0.25f * size, 1.0f);
            Burst(pos, Color.white, 10, "flare", 2f, 1.5f * size, 0.3f);
        }

        static Gradient fade;
        static Gradient Fade()
        {
            if (fade != null) return fade;
            fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                         new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 0.6f), new GradientAlphaKey(0, 1) });
            return fade;
        }
    }
}
