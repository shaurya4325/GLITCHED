using System.Collections;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// A backdrop building the Glitch demolishes when the player passes triggerX: charges go off up its facade, then it
    /// drops straight down into a rolling dust cloud (sinking below the street, like a controlled demolition),
    /// leaving a smoking gap in the skyline.
    /// </summary>
    public class Demolition : MonoBehaviour
    {
        public float triggerX;
        public BlastStyle style;
        public string line = "";
        bool done;
        Bounds b;

        void Start()
        {
            var rs = GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) { enabled = false; return; }
            b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
        }

        void Update()
        {
            if (done) return;
            var p = PlayerController.Instance;
            if (p == null || p.transform.position.x < triggerX || p.Locked) return;   // not while reading a lesson or talking: it should be seen
            done = true;
            StartCoroutine(Collapse());
        }

        IEnumerator Collapse()
        {
            float H = b.size.y, front = b.min.z;
            // the charges, floor by floor, on the side facing the camera
            for (int i = 0; i < 6; i++)
            {
                var at = new Vector3(Random.Range(b.min.x + 2f, b.max.x - 2f), Mathf.Lerp(b.min.y + 3f, b.min.y + H * 0.75f, i / 5f), front - 0.5f);
                Blast.At(at, Mathf.Clamp(b.size.x * 0.12f, 2f, 4f), style, true, false, i % 2 == 1);
                yield return new WaitForSeconds(0.16f);
            }
            Blast.At(new Vector3(b.center.x, b.min.y + 2f, front - 1f), Mathf.Clamp(b.size.x * 0.2f, 3f, 6f), style);
            if (!string.IsNullOrEmpty(line)) Companion.Say(line, 3.5f);
            StartCoroutine(PullBack());
            Services.Audio.Play("thunder", 0.8f);
            var dust = Dust();
            Vector3 start = transform.position;
            Quaternion r0 = transform.rotation;
            float tilt = Random.value < 0.5f ? -1f : 1f, shakeT = 0f;
            const float dur = 3.4f;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float k = t / dur, e = k * k;
                transform.position = start + Vector3.down * (H * 1.08f * e) + new Vector3(Mathf.Sin(t * 47f) * 0.15f, 0f, Mathf.Sin(t * 31f) * 0.1f);
                transform.rotation = r0 * Quaternion.Euler(3f * k, 0f, tilt * 7f * k);
                if ((shakeT -= Time.deltaTime) <= 0f)
                {
                    shakeT = 0.35f;
                    CameraRig.Instance?.Shake(0.35f);
                    if (Random.value < 0.5f) Blast.At(new Vector3(Random.Range(b.min.x, b.max.x), b.min.y + Random.Range(1f, 6f), front - 0.5f), 2f, style, false, false, true);
                }
                yield return null;
            }
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            Blast.SmokeColumn(new Vector3(b.center.x, b.min.y, b.center.z), Mathf.Max(2f, b.size.x * 0.12f), 25f, new Color(0.42f, 0.4f, 0.37f));
            yield return new WaitForSeconds(8f);
            if (dust != null) Destroy(dust, 10f);
        }

        /// <summary>The camera eases back a little so the whole collapse fits in the frame, then returns.</summary>
        IEnumerator PullBack()
        {
            var rig = CameraRig.Instance;
            if (rig == null || rig.ExtraDistance > 0.01f) yield break;   // a boss fight already owns the framing
            float set = 0f;
            bool Mine() => rig != null && Mathf.Abs(rig.ExtraDistance - set) < 0.01f;   // someone else (a boss fight) took over: let go
            for (float t = 0f; t < 1f; t += Time.deltaTime) { if (!Mine()) yield break; rig.SetExtraDistance(set = Mathf.SmoothStep(0f, 7f, t)); yield return null; }
            yield return new WaitForSeconds(3f);
            for (float t = 0f; t < 1.5f; t += Time.deltaTime) { if (!Mine()) yield break; rig.SetExtraDistance(set = Mathf.SmoothStep(7f, 0f, t / 1.5f)); yield return null; }
            if (Mine()) rig.SetExtraDistance(0f);
        }

        /// <summary>The pyroclastic-looking dust that rolls out from the base as it falls.</summary>
        GameObject Dust()
        {
            var go = new GameObject("DemolitionDust");
            go.transform.position = new Vector3(b.center.x, b.min.y + 1f, b.min.z);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main;
            m.duration = 3.4f; m.loop = false; m.playOnAwake = false;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(3f, 9f);
            m.startSize = new ParticleSystem.MinMaxCurve(b.size.x * 0.25f, b.size.x * 0.55f);
            m.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            m.maxParticles = 300;
            var fog = RenderSettings.fogColor;
            var c = Color.Lerp(new Color(0.8f, 0.74f, 0.66f), fog, 0.25f);   // light concrete dust
            m.startColor = new Color(c.r, c.g, c.b, 0.9f);
            var em = ps.emission; em.rateOverTime = 40f * Mathf.Max(0.4f, Services.Graphics != null ? Services.Graphics.ParticleMultiplier : 1f);
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(b.size.x, 2f, b.size.z);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.radial = new ParticleSystem.MinMaxCurve(1.5f, 4f); vel.y = new ParticleSystem.MinMaxCurve(0.6f, 2.2f);
            vel.x = new ParticleSystem.MinMaxCurve(0f, 0f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);   // all three axes in the same mode
            var lim = ps.limitVelocityOverLifetime; lim.enabled = true; lim.dampen = 0.15f; lim.limit = 2f;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.5f, 1, 1.8f));
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(0.85f, 0.85f, 0.85f), 1) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.1f), new GradientAlphaKey(0.6f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var r = go.GetComponent<ParticleSystemRenderer>();
            var mat = MaterialCache.Instance("palpha");
            var t = Services.Db.materials != null ? Services.Db.materials.Particle("smoke") : null;
            if (t != null) mat.SetTexture("_BaseMap", t);
            mat.SetColor("_BaseColor", new Color(3.6f, 3.6f, 3.6f, 1f));   // the smoke texture is mid-grey: lift it to real dust
            r.sharedMaterial = mat;
            ps.Play();
            return go;
        }
    }
}
