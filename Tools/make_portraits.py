# Generates dialogue portraits for the villains and NPCs (characters without a face-decal folder).
# Output: Assets/_Glitched/Resources/Portraits/<characterId>.png (512x512, transparent corners).
# Replace any PNG with hand-drawn art of the same name: no code changes needed.
import os, math, random
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.join(os.path.dirname(__file__), "..", "Assets", "_Glitched", "Resources", "Portraits")
S, K = 512, 4
INK = (20, 12, 26, 255)


def P(x, y):
    return (x * K, y * K)


def ell(d, cx, cy, rx, ry, fill, outline=None, w=0):
    d.ellipse([P(cx - rx, cy - ry), P(cx + rx, cy + ry)], fill=fill, outline=outline, width=w * K)


def poly(d, pts, fill, outline=None, w=0):
    d.polygon([P(x, y) for x, y in pts], fill=fill, outline=outline)
    if outline and w:
        d.line([P(x, y) for x, y in pts + [pts[0]]], fill=outline, width=w * K, joint="curve")


def line(d, pts, w, fill):
    d.line([P(x, y) for x, y in pts], fill=fill, width=w * K, joint="curve")
    for x, y in (pts[0], pts[-1]):
        ell(d, x, y, w / 2, w / 2, fill)


def arc(cx, cy, rx, ry, a0, a1, n=28):
    return [(cx + rx * math.cos(math.radians(a0 + (a1 - a0) * i / n)), cy + ry * math.sin(math.radians(a0 + (a1 - a0) * i / n))) for i in range(n + 1)]


def rgba(c, a=255):
    return (int(c[0] * 255), int(c[1] * 255), int(c[2] * 255), a)


def background(d, col):
    dark = tuple(int(v * 0.18) for v in col[:3]) + (255,)
    d.rectangle([P(0, 0), P(S, S)], fill=dark)
    for y in range(0, S, 10):
        d.rectangle([P(0, y), P(S, y + 3)], fill=tuple(int(v * 0.26) for v in col[:3]) + (255,))


def glitch_slices(im, seed, n=6, amp=26):
    rnd = random.Random(seed)
    for _ in range(n):
        y = rnd.randint(0, S * K - 40 * K)
        h = rnd.randint(6, 22) * K
        dx = rnd.randint(-amp, amp) * K
        band = im.crop((0, y, S * K, y + h))
        im.paste(band, (dx, y))
    return im


def finish(im, name, seed):
    im = glitch_slices(im, seed)
    out = im.resize((S, S), Image.LANCZOS)
    mask = Image.new("L", (S, S), 0)
    ImageDraw.Draw(mask).rounded_rectangle([0, 0, S - 1, S - 1], radius=56, fill=255)
    out.putalpha(mask)
    os.makedirs(ROOT, exist_ok=True)
    out.save(os.path.join(ROOT, f"{name}.png"))


def new(col):
    im = Image.new("RGBA", (S * K, S * K), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    background(d, col)
    return im, d


def lord_loop():
    cy, mg = (0.2, 0.9, 1.0), (1.0, 0.2, 0.8)
    im, d = new(cy)
    d.rounded_rectangle([P(70, 90), P(442, 400)], radius=40 * K, fill=(14, 20, 34, 255), outline=rgba(cy), width=14 * K)
    for cx in (190, 322):  # infinity-loop eyes
        for ox in (-26, 26):
            ell(d, cx + ox, 220, 30, 24, None, rgba(cy), 10)
    line(d, [(150, 160), (225, 182)], 14, rgba(mg))
    line(d, [(362, 160), (287, 182)], 14, rgba(mg))
    for i, x in enumerate(range(150, 370, 44)):  # repeating mouth: the same bar, again and again
        d.rectangle([P(x, 300), P(x + 30, 330)], fill=rgba(mg if i % 2 else cy))
    line(d, [(256, 400), (256, 470)], 20, rgba(cy))
    finish(im, "lord_loop", 1)


def lord_laws():
    pu, og = (0.6, 0.3, 1.0), (1.0, 0.6, 0.2)
    im, d = new(pu)
    poly(d, [(256, 50), (430, 230), (256, 470), (82, 230)], rgba(pu), INK, 10)  # crystal head
    poly(d, [(256, 50), (430, 230), (256, 230)], (190, 140, 255, 255))
    for side in (-1, 1):  # judge's wig curls
        for i in range(3):
            ell(d, 256 + side * 175, 200 + i * 52, 34, 26, (240, 236, 250, 255), INK, 6)
    line(d, [(170, 205), (235, 222)], 14, INK)
    line(d, [(342, 205), (277, 222)], 14, INK)
    ell(d, 205, 250, 22, 22, (255, 255, 255, 255), INK, 6); ell(d, 205, 252, 9, 9, INK)
    ell(d, 307, 250, 30, 30, (255, 255, 255, 255), rgba(og), 8); ell(d, 307, 252, 10, 10, INK)  # monocle
    line(d, [(337, 260), (360, 330)], 4, rgba(og))
    line(d, arc(256, 360, 60, 22, 200, 340), 12, INK)
    finish(im, "lord_laws", 2)


def lord_lab():
    gr, tl = (0.3, 1.0, 0.4), (0.1, 0.4, 0.4)
    im, d = new(gr)
    for i in range(9):  # wild hair
        a = math.radians(200 + i * 17.5)
        poly(d, [(256 + 120 * math.cos(a - 0.2), 220 + 120 * math.sin(a - 0.2)), (256 + 230 * math.cos(a), 220 + 210 * math.sin(a)), (256 + 120 * math.cos(a + 0.2), 220 + 120 * math.sin(a + 0.2))], (235, 245, 235, 255), INK, 5)
    ell(d, 256, 280, 150, 170, (190, 240, 200, 255), INK, 10)
    d.rectangle([P(100, 205), P(412, 235)], fill=INK)
    for cx in (195, 317):  # goggles
        ell(d, cx, 225, 56, 50, rgba(gr), INK, 12)
        ell(d, cx - 14, 210, 14, 12, (255, 255, 255, 220))
    line(d, arc(256, 330, 80, 60, 15, 165), 14, INK)  # manic grin
    for x in range(200, 320, 24):
        line(d, [(x, 352), (x + 6, 376)], 4, INK)
    poly(d, [(380, 400), (450, 400), (470, 470), (360, 470)], rgba(tl), INK, 6)  # flask
    ell(d, 415, 440, 32, 18, rgba(gr))
    finish(im, "lord_lab", 3)


def lord_within():
    rd, pk = (0.8, 0.1, 0.3), (1.0, 0.5, 0.7)
    im, d = new(rd)
    for i in range(14):  # virus spikes
        a = math.radians(i * 360 / 14)
        x, y = 256 + 175 * math.cos(a), 290 + 160 * math.sin(a)
        line(d, [(256 + 130 * math.cos(a), 290 + 120 * math.sin(a)), (x, y)], 12, rgba(pk))
        ell(d, x, y, 16, 16, rgba(pk), INK, 4)
    ell(d, 256, 290, 140, 130, rgba(rd), INK, 10)
    d.rectangle([P(196, 70), P(316, 170)], fill=INK)  # impeccable top hat
    d.rectangle([P(160, 160), P(352, 182)], fill=INK)
    d.rectangle([P(196, 140), P(316, 152)], fill=rgba(pk))
    for cx in (210, 302):  # half-closed polite eyes
        ell(d, cx, 270, 26, 22, (255, 255, 255, 255), INK, 5)
        d.rectangle([P(cx - 30, 244), P(cx + 30, 268)], fill=rgba(rd))
        line(d, [(cx - 28, 268), (cx + 28, 268)], 6, INK)
        ell(d, cx, 278, 8, 8, INK)
    line(d, arc(256, 330, 46, 18, 20, 160), 8, INK)
    finish(im, "lord_within", 4)


def the_source():
    wh, mg = (1.0, 1.0, 1.0), (1.0, 0.2, 0.8)
    im, d = new((0.55, 0.35, 0.75))
    for r, c in ((210, (255, 60, 200, 255)), (170, (40, 230, 255, 255)), (130, (255, 255, 255, 255))):
        ell(d, 256, 256, r, r, None, c, 8)
    ell(d, 256, 256, 96, 96, (255, 255, 255, 255))
    ell(d, 256, 256, 52, 64, rgba(mg))
    ell(d, 256, 256, 18, 40, INK)
    for i in range(12):  # orbiting code fragments
        a = math.radians(i * 30 + 8)
        x, y = 256 + 236 * math.cos(a), 256 + 236 * math.sin(a)
        d.rectangle([P(x - 10, y - 6), P(x + 10, y + 6)], fill=(255, 255, 255, 255))
    finish(im, "the_source", 5)


def npc():
    bl = (0.6, 0.7, 0.9)
    im, d = new(bl)
    ell(d, 256, 280, 140, 150, (225, 200, 180, 255), INK, 10)
    d.pieslice([P(116, 130), P(396, 330)], 180, 360, fill=(70, 50, 60, 255))
    for cx in (205, 307):  # frozen mid-blink
        ell(d, cx, 275, 22, 26, (255, 255, 255, 255), INK, 5)
        ell(d, cx, 278, 10, 12, INK)
    line(d, [(215, 355), (297, 355)], 10, INK)
    d.rectangle([P(140, 300), P(372, 318)], fill=(80, 240, 255, 160))  # stuck scan line
    finish(im, "npc", 6)


if __name__ == "__main__":
    for f in (lord_loop, lord_laws, lord_lab, lord_within, the_source, npc):
        f()
    print("portraits written to", os.path.abspath(ROOT))
