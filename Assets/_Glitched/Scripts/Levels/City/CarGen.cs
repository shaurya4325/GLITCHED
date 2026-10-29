using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Procedural cars for traffic and parking: sedan, hatchback, SUV, van and taxi. Body from an extruded side
    /// profile, inset glass cabin, rubber wheels with metal hubs, head/tail lights. Car faces +X.
    /// </summary>
    public static class CarGen
    {
        public enum Kind { Sedan, Hatch, SUV, Van, Taxi }

        static readonly Color[] Paints =
        {
            new Color(0.75f, 0.76f, 0.78f), new Color(0.1f, 0.1f, 0.11f), new Color(0.92f, 0.92f, 0.9f), new Color(0.32f, 0.34f, 0.37f),
            new Color(0.45f, 0.06f, 0.06f), new Color(0.1f, 0.18f, 0.35f), new Color(0.55f, 0.55f, 0.5f), new Color(0.18f, 0.25f, 0.2f),
        };

        public static GameObject Build(Transform parent, Vector3 pos, float yaw, System.Random rnd, Kind? kind = null, float glitch = 0.6f)
        {
            var k = kind ?? (Kind)rnd.Next(4);
            Color paint = k == Kind.Taxi ? new Color(0.95f, 0.72f, 0.1f) : Paints[rnd.Next(Paints.Length)];
            float L, W, wheelR = 0.33f;
            Vector2[] body, cabin;
            switch (k)
            {
                case Kind.Hatch:
                    L = 4.0f; W = 1.75f;
                    body = new[] { V(-2.0f, 0.3f), V(2.0f, 0.3f), V(2.05f, 0.62f), V(1.4f, 0.86f), V(-1.95f, 0.9f), V(-2.02f, 0.6f) };
                    cabin = new[] { V(-1.85f, 0.88f), V(1.2f, 0.86f), V(0.35f, 1.42f), V(-1.6f, 1.45f), V(-1.9f, 1.15f) };
                    break;
                case Kind.SUV:
                    L = 4.7f; W = 1.9f; wheelR = 0.38f;
                    body = new[] { V(-2.35f, 0.38f), V(2.35f, 0.38f), V(2.4f, 0.8f), V(1.6f, 1.05f), V(-2.3f, 1.08f), V(-2.38f, 0.75f) };
                    cabin = new[] { V(-2.2f, 1.05f), V(1.45f, 1.05f), V(0.7f, 1.75f), V(-2.1f, 1.78f) };
                    break;
                case Kind.Van:
                    L = 5.2f; W = 1.95f; wheelR = 0.36f;
                    body = new[] { V(-2.6f, 0.38f), V(2.6f, 0.38f), V(2.62f, 0.95f), V(2.2f, 1.2f), V(-2.58f, 1.2f), V(-2.62f, 0.9f) };
                    cabin = new[] { V(-2.55f, 1.18f), V(2.05f, 1.18f), V(1.45f, 2.1f), V(-2.5f, 2.15f) };
                    break;
                default: // sedan / taxi
                    L = 4.6f; W = 1.8f;
                    body = new[] { V(-2.3f, 0.3f), V(2.3f, 0.3f), V(2.35f, 0.62f), V(1.35f, 0.82f), V(-1.6f, 0.86f), V(-2.3f, 0.8f), V(-2.35f, 0.58f) };
                    cabin = new[] { V(-1.5f, 0.84f), V(1.15f, 0.82f), V(0.35f, 1.38f), V(-1.05f, 1.4f) };
                    break;
            }
            var mb = new MeshBuilder(6); // 0 paint, 1 glass, 2 rubber, 3 hub metal, 4 headlight, 5 taillight
            mb.ExtrudeProfile(body, -W / 2, W / 2, 0, Vector3.zero);
            mb.ExtrudeProfile(cabin, -W / 2 + 0.09f, W / 2 - 0.09f, 1, Vector3.zero);
            // pillars / roof in paint over the glass edges
            var roofA = cabin[cabin.Length - 1]; var roofB = cabin[cabin.Length - 2];
            mb.BoxC(new Vector3((roofA.x + roofB.x) / 2, Mathf.Max(roofA.y, roofB.y) + 0.02f, 0), new Vector3(Mathf.Abs(roofB.x - roofA.x) + 0.1f, 0.05f, W - 0.16f), 0);
            float wx = L / 2 - 0.8f;
            foreach (float x in new[] { -wx, wx })
                foreach (float z in new[] { -W / 2 + 0.05f, W / 2 - 0.05f })
                {
                    var c = new Vector3(x, wheelR, z);
                    float side = Mathf.Sign(z);
                    mb.Tube(c - new Vector3(0, 0, side * 0.11f), c + new Vector3(0, 0, side * 0.11f), wheelR, wheelR, 16, 2);
                    var hub = c + new Vector3(0, 0, side * 0.115f);
                    mb.Tube(hub, hub + new Vector3(0, 0, side * 0.01f), wheelR * 0.62f, wheelR * 0.62f, 12, 3);
                }
            float fx = body[1].x + 0.04f, rx = body[0].x - 0.04f;
            foreach (float z in new[] { -W / 2 + 0.3f, W / 2 - 0.3f })
            {
                mb.BoxC(new Vector3(fx, 0.62f, z), new Vector3(0.05f, 0.12f, 0.36f), 4);
                mb.BoxC(new Vector3(rx, 0.68f, z), new Vector3(0.05f, 0.12f, 0.34f), 5);
            }
            if (k == Kind.Taxi) mb.BoxC(new Vector3(-0.35f, 1.46f, 0), new Vector3(0.6f, 0.16f, 0.25f), 4);
            var go = mb.ToObject("Car_" + k, parent, new[]
            {
                CityMaterials.Solid(paint, 0.22f, 0.55f, glitch),
                CityMaterials.Glass(new Color(0.3f, 0.35f, 0.4f)),
                CityMaterials.PBR("Rubber004", new Color(0.22f, 0.22f, 0.22f), 0.5f, 1f, 0f, glitch),
                CityMaterials.Solid(new Color(0.6f, 0.62f, 0.64f), 0.3f, 0.9f, glitch),
                CityMaterials.Emissive(new Color(1f, 1f, 0.95f), new Color(3f, 3f, 2.6f)),
                CityMaterials.Emissive(new Color(0.6f, 0.05f, 0.04f), new Color(2.4f, 0.1f, 0.06f)),
            });
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            return go;
        }

        static Vector2 V(float x, float y) => new Vector2(x, y);
    }
}
