using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// A sealed area that opens with an ability (double jump, dash, wall jump, shrink).
    /// Used for light backtracking: come back later with a new ability.
    /// </summary>
    public class SealedBarrier : MonoBehaviour
    {
        public string requiredAbility = "dash";
        public string lockedLine = "That's sealed. We'll need a new trick to get through.";
        Collider solid;
        bool open;
        bool told;

        void Start()
        {
            solid = GetComponent<Collider>();
            if (Services.Game.HasAbility(requiredAbility)) Open(false);
            GameEvents.AbilityUnlocked += OnAbility;
        }

        void OnDestroy() => GameEvents.AbilityUnlocked -= OnAbility;
        void OnAbility(string id) { if (id == requiredAbility) Open(true); }

        void OnTriggerEnter(Collider other) => OnHintEnter(other);

        public void OnHintEnter(Collider other)
        {
            if (open || told || !other.CompareTag("Player")) return;
            told = true;
            Companion.Say(lockedLine + $"\n<size=80%><color=#40ebff>Needs: {GameManager.AbilityName(requiredAbility)}</color></size>", 4f, "bored", lockedLine);
        }

        public void Open(bool fx)
        {
            if (open) return;
            open = true;
            if (fx) { VFX.GlitchApart(transform.position + Vector3.up, 1.5f); Services.Audio.Play("door_open"); }
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            foreach (var c in GetComponentsInChildren<Collider>()) if (!c.isTrigger) c.enabled = false;
        }
    }
}
