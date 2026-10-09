"""Run with Blender --background --python Tools/build_campaign_props.py -- /path/to/project.
Builds original, editable modular furniture. Dimensions are metres; Z is up in Blender.
"""
import bpy, math, sys
from pathlib import Path
from mathutils import Vector
root = Path(sys.argv[sys.argv.index('--') + 1])
out = root / 'Assets' / 'CampaignArt' / 'Models'
out.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
colors = {'Enamel':(.64,.65,.56,1),'Metal':(.19,.23,.23,1),'Fabric':(.19,.32,.28,1),'Linen':(.72,.73,.64,1),'Wood':(.27,.18,.105,1),'Screen':(.045,.16,.18,1),'Paper':(.81,.77,.62,1),'Leaf':(.13,.24,.12,1),'Pot':(.29,.27,.23,1),'Black':(.035,.045,.045,1)}
mats={}
for name,color in colors.items():
    m=bpy.data.materials.new(name); m.diffuse_color=color; m.use_nodes=True
    bsdf=m.node_tree.nodes.get('Principled BSDF'); bsdf.inputs['Base Color'].default_value=color
    bsdf.inputs['Roughness'].default_value=.42 if name=='Metal' else .72
    bsdf.inputs['Metallic'].default_value=.65 if name=='Metal' else 0
    mats[name]=m
parts=[]
def finish(name,mat):
    o=bpy.context.object; o.name=name; o.data.materials.append(mats[mat]); parts.append(o)
    return o

def box(name,p,s,mat,bevel=.025):
    bpy.ops.mesh.primitive_cube_add(size=1,location=p); o=finish(name,mat); o.dimensions=s
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=o.modifiers.new('Soft edges','BEVEL'); mod.width=bevel; mod.segments=2
        bpy.context.view_layer.objects.active=o; bpy.ops.object.modifier_apply(modifier=mod.name)
        o.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
    return o

def rod(name,a,b,r=.025,mat='Metal',verts=10):
    mid=(Vector(a)+Vector(b))/2; d=Vector(b)-Vector(a)
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=d.length,location=mid)
    o=finish(name,mat); o.rotation_euler=d.to_track_quat('Z','Y').to_euler(); return o

def wheel(x,y,z=.12):
    rod('Caster',(x-.035,y,z),(x+.035,y,z),.09,'Black',12)

def bed():
    box('Chassis',(0,0,.38),(1.05,2.12,.12),'Metal')
    box('Mattress',(0,0,.65),(1.02,2.04,.25),'Linen',.09)
    box('Blanket',(0,-.3,.80),(1.01,1.38,.07),'Fabric',.055)
    box('Pillow',(0,.68,.85),(.75,.43,.13),'Linen',.10)
    for y in [-1.06,1.06]:
        box('End board',(0,y,.87),(1.10,.07,.40),'Enamel',.04)
        for x in [-.45,.45]:
            rod('Leg',(x,y,.13),(x,y,.90),.035); wheel(x,y)
    for x in [-.55,.55]:
        rod('Rail',(x,-.7,1.03),(x,.65,1.03))
        for y in [-.7,0,.65]: rod('Rail support',(x,y,.50),(x,y,1.03),.02)

def chair():
    box('Seat',(0,0,.52),(.55,.53,.13),'Fabric',.08)
    box('Back',(0,.23,.92),(.57,.13,.69),'Fabric',.09)
    rod('Stem',(0,0,.12),(0,0,.47),.055)
    for i in range(5):
        a=i*math.tau/5; x,y=math.cos(a)*.37,math.sin(a)*.37
        rod('Base',(0,0,.15),(x,y,.12)); wheel(x,y,.09)
    for x in [-.34,.34]:
        rod('Arm post',(x,.12,.48),(x,.12,.78)); box('Arm rest',(x,0,.79),(.075,.42,.075),'Black')

def desk():
    box('Desktop',(0,0,.79),(1.8,.85,.10),'Wood',.035)
    box('Drawers',(-.62,0,.38),(.48,.7,.73),'Enamel')
    for z in [.22,.43,.64]:
        box('Drawer',(-.62,-.362,z),(.44,.025,.17),'Enamel',.01)
        rod('Pull',(-.72,-.39,z),(-.52,-.39,z),.012)
    for y in [-.30,.30]: rod('Desk leg',(.72,y,.06),(.72,y,.75),.035)
    box('Monitor',(0,.15,1.15),(.65,.065,.40),'Black')
    box('Display',(0,.11,1.15),(.58,.012,.33),'Screen',.01)
    rod('Monitor support',(0,.17,.83),(0,.17,1.0),.04)
    box('Keyboard',(0,-.23,.86),(.47,.17,.026),'Black',.01)
    for i in range(4): box('Document',(.55,-.10+i*.025,.85+i*.004),(.28,.34,.006),'Paper',.001)

def cabinet():
    box('Storage',(0,0,.66),(.90,.48,1.30),'Enamel')
    for x in [-.23,.23]:
        box('Door',(x,-.252,.68),(.42,.025,1.17),'Enamel',.015)
        rod('Handle',(x+.10,-.29,.60),(x+.10,-.29,.77),.013)
    box('Counter',(0,0,1.33),(.96,.54,.055),'Wood')

def trolley():
    for z in [.25,.75]: box('Tray',(0,0,z),(.65,.46,.05),'Enamel')
    for x in [-.28,.28]:
        for y in [-.19,.19]: rod('Upright',(x,y,.12),(x,y,.86),.022); wheel(x,y,.10)
    box('Monitor',(0,.05,1.0),(.36,.22,.38),'Enamel')
    box('Display',(0,-.067,1.02),(.29,.014,.25),'Screen')
    rod('IV pole',(.42,.10,.08),(.42,.10,1.90),.018)
    rod('Hook',(.26,.10,1.88),(.60,.10,1.88),.018)
    box('IV bag',(.55,.10,1.64),(.15,.045,.25),'Linen',.025)

def curtain():
    for x in [-1.1,1.1]: rod('Stand',(x,0,.05),(x,0,2.0),.022)
    rod('Curtain track',(-1.1,0,2),(1.1,0,2),.027)
    vs=[]; fs=[]; n=64
    for i in range(n+1):
        x=-1.08+2.16*i/n; y=.055*math.sin(i*math.pi/2)
        vs.extend([(x,y,.25),(x,y,1.92)])
        if i<n: k=i*2; fs.append((k,k+2,k+3,k+1))
    me=bpy.data.meshes.new('Pleated fabric'); me.from_pydata(vs,[],fs); me.materials.append(mats['Fabric'])
    o=bpy.data.objects.new('Curtain fabric',me); bpy.context.collection.objects.link(o); parts.append(o)
    sol=o.modifiers.new('Fabric thickness','SOLIDIFY'); sol.thickness=.008

def plant():
    bpy.ops.mesh.primitive_cone_add(vertices=16,radius1=.20,radius2=.27,depth=.46,location=(0,0,.23)); finish('Planter','Pot')
    for i in range(12):
        a=i*2.4; h=.70+(i%4)*.18
        tip=(math.cos(a)*.38,math.sin(a)*.38,h)
        rod('Stem',(0,0,.42),tip,.009,'Leaf')
        bpy.ops.mesh.primitive_uv_sphere_add(segments=10,ring_count=6,location=tip)
        o=finish('Leaf','Leaf'); o.scale=(.09,.25,.027); o.rotation_euler=(.5,.3,a)

def bench():
    for x in [-.66,0,.66]:
        box('Seat',(x,0,.48),(.57,.52,.12),'Fabric',.07)
        box('Back',(x,.22,.85),(.57,.09,.61),'Fabric',.07)
    rod('Beam',(-.90,0,.36),(.90,0,.36),.045)
    for x in [-.72,.72]:
        for y in [-.19,.19]: rod('Leg',(x,y,.06),(x,y,.41),.03)


def reception():
    box('Counter front',(0,0,.60),(2.5,.65,1.20),'Fabric')
    box('Return',(-.95,-.7,.60),(.60,1.5,1.20),'Fabric')
    box('Countertop',(0,0,1.23),(2.65,.81,.09),'Enamel')
    box('Return top',(-.95,-.7,1.23),(.76,1.55,.09),'Enamel')
    box('Foot rail',(0,-.34,.13),(2.4,.03,.12),'Metal')
    box('Monitor',(.35,0,1.57),(.62,.08,.36),'Black')
    rod('Screen stand',(.35,0,1.28),(.35,0,1.43),.04)
    box('Display',(.35,-.048,1.57),(.56,.01,.30),'Screen')
    for i in range(3): box('Stacked records',(-.55,0,1.30+i*.055),(.38,.30,.045),'Paper')

def sink():
    box('Pedestal',(0,0,.43),(.50,.42,.86),'Enamel',.06)
    box('Basin base',(0,0,.88),(.70,.53,.09),'Linen',.045)
    for x in [-.31,.31]: box('Basin rim',(x,0,.97),(.08,.53,.12),'Linen')
    for y in [-.23,.23]: box('Basin rim',(0,y,.97),(.61,.07,.12),'Linen')
    rod('Tap',(0,.19,.98),(0,.19,1.17),.023)
    rod('Spout',(0,.19,1.17),(0,.03,1.17),.023)

for index,(name,build) in enumerate([('HospitalBed',bed),('OfficeChair',chair),('OfficeDesk',desk),('Cabinet',cabinet),('MedicalTrolley',trolley),('PrivacyCurtain',curtain),('Plant',plant),('WaitingBench',bench),('ReceptionCounter',reception),('Washbasin',sink)]):
    parts=[]; build()
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts: o.select_set(True)
    bpy.context.view_layer.objects.active=parts[0]
    bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,object_types={'MESH'},apply_unit_scale=True,bake_anim=False,axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True)
    for o in parts: o.location.x += index*3
(root / 'Tools' / 'Art').mkdir(exist_ok=True)
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(root/'Tools'/'Art'/'FurnitureLibrary.blend'))
print('CAMPAIGN_PROPS_COMPLETE')
