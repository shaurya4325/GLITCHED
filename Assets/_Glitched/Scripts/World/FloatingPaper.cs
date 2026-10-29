using UnityEngine;

namespace Glitched
{
    /// <summary>An exam paper the Glitch has lifted off its desk: drifts and turns in the air.</summary>
    public class FloatingPaper : MonoBehaviour
    {
        [SerializeField] Vector3 home; [SerializeField] float height, phase;   // kept when the hall is built into the scene
        public void Init(float h, float p) { home = transform.position; height = h; phase = p; }
        void Update()
        {
            float t = Time.time * 0.6f + phase;
            transform.position = home + new Vector3(Mathf.Sin(t * 0.7f) * 0.25f, height * (0.6f + 0.4f * Mathf.Sin(t)), Mathf.Cos(t * 0.5f) * 0.2f);
            transform.rotation = Quaternion.Euler(Mathf.Sin(t * 1.3f) * 25f, t * 40f, Mathf.Cos(t * 1.1f) * 20f);
        }
    }
}
