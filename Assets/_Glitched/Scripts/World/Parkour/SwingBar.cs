using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// A horizontal bar to swing from. The player hangs from it as a pendulum (period T = 2π√(L/g)): lean with
    /// the swing to pump it higher, jump to let go and fly off along the tangent.
    /// </summary>
    public class SwingBar : MonoBehaviour
    {
        public static readonly List<SwingBar> All = new List<SwingBar>();
        public float catchRadius = 1.2f;
        [Tooltip("Pendulum length (m), hands to the body's centre of mass.")]
        public float length = 1.6f;
        [Tooltip("Angular acceleration (rad/s²) from leaning with the swing.")]
        public float pump = 7f;
        [Tooltip("Multiplier on the release speed.")]
        public float release = 1.2f;
        public Transform bar;
        float wobble;

        public Vector2 Pivot => transform.position;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public void Catch() { wobble = 1f; Services.Audio.Play("footstep", 0.8f); }

        void Update()
        {
            wobble = Mathf.MoveTowards(wobble, 0f, Time.deltaTime * 2f);
            if (bar != null) bar.localPosition = new Vector3(0f, -Mathf.Sin(Time.time * 30f) * 0.03f * wobble, 0f);
        }
    }
}
