"""Re-imports each FBX in an empty scene and prints dims/origin/tris vs budget."""
import sys, os, bpy
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from labkit import BUDGET
from mathutils import Vector
M = '/home/claude/out/Assets/DarkShot/Models/'
for f in sorted(os.listdir(M)):
    if not f.endswith('.fbx') or f == 'BatteryStation.fbx': continue
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=M + f)
    name = f[:-4]; tot = 0
    for o in bpy.data.objects:
        if o.type != 'MESH': continue
        vs = [o.matrix_world @ v.co for v in o.data.vertices]
        lo = Vector((min(v[i] for v in vs) for i in range(3))); hi = Vector((max(v[i] for v in vs) for i in range(3)))
        t = sum(len(p.vertices) - 2 for p in o.data.polygons); tot += t
        loc = o.matrix_world.translation
        print(f'  {o.name:18s} tris={t:5d} dims(x,y,z blender)={tuple(round(x,3) for x in o.dimensions)} origin={tuple(round(x,3) for x in loc)} '
              f'bboxRelOrigin min={tuple(round(x,3) for x in (lo-loc))} max={tuple(round(x,3) for x in (hi-loc))} mats={[m.name for m in o.data.materials]}')
    b = BUDGET.get(name); print(f'{name}: total tris {tot} / budget {b} -> {"OK" if b is None or tot<=b else "OVER"}')
