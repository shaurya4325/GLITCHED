using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Trigger volume that changes the camera for big moments (pull back to reveal a huge tower, tighten in tunnels).
    /// Put it on a GameObject with a trigger BoxCollider at the gameplay plane.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class CameraZone : MonoBehaviour
    {
        public float distance = 18f;
        public float fov = 40f;
        public float yawOffset;
        public float heightOffset;
        public float blendTime = 1.2f;
        public int priority;
        [Tooltip("Optional: say something when the player first enters (e.g. 'Whoa. That tower is HUGE.').")]
        public string companionLine;
        bool spoken;

        void Reset() { GetComponent<BoxCollider>().isTrigger = true; }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (CameraRig.Instance != null) CameraRig.Instance.PushZone(this);
            if (!spoken && !string.IsNullOrEmpty(companionLine))
            {
                spoken = true;
                Companion.Say(companionLine);
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (CameraRig.Instance != null) CameraRig.Instance.PopZone(this);
        }
    }
}
