import sys; sys.path.insert(0, '/home/claude/blender_work')
from common import *

def mirror(p, s): return (p[0] * s, p[1], p[2])

# (name, head, tail, parent)
def bone_table():
    T = [
     ('Hips', (0, .05, .40), (0, .04, .50), None),
     ('Spine', (0, .04, .50), (0, -.02, .60), 'Hips'),
     ('Chest', (0, -.02, .60), (0, -.10, .72), 'Spine'),
     ('Neck', (0, -.10, .72), (0, -.18, .78), 'Chest'),
     ('Head', (0, -.18, .78), (0, -.26, .98), 'Neck'),
     ('Jaw', (0, -.28, .79), (0, -.40, .69), 'Head'),
     ('Tongue_1', (0, -.36, .73), (0, -.47, .69), 'Jaw'),
     ('Tongue_2', (0, -.47, .69), (0, -.54, .61), 'Tongue_1'),
     ('Tongue_3', (0, -.54, .61), (0, -.57, .50), 'Tongue_2'),
    ]
    for sd, sg in (('L', 1), ('R', -1)):
        T += [
         ('UpperArm_' + sd, mirror((.17, -.06, .68), sg), mirror((.27, -.14, .52), sg), 'Chest'),
         ('LowerArm_' + sd, mirror((.27, -.14, .52), sg), mirror((.29, -.26, .36), sg), 'UpperArm_' + sd),
         ('Hand_' + sd, mirror((.29, -.26, .36), sg), mirror((.29, -.33, .20), sg), 'LowerArm_' + sd),
         ('Claw_' + sd, mirror((.29, -.33, .20), sg), mirror((.29, -.38, .08), sg), 'Hand_' + sd),
         ('UpperLeg_' + sd, mirror((.09, .04, .40), sg), mirror((.11, -.03, .21), sg), 'Hips'),
         ('LowerLeg_' + sd, mirror((.11, -.03, .21), sg), mirror((.11, .03, .03), sg), 'UpperLeg_' + sd),
        ]
        T += [
         ('Branch' + sd + '_1', mirror((.05, .10, .72), sg), mirror((.14, .24, .82), sg), 'Chest'),
         ('Branch' + sd + '_2', mirror((.14, .24, .82), sg), mirror((.27, .44, .76), sg), 'Branch' + sd + '_1'),
        ]
    return T

def make_rig():
    ad = bpy.data.armatures.new('BloomRig')
    ob = bpy.data.objects.new('BloomRig', ad)
    bpy.context.scene.collection.objects.link(ob)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.mode_set(mode='EDIT')
    for n, h, t, p in bone_table():
        eb = ad.edit_bones.new(n); eb.head = V(h); eb.tail = V(t)
        y = (V(t) - V(h)).normalized()
        x = (V((1, 0, 0)) - y * y.x).normalized()
        eb.align_roll(x.cross(y))
        if p: eb.parent = ad.edit_bones[p]
    bpy.ops.object.mode_set(mode='OBJECT')
    ad.display_type = 'STICK'
    return ob

BONES = [b[0] for b in bone_table()]

def spinal(co):
    z = co.z
    if z < .44: return {'Hips': 1}
    if z < .54: t = (z - .44) / .10; return {'Hips': 1 - t, 'Spine': t}
    if z < .60: return {'Spine': 1}
    if z < .70: t = (z - .60) / .10; return {'Spine': 1 - t, 'Chest': t}
    return {'Chest': 1}

def ell(c, r, az, el, k=1.0):
    az = math.radians(az); el = math.radians(el)
    return V((c[0] + r[0] * k * math.cos(el) * math.sin(az), c[1] - r[1] * k * math.cos(el) * math.cos(az), c[2] + r[2] * k * math.sin(el)))

def arm_pts(sg):
    return [mirror(p, sg) for p in [(.15, -.05, .68), (.22, -.10, .60), (.27, -.14, .52), (.28, -.20, .44), (.29, -.26, .36)]]
LEG_PTS = [(.09, .04, .40), (.10, .0, .30), (.11, -.03, .21), (.11, .0, .12), (.11, .03, .04)]

def baby(b, c, facing, sc, wts):
    f = V(facing).normalized()
    R = V((0, -1, 0)).rotation_difference(f).to_matrix()
    P = lambda l: V(c) + R @ (V(l) * sc)
    b.blob(P((0, -.01, .055)), (.045 * sc, .04 * sc, .045 * sc), 'M_BloomSkin', wts, 5, 3, R)
    b.blob(P((0, 0, 0)), (.04 * sc, .033 * sc, .05 * sc), 'M_BloomSkin', wts, 5, 3, R)
    for sx in (-1, 1):
        b.blob(P((sx * .02, -.045, .065)), .008 * sc, 'M_BloomMouth', wts, 4, 2)
        b.sweep([P((sx * .04, -.01, .02)), P((sx * .06, -.04, -.02)), P((sx * .06, -.065, -.04))], [.012 * sc, .009 * sc, .006 * sc], 'M_BloomSkin', wts, 3)
    b.blob(P((0, -.052, .042)), (.012 * sc, .006 * sc, .008 * sc), 'M_BloomMouth', wts, 4, 2)

def build_body(b, variant=None, rng=None):
    rng = rng or random.Random(7)
    H = {'Head': 1}
    # torso
    b.blob((0, .05, .42), (.14, .12, .11), 'M_BloomSkin', spinal, 8, 4)
    b.blob((0, -.03, .52), (.16, .14, .13), 'M_BloomSkin', spinal, 8, 4)
    b.blob((0, -.05, .64), (.17, .14, .12), 'M_BloomSkin', spinal, 8, 4)
    b.blob((0, .07, .69), (.13, .11, .11), 'M_BloomSkin', spinal, 8, 4)
    b.blob((.17, -.05, .68), .065, 'M_BloomSkin', {'Chest': .5, 'UpperArm_L': .5}, 6, 4)
    b.blob((-.17, -.05, .68), .065, 'M_BloomSkin', {'Chest': .5, 'UpperArm_R': .5}, 6, 4)
    b.sweep([(0, -.11, .69), (0, -.15, .73), (0, -.19, .77)], .07, 'M_BloomSkin', [{'Chest': 1}, {'Neck': 1}, {'Neck': .5, 'Head': .5}], 6, caps=(False, False))
    # head
    b.blob((0, -.26, .83), (.17, .16, .17), 'M_BloomSkin', H, 10, 7)
    b.blob((0, -.30, .90), (.14, .10, .08), 'M_BloomSkin', H, 8, 4)
    for sx in (-1, 1):
        b.blob((sx * .11, -.35, .75), (.06, .05, .05), 'M_BloomSkin', H, 5, 3)
        er = .056 if sx > 0 else .05; ez = .825 if sx > 0 else .835
        b.blob((sx * .075, -.365, ez), er, 'M_BloomEyes', H, 8, 5)
        b.blob((sx * .072, -.414, ez - .004), .018, 'M_BloomMouth', H, 5, 3)
        ring = [V((sx * .075 + math.cos(a) * (er + .008), -.402, ez + math.sin(a) * (er + .008))) for a in [2 * math.pi * k / 10 for k in range(10)]]
        b.sweep(ring, .017, 'M_BloomSkin', H, 4, closed=True)
        b.blob((sx * .08, -.39, ez + .07), (.055, .03, .02), 'M_BloomSkin', H, 6, 4)
    b.blob((0, -.405, .775), (.02, .018, .02), 'M_BloomSkin', H, 5, 3)
    # mouth
    b.blob((0, -.372, .725), (.095, .035, .065), 'M_BloomMouth', H, 8, 4)
    ringp = []; ringw = []
    for k in range(12):
        a = 2 * math.pi * k / 12
        ringp.append(V((math.cos(a) * .105, -.395 + (1 - math.cos(a)) * .0 + abs(math.cos(a)) * .0, .73 + math.sin(a) * .06 * (1 if math.sin(a) > 0 else 1.6))))
    wts = lambda i, n: ({'Head': 1} if math.sin(2 * math.pi * i / 12) > .3 else ({'Jaw': 1} if math.sin(2 * math.pi * i / 12) < -.3 else {'Head': .5, 'Jaw': .5}))
    b.sweep(ringp, .016, 'M_BloomSkin', wts, 4, closed=True)
    b.blob((0, -.355, .675), (.085, .06, .045), 'M_BloomSkin', {'Jaw': 1}, 8, 5)
    # teeth
    for i in range(6):
        x = -.07 + i * .028 + rng.uniform(-.005, .005); l = rng.uniform(.015, .035)
        p0 = V((x, -.396, .76 + .0)); b.sweep([p0, p0 + V((rng.uniform(-.01, .01), -.004, -l))], [.007, .002], 'M_BloomEyes', H, 3, caps=(False, False))
    for i in range(5):
        x = -.056 + i * .028 + rng.uniform(-.005, .005); l = rng.uniform(.012, .03)
        p0 = V((x, -.392, .70)); b.sweep([p0, p0 + V((rng.uniform(-.01, .01), -.004, l))], [.007, .002], 'M_BloomEyes', {'Jaw': 1}, 3, caps=(False, False))
    # tongue
    tp = [(0, -.34, .72), (0, -.43, .70), (0, -.50, .65), (0, -.55, .57), (0, -.57, .50)]
    tw = [{'Jaw': .6, 'Tongue_1': .4}, {'Tongue_1': 1}, {'Tongue_1': .4, 'Tongue_2': .6}, {'Tongue_2': .5, 'Tongue_3': .5}, {'Tongue_3': 1}]
    b.sweep(tp, [.024, .022, .019, .016, .009], 'M_BloomVeins', tw, 6)
    # arms
    for sd, sg in (('L', 1), ('R', -1)):
        ap = arm_pts(sg); U, Lw, Hd = 'UpperArm_' + sd, 'LowerArm_' + sd, 'Hand_' + sd
        aw = [{U: .8, 'Chest': .2}, {U: 1}, {U: .5, Lw: .5}, {Lw: 1}, {Lw: .5, Hd: .5}]
        b.sweep(ap, [.06, .055, .05, .045, .038], 'M_BloomSkin', aw, 6, caps=(False, True))
        b.blob(mirror((.29, -.29, .31), sg), (.06, .04, .055), 'M_BloomSkin', {Hd: 1}, 6, 4)
        for i, fx in enumerate((-.045, -.015, .015, .045)):
            fl = [1.0, 1.25, 1.1, .85][i] * (1.15 if sd == 'R' else 1.0)
            x0 = .29 * sg + fx; sp = fx * 0.8
            P0 = V((x0, -.30, .28)); P1 = V((x0 + sp, -.30 - .035 * fl, .28 - .08 * fl)); P2 = V((x0 + sp * 1.6, -.30 - .07 * fl, .28 - .16 * fl)); P3 = V((x0 + sp * 1.9, -.30 - .09 * fl, .28 - .21 * fl))
            b.sweep([P0, P1, P2], [.019, .015, .012], 'M_BloomSkin', [{Hd: 1}, {Hd: .6, 'Claw_' + sd: .4}, {'Claw_' + sd: 1}], 3, caps=(True, False))
            b.sweep([P2, P3 + V((0, -.02, .02))], [.012, .002], 'M_Branch', {'Claw_' + sd: 1}, 3, caps=(False, False))
        # legs
        UL, LL = 'UpperLeg_' + sd, 'LowerLeg_' + sd
        lp = [mirror(p, sg) for p in LEG_PTS]
        lw = [{'Hips': .5, UL: .5}, {UL: 1}, {UL: .5, LL: .5}, {LL: 1}, {LL: 1}]
        b.sweep(lp, [.072, .064, .052, .044, .04], 'M_BloomSkin', lw, 6, caps=(False, True))
        b.blob(mirror((.11, -.02, .025), sg), (.05, .09, .025), 'M_BloomSkin', {LL: 1}, 6, 4)
        for tx in (-.025, 0, .025):
            b.sweep([V(mirror((.11 + tx, -.08, .02), sg)), V(mirror((.11 + tx * 1.4, -.13, .015), sg))], [.012, .005], 'M_BloomSkin', {LL: 1}, 3, caps=(True, True))
    # babies
    baby(b, (.08, -.16, .60), (.3, -1, .1), 1.5, {'Chest': 1})
    baby(b, (-.12, -.02, .76), (-.6, -.3, 1), 1.4, {'Chest': 1})
    baby(b, (-.10, -.16, .49), (-.4, -1, -.1), 1.3, {'Spine': 1})
    baby(b, (.12, .13, .62), (.5, 1, .3), 1.4, {'Chest': 1})
    # lumps
    for i in range(9):
        c, r = rng.choice([((0, -.05, .64), (.17, .14, .12)), ((0, -.03, .52), (.16, .14, .13)), ((0, .07, .69), (.13, .11, .11)), ((0, -.26, .83), (.17, .16, .17))])
        p = ell(c, r, rng.uniform(-150, 150), rng.uniform(-30, 60), 1.0)
        if c[2] == .83 and p.y < -0.30 and p.z < .88: continue
        b.blob(p, rng.uniform(.018, .035), 'M_BloomSkin', H if c[2] == .83 else spinal, 4, 3)
    # veins
    def vein(c, r, az0, el0, daz, dele, n, w, rad=.006):
        pts = [ell(c, r, az0 + daz * i + rng.uniform(-6, 6), el0 + dele * i + rng.uniform(-6, 6), 1.015) for i in range(n)]
        b.sweep(pts, [rad * (1 - .5 * i / n) for i in range(n)], 'M_BloomVeins', ([spinal(p) for p in pts] if w is spinal else w), 3, caps=(False, False))
    for az in (-100, -40, 20, 80):
        vein((0, -.05, .64), (.17, .14, .12), az, -20, rng.uniform(-8, 8), 18, 4, spinal)
    for az in (-70, 20):
        vein((0, -.03, .52), (.16, .14, .13), az, -10, rng.uniform(-8, 8), -16, 4, spinal)
    for az in (-60, 0, 60):
        vein((0, -.26, .83), (.17, .16, .17), az, 20, rng.uniform(-6, 6), 14, 4, H)
    # back branches
    for sd, sg in (('L', 1), ('R', -1)):
        B1, B2 = 'Branch' + sd + '_1', 'Branch' + sd + '_2'
        bw = lambda i, n, B1=B1, B2=B2: ({B1: 1} if i / (n - 1) < .35 else ({B2: 1} if i / (n - 1) > .6 else {B1: .5, B2: .5}))
        defs = [
         [(.05, .10, .72), (.10, .20, .80), (.14, .24, .82), (.20, .32, .84), (.26, .40, .80), (.30, .46, .72)],
         [(.07, .12, .64), (.15, .22, .58), (.21, .33, .46), (.24, .43, .30), (.25, .50, .16)],
         [(.03, .10, .74), (.07, .18, .86), (.10, .26, .96), (.15, .31, 1.02)],
        ]
        for j, d in enumerate(defs):
            pts = [mirror(p, sg) for p in d]; n = len(pts)
            rad = [(.032 - .005 * j) * (1 - .8 * i / (n - 1)) + .004 for i in range(n)]
            b.sweep(pts, rad, 'M_Branch', bw, 4, caps=(False, True))
            b.blob(V(pts[-1]), .03 - .005 * j, 'M_BranchBulb', {B2: 1}, 5, 3)
        for j in range(2):
            b.blob(V(mirror((.08 + .05 * j, .14 + .06 * j, .70 + .1 * j), sg)), .022, 'M_BranchBulb', {B1: 1}, 4, 3)
    # tiny roots on nape
    for sx in (-1, 1):
        pts = [(sx * .03, -.08, .74), (sx * .05, -.04, .82), (sx * .07, 0, .90)]
        # skip
    return rng

def build_cloth(b, variant, rng):
    if variant == 'LabCoat':
        path = [(0, -.06, .73), (0, -.05, .63), (0, -.01, .52), (0, .03, .40), (0, .05, .29), (0, .06, .22)]
        rad = [(.185, .175), (.20, .185), (.195, .18), (.21, .19), (.235, .20), (.25, .21)]
        rings = b.sweep(path, rad, 'M_Coat', None, 10, caps=(False, False), cloth=True)
        for v in rings[-1]: v.co.z -= rng.uniform(0, .09); v.co.x *= rng.uniform(.95, 1.08)
        for v in rings[-2]: v.co.z -= rng.uniform(0, .02)
        faces = [f for f in b.bm.faces if f.calc_center_median().y < -.17 and abs(f.calc_center_median().x) < .04 and f.calc_center_median().z < .56]
        bmesh.ops.delete(b.bm, geom=faces, context='FACES')
        for sg in (1, -1):
            ap = arm_pts(sg)[:4]
            r = b.sweep(ap, [.095, .085, .078, .072], 'M_Coat', None, 8, caps=(False, False), cloth=True)
            for v in r[-1]: v.co.z -= rng.uniform(0, .06)
        b.sweep([(0, -.11, .735), (0, -.13, .75), (0, -.17, .775)], [(.11, .10), (.12, .11), (.14, .13)], 'M_Coat', None, 10, caps=(False, False), cloth=True)
        # stains
        cv = [v for v in b.cloth if v.is_valid]
        for k in range(16):
            c = rng.choice(cv).co.copy(); rr = rng.uniform(.06, .14)
            col = rng.choice([(.30, .02, .02, 1), (.22, .12, .04, 1), (.10, .16, .06, 1)])
            for v in cv:
                d = (v.co - c).length
                if d < rr:
                    t = min(1, 1.8 * (1 - d / rr)); o = b.vcol.get(v, (1, 1, 1, 1))
                    b.vcol[v] = tuple(o[i] * (1 - t) + col[i] * t for i in range(3)) + (1,)
        for v in cv:
            t = max(0, min(1, (.45 - v.co.z) / .25)) * .45; o = b.vcol.get(v, (1, 1, 1, 1))
            b.vcol[v] = tuple(o[i] * (1 - t) + .2 * t for i in range(3)) + (1,)
    if variant == 'Engineer':
        path = [(0, -.06, .73), (0, -.05, .63), (0, -.01, .52), (0, .03, .42), (0, .04, .36)]
        rad = [(.175, .165), (.19, .17), (.185, .17), (.20, .175), (.20, .18)]
        rings = b.sweep(path, rad, 'M_EngineerSuit', None, 10, caps=(False, False), cloth=True)
        for sg in (1, -1):
            lp = [mirror(p, sg) for p in LEG_PTS]
            r = b.sweep(lp, [.09, .08, .068, .06, .056], 'M_EngineerSuit', None, 8, caps=(False, False), cloth=True)
            for v in r[-1]: v.co.z += rng.uniform(0, .025)
            ap = arm_pts(sg)[:3]
            r = b.sweep(ap, [.085, .075, .068], 'M_EngineerSuit', None, 8, caps=(False, False), cloth=True)
            for v in r[-1]: v.co.z += rng.uniform(0, .03)
        for z, k in ((.50, 0), (.44, 0)):
            pr = [V((math.cos(a) * (.205 if z > .47 else .21), -.01 + math.sin(a) * (.185 if z > .47 else .19), z)) for a in [2 * math.pi * i / 14 for i in range(14)]]
            pr = [V(((p.x), p.y + (.03 if z < .47 else 0) * 0, p.z)) for p in pr]
            b.sweep(pr, .011, 'M_Helmet', None, 4, closed=True, cloth=True)
        for sx in (1, -1):
            b.sweep([(sx * .10, -.14, .74), (sx * .09, -.21, .66), (sx * .08, -.22, .56)], .014, 'M_Helmet', None, 4, caps=(True, True), cloth=True)
        # helmet
        R = Euler((math.radians(-18), 0, 0)).to_matrix()
        res = bmesh.ops.create_uvsphere(b.bm, u_segments=12, v_segments=8, radius=1.0, matrix=Matrix.Translation((0, -.22, .88)) @ R.to_4x4() @ Matrix.Diagonal((.20, .19, .16, 1)))
        low = [v for v in res['verts'] if (R.inverted() @ (v.co - V((0, -.22, .88)))).z < -.01]
        bmesh.ops.delete(b.bm, geom=low, context='VERTS')
        hv = [v for v in res['verts'] if v.is_valid]
        for v in hv: b.w[v] = {'Head': 1}
        for f in {f for v in hv for f in v.link_faces}: f.material_index = b.mi('M_Helmet')
        # helmet face normals: orient outward
        fs = list({f for v in hv for f in v.link_faces}); bmesh.ops.recalc_face_normals(b.bm, faces=fs)
        arc = [V((0, -.22, .88)) + R @ V((0, -.19 * math.cos(a) * 1.0, .16 * math.sin(a) * 1.0)) * 1.04 for a in [math.radians(x) for x in (0, 25, 50, 75, 100, 130, 160, 180)]]
        arc = [V((0, -.22 - 0.19 * 1.04 * math.cos(math.radians(a)) * math.cos(math.radians(-18)) , 0)) * 0 + V((0, 0, 0)) for a in (0,)] and arc
        b.sweep(arc, .012, 'M_Helmet', {'Head': 1}, 4, caps=(True, True))
        b.sweep([V((-.0, -.425, .965)), V((0, -.445, .95))], .022, 'M_Station', {'Head': 1}, 6)

def make_variant_scene(variant):
    clear_scene()
    rig = make_rig()
    b = B(); rng = build_body(b, variant)
    if variant: build_cloth(b, variant, rng)
    ob, tris = b.build('Bloom_Body', BONES)
    ob.parent = rig
    m = ob.modifiers.new('Armature', 'ARMATURE'); m.object = rig
    return rig, ob, tris
