# Prepares a scanned person for Mixamo's auto-rigger: Z up, facing -Y, feet on the floor, centred, scaled to a
# real height, optional decimation, exported as FBX with the texture embedded.
#   blender -b --factory-startup --python Tools/prep_for_mixamo.py -- <in.glb|obj|blend> <out.fbx> <height m> [max faces]
import bpy, sys, os, mathutils
a = sys.argv[sys.argv.index("--") + 1:]
src, out, height = a[0], a[1], float(a[2])
max_faces = int(a[3]) if len(a) > 3 else 90000
bpy.ops.wm.read_factory_settings(use_empty=True)
ext = os.path.splitext(src)[1].lower()
if ext == ".blend": bpy.ops.wm.open_mainfile(filepath=src)
elif ext == ".obj": bpy.ops.wm.obj_import(filepath=src)
else: bpy.ops.import_scene.gltf(filepath=src)
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
bpy.ops.object.select_all(action="DESELECT")
for o in meshes: o.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
if len(meshes) > 1: bpy.ops.object.join()
o = bpy.context.view_layer.objects.active
o.parent = None
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
o.name = os.path.splitext(os.path.basename(out))[0]
# measure, centre on x/y, feet at 0, scale to height
vs = [v.co for v in o.data.vertices]
mn = mathutils.Vector([min(v[i] for v in vs) for i in range(3)])
mx = mathutils.Vector([max(v[i] for v in vs) for i in range(3)])
k = height / (mx.z - mn.z)
off = mathutils.Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, mn.z))
for v in o.data.vertices: v.co = (v.co - off) * k
o.data.update()
faces = len(o.data.polygons)
if faces > max_faces:
    m = o.modifiers.new("Decimate", "DECIMATE"); m.ratio = max_faces / faces
    bpy.ops.object.modifier_apply(modifier=m.name)
# smooth normals look better on scans
for p in o.data.polygons: p.use_smooth = True
print("[PREP]", o.name, "scale", round(k, 4), "faces", faces, "->", len(o.data.polygons), "height", height)
bpy.ops.export_scene.fbx(filepath=out, use_selection=False, object_types={"MESH"}, path_mode="COPY", embed_textures=True,
                         apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE")
print("[PREP] wrote", out, os.path.getsize(out) // 1024, "KB")
