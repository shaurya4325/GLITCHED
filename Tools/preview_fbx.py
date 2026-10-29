# Renders a quick front / three-quarter preview of an exported character FBX with Blender (EEVEE).
#   blender -b --factory-startup --python Tools/preview_fbx.py -- <in.fbx> <out.png>
import bpy, sys, math
argv = sys.argv[sys.argv.index("--") + 1:]
src, out = argv[0], argv[1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
zmax = max((o.matrix_world @ v.co).z for o in meshes for v in o.data.vertices)
h = zmax
scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items] else "BLENDER_EEVEE"
scene.render.resolution_x, scene.render.resolution_y = 900, 1200
world = bpy.data.worlds.new("w"); scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.32, 0.34, 0.38, 1)
for i, (ang, d) in enumerate([(25, 1.2), (-60, 0.6)]):
    l = bpy.data.lights.new("key%d" % i, "AREA"); l.energy = 350 * d; l.size = 2
    lo = bpy.data.objects.new("key%d" % i, l); scene.collection.objects.link(lo)
    lo.location = (math.sin(math.radians(ang)) * 3, -math.cos(math.radians(ang)) * 3, h * 0.9)
    lo.rotation_euler = (math.radians(70), 0, math.radians(ang))
cam = bpy.data.cameras.new("cam"); cam.lens = 70
co = bpy.data.objects.new("cam", cam); scene.collection.objects.link(co); scene.camera = co
co.location = (1.2, -4.6, h * 0.55)
co.rotation_euler = (math.radians(90), 0, math.radians(15))
scene.render.filepath = out
bpy.ops.render.render(write_still=True)
print("[PREVIEW] wrote", out, "height", round(h, 2))
