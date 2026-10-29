using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Realistic gameplay geometry for the city levels (replaces the floating glowing slabs):
    ///   street-level ground  -> sidewalk slab running back to the road, paving on top, retaining walls in the pits
    ///   raised ground (1-3m) -> concrete plaza;  raised ground (> 3m) -> roof of a low building with shopfronts
    ///   gaps                 -> construction pits with a real floor (landing there respawns the player)
    ///   platforms            -> steel scaffolding decks; moving platforms -> window-cleaning gondolas on cables
    ///   walls / ceilings     -> concrete walls / an overpass slab on columns
    /// Every visible walking surface sits exactly at its collider's height, so characters never sink into it.
    /// </summary>
    public class CityGround
    {
        readonly LevelPlan plan;
        readonly Transform geo;
        readonly System.Random rnd;
        readonly float depth;
        const float Front = -6f;                       // front edge of the walkable ground (camera side)
        const float Back = CityBackdrop.RoadNear;      // back edge: the kerb at the road
        const float PitFloor = -8f;
        readonly float glitch = 1f;
        readonly bool valley;   // Physics Valley: grass paths, rock ledges, river ravines, timber boardwalks
        readonly bool plant;    // Chemical Chaos: concrete plant yard, safety lines, chemical trenches, steel catwalks
        readonly bool lab;      // Bio-Zone: tiled atrium floor, mezzanine slabs, maintenance trenches
        readonly bool roof;     // The Core: rooftops of skyscrapers, the gaps drop to the city far below

        Material paving, concrete, granite, gravel, steel, grating, yellow, cable, dirt;

        public CityGround(LevelPlan plan, Transform geo, System.Random rnd, float depth)
        {
            this.plan = plan; this.geo = geo; this.rnd = rnd; this.depth = depth;
            valley = plan.arcId == "PhysicsValley";
            plant = plan.arcId == "ChemicalChaos";
            lab = plan.arcId == "BioZone";
            roof = plan.arcId == "TheCore";
        }

        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

        void Mats()
        {
            paving = CityMaterials.PBR("PavingStones138", new Color(0.84f, 0.82f, 0.78f), 1.5f, 1f, 0f, glitch);
            concrete = CityMaterials.PBR("Concrete046", new Color(0.68f, 0.67f, 0.64f), 3f, 1f, 0f, glitch);
            granite = CityMaterials.PBR("Granite002A", new Color(0.72f, 0.72f, 0.72f), 1.5f, 0.9f, 0f, glitch);
            gravel = CityMaterials.PBR("Gravel042", new Color(0.75f, 0.72f, 0.68f), 2f, 1f, 0f, glitch);
            dirt = CityMaterials.PBR("Ground109", new Color(0.8f, 0.75f, 0.7f), 3f, 1f, 0f, glitch);
            steel = CityMaterials.PBR("PaintedMetal004", new Color(0.3f, 0.32f, 0.34f), 1.5f, 0.8f, 0.6f, glitch);
            grating = CityMaterials.PBR("DiamondPlate009", new Color(0.55f, 0.55f, 0.55f), 1f, 0.7f, 0.8f, glitch);
            yellow = CityMaterials.Solid(new Color(0.9f, 0.68f, 0.08f), 0.5f, 0f, 0.5f);
            cable = CityMaterials.Solid(new Color(0.08f, 0.08f, 0.08f), 0.4f, 0.8f, 0.5f);
            if (lab)
            {
                paving = CityMaterials.PBR("Concrete046", new Color(0.95f, 0.95f, 0.94f), 4f, 0.45f, 0f, glitch);
                concrete = CityMaterials.PBR("Concrete046", new Color(0.88f, 0.87f, 0.85f), 3f, 1f, 0f, glitch);
                granite = CityMaterials.Solid(new Color(0.82f, 0.84f, 0.86f), 0.3f, 0.6f, glitch);
            }
            if (roof)
            {
                paving = CityMaterials.PBR("Concrete042A", new Color(0.62f, 0.62f, 0.62f), 3f, 1f, 0f, glitch);
                granite = CityMaterials.Solid(new Color(0.3f, 0.31f, 0.33f), 0.5f, 0.5f, glitch);
            }
            if (plant)
            {
                paving = CityMaterials.PBR("Concrete042A", new Color(0.78f, 0.77f, 0.74f), 3f, 1f, 0f, glitch);
                granite = yellow;
            }
            if (valley)
            {
                paving = CityMaterials.PBR("Grass005", new Color(1f, 1.05f, 0.88f), 3f, 1f, 0f, glitch);
                concrete = CityMaterials.PBR("Rock064", new Color(0.8f, 0.78f, 0.75f), 4f, 1f, 0f, glitch);
                granite = CityMaterials.PBR("Rock058", new Color(0.78f, 0.76f, 0.74f), 2.5f, 1f, 0f, glitch);
                gravel = CityMaterials.PBR("Gravel042", new Color(0.7f, 0.68f, 0.64f), 2f, 1f, 0f, glitch);
                grating = CityMaterials.PBR("WoodFloor064", new Color(0.75f, 0.66f, 0.55f), 1.5f, 1f, 0f, glitch);
                steel = CityMaterials.PBR("Wood066", new Color(0.55f, 0.45f, 0.36f), 1.2f, 1f, 0f, glitch);
                yellow = CityMaterials.PBR("Wood066", new Color(0.62f, 0.52f, 0.42f), 1.2f, 1f, 0f, glitch);
            }
        }

        public void Build()
        {
            Mats();
            var segs = new List<Vector3>(plan.ground);
            segs.Sort((a, b) => a.x.CompareTo(b.x));
            foreach (var g in segs) Ground(g.x, g.y, g.z);
            for (int i = 0; i + 1 < segs.Count; i++)
            {
                float a = segs[i].y, b = segs[i + 1].x;
                if (b - a > 0.05f) Pit(a, b);
            }
            foreach (var p in plan.platforms) Platform(p);
            foreach (var w in plan.walls) Wall(w.x, w.y, w.z);
            foreach (var c in plan.ceilings) Overpass(c.x, c.y, c.z);
        }

        // ------------------------------------------------------------------ ground segments
        void Ground(float x0, float x1, float top)
        {
            var go = new GameObject($"Ground_{x0:0}_{x1:0}");
            go.transform.SetParent(geo, false);
            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3((x0 + x1) / 2f, top - 8f, 0f);
            col.size = new Vector3(x1 - x0, 16f, depth);
            float w = x1 - x0;
            if (roof) { Rooftop(go, x0, x1, top, w); return; }
            if (top <= 3.01f || valley || lab)
            {
                // sidewalk / plaza slab: paving on top, concrete faces (seen in the pits and at steps)
                var mb = new MeshBuilder(3);
                mb.Box(new Vector3(x0, top - 16f, Front), new Vector3(w, 16f, Back - Front), 1, 0, float.NaN, MeshBuilder.Faces.Sides | MeshBuilder.Faces.Top);
                // granite kerb line where a raised plaza meets street level, and along the road
                if (!valley && !lab) mb.BoxC(new Vector3((x0 + x1) / 2f, top - 0.1f, Back - 0.15f), new Vector3(w, 0.3f, 0.32f), 2, MeshBuilder.Faces.Top | MeshBuilder.Faces.Back | MeshBuilder.Faces.Front);
                if (top > 0.5f)
                {
                    // raised plaza: a low granite edge on the camera side and a railing at the road side
                    mb.BoxC(new Vector3((x0 + x1) / 2f, top + 0.06f, Front + 0.2f), new Vector3(w, 0.12f, 0.4f), 2);
                }
                Finish(mb, go, new[] { paving, concrete, granite });
                if (top > 0.5f && !valley) Railing(x0 + 0.3f, x1 - 0.3f, top, Back - 0.6f);
                if (plant)
                {
                    var lines = new MeshBuilder(1);
                    lines.BoxC(new Vector3((x0 + x1) / 2f, top + 0.004f, -3.2f), new Vector3(w, 0.008f, 0.12f), 0, MeshBuilder.Faces.Top);
                    lines.BoxC(new Vector3((x0 + x1) / 2f, top + 0.004f, 3.2f), new Vector3(w, 0.008f, 0.12f), 0, MeshBuilder.Faces.Top);
                    SceneSpawn.MarkStatic(lines.ToObject("SafetyLines", go.transform, new[] { yellow }, false));
                }
                if (valley) LedgeDressing(x0, x1, top, go.transform);
            }
            else
            {
                // roof of a low building: shopfronts on the camera side, rooftop paving, parapet at the back
                var style = plant ? CityMaterials.FacadeStyle.Industrial : rnd.NextDouble() < 0.5 ? CityMaterials.FacadeStyle.Stone : CityMaterials.FacadeStyle.Plaster;
                var mb = new MeshBuilder(3);
                mb.Box(new Vector3(x0, top - 16f, Front), new Vector3(w, 16f, Back - Front), 0, 2, 0f, MeshBuilder.Faces.Sides);
                mb.Box(new Vector3(x0, top - 0.05f, Front), new Vector3(w, 0.05f, Back - Front), 1, 1, float.NaN, MeshBuilder.Faces.Top);
                mb.BoxC(new Vector3((x0 + x1) / 2f, top + 0.5f, Back - 0.15f), new Vector3(w, 1.0f, 0.3f), 2);    // back parapet
                mb.BoxC(new Vector3((x0 + x1) / 2f, top - 0.12f, Front - 0.12f), new Vector3(w + 0.1f, 0.24f, 0.24f), 2); // roof edge coping
                // shop awnings at street level
                for (float x = x0 + 1.5f; x < x1 - 3f; x += R(6f, 9f))
                    mb.BoxC(new Vector3(x + 1.5f, 3.0f, Front - 0.6f), new Vector3(3f, 0.12f, 1.2f), 2);
                Finish(mb, go, new[] { CityMaterials.Facade(style, rnd.Next(4), glitch), paving, BuildingGen.Trim(style, glitch) });
                // a little rooftop life: AC units and a vent at the back, clear of the walking line
                var extra = new MeshBuilder(1);
                for (float x = x0 + R(2f, 5f); x < x1 - 2f; x += R(7f, 12f))
                    extra.BoxC(new Vector3(x, top + 0.55f, Back - 1.4f), new Vector3(R(1.2f, 2f), 1.1f, 1.2f), 0);
                if (extra.VertexCount > 0) SceneSpawn.MarkStatic(extra.ToObject("RooftopUnits", go.transform, new[] { BuildingGen.Trim(CityMaterials.FacadeStyle.Industrial, glitch) }));
            }
            SceneSpawn.MarkStatic(go);
        }

        /// <summary>The Core: a skyscraper rooftop; the tower's glass facade drops all the way to the street.</summary>
        void Rooftop(GameObject go, float x0, float x1, float top, float w)
        {
            const float street = -220f;
            var mb = new MeshBuilder(3);
            mb.Box(new Vector3(x0, street, Front), new Vector3(w, top - street, Back - Front + 8f), 0, 1, street, MeshBuilder.Faces.Sides);
            mb.Box(new Vector3(x0, top - 0.05f, Front), new Vector3(w, 0.05f, Back - Front + 8f), 1, 1, float.NaN, MeshBuilder.Faces.Top);
            mb.BoxC(new Vector3((x0 + x1) / 2f, top + 0.55f, Front + 0.12f), new Vector3(w, 1.1f, 0.24f), 2);         // parapet towards the camera
            mb.BoxC(new Vector3((x0 + x1) / 2f, top + 0.55f, Back + 7.8f), new Vector3(w, 1.1f, 0.24f), 2);
            for (float x = x0 + R(2f, 5f); x < x1 - 2f; x += R(7f, 12f))                                             // rooftop plant at the back
                mb.BoxC(new Vector3(x, top + 0.8f, Back + R(2f, 5f)), new Vector3(R(2f, 3.5f), 1.6f, R(1.5f, 2.5f)), 2);
            Finish(mb, go, new[] { CityMaterials.Facade(CityMaterials.FacadeStyle.GlassTower, rnd.Next(4), glitch), paving, BuildingGen.Trim(CityMaterials.FacadeStyle.GlassTower, glitch) });
            SceneSpawn.MarkStatic(go);
        }

        /// <summary>A gap between two towers: nothing to stand on, the street is 220 m below. Falling respawns.</summary>
        void RoofGap(float x0, float x1)
        {
            var trig = new GameObject($"RoofGap_{x0:0}");
            trig.transform.SetParent(geo, false);
            trig.transform.localPosition = new Vector3((x0 + x1) / 2f, -12f, 0f);
            var box = trig.AddComponent<BoxCollider>();
            box.isTrigger = true; box.size = new Vector3(x1 - x0 + 1f, 4f, depth);
            var hz = trig.AddComponent<Hazard>();
            hz.respawn = true; hz.line = "Don't look down. I looked down. Do not recommend.";
        }

        /// <summary>Valley paths: a few rocks and grass tufts at the back edge, clear of the walking line.</summary>
        void LedgeDressing(float x0, float x1, float top, Transform owner)
        {
            var nat = new NatureGen(owner, rnd);
            for (float x = x0 + R(1f, 3f); x < x1 - 1f; x += R(2.5f, 6f))
            {
                if (rnd.NextDouble() < 0.35) nat.Rock(new Vector3(x, top - 0.15f, R(3.4f, 5.4f)), R(0.3f, 0.8f));
                nat.GrassTuft(new Vector3(x + R(-1f, 1f), top, R(3.2f, 5.6f)), R(0.25f, 0.45f));
                if (rnd.NextDouble() < 0.4) nat.GrassTuft(new Vector3(x + R(-1f, 1f), top, R(-5.6f, -3.2f)), R(0.2f, 0.35f));
            }
            nat.Flush("LedgeDressing");
        }

        void Finish(MeshBuilder mb, GameObject owner, Material[] mats)
        {
            var vis = mb.ToObject("Visual", owner.transform, mats);
            SceneSpawn.MarkStatic(vis);
        }

        void Railing(float x0, float x1, float y, float z)
        {
            var mb = new MeshBuilder(1);
            mb.BoxC(new Vector3((x0 + x1) / 2f, y + 1.05f, z), new Vector3(x1 - x0, 0.06f, 0.06f), 0);
            mb.BoxC(new Vector3((x0 + x1) / 2f, y + 0.55f, z), new Vector3(x1 - x0, 0.03f, 0.03f), 0);
            for (float x = x0; x <= x1 + 0.01f; x += 1.6f) mb.BoxC(new Vector3(x, y + 0.52f, z), new Vector3(0.05f, 1.05f, 0.05f), 0);
            SceneSpawn.MarkStatic(mb.ToObject("Railing", geo, new[] { steel }));
        }

        // ------------------------------------------------------------------ pits (gaps)
        void Pit(float x0, float x1)
        {
            var go = new GameObject($"Pit_{x0:0}_{x1:0}");
            go.transform.SetParent(geo, false);
            var mb = new MeshBuilder(4);
            float w = x1 - x0;
            mb.Box(new Vector3(x0 - 0.5f, PitFloor - 0.5f, Front), new Vector3(w + 1f, 0.5f, Back - Front), 0, 0, float.NaN, MeshBuilder.Faces.Top);
            // retaining wall at the road side (the road continues above it)
            mb.Box(new Vector3(x0, PitFloor, Back), new Vector3(w, -PitFloor, 0.3f), 1, 1, float.NaN, MeshBuilder.Faces.Front | MeshBuilder.Faces.Top);
            // site clutter on the pit floor: concrete blocks, a puddle, rebar
            for (int i = 0; i < Mathf.Max(1, (int)(w / 4f)); i++)
                mb.BoxC(new Vector3(R(x0 + 0.8f, x1 - 0.8f), PitFloor + 0.3f, R(0f, Back - 1.5f)), new Vector3(R(0.8f, 1.6f), 0.6f, R(0.6f, 1.2f)), 1);
            mb.BoxC(new Vector3((x0 + x1) / 2f, PitFloor + 0.01f, R(-3f, 0f)), new Vector3(Mathf.Min(w * 0.6f, 3f), 0.01f, 1.6f), 2, MeshBuilder.Faces.Top);
            for (int i = 0; i < 5; i++)
            {
                var b = new Vector3(R(x0 + 0.5f, x1 - 0.5f), PitFloor, R(1f, Back - 0.5f));
                mb.Tube(b, b + new Vector3(R(-0.2f, 0.2f), R(1f, 2f), R(-0.2f, 0.2f)), 0.02f, 0.02f, 5, 3);
            }
            if (roof) { RoofGap(x0, x1); return; }
            var puddle = valley ? CityMaterials.Water(new Color(0.03f, 0.07f, 0.07f), new Color(0.07f, 0.13f, 0.12f), new Vector2(0.8f, 0f))
                       : lab ? CityMaterials.Water(new Color(0.03f, 0.08f, 0.09f), new Color(0.08f, 0.18f, 0.2f), new Vector2(0.1f, 0f))
                       : plant ? CityMaterials.Water(new Color(0.03f, 0.07f, 0.03f), new Color(0.06f, 0.13f, 0.05f), new Vector2(0.15f, 0f), new Color(0.03f, 0.12f, 0.02f))
                       : CityMaterials.Solid(new Color(0.05f, 0.06f, 0.07f), 0.05f, 0f, 0.5f);
            if (valley || plant || lab) mb.Box(new Vector3(x0 - 0.5f, PitFloor + 1.2f, Front), new Vector3(w + 1f, 0.01f, Back - Front), 2, 2, float.NaN, MeshBuilder.Faces.Top);   // stream in the ravine
            var vis = mb.ToObject("PitVisual", go.transform, new[] { gravel, concrete, puddle, steel });
            SceneSpawn.MarkStatic(vis);
            // landing in the pit = fall respawn (it is a real place, not a void)
            var trig = new GameObject("PitRespawn");
            trig.transform.SetParent(go.transform, false);
            trig.transform.localPosition = new Vector3((x0 + x1) / 2f, PitFloor + 0.8f, 0f);
            var box = trig.AddComponent<BoxCollider>();
            box.isTrigger = true; box.size = new Vector3(w + 1f, 1.6f, depth);
            var hz = trig.AddComponent<Hazard>();
            hz.respawn = true;
            hz.line = valley ? "Cold! Cold cold cold. Physics fact: water is very good at stealing heat."
                    : plant ? "That trench is full of something green. Green means 'acid' AND 'no'."
                    : lab ? "Maintenance trench. Very clean. Very wet. Very embarrassing."
                    : "Construction site. Hard hats only. I have neither a hat nor a hard head.";
            // warning barriers on both edges, behind the walking line
            if (!valley && !lab)
            {
                Barrier(x0 - 0.6f, plan.GroundY(x0 - 0.6f));
                Barrier(x1 + 0.6f, plan.GroundY(x1 + 0.6f));
            }
        }

        void Barrier(float x, float y)
        {
            if (y < -5f) return;
            var mb = new MeshBuilder(2);
            var p = new Vector3(x, y, Back - 1.2f);
            mb.BoxC(p + new Vector3(0, 0.5f, 0), new Vector3(0.08f, 1.0f, 1.4f), 0);
            for (int i = 0; i < 3; i++) mb.BoxC(p + new Vector3(0, 0.35f + i * 0.28f, 0), new Vector3(0.1f, 0.16f, 1.5f), i % 2 == 0 ? 1 : 0);
            SceneSpawn.MarkStatic(mb.ToObject("Barrier", geo, new[] { CityMaterials.Solid(new Color(0.92f, 0.92f, 0.9f), 0.6f), CityMaterials.Solid(new Color(0.95f, 0.38f, 0.08f), 0.55f) }));
        }

        // ------------------------------------------------------------------ platforms
        void Platform(PlatformDef p)
        {
            bool moving = p.move != Vector2.zero;
            var go = new GameObject(moving ? "Gondola" : "Scaffold");
            go.transform.SetParent(geo, false);
            go.transform.localPosition = new Vector3(p.x, p.y - 0.4f, 0);
            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(p.w, 0.8f, depth * 0.7f);
            float hw = p.w / 2f, hd = depth * 0.35f;
            var mb = new MeshBuilder(4);
            // deck: grating top, steel frame with a yellow safety edge facing the camera
            mb.Box(new Vector3(-hw, 0.25f, -hd), new Vector3(p.w, 0.15f, hd * 2f), 1, 0, float.NaN, MeshBuilder.Faces.All);
            mb.BoxC(new Vector3(0, 0.12f, -hd), new Vector3(p.w, 0.3f, 0.08f), 2);
            mb.BoxC(new Vector3(0, 0.12f, hd), new Vector3(p.w, 0.3f, 0.08f), 1);
            if (moving || roof)
            {
                // gondola: back rail and steel cables running up out of view (on the rooftops every platform hangs)
                mb.BoxC(new Vector3(0, 1.4f, hd - 0.05f), new Vector3(p.w, 0.06f, 0.06f), 1);
                foreach (float x in new[] { -hw + 0.1f, hw - 0.1f })
                {
                    mb.BoxC(new Vector3(x, 0.9f, hd - 0.05f), new Vector3(0.06f, 1.0f, 0.06f), 1);
                    mb.Tube(new Vector3(x, 1.4f, hd - 0.05f), new Vector3(x, 60f, hd - 0.05f), 0.02f, 0.02f, 4, 3);
                }
                if (moving)
                {
                    var mp = go.AddComponent<MovingPlatform>();
                    mp.offset = new Vector3(p.move.x, p.move.y, 0);
                    mp.period = p.period;
                    mp.phase = R(0, 1);
                }
                else SceneSpawn.MarkStatic(go);
            }
            else
            {
                // scaffold legs down to whatever is below (ground, plaza or pit floor), with cross bracing
                float below = plan.GroundY(p.x);
                if (below < -5f) below = PitFloor;
                float legH = p.y - 0.4f - below;
                if (legH > 0.3f)
                    foreach (float x in new[] { -hw + 0.15f, hw - 0.15f })
                        foreach (float z in new[] { -hd + 0.1f, hd - 0.1f })
                            mb.Tube(new Vector3(x, 0.1f, z), new Vector3(x, -legH, z), 0.04f, 0.04f, 6, 1);
                if (legH > 1.5f)
                    for (float y = -legH + 0.8f; y < -0.4f; y += 1.8f)
                    {
                        mb.Tube(new Vector3(-hw + 0.15f, y, hd - 0.1f), new Vector3(hw - 0.15f, y + 1.2f, hd - 0.1f), 0.03f, 0.03f, 5, 1);
                        mb.Tube(new Vector3(-hw + 0.15f, y, -hd + 0.1f), new Vector3(hw - 0.15f, y, -hd + 0.1f), 0.03f, 0.03f, 5, 1);
                    }
                SceneSpawn.MarkStatic(go);
            }
            var vis = mb.ToObject("Visual", go.transform, new[] { grating, steel, yellow, cable });
            if (!moving) SceneSpawn.MarkStatic(vis);
        }

        void Wall(float x, float y0, float y1)
        {
            var go = new GameObject("Wall");
            go.transform.SetParent(geo, false);
            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(x, (y0 + y1) / 2f, 0); col.size = new Vector3(1f, y1 - y0, depth);
            var mb = new MeshBuilder(1);
            mb.Box(new Vector3(x - 0.5f, y0, Front + 1f), new Vector3(1f, y1 - y0, Back - Front - 1f), 0, 0, float.NaN, MeshBuilder.Faces.All);
            Finish(mb, go, new[] { concrete });
            SceneSpawn.MarkStatic(go);
        }

        void Overpass(float x0, float x1, float bottom)
        {
            var go = new GameObject("Overpass");
            go.transform.SetParent(geo, false);
            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3((x0 + x1) / 2f, bottom + 5f, 0); col.size = new Vector3(x1 - x0, 10f, depth);
            var mb = new MeshBuilder(2);
            mb.Box(new Vector3(x0, bottom, Front), new Vector3(x1 - x0, 1.4f, Back - Front + 14f), 0, 0, float.NaN, MeshBuilder.Faces.All);
            mb.BoxC(new Vector3((x0 + x1) / 2f, bottom + 1.9f, Front + 0.2f), new Vector3(x1 - x0, 1.0f, 0.3f), 1);
            Finish(mb, go, new[] { concrete, steel });
            SceneSpawn.MarkStatic(go);
        }
    }
}
