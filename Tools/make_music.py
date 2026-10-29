# Procedural, original music for GLITCHED (placeholder score until the team composes its own).
# Every loop is an exact number of bars, so Unity can loop it seamlessly and layer stems in sync.
import os, math
import numpy as np
import wave

SR = 32000
OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "_Glitched", "Audio", "Music")
rng = np.random.default_rng(7)

NOTE = {"C": 0, "C#": 1, "Db": 1, "D": 2, "D#": 3, "Eb": 3, "E": 4, "F": 5, "F#": 6, "Gb": 6, "G": 7, "G#": 8, "Ab": 8, "A": 9, "A#": 10, "Bb": 10, "B": 11}


def hz(n):  # "A4"
    name, octv = n[:-1], int(n[-1])
    return 440.0 * 2 ** ((NOTE[name] + 12 * (octv + 1) - 69) / 12)


def midi(n):
    name, octv = n[:-1], int(n[-1])
    return NOTE[name] + 12 * (octv + 1)


def mhz(m):
    return 440.0 * 2 ** ((m - 69) / 12)


def env(n, a=0.005, d=0.1, s=0.6, r=0.1):
    t = np.arange(n) / SR
    e = np.ones(n) * s
    na, nd, nr = int(a * SR), int(d * SR), int(r * SR)
    if na: e[:na] = np.linspace(0, 1, na, endpoint=False)
    if nd: e[na:na + nd] = np.linspace(1, s, min(nd, max(0, n - na)))[: len(e[na:na + nd])]
    if nr and n > nr: e[-nr:] *= np.linspace(1, 0, nr)
    return e


def osc(kind, f, n, phase=0.0, detune=0.0):
    t = np.arange(n) / SR
    ph = (f * (1 + detune)) * t + phase
    if kind == "sine": return np.sin(2 * np.pi * ph)
    if kind == "saw": return 2 * (ph % 1) - 1
    if kind == "square": return np.sign(np.sin(2 * np.pi * ph)) * 0.7
    if kind == "pulse": return np.where((ph % 1) < 0.25, 0.7, -0.7)
    if kind == "tri": return 2 * np.abs(2 * (ph % 1) - 1) - 1
    raise ValueError(kind)


def lowpass(x, cutoff):
    # one-pole low pass, cutoff in Hz (vectorised via cumulative trick is unstable; use a simple loop in chunks)
    a = math.exp(-2 * math.pi * cutoff / SR)
    y = np.empty_like(x)
    acc = 0.0
    for i in range(len(x)):
        acc = (1 - a) * x[i] + a * acc
        y[i] = acc
    return y


def lp_fast(x, cutoff):
    # cheaper approximate lowpass: moving average of a width matching the cutoff
    w = max(1, int(SR / (cutoff * 2.2)))
    if w <= 1: return x
    k = np.ones(w) / w
    return np.convolve(x, k, mode="same")


class Track:
    def __init__(self, bpm, bars, beats_per_bar=4):
        self.bpm, self.bars = bpm, bars
        self.beat = 60.0 / bpm
        self.n = int(round(bars * beats_per_bar * self.beat * SR))
        self.buf = np.zeros(self.n)

    def add(self, start_beat, sig, gain=1.0):
        i = int(start_beat * self.beat * SR) % self.n
        sig = sig * gain
        end = i + len(sig)
        if end <= self.n:
            self.buf[i:end] += sig
        else:  # wrap the tail to the start so loops are seamless
            k = self.n - i
            self.buf[i:] += sig[:k]
            rest = sig[k:]
            while len(rest):
                m = min(len(rest), self.n)
                self.buf[:m] += rest[:m]
                rest = rest[m:]

    def note(self, start, length, f, kind="saw", gain=0.2, a=0.005, d=0.1, s=0.6, r=0.08, detune=0.0, cutoff=None, voices=1):
        n = int(length * self.beat * SR) + int(r * SR)
        sig = np.zeros(n)
        for v in range(voices):
            dt = detune * (v - (voices - 1) / 2)
            sig += osc(kind, f, n, phase=rng.random(), detune=dt)
        sig /= voices
        if cutoff: sig = lp_fast(sig, cutoff)
        self.add(start, sig * env(n, a, d, s, r), gain)

    def kick(self, start, gain=0.9):
        n = int(0.35 * SR)
        t = np.arange(n) / SR
        f = 45 + 110 * np.exp(-t * 28)
        ph = np.cumsum(f) / SR
        self.add(start, np.sin(2 * np.pi * ph) * np.exp(-t * 9), gain)

    def snare(self, start, gain=0.35):
        n = int(0.22 * SR)
        t = np.arange(n) / SR
        noise = rng.uniform(-1, 1, n)
        noise = noise - lp_fast(noise, 1200)
        tone = np.sin(2 * np.pi * 190 * t) * np.exp(-t * 30)
        self.add(start, (noise * np.exp(-t * 18) + tone * 0.6), gain)

    def hat(self, start, gain=0.12, open_=False):
        n = int((0.25 if open_ else 0.05) * SR)
        t = np.arange(n) / SR
        noise = rng.uniform(-1, 1, n)
        noise = noise - lp_fast(noise, 6000)
        self.add(start, noise * np.exp(-t * (10 if open_ else 70)), gain)

    def blip(self, start, f, gain=0.1):
        n = int(0.08 * SR)
        t = np.arange(n) / SR
        self.add(start, osc("square", f, n) * np.exp(-t * 40), gain)

    def heartbeat(self, start, gain=0.7):
        for off, g in ((0, 1.0), (0.28, 0.7)):
            n = int(0.25 * SR)
            t = np.arange(n) / SR
            f = 40 + 40 * np.exp(-t * 20)
            self.add(start + off, np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 14) * g, gain)

    def bubble(self, start, gain=0.12):
        n = int(0.12 * SR)
        t = np.arange(n) / SR
        f0 = rng.uniform(400, 900)
        f = f0 * (1 + 2.5 * t / 0.12)
        self.add(start, np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 25), gain)

    def echo(self, delay_beats=0.75, fb=0.35, mix=0.3):
        d = int(delay_beats * self.beat * SR)
        out = self.buf.copy()
        tap = self.buf.copy()
        for _ in range(5):
            tap = np.roll(tap, d) * fb
            out += tap * mix / fb
        self.buf = out

    def render(self, name, master=0.8):
        x = self.buf
        peak = np.max(np.abs(x)) or 1
        x = np.tanh(x / peak * 1.4) * master
        # gentle stereo: slightly delayed copy on the right
        d = int(0.011 * SR)
        L = x
        Rr = np.roll(x, d) * 0.92 + x * 0.08
        st = np.stack([L, Rr], axis=1)
        pcm = (np.clip(st, -1, 1) * 32767).astype(np.int16)
        os.makedirs(OUT, exist_ok=True)
        with wave.open(os.path.join(OUT, name + ".wav"), "wb") as w:
            w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR)
            w.writeframes(pcm.tobytes())
        print("wrote", name, f"{len(x) / SR:.1f}s")


def chord(root, quality="m"):
    r = midi(root)
    third = 3 if quality == "m" else 4
    return [r, r + third, r + 7]


def pads(t, prog, octave_shift=0, gain=0.09, kind="saw", cutoff=1800, beats=4):
    for i, (root, q) in enumerate(prog * max(1, (t.bars * 4) // (len(prog) * beats))):
        for m in chord(root, q):
            t.note(i * beats, beats, mhz(m + octave_shift), kind, gain, a=0.4, d=0.5, s=0.8, r=0.6, detune=0.006, cutoff=cutoff, voices=3)


def arp(t, prog, pattern=(0, 1, 2, 1), step=0.5, octave=12, gain=0.07, kind="pulse", beats=4):
    steps = int(beats / step)
    for i, (root, q) in enumerate(prog * max(1, (t.bars * 4) // (len(prog) * beats))):
        c = chord(root, q) + [chord(root, q)[0] + 12]
        for s in range(steps):
            m = c[pattern[s % len(pattern)] % len(c)] + octave
            t.note(i * beats + s * step, step * 0.9, mhz(m), kind, gain, a=0.002, d=0.08, s=0.3, r=0.05, cutoff=4000)


def bass(t, prog, pattern="8ths", gain=0.22, kind="saw", beats=4):
    for i, (root, q) in enumerate(prog * max(1, (t.bars * 4) // (len(prog) * beats))):
        r = midi(root) - 24
        if pattern == "8ths":
            for s in range(int(beats * 2)):
                t.note(i * beats + s * 0.5, 0.45, mhz(r + (12 if s % 4 == 3 else 0)), kind, gain, a=0.002, d=0.1, s=0.5, r=0.03, cutoff=700)
        elif pattern == "whole":
            t.note(i * beats, beats, mhz(r), "sine", gain * 1.4, a=0.05, d=0.3, s=0.8, r=0.2)
        elif pattern == "pump":
            for s in range(beats):
                t.note(i * beats + s + 0.5, 0.45, mhz(r), kind, gain, a=0.002, d=0.12, s=0.4, r=0.03, cutoff=600)


def drums(t, style="four", beats=4, hats=True):
    for b in range(t.bars * beats):
        if style == "four":
            t.kick(b)
            if b % 2 == 1: t.snare(b)
        elif style == "half":
            if b % 4 == 0: t.kick(b)
            if b % 4 == 2: t.snare(b)
        elif style == "boss":
            t.kick(b); t.kick(b + 0.75, 0.5)
            if b % 2 == 1: t.snare(b, 0.45)
        if hats:
            t.hat(b + 0.5)
            if style == "boss": t.hat(b + 0.25, 0.06); t.hat(b + 0.75, 0.06)


def melody(t, notes, gain=0.08, kind="square", cutoff=3500):
    # notes: list of (beat, length, "A4") ; repeats to fill bars
    span = max(b + l for b, l, _ in notes)
    reps = int(t.bars * 4 // span)
    for r in range(reps):
        for b, l, n in notes:
            t.note(r * span + b, l, hz(n), kind, gain, a=0.01, d=0.15, s=0.6, r=0.12, cutoff=cutoff, detune=0.003, voices=2)


AM = [("A3", "m"), ("F3", "M"), ("C4", "M"), ("G3", "M")]
DM = [("D3", "m"), ("Bb3", "M"), ("F3", "M"), ("C4", "M")]
EM = [("E3", "m"), ("C3", "M"), ("G3", "M"), ("D3", "M")]
CM = [("C3", "M"), ("A3", "m"), ("F3", "M"), ("G3", "M")]
FSM = [("F#3", "m"), ("D3", "M"), ("A3", "M"), ("E3", "M")]
BOSS = [("A3", "m"), ("A3", "m"), ("F3", "M"), ("G3", "M")]


def title():
    t = Track(96, 16)
    pads(t, AM, gain=0.08, cutoff=1400)
    arp(t, AM, pattern=(0, 2, 1, 3, 2, 1), step=1 / 3, octave=12, gain=0.045, kind="tri")
    bass(t, AM, "whole", gain=0.16)
    for b in range(0, 64, 4): t.kick(b, 0.5)
    for b in range(2, 64, 4): t.snare(b, 0.18)
    melody(t, [(0, 1.5, "E5"), (1.5, 0.5, "D5"), (2, 2, "C5"), (4, 1.5, "A4"), (5.5, 0.5, "C5"), (6, 2, "D5"),
               (8, 1.5, "E5"), (9.5, 0.5, "G5"), (10, 2, "E5"), (12, 3, "D5"), (15, 1, "B4")], gain=0.06, kind="tri")
    t.echo(0.75, 0.4, 0.35)
    t.render("title")


def explore_codecity():
    base = Track(112, 16)
    bass(base, AM, "8ths", gain=0.2)
    drums(base, "four")
    pads(base, AM, gain=0.05, cutoff=1200)
    base.render("explore_CodeCity")
    lead = Track(112, 16)
    arp(lead, AM, pattern=(0, 1, 2, 3, 2, 1, 0, 2), step=0.25, gain=0.05)
    for b in range(0, 64, 2): lead.blip(b + 1.75, 1760 if b % 4 else 1320, 0.04)
    lead.echo(0.5, 0.3, 0.25)
    lead.render("explore_CodeCity_lead")


def explore_physics():
    base = Track(100, 16)
    pads(base, DM, gain=0.07, kind="tri", cutoff=2200)
    bass(base, DM, "pump", gain=0.2, kind="tri")
    drums(base, "half")
    base.render("explore_PhysicsValley")
    lead = Track(100, 16)
    melody(lead, [(0, 1, "D5"), (1, 1, "F5"), (2, 2, "A5"), (4, 1, "G5"), (5, 1, "F5"), (6, 2, "D5"), (8, 1, "C5"), (9, 1, "D5"), (10, 2, "F5"), (12, 4, "E5")], gain=0.05, kind="tri")
    lead.echo(1.0, 0.45, 0.4)
    lead.render("explore_PhysicsValley_lead")


def explore_chem():
    base = Track(118, 16)
    bass(base, EM, "8ths", gain=0.18, kind="square")
    drums(base, "four")
    for b in range(0, 64):
        if rng.random() < 0.6: base.bubble(b + rng.choice([0, 0.25, 0.5, 0.75]), 0.08)
    base.render("explore_ChemicalChaos")
    lead = Track(118, 16)
    arp(lead, EM, pattern=(0, 2, 1, 3), step=0.25, gain=0.05, kind="square")
    lead.echo(0.75, 0.3, 0.25)
    lead.render("explore_ChemicalChaos_lead")


def explore_bio():
    base = Track(76, 8)
    pads(base, FSM, gain=0.08, kind="sine", cutoff=900)
    for b in range(0, 32): base.heartbeat(b, 0.6)
    bass(base, FSM, "whole", gain=0.18)
    base.render("explore_BioZone")
    lead = Track(76, 8)
    melody(lead, [(0, 2, "C#5"), (2, 2, "A4"), (4, 2, "E5"), (6, 2, "C#5"), (8, 3, "B4"), (11, 1, "A4"), (12, 4, "F#4")], gain=0.05, kind="sine")
    lead.echo(1.0, 0.5, 0.45)
    lead.render("explore_BioZone_lead")


def explore_core():
    base = Track(126, 16)
    bass(base, BOSS, "8ths", gain=0.2, kind="square")
    drums(base, "four")
    pads(base, BOSS, gain=0.05, cutoff=900, kind="square")
    base.render("explore_TheCore")
    lead = Track(126, 16)
    arp(lead, BOSS, pattern=(0, 3, 1, 2), step=0.25, octave=24, gain=0.035)
    lead.render("explore_TheCore_lead")


def puzzle():
    t = Track(84, 8)
    pads(t, [("D3", "m"), ("A3", "m")], gain=0.06, kind="sine", cutoff=700, beats=8)
    for b in range(0, 32, 2):
        t.note(b + rng.choice([0, 0.5, 1]), 0.25, mhz(midi(rng.choice(["D5", "F5", "A5", "E5", "C6"]))), "tri", 0.035, a=0.003, d=0.2, s=0.0, r=0.3)
    for b in range(0, 32, 4): t.hat(b + 2, 0.05, open_=True)
    t.echo(1.5, 0.5, 0.45)
    t.render("puzzle")


def combat():
    t = Track(132, 8)
    bass(t, BOSS, "8ths", gain=0.22)
    drums(t, "four")
    arp(t, BOSS, pattern=(0, 1, 2, 1), step=0.25, gain=0.045)
    t.render("combat")


def boss_stems():
    # three stems of identical length: base (phase 1+), drive (phase 3+), lead (phase 5)
    base = Track(140, 8)
    bass(base, BOSS, "8ths", gain=0.24)
    drums(base, "boss")
    base.render("boss_1_base")
    drive = Track(140, 8)
    pads(drive, BOSS, gain=0.07, cutoff=1600)
    arp(drive, BOSS, pattern=(0, 1, 2, 3), step=0.25, octave=12, gain=0.05, kind="saw")
    drive.render("boss_2_drive")
    lead = Track(140, 8)
    melody(lead, [(0, 0.5, "A5"), (0.5, 0.5, "A5"), (1, 1, "C6"), (2, 0.5, "B5"), (2.5, 0.5, "A5"), (3, 1, "E5"),
                  (4, 0.5, "F5"), (4.5, 0.5, "G5"), (5, 1, "A5"), (6, 1, "G5"), (7, 1, "E5")], gain=0.07, kind="square")
    for b in range(0, 32, 1): lead.hat(b + 0.5, 0.05, open_=True)
    lead.render("boss_3_lead")


def victory():
    t = Track(128, 4)
    prog = [("C4", "M"), ("F4", "M"), ("G4", "M"), ("C4", "M")]
    pads(t, prog, gain=0.08, cutoff=3000)
    arp(t, prog, pattern=(0, 1, 2, 3), step=0.25, gain=0.05, kind="tri", octave=12)
    melody(t, [(0, 0.5, "C5"), (0.5, 0.5, "E5"), (1, 0.5, "G5"), (1.5, 1.5, "C6"), (4, 0.5, "A5"), (4.5, 0.5, "C6"), (5, 2, "F6"),
               (8, 0.5, "G5"), (8.5, 0.5, "B5"), (9, 2, "D6"), (12, 4, "C6")], gain=0.07, kind="square")
    drums(t, "four")
    t.render("victory")


def restoration():
    t = Track(90, 8)
    prog = [("C3", "M"), ("G3", "M"), ("A3", "m"), ("F3", "M")]
    pads(t, prog, gain=0.09, cutoff=2600)
    arp(t, prog, pattern=(0, 1, 2, 3, 2, 1), step=1 / 3, octave=24, gain=0.035, kind="tri")
    bass(t, prog, "whole", gain=0.16)
    t.echo(0.75, 0.45, 0.4)
    t.render("restoration")


def opening_room():
    t = Track(80, 8)
    prog = [("F3", "M"), ("A3", "m"), ("D3", "m"), ("C3", "M")]
    pads(t, prog, gain=0.06, kind="tri", cutoff=1000)
    for b in range(32):
        if b % 2 == 0: t.kick(b, 0.35)
        if b % 4 == 2: t.snare(b, 0.12)
        t.hat(b + 0.5, 0.05)
    melody(t, [(0, 1, "A4"), (1, 1, "C5"), (2, 2, "F5"), (4, 1, "E5"), (5, 1, "D5"), (6, 2, "C5")], gain=0.04, kind="sine")
    t.render("opening_room")


def ending():
    t = Track(92, 16)
    prog = [("C3", "M"), ("A3", "m"), ("F3", "M"), ("G3", "M")]
    pads(t, prog, gain=0.08, cutoff=2400)
    bass(t, prog, "pump", gain=0.16, kind="tri")
    drums(t, "half")
    melody(t, [(0, 1, "E5"), (1, 1, "G5"), (2, 2, "C6"), (4, 1, "A5"), (5, 1, "G5"), (6, 2, "E5"), (8, 1, "F5"), (9, 1, "A5"), (10, 2, "C6"), (12, 3, "B5"), (15, 1, "G5")], gain=0.06, kind="tri")
    t.echo(0.75, 0.4, 0.3)
    t.render("ending")


if __name__ == "__main__":
    title(); explore_codecity(); explore_physics(); explore_chem(); explore_bio(); explore_core()
    puzzle(); combat(); boss_stems(); victory(); restoration(); opening_room(); ending()
