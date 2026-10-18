"""Render four original acoustic candidate arrangements from hash-pinned CC0 samples.

python3 scripts/render-town-candidates.py --sample-dir /path/to/verified/wavs
No synthesizer, service or SDK is added to the game. Existing town OGG is untouched.
"""
import argparse
import hashlib
import json
import subprocess
from pathlib import Path
import numpy as np
from scipy.io import wavfile
from scipy.signal import resample_poly

parser=argparse.ArgumentParser()
parser.add_argument('--sample-dir',type=Path,required=True)
args=parser.parse_args()
root=Path(__file__).resolve().parents[1]
rate=44100
sources=json.loads((root/'References/Preview/release-review-2026-10-09/audio/sources/sources.json').read_text())
names={
    'piano3':('JHPiano_NoSus_Close_C3_vl2_rr1.wav',48),
    'piano4':('JHPiano_NoSus_Close_C4_vl2_rr1.wav',60),
    'piano5':('JHPiano_NoSus_Close_C5_vl2_rr1.wav',72),
    'wood4':('Marimba_hit_Outrigger_C4_soft_01.wav',60),
    'wood6':('Marimba_hit_Outrigger_C6_soft_01.wav',84),
    'bell':('glock_soft_C5_02.wav',72),
    'shaker':('Mid_ShakerDouble_Down_rr1.wav',0),
    'strings4':('VlnEns_susVib_C4_v1.wav',60),
    'strings5':('VlnEns_susVib_G4_v1.wav',67),
}
bank={}
for key,(name,midi) in names.items():
    p=args.sample_dir/name
    expected=next(s['sha256'] for s in sources if s['file']==name)
    assert hashlib.sha256(p.read_bytes()).hexdigest()==expected,name
    sr,a=wavfile.read(p);assert sr==rate
    a=a.astype(np.float64)/2**15 if a.dtype==np.int16 else a.astype(np.float64)/2**31
    if a.ndim==1:a=np.repeat(a[:,None],2,axis=1)
    active=np.flatnonzero(np.max(np.abs(a),axis=1)>.001)
    if len(active):a=a[max(0,int(active[0])-220):]
    a/=max(.1,np.sqrt(np.mean(a[:min(len(a),rate)]**2))*4)
    bank[key]=(midi,a)

cache={}
def note(kind,pitch,seconds):
    key=(kind,pitch,round(seconds,3))
    if key in cache:return cache[key]
    options=[k for k in bank if k.startswith(kind)]
    source=min(options,key=lambda k:abs(bank[k][0]-pitch))
    base,sample=bank[source]
    scale=2**((pitch-base)/12)
    a=resample_poly(sample,1000,max(1,round(1000*scale))) if base else sample.copy()
    length=min(len(a),round((seconds+(.8 if kind!='strings' else .2))*rate))
    a=a[:length].copy()
    attack=min(length,round((.22 if kind=='strings' else .004)*rate))
    release=min(length-attack,round((.55 if kind=='strings' else .22)*rate))
    a[:attack]*=np.linspace(0,1,attack)[:,None]
    if release:a[-release:]*=np.linspace(1,0,release)[:,None]
    cache[key]=a
    return a

output=root/'Assets/Mofumachi/Resources/Mofumachi/Audio'
review=root/'References/Preview/seamless-town-2026-10-18/audio'
review.mkdir(parents=True,exist_ok=True)
report=[]
# Distinct meters, tonalities, melody phrases and accompaniment, not alternate encodes.
tracks=[('title-letter',88,3,0,'花あかりの招待状'),('title-garden',96,3,5,'ひだまりの庭の扉'),
        ('merge-picnic',104,4,2,'こもれびピクニック'),('merge-ribbon',112,4,7,'リボンを結ぶ午後')]
phrases=[
    [72,76,79,81,79,76,74,72,69,72,76,74,71,74,79,76],
    [72,74,76,79,76,81,79,77,76,74,72,69,74,77,76,72],
    [76,79,81,79,76,74,72,76,77,81,79,76,74,71,74,72],
    [79,76,74,72,76,79,81,79,77,74,71,74,76,72,69,72],
]
progression=[(48,64,67),(55,62,67),(57,60,64),(52,59,64),
             (53,60,65),(48,64,67),(50,62,65),(55,62,67)]
for index,(slug,tempo,beats,key,title) in enumerate(tracks):
    beat=60/tempo;bars=32;length=round(bars*beats*beat*rate)
    mix=np.zeros((length+rate*8,2))
    def put(kind,pitch,at,duration,gain,pan=0):
        a=note(kind,pitch+key,beat*duration)*gain*np.array([1-pan,1+pan])
        start=round(at*beat*rate);n=min(len(a),len(mix)-start)
        mix[start:start+n]+=a[:n]
    for bar in range(bars):
        chord=progression[(bar+(4 if index==1 else 0))%8]
        at=bar*beats;section=bar//8;melody=phrases[index][bar%16]+(0 if section!=2 else -12)
        lift=.85 if section in (0,3) else 1
        put('piano',chord[0],at,1.6,.14,-.1)
        if index==0:
            for b in (1,2):
                for p in chord[1:]:put('piano',p,at+b,.85,.085,.10)
            put('piano',melody,at,1.7,.25*lift,-.04)
            if bar%2:put('bell',melody+12,at+1.5,.8,.036,.18)
        elif index==1:
            for k in range(6):put('wood',chord[1+k%2]+(12 if k%3==2 else 0),at+k*.5,.45,.10,(-1)**k*.12)
            put('piano',melody,at,.8,.20*lift,-.12)
            put('piano',phrases[index][(bar+1)%16],at+1.5,1.1,.17*lift,.10)
            if bar%4==3:put('bell',melody+12,at+2.5,.6,.030,.18)
        elif index==2:
            for k in range(8):put('wood',chord[1+k%2]+(12 if k%4==3 else 0),at+k*.5,.40,.12 if k%2==0 else .075,(-1)**k*.13)
            for k in (0,1.5,3):put('piano',melody+(0 if k!=1.5 else -5),at+k,.7,.17*lift,-.06)
            for k in range(4):put('shaker',-key,at+k+.5,.12,.014)
        else:
            for k in (1,2.5):
                for p in chord[1:]:put('piano',p,at+k,.65,.08,.14)
            for k,p in zip((0,1,2.5),(melody,phrases[index][(bar+1)%16],melody-5)):put('piano',p,at+k,.7,.21*lift,-.1)
            put('wood',chord[1],at+3.5,.4,.07,.12)
            if bar%4 in (1,3):put('bell',melody+12,at+2,.6,.027,.17)
        if bar%4==2 and section in (1,2):put('piano',melody+2,at+beats-.5,.4,.10,.12)
        if index<2 and section in (1,2):
            for p in chord[1:]:put('strings',p,at,beats-.3,.025,.15 if p%2 else -.15)
    folded=mix[:length].copy()
    tail=mix[length:];folded[:len(tail)]+=tail
    # Periodic room response preserves the loop, without an ending reverb cutoff.
    room=folded.copy()
    for delay,gain in ((.071,.045),(.113,.028),(.173,.018)):
        room+=np.roll(folded,round(delay*rate),axis=0)[:,::-1]*gain
    room-=room.mean(axis=0)
    room*=min(.46/np.max(np.abs(room)),.095/max(.001,np.sqrt(np.mean(room**2))))
    temp=args.sample_dir/(slug+'.wav');wavfile.write(temp,rate,np.int16(np.clip(room,-1,1)*32767))
    target=output/('bgm-'+slug+'.ogg')
    subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-i',str(temp),'-c:a','libvorbis','-q:a','5',str(target)],check=True)
    preview=review/(slug+'-8s.mp3')
    subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-i',str(target),'-t','8','-af','afade=t=out:st=7.7:d=0.3','-c:a','libmp3lame','-q:a','3',str(preview)],check=True)
    decoded=subprocess.check_output(['ffmpeg','-hide_banner','-loglevel','error','-i',str(target),'-f','f32le','-ac','2','-ar',str(rate),'-'])
    pcm=np.frombuffer(decoded,dtype=np.float32).reshape(-1,2)
    assert np.isfinite(pcm).all() and np.max(np.abs(pcm))<.56
    report.append(dict(id=slug,title=title,bpm=tempo,meter=str(beats)+'/4',bars=bars,seconds=len(pcm)/rate,
                       peakDbFS=float(20*np.log10(np.max(np.abs(pcm)))),rmsDbFS=float(20*np.log10(np.sqrt(np.mean(pcm**2)))),
                       loopBoundaryDelta=float(np.max(np.abs(pcm[0]-pcm[-1]))),sha256=hashlib.sha256(target.read_bytes()).hexdigest(),
                       preview=preview.name,sampleLicense='CC0-1.0',listeningStatus='Pixel listening pending'))
    print(slug,'rendered',flush=True)
(review/'candidate-audio.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
