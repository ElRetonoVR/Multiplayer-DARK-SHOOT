import bpy, bmesh, math, random, os
from mathutils import Vector, Matrix, Euler
from mathutils.kdtree import KDTree

OUT = os.environ.get('DS_OUT', '/home/claude/out')
MODELS = OUT + '/Assets/DarkShot/Models'
BLEND = OUT + '/BlenderSource/blend'
RENDERS = OUT + '/BlenderSource/renders'
for d in (MODELS, BLEND, RENDERS):
    os.makedirs(d, exist_ok=True)

# name: (hex, emission strength, roughness)
MATS = {
 'M_BloomSkin': ('C9B8A8', 0, 0.35), 'M_BloomVeins': ('5A1E2A', 0.35, 0.4),
 'M_BloomEyes': ('E8E4D0', 0, 0.05), 'M_BloomMouth': ('3A0A10', 0, 0.3),
 'M_Coat': ('D8D8D0', 0, 0.9), 'M_EngineerSuit': ('C25A1A', 0, 0.85),
 'M_Helmet': ('D9B21E', 0, 0.4), 'M_Battery': ('2A2E33', 0, 0.4),
 'M_BatteryGlow': ('3CFF9A', 4.0, 0.3), 'M_Station': ('3B3F44', 0, 0.55),
 'M_Branch': ('1E1418', 0, 0.6), 'M_BranchBulb': ('7A2A3A', 0.5, 0.35),
}

def s2l(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4

def hexlin(h):
    return tuple(s2l(int(h[i:i + 2], 16) / 255) for i in (0, 2, 4))

def get_mat(name):
    m = bpy.data.materials.get(name)
    if m: return m
    hx, em, rough = MATS[name]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    p = m.node_tree.nodes.get('Principled BSDF')
    lin = hexlin(hx)
    p.inputs['Base Color'].default_value = (*lin, 1)
    p.inputs['Roughness'].default_value = rough
    if em > 0:
        p.inputs['Emission Color'].default_value = (*lin, 1)
        p.inputs['Emission Strength'].default_value = em
    m.diffuse_color = (*lin, 1)
    return m

def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.unit_settings.system = 'METRIC'; sc.unit_settings.scale_length = 1.0
    sc.render.fps = 30
    return sc

V = Vector

class B:
    """bmesh builder with per-vertex bone weights, materials, vertex colors"""
    def __init__(s):
        s.bm = bmesh.new(); s.w = {}; s.mats = []; s.vcol = {}; s.cloth = []
    def mi(s, n):
        if n not in s.mats: s.mats.append(n)
        return s.mats.index(n)
    def _fin(s, verts, mat, wts, cloth=False):
        faces = list({f for v in verts for f in v.link_faces})
        for f in faces: f.material_index = s.mi(mat)
        bmesh.ops.recalc_face_normals(s.bm, faces=faces)
        for v in verts:
            if cloth: s.cloth.append(v)
            elif wts is not None:
                s.w[v] = dict(wts(v.co) if callable(wts) else wts)
        return faces
    def blob(s, c, r, mat, wts=None, segs=8, rings=6, rot=None, cloth=False):
        if not hasattr(r, '__len__'): r = (r, r, r)
        M = Matrix.Translation(V(c)) @ (rot.to_4x4() if rot is not None else Matrix.Identity(4)) @ Matrix.Diagonal(V((r[0], r[1], r[2], 1)))
        res = bmesh.ops.create_uvsphere(s.bm, u_segments=segs, v_segments=rings, radius=1.0, matrix=M)
        s._fin(res['verts'], mat, wts, cloth); return res['verts']
    def box(s, c, size, mat, wts=None, rot=None):
        M = Matrix.Translation(V(c)) @ (rot.to_4x4() if rot is not None else Matrix.Identity(4)) @ Matrix.Diagonal(V((size[0], size[1], size[2], 1)))
        res = bmesh.ops.create_cube(s.bm, size=1.0, matrix=M)
        s._fin(res['verts'], mat, wts); return res['verts']
    def sweep(s, pts, radii, mat, wts=None, sides=6, closed=False, caps=(True, True), cloth=False, n0=None):
        pts = [V(p) for p in pts]; n = len(pts); rings = []; prev = None
        for i, p in enumerate(pts):
            if closed: t = pts[(i + 1) % n] - pts[(i - 1) % n]
            else: t = pts[min(i + 1, n - 1)] - pts[max(i - 1, 0)]
            t.normalize()
            if prev is None:
                a = V(n0) if n0 is not None else (V((1, 0, 0)) if abs(t.x) < 0.9 else V((0, 1, 0)))
                nn = (a - a.dot(t) * t).normalized()
            else:
                nn = (prev - prev.dot(t) * t).normalized()
            prev = nn; bn = t.cross(nn)
            r = radii[i] if hasattr(radii, '__len__') else radii
            ra, rb = (r if hasattr(r, '__len__') else (r, r))
            ring = []
            for k in range(sides):
                ang = 2 * math.pi * k / sides
                ring.append(s.bm.verts.new(p + nn * math.cos(ang) * ra + bn * math.sin(ang) * rb))
            rings.append(ring)
        nseg = n if closed else n - 1
        for i in range(nseg):
            a = rings[i]; b = rings[(i + 1) % n]
            for k in range(sides):
                s.bm.faces.new((a[k], a[(k + 1) % sides], b[(k + 1) % sides], b[k]))
        if not closed:
            if caps[0]: s.bm.faces.new(rings[0][::-1])
            if caps[1]: s.bm.faces.new(rings[-1])
        verts = [v for r in rings for v in r]
        s._fin(verts, mat, None, cloth)
        if not cloth and wts is not None:
            for i, r in enumerate(rings):
                for v in r:
                    w = wts(i, n) if callable(wts) else (wts[i] if isinstance(wts, list) else wts)
                    s.w[v] = dict(w)
        return rings
    def build(s, name, bones, nearest_exclude=('BranchL', 'BranchR', 'Tongue', 'Jaw')):
        bm = s.bm
        # remove loose verts
        loose = [v for v in bm.verts if not v.link_faces]
        for v in loose: s.w.pop(v, None)
        if loose: bmesh.ops.delete(bm, geom=loose, context='VERTS')
        s.cloth = [v for v in s.cloth if v.is_valid]
        if s.cloth:
            kd = KDTree(len(s.w)); lst = list(s.w.keys())
            for i, v in enumerate(lst):
                if v.is_valid and not any(k.startswith(nearest_exclude) for k in s.w[v]): kd.insert(v.co, i)
            kd.balance()
            for v in s.cloth:
                acc = {}
                for co, i, d in kd.find_n(v.co, 3):
                    wt = 1.0 / (d + 0.01)
                    for k, x in s.w[lst[i]].items(): acc[k] = acc.get(k, 0) + x * wt
                if v.co.z < 0.40:
                    hips = {'Hips': 0.5 * sum(acc.values())}
                    for k, x in hips.items(): acc[k] = acc.get(k, 0) + x
                s.w[v] = acc
        bmesh.ops.triangulate(bm, faces=bm.faces[:], quad_method='BEAUTY', ngon_method='BEAUTY')
        if s.vcol or s.cloth:
            layer = bm.loops.layers.color.get('Col') or bm.loops.layers.color.new('Col')
            for f in bm.faces:
                for l in f.loops: l[layer] = s.vcol.get(l.vert, (1, 1, 1, 1))
        bm.verts.ensure_lookup_table()
        me = bpy.data.meshes.new(name); bm.to_mesh(me)
        ob = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(ob)
        for n in s.mats: me.materials.append(get_mat(n))
        for p in me.polygons: p.use_smooth = False
        if s.w:
            for b in bones: ob.vertex_groups.new(name=b)
            for v, w in s.w.items():
                if not v.is_valid: continue
                items = sorted(w.items(), key=lambda kv: -kv[1])[:4]
                tot = sum(x for _, x in items)
                for k, x in items:
                    if x > 1e-4: ob.vertex_groups[k].add([v.index], x / tot, 'REPLACE')
        tris = len(me.loop_triangles) if False else sum(len(p.vertices) - 2 for p in me.polygons)
        bm.free()
        return ob, tris

def tri_count(ob):
    return sum(len(p.vertices) - 2 for p in ob.data.polygons)

def _patch_fbx_take_names():
    # Blender names FBX takes '<Object>|<Action>'; Unity would show clips as 'BloomRig|Walk'. Use plain action names.
    from io_scene_fbx import export_fbx_bin as E
    if getattr(E, '_ds_patched', False): return
    orig = E.get_blenderID_name
    def name_fn(bid):
        if isinstance(bid, tuple) and len(bid) == 2 and isinstance(bid[1], bpy.types.Action):
            return bid[1].name
        return orig(bid)
    E.get_blenderID_name = name_fn; E._ds_patched = True

def export_fbx(path, objs, anim=False):
    _patch_fbx_take_names()
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z', axis_up='Y', bake_anim=anim, bake_anim_use_all_actions=anim,
        bake_anim_use_nla_strips=False, add_leaf_bones=False, mesh_smooth_type='FACE',
        object_types={'ARMATURE', 'MESH'}, use_mesh_modifiers=True, bake_anim_force_startend_keying=True)
