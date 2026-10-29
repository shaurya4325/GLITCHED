# GLITCHED: DEVLOG

## How to open and run
1. Unity Hub -> **Add project from disk** -> `C:\code-shaurya\Glitch` (Unity **6000.0.85f1**, URP).
2. First time only: menu **Glitched -> Build Everything** (creates fonts, materials, quality tiers, data assets, animators and all scenes). Run it again whenever you add assets.
3. Open `Assets/_Glitched/Scenes/Boot.unity` and press **Play**. Any level scene can also be played directly (it creates a temporary test save with every region open).
4. Builds: **Glitched -> Build Windows (.exe)** -> `Builds/Windows/GLITCHED.exe`, **Glitched -> Build Android (.apk)** -> `Builds/Android/GLITCHED.apk`.

## What is built (all phases, in one pass)
| Phase | Status | Where |
|---|---|---|
| 0 Setup | URP (Universal 3D template), Input System, Cinemachine 3, TextMeshPro, Timeline, ProBuilder, Splines, Newtonsoft; folders; `.gitignore` + LFS attributes; 5 quality levels (Potato/Low/Medium/High/Ultra) on 3 URP assets (Mobile-Lite, Balanced, PC-High); `PlayerControls.inputactions` with Keyboard&Mouse, Gamepad and Touch schemes; `GameBootstrap`; Boot -> splash -> benchmark (first launch) -> Title | `Scripts/Core`, `Settings/`, `Resources/PlayerControls.inputactions` |
| 1 2.5D camera + movement | Perspective camera (FOV 40, ~18 back), Cinemachine Position Composer, look-ahead 2.5, **yaw sway** (orbits the player so building sides turn), jump/fall pitch, camera zones, impulse shake, encounter framing. Player: run, variable jump, coyote 0.1 s, buffer 0.12 s, double jump, dash, wall-jump, shrink/grow, gravity flips, moving platforms, PlaneLock | `Scripts/Camera`, `Scripts/Player` |
| 2 Graphics tiers | `GraphicsTierSO` x5, `DeviceProfiler` (Intel UHD/HD and low-RAM Android start at Low) + 3-second benchmark, live switching, fps-drop toast (never in boss fights), debug overlay F3 / 3-finger tap | `Scripts/Settings` |
| 3 Living background + corruption | Real geometry at L0 / L2 / L3 / L4 depths, L5 sky events; SplineTraffic, FloatingBob, WindowLightFlicker, HologramBillboard, CrowdLoop, AmbientParticles, SkyEvents, RegionFlash, GlitchProp; global `_Corruption`; Glitchable shader (full + GLITCH_LOW); restoration wave along X | `Scripts/World`, `Art/Shaders` |
| 4 Challenge engine | `IChallenge` + 10 types (Drag&Drop, Logic, Numerical, Debugging, Coding, Simulation, Matching, Prediction, Short Answer, Construction), mouse + touch + keys 1-4; JSON bank (153 questions) + validator | `Scripts/Challenges`, `StreamingAssets/Questions` |
| 5 Combat | Encounter flow with wind-up timer, ATTACK / CRITICAL (punch-in, shake, 0.15 s slow-mo) / COMBO (x1.25 steps, cap x2.5) / ULTIMATE (music drop-out + BOOM) / ENEMY TURN, Stability, battle log | `Scripts/Combat` |
| 6 Failure teaching + bosses | Defeat 1 = HINT, defeat 2 = interactive TEACHING MODE (4 labs: CS loop sandbox / binary / gates, Physics sliders, Chemistry bond lab, Biology flow trace), REMATCH with guaranteed CRITICAL; 5-phase Glitch Lords, 3-phase mini-bosses; regeneration on wrong answers; 3-question mastery check | `Scripts/Combat/BossFight.cs`, `Scripts/Learning/TeachingMode` |
| 7 Mastery, progression, save | EMA mastery per concept, adaptive selector (weak concepts weighted, no repeats, +/-1 difficulty), XP / levels, Knowledge Points (mastery only, plus perfect elites), coins + cosmetic shop, abilities, 11 achievements, 3 JSON save slots + autosave at checkpoints | `Scripts/Learning`, `Scripts/Core` |
| 8 Characters + dialogue | `ExpressionController` (9 expressions + talk/blink face decals), companion Pixel (spring follow, screen-eye moods, speech bubbles), dialogue with glitch text, `CharacterProfileSO` | `Scripts/Player`, `Scripts/Narrative`, `Resources/Faces` |
| 9-11 Arcs + final | Code City, Physics Valley, Chemical Chaos, Bio-Zone (with the shrink scene): hub + 2 levels + boss arena each; The Core (1 level + The Source); opening cinematic, first-five-minutes tutorial, arrival scenes, restoration sequence, ending + credits | `Scripts/Levels/LevelPlans.cs`, `Scenes/Arcs` |
| 12 Audio, UI, accessibility, Expo | Adaptive layered music (procedural original score), bit-crush bursts, Kenney + synthesised SFX; all menus; text size, dyslexia font, colour-blind-safe answers (shape + colour), reduce flashing, relaxed timers, sway/shake toggles; localisation CSV (English + partial Hindi); **Expo mode** (click the title logo 5x or Ctrl+Shift+E) | `Scripts/Audio`, `Scripts/UI` |
| 13 Builds | Windows build menu + batch; Android IL2CPP ARM64 min API 29 | `Scripts/Editor/BuildTools.cs` |

## How to test each part
- **Movement + parallax**: `Scenes/Test/Test_Greybox` -> run left/right, watch building sides turn and layers slide at different speeds.
- **Corruption**: `Scenes/Test/Test_Corruption` -> drag the slider 0 -> 1, press R / PLAY RESTORATION. F5 replays restoration in any level (editor/dev builds).
- **All 10 challenge types**: `Scenes/Test/Test_Challenges`.
- **Combat + failure teaching**: Code City Hub -> the first Bug Monster (rigged to be lost once). Lose twice to a boss on purpose to see the teaching lab and rematch.
- **Tiers**: Pause -> Settings -> Graphics (switch live, F3 shows fps / draw calls).
- **Save**: quit to title, Continue: progress, mastery and position persist.

## Swapping in real assets (no code changes)
- **Protagonist**: drop the Mixamo Ty FBX (with skin) into `Art/Characters/Player/`, then *Glitched -> Build Everything*. It becomes Humanoid, gets the Toon shader, the shared Humanoid animator and the face-decal expressions.
- **Mixamo animations** (without skin) go in `Art/Animations/Mixamo/`; they are added to the Humanoid controller automatically.
- **Any character/enemy/boss**: set `modelPrefab` on its asset in `Data/Characters`, `Data/Enemies` or `Data/Bosses`.
- **Region pieces**: edit `Data/Kits/Kit_<Region>` lists, then *Glitched -> Rebuild Scenes Only*.
- **Faces**: replace `Resources/Faces/Aarav/face_<expression>.png`.
- **Villain/NPC dialogue portraits**: replace `Resources/Portraits/<characterId>.png` (generated by `Tools/make_portraits.py`).
- **Questions**: edit `StreamingAssets/Questions/*.json` (see the README there).

## Decisions and notes
- Shaders are hand-written URP HLSL (`Art/Shaders`) instead of Shader Graph, so they are text, SRP-Batcher compatible and have an explicit low-end variant (`GLITCH_LOW`).
- Levels are described as data (`LevelPlans.cs`) and baked into scenes by the builder, so the scenes are real and editable in the editor.
- Cutscenes are code-driven sequences (`CutscenePlayer`) using a Cinemachine cutscene camera; they are skippable (hold Esc / hold the screen).
- Lighting is realtime (no lightmap baking) to keep the build simple; shadows only on Medium+.
- Mini-boss names (please approve or rename in `Data/Bosses`): **Sergeant Syntax** (Code City), **Baron Von Friction** (Physics Valley), **Count Catalyst** (Chemical Chaos), **Professor Plaque** (Bio-Zone).
- Defaults used: protagonist Aarav, companion Pixel, final boss The Source.
- All questions are marked `needsTeacherReview: true`.

## Known issues / next
- ~~Lord Infinite Loop placeholder~~: fixed on 2026-10-27 (realistic KW-II mech, see below).
- Optional extra Quaternius packs (Cyberpunk, Ruins, Space, Monsters) were rate-limited and are not needed; download them into `_Incoming/` and run `Tools/import_assets.py` to use them.
- The installer is not code-signed (SmartScreen: More info → Run anyway).

## Test pass (2026-10-25)
- Automated captures (`Tools/rebuild_and_capture.sh`) of Title, World Map, all region first levels and the Code City boss: no script exceptions after fixes.
- Fixed: `CharacterController.Move` on a disabled controller during cutscenes; the Opening cutscene screen texture (holograms now fall back to `Resources/Holograms`); a challenge coroutine crashing (and blocking later challenges) when the scene changed mid-question.
- Toned down the full-screen hit/transition glitch: it was hiding the whole scene at full strength.
- Captures ignore UI navigation input. Don't type or click in the game window while a capture is running.

## Mixamo characters (2026-10-25)
- Downloaded from mixamo.com with the user's login: Ty (Aarav), Exo Red (Sergeant Syntax), Warrok (Lord of Broken Laws), Brute (Count Catalyst), Mutant (Lord of the Lab), Parasite L Starkie (Lord Within), X Bot (The Source); 15 animations in `Art/Animations/Mixamo`.
- Build Everything unpacks the FBX-embedded textures into `Textures_<model>/` and links them by the file name in the Mixamo material description (Mixamo stores absolute server paths); invalid Mixamo avatars (Mutant) get an explicit mixamorig bone map.
- Mixamo clips are renamed `Mixamo_<file>` on import and preferred by the Humanoid controller; Universal Animation Library 2 fills the gaps (attacks).
- Real models keep their own face; the 2D expression decals are used on the placeholder body and in dialogue portraits.

## Realistic overhaul (2026-10-25)
Direction from the user: very realistic graphics, a clean world with only a subtle hint of glitch, no void anywhere, own (generated) assets and characters.
- **Rendering**: `Glitched/Realistic` (PBR: albedo/normal/roughness/AO, world-space UVs, alpha-cut leaves with translucency, hit flash / dissolve for characters), `Glitched/Facade` (procedural window grid + interior-mapped rooms + reflective glass on plain boxes), `Glitched/Terrain` (grass/rock/snow by slope and height), `Glitched/Water`. Shared subtle corruption in `GlitchPBR.hlsl` (slow colour drain, rare soft scan line, rare tiny tear, restoration glow). ACES tonemapping, SSAO on High/Ultra, one realtime reflection probe per level (`SceneReflection`), screen-glitch pulses made rare and faint.
- **Generators (Scripts/Levels/City)**: `MeshBuilder`, `CityMaterials` (ambientCG PBR sets via `MaterialLibrarySO.pbr`), `BuildingGen`, `StreetGen`, `CarGen`, `NatureGen`, `CityGround` (gameplay ground per region: walking surfaces sit exactly on their colliders), region backdrops `CityBackdrop`, `ValleyBackdrop`, `PlantBackdrop`, `BioBackdrop`, `CoreBackdrop`, and `CityProps` (kiosk checkpoints, sign-pillar terminals, steel shutters, light curtains, portals, floodlit boss plazas). `RoadTraffic` drives cars on the left (India).
- **Regions as real places**: Code City (downtown streets), Physics Valley (alpine valley + research campus, river ravines), Chemical Chaos (chemical plant), Bio-Zone (biotech research atrium), The Core (skyscraper rooftops over the night city, no void). World map = one landmass in the sea; bedroom rebuilt realistically.
- **Characters (Scripts/Characters)**: `HumanGen` builds a Mixamo-named humanoid (T-pose) with one skinned mesh, modelled face, blend-shape expressions and robot/mech mode; `MachineGen` + `MachineMotion` for drones, robot bugs, liquid-metal slimes and delivery bots. Built and assigned by *Build Everything* (`BuildCharacters`). Feet are planted each frame (`CharacterVisual.PlantFeet`).
- Textures: `Tools/fetch_pbr.py` (ambientCG CC0), `Tools/make_foliage.py` (generated leaf atlas). Animations: Mixamo clips + Universal Animation Library 2.

## Human pace, parkour, Class XI lessons (2026-10-27)
Direction from the user: the run looked too fast and unrealistic; add parkour moves and parkour levels; small enemies should ask CBSE Class XI MCQs; each terminal teaches a topic and the enemies after it test only that topic.
- **Movement**: walk 1.6 (analog), jog 5.0, sprint 6.8 m/s after 0.7 s of running one way; acceleration 20 m/s² (was 75), momentum kept in the air, a running take-off adds 1.2 m/s. Locomotion clips play at the speed they really cover on Shaurya's rig: Mixamo walk 1.5, UAL1 Jog_Fwd 5.06, UAL1 Sprint 7.8 m/s (measured from the root-motion versions), so the feet don't skate. The old setup ran at 7.5 m/s with the run clip at 1.65x.
- **Parkour** (`PlayerController`): ledge mantle (hands reach 2.3 m), speed vault over low blocks, slide (down at a run; keeps sliding under a low ceiling), landing roll after big drops, swing bars (a real pendulum, lean to pump), ziplines, springboards, crumbling slabs. Clips: UAL2 ClimbUp_1m / NinjaJump_Start / Slide_*, UAL1 Roll (Universal Animation Library 1, CC0, Quaternius). `CharacterVisual.Reach` lifts the arms for bars and cables.
- **Level pieces** (`LevelPlan`: `Vault`, `Duct`, `Spring`, `Zip`, `Swing`, `Crumble`, `Quiz`, `Run`; built by `ParkourBuilder`). Quiz pits: a Class XI MCQ on a holo-board, three answer pillars and a firewall; jump onto the right answer and it launches you over as the firewall dissolves, wrong pillars sink.
- **Parkour runs** (one per region, from its hub; also from the title screen): Rooftop Run (Number Systems), Pendulum Peaks (Oscillations), Reactor Rush (Periodic Trends), Synapse Sprint (Neural Control). Timed, with gold/silver/bronze and saved best times.
- **Class XI content** (`Tools/build_class11.py` -> `StreamingAssets/Lessons/Class11.json` + `Questions/Class11_*.json`): 20 topics aligned to the official CBSE 2026-27 curriculum (cbseacademic.nic.in) and NCERT chapters, 3-page lessons, 185 original MCQs (all `needsTeacherReview`).
- **Lesson terminals** (`LessonTerminal`, `LessonCard`): each teaches one topic; enemies take the topic of the last lesson terminal before them (`LevelPlan.ResolveTopics`). Skipping the lesson shows a one-line recap in the first question.
- **Quiz battles** (`QuickQuiz`): small enemies ask one MCQ at a time on a card above the fight (1-4 / A-D, click or tap), the attack charges while you think, wrong answers show the answer and why in the same card. Bosses keep the ten interactive challenge types.
- Test bot for captures: `GLITCHED.exe -capture CodeCity_Run -captureBot 1` plays a level on its own (`ParkourBot`); `Scene@x` starts at x.

## Follow-up (2026-10-27)
- Fixed: Shaurya ran facing backwards (Quaternius UAL clips are now oriented by the body on import).
- `QuestionGenerator`: fresh Class XI MCQs for all 20 topics (random numbers + NCERT fact tables); 60% of small-enemy / quiz-jump questions are generated, the rest come from the bank. Each new save reseeds the question stream; doors pick a random question on their concept.
- The four separate parkour levels are gone: every chapter's main level (and The Core) now runs through a parkour stretch before its mini-boss (`LevelPlans.ParkourStretch`).
- Developer mode: main menu, top right, password; shows the answer on every quiz card, challenge panel and quiz-jump pillar (`DevMode`).

## Explosions and depth polish (2026-10-27)
Looked at what makes 2.5D games like Limbo / Inside / Little Nightmares read so well (fog-separated planes, dark foreground silhouettes, depth of field, background events) and at "game feel" practice (flash + debris + shake + sound landing together, used with restraint).
- `Blast`: layered explosions (light flash, fireball, flame licks, 3D debris chunks that bounce, sparks/embers, rising smoke, shockwave, scorch mark, distance-scaled shake and sound) in five region styles; smoke columns and lingering fires.
- Set pieces (`LevelPlan.Demolish / Chase / BlastProp`, `SetPieceBuilder`): backdrop buildings demolished floor by floor and dropped into a dust cloud (`Demolition`, camera eases back), blasts chasing the player through every parkour stretch (`ChaseBlasts`), props that spark then blow (`PropBlast`), distant skyline blasts in corrupted regions (`AmbientBlasts`). Enemies and bosses blow apart.
- `ForegroundBuilder`: L0 silhouettes per region (poles and cables, pine trunks, pipe runs, culture tubes, lattice masts) on Glitched/ForegroundDither (dithers away over the player).
- `LightShaftBuilder`: soft god-rays behind the gameplay plane (generated Art/Textures/VFX/shaft_01.png).
- Fixed: quiz / challenge / lesson cards left over (or crashing) when the scene changes under them.
- Note: every MonoBehaviour that is saved in a scene must live in a file of the same name (four set-piece classes in one file corrupted every level file in the build).

## 2026-10-27 · realistic bosses, rough work, the chapter chain, the new opening
- Nine realistic bosses: rigged, animated Sketchfab models (CC BY, credited) converted by `Tools/blender_glb_to_fbx.py` into `Art/Characters/BossesHQ/<id>/` (FBX, Textures/, manifest). `GlitchedBuilder.BuildHQBosses` makes Glitched/Realistic PBR materials (albedo, normal, roughness/metal via the new `_MetalFromMap`, AO, emission) and assigns them; wrappers rebuild when their source model changes.
- `ScratchTools`: rough paper (`ScratchPadCanvas`, draw / erase / clear) and a scientific calculator (DEG trig, log, ln, √, x², ^, E, π, Ans) on every QuickQuiz and ChallengeEngine screen (R / C). The attack timer pauses while they are open; the calculator takes the number keys.
- Chapter chain: after each region's restoration a `ChapterPortal` opens in the arena (Code City → Physics Valley → Chemical Chaos → Bio-Zone); after the Bio-Zone a red exam rift drops Shaurya into Kanha's Examination Hall (FinalExam arrival, flag `exam_transfer`).
- Opening cutscene plays by itself (`CutscenePlayer.AutoAdvance`): the monitor screen shatters into real glass (`GlassShatter`), the Glitch bursts in, scatters onto every surface with spreading corruption veins (`GlitchEntity.Scatter`), the room levitates, then the Glitch reforms into a whirlpool and pulls him in.
- Fixes: save slots went blank after DELETE (layout groups added twice on refresh; `UIFactory.VLayout/HLayout` reuse); the pause menu is ESC only (P removed: it is a letter in typed answers), ignored while typing, and works in the exam hall.
- `DisclaimerScreen`: "keep a pen and a notebook with you", on the title menu and before every new game.
- Boss sizing: converted rigs are flattened in Blender when that keeps their clips the same size (`flatten()` + height check), and `Enemy.Renormalize` sizes bosses by their skeleton on a `BossScale` holder (Enemy.Update rewrites Model.localScale / localPosition every frame). Lord Infinite Loop now uses the KW-II mech (the Light Assault Mech rig skinned badly); Count Catalyst keeps its generated robot.
- UIGraph stops before uGUI's 65 000-vertex limit; prediction options use two columns under code.

## 2026-10-29 · stall server, verified copies, release build, public repo
- **Stall server** (`Server/`, Node.js, no dependencies): 3D website (three.js scene the camera flies through while scrolling: hero core, Code City, Physics Valley, Chemical Chaos, Bio-Zone, The Core, Exam Hall, install core), hold-to-install button, setup + portable downloads with SHA-256, live stats, visitor QR, admin page (approvals, copies, downloads, activity, settings, live over SSE), phone approve page. Desktop shortcut "GLITCHED Server": closing its window stops the server. `npm test` runs the copy lifecycle end to end.
- **Copy verification** (`Scripts/Licence/`): release builds find the server (last address, this PC, UDP beacon, typed address), show a QR + 6-letter code, and unlock when the team approves from the phone. RSA-2048 signed licence bound to the PC, re-checked every minute (block, remove, access change, lockdown, team messages), offline days with clock-rollback check. Access types: Owner, Full, Judge (every chapter open), Demo (Code City only), Blocked. Capture mode can't bypass it in release builds; editor and dev builds skip it.
- **Display setting**: Settings → Graphics → Display: FULLSCREEN / WINDOWED (remembered).
- **Release build** for the judges (no Development Build label): `BuildTools.BuildWindowsReleaseToBatch`. Builds allow http for the server connection.
- **Bedroom posters** in the opening were white in builds: their hologram textures are now in `Resources/Holograms` too.
- **Developer-mode password** is stored as a PBKDF2 hash instead of plain text.
- **Installer**: `Tools/GLITCHED_Setup.iss` (Inno Setup), per-user install, desktop + Start shortcuts, uninstaller.
- **Docs**: README, LICENSE (all rights reserved; third-party licences kept), Docs/SERVER.md, BUILDING.md, EXPO_CHECKLIST.md, ARCHITECTURE.md, updated ASSET_CREDITS.md.

