using UnityEngine;

namespace Glitched
{
    /// <summary>Physics Valley gravity-flip zone: inside it, gravity points up.</summary>
    public class GravityZone : MonoBehaviour
    {
        public float gravitySign = -1f;
        public string enterLine = "Gravity's having a day. Up is down now. Try not to think about it.";
        static bool explained;

        void OnTriggerEnter(Collider other)
        {
            var p = other.GetComponent<PlayerController>();
            if (p == null) return;
            p.SetGravity(gravitySign);
            if (!explained && !string.IsNullOrEmpty(enterLine)) { explained = true; Companion.Say(enterLine); }
        }

        void OnTriggerExit(Collider other)
        {
            var p = other.GetComponent<PlayerController>();
            if (p != null) p.SetGravity(1f);
        }
    }
}
