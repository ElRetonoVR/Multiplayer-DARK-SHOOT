import sys; sys.path.insert(0, '/home/claude/blender_work')
from anim import *
for v in ('LabCoat', 'Engineer'):
    rig, ob, tris = make_variant_scene(v)
    print(v, tris)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND + f'/Bloom_{v}.blend')
    export_fbx(MODELS + f'/Bloom_{v}.fbx', [rig, ob], anim=False)
