using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// The layout of every level (1 hub + 2 levels + 1 boss arena per arc, plus The Core).
    /// Jump physics for reference: a single jump is ~3.1 m high / ~5.8 m long; double jump adds ~2.5 m;
    /// dash adds ~3 m of distance. Abilities unlock after each mini-boss and gate the rest of the arc.
    /// Edit a plan, then run Glitched -> Rebuild Scenes.
    /// </summary>
    public static class LevelPlans
    {
        static Dictionary<string, LevelPlan> plans;

        public static IEnumerable<LevelPlan> All { get { Ensure(); return plans.Values; } }
        public static LevelPlan Get(string id) { Ensure(); return plans.TryGetValue(id, out var p) ? p : null; }

        static void Ensure()
        {
            if (plans != null) return;
            plans = new Dictionary<string, LevelPlan>();
            void Add(LevelPlan p) => plans[p.levelId] = p;
            bool full = !ContentDefaults.Prototype;   // the prototype skips each chapter's second level
            Add(CodeCityHub()); Add(CodeCityL1()); if (full) Add(CodeCityL2()); Add(CodeCityBoss());
            Add(PhysicsHub()); Add(PhysicsL1()); if (full) Add(PhysicsL2()); Add(PhysicsBoss());
            Add(ChemHub()); Add(ChemL1()); if (full) Add(ChemL2()); Add(ChemBoss());
            Add(BioHub()); Add(BioL1()); if (full) Add(BioL2()); Add(BioBoss());
            Add(CoreL1()); Add(CoreBoss());
        }

        static LevelPlan HubBase(string arc, float max)
        {
            var p = new LevelPlan(arc, arc + "_Hub", LevelKind.Hub, -26, max);
            p.Spawn("default", 2).Spawn("fromMap", -14).Spawn("fromL1", max - 40, -1).Spawn("fromBoss", max - 46, -1);
            p.Portal(-21, "WorldMap", "arc:" + arc, "World Map");
            return p;
        }

        /// <summary>
        /// The parkour stretch every chapter's level runs through before its mini-boss (all pieces tested by the bot):
        /// a vault and a slide duct, crumbling slabs, a swing bar over a 9 m gap, a quiz jump, a springboard up a
        /// 5.5 m wall and a zipline back down. Starts at ground x (height y) and returns where the ground continues.
        /// </summary>
        static float ParkourStretch(LevelPlan p, float x, float y, string enemy)
        {
            p.G(x, x + 20, y).Vault(x + 6, 1f).Duct(x + 13);
            p.Crumble(x + 24, y + 0.2f).Crumble(x + 28.6f, y + 0.9f).Crumble(x + 33.2f, y + 0.2f);
            p.G(x + 37, x + 55, y).CP(x + 40);
            p.Swing(x + 58.8f, y + 3.8f).G(x + 64, x + 80, y).CP(x + 67).E(enemy, x + 74);
            p.Quiz(x + 80, y).G(x + 97, x + 112, y).CP(x + 100).Spring(x + 106, 7.5f);
            p.G(x + 112, x + 134, y + 5.5f).Zip(x + 130, y + 7.7f, x + 156, y + 2.2f);
            p.G(x + 152, x + 170, y).CP(x + 160);
            return x + 170;
        }

        static void HubPortals(LevelPlan p, string arc, float x, string l1, string l2, string boss)
        {
            p.Portal(x, arc + "_L1", "default", l1.ToUpper());
            if (!ContentDefaults.Prototype) p.Portal(x + 7, arc + "_L2", "default", l2.ToUpper(), false, false, arc + "_L2");
            p.Portal(x + (ContentDefaults.Prototype ? 7 : 14), arc + "_Boss", "default", boss.ToUpper(), false, false, arc + "_Boss");
        }

        // ================================================================== ARC 1: CODE CITY
        static LevelPlan CodeCityHub()
        {
            var p = HubBase("CodeCity", 172);
            p.G(-26, 22).G(26, 40).P(33, 2.6f, 3.5f).G(40, 70).G(70, 96, 1.5f).G(96, 172);
            p.ShardRow(31.5f, 34.5f, 3.8f, 2);
            p.Npc("Lift Operator", 12, new[] { "G0ING UP! G0ING UP! G0ING UP! G0ING UP!" }, new[] { "The lifts work! Going up. And, crucially, STOPPING." }, "the lift's loop never changes the floor number, so it never stops");
            p.Npc("Robo-Sweeper", 35, new[] { "BONK. Recalculating. BONK. Recalculating." }, new[] { "There was a wall there the whole time? Wow." }, "", true);
            p.Lesson(47, "cs_bool", "Gate rule: open if KEYCARD AND NOT ALARM. Doors here run on logic. Broken logic. Like my uncle.");
            p.Door("cc_hub_gate", 55, "Boolean Logic", "CS_BOOL_003", "The gate's rule is scrambled. Put it back together.");
            p.CP(62);
            p.E("bug_monster", 84, "cc_hub_bug", true, "CS_LOOP_005");
            p.Shop(102).Mastery(106);
            p.Npc("Shopkeeper-Bot", 113, new[] { "W3LCOME TO THE C0STUME... THE C0STUME... THE C0S—" }, new[] { "Welcome! Every costume is 100% ridiculous and 0% armour." });
            p.Sign(120, 3.6f, "REGIONS  →");
            HubPortals(p, "CodeCity", 128, "Circuit Streets", "Server Spires", "The Infinite Core");
            p.Vault(76, 0.9f);
            p.Seal(152, "dash", "A momentum gate. We'll need to go FAST to get through. Later.");
            p.ShardRow(157, 168, 1.2f, 4, 10);
            p.Zone(-26, 12, 14.5f, 38);
            p.Zone(70, 96, 19, 41, 0, 1);
            p.Demolish(64).BlastProp(95);
            p.Script("CodeCityHubIntro").Objective("Follow Pixel. Get through the jammed gate.");
            p.title = "Code City Hub";
            return p;
        }

        static LevelPlan CodeCityL1()
        {
            var p = new LevelPlan("CodeCity", "CodeCity_L1", LevelKind.Level, -12, 272);
            p.Spawn("default", 0).Portal(-8, "CodeCity_Hub", "fromL1", "Back to the Hub");
            p.G(-12, 30).G(34, 52).P(43, 3f, 4).ShardRow(41.5f, 44.5f, 4.2f, 3);
            p.Lesson(12, "cs_python", "Variables, types, operators. The alphabet of Python. The slime ahead is made of all three.");
            p.Vault(58, 1f).E("syntax_slime", 88);
            p.G(52, 64, 2).P(69, 3.6f, 4, 5, 0, 4f).G(80, 108, 1);
            p.Npc("Taxi Driver", 22, new[] { "Where to? Where to? Where t0? Where t0?" }, new[] { "Where to? Anywhere! The roads loop properly now!" }, "an infinite loop is a loop whose condition never becomes False");
            p.E("syntax_slime", 46);
            p.CP(84);
            p.P(96, 3.4f, 3).P(103, 5.8f, 3).ShardRow(102, 104, 7f, 2, 10).P(107.5f, 3.5f, 3);
            p.G(110, 140, 4);
            p.Zone(112, 138, 26, 44, 0, 3, "Whoa. That tower over there is floating. Towers shouldn't float.");
            p.E("loop_drone", 126);
            p.Lesson(114, "cs_flow", "Every loop needs a start, a condition and a change. Write that down. Or don't. I'm not your teacher. Well...");
            p.Spring(117, 6f).ShardRow(116.4f, 117.6f, 10.4f, 2, 10);
            p.Zip(134, 6.2f, 152, 3.1f);
            p.G(144, 176, 1).CP(148);
            p.Npc("Robot #404", 153, new[] { "BONK. Door not found. BONK. Door not found." }, new[] { "Oh! It was a DOOR-SHAPED WALL. I feel silly." }, "", true);
            p.Door("cc_l1_door", 161, "Conditions", "", "This door runs an if/else that never takes the right branch.");
            p.E("bug_monster", 169);
            p.G(176, 206).E("syntax_slime", 186).E("off_by_one", 199);
            float px = ParkourStretch(p, 206, 0, "loop_drone");
            p.G(px, px + 66).CP(px + 3).Duct(px + 8);
            p.Boss("sergeant_syntax", px + 42, px + 16, px + 24, true);
            p.maxX = px + 66;
            p.P(px + 56, 5.1f, 4).Portal(px + 56, ContentDefaults.Prototype ? "CodeCity_Boss" : "CodeCity_L2", "default", ContentDefaults.Prototype ? "THE INFINITE CORE" : "TO SERVER SPIRES", true, false, "", 5.1f);
            p.Sign(px + 49, 3.2f, "↑ (needs a jump that calls itself)");
            p.BlastProp(100).Demolish(112).Chase(208, 262).Demolish(px + 4, "Sergeant Syntax just knocked over a building to make an entrance. Show-off.");
            p.Objective("Cross Circuit Streets. Something loud is waiting at the end.");
            p.title = "Circuit Streets";
            return p;
        }

        static LevelPlan CodeCityL2()
        {
            var p = new LevelPlan("CodeCity", "CodeCity_L2", LevelKind.Level, -12, 300);
            p.Spawn("default", 0).Portal(-8, "CodeCity_Hub", "fromL1", "Back to the Hub");
            p.G(-12, 26).P(31, 2.6f, 4).P(37.5f, 5.2f, 4).G(42, 72, 4);
            p.E("loop_drone", 60);
            p.G(76, 102, 4).Door("cc_l2_door", 87, "Binary", "CS_BIN_001", "The door wants a code. In binary. Of course it does.").CP(94);
            p.P(106, 6.6f, 3).P(112, 9.2f, 3).ShardRow(111, 113, 10.6f, 2, 10).G(116, 152, 8);
            p.Zone(116, 152, 24, 42, 0, 2, "We're really high up. Don't look down. Okay, look down. Wow.");
            p.E("crypto_cracker", 134);
            p.Term(144, "SECURITY NOTICE", "Strong passwords are LONG and mix letters, numbers and symbols.\n'password123' is not a password. It is a welcome mat.", "Mine is 'Pixel'. Don't tell anyone.");
            p.G(156, 192, 3).CP(160);
            p.E("syntax_slime", 170);
            p.Npc("Server Admin", 178, new[] { "Have you tried turning it off and on? Have you tried turning it off and—" }, new[] { "Everything's back online. I'm taking a very long nap." }, "binary place values double each step: 1, 2, 4, 8, 16");
            p.G(196, 232, 3).E("bug_monster", 207).E("loop_drone", 222);
            p.P(236, 5.5f, 3).P(242, 8f, 3).ShardRow(241, 243, 9.4f, 2, 10);
            p.G(232, 300, 3).CP(248);
            p.Npc("Archivist", 262, new[] { "Searching... searching... searching every record ONE BY ONE..." }, new[] { "Binary search! Halve it every time! Why didn't I think of that?" }, "binary search halves the list each step");
            p.Portal(288, "CodeCity_Boss", "default", "THE INFINITE CORE", true);
            p.Objective("Climb the Server Spires to the Infinite Core.");
            p.title = "Server Spires";
            return p;
        }

        static LevelPlan CodeCityBoss()
        {
            var p = new LevelPlan("CodeCity", "CodeCity_Boss", LevelKind.BossArena, -12, 104);
            p.Spawn("default", 0).G(-12, 104).Portal(-8, "CodeCity_Hub", "fromBoss", "Retreat to the Hub");
            p.Boss("lord_infinite_loop", 66, 20, 34, false);
            p.Zone(14, 104, 26, 44, 0, 3);
            p.Objective("Defeat Lord Infinite Loop.");
            p.title = "The Infinite Core";
            return p;
        }

        // ================================================================== ARC 2: PHYSICS VALLEY
        static LevelPlan PhysicsHub()
        {
            var p = HubBase("PhysicsValley", 178);
            p.G(-26, 30).G(34, 40, 1).G(40, 58, 1).Ceil(40, 58, 9.5f).G(58, 112);
            p.Npc("Professor Apple", 14, new[] { "What goes up... what goes up... what goes up..." }, new[] { "What goes up comes DOWN! As it should! Glorious gravity!" }, "an object stays at rest unless a force acts on it");
            p.Grav(42, 56, 1, 9.5f);
            p.ShardRow(44, 54, 8.4f, 4, 5);
            p.Lesson(64, "ph_laws", "Someone graffitied 'GRAVITY IS A SUGGESTION' under it. Rude.");
            p.Spring(22, 6.5f).ShardRow(21.4f, 22.6f, 9.8f, 2, 10);
            p.Door("pv_hub_door", 76, "Newton's Laws", "", "This gate opens with the right force. Literally.");
            p.CP(84).E("momentum_mite", 96);
            p.Shop(104).Mastery(108);
            p.G(112, 178);
            HubPortals(p, "PhysicsValley", 124, "Floating Isles", "Tesla Ridge", "The Courtroom");
            p.Seal(152, "wall_jump", "A cliff with no handholds. We'll need to bond with walls first.");
            p.ShardRow(157, 172, 1.2f, 5, 10);
            p.Zone(36, 62, 20, 42, 0, 3, "Gravity's having a day. Up is down in there. Try not to think about it.");
            p.Demolish(86);
            p.Script("ArcArrival").Objective("Explore Physics Valley. The laws have been rewritten.");
            p.title = "Physics Valley Hub";
            return p;
        }

        static LevelPlan PhysicsL1()
        {
            var p = new LevelPlan("PhysicsValley", "PhysicsValley_L1", LevelKind.Level, -12, 280);
            p.Spawn("default", 0).Portal(-8, "PhysicsValley_Hub", "fromL1", "Back to the Hub");
            p.G(-12, 20).G(25, 40, 1.5f).P(46, 3f, 4, 0, 2.6f, 3.2f).G(52, 70, 3);
            p.Lesson(10, "ph_kinematics", "Launch range: R = u² sin 2θ / g. 45° goes furthest, and mass doesn't appear. At all.");
            p.Vault(32, 0.9f);
            p.E("gravity_goon", 62);
            p.P(76, 4.6f, 3).G(82, 100, 2).CP(86);
            p.G(100, 104, 2).Ceil(100, 124, 11).Grav(101, 121, 2, 11).G(120, 150, 2);
            p.ShardRow(106, 118, 9.8f, 4, 5);
            p.E("static_spark", 133);
            p.Lesson(124, "ph_work", "Baron Von Friction is up ahead. Friction does NEGATIVE work. Be more positive than him.");
            p.G(155, 186).CP(158);
            p.Npc("Weather Vane", 166, new[] { "Wind: up. Rain: up. Me: UP. Everything: UP." }, new[] { "Wind from the west. Rain falls DOWN. Lovely." });
            p.E("momentum_mite", 177);
            p.P(192, 2.6f, 4, 6, 0, 5f);
            p.G(202, 210, 1).CP(206);
            float px = ParkourStretch(p, 210, 1, "momentum_mite");
            p.G(px, px + 70, 1).CP(px + 4);
            p.Boss("baron_friction", px + 40, px + 10, px + 18, true);
            p.maxX = px + 70;
            p.Seal(px + 52, "dash", "The Momentum Gate. It only opens for things in motion.", 6f);
            p.Portal(px + 62, ContentDefaults.Prototype ? "PhysicsValley_Boss" : "PhysicsValley_L2", "default", ContentDefaults.Prototype ? "THE COURTROOM OF BROKEN LAWS" : "TO TESLA RIDGE", true);
            p.Zone(120, 150, 24, 43, 0, 2, "Look at those islands. Floating like they forgot gravity's phone number.");
            p.BlastProp(92).Chase(212, 262).Demolish(214);   // the campus lab stands mid-level
            p.Objective("Cross the Floating Isles.");
            p.title = "Floating Isles";
            return p;
        }

        static LevelPlan PhysicsL2()
        {
            var p = new LevelPlan("PhysicsValley", "PhysicsValley_L2", LevelKind.Level, -12, 302);
            p.Spawn("default", 0).Portal(-8, "PhysicsValley_Hub", "fromL1", "Back to the Hub");
            p.G(-12, 40).Hazard(20, 24, 0, "sparks", false).E("static_spark", 33);
            p.G(44, 82, 1.5f).Door("pv_l2_door", 58, "Electricity", "PH_ELEC_002", "A Tesla gate. It needs exactly the right current.").CP(66).E("gravity_goon", 76);
            p.G(86, 120, 3).P(100, 6.2f, 4).ShardRow(99, 101, 7.6f, 2, 10).E("tesla_wraith", 112);
            p.Sign(117, 6f, "DASH  →→");
            p.G(128, 172, 3).CP(132);
            p.Term(141, "TESLA LOG", "SERIES: one path, resistances ADD.\nPARALLEL: many paths, the total resistance DROPS.", "");
            p.E("static_spark", 154).Hazard(160, 163, 3, "sparks", false);
            p.G(176, 232, 1).Npc("Lineworker", 184, new[] { "Volts! Amps! Ohms! Volts! Amps! Ohms!" }, new[] { "V = IR. I'm getting it tattooed." }, "I = V / R: more resistance means less current");
            p.E("momentum_mite", 194).E("tesla_wraith", 214);
            p.G(236, 302).CP(240);
            p.ShardRow(250, 262, 1.3f, 4);
            p.Portal(290, "PhysicsValley_Boss", "default", "THE COURTROOM OF BROKEN LAWS", true);
            p.Objective("Survive Tesla Ridge.");
            p.title = "Tesla Ridge";
            return p;
        }

        static LevelPlan PhysicsBoss()
        {
            var p = new LevelPlan("PhysicsValley", "PhysicsValley_Boss", LevelKind.BossArena, -12, 104);
            p.Spawn("default", 0).G(-12, 104).Portal(-8, "PhysicsValley_Hub", "fromBoss", "Retreat to the Hub");
            p.P(66, 2.5f, 8);
            p.Boss("lord_broken_laws", 66, 20, 34, false, 2.5f);
            p.Zone(14, 104, 27, 44, 0, 3);
            p.Objective("Defeat the Lord of Broken Laws.");
            p.title = "The Courtroom of Broken Laws";
            return p;
        }

        // ================================================================== ARC 3: CHEMICAL CHAOS
        static LevelPlan ChemHub()
        {
            var p = HubBase("ChemicalChaos", 178);
            p.G(-26, 28).G(28, 44, 1.2f).Hazard(44, 50, -0.5f, "acid").G(50, 112);
            p.P(47, 2.6f, 3.5f, 0, 0);
            p.Npc("Lab Assistant", 12, new[] { "Hydrogen won't bond! Oxygen won't bond! N0B0DY WILL B0ND!" }, new[] { "Water is wet again! Two hydrogens and an oxygen, holding hands!" }, "atoms bond to fill their outer shell");
            p.Lesson(58, "ch_basic", "Goggles on. Not literally, you don't have any. Yet.");
            p.Door("ch_hub_door", 70, "Molecules", "CH_MOL_001", "The door is fused shut. Make some water to cool it.");
            p.CP(78).E("hydrogen_hopper", 88);
            p.Shop(102).Mastery(106);
            p.G(112, 178);
            HubPortals(p, "ChemicalChaos", 124, "Beaker Towers", "Acid Works", "The Grand Reactor");
            p.Duct(36);
            p.Seal(152, "shrink", "A tiny vent. Way too small for us. For now.");
            p.ShardRow(157, 172, 1.2f, 5, 10);
            p.Demolish(78).BlastProp(97);
            p.Script("ArcArrival").Objective("Explore the corrupted laboratory.");
            p.title = "Chemical Chaos Hub";
            return p;
        }

        static LevelPlan ChemL1()
        {
            var p = new LevelPlan("ChemicalChaos", "ChemicalChaos_L1", LevelKind.Level, -12, 282);
            p.Spawn("default", 0).Portal(-8, "ChemicalChaos_Hub", "fromL1", "Back to the Hub");
            p.G(-12, 24).Hazard(24, 34, -0.5f, "acid").P(29, 1.8f, 3, 0, 1.5f, 3f).G(34, 60, 1);
            p.Lesson(10, "ch_atom", "These element creatures used to be the periodic table. Know what's inside them and they calm down.");
            p.Duct(56).Vault(152, 1f);
            p.E("oxy_brute", 48);
            p.P(64, 3.5f, 3).P(70, 6f, 3).G(74, 100, 5).CP(78);
            p.Npc("Beaker Bot", 86, new[] { "Bubble. Bubble. Toil. Bubble. Bubble." }, new[] { "Everything's bonding nicely. Like a big molecular hug." }, "carbon makes four bonds");
            p.E("carbon_clump", 95);
            p.G(104, 140, 2).Hazard(118, 124, 1.5f, "acid").P(121, 4f, 4, 0, 0).E("hydrogen_hopper", 132);
            p.Lesson(108, "ch_bond");
            p.G(144, 186, 2).CP(148).E("sodium_sprinter", 160);
            p.Door("ch_l1_door", 170, "Equations", "", "This valve only opens for a BALANCED equation.");
            p.E("acid_alchemist", 180);
            p.G(190, 200).CP(194);
            float px = ParkourStretch(p, 200, 0, "hydrogen_hopper");
            p.G(px, px + 92).CP(px + 4);
            p.Boss("count_catalyst", px + 42, px + 14, px + 22, true);
            p.maxX = px + 92;
            p.W(px + 62, 0, 7).G(px + 66, px + 92, 8);
            p.Sign(px + 58, 3.4f, "↑ WALL-JUMP SHAFT");
            p.Portal(px + 82, ContentDefaults.Prototype ? "ChemicalChaos_Boss" : "ChemicalChaos_L2", "default", ContentDefaults.Prototype ? "THE GRAND REACTOR" : "TO THE ACID WORKS", true, false, "", 8f);
            p.Demolish(150).BlastProp(176).Chase(202, 252);
            p.Objective("Climb Beaker Towers.");
            p.title = "Beaker Towers";
            return p;
        }

        static LevelPlan ChemL2()
        {
            var p = new LevelPlan("ChemicalChaos", "ChemicalChaos_L2", LevelKind.Level, -12, 300);
            p.Spawn("default", 0).Portal(-8, "ChemicalChaos_Hub", "fromL1", "Back to the Hub");
            p.G(-12, 32).E("hydrogen_hopper", 22);
            p.Hazard(32, 46, -0.5f, "acid").P(36, 2f, 3, 0, 0).P(42, 2.8f, 3, 0, 0).G(46, 80, 1.5f);
            p.Door("ch_l2_door", 58, "Bonding", "", "The pipe is sealed by ions that refuse to bond.").CP(66).E("oxy_brute", 74);
            p.W(84, 0, 7).G(88, 120, 7).ShardRow(92, 100, 8.4f, 3, 10).E("carbon_clump", 110);
            p.Zone(86, 122, 24, 43, 0, 3, "That's the Grand Reactor in the distance. It's glowing. Glowing is bad.");
            p.G(124, 160, 3).CP(128);
            p.Term(136, "REACTOR NOTE", "Equations must BALANCE: the same number of each atom on both sides.\nMass is never created or destroyed.", "The Lord of the Lab disagrees. Loudly.");
            p.E("sodium_sprinter", 148);
            p.G(164, 200, 1).Npc("Valve Keeper", 172, new[] { "Open! Close! Open! Close! Open!" }, new[] { "Balanced. Stable. Beautifully boring." }, "count the atoms on both sides");
            p.E("acid_alchemist", 188);
            p.Hazard(200, 214, -0.5f, "acid").P(205, 2.5f, 3, 4, 0, 3.5f).G(214, 300, 1).CP(218);
            p.ShardRow(240, 252, 1.3f, 4);
            p.Portal(288, "ChemicalChaos_Boss", "default", "THE GRAND REACTOR", true);
            p.Objective("Shut down the Acid Works.");
            p.title = "Acid Works";
            return p;
        }

        static LevelPlan ChemBoss()
        {
            var p = new LevelPlan("ChemicalChaos", "ChemicalChaos_Boss", LevelKind.BossArena, -12, 104);
            p.Spawn("default", 0).G(-12, 104).Portal(-8, "ChemicalChaos_Hub", "fromBoss", "Retreat to the Hub");
            p.Boss("lord_of_the_lab", 66, 20, 34, false);
            p.Zone(14, 104, 26, 44, 0, 3);
            p.Objective("Defeat the Lord of the Lab.");
            p.title = "The Grand Reactor";
            return p;
        }

        // ================================================================== ARC 4: BIO-ZONE
        static LevelPlan BioHub()
        {
            var p = HubBase("BioZone", 178);
            p.G(-26, 30).G(34, 70).Ceil(52, 60, 1.1f).ShardRow(53, 59, 0.5f, 3, 10).G(70, 112, 1);
            p.Npc("White Cell Wanda", 14, new[] { "Patrolling... patrolling... I forgot what I'm patrolling for..." }, new[] { "Patrol complete. No viruses. Just one very small student." }, "arteries carry blood AWAY from the heart");
            p.Lesson(44, "bio_cell", "I'm reading it upside down and it still makes sense.");
            p.Sign(50, 3.4f, "SHRINK (Q) →");
            p.Door("bz_hub_door", 80, "Cells", "", "A cell membrane. It only lets in those who know how cells work.");
            p.CP(88).E("rogue_cell", 98);
            p.Shop(104).Mastery(108);
            p.G(112, 178);
            HubPortals(p, "BioZone", 124, "Vessel Rapids", "The Breathing Caverns", "The Heart of the Matter");
            p.ShardRow(157, 172, 1.2f, 5, 10);
            p.BlastProp(68);
            p.Script("BioZoneArrival").Objective("You're microscopic. Find out what's attacking the organism.");
            p.title = "Bio-Zone Hub";
            return p;
        }

        static LevelPlan BioL1()
        {
            var p = new LevelPlan("BioZone", "BioZone_L1", LevelKind.Level, -12, 282);
            p.Spawn("default", 0).Portal(-8, "BioZone_Hub", "fromL1", "Back to the Hub");
            p.G(-12, 30).E("virus_swarmling", 22);
            p.Lesson(10, "bio_breath", "This is the airway. Everything here moves because of pressure. Like exams.");
            p.Hazard(30, 52, -0.6f, "blood").P(35, 1.6f, 3.2f, 4, 0, 3f).P(45, 1.6f, 3.2f, -3, 0, 2.6f).G(52, 84, 1);
            p.Ceil(64, 74, 2.1f).CP(58).E("plaque_puff", 80);
            p.G(88, 124, 2).Npc("Red Cell Rick", 96, new[] { "Oxygen... oxygen... where do I take the oxygen..." }, new[] { "Lungs to heart to body! I know my route now!" }, "red blood cells carry oxygen using haemoglobin");
            p.Lesson(101, "bio_circ", "Professor Plaque blocks arteries for fun. Know the route and he's just a speed bump.");
            p.Door("bz_l1_door", 108, "Circulation", "", "A valve. One-way only, like every good heart valve.");
            p.E("rogue_cell", 118);
            p.Hazard(124, 140, 1.4f, "blood").P(129, 2.8f, 2.6f).P(135.5f, 3.2f, 2.6f).G(140, 180, 2).CP(144);
            p.E("virus_swarmling", 162).E("antibody_imposter", 174);
            p.G(184, 194, 1).CP(188);
            float px = ParkourStretch(p, 194, 1, "virus_swarmling");
            p.G(px, px + 98, 1).CP(px + 4);
            p.Boss("professor_plaque", px + 46, px + 16, px + 24, true);
            p.maxX = px + 98;
            p.Ceil(px + 70, px + 80, 2.1f).Portal(px + 88, ContentDefaults.Prototype ? "BioZone_Boss" : "BioZone_L2", "default", ContentDefaults.Prototype ? "THE HEART OF THE MATTER" : "TO THE BREATHING CAVERNS", true);
            p.BlastProp(60).BlastProp(150).Chase(196, 246);
            p.Objective("Ride the Vessel Rapids.");
            p.title = "Vessel Rapids";
            return p;
        }

        static LevelPlan BioL2()
        {
            var p = new LevelPlan("BioZone", "BioZone_L2", LevelKind.Level, -12, 300);
            p.Spawn("default", 0).Portal(-8, "BioZone_Hub", "fromL1", "Back to the Hub");
            p.G(-12, 40).E("plaque_puff", 30);
            p.P(46, 3f, 4, 0, 2f, 3.4f).G(52, 90, 3).CP(56);
            p.Npc("Alveolus Al", 66, new[] { "In... out... in... in... in... IN..." }, new[] { "In, out, in, out. Breathing is easy when it's done right." }, "aerobic respiration uses oxygen to release energy");
            p.Door("bz_l2_door", 76, "Respiration", "", "The airway is blocked. Prove you know where the air goes.");
            p.E("virus_swarmling", 86);
            p.G(94, 130, 1).Ceil(102, 114, 2.1f).ShardRow(104, 112, 0.6f, 3, 10).E("rogue_cell", 122);
            p.Zone(94, 132, 24, 43, 0, 2, "Those big pink bubbles are alveoli. This is where oxygen gets into the blood.");
            p.G(134, 170, 3).CP(138).E("antibody_imposter", 158);
            p.Term(146, "BREATH LOG", "Nose -> pharynx -> larynx -> trachea -> bronchi -> bronchioles -> alveoli.\nIn the alveoli, oxygen enters the blood.", "");
            p.Hazard(170, 186, 1.4f, "blood").P(178, 3.4f, 4, 0, 0).G(186, 300, 2).CP(190);
            p.E("plaque_puff", 204).E("virus_swarmling", 222);
            p.ShardRow(240, 252, 3.3f, 4);
            p.Portal(288, "BioZone_Boss", "default", "THE HEART OF THE MATTER", true);
            p.Objective("Clear the Breathing Caverns.");
            p.title = "The Breathing Caverns";
            return p;
        }

        static LevelPlan BioBoss()
        {
            var p = new LevelPlan("BioZone", "BioZone_Boss", LevelKind.BossArena, -12, 104);
            p.Spawn("default", 0).G(-12, 104).Portal(-8, "BioZone_Hub", "fromBoss", "Retreat to the Hub");
            p.Boss("lord_within", 66, 20, 34, false);
            p.Zone(14, 104, 26, 44, 0, 3);
            p.Objective("Defeat the Lord Within.");
            p.title = "The Heart of the Matter";
            return p;
        }

        // ================================================================== FINAL: THE CORE
        static LevelPlan CoreL1()
        {
            var p = new LevelPlan("TheCore", "TheCore_L1", LevelKind.Level, -12, 330);
            p.Spawn("default", 0).Spawn("fromMap", 0).Portal(-8, "WorldMap", "arc:TheCore", "World Map");
            p.G(-12, 40).E("glitch_fragment", 28);
            p.Lesson(14, "cs_lists", "Everything in The Source is a list. Even its feelings. Especially its feelings.").Vault(36, 1f);
            // a Code City piece: double-jump ledge
            p.P(46, 3.4f, 3).P(52, 6.2f, 3).G(56, 90, 6).CP(60).E("glitch_fragment", 76);
            // a Physics piece: dash gap + gravity
            p.G(98, 130, 6).Lesson(102, "ph_grav").Sign(93, 9f, "DASH →").Ceil(110, 126, 14).Grav(111, 125, 6, 14).E("glitch_fragment", 128);
            // a Chemistry piece: wall-jump shaft + acid
            p.G(130, 168, 2).Lesson(134, "ch_redox").Hazard(140, 146, 1.5f, "acid").P(143, 4f, 3).W(164, 2, 9).G(168, 204, 10).CP(172).E("source_shard", 192);
            // a Bio piece: shrink tunnel
            p.G(204, 260, 4).Ceil(218, 230, 5.1f).E("glitch_fragment", 246);
            p.Lesson(209, "bio_division").Vault(252, 1f);
            p.Term(238, "THE SOURCE'S DIARY", "Day 1: Wrote the rules.\nDay 2: Deleted the rules.\nDay 3: Why is everything on fire?", "This is the most relatable thing I've ever read.");
            p.G(260, 290, 4).CP(264).ShardRow(272, 284, 5.3f, 4, 10);
            float px = ParkourStretch(p, 290, 4, "glitch_fragment");
            p.G(px, px + 40, 4).CP(px + 4);
            p.maxX = px + 40;
            p.Portal(px + 28, "TheCore_Boss", "default", "THE SOURCE", true);
            p.Zone(130, 210, 26, 44, 0, 3, "Code, physics, chemistry, biology... all mashed together. That's what The Source wants.");
            p.Demolish(60).BlastProp(150).Demolish(175).Chase(292, 342);
            p.Objective("Reach The Source. Use everything you've learned.");
            p.title = "The Fractured Nexus";
            return p;
        }

        static LevelPlan CoreBoss()
        {
            var p = new LevelPlan("TheCore", "TheCore_Boss", LevelKind.BossArena, -12, 110);
            p.Spawn("default", 0).G(-12, 110);
            p.Boss("the_source", 70, 22, 36, false);
            p.Zone(14, 110, 30, 46, 0, 4);
            p.Objective("Defeat The Source.");
            p.title = "The Source";
            return p;
        }

    }
}
