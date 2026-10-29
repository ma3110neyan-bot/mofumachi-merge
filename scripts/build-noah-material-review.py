"""Package unapproved source materials in an offline review, never a game capture."""
from pathlib import Path
import base64
import hashlib
import json

ROOT = Path(__file__).resolve().parent.parent
ART = ROOT / "References/Characters/noah-authored-parts-2026-10-29"
ORIGINAL = ROOT / "Assets/Mofumachi/Resources/Mofumachi/CharacterBodyAtlas.png"
EXPECTED = "23d14025f146c429da01b6e3a74ef803254755f8d149f9eb53516aacaa30163d"
if hashlib.sha256(ORIGINAL.read_bytes()).hexdigest() != EXPECTED:
    raise RuntimeError("Formal atlas changed; stop packaging.")
layout = json.loads((ART / "parts-layout.json").read_text())
paths = {"original": ORIGINAL, "direction": ART / "turnaround-draft-02.png"}
paths.update({k: ART / v["file"] for k, v in layout["sources"].items()})
images = {k: "data:image/png;base64," + base64.b64encode(v.read_bytes()).decode("ascii") for k, v in paths.items()}
data = {"layout": layout, "images": images,
        "directions": {"left": [577, 34, 521, 615], "right": [1112, 49, 484, 601], "back": [1632, 37, 472, 610]}}
template = r'''<!doctype html><html lang="ja"><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>ノア｜方向別素材・分割接続の確認</title>
<style>
*{box-sizing:border-box}body{margin:0;background:#f5f2eb;color:#453b36;font:16px/1.7 system-ui,sans-serif}main{max-width:1050px;margin:auto;padding:28px 20px}h1{font-size:25px;margin:0 0 12px}h2{font-size:20px;margin:28px 0 8px}.notice{background:#fff4d8;border-left:5px solid #bb7a32;padding:14px 18px;border-radius:8px}p{margin:8px 0}button,select{font:inherit;padding:7px 13px;border:1px solid #d7c8b7;border-radius:8px;background:#fff;cursor:pointer}button[aria-pressed=true]{background:#ceeadc;border-color:#5e967a}.controls{display:flex;gap:8px;flex-wrap:wrap;align-items:center;margin:14px 0}.grid{display:grid;grid-template-columns:1fr 1fr;gap:18px}figure{margin:0;background:#fff;border:1px solid #e7ded1;border-radius:15px;overflow:hidden;text-align:center}figcaption{padding:10px 8px;font-weight:650}canvas{display:block;max-width:100%;height:auto;margin:auto;background:linear-gradient(#fffdfa,#f1ecdf)}small,.muted{color:#766c63}#error{color:#9e3333;white-space:pre-wrap}a{color:#39795d}@media(max-width:650px){.grid{grid-template-columns:1fr}main{padding:20px 12px}}
</style><main>
<h1>ノア｜方向別素材とパーツ接続</h1>
<div class="notice"><strong>未承認の素材・ブラウザー試作です。Unity実画面／Pixel 3aの結果ではありません。</strong><br>正式原画と製品表示は変更していません。右・後ろは方向別の素材候補、分割動作は左斜め1方向だけです。</div>
<h2>1. 正式原画との静止比較</h2>
<p>正面は正式atlasの同じ画像を表示します。新しく描かれた正面案は採用せず、顔・目の基準を保っています。左右・後ろの候補で、ノアらしさと向きの奥行きを確認します。</p>
<div class="controls" id="directions"><button data-direction="front" aria-pressed="true">正面・正式原画</button><button data-direction="left" aria-pressed="false">左斜め・候補</button><button data-direction="right" aria-pressed="false">右斜め・候補</button><button data-direction="back" aria-pressed="false">後ろ・候補</button><label>比較の大きさ <select id="size"><option value="160">高さ160pxの目安</option><option value="320">高さ320px・拡大確認</option></select></label></div>
<div class="grid"><figure><figcaption>正式原画・変更なし</figcaption><canvas id="original" width="360" height="390"></canvas></figure><figure><figcaption id="direction-label">正面・正式原画そのもの</figcaption><canvas id="direction" width="360" height="390"></canvas></figure></div>
<h2>2. 左斜め・独立パーツの接続確認</h2>
<p>頭・胴・腕・脚・尾・ポーチを別の絵として配置し、顔を伸ばさずに動かす接続試作です。原画の印象や関節の継ぎ目の最終判定は保留です。まばたき・表情差分・全方向の分割リグ・ゲームへの組込みは未完了です。</p>
<div class="controls" id="modes"><button data-mode="idle" aria-pressed="true">静止・呼吸</button><button data-mode="walk" aria-pressed="false">歩行の見本</button><button data-mode="talk" aria-pressed="false">会話の身振り</button><button data-mode="greet" aria-pressed="false">軽い手振り</button><button id="pause">一時停止</button><label><input id="bones" type="checkbox"> 接続点を表示</label></div>
<div class="grid"><figure><figcaption>左斜め・一体の素材候補</figcaption><canvas id="whole" width="360" height="390"></canvas></figure><figure><figcaption>左斜め・独立パーツの接続試作</figcaption><canvas id="rig" width="360" height="390"></canvas></figure></div>
<p class="muted">同じ足元線・同じ高さの目安で比較します。影は接続確認用のブラウザー描画です。FPS・発熱・タップ遅延などの実機性能は測定していません。他5名・街・噴水・北境界・保存処理は対象外です。</p>
<p id="status" role="status">素材を読み込んでいます…</p><p id="error" role="alert"></p>
<script>__MODEL__</script><script>
'use strict';const DATA=__DATA__;
const images={};let direction='front',mode='idle',amount=1,paused=false,clock=0,last=0,drawn=0,displayPose=NoahPartsRig.pose(0,'idle',1);
const cvs=Object.fromEntries(['original','direction','whole','rig'].map(id=>[id,document.getElementById(id)]));
function clear(c){c.clearRect(0,0,360,390);c.save();c.strokeStyle='#baa88d';c.setLineDash([5,5]);c.beginPath();c.moveTo(20,363);c.lineTo(340,363);c.stroke();c.restore();}
function crop(c,img,box,height){const [x,y,w,h]=box;const s=height/h;c.drawImage(img,x,y,w,h,180-w*s/2,363-height,w*s,height);}
function originals(){const h=Number(document.getElementById('size').value);for(const id of ['original','direction','whole'])clear(cvs[id].getContext('2d'));crop(cvs.original.getContext('2d'),images.original,[1263,14,254,323],h);if(direction==='front')crop(cvs.direction.getContext('2d'),images.original,[1263,14,254,323],h);else crop(cvs.direction.getContext('2d'),images.direction,DATA.directions[direction],h);crop(cvs.whole.getContext('2d'),images.direction,DATA.directions.left,h);}
function drawRig(at){const c=cvs.rig.getContext('2d');clear(c);const scale=Number(document.getElementById('size').value)/600;const p=displayPose;c.save();c.translate(180,363);c.scale(scale,scale);c.fillStyle='rgba(58,43,34,.30)';c.beginPath();c.ellipse(0,0,95,14,0,0,Math.PI*2);c.fill();for(const part of DATA.layout.parts){c.save();c.transform(...p.matrices[part.id]);const [x,y,w,h]=part.crop;const s=part.scale;c.drawImage(images[part.source],x,y,w,h,-part.pivot[0]*w*s,-part.pivot[1]*h*s,w*s,h*s);c.restore();}if(document.getElementById('bones').checked){c.fillStyle='#2086ad';for(const m of Object.values(p.matrices)){c.beginPath();c.arc(m[4],m[5],5,0,Math.PI*2);c.fill();}}c.restore();window.reviewState={mode,direction,clock:at,drawn:++drawn,feet:p.feet};}
function frame(now){if(last&&!paused){const dt=Math.min((now-last)/1000,.05);clock+=dt;displayPose=NoahPartsRig.blend(displayPose,NoahPartsRig.pose(clock,mode,amount),1-Math.exp(-dt*12));}last=now;drawRig(clock);requestAnimationFrame(frame);}
function press(group,attr,value){for(const b of group.querySelectorAll('button'))b.setAttribute('aria-pressed',String(b.dataset[attr]===value));}
document.getElementById('directions').addEventListener('click',e=>{if(!e.target.dataset.direction)return;direction=e.target.dataset.direction;press(e.currentTarget,'direction',direction);document.getElementById('direction-label').textContent={front:'正面・正式原画そのもの',left:'左斜め・方向別素材候補',right:'右斜め・方向別素材候補',back:'後ろ・方向別素材候補'}[direction];originals();});
document.getElementById('modes').addEventListener('click',e=>{if(!e.target.dataset.mode)return;mode=e.target.dataset.mode;if(paused)displayPose=NoahPartsRig.pose(clock,mode,amount);press(e.currentTarget,'mode',mode);});
document.getElementById('size').addEventListener('change',originals);
document.getElementById('pause').addEventListener('click',e=>{paused=!paused;e.target.textContent=paused?'再生する':'一時停止';});
window.setNoahReviewPose=(at,newMode='idle')=>{paused=true;clock=at;mode=newMode;displayPose=NoahPartsRig.pose(at,mode,amount);drawRig(at);};
Promise.all(Object.entries(DATA.images).map(([id,url])=>new Promise((ok,bad)=>{const im=new Image();im.onload=()=>{images[id]=im;ok();};im.onerror=()=>bad(new Error('画像を読み込めません: '+id));im.src=url;}))).then(()=>{originals();drawRig(0);window.reviewReady=true;document.getElementById('status').textContent='素材の読込完了。ゲームへの組込み・正式採用・Pixel検証は未了です。';requestAnimationFrame(frame);}).catch(err=>{document.getElementById('error').textContent=String(err);});
</script></main></html>'''
model = (ART / "rig-model.cjs").read_text()
output = template.replace("__MODEL__", model).replace("__DATA__", json.dumps(data, separators=(",", ":")))
(ART / "noah-material-review.html").write_text(output)
print("Offline material review written; original atlas verified; no product import or game proof.")
