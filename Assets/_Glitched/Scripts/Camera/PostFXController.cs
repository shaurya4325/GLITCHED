using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Glitched
{
    /// <summary>
    /// Builds the global post-processing Volume for the current tier (colour grading, bloom, vignette,
    /// chromatic aberration, depth of field) and reacts to corruption and impacts.
    /// </summary>
    public class PostFXController : MonoBehaviour
    {
        Volume volume;
        VolumeProfile profile;
        ColorAdjustments color;
        Bloom bloom;
        Vignette vignette;
        ChromaticAberration chroma;
        DepthOfField dof;
        LensDistortion lens;
        Tonemapping tonemap;
        WhiteBalance white;
        SplitToning split;
        FilmGrain grain;
        float impact;

        /// <summary>Camera-to-subject distance of the current cutscene shot (metres), or below 0 in gameplay:
        /// close-ups then focus on the speaker with a real lens (bokeh), so the background falls away.</summary>
        public static float CutsceneFocus = -1f;
        float focus = 10f;

        /// <summary>Each world's film look: white balance, split toning (shadow / highlight tint) and its saturation.</summary>
        struct Grade { public float temp, tint, sat, contrast, balance, exposure; public Color shadows, highlights; }
        static Grade GradeFor(string region)
        {
            switch (region)
            {
                case "PhysicsValley": return new Grade { temp = 8, tint = 2, sat = 22, contrast = 10, balance = 10, exposure = 0.25f, shadows = new Color(0.45f, 0.55f, 0.75f), highlights = new Color(1f, 0.82f, 0.6f) };    // golden valley
                case "ChemicalChaos": return new Grade { temp = -2, tint = -6, sat = 18, contrast = 14, balance = -10, shadows = new Color(0.4f, 0.6f, 0.5f), highlights = new Color(0.95f, 0.95f, 0.7f) };  // industrial green-yellow
                case "BioZone": return new Grade { temp = 2, tint = -8, sat = 24, contrast = 18, balance = 0, exposure = -0.1f, shadows = new Color(0.35f, 0.55f, 0.55f), highlights = new Color(0.9f, 1f, 0.75f) };            // lush
                case "TheCore": return new Grade { temp = -10, tint = 10, sat = 22, contrast = 16, balance = -15, shadows = new Color(0.3f, 0.4f, 0.8f), highlights = new Color(1f, 0.7f, 0.95f) };           // cyan-magenta
                case "ExamHall": return new Grade { temp = 0, tint = 3, sat = 14, contrast = 14, balance = 15, exposure = -0.05f, shadows = new Color(0.45f, 0.42f, 0.5f), highlights = new Color(1f, 0.8f, 0.58f) };          // dusty afternoon
                default: return new Grade { temp = 2, tint = 0, sat = 22, contrast = 12, balance = -5, shadows = new Color(0.38f, 0.5f, 0.68f), highlights = new Color(1f, 0.86f, 0.7f) };                 // Code City: teal and orange
            }
        }
        public static PostFXController Instance { get; private set; }

        /// <summary>0..1: freeze-frame look for character intro cards (desaturated, punchy, vignetted).</summary>
        public static float Freeze;

        void Awake()
        {
            Instance = this;
            var go = new GameObject("PostFX Volume");
            go.transform.SetParent(transform, false);
            volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10;
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            color = profile.Add<ColorAdjustments>(true);
            bloom = profile.Add<Bloom>(true);
            vignette = profile.Add<Vignette>(true);
            chroma = profile.Add<ChromaticAberration>(true);
            dof = profile.Add<DepthOfField>(true);
            lens = profile.Add<LensDistortion>(true);
            tonemap = profile.Add<Tonemapping>(true);
            white = profile.Add<WhiteBalance>(true);
            split = profile.Add<SplitToning>(true);
            grain = profile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Thin1);
            grain.intensity.Override(0.12f);
            grain.response.Override(0.85f);
            volume.sharedProfile = profile;

            tonemap.mode.Override(TonemappingMode.ACES);   // filmic, realistic highlight roll-off
            bloom.threshold.Override(0.9f);   // emissives, the sun on glass and Kanha's aura glow; the lit world does not
            bloom.intensity.Override(0.55f);
            bloom.scatter.Override(0.68f);
            vignette.intensity.Override(0.18f);
            vignette.smoothness.Override(0.45f);
            dof.mode.Override(DepthOfFieldMode.Gaussian);
            dof.gaussianStart.Override(140f);
            dof.gaussianEnd.Override(360f);
            dof.gaussianMaxRadius.Override(1.2f);
            lens.intensity.Override(0f);
            ApplyTier();
            GameEvents.TierChanged += OnTier;
            GameEvents.SettingsChanged += ApplyTier;
        }

        void OnDestroy()
        {
            GameEvents.TierChanged -= OnTier;
            GameEvents.SettingsChanged -= ApplyTier;
            if (profile != null) Destroy(profile);
            if (Instance == this) Instance = null;
        }

        void OnTier(GraphicsTier t) => ApplyTier();

        void ApplyTier()
        {
            var t = Services.Graphics.Current;
            bool post = Services.Graphics.PostEnabled;
            volume.enabled = post;
            if (t == null) return;
            color.active = t.colorGrading || t.tier >= GraphicsTier.Medium;
            bloom.active = t.bloom;
            bloom.highQualityFiltering.Override(t.tier >= GraphicsTier.Ultra);
            vignette.active = t.vignette;
            chroma.active = t.chromaticAberration;
            dof.active = t.depthOfField;
            lens.active = t.tier >= GraphicsTier.High;
            tonemap.active = t.tier >= GraphicsTier.Medium;
            white.active = split.active = t.colorGrading || t.tier >= GraphicsTier.Medium;
            grain.active = t.tier >= GraphicsTier.Ultra;
            bloom.intensity.Override(t.tier >= GraphicsTier.High ? 0.7f : 0.5f);
        }

        /// <summary>Impact kick: chromatic aberration + lens distortion pulse.</summary>
        public static void Kick(float strength)
        {
            if (Instance == null) return;
            if (Services.Settings.Data.reduceFlashing) strength *= 0.35f;
            Instance.impact = Mathf.Max(Instance.impact, strength);
        }

        void Update()
        {
            float c = Services.Corruption.Value;
            // this world's grade, drained toward a cold, flat look while it is corrupted
            var g = GradeFor(Services.Corruption.Theme != null ? Services.Corruption.Theme.id : "");
            // split toning works on tints around grey: half-strength colours keep it a grade, not a wash
            Color Soft(Color k) => Color.Lerp(new Color(0.5f, 0.5f, 0.5f), k, 0.5f);
            white.temperature.Override(Mathf.Lerp(g.temp, -4f, c * 0.3f));
            white.tint.Override(Mathf.Lerp(g.tint, 4f, c * 0.3f));
            split.shadows.Override(Soft(Color.Lerp(g.shadows, new Color(0.45f, 0.38f, 0.65f), c * 0.3f)));
            split.highlights.Override(Soft(g.highlights));
            split.balance.Override(g.balance);
            // cutscene close-ups: a 50 mm lens at f/2.8 focused on the subject; gameplay keeps the far-haze blur
            bool cine = CutsceneFocus > 0f && dof.active && Services.Graphics.Tier >= GraphicsTier.High;
            if (cine)
            {
                focus = Mathf.Lerp(focus, CutsceneFocus, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 6f));
                dof.mode.Override(DepthOfFieldMode.Bokeh);
                dof.focusDistance.Override(focus);
                dof.focalLength.Override(Mathf.Lerp(35f, 65f, Mathf.InverseLerp(8f, 2f, focus)));   // tighter shots, longer lens
                dof.aperture.Override(2.8f);
                dof.bladeCount.Override(7);
            }
            else dof.mode.Override(DepthOfFieldMode.Gaussian);
            impact = Mathf.MoveTowards(impact, 0f, Time.unscaledDeltaTime * 2.5f);
            // vivid either way: corruption only takes the edge off the colour (the sky, glitches and music carry it)
            color.saturation.Override(Mathf.Lerp(g.sat, g.sat - 6f, c));
            color.contrast.Override(Mathf.Lerp(g.contrast, g.contrast + 4f, c));
            color.postExposure.Override(Mathf.Lerp(0.3f, 0.2f, c) + g.exposure);
            chroma.intensity.Override(Mathf.Clamp01(c * 0.04f + impact * 0.6f + GlitchFX.Level * 0.4f));
            lens.intensity.Override(-impact * 0.25f);
            vignette.color.Override(Color.black);
            if (Freeze > 0f)
            {
                color.saturation.Override(Mathf.Lerp(color.saturation.value, -70f, Freeze));
                color.contrast.Override(Mathf.Lerp(color.contrast.value, 35f, Freeze));
                vignette.intensity.Override(Mathf.Lerp(0.18f, 0.42f, Freeze));
            }
            else vignette.intensity.Override(0.18f);
        }
    }
}
