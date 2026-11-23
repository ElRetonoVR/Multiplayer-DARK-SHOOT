import sys; sys.path.insert(0, '/home/claude/blender_work')
from anim import *
rig, ob, tris = make_variant_scene(None)
acts = make_actions(rig)
print('tris', tris, [a.name for a in bpy.data.actions])
bpy.ops.wm.save_as_mainfile(filepath=BLEND + '/Bloom_Base.blend')
export_fbx(MODELS + '/Bloom_Base.fbx', [rig, ob], anim=True)
