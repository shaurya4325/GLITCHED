# Building GLITCHED

## What you need

| Tool | Version | For |
|---|---|---|
| Unity | **6000.0.85f1** with Windows Build Support (and Android, optional) | the game |
| Unity Hub | any, **signed in** | Unity's licence (batch builds fail with exit code 198 without it) |
| Git + Git LFS | `git lfs install` once | models, textures, sounds and fonts are LFS files |
| Node.js | 18 or newer | the stall server (no `npm install` needed) |
| Inno Setup 6 | `winget install JRSoftware.InnoSetup` | the installer |
| Python 3 + Pillow, Blender 4.5 | optional | only to regenerate content in `Tools/` |

## Open the project

1. `git clone` the repository (with LFS), then Unity Hub → **Add project from disk** → the folder.
2. Menu **Glitched → Build Everything**: makes fonts, materials, quality tiers, data assets, animators, the realistic bosses and every scene. Run it again after adding assets.
3. Open `Assets/_Glitched/Scenes/Boot.unity` and press **Play**. Any level scene can also be played directly (it makes a temporary save with every region open).

The editor never asks for copy verification.

## Builds

| Menu | Batch method | Output | Verification |
|---|---|---|---|
| Glitched → Build Windows (.exe) | `BuildTools.BuildWindowsBatch` | `Builds/Windows` | **on** |
| (batch only) | `BuildTools.BuildWindowsReleaseToBatch` | folder in `GLITCHED_OUT` | **on** |
| Glitched → Build Windows Development | `BuildTools.BuildWindowsDevBatch` / `BuildWindowsDevToBatch` | `Builds/WindowsDev` / `GLITCHED_OUT` | off (pass `-licence` to test it) |
| Glitched → Build Android (.apk) | `BuildTools.BuildAndroidBatch` | `Builds/Android` | on |

**Judges get the release build.** Development builds show a "Development Build" label and are slower.

Batch example (Git Bash, Unity Hub running):

```bash
GLITCHED_OUT="Builds/WindowsRelease" "/c/Program Files/Unity/Hub/Editor/6000.0.85f1/Editor/Unity.exe" \
  -batchmode -quit -projectPath . -executeMethod Glitched.EditorTools.BuildTools.BuildWindowsReleaseToBatch -logFile build.log
```

Building into a separate folder (`GLITCHED_OUT`) leaves a copy that's running untouched. Copy it over `Builds/WindowsJudges` afterwards.

Every build also:
- copies the voice lines recorded on this PC (Voice Studio) into `StreamingAssets/Voice`, so they ship with the game;
- allows plain `http://` for the game's connection to the stall server;
- warns if the licence key is missing (see below).

### The licence key (release builds)

Release builds only trust the stall server whose key they were built with. The public key lives at `Assets/_Glitched/Resources/Licence/licence_key.json`. It's made from the server's key pair:

```bat
Server\tools\export-game-key.cmd
```

Run it once on the stall laptop (it creates the key pair on first use), then build. If the server's key ever changes, export it again and rebuild the game and the installer. **Back up `Server/data/keys`.** See [SERVER.md](SERVER.md#back-up-the-key).

## The installer and the portable ZIP

```bat
"%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" Tools\GLITCHED_Setup.iss
```

- Packs `Builds/WindowsJudges` into `Builds/Installer/GLITCHED_Setup.exe` (≈ 390 MB), leaving out Burst debug files.
- It installs for the current user without admin rights (all users is offered), adds Start-menu and desktop shortcuts and an uninstaller, and offers to start the game.
- The portable ZIP is the same folder, zipped: `Builds/Installer/GLITCHED_Portable.zip` (7-Zip: `7z a -tzip -mx=7 ... -xr!*_DoNotShip`).
- The stall server serves both files (paths in `Server/config.json`).

The installer is not code-signed, so Windows SmartScreen shows "Windows protected your PC" → **More info → Run anyway**. Signing it needs a paid code-signing certificate.

## Testing

| What | How |
|---|---|
| Scripts compile | `bash Tools/compile_check.sh` (Roslyn, outside Unity) |
| Screenshots of any scene | `GLITCHED.exe -capture "Scene[,Scene@x]" -captureDir D -captureDelay s -captureShots n -captureTier High [-captureBot 1] [-captureTools 1]` |
| A bot plays the level | `-captureBot 1` (logs `[Bot] x=…`; answers quizzes) |
| Copy verification in a dev build | add `-licence` |
| The server | `cd Server && npm test` (the whole copy lifecycle against a throwaway database) |

Capture runs still need a verified copy in release builds.

## Regenerating content (optional)

| Tool | Makes |
|---|---|
| `Tools/build_questions.py`, `build_class11.py` | the question bank and Class XI lessons (StreamingAssets/Questions) |
| `Tools/make_sfx.py`, `make_music.py` | synthesised sound effects and the score |
| `Tools/make_faces.py`, `make_portraits.py`, `make_holograms.py` | face decals, dialogue portraits, hologram textures |
| `Tools/blender_glb_to_fbx.py` | Sketchfab boss GLB → FBX (clip renames, texture repack) |
| `Tools/import_assets.py` | sorts downloaded asset packs from `_Incoming/` into `Assets/` |
