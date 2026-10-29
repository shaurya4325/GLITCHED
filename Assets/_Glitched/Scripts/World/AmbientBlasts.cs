using System.Collections;
using UnityEngine;

namespace Glitched
{
    /// <summary>Corrupted regions: now and then something blows up far away on the skyline, and smoke rises for a while.</summary>
    public class AmbientBlasts : MonoBehaviour
    {
        public BlastStyle style;
        public float minZ = 160f, maxZ = 320f;
        float t;

        void Start() => t = Random.Range(8f, 16f);

        void Update()
        {
            if (Services.Corruption.Value < 0.3f || Services.Graphics == null || !Services.Graphics.LayerActive(DepthLayer.L5_SkyEvents)) return;
            if ((t -= Time.deltaTime) > 0f) return;
            t = Random.Range(22f, 45f);
            var cam = CameraRig.Instance != null ? CameraRig.Instance.Cam.transform.position : Vector3.zero;
            float z = Random.Range(minZ, maxZ);
            var pos = new Vector3(cam.x + Random.Range(-0.45f, 0.45f) * z, Random.Range(10f, 45f), z);
            Blast.At(pos, Random.Range(6f, 10f), style, false, false, true);
            Services.Audio.Play("thunder", 0.3f);
            Blast.SmokeColumn(pos, Random.Range(4f, 7f), 30f, new Color(0.3f, 0.28f, 0.27f));
        }
    }
}
