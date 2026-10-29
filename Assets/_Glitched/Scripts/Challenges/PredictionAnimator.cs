using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>Small vector "play-outs" for Prediction challenges (t goes 0 -> 1 after the answer).</summary>
    public static class PredictionAnimator
    {
        static readonly List<Vector2> pts = new List<Vector2>();

        public static void Draw(UIGraph g, string anim, float t, bool playing, float time)
        {
            g.Clear();
            var th = UIFactory.Theme;
            Vector2 s = g.Size;
            float w = s.x, h = s.y;
            Color dim = new Color(1, 1, 1, 0.15f);
            if (!playing)
            {
                // idle: a pulsing question mark-ish pattern
                float p = 0.5f + 0.5f * Mathf.Sin(time * 3f);
                g.Circle(new Vector2(w / 2, h / 2), 40 + p * 10, new Color(th.cyan.r, th.cyan.g, th.cyan.b, 0.25f), false, 4);
                g.Circle(new Vector2(w / 2, h / 2), 12, th.cyan);
                return;
            }
            switch (anim)
            {
                case "two_balls":
                    {
                        float ground = 30, top = h - 40;
                        float y = Mathf.Lerp(top, ground, t * t);
                        g.Line(new Vector2(20, ground - 12), new Vector2(w - 20, ground - 12), dim, 3);
                        g.Box(new Vector2(30, top - 10), new Vector2(110, top + 10), dim);
                        g.Circle(new Vector2(100, y), 16, th.magenta);
                        pts.Clear();
                        for (float k = 0; k <= t; k += 0.05f) pts.Add(new Vector2(100 + k * (w - 200), Mathf.Lerp(top, ground, k * k)));
                        g.Polyline(pts, new Color(th.cyan.r, th.cyan.g, th.cyan.b, 0.4f), 3);
                        g.Circle(new Vector2(100 + t * (w - 200), y), 16, th.cyan);
                        break;
                    }
                case "rocket":
                    {
                        float y = 40 + t * t * (h - 80);
                        g.Line(new Vector2(20, 30), new Vector2(w - 20, 30), dim, 3);
                        g.Box(new Vector2(w / 2 - 18, y), new Vector2(w / 2 + 18, y + 70), th.text);
                        g.Circle(new Vector2(w / 2, y + 74), 18, th.magenta);
                        for (int i = 0; i < 8; i++) g.Circle(new Vector2(w / 2 + Random.Range(-14f, 14f), y - 10 - i * 12 * (0.5f + t)), 10 - i, new Color(1, 0.6f, 0.2f, 0.8f - i * 0.1f));
                        g.Arrow(new Vector2(w / 2 + 60, y + 20), new Vector2(w / 2 + 60, y + 80), th.green, 4);
                        g.Arrow(new Vector2(w / 2 - 60, y + 20), new Vector2(w / 2 - 60, y - 40), th.yellow, 4);
                        break;
                    }
                case "pendulum":
                    {
                        Vector2 pivot = new Vector2(w / 2, h - 20);
                        float ang = Mathf.Cos(t * Mathf.PI * 3f) * 0.9f;
                        Vector2 bob = pivot + new Vector2(Mathf.Sin(ang), -Mathf.Cos(ang)) * (h - 80);
                        g.Line(pivot, bob, th.text, 3);
                        g.Circle(bob, 20, th.cyan);
                        float ke = Mathf.Cos(ang * 1.4f);
                        g.Box(new Vector2(20, 20), new Vector2(50, 20 + ke * 120), th.green);
                        g.Box(new Vector2(60, 20), new Vector2(90, 20 + (1 - ke) * 120), th.magenta);
                        break;
                    }
                case "circuit":
                    {
                        float x0 = 60, x1 = w - 60, y0 = 50, y1 = h - 50;
                        bool broken = t > 0.3f;
                        g.Line(new Vector2(x0, y0), new Vector2(x1, y0), th.text, 4);
                        g.Line(new Vector2(x0, y0), new Vector2(x0, y1), th.text, 4);
                        g.Line(new Vector2(x1, y0), new Vector2(x1, y1), th.text, 4);
                        g.Line(new Vector2(x0, y1), new Vector2(w / 2 - 70, y1), th.text, 4);
                        g.Line(new Vector2(w / 2 + 70, y1), new Vector2(x1, y1), th.text, 4);
                        Color lit = broken ? new Color(0.2f, 0.2f, 0.25f) : th.yellow;
                        g.Circle(new Vector2(w / 2 - 40, y1), 22, lit);
                        g.Circle(new Vector2(w / 2 + 40, y1), 22, broken ? new Color(0.4f, 0.1f, 0.1f) : th.yellow);
                        if (broken) g.Line(new Vector2(w / 2 + 25, y1 - 15), new Vector2(w / 2 + 55, y1 + 15), th.red, 4);
                        break;
                    }
                case "fizz":
                case "neutralise":
                    {
                        g.Box(new Vector2(w / 2 - 90, 20), new Vector2(w / 2 + 90, h - 60), new Color(0.3f, 0.6f, 1f, 0.25f));
                        Random.InitState(7);
                        for (int i = 0; i < 30; i++)
                        {
                            float x = w / 2 + Random.Range(-80f, 80f);
                            float y = 20 + Mathf.Repeat(Random.value + t * (anim == "fizz" ? 2.5f : 0.8f), 1f) * (h - 90);
                            g.Circle(new Vector2(x, y), Random.Range(3f, 8f), anim == "fizz" ? th.yellow : th.green);
                        }
                        Random.InitState((int)(time * 1000));
                        if (anim == "fizz" && t > 0.5f) g.Circle(new Vector2(w / 2, h - 50), 30 * (t - 0.5f) * 2, new Color(1, 0.5f, 0.1f, 0.8f));
                        break;
                    }
                case "noble":
                    {
                        Vector2 c = new Vector2(w / 2, h / 2);
                        g.Circle(c, 26, th.magenta);
                        g.Circle(c, 70, dim, false, 3);
                        g.Circle(c, 120, dim, false, 3);
                        for (int i = 0; i < 2; i++) { float a = time * 2 + i * Mathf.PI; g.Circle(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 70, 8, th.cyan); }
                        for (int i = 0; i < 8; i++) { float a = -time * 1.4f + i * Mathf.PI / 4; g.Circle(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 120, 8, th.cyan); }
                        float ox = Mathf.Lerp(w - 40, w / 2 + 160, Mathf.Min(1, t * 2)) + (t > 0.5f ? (t - 0.5f) * 300 : 0);
                        g.Circle(new Vector2(ox, h / 2), 20, th.red);
                        break;
                    }
                case "osmosis":
                    {
                        float r = 50 + t * 70;
                        Vector2 c = new Vector2(w / 2, h / 2);
                        for (int i = 0; i < 14; i++) { float a = i / 14f * Mathf.PI * 2; float d = Mathf.Lerp(200, r, Mathf.Repeat(t * 2 + i * 0.07f, 1)); g.Circle(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d, 5, th.cyan); }
                        if (t < 0.9f) g.Circle(c, r, new Color(1f, 0.3f, 0.35f, 0.85f));
                        else for (int i = 0; i < 12; i++) { float a = i / 12f * Mathf.PI * 2; g.Circle(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r * 1.2f, 10, new Color(1f, 0.3f, 0.35f, 0.8f)); }
                        break;
                    }
                case "population":
                    {
                        pts.Clear();
                        for (int i = 0; i <= 40; i++) { float k = i / 40f; pts.Add(new Vector2(30 + k * (w - 60), 40 + (k < 0.3f ? 0.3f : 0.3f + (k - 0.3f) * 1.3f) * (h - 80) * Mathf.Min(1, t * 1.5f + 0.2f))); }
                        g.Polyline(pts, th.green, 4);
                        pts.Clear();
                        for (int i = 0; i <= 40; i++) { float k = i / 40f; pts.Add(new Vector2(30 + k * (w - 60), 40 + (k < 0.3f ? 0.5f : 0.02f) * (h - 80))); }
                        g.Polyline(pts, th.red, 3);
                        g.Line(new Vector2(30, 36), new Vector2(w - 30, 36), dim, 2);
                        break;
                    }
                case "induction":
                    {
                        float mx = Mathf.Lerp(30, w / 2 - 40, Mathf.Min(1, t * 1.8f));
                        g.Box(new Vector2(mx, h / 2 - 20), new Vector2(mx + 60, h / 2 + 20), th.red);
                        g.Box(new Vector2(mx + 60, h / 2 - 20), new Vector2(mx + 120, h / 2 + 20), th.cyan);
                        for (int i = 0; i < 6; i++) g.Circle(new Vector2(w / 2 + 40 + i * 22, h / 2), 30, th.yellow, false, 3);
                        float needle = Mathf.Sin(Mathf.Min(1, t * 1.8f) * Mathf.PI) * 50f;
                        Vector2 gp = new Vector2(w - 90, 60);
                        g.Circle(gp, 50, dim, false, 3);
                        g.Line(gp, gp + new Vector2(Mathf.Sin(needle * Mathf.Deg2Rad), Mathf.Cos(needle * Mathf.Deg2Rad)) * 46, th.magenta, 4);
                        break;
                    }
                default: // print_output: a little terminal typing the outcome
                    {
                        g.Box(new Vector2(20, 20), new Vector2(w - 20, h - 20), new Color(0.02f, 0.05f, 0.04f, 1f));
                        int lines = Mathf.FloorToInt(t * 6);
                        for (int i = 0; i < lines; i++) g.Box(new Vector2(40, h - 50 - i * 30), new Vector2(40 + Random.Range(80f, w - 100), h - 36 - i * 30), new Color(0.3f, 1f, 0.5f, 0.6f));
                        if (Mathf.Repeat(time * 2, 1) < 0.5f) g.Box(new Vector2(40, h - 50 - lines * 30), new Vector2(56, h - 36 - lines * 30), new Color(0.3f, 1f, 0.5f, 1f));
                        break;
                    }
            }
        }
    }
}
