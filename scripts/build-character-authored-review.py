"""Offline source preview only, using the unchanged Noah browser rig."""
from pathlib import Path
import base64
import hashlib
import json
import sys
ROOT=Path(__file__).resolve().parents[1]
character=sys.argv[1] if len(sys.argv)>1 else 'momo'
names={'momo':'モモ','lulu':'ルル','pom':'ポム','fio':'フィオ'}
if character not in names: raise RuntimeError('Character not prepared at its sequential gate.')
art=ROOT/f'References/Characters/{character}-authored-parts-2026-10-30'
layout=json.loads((art/'layout.json').read_text())
atlas=ROOT/'Assets/Mofumachi/Resources/Mofumachi/CharacterBodyAtlas.png'
if hashlib.sha256(atlas.read_bytes()).hexdigest()!='23d14025f146c429da01b6e3a74ef803254755f8d149f9eb53516aacaa30163d': raise RuntimeError('Formal atlas changed.')
images={'front':atlas}
for key,s in layout['sources'].items():
    p=art/s['file']
    if hashlib.sha256(p.read_bytes()).hexdigest()!=s['sha256']: raise RuntimeError('Material changed.')
    images[key]=p
data={'layout':layout,'images':{k:'data:image/png;base64,'+base64.b64encode(p.read_bytes()).decode('ascii') for k,p in images.items()}}
template=ROOT/'References/Characters/momo-authored-parts-2026-10-30/review.template.html'
html=template.read_text().replace('モモ',names[character]).replace("character:'momo'","character:"+json.dumps(character)).replace('__RIG__',(ROOT/'References/Characters/noah-authored-parts-2026-10-29/rig-model.cjs').read_text()).replace('__DIRECTIONS__',(ROOT/'References/Characters/noah-directional-parts-2026-10-30/direction-review.cjs').read_text()).replace('__DATA__',json.dumps(data,separators=(',',':')))
(art/'material-review.html').write_text(html)
print(character+': offline material review built. Not Unity/Pixel evidence.')
