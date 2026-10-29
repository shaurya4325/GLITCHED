using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Procedural buildings for the realistic city. One mesh per building with three sub-meshes:
    /// 0 = facade (windows, Facade shader), 1 = roof / slab concrete, 2 = trim (cornices, parapets, rooftop machinery).
    /// Dimensions snap to the facade bay width so windows land cleanly on every face.
    /// </summary>
    public static class BuildingGen
    {
        public struct Spec
        {
            public CityMaterials.FacadeStyle style;
            public int variant;
            public float width, depth, height;     // metres (width/depth are snapped to bays)
            public CityMaterials.FacadeStyle? podiumStyle;
            public int podiumFloors;
            public bool setbacks;                  // towers step back as they rise
            public bool corrupted;                 // a subtle hint: the top tier floats a little, slightly off-axis
            public float glitch;
            public float foundation;               // extra plinth depth for uneven ground (0 = 1.2 m)
        }

        public static float BayOf(CityMaterials.FacadeStyle s)
        {
            switch (s)
            {
                case CityMaterials.FacadeStyle.Brick: return 2.6f;
                case CityMaterials.FacadeStyle.Plaster: return 2.8f;
                case CityMaterials.FacadeStyle.Stone: return 3.4f;
                case CityMaterials.FacadeStyle.Industrial: return 6f;
                default: return 3f;
            }
        }

        static float FloorOf(CityMaterials.FacadeStyle s)
        {
            switch (s)
            {
                case CityMaterials.FacadeStyle.Brick: return 3.2f;
                case CityMaterials.FacadeStyle.Plaster: return 3.1f;
                case CityMaterials.FacadeStyle.Stone: return 4.2f;
                case CityMaterials.FacadeStyle.Industrial: return 5f;
                case CityMaterials.FacadeStyle.GlassTower: return 3.8f;
                default: return 3.6f;
            }
        }

        /// <summary>A building front (the -Z face) creatures can crawl on: x range, height range, the wall's z.</summary>
        public struct Wall { public float x0, x1, y0, y1, z; }

        /// <summary>When set, every building built adds its street-facing wall here (for the wall crawlers).</summary>
        public static System.Collections.Generic.List<Wall> CollectWalls;

                /// <summary>The real width a spec will be built at (snapped to whole bays).</summary>
        public static float SnappedWidth(Spec s) => Snap(s.width, BayOf(s.style));
        public static float SnappedDepth(Spec s) => Snap(s.depth, BayOf(s.style));

        static float Snap(float v, float step) => Mathf.Max(step, Mathf.Round(v / step) * step);

        /// <summary>Builds the building with its front-left-bottom corner... centred on <paramref name="baseCenter"/> (x/z centre, y = ground).</summary>
        public static GameObject Build(Transform parent, Vector3 baseCenter, Spec s, System.Random rnd, string name = "Building")
        {
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            float bay = BayOf(s.style), floorH = FloorOf(s.style);
            float w = Snap(s.width, bay), d = Snap(s.depth, bay);
            float H = Mathf.Max(floorH * 2, Mathf.Round(s.height / floorH) * floorH);
            var mb = new MeshBuilder(3);
            var podium = new MeshBuilder(3);
            float y = 0f;
            // foundation plinth so buildings sit into whatever ground surrounds them
            float fd = s.foundation > 0 ? s.foundation : 1.2f;
            mb.BoxC(new Vector3(0, -fd / 2f, 0), new Vector3(w + 0.2f, fd, d + 0.2f), 2, MeshBuilder.Faces.Sides);

            // podium (different style at street level: shops / lobby)
            if (s.podiumStyle.HasValue && s.podiumFloors > 0)
            {
                float pBay = BayOf(s.podiumStyle.Value), pFloor = FloorOf(s.podiumStyle.Value);
                float ph = pFloor * s.podiumFloors;
                float pw = Snap(w + 1.2f, pBay), pd = Snap(d + 1.2f, pBay);
                podium.Box(new Vector3(-pw / 2, 0, -pd / 2), new Vector3(pw, ph, pd), 0, 1, 0f, MeshBuilder.Faces.Sides | MeshBuilder.Faces.Top);
                // slab edge + parapet line between podium and tower
                mb.BoxC(new Vector3(0, ph + 0.15f, 0), new Vector3(pw + 0.3f, 0.3f, pd + 0.3f), 2);
                y = ph;
            }

            // tower tiers
            int tiers = s.setbacks ? (H > 60 ? 3 : 2) : 1;
            float remaining = H - y;
            float tw = w, td = d, baseY = 0f;
            for (int t = 0; t < tiers; t++)
            {
                float th = t == tiers - 1 ? remaining : Mathf.Round(remaining * R(0.45f, 0.6f) / floorH) * floorH;
                th = Mathf.Max(floorH * 2, th);
                float lift = 0f, skew = 0f;
                if (s.corrupted && t == tiers - 1 && tiers > 1) { lift = R(0.9f, 1.6f); skew = R(0.4f, 0.8f); }
                var min = new Vector3(-tw / 2 + skew, y + lift, -td / 2);
                // facade UVs: v measured from the building base so floors line up between tiers
                mb.Box(min, new Vector3(tw, th, td), 0, 1, baseY + lift, MeshBuilder.Faces.Sides | MeshBuilder.Faces.Top | (lift > 0 ? MeshBuilder.Faces.Bottom : MeshBuilder.Faces.None));
                // parapet / cornice
                float top = y + lift + th;
                if (s.style == CityMaterials.FacadeStyle.Brick || s.style == CityMaterials.FacadeStyle.Stone || s.style == CityMaterials.FacadeStyle.Plaster)
                {
                    mb.BoxC(new Vector3(skew, top - 0.25f, 0), new Vector3(tw + 0.5f, 0.5f, td + 0.5f), 2);   // cornice
                    mb.BoxC(new Vector3(skew, top + 0.45f, -td / 2 + 0.12f), new Vector3(tw, 0.9f, 0.24f), 2);
                    mb.BoxC(new Vector3(skew, top + 0.45f, td / 2 - 0.12f), new Vector3(tw, 0.9f, 0.24f), 2);
                    mb.BoxC(new Vector3(skew - tw / 2 + 0.12f, top + 0.45f, 0), new Vector3(0.24f, 0.9f, td), 2);
                    mb.BoxC(new Vector3(skew + tw / 2 - 0.12f, top + 0.45f, 0), new Vector3(0.24f, 0.9f, td), 2);
                }
                else
                {
                    mb.BoxC(new Vector3(skew, top + 0.6f, -td / 2 + 0.1f), new Vector3(tw, 1.2f, 0.2f), 2);
                    mb.BoxC(new Vector3(skew, top + 0.6f, td / 2 - 0.1f), new Vector3(tw, 1.2f, 0.2f), 2);
                    mb.BoxC(new Vector3(skew - tw / 2 + 0.1f, top + 0.6f, 0), new Vector3(0.2f, 1.2f, td), 2);
                    mb.BoxC(new Vector3(skew + tw / 2 - 0.1f, top + 0.6f, 0), new Vector3(0.2f, 1.2f, td), 2);
                }
                y += th;
                remaining -= th;
                if (remaining < floorH * 2) { tiers = t + 1; }
                float shrink = Snap(tw * R(0.65f, 0.8f), bay);
                tw = Mathf.Max(bay * 2, shrink);
                td = Mathf.Max(bay * 2, Snap(td * R(0.7f, 0.85f), bay));
                if (remaining <= 0) break;
            }

            // rooftop machinery: HVAC boxes, a water tank on older buildings, an antenna on tall towers
            float roofY = y + (s.corrupted ? 1.2f : 0f);
            int units = rnd.Next(1, 4);
            for (int i = 0; i < units; i++)
            {
                var size = new Vector3(R(1.4f, 3.2f), R(1.0f, 1.8f), R(1.2f, 2.6f));
                var c = new Vector3(R(-tw * 0.3f, tw * 0.3f), roofY + size.y / 2, R(-td * 0.25f, td * 0.25f));
                mb.BoxC(c, size, 2);
            }
            if ((s.style == CityMaterials.FacadeStyle.Brick || s.style == CityMaterials.FacadeStyle.Plaster) && rnd.NextDouble() < 0.45)
            {
                var tp = new Vector3(R(-tw * 0.2f, tw * 0.2f), roofY, R(-td * 0.2f, td * 0.2f));
                for (int k = 0; k < 4; k++) mb.Tube(tp + new Vector3((k % 2 - 0.5f) * 1.6f, 0, (k / 2 - 0.5f) * 1.6f), tp + new Vector3((k % 2 - 0.5f) * 1.6f, 2.2f, (k / 2 - 0.5f) * 1.6f), 0.08f, 0.08f, 6, 2);
                mb.Cylinder(tp + Vector3.up * 2.2f, 1.3f, 1.3f, 2.6f, 14, 2);
                mb.Cylinder(tp + Vector3.up * 4.8f, 1.35f, 0.1f, 0.9f, 14, 2);
            }
            if (H > 70 && rnd.NextDouble() < 0.6)
            {
                var ap = new Vector3(0, roofY, 0);
                mb.Tube(ap, ap + Vector3.up * R(8f, 18f), 0.25f, 0.08f, 8, 2);
            }

            if (CollectWalls != null && w > 6f)
            {
                var wp = parent.TransformPoint(baseCenter);
                CollectWalls.Add(new Wall { x0 = wp.x - w / 2f + 0.5f, x1 = wp.x + w / 2f - 0.5f, y0 = wp.y + 4f, y1 = wp.y + Mathf.Min(H, 60f) - 1f, z = wp.z - d / 2f });
            }
            var go = mb.ToObject(name, parent, new[]
            {
                CityMaterials.Facade(s.style, s.variant, s.glitch),
                CityMaterials.PBR("Concrete034", new Color(0.62f, 0.62f, 0.6f), 3f, 1f, 0f, s.glitch),
                Trim(s.style, s.glitch),
            });
            go.transform.localPosition = baseCenter;
            if (podium.VertexCount > 0)
            {
                var p = podium.ToObject(name + "_Podium", go.transform, new[]
                {
                    CityMaterials.Facade(s.podiumStyle.Value, s.variant + 3, s.glitch),
                    CityMaterials.PBR("Concrete034", new Color(0.6f, 0.6f, 0.58f), 3f, 1f, 0f, s.glitch),
                    Trim(s.podiumStyle.Value, s.glitch),
                });
                p.transform.localPosition = Vector3.zero;
            }
            return go;
        }

        public static Material Trim(CityMaterials.FacadeStyle s, float glitch)
        {
            switch (s)
            {
                case CityMaterials.FacadeStyle.Brick:
                case CityMaterials.FacadeStyle.Plaster:
                case CityMaterials.FacadeStyle.Stone:
                    return CityMaterials.PBR("Concrete046", new Color(0.82f, 0.8f, 0.76f), 2.5f, 1f, 0f, glitch);
                case CityMaterials.FacadeStyle.Industrial:
                    return CityMaterials.PBR("PaintedMetal004", new Color(0.55f, 0.57f, 0.6f), 2f, 1f, 0.3f, glitch);
                default:
                    return CityMaterials.PBR("Metal009", new Color(0.5f, 0.52f, 0.55f), 2f, 0.7f, 0.6f, glitch);
            }
        }

        /// <summary>Picks a believable building for a city block slot.</summary>
        public static Spec Random(System.Random rnd, float width, float depth, float minH, float maxH, bool downtown, float glitch)
        {
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            var s = new Spec { width = width, depth = depth, height = R(minH, maxH), variant = rnd.Next(4), glitch = glitch };
            double p = rnd.NextDouble();
            if (s.height > 45 || (downtown && p < 0.45))
            {
                s.style = p < 0.6 ? CityMaterials.FacadeStyle.GlassTower : CityMaterials.FacadeStyle.Office;
                s.podiumStyle = rnd.NextDouble() < 0.6 ? CityMaterials.FacadeStyle.Stone : (CityMaterials.FacadeStyle?)null;
                s.podiumFloors = s.podiumStyle.HasValue ? rnd.Next(1, 3) : 0;
                s.setbacks = s.height > 50 && rnd.NextDouble() < 0.6;
            }
            else
            {
                s.style = p < 0.4 ? CityMaterials.FacadeStyle.Brick : p < 0.7 ? CityMaterials.FacadeStyle.Plaster : p < 0.9 ? CityMaterials.FacadeStyle.Office : CityMaterials.FacadeStyle.Stone;
                s.height = Mathf.Min(s.height, 32f);
            }
            return s;
        }
    }
}
