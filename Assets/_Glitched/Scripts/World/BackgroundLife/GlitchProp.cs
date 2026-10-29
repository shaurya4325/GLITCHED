using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// A corrupted prop that flickers out of existence and returns in the wrong place or at the wrong scale.
    /// Back to normal once the region is restored.
    /// </summary>
    public class GlitchProp : MonoBehaviour
    {
        public float wanderRadius = 1.5f;
        public float minScale = 0.6f, maxScale = 1.5f;
        Vector3 basePos, baseScale;
        Renderer[] renderers;
        float next;
        bool hidden;

        void Start()
        {
            basePos = transform.localPosition;
            baseScale = transform.localScale;
            renderers = GetComponentsInChildren<Renderer>();
            next = Time.time + Random.Range(1f, 6f);
        }

        void Update()
        {
            float c = Services.Corruption.Value;
            if (c < 0.3f)
            {
                if (hidden || transform.localPosition != basePos) { Show(true); transform.localPosition = basePos; transform.localScale = baseScale; }
                return;
            }
            if (Time.time < next) return;
            if (!hidden)
            {
                Show(false);
                next = Time.time + Random.Range(0.08f, 0.4f);
            }
            else
            {
                Show(true);
                bool wrong = Random.value < 0.6f * c;
                transform.localPosition = wrong ? basePos + Random.insideUnitSphere * wanderRadius : basePos;
                transform.localScale = wrong ? baseScale * Random.Range(minScale, maxScale) : baseScale;
                next = Time.time + Random.Range(1.5f, 7f) / Mathf.Max(0.3f, c);
            }
        }

        void Show(bool on)
        {
            hidden = !on;
            foreach (var r in renderers) if (r != null) r.enabled = on;
        }
    }
}
