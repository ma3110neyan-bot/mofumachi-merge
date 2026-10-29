const test = require('node:test');
const assert = require('node:assert/strict');
const { pose, point, blend } = require('../../References/Characters/noah-authored-parts-2026-10-29/rig-model.cjs');
const length = (a,b) => Math.hypot(a.x-b.x,a.y-b.y);

test('rigid head preserves facial distances while body and limbs articulate independently',()=>{
  for(const mode of ['idle','walk','talk','greet']) for(const at of [0,.2,.7,1.4]){
    const p=pose(at,mode,1);const h=p.matrices.head;
    assert.ok(Math.abs(length(point(h,40,80),point(h,190,80))-150)<1e-8);
    assert.notDeepEqual(h,p.matrices.body);
    assert.notDeepEqual(p.matrices.nearArm,p.matrices.farArm);
  }
  assert.notDeepEqual(pose(.1,'talk',1).matrices.head,pose(.7,'talk',1).matrices.head);
});
test('walking leaves one foot planted and neither foot penetrates the ground',()=>{
  for(let i=0;i<140;i++){
    const p=pose(i/30,'walk',1);
    assert.ok(p.feet.every(f=>f.y<=0));
    assert.equal(Math.max(...p.feet.map(f=>f.y)),0);
    assert.equal(p.matrices.nearLeg[5],p.feet[0].y);
    assert.equal(p.matrices.farLeg[5],p.feet[1].y);
  }
});
test('settling activity to zero returns every part to the same neutral pose',()=>{
  const neutral=pose(0,'idle',0).matrices;
  assert.deepEqual(pose(13,'walk',0).matrices,neutral);
  const almost=pose(13,'walk',.00001).matrices;
  for(const id of Object.keys(neutral))for(let i=0;i<6;i++)assert.ok(Math.abs(almost[id][i]-neutral[id][i])<.001);
});
test('nonfinite clocks and activity cannot send pieces off canvas',()=>{
  for(const t of [NaN,Infinity,-Infinity,-1])for(const a of [NaN,Infinity,-1,2]){
    const p=pose(t,'walk',a);
    assert.equal(Object.keys(p.matrices).length,8);
    assert.ok(Object.values(p.matrices).flat().every(Number.isFinite));
  }
});
test('walk-to-stop blending has exact endpoints and keeps facial geometry rigid',()=>{
  const from=pose(.2,'walk',1),to=pose(.2,'idle',1);
  assert.deepEqual(blend(from,to,0),from);
  assert.deepEqual(blend(from,to,1),to);
  const mid=blend(from,to,.5);
  assert.ok(Math.abs(length(point(mid.matrices.head,40,80),point(mid.matrices.head,190,80))-150)<1e-8);
  assert.ok(mid.feet.every(f=>f.y<=0));
  assert.equal(Math.max(...mid.feet.map(f=>f.y)),0);
});
test('smoothing across alternate walking steps cannot lift both feet off the ground',()=>{
  const mixed=blend(pose(.2,'walk',1),pose(.6,'walk',1),.5);
  assert.equal(Math.max(...mixed.feet.map(f=>f.y)),0);
  assert.equal(mixed.matrices.nearLeg[5],mixed.feet[0].y);
  assert.equal(mixed.matrices.farLeg[5],mixed.feet[1].y);
});
test('paw scale follows annotated whole-view feet while excess thigh stays behind the body',()=>{
  const layout=require('../../References/Characters/noah-authored-parts-2026-10-29/parts-layout.json');
  // Independent visual measurements: corrected whole-view feet at600px body
  // height are about110/98px wide; detached source paws are163/148px wide.
  const width={};
  for(const [id,sourcePaw,referencePaw] of [['farLeg',163,110],['nearLeg',148,98]]){
    const part=layout.parts.find(p=>p.id===id);width[id]=sourcePaw*part.scale;
    assert.ok(width[id]>=referencePaw*.8&&width[id]<=referencePaw*1.2,`${id} shrank the paw with its thigh: ${width[id]}`);
  }
  const p=pose(0,'idle',0);
  assert.ok(p.feet[1].x+width.farLeg/2<p.feet[0].x-width.nearLeg/2,'restored paws must not overlap in a neutral stance');
});
