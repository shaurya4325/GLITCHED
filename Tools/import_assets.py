# Copies the downloaded asset packs from _Incoming/ into the Unity project (Assets/_Glitched/Art, Audio),
# sorted the way the asset prompt describes. Only the Unity-ready variants are copied.
# Run again any time you add packs to _Incoming/.
import os, shutil, glob

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
INC = os.path.join(ROOT, "_Incoming")
Q = os.path.join(INC, "quaternius", "x")
K = os.path.join(INC, "kenney")
ART = os.path.join(ROOT, "Assets", "_Glitched", "Art")
AUDIO = os.path.join(ROOT, "Assets", "_Glitched", "Audio")
count = 0


def copy(src, dst_dir, rename=None):
    global count
    if not os.path.exists(src):
        print("  missing:", src); return
    os.makedirs(dst_dir, exist_ok=True)
    dst = os.path.join(dst_dir, rename or os.path.basename(src))
    if not os.path.exists(dst) or os.path.getsize(dst) != os.path.getsize(src):
        shutil.copy2(src, dst); count += 1


def copy_glob(pattern, dst_dir, recursive=False):
    for f in glob.glob(pattern, recursive=recursive):
        if os.path.isfile(f): copy(f, dst_dir)


def copy_tree(src_dir, dst_dir, exts):
    for dp, dn, fn in os.walk(src_dir):
        for f in fn:
            if os.path.splitext(f)[1].lower() in exts:
                rel = os.path.relpath(dp, src_dir)
                copy(os.path.join(dp, f), os.path.join(dst_dir, rel))


print("Quaternius environments")
copy_glob(os.path.join(Q, "DowntownCityMegaKit", "Exports", "FBX (Unity)", "*.fbx"), os.path.join(ART, "Environments", "CodeCity", "Downtown", "FBX"))
copy_glob(os.path.join(Q, "DowntownCityMegaKit", "Textures", "*.png"), os.path.join(ART, "Environments", "CodeCity", "Downtown", "Textures"))
copy_glob(os.path.join(Q, "StylizedNatureMegaKit", "FBX (Unity)", "*.fbx"), os.path.join(ART, "Environments", "PhysicsValley", "Nature", "FBX"))
copy_glob(os.path.join(Q, "StylizedNatureMegaKit", "Textures", "*.png"), os.path.join(ART, "Environments", "PhysicsValley", "Nature", "Textures"))
sci = glob.glob(os.path.join(Q, "ModularSciFiMegaKit", "*"))
for base in sci:
    if os.path.isdir(os.path.join(base, "FBX (Unity)")):
        copy_tree(os.path.join(base, "FBX (Unity)"), os.path.join(ART, "Environments", "ChemicalChaos", "SciFiMegaKit", "FBX"), {".fbx"})
    for t in glob.glob(os.path.join(base, "**", "*.png"), recursive=True):
        if "Unreal" not in t: copy(t, os.path.join(ART, "Environments", "ChemicalChaos", "SciFiMegaKit", "Textures"))
copy_glob(os.path.join(Q, "Sci-FiEssentialsKit", "FBX (Unity)", "*.fbx"), os.path.join(ART, "Environments", "Shared", "SciFiEssentials", "FBX"))
copy_glob(os.path.join(Q, "Sci-FiEssentialsKit", "Textures", "*.png"), os.path.join(ART, "Environments", "Shared", "SciFiEssentials", "Textures"))
copy_glob(os.path.join(Q, "FantasyPropsMegaKit", "Exports", "FBX", "*.fbx"), os.path.join(ART, "Environments", "Shared", "FantasyProps", "FBX"))
for t in glob.glob(os.path.join(Q, "FantasyPropsMegaKit", "Textures", "*.png")):
    copy(t, os.path.join(ART, "Environments", "Shared", "FantasyProps", "Textures"))
plat = glob.glob(os.path.join(Q, "UltimatePlatformer", "*"))
for base in plat:
    for sub in ["Cubes", "Items", "Enemies", "Environment", "Character"]:
        d = os.path.join(base, sub, "FBX")
        if os.path.isdir(d): copy_glob(os.path.join(d, "*.fbx"), os.path.join(ART, "Environments", "Shared", "Platformer", "FBX", sub))
kk = os.path.join(Q, "KayKitPlatformer", "KayKit_Platformer_Pack_1.0_FREE")
for color in ["neutral", "blue"]:
    copy_glob(os.path.join(kk, "Assets", "fbx(unity)", color, "*.fbx"), os.path.join(ART, "Environments", "Shared", "KayKit", "FBX", color))
copy_glob(os.path.join(kk, "Textures", "*.png"), os.path.join(ART, "Environments", "Shared", "KayKit", "Textures"))
copy_glob(os.path.join(kk, "Assets", "fbx(unity)", "*.png"), os.path.join(ART, "Environments", "Shared", "KayKit", "Textures"))
copy_glob(os.path.join(K, "kenney_space-kit", "Models", "FBX format", "*.fbx"), os.path.join(ART, "Environments", "FinalVoid", "SpaceKit", "FBX"))
copy_glob(os.path.join(K, "kenney_space-kit", "Models", "FBX format", "*.png"), os.path.join(ART, "Environments", "FinalVoid", "SpaceKit", "FBX"))
copy_glob(os.path.join(K, "kenney_space-kit", "Models", "FBX format", "Textures", "*.png"), os.path.join(ART, "Environments", "FinalVoid", "SpaceKit", "Textures"))

print("Characters and animation")
copy(os.path.join(Q, "AnimatedRobot", "FBX", "Robot.fbx"), os.path.join(ART, "Characters", "Minions", "Robot"))
copy_glob(os.path.join(Q, "MonsterPackAnimated", "FBX", "*.fbx"), os.path.join(ART, "Characters", "Minions", "Monsters"))
copy_glob(os.path.join(Q, "MonsterPackAnimated", "**", "*.jpg"), os.path.join(ART, "Characters", "Minions", "Monsters"), True)
ub = os.path.join(Q, "UniversalBaseCharacters", "Universal Base Characters[Standard]")
copy_glob(os.path.join(ub, "Base Characters", "Unity", "*.fbx"), os.path.join(ART, "Characters", "NPCs", "UniversalBase"))
for t in glob.glob(os.path.join(ub, "Base Characters", "Textures", "*.png")):
    copy(t, os.path.join(ART, "Characters", "NPCs", "UniversalBase", "Textures"))
for t in glob.glob(os.path.join(ub, "Base Characters", "Textures", "Normals Unity - Godot", "*.png")):
    copy(t, os.path.join(ART, "Characters", "NPCs", "UniversalBase", "Textures", "Normals"))
copy_glob(os.path.join(Q, "UAL2", "Universal Animation Library 2[Standard]", "Unity", "UAL2_Standard.fbx"), os.path.join(ART, "Animations", "UAL2"))

print("Skies, PBR, VFX")
copy_glob(os.path.join(INC, "hdri", "*.hdr"), os.path.join(ART, "Textures", "Skies"))
import zipfile
for z in glob.glob(os.path.join(INC, "ambientcg", "*.zip")):
    name = os.path.splitext(os.path.basename(z))[0]
    dst = os.path.join(ART, "Textures", "PBR", name)
    os.makedirs(dst, exist_ok=True)
    with zipfile.ZipFile(z) as zf:
        for m in zf.namelist():
            low = m.lower()
            if low.endswith(".jpg") and ("_color" in low or "_normalgl" in low):
                target = os.path.join(dst, os.path.basename(m))
                if not os.path.exists(target):
                    with zf.open(m) as s, open(target, "wb") as d: shutil.copyfileobj(s, d); count += 1
pp = os.path.join(K, "kenney_particle-pack", "PNG (Transparent)")
for n in ["circle_05", "flare_01", "light_01", "magic_02", "magic_04", "smoke_04", "smoke_07", "spark_01", "spark_04", "spark_06", "star_04", "star_07", "fire_01", "flame_03", "scorch_02", "trace_04", "twirl_02", "window_01", "symbol_01", "slash_02"]:
    copy(os.path.join(pp, n + ".png"), os.path.join(ART, "Textures", "VFX"))

print("Audio")
for pack in ["kenney_interface-sounds", "kenney_impact-sounds", "kenney_sci-fi-sounds", "kenney_digital-audio", "kenney_ui-audio"]:
    copy_glob(os.path.join(K, pack, "Audio", "*.ogg"), os.path.join(AUDIO, "SFX", pack.replace("kenney_", "")))
for sub in ["8-Bit jingles", "Hit jingles"]:
    copy_glob(os.path.join(K, "kenney_music-jingles", "Audio", sub, "*.ogg"), os.path.join(AUDIO, "SFX", "jingles"))

print("Fonts")
copy_glob(os.path.join(INC, "fonts", "*.*"), os.path.join(ART, "Fonts"))
print("copied", count, "files")
