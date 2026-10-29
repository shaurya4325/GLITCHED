using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// The game Shaurya is playing in the opening: a night street race. A small real city street is built far below
    /// the bedroom and a camera drives down it (driver's-eye view, weaving between cars), rendering into a texture
    /// that the gaming monitor displays.
    /// </summary>
    public class GameFeed : MonoBehaviour
    {
        public RenderTexture Texture { get; private set; }
        Camera cam;
        Transform rig;
        float x, sway;
        const float Length = 360f, Speed = 26f;
        static readonly Vector3 Origin = new Vector3(0, -600f, 0);

        public static GameFeed Create(Transform parent)
        {
            var go = new GameObject("GameFeed");
            go.transform.SetParent(parent, false);
            go.transform.position = Origin;
            var f = go.AddComponent<GameFeed>();
            f.Build();
            return f;
        }

        void Build()
        {
            var rnd = new System.Random(2026);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            var world = new GameObject("RaceStreet").transform;
            world.SetParent(transform, false);
            var street = new StreetGen(world, rnd, 0f);
            street.maxLights = 0;
            street.Road(-40f, Length + 60f, -7f, 7f, CityBackdrop.RoadY);
            street.Sidewalk(-40f, Length + 60f, 7f, 11f, false, CityBackdrop.RoadY);
            street.Sidewalk(-40f, Length + 60f, -11f, -7f, true, CityBackdrop.RoadY);
            for (float px = -30f; px < Length + 40f; px += 22f)
            {
                street.StreetLight(new Vector3(px, 0, 7.6f), -1f, false);
                street.StreetLight(new Vector3(px + 11f, 0, -7.6f), 1f, false);
            }
            street.Flush("RaceFurniture");
            foreach (float side in new[] { -1f, 1f })
                for (float px = -40f; px < Length + 60f;)
                {
                    var spec = BuildingGen.Random(rnd, R(14f, 26f), R(14f, 22f), 18f, 70f, true, 0f);
                    float w = BuildingGen.SnappedWidth(spec), d = BuildingGen.SnappedDepth(spec);
                    var b = BuildingGen.Build(world, new Vector3(px + w / 2f, 0f, side * (11.5f + d / 2f)), spec, rnd, "RaceBuilding");
                    if (side < 0) b.transform.localRotation = Quaternion.Euler(0, 180, 0);
                    px += w + 0.1f;
                }
            // traffic to overtake
            var tpls = new GameObject("CarTemplates").transform;
            tpls.SetParent(world, false);
            var traffic = new GameObject("Traffic").AddComponent<RoadTraffic>();
            traffic.transform.SetParent(world, false);
            traffic.transform.localPosition = new Vector3(0, CityBackdrop.RoadY, 0);
            for (int i = 0; i < 5; i++) { var c = CarGen.Build(tpls, Vector3.zero, 0f, rnd, null, 0f); c.SetActive(false); traffic.templates.Add(c); }
            traffic.x0 = -40f; traffic.x1 = Length + 60f; traffic.carsPerLane = 4;
            traffic.lanes.Add(new RoadTraffic.Lane { z = -3.5f, dir = 1, speed = 9f });
            traffic.lanes.Add(new RoadTraffic.Lane { z = 3.5f, dir = -1, speed = 12f });
            // the player's car, seen from behind
            rig = new GameObject("PlayerCar").transform;
            rig.SetParent(transform, false);
            var car = CarGen.Build(rig, new Vector3(0, CityBackdrop.RoadY, 0), 0f, rnd, CarGen.Kind.Sedan, 0f);
            car.name = "Car";
            Texture = new RenderTexture(1280, 540, 24) { name = "GameFeedRT", antiAliasing = 2 };
            cam = new GameObject("FeedCamera").AddComponent<Camera>();
            cam.transform.SetParent(rig, false);
            // third-person chase view, like most racing games
            cam.transform.localPosition = new Vector3(-6.2f, 2.3f + CityBackdrop.RoadY, 0f);
            cam.transform.localRotation = Quaternion.Euler(9f, 90f, 0f);
            cam.fieldOfView = 62f;
            cam.nearClipPlane = 0.3f; cam.farClipPlane = 600f;
            cam.targetTexture = Texture;
            cam.clearFlags = CameraClearFlags.Skybox;
            var lg = new GameObject("Headlights").AddComponent<Light>();
            lg.transform.SetParent(rig, false);
            lg.transform.localPosition = new Vector3(2.4f, 0.7f, 0);
            lg.transform.localRotation = Quaternion.Euler(8f, 90f, 0f);
            lg.type = LightType.Spot; lg.range = 45f; lg.spotAngle = 60f; lg.intensity = 6f; lg.color = new Color(1f, 0.95f, 0.85f);
            lg.shadows = LightShadows.None;
            x = 0f;
        }

        void Update()
        {
            if (rig == null) return;
            x += Speed * Time.deltaTime;
            if (x > Length) x -= Length;
            // weave between lanes like someone playing a racing game
            sway = Mathf.Sin(Time.time * 0.7f) * 2.6f + Mathf.Sin(Time.time * 1.9f) * 0.6f;
            rig.localPosition = new Vector3(x, 0f, -3.5f + sway * 0.6f);
            rig.localRotation = Quaternion.Euler(0, -Mathf.Cos(Time.time * 0.7f) * 6f, 0);
        }

        void OnDestroy() { if (Texture != null) Texture.Release(); }
    }
}
