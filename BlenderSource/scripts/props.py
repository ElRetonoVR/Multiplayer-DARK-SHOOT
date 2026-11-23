import sys; sys.path.insert(0, '/home/claude/blender_work')
from common import *
from bloom import baby, ell
from math import sin, cos, pi, radians as rad

def one(name, b):
    ob, tris = b.build(name, [])
    return ob, tris

def build_face():
    clear_scene(); b = B(); rng = random.Random(11)
    S = 'M_BloomSkin'
    HR = (.25, .20, .28)
    b.blob((0, 0, 0), HR, S, None, 20, 14)
    b.blob((0, -.03, .17), (.2, .13, .12), S, None, 12, 6)           # forehead
    b.blob((-.015, -.12, -.27), (.14, .09, .075), S, None, 10, 6)       # chin, off-center
    for sx in (-1, 1):
        b.blob((sx * .16, -.12, -.06), (.085, .06, .08), S, None, 8, 5)    # cheeks
        er = .078 if sx > 0 else .068; ez = .055 if sx > 0 else .075
        ex = sx * .105
        b.blob((ex, -.132, ez), er, 'M_BloomEyes', None, 14, 9)
        b.blob((ex + sx * .004, -.2, ez - .004), .022, 'M_BloomMouth', None, 6, 4)
        for k in range(7):                                                     # veins over eyeballs
            a = rng.uniform(0, 2 * pi); pts = []
            for t in (.35, .6, .85):
                pts.append(V((ex + cos(a) * er * t * 1.05, -.132 - er * math.sqrt(max(0.05, 1 - (t * 1.05) ** 2)) * .98, ez + sin(a) * er * t * 1.05)))
            b.sweep(pts, .0035, 'M_BloomVeins', None, 3, caps=(False, False))
        ring = [V((ex + cos(a) * (er + .014), -.172, ez + sin(a) * (er + .014))) for a in [2 * pi * k / 12 for k in range(12)]]
        b.sweep(ring, .022, S, None, 5, closed=True)
        b.blob((ex, -.15, ez + er + .035), (.1, .05, .03), S, None, 8, 4, Euler((0, rad(-sx * (14 if sx > 0 else 4)), 0)).to_matrix())   # brow
    b.blob((0, -.19, -.04), (.04, .035, .05), S, None, 8, 5)  # nose
    for sx in (-1, 1): b.blob((sx * .017, -.215, -.058), (.012, .01, .02), 'M_BloomMouth', None, 5, 3)
    def surf(x, z): return -.2 * math.sqrt(max(.08, 1 - (x / .25) ** 2 - (z / .28) ** 2))
    mz = -.145
    b.blob((0, -.125, mz), (.16, .05, .095), 'M_BloomMouth', None, 14, 8)
    ring = []
    for k in range(20):
        a = 2 * pi * k / 20; x = cos(a) * .175; z = mz + sin(a) * (.10 if sin(a) > 0 else .125)
        ring.append(V((x, surf(x, z) - .004, z)))
    b.sweep(ring, .02, S, None, 5, closed=True)
    for i in range(10):
        a = pi * (.08 + .84 * i / 9); x = cos(a) * .165; z = mz + sin(a) * .098; l = rng.uniform(.05, .10)
        p0 = V((x, surf(x, z) - .008, z)); d = V((rng.uniform(-.02, .02), -.01, -l))
        b.sweep([p0, p0 + d * .6, p0 + d], [.02, .014, .003], 'M_BloomEyes', None, 4, caps=(True, False))
    for i in range(8):
        a = pi * (1.1 + .8 * i / 7); x = cos(a) * .15; z = mz + sin(a) * .115; l = rng.uniform(.04, .08)
        p0 = V((x, surf(x, z) - .008, z)); d = V((rng.uniform(-.02, .02), -.01, l))
        b.sweep([p0, p0 + d * .6, p0 + d], [.018, .013, .003], 'M_BloomEyes', None, 4, caps=(True, False))
    tp = [(0, -.10, -.16), (0, -.19, -.19), (.01, -.27, -.23), (.02, -.31, -.31), (.0, -.29, -.40), (-.03, -.23, -.47), (-.03, -.17, -.50)]
    b.sweep(tp, [.055, .052, .048, .043, .036, .028, .012], 'M_BloomVeins', None, 8)
    gp = [(p[0], p[1] - .03, p[2] + .02) for p in tp[1:6]]
    b.sweep(gp, .007, 'M_BloomMouth', None, 4, caps=(False, False))
    # embedded babies
    baby(b, (-.19, -.1, .12), (-1, -.7, .3), 1.8, None)
    baby(b, (.15, -.12, .2), (.5, -1, .6), 1.6, None)
    baby(b, (.2, -.05, -.17), (1, -.5, -.3), 1.4, None)
    # lumps
    for i in range(14):
        p = ell((0, 0, 0), HR, rng.uniform(-80, 80), rng.uniform(-30, 70), 1.0)
        if abs(p.x) < .2 and -.2 < p.z < .12 and p.y < -.1: continue
        b.blob(p, rng.uniform(.015, .035), S, None, 5, 3)
    # veins
    for i in range(24):
        az = rng.uniform(-85, 85); el = rng.uniform(-55, 75)
        pts = [ell((0, 0, 0), HR, az + i2 * rng.uniform(-6, 8), el + i2 * rng.uniform(-7, 9), 1.012) for i2 in range(5)]
        if any(abs(p.x) < .2 and -.22 < p.z < .1 and p.y < -.12 for p in pts): continue
        b.sweep(pts, [.008 - .0012 * j for j in range(5)], 'M_BloomVeins', None, 3, caps=(False, False))
    # roots on scalp
    for sx in (-1, 1):
        for j in range(3):
            st = ell((0, 0, 0), HR, sx * (60 + 10 * j), 30 + 15 * j, .98)
            pts = [st, st + V((sx * .02, .05, .06 + .02 * j)), st + V((sx * .045, .10, .10 + .03 * j)), st + V((sx * .06, .16, .09 + .03 * j))]
            b.sweep(pts, [.02, .016, .011, .006], 'M_Branch', None, 4, caps=(False, True))
            b.blob(pts[-1], .018, 'M_BranchBulb', None, 5, 3)
    return one('Bloom_Face', b)

def build_battery():
    clear_scene(); b = B()
    b.sweep([(0, 0, -.075), (0, 0, .06)], .035, 'M_Battery', None, 12)
    b.sweep([(0, 0, -.022), (0, 0, .03)], .0365, 'M_BatteryGlow', None, 12)
    b.sweep([(0, 0, .06), (0, 0, .075)], .014, 'M_Station', None, 12)
    b.sweep([(0, 0, -.052), (0, 0, -.044)], .0375, 'M_Station', None, 12)
    b.sweep([(0, 0, .042), (0, 0, .05)], .0375, 'M_Station', None, 12)
    return one('Battery', b)

def build_station():
    clear_scene(); b = B(); M = 'M_Station'; D = 'M_Battery'; G = 'M_BatteryGlow'
    b.box((0, 0, .03), (.46, .38, .06), M)
    b.box((0, .02, .35), (.20, .16, .58), M)
    b.box((0, 0, .76), (.36, .26, .34), M)
    b.box((0, -.02, .94), (.30, .20, .03), M)
    b.box((0, -.133, .77), (.07, .006, .22), D)                     # slot
    for x in (-.045, .045): b.box((x, -.14, .77), (.016, .02, .24), M)
    b.box((0, -.14, .895), (.11, .02, .016), M); b.box((0, -.14, .645), (.11, .02, .016), M)
    for x in (-.095, .095): b.box((x, -.135, .77), (.014, .01, .2), G)
    b.box((0, -.15, .60), (.20, .12, .025), M)                      # tray
    b.box((0, -.205, .62), (.20, .012, .04), M)
    for i in range(4): b.box((0, -.101, .5 - i * .06), (.12, .006, .014), D)
    b.sweep([(0, -.02, .96), (0, -.02, 1.0)], .035, G, None, 8)     # beacon
    b.sweep([(0, -.02, .955), (0, -.02, .965)], .045, M, None, 8)
    for sx in (-1, 1):
        b.sweep([(sx * .18, .0, .80), (sx * .24, .0, .70), (sx * .26, -.02, .5), (sx * .24, -.08, .25), (sx * .2, -.12, .07)], .012, D, None, 5)
        for x in (-.19, .19): pass
    for sx in (-1, 1):
        for sy in (-1, 1): b.box((sx * .19, sy * .15, .068), (.026, .026, .016), D)
    for sx in (-1, 1):
        for sz in (.65, .88): b.box((sx * .175, -.128, sz), (.02, .012, .02), D)
    b.box((0, -.07, .08), (.12, .1, .03), D)
    return one('BatteryStation', b)

def grow(b, pts2, r0, mapper, rng, depth=0, maxd=2, bulb=True):
    n = len(pts2)
    pts = [mapper(p, r0 * (1 - .8 * i / (n - 1)) + .006, i / (n - 1)) for i, p in enumerate(pts2)]
    rad = [r0 * (1 - .8 * i / (n - 1)) + .006 for i in range(n)]
    b.sweep(pts, rad, 'M_Branch', None, 5, caps=(True, True))
    if bulb: b.blob(pts[-1], rad[-1] * 2.4 + .02, 'M_BranchBulb', None, 6, 3)
    for kk in (n // 3, (2 * n) // 3):
        if r0 > .03 or kk == n // 2: b.blob(pts[kk], rad[kk] * 1.9, 'M_Branch', None, 6, 3)
    if r0 > .035: b.blob(pts[n // 2] + V((0, -rad[n // 2] * 1.2, 0)) if mapper(V((0, 0)), .1, 0).y < 0 else pts[n // 2] + V((0, 0, rad[n // 2] * 1.2)), rad[n // 2] * 1.1, 'M_BranchBulb', None, 5, 3)
    return pts

def path2(start, ang, length, steps, rng, curl=0.0, wob=.3):
    p = V(start); pts = [p.copy()]; a = ang; l = length / steps
    for i in range(steps):
        a += rng.uniform(-wob, wob) + curl
        p = p + V((sin(a), cos(a))) * l; pts.append(p.copy())
    return pts

def build_branch(kind):
    clear_scene(); b = B()
    if kind == 'Floor':
        rng = random.Random(5)
        def mapper(p, r, t): return V((p.x, p.y, r * .55 + (t ** 2) * .22))
        angs = [10, 80, 150, 215, 280, 335]
        for i, a in enumerate(angs):
            L = rng.uniform(1.0, 1.15)
            # path2 uses (x, up) = (sin a, cos a): use as (x, y)
            pts = path2((0, 0), rad(a), L, 8, rng, curl=rng.choice([-.1, .1]), wob=.3)
            pp = grow(b, pts, .075 if i % 2 == 0 else .06, mapper, rng)
            if i in (0, 2):
                k = 4; sub = path2(pts[k], rad(a) + rng.choice([-1, 1]) * .9, L * .45, 5, rng, curl=.1, wob=.25)
                grow(b, sub, .04, mapper, rng)
        b.blob((0, 0, .02), (.1, .1, .05), 'M_Branch', None, 8, 4)
        name = 'Branch_Floor'
    else:
        rng = random.Random(21 if kind == 'A' else 33)
        def mapper(p, r, t): return V((p.x, -r * .55, p.y))
        if kind == 'A':
            main = path2((0, 0), 0.05, 1.95, 11, rng, curl=-.04, wob=.22)
            grow(b, main, .08, mapper, rng)
            for k, sgn in ((3, 1), (6, -1), (8, 1)):
                sub = path2(main[k], sgn * 1.0, .75 if k < 7 else .5, 6, rng, curl=-sgn * .06, wob=.25)
                grow(b, sub, .045, mapper, rng)
            for sgn in (-1, 1):
                sub = path2((0, 0), sgn * 1.25, .85, 6, rng, curl=sgn * .1, wob=.25)
                grow(b, sub, .048, mapper, rng)
        else:
            main = path2((0, 0), 1.35, 2.0, 12, rng, curl=-.07, wob=.2)
            grow(b, main, .08, mapper, rng)
            for k, sgn in ((3, -1), (6, 1), (9, -1)):
                sub = path2(main[k], 1.35 + sgn * .9, .7 if k < 8 else .5, 6, rng, curl=-sgn * .1, wob=.25)
                grow(b, sub, .045, mapper, rng)
            sub = path2((0, 0), -.45, .9, 7, rng, curl=-.05, wob=.25)
            grow(b, sub, .05, mapper, rng)
        b.blob((0, -.03, 0), (.1, .06, .1), 'M_Branch', None, 8, 4)
        name = f'Branch_Wall_{kind}'
    return one(name, b)

if __name__ == '__main__':
    res = {}
    for nm, fn, arg in (('Bloom_Face', build_face, None), ('Battery', build_battery, None), ('BatteryStation', build_station, None),
                        ('Branch_Wall_A', build_branch, 'A'), ('Branch_Wall_B', build_branch, 'B'), ('Branch_Floor', build_branch, 'Floor')):
        ob, tris = fn(arg) if arg else fn()
        bpy.ops.wm.save_as_mainfile(filepath=f'{BLEND}/{nm}.blend')
        export_fbx(f'{MODELS}/{nm}.fbx', [ob], anim=False)
        print('PROP', nm, tris, [round(x, 3) for x in ob.dimensions])
