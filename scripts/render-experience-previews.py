#!/usr/bin/env python3
"""Render clearly labeled source-asset/layout previews (NOT Unity screenshots).
Developer tooling: Python Playwright + Chromium; no game runtime dependency.
"""
from pathlib import Path
import html
import itertools
import re
import functools
import threading
from http.server import ThreadingHTTPServer,SimpleHTTPRequestHandler
from playwright.sync_api import sync_playwright
ROOT=Path(__file__).resolve().parent.parent
OUT=ROOT/'References/Preview/release-experience-2026-10-15'
OUT.mkdir(parents=True,exist_ok=True)
ART='../../../Assets/Mofumachi/Resources/Mofumachi/'
IDS=itertools.count()

def sprite(name,bounds,atlas=(1536,1024),cls='',style=''):
 x,y,w,h=bounds;clip='crop'+str(next(IDS))
 return f'<svg class="sprite {cls}" viewBox="{x} {y} {w} {h}" style="{style}" aria-hidden="true"><defs><clipPath id="{clip}"><rect x="{x}" y="{y}" width="{w}" height="{h}"/></clipPath></defs><image clip-path="url(#{clip})" href="{ART}{name}.png" width="{atlas[0]}" height="{atlas[1]}"/></svg>'
CHAR=[(17,12,266,324),(284,12,230,324),(524,20,221,316),(771,5,237,331),(1005,31,243,306),(1263,14,254,323)]
ITEM=[(16,121,351,254),(404,54,422,322),(828,42,423,344),(38,493,304,250),(388,453,412,302),(803,429,439,341),(62,828,242,363),(382,787,392,418),(772,787,476,438)]
PROP=[(116,17,620,327),(1090,3,161,351),(132,343,572,311),(835,354,630,325),(96,661,655,343),(808,688,652,290)]
KINDS=['Flowers','Lamp','CafeTable','Workshop','Arch','Garland']
raw=(ROOT/'Assets/Mofumachi/Presentation/TownGrowthPresentation.cs').read_text()
regions=raw.split('new[]{')[1:]
placements=[]
for part in regions[:3]:
 placements.append([(KINDS.index(k),int(l),float(x),float(y),float(w)) for k,l,x,y,w in re.findall(r'P\(TownGrowthProp\.(\w+),(\d+),([.\d]+)f,([.\d]+)f,([.\d]+)f\)',part)])

def town(region,stage,extras=True,small=False):
 key=['West','', 'East'][region];bg='Town'+key+'Background';code=['西 · カフェのお庭','中央 · 噴水の広場','東 · 花の小道'][region]
 content=f'<div class="town {"small" if small else ""}" style="background-image:url({ART}{bg}.png)"><div class="townlabel">{code}　Lv.{stage}</div>'
 if extras:
  for kind,lv,x,y,w in placements[region]:
   if lv<=stage:
    bw,bh=PROP[kind][2:];content+=sprite('TownGrowthPropsAtlas',PROP[kind],style=f'position:absolute;left:{(x-w/2)*100}%;bottom:{y*100}%;width:{w*100}%;height:auto;aspect-ratio:{bw}/{bh};filter:drop-shadow(0 2px 1px #513b3c35)')
 for j,(x,y) in enumerate([(.42,.43),(.70,.54)]):
  content+=sprite('CharacterBodyAtlas',CHAR[(region*2+j)%6],style=f'position:absolute;width:17%;height:22%;left:{x*100}%;bottom:{y*100}%;filter:drop-shadow(0 4px 2px #46334744)')
 if extras and stage>=2:content+='<div class="petals">✧　·　✧　　·　✧</div>'
 return content+'</div>'

def merge(after):
 cells=''
 demo=[0,3,6,1,4,7,2,5,8,3,3,0,6,4,1,7,0,5]
 for i in range(30):
  inside=''
  if i<len(demo):
   n=demo[i];inside=sprite('ItemAtlas',ITEM[n],(1254,1254),cls='item')+f'<span class="lv">Lv.{n%3+1}</span>'
  cells+=f'<div class="cell {"select" if i==9 and after else ""} {"match" if i==10 and after else ""}">{inside}</div>'
 return f'''<div class="phone merge {'after' if after else 'before'}"><div class="header">Merge</div><div class="need">{sprite('ItemAtlas',ITEM[4],(1254,1254))}<div>パン Lv.2<br><b>0 / 1　あと1個</b></div></div><div class="tray" {'style="background-image:url('+ART+'MergeBoardFrame.png)"' if after else ''}><div class="grid">{cells}</div></div><div class="production"><span>お茶<br>作る</span><span class="active">パン<br>作る</span><span>お花<br>作る</span><span>生成枠<br>整理</span><strong>納品する</strong></div><div class="foot">街　　依頼　　Merge　　設定</div></div>'''

css='''
.panel::before{content:'素材・配置プレビュー · Unity／Pixel実画面ではありません';display:block;font-size:11px;color:#92716b;margin-bottom:12px}
@font-face{font-family:ui;src:url(../../../Assets/Mofumachi/Resources/Mofumachi/UIFont.ttf)}
@font-face{font-family:story;src:url(../../../Assets/Mofumachi/Resources/Mofumachi/UIFontBody.ttf)}
*{box-sizing:border-box}body{margin:0;padding:32px;background:#fbf5ef;color:#523247;font-family:ui,sans-serif}h1{font-size:30px;margin:0 0 9px}h2{font-size:24px;margin:0 0 18px}h3{font-size:19px;margin:4px 0 12px}p{line-height:1.7;margin:8px 0}.warning{background:#fff1c8;border:1px solid #c8a866;padding:14px;border-radius:14px;font-size:17px}.deck{display:grid;grid-template-columns:1fr 1fr;gap:22px;margin-top:22px}.panel{background:#fffdf8;padding:24px;border:1px solid #e1c9b8;border-radius:22px;overflow:hidden}.row{display:flex;gap:20px;align-items:center;justify-content:center}.badge{display:inline-block;border-radius:20px;background:#ffe0e8;padding:5px 15px;font-size:14px}.sprite{display:block;overflow:hidden}.town{width:230px;height:474px;border-radius:19px;position:relative;background-size:100% 100%;overflow:hidden;box-shadow:0 8px 18px #48332b22;border:2px solid #c6a17e;flex-shrink:0}.town.small{width:192px;height:396px}.townlabel{position:absolute;z-index:5;top:10px;left:8px;right:8px;padding:5px;background:#fff8e5e8;border-radius:14px;text-align:center;font-size:12px}.petals{position:absolute;inset:45% 7% 0;color:#ffe3c5;font-size:30px;text-shadow:0 0 10px #ffefa2;pointer-events:none}.roadflow{padding:14px;background:#fff4da;border:1px solid #d7b782;border-radius:12px;text-align:center;font-size:15px}.roadflow b{color:#c85e87}.phone{width:300px;height:590px;border:3px solid #b99272;border-radius:24px;background:#e6c9a5;padding:12px;overflow:hidden;box-shadow:0 10px 24px #54332b22;flex-shrink:0}.header{height:40px;background:#e9f6f5;border:1px solid #d1b38b;border-radius:13px;text-align:center;font-size:21px;padding:5px;color:#65435b}.need{height:49px;margin:6px 0;background:#fff9e9;border:1px solid #dbc28d;border-radius:12px;display:flex;gap:12px;align-items:center;padding:4px 18px;font-size:12px}.need svg{height:40px;width:48px}.need b{color:#c5596d}.tray{height:372px;padding:14px 10px;border:2px solid #b48b65;border-radius:15px;background:#ead6ba}.grid{display:grid;grid-template-columns:repeat(5,1fr);grid-template-rows:repeat(6,1fr);height:100%;gap:4px}.cell{position:relative;background:#fff7e9;border:1px solid #c0a385;border-radius:9px;min-width:0}.item{position:absolute;left:9%;top:8%;width:82%;height:68%;object-fit:contain}.lv{position:absolute;bottom:2px;left:0;right:0;text-align:center;font-size:10px}.after .tray{background-size:100% 100%;box-shadow:0 7px 0 #9f7556,0 10px 8px #38252c33;padding:17px 11px 12px}.after .cell{border-radius:50%;background:radial-gradient(ellipse at 50% 25%,#fffef9,#f9eedb 62%,#e5cdb0 76%,#fff6df 85%,#d2ac86);box-shadow:0 3px 0 #c5a27f,0 6px 4px #61453629;border:1px solid #f7e7ca}.after .item{top:6%;left:4%;width:92%;height:70%;filter:drop-shadow(0 3px 1px #543a4b55)}.after .lv{background:#fff3dbe8;border:1px solid #d6b692;border-radius:6px;left:35%;bottom:0;font-size:9px;height:15px}.after .select .item{transform:translateY(-3px) scale(1.06)}.after .select{box-shadow:0 0 0 2px #f1a3bf,0 5px 3px #795f6444}.after .match{box-shadow:0 0 9px 3px #ffe6a2,0 3px 0 #bf9e77}.production{display:flex;gap:4px;margin-top:12px;height:47px;font-size:10px;align-items:stretch}.production span,.production strong{background:#fff9ea;border:1px solid #c9a17b;border-radius:9px;text-align:center;flex:1;padding:6px 1px}.production .active{background:#ffd8e4}.production strong{background:#ffe5ec;color:#8f6475;padding-top:13px}.foot{margin-top:8px;font-size:12px;text-align:center}.before .tray{background:#fff9ee}.before .cell{border-radius:9px}.settings{background:#fff4f5;padding:18px;width:360px;border:2px solid #d7b38d;border-radius:22px}.settings h3{text-align:center}.slider{height:16px;background:linear-gradient(90deg,#94d7bf 75%,#e4e5d9 75%);border-radius:9px;margin:14px 0 22px}.song{padding:12px;background:#fffbed;border:1px solid #dfc89e;border-radius:15px;margin-top:12px;font-size:16px}.song div{display:flex;align-items:center;justify-content:space-between;margin-top:10px;font-size:12px}.song button{border:1px solid #d1a78c;background:#ffe0e7;border-radius:10px;padding:10px 20px;color:#674158;font-family:ui;font-size:13px}.song.selected{border:2px solid #d9ad5b}.story{font-family:story;font-size:19px;line-height:1.65}.goals{background:#fff6df;border:1px solid #d9b878;border-radius:18px;padding:17px;margin:13px 0}.goals b{color:#b85b7e}.charstrip{display:flex;gap:6px;justify-content:space-between}.pose{width:120px;text-align:center;font-size:13px}.pose svg{width:112px;height:142px;filter:drop-shadow(0 5px 2px #503b472d)}.pose:nth-child(2) svg{transform:translateY(-4px) rotate(-2deg)}.pose:nth-child(3) svg{transform:translateY(-2px) rotate(2deg)}.pose:nth-child(4) svg{transform:translateY(-7px) scale(1.02)}.note{font-size:13px;color:#8b6c70}footer{margin-top:20px;font-size:14px}.growrow{display:flex;justify-content:center;gap:13px}.growrow .town{width:192px;height:396px}.growthblock{margin-top:18px}.native{background:#e9f5ed;padding:14px;border-radius:14px;font-size:14px}
'''
walking=f'''<section class="panel" id="walking"><h2>1　街を「選んで、歩く」へ</h2><div class="badge">Before：上部ショートカット／行き先カード中心</div><p>After：キャラをタップ → 道・花・ベンチ・出口をタップ。</p><div class="row">{town(0,2,small=True)}{town(1,2,small=True)}{town(2,2,small=True)}</div><div class="roadflow"><b>出口まで歩く → 約0.30秒の花の光 → 隣の入口から歩く</b><br>西 ⇄ 中央 ⇄ 東 ／ 上部ボタンと行き先カードも利用可能</div><p class="note">絵は承認済み背景と正式素材の配置プレビューです。実際のカメラ位置・経路の見え方はWindows／Pixelで確認します。</p></section>'''
mergepanel=f'''<section class="panel" id="merge"><h2>2　Merge · 画面全体の仕上げ</h2><div class="row"><div><h3>Before · 前候補の構成</h3>{merge(False)}</div><div><h3>After · 金縁と磁器のトレイ</h3>{merge(True)}</div></div><p>大きな絵＋角のLv.／接地影／選択浮遊／合成候補の光。<br>不足品を上段、生成を下段に整理。30マス・ルールは維持。</p></section>'''
poses=['ふだん','左足の一歩','方向転換','到着・うれしい','びっくり']
charhtml=''
for i,label in enumerate(poses):
 b=CHAR[0] if i<3 else (17,345,266,324) if i==3 else (17,676,266,325)
 charhtml+=f'<div class="pose">{sprite("CharacterBodyAtlas",b)}{label}</div>'
characters=f'''<section class="panel" id="characters"><h2>3　キャラに、動きと気持ち</h2><div class="charstrip">{charhtml}</div><p>正式原画を保ち、足・手の小さな変形、上下動、揺れ、加減速、まばたき、向き合う反応を追加。</p><p class="note">このコマ割りは既存表情素材＋配置の説明図です。Unityの実アニメーション録画ではありません。6名を複製せず、各地域へ分散します。</p><div class="native">Windows全件テストでは、隔離状態から歩行8コマの実画面PNGを別途出力します。</div></section>'''
music=f'''<section class="panel" id="music"><h2>4　3曲を聴いて、選んで、保存</h2><div class="row"><div class="settings"><h3>音楽をえらぶ</h3><div>BGM　ON　　75%</div><div class="slider"></div><div class="song">もふまちのはじまり<div>ピアノ・ベル<button>試聴</button><button>選ぶ</button></div></div><div class="song selected">おさんぽタウン<div>選択中<button>試聴</button><button>選択中</button></div></div><div class="song">もふもふマージタイム<div>マリンバ・シェイカー<button>試聴</button><button>選ぶ</button></div></div></div><div style="width:240px"><p>3つの異なる録音ループ。<br>試聴は8秒、保存と分離。</p><p>戻る・中断で元の曲へ。<br>再起動後も選択を保持。</p><p class="note">製品は既存の設定画面をスクロールして選曲します。このカードは機能を並べた説明用レイアウトです。</p></div></div></section>'''
growth='<section class="panel" id="growth" style="grid-column:1/-1"><h2>5　Lv.1からLv.6へ · 同じ美しい街を、もっと豊かに</h2><div class="row">'
for r,label in enumerate(['西 · カフェと花の庭','中央 · 噴水の広場','東 · 花の小道と工房']):
 growth+=f'<div class="growthblock"><h3>{label}</h3><div class="growrow">{town(r,1,small=True)}{town(r,6,small=True)}</div></div>'
growth+='</div><p>各地域で1点 → 16点の追加装飾へ。カフェ席・花壇・街灯・工房飾り・アーチ・リボンと、軽い花の光。元の背景は描き換えていません。</p><p class="note">成長は既存の街Lv.に従います。保存フィールド・報酬計算は追加していません。キャラや道との重なりは実機確認項目です。</p></section>'
goals='''<section class="panel" id="goals" style="grid-column:1/-1"><h2>6　「あと1お届け」が、次の景色につながる</h2><div class="row"><div style="width:45%"><div class="goals"><b>あと1お届けで、街に変化♪</b><br>Lv.2 · カフェの席と花の小道</div><div class="story">ルルのお茶に合うパンを焼いて、<br>半分こしたいな。</div></div><div style="width:45%"><div class="goals"><b>納品 → 個性のあるお礼 → 街の変化 → 再会</b><br>既存の3章6依頼・完了IDを使った短い物語。</div><div class="story">ふわふわのパンだね！<br>ルルとカフェで待ってるよ。</div><p class="note">Klee One：会話・説明・お礼・名前。<br>操作／Coins／Lv.は丸ゴシックで読みやすく。</p></div></div></section>'''
page=f'''<!doctype html><html lang="ja"><meta charset="utf-8"><title>もふまち · Experience Pass 比較</title><style>{css}</style><body><h1>もふまちメルジュ · 大規模仕上げ候補</h1><div class="warning"><b>素材・配置プレビュー（Unity／Pixel 3aの実画面ではありません）</b><br>正式背景・正式6名・アイテム素材と実装方針を使った比較資料です。動作・文字切れ・負荷の合格証明ではありません。</div><div class="deck">{walking}{mergepanel}{characters}{music}{growth}{goals}</div><footer>候補：feat/release-experience-pass-20261015 ／ 5×6・保存v3・納品・報酬・正式原画を維持。<br>Windows全件成功 → 新しいQA APK → Pixel 3aで比較・10分試遊。main未統合。</footer></body></html>'''
(OUT/'comparison.html').write_text(page)
server=ThreadingHTTPServer(('127.0.0.1',0),functools.partial(SimpleHTTPRequestHandler,directory=str(ROOT)))
threading.Thread(target=server.serve_forever,daemon=True).start()
with sync_playwright() as p:
 browser=p.chromium.launch(executable_path='/usr/bin/chromium',headless=True,args=['--no-sandbox','--allow-file-access-from-files'])
 view=browser.new_page(viewport={'width':1520,'height':1100},device_scale_factor=1)
 view.goto(f'http://127.0.0.1:{server.server_port}/References/Preview/release-experience-2026-10-15/comparison.html');view.evaluate('document.fonts.ready');view.screenshot(path=str(OUT/'comparison.png'),full_page=True)
 for key in ['walking','merge','characters','music','growth','goals']:
  view.locator('#'+key).screenshot(path=str(OUT/(key+'.png')))
 browser.close()
server.shutdown()
for p in sorted(OUT.glob('*.png')):print(p.name,p.stat().st_size)
