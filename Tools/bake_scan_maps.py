# Bakes the extra surface maps a 3D-scanned character needs to sit in a lit game world, with Cycles on the GPU:
#   <name>_ao.png      ambient occlusion in the scan's own UV atlas (armpits, collar, folds, between the legs)
#   <name>_normal.png  fine detail (fabric weave, hair strands, wrinkles) derived from the albedo, tangent space
#   blender -b --factory-startup --python Tools/bake_scan_maps.py -- <model.fbx> <albedo> <out dir> <name> [size]
import bpy, sys, os
import numpy as np

a = sys.argv[sys.argv.index("--") + 1:]
src, albedo_path, out_dir, name = a[0], a[1], a[2], a[3]
size = int(a[4]) if len(a) > 4 else 2048

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
for o in bpy.context.scene.objects:   # bake the bind pose: the armature must not deform anything
    for m in getattr(o, "modifiers", []):
        if m.type == "ARMATURE": m.show_viewport = m.show_render = False

sc = bpy.context.scene
sc.render.engine = "CYCLES"
prefs = bpy.context.preferences.addons["cycles"].preferences
for backend in ("OPTIX", "CUDA"):
    try:
        prefs.compute_device_type = backend; prefs.get_devices()
        if any(d.type == backend for d in prefs.devices):
            for d in prefs.devices: d.use = d.type == backend
            sc.cycles.device = "GPU"; print("[bake] device", backend); break
    except Exception as e: print("[bake]", backend, e)
sc.cycles.samples = 256
sc.cycles.use_denoising = False

# ---------------- ambient occlusion
ao = bpy.data.images.new(name + "_ao", size, size, alpha=False, float_buffer=False)
for o in meshes:
    mat = bpy.data.materials.new("Bake_" + o.name); mat.use_nodes = True
    t = mat.node_tree.nodes.new("ShaderNodeTexImage"); t.image = ao
    mat.node_tree.nodes.active = t
    o.data.materials.clear(); o.data.materials.append(mat)
bpy.ops.object.select_all(action="DESELECT")
for o in meshes: o.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
sc.render.bake.margin = 12
sc.world = bpy.data.worlds.new("W")
sc.world.light_settings.distance = 0.35   # occlusion reach in metres: body-scale folds, not the whole room
bpy.ops.object.bake(type="AO")
ao_px = np.array(ao.pixels[:], dtype=np.float32).reshape(size, size, 4)[:, :, 0]
# keep it gentle: scans already carry some shading in the albedo
ao_px = np.clip(0.25 + 0.75 * ao_px, 0, 1)
ao.pixels = np.dstack([ao_px, ao_px, ao_px, np.ones_like(ao_px)]).ravel()
ao.filepath_raw = os.path.join(out_dir, name + "_ao.png"); ao.file_format = "PNG"; ao.save()
print("[bake] AO saved", ao.filepath_raw)

# ---------------- detail normal map from the albedo (high-passed luminance as height)
img = bpy.data.images.load(albedo_path)
w, h = img.size
px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, img.channels)[:, :, :3]
lum = px @ np.array([0.299, 0.587, 0.114], dtype=np.float32)

def blur(x, r):
    # separable box blur, three passes ~ gaussian
    for _ in range(3):
        k = 2 * r + 1
        c = np.cumsum(np.pad(x, ((0, 0), (r + 1, r)), mode="edge"), axis=1)
        x = (c[:, k:] - c[:, :-k]) / k
        c = np.cumsum(np.pad(x, ((r + 1, r), (0, 0)), mode="edge"), axis=0)
        x = (c[k:, :] - c[:-k, :]) / k
    return x

scale = w / 2048.0
height = lum - blur(lum, max(2, int(10 * scale)))     # only the fine detail: big shapes are in the geometry
height = blur(height, 1)
gx = (np.roll(height, -1, 1) - np.roll(height, 1, 1)) * 0.5
gy = (np.roll(height, -1, 0) - np.roll(height, 1, 0)) * 0.5
strength = 3.0 * scale
nx, ny, nz = -gx * strength, -gy * strength, np.ones_like(gx)
ln = np.sqrt(nx * nx + ny * ny + nz * nz)
nrm = np.dstack([nx / ln * 0.5 + 0.5, ny / ln * 0.5 + 0.5, nz / ln * 0.5 + 0.5, np.ones_like(nx)])
nimg = bpy.data.images.new(name + "_normal", w, h, alpha=False)
nimg.colorspace_settings.name = "Non-Color"
nimg.pixels = nrm.astype(np.float32).ravel()
nimg.filepath_raw = os.path.join(out_dir, name + "_normal.png"); nimg.file_format = "PNG"; nimg.save()
print("[bake] normal saved", nimg.filepath_raw)
