# Generates the placeholder cartoon face textures for GLITCHED.
#  - Protagonist (Aarav): 9 expressions + talk + blink, as transparent PNG face decals.
#  - Companion (Pixel): screen-eye expressions.
# Replace any PNG with hand-drawn art of the same name: no code changes needed.
import os, math
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.join(os.path.dirname(__file__), "..", "Assets", "_Glitched", "Resources", "Faces")
S = 512          # output size
K = 4            # supersampling
INK = (34, 22, 30, 255)
WHITE = (255, 255, 255, 255)
BLUSH = (255, 110, 120, 120)
SWEAT = (120, 210, 255, 255)
MOUTH = (120, 30, 45, 255)
TONGUE = (235, 95, 110, 255)


def canvas():
    im = Image.new("RGBA", (S * K, S * K), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im)


def P(x, y):
    return (x * K, y * K)


def ell(d, cx, cy, rx, ry, fill, outline=None, w=0):
    d.ellipse([P(cx - rx, cy - ry), P(cx + rx, cy + ry)], fill=fill, outline=outline, width=w * K)


def line(d, pts, w, fill=INK):
    d.line([P(*p) for p in pts], fill=fill, width=int(w * K), joint="curve")
    for p in (pts[0], pts[-1]):
        ell(d, p[0], p[1], w / 2, w / 2, fill)


def arc_pts(cx, cy, rx, ry, a0, a1, n=24):
    return [(cx + rx * math.cos(math.radians(a0 + (a1 - a0) * i / n)), cy + ry * math.sin(math.radians(a0 + (a1 - a0) * i / n))) for i in range(n + 1)]


def eye(d, cx, cy, r=46, pupil=(0, 0), pr=20, lid=0.0, squash=1.0, shine=True):
    ry = r * squash
    ell(d, cx, cy, r, ry, WHITE, INK, 7)
    px, py = cx + pupil[0], cy + pupil[1]
    ell(d, px, py, pr, pr * min(1, squash + 0.2), INK)
    if shine:
        ell(d, px - pr * 0.35, py - pr * 0.4, pr * 0.32, pr * 0.32, WHITE)
    if lid > 0:  # upper eyelid covering part of the eye
        top = cy - ry - 4
        d.rectangle([P(cx - r - 6, top - 10), P(cx + r + 6, cy - ry + 2 * ry * lid)], fill=(0, 0, 0, 0))
        line(d, [(cx - r, cy - ry + 2 * ry * lid), (cx + r, cy - ry + 2 * ry * lid)], 8)


def brow(d, cx, cy, ang, length=70, w=12, curve=0):
    a = math.radians(ang)
    dx, dy = math.cos(a) * length / 2, math.sin(a) * length / 2
    if curve:
        pts = [(cx - dx + (dx * 2) * t, cy - dy + (dy * 2) * t - curve * math.sin(math.pi * t)) for t in [i / 12 for i in range(13)]]
    else:
        pts = [(cx - dx, cy - dy), (cx + dx, cy + dy)]
    line(d, pts, w)


def finish(im, name, folder="Aarav"):
    out = im.resize((S, S), Image.LANCZOS)
    path = os.path.join(ROOT, folder)
    os.makedirs(path, exist_ok=True)
    out.save(os.path.join(path, f"face_{name}.png"))


L, R, EY = 176, 336, 230   # eye centres


def neutral(talk=False, blink=False):
    im, d = canvas()
    if blink:
        line(d, arc_pts(L, EY, 42, 14, 10, 170), 8)
        line(d, arc_pts(R, EY, 42, 14, 10, 170), 8)
    else:
        eye(d, L, EY); eye(d, R, EY)
    brow(d, L, 160, -4); brow(d, R, 160, 4)
    if talk:
        ell(d, 256, 370, 34, 26, MOUTH, INK, 7)
        ell(d, 256, 382, 18, 9, TONGUE)
    else:
        line(d, arc_pts(256, 352, 40, 16, 20, 160), 9)
    return im


def surprised():
    im, d = canvas()
    eye(d, L, EY, r=54, pr=14); eye(d, R, EY, r=54, pr=14)
    brow(d, L, 142, -10, curve=10); brow(d, R, 142, 10, curve=10)
    ell(d, 256, 380, 22, 30, MOUTH, INK, 7)
    return im


def smirk():
    im, d = canvas()
    eye(d, L, EY, pupil=(8, 4), lid=0.35); eye(d, R, EY, pupil=(8, 4), lid=0.35)
    brow(d, L, 168, 6); brow(d, R, 150, -14, curve=6)
    line(d, [(212, 372), (262, 368), (306, 342)], 9)
    return im


def cool():
    im, d = canvas()
    # sunglasses
    for cx in (L, R):
        d.rounded_rectangle([P(cx - 66, EY - 40), P(cx + 66, EY + 38)], radius=30 * K, fill=INK)
        d.polygon([P(cx - 46, EY - 26), P(cx - 14, EY - 26), P(cx - 40, EY + 6)], fill=(90, 230, 255, 255))
    line(d, [(L + 60, EY - 18), (R - 60, EY - 18)], 12)
    line(d, [(L - 66, EY - 20), (L - 96, EY - 30)], 10)
    line(d, [(R + 66, EY - 20), (R + 96, EY - 30)], 10)
    line(d, [(216, 360), (256, 372), (300, 350)], 9)
    return im


def flustered():
    im, d = canvas()
    eye(d, L, EY, r=44, pupil=(-6, 6), pr=16); eye(d, R, EY, r=44, pupil=(-6, 6), pr=16)
    brow(d, L, 168, 14); brow(d, R, 168, -14)
    ell(d, L - 20, 300, 44, 18, BLUSH); ell(d, R + 20, 300, 44, 18, BLUSH)
    pts = [(206 + i * 12, 372 + (8 if i % 2 else -8)) for i in range(9)]
    line(d, pts, 8)
    return im


def scared():
    im, d = canvas()
    eye(d, L, EY, r=56, pr=10); eye(d, R, EY, r=56, pr=10)
    brow(d, L, 150, 18, curve=-6); brow(d, R, 150, -18, curve=-6)
    d.rounded_rectangle([P(206, 350), P(306, 404)], radius=20 * K, fill=WHITE, outline=INK, width=7 * K)
    for x in (231, 256, 281):
        line(d, [(x, 352), (x, 402)], 5)
    line(d, [(208, 377), (304, 377)], 5)
    return im


def defeated():
    im, d = canvas()
    # X X eyes
    for cx in (L, R):
        line(d, [(cx - 30, EY - 30), (cx + 30, EY + 30)], 12)
        line(d, [(cx + 30, EY - 30), (cx - 30, EY + 30)], 12)
    brow(d, L, 165, 18); brow(d, R, 165, -18)
    line(d, arc_pts(256, 392, 46, 22, 200, 340), 9)
    return im


def sweating():
    im, d = canvas()
    eye(d, L, EY, pupil=(10, -4), pr=16); eye(d, R, EY, pupil=(10, -4), pr=16)
    brow(d, L, 158, 16); brow(d, R, 158, -16)
    line(d, [(206, 370), (306, 370)], 9)
    for (x, y, s) in ((404, 150, 1.0), (430, 236, 0.7), (96, 190, 0.8)):
        d.polygon([P(x, y - 34 * s), P(x - 18 * s, y), P(x + 18 * s, y)], fill=SWEAT)
        ell(d, x, y + 4 * s, 18 * s, 18 * s, SWEAT)
    return im


def melting():
    im, d = canvas()
    # drooping eyes that run down the face
    for cx in (L, R):
        ell(d, cx, EY + 20, 40, 56, WHITE, INK, 7)
        ell(d, cx, EY + 44, 14, 14, INK)
        d.rectangle([P(cx - 12, EY + 70), P(cx + 12, EY + 150)], fill=WHITE)
        line(d, [(cx - 12, EY + 72), (cx - 12, EY + 150)], 6)
        line(d, [(cx + 12, EY + 72), (cx + 12, EY + 150)], 6)
        ell(d, cx, EY + 156, 16, 16, WHITE, INK, 6)
    line(d, arc_pts(256, 420, 50, 18, 200, 340), 9)
    d.rectangle([P(286, 410), P(298, 470)], fill=MOUTH)
    ell(d, 292, 474, 10, 10, MOUTH)
    return im


def face_plate():
    """Soft-edged white oval multiplied by the skin tint in the shader, hides the model's painted face."""
    im = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.ellipse([40, 40, S - 40, S - 30], fill=(255, 255, 255, 255))
    im = im.filter(ImageFilter.GaussianBlur(14))
    os.makedirs(os.path.join(ROOT, "Aarav"), exist_ok=True)
    im.save(os.path.join(ROOT, "Aarav", "face_plate.png"))


# ---------------------------------------------------------------- Pixel, the companion (screen eye, emissive cyan on black)
CY = (80, 240, 255, 255)


def pixel(name, draw):
    im = Image.new("RGBA", (S * K, S * K), (6, 14, 24, 255))
    d = ImageDraw.Draw(im)
    for y in range(0, S, 8):  # scanlines
        d.rectangle([P(0, y), P(S, y + 2)], fill=(10, 24, 38, 255))
    draw(d)
    out = im.resize((S, S), Image.LANCZOS)
    path = os.path.join(ROOT, "Pixel")
    os.makedirs(path, exist_ok=True)
    out.save(os.path.join(path, f"eye_{name}.png"))


def px_normal(d):
    ell(d, 256, 256, 120, 120, CY)
    ell(d, 256, 256, 62, 62, (6, 14, 24, 255))
    ell(d, 226, 222, 20, 20, CY)


def px_happy(d):
    line(d, arc_pts(256, 290, 120, 90, 200, 340), 34, CY)


def px_blink(d):
    line(d, [(140, 256), (372, 256)], 30, CY)


def px_bored(d):
    ell(d, 256, 280, 120, 70, CY)
    d.rectangle([P(120, 180), P(392, 272)], fill=(6, 14, 24, 255))
    ell(d, 256, 300, 40, 30, (6, 14, 24, 255))


def px_alarm(d):
    ell(d, 256, 256, 150, 150, (255, 80, 200, 255))
    ell(d, 256, 256, 26, 26, (6, 14, 24, 255))


def px_heart(d):
    c = (255, 90, 170, 255)
    ell(d, 206, 220, 64, 64, c); ell(d, 306, 220, 64, 64, c)
    d.polygon([P(146, 246), P(366, 246), P(256, 380)], fill=c)


def px_error(d):
    m = (255, 70, 200, 255)
    line(d, [(170, 170), (342, 342)], 34, m)
    line(d, [(342, 170), (170, 342)], 34, m)


if __name__ == "__main__":
    finish(neutral(), "neutral")
    finish(neutral(talk=True), "talk")
    finish(neutral(blink=True), "blink")
    finish(surprised(), "surprised")
    finish(smirk(), "smirk")
    finish(cool(), "cool")
    finish(flustered(), "flustered")
    finish(scared(), "scared")
    finish(defeated(), "defeated")
    finish(sweating(), "sweating")
    finish(melting(), "melting")
    face_plate()
    for n, f in (("normal", px_normal), ("happy", px_happy), ("blink", px_blink), ("bored", px_bored), ("alarm", px_alarm), ("heart", px_heart), ("error", px_error)):
        pixel(n, f)
    print("faces written to", os.path.abspath(ROOT))
