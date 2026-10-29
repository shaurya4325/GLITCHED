using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// A mini experiment: slider values in, a live picture and readouts out (Section 8, type 6).
    /// Shared by the Simulation challenge and the interactive teaching labs.
    /// </summary>
    public abstract class SimBase
    {
        protected readonly Dictionary<string, float> V = new Dictionary<string, float>();
        protected readonly Dictionary<string, double> F = new Dictionary<string, double>();
        protected static readonly List<Vector2> Pts = new List<Vector2>();
        public virtual float PlayDuration => 2f;

        public void Set(string id, float value) => V[id] = value;
        public float Get(string id, float fallback = 0f) => V.TryGetValue(id, out var v) ? v : (F.TryGetValue(id, out var f) ? (float)f : fallback);
        public void SetFixed(Dictionary<string, double> fixedValues)
        {
            if (fixedValues == null) return;
            foreach (var kv in fixedValues) F[kv.Key] = kv.Value;
        }

        /// <summary>A computed result used by "goal" challenges (range, a, I, co...).</summary>
        public abstract float Metric(string name);
        /// <summary>Short live readout text.</summary>
        public abstract string Readout();
        /// <summary>Draw the experiment. playT: -1 while idle, 0..1 while the RUN animation plays.</summary>
        public abstract void Draw(UIGraph g, float playT, float time);

        public static SimBase Create(string key)
        {
            switch (key)
            {
                case "projectile": return new ProjectileSim();
                case "cart": return new CartSim();
                case "ohm": return new OhmSim();
                case "pendulum": return new PendulumSim();
                case "rate": return new RateSim();
                case "enzyme": return new EnzymeSim();
                case "heart": return new HeartSim();
                case "photo": return new PhotoSim();
                default: return new ProjectileSim();
            }
        }

        protected static Color Cyan => UIFactory.Theme.cyan;
        protected static Color Magenta => UIFactory.Theme.magenta;
        protected static Color Yellow => UIFactory.Theme.yellow;
        protected static Color Dim => new Color(1, 1, 1, 0.15f);

        /// <summary>Draws simple axes with a label area.</summary>
        protected static void Axes(UIGraph g, Vector2 o, float w, float h)
        {
            g.Line(o, o + new Vector2(w, 0), Dim, 2);
            g.Line(o, o + new Vector2(0, h), Dim, 2);
        }
    }

    public class ProjectileSim : SimBase
    {
        public override float PlayDuration => 2.2f;
        float G => Get("g", 10f);
        float Rad => Get("angle", 45f) * Mathf.Deg2Rad;
        public float Range => Get("v", 15f) * Get("v", 15f) * Mathf.Sin(2 * Rad) / Mathf.Max(0.1f, G);
        public float MaxHeight => Mathf.Pow(Get("v", 15f) * Mathf.Sin(Rad), 2) / (2 * Mathf.Max(0.1f, G));
        public float FlightTime => 2 * Get("v", 15f) * Mathf.Sin(Rad) / Mathf.Max(0.1f, G);

        public override float Metric(string name) => name == "height" ? MaxHeight : name == "time" ? FlightTime : Range;
        public override string Readout() => $"range <b>{Range:0.0} m</b>   max height {MaxHeight:0.0} m   flight {FlightTime:0.00} s";

        public override void Draw(UIGraph g, float playT, float time)
        {
            var s = g.Size;
            float pad = 40, w = s.x - pad * 2, h = s.y - pad * 2;
            float worldW = 80f;
            float sx = w / worldW;
            float sy = sx;
            Vector2 o = new Vector2(pad, pad);
            g.Line(o, o + new Vector2(w, 0), Dim, 3);
            for (int m = 10; m <= 80; m += 10) g.Line(o + new Vector2(m * sx, -6), o + new Vector2(m * sx, 6), Dim, 2);
            // target platform (goal metric range)
            float tmin = Get("_goalMin", -1), tmax = Get("_goalMax", -1);
            if (tmin > 0) g.Box(o + new Vector2(tmin * sx, -12), o + new Vector2(tmax * sx, 0), Magenta);
            // predicted arc (dotted) and the flying probe
            float v = Get("v", 15f), a = Rad, gg = G;
            float T = FlightTime;
            Pts.Clear();
            int steps = 40;
            float shown = playT < 0 ? 1f : playT;
            for (int i = 0; i <= steps; i++)
            {
                float t = T * i / steps;
                float x = v * Mathf.Cos(a) * t, y = v * Mathf.Sin(a) * t - 0.5f * gg * t * t;
                Pts.Add(o + new Vector2(Mathf.Min(x, worldW) * sx, Mathf.Max(0, y) * sy));
            }
            if (playT < 0)
                for (int i = 0; i < Pts.Count - 1; i += 2) g.Line(Pts[i], Pts[i + 1], new Color(Cyan.r, Cyan.g, Cyan.b, 0.45f), 3);
            else
            {
                int upto = Mathf.Clamp(Mathf.RoundToInt(shown * steps), 1, steps);
                g.Polyline(Pts.GetRange(0, upto + 1), Cyan, 4);
                g.Circle(Pts[upto], 12, Yellow);
            }
            // launcher
            g.Line(o, o + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 50, Yellow, 8);
            g.Circle(o, 12, Magenta);
        }
    }

    public class CartSim : SimBase
    {
        public float A => Get("F", 50) / Mathf.Max(0.1f, Get("m", 20));
        public override float Metric(string name) => A;
        public override string Readout() => $"a = F / m = {Get("F", 50):0} / {Get("m", 20):0} = <b>{A:0.00} m/s²</b>";
        public override void Draw(UIGraph g, float playT, float time)
        {
            var s = g.Size;
            float ground = 50;
            g.Line(new Vector2(20, ground), new Vector2(s.x - 20, ground), Dim, 3);
            float t = playT < 0 ? 0 : playT * 2.5f;
            float x = 60 + 0.5f * A * t * t * 18f;
            x = Mathf.Min(x, s.x - 140);
            float m = Get("m", 20);
            float bw = 60 + m * 2.4f, bh = 40 + m * 1.2f;
            g.Box(new Vector2(x, ground + 14), new Vector2(x + bw, ground + 14 + bh), Cyan);
            g.Circle(new Vector2(x + 18, ground + 10), 12, Dim);
            g.Circle(new Vector2(x + bw - 18, ground + 10), 12, Dim);
            float f = Get("F", 50);
            g.Arrow(new Vector2(x - 20 - f * 0.6f, ground + 14 + bh / 2), new Vector2(x - 6, ground + 14 + bh / 2), Magenta, 6, 16);
            // speed bar
            float vel = A * t;
            g.Box(new Vector2(s.x - 60, 20), new Vector2(s.x - 30, 20 + Mathf.Min(vel * 12, s.y - 40)), Yellow);
        }
    }

    public class OhmSim : SimBase
    {
        public float I => Get("V", 6) / Mathf.Max(0.1f, Get("R", 6));
        public override float Metric(string name) => I;
        public override string Readout() => $"I = V / R = {Get("V", 6):0.0} / {Get("R", 6):0} = <b>{I:0.00} A</b>";
        public override void Draw(UIGraph g, float playT, float time)
        {
            var s = g.Size;
            float x0 = 80, x1 = s.x - 80, y0 = 50, y1 = s.y - 50;
            var wire = new Color(0.8f, 0.85f, 1f, 0.8f);
            g.Line(new Vector2(x0, y0), new Vector2(x1, y0), wire, 4);
            g.Line(new Vector2(x0, y0), new Vector2(x0, y1), wire, 4);
            g.Line(new Vector2(x1, y0), new Vector2(x1, y1), wire, 4);
            g.Line(new Vector2(x0, y1), new Vector2(s.x / 2 - 70, y1), wire, 4);
            g.Line(new Vector2(s.x / 2 + 70, y1), new Vector2(x1, y1), wire, 4);
            // resistor zig-zag
            Pts.Clear();
            for (int i = 0; i <= 8; i++) Pts.Add(new Vector2(s.x / 2 - 70 + i * 17.5f, y1 + (i % 2 == 0 ? 0 : (i % 4 == 1 ? 16 : -16))));
            g.Polyline(Pts, Yellow, 4);
            // battery
            g.Line(new Vector2(x0 - 22, s.y / 2 - 20), new Vector2(x0 + 22, s.y / 2 - 20), Magenta, 6);
            g.Line(new Vector2(x0 - 12, s.y / 2 + 4), new Vector2(x0 + 12, s.y / 2 + 4), Magenta, 6);
            // bulb brightness ~ power
            float p = Mathf.Clamp01(I / 2f);
            g.Circle(new Vector2(s.x / 2, y0), 26, new Color(1f, 0.9f, 0.3f, 0.25f + p * 0.75f));
            g.Circle(new Vector2(s.x / 2, y0), 26 + p * 30, new Color(1f, 0.9f, 0.3f, p * 0.25f), true);
            // moving charges (speed ~ current)
            int n = 16;
            float len = 2 * (x1 - x0) + 2 * (y1 - y0);
            for (int i = 0; i < n; i++)
            {
                float d = Mathf.Repeat(i / (float)n * len + time * I * 120f, len);
                g.Circle(PathPoint(d, x0, x1, y0, y1), 5, Cyan);
            }
        }

        static Vector2 PathPoint(float d, float x0, float x1, float y0, float y1)
        {
            float w = x1 - x0, h = y1 - y0;
            if (d < w) return new Vector2(x0 + d, y1);
            d -= w; if (d < h) return new Vector2(x1, y1 - d);
            d -= h; if (d < w) return new Vector2(x1 - d, y0);
            d -= w; return new Vector2(x0, y0 + d);
        }
    }

    public class PendulumSim : SimBase
    {
        public float Period => 2 * Mathf.PI * Mathf.Sqrt(Get("L", 1) / Mathf.Max(0.1f, Get("g", 9.8f)));
        public override float Metric(string name) => Period;
        public override string Readout() => $"T = 2π√(L/g) = <b>{Period:0.00} s</b>";
        public override void Draw(UIGraph g, float playT, float time)
        {
            var s = g.Size;
            Vector2 pivot = new Vector2(s.x / 2, s.y - 24);
            float L = Get("L", 1);
            float len = Mathf.Lerp(40, s.y - 60, L / 4f);
            float ang = 0.6f * Mathf.Cos(2 * Mathf.PI * time / Period);
            Vector2 bob = pivot + new Vector2(Mathf.Sin(ang), -Mathf.Cos(ang)) * len;
            g.Line(pivot - new Vector2(60, 0), pivot + new Vector2(60, 0), Dim, 4);
            g.Line(pivot, bob, new Color(0.9f, 0.9f, 1f), 3);
            g.Circle(bob, 18, Cyan);
            // energy bars: KE vs PE
            float ke = Mathf.Cos(ang) - Mathf.Cos(0.6f);
            float k01 = Mathf.Clamp01(ke / (1 - Mathf.Cos(0.6f)));
            g.Box(new Vector2(30, 20), new Vector2(60, 20 + k01 * 140), Yellow);
            g.Box(new Vector2(70, 20), new Vector2(100, 20 + (1 - k01) * 140), Magenta);
        }
    }

    public class RateSim : SimBase
    {
        public float Rate => Get("c", 0.5f) * Mathf.Pow(2f, (Get("T", 25) - 25f) / 10f);
        public override float Metric(string name) => Rate;
        public override string Readout() => $"collisions/s: <b>{Rate * 100:0}</b>   (rate roughly doubles every +10 °C)";
        public override void Draw(UIGraph g, float playT, float time)
        {
            var s = g.Size;
            g.Box(new Vector2(20, 20), new Vector2(s.x - 20, s.y - 20), new Color(0.2f, 0.6f, 1f, 0.08f));
            int n = Mathf.RoundToInt(6 + Get("c", 0.5f) * 22);
            float speed = 40 + Get("T", 25) * 3f;
            for (int i = 0; i < n; i++)
            {
                float a = i * 2.39996f;
                float x = 30 + Mathf.PingPong(i * 97 + time * speed * (0.7f + (i % 3) * 0.2f), s.x - 60);
                float y = 30 + Mathf.PingPong(i * 53 + time * speed * (0.6f + (i % 4) * 0.15f), s.y - 60);
                g.Circle(new Vector2(x, y), 8, i % 2 == 0 ? Cyan : Magenta);
                if ((i * 7 + Mathf.FloorToInt(time * Rate * 6)) % 23 == 0) g.Circle(new Vector2(x, y), 16, new Color(1, 1, 0.4f, 0.6f), false, 3);
            }
        }
    }

    public class EnzymeSim : SimBase
    {
        public static float Activity(float t) => t > 60 ? 0 : Mathf.Exp(-Mathf.Pow((t - 37f) / 11f, 2));
        public override float Metric(string name) => Activity(Get("T", 20));
        public override string Readout()
        {
            float t = Get("T", 20);
            string note = t > 55 ? "<color=#ff5a6a>denatured!</color>" : t < 15 ? "slow (cold)" : Mathf.Abs(t - 37) < 4 ? "<color=#7cffa0>optimum</color>" : "";
            return $"{t:0} °C   activity <b>{Activity(t) * 100:0}%</b>   {note}";
        }
        public override void Draw(UIGraph g, float playT, float time)
        {
            var s = g.Size;
            Vector2 o = new Vector2(40, 30);
            float w = s.x - 80, h = s.y - 60;
            Axes(g, o, w, h);
            Pts.Clear();
            float cur = Get("T", 20);
            for (int i = 0; i <= 80; i++) { float t = i; if (t > cur) break; Pts.Add(o + new Vector2(t / 80f * w, Activity(t) * h)); }
            if (Pts.Count >= 2) g.Polyline(Pts, Cyan, 4);
            g.Circle(o + new Vector2(cur / 80f * w, Activity(cur) * h), 10, Yellow);
            g.Line(o + new Vector2(37f / 80f * w, 0), o + new Vector2(37f / 80f * w, h), new Color(1, 1, 1, 0.08f), 2);
        }
    }

    public class HeartSim : SimBase
    {
        public float CO => Get("hr", 60) * Get("sv", 60) / 1000f;
        public override float Metric(string name) => CO;
        public override string Readout() => $"cardiac output = {Get("hr", 60):0} × {Get("sv", 60):0} mL = <b>{CO:0.00} L/min</b>";
        public override void Draw(UIGraph g, float playT, float time)
        {
            var s = g.Size;
            float beat = Mathf.Repeat(time * Get("hr", 60) / 60f, 1f);
            float pulse = beat < 0.15f ? Mathf.Sin(beat / 0.15f * Mathf.PI) : 0f;
            Vector2 c = new Vector2(s.x * 0.3f, s.y / 2);
            float r = 50 + Get("sv", 60) * 0.25f + pulse * 14;
            var red = new Color(1f, 0.25f, 0.35f);
            g.Circle(c + new Vector2(-r * 0.45f, r * 0.2f), r * 0.6f, red);
            g.Circle(c + new Vector2(r * 0.45f, r * 0.2f), r * 0.6f, red);
            g.Circle(c + new Vector2(0, -r * 0.3f), r * 0.55f, red);
            // ECG trace
            Pts.Clear();
            float x0 = s.x * 0.55f, w = s.x * 0.42f;
            for (int i = 0; i <= 60; i++)
            {
                float k = i / 60f;
                float ph = Mathf.Repeat(k * 3 + time * Get("hr", 60) / 60f, 1f);
                float y = ph < 0.08f ? Mathf.Sin(ph / 0.08f * Mathf.PI) * 60 : ph < 0.12f ? -Mathf.Sin((ph - 0.08f) / 0.04f * Mathf.PI) * 20 : 0;
                Pts.Add(new Vector2(x0 + k * w, s.y / 2 + y));
            }
            g.Polyline(Pts, new Color(0.4f, 1f, 0.5f), 3);
        }
    }

    public class PhotoSim : SimBase
    {
        public float Rate => Mathf.Min(Get("light", 10) / 100f, Get("co2", 0.04f) / 0.06f) * 100f;
        public override float Metric(string name) => Rate;
        public override string Readout() => $"O₂ bubbles/min: <b>{Rate:0}</b>   " + (Get("light", 10) / 100f > Get("co2", 0.04f) / 0.06f ? "<color=#ffd640>limited by CO₂</color>" : "limited by light");
        public override void Draw(UIGraph g, float playT, float time)
        {
            var s = g.Size;
            // lamp
            float L = Get("light", 10) / 100f;
            g.Circle(new Vector2(60, s.y - 60), 26, new Color(1f, 0.95f, 0.4f, 0.3f + L * 0.7f));
            for (int i = 0; i < 6; i++) g.Line(new Vector2(90, s.y - 60 - i * 8), new Vector2(160 + L * 60, s.y - 60 - i * 20), new Color(1f, 0.95f, 0.4f, L * 0.5f), 3);
            // beaker with pondweed
            g.Box(new Vector2(200, 20), new Vector2(380, s.y - 40), new Color(0.3f, 0.6f, 1f, 0.15f));
            g.Line(new Vector2(290, 30), new Vector2(290, s.y - 90), new Color(0.3f, 0.9f, 0.4f), 6);
            int n = Mathf.RoundToInt(Rate / 8f);
            for (int i = 0; i < n; i++)
                g.Circle(new Vector2(290 + Mathf.Sin(i * 3.1f + time * 2) * 12, 40 + Mathf.Repeat(i * 0.17f + time * 0.6f, 1f) * (s.y - 100)), 5, new Color(0.9f, 1f, 1f, 0.8f));
            // graph rate vs light (plateau)
            Vector2 o = new Vector2(420, 30);
            float w = s.x - 450, h = s.y - 60;
            Axes(g, o, w, h);
            Pts.Clear();
            float cap = Get("co2", 0.04f) / 0.06f;
            for (int i = 0; i <= 50; i++) { float l = i / 50f; Pts.Add(o + new Vector2(l * w, Mathf.Min(l, cap) * h)); }
            g.Polyline(Pts, Cyan, 3);
            g.Circle(o + new Vector2(L * w, Mathf.Min(L, cap) * h), 9, Yellow);
        }
    }
}
