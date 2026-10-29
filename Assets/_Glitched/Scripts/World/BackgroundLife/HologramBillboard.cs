using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// A hologram screen showing scrolling content. When corrupted it uses the glitch text effect
    /// (the Hologram shader tears rows and swaps to the accent colour), and occasionally flickers off.
    /// </summary>
    public class HologramBillboard : MonoBehaviour
    {
        public Texture2D content;
        public Color color = new Color(0.4f, 1.6f, 2f);
        public Vector2 scroll = new Vector2(0, 0.08f);
        public bool faceCamera;
        Renderer r;
        MaterialPropertyBlock mpb;
        float offUntil;

        void Start()
        {
            r = GetComponentInChildren<Renderer>();
            mpb = new MaterialPropertyBlock();
            if (r == null) return;
            r.GetPropertyBlock(mpb);
            if (content != null) mpb.SetTexture("_BaseMap", content);
            mpb.SetColor("_BaseColor", color);
            mpb.SetVector("_Scroll", scroll);
            r.SetPropertyBlock(mpb);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void Update()
        {
            if (r == null) return;
            float c = Services.Corruption.Value;
            if (Time.time > offUntil && Random.value < Time.deltaTime * 0.4f * c) offUntil = Time.time + Random.Range(0.05f, 0.3f);
            r.enabled = Time.time > offUntil;
            if (faceCamera && CameraRig.Instance != null)
            {
                var cam = CameraRig.Instance.Cam.transform;
                Vector3 d = transform.position - cam.position; d.y = 0;
                if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
            }
        }
    }
}
