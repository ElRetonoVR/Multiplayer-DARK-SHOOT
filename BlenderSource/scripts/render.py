import bpy, math, sys
from mathutils import Vector
RENDERS='/home/claude/darkshot_out/renders'

def prep_materials():
    for m in bpy.data.materials:
        if not m.use_nodes: continue
        nt = m.node_tree
        p = nt.nodes.get('Principled BSDF')
        if p is None: continue
        c = p.inputs['Base Color'].default_value
        m.diffuse_color = c
        if m.name in ('M_Coat', 'M_EngineerSuit') and not nt.nodes.get('VC'):
            vc = nt.nodes.new('ShaderNodeVertexColor'); vc.name = 'VC'; vc.layer_name = 'Col'
            mx = nt.nodes.new('ShaderNodeMix'); mx.data_type = 'RGBA'; mx.blend_type = 'MULTIPLY'
            mx.inputs[0].default_value = 1
            mx.inputs[6].default_value = c
            nt.links.new(vc.outputs['Color'], mx.inputs[7])
            nt.links.new(mx.outputs[2], p.inputs['Base Color'])

def setup(res=800, samples=40):
    sc = bpy.context.scene
    sc.render.engine = 'CYCLES'; sc.cycles.device = 'CPU'; sc.cycles.samples = samples
    sc.cycles.use_denoising = True; sc.view_settings.view_transform = 'Standard'
    sc.render.resolution_x = sc.render.resolution_y = res
    sc.render.image_settings.file_format = 'PNG'
    w = bpy.data.worlds.new('W'); sc.world = w; w.use_nodes = True
    bg = w.node_tree.nodes['Background']; bg.inputs[0].default_value = (0.045, 0.045, 0.05, 1); bg.inputs[1].default_value = 1.0
    # make the *camera* background gray via film? use shadow-catcher-free approach: big backdrop plane
    return sc

def lights(center, d):
    sc = bpy.context.scene
    def add(name, loc, energy, size, col=(1, 1, 1)):
        l = bpy.data.lights.new(name, 'AREA'); l.energy = energy; l.size = size; l.color = col
        o = bpy.data.objects.new(name, l); sc.collection.objects.link(o)
        o.location = center + Vector(loc) * d
        o.rotation_euler = (center - o.location).to_track_quat('-Z', 'Y').to_euler()
    add('key', (-0.9, -1.1, 0.9), 80 * d * d, d * .8, (1.0, .95, .88))
    add('fill', (1.2, -0.9, 0.2), 25 * d * d, d * 1.0, (.75, .85, 1.0))
    add('rim', (0.2, 1.3, 0.8), 70 * d * d, d * .6, (1, 1, 1))

def backdrop(center, d):
    # dark grey cyclorama so that background reads as dark grey
    sc = bpy.context.scene
    bpy.ops.mesh.primitive_plane_add(size=d * 30, location=(center.x, center.y + d * 6, center.z))
    o = bpy.context.object; o.rotation_euler = (math.pi / 2, 0, 0); o.name = 'BACK'
    m = bpy.data.materials.new('BG'); m.use_nodes = True
    m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (0.05, 0.05, 0.055, 1)
    m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = 1
    o.data.materials.append(m)
    o.visible_shadow = False
    # camera rays see the plane; plane is behind in world, so rotate it with camera instead
    return o

def camera(center, d, az, el, lens=50):
    sc = bpy.context.scene
    cam = bpy.data.cameras.new('cam'); cam.lens = lens
    o = bpy.data.objects.new('cam', cam); sc.collection.objects.link(o)
    a = math.radians(az); e = math.radians(el)
    # az=0: from -Y (front); az=90: from +X
    o.location = center + Vector((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e))) * d
    o.rotation_euler = (center - o.location).to_track_quat('-Z', 'Y').to_euler()
    sc.camera = o
    return o

def shoot(name, center, d, az, el, lens=50, res=800, samples=40):
    for o in [o for o in bpy.data.objects if o.type in ('CAMERA', 'LIGHT') or o.name.startswith('BACK')]:
        bpy.data.objects.remove(o)
    sc = setup(res, samples)
    center = Vector(center)
    cam = camera(center, d, az, el, lens)
    lights(center, d)
    # backdrop plane behind the subject as seen from camera
    bpy.ops.mesh.primitive_plane_add(size=d * 8)
    bd = bpy.context.object; bd.name = 'BACK'
    dirv = (center - cam.location).normalized()
    bd.location = center + dirv * d * 1.2
    bd.rotation_euler = dirv.to_track_quat('Z', 'Y').to_euler()
    m = bpy.data.materials.new('BG'); m.use_nodes = True
    pb = m.node_tree.nodes['Principled BSDF']
    pb.inputs['Base Color'].default_value = (0.045, 0.045, 0.05, 1); pb.inputs['Roughness'].default_value = 1
    bd.data.materials.append(m); bd.visible_shadow = False
    sc.render.filepath = f'{RENDERS}/{name}.png'
    bpy.ops.render.render(write_still=True)
    print('rendered', name)
