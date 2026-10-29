using TMPro;
using UnityEngine;

namespace Glitched
{
    /// <summary>Credits: the team, the school, and every asset source with its licence.</summary>
    public class CreditsScreen : SubScreen
    {
        public static void Open() => Create<CreditsScreen>("CREDITS", new Vector2(1400, 920));

        public const string Text =
            "<size=130%><b>GLITCHED</b></size>\n<color=#40ebff>KNOWLEDGE IS YOUR WEAPON</color>\n\n" +
            "A project for the <b>CBSE Skill Expo & Guidance Festival 2026-27</b>\nCategory II (Classes 11-12)\n\n" +
            "<b>DAV Public School, Defence Colony, Dehradun</b>\n\n" +
            "<color=#ff3dc8>DESIGN, STORY & PROGRAMMING</color>\nThe GLITCHED student team\n\n" +
            "<color=#ff3dc8>ENGINE</color>\nUnity 6 (URP), Cinemachine, Input System, TextMeshPro\n\n" +
            "<color=#ff3dc8>3D MODELS (CC0)</color>\nQuaternius: Downtown City MegaKit, Stylized Nature MegaKit, Modular Sci-Fi MegaKit, Sci-Fi Essentials Kit, Fantasy Props MegaKit, Ultimate Platformer Pack, Animated Robot, Monster Pack, Universal Base Characters, Universal Animation Library\nKay Lousberg: KayKit Platformer Pack\nKenney: Space Kit\n\n" +
            "<color=#ff3dc8>BOSS MODELS (CC BY 4.0, via Sketchfab)</color>\nSergeant Syntax: \"Steampunk Robot - Police Enforcer Variant\" by Lagst\nLord Infinite Loop: \"Mech Robot KW-II\" by swds321\nBaron Friction: \"Security Cyborg\" by fletcherkinnear\nLord of Broken Laws: \"Animated Mech - Marceline Sentinel Armor\" by RainOnTheMoon\nLord of the Lab: \"Alien Ninja Creature\" by jungle_jim\nProfessor Plaque: \"Living Flesh Blob\" by Tim0\nLord Within: \"Insectoid Monster Rig\" by SuperKapoo913\nThe Source: \"RoboWyvern\" by amzarraif99\n(converted, rescaled and re-textured for GLITCHED)\n\n" +
            "<color=#ff3dc8>CHARACTERS & ANIMATION</color>\nMixamo (Adobe), free for use in games\n\n" +
            "<color=#ff3dc8>SKIES (CC0)</color>\nPoly Haven: Kloppenheim 02, Kloofendal 48d & 43d, Qwantani Dusk 2 & Mid Morning, Aerodynamics Workshop, Industrial Sunset 02\n\n" +
            "<color=#ff3dc8>TEXTURES (CC0)</color>\nambientCG.com\n\n" +
            "<color=#ff3dc8>SOUND (CC0)</color>\nKenney: Interface, Impact, Sci-Fi, Digital Audio, UI Audio, Music Jingles\nOriginal procedural score composed for GLITCHED\n\n" +
            "<color=#ff3dc8>FONTS (SIL OFL)</color>\nChakra Petch, Russo One, JetBrains Mono, Hind, OpenDyslexic, DejaVu Sans\n\n" +
            "<color=#8fa3c0>\"Knowledge isn't something you collect. It's something you use.\"</color>";

        protected override void Build()
        {
            var list = UIFactory.Scroll(Body, out _, 10);
            UIFactory.Stretch((RectTransform)list.parent);
            var t = UIFactory.Text(list, Text, 28, null, TextAlignmentOptions.Top);
            UIFactory.Fit(t);
        }
    }
}
