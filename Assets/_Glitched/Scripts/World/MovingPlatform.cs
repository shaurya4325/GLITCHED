using UnityEngine;

namespace Glitched
{
    /// <summary>A platform that moves between two points (ping-pong). The player is carried along.</summary>
    public class MovingPlatform : MonoBehaviour
    {
        public Vector3 offset = new Vector3(4, 0, 0);
        public float period = 4f;
        public float phase;
        [Tooltip("When corrupted, the platform stutters and skips.")]
        public bool glitchy = true;
        Vector3 start, last;
        public Vector3 Delta { get; private set; }

        void Start() { start = transform.position; last = start; }

        void Update()
        {
            float t = Time.time / Mathf.Max(0.1f, period) + phase;
            float k = Mathf.SmoothStep(0, 1, Mathf.PingPong(t * 2f, 1f));
            if (glitchy && Services.Corruption.Value > 0.5f && Mathf.Repeat(Time.time * 3.1f + phase, 1f) < 0.08f) k += 0.05f;
            Vector3 p = start + offset * k;
            Delta = p - last;
            last = p;
            transform.position = p;
        }
    }
}
