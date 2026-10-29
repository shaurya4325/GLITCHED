# Downloads the realistic PBR texture sets (ambientCG, CC0) used by the GLITCHED city and characters,
# and extracts Color / NormalGL / Roughness / AmbientOcclusion / Metalness into Assets/_Glitched/Art/Textures/PBR/<id>/.
# Also re-extracts the roughness and AO maps from the 1K sets that were imported earlier with colour+normal only.
#   python -I Tools/fetch_pbr.py
import os, sys, zipfile, urllib.request, glob

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
INC = os.path.join(ROOT, "_Incoming", "ambientcg2k")
OUT = os.path.join(ROOT, "Assets", "_Glitched", "Art", "Textures", "PBR")
KEEP = ("_color", "_normalgl", "_roughness", "_ambientocclusion", "_metalness")

SETS_2K = [
    "Concrete046", "Concrete042A", "Plaster001", "PaintedPlaster017", "Bricks097", "Bricks060",
    "PaintedMetal004", "Metal009", "Metal027", "Bark014", "Fabric086", "Fabric081C", "Rubber004",
    "Granite002A", "Ground109", "Gravel042", "Road007", "Asphalt031", "Leather008", "TactilePaving002",
    "PavingStones138", "Tiles074", "Wood066", "Plastic006",
]


def extract(zpath, set_id):
    dest = os.path.join(OUT, set_id)
    os.makedirs(dest, exist_ok=True)
    n = 0
    with zipfile.ZipFile(zpath) as z:
        for name in z.namelist():
            low = name.lower()
            if low.endswith(".jpg") and any(k in low for k in KEEP):
                target = os.path.join(dest, os.path.basename(name))
                if not os.path.exists(target):
                    with z.open(name) as src, open(target, "wb") as dst:
                        dst.write(src.read())
                    n += 1
    return n


def main():
    os.makedirs(INC, exist_ok=True)
    for sid in SETS_2K:
        zpath = os.path.join(INC, f"{sid}_2K-JPG.zip")
        if not os.path.exists(zpath):
            url = f"https://ambientcg.com/get?file={sid}_2K-JPG.zip"
            try:
                req = urllib.request.Request(url, headers={"User-Agent": "GLITCHED-asset-fetch"})
                with urllib.request.urlopen(req, timeout=120) as r, open(zpath, "wb") as f:
                    f.write(r.read())
            except Exception as e:
                print("FAILED", sid, e)
                if os.path.exists(zpath):
                    os.remove(zpath)
                continue
        try:
            print(sid, "extracted", extract(zpath, sid))
        except zipfile.BadZipFile:
            print("BAD ZIP", sid)
            os.remove(zpath)
    # roughness / AO for the earlier 1K sets
    for zpath in glob.glob(os.path.join(ROOT, "_Incoming", "ambientcg", "*.zip")):
        sid = os.path.splitext(os.path.basename(zpath))[0]
        if os.path.isdir(os.path.join(OUT, sid)):
            print(sid, "(1K) extracted", extract(zpath, sid))


if __name__ == "__main__":
    main()
