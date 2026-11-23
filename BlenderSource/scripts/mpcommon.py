"""Helpers/materials shared by player.py and labkit.py (DarkShot multiplayer models)."""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import *
from math import sin, cos, pi, sqrt

# name: (hex, emission strength, roughness)  -- emission colour == base colour
MATS.update({
 # avatar
 'M_AvatarSuit': ('3C4640', 0, 0.85), 'M_AvatarVisor': ('0B1218', 0, 0.08),
 'M_AvatarGear': ('16181B', 0, 0.7), 'M_AvatarAccent': ('E6F0EC', 2.5, 0.4),
 # lab
 'M_LabMetal': ('3A4350', 0, 0.45), 'M_LabPanel': ('4A5758', 0, 0.7),
 'M_LabPipe': ('2A3037', 0, 0.5), 'M_LabFloor': ('2E3539', 0, 0.8),
 'M_LabFloorEdge': ('1F2427', 0, 0.6), 'M_LabGlass': ('6FA3AA', 0, 0.05),
 'M_LabLight': ('FFE2A8', 5.0, 0.4), 'M_LabLED': ('FF3A22', 4.0, 0.4),
 'M_LabLEDGreen': ('3CCB78', 3.0, 0.4), 'M_LabLiquid': ('4FD08A', 1.2, 0.2),
 'M_LabHazard': ('8C6A1C', 0, 0.6),
})

def bx(b, x0, x1, y0, y1, z0, z1, mat):
    return b.box(((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2), (x1 - x0, y1 - y0, z1 - z0), mat)

def cyl(b, p0, p1, r, mat, sides=8, n0=(1, 0, 0)):
    return b.sweep([p0, p1], [r, r], mat, None, sides, n0=n0)

def new_obj(name, b):
    ob, tris = b.build(name, [])
    return ob, tris
