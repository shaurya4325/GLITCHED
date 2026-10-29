using System.Collections;
using UnityEngine;

namespace Glitched
{
    /// <summary>A short arrival moment for a region hub: a sweeping establishing shot, Pixel's take and Shaurya's reaction.</summary>
    public class ArcArrival : LevelScript
    {
        public override IEnumerator Run(RegionDirector director)
        {
            var arc = director.Arc;
            var d = Services.Game.Data;
            var p = PlayerController.Instance;
            if (arc == null || p == null || d.HasFlag("arrived_" + arc.id)) yield break;
            d.SetFlag("arrived_" + arc.id);
            if (Services.Game.IsArcRestored(arc.id)) yield break;
            CutscenePlayer.Begin();
            Vector3 c = p.transform.position;
            yield return CutscenePlayer.Shot(c + new Vector3(-6, 14, -34), c + new Vector3(30, 8, 40), 48, 0);
            yield return CutscenePlayer.Shot(c + new Vector3(16, 8, -30), c + new Vector3(30, 6, 30), 46, 4f);
            Services.UI.Banner(arc.displayName.ToUpper(), UIFactory.Theme.cyan, 120, 1.8f, arc.subject.ToUpper());
            yield return CutscenePlayer.Wait(1.2f);
            foreach (var line in arc.arrivalLines) yield return CutscenePlayer.Say("pixel", line, "normal");
            string reaction = arc.id == "PhysicsValley" ? "So physics is just... broken here. Great. My favourite subject. To fail." :
                              arc.id == "ChemicalChaos" ? "The elements are ALIVE? Okay. I've definitely licked worse things in a lab. Joking. Mostly." :
                              arc.id == "BioZone" ? "We're INSIDE someone. Someone please say we're not inside someone." :
                              "Okay. New place. Same panic.";
            yield return CutscenePlayer.Shot(c + new Vector3(2, 2.2f, -8), c + Vector3.up * 1.5f, 38, 1.2f);
            yield return CutscenePlayer.Say("aarav", reaction, "sweating");
            CutscenePlayer.End();
        }
    }
}
