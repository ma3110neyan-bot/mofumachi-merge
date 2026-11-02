"""Independent original-pixel checks on exported *production* mesh quads.

This is not Unity/Pixel evidence. No artwork, offsets or quads are corrected.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[3]
spec = importlib.util.spec_from_file_location('preview', ROOT / 'scripts/render-head-restoration-jpegs.py')
preview = importlib.util.module_from_spec(spec)
spec.loader.exec_module(preview)
parser = argparse.ArgumentParser()
parser.add_argument('meshes', type=Path)
parser.add_argument('output', type=Path)
args = parser.parse_args()
reference_path = ROOT / 'Tests/Fixtures/character-head-calibration-2026-11-02.json'
reference = json.loads(reference_path.read_text())
atlas = preview.load_original(ROOT / 'Assets/Mofumachi/Resources/Mofumachi/CharacterBodyAtlas.png')
bounds = [[17,12,266,324],[284,12,230,324],[524,20,221,316],[771,5,237,331],[1005,31,243,306],[1263,14,254,323]]
results = []

def bilinear(alpha, coordinates):
    x, y = coordinates.T
    h, w = alpha.shape
    valid = (x >= 0) & (y >= 0) & (x < w-1) & (y < h-1)
    ix, iy = np.clip(np.floor(x).astype(int), 0, w-2), np.clip(np.floor(y).astype(int), 0, h-2)
    fx, fy = x-ix, y-iy
    return np.where(valid, alpha[iy,ix]*(1-fx)*(1-fy)+alpha[iy,ix+1]*fx*(1-fy)+alpha[iy+1,ix]*(1-fx)*fy+alpha[iy+1,ix+1]*fx*fy, 0)

for name in preview.NAMES:
    c = preview.Character(name, args.meshes, atlas, bounds)
    trace = json.loads((args.meshes / f'{name}-trace.json').read_text())
    row = {'character': name, 'mesh_frames_checked': 0, 'collar_failures': 0,
           'minimum_collar_alpha': 1., 'maximum_garment_occlusion': 0., 'native_verified': False}
    variants = {}
    for direction in ('left', 'right', 'back'):
        parts = c.layout['views'][direction]['parts']
        hi = next(i for i,p in enumerate(parts) if p['id']=='head')
        bi = next(i for i,p in enumerate(parts) if p['id']=='body')
        variants[direction, False] = (parts[hi], np.asarray(c.chips[direction,hi])[:,:,3]/255.)
        if name!='noah' and direction!='back':
            closed = c.layout['views'][direction]['closedHead']
            source = c.layout['sources'][direction]
            original = preview.load_original(c.layout_path.parent/source['file'], source['sha256'])
            variants[direction, True] = (closed, np.asarray(preview.part_chip(original,closed))[:,:,3]/255.)
    for frame in trace['frames']:
        direction = frame['direction']
        landmarks = next(r for r in reference['rows'] if r['character']==name and r['direction']==direction)
        parts = c.layout['views'][direction]['parts']
        bp = next(p for p in parts if p['id']=='body')
        hp, alpha = variants[direction, frame['blink']]
        bq, hq = np.asarray(frame['body']), np.asarray(frame['head'])
        body_matrix = np.column_stack(((bq[2]-bq[1])/bp['crop'][2], (bq[0]-bq[1])/bp['crop'][3]))
        head_inverse = np.linalg.inv(np.column_stack(((hq[2]-hq[1])/hp['crop'][2], (hq[0]-hq[1])/hp['crop'][3])))
        def coverage(key):
            points = np.asarray([[x+dx+.5,y+dy+.5] for x,y in landmarks[key] for dx in (-1,0,1) for dy in (-1,0,1)])
            world = points @ body_matrix.T + bq[1]
            head = (world-hq[1]) @ head_inverse.T - .5
            return bilinear(alpha, head)
        cover, clear = coverage('collar_cover').min(), coverage('garment_clear').max()
        row['minimum_collar_alpha'] = min(row['minimum_collar_alpha'], float(cover))
        row['maximum_garment_occlusion'] = max(row['maximum_garment_occlusion'], float(clear))
        row['mesh_frames_checked'] += 1
        if cover < .92-1e-4 or clear > .08+1e-4:
            row['collar_failures'] += 1
            if row['collar_failures']==1: print(name, 'FAIL first frame', frame['frame'], direction, cover, clear)
    row['result'] = 'PASS' if row['collar_failures']==0 else 'FAIL'
    results.append(row)
    print(name, row['result'], 'measured split frames=', row['mesh_frames_checked'], 'failures=', row['collar_failures'])
report = {'kind': 'original-collar-pixels-on-production-CLR-mesh', 'native_unity': False,
          'visual_accepted': False, 'reference_sha256': hashlib.sha256(reference_path.read_bytes()).hexdigest(),
          'characters': results, 'original_inputs': preview.INPUTS}
args.output.write_text(json.dumps(report, ensure_ascii=False, indent=2)+'\n')
if any(r['result']!='PASS' for r in results): raise SystemExit(1)
