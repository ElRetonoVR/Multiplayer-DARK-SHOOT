"""Player_CDC: 4 separate objects (Head, Trunk, LeftHand, RightHand). Blender: Z up, front = -Y
(exports to Unity: +Y up, +Z forward). Character's right side = Blender -X (Unity +X)."""
from mpcommon import *

SU, VI, GE, AC = 'M_AvatarSuit', 'M_AvatarVisor', 'M_AvatarGear', 'M_AvatarAccent'

def build_head():
    b = B()
    # origin = centre of the eyes. helmet shell
    b.blob((0, .035, .045), (.128, .145, .135), SU, None, 14, 9)
    b.blob((0, -.07, 0), (.105, .075, .062), VI, None, 12, 7)                    # visor
    # accent band across the top of the visor, wrapping round the sides of the helmet
    pts = []
    for k in range(11):
        t = pi * (k / 10 - .0)           # 0..pi : right side (-x) -> front -> left side (+x)
        x = -cos(t) * .134; y = .035 - sin(t) * .15
        pts.append(V((x, y, .085)))
    b.sweep(pts, .0055, AC, None, 4)
    # crown stripe front -> back
    pts = []
    for k in range(9):
        t = pi * .5 * (k / 8) + pi * .12 ; a = pi * (.08 + .84 * k / 8)
        pts.append(V((0, .035 - cos(a) * .15, .045 + sin(a) * .141)))
    b.sweep(pts, .0055, AC, None, 4)
    # visor bottom stripe
    pts = [V((x, -.133 + (x / .105) ** 2 * .05 - .003, -.034)) for x in (-.07, -.035, 0, .035, .07)]
    b.sweep(pts, .004, AC, None, 4)
    # respirator
    b.blob((0, -.092, -.078), (.065, .05, .055), GE, None, 10, 6)
    for sx in (-1, 1):
        cyl(b, (sx * .066, -.108, -.085), (sx * .066, -.168, -.085), .032, GE, 8, n0=(1, 0, 0))
        b.box((sx * .128, .03, .02), (.012, .06, .05), GE)                      # ear comms
    # neck seal ring
    b.sweep([(0, .04, -.105), (0, .04, -.15)], [(.088, .092), (.088, .092)], GE, None, 10)
    # antenna
    b.sweep([(-.09, .09, .12), (-.1, .12, .22)], [.005, .003], GE, None, 3)
    return new_obj('Avatar_Head', b)

def build_trunk():
    b = B()
    rings = [(-.03, (.15, .09)), (-.12, (.185, .112)), (-.35, (.17, .108)), (-.55, (.155, .10)), (-.75, (.16, .10))]
    b.sweep([(0, 0, z) for z, _ in rings], [r for _, r in rings], SU, None, 10, n0=(1, 0, 0))
    b.sweep([(0, 0, -.03), (0, 0, .085)], [.055, .055], SU, None, 8)                  # neck
    b.sweep([(0, .01, -.02), (0, .01, .03)], [(.088, .078), (.088, .078)], GE, None, 10)   # collar
    for sx in (-1, 1):
        b.blob((sx * .18, 0, -.05), (.065, .085, .04), GE, None, 8, 4)               # shoulder pads
        bx(b, sx * .18 - .012, sx * .18 + .012, -.05, .05, -.012, -.004, AC)           # pad stripe
        cyl(b, (sx * .17, 0, -.1), (sx * .245, 0, -.1), .05, SU, 8, n0=(0, 1, 0))     # short sleeve stub
        cyl(b, (sx * .237, 0, -.1), (sx * .247, 0, -.1), .054, GE, 8, n0=(0, 1, 0))
        bx(b, sx * .09 - .025, sx * .09 + .025, -.115, .115, -.07, -.045, GE)         # straps
        bx(b, sx * .075 - .008, sx * .075 + .008, -.151, -.143, -.40, -.10, AC)     # front accent lines
    # vest
    bx(b, -.14, .14, -.145, -.105, -.42, -.08, GE)
    bx(b, -.14, .14, .105, .145, -.42, -.08, GE)
    for x in (-.1, 0, .1):
        bx(b, x - .035, x + .035, -.19, -.145, -.36, -.28, GE)                    # pouches
    bx(b, -.008, .008, .143, .151, -.40, -.08, AC)                                 # back accent
    bx(b, -.12, .12, .143, .151, -.22, -.205, AC)
    bx(b, -.165, .165, -.12, .12, -.72, -.66, GE)                                    # belt
    bx(b, -.025, .025, -.125, -.118, -.70, -.675, AC)
    bx(b, .14, .18, -.13, -.1, -.2, -.13, GE)                                      # radio
    # back pack
    bx(b, -.12, .12, .10, .2, -.5, -.12, GE)
    for sx in (-1, 1):
        cyl(b, (sx * .07, .2, -.58), (sx * .07, .2, -.1), .045, GE, 8)
        cyl(b, (sx * .07, .2, -.24), (sx * .07, .2, -.2), .0475, AC, 8)
    return new_obj('Avatar_Trunk', b)

def build_hand(left):
    """Origin = grip point (centre of controller handle, vertical axis Z). m=+1 -> inner side is +X (right hand)."""
    b = B(); m = -1 if left else 1
    b.blob((0, .043, 0), (.043, .022, .052), GE, None, 8, 6)                       # palm / back of hand
    bx(b, -.006, .006, .060, .067, -.045, .045, AC)                                # back-of-hand stripe
    R = .034
    for i in range(4):
        z = .031 - i * .0195; R2 = R * (.93 if i == 3 else 1)
        pts = []
        for k in range(5):
            th = pi * k / 4
            pts.append(V((-m * R2 * cos(th), .02 - (R2 + .02) * sin(th), z)))
        b.sweep(pts, [.0105, .0105, .0098, .0095, .0090], GE, None, 5)
    # thumb along the inner side, pointing forward
    b.sweep([(m * .036, .036, .03), (m * .044, .0, .036), (m * .04, -.04, .034)], [.0125, .011, .0085], GE, None, 5)
    # forearm + cuff
    b.sweep([(0, .07, -.005), (0, .15, 0), (0, .225, .02)], [.029, .032, .036], SU, None, 8)
    b.sweep([(0, .06, -.005), (0, .115, 0)], [.035, .035], GE, None, 8)
    b.sweep([(0, .122, .0), (0, .134, .0)], [.0385, .0385], AC, None, 8)
    return new_obj('Avatar_LeftHand' if left else 'Avatar_RightHand', b)

# rest layout (reference pose) -- object origins in the FBX
REST = {'Avatar_Head': (0, -.07, 1.70), 'Avatar_Trunk': (0, 0, 1.51),
        'Avatar_LeftHand': (.30, -.12, 1.20), 'Avatar_RightHand': (-.30, -.12, 1.20)}

def build_player():
    clear_scene()
    objs = []; info = {}
    for fn, args in ((build_head, ()), (build_trunk, ()), (build_hand, (True,)), (build_hand, (False,))):
        ob, t = fn(*args); ob.location = REST[ob.name]; objs.append(ob); info[ob.name] = t
    return objs, info

if __name__ == '__main__':
    objs, info = build_player()
    print(info, sum(info.values()))
