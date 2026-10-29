using UnityEngine;

namespace Glitched
{
    /// <summary>Controls the gaming monitor's screen material: live feed, in-screen glitch, crack, an image overlay.</summary>
    public class MonitorScreenFx : MonoBehaviour
    {
        Material mat;
        public float glitch, crack, image;

        public void Setup(Material m, Texture feed)
        {
            mat = m;
            if (feed != null) mat.SetTexture("_MainTex", feed);
            var crackTex = Resources.Load<Texture2D>("FX/monitor_crack");
            if (crackTex != null) mat.SetTexture("_CrackTex", crackTex);
            var err = Resources.Load<Texture2D>("FX/error404");
            if (err != null) mat.SetTexture("_ErrorTex", err);
        }

        /// <summary>Shows an image over the feed (error screen, Pixel's eye at the end).</summary>
        public void ShowImage(Texture tex, float amount = 1f)
        {
            if (mat == null) return;
            if (tex != null) mat.SetTexture("_ErrorTex", tex);
            image = amount;
        }

        void Update()
        {
            if (mat == null) return;
            mat.SetFloat("_Glitch", glitch);
            mat.SetFloat("_Crack", crack);
            mat.SetFloat("_Error", image);
        }
    }
}
