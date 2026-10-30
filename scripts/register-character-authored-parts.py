"""Register isolated source islands onto the unchanged Noah bones.

This crops/records source pixels; it does not redraw, recolor, mirror or
overwrite the raw source PNGs. Inspect the assembled character separately.
"""
import argparse
import hashlib
import json
from pathlib import Path
import numpy as np
from PIL import Image
from scipy import ndimage

parser = argparse.ArgumentParser()
parser.add_argument('character', choices=['lulu', 'pom', 'fio', 'miel'])
parser.add_argument('--head-height', type=float, default=340)
parser.add_argument('--tail-height', type=float, default=200)
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
art = root / f'References/Characters/{args.character}-authored-parts-2026-10-30'
front = {'lulu':[284,12,230,324], 'pom':[524,20,221,316],
         'fio':[771,5,237,331], 'miel':[1005,31,243,306]}[args.character]
layout = dict(version=1, characterId=args.character,
              method='unchanged NoahAuthoredMotion with rigid source registration',
              front_crop=front, formalWidth=front[2], formalHeight=front[3],
              sources={}, views={})
for direction in ['left','right','back']:
    source = art / f'parts-{direction}.png'
    image = Image.open(source)
    labels, _ = ndimage.label(np.array(image.getchannel('A')) > 40)
    components = [(i+1,s) for i,s in enumerate(ndimage.find_objects(labels))
                  if s is not None and np.sum(labels[s] == i+1) > 1000]
    if len(components) != 8:
        raise ValueError(f'{direction}: expected eight isolated islands, got {len(components)}')
    rows = [sorted([c for c in components if ((c[1][0].start+c[1][0].stop)/2 < image.height/2) == top],
                   key=lambda c:c[1][1].start) for top in [True,False]]
    if any(len(row) != 4 for row in rows):
        raise ValueError(f'{direction}: expected two rows of four')
    parts = {}
    names = ['head','body','farArm','nearArm','farLeg','nearLeg','tail','closedHead']
    for name,(component,s) in zip(names, rows[0]+rows[1]):
        y,x = max(0,s[0].start-4),max(0,s[1].start-4)
        w,h = min(image.width,s[1].stop+4)-x,min(image.height,s[0].stop+4)-y
        head = name in ['head','closedHead']
        height = args.head_height if head else args.tail_height if name=='tail' else 230 if name=='body' else 125
        part = dict(id='head' if head else name,crop=[x,y,w,h],
                    pivot=[.5,1 if head else .9 if name=='tail' else .1 if name.endswith('Arm') else 1],
                    scale=height/h,rotation=0,offset=[0,15] if head else [0,0])
        if name=='farArm': part['rotation'] = -.7 if direction=='left' else .7 if direction=='right' else 0
        # An alpha gutter selects only this island if its bounding rectangle
        # includes a corner of a neighboring source island.
        mask=ndimage.binary_dilation(labels==component,iterations=4)[y:y+h,x:x+w]
        neighbors=(labels[y:y+h,x:x+w]!=0)&(labels[y:y+h,x:x+w]!=component)
        if np.any(neighbors):
            part['maskRows']=[dict(min=int(a.min()),max=int(a.max())) if (a:=np.flatnonzero(row)).size
                              else dict(min=-1,max=-1) for row in mask]
        parts[name]=part
    order=['tail','farLeg','nearLeg','farArm','body','nearArm','head'] if direction!='back' else ['farArm','farLeg','nearLeg','body','tail','nearArm','head']
    layout['sources'][direction]=dict(file=source.name,width=image.width,height=image.height,
                                     sha256=hashlib.sha256(source.read_bytes()).hexdigest())
    layout['views'][direction]=dict(parts=[parts[n] for n in order])
    if direction!='back': layout['views'][direction]['closedHead']=parts['closedHead']
(art/'layout.json').write_text(json.dumps(layout,ensure_ascii=False,indent=2)+'\n')
print(f'{args.character}: three registered views; raw images unchanged; not Unity/Pixel acceptance.')
