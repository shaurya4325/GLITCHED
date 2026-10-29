using UnityEngine;

namespace Glitched
{
    /// <summary>Buildings and chunks that bob, tilt and slowly rotate. Much stronger when corrupted.</summary>
    public class FloatingBob : MonoBehaviour
    {
        public float bobHeight = 0.4f;
        public float bobSpeed = 0.5f;
        public float tilt = 3f;
        public float spin;
        public float corruptedMultiplier = 4f;
        [Tooltip("Extra offset applied only while corrupted (buildings floating off the ground).")]
        public Vector3 corruptedLift = new Vector3(0, 3f, 0);
        public Vector3 corruptedTilt = new Vector3(0, 0, 8f);
        Vector3 basePos;
        Quaternion baseRot;
        float seed;

        void Start()
        {
            basePos = transform.localPosition;
            baseRot = transform.localRotation;
            seed = Random.value * 100f;
        }

        void Update()
        {
            float c = Services.Corruption.Value;
            float k = Mathf.Lerp(1f, corruptedMultiplier, c);
            float t = Time.time * bobSpeed + seed;
            transform.localPosition = basePos + corruptedLift * c + Vector3.up * Mathf.Sin(t) * bobHeight * k;
            var rot = Quaternion.Euler(Mathf.Sin(t * 0.7f) * tilt * k * 0.3f + corruptedTilt.x * c, spin * Time.time * c, Mathf.Cos(t * 0.6f) * tilt * k + corruptedTilt.z * c);
            transform.localRotation = baseRot * rot;
        }
    }
}
