"""python3 render_mp.py <name> ...   (renders FBX from Assets/DarkShot/Models to BlenderSource/renders)"""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from render import *
M = '/home/claude/out/Assets/DarkShot/Models/'
RENDERS = '/home/claude/out/BlenderSource/renders'
import render; render.RENDERS = RENDERS
_shoot = render.shoot
def shoot(name, c, d, az, el, lens=50): _shoot(name, c, d, az, el, lens, 640, 20)
def load(f, floor=True, floor_z=0.0):
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=M + f + '.fbx'); prep_materials()
    g = bpy.data.materials.get('M_LabGlass')
    if g:
        p = g.node_tree.nodes['Principled BSDF']; p.inputs['Alpha'].default_value = .4
    if floor:
        bpy.ops.mesh.primitive_plane_add(size=30, location=(0, 0, floor_z)); fl = bpy.context.object; fl.name = 'FLOOR'
        m = bpy.data.materials.new('FL'); m.use_nodes = True; m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (.025, .025, .03, 1); fl.data.materials.append(m)
J = {  # name: (center, dist, floor, extra views)
 'Lab_WallPanel': ((0, 0, 1.45), 4.6), 'Lab_WallEnd': ((0, 0, 1.45), 4.6), 'Lab_Block': ((0, 0, 1.4), 9.5),
 'Lab_Platform': ((0, 0, .2), 9), 'Lab_Crate': ((0, 0, .4), 2.4), 'Lab_Table': ((0, 0, .5), 3.6),
 'Lab_ServerRack': ((0, 0, 1.0), 4.2), 'Lab_CeilingLamp': ((0, 0, -.2), 1.8), 'Lab_FloorTile': ((0, 0, 0), 5.0),
}
for w in sys.argv[1:]:
    if w == 'Player_CDC':
        load(w, True); c = (0, -.05, 1.05)
        shoot('Player_CDC_front', c, 3.0, 0, 6); shoot('Player_CDC_34', c, 3.0, 40, 10); shoot('Player_CDC_back', c, 3.0, 180, 10)
        load(w, True); c = (0, -.05, 1.5); shoot('Player_CDC_head_front', (0, -.07, 1.62), 1.5, 0, 4); shoot('Player_CDC_head_34', (0, -.07, 1.62), 1.5, 40, 12)
    elif w == 'hand1':
        load('Player_CDC', False)
        for o in list(bpy.data.objects):
            if o.name != 'Avatar_RightHand': bpy.data.objects.remove(o)
        c = (-.3, .05, 1.2)
        shoot('Player_CDC_handR_a', c, .7, 30, 20); shoot('Player_CDC_handR_b', c, .7, -60, 30); shoot('Player_CDC_handR_c', c, .7, 180, 10)
    elif w == 'hands':
        load('Player_CDC', False)
        for o in bpy.data.objects:
            if o.name in ('Avatar_Head', 'Avatar_Trunk'): bpy.data.objects.remove(o)
        shoot('Player_CDC_hands_34', (0, .0, 1.2), 1.4, 40, 25); shoot('Player_CDC_hands_side', (0, .05, 1.2), 1.4, 90, 5)
    else:
        c, d = J[w]
        if w == 'Lab_WallPanel':
            load(w, True); shoot(w + '_front', c, d, 0, 3); shoot(w + '_back', c, d, 180, 3)
            shoot(w + '_34', c, d, 40, 12)
        elif w == 'Lab_CeilingLamp':
            load(w, False); shoot(w + '_front', c, d, 0, -5); shoot(w + '_34', c, d, 40, -25)
        elif w == 'Lab_FloorTile':
            load(w, False); shoot(w + '_top', c, d, 0, 80); shoot(w + '_34', c, d, 40, 35)
        else:
            load(w, True); shoot(w + '_front', c, d, 0, 5); shoot(w + '_34', c, d, 40, 18)
