# Poses an exported character FBX (arms down, one knee bent) and renders it, to check the skinning outside Unity.
#   blender -b --factory-startup --python Tools/pose_test_fbx.py -- <in.fbx> <out.png>
import bpy, sys, math
argv = sys.argv[sys.argv.index("--") + 1:]
src, out = argv[0], argv[1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
arm = next(o for o in bpy.context.scene.objects if o.type == "ARMATURE")
print("[POSE] armature", arm.name, "rot", tuple(round(math.degrees(a)) for a in arm.rotation_euler), "scale", tuple(arm.scale))
for m in [o for o in bpy.context.scene.objects if o.type == "MESH"]:
    groups = len(m.vertex_groups)
    mods = [x.type for x in m.modifiers]
    print("[POSE] mesh", m.name, "groups", groups, "mods", mods, "parent", m.parent.name if m.parent else None)
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="POSE")
pb = arm.pose.bones
for n, ang in (("mixamorig:LeftArm", 50), ("mixamorig:RightArm", -50)):
    if n in pb:
        pb[n].rotation_mode = "XYZ"; pb[n].rotation_euler = (0, 0, math.radians(ang))
if "mixamorig:LeftUpLeg" in pb:
    pb["mixamorig:LeftUpLeg"].rotation_mode = "XYZ"; pb["mixamorig:LeftUpLeg"].rotation_euler = (math.radians(-60), 0, 0)
if "mixamorig:LeftLeg" in pb:
    pb["mixamorig:LeftLeg"].rotation_mode = "XYZ"; pb["mixamorig:LeftLeg"].rotation_euler = (math.radians(70), 0, 0)
bpy.ops.object.mode_set(mode="OBJECT")
scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE_NEXT"
scene.render.resolution_x, scene.render.resolution_y = 800, 1000
world = bpy.data.worlds.new("w"); scene.world = world; world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.5, 0.5, 0.55, 1)
l = bpy.data.lights.new("k", "SUN"); l.energy = 3
lo = bpy.data.objects.new("k", l); scene.collection.objects.link(lo); lo.rotation_euler = (math.radians(50), 0, math.radians(30))
cam = bpy.data.cameras.new("cam"); cam.lens = 50
co = bpy.data.objects.new("cam", cam); scene.collection.objects.link(co); scene.camera = co
co.location = (2.2, -4.5, 1.0); co.rotation_euler = (math.radians(88), 0, math.radians(26))
scene.render.filepath = out
bpy.ops.render.render(write_still=True)
print("[POSE] wrote", out)
