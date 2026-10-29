using UnityEngine;

namespace Glitched
{
    /// <summary>Everything that makes one generated person look different: body, face, hair, outfit, colours.</summary>
    [System.Serializable]
    public class HumanSpec
    {
        public string id = "human";
        public float height = 1.65f;          // metres
        [Range(0, 1)] public float build = 0.4f;        // 0 slim .. 1 heavy
        [Range(0, 1)] public float femininity = 0f;     // shoulder/hip/chest proportions
        public Color skin = new Color(0.78f, 0.58f, 0.45f);
        public Color hairColor = new Color(0.08f, 0.06f, 0.05f);
        public Color eyeColor = new Color(0.25f, 0.16f, 0.1f);
        public HairStyle hair = HairStyle.Short;
        public Top top = Top.Hoodie;
        public Color topColor = new Color(0.16f, 0.3f, 0.72f);
        public Color topAccent = new Color(0.92f, 0.92f, 0.95f);
        public Bottom bottom = Bottom.Jeans;
        public Color bottomColor = new Color(0.16f, 0.22f, 0.36f);
        public Color shoeColor = new Color(0.92f, 0.92f, 0.92f);
        public Color shoeAccent = new Color(0.85f, 0.2f, 0.25f);
        public bool backpack;
        public Color backpackColor = new Color(0.85f, 0.42f, 0.12f);
        public bool headphones;
        public bool glasses;
        public bool labCoat;                  // scientist: long white coat over the top
        public bool goggles;

        [Header("Robot / mech body (rigid armour plates on the same humanoid skeleton)")]
        public bool robot;
        public Color plate = new Color(0.72f, 0.74f, 0.76f);
        public Color plateDark = new Color(0.16f, 0.17f, 0.19f);
        public Color glow = new Color(0.3f, 0.9f, 1f);
        [Range(0, 1)] public float bulk = 0.3f;     // 0 lean android .. 1 hulking mech
        public bool visor = true;                   // a glowing visor strip instead of two eyes
        public bool antenna;
        public bool peakedCap;                      // drill-sergeant hat

        public enum HairStyle { Short, Messy, Long, Bun, Buzz, Bald, Cap }
        public enum Top { TShirt, Hoodie, Shirt, Jacket, Suit }
        public enum Bottom { Jeans, Trousers, Shorts, Skirt }

        /// <summary>The protagonist: a Class 11 student who was gaming twenty minutes ago.</summary>
        public static HumanSpec Aarav() => new HumanSpec
        {
            id = "aarav", height = 1.66f, build = 0.35f, femininity = 0f,
            skin = new Color(0.72f, 0.52f, 0.39f), hairColor = new Color(0.06f, 0.045f, 0.04f), eyeColor = new Color(0.22f, 0.14f, 0.08f),
            hair = HairStyle.Messy, top = Top.Hoodie, topColor = new Color(0.17f, 0.3f, 0.66f), topAccent = new Color(0.94f, 0.94f, 0.96f),
            bottom = Bottom.Jeans, bottomColor = new Color(0.17f, 0.23f, 0.37f), shoeColor = new Color(0.94f, 0.94f, 0.94f), shoeAccent = new Color(0.86f, 0.22f, 0.26f),
            backpack = true, backpackColor = new Color(0.88f, 0.45f, 0.13f), headphones = true,
        };

        /// <summary>Corporate security android (Crypto Cracker).</summary>
        public static HumanSpec SecurityBot() => new HumanSpec
        {
            id = "security_bot", height = 1.85f, robot = true, bulk = 0.2f, plate = new Color(0.82f, 0.83f, 0.85f), plateDark = new Color(0.12f, 0.13f, 0.15f),
            glow = new Color(1f, 0.25f, 0.3f), visor = true,
        };

        /// <summary>Sergeant Syntax: a drill-sergeant robot with a peaked cap.</summary>
        public static HumanSpec SergeantBot() => new HumanSpec
        {
            id = "sergeant_syntax", height = 2.4f, robot = true, bulk = 0.6f, plate = new Color(0.32f, 0.38f, 0.26f), plateDark = new Color(0.12f, 0.13f, 0.11f),
            glow = new Color(1f, 0.45f, 0.15f), visor = false, peakedCap = true,
        };

        /// <summary>Lord Infinite Loop: a hulking server-mech with a screen for a face.</summary>
        public static HumanSpec LoopMech() => new HumanSpec
        {
            id = "lord_infinite_loop", height = 5f, robot = true, bulk = 1f, plate = new Color(0.2f, 0.22f, 0.25f), plateDark = new Color(0.08f, 0.085f, 0.095f),
            glow = new Color(0.3f, 0.9f, 1f), visor = true, antenna = true,
        };

        /// <summary>A random citizen of the city (seeded, so the same NPC always looks the same).</summary>
        public static HumanSpec Citizen(int seed)
        {
            var r = new System.Random(seed);
            float F() => (float)r.NextDouble();
            Color Pick(params Color[] c) => c[r.Next(c.Length)];
            bool fem = F() < 0.5f;
            var s = new HumanSpec
            {
                id = "citizen_" + seed,
                height = fem ? 1.52f + F() * 0.16f : 1.62f + F() * 0.2f,
                build = F() * 0.8f,
                femininity = fem ? 0.75f + F() * 0.25f : F() * 0.15f,
                skin = Color.Lerp(new Color(0.86f, 0.68f, 0.55f), new Color(0.42f, 0.28f, 0.2f), F()),
                hairColor = Pick(new Color(0.05f, 0.04f, 0.035f), new Color(0.12f, 0.08f, 0.05f), new Color(0.28f, 0.2f, 0.12f), new Color(0.5f, 0.5f, 0.52f)),
                hair = fem ? (F() < 0.6f ? HairStyle.Long : HairStyle.Bun) : (HairStyle)r.Next(0, 6),
                top = (Top)r.Next(0, 5),
                topColor = Pick(new Color(0.85f, 0.85f, 0.83f), new Color(0.15f, 0.16f, 0.18f), new Color(0.55f, 0.12f, 0.12f), new Color(0.2f, 0.35f, 0.25f), new Color(0.3f, 0.45f, 0.7f), new Color(0.7f, 0.6f, 0.45f), new Color(0.45f, 0.3f, 0.5f)),
                topAccent = Pick(Color.white, new Color(0.2f, 0.2f, 0.22f)),
                bottom = fem && F() < 0.3f ? Bottom.Skirt : (F() < 0.55f ? Bottom.Jeans : Bottom.Trousers),
                bottomColor = Pick(new Color(0.16f, 0.22f, 0.36f), new Color(0.12f, 0.12f, 0.13f), new Color(0.4f, 0.36f, 0.3f), new Color(0.28f, 0.3f, 0.34f)),
                shoeColor = Pick(new Color(0.1f, 0.09f, 0.08f), new Color(0.92f, 0.92f, 0.92f), new Color(0.35f, 0.22f, 0.14f)),
                glasses = F() < 0.2f,
                backpack = F() < 0.15f,
            };
            s.shoeAccent = s.shoeColor * 0.7f;
            s.eyeColor = Color.Lerp(new Color(0.18f, 0.11f, 0.06f), new Color(0.3f, 0.36f, 0.3f), F() * 0.4f);
            return s;
        }
    }
}
