using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Street-level dressing for the realistic city: roads, curbs, sidewalks, lane markings, street lights, trees,
    /// benches, bins, bollards, hydrants, bus shelters and traffic lights. Static pieces are merged into one mesh per
    /// chunk (few draw calls on low-end GPUs); only a handful of real lights are created near the gameplay plane.
    /// </summary>
    public class StreetGen
    {
        // sub-mesh slots of the merged furniture mesh
        public const int Metal = 0, Concrete = 1, Wood = 2, Lamp = 3, Glass = 4, Bark = 5, Leaves = 6, Rubber = 7, Red = 8, Yellow = 9, Paint = 10, Grate = 11, Count = 12;

        readonly Transform parent;
        readonly System.Random rnd;
        readonly float glitch;
        readonly Dictionary<int, MeshBuilder> chunks = new Dictionary<int, MeshBuilder>();
        public int lightsMade, maxLights = 14;
        public float chunkSize = 60f;

        public StreetGen(Transform parent, System.Random rnd, float glitch)
        {
            this.parent = parent; this.rnd = rnd; this.glitch = glitch;
        }

        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

        MeshBuilder At(float x)
        {
            int k = Mathf.FloorToInt(x / chunkSize);
            if (!chunks.TryGetValue(k, out var mb)) chunks[k] = mb = new MeshBuilder(Count);
            return mb;
        }

        public Material[] Mats() => new[]
        {
            CityMaterials.PBR("PaintedMetal004", new Color(0.18f, 0.19f, 0.2f), 1.5f, 0.8f, 0.6f, glitch),
            CityMaterials.PBR("Concrete046", new Color(0.72f, 0.71f, 0.68f), 2f, 1f, 0f, glitch),
            CityMaterials.PBR("Wood066", new Color(0.8f, 0.7f, 0.6f), 1.2f, 1f, 0f, glitch),
            CityMaterials.Emissive(new Color(1f, 0.95f, 0.85f), new Color(6f, 5f, 3.6f)),
            CityMaterials.Glass(new Color(0.5f, 0.6f, 0.65f)),
            CityMaterials.PBR("Bark014", Color.white, 1.2f, 1f, 0f, glitch),
            CityMaterials.Leaves(0),
            CityMaterials.PBR("Rubber004", new Color(0.25f, 0.25f, 0.25f), 0.6f, 1f, 0f, glitch),
            CityMaterials.Solid(new Color(0.6f, 0.08f, 0.07f), 0.45f, 0f, glitch),
            CityMaterials.Solid(new Color(0.85f, 0.65f, 0.1f), 0.5f, 0f, glitch),
            CityMaterials.Solid(new Color(0.9f, 0.9f, 0.88f), 0.6f, 0f, 0.5f),
            CityMaterials.PBR("DiamondPlate009", new Color(0.35f, 0.35f, 0.35f), 1f, 0.8f, 0.8f, glitch),
        };

        /// <summary>Writes all merged chunks into the scene.</summary>
        public void Flush(string name)
        {
            var mats = Mats();
            foreach (var kv in chunks)
            {
                var go = kv.Value.ToObject($"{name}_{kv.Key}", parent, mats);
                SceneSpawn.MarkStatic(go);
            }
            chunks.Clear();
        }

        // ------------------------------------------------------------------ roads
        public void Road(float x0, float x1, float zNear, float zFar, float y = 0f)
        {
            var mb = new MeshBuilder(3);
            mb.Box(new Vector3(x0, y - 0.3f, zNear), new Vector3(x1 - x0, 0.3f, zFar - zNear), 0, 0, float.NaN, MeshBuilder.Faces.Top);
            float mid = (zNear + zFar) / 2f;
            // double yellow centre line, dashed white lane lines
            mb.BoxC(new Vector3((x0 + x1) / 2, y + 0.005f, mid - 0.12f), new Vector3(x1 - x0, 0.01f, 0.12f), 1, MeshBuilder.Faces.Top);
            mb.BoxC(new Vector3((x0 + x1) / 2, y + 0.005f, mid + 0.12f), new Vector3(x1 - x0, 0.01f, 0.12f), 1, MeshBuilder.Faces.Top);
            float lane = (zFar - zNear) / 4f;
            foreach (float lz in new[] { zNear + lane, zFar - lane })
                for (float x = x0; x < x1; x += 9f)
                    mb.BoxC(new Vector3(x + 1.5f, y + 0.005f, lz), new Vector3(3f, 0.01f, 0.13f), 2, MeshBuilder.Faces.Top);
            var go = mb.ToObject("Road", parent, new[]
            {
                CityMaterials.PBR("Asphalt031", new Color(0.62f, 0.62f, 0.62f), 6f, 1f, 0f, glitch),
                CityMaterials.Solid(new Color(0.85f, 0.66f, 0.15f), 0.6f, 0f, 0.4f),
                CityMaterials.Solid(new Color(0.88f, 0.88f, 0.86f), 0.6f, 0f, 0.4f),
            }, false);
            SceneSpawn.MarkStatic(go);
        }

        /// <summary>Sidewalk slab with a granite curb along its road side.</summary>
        public void Sidewalk(float x0, float x1, float zNear, float zFar, bool curbOnFar, float y = 0f)
        {
            var mb = new MeshBuilder(2);
            mb.Box(new Vector3(x0, y - 0.6f, zNear), new Vector3(x1 - x0, 0.6f + 0.15f, zFar - zNear), 1, 0, float.NaN, MeshBuilder.Faces.Top | MeshBuilder.Faces.Front | MeshBuilder.Faces.Back);
            float cz = curbOnFar ? zFar - 0.15f : zNear + 0.15f;
            mb.BoxC(new Vector3((x0 + x1) / 2, y + 0.07f, cz), new Vector3(x1 - x0, 0.17f, 0.3f), 1, MeshBuilder.Faces.Top | MeshBuilder.Faces.Front | MeshBuilder.Faces.Back);
            var go = mb.ToObject("Sidewalk", parent, new[]
            {
                CityMaterials.PBR("PavingStones138", new Color(0.82f, 0.8f, 0.76f), 1.5f, 1f, 0f, glitch),
                CityMaterials.PBR("Granite002A", new Color(0.7f, 0.7f, 0.7f), 1.5f, 0.9f, 0f, glitch),
            });
            SceneSpawn.MarkStatic(go);
        }

        public void Crosswalk(float xCenter, float zNear, float zFar, float y = 0f)
        {
            var mb = At(xCenter);
            for (float z = zNear + 0.6f; z < zFar - 0.4f; z += 1.1f)
                mb.BoxC(new Vector3(xCenter, y + 0.008f, z), new Vector3(4f, 0.012f, 0.55f), Paint, MeshBuilder.Faces.Top);
        }

        // ------------------------------------------------------------------ furniture
        /// <summary>Street light on the sidewalk, arm reaching towards <paramref name="armDir"/> (+1 = +z).</summary>
        public void StreetLight(Vector3 p, float armDir, bool realLight)
        {
            var mb = At(p.x);
            mb.Cylinder(p, 0.22f, 0.2f, 0.5f, 10, Metal);                           // base
            mb.Cylinder(p + Vector3.up * 0.5f, 0.1f, 0.075f, 6.8f, 10, Metal);      // pole
            var top = p + Vector3.up * 7.2f;
            var end = top + new Vector3(0, 0.25f, armDir * 1.8f);
            mb.Tube(top, end, 0.06f, 0.05f, 8, Metal);                             // arm
            mb.BoxC(end + new Vector3(0, -0.05f, armDir * 0.25f), new Vector3(0.36f, 0.14f, 0.7f), Metal);
            mb.BoxC(end + new Vector3(0, -0.13f, armDir * 0.25f), new Vector3(0.28f, 0.03f, 0.56f), Lamp, MeshBuilder.Faces.Bottom);
            if (realLight && lightsMade < maxLights)
            {
                lightsMade++;
                var lg = new GameObject("StreetLamp");
                lg.transform.SetParent(parent, false);
                lg.transform.localPosition = end + new Vector3(0, -0.3f, armDir * 0.25f);
                lg.transform.localRotation = Quaternion.Euler(90, 0, 0);
                var l = lg.AddComponent<Light>();
                l.type = LightType.Spot;
                l.spotAngle = 120f; l.innerSpotAngle = 60f;
                l.range = 14f; l.intensity = 9f;
                l.color = new Color(1f, 0.86f, 0.66f);
                l.shadows = LightShadows.None;
                l.renderMode = LightRenderMode.Auto;
                lg.AddComponent<WorldLight>();
            }
        }

        public void Tree(Vector3 p, float height)
        {
            var mb = At(p.x);
            float trunkH = height * 0.42f;
            mb.Cylinder(p, 0.16f * height / 7f, 0.11f * height / 7f, trunkH, 9, Bark, false);
            // tree grate around the base
            mb.BoxC(p + new Vector3(0, 0.02f, 0), new Vector3(1.4f, 0.04f, 1.4f), Grate, MeshBuilder.Faces.Top);
            var crown = p + Vector3.up * (trunkH + height * 0.25f);
            // a few branches up into the crown
            for (int i = 0; i < 4; i++)
            {
                float a = i * 1.57f + R(-0.4f, 0.4f);
                var dir = new Vector3(Mathf.Cos(a) * 0.6f, 1f, Mathf.Sin(a) * 0.6f).normalized;
                mb.Tube(p + Vector3.up * trunkH * 0.9f, p + Vector3.up * trunkH * 0.9f + dir * height * 0.3f, 0.07f, 0.03f, 6, Bark);
            }
            // leaf cards around an ellipsoid crown, facing outwards
            float rx = height * 0.32f, ry = height * 0.3f;
            int cards = Mathf.RoundToInt(26 + height * 3);
            var uv = new Rect(0f, 0f, 0.5f, 0.5f);
            for (int i = 0; i < cards; i++)
            {
                var n = Random3();
                var c = crown + new Vector3(n.x * rx, n.y * ry, n.z * rx) * R(0.55f, 1.0f);
                float s = R(0.9f, 1.4f) * height / 7f;
                var right = Vector3.Cross(n, Vector3.up).normalized;
                if (right.sqrMagnitude < 0.01f) right = Vector3.right;
                right = Quaternion.AngleAxis(R(0, 360), n) * right;
                var up = Vector3.Cross(right, n).normalized;
                var leafN = (n + Vector3.up * 0.6f).normalized;   // soft, sky-facing normals: no dark card edges
                mb.Card(c, right * s, up * s, Leaves, leafN, uv);
            }
        }

        Vector3 Random3()
        {
            float u = R(-1f, 1f), th = R(0f, Mathf.PI * 2f);
            float r = Mathf.Sqrt(1f - u * u);
            return new Vector3(r * Mathf.Cos(th), u, r * Mathf.Sin(th));
        }

        public void Bench(Vector3 p)
        {
            var mb = At(p.x);
            foreach (float dx in new[] { -0.75f, 0.75f })
            {
                mb.BoxC(p + new Vector3(dx, 0.22f, 0), new Vector3(0.06f, 0.44f, 0.5f), Metal);
                mb.BoxC(p + new Vector3(dx, 0.62f, 0.2f), new Vector3(0.06f, 0.42f, 0.06f), Metal);
            }
            for (int i = 0; i < 3; i++) mb.BoxC(p + new Vector3(0, 0.46f, -0.17f + i * 0.15f), new Vector3(1.8f, 0.04f, 0.12f), Wood);
            for (int i = 0; i < 2; i++) mb.BoxC(p + new Vector3(0, 0.62f + i * 0.16f, 0.23f), new Vector3(1.8f, 0.11f, 0.035f), Wood);
        }

        public void Bin(Vector3 p)
        {
            var mb = At(p.x);
            mb.Cylinder(p, 0.27f, 0.3f, 0.9f, 12, Metal);
            mb.Cylinder(p + Vector3.up * 0.9f, 0.32f, 0.32f, 0.06f, 12, Metal);
        }

        public void Bollard(Vector3 p)
        {
            var mb = At(p.x);
            mb.Cylinder(p, 0.11f, 0.1f, 0.85f, 10, Metal);
            mb.Cylinder(p + Vector3.up * 0.7f, 0.112f, 0.112f, 0.06f, 10, Paint, false);
        }

        public void Hydrant(Vector3 p)
        {
            var mb = At(p.x);
            mb.Cylinder(p, 0.16f, 0.14f, 0.6f, 10, Red);
            mb.Cylinder(p + Vector3.up * 0.6f, 0.15f, 0.05f, 0.18f, 10, Red);
            mb.Tube(p + new Vector3(-0.22f, 0.4f, 0), p + new Vector3(0.22f, 0.4f, 0), 0.06f, 0.06f, 8, Red);
        }

        public void BusShelter(Vector3 p)
        {
            var mb = At(p.x);
            foreach (float dx in new[] { -1.8f, 1.8f })
                mb.BoxC(p + new Vector3(dx, 1.2f, 0.6f), new Vector3(0.08f, 2.4f, 0.08f), Metal);
            mb.BoxC(p + new Vector3(0, 2.45f, 0.1f), new Vector3(3.9f, 0.1f, 1.6f), Metal);
            mb.BoxC(p + new Vector3(0, 1.25f, 0.62f), new Vector3(3.6f, 2.2f, 0.03f), Glass);
            mb.BoxC(p + new Vector3(-1.8f, 1.25f, 0.15f), new Vector3(0.03f, 2.2f, 0.9f), Glass);
            mb.BoxC(p + new Vector3(0, 0.45f, 0.45f), new Vector3(2.4f, 0.05f, 0.35f), Wood);
        }

        public void TrafficLight(Vector3 p, float facing)
        {
            var mb = At(p.x);
            mb.Cylinder(p, 0.1f, 0.09f, 3.6f, 10, Metal);
            mb.BoxC(p + new Vector3(0, 3.95f, facing * -0.12f), new Vector3(0.32f, 0.9f, 0.24f), Metal);
            mb.BoxC(p + new Vector3(0, 4.22f, facing * -0.25f), new Vector3(0.18f, 0.18f, 0.02f), Red);
            mb.BoxC(p + new Vector3(0, 3.68f, facing * -0.25f), new Vector3(0.18f, 0.18f, 0.02f), Lamp);
        }

        public void Planter(Vector3 p, float length)
        {
            var mb = At(p.x);
            mb.BoxC(p + new Vector3(0, 0.3f, 0), new Vector3(length, 0.6f, 0.8f), Concrete);
            var uv = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
            for (float x = -length / 2 + 0.3f; x < length / 2 - 0.2f; x += 0.45f)
            {
                var c = p + new Vector3(x, 0.85f, R(-0.15f, 0.15f));
                mb.Card(c, new Vector3(0.45f, 0, 0), new Vector3(0, 0.35f, 0), Leaves, Vector3.up, uv);
                mb.Card(c, new Vector3(0, 0, 0.35f), new Vector3(0, 0.35f, 0), Leaves, Vector3.up, uv);
            }
        }
    }
}
