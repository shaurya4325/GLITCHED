using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// The realistic Code City around the gameplay path (all geometry generated here, no imported kits):
    ///   z  2.7..6   back sidewalk strip (built per ground segment by LevelBuilder)
    ///   z  6..20    four-lane road with left-hand traffic, crosswalks at the cross streets
    ///   z 20..24    far sidewalk: trees, street lights, benches, a bus shelter, parked cars
    ///   z 24..60    the street-facing building row (continuous frontage, cross streets every ~100 m)
    ///   z 70..160   downtown towers;  z 200..520 skyline;  ground to the horizon, fog hides the edge.
    /// Corruption is a subtle hint: a couple of towers have their top tier floating a little off-axis.
    /// </summary>
    public class CityBackdrop
    {
        readonly LevelPlan plan;
        readonly Transform root;
        readonly System.Random rnd;
        readonly float x0, x1;
        readonly float glitch;
        Transform near, mid, far, streetRoot;
        readonly List<float> crossStreets = new List<float>();

        public const float RoadNear = 6f, RoadFar = 20f, FarWalk = 24f;
        /// <summary>Road surface height: sidewalks are at 0, so kerbs are 15 cm.</summary>
        public const float RoadY = -0.15f;

        public CityBackdrop(LevelPlan plan, Transform root)
        {
            this.plan = plan; this.root = root;
            rnd = new System.Random(plan.levelId.GetHashCode() * 17 + 3);
            x0 = plan.minX; x1 = plan.maxX;
            glitch = 1f;
        }

        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

        Transform Layer(string name, DepthLayer layer)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.AddComponent<DepthLayerTag>().layer = layer;
            return go.transform;
        }

        public void Build()
        {
            var bg = new GameObject("City").transform;
            bg.SetParent(root, false);
            near = Layer("L2 Street", DepthLayer.L2_Near); near.SetParent(bg, true);
            mid = Layer("L3 Downtown", DepthLayer.L3_Mid); mid.SetParent(bg, true);
            far = Layer("L4 Skyline", DepthLayer.L4_Far); far.SetParent(bg, true);
            streetRoot = new GameObject("StreetFurniture").transform; streetRoot.SetParent(near, false);

            float sx0 = x0 - 220, sx1 = x1 + 220;
            for (float x = x0 + R(40, 70); x < x1 + 120; x += R(95, 125)) crossStreets.Add(x);

            Ground(sx0, sx1);
            var street = new StreetGen(streetRoot, rnd, glitch);
            var roadGen = new StreetGen(near, rnd, glitch);
            roadGen.Road(sx0, sx1, RoadNear, RoadFar, RoadY);
            roadGen.Sidewalk(sx0, sx1, RoadFar, FarWalk, false, RoadY);
            foreach (var cx in crossStreets)
            {
                street.Crosswalk(cx, RoadNear, RoadFar, RoadY);
                CrossStreet(cx);
            }
            FarSidewalk(street, sx0, sx1);
            NearSidewalkDressing(street);
            street.Flush("Street");

            WallCrawlers.BeginCollect();
            BuildingRow(near, x0 - 160, x1 + 160, FarWalk, 16f, 30f, 14f, 48f, true, false);
            WallCrawlers.Finish(near, Mathf.Clamp(Mathf.RoundToInt((x1 - x0) / 18f), 6, 18));
            BuildingRow(mid, x0 - 220, x1 + 220, 70f, 22f, 36f, 36f, 120f, true, true);
            Skyline();
            Traffic();
        }

        // ------------------------------------------------------------------ ground to the horizon
        void Ground(float sx0, float sx1)
        {
            var mb = new MeshBuilder(2);
            // block interiors behind the frontage (courtyards, low roofs) and the far city floor
            mb.Box(new Vector3(sx0 - 1500, RoadY - 0.35f, FarWalk), new Vector3(sx1 - sx0 + 3000, 0.3f, 2200), 0, 0, float.NaN, MeshBuilder.Faces.Top);   // below the cross streets
            // in front of the gameplay path (what the camera sees below the player)
            mb.Box(new Vector3(sx0 - 400, -0.32f, -80f), new Vector3(sx1 - sx0 + 800, 0.3f, 74f), 1, 1, float.NaN, MeshBuilder.Faces.Top);
            var go = mb.ToObject("CityGround", near, new[]
            {
                CityMaterials.PBR("Asphalt033", new Color(0.5f, 0.5f, 0.5f), 12f, 1f, 0f, glitch),
                CityMaterials.PBR("PavingStones138", new Color(0.7f, 0.69f, 0.66f), 1.5f, 1f, 0f, glitch),
            }, false);
            SceneSpawn.MarkStatic(go);
        }

        void CrossStreet(float cx)
        {
            var mb = new MeshBuilder(2);
            mb.Box(new Vector3(cx - 7f, RoadY - 0.3f, RoadFar), new Vector3(14f, 0.3f, 260f), 0, 0, float.NaN, MeshBuilder.Faces.Top);
            // centre line going away from the camera
            mb.BoxC(new Vector3(cx, RoadY + 0.005f, RoadFar + 130f), new Vector3(0.12f, 0.01f, 260f), 1, MeshBuilder.Faces.Top);
            var go = mb.ToObject("CrossStreet", near, new[]
            {
                CityMaterials.PBR("Asphalt031", new Color(0.62f, 0.62f, 0.62f), 6f, 1f, 0f, glitch),
                CityMaterials.Solid(new Color(0.85f, 0.66f, 0.15f), 0.6f, 0f, 0.4f),
            }, false);
            SceneSpawn.MarkStatic(go);
        }

        bool NearCross(float x, float margin)
        {
            foreach (var c in crossStreets) if (Mathf.Abs(x - c) < 7f + margin) return true;
            return false;
        }

        void FarSidewalk(StreetGen s, float sx0, float sx1)
        {
            float walkZ = (RoadFar + FarWalk) / 2f;
            for (float x = sx0; x < sx1; x += 12f)
            {
                if (NearCross(x, 2f)) continue;
                s.Tree(new Vector3(x + R(-1f, 1f), 0f, walkZ - 0.4f), R(6.5f, 8.5f));
            }
            for (float x = sx0 + 6; x < sx1; x += 24f)
                if (!NearCross(x, 1f)) s.StreetLight(new Vector3(x, 0f, RoadFar + 0.6f), -1f, false);
            for (float x = sx0 + 3; x < sx1; x += R(18f, 30f))
            {
                if (NearCross(x, 3f)) continue;
                double p = rnd.NextDouble();
                var pos = new Vector3(x, 0f, FarWalk - 1.2f);
                if (p < 0.35) s.Bench(pos);
                else if (p < 0.55) s.Bin(pos);
                else if (p < 0.65) s.Hydrant(new Vector3(x, 0f, RoadFar + 0.9f));
                else if (p < 0.8) s.Planter(pos, R(2.5f, 4f));
            }
            s.BusShelter(new Vector3((x0 + x1) * 0.5f + R(-30, 30), 0f, FarWalk - 2.4f));
            foreach (var cx in crossStreets)
            {
                s.TrafficLight(new Vector3(cx - 8.5f, 0f, RoadFar + 0.8f), 1f);
                s.TrafficLight(new Vector3(cx + 8.5f, 0f, RoadNear - 0.8f), -1f);
            }
            // parked cars along the far curb
            for (float x = sx0 + 10; x < sx1; x += R(9f, 26f))
                if (!NearCross(x, 6f) && rnd.NextDouble() < 0.55) CarGen.Build(near, new Vector3(x, RoadY, RoadFar - 1.5f), 0f, rnd, null, 0.6f);   // parked on the left of the +x carriageway
        }

        /// <summary>Lights and trees on the strip behind the gameplay plane (the player's own sidewalk).</summary>
        void NearSidewalkDressing(StreetGen s)
        {
            for (float x = x0 + 8; x < x1; x += 26f)
            {
                float gy = plan.GroundY(x);
                if (gy < -5f || Mathf.Abs(gy) > 0.5f) continue;   // only on street-level ground
                s.StreetLight(new Vector3(x, gy, RoadNear - 0.7f), 1f, true);
            }
            for (float x = x0 + 20; x < x1; x += R(30f, 45f))
            {
                float gy = plan.GroundY(x);
                if (gy < -5f || Mathf.Abs(gy) > 0.5f) continue;
                s.Tree(new Vector3(x, gy, RoadNear - 1.4f), R(6f, 7.5f));
            }
        }

        // ------------------------------------------------------------------ buildings
        void BuildingRow(Transform parent, float bx0, float bx1, float frontZ, float minD, float maxD, float minH, float maxH, bool downtown, bool corruptSome)
        {
            float x = bx0;
            int corrupted = 0;
            while (x < bx1)
            {
                var spec = BuildingGen.Random(rnd, R(12f, 30f), R(minD, maxD), minH, maxH, downtown, glitch);
                float w = BuildingGen.SnappedWidth(spec);
                float d = BuildingGen.SnappedDepth(spec);
                // leave the cross streets open (front row only)
                if (frontZ < 40f)
                {
                    bool blocked = false;
                    foreach (var c in crossStreets) if (x + w > c - 7.5f && x < c + 7.5f) { x = c + 7.5f; blocked = true; break; }
                    if (blocked) continue;
                }
                if (corruptSome && corrupted < 3 && spec.height > 50 && rnd.NextDouble() < 0.35) { spec.corrupted = true; spec.setbacks = true; corrupted++; }
                float z = frontZ + d / 2f + (frontZ < 40f ? R(0f, 0.8f) : R(0f, 18f));
                BuildingGen.Build(parent, new Vector3(x + w / 2f, 0f, z), spec, rnd);
                x += w + (frontZ < 40f ? (rnd.NextDouble() < 0.2 ? R(2f, 5f) : 0.02f) : R(4f, 14f));
            }
        }

        void Skyline()
        {
            for (float x = x0 - 600; x < x1 + 600; x += R(22f, 48f))
            {
                var spec = BuildingGen.Random(rnd, R(18f, 40f), R(18f, 40f), 55f, 240f, true, glitch * 0.6f);
                spec.podiumStyle = null; spec.podiumFloors = 0;
                BuildingGen.Build(far, new Vector3(x, 0f, R(220f, 520f)), spec, rnd, "SkylineTower");
            }
        }

        // ------------------------------------------------------------------ traffic
        void Traffic()
        {
            var tpls = new GameObject("CarTemplates").transform;
            tpls.SetParent(near, false);
            var list = new List<GameObject>();
            for (int i = 0; i < 7; i++)
            {
                var car = CarGen.Build(tpls, Vector3.zero, 0f, rnd, i == 0 ? CarGen.Kind.Taxi : (CarGen.Kind?)null, 0.6f);
                car.SetActive(false);
                list.Add(car);
            }
            var go = new GameObject("RoadTraffic");
            go.transform.SetParent(near, false);
            go.transform.localPosition = new Vector3(0, RoadY, 0);
            var tr = go.AddComponent<RoadTraffic>();
            tr.templates = list;
            tr.x0 = x0 - 200; tr.x1 = x1 + 200;
            tr.carsPerLane = Mathf.Clamp(Mathf.RoundToInt((x1 - x0) / 60f), 3, 8);
            // India drives on the left: cars heading -x use the near lanes, +x the far lanes
            // lanes centred between the painted lines (dashes at 9.5 and 16.5, centre line at 13); the outer far lane is parking
            tr.lanes.Add(new RoadTraffic.Lane { z = 7.75f, dir = -1, speed = 10f });
            tr.lanes.Add(new RoadTraffic.Lane { z = 11.25f, dir = -1, speed = 13f });
            tr.lanes.Add(new RoadTraffic.Lane { z = 14.75f, dir = 1, speed = 12f });
        }
    }
}
