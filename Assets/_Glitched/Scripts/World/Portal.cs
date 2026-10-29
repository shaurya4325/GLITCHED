using UnityEngine;

namespace Glitched
{
    /// <summary>A door/portal to another scene (next level, back to the hub, the world map).</summary>
    public class Portal : Interactable
    {
        public string targetScene;
        public string spawnId = "";
        public string label = "Enter";
        [Tooltip("Level id that must be open (Game.IsLevelOpen) before this portal works. Empty = always.")]
        public string requiresLevelOpen = "";
        [Tooltip("Mark this level complete when used (level exits).")]
        public bool completesLevel;
        public bool auto;             // walk-in exit (no button)
        bool used2;

        public override string PromptText => Locked ? $"<color=#ff5a6a>LOCKED</color>  {label}" : label;

        bool Locked
        {
            get
            {
                if (string.IsNullOrEmpty(requiresLevelOpen)) return false;
                var arc = Services.Game.CurrentArc;
                if (arc == null) return false;
                var l = arc.Level(requiresLevelOpen);
                return l != null && !Services.Game.IsLevelOpen(arc, l);
            }
        }

        public override void Interact(PlayerController p)
        {
            if (Locked) { Companion.Say("Locked. The Glitch keeps a tidy queue. Finish the earlier area first.", 3f, "bored"); Services.Audio.Play("door_locked"); return; }
            Go();
        }

        void Go()
        {
            if (used2) return;
            used2 = true;
            var g = Services.Game;
            if (completesLevel && g.CurrentArc != null && g.CurrentLevel != null) g.CompleteLevel(g.CurrentArc.id, g.CurrentLevel.id);
            Services.Scenes.Load(targetScene, spawnId);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!auto || !other.CompareTag("Player")) return;
            if (Locked) return;
            Go();
        }
    }
}
