using UnityEngine;

namespace Glitched
{
    /// <summary>Spins a transform (propellers, halos, rings, floating debris).</summary>
    public class Spinner : MonoBehaviour
    {
        public Vector3 speed = new Vector3(0, 90, 0);
        public bool unscaled;
        void Update() => transform.Rotate(speed * (unscaled ? Time.unscaledDeltaTime : Time.deltaTime), Space.Self);
    }
}
