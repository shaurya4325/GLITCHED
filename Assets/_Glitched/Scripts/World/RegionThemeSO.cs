using UnityEngine;

namespace Glitched
{
    /// <summary>Colours, sky and atmosphere for one region, in its corrupted and restored states.</summary>
    [CreateAssetMenu(menuName = "Glitched/Region Theme", fileName = "RegionTheme")]
    public class RegionThemeSO : ScriptableObject
    {
        public string id;

        [Header("Corrupted")]
        public Color fogCorrupted = new Color(0.05f, 0.06f, 0.15f);
        public Color ambientSkyCorrupted = new Color(0.25f, 0.2f, 0.45f);
        public Color ambientEquatorCorrupted = new Color(0.15f, 0.1f, 0.3f);
        public Color ambientGroundCorrupted = new Color(0.05f, 0.05f, 0.1f);
        public Color sunCorrupted = new Color(0.7f, 0.5f, 1f);
        public Color accentCorrupted = new Color(1f, 0.2f, 0.85f);
        public Texture skyCorrupted;
        public Color skyTintCorrupted = new Color(0.6f, 0.5f, 0.9f);

        [Header("Restored")]
        public Color fogRestored = new Color(0.65f, 0.8f, 1f);
        public Color ambientSkyRestored = new Color(0.6f, 0.75f, 1f);
        public Color ambientEquatorRestored = new Color(0.5f, 0.55f, 0.6f);
        public Color ambientGroundRestored = new Color(0.3f, 0.28f, 0.25f);
        public Color sunRestored = new Color(1f, 0.95f, 0.85f);
        public Color accentRestored = new Color(0.2f, 0.9f, 1f);
        public Texture skyRestored;
        public Color skyTintRestored = Color.white;

        [Header("Atmosphere")]
        public float fogStart = 40f;
        public float fogEnd = 420f;
        public float fogDensity = 0.006f;
        public float sunIntensityCorrupted = 0.7f;
        public float sunIntensityRestored = 1.25f;
        public Vector3 sunEuler = new Vector3(40f, -30f, 0f);
        public float skyExposure = 1f;
        public string ambientParticles = "datamotes";   // datamotes, dust, bubbles, cells, sparks
        public string musicKey = "explore_CodeCity";
    }
}
