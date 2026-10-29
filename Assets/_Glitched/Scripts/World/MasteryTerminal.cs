namespace Glitched
{
    /// <summary>A terminal that shows the mastery record (same as Pause -> Mastery).</summary>
    public class MasteryTerminal : Interactable
    {
        void Start() { prompt = "Mastery record"; }

        public override void Interact(PlayerController p)
        {
            Services.Audio.Play("terminal");
            Services.Game.SetPaused(true);
            SubScreen.Create<MasteryScreen>("MASTERY", new UnityEngine.Vector2(1400, 900), () => Services.Game.SetPaused(false));
        }
    }
}
