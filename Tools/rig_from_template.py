# Rigs a scanned person with a copy of a Mixamo skeleton taken from another, already-rigged character: each joint
# is moved onto the target body (from marker positions measured on its front view, then centred inside the limb
# using the mesh itself) and the mesh is bound with automatic (heat) weights. Same bone names as Mixamo, so every
# Mixamo animation and the game's humanoid controller work on it.
#   blender -b --factory-startup --python Tools/rig_from_template.py -- <template_rigged.fbx> <target.fbx> <markers.json> <out.fbx>
# markers.json: {"height": m, "px_top": y, "px_bottom": y, "px_center": x,
#                "chin": [x, y], "wrist_r": [x, y], "wrist_l": .., "elbow_r": .., "elbow_l": .., "knee_r": .., "knee_l": .., "groin": [x, y]}
# (pixel positions on a front view; viewer-left is the character's right)
import bpy, sys, json, os, mathutils
from mathutils import Vector
a = sys.argv[sys.argv.index("--") + 1:]
template, target, markers, out = a[0], a[1], a[2], a[3]
M = json.load(open(markers))
bpy.ops.wm.read_factory_settings(use_empty=True)

def import_fbx(p):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=p)
    return [o for o in bpy.data.objects if o not in before]

def apply_all(objs):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

tmpl = import_fbx(template)
arm = next(o for o in tmpl if o.type == "ARMATURE")
for o in tmpl:
    if o.type == "MESH": bpy.data.objects.remove(o, do_unlink=True)
apply_all([arm])
body_objs = import_fbx(target)
body = next(o for o in body_objs if o.type == "MESH")
body.parent = None
apply_all([body])
verts = [body.matrix_world @ v.co for v in body.data.vertices]
zmin = min(v.z for v in verts); zmax = max(v.z for v in verts)
print("[RIG] body", len(verts), "verts, z", round(zmin, 3), round(zmax, 3))

s = M["height"] / (M["px_bottom"] - M["px_top"])
def P(key):   # marker pixel -> world (x, z); y found from the mesh
    px, py = M[key]
    return Vector(((px - M["px_center"]) * s, 0.0, zmin + (M["px_bottom"] - py) * s))

def centre(p, r=0.06, rz=None):
    """Centre of the body cross-section around p (so the joint sits inside the limb)."""
    rz = rz or r
    near = [v for v in verts if abs(v.x - p.x) < r and abs(v.z - p.z) < rz]
    if len(near) < 8: return Vector((p.x, sum(v.y for v in verts) / len(verts), p.z))
    c = sum(near, Vector()) / len(near)
    return Vector((c.x, c.y, p.z))

J = {}
J["RightHand"] = centre(P("wrist_r"), 0.05); J["LeftHand"] = centre(P("wrist_l"), 0.05)
J["RightForeArm"] = centre(P("elbow_r"), 0.06); J["LeftForeArm"] = centre(P("elbow_l"), 0.06)
J["RightLeg"] = centre(P("knee_r"), 0.07); J["LeftLeg"] = centre(P("knee_l"), 0.07)
groin = centre(P("groin"), 0.08)
chin = P("chin")
J["Hips"] = Vector((groin.x, groin.y, groin.z + 0.07))
J["RightUpLeg"] = centre(Vector((groin.x - 0.085, 0, groin.z + 0.02)), 0.06)
J["LeftUpLeg"] = centre(Vector((groin.x + 0.085, 0, groin.z + 0.02)), 0.06)
for side, knee in (("Right", J["RightLeg"]), ("Left", J["LeftLeg"])):
    low = [v for v in verts if v.z < zmin + 0.06 and (v.x < groin.x if side == "Right" else v.x > groin.x)]
    ank = sum(low, Vector()) / len(low) if low else Vector((knee.x, knee.y, zmin))
    J[side + "Foot"] = Vector((ank.x, ank.y, zmin + 0.05))
    J[side + "ToeBase"] = J[side + "Foot"] + Vector((0, -0.11, -0.04))
    J[side + "Toe_End"] = J[side + "ToeBase"] + Vector((0, -0.06, 0))
neck_z = chin.z - 0.09
J["Neck"] = centre(Vector((chin.x * 0.4 + groin.x * 0.6, 0, neck_z)), 0.07)
J["Head"] = centre(Vector((chin.x * 0.7 + groin.x * 0.3, 0, chin.z + 0.03)), 0.07)
J["HeadTop_End"] = Vector((J["Head"].x, J["Head"].y, zmax))
sh_z = chin.z - 0.12
for side, sg in (("Right", -1), ("Left", 1)):
    J[side + "Arm"] = centre(Vector((groin.x + sg * 0.17, 0, sh_z)), 0.06)
    J[side + "Shoulder"] = Vector((J["Neck"].x + sg * 0.05, J["Neck"].y, sh_z + 0.03))
    hand = J[side + "Hand"]; d = (hand - J[side + "ForeArm"]).normalized()
    for i, dist in enumerate((0.08, 0.11, 0.135, 0.155), 1): J[f"{side}HandIndex{i}"] = hand + d * dist
for i, f in enumerate((0.2, 0.45, 0.7), 0):
    J[["Spine", "Spine1", "Spine2"][i]] = J["Hips"].lerp(J["Neck"], f)

bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="EDIT")
eb = arm.data.edit_bones
for b in eb: b.use_connect = False
missing = []
for b in eb:
    key = b.name.replace("mixamorig:", "")
    if key in J: b.head = J[key]
    else: missing.append(key)
for b in eb:
    kids = [c for c in b.children if c.name.replace("mixamorig:", "") in J]
    if kids: b.tail = kids[0].head if (kids[0].head - b.head).length > 0.01 else b.head + Vector((0, 0, 0.05))
    elif b.parent is not None:
        d = (b.head - b.parent.head); b.tail = b.head + (d.normalized() if d.length > 1e-4 else Vector((0, 0, 1))) * 0.05
for b in eb: b.roll = 0.0
bpy.ops.armature.select_all(action="SELECT")
bpy.ops.armature.calculate_roll(type="GLOBAL_POS_Y")
bpy.ops.object.mode_set(mode="OBJECT")
print("[RIG] joints set; template bones without a joint:", missing)
for k in ("Hips", "Neck", "Head", "LeftArm", "LeftForeArm", "LeftHand", "LeftLeg", "LeftFoot"):
    print("[RIG]  ", k, tuple(round(x, 3) for x in J[k]))

# bind
bpy.ops.object.select_all(action="DESELECT")
body.select_set(True); arm.select_set(True); bpy.context.view_layer.objects.active = arm
bpy.ops.object.parent_set(type="ARMATURE_AUTO")
groups = [g.name for g in body.vertex_groups]
unweighted = sum(1 for v in body.data.vertices if not any(g.weight > 0.01 for g in v.groups))
print("[RIG] weights:", len(groups), "groups, unweighted verts", unweighted, "of", len(body.data.vertices))
arm.name = "Armature"
bpy.ops.object.select_all(action="SELECT")
bpy.ops.export_scene.fbx(filepath=out, use_selection=True, object_types={"ARMATURE", "MESH"}, add_leaf_bones=False, bake_anim=False,
                         path_mode="COPY", embed_textures=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
                         axis_forward="-Z", axis_up="Y", primary_bone_axis="Y", secondary_bone_axis="X")
print("[RIG] wrote", out, os.path.getsize(out) // 1024, "KB")
