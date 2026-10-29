using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>Procedurally generated UI sprites (rounded panels, outlines, circles, glows), cached.</summary>
    public static class UISprites
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Rounded(int radius = 18, int border = 0)
        {
            string key = $"r{radius}_{border}";
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            int size = radius * 2 + 8;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = key };
            var px = new Color32[size * size];
            float c = size / 2f;
            float inner = size / 2f - 4;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // signed distance to a rounded square of half-size (inner) with corner radius
                    float dx = Mathf.Abs(x + 0.5f - c) - (inner - radius);
                    float dy = Mathf.Abs(y + 0.5f - c) - (inner - radius);
                    float outside = new Vector2(Mathf.Max(dx, 0), Mathf.Max(dy, 0)).magnitude + Mathf.Min(Mathf.Max(dx, dy), 0) - radius;
                    float a = Mathf.Clamp01(0.5f - outside);
                    if (border > 0) a *= Mathf.Clamp01(outside + border + 0.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            int b = radius + 4;
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            s.name = key;
            cache[key] = s;
            return s;
        }

        public static Sprite Circle(int size = 64, bool ring = false, int thickness = 6)
        {
            string key = $"c{size}_{ring}_{thickness}";
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = key };
            var px = new Color32[size * size];
            float c = size / 2f, r = size / 2f - 1;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
                    float a = Mathf.Clamp01(r - d + 0.5f);
                    if (ring) a *= Mathf.Clamp01(d - (r - thickness) + 0.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
            cache[key] = s;
            return s;
        }

        public static Sprite Glow(int size = 128)
        {
            string key = $"g{size}";
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = key };
            var px = new Color32[size * size];
            float c = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c)) / c;
                    float a = Mathf.Pow(Mathf.Clamp01(1 - d), 2.2f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
            cache[key] = s;
            return s;
        }

        /// <summary>Diagonal stripes, used for "locked" and warning bars.</summary>
        public static Sprite Stripes()
        {
            const string key = "stripes";
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, name = key };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    px[y * size + x] = ((x + y) / 8) % 2 == 0 ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 40);
            tex.SetPixels32(px);
            tex.Apply(false, true);
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
            cache[key] = s;
            return s;
        }

        static Texture2D white;
        public static Texture2D White
        {
            get
            {
                if (white == null)
                {
                    white = new Texture2D(2, 2) { name = "white" };
                    white.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                    white.Apply();
                }
                return white;
            }
        }
    }
}
