# Generates the GLITCHED foliage atlas: alpha-tested leaf clusters and grass for the hand-made trees and planters.
# Output: Assets/_Glitched/Art/Textures/Foliage/leaf_atlas.png (2048x2048 RGBA, 2x2 cells)
#   cell 0 (bottom-left)  broadleaf cluster (street trees)
#   cell 1 (bottom-right) small-leaf cluster (birch / ornamental)
#   cell 2 (top-left)     grass tuft
#   cell 3 (top-right)    hedge / shrub leaves
import os, math, random
from PIL import Image, ImageDraw, ImageFilter

OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "_Glitched", "Art", "Textures", "Foliage")
S = 2048
C = S // 2
K = 2  # supersample


def leaf(d, cx, cy, length, width, angle, col, vein):
    pts = []
    for i in range(24):
        t = i / 23.0
        # leaf outline: pointed tip, rounded base
        w = math.sin(math.pi * t) ** 0.8 * width * (1.0 - 0.35 * t)
        pts.append((t * length, w))
    outline = pts + [(x, -y) for x, y in reversed(pts)]
    ca, sa = math.cos(angle), math.sin(angle)
    poly = [(cx + x * ca - y * sa, cy + x * sa + y * ca) for x, y in outline]
    d.polygon(poly, fill=col)
    d.line([(cx, cy), (cx + length * 0.95 * ca, cy + length * 0.95 * sa)], fill=vein, width=max(1, int(width * 0.08)))


def cluster(rng, size, n, leaf_len, leaf_w, greens, stem=True):
    im = Image.new("RGBA", (size * K, size * K), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    cx = cy = size * K / 2
    if stem:
        for _ in range(7):
            a = rng.uniform(0, math.pi * 2)
            r = rng.uniform(0.25, 0.42) * size * K
            d.line([(cx, cy), (cx + math.cos(a) * r, cy + math.sin(a) * r)], fill=(70, 52, 34, 255), width=int(4 * K))
    for i in range(n):
        a = rng.uniform(0, math.pi * 2)
        r = (rng.random() ** 0.6) * size * K * 0.36
        px, py = cx + math.cos(a) * r, cy + math.sin(a) * r
        g = greens[rng.randrange(len(greens))]
        shade = rng.uniform(0.7, 1.15)
        col = tuple(min(255, int(c * shade)) for c in g) + (255,)
        vein = tuple(min(255, int(c * shade * 1.18)) for c in g) + (255,)
        leaf(d, px, py, leaf_len * K * rng.uniform(0.8, 1.2), leaf_w * K * rng.uniform(0.8, 1.2), a + rng.uniform(-0.6, 0.6), col, vein)
    im = im.resize((size, size), Image.LANCZOS)
    return im


def grass(rng, size):
    im = Image.new("RGBA", (size * K, size * K), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    for _ in range(160):
        x = rng.uniform(0.1, 0.9) * size * K
        h = rng.uniform(0.35, 0.95) * size * K
        lean = rng.uniform(-0.25, 0.25) * size * K
        w = rng.uniform(5, 11) * K
        g = rng.choice([(74, 104, 46), (92, 122, 54), (60, 88, 40), (110, 130, 64)])
        base = size * K
        d.polygon([(x - w, base), (x + w, base), (x + lean, base - h)], fill=g + (255,))
    return im.resize((size, size), Image.LANCZOS)


def main():
    rng = random.Random(7)
    os.makedirs(OUT, exist_ok=True)
    atlas = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    broad = [(58, 86, 40), (72, 102, 46), (48, 74, 34), (86, 112, 52), (64, 92, 38)]
    small = [(96, 120, 52), (118, 138, 60), (80, 106, 46), (132, 146, 70)]
    hedge = [(40, 66, 32), (52, 80, 38), (34, 56, 28)]
    atlas.paste(cluster(rng, C, 420, 70, 26, broad), (0, C))          # bottom-left (UV 0..0.5, 0..0.5)
    atlas.paste(cluster(rng, C, 700, 40, 16, small), (C, C))          # bottom-right
    atlas.paste(grass(rng, C), (0, 0))                                # top-left
    atlas.paste(cluster(rng, C, 900, 30, 14, hedge, stem=False), (C, 0))  # top-right
    # bleed colour into transparent pixels so mipmaps don't get dark halos
    rgb = atlas.convert("RGB").filter(ImageFilter.MaxFilter(9))
    alpha = atlas.split()[3]
    out = Image.merge("RGBA", (*rgb.split(), alpha))
    filled = Image.composite(atlas.convert("RGB"), rgb, alpha.point(lambda a: 255 if a > 8 else 0))
    out = Image.merge("RGBA", (*filled.split(), alpha))
    out.save(os.path.join(OUT, "leaf_atlas.png"))
    print("foliage atlas written to", os.path.abspath(OUT))


if __name__ == "__main__":
    main()
