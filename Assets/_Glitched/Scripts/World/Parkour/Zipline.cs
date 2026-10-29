using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>A cable from a high anchor (a) to a low one (b). Jump up to grab it and ride the trolley down.</summary>
    public class Zipline : MonoBehaviour
    {
        public static readonly List<Zipline> All = new List<Zipline>();
        public Vector3 a, b;
        public float catchRadius = 0.9f;
        public float maxSpeed = 12f;
        public Transform trolley;
        float home = 0.4f, at = 0.4f;
        bool riding;

        public float Length => Vector3.Distance(a, b);
        public Vector3 Direction => (b - a).normalized;
        public Vector3 Point(float s) => a + Direction * Mathf.Clamp(s, 0f, Length);

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        /// <summary>Distance along the cable of the point nearest p, and how far p is from the cable.</summary>
        public float Project(Vector3 p, out float dist)
        {
            float s = Mathf.Clamp(Vector3.Dot(p - a, Direction), 0f, Length);
            dist = Vector2.Distance(p, Point(s));
            return s;
        }

        public void Catch() { riding = true; Services.Audio.Play("dash", 0.5f); }
        public void Ride(float s) { at = s; }
        public void Release() { riding = false; }

        void Update()
        {
            if (!riding) at = Mathf.MoveTowards(at, home, Time.deltaTime * 4f);   // the trolley rolls back up for the next ride
            if (trolley != null) trolley.position = Point(at);
        }
    }
}
