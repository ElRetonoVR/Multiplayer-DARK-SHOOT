import sys; sys.path.insert(0, '/home/claude/blender_work')
from bloom import *
from math import sin, cos, pi, radians as rad

SIDES = (('L', 1), ('R', -1))
TONGUES = ['Tongue_1', 'Tongue_2', 'Tongue_3']

def zero_pose():
    return {}

def idle_pose(f, N=60):
    p = 2 * pi * f / N; P = {}
    P['Hips.loc'] = (0, .004 * sin(p), 0)
    P['Hips'] = (1 * sin(p + .3), 1.5 * sin(2 * p), 1.5 * sin(p))
    P['Spine'] = (2 + 1.5 * sin(p - .4), 0, 1.2 * sin(p + .5))
    P['Chest'] = (3 + 2.5 * sin(p), 0, 1.5 * sin(p + 1))
    P['Neck'] = (3 + 1 * sin(p - .6), 2 * sin(p + .5), 0)
    P['Head'] = (4 + 2.5 * sin(p - .9), 3 * sin(2 * p), 4 * sin(p + .5))
    P['Jaw'] = (8 + 4 * sin(p - .5), 0, 0)
    for k, t in enumerate(TONGUES):
        P[t] = (5 * sin(2 * p - k * .8), 0, 6 * sin(p + k * .7))
    for sd, sg in SIDES:
        ph = 0 if sg > 0 else 1.7
        P['UpperArm_' + sd] = (3 * sin(p + .3 + ph), 0, -sg * (4 + 2 * sin(p + ph)))
        P['LowerArm_' + sd] = (-6 + 3 * sin(p + .8 + ph), 0, 0)
        P['Hand_' + sd] = (4 * sin(2 * p + ph), 0, 0)
        P['Claw_' + sd] = (-6 + 8 * sin(2 * p + 1 + ph), 0, 0)
        P['UpperLeg_' + sd] = (2 * sin(p + ph), 0, 0)
        P['LowerLeg_' + sd] = (2 + 1 * sin(p + ph), 0, 0)
        P['Branch' + sd + '_1'] = (8 * sin(2 * p + .5 + ph), 0, 12 * sin(p + ph))
        P['Branch' + sd + '_2'] = (12 * sin(2 * p - .4 + ph), 0, 18 * sin(p - .9 + ph))
    return P

def walk_pose(f, N=36):
    p = 2 * pi * f / N; P = {}
    P['Hips.loc'] = (.02 * sin(p), .01 * cos(2 * p) - .012, 0)
    P['Hips'] = (2, 7 * sin(p), 5 * sin(p))
    P['Spine'] = (4, -5 * sin(p), -3 * sin(p))
    P['Chest'] = (4 + 3 * cos(2 * p), -7 * sin(p), -3 * sin(p))
    P['Neck'] = (0, 3 * sin(p + .5), 0)
    P['Head'] = (-16 + 4 * sin(2 * p), 5 * sin(p + .5), 7 * sin(p + 1))
    P['Jaw'] = (14 + 6 * sin(2 * p + 1), 0, 0)
    for k, t in enumerate(TONGUES):
        P[t] = (6 * sin(2 * p - k * .9), 0, 5 * sin(p - k * .6))
    P['UpperLeg_L'] = (-26 * sin(p), 0, 0)
    P['LowerLeg_L'] = (6 + 38 * max(0, cos(p)), 0, 0)
    P['UpperLeg_R'] = (-14 * sin(p + pi) + 6, 0, 0)
    P['LowerLeg_R'] = (8 + 18 * max(0, cos(p + pi)), 0, 0)
    P['UpperArm_L'] = (10 + 22 * sin(p), 0, 5)
    P['LowerArm_L'] = (-10 - 8 * sin(p + .5), 0, 0)
    P['UpperArm_R'] = (-18 - 16 * sin(p), 0, -5)
    P['LowerArm_R'] = (-25 - 10 * sin(p + .5), 0, 0)
    for sd, sg in SIDES:
        P['Hand_' + sd] = (-6 * sin(2 * p + sg), 0, 0)
        P['Claw_' + sd] = (-10 + 12 * sin(2 * p + 1 + sg), 0, 0)
        ph = 0 if sg > 0 else 2
        P['Branch' + sd + '_1'] = (10 * sin(2 * p + ph), 0, 10 * sin(p + ph))
        P['Branch' + sd + '_2'] = (14 * sin(2 * p - .8 + ph), 0, 16 * sin(p - 1 + ph))
    return P

def attack_keys():
    def base(**k):
        P = {}
        for n, v in k.items(): P[n.replace('__', '.')] = v
        return P
    def arms(x, lx, hx, cx, z):
        d = {}
        for sd, sg in SIDES:
            d['UpperArm_' + sd] = (x, 0, -sg * z); d['LowerArm_' + sd] = (lx, 0, 0)
            d['Hand_' + sd] = (hx, 0, 0); d['Claw_' + sd] = (cx, 0, 0)
        return d
    def legs(ul, ll, ur, lr):
        return {'UpperLeg_L': (ul, 0, 0), 'LowerLeg_L': (ll, 0, 0), 'UpperLeg_R': (ur, 0, 0), 'LowerLeg_R': (lr, 0, 0)}
    def br(a, b2):
        d = {}
        for sd, sg in SIDES:
            d['Branch' + sd + '_1'] = (a, 0, 0); d['Branch' + sd + '_2'] = (b2, 0, 0)
        return d
    def tongue(sy, r1, r2, r3):
        return {'Tongue_1': (r1, 0, 0), 'Tongue_2': (r2, 0, 0), 'Tongue_3': (r3, 0, 0), 'sy': sy}
    K = {}
    K[0] = {}
    K[4] = dict(**{'Hips.loc': (0, -.07, -.06), 'Hips': (-6, 0, 0), 'Spine': (-4, 0, 0), 'Chest': (-8, 0, 0), 'Neck': (-2, 0, 0), 'Head': (2, 0, 0), 'Jaw': (4, 0, 0)},
                **arms(45, -30, 0, -5, 10), **legs(-20, 35, -20, 35), **br(-25, -25), **tongue(1.0, 0, 0, 0))
    K[8] = dict(**{'Hips.loc': (0, -.01, .12), 'Hips': (8, 0, 0), 'Spine': (8, 0, 0), 'Chest': (12, 0, 0), 'Neck': (-4, 0, 0), 'Head': (-22, 0, 0), 'Jaw': (38, 0, 0)},
                **arms(-95, -20, -15, -25, 28), **legs(-35, 40, 20, 10), **br(-35, -30), **tongue(1.35, -25, -35, -30))
    K[11] = dict(**{'Hips.loc': (0, -.015, .10), 'Hips': (6, 0, 0), 'Spine': (6, 0, 0), 'Chest': (10, 0, 0), 'Neck': (-3, 0, 0), 'Head': (-18, 0, 0), 'Jaw': (36, 0, 0)},
                 **arms(-85, -25, -12, -22, 24), **legs(-30, 35, 15, 12), **br(-25, -22), **tongue(1.3, -22, -30, -28))
    K[16] = dict(**{'Hips.loc': (0, 0, .06), 'Hips': (3, 0, 0), 'Spine': (3, 0, 0), 'Chest': (4, 0, 0), 'Head': (0, 0, 0), 'Jaw': (20, 0, 0)},
                 **arms(-40, -20, -6, -10, 8), **legs(-12, 15, 8, 8), **br(-10, -8), **tongue(1.2, -3, -10, -12))
    K[20] = dict(**{'Hips.loc': (0, 0, .02), 'Hips': (2, 0, 0), 'Spine': (2, 0, 0), 'Chest': (3, 0, 0), 'Jaw': (6, 0, 0)},
                 **arms(-10, -10, -2, -4, 4), **legs(-4, 5, 3, 3), **br(-3, -3), **tongue(1.05, -1, -3, -3))
    K[24] = {}
    return K

def key_all(rig, f, P):
    for pb in rig.pose.bones:
        n = pb.name
        r = P.get(n, (0, 0, 0))
        pb.rotation_euler = (rad(r[0]), rad(r[1]), rad(r[2]))
        pb.keyframe_insert('rotation_euler', frame=f, group=n)
        if n == 'Hips':
            pb.location = P.get('Hips.loc', (0, 0, 0)); pb.keyframe_insert('location', frame=f, group=n)
        if n in TONGUES:
            sy = P.get('sy', 1.0); pb.scale = (1, sy, 1); pb.keyframe_insert('scale', frame=f, group=n)

def make_actions(rig):
    for pb in rig.pose.bones: pb.rotation_mode = 'XYZ'
    rig.animation_data_create()
    def new(name, n, step, fn, loop):
        act = bpy.data.actions.new(name); act.use_fake_user = True
        rig.animation_data.action = act
        for f in range(0, n + 1, step): key_all(rig, f, fn(f))
        act.use_frame_range = True; act.frame_start = 0; act.frame_end = n
        act.use_cyclic = loop
        return act
    a1 = new('Idle', 60, 3, idle_pose, True)
    a2 = new('Walk', 36, 3, walk_pose, True)
    K = attack_keys()
    a3 = bpy.data.actions.new('Attack'); a3.use_fake_user = True
    rig.animation_data.action = a3
    for f, P in K.items():
        if 'sy' not in P: pass
        key_all(rig, f, P)
    a3.use_frame_range = True; a3.frame_start = 0; a3.frame_end = 24; a3.use_cyclic = False
    for pb in rig.pose.bones:
        pb.rotation_euler = (0, 0, 0); pb.location = (0, 0, 0); pb.scale = (1, 1, 1)
    rig.animation_data.action = a1
    return a1, a2, a3

def set_action_frame(rig, name, f):
    rig.animation_data.action = bpy.data.actions[name]
    bpy.context.scene.frame_set(f)
