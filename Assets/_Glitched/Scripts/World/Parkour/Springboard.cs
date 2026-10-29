using UnityEngine;

namespace Glitched
{
    /// <summary>A springboard: land on it and it throws you up (stored elastic energy: ½kx² becomes mgh).</summary>
    public class Springboard : MonoBehaviour
    {
        [Tooltip("Height (m) the bounce reaches.")]
        public float height = 7f;
        [Tooltip("Optional horizontal launch speed (0 = keep your own).")]
        public float forward;
        public Transform pad;
        float squash, cooldown;
        Vector3 padScale = Vector3.one;

        void Start() { if (pad != null) padScale = pad.localScale; }

        public void Bounce(PlayerController p)
        {
            if (cooldown > 0f) return;
            cooldown = 0.3f;
            p.Launch(height, Mathf.Abs(forward) > 0.01f ? forward : p.Velocity.x);
            squash = 1f;
            Services.Audio.Play("double_jump");
            VFX.Burst(transform.position + Vector3.up * 0.4f, UIFactory.Theme.yellow, 14, "spark", 5f);
            if (CameraRig.Instance != null) CameraRig.Instance.Shake(0.12f);
        }

        void Update()
        {
            cooldown -= Time.deltaTime;
            squash = Mathf.MoveTowards(squash, 0f, Time.deltaTime * 3.5f);
            if (pad != null) pad.localScale = new Vector3(padScale.x, padScale.y * (1f - 0.45f * Mathf.Sin(squash * Mathf.PI)), padScale.z);
        }
    }
}
