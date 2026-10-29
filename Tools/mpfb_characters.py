# Generates GLITCHED's realistic people with MakeHuman (MPFB2 add-on for Blender, CC0 assets) and exports them
# as Unity-ready FBX files (mixamo_unity rig, game-engine materials, embedded textures).
#   "C:\Program Files\Blender Foundation\Blender 4.5\blender.exe" -b --factory-startup --python Tools/mpfb_characters.py -- <mpfb.zip> <assets dir> <out dir>
import bpy, sys, os, json, random, traceback

argv = sys.argv[sys.argv.index("--") + 1:]
MPFB_ZIP, ASSETS, OUT = argv[0], argv[1], argv[2]
ONLY = argv[3].split(",") if len(argv) > 3 else None   # optional: just these ids
os.makedirs(OUT, exist_ok=True)


def install_mpfb():
    mod = "bl_ext.user_default.mpfb"
    try:
        bpy.ops.preferences.addon_enable(module=mod)   # installed but not enabled under --factory-startup
        __import__(mod + ".services.humanservice")
        return mod
    except Exception:
        pass
    bpy.ops.extensions.package_install_files(repo="user_default", filepath=MPFB_ZIP, enable_on_install=True)
    bpy.ops.preferences.addon_enable(module=mod)
    __import__(mod)
    return mod


MOD = install_mpfb()
HumanService = __import__(MOD + ".services.humanservice", fromlist=["HumanService"]).HumanService
RigService = __import__(MOD + ".services.rigservice", fromlist=["RigService"]).RigService
TargetService = __import__(MOD + ".services.targetservice", fromlist=["TargetService"]).TargetService


def A(*parts):
    return os.path.join(ASSETS, *parts)


def clear_scene():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for coll in (bpy.data.meshes, bpy.data.materials, bpy.data.armatures, bpy.data.images):
        for b in list(coll):
            if b.users == 0:
                coll.remove(b)


# age: MakeHuman maps 0 -> 1 y, 0.1875 -> 11 y, 0.5 -> 25 y, 1 -> 90 y
def age_value(years):
    if years <= 11: return years / 11 * 0.1875
    if years <= 25: return 0.1875 + (years - 11) / 14 * 0.3125
    return 0.5 + (years - 25) / 65 * 0.5


CHARACTERS = [
    # Shaurya: Class 11 gamer, slim, South Asian
    dict(id="shaurya", gender=1.0, age=16, muscle=0.5, weight=0.45, height=0.55, race=(0.2, 0.55, 0.25),
         skin="young_asian_male", hair="short02", eyebrows="eyebrow002", lashes="eyelashes01", eyes="brown",
         clothes=["male_casualsuit01", "shoes02"]),
]
CITIZEN_LOOKS = [
    (1.0, 34, "middleage_asian_male", "short04", ["male_worksuit01", "shoes01"]),
    (0.0, 28, "young_asian_female", "long01", ["female_casualsuit01", "shoes03"]),
    (1.0, 52, "middleage_caucasian_male", "short01", ["male_elegantsuit01", "shoes06"]),
    (0.0, 41, "middleage_african_female", "bob02", ["female_elegantsuit01", "shoes04"]),
    (1.0, 23, "young_african_male", "short03", ["male_casualsuit03", "shoes05"]),
    (0.0, 19, "young_caucasian_female", "ponytail01", ["female_sportsuit01", "shoes03"]),
    (1.0, 67, "old_asian_male", "short04", ["male_casualsuit05", "shoes01"]),
    (0.0, 33, "young_asian_female", "bob01", ["female_casualsuit02", "shoes04"]),
    (1.0, 29, "young_caucasian_male", "short02", ["male_casualsuit01", "shoes02"]),
    (0.0, 60, "old_caucasian_female", "braid01", ["female_casualsuit01", "shoes06"]),
]
rnd = random.Random(11)
for i, (g, years, skin, hair, clothes) in enumerate(CITIZEN_LOOKS):
    race = {"asian": (0.15, 0.7, 0.15), "african": (0.75, 0.1, 0.15), "caucasian": (0.05, 0.1, 0.85)}[skin.split("_")[1]]
    CHARACTERS.append(dict(id=f"citizen_{i}", gender=g, age=years, muscle=rnd.uniform(0.35, 0.6), weight=rnd.uniform(0.35, 0.65),
                           height=rnd.uniform(0.4, 0.65), race=race, skin=skin, hair=hair, eyebrows=f"eyebrow00{rnd.randint(1, 9)}",
                           lashes=f"eyelashes0{rnd.randint(1, 4)}", eyes=rnd.choice(["brown", "brownlight", "bluegreen", "grey"]), clothes=clothes))


def grow_clothes_masks(basemesh, rings=1):
    """MPFB trims the hide-under-clothes groups conservatively; grow them so skin does not poke through when bending."""
    import bmesh
    groups = [m.vertex_group for m in basemesh.modifiers if m.type == "MASK" and m.invert_vertex_group and m.vertex_group and m.name != "Hide helpers"]
    if not groups:
        return
    bm = bmesh.new()
    bm.from_mesh(basemesh.data)
    bm.verts.ensure_lookup_table()
    for name in groups:
        vg = basemesh.vertex_groups.get(name)
        if vg is None:
            continue
        inside = {v.index for v in basemesh.data.vertices if any(g.group == vg.index and g.weight > 0.5 for g in v.groups)}
        for _ in range(rings):
            inside |= {e.other_vert(bm.verts[i]).index for i in list(inside) for e in bm.verts[i].link_edges}
        vg.add(list(inside), 1.0, "REPLACE")
        print("[MPFB]   mask", name, "->", len(inside), "verts")
    bm.free()


def make(spec):
    clear_scene()
    macro = TargetService.get_default_macro_info_dict()
    macro["gender"] = spec["gender"]
    macro["age"] = age_value(spec["age"])
    macro["muscle"] = spec["muscle"]
    macro["weight"] = spec["weight"]
    macro["height"] = spec["height"]
    macro["proportions"] = 0.6
    af, asn, ca = spec["race"]
    macro["race"]["african"], macro["race"]["asian"], macro["race"]["caucasian"] = af, asn, ca
    # feet_on_ground lifts the mesh object but the rig is fitted to the unlifted body (bones ~0.9 m below the skin,
    # every joint stretched the skin); Unity puts the feet on the ground itself (CharacterVisual.Normalize)
    basemesh = HumanService.create_human(mask_helpers=True, detailed_helpers=False, extra_vertex_groups=True, feet_on_ground=False, scale=0.1, macro_detail_dict=macro)
    HumanService.set_character_skin(A("skins", spec["skin"], spec["skin"] + ".mhmat"), basemesh, skin_type="GAMEENGINE")
    # rig first: proxies added to a rigged basemesh get the rig's weights (otherwise clothes and hair stay static)
    rig = HumanService.add_builtin_rig(basemesh, "mixamo_unity", import_weights=True)
    HumanService.add_mhclo_asset(A("eyes", "high-poly", "high-poly.mhclo"), basemesh, asset_type="Eyes", subdiv_levels=0, material_type="GAMEENGINE",
                                 alternative_materials=None)
    HumanService.add_mhclo_asset(A("eyebrows", spec["eyebrows"], spec["eyebrows"] + ".mhclo"), basemesh, asset_type="Eyebrows", subdiv_levels=0, material_type="GAMEENGINE")
    HumanService.add_mhclo_asset(A("eyelashes", spec["lashes"], spec["lashes"] + ".mhclo"), basemesh, asset_type="Eyelashes", subdiv_levels=0, material_type="GAMEENGINE")
    HumanService.add_mhclo_asset(A("teeth", "teeth_base", "teeth_base.mhclo"), basemesh, asset_type="Teeth", subdiv_levels=0, material_type="GAMEENGINE")
    HumanService.add_mhclo_asset(A("hair", spec["hair"], spec["hair"] + ".mhclo"), basemesh, asset_type="Hair", subdiv_levels=0, material_type="GAMEENGINE")
    for c in spec["clothes"]:
        HumanService.add_mhclo_asset(A("clothes", c, c + ".mhclo"), basemesh, asset_type="Clothes", subdiv_levels=0, material_type="GAMEENGINE")
    # fit the skeleton (and the clothes) to the final body shape, after the macro targets
    bpy.context.view_layer.update()
    RigService.refit_existing_armature(rig, basemesh)
    grow_clothes_masks(basemesh)
    for o in bpy.data.objects:
        if o.type == "MESH":
            print("[MPFB]  ", o.name, "scale", tuple(round(x, 3) for x in o.scale), "rot", tuple(round(x, 2) for x in o.rotation_euler), "parent", o.parent.name if o.parent else None, "armature mods", [m.object.name if m.object else None for m in o.modifiers if m.type == "ARMATURE"])
    # bake the masks (hide-under-clothes, helpers) into the meshes: exporting them as live modifiers removes vertices
    # after the skin weights are gathered, which shifts every weight onto the wrong vertex (knees weighted to fingers)
    for o in [o for o in bpy.data.objects if o.type == "MESH"]:
        bpy.ops.object.select_all(action="DESELECT")
        o.select_set(True)
        bpy.context.view_layer.objects.active = o
        if o.data.shape_keys:
            bpy.ops.object.shape_key_remove(all=True, apply_mix=True)   # bake the body-shape targets first
        for m in [m for m in o.modifiers if m.type != "ARMATURE"]:
            bpy.ops.object.modifier_apply(modifier=m.name)
    if os.environ.get("MPFB_CHECK"):
        bm = basemesh
        zs = [(bm.matrix_world @ v.co).z for v in bm.data.vertices]
        print("[MPFB]   body z", round(min(zs), 3), round(max(zs), 3), "loc", tuple(round(x, 3) for x in bm.location), "rig loc", tuple(round(x, 3) for x in rig.location))
        for bn in ("mixamorig:Hips", "mixamorig:Head", "mixamorig:LeftFoot", "mixamorig:LeftHand"):
            print("[MPFB]   bone", bn, tuple(round(x, 3) for x in rig.matrix_world @ rig.data.bones[bn].head_local))
        for bn in ("mixamorig:LeftHandIndex3", "mixamorig:LeftForeArm", "mixamorig:LeftLeg"):
            pt = rig.matrix_world @ rig.data.bones[bn].head_local
            best = min(bm.data.vertices, key=lambda v: (bm.matrix_world @ v.co - pt).length)
            gs = sorted(((bm.vertex_groups[g.group].name, round(g.weight, 2)) for g in best.groups), key=lambda x: -x[1])[:4]
            print("[MPFB]   weights near", bn, round((bm.matrix_world @ best.co - pt).length, 3), gs)
    # origin at the soles: lift the whole rig (meshes are its children) so the lowest vertex sits on z = 0
    lowest = min((o.matrix_world @ v.co).z for o in bpy.data.objects if o.type == "MESH" for v in o.data.vertices)
    rig.location.z -= lowest
    bpy.context.view_layer.update()
    print("[MPFB]   lifted rig by", round(-lowest, 3))
    # the exporter writes the current pose as the skeleton but binds the meshes to the rest pose: any leftover pose
    # (MPFB can leave one) makes every animated joint stretch the skin, so export in the rest pose
    import mathutils
    posed = [pb.name for pb in rig.pose.bones if (pb.matrix_basis - mathutils.Matrix.Identity(4)).to_quaternion().angle > 1e-4 or pb.matrix_basis.to_translation().length > 1e-5]
    for pb in rig.pose.bones:
        pb.matrix_basis = mathutils.Matrix.Identity(4)
    rig.data.pose_position = "REST"
    bpy.context.view_layer.update()
    print("[MPFB]   reset", len(posed), "posed bones", posed[:6])
    print("[MPFB]   rig scale", tuple(round(x, 3) for x in rig.scale), "rot", tuple(round(x, 2) for x in rig.rotation_euler), "bones", len(rig.data.bones))
    # export the rig and everything parented to it
    bpy.ops.object.select_all(action="DESELECT")
    for o in bpy.data.objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active = rig
    path = os.path.join(OUT, spec["id"] + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
                             object_types={"ARMATURE", "MESH"}, use_mesh_modifiers=False, add_leaf_bones=False,
                             bake_anim=False, path_mode="COPY", embed_textures=True, mesh_smooth_type="FACE",
                             primary_bone_axis="Y", secondary_bone_axis="X", axis_forward="-Z", axis_up="Y")
    print("[MPFB] exported", path)
    return {"id": spec["id"], "height_m": spec["height"]}


done = []
for spec in CHARACTERS:
    if ONLY and spec["id"] not in ONLY:
        continue
    try:
        done.append(make(spec))
    except Exception:
        traceback.print_exc()
        print("[MPFB] FAILED", spec["id"])
with open(os.path.join(OUT, "characters.json"), "w") as f:
    json.dump(done, f, indent=1)
print("[MPFB] finished", len(done))
