# GLITCHED

**Knowledge is your weapon.** A 2.5D science adventure where every fight is won by what you know.

A project for the **CBSE Skill Expo & Guidance Festival 2026-27** (Category II, Classes 11–12) by the GLITCHED student team, **DAV Public School, Defence Colony, Dehradun**.

![Title screen](Docs/Screens/site/01_title_screen.jpg)

---

## The game

It's the night before exams. Shaurya's monitor shatters, a living glitch tears out of it, and he is pulled into the worlds of the CBSE syllabus, each one run by a boss who broke its laws. He can't win with a sword. He wins by *knowing* things.

| Chapter | Subject | Bosses |
|---|---|---|
| 1. Code City | Computer Science | Sergeant Syntax · Lord Infinite Loop |
| 2. Physics Valley | Physics | Baron Von Friction · The Lord of Broken Laws |
| 3. Chemical Chaos | Chemistry | Count Catalyst · The Lord of the Lab |
| 4. Bio-Zone | Biology | Professor Plaque · The Lord Within |
| 5. The Core | Everything | The Source |
| Final exam | Every chapter | **Kanha the Dard**, in the exam hall |

**What's inside**

- **Battles won by answering.** Answer fast for a CRITICAL hit, chain answers into a COMBO, and save up for an ULTIMATE.
- **10 challenge types:** drag and drop, logic, numericals, debugging, coding, simulations, matching, prediction, short answers and building things.
- **364 questions and 20 lessons** written to the CBSE Class XI syllabus (CS, Physics, Chemistry, Biology).
- **Losing teaches you.** Lose once and you get a hint. Lose twice and an interactive lab opens (loop sandbox, binary, logic gates, physics sliders, bond builder, blood-flow tracing), then a rematch.
- **Adaptive mastery.** Every concept is tracked, weak topics return more often, and the difficulty follows the player.
- **Rough paper and a scientific calculator** on every question. The timer pauses while you work.
- **8 realistic boss models**, chapter portals, an animated opening that plays by itself, and an exam-hall finale.
- **Voices recorded by the team**, plus an in-game Voice Studio to re-record any line.
- **Accessibility:** text size, a dyslexia-friendly font, colour-blind-safe answers, reduced flashing, relaxed timers and camera-sway control.
- **Runs on school PCs.** Five quality levels (Potato → Ultra) are picked by a quick test on first launch, with fullscreen or windowed play.
- **Expo mode:** a 10-minute guided demo that resets between visitors.
- **Verified copies.** Each install is approved by QR code at the stall and signed for its own PC (see below).

| | |
|---|---|
| ![Code City](Docs/Screens/site/02_code_city.jpg) | ![Lord Infinite Loop](Docs/Screens/site/04_lord_infinite_loop.jpg) |
| ![Rough paper and calculator](Docs/Screens/site/05_rough_paper_and_calculator.jpg) | ![Physics Valley](Docs/Screens/site/06_physics_valley.jpg) |

### Controls

| Key | Action |
|---|---|
| A / D or ← / → | Move |
| Space / W | Jump (hold for higher) |
| Shift | Dash |
| E | Talk / use |
| Q | Shrink (Bio-Zone) |
| 1–4 or mouse | Answer |
| R / C | Rough paper / calculator (during a question) |
| Esc | Pause |

Gamepads and touch screens work too. **Keep a pen and a notebook with you.**

---

## Getting the game

**Website: https://shaurya4325.github.io/GLITCHED/** (works on phones) · **Downloads: [GitHub release v1.0](https://github.com/shaurya4325/GLITCHED/releases/tag/v1.0)**

At the exhibition the game is also handed out by the **GLITCHED stall server**, which runs on the team's laptop:

1. Join the stall's Wi-Fi and open the address on the stall's poster (or scan its QR code).
2. On the website, **hold the install core** until it fills. `GLITCHED_Setup.exe` downloads.
3. Run it. If Windows shows *"Windows protected your PC"*, click **More info → Run anyway** (the game isn't code-signed).
4. Launch GLITCHED. It shows a **QR code**. A team member scans it with the admin phone and the game unlocks.

The server is described in **[Docs/SERVER.md](Docs/SERVER.md)**.

---

## This repository

| Folder | What's in it |
|---|---|
| `Assets/_Glitched/` | The Unity project: scripts, art, scenes, data. [Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md) is the tour. |
| `Assets/StreamingAssets/` | Questions and lessons (JSON) and the recorded voices. |
| `Server/` | The stall server: 3D website, downloads, copy verification, admin page (Node.js, no packages to install). |
| `Tools/` | Python and Blender helpers that generated content (sounds, faces, questions, boss conversion), plus the installer script. |
| `Docs/` | Guides, asset credits and screenshots. |

### Guides

- **[Docs/BUILDING.md](Docs/BUILDING.md)**: open the project, build the game, make the installer.
- **[Docs/SERVER.md](Docs/SERVER.md)**: run the stall server, approve copies, admin page, troubleshooting.
- **[Docs/WEBSITE.md](Docs/WEBSITE.md)**: the public GitHub Pages website and how to update it.
- **[Docs/EXPO_CHECKLIST.md](Docs/EXPO_CHECKLIST.md)**: what to do on the exhibition day, in order.
- **[Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md)**: how the game and the verification system are put together.
- **[Docs/ASSET_CREDITS.md](Docs/ASSET_CREDITS.md)**: every third-party model, sound, texture and font, with its licence.
- **[Assets/_Glitched/DEVLOG.md](Assets/_Glitched/DEVLOG.md)**: the development log.

### Quick start (developers)

```text
Unity 6000.0.85f1 (URP) · Windows 10/11 · Node.js 18+ for the server
```

1. Clone with Git LFS (`git lfs install` first). The models, textures and sounds are LFS files.
2. Unity Hub → **Add project from disk** → this folder.
3. Menu **Glitched → Build Everything** (first time), then open `Assets/_Glitched/Scenes/Boot.unity` and press Play.
4. Server: `cd Server` → `node server.js` → http://localhost:8080.

The editor and development builds skip copy verification; release builds need the stall server (see [Docs/BUILDING.md](Docs/BUILDING.md)).

---

## Credits and licence

Design, story, programming and voices: the GLITCHED student team, DAV Public School, Defence Colony, Dehradun.

Third-party assets are listed with their licences in **[Docs/ASSET_CREDITS.md](Docs/ASSET_CREDITS.md)**: Quaternius, Kay Lousberg, Kenney, Poly Haven and ambientCG (CC0), Mixamo (Adobe), realistic boss models from Sketchfab (CC BY 4.0, credited by author), three.js and qrcode-generator (MIT), and fonts under the SIL Open Font License.

**© 2026 the GLITCHED team. All rights reserved.** The source is published so it can be read and judged. It is not open source; see [LICENSE](LICENSE). Third-party assets keep their own licences.
