from pathlib import Path
import json, hashlib, os
from xml.etree import ElementTree as E
import numpy as np
from PIL import Image

root = Path('/workspace/runtime/momo-native-20261101-111715')
original = root / 'originals'
def read(name):
    return json.loads((original / name).read_text(encoding='utf-8-sig'))

proof, source, prepared = read('proof.json'), read('source-info.json'), read('prepared.json')
xml = E.parse(original / 'playmode.xml').getroot()
cases = list(xml.iter('test-case'))
assert len(cases) == 1 and cases[0].get('methodname') == 'MomoAuthoredPartsPreserveIdentityInputAndState'
assert [xml.get(k) for k in ['result','total','passed','failed','skipped','inconclusive']] == ['Passed','1','1','0','0','0']
assert proof['gitRevision'] == source['gitRevision'] == '34a974880adaf04fede83b85281be08a5fb46949'
assert proof['productFingerprint'] == source['productFingerprint']
assert proof['input_sha256'] == prepared['input_sha256']
log = (original / 'playmode.log').read_text(encoding='utf-8-sig')
for key, switch in [('gitRevision','mofumachiSourceRevision'),('productFingerprint','mofumachiProductFingerprint')]:
    assert '-' + switch + '\n' + proof[key] in log
assert 'Test run completed. Exiting with code 0 (Ok)' in log
assert source['native_test_passed'] and source['source_unchanged'] and source['error'] == ''
for key in ['native_unity','tap_verified','conversation_verified','save_unchanged','greeting_authored','talk_authored','blink_authored','rear_blink_verified','grounding_verified']:
    assert proof[key] is True, key
for key in ['pixel_verified','adopted']:
    assert proof[key] is False and source[key] is False
assert set(proof['observed_directions']) == {'Front','Back','Left','Right'}
images = {}
for path in original.iterdir():
    if path.suffix.lower() not in ['.png','.jpg','.jpeg']:
        continue
    data = path.read_bytes()
    with Image.open(path) as im:
        dimensions = im.size
        expected = (1440,1280) if path.name.endswith('-before-left-after-right.jpg') else (720,1280)
        assert dimensions == expected, (path.name, dimensions)
        im.verify()
    images[path.name] = {'file':path.name,'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest(),'width':dimensions[0],'height':dimensions[1]}
refs = []
def find_refs(value):
    if isinstance(value,dict):
        if {'file','sha256','bytes','width','height'}.issubset(value) and value['file']:
            refs.append(value)
        for child in value.values():
            find_refs(child)
    elif isinstance(value,list):
        for child in value:
            find_refs(child)
find_refs(proof)
for item in refs:
    assert item['file'] in images, item['file']
    for key in ['bytes','sha256','width','height']:
        assert item[key] == images[item['file']][key], (item['file'],key)
unreferenced = set(images) - {row['file'] for row in refs}
assert not unreferenced, unreferenced
composites = [name for name in images if name.endswith('-before-left-after-right.jpg')]
assert len(composites) == 5
comparisons = []
for kind in ['pairs','grounding_pairs']:
    for pair in proof[kind]:
        for key in ['frame_unchanged','actors_unchanged','other_five_unchanged','camera_unchanged','water_clock_unchanged','water_phase_unchanged','state_unchanged','store_writes_unchanged','world_model_unchanged']:
            assert pair[key] is True
        with Image.open(original / pair['before']['file']) as im:
            first = np.asarray(im.convert('RGB'))
        with Image.open(original / pair['after']['file']) as im:
            second = np.asarray(im.convert('RGB'))
        changed = np.any(first != second,axis=2)
        y,x = np.where(changed)
        unity_y = 1279-y
        outside = (x<pair['allowed_x']) | (x>=pair['allowed_x']+pair['allowed_width']) | (unity_y<pair['allowed_y']) | (unity_y>=pair['allowed_y']+pair['allowed_height'])
        count, external = int(changed.sum()), int(outside.sum())
        assert count == pair['changed_pixels'] and count > 0, (pair['id'],count,pair['changed_pixels'])
        assert external == pair['outside_allowed_pixels'] == 0, (pair['id'],external)
        comparisons.append({'kind':kind,'id':pair['id'],'direction':pair['mode'],'changed_pixels':count,'outside_allowed_pixels':external})
assert len(proof['pairs']) == 11 and len(proof['grounding_pairs']) == 5
assert {pair['mode'] for pair in proof['grounding_pairs']} == {'Left','Right','Front','Back'}
art = Path('References/Characters/momo-authored-parts-2026-10-30')
raw = (art/'layout.json').read_bytes()
layout = json.loads(raw)
source_rows = []
for direction in ['left','right','back']:
    record = layout['sources'][direction]
    digest = hashlib.sha256((art/record['file']).read_bytes()).hexdigest()
    assert digest == record['sha256']
    source_rows.append(direction+':'+digest)
line_variants = {}
text = raw.decode('utf-8').replace('\r\n','\n')
for ending, content in [('LF',text.encode()),('CRLF',text.replace('\n','\r\n').encode())]:
    rows = [hashlib.sha256(content).hexdigest()] + source_rows
    line_variants[ending] = hashlib.sha256('\n'.join(rows).encode()).hexdigest()
assert proof['input_sha256'] in line_variants.values()
validation = root/'validation'
screen = validation/'screenshots/momo-authored'
screen.mkdir(parents=True,exist_ok=True)
for path in original.iterdir():
    target = (screen if path.suffix.lower() in ['.png','.jpg','.jpeg'] or path.name=='proof.json' else validation)/path.name
    if not target.exists():
        os.link(path,target)
verified = {
    'gitRevision':source['gitRevision'],'productFingerprint':source['productFingerprint'],
    'input_sha256':proof['input_sha256'],
    'layout_line_ending_verified':[key for key,value in line_variants.items() if value==proof['input_sha256']],
    'xml':{'total':1,'passed':1,'failed':0,'skipped':0,'inconclusive':0,'exit_code':0},
    'native_images':len(images),'proof_referenced_images':len({row['file'] for row in refs}),
    'comparison_jpegs':len(composites),'frames':len(proof['frames']),
    'sorting_images':len(proof['sorting_images']),'comparisons':comparisons,'images':list(images.values()),
    'native_runtime_test_passed':True,'pixel_verified':False,'formal_fixed':False,'adopted':False
}
(root/'verified.json').write_text(json.dumps(verified,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({key:value for key,value in verified.items() if key not in ['images','comparisons']},ensure_ascii=False))
