using UnityEngine;
using UnityEngine.Rendering;

namespace Glitched
{
    public enum GraphicsTier { Potato = 0, Low = 1, Medium = 2, High = 3, Ultra = 4 }

    /// <summary>Everything one graphics quality tier controls (Section 4.1 of the design).</summary>
    [CreateAssetMenu(menuName = "Glitched/Graphics Tier", fileName = "GraphicsTier")]
    public class GraphicsTierSO : ScriptableObject
    {
        public GraphicsTier tier;
        [Tooltip("URP asset used by this tier (Mobile-Lite, Balanced or PC-High).")]
        public RenderPipelineAsset urpAsset;
        [Range(0.5f, 1f)] public float renderScale = 1f;
        [Tooltip("0 = unlimited (vsync)")] public int fpsCap = 60;
        public bool realtimeShadows = true;
        [Range(1, 4)] public int shadowCascades = 1;
        public int shadowResolution = 1024;
        public float shadowDistance = 40f;
        [Tooltip("1, 2, 4 or 8")] public int msaa = 1;

        [Header("Post-processing")]
        public bool postProcessing = true;
        public bool colorGrading = true;
        public bool bloom = true;
        public bool depthOfField;
        public bool vignette = true;
        public bool chromaticAberration;
        public bool fullscreenGlitch = true;
        [Tooltip("Potato/Low draw the glitch as a cheap UI overlay instead of post-processing.")]
        public bool glitchAsUIOverlay;

        [Header("Depth layers (L0 foreground ... L5 sky events)")]
        public bool layerL0 = true;
        public bool layerL3 = true;
        public bool layerL5Events = true;
        public bool simpleL2;

        [Header("Density multipliers")]
        [Range(0, 1)] public float trafficAndCrowd = 1f;
        [Range(0, 1)] public float particles = 1f;
        public float lodBias = 1f;
        [Tooltip("0 = full, 1 = half, 2 = quarter")] public int textureMipLimit;

        [Header("Feel")]
        public float cameraYawSway = 3f;
        public bool simpleGlitchShader;
        public FogMode fogMode = FogMode.ExponentialSquared;
        public bool heightFog;
    }
}
