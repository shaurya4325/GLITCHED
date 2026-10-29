using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>Marks a decorative light so the graphics tiers can switch it off on low-end devices.</summary>
    public class WorldLight : MonoBehaviour
    {
        public static readonly List<Light> All = new List<Light>();
        Light l;
        void OnEnable() { l = GetComponent<Light>(); if (l != null) All.Add(l); Apply(); }
        void OnDisable() { if (l != null) All.Remove(l); }
        public static bool Enabled = true;
        public void Apply() { if (l != null) l.enabled = Enabled; }
        public static void SetAll(bool on) { Enabled = on; foreach (var x in All) if (x != null) x.enabled = on; }
    }
}
