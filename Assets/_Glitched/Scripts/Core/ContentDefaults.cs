using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// The default content of GLITCHED: characters, enemies, bosses, arcs, cosmetics, achievements and graphics tiers.
    /// The editor builder (Glitched -> Build Everything) saves these as editable assets in Assets/_Glitched/Data.
    /// After that, edit the assets, not this file.
    /// </summary>
    public static class ContentDefaults
    {
        /// <summary>Prototype length (Expo): each chapter is Hub -> one level (with its mini-boss) -> the Glitch Lord.</summary>
        public const bool Prototype = true;

        public static GameDatabase CreateRuntimeDatabase()
        {
            var db = ScriptableObject.CreateInstance<GameDatabase>();
            db.isRuntimeDefault = true;
            db.graphicsTiers = CreateTiers();
            db.characters = CreateCharacters();
            db.enemies = CreateEnemies();
            db.arcs = CreateArcs(db);
            db.cosmetics = CreateCosmetics();
            db.achievements = CreateAchievements();
            db.sfx = ScriptableObject.CreateInstance<SfxLibrarySO>();
            db.uiTheme = ScriptableObject.CreateInstance<UIThemeSO>();
            db.materials = ScriptableObject.CreateInstance<MaterialLibrarySO>();
            return db;
        }

        // ------------------------------------------------------------------ graphics tiers (Section 4.1)
        public static List<GraphicsTierSO> CreateTiers()
        {
            var list = new List<GraphicsTierSO>();
            GraphicsTierSO T(GraphicsTier t) { var s = ScriptableObject.CreateInstance<GraphicsTierSO>(); s.tier = t; s.name = "Tier_" + t; list.Add(s); return s; }

            var p = T(GraphicsTier.Potato);
            p.renderScale = 0.6f; p.fpsCap = 30; p.realtimeShadows = false; p.msaa = 1;
            p.postProcessing = false; p.colorGrading = false; p.bloom = false; p.vignette = false; p.fullscreenGlitch = false; p.glitchAsUIOverlay = true;
            p.layerL0 = false; p.layerL3 = false; p.layerL5Events = false; p.simpleL2 = true;
            p.trafficAndCrowd = 0.1f; p.particles = 0.15f; p.lodBias = 0.4f; p.textureMipLimit = 2;
            p.cameraYawSway = 1f; p.simpleGlitchShader = true; p.fogMode = FogMode.Linear; p.shadowDistance = 0;

            var l = T(GraphicsTier.Low);
            l.renderScale = 0.75f; l.fpsCap = 30; l.realtimeShadows = false; l.msaa = 1;
            l.postProcessing = true; l.colorGrading = true; l.bloom = false; l.vignette = false; l.fullscreenGlitch = false; l.glitchAsUIOverlay = true;
            l.layerL0 = false; l.layerL3 = false; l.layerL5Events = false;
            l.trafficAndCrowd = 0.3f; l.particles = 0.35f; l.lodBias = 0.6f; l.textureMipLimit = 1;
            l.cameraYawSway = 2f; l.simpleGlitchShader = true; l.fogMode = FogMode.Linear; l.shadowDistance = 0;

            var m = T(GraphicsTier.Medium);
            m.renderScale = 0.9f; m.fpsCap = 60; m.realtimeShadows = true; m.shadowCascades = 1; m.shadowResolution = 512; m.shadowDistance = 35; m.msaa = 2;
            m.postProcessing = true; m.bloom = true; m.vignette = true; m.chromaticAberration = false; m.fullscreenGlitch = true;
            m.layerL0 = true; m.layerL3 = true; m.layerL5Events = false;
            m.trafficAndCrowd = 0.6f; m.particles = 0.6f; m.lodBias = 1f; m.textureMipLimit = 0;
            m.cameraYawSway = 3f; m.simpleGlitchShader = false; m.fogMode = FogMode.ExponentialSquared;

            var h = T(GraphicsTier.High);
            h.renderScale = 1f; h.fpsCap = 60; h.realtimeShadows = true; h.shadowCascades = 2; h.shadowResolution = 1024; h.shadowDistance = 50; h.msaa = 4;
            h.postProcessing = true; h.bloom = true; h.depthOfField = true; h.vignette = true; h.chromaticAberration = true; h.fullscreenGlitch = true;
            h.layerL0 = true; h.layerL3 = true; h.layerL5Events = true;
            h.trafficAndCrowd = 1f; h.particles = 1f; h.lodBias = 1.5f; h.cameraYawSway = 3f; h.heightFog = true;

            var u = T(GraphicsTier.Ultra);
            u.renderScale = 1f; u.fpsCap = 0; u.realtimeShadows = true; u.shadowCascades = 4; u.shadowResolution = 4096; u.shadowDistance = 120; u.msaa = 4;
            u.postProcessing = true; u.bloom = true; u.depthOfField = true; u.vignette = true; u.chromaticAberration = true; u.fullscreenGlitch = true;
            u.layerL0 = true; u.layerL3 = true; u.layerL5Events = true;
            u.trafficAndCrowd = 1f; u.particles = 1f; u.lodBias = 2f; u.cameraYawSway = 3f; u.heightFog = true;
            return list;
        }

        // ------------------------------------------------------------------ characters (Section 10.1)
        public static List<CharacterProfileSO> CreateCharacters()
        {
            var list = new List<CharacterProfileSO>();
            CharacterProfileSO C(string id, string name, string personality, string catchphrase, string fail, Color a, Color b, float h, float pitch)
            {
                var c = ScriptableObject.CreateInstance<CharacterProfileSO>();
                c.id = id; c.name = "Char_" + id; c.displayName = name; c.personality = personality; c.catchphrase = catchphrase; c.failureReaction = fail;
                c.primaryColor = a; c.secondaryColor = b; c.height = h; c.voicePitch = pitch;
                list.Add(c); return c;
            }

            var aarav = C("aarav", "Shaurya", "An ordinary Class 11 student and gamer: curious, funny, slightly sarcastic, brave but regularly terrified. He was gaming twenty minutes ago.",
                "I was GAMING twenty minutes ago.", "Okay. Okay okay okay. That was a practice attempt.", new Color(0.18f, 0.32f, 0.75f), new Color(0.95f, 0.95f, 0.98f), 1.6f, 1.15f);
            aarav.useFaceDecal = true; aarav.faceFolder = "Faces/Aarav"; aarav.idleStyle = "fidget";

            var pixel = C("pixel", "Pixel", "A small floating cube-bot with one big screen-eye. Never fully explains anything, delivers most of the comedy, and is completely unbothered that the world is ending.",
                "That's a very long story.", "Don't worry. I've seen worse. Not recently. But I have.", new Color(0.25f, 0.9f, 1f), new Color(0.1f, 0.12f, 0.18f), 0.45f, 1.8f);
            pixel.humanoid = false; pixel.faceFolder = "Faces/Pixel"; pixel.idleStyle = "bob"; pixel.voiceBlipKey = "blip_pixel";

            var loop = C("lord_loop", "Lord Infinite Loop", "A giant corrupted AI that cannot stop repeating itself, and is extremely annoyed about it. Speaks in echoes.",
                "AGAIN. AGAIN. AGAIN.", "WHY DOES THIS KEEP HAPPENING HAPPENING HAPPENING?", new Color(0.2f, 0.9f, 1f), new Color(1f, 0.2f, 0.8f), 4.5f, 0.6f);
            loop.humanoid = false; loop.idleStyle = "loop";
            C("lord_laws", "The Lord of Broken Laws", "A pompous cosmic judge who rewrites gravity, force and time on a whim, then fines you for falling.",
                "Gravity is a SUGGESTION.", "OBJECTION! ...to myself.", new Color(0.6f, 0.3f, 1f), new Color(1f, 0.6f, 0.2f), 4.2f, 0.7f);
            C("lord_lab", "The Lord of the Lab", "A diva mad scientist who has taught the elements to refuse to bond. Treats every explosion as a standing ovation.",
                "Chemistry? I prefer CHAOS-try.", "My beautiful catastrophe!", new Color(0.3f, 1f, 0.4f), new Color(0.1f, 0.4f, 0.4f), 4f, 0.75f);
            C("lord_within", "The Lord Within", "A parasite with impeccable manners who attacks the systems that keep the body alive. Whispers. Polite. Terrifying.",
                "Make yourself at home. I did.", "How rude. You've been... healthy.", new Color(0.8f, 0.1f, 0.3f), new Color(1f, 0.5f, 0.7f), 4f, 0.65f);
            C("the_source", "The Source", "The corrupted core program of The Nexus. It wrote the rules, then deleted them, then forgot why.",
                "I WROTE the rules. Then I deleted them.", "ERROR: SELF NOT FOUND.", new Color(1f, 1f, 1f), new Color(1f, 0.2f, 0.8f), 6f, 0.5f);

            var kanha = C("kanha", "Kanha the Dard", "The class topper who never lost an exam, until The Nexus taught him what losing felt like. He built the Glitch so everyone else could fail too. Calm, smug, terrifyingly well-prepared.",
                "Pens down. Your time is UP.", "Impossible. I studied the WHOLE syllabus.", new Color(0.55f, 0.05f, 0.12f), new Color(1f, 0.25f, 0.5f), 1.68f, 0.82f);
            kanha.idleStyle = "calm";

            var npc = C("npc", "Nexus Citizen", "A citizen of The Nexus. Currently stuck.", "WELC0ME T0 THE NEXUS!", "Oh no.", new Color(0.6f, 0.7f, 0.9f), Color.white, 1.55f, 1f);
            npc.idleStyle = "frozen";
            return list;
        }

        // ------------------------------------------------------------------ enemies (Section 6.2)
        public static List<EnemyDataSO> CreateEnemies()
        {
            var list = new List<EnemyDataSO>();
            EnemyDataSO E(string id, string name, string arc, EnemyTier tier, string shape, Color tint, float h, string[] concepts, string[] attacks, string[] taunts, int hp = 100, int hits = 1)
            {
                var e = ScriptableObject.CreateInstance<EnemyDataSO>();
                e.id = id; e.name = "Enemy_" + id; e.displayName = name; e.arc = arc; e.tier = tier; e.placeholderShape = shape; e.tint = tint; e.height = h;
                e.concepts = new List<string>(concepts); e.attackNames = new List<string>(attacks); e.taunts = new List<string>(taunts);
                e.maxHP = hp; e.challengesToDefeat = hits;
                if (tier == EnemyTier.Elite) { e.attackDamage = 20; e.rewardXP = 140; e.rewardCoins = 25; }
                list.Add(e); return e;
            }

            // Code City
            E("bug_monster", "BUG MONSTER", "CodeCity", EnemyTier.Normal, "blob", new Color(0.4f, 1f, 0.3f), 1.1f, new[] { "Loops", "Conditions" },
                new[] { "OFF-BY-ONE", "INFINITE NIBBLE", "STACK OVERFLOW SLAP" }, new[] { "YOU CAN'T FIX MY CODE!", "I'M NOT A BUG, I'M A FEATURE!", "404: YOUR CHANCES NOT FOUND!" });
            E("syntax_slime", "SYNTAX SLIME", "CodeCity", EnemyTier.Normal, "spiky", new Color(1f, 0.5f, 0.1f), 1f, new[] { "Programming Basics", "Variables", "Data Types" },
                new[] { "MISSING COLON", "INDENT INVASION", "TYPE ERROR TACKLE" }, new[] { "Your indentation is WRONG and so are YOU!", "I eat colons for breakfast!" });
            E("loop_drone", "LOOP DRONE", "CodeCity", EnemyTier.Normal, "robot", new Color(0.3f, 0.8f, 1f), 1.2f, new[] { "Loops", "Boolean Logic", "Binary" },
                new[] { "WHILE TRUE BEAM", "BIT FLIP" }, new[] { "BEEP. BEEP. BEEP. BEEP.", "01011001 01001111 01010101!" }).flying = true;
            E("crypto_cracker", "CRYPTO CRACKER", "CodeCity", EnemyTier.Elite, "robot", new Color(1f, 0.2f, 0.6f), 1.8f, new[] { "Cybersecurity", "Algorithms", "Binary" },
                new[] { "PHISHING HOOK", "BRUTE FORCE BARRAGE" }, new[] { "Your password is 'password', isn't it?", "I've already guessed it. Probably." }, 300, 3);
            E("off_by_one", "OFF-BY-ONE", "CodeCity", EnemyTier.Elite, "spiky", new Color(0.9f, 1f, 0.2f), 1.6f, new[] { "Loops", "Algorithms" },
                new[] { "OFF-BY-ONE", "FENCEPOST FURY" }, new[] { "There are TEN fence posts! Or eleven. Or nine.", "I always stop one step too early. Or late!" }, 300, 3);

            // Physics Valley
            E("gravity_goon", "GRAVITY GOON", "PhysicsValley", EnemyTier.Normal, "crystal", new Color(0.7f, 0.4f, 1f), 1.4f, new[] { "Gravitation", "Motion" },
                new[] { "UPWARD FALL", "ROCK DROP (SIDEWAYS)" }, new[] { "What goes up... stays up! HA!", "Down is a matter of opinion!" });
            E("static_spark", "STATIC SPARK", "PhysicsValley", EnemyTier.Normal, "ghost", new Color(1f, 0.9f, 0.3f), 1.1f, new[] { "Electricity", "Magnetism" },
                new[] { "SHORT CIRCUIT", "ZAP OF OHM" }, new[] { "I resist you! Literally! Six ohms!", "Feeling... charged?" }).flying = true;
            E("momentum_mite", "MOMENTUM MITE", "PhysicsValley", EnemyTier.Normal, "blob", new Color(1f, 0.55f, 0.2f), 0.9f, new[] { "Newton's Laws", "Work & Energy" },
                new[] { "INERTIA SLAM", "UNBALANCED FORCE" }, new[] { "An object at rest stays AT REST! Like me! Forever!", "F = ME, A!" });
            E("tesla_wraith", "TESLA WRAITH", "PhysicsValley", EnemyTier.Elite, "ghost", new Color(0.4f, 0.8f, 1f), 2f, new[] { "Electricity", "Magnetism", "Waves" },
                new[] { "INDUCED CURRENT", "RESONANCE SCREAM" }, new[] { "I am the potential difference you fear!", "Waves of PAIN, frequency: HIGH!" }, 300, 3).flying = true;

            // Chemical Chaos (element creatures)
            E("hydrogen_hopper", "HYDROGEN HOPPER", "ChemicalChaos", EnemyTier.Normal, "blob", new Color(0.95f, 0.95f, 1f), 0.8f, new[] { "Atoms", "Molecules" },
                new[] { "LONE PROTON PUNCH", "FLAMMABLE FLOP" }, new[] { "I'm the LIGHTEST and the BRIGHTEST!", "Bond? With YOU? Never!" });
            E("oxy_brute", "OXY BRUTE", "ChemicalChaos", EnemyTier.Normal, "spiky", new Color(1f, 0.2f, 0.2f), 1.3f, new[] { "Bonding", "Reactions" },
                new[] { "OXIDISE!", "RUST RUSH" }, new[] { "I need TWO electrons and I want them NOW!", "Everything rusts eventually!" });
            E("carbon_clump", "CARBON CLUMP", "ChemicalChaos", EnemyTier.Normal, "crystal", new Color(0.15f, 0.15f, 0.18f), 1.2f, new[] { "Bonding", "Molecules", "Elements" },
                new[] { "FOUR-BOND FURY", "DIAMOND HARD HEADBUTT" }, new[] { "I could be a DIAMOND. I choose violence.", "Four bonds and NOT ONE of them for you!" });
            E("sodium_sprinter", "SODIUM SPRINTER", "ChemicalChaos", EnemyTier.Normal, "blob", new Color(1f, 0.85f, 0.2f), 0.9f, new[] { "Periodic Table", "Reactions" },
                new[] { "SPLASH REACTION", "ALKALI ATTACK" }, new[] { "Just add water and STAND BACK!", "One spare electron and NO chill!" });
            E("acid_alchemist", "ACID ALCHEMIST", "ChemicalChaos", EnemyTier.Elite, "ghost", new Color(0.4f, 1f, 0.3f), 1.9f, new[] { "Equations", "Reactions", "Molecules" },
                new[] { "UNBALANCED EQUATION", "CORROSIVE CLOUD" }, new[] { "Mass is NOT conserved in MY lab!", "Balance? I've never heard of her." }, 300, 3);

            // Bio-Zone
            E("virus_swarmling", "VIRUS SWARMLING", "BioZone", EnemyTier.Normal, "virus", new Color(0.7f, 0.2f, 1f), 0.9f, new[] { "Cells", "Human Body" },
                new[] { "REPLICATE!", "SPIKE PROTEIN POKE" }, new[] { "We are many. You are one cell-sized idiot.", "Copy, copy, copy, copy!" }).flying = true;
            E("rogue_cell", "ROGUE CELL", "BioZone", EnemyTier.Normal, "cell", new Color(1f, 0.4f, 0.5f), 1.1f, new[] { "Cells", "Genetics" },
                new[] { "MUTATION SLAP", "UNCONTROLLED DIVISION" }, new[] { "My DNA has... opinions now.", "Mitosis? More like MY-tosis!" });
            E("plaque_puff", "PLAQUE PUFF", "BioZone", EnemyTier.Normal, "blob", new Color(1f, 0.9f, 0.5f), 1f, new[] { "Circulation", "Respiration" },
                new[] { "ARTERY JAM", "CHOLESTEROL CHOMP" }, new[] { "Traffic jam in the aorta! HA!", "I'm just a little... clog." });
            E("antibody_imposter", "ANTIBODY IMPOSTER", "BioZone", EnemyTier.Elite, "virus", new Color(1f, 1f, 1f), 1.8f, new[] { "Human Body", "Circulation", "Ecosystems" },
                new[] { "FRIENDLY FIRE", "AUTOIMMUNE AMBUSH" }, new[] { "Trust me, I'm a white blood cell.", "The body attacks... ITSELF! Thanks to me!" }, 300, 3);

            // The Core
            E("glitch_fragment", "GLITCH FRAGMENT", "TheCore", EnemyTier.Normal, "crystal", new Color(1f, 1f, 1f), 1.4f, new string[0],
                new[] { "REGION SWAP", "NULL STRIKE" }, new[] { "CODE? PHYSICS? CHEMISTRY? BIOLOGY? YES.", "I AM EVERY ERROR AT ONCE." });
            E("source_shard", "SOURCE SHARD", "TheCore", EnemyTier.Elite, "mech", new Color(1f, 0.3f, 0.9f), 2.2f, new string[0],
                new[] { "TOTAL RECALL", "CROSS-SUBJECT CRUSH" }, new[] { "Everything you learned... I will make you USE.", "Synthesis is SUFFERING." }, 300, 3);
            return list;
        }

        // ------------------------------------------------------------------ bosses (Section 6.2 + 10.1)
        static BossDataSO Boss(string id, string name, string arc, string concept, string shape, Color tint, float h, bool mini, string profile, List<CharacterProfileSO> chars)
        {
            var b = ScriptableObject.CreateInstance<BossDataSO>();
            b.id = id; b.name = "Boss_" + id; b.displayName = name; b.arc = arc; b.bossConcept = concept; b.placeholderShape = shape; b.tint = tint; b.height = h;
            b.tier = mini ? EnemyTier.MiniBoss : EnemyTier.Boss;
            b.maxHP = mini ? 300 : 500;
            b.attackDamage = mini ? 20 : 25;
            b.rewardXP = mini ? 250 : 500; b.rewardCoins = mini ? 40 : 100;
            if (profile != null) b.profile = chars.Find(c => c.id == profile);
            if (mini)
            {
                b.phases = new List<BossPhase>
                {
                    new BossPhase { name = "WARM-UP", difficulty = 1, damage = 80 },
                    new BossPhase { name = "ANALYSIS", difficulty = 2, damage = 100, types = new List<ChallengeType> { ChallengeType.Debugging, ChallengeType.Matching, ChallengeType.DragDrop } },
                    new BossPhase { name = "FINAL BLOW", difficulty = 3, damage = 0 },
                };
            }
            else
            {
                b.phases = new List<BossPhase>
                {
                    new BossPhase { name = "01 RECALL", difficulty = 1, damage = 50, types = new List<ChallengeType> { ChallengeType.Logic, ChallengeType.Prediction, ChallengeType.Matching } },
                    new BossPhase { name = "02 ANALYSIS", difficulty = 2, damage = 75, types = new List<ChallengeType> { ChallengeType.Debugging, ChallengeType.DragDrop, ChallengeType.Prediction } },
                    new BossPhase { name = "03 REASONING", difficulty = 3, damage = 75, types = new List<ChallengeType> { ChallengeType.Logic, ChallengeType.Numerical, ChallengeType.Prediction } },
                    new BossPhase { name = "04 APPLICATION", difficulty = 4, damage = 100, types = new List<ChallengeType> { ChallengeType.Simulation, ChallengeType.Coding, ChallengeType.Construction, ChallengeType.DragDrop } },
                    new BossPhase { name = "05 SYNTHESIS: THE FINAL TEST", difficulty = 5, damage = 0 },
                };
            }
            return b;
        }

        // ------------------------------------------------------------------ arcs (Section 10.4)
        public static List<ArcDataSO> CreateArcs(GameDatabase db)
        {
            var chars = db.characters;
            var arcs = new List<ArcDataSO>();

            ArcDataSO A(string id, string name, string subject, int order, string tagline, string[] concepts, ChallengeType[] types, string ability, string cosmetic)
            {
                var a = ScriptableObject.CreateInstance<ArcDataSO>();
                a.id = id; a.name = "Arc_" + id; a.displayName = name; a.subject = subject; a.order = order; a.tagline = tagline;
                a.concepts = new List<string>(concepts); a.emphasisTypes = new List<ChallengeType>(types);
                a.abilityReward = ability; a.cosmeticReward = cosmetic;
                a.theme = CreateTheme(id);
                arcs.Add(a); return a;
            }

            void Levels(ArcDataSO a, string l1, string l2, string boss, bool hasHub = true, string run = null)
            {
                if (hasHub) a.levels.Add(new LevelInfo { id = a.id + "_Hub", scene = a.id + "_Hub", displayName = a.displayName + " Hub", kind = LevelKind.Hub, next = a.id + "_L1" });
                a.levels.Add(new LevelInfo { id = a.id + "_L1", scene = a.id + "_L1", displayName = l1, kind = LevelKind.Level, next = string.IsNullOrEmpty(l2) ? a.id + "_Boss" : a.id + "_L2" });
                if (!string.IsNullOrEmpty(l2))
                    a.levels.Add(new LevelInfo { id = a.id + "_L2", scene = a.id + "_L2", displayName = l2, kind = LevelKind.Level, next = a.id + "_Boss" });
                a.levels.Add(new LevelInfo { id = a.id + "_Boss", scene = a.id + "_Boss", displayName = boss, kind = LevelKind.BossArena, next = "" });
                // the parkour side level (opened from the hub at any time)
                if (!string.IsNullOrEmpty(run)) a.levels.Add(new LevelInfo { id = a.id + "_Run", scene = a.id + "_Run", displayName = run, kind = LevelKind.Level, next = "" });
            }

            // Arc 1: Code City
            var cc = A("CodeCity", "Code City", "Computer Science", 0, "A city run by a programming error. Doors open at random, robots walk into walls, lifts go nowhere.",
                new[] { "Programming Basics", "Variables", "Conditions", "Loops", "Boolean Logic", "Binary", "Algorithms", "Data Types", "Cybersecurity" },
                new[] { ChallengeType.Debugging, ChallengeType.Coding, ChallengeType.Logic }, "double_jump", "binary_halo");
            Levels(cc, "Circuit Streets", Prototype ? null : "Server Spires", "The Infinite Core");
            cc.concepts.InsertRange(0, new[] { "Boolean Logic", "Python Basics", "Flow of Control", "Number Systems", "Strings & Lists" });
            cc.miniBoss = Boss("sergeant_syntax", "SERGEANT SYNTAX", "CodeCity", "Conditions", "robot", new Color(1f, 0.3f, 0.2f), 2.4f, true, null, chars);
            cc.miniBoss.introLines = new List<string> { "ATTENTION, RECRUIT! Your COLONS are a DISGRACE!", "Drop and give me twenty... lines of VALID PYTHON!" };
            cc.miniBoss.taunts = new List<string> { "IS THAT INDENTED? IS IT?!", "I've seen better code written by a TOASTER!" };
            cc.miniBoss.finalDefeatLine = "At ease... recruit... your syntax... is... valid...";
            cc.boss = Boss("lord_infinite_loop", "LORD INFINITE LOOP", "CodeCity", "Loops", "mech", new Color(0.2f, 0.9f, 1f), 5f, false, "lord_loop", chars);
            cc.boss.introLines = new List<string> { "WELCOME, WELCOME, WELCOME, little process.", "I have been running since the beginning of time. Time. Time. TIME.", "You will never break my loop. Loop. LOOP." };
            cc.boss.attackNames = new List<string> { "WHILE TRUE", "RECURSION CRUSH", "STACK OVERFLOW", "INFINITE ECHO" };
            cc.boss.taunts = new List<string> { "AGAIN. AGAIN. AGAIN.", "You will repeat this fight FOREVER-EVER-EVER." };
            cc.boss.hintTaunts = new List<string> { "Ha-ha-ha. Look at THAT line. What changes each time? NOTHING. Like you.", "Your loop never ENDS. Just like my monologue." };
            cc.boss.teachingLines = new List<string> { "*sigh* Fine. FINE. Fine. Let me show you how a loop works, since you clearly can't.", "Every loop needs three things: a START, a CONDITION, and a CHANGE. Watch what happens when I remove the change.", "See? It never stops. That's ME. That's my whole tragic life. Now YOU fix it." };
            cc.arrivalLines = new List<string> { "Code City. Population: a lot of very confused robots.", "Every NPC here is stuck repeating the same line. Forever." };
            cc.restoredLines = new List<string> { "The lifts work! The doors close! Somebody finally finished their sentence!", "Code City is BACK ONLINE." };

            // Arc 2: Physics Valley
            var pv = A("PhysicsValley", "Physics Valley", "Physics", 1, "The laws themselves are corrupted. Gravity changes direction without warning and time runs strangely.",
                new[] { "Motion", "Newton's Laws", "Work & Energy", "Gravitation", "Electricity", "Magnetism", "Waves" },
                new[] { ChallengeType.Simulation, ChallengeType.Numerical, ChallengeType.Prediction }, "dash", "propeller");
            Levels(pv, "Floating Isles", Prototype ? null : "Tesla Ridge", "The Courtroom of Broken Laws");
            pv.concepts.InsertRange(0, new[] { "Laws of Motion", "Kinematics", "Work, Energy and Power", "Oscillations", "Gravitation" });
            pv.miniBoss = Boss("baron_friction", "BARON VON FRICTION", "PhysicsValley", "Work & Energy", "crystal", new Color(1f, 0.6f, 0.2f), 2.4f, true, null, chars);
            pv.miniBoss.introLines = new List<string> { "I am Baron Von Friction! I oppose ALL motion!", "...why am I sliding? WHO REMOVED MY FRICTION?" };
            pv.miniBoss.finalDefeatLine = "I'm... grinding... to a... halt...";
            pv.boss = Boss("lord_broken_laws", "THE LORD OF BROKEN LAWS", "PhysicsValley", "Newton's Laws", "crystal", new Color(0.6f, 0.3f, 1f), 5f, false, "lord_laws", chars);
            pv.boss.introLines = new List<string> { "ORDER! ORDER IN MY VALLEY!", "I have declared gravity ILLEGAL on Tuesdays.", "And today, little student, is whatever day I SAY it is." };
            pv.boss.attackNames = new List<string> { "GRAVITY REVERSAL", "INFINITE ACCELERATION", "TIME DILATION SMACK", "UNBALANCED VERDICT" };
            pv.boss.taunts = new List<string> { "Gravity is a SUGGESTION.", "Newton? Never heard of him. Overruled!" };
            pv.boss.hintTaunts = new List<string> { "Pathetic! You forgot the NET force. Forces in opposite directions SUBTRACT, fool.", "Objection! F = ma. You used F = m + a. I'm almost impressed." };
            pv.boss.teachingLines = new List<string> { "*sigh* This court is now a classroom. Against my will.", "Here. Sliders. Push the force up and watch the acceleration. Now add mass. See? Heavier things are LAZIER.", "Change ONE thing at a time. That's how real physicists... ugh... learn. Now go away. No, wait, come back and fight me." };
            pv.arrivalLines = new List<string> { "Physics Valley. Watch your step. Also your up-step. And your sideways-step.", "The debris here falls UP. Don't think about it too hard." };
            pv.restoredLines = new List<string> { "Things fall DOWN again! Beautiful, boring, correct DOWN!", "Physics Valley obeys the law once more." };

            // Arc 3: Chemical Chaos
            var ch = A("ChemicalChaos", "Chemical Chaos", "Chemistry", 2, "A vast corrupted laboratory where the elements have become creatures with opinions about what they will bond with.",
                new[] { "Atoms", "Elements", "Periodic Table", "Molecules", "Reactions", "Equations", "Bonding" },
                new[] { ChallengeType.Construction, ChallengeType.Matching, ChallengeType.DragDrop }, "wall_jump", "goggles");
            Levels(ch, "Beaker Towers", Prototype ? null : "Acid Works", "The Grand Reactor");
            ch.concepts.InsertRange(0, new[] { "Mole Concept", "Structure of Atom", "Chemical Bonding", "Periodic Trends", "Redox Reactions" });
            ch.miniBoss = Boss("count_catalyst", "COUNT CATALYST", "ChemicalChaos", "Reactions", "ghost", new Color(0.4f, 1f, 0.6f), 2.6f, true, null, chars);
            ch.miniBoss.introLines = new List<string> { "IspeedEVERYTHINGupIamCountCatalystFIGHTmeQUICKLY!", "...I'm not even used up by the reaction. Unlike you." };
            ch.miniBoss.finalDefeatLine = "Slowing... down... is this... equilibrium...?";
            ch.boss = Boss("lord_of_the_lab", "THE LORD OF THE LAB", "ChemicalChaos", "Bonding", "mech", new Color(0.3f, 1f, 0.4f), 5f, false, "lord_lab", chars);
            ch.boss.introLines = new List<string> { "Welcome to my LAB-oratory! The 'oratory' is where I give speeches.", "I taught every element to REFUSE to bond. No more water! No more salt! Only CHAOS!", "Chemistry? I prefer CHAOS-try." };
            ch.boss.attackNames = new List<string> { "IONIC BREAKUP", "COVALENT CANCEL", "NOBLE SNUB", "EXOTHERMIC EXPLOSION" };
            ch.boss.taunts = new List<string> { "Bond? BOND? Not in MY lab!", "Mix it, shake it, watch it EXPLODE!" };
            ch.boss.hintTaunts = new List<string> { "Count the valence electrons, genius. Oxygen wants TWO. Not one. TWO.", "Sodium GIVES. Chlorine TAKES. It's practically a soap opera." };
            ch.boss.teachingLines = new List<string> { "*sigh* Fine. Lab safety lecture. Goggles ON.", "Atoms bond to fill their outer shell. Drag some together. Watch them... ugh... get along.", "See how carbon grabs four hydrogens? Disgusting teamwork. Now try it yourself." };
            ch.arrivalLines = new List<string> { "Chemical Chaos. Don't lick anything.", "Those element creatures used to be the periodic table. Now they're... the periodic TANTRUM." };
            ch.restoredLines = new List<string> { "Everything's bonding again! Water is WET again!", "Chemical Chaos is now just... Chemistry. Neat and tidy." };

            // Arc 4: Bio-Zone
            var bz = A("BioZone", "Bio-Zone", "Biology", 3, "Shrunk to microscopic scale and dropped inside a living organism, where the corruption is attacking the systems that keep it alive.",
                new[] { "Cells", "Human Body", "Genetics", "Respiration", "Circulation", "Ecosystems" },
                new[] { ChallengeType.Matching, ChallengeType.ShortAnswer, ChallengeType.Simulation }, "shrink", "duck");
            Levels(bz, "Vessel Rapids", Prototype ? null : "The Breathing Caverns", "The Heart of the Matter");
            bz.concepts.InsertRange(0, new[] { "Cell: The Unit of Life", "Breathing and Exchange of Gases", "Body Fluids and Circulation", "Neural Control", "Cell Cycle and Cell Division" });
            bz.miniBoss = Boss("professor_plaque", "PROFESSOR PLAQUE", "BioZone", "Circulation", "blob", new Color(1f, 0.9f, 0.4f), 2.4f, true, null, chars);
            bz.miniBoss.introLines = new List<string> { "Ah, a student! I am Professor Plaque. I specialise in... blockages.", "Class is in session. Permanently. Nobody gets through this artery." };
            bz.miniBoss.finalDefeatLine = "Dissolving... like a good... medication...";
            bz.boss = Boss("lord_within", "THE LORD WITHIN", "BioZone", "Circulation", "virus", new Color(0.8f, 0.1f, 0.3f), 5f, false, "lord_within", chars);
            bz.boss.introLines = new List<string> { "Oh! A visitor. How lovely. Mind the blood.", "I've made myself quite comfortable in this body. The heart is SUCH a lovely room.", "Make yourself at home. I did." };
            bz.boss.attackNames = new List<string> { "CLOT", "OXYGEN THEFT", "VALVE REVERSAL", "SYSTEM SHUTDOWN" };
            bz.boss.taunts = new List<string> { "Shhh. The body is sleeping. Forever.", "Such a fragile little system. Pumps, pipes, tubes..." };
            bz.boss.hintTaunts = new List<string> { "Arteries go AWAY, darling. A for away. Honestly.", "Right side of the heart: used blood, to the LUNGS. Did no one tell you?" };
            bz.boss.teachingLines = new List<string> { "*sigh* Very well. A tour of the facilities.", "Trace the blood with your finger. Body, right heart, lungs, left heart, body. Round and round.", "There. Now you understand my house better than I do. How annoying." };
            bz.arrivalLines = new List<string> { "We're... inside someone. Don't be weird about it.", "That pulsing? That's the heart. The Lord Within has moved in." };
            bz.restoredLines = new List<string> { "The heart's beating steady! The organism is HEALTHY!", "Bio-Zone restored. The patient will make a full recovery." };

            // Final: The Core
            var core = A("TheCore", "The Core", "Everything", 4, "A fractured mix of all four regions. The region-swap glitch never stops.",
                new[] { "Synthesis" }, new[] { ChallengeType.Coding, ChallengeType.Simulation, ChallengeType.Construction, ChallengeType.Matching }, "", "crown");
            Levels(core, "The Fractured Nexus", "", "The Source", hasHub: false);
            core.boss = Boss("the_source", "THE SOURCE", "TheCore", "Synthesis", "mech", new Color(1f, 1f, 1f), 6f, false, "the_source", chars);
            core.boss.introLines = new List<string> { "So. You fixed my loops. My laws. My lab. My little body.", "I WROTE the rules of The Nexus. Then I deleted them. It was fun.", "Let's see if you understand ALL of it. At once." };
            core.boss.attackNames = new List<string> { "REGION SWAP", "TOTAL CORRUPTION", "NULL REALITY", "THE LAST GLITCH" };
            core.boss.taunts = new List<string> { "Code? Physics? Chemistry? Biology? They're all just rules. And I BREAK rules.", "Knowledge is your weapon? Then I'll make you DROP it." };
            core.boss.hintTaunts = new List<string> { "Everything connects, student. Physics runs the code. Chemistry runs the cell.", "You separate subjects into boxes. I don't. That's why I'm winning." };
            core.boss.teachingLines = new List<string> { "*sigh* Even gods must explain themselves sometimes.", "Look: a loop can simulate a cell dividing. Physics tells the robot how fast. Chemistry fuels the muscle.", "It's ONE world. You just learned it in four pieces. Now put it together." };
            core.boss.finalDefeatLine = "NO... YOU UNDERSTOOD... EVERYTHING... But I was only the program. My AUTHOR is still out there. His name... is KANHA.";
            core.boss.phases[0].concept = ""; // phases draw from every arc (see Boss.cs)

            foreach (var a in arcs) { var seen = new HashSet<string>(); a.concepts.RemoveAll(c => !seen.Add(c)); }   // the Class XI topics overlap a few old concepts
            return arcs;
        }

        // ------------------------------------------------------------------ region palettes (asset prompt, Step 3.4)
        /// <summary>Realistic city lighting, tuned for colour: a vivid neon-sunset dusk while corrupted -> bright,
        /// saturated blue-sky daylight once restored. Ambient carries real colour so shadows are never grey.</summary>
        public static void ApplyRealisticTheme(RegionThemeSO t, string arcId = "CodeCity")
        {
            // Code City: golden sunset haze with blue shadows and neon windows -> crisp blue noon
            t.fogCorrupted = new Color(0.8f, 0.64f, 0.52f);
            t.ambientSkyCorrupted = new Color(0.4f, 0.48f, 0.8f);
            t.ambientEquatorCorrupted = new Color(0.6f, 0.5f, 0.44f);
            t.ambientGroundCorrupted = new Color(0.22f, 0.18f, 0.15f);
            t.sunCorrupted = new Color(1f, 0.6f, 0.36f);
            t.sunIntensityCorrupted = 2.3f;
            t.skyTintCorrupted = new Color(1.02f, 0.94f, 0.92f);
            t.accentCorrupted = new Color(1f, 0.25f, 0.8f);
            t.fogRestored = new Color(0.6f, 0.78f, 1f);
            t.ambientSkyRestored = new Color(0.5f, 0.68f, 1.05f);
            t.ambientEquatorRestored = new Color(0.6f, 0.6f, 0.56f);
            t.ambientGroundRestored = new Color(0.34f, 0.29f, 0.22f);
            t.sunRestored = new Color(1f, 0.93f, 0.8f);
            t.sunIntensityRestored = 2.7f;
            t.skyTintRestored = new Color(0.95f, 1f, 1.08f);
            t.accentRestored = new Color(0.25f, 0.9f, 1f);
            t.sunEuler = new Vector3(28f, -38f, 0f);
            t.fogStart = 70f; t.fogEnd = 1000f; t.fogDensity = 0.0016f;
            t.skyExposure = 1.1f;
            t.ambientParticles = "dust";
            if (arcId == "PhysicsValley")
            {
                // blazing amber sunset over the valley -> bright alpine morning
                t.fogCorrupted = new Color(0.92f, 0.76f, 0.62f);
                t.ambientSkyCorrupted = new Color(0.46f, 0.56f, 0.88f);
                t.ambientEquatorCorrupted = new Color(0.72f, 0.48f, 0.34f);
                t.ambientGroundCorrupted = new Color(0.28f, 0.2f, 0.14f);
                t.sunCorrupted = new Color(1f, 0.64f, 0.34f);
                t.sunIntensityCorrupted = 2.4f;
                t.skyTintCorrupted = new Color(1.05f, 0.9f, 0.82f);
                t.accentCorrupted = new Color(1f, 0.55f, 0.15f);
                t.fogRestored = new Color(0.66f, 0.82f, 1f);
                t.ambientSkyRestored = new Color(0.5f, 0.68f, 1f);
                t.ambientEquatorRestored = new Color(0.58f, 0.62f, 0.45f);
                t.sunRestored = new Color(1f, 0.95f, 0.82f);
                t.accentRestored = new Color(1f, 0.8f, 0.3f);
                t.sunEuler = new Vector3(24f, -50f, 0f);
                t.fogEnd = 1500f; t.fogDensity = 0.0011f;
            }
            if (arcId == "ChemicalChaos")
            {
                // toxic emerald haze over the plant -> clean bright daylight
                t.fogCorrupted = new Color(0.42f, 0.7f, 0.54f);
                t.ambientSkyCorrupted = new Color(0.38f, 0.62f, 0.58f);
                t.ambientEquatorCorrupted = new Color(0.42f, 0.58f, 0.3f);
                t.sunCorrupted = new Color(0.9f, 1f, 0.55f);
                t.skyTintCorrupted = new Color(0.78f, 1.05f, 0.82f);
                t.accentCorrupted = new Color(0.45f, 1f, 0.3f);
                t.fogRestored = new Color(0.68f, 0.84f, 1f);
                t.accentRestored = new Color(0.35f, 0.8f, 1f);
                t.fogDensity = 0.002f;
            }
            if (arcId == "BioZone")
            {
                // coral-red emergency glow in the research centre -> fresh, sunny daylight
                t.fogCorrupted = new Color(0.7f, 0.42f, 0.5f);
                t.ambientSkyCorrupted = new Color(0.7f, 0.42f, 0.56f);
                t.ambientEquatorCorrupted = new Color(0.66f, 0.38f, 0.4f);
                t.sunCorrupted = new Color(1f, 0.6f, 0.52f);
                t.skyTintCorrupted = new Color(1.05f, 0.82f, 0.88f);
                t.accentCorrupted = new Color(1f, 0.25f, 0.35f);
                t.fogRestored = new Color(0.7f, 0.86f, 1f);
                t.ambientSkyRestored = new Color(0.52f, 0.72f, 1f);
                t.ambientEquatorRestored = new Color(0.55f, 0.65f, 0.5f);
                t.accentRestored = new Color(0.4f, 0.85f, 1f);
                t.sunEuler = new Vector3(58f, -30f, 0f);
                t.fogDensity = 0.0018f;
            }
            if (arcId == "TheCore")
            {
                // deep violet neon night -> a golden sunrise after the glitch
                t.fogCorrupted = new Color(0.16f, 0.12f, 0.3f);
                t.ambientSkyCorrupted = new Color(0.3f, 0.26f, 0.6f);
                t.ambientEquatorCorrupted = new Color(0.36f, 0.18f, 0.44f);
                t.ambientGroundCorrupted = new Color(0.1f, 0.07f, 0.14f);
                t.sunCorrupted = new Color(0.55f, 0.62f, 1f);
                t.sunIntensityCorrupted = 1.1f;
                t.skyTintCorrupted = new Color(0.9f, 0.85f, 1.15f);
                t.accentCorrupted = new Color(0.9f, 0.3f, 1f);
                t.fogRestored = new Color(1f, 0.72f, 0.55f);
                t.ambientSkyRestored = new Color(0.6f, 0.62f, 0.9f);
                t.ambientEquatorRestored = new Color(0.82f, 0.58f, 0.46f);
                t.sunRestored = new Color(1f, 0.76f, 0.5f);
                t.sunIntensityRestored = 2.4f;
                t.sunEuler = new Vector3(12f, -60f, 0f);
                t.fogEnd = 1800f; t.fogDensity = 0.0009f;
            }
        }

        public static RegionThemeSO CreateTheme(string id)
        {
            var t = ScriptableObject.CreateInstance<RegionThemeSO>();
            t.id = id; t.name = "Theme_" + id;
            switch (id)
            {
                case "ExamHall":   // Kanha the Dard's exam hall: late-afternoon sun, warm dust; the Glitch adds a red cast
                    t.fogCorrupted = new Color(0.5f, 0.32f, 0.3f); t.ambientSkyCorrupted = new Color(0.64f, 0.58f, 0.7f); t.ambientEquatorCorrupted = new Color(0.62f, 0.46f, 0.42f); t.ambientGroundCorrupted = new Color(0.28f, 0.18f, 0.15f);
                    t.sunCorrupted = new Color(1f, 0.7f, 0.55f); t.accentCorrupted = new Color(1f, 0.22f, 0.32f); t.skyTintCorrupted = new Color(1f, 0.6f, 0.55f);
                    t.fogRestored = new Color(0.62f, 0.52f, 0.42f); t.ambientSkyRestored = new Color(0.66f, 0.76f, 0.9f); t.ambientEquatorRestored = new Color(0.62f, 0.56f, 0.48f); t.ambientGroundRestored = new Color(0.3f, 0.22f, 0.16f);
                    t.sunRestored = new Color(1f, 0.84f, 0.62f); t.accentRestored = new Color(0.25f, 0.9f, 1f);
                    t.ambientParticles = "dust"; t.musicKey = "boss"; t.fogEnd = 90;
                    break;
                case "CodeCity":
                    t.fogCorrupted = new Color(0.05f, 0.06f, 0.16f); t.ambientSkyCorrupted = new Color(0.22f, 0.22f, 0.5f); t.ambientEquatorCorrupted = new Color(0.25f, 0.1f, 0.35f); t.ambientGroundCorrupted = new Color(0.04f, 0.05f, 0.1f);
                    t.sunCorrupted = new Color(0.55f, 0.6f, 1f); t.accentCorrupted = new Color(1f, 0.2f, 0.85f); t.skyTintCorrupted = new Color(0.55f, 0.45f, 1f);
                    t.fogRestored = new Color(0.66f, 0.8f, 0.98f); t.ambientSkyRestored = new Color(0.62f, 0.76f, 1f); t.ambientEquatorRestored = new Color(0.62f, 0.62f, 0.62f); t.ambientGroundRestored = new Color(0.35f, 0.32f, 0.3f);
                    t.sunRestored = new Color(1f, 0.95f, 0.84f); t.accentRestored = new Color(0.25f, 0.9f, 1f);
                    t.ambientParticles = "datamotes"; t.musicKey = "explore_CodeCity"; t.fogEnd = 460;
                    break;
                case "PhysicsValley":
                    t.fogCorrupted = new Color(0.25f, 0.12f, 0.3f); t.ambientSkyCorrupted = new Color(0.5f, 0.3f, 0.6f); t.ambientEquatorCorrupted = new Color(0.6f, 0.3f, 0.25f); t.ambientGroundCorrupted = new Color(0.15f, 0.08f, 0.15f);
                    t.sunCorrupted = new Color(1f, 0.5f, 0.3f); t.accentCorrupted = new Color(1f, 0.5f, 0.1f); t.skyTintCorrupted = new Color(0.9f, 0.55f, 1f);
                    t.fogRestored = new Color(0.72f, 0.85f, 0.8f); t.ambientSkyRestored = new Color(0.6f, 0.8f, 0.95f); t.ambientEquatorRestored = new Color(0.6f, 0.65f, 0.45f); t.ambientGroundRestored = new Color(0.3f, 0.35f, 0.2f);
                    t.sunRestored = new Color(1f, 0.9f, 0.65f); t.accentRestored = new Color(1f, 0.8f, 0.3f);
                    t.ambientParticles = "dust"; t.musicKey = "explore_PhysicsValley"; t.fogEnd = 520;
                    break;
                case "ChemicalChaos":
                    t.fogCorrupted = new Color(0.04f, 0.16f, 0.15f); t.ambientSkyCorrupted = new Color(0.15f, 0.45f, 0.4f); t.ambientEquatorCorrupted = new Color(0.2f, 0.5f, 0.15f); t.ambientGroundCorrupted = new Color(0.03f, 0.1f, 0.08f);
                    t.sunCorrupted = new Color(0.6f, 1f, 0.5f); t.accentCorrupted = new Color(0.4f, 1f, 0.2f); t.skyTintCorrupted = new Color(0.4f, 0.9f, 0.7f);
                    t.fogRestored = new Color(0.85f, 0.92f, 0.97f); t.ambientSkyRestored = new Color(0.85f, 0.9f, 1f); t.ambientEquatorRestored = new Color(0.75f, 0.8f, 0.85f); t.ambientGroundRestored = new Color(0.5f, 0.55f, 0.6f);
                    t.sunRestored = new Color(0.95f, 0.97f, 1f); t.accentRestored = new Color(0.4f, 0.7f, 1f);
                    t.ambientParticles = "bubbles"; t.musicKey = "explore_ChemicalChaos"; t.fogEnd = 380;
                    break;
                case "BioZone":
                    t.fogCorrupted = new Color(0.3f, 0.02f, 0.06f); t.ambientSkyCorrupted = new Color(0.6f, 0.15f, 0.25f); t.ambientEquatorCorrupted = new Color(0.45f, 0.05f, 0.12f); t.ambientGroundCorrupted = new Color(0.15f, 0.02f, 0.05f);
                    t.sunCorrupted = new Color(1f, 0.35f, 0.4f); t.accentCorrupted = new Color(0.8f, 0.2f, 1f); t.skyTintCorrupted = new Color(1f, 0.3f, 0.4f);
                    t.fogRestored = new Color(1f, 0.72f, 0.78f); t.ambientSkyRestored = new Color(1f, 0.8f, 0.85f); t.ambientEquatorRestored = new Color(0.9f, 0.6f, 0.65f); t.ambientGroundRestored = new Color(0.55f, 0.3f, 0.35f);
                    t.sunRestored = new Color(1f, 0.92f, 0.9f); t.accentRestored = new Color(1f, 0.55f, 0.7f);
                    t.ambientParticles = "cells"; t.musicKey = "explore_BioZone"; t.fogStart = 20; t.fogEnd = 240; t.fogDensity = 0.012f;
                    break;
                case "TheCore":
                    t.fogCorrupted = new Color(0.01f, 0.01f, 0.03f); t.ambientSkyCorrupted = new Color(0.2f, 0.2f, 0.3f); t.ambientEquatorCorrupted = new Color(0.25f, 0.05f, 0.25f); t.ambientGroundCorrupted = new Color(0.02f, 0.02f, 0.05f);
                    t.sunCorrupted = new Color(0.8f, 0.8f, 1f); t.accentCorrupted = new Color(1f, 0.2f, 0.85f); t.skyTintCorrupted = new Color(0.25f, 0.2f, 0.35f);
                    t.fogRestored = new Color(0.7f, 0.85f, 1f); t.ambientSkyRestored = new Color(0.7f, 0.8f, 1f); t.ambientEquatorRestored = new Color(0.6f, 0.6f, 0.7f); t.ambientGroundRestored = new Color(0.35f, 0.35f, 0.4f);
                    t.sunRestored = new Color(1f, 0.96f, 0.9f); t.accentRestored = new Color(0.4f, 1f, 1f);
                    t.ambientParticles = "sparks"; t.musicKey = "explore_TheCore"; t.fogEnd = 600;
                    break;
                default:
                    t.fogRestored = new Color(0.8f, 0.85f, 0.9f);
                    break;
            }
            return t;
        }

        // ------------------------------------------------------------------ cosmetics (Section 9.3)
        public static List<CosmeticSO> CreateCosmetics()
        {
            var list = new List<CosmeticSO>();
            void C(string id, string name, string hat, Color c, int price, string desc, bool reward = false, bool tint = false, Color body = default)
            {
                var s = ScriptableObject.CreateInstance<CosmeticSO>();
                s.id = id; s.name = "Cosmetic_" + id; s.displayName = name; s.hat = hat; s.hatColor = c; s.price = price; s.description = desc; s.rewardOnly = reward;
                s.tintBody = tint; s.bodyTint = body == default ? Color.white : body;
                list.Add(s);
            }
            C("default", "School Uniform", "none", Color.white, 0, "Exactly what you were wearing twenty minutes ago.");
            C("cone", "Traffic Cone of Wisdom", "cone", new Color(1f, 0.45f, 0.1f), 60, "Warning: knowledge ahead.");
            C("chef", "Chef of Algorithms", "chef", Color.white, 80, "Cooks up a mean recursive soufflé.");
            C("tophat", "Very Formal Hat", "tophat", new Color(0.1f, 0.1f, 0.12f), 120, "For debugging at the opera.");
            C("antenna", "Wi-Fi Antennae", "antenna", new Color(0.3f, 1f, 1f), 90, "Five bars of pure confidence.");
            C("neon_hoodie", "Neon Hoodie", "none", Color.white, 150, "Glows in the dark. Glows in the light. Just glows.", false, true, new Color(1f, 0.25f, 0.8f));
            C("binary_halo", "Binary Halo", "halo", new Color(0.3f, 1f, 1f), 0, "Reward: restore Code City. 01000001 01001110 01000111 01000101 01001100.", true);
            C("propeller", "Anti-Gravity Propeller Beanie", "propeller", new Color(1f, 0.85f, 0.2f), 0, "Reward: restore Physics Valley. Does not actually fly. Probably.", true);
            C("goggles", "Lab Goggles of Safety", "goggles", new Color(0.4f, 1f, 0.5f), 0, "Reward: restore Chemical Chaos. Safety first, style second, chaos never.", true);
            C("duck", "Rubber Duck Debugger", "duck", new Color(1f, 0.9f, 0.1f), 0, "Reward: restore Bio-Zone. Explain your code to the duck. It listens.", true);
            C("crown", "Crown of the Nexus", "crown", new Color(1f, 0.8f, 0.2f), 0, "Reward: defeat The Source. Knowledge is your weapon. And your hat.", true);
            return list;
        }

        // ------------------------------------------------------------------ achievements (Section 9.3)
        public static List<AchievementSO> CreateAchievements()
        {
            var list = new List<AchievementSO>();
            void A(string id, string title, string desc, string stat, int target, int coins = 25)
            {
                var a = ScriptableObject.CreateInstance<AchievementSO>();
                a.id = id; a.name = "Ach_" + id; a.title = title; a.description = desc; a.statKey = stat; a.target = target; a.coinReward = coins;
                list.Add(a);
            }
            A("bug_hunter", "Bug Hunter", "Fix 25 programming bugs.", "bugs_fixed", 25, 50);
            A("newton", "Newton Would Be Proud", "Master the physics challenges (pass the Physics Valley mastery check).", "physics_mastered", 1, 50);
            A("speed_solver", "Speed Solver", "Solve 10 challenges inside the fast time.", "fast_solves", 10);
            A("never_give_up", "Never Give Up", "Defeat a boss after failing twice.", "never_give_up", 1, 75);
            A("perfect_mind", "Perfect Mind", "Clear a boss without a single mistake.", "perfect_boss", 1, 100);
            A("combo_breaker", "Combo Breaker", "Reach a x5 combo.", "max_combo", 5);
            A("chemical_bond", "Bond. Chemical Bond.", "Build 10 molecules.", "molecules_built", 10);
            A("explorer", "Data Archaeologist", "Collect 30 data shards.", "shards", 30);
            A("fashion_glitch", "Fashion Glitch", "Buy a ridiculous costume.", "cosmetics_bought", 1);
            A("inside_job", "Inside Job", "Restore the Bio-Zone.", "restored_BioZone", 1, 50);
            A("world_saved", "Knowledge Is Your Weapon", "Defeat The Source and save The Nexus.", "game_complete", 1, 200);
            return list;
        }
    }
}
