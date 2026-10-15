"""Render two original arrangements from the project's hash-pinned CC0 samples.

Usage: python3 scripts/render-experience-bgm.py --sample-dir /path/to/verified/wavs
Requires NumPy, SciPy and FFmpeg; no runtime synthesizer or SDK is added to Unity.
"""
import argparse, hashlib, json, subprocess
from pathlib import Path
import numpy as np
from scipy.io import wavfile
from scipy.signal import resample

parser=argparse.ArgumentParser();parser.add_argument('--sample-dir',type=Path,required=True);args=parser.parse_args()
root=Path(__file__).resolve().parents[1];rate=44100
sources=json.loads((root/'References/Preview/release-review-2026-10-09/audio/sources/sources.json').read_text())
bank={}
for name,midi in [('JHPiano_NoSus_Close_C4_vl2_rr1.wav',60),('JHPiano_NoSus_Close_C5_vl2_rr1.wav',72),('Marimba_hit_Outrigger_C4_soft_01.wav',60),('Marimba_hit_Outrigger_C6_soft_01.wav',84),('glock_soft_C5_02.wav',72),('Mid_ShakerDouble_Down_rr1.wav',0)]:
    p=args.sample_dir/name;data=p.read_bytes();source=next(s for s in sources if s['file']==name)
    assert hashlib.sha256(data).hexdigest()==source['sha256'],name
    sr,a=wavfile.read(p);assert sr==rate
    a=a.astype(np.float64)/float(2**(np.iinfo(a.dtype).bits-1))
    active=np.flatnonzero(np.max(np.abs(a),axis=1)>.002)
    if len(active):a=a[max(0,int(active[0])-128):]
    bank[name]=(midi,a)
cache={}
def note(kind,pitch,duration):
    key=(kind,pitch,round(duration,3))
    if key in cache:return cache[key]
    names=[n for n in bank if kind in n];name=min(names,key=lambda n:abs(bank[n][0]-pitch));base,sample=bank[name]
    a=resample(sample,int(len(sample)/2**((pitch-base)/12))) if kind!='Shaker' else sample.copy()
    length=min(len(a),int((duration+.85)*rate));a=a[:length].copy()
    attack=min(int(.008*rate),length);release=min(int(.16*rate),length)
    a[:attack]*=np.linspace(0,1,attack)[:,None];a[-release:]*=np.linspace(1,0,release)[:,None]
    cache[key]=a;return a
output=root/'Assets/Mofumachi/Resources/Mofumachi/Audio';report=[]
for track,tempo,beats,bars in [('bgm-beginning-loop',84,3,16),('bgm-merge-loop',112,4,16)]:
    beat=60/tempo;length=round(bars*beats*beat*rate);mix=np.zeros((length+rate*8,2))
    def put(kind,pitch,at,duration,gain,pan=0):
        a=note(kind,pitch,duration*beat)*gain*np.array([1-pan,1+pan]);start=round(at*beat*rate)
        mix[start:start+len(a)]+=a[:len(mix)-start]
    chords=[(48,64,67),(53,65,69),(57,64,69),(55,62,67)]
    melody=[72,76,79,77,76,74,72,69,72,74,77,76,74,71,74,79]
    for bar in range(bars):
        chord=chords[bar%4];at=bar*beats
        if beats==3:
            put('Piano',chord[0],at,1.6,.12,-.12)
            for b in [1,2]:
                for pitch in chord[1:]:put('Piano',pitch,at+b,.75,.085,.12)
            put('Piano',melody[bar],at,.95,.19,-.04)
            if bar%2==1:put('glock',melody[bar]+12,at+1.5,.8,.055,.18)
        else:
            put('Piano',chord[0],at,1.3,.10,-.12)
            for k in range(8):put('Marimba',chord[1+k%2]+(12 if k%4==3 else 0),at+k*.5,.36,.12 if k%2 else .16,(-1)**k*.16)
            for k in range(4):put('Shaker',0,at+k+.5,.12,.075)
            if bar%2==0:put('glock',melody[bar]+12,at+2.5,.6,.05,.15)
    folded=mix[:length].copy();tail=mix[length:];folded[:len(tail)]+=tail
    folded-=folded.mean(axis=0);folded*=.30/max(np.max(np.abs(folded)),.001)
    temp=args.sample_dir/(track+'.wav');wavfile.write(temp,rate,np.int16(np.clip(folded,-1,1)*32767))
    target=output/(track+'.ogg');subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-i',str(temp),'-c:a','libvorbis','-q:a','5',str(target)],check=True)
    decoded=subprocess.check_output(['ffmpeg','-hide_banner','-loglevel','error','-i',str(target),'-f','f32le','-ac','2','-ar',str(rate),'-'])
    pcm=np.frombuffer(decoded,dtype=np.float32).reshape(-1,2);assert np.isfinite(pcm).all()
    report.append({'file':target.name,'sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'bytes':target.stat().st_size,'seconds':len(pcm)/rate,'peakDbFS':float(20*np.log10(np.max(np.abs(pcm)))),'boundaryDelta':float(np.max(np.abs(pcm[0]-pcm[-1]))),'tempo':tempo,'meter':str(beats)+'/4','sampleLicense':'CC0-1.0','listeningStatus':'not_yet_reviewed_on_Pixel'})
print(json.dumps(report,ensure_ascii=False,indent=2))
