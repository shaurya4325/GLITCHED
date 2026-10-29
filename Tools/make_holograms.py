# Generates hologram billboard textures (white-on-black, tinted in the shader).
import os, random, math
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.join(os.path.dirname(__file__), "..")
OUT = os.path.join(ROOT, "Assets", "_Glitched", "Art", "Textures", "Holograms")
FONTS = os.path.join(ROOT, "Assets", "_Glitched", "Art", "Fonts")
random.seed(5)


def font(name, size):
    return ImageFont.truetype(os.path.join(FONTS, name), size)


def canvas(w=512, h=512):
    return Image.new("RGB", (w, h), (0, 0, 0))


def save(im, name):
    os.makedirs(OUT, exist_ok=True)
    im.save(os.path.join(OUT, name + ".png"))


def centered(d, w, h, text, f, y=None, fill=(255, 255, 255)):
    bb = d.textbbox((0, 0), text, font=f)
    tw, th = bb[2] - bb[0], bb[3] - bb[1]
    d.text(((w - tw) / 2, (h - th) / 2 if y is None else y), text, font=f, fill=fill)


def code_lines():
    im = canvas(); d = ImageDraw.Draw(im); f = font("JetBrainsMono.ttf", 22)
    lines = ["for i in range(10):", "    if i % 2 == 0:", "        print(i)", "while True:", "    glitch()", "x = x + 1", "def fix(city):", "    return city", "nexus.reboot()", "if key and not alarm:", "    door.open()", "binary = 0b1011", "lift.floor = -1  # ???", "except GlitchError:", "    pass  # :(", "robots = []", "robots.append('bonk')"]
    y = 6
    while y < 512:
        line = random.choice(lines)
        ind = random.choice([0, 0, 24, 48])
        d.text((10 + ind, y), line, font=f, fill=(255, 255, 255) if random.random() > 0.2 else (140, 140, 140))
        y += 30
    save(im, "code_lines")


def binary_rain():
    im = canvas(); d = ImageDraw.Draw(im); f = font("JetBrainsMono.ttf", 20)
    for x in range(0, 512, 18):
        for y in range(0, 512, 22):
            if random.random() < 0.7:
                v = random.randint(80, 255)
                d.text((x, y), random.choice("01"), font=f, fill=(v, v, v))
    save(im, "binary_rain")


def banner(name, title, sub="", size=64):
    im = canvas(1024, 512); d = ImageDraw.Draw(im)
    d.rectangle([12, 12, 1011, 499], outline=(255, 255, 255), width=6)
    centered(d, 1024, 512, title, font("RussoOne-Regular.ttf", size), 150)
    if sub: centered(d, 1024, 512, sub, font("ChakraPetch-Medium.ttf", 34), 300, (190, 190, 190))
    save(im, name)


def equations():
    im = canvas(); d = ImageDraw.Draw(im); f = font("DejaVuSans.ttf", 38)
    eqs = ["v = u + at", "F = m a", "V = I R", "W = F d", "KE = ½mv²", "PE = mgh", "v = f λ", "T = 2π√(L/g)"]
    for i, e in enumerate(eqs):
        d.text((24, 20 + i * 60), e, font=f, fill=(255, 255, 255))
    save(im, "equations")


def wave():
    im = canvas(); d = ImageDraw.Draw(im)
    for k in range(3):
        pts = [(x, 256 + math.sin(x / 512 * math.pi * (2 + k * 2)) * (150 - k * 40)) for x in range(0, 512, 4)]
        d.line(pts, fill=(255 - k * 60,) * 3, width=6 - k)
    d.line([(0, 256), (512, 256)], fill=(90, 90, 90), width=2)
    save(im, "wave")


def periodic():
    im = canvas(1024, 512); d = ImageDraw.Draw(im); f = font("ChakraPetch-Bold.ttf", 22)
    sym = "H He Li Be B C N O F Ne Na Mg Al Si P S Cl Ar K Ca".split()
    layout = [(0, 0), (17, 0), (0, 1), (1, 1), (12, 1), (13, 1), (14, 1), (15, 1), (16, 1), (17, 1), (0, 2), (1, 2), (12, 2), (13, 2), (14, 2), (15, 2), (16, 2), (17, 2), (0, 3), (1, 3)]
    cw, ch = 54, 64
    for (s, (cx, cy)) in zip(sym, layout):
        x, y = 20 + cx * cw, 40 + cy * (ch + 8)
        d.rectangle([x, y, x + cw - 6, y + ch], outline=(255, 255, 255), width=3)
        d.text((x + 8, y + 18), s, font=f, fill=(255, 255, 255))
    for cx in range(2, 12):
        for cy in range(3, 4):
            x, y = 20 + cx * cw, 40 + cy * (ch + 8)
            d.rectangle([x, y, x + cw - 6, y + ch], outline=(120, 120, 120), width=2)
    centered(d, 1024, 512, "THE PERIODIC TABLE (OF REFUSAL)", font("RussoOne-Regular.ttf", 40), 400)
    save(im, "periodic")


def molecules():
    im = canvas(); d = ImageDraw.Draw(im)
    def atom(x, y, r, label):
        d.ellipse([x - r, y - r, x + r, y + r], outline=(255, 255, 255), width=5)
        centered_text = font("ChakraPetch-Bold.ttf", int(r * 0.9))
        bb = d.textbbox((0, 0), label, font=centered_text)
        d.text((x - (bb[2] - bb[0]) / 2, y - (bb[3] - bb[1]) / 2 - 6), label, font=centered_text, fill=(255, 255, 255))
    d.line([(150, 160), (90, 230)], fill=(200, 200, 200), width=6); d.line([(150, 160), (210, 230)], fill=(200, 200, 200), width=6)
    atom(150, 150, 46, "O"); atom(90, 240, 30, "H"); atom(210, 240, 30, "H")
    d.line([(300, 380), (380, 380)], fill=(200, 200, 200), width=6); d.line([(380, 380), (460, 380)], fill=(200, 200, 200), width=6)
    atom(300, 380, 36, "O"); atom(380, 380, 40, "C"); atom(460, 380, 36, "O")
    save(im, "molecules")


def ecg():
    im = canvas(1024, 256); d = ImageDraw.Draw(im)
    pts = []
    for x in range(0, 1024, 2):
        ph = (x % 256) / 256
        y = 128
        if 0.4 < ph < 0.45: y -= 90 * math.sin((ph - 0.4) / 0.05 * math.pi)
        elif 0.45 < ph < 0.5: y += 40 * math.sin((ph - 0.45) / 0.05 * math.pi)
        elif 0.6 < ph < 0.75: y -= 20 * math.sin((ph - 0.6) / 0.15 * math.pi)
        pts.append((x, y))
    d.line(pts, fill=(255, 255, 255), width=6)
    save(im, "ecg")


def dna():
    im = canvas(256, 1024); d = ImageDraw.Draw(im)
    for y in range(0, 1024, 6):
        a = y / 1024 * math.pi * 8
        x1 = 128 + math.sin(a) * 90; x2 = 128 - math.sin(a) * 90
        d.ellipse([x1 - 6, y - 6, x1 + 6, y + 6], fill=(255, 255, 255)); d.ellipse([x2 - 6, y - 6, x2 + 6, y + 6], fill=(180, 180, 180))
        if y % 36 == 0: d.line([(x1, y), (x2, y)], fill=(150, 150, 150), width=4)
    save(im, "dna")


if __name__ == "__main__":
    code_lines(); binary_rain(); equations(); wave(); periodic(); molecules(); ecg(); dna()
    banner("welcome", "WELC0ME T0 C0DE CITY", "population: confused")
    banner("error404", "ERROR 404", "CITY NOT FOUND. HAVE YOU TRIED TURNING IT OFF AND ON?")
    banner("ad_ram", "DOWNLOAD MORE RAM", "ONLY 99.99 COINS!  (DO NOT DO THIS)")
    banner("ad_gravity", "GRAVITY: NOW OPTIONAL", "THE LORD OF BROKEN LAWS APPROVES THIS MESSAGE")
    banner("ad_water", "DRINK H2O", "IT'S JUST TWO HYDROGENS AND AN OXYGEN HOLDING HANDS", 70)
    banner("ad_bio", "WASH YOUR HANDS", "SERIOUSLY. WE'RE INSIDE SOMEONE.")
    banner("ad_core", "THE SOURCE IS WATCHING", "IT WROTE THE RULES. THEN DELETED THEM.")
    print("holograms written to", os.path.abspath(OUT))
