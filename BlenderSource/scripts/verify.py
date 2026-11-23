import bpy, sys, json
from mathutils import Vector
def verify(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=path)
    sc = bpy.context.scene
    meshes = [o for o in bpy.data.objects if o.type == 'MESH']
    arms = [o for o in bpy.data.objects if o.type == 'ARMATURE']
    tris = 0; mn = Vector((1e9,) * 3); mx = Vector((-1e9,) * 3)
    dg = bpy.context.evaluated_depsgraph_get()
    for o in meshes:
        tris += sum(len(p.vertices) - 2 for p in o.data.polygons)
        for v in o.data.vertices:
            w = o.matrix_world @ v.co
            for i in range(3): mn[i] = min(mn[i], w[i]); mx[i] = max(mx[i], w[i])
    d = mx - mn
    bones = [b.name for a in arms for b in a.data.bones]
    acts = []
    for a in bpy.data.actions:
        fr = a.frame_range
        acts.append((a.name, round((fr[1] - fr[0]) / sc.render.fps, 3), tuple(fr), sc.render.fps))
    mats = sorted({m.name for m in bpy.data.materials})
    info = dict(file=path.split('/')[-1], meshes=[(o.name, len(o.vertex_groups), [m.name for m in o.data.materials]) for o in meshes],
                tris=tris, nbones=len(bones), bones=bones, actions=acts, dims=[round(x, 3) for x in d],
                bbox_min=[round(x, 3) for x in mn], bbox_max=[round(x, 3) for x in mx], armatures=[a.name for a in arms],
                vcol=[list(o.data.color_attributes.keys()) for o in meshes], mats=mats)
    return info
if __name__ == '__main__':
    for p in sys.argv[sys.argv.index('--') + 1:]:
        print(json.dumps(verify(p)))
