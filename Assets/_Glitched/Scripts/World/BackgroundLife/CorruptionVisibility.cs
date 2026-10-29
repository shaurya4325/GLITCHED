using UnityEngine;

namespace Glitched
{
    /// <summary>Shows its children only while the region is corrupted (virus swarms, sky tears, broken props) or only once restored.</summary>
    public class CorruptionVisibility : MonoBehaviour
    {
        public bool showWhenCorrupted = true;
        public float threshold = 0.5f;
        bool state, init;

        void Update()
        {
            bool corrupted = Services.Corruption.Value > threshold;
            bool on = showWhenCorrupted ? corrupted : !corrupted;
            if (init && on == state) return;
            init = true; state = on;
            for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(on);
        }
    }
}
