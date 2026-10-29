# Turns a scanned person into a clean, watertight, game-ready mesh: voxel remesh (closes holes, removes inner faces),
# decimate, fresh UVs, then bakes the scan's colours onto it with Cycles on the GPU (RTX: OptiX, else CUDA).
#   blender -b --factory-startup --python Tools/remesh_bake.py -- <in.blend|obj|glb> <out.fbx> <height m> [voxel m] [faces] [tex px]
import bpy, sys, os, mathutils
a = sys.argv[sys.argv.index("--") + 1:]
src, out, height = a[0], a[1], float(a[2])
voxel = float(a[3]) if len(a) > 3 else 0.007
faces = int(a[4]) if len(a) > 4 else 45000
tex = int(a[5]) if len(a) > 5 else 2048
bpy.ops.wm.read_factory_settings(use_empty=True)
ext = os.path.splitext(src)[1].lower()
if ext == ".blend": bpy.ops.wm.open_mainfile(filepath=src)
elif ext == ".obj": bpy.ops.wm.obj_import(filepath=src)
else: bpy.ops.import_scene.gltf(filepath=src)
srcobj = [o for o in bpy.context.scene.objects if o.type == "MESH"][0]
bpy.ops.object.select_all(action="DESELECT"); srcobj.select_set(True); bpy.context.view_layer.objects.active = srcobj
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
vs = [v.co for v in srcobj.data.vertices]
mn = mathutils.Vector([min(v[i] for v in vs) for i in range(3)]); mx = mathutils.Vector([max(v[i] for v in vs) for i in range(3)])
k = height / (mx.z - mn.z); off = mathutils.Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, mn.z))
for v in srcobj.data.vertices: v.co = (v.co - off) * k
srcobj.data.update()
# --- the clean copy: first close the scan (fill holes, fuse duplicate points), so it has an inside to remesh
bpy.ops.object.duplicate(); low = bpy.context.view_layer.objects.active; low.name = os.path.splitext(os.path.basename(out))[0]
low.data.materials.clear()
import bmesh
bm = bmesh.new(); bm.from_mesh(low.data)
bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=voxel * 0.25)
edges = [e for e in bm.edges if e.is_boundary]
print("[RB] open edges", len(edges))
bmesh.ops.holes_fill(bm, edges=edges, sides=0)
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
print("[RB] open edges after fill", sum(1 for e in bm.edges if e.is_boundary), "non-manifold", sum(1 for e in bm.edges if not e.is_manifold))
bm.to_mesh(low.data); bm.free()
m = low.modifiers.new("Remesh", "REMESH"); m.mode = "VOXEL"; m.voxel_size = voxel; m.use_smooth_shade = True
bpy.ops.object.modifier_apply(modifier=m.name)
# keep the biggest piece only (voxel remesh can leave crumbs)
bm = bmesh.new(); bm.from_mesh(low.data); bm.verts.ensure_lookup_table()
seen, best = set(), []
for v in bm.verts:
    if v in seen: continue
    comp, st = [], [v]; seen.add(v)
    while st:
        x = st.pop(); comp.append(x)
        for e in x.link_edges:
            y = e.other_vert(x)
            if y not in seen: seen.add(y); st.append(y)
    if len(comp) > len(best): best = comp
print("[RB] pieces: biggest", len(best), "of", len(bm.verts), "verts")
keep = set(best); bmesh.ops.delete(bm, geom=[v for v in bm.verts if v not in keep], context="VERTS")
bm.to_mesh(low.data); bm.free()
n = len(low.data.polygons)
if n > faces:
    d = low.modifiers.new("Decimate", "DECIMATE"); d.ratio = faces / n
    bpy.ops.object.modifier_apply(modifier=d.name)
print("[RB] remeshed", n, "->", len(low.data.polygons), "faces")
# --- UVs + bake target
bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.uv.smart_project(angle_limit=1.15, island_margin=0.003)
bpy.ops.object.mode_set(mode="OBJECT")
img = bpy.data.images.new(low.name + "_albedo", tex, tex)
mat = bpy.data.materials.new(low.name + "_mat"); mat.use_nodes = True
nodes = mat.node_tree.nodes
it = nodes.new("ShaderNodeTexImage"); it.image = img
mat.node_tree.links.new(it.outputs["Color"], nodes["Principled BSDF"].inputs["Base Color"])
nodes.active = it
low.data.materials.append(mat)
# --- Cycles on the GPU
sc = bpy.context.scene; sc.render.engine = "CYCLES"
prefs = bpy.context.preferences.addons["cycles"].preferences
for backend in ("OPTIX", "CUDA"):
    try:
        prefs.compute_device_type = backend; prefs.get_devices()
        gpus = [d for d in prefs.devices if d.type == backend]
        if gpus:
            for d in prefs.devices: d.use = d.type == backend
            sc.cycles.device = "GPU"; print("[RB] baking on", backend, [d.name for d in gpus]); break
    except Exception as e: print("[RB]", backend, e)
sc.cycles.samples = 16
sc.render.bake.use_selected_to_active = True
sc.render.bake.cage_extrusion = voxel * 3
sc.render.bake.max_ray_distance = voxel * 8
sc.render.bake.use_pass_direct = False; sc.render.bake.use_pass_indirect = False; sc.render.bake.use_pass_color = True
sc.render.bake.margin = 8
bpy.ops.object.select_all(action="DESELECT"); srcobj.select_set(True); low.select_set(True); bpy.context.view_layer.objects.active = low
bpy.ops.object.bake(type="DIFFUSE", pass_filter={"COLOR"})
png = os.path.splitext(out)[0] + "_albedo.png"
img.filepath_raw = png; img.file_format = "PNG"; img.save()
print("[RB] baked", png)
# --- export only the clean mesh
bpy.data.objects.remove(srcobj, do_unlink=True)
for p in low.data.polygons: p.use_smooth = True
bpy.ops.export_scene.fbx(filepath=out, use_selection=False, object_types={"MESH"}, path_mode="COPY", embed_textures=True,
                         apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y")
print("[RB] wrote", out, os.path.getsize(out) // 1024, "KB")
