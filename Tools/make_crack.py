# Generates the cracked-glass overlay for the opening cinematic's monitor (alpha = crack lines).
# Output: Assets/_Glitched/Resources/FX/monitor_crack.png (2048x860, ultrawide)
import os, math, random
from PIL import Image, ImageDraw, ImageFilter

OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "_Glitched", "Resources", "FX")
W, H = 2048, 860


def main():
    rng = random.Random(42)
    im = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(im)
    cx, cy = int(W * 0.56), int(H * 0.46)   # impact point: where the Glitch pushes through
    # radial cracks with branches
    for k in range(26):
        a = k / 26 * math.tau + rng.uniform(-0.12, 0.12)
        x, y = cx, cy
        length = rng.uniform(250, 1100)
        width = rng.uniform(3, 6)
        steps = int(length / 18)
        for s in range(steps):
            a += rng.uniform(-0.18, 0.18)
            nx, ny = x + math.cos(a) * 18, y + math.sin(a) * 18
            d.line([(x, y), (nx, ny)], fill=255, width=max(1, int(width * (1 - s / steps)) + 1))
            if rng.random() < 0.07:
                ba = a + rng.choice([-1, 1]) * rng.uniform(0.5, 1.1)
                bx, by = nx, ny
                for b in range(rng.randint(4, 14)):
                    ba += rng.uniform(-0.2, 0.2)
                    ex, ey = bx + math.cos(ba) * 14, by + math.sin(ba) * 14
                    d.line([(bx, by), (ex, ey)], fill=200, width=2)
                    bx, by = ex, ey
            x, y = nx, ny
    # concentric rings around the impact
    for r in (40, 90, 160, 260):
        pts = []
        for k in range(60):
            a = k / 60 * math.tau
            rr = r * rng.uniform(0.85, 1.15)
            pts.append((cx + math.cos(a) * rr, cy + math.sin(a) * rr * 0.9))
        for k in range(len(pts)):
            if rng.random() < 0.6:
                d.line([pts[k], pts[(k + 1) % len(pts)]], fill=180, width=2)
    # shattered centre
    d.ellipse([cx - 26, cy - 24, cx + 26, cy + 24], fill=255)
    im = im.filter(ImageFilter.GaussianBlur(0.8))
    rgba = Image.merge("RGBA", (Image.new("L", (W, H), 255),) * 3 + (im,))
    os.makedirs(OUT, exist_ok=True)
    rgba.save(os.path.join(OUT, "monitor_crack.png"))
    print("crack written")


if __name__ == "__main__":
    main()
