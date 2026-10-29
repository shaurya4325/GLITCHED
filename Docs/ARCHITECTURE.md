# How GLITCHED is put together

## Big picture

```text
                ┌─────────────── Unity game (Assets/_Glitched) ───────────────┐
 Boot scene ──► Services (managers) ──► LicenceClient gate ──► Title ──► Opening ──► Code City ─► … ─► Exam Hall
                    │                         │  UDP 47777 + HTTP 8080
                    │                         ▼
                    │              Stall server (Server/, Node.js) ◄── admin phone / admin page
                    ▼                         │
 StreamingAssets: questions, lessons, voices  └── website + downloads for visitors
```

- **Scenes are generated.** `Editor/GlitchedBuilder.cs` (menu *Glitched → Build Everything*) turns data into assets and scenes: levels are described in `Levels/LevelPlans.cs` and baked by `LevelBuilder` / `ParkourBuilder` / `BackdropBuilder`. The scenes are real and editable afterwards.
- **Content is data.** Arcs, enemies, bosses, characters, cosmetics and graphics tiers are ScriptableObjects in `Assets/_Glitched/Data` (defaults in `Core/ContentDefaults.cs`). Questions and lessons are JSON in `StreamingAssets/Questions`, so teachers can edit them without Unity.
- **Every manager lives in `Services`** (`Core/Services.cs`), created before the first scene loads, so any scene can be played directly.

## Scripts (`Assets/_Glitched/Scripts`)

| Folder | Responsibility |
|---|---|
| `Core/` | `Services` (boot), `GameManager` (save data, currencies, arcs), `SaveSystem` (3 slots), `SceneLoader` (glitch transitions), `InputHub` (keyboard, gamepad, touch), `ExpoMode`, `DevMode`, `CaptureRunner` (automated screenshots), `ParkourBot` (test bot). |
| `Licence/` | Copy verification: `LicenceClient` (find the server, pair, heartbeat), `LicenceToken` (RSA signature check), `LicenceScreen` (QR / blocked / verified UI), `LicenceAccess` (what each access type allows). |
| `Challenges/` | `ChallengeEngine` runs the 10 challenge types (`Types/`), `QuickQuiz` for battle questions, timers that pause during rough work. |
| `Combat/` | `EncounterManager` (turns, CRITICAL / COMBO / ULTIMATE), `Enemy`, `BossFight`, `MasteryCheck`, failure → hint → teaching lab → rematch. |
| `Learning/` | `QuestionBank` (JSON loader + validation), `MasteryTracker` (per-concept EMA), `QuestionSelector` (adaptive), `QuestionGenerator` (numerical variants), lessons. Teaching labs are in `Learning/TeachingMode`. |
| `Levels/` | Level plans and the builders that turn them into geometry, enemies, doors, terminals and set pieces. |
| `World/` | Corruption → restoration, chapter portals, the exam hall, blasts and demolition, checkpoints, data shards, glass shatter, floating paper. |
| `Narrative/` | Cutscenes (`CutscenePlayer`, the opening, arrivals, ending, `FinalExam`), the companion Pixel, the Glitch entity, Kanha's lines, the procedural bedroom. |
| `Player/` | `PlayerController` (run, jump, dash, wall-hop, shrink), procedural animation, faces and expressions. |
| `Characters/` | Procedural humans, machines and creatures used where no model exists. |
| `Camera/` | 2.5D camera rig with yaw sway, zones and post-processing control. |
| `Audio/` | `AudioManager`, adaptive `MusicDirector`, `VoiceBank` (recorded lines by id). |
| `UI/` | Everything on screen, built in code by `UIFactory` (menus, HUD, dialogue, rough paper + calculator, Voice Studio, settings, credits, disclaimer). |
| `Settings/` | Settings data and store, `GraphicsManager` (tiers, fullscreen/windowed), `DeviceProfiler` + `BenchmarkRunner` (first-launch tier pick). |
| `Editor/` | The builders, asset importer rules, the HQ boss setup, build menu (`BuildTools`), voice-script export. |

Rule: one MonoBehaviour per file, with the same name as the class (Unity can't load scenes otherwise).

## Copy verification, step by step

```text
Game (release build)                         Stall server
────────────────────                         ────────────
licence.json in the player data folder?
  └ verify RSA signature + this PC's id
find server: last address → 127.0.0.1:8080 → UDP broadcast "GLITCHED?"  ──►  beacon answers {port, fp}
GET /api/v1/hello  (fingerprint must match the key built into the game)
has a licence → POST /api/v1/check ───────────────────────────────────────►  active / blocked / locked / expired / unknown
no licence    → POST /api/v1/pair  ───────────────────────────────────────►  pending + 6-letter code + QR matrix
   shows QR (http://<server>/a/<code>)                    admin phone scans ─►  approve page → POST /api/admin/approve
   polls GET /api/v1/pair/<id> every 2 s ◄──────────────────────────────────  approved + signed licence
every 60 s while playing: POST /api/v1/check (new licence, access type, team message)
no server: keep playing until the licence's offline date ("off"), unless the clock went backwards
```

The licence is `base64url(json).base64url(signature)`, with fields `lid, did, typ, exp, off, iat, nm, sid`. It is signed with RSA-2048 / SHA-256 / PKCS#1 v1.5 by `Server/lib/security.js` and checked by `Licence/LicenceToken.cs` with the public key in `Resources/Licence/licence_key.json`.

Access types (`LicenceAccess`): **DEMO** blocks every scene outside Code City in `SceneLoader.Load`. **JUDGE** makes `GameManager.IsArcUnlocked` / `IsLevelOpen` return true and grants the abilities of earlier chapters. **BLOCKED** / lockdown freeze the game behind the lock screen.

## The stall server (`Server/`)

Plain Node.js (no dependencies). `server.js` holds the routes; `lib/store.js` is a JSON database written atomically (with a `.bak`). The website is static files in `public/`: `js/world.js` is a single three.js scene that the camera flies through as the page scrolls (one camera stop per `data-cam` section), and `js/site.js` handles the rest. The admin page gets live updates over Server-Sent Events. Details in [SERVER.md](SERVER.md).
