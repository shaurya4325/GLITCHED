using UnityEngine;

namespace Glitched
{
    /// <summary>Region ambience around the camera: data motes, dust, bubbles, blood cells or sparks.</summary>
    public class AmbientParticles : MonoBehaviour
    {
        public string style = "datamotes";
        public int maxParticles = 160;
        ParticleSystem ps;

        void Start()
        {
            ps = gameObject.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.Max(10, Mathf.RoundToInt(maxParticles * Services.Graphics.ParticleMultiplier));
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(60f, 30f, 30f);
            var em = ps.emission;
            em.rateOverTime = main.maxParticles / 7f;
            var noise = ps.noise;
            noise.enabled = true; noise.strength = 0.4f; noise.frequency = 0.3f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(1, 0.8f), new GradientAlphaKey(0, 1) });
            col.color = g;
            string tex = "spark";
            bool additive = true;
            switch (style)
            {
                case "dust":
                    main.startColor = new Color(1f, 0.8f, 0.6f, 0.5f); main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
                    main.startSpeed = 0.2f; main.gravityModifier = -0.01f; tex = "circle"; break;
                case "bubbles":
                    main.startColor = new Color(0.6f, 1f, 0.7f, 0.6f); main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
                    main.startSpeed = 0.1f; main.gravityModifier = -0.04f; tex = "circle"; break;
                case "cells":
                    main.startColor = new Color(1f, 0.3f, 0.35f, 0.7f); main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
                    main.startSpeed = 0.6f; main.gravityModifier = 0; tex = "circle"; additive = false; break;
                case "sparks":
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.3f, 0.9f), new Color(0.3f, 1f, 1f)); main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
                    main.startSpeed = 0.5f; tex = "spark"; break;
                default:
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.3f, 1f, 1f, 0.8f), new Color(1f, 0.3f, 0.9f, 0.8f)); main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
                    main.startSpeed = 0.15f; main.gravityModifier = -0.02f; tex = "square"; break;
            }
            var r = GetComponent<ParticleSystemRenderer>();
            var mat = MaterialCache.Instance(additive ? "padd" : "palpha");
            var lib = Services.Db.materials;
            var t = lib != null ? lib.Particle(tex) : null;
            if (t != null) mat.SetTexture("_BaseMap", t);
            r.sharedMaterial = mat;
            ps.Play();
        }

        void LateUpdate()
        {
            // follow the camera so the ambience is always in view (on the gameplay side of the depth layers)
            if (CameraRig.Instance != null)
            {
                var c = CameraRig.Instance.Cam.transform.position;
                transform.position = new Vector3(c.x, c.y, 12f);
            }
        }
    }
}
