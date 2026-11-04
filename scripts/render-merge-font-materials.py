#!/usr/bin/env python3
"""New font specimens from bundled TTFs. Does not edit or repaint game screenshots."""
from pathlib import Path
import hashlib,json
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parent.parent
OUT=ROOT/'References/Preview/merge-font-abc-2026-11-04'
FONTS=ROOT/'Assets/Mofumachi/Resources/Mofumachi'
S=4
INK=(82,46,74,255);WHITE=(255,254,245,255);GOLD=(239,213,169,255)
def face(key,size):return ImageFont.truetype(str(FONTS/(key+'.ttf')),round(size*S))
def box(im,rect,color,radius=15):
 d=ImageDraw.Draw(im);d.rounded_rectangle(tuple(round(v*S) for v in rect),radius*S,fill=color,outline=GOLD,width=2*S)
def text(im,xy,copy,key,size,color=INK,bold=False,outline=False):
 layer=Image.new('RGBA',im.size,(0,0,0,0));d=ImageDraw.Draw(layer);f=face(key,size);xy=tuple(round(v*S) for v in xy)
 # Explicit specimen approximation of UGUI's dynamic Bold and .35 reference-px outline.
 inner=round(.5*S) if bold else 0
 if outline:d.text(xy,copy,font=f,anchor='mm',fill=color,stroke_width=inner+round(.7*S),stroke_fill=(148,48,79,173),align='center',spacing=3*S)
 d.text(xy,copy,font=f,anchor='mm',fill=color,stroke_width=inner,stroke_fill=color,align='center',spacing=3*S)
 im.alpha_composite(layer)
def label(im,xy,copy,size=17):text(im,xy,copy,'UIFontReadingBold',size)
def ribbon(im,w,y,variant,baseline=False):
 width=(w-40)*.58;x=(w-width)/2;box(im,(x,y,x+width,y+96),(244,119,162,255),14)
 key='UIFontStory' if variant=='C' and not baseline else 'UIFontAccent'
 text(im,(w/2,y+48),'ごほうびMerge',key,36,WHITE,bold=(variant!='C' and not baseline),outline=(variant=='B' and not baseline))
def render(w,h,v):
 im=Image.new('RGBA',(w*S,h*S),(255,248,238,255))
 title={'A':'A / Hachi - light bold','B':'B / Hachi - bold + thin rose outline','C':'C / Kiwi Maru Medium'}[v]
 label(im,(w/2,28),title,21);label(im,(w/2,58),'FONT MATERIAL / NOT UNITY',18)
 ribbon(im,w,92,v)
 label(im,(w/2,225),'Menus: Hachi Maru Pop / light bold',18)
 names=['お茶\n作る','パン\n作る','お花\n作る'];cw=(w-64)/3
 for i,name in enumerate(names):
  x=22+i*(cw+10);box(im,(x,250,x+cw,340),(255,181,208,255) if i<2 else (255,246,228,255),20);text(im,(x+cw/2,295),name,'UIFontAccent',24,bold=True)
 box(im,(22,354,w*.34,440),(255,246,228,255),20);text(im,((22+w*.34)/2,397),'生成枠\n整理','UIFontAccent',24,bold=True)
 box(im,(w*.34+12,354,w-22,440),(255,181,208,255),20);text(im,((w*.34+12+w-22)/2,397),'依頼を見る','UIFontAccent',26,bold=True)
 box(im,(22,454,w*.34,518),(162,224,203,255),20);text(im,((22+w*.34)/2,486),'戻る','UIFontAccent',26,bold=True)
 box(im,(w*.34+12,454,w-22,518),(255,181,208,255),20);text(im,((w*.34+12+w-22)/2,486),'納品する','UIFontAccent',26,bold=True)
 for i,name in enumerate(['もふまち','依頼','Merge']):
  x=22+i*(cw+10);box(im,(x,536,x+cw,600),(255,181,208,255) if i==2 else (162,224,203,255),20);text(im,(x+cw/2,568),name,'UIFontAccent',24,bold=True)
 label(im,(w/2,636),'Numbers: existing Zen Maru Gothic Bold',17)
 info=['パン Lv.2','0 / 1 あと1個','30 Coins','40 Coins','70 Coins','1 / 1 完了']
 for i,copy in enumerate(info):
  col=i%2;row=i//2;x=22+col*(w/2-2);box(im,(x,656+row*54,x+w/2-40,704+row*54),(255,244,226,255),10);text(im,(x+(w/2-40)/2,680+row*54),copy,'UIFontReadingBold',24)
 label(im,(w/2,854),'Current thin title reference',18);ribbon(im,w,878,v,True)
 label(im,(w/2,min(h-34,1030)),'TTF specimen; native UGUI rendering pending.',15)
 return im.convert('RGB').resize((w,h),Image.Resampling.LANCZOS)
def main():
 OUT.mkdir(parents=True,exist_ok=True);rows=[]
 for w,h in [(720,1480),(640,1072)]:
  for v in 'ABC':
   p=OUT/f'{v}-{w}x{h}-material.png';render(w,h,v).save(p)
   rows.append(dict(file=p.name,width=w,height=h,variant=v,sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
 sheet=Image.new('RGBA',(2160*S,560*S),(255,248,238,255))
 for row,(w,h) in enumerate([(720,1480),(640,1072)]):
  for col,v in enumerate('ABC'):
   panel=Image.new('RGBA',(720*S,280*S),(255,248,238,255))
   label(panel,(360,30),f'{v} / {w}x{h} material - not Unity',19)
   small=Image.new('RGBA',(w*S,210*S),(255,248,238,255));ribbon(small,w,45,v)
   panel.alpha_composite(small,(round((720-w)/2*S),50*S));sheet.alpha_composite(panel,(col*720*S,row*280*S))
 sheet.convert('RGB').resize((2160,560),Image.Resampling.LANCZOS).save(OUT/'title-comparison-material.png')
 text='<meta charset="utf-8"><title>Mergeフォント A/B/C</title><style>body{background:#fff8ee;color:#523047;font:18px sans-serif}section{display:flex;gap:12px}figure{margin:4px}img{width:min(30vw,360px);height:auto}</style><h1>Mergeフォント A/B/C比較標本</h1><p>同じ実フォントから描画した素材標本です。Unity実画面ではありません。太字・輪郭の標本描画には近似があります。製品の盤面・背景・ボタン配置は不変。この標本の配置は字体の確認用です。実Unity6枚は専用CaptureOnlyで確認します。</p><p>A：Hachi軽い太字、B：A＋薄い濃ピンク輪郭、C：Kiwi Maru Medium。推奨候補B、Visual確認待ち。</p>'
 for w,h in [(720,1480),(640,1072)]:
  text+=f'<h2>{w}×{h}相当</h2><section>'
  for v in 'ABC':
   n=f'{v}-{w}x{h}-material.png';text+=f'<figure><figcaption>{v}</figcaption><a href="{n}"><img src="{n}"></a></figure>'
  text+='</section>'
 (OUT/'index.html').write_text('<!doctype html>'+text)
 (OUT/'material-proof.json').write_text(json.dumps(dict(kind='font-material-not-unity',native_unity=False,native_verified=False,renderer='Pillow/FreeType; synthetic bold/outline approximation',images=rows),indent=2)+'\n')
 obsolete=OUT/'font-material.html'
 if obsolete.exists():obsolete.unlink()
 print('Six actual-TTF material specimens generated. Not Unity/Pixel captures.')
if __name__=='__main__':main()
