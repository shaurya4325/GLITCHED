using UnityEngine;

namespace Glitched
{
    /// <summary>A platform that cracks a moment after you step on it, falls, and (unless permanent) comes back.</summary>
    public class CrumblePlatform : MonoBehaviour
    {
        public float delay = 0.55f;
        public float respawn = 3.5f;
        public bool permanent;
        public Transform visual;
        Collider[] cols;
        Vector3 home;
        float t;
        int state;   // 0 idle, 1 shaking, 2 fallen
        public bool Fallen => state == 2;

        void Start()
        {
            cols = GetComponentsInChildren<Collider>();
            if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
            if (visual != null) home = visual.localPosition;
        }

        public void Step(PlayerController p)
        {
            if (state != 0) return;
            state = 1; t = 0f;
            Services.Audio.Play("door_locked", 0.5f);
        }

        /// <summary>Drops at once (a wrong answer platform).</summary>
        public void Break(float after = 0.12f)
        {
            if (state == 2) return;
            state = 1; t = delay - after;
        }

        void Update()
        {
            if (state == 0) return;
            t += Time.deltaTime;
            if (state == 1)
            {
                if (visual != null) visual.localPosition = home + new Vector3(Mathf.Sin(t * 70f) * 0.05f, Mathf.Sin(t * 53f) * 0.03f, 0f);
                if (t >= delay)
                {
                    state = 2; t = 0f;
                    foreach (var c in cols) if (c != null && !c.isTrigger) c.enabled = false;
                    VFX.Dust(transform.position);
                    Services.Audio.Play("fall", 0.4f);
                }
            }
            else
            {
                if (visual != null && t < 2f) visual.localPosition = home + Vector3.down * (4.9f * t * t);
                if (!permanent && t >= respawn)
                {
                    state = 0;
                    if (visual != null) visual.localPosition = home;
                    foreach (var c in cols) if (c != null) c.enabled = true;
                    VFX.Burst(transform.position, UIFactory.Theme.cyan, 8, "spark");
                }
                else if (permanent && visual != null && t >= 2f) visual.gameObject.SetActive(false);
            }
        }
    }
}
