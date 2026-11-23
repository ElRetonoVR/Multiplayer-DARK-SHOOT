"""Generates every multiplayer model: FBX (Assets/DarkShot/Models), .blend (BlenderSource/blend).  python3 make_mp.py"""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from mpcommon import *
import player, labkit

def save(name, objs):
    export_fbx(f'{MODELS}/{name}.fbx', objs)
    bpy.ops.wm.save_as_mainfile(filepath=f'{BLEND}/{name}.blend')

if __name__ == '__main__':
    objs, info = player.build_player(); save('Player_CDC', objs); print('Player_CDC', info, sum(info.values()))
    for name, fn in labkit.BUILDERS:
        clear_scene(); ob, t = fn(); save(name, [ob]); print(name, t)
