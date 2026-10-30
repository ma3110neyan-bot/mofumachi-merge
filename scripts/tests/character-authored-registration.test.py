"""CPU source-alpha joint checks; not a Unity render or device result."""
import json
from pathlib import Path
import subprocess
import sys
import unittest
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT=Path(__file__).resolve().parents[2]
CHARACTER=sys.argv.pop(1)
ART=ROOT/f'References/Characters/{CHARACTER}-authored-parts-2026-10-30'
LAYOUT=json.loads((ART/'layout.json').read_text())
NODE="const r=require(process.argv[1]);let out=[];for(const d of ['left','right','back'])for(const m of ['idle','walk','talk','greet'])for(const t of [0,.3,.6,1.2])out.push({d,m,t,pose:r.poseForDirection(t,m,d)});process.stdout.write(JSON.stringify(out));"
POSES=json.loads(subprocess.check_output(['node','-e',NODE,str(ROOT/'References/Characters/noah-directional-parts-2026-10-30/direction-review.cjs')]))

def occupancy(part, direction, matrix):
    x,y,w,h=part['crop']
    a=np.array(Image.open(ART/LAYOUT['sources'][direction]['file']).getchannel('A'))[y:y+h,x:x+w]
    if 'maskRows' in part:
        for row,span in enumerate(part['maskRows']):
            if span['min']<0:a[row]=0
            else:a[row,:span['min']]=0;a[row,span['max']+1:]=0
    yy,xx=np.nonzero(a[::2,::2]>100)
    px=(xx*2-part['pivot'][0]*w)*part['scale']
    py=(yy*2-part['pivot'][1]*h)*part['scale']
    angle=part['rotation'];c,s=np.cos(angle),np.sin(angle)
    rx=c*px-s*py+part['offset'][0];ry=s*px+c*py+part['offset'][1]
    aa,b,cc,d,tx,ty=matrix
    ox=np.rint((aa*rx+cc*ry+tx+700)/2).astype(int)
    oy=np.rint((b*rx+d*ry+ty+800)/2).astype(int)
    result=np.zeros((600,700),bool)
    valid=(ox>=0)&(ox<700)&(oy>=0)&(oy<600)
    result[oy[valid],ox[valid]]=True
    return result

class Registration(unittest.TestCase):
    def test_head_meets_body_through_existing_rigid_motion(self):
        for sample in POSES:
            parts={p['id']:p for p in LAYOUT['views'][sample['d']]['parts']}
            head=occupancy(parts['head'],sample['d'],sample['pose']['matrices']['head'])
            body=occupancy(parts['body'],sample['d'],sample['pose']['matrices']['body'])
            # Four canonical units allow subpixel antialiasing; a visible gap
            # must not be hidden by an arbitrary rectangle around the ears.
            self.assertTrue(np.any(ndimage.binary_dilation(head,iterations=2)&body),
                            f"Disconnected collar: {sample['d']} {sample['m']} {sample['t']}")

if __name__=='__main__':unittest.main()
