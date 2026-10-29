using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Emissive windows turning on and off in patterns. Driven by the Glitchable material property _WindowFlicker
    /// (the shader picks per-window cells), not by per-window objects.
    /// </summary>
    public class WindowLightFlicker : MonoBehaviour
    {
        [Range(0, 1)] public float amount = 1f;
        public float cellSize = 1.5f;
        public Color windowColor = new Color(1.4f, 1.1f, 0.6f);
        MaterialPropertyBlock mpb;
        Renderer[] renderers;

        void Start()
        {
            mpb = new MaterialPropertyBlock();
            renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.GetPropertyBlock(mpb);
                mpb.SetFloat("_WindowFlicker", amount);
                mpb.SetFloat("_WindowCell", cellSize);
                r.SetPropertyBlock(mpb);
            }
        }
    }
}
