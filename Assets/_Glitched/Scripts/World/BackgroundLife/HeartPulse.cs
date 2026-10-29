using UnityEngine;

namespace Glitched
{
    /// <summary>Organic walls that pulse with the heartbeat (Bio-Zone). The beat is irregular while corrupted.</summary>
    public class HeartPulse : MonoBehaviour
    {
        public float bpm = 72f;
        public float amount = 0.05f;
        Vector3 baseScale;
        void Start() => baseScale = transform.localScale;
        void Update()
        {
            float c = Services.Corruption.Value;
            float rate = bpm / 60f * (1f + c * Mathf.PerlinNoise(Time.time * 0.5f, transform.position.x) * 0.8f);
            float ph = Mathf.Repeat(Time.time * rate, 1f);
            float beat = ph < 0.12f ? Mathf.Sin(ph / 0.12f * Mathf.PI) : (ph > 0.25f && ph < 0.34f ? Mathf.Sin((ph - 0.25f) / 0.09f * Mathf.PI) * 0.6f : 0f);
            transform.localScale = baseScale * (1f + beat * amount);
        }
    }
}
