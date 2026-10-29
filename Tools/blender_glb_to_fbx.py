# Blender (4.5) batch script: convert a Sketchfab .glb boss into a Unity-ready FBX + texture set + manifest.
#   blender -b --factory-startup --python Tools/blender_glb_to_fbx.py -- <in.glb> <out_dir> <name> [inspect]
# Writes <out_dir>/<name>.fbx (all actions as takes), <out_dir>/Textures/*.png and <out_dir>/<name>.json:
#   { "height": metres, "actions": [...], "materials": { mat: { base, normal, mr, emission, occlusion, metallic, roughness, emissive } } }
import bpy, sys, os, json, mathutils

argv = sys.argv[sys.argv.index("--") + 1:]
src, out, name = os.path.abspath(argv[0]), os.path.abspath(argv[1]), argv[2]
inspect = len(argv) > 3 and argv[3] == "inspect"
clipmap = json.load(open(argv[3])) if len(argv) > 3 and argv[3].endswith(".json") else None   # {"idle_loop": "Source Action", ...}
os.makedirs(os.path.join(out, "Textures"), exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)

# the glTF importer adds an icosphere as the bones' display shape: not part of the model
for o in list(bpy.context.scene.objects):
    if o.type == "MESH" and (not o.visible_get() or o.name.startswith("Icosphere") or len(o.data.polygons) == 0):
        bpy.data.objects.remove(o, do_unlink=True)
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
arms = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"]

def bounds():
    dg = bpy.context.evaluated_depsgraph_get()
    lo = mathutils.Vector((1e9, 1e9, 1e9)); hi = -lo
    for o in meshes:
        e = o.evaluated_get(dg)
        for v in e.to_mesh().vertices:
            w = e.matrix_world @ v.co
            lo = mathutils.Vector(map(min, lo, w)); hi = mathutils.Vector(map(max, hi, w))
        e.to_mesh_clear()
    return lo, hi

if clipmap:
    # keep only the mapped clips, renamed to the names GlitchedBuilder.BuildController looks for
    src = {a.name: a for a in bpy.data.actions}
    keep = []
    for new, old in clipmap.items():
        a = src.get(old) or next((x for n, x in src.items() if n.endswith("|" + old) or n == old), None)
        if a is None: print("CLIPMISS", name, old); continue
        if a in keep: a = a.copy()
        a.name = new; a.use_fake_user = True; keep.append(a)
    for a in list(bpy.data.actions):
        if a not in keep: bpy.data.actions.remove(a)
def flatten():
    """Sketchfab files nest the rig under empties scaled 0.01 / 100 / 39...: Unity's FBX import mixes those up with
    the animation and the model changes size as it plays. Bake every rig into plain world space: armature and its
    skinned meshes with identity transforms, bone location keys rescaled to match, the empties removed."""
    for arm in arms:
        kids = [o for o in bpy.context.scene.objects if o.type == "MESH" and (o.parent == arm or any(m.type == "ARMATURE" and m.object == arm for m in o.modifiers))]
        if any(o.parent == arm and o.parent_type == "BONE" for o in bpy.context.scene.objects):
            print("FLATTEN skip (bone-parented parts)", arm.name); continue
        s = arm.matrix_world.to_scale().x
        for o in [arm] + kids:
            m = o.matrix_world.copy(); o.parent = None; o.matrix_world = m
        bpy.ops.object.select_all(action="DESELECT")
        for o in [arm] + kids: o.select_set(True)
        bpy.context.view_layer.objects.active = arm
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True, isolate_users=True)
        for o in kids:
            o.parent = arm; o.matrix_parent_inverse = arm.matrix_world.inverted()
        # bone translations are stored in armature units: scale them with the rig; object-level keys go
        for a in bpy.data.actions:
            for fc in list(getattr(a, "fcurves", [])):
                if fc.data_path.startswith("pose.bones") and fc.data_path.endswith(".location") and os.environ.get("GLB_NO_LOCSCALE") != "1":
                    for k in fc.keyframe_points:
                        k.co[1] *= s; k.handle_left[1] *= s; k.handle_right[1] *= s
                elif not fc.data_path.startswith("pose.bones"):
                    a.fcurves.remove(fc)
        print("FLATTEN", arm.name, "scale", round(s, 5), "meshes", len(kids))
    for o in list(bpy.context.scene.objects):
        if o.type == "EMPTY" and not o.children: bpy.data.objects.remove(o, do_unlink=True)

def assign(arm, a):
    if arm.animation_data is None: arm.animation_data_create()
    arm.animation_data.action = a
    slots = getattr(a, "slots", None)
    if slots and len(slots) > 0 and hasattr(arm.animation_data, "action_slot"): arm.animation_data.action_slot = slots[0]

def heights():
    """Height of the rig at a few frames of every clip."""
    out = {}
    for a in bpy.data.actions:
        for arm in arms: assign(arm, a)
        hs = []
        f0, f1 = int(a.frame_range[0]), int(a.frame_range[1])
        for f in range(f0, max(f0, f1) + 1, max(1, (f1 - f0) // 4 or 1)):
            bpy.context.scene.frame_set(f); l, h = bounds(); hs.append(round(h.z - l.z, 3))
        out[a.name] = hs
    for arm in arms:
        if arm.animation_data: arm.animation_data.action = None
    bpy.context.scene.frame_set(0)
    return out

if not inspect and os.environ.get("GLB_NO_FLATTEN") != "1":
    before = heights()
    flatten()
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    after = heights()
    ok = all(abs(x - y) <= 0.05 * max(x, 0.01) for k in before for x, y in zip(before[k], after.get(k, [])))
    print("HEIGHTS", name, "before", before, "after", after)
    if not ok:
        print("FLATTEN_BAD", name)
        sys.exit(3)   # the driver re-runs with GLB_NO_FLATTEN=1

actions = [a.name for a in bpy.data.actions]
lo, hi = bounds()
info = {"height": hi.z - lo.z, "width": hi.x - lo.x, "depth": hi.y - lo.y, "min": list(lo), "max": list(hi),
        "actions": [(a.name, round(a.frame_range[1] - a.frame_range[0])) for a in bpy.data.actions],
        "meshes": len(meshes), "armatures": len(arms),
        "tris": sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in meshes)}

def tex_of(socket, seen=None):
    """The image feeding a socket (through normal-map / separate / mix nodes)."""
    if socket is None or not socket.is_linked: return None, None
    node = socket.links[0].from_node
    chan = socket.links[0].from_socket.name
    for _ in range(6):
        if node.type == "TEX_IMAGE": return node.image, chan
        ins = [i for i in node.inputs if i.is_linked and i.type in ("RGBA", "VECTOR", "VALUE") and i.name not in ("Vector",)]
        if not ins: return None, None
        if node.type in ("SEPARATE_COLOR", "SEPRGB", "SEPARATE_RGB"): pass
        node = ins[0].links[0].from_node
    return None, None

def save(img, tag, mat):
    """Write the image for one material slot role; glTF metallic-roughness (G = rough, B = metal) is repacked
    for Glitched/Realistic (R = rough, G = metal)."""
    if img is None: return None
    fn = f"{name}_{bpy.path.clean_name(mat)}_{tag}.png"
    path = os.path.join(out, "Textures", fn)
    import numpy as np
    w, h = img.size
    px = np.empty(w * h * 4, dtype=np.float32); img.pixels.foreach_get(px)
    if tag == "mr":
        px = px.reshape(-1, 4); o = np.ones_like(px); o[:, 0] = px[:, 1]; o[:, 1] = px[:, 2]; o[:, 2] = 0; px = o.ravel()
    # a fresh, unpacked image: Image.save() on the importer's packed images only re-packs them
    dst = bpy.data.images.new(fn, w, h, alpha=tag == "base", float_buffer=False)
    dst.colorspace_settings.name = "sRGB" if tag in ("base", "emission") else "Non-Color"
    dst.pixels.foreach_set(px)
    if max(w, h) > 2048: dst.scale(w * 2048 // max(w, h), h * 2048 // max(w, h))
    dst.filepath_raw = path; dst.file_format = "PNG"; dst.save()
    return fn

mats = {}
for m in bpy.data.materials:
    if not m.use_nodes: continue
    bsdf = next((n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
    if bsdf is None: continue
    d = {}
    img, _ = tex_of(bsdf.inputs["Base Color"]); d["base"] = None if inspect else save(img, "base", m.name)
    if not bsdf.inputs["Base Color"].is_linked: d["color"] = list(bsdf.inputs["Base Color"].default_value)
    img, _ = tex_of(bsdf.inputs["Normal"]); d["normal"] = None if inspect else save(img, "normal", m.name)
    img, _ = tex_of(bsdf.inputs["Roughness"]) if bsdf.inputs["Roughness"].is_linked else tex_of(bsdf.inputs["Metallic"])
    d["mr"] = None if inspect else save(img, "mr", m.name)
    img, _ = tex_of(bsdf.inputs["Emission Color"]); d["emission"] = None if inspect else save(img, "emission", m.name)
    d["metallic"] = bsdf.inputs["Metallic"].default_value if not bsdf.inputs["Metallic"].is_linked else 1.0
    d["roughness"] = bsdf.inputs["Roughness"].default_value if not bsdf.inputs["Roughness"].is_linked else 1.0
    d["emissive"] = bsdf.inputs["Emission Strength"].default_value
    d["alpha"] = m.blend_method != "OPAQUE" or bsdf.inputs["Alpha"].is_linked
    # glTF occlusion lives in a custom group node "glTF Material Output" / "glTF Settings"
    for n in m.node_tree.nodes:
        if n.type == "GROUP" and n.node_tree and "glTF" in n.node_tree.name and "Occlusion" in n.inputs:
            img, _ = tex_of(n.inputs["Occlusion"]); d["occlusion"] = None if inspect else save(img, "ao", m.name)
    mats[m.name] = d
info["materials"] = mats

if not inspect:
    # one FBX: every action becomes a take (Unity clip); metres, Y-up handled by the exporter
    for a in arms:
        if a.animation_data is None: a.animation_data_create()
    bpy.ops.export_scene.fbx(filepath=os.path.join(out, name + ".fbx"), use_selection=False,
                             object_types={"ARMATURE", "MESH", "EMPTY"}, apply_unit_scale=True, apply_scale_options="FBX_SCALE_NONE",
                             add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
                             bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.5, path_mode="STRIP", mesh_smooth_type="FACE",
                             use_tspace=True, axis_forward="-Z", axis_up="Y")
with open(os.path.join(out, name + ".json"), "w") as f:
    json.dump(info, f, indent=1)
print("GLBINFO", name, json.dumps({k: v for k, v in info.items() if k != "materials"}), "mats", len(mats))
