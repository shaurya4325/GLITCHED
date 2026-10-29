using UnityEngine;
using UnityEngine.Rendering;

namespace Glitched
{
    /// <summary>
    /// The single global Corruption value (0 = healthy, 1 = fully corrupted, Section 3.3).
    /// Sets the _Corruption shader global and blends fog, ambient light, the sun and the sky between the
    /// region theme's corrupted and restored palettes. Also fires random "corruption spikes".
    /// </summary>
    public class CorruptionManager : MonoBehaviour
    {
        public float Value { get; private set; } = 1f;
        public RegionThemeSO Theme { get; private set; }
        public Light Sun { get; private set; }
        public static event System.Action<float> Spike;    // intensity

        Material skyMat;
        TweenHandle tween;
        float spikeTimer = 8f;
        static readonly int IdCorruption = Shader.PropertyToID("_Corruption");
        static readonly int IdAccent = Shader.PropertyToID("_RegionAccent");
        static readonly int IdAccent2 = Shader.PropertyToID("_RegionAccent2");
        static readonly int IdHeightFogColor = Shader.PropertyToID("_HeightFogColor");
        static readonly int IdHeightFogParams = Shader.PropertyToID("_HeightFogParams");
        static readonly int IdWaveActive = Shader.PropertyToID("_RestoreWaveActive");

        void Awake()
        {
            Shader.SetGlobalFloat(IdWaveActive, 0f);
            Shader.SetGlobalFloat("_RestoreWaveWidth", 12f);
            Shader.SetGlobalVector("_PlayerScreenPos", new Vector4(0.5f, 0.5f, 0.1f, 0f));
        }

        /// <summary>Called by each level when it loads.</summary>
        public void SetupRegion(RegionThemeSO theme, Light sun, float startValue)
        {
            Theme = theme;
            Sun = sun;
            if (Sun != null) RenderSettings.sun = Sun;
            var lib = Services.Db.materials;
            if (lib != null && lib.sky != null)
            {
                skyMat = new Material(lib.sky);
                if (theme != null)
                {
                    if (theme.skyCorrupted != null) skyMat.SetTexture("_SkyCorrupted", theme.skyCorrupted);
                    if (theme.skyRestored != null) skyMat.SetTexture("_SkyRestored", theme.skyRestored);
                    skyMat.SetFloat("_UseTextures", theme.skyCorrupted != null || theme.skyRestored != null ? 1f : 0f);
                    skyMat.SetColor("_TintCorrupted", theme.skyTintCorrupted);
                    skyMat.SetColor("_TintRestored", theme.skyTintRestored);
                    skyMat.SetFloat("_Exposure", theme.skyExposure);
                    skyMat.SetColor("_GradientTop", theme.ambientSkyCorrupted);
                    skyMat.SetColor("_GradientBottom", theme.fogCorrupted);
                }
                bool aurora = Services.Graphics.LayerActive(DepthLayer.L5_SkyEvents);
                skyMat.SetFloat("_Aurora", aurora ? 1f : 0f);
                RenderSettings.skybox = skyMat;
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = true;
            var tier = Services.Graphics.Current;
            RenderSettings.fogMode = tier != null ? tier.fogMode : FogMode.ExponentialSquared;
            Set(startValue);
        }

        public void Set(float v)
        {
            tween?.Kill();
            Apply(Mathf.Clamp01(v));
        }

        public TweenHandle TweenTo(float v, float seconds, Ease ease = Ease.InOutSine)
        {
            tween?.Kill();
            float from = Value;
            tween = Tween.Value(from, Mathf.Clamp01(v), seconds, Apply, ease, false, this);
            return tween;
        }

        void Apply(float v)
        {
            Value = v;
            Shader.SetGlobalFloat(IdCorruption, v);
            var t = Theme;
            if (t != null)
            {
                float h = 1f - v;
                var fog = Color.Lerp(t.fogCorrupted, t.fogRestored, h);
                RenderSettings.fogColor = fog;
                RenderSettings.fogStartDistance = t.fogStart;
                RenderSettings.fogEndDistance = t.fogEnd;
                RenderSettings.fogDensity = Mathf.Lerp(t.fogDensity * 1.4f, t.fogDensity, h);
                RenderSettings.ambientSkyColor = Color.Lerp(t.ambientSkyCorrupted, t.ambientSkyRestored, h);
                RenderSettings.ambientEquatorColor = Color.Lerp(t.ambientEquatorCorrupted, t.ambientEquatorRestored, h);
                RenderSettings.ambientGroundColor = Color.Lerp(t.ambientGroundCorrupted, t.ambientGroundRestored, h);
                if (Sun != null)
                {
                    Sun.color = Color.Lerp(t.sunCorrupted, t.sunRestored, h);
                    Sun.intensity = Mathf.Lerp(t.sunIntensityCorrupted, t.sunIntensityRestored, h);
                }
                var accent = Color.Lerp(t.accentCorrupted, t.accentRestored, h);
                Shader.SetGlobalColor(IdAccent, accent);
                Shader.SetGlobalColor(IdAccent2, Color.Lerp(new Color(0.25f, 0.92f, 1f), Color.white * 0.8f, h * 0.5f));
                Shader.SetGlobalColor(IdHeightFogColor, fog);
                Shader.SetGlobalVector(IdHeightFogParams, new Vector4(-4f, 0.12f, 0.55f, 0));
                if (skyMat != null) skyMat.SetFloat("_HorizonHaze", Mathf.Lerp(0.16f, 0.08f, h));
                if (skyMat != null) skyMat.SetColor("_HorizonColor", fog);
            }
            else
            {
                Shader.SetGlobalColor(IdAccent, new Color(1f, 0.24f, 0.78f));
                Shader.SetGlobalColor(IdAccent2, new Color(0.25f, 0.92f, 1f));
            }
            GameEvents.RaiseCorruption(v);
        }

        void Update()
        {
            if (Value < 0.25f) return;
            spikeTimer -= Time.deltaTime;
            if (spikeTimer <= 0)
            {
                spikeTimer = Random.Range(7f, 16f) / Mathf.Max(0.4f, Value);
                float k = Random.Range(0.4f, 1f) * Value;
                Spike?.Invoke(k);
                Services.Audio.BitCrushBurst(0.5f);
                GlitchFX.Pulse(0.25f * k, 0.25f);
            }
        }
    }
}
