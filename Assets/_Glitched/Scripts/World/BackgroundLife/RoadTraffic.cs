using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Cars driving along straight road lanes (x axis) in the realistic city. Cars are cloned from inactive template
    /// children, wrap around at the ends, keep a gap to the car ahead, and the count follows the graphics tier.
    /// Corruption is subtle: now and then a car hesitates for a moment.
    /// </summary>
    public class RoadTraffic : MonoBehaviour
    {
        [System.Serializable]
        public class Lane { public float z; public int dir = 1; public float speed = 11f; }

        public List<Lane> lanes = new List<Lane>();
        public List<GameObject> templates = new List<GameObject>();
        public float x0 = -150, x1 = 450;
        public int carsPerLane = 5;

        class Car { public Transform t; public Lane lane; public float x, speed, hesitate; }
        readonly List<Car> cars = new List<Car>();

        void Start()
        {
            Rebuild();
            GameEvents.TierChanged += OnTier;
        }

        void OnDestroy() => GameEvents.TierChanged -= OnTier;
        void OnTier(GraphicsTier t) => Rebuild();

        void Rebuild()
        {
            foreach (var c in cars) if (c.t != null) Destroy(c.t.gameObject);
            cars.Clear();
            if (templates.Count == 0) return;
            float density = Services.Graphics != null ? Services.Graphics.DensityMultiplier : 1f;
            int per = Mathf.Max(1, Mathf.RoundToInt(carsPerLane * density));
            float len = x1 - x0;
            var rnd = new System.Random(GetInstanceID());
            foreach (var lane in lanes)
                for (int i = 0; i < per; i++)
                {
                    var tpl = templates[rnd.Next(templates.Count)];
                    if (tpl == null) continue;
                    var go = Instantiate(tpl, transform);
                    go.SetActive(true);
                    var car = new Car
                    {
                        t = go.transform, lane = lane,
                        x = x0 + (i + (float)rnd.NextDouble() * 0.6f) / per * len,
                        speed = lane.speed * (0.85f + (float)rnd.NextDouble() * 0.3f),
                    };
                    go.transform.localRotation = Quaternion.Euler(0, lane.dir > 0 ? 0 : 180, 0);
                    cars.Add(car);
                }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float c = Services.Corruption != null ? Services.Corruption.Value : 0f;
            float len = x1 - x0;
            foreach (var car in cars)
            {
                if (car.t == null) continue;
                float v = car.speed;
                if (car.hesitate > 0) { car.hesitate -= dt; v *= 0.15f; }
                else if (c > 0.3f && Random.value < 0.0008f * c) car.hesitate = Random.Range(0.4f, 1.2f);
                car.x += v * car.lane.dir * dt;
                if (car.x > x1) car.x -= len;
                if (car.x < x0) car.x += len;
                car.t.localPosition = new Vector3(car.x, 0f, car.lane.z);
            }
        }
    }
}
