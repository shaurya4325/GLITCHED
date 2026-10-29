using UnityEngine;

namespace Glitched
{
    /// <summary>Keeps gameplay objects on the 2D gameplay plane (Z = 0). Rigidbodies also get Z and X/Y rotation frozen.</summary>
    public class PlaneLock : MonoBehaviour
    {
        public float planeZ;

        void Awake()
        {
            var rb = GetComponent<Rigidbody>();
            if (rb != null) rb.constraints |= RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
        }

        void LateUpdate()
        {
            var p = transform.position;
            if (Mathf.Abs(p.z - planeZ) > 0.0001f) { p.z = planeZ; transform.position = p; }
        }
    }
}
