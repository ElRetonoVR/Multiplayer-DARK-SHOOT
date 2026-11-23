import sys; sys.path.insert(0, '/home/claude/blender_work')
import bpy
from render import *
M = '/home/claude/darkshot_out/Assets/DarkShot/Models/'
def load(f):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=M + f)
    prep_materials()
def setact(name, frame):
    arm = [o for o in bpy.data.objects if o.type == 'ARMATURE'][0]
    arm.animation_data.action = bpy.data.actions['BloomRig|' + name]
    bpy.context.scene.frame_set(frame)
C = (0, -0.05, 0.5)
which = sys.argv[sys.argv.index('--') + 1:]
for w in which:
    if w == 'base':
        load('Bloom_Base.fbx')
        shoot('Bloom_Base_front', C, 2.4, 0, 4); shoot('Bloom_Base_34', C, 2.4, 40, 14)
    if w == 'variants':
        for v in ('LabCoat', 'Engineer'):
            load(f'Bloom_{v}.fbx')
            shoot(f'Bloom_{v}_front', C, 2.4, 0, 4); shoot(f'Bloom_{v}_34', C, 2.4, 40, 14)
    if w == 'anim':
        load('Bloom_Base.fbx')
        for i, f in enumerate((1, 13, 25)):
            setact('Walk', f); shoot(f'Bloom_Base_Walk_{i+1}', (0, -.12, .5), 2.6, 55, 10)
        for i, f in enumerate((6, 10)):
            setact('Attack', f); shoot(f'Bloom_Base_Attack_{i+1}', (0, -.3, .5), 2.9, 55, 10)

    if w == 'face':
        load('Bloom_Face.fbx'); shoot('Bloom_Face_front', (0, 0, -.05), 2.2, 0, 0); shoot('Bloom_Face_34', (0, 0, -.05), 2.2, 35, 10)
    if w == 'props':
        load('Battery.fbx'); b = [o for o in bpy.data.objects if o.type == 'MESH'][0]
        bpy.ops.import_scene.fbx(filepath=M + 'BatteryStation.fbx'); prep_materials()
        b.location = (.45, -.25, .075)
        bpy.context.view_layer.update()
        bpy.ops.mesh.primitive_plane_add(size=8, location=(0, 0, -0.001)); fl = bpy.context.object; fl.name = 'FLOOR'
        m = bpy.data.materials.new('FL'); m.use_nodes = True; m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (.03, .03, .035, 1); fl.data.materials.append(m)
        shoot('Battery_Station', (.15, -.1, .5), 2.6, 25, 10)
        shoot('Battery_Station_front', (.15, -.1, .5), 2.6, 0, 5)
    if w == 'branches':
        for nm, c, d, az, el, wall in (('Branch_Wall_A', (0, 0, 1.0), 4.0, 0, 0, True), ('Branch_Wall_B', (.6, 0, .45), 4.6, 0, 0, True), ('Branch_Floor', (0, 0, 0.1), 3.8, 20, 40, False)):
            load(nm + '.fbx')
            if wall:
                bpy.ops.mesh.primitive_plane_add(size=10, location=(c[0], 0.006, c[2])); p = bpy.context.object; p.rotation_euler = (math.pi / 2, 0, 0)
            else:
                bpy.ops.mesh.primitive_plane_add(size=10, location=(0, 0, -0.004)); p = bpy.context.object
            m = bpy.data.materials.new('WL'); m.use_nodes = True; m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (.12, .12, .13, 1); p.data.materials.append(m)
            shoot(nm, c, d, az, el)
