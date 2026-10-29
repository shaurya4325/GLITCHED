using System;
using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// A level described as data: ground runs, platforms, walls, and everything placed in them. The LevelBuilder
    /// turns a plan into scene geometry (gameplay plane at Z = 0) plus the region's living 3D backdrop.
    /// Units are metres; x runs left to right, y is up.
    /// </summary>
    public class LevelPlan
    {
        public string arcId, levelId, scene, title = "";
        public LevelKind kind = LevelKind.Level;
        public float minX = -20, maxX = 200;
        public string objective = "";
        public string levelScript = "";      // LevelScript component type name
        public bool startHealthy;

        public readonly List<Vector3> ground = new List<Vector3>();        // x0, x1, top y
        public readonly List<PlatformDef> platforms = new List<PlatformDef>();
        public readonly List<Vector3> walls = new List<Vector3>();         // x, y0, y1
        public readonly List<Vector3> ceilings = new List<Vector3>();      // x0, x1, bottom y (shrink tunnels)
        public readonly List<SpawnDef> spawns = new List<SpawnDef>();
        public readonly List<CheckpointDef> checkpoints = new List<CheckpointDef>();
        public readonly List<EnemyDef> enemies = new List<EnemyDef>();
        public readonly List<NPCDef> npcs = new List<NPCDef>();
        public readonly List<TerminalDef> terminals = new List<TerminalDef>();
        public readonly List<DoorDef> doors = new List<DoorDef>();
        public readonly List<BarrierDef> barriers = new List<BarrierDef>();
        public readonly List<Vector3> shards = new List<Vector3>();        // x, y, value
        public readonly List<PortalDef> portals = new List<PortalDef>();
        public readonly List<ZoneDef> zones = new List<ZoneDef>();
        public readonly List<Rect> gravity = new List<Rect>();
        public readonly List<HazardDef> hazards = new List<HazardDef>();
        public readonly List<SignDef> signs = new List<SignDef>();
        public readonly List<ParkourDef> parkour = new List<ParkourDef>();
        public readonly List<SetPieceDef> setPieces = new List<SetPieceDef>();
        public RunDef run;
        public float shopX = float.NaN, masteryX = float.NaN;
        public BossDef boss;

        public LevelPlan(string arc, string level, LevelKind k, float min, float max)
        {
            arcId = arc; levelId = level; scene = level; kind = k; minX = min; maxX = max;
        }

        // ------------------------------------------------------------------ fluent builders
        public LevelPlan G(float x0, float x1, float y = 0) { ground.Add(new Vector3(x0, x1, y)); return this; }
        public LevelPlan P(float x, float y, float w, float moveX = 0, float moveY = 0, float period = 4f) { platforms.Add(new PlatformDef { x = x, y = y, w = w, move = new Vector2(moveX, moveY), period = period }); return this; }
        public LevelPlan W(float x, float y0, float y1) { walls.Add(new Vector3(x, y0, y1)); return this; }
        public LevelPlan Ceil(float x0, float x1, float y) { ceilings.Add(new Vector3(x0, x1, y)); return this; }
        public LevelPlan Spawn(string id, float x, int facing = 1) { spawns.Add(new SpawnDef { id = id, x = x, facing = facing }); return this; }
        public LevelPlan CP(float x) { checkpoints.Add(new CheckpointDef { id = $"{levelId}_cp{checkpoints.Count}", x = x }); return this; }
        public LevelPlan E(string enemy, float x, string id = null, bool tutorial = false, string firstQ = null, float y = float.NaN)
        {
            enemies.Add(new EnemyDef { enemyId = enemy, x = x, y = y, id = id ?? $"{levelId}_{enemy}_{enemies.Count}", tutorial = tutorial, firstQuestion = firstQ });
            return this;
        }
        public LevelPlan Npc(string name, float x, string[] corrupted, string[] restored, string clue = "", bool wallBonk = false, Color? shirt = null)
        {
            npcs.Add(new NPCDef { name = name, x = x, corrupted = corrupted, restored = restored, clue = clue, wallBonk = wallBonk, shirt = shirt ?? Color.HSVToRGB((npcs.Count * 0.17f) % 1f, 0.55f, 0.9f) });
            return this;
        }
        public LevelPlan Term(float x, string title, string text, string comment = "") { terminals.Add(new TerminalDef { x = x, title = title, text = text, comment = comment }); return this; }
        /// <summary>A lesson terminal teaching one Class XI topic: the small enemies after it ask MCQs on that topic.</summary>
        public LevelPlan Lesson(float x, string topic, string comment = "") { terminals.Add(new TerminalDef { x = x, topic = topic, comment = comment }); return this; }

        // ------------------------------------------------------------------ parkour
        /// <summary>A low obstacle to vault over at a run (h up to ~1.3 m) or climb onto (longer ones).</summary>
        public LevelPlan Vault(float x, float h = 1f, float w = 1.2f) { parkour.Add(new ParkourDef { kind = "vault", x = x, h = h, w = w }); return this; }
        /// <summary>A low duct hanging across the path: slide (down at a run) to get under it.</summary>
        public LevelPlan Duct(float x, float w = 1.6f, float clearance = 1.05f) { parkour.Add(new ParkourDef { kind = "duct", x = x, w = w, h = clearance }); return this; }
        public LevelPlan Spring(float x, float height = 7f, float forward = 0f) { parkour.Add(new ParkourDef { kind = "spring", x = x, h = height, value = forward }); return this; }
        /// <summary>A zipline from (x0, y0) down to (x1, y1): heights are the cable (hand height, ~1.9 m above the feet).</summary>
        public LevelPlan Zip(float x0, float y0, float x1, float y1) { parkour.Add(new ParkourDef { kind = "zip", x = x0, y = y0, x1 = x1, y1 = y1 }); return this; }
        /// <summary>A swing bar with its pivot at (x, y).</summary>
        public LevelPlan Swing(float x, float y) { parkour.Add(new ParkourDef { kind = "swing", x = x, y = y }); return this; }
        public LevelPlan Crumble(float x, float y, float w = 2.6f) { parkour.Add(new ParkourDef { kind = "crumble", x = x, y = y, w = w }); return this; }
        /// <summary>A quiz pit: three answer pillars (tops at y) in the gap [x, x + QuizWidth], then a firewall. Leave that gap open.</summary>
        public LevelPlan Quiz(float x, float y, string topic = null) { parkour.Add(new ParkourDef { kind = "quiz", x = x, y = y, topic = topic }); return this; }
        public const float QuizWidth = 17f;

        // ------------------------------------------------------------------ set pieces (explosions)
        /// <summary>When the player passes triggerX, the Glitch demolishes a backdrop building just ahead.</summary>
        public LevelPlan Demolish(float triggerX, string line = "") { setPieces.Add(new SetPieceDef { kind = "demolish", x = triggerX, line = line }); return this; }
        /// <summary>Blasts chase the player from x0 to x1 (a parkour stretch).</summary>
        public LevelPlan Chase(float x0, float x1) { setPieces.Add(new SetPieceDef { kind = "chase", x = x0, x1 = x1 }); return this; }
        /// <summary>A prop beside the path at x that explodes as the player arrives.</summary>
        public LevelPlan BlastProp(float x) { setPieces.Add(new SetPieceDef { kind = "prop", x = x }); return this; }
        /// <summary>A timed parkour run from start to finish, with medal times in seconds.</summary>
        public LevelPlan Run(float start, float finish, float gold, float silver, float bronze) { run = new RunDef { start = start, finish = finish, gold = gold, silver = silver, bronze = bronze }; return this; }

        /// <summary>Each enemy (and quiz pit) takes the topic of the last lesson terminal before it (or the first one in the level).</summary>
        public void ResolveTopics()
        {
            var lessons = terminals.FindAll(t => !string.IsNullOrEmpty(t.topic));
            if (lessons.Count == 0) return;
            lessons.Sort((a, b) => a.x.CompareTo(b.x));
            string TopicAt(float x)
            {
                string t = lessons[0].topic;
                foreach (var l in lessons) if (l.x <= x) t = l.topic;
                return t;
            }
            foreach (var e in enemies) if (string.IsNullOrEmpty(e.topic)) e.topic = TopicAt(e.x);
            foreach (var p in parkour) if (p.kind == "quiz" && string.IsNullOrEmpty(p.topic)) p.topic = TopicAt(p.x);
        }
        public LevelPlan Door(string id, float x, string concept, string questionId = "", string line = "") { doors.Add(new DoorDef { id = id, x = x, concept = concept, questionId = questionId, line = line }); return this; }
        public LevelPlan Seal(float x, string ability, string line = "", float height = 4f) { barriers.Add(new BarrierDef { x = x, ability = ability, line = line, height = height }); return this; }
        public LevelPlan Shard(float x, float y, int value = 5) { shards.Add(new Vector3(x, y, value)); return this; }
        public LevelPlan ShardRow(float x0, float x1, float y, int count, int value = 5)
        {
            for (int i = 0; i < count; i++) Shard(Mathf.Lerp(x0, x1, count == 1 ? 0.5f : i / (float)(count - 1)), y, value);
            return this;
        }
        public LevelPlan Portal(float x, string scene, string spawn, string label, bool completes = false, bool auto = false, string requires = "", float y = float.NaN)
        {
            portals.Add(new PortalDef { x = x, y = y, scene = scene, spawn = spawn, label = label, completes = completes, auto = auto, requires = requires });
            return this;
        }
        public LevelPlan Zone(float x0, float x1, float distance, float fov = 40f, float yaw = 0f, float height = 0f, string line = "")
        {
            zones.Add(new ZoneDef { x0 = x0, x1 = x1, distance = distance, fov = fov, yaw = yaw, height = height, line = line });
            return this;
        }
        public LevelPlan Grav(float x0, float x1, float y0, float y1) { gravity.Add(Rect.MinMaxRect(x0, y0, x1, y1)); return this; }
        public LevelPlan Hazard(float x0, float x1, float y, string kind = "acid", bool respawn = true) { hazards.Add(new HazardDef { x0 = x0, x1 = x1, y = y, kind = kind, respawn = respawn }); return this; }
        public LevelPlan Sign(float x, float y, string text) { signs.Add(new SignDef { x = x, y = y, text = text }); return this; }
        public LevelPlan Shop(float x) { shopX = x; return this; }
        public LevelPlan Mastery(float x) { masteryX = x; return this; }
        public LevelPlan Boss(string bossId, float x, float triggerX, float playerX, bool mini, float y = float.NaN, string after = "")
        {
            boss = new BossDef { bossId = bossId, x = x, y = y, triggerX = triggerX, playerX = playerX, mini = mini, after = after };
            return this;
        }
        public LevelPlan Script(string type) { levelScript = type; return this; }
        public LevelPlan Objective(string text) { objective = text; return this; }

        /// <summary>Top surface height at x (ground or platforms), or -20 if there is a pit.</summary>
        public float GroundY(float x)
        {
            float best = float.NegativeInfinity;
            foreach (var g in ground) if (x >= g.x && x <= g.y) best = Mathf.Max(best, g.z);
            if (!float.IsNegativeInfinity(best)) return best;
            foreach (var p in platforms) if (Mathf.Abs(x - p.x) <= p.w / 2 && p.move == Vector2.zero) best = Mathf.Max(best, p.y);
            return float.IsNegativeInfinity(best) ? -20f : best;
        }
    }

    public class PlatformDef { public float x, y, w; public Vector2 move; public float period; }
    public class SpawnDef { public string id; public float x; public int facing; }
    public class CheckpointDef { public string id; public float x; }
    public class EnemyDef { public string enemyId, id, firstQuestion, topic; public float x, y; public bool tutorial; }
    public class NPCDef { public string name, clue; public float x; public string[] corrupted, restored; public bool wallBonk; public Color shirt; }
    public class TerminalDef { public float x; public string title, text, comment, topic; }
    public class ParkourDef { public string kind, topic; public float x, y, x1, y1, w, h, value; }
    public class RunDef { public float start, finish, gold, silver, bronze; }
    public class SetPieceDef { public string kind, line; public float x, x1; }
    public class DoorDef { public string id, concept, questionId, line; public float x; }
    public class BarrierDef { public float x, height; public string ability, line; }
    public class PortalDef { public float x, y; public string scene, spawn, label, requires; public bool completes, auto; }
    public class ZoneDef { public float x0, x1, distance, fov, yaw, height; public string line; }
    public class HazardDef { public float x0, x1, y; public string kind; public bool respawn; }
    public class SignDef { public float x, y; public string text; }
    public class BossDef { public string bossId, after; public float x, y, triggerX, playerX; public bool mini; }
}
