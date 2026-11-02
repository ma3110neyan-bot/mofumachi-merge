"""Offline annotation measurement. Never edits artwork or applies runtime fitting."""
import importlib.util
import json
import math
from pathlib import Path
import numpy as np

ROOT=Path(__file__).resolve().parents[3]
spec=importlib.util.spec_from_file_location('preview',ROOT/'scripts/render-head-restoration-jpegs.py')
preview=importlib.util.module_from_spec(spec);spec.loader.exec_module(preview)
REFERENCE=ROOT/'Tests/Fixtures/character-head-calibration-2026-11-02.json'
record=json.loads(REFERENCE.read_text())
if 'horizontal_review' in record:
    raise SystemExit('Refusing to replace individually reviewed horizontal axes with the earlier collar-only calibration.')
atlas=preview.load_original(ROOT/'Assets/Mofumachi/Resources/Mofumachi/CharacterBodyAtlas.png')
bounds=[[17,12,266,324],[284,12,230,324],[524,20,221,316],[771,5,237,331],[1005,31,243,306],[1263,14,254,323]]
characters={name:preview.Character(name,ROOT/'References/Review/character-head-restore-preview-2026-11-02/mesh-data',atlas,bounds) for name in preview.NAMES}

ALPHAS={}
def alpha_at(chip, x, y):
    if id(chip) not in ALPHAS:ALPHAS[id(chip)]=(chip,np.asarray(chip)[:,:,3]/255.0)
    a=ALPHAS[id(chip)][1]
    inside=(x>=0)&(y>=0)&(x<chip.width-1)&(y<chip.height-1)
    ix=np.clip(np.floor(x).astype(int),0,chip.width-2);iy=np.clip(np.floor(y).astype(int),0,chip.height-2)
    fx=x-ix;fy=y-iy
    return np.where(inside,a[iy,ix]*(1-fx)*(1-fy)+a[iy,ix+1]*fx*(1-fy)+a[iy+1,ix]*(1-fx)*fy+a[iy+1,ix+1]*fx*fy,0)

def points(part, values, radius):
    w,h=part['crop'][2:];p=part['pivot'];s=part['scale'];o=part.get('offset',[0,0])
    rows=[]
    for x,y in values:
        for dx in [-radius,0,radius]:
            for dy in [-radius,0,radius]:rows.append([(x+dx+.5-p[0]*w)*s+o[0],(y+dy+.5-p[1]*h)*s+o[1]])
    return np.asarray(rows)

errors=[]
for row in record['rows']:
    name,d=row['character'],row['direction'];character=characters[name]
    frame=character.frames['idle',d];body_index=next(i for i,p in enumerate(frame['parts']) if p['id']=='body');head_index=next(i for i,p in enumerate(frame['parts']) if p['id']=='head')
    body=character.parts[d,body_index];head=character.parts[d,head_index]
    for x,y in row['collar_cover']+row['garment_clear']:
        if np.asarray(character.chips[d,body_index])[y,x,3]<128:print(f'Landmark outside body {name}/{d}: {x},{y}',flush=True);errors.append((name,d,x,y))
    cover=points(body,row['collar_cover'],1);clear=points(body,row['garment_clear'],1)
    head_variants=[(head,character.chips[d,head_index])]
    if name!='noah' and d!='back':
        closed=character.layout['views'][d]['closedHead'];src=character.layout['sources'][d]
        original=preview.load_original(character.layout_path.parent/src['file'],src['sha256'])
        head_variants.append((closed,preview.part_chip(original,closed)))
    base=np.asarray([20 if d=='right' else -20 if d=='left' else 0,-238])+np.asarray(head.get('offset',[0,0]))
    angles=np.asarray([-.05,-.04,-.02,-.006,0,.006,.02,.04,.05])
    def check(origin):
        for part,chip in head_variants:
            cosine,sine=np.cos(angles)[:,None],np.sin(angles)[:,None]
            def sample(p):
                p=p-origin;x=cosine*p[:,0]+sine*p[:,1];y=-sine*p[:,0]+cosine*p[:,1]
                return alpha_at(chip,x/part['scale']+part['pivot'][0]*part['crop'][2]-.5,y/part['scale']+part['pivot'][1]*part['crop'][3]-.5)
            if sample(cover).min()<.92 or sample(clear).max()>.08:return False
        return True
    found=None
    for dx in sorted(np.arange(-60,60.01,1),key=lambda n:(abs(n),n)):
        for dy in sorted(np.arange(-65,85.01,.5),key=lambda n:(abs(n),n)):
            origin=base+[dx,dy]
            if check(origin):found=origin.tolist();break
        if found is not None:break
    if found is None:
        print(f'No safe head-only calibration for {name}/{d}',flush=True);errors.append((name,d));continue
    row['head_local']=found;row['original_head_local']=base.tolist();row['correction']=(np.asarray(found)-base).tolist();row['coverage_angles']=angles.tolist()
    print(name,d,'head=',found,'correction=',row['correction'])
if errors:raise RuntimeError(str(errors))
REFERENCE.write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n')
