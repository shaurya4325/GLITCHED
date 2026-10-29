using UnityEngine;

namespace Glitched
{
    /// <summary>Forwards a child trigger's enter event to a SealedBarrier (its own collider is solid).</summary>
    public class TriggerRelay : MonoBehaviour
    {
        public SealedBarrier target;
        void OnTriggerEnter(Collider other) { if (target != null) target.OnHintEnter(other); }
    }
}
