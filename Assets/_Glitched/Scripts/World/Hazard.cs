using UnityEngine;

namespace Glitched
{
    /// <summary>Damaging area (acid, sparks, spikes). Optionally respawns the player like a pit.</summary>
    public class Hazard : MonoBehaviour
    {
        public float damage = 15f;
        public bool respawn;
        public string line = "";
        float cooldown;

        void OnTriggerStay(Collider other)
        {
            if (cooldown > 0 || !other.CompareTag("Player")) return;
            var p = other.GetComponent<PlayerController>();
            if (p == null || p.Invulnerable || p.Locked) return;
            cooldown = 0.8f;
            if (respawn) { p.FallRespawn(); return; }
            p.TakeDamage(damage, transform.position);
            if (!string.IsNullOrEmpty(line)) Companion.Say(line);
            if (p.Stability <= 0) p.RespawnAtCheckpoint(true);
        }

        void Update() { if (cooldown > 0) cooldown -= Time.deltaTime; }
    }
}
