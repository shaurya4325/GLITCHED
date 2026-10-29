# Renders a dialogue portrait (head and shoulders) of a scanned character with Cycles on the GPU.
#   blender -b --factory-startup --python Tools/render_portrait.py -- <model.fbx|glb|obj> <albedo image or -> <out.png> [size] [yaw deg]
import bpy, sys, math, mathutils
a = sys.argv[sys.argv.index("--") + 1:]
src, albedo, out = a[0], a[1], a[2]
size = int(a[3]) if len(a) > 3 else 512
yaw = float(a[4]) if len(a) > 4 else 18
bpy.ops.wm.read_factory_settings(use_empty=True)
if src.lower().endswith(".fbx"): bpy.ops.import_scene.fbx(filepath=src)
elif src.lower().endswith(".obj"): bpy.ops.wm.obj_import(filepath=src)
else: bpy.ops.import_scene.gltf(filepath=src)
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
if albedo != "-":
    img = bpy.data.images.load(albedo)
    mat = bpy.data.materials.new("Portrait"); mat.use_nodes = True
    n = mat.node_tree.nodes; t = n.new("ShaderNodeTexImage"); t.image = img
    bsdf = n["Principled BSDF"]; mat.node_tree.links.new(t.outputs["Color"], bsdf.inputs["Base Color"]); bsdf.inputs["Roughness"].default_value = 0.75
    for o in meshes:
        o.data.materials.clear(); o.data.materials.append(mat)
pts = [o.matrix_world @ v.co for o in meshes for v in o.data.vertices]
top = max(p.z for p in pts)
head = [p for p in pts if p.z > top - 0.3]
c = sum(head, mathutils.Vector()) / len(head)
face = mathutils.Vector((c.x, min(p.y for p in head) + 0.09, top - 0.17))
sc = bpy.context.scene
sc.render.engine = "CYCLES"
prefs = bpy.context.preferences.addons["cycles"].preferences
for backend in ("OPTIX", "CUDA"):
    try:
        prefs.compute_device_type = backend; prefs.get_devices()
        if any(d.type == backend for d in prefs.devices):
            for d in prefs.devices: d.use = d.type == backend
            sc.cycles.device = "GPU"; break
    except Exception: pass
sc.cycles.samples = 96; sc.cycles.use_denoising = True
sc.render.resolution_x = sc.render.resolution_y = size
sc.render.film_transparent = True
sc.view_settings.view_transform = "AgX"
w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True; w.node_tree.nodes["Background"].inputs[0].default_value = (0.35, 0.37, 0.42, 1); w.node_tree.nodes["Background"].inputs[1].default_value = 0.6
for name, loc, energy, col in (("key", (0.9, -1.4, 0.5), 120, (1, 0.93, 0.85)), ("rim", (-0.8, 1.0, 0.6), 90, (0.55, 0.75, 1))):
    l = bpy.data.lights.new(name, "AREA"); l.energy = energy; l.size = 0.9; l.color = col
    lo = bpy.data.objects.new(name, l); sc.collection.objects.link(lo); lo.location = face + mathutils.Vector(loc)
    lo.rotation_euler = (face - lo.location).to_track_quat("-Z", "Y").to_euler()
cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); sc.collection.objects.link(cam); sc.camera = cam
cam.data.lens = 85
r = math.radians(yaw)
cam.location = face + mathutils.Vector((math.sin(r) * 1.25, -math.cos(r) * 1.25, 0.02))
cam.rotation_euler = (face + mathutils.Vector((0, 0, -0.04)) - cam.location).to_track_quat("-Z", "Y").to_euler()
sc.render.filepath = out
bpy.ops.render.render(write_still=True)
print("[PORTRAIT] wrote", out)
