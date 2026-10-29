using UnityEngine;

namespace Glitched
{
    /// <summary>Keeps a UI element positioned over a world point or transform (damage numbers, speech bubbles, enemy bars).</summary>
    public class WorldFollower : MonoBehaviour
    {
        public Transform target;
        public Vector3 worldPoint;
        public Vector3 offset;
        public float rise;
        public bool clampToScreen;
        RectTransform rt;
        RectTransform parentRt;

        public void Init(Vector3 world, Vector3 extraOffset)
        {
            target = null; worldPoint = world; offset = extraOffset; rise = 0;
            Place();
        }

        public void Init(Transform t, Vector3 off)
        {
            target = t; offset = off; rise = 0;
            Place();
        }

        void LateUpdate() => Place();

        void Place()
        {
            if (rt == null) { rt = (RectTransform)transform; parentRt = rt.parent as RectTransform; }
            var cam = CameraRig.Instance != null ? CameraRig.Instance.Cam : Camera.main;
            if (cam == null || parentRt == null) return;
            Vector3 w = (target != null ? target.position : worldPoint) + offset + Vector3.up * rise;
            Vector3 sp = cam.WorldToScreenPoint(w);
            bool behind = sp.z < 0;
            if (clampToScreen)
            {
                sp.x = Mathf.Clamp(sp.x, 120, Screen.width - 120);
                sp.y = Mathf.Clamp(sp.y, 120, Screen.height - 120);
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRt, sp, null, out var local);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = behind ? new Vector2(99999, 99999) : local;
        }
    }
}
