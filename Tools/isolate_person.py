# Cuts the person out of a Polycam scene scan (OBJ): keeps a cylinder around the person, slices off the floor,
# keeps the largest connected piece, and saves it as a .blend + OBJ with its texture.
#   blender -b --factory-startup --python Tools/isolate_person.py -- <scene.obj> <out dir> [radius] [cx cy] [floor margin]
# Distances are in the scan's own units (Polycam scenes are not always in metres).
import bpy, bmesh, sys, math, statistics, os, mathutils
a = sys.argv[sys.argv.index("--") + 1:]
src, out = a[0], a[1]
R = float(a[2]) if len(a) > 2 else 0.75
os.makedirs(out, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.obj_import(filepath=src)
o = [x for x in bpy.context.scene.objects if x.type == "MESH"][0]
bpy.context.view_layer.objects.active = o
o.select_set(True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)   # the importer turns Y-up into a rotation: bake it, Z is up
bm = bmesh.new(); bm.from_mesh(o.data)
# centre: the column of geometry standing 0.8..1.6 m above the scene floor near the middle
zs = sorted(v.co.z for v in bm.verts if abs(v.co.x) < 1.5 and abs(v.co.y) < 1.5)
floor0 = zs[len(zs) // 10]
body = [v.co for v in bm.verts if math.hypot(v.co.x, v.co.y) < 0.9 and floor0 + 0.8 < v.co.z < floor0 + 1.6]
cx = statistics.median(p.x for p in body) if len(a) < 5 else float(a[3])
cy = statistics.median(p.y for p in body) if len(a) < 5 else float(a[4])
# local floor: the ground in a ring just outside the feet
ring = [v.co.z for v in bm.verts if R < math.hypot(v.co.x - cx, v.co.y - cy) < R * 1.6 and v.co.z < floor0 + R]
floor = statistics.median(ring) if ring else floor0
print("[ISO] centre", round(cx, 3), round(cy, 3), "floor", round(floor, 3))
# 1) everything outside the cylinder, and everything at floor level, goes
cut = floor + (float(a[5]) if len(a) > 5 else 0.025)
bmesh.ops.delete(bm, geom=[v for v in bm.verts if math.hypot(v.co.x - cx, v.co.y - cy) > R or v.co.z < cut], context="VERTS")
# 2) keep the largest connected piece (the person); floor shards and bits of wall fall away
bm.verts.ensure_lookup_table()
seen, best = set(), []
for v in bm.verts:
    if v in seen: continue
    comp, stack = [], [v]
    seen.add(v)
    while stack:
        x = stack.pop(); comp.append(x)
        for e in x.link_edges:
            y = e.other_vert(x)
            if y not in seen: seen.add(y); stack.append(y)
    if len(comp) > len(best): best = comp
keep = set(best)
bmesh.ops.delete(bm, geom=[v for v in bm.verts if v not in keep], context="VERTS")
# 3) put him on the origin, feet on z = 0
for v in bm.verts: v.co -= mathutils.Vector((cx, cy, cut))
zmax = max(v.co.z for v in bm.verts)
print("[ISO] kept", len(bm.verts), "verts, height", round(zmax, 3))
bm.to_mesh(o.data); bm.free()
o.data.update()
o.name = "Person"
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out, "person.blend"))
bpy.ops.wm.obj_export(filepath=os.path.join(out, "person.obj"), export_selected_objects=False, path_mode="COPY")
print("[ISO] saved", out)
