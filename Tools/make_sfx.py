# Synthesises GLITCHED's comedic and feedback sound effects (original, CC0 by the team).
import os, wave
import numpy as np

SR = 32000
OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "_Glitched", "Audio", "SFX", "generated")
rng = np.random.default_rng(3)


def save(name, x, vol=0.8):
    x = np.asarray(x, dtype=np.float64)
    peak = np.max(np.abs(x)) or 1
    x = x / peak * vol
    fade = min(len(x), int(0.01 * SR))
    x[-fade:] *= np.linspace(1, 0, fade)
    os.makedirs(OUT, exist_ok=True)
    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((np.clip(x, -1, 1) * 32767).astype(np.int16).tobytes())


def t(sec): return np.arange(int(sec * SR)) / SR


def tone(freq_curve, kind="sine"):
    ph = np.cumsum(freq_curve) / SR
    if kind == "saw": return 2 * (ph % 1) - 1
    if kind == "square": return np.sign(np.sin(2 * np.pi * ph))
    if kind == "tri": return 2 * np.abs(2 * (ph % 1) - 1) - 1
    return np.sin(2 * np.pi * ph)


def env(n, a=0.005, r=0.1):
    e = np.ones(n)
    na, nr = int(a * SR), int(r * SR)
    if na: e[:na] = np.linspace(0, 1, na)
    if nr: e[-nr:] *= np.linspace(1, 0, nr)
    return e


def lp(x, w):
    return np.convolve(x, np.ones(w) / w, mode="same")


def comedy_fail():
    out = []
    for i, (f, d) in enumerate([(392, 0.32), (370, 0.32), (349, 0.32), (330, 0.9)]):
        tt = t(d)
        vib = 1 + (0.03 * np.sin(2 * np.pi * 6 * tt) if i == 3 else 0)
        bend = np.linspace(1, 0.94 if i == 3 else 0.99, len(tt))
        x = tone(f * vib * bend, "saw") * 0.6 + tone(f * 2 * vib * bend, "square") * 0.15
        x = lp(x, 9) * env(len(tt), 0.02, 0.08)
        out.append(x)
    save("comedy_fail", np.concatenate(out), 0.7)


def slide_whistle():
    tt = t(0.8)
    f = np.linspace(1800, 300, len(tt)) * (1 + 0.02 * np.sin(2 * np.pi * 9 * tt))
    save("fall", tone(f) * env(len(tt), 0.01, 0.1), 0.6)


def boing():
    tt = t(0.45)
    f = 180 + 260 * np.exp(-tt * 9) * (1 + 0.4 * np.sin(2 * np.pi * 18 * tt))
    save("boing", tone(f) * np.exp(-tt * 5), 0.7)


def blips():
    for i, base in enumerate([420, 470, 520, 560, 610, 680]):
        tt = t(0.055)
        f = base * np.linspace(1, 1 + rng.uniform(-0.15, 0.25), len(tt))
        x = tone(f, "square") * 0.5 + tone(f * 0.5, "tri") * 0.5
        save(f"blip_{i}", lp(x, 3) * env(len(tt), 0.003, 0.02), 0.45)
    for i, base in enumerate([900, 1050, 1200, 1350]):
        tt = t(0.045)
        f = base * np.linspace(1, 1.3, len(tt))
        save(f"blip_pixel_{i}", tone(f, "sine") * env(len(tt), 0.002, 0.02), 0.4)


def boss_roar():
    tt = t(1.6)
    n = rng.uniform(-1, 1, len(tt))
    growl = lp(n, 40) * (0.6 + 0.4 * np.sin(2 * np.pi * 28 * tt))
    sub = tone(55 + 20 * np.sin(2 * np.pi * 0.8 * tt), "saw") * 0.6
    save("boss_roar", (growl * 3 + sub) * env(len(tt), 0.15, 0.5), 0.9)


def boom():
    tt = t(1.2)
    f = 40 + 120 * np.exp(-tt * 10)
    x = tone(f) * np.exp(-tt * 3) + lp(rng.uniform(-1, 1, len(tt)), 25) * np.exp(-tt * 8) * 0.8
    save("boom", x, 1.0)


def crit():
    tt = t(0.7)
    shimmer = sum(tone(np.full(len(tt), f)) * np.exp(-tt * (4 + i)) for i, f in enumerate([1320, 1760, 2093, 2637]))
    whoosh = lp(rng.uniform(-1, 1, len(tt)), 6) * np.exp(-tt * 6) * 0.5
    hit = tone(60 + 200 * np.exp(-tt * 30)) * np.exp(-tt * 12)
    save("crit", shimmer * 0.4 + whoosh + hit, 0.9)


def correct():
    out = []
    for f in [784, 988, 1319]:
        tt = t(0.09)
        out.append((tone(np.full(len(tt), f), "tri") * 0.7 + tone(np.full(len(tt), f * 2)) * 0.2) * env(len(tt), 0.003, 0.04))
    tt = t(0.35)
    out.append(tone(np.full(len(tt), 1568), "tri") * np.exp(-tt * 7))
    save("correct", np.concatenate(out), 0.6)


def wrong():
    tt = t(0.45)
    x = (tone(np.full(len(tt), 150), "square") + tone(np.full(len(tt), 157), "square")) * 0.5
    save("wrong", lp(x, 5) * env(len(tt), 0.005, 0.1), 0.55)


def mastered():
    out = []
    notes = [523, 659, 784, 1047, 784, 1047, 1319]
    for i, f in enumerate(notes):
        d = 0.12 if i < len(notes) - 1 else 0.7
        tt = t(d)
        x = (tone(np.full(len(tt), f), "square") * 0.35 + tone(np.full(len(tt), f), "tri") * 0.6) * env(len(tt), 0.004, 0.05 if d < 0.2 else 0.4)
        out.append(x)
    save("mastered", np.concatenate(out), 0.7)


def restore():
    tt = t(2.5)
    x = np.zeros(len(tt))
    for i, f in enumerate([262, 330, 392, 523, 659, 784]):
        start = int(i * 0.25 * SR)
        seg = tone(np.full(len(tt) - start, f), "sine") * np.exp(-np.arange(len(tt) - start) / SR * 1.5)
        x[start:] += seg
    save("restore", x * env(len(tt), 0.05, 0.6), 0.7)


def glitch_big():
    tt = t(0.9)
    n = rng.uniform(-1, 1, len(tt))
    step = 40
    held = np.repeat(n[::step], step)[:len(tt)]
    q = np.round(held * 4) / 4
    tones = tone(200 + 600 * (np.floor(tt * 30) % 5), "square") * 0.4
    save("glitch_big", (q + tones) * env(len(tt), 0.005, 0.2), 0.7)


def transition():
    tt = t(0.5)
    f = 200 * np.exp(tt * 4)
    save("transition", (tone(f, "saw") * 0.3 + lp(rng.uniform(-1, 1, len(tt)), 4) * 0.4) * env(len(tt), 0.02, 0.15), 0.5)


def shrink_grow():
    tt = t(0.4)
    save("shrink", tone(np.linspace(900, 200, len(tt)), "tri") * env(len(tt), 0.01, 0.1), 0.6)
    save("grow", tone(np.linspace(200, 900, len(tt)), "tri") * env(len(tt), 0.01, 0.1), 0.6)


def gravity_flip():
    tt = t(0.6)
    f = 300 + 250 * np.sin(2 * np.pi * 7 * tt) * np.linspace(1, 0, len(tt))
    save("gravity_flip", tone(f, "sine") * env(len(tt), 0.01, 0.2), 0.6)


def timer_tick():
    tt = t(0.05)
    save("timer_tick", tone(np.full(len(tt), 1800), "square") * np.exp(-tt * 80), 0.35)


def heartbeat():
    tt = t(0.8)
    x = np.zeros(len(tt))
    for off, g in ((0, 1.0), (0.26, 0.7)):
        s = int(off * SR)
        seg = t(0.25)
        x[s:s + len(seg)] += tone(40 + 40 * np.exp(-seg * 20)) * np.exp(-seg * 14) * g
    save("heartbeat", x, 0.9)


def bond():
    tt = t(0.5)
    x = tone(np.linspace(500, 1200, len(tt)), "sine") * np.exp(-tt * 3) + tone(np.full(len(tt), 1500)) * np.exp(-tt * 9) * 0.4
    save("bond", x, 0.6)


if __name__ == "__main__":
    comedy_fail(); slide_whistle(); boing(); blips(); boss_roar(); boom(); crit(); correct(); wrong()
    mastered(); restore(); glitch_big(); transition(); shrink_grow(); gravity_flip(); timer_tick(); heartbeat(); bond()
    print("sfx written to", os.path.abspath(OUT))
