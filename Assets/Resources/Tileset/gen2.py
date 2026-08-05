from PIL import Image
import math, random, json
from pathlib import Path

T=32
# ── 벽 장식(walldeco) 실측 색에 맞춘 팔레트 ──────────────
FLOOR=(98,122,117)     # 휘도 114 · 오브젝트 청록 계열과 동일 색조
FLOOR_D=(86,108,104)
JOINT=(74,94,91)
WALL =(38,46,48)       # 휘도 44 · walldeco rgb(44,52,54)와 정합
WALL_J=(26,33,35)
MOSS =(56,74,58)
def lum(c): return round(0.299*c[0]+0.587*c[1]+0.114*c[2],1)
def sh(c,d): return tuple(max(0,min(255,int(v+d))) for v in c)
def noise(seed):
    r=random.Random(seed); return [[r.random() for _ in range(T)] for _ in range(T)]

def floor_base(seed=1):
    im=Image.new('RGBA',(T,T)); px=im.load(); n=noise(seed); SH=8
    for y in range(T):
        row=y//SH; off=(row*11)%16
        for x in range(T):
            c=sh(FLOOR,(n[y][x]-0.5)*8)
            if y%SH==0: c=JOINT
            if (x+off)%16==0: c=JOINT
            px[x,y]=(*c,255)
    return im
def cracks(im,seed=7):
    px=im.load(); r=random.Random(seed)
    for _ in range(3):
        x,y=r.randrange(T),r.randrange(T); a=r.uniform(0,6.28)
        for _ in range(r.randint(10,18)):
            x=(x+math.cos(a))%T; y=(y+math.sin(a))%T
            px[int(x),int(y)]=(*sh(JOINT,-12),255); a+=r.uniform(-.5,.5)
    return im
def moss(im,seed=11,amount=.10):
    px=im.load(); r=random.Random(seed)
    for y in range(T):
        for x in range(T):
            if px[x,y][:3]==JOINT and r.random()<.35: px[x,y]=(*MOSS,255)
    for _ in range(int(T*T*amount/9)):
        cx,cy=r.randrange(T),r.randrange(T)
        for dy in(-1,0,1):
            for dx in(-1,0,1):
                if r.random()<.6: px[(cx+dx)%T,(cy+dy)%T]=(*sh(MOSS,r.randint(-8,8)),255)
    return im
def gravel(im,seed=13):
    px=im.load(); r=random.Random(seed)
    for _ in range(70):
        x,y=r.randrange(T),r.randrange(T); d=r.choice([-20,-14,14,20])
        px[x,y]=(*sh(FLOOR,d),255)
        if r.random()<.4: px[(x+1)%T,y]=(*sh(FLOOR,d-4),255)
    return im
def wall_base(seed=3):
    im=Image.new('RGBA',(T,T)); px=im.load(); n=noise(seed); BH=11
    for y in range(T):
        row=y//BH; off=(row*7)%16
        for x in range(T):
            c=sh(WALL,(n[y][x]-0.5)*6)
            if y%BH==0: c=WALL_J
            if (x+off)%16==0: c=WALL_J
            px[x,y]=(*c,255)
    r=random.Random(seed+1)
    for _ in range(int(T*T*.05)):
        px[r.randrange(T),r.randrange(T)]=(*sh(MOSS,-18),255)
    return im
def wall_edge(mask):
    im=wall_base().copy(); px=im.load(); HI=sh(WALL,22); MID=sh(WALL,11)
    if not mask&1:
        for x in range(T): px[x,0]=(*HI,255); px[x,1]=(*MID,255)
    if not mask&2:
        for y in range(T): px[T-1,y]=(*HI,255); px[T-2,y]=(*MID,255)
    if not mask&4:
        for x in range(T): px[x,T-1]=(*sh(WALL,-14),255); px[x,T-2]=(*sh(WALL,-7),255)
    if not mask&8:
        for y in range(T): px[0,y]=(*HI,255); px[1,y]=(*MID,255)
    return im

NAMES={0:'floor_a_base',1:'wall_solid',2:'floor_b_cracked',3:'floor_c_gravel',
       4:'floor_d_moss',5:'floor_e_wet'}
tiles={0:floor_base(),1:wall_base(),2:moss(cracks(floor_base(2)),12),
       3:gravel(floor_base(4)),4:moss(floor_base(5),17,.34)}
f=floor_base(6); pf=f.load(); rr=random.Random(21)
for _ in range(5):
    cx,cy=rr.randrange(T),rr.randrange(T); rad=rr.randint(4,8)
    for dy in range(-rad,rad+1):
        for dx in range(-rad,rad+1):
            if dx*dx+dy*dy<=rad*rad and rr.random()<.75:
                x,y=(cx+dx)%T,(cy+dy)%T; pf[x,y]=(*sh(pf[x,y][:3],-15),255)
tiles[5]=f
MASKNAME=['open','n','e','ne','s','ns','es','nes','w','nw','ew','new','sw','nsw','esw','nesw']
for m in range(16):
    tiles[8+m]=wall_edge(m); NAMES[8+m]=f'wall_edge_{m:02d}_{MASKNAME[m]}'

# 개별 PNG
single=Path('tiles_single'); single.mkdir(exist_ok=True)
for tid,im in tiles.items():
    im.save(single/f'tile_{tid:02d}_{NAMES[tid]}.png')
# 시트
sheet=Image.new('RGBA',(T*8,T*3),(0,0,0,0))
for tid,im in tiles.items(): sheet.paste(im,((tid%8)*T,(tid//8)*T))
sheet.save('temple_tileset.png')

man={'schema':'one_lantern.tileset.v2','tilePx':T,'displayScale':2,'columns':8,'tilecount':24,
 'sheet':'temple_tileset.png','singles':'tiles_single/',
 'autotile':{'formula':'tileid = 8 + mask','bits':{'1':'N','2':'E','4':'S','8':'W'},
             'note':'맵 바깥은 벽으로 취급'},
 'tiles':{str(t):{'name':NAMES[t],'walkable':t in (0,2,3,4,5),
          'noisy': t==3,'file':f'tiles_single/tile_{t:02d}_{NAMES[t]}.png'} for t in tiles},
 'palette':{'floor':list(FLOOR),'wall':list(WALL),'note':'벽 장식(walldeco) 실측 색과 정합'}}
Path('tileset_manifest.json').write_text(json.dumps(man,indent=2,ensure_ascii=False),encoding='utf-8')
for t in [0,1,2,3,4,5]:
    px=tiles[t].load(); s=[0,0,0]
    for y in range(T):
        for x in range(T):
            for i in range(3): s[i]+=px[x,y][i]
    a=tuple(v//(T*T) for v in s)
    print(f"tile {t:2d} {NAMES[t]:<18} rgb{a} 휘도 {lum(a)}")
print(f"\n명도 대비 = {lum((98,122,117))/lum((38,46,48)):.2f} : 1")
