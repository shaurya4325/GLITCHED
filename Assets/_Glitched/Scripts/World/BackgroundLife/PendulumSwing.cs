using UnityEngine;

namespace Glitched
{
    /// <summary>A giant swinging pendulum (Physics Valley). Swings wildly and irregularly while corrupted.</summary>
    public class PendulumSwing : MonoBehaviour
    {
        public float amplitude = 30f;
        public float period = 4f;
        float phase;
        void Start() => phase = Random.value * 10f;
        void Update()
        {
            float c = Services.Corruption.Value;
            float a = amplitude * (1f + c * 0.8f);
            float t = Time.time + phase;
            float angle = Mathf.Sin(t * Mathf.PI * 2f / period) * a + (c > 0.5f ? Mathf.Sin(t * 7.3f) * 8f * c : 0f);
            transform.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
