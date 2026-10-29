using System.Collections;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// "The background briefly swaps region" glitch: for 3-6 frames the whole background takes another region's
    /// palette and props. Triggered by corruption spikes; runs constantly in The Core.
    /// </summary>
    public class RegionFlash : MonoBehaviour
    {
        [Tooltip("Groups of props from other regions, hidden normally and shown during a flash.")]
        public GameObject[] altProps;
        public string[] altThemes = { "CodeCity", "PhysicsValley", "ChemicalChaos", "BioZone" };
        public bool constant;          // The Core
        public float constantInterval = 4f;
        float timer;
        bool flashing;

        void OnEnable() { CorruptionManager.Spike += OnSpike; timer = constantInterval; }
        void OnDisable() => CorruptionManager.Spike -= OnSpike;
        void Start() { foreach (var g in altProps) if (g != null) g.SetActive(false); }

        void OnSpike(float k) { if (!flashing && k > 0.3f && Services.Corruption.Value > 0.3f) StartCoroutine(Flash(Random.Range(3, 7))); }

        void Update()
        {
            if (!constant || flashing || Services.Corruption.Value < 0.2f) return;
            timer -= Time.deltaTime;
            if (timer <= 0) { timer = Random.Range(constantInterval * 0.5f, constantInterval * 1.5f); StartCoroutine(Flash(Random.Range(3, 7))); }
        }

        IEnumerator Flash(int frames)
        {
            flashing = true;
            var theme = ContentDefaults.CreateTheme(altThemes[Random.Range(0, altThemes.Length)]);
            var fog = RenderSettings.fogColor;
            var sky = RenderSettings.ambientSkyColor;
            var eq = RenderSettings.ambientEquatorColor;
            var accent = Shader.GetGlobalColor("_RegionAccent");
            GameObject alt = altProps != null && altProps.Length > 0 ? altProps[Random.Range(0, altProps.Length)] : null;
            RenderSettings.fogColor = theme.fogCorrupted;
            RenderSettings.ambientSkyColor = theme.ambientSkyCorrupted;
            RenderSettings.ambientEquatorColor = theme.ambientEquatorCorrupted;
            Shader.SetGlobalColor("_RegionAccent", theme.accentCorrupted);
            if (alt != null) alt.SetActive(true);
            GlitchFX.Pulse(0.35f, 0.15f);
            for (int i = 0; i < frames; i++) yield return null;
            if (alt != null) alt.SetActive(false);
            RenderSettings.fogColor = fog;
            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = eq;
            Shader.SetGlobalColor("_RegionAccent", accent);
            Object.Destroy(theme);
            flashing = false;
        }
    }
}
