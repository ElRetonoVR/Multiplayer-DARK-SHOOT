"""Lab kit. Blender: Z up, X width, Y depth (front = -Y). Pivot at centre of base unless noted."""
from mpcommon import *
MET, PAN, PIP, FLO, GLA, LIT, LED, LEG, LIQ, HAZ = ('M_LabMetal', 'M_LabPanel', 'M_LabPipe', 'M_LabFloor', 'M_LabGlass',
    'M_LabLight', 'M_LabLED', 'M_LabLEDGreen', 'M_LabLiquid', 'M_LabHazard')
SKIN, VEIN, BR = 'M_BloomSkin', 'M_BloomVeins', 'M_Branch'

def lump(b, c, r, mat=SKIN, segs=5, rings=3): return b.blob(c, r, mat, None, segs, rings)

def wall_panel():
    b = B(); H = 2.93
    bx(b, -.495, .495, -.04, .04, 0, H, PAN)
    bx(b, -.5, .5, -.06, .06, 0, .16, MET)
    bx(b, -.5, .5, -.06, .06, 2.80, H, MET)
    for s in (-1, 1):
        for z0, z1 in ((.26, 1.30), (1.42, 2.62)):
            bx(b, -.42, .42, min(s * .04, s * .046), max(s * .04, s * .046), z0, z1, PAN)
    # front (-Y): round pipe + collars ; back (+Y): rectangular conduit + collars
    b.sweep([(-.5, -.03, 2.71), (.5, -.03, 2.71)], [.03, .03], PIP, None, 6, n0=(0, 1, 0))
    for x in (-.28, .28): bx(b, x - .02, x + .02, -.06, 0, 2.71 - .042, 2.71 + .042, MET)
    bx(b, -.5, .5, .005, .06, 2.64, 2.76, PIP)
    for x in (-.28, .28): bx(b, x - .02, x + .02, .0, .06, 2.62, 2.78, MET)
    # emergency indicator lights between panels
    bx(b, .34, .38, -.06, -.05, 1.34, 1.38, LED); bx(b, -.38, -.34, .05, .06, 1.34, 1.38, LED)
    # infection : flat lumps + veins hugging the surface (y inside +-0.06)
    for s, items in ((-1, [((-.30, .55), (.10, .014, .08)), ((.3, .24), (.07, .014, .05))]),
                     (1, [((.28, 1.75), (.10, .014, .08)), ((-.25, .5), (.08, .014, .06))])):
        for (x, z), r in items: lump(b, (x, s * .046, z), r, SKIN, 6, 4)
    for pts in (((-.30, -.05, .55), (-.18, -.05, .7), (-.12, -.05, .95)), ((.3, -.05, .24), (.2, -.05, .4), (.12, -.05, .65)),
                ((.28, .05, 1.75), (.18, .05, 1.5), (.1, .05, 1.3)), ((-.25, .05, .5), (-.3, .05, .75), (-.34, .05, 1.0))):
        b.sweep([V(p) for p in pts], [.008, .007, .004], VEIN, None, 3, caps=(False, False))
    return new_obj('Lab_WallPanel', b)

def wall_end():
    b = B()
    bx(b, -.08, .08, -.08, .08, 0, 2.93, PAN)
    bx(b, -.1, .1, -.1, .1, 0, .14, MET)
    bx(b, -.1, .1, -.1, .1, 2.79, 2.93, MET)
    for z in (.9, 1.95): bx(b, -.1, .1, -.1, .1, z, z + .08, MET)
    for sy in (-1, 1): bx(b, -.02, .02, min(sy*.1, sy*.08), max(sy*.1, sy*.08), 2.45, 2.55, LED)
    bx(b, .08, .1, -.02, .02, 2.45, 2.55, LED)
    return new_obj('Lab_WallEnd', b)

def block():
    b = B(); X, Y = 1.585, 1.49
    bx(b, -1.57, 1.57, -1.475, 1.475, 0, .28, MET)
    for sy in (-1, 1): bx(b, -1.57, 1.57, min(sy*1.475, sy*1.49), max(sy*1.475, sy*1.49), .08, .16, HAZ)
    for sx in (-1, 1): bx(b, min(sx*1.57, sx*1.585), max(sx*1.57, sx*1.585), -1.475, 1.475, .08, .16, HAZ)
    bx(b, -1.45, 1.45, -1.34, 1.34, .28, 2.62, PAN)
    bx(b, -1.55, 1.55, -1.46, 1.46, 2.62, 2.80, MET)
    bx(b, -1.2, 1.2, -1.1, 1.1, 2.80, 2.92, PIP)
    for i in range(6): bx(b, -1.0 + i * .4 - .04, -1.0 + i * .4 + .04, -.9, .9, 2.92, 2.97, MET)
    bx(b, -.15, .15, -.15, .15, 2.92, 3.0, LED)                                         # beacon
    # cryo tanks at the four corners
    for sx in (-1, 1):
        for sy in (-1, 1):
            cx, cy = sx * 1.285, sy * 1.18
            cyl(b, (cx, cy, .28), (cx, cy, 2.62), .30, GLA, 8)
            cyl(b, (cx, cy, .45), (cx, cy, 2.45), .22, LIQ, 8)
            cyl(b, (cx, cy, .28), (cx, cy, .42), .30, MET, 8)
            cyl(b, (cx, cy, 2.48), (cx, cy, 2.62), .30, MET, 8)
    # front/back windows with a specimen inside
    for sy in (-1, 1):
        bx(b, -.65, .65, min(sy * 1.34, sy * 1.37), max(sy * 1.34, sy * 1.37), .9, 2.2, MET)
        bx(b, -.55, .55, min(sy * 1.37, sy * 1.385), max(sy * 1.37, sy * 1.385), 1.0, 2.1, GLA)
    lump(b, (0, -1.30, 1.5), (.4, .08, .5), SKIN, 8, 5)
    lump(b, (.3, 1.30, 1.3), (.3, .08, .4), SKIN, 8, 5)
    # +X : hatch with wheel; -X : control panel
    bx(b, 1.45, 1.48, -.55, .55, .45, 2.05, MET)
    bx(b, 1.48, 1.5, -.48, .48, .52, 1.98, PAN)
    ring = [V((1.52, cos(2 * pi * k / 8) * .2, 1.25 + sin(2 * pi * k / 8) * .2)) for k in range(8)]
    b.sweep(ring, .018, PIP, None, 4, closed=True)
    bx(b, 1.5, 1.54, -.2, .2, 1.235, 1.265, PIP); bx(b, 1.5, 1.54, -.015, .015, 1.05, 1.45, PIP)
    bx(b, -1.5, -1.45, -.6, .6, 1.0, 2.0, MET)
    bx(b, -1.52, -1.5, -.4, .4, 1.4, 1.85, LEG)
    for i in range(3): bx(b, -1.53, -1.5, -.3 + i * .3 - .04, -.3 + i * .3 + .04, 1.15, 1.25, LED if i != 1 else LEG)
    # pipes on the +/-X faces
    for sx in (-1, 1):
        b.sweep([(sx * 1.52, -1.25, 2.3), (sx * 1.52, 1.2, 2.3), (sx * 1.52, 1.2, 2.62)], [.04] * 3, PIP, None, 6)
        b.sweep([(sx * 1.52, 1.25, 2.45), (sx * 1.52, -1.2, 2.45), (sx * 1.52, -1.2, 2.62)], [.035] * 3, PIP, None, 6)
    # infection
    rng = random.Random(5)
    for (x, y, z, r) in ((-1.45, -.9, .55, (.14, .12, .1)), (-1.45, -.7, .4, (.1, .09, .08)), (-1.2, -1.34, .5, (.15, .08, .12)),
                         (-.9, -1.36, .38, (.1, .06, .08)), (1.2, 1.34, 2.3, (.14, .08, .1)), (-.6, 1.34, 2.4, (.11, .07, .09)),
                         (.7, -1.34, .4, (.12, .07, .1)), (0, 0, 2.95, (.0, .0, .0))):
        if r[0] > 0: lump(b, (x, y, z), r, SKIN, 6, 4)
    for pts in (((-1.45, -.9, .55), (-1.45, -.6, .9), (-1.45, -.3, 1.0), (-1.45, -.2, 1.35)),
                ((-1.2, -1.34, .5), (-.9, -1.34, .8), (-.7, -1.34, 1.0), (-.6, -1.34, 1.3)),
                ((1.2, 1.34, 2.3), (1.0, 1.34, 2.0), (.9, 1.34, 1.7), (.85, 1.34, 1.45)),
                ((.7, -1.34, .4), (.9, -1.34, .7), (1.1, -1.34, .8), (1.2, -1.34, 1.1))):
        b.sweep([V(p) for p in pts], [.012, .01, .008, .004], VEIN, None, 3, caps=(False, False))
    for pts in (((-1.2, -1.34, .5), (-1.1, -1.36, .2), (-.9, -1.38, .1)),):
        b.sweep([V(p) for p in pts], [.03, .02, .01], BR, None, 4)
    for sx, sy in ((-1, -1), (1, 1), (1, -1), (-1, 1)):
        bx(b, sx * 1.4 - .03, sx * 1.4 + .03, sy * 1.2 - .03, sy * 1.2 + .03, 2.8, 2.84, LED)
    return new_obj('Lab_Block', b)

def platform():
    b = B(); n = 28
    cyl(b, (0, 0, 0), (0, 0, .1), 2.5, MET, n)
    cyl(b, (0, 0, .1), (0, 0, .2), 2.44, PIP, n)
    cyl(b, (0, 0, .2), (0, 0, .34), 2.5, MET, n)
    cyl(b, (0, 0, .34), (0, 0, .46), 2.38, PAN, n)
    for i in range(12):
        a = 2 * pi * i / 12
        b.box((cos(a) * 1.4, sin(a) * 1.4, .48), (1.9, .07, .04), MET, rot=Euler((0, 0, a)).to_matrix())
    cyl(b, (0, 0, .46), (0, 0, .5), .55, MET, 16)
    for i in range(12):
        a = 2 * pi * (i + .5) / 12
        b.box((cos(a) * 2.47, sin(a) * 2.47, .15), (.1, .18, .05), LED if i % 3 == 0 else LEG, rot=Euler((0, 0, a)).to_matrix())
    return new_obj('Lab_Platform', b)

def crate():
    b = B()
    bx(b, -.375, .375, -.375, .375, .06, .74, PAN)
    for sx in (-1, 1):
        for sy in (-1, 1):
            bx(b, sx * .365 - .035, sx * .365 + .035, sy * .365 - .035, sy * .365 + .035, .06, .74, MET)
    bx(b, -.4, .4, -.4, .4, 0, .06, MET); bx(b, -.4, .4, -.4, .4, .74, .8, MET)
    bx(b, -.15, .15, -.385, -.375, .3, .5, HAZ)
    bx(b, -.2, -.14, -.39, -.375, .6, .72, PIP); bx(b, .14, .2, -.39, -.375, .6, .72, PIP)
    bx(b, .22, .26, -.39, -.375, .38, .42, LED)
    lump(b, (.38, .1, .42), (.02, .12, .1), SKIN, 6, 4)
    return new_obj('Lab_Crate', b)

def table():
    b = B()
    bx(b, -.8, .8, -.45, .45, .64, .70, MET)
    for sx in (-1, 1):
        for sy in (-1, 1): bx(b, sx * .74 - .025, sx * .74 + .025, sy * .39 - .025, sy * .39 + .025, 0, .64, PIP)
    bx(b, -.72, .72, -.37, .37, .18, .21, PAN)
    for sy in (-1, 1): bx(b, -.74, .74, sy * .39 - .01 , sy * .39 + .01, .56, .64, PIP)
    bx(b, -.58, -.12, .02, .16, .70, .72, PAN); bx(b, -.58, -.12, .15, .16, .72, .76, PAN)
    for i in range(6):
        x = -.52 + i * .075
        cyl(b, (x, .08, .72), (x, .08, .88), .018, GLA, 6)
        cyl(b, (x, .08, .725), (x, .08, .80), .011, LIQ, 5)
    for x, y in ((.3, .2), (.5, -.1)): cyl(b, (x, y, .70), (x, y, .80), .05, GLA, 8)
    bx(b, .1, .45, -.3, -.06, .70, .73, PAN)
    lump(b, (.27, -.18, .745), (.07, .06, .03), SKIN, 6, 3)
    bx(b, -.72, -.66, -.44, -.43, .66, .68, LED)
    return new_obj('Lab_Table', b)

def server_rack():
    b = B()
    bx(b, -.3, .3, -.38, .4, .06, 1.96, MET)
    bx(b, -.3, .3, -.4, .4, 1.96, 2.0, PIP)
    for sx in (-1, 1):
        for sy in (-1, 1): bx(b, sx * .27 - .03, sx * .27 + .03, sy * .37 - .03, sy * .37 + .03, 0, .06, PIP)
    for i in range(8):
        z = .2 + i * .2
        bx(b, -.26, .26, -.395, -.38, z, z + .15, PAN)
        bx(b, -.23, -.2, -.40, -.395, z + .06, z + .09, LEG if i % 3 else LED)
        bx(b, -.17, -.14, -.40, -.395, z + .06, z + .09, LED if i % 2 else LEG)
    bx(b, -.2, .2, -.4, -.39, 1.84, 1.9, PIP)
    bx(b, .22, .25, -.40, -.39, .4, 1.2, PIP)
    bx(b, -.22, .22, .4 - .01, .4, .3, 1.0, PIP); bx(b, -.22, .22, .4 - .01, .4, 1.1, 1.8, PIP)
    for x in (-.1, .1):
        b.sweep([(x, .385, .5), (x, .385, .2), (x, .385, .06)], [.012] * 3, PIP, None, 4)
    bx(b, -.04, .04, -.4, -.39, 1.7, 1.78, LED)
    return new_obj('Lab_ServerRack', b)

def ceiling_lamp():
    b = B()
    bx(b, -.04, .04, -.04, .04, -.02, 0, PIP)
    for sx in (-1, 1):
        for sy in (-1, 1): b.sweep([(0, 0, -.02), (sx * .25, sy * .25, -.3)], [.004, .004], PIP, None, 3)
    bx(b, -.27, .27, -.27, .27, -.33, -.30, PAN)
    for (x0, x1, y0, y1) in ((-.3, .3, -.3, -.27), (-.3, .3, .27, .3), (-.3, -.27, -.27, .27), (.27, .3, -.27, .27)):
        bx(b, x0, x1, y0, y1, -.40, -.30, MET)
    bx(b, -.27, .27, -.27, .27, -.385, -.375, LIT)
    for i in (-1, 0, 1):
        bx(b, i * .13 - .007, i * .13 + .007, -.27, .27, -.40, -.385, MET)
        bx(b, -.27, .27, i * .13 - .007, i * .13 + .007, -.40, -.385, MET)
    return new_obj('Lab_CeilingLamp', b)

def floor_tile():
    b = B(); bm = b.bm
    h, i, d, t = 1.0, .9, .012, .05
    o = [bm.verts.new((sx * h, sy * h, 0)) for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
    n = [bm.verts.new((sx * i, sy * i, -d)) for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
    u = [bm.verts.new((sx * h, sy * h, -t)) for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
    def f(vs, mat):
        fc = bm.faces.new(vs); fc.material_index = b.mi(mat)
    for k in range(4):
        k2 = (k + 1) % 4
        f((o[k], o[k2], n[k2], n[k]), 'M_LabFloorEdge')       # bevelled border (CCW seen from above)
        f((o[k], u[k], u[k2], o[k2]), 'M_LabFloorEdge')       # outer side
    f((n[0], n[1], n[2], n[3]), FLO)
    # fix windings
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return new_obj('Lab_FloorTile', b)

BUILDERS = [('Lab_WallPanel', wall_panel), ('Lab_WallEnd', wall_end), ('Lab_Block', block), ('Lab_Platform', platform),
            ('Lab_Crate', crate), ('Lab_Table', table), ('Lab_ServerRack', server_rack), ('Lab_CeilingLamp', ceiling_lamp),
            ('Lab_FloorTile', floor_tile)]
BUDGET = {'Lab_WallPanel': 400, 'Lab_WallEnd': 120, 'Lab_FloorTile': 20, 'Lab_Crate': 200, 'Lab_Table': 600,
          'Lab_ServerRack': 500, 'Lab_CeilingLamp': 200, 'Lab_Block': 2500, 'Lab_Platform': 800, 'Player_CDC': 3000}
