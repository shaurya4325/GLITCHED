namespace Glitched
{
    /// <summary>The cosmetic shop terminal in each hub.</summary>
    public class ShopTerminal : Interactable
    {
        void Start() { prompt = "Costume terminal"; }

        public override void Interact(PlayerController p)
        {
            Services.Audio.Play("terminal");
            ShopScreen.Open();
        }
    }
}
