const test=require('node:test');
const assert=require('node:assert/strict');
const review=require('../../References/Characters/noah-directional-parts-2026-10-30/direction-review.cjs');

test('dead-zone keeps facing and vertical travel chooses away or front',()=>{
  assert.equal(review.selectDirection(0,0,'right'),'right');
  assert.equal(review.selectDirection(.03,-.02,'back'),'back');
  assert.equal(review.selectDirection(-1,.2,'front'),'left');
  assert.equal(review.selectDirection(1,.2,'front'),'right');
  assert.equal(review.selectDirection(.2,-1,'left'),'back');
  assert.equal(review.selectDirection(.2,1,'right'),'front');
  assert.equal(review.selectDirection(NaN,Infinity,'unknown'),'front');
});
test('continuous sample covers idle, left/right/away walking, stops and ends talking',()=>{
  const samples=Array.from({length:181},(_,i)=>review.sampleSequence(i/10));
  assert.equal(samples[0].mode,'idle');
  for(const direction of ['left','right','back'])assert.ok(samples.some(p=>p.direction===direction&&p.mode==='walk'));
  assert.ok(samples.some(p=>p.mode==='idle'&&p.direction==='right'));
  assert.equal(samples.at(-1).mode,'talk');
  assert.equal(review.sampleSequence(NaN).mode,'idle');
  assert.deepEqual(review.sampleSequence(-5),review.sampleSequence(0));
});
test('three assembled directions use only rigid positive-determinant transforms and stay grounded',()=>{
  const heads=[];
  for(const d of ['left','right','back'])for(const mode of ['idle','walk','talk','greet'])for(let i=0;i<50;i++){
    const p=review.poseForDirection(i/30,mode,d);
    assert.equal(Object.keys(p.matrices).length,8);
    for(const m of Object.values(p.matrices)){
      assert.ok(m.every(Number.isFinite));
      assert.ok(Math.abs(m[0]*m[3]-m[1]*m[2]-1)<1e-8,'face/part must not be mirrored or sheared');
      assert.ok(Math.abs(Math.hypot(m[0],m[1])-1)<1e-8);
    }
    assert.equal(Math.max(...p.feet.map(f=>f.y)),0);
    if(mode==='idle'&&i===0)heads.push(p.matrices.head[4]);
  }
  assert.notEqual(heads[0],heads[1]);
});
test('rear tail root stays behind torso and right pouch remains near side',()=>{
  const layouts=require('../../References/Characters/noah-directional-parts-2026-10-30/directions-layout.json');
  const order=d=>layouts.views[d].parts.map(p=>p.id);
  assert.ok(order('back').indexOf('tail')<order('back').indexOf('body'),'tail root must be occluded by rear torso, not appear as an extra limb');
  assert.ok(order('right').indexOf('pouch')>order('right').indexOf('body'));
  assert.equal(review.poseForDirection(0,'idle','back',0).matrices.pouch[4]>0,true);
  assert.equal(review.poseForDirection(0,'idle','right',0).matrices.pouch[4]<0,true);
});
test('assembled source rectangles remain inside the320px review viewport',()=>{
  const layouts=require('../../References/Characters/noah-directional-parts-2026-10-30/directions-layout.json');
  const {point}=require('../../References/Characters/noah-authored-parts-2026-10-29/rig-model.cjs');
  for(const d of ['left','right','back'])for(const at of [0,.2,.7,1.4]){
    const pose=review.poseForDirection(at,'walk',d);
    for(const p of layouts.views[d].parts){
      const [,,w,h]=p.crop;
      for(const [x,y] of [[0,0],[w,0],[w,h],[0,h]]){
        const v=point(pose.matrices[p.id],(x-p.pivot[0]*w)*p.scale,(y-p.pivot[1]*h)*p.scale);
        const screenX=180+v.x*320/600;
        assert.ok(screenX>=3&&screenX<=357,`${d} ${p.id} clips viewport at${screenX}`);
      }
    }
  }
});
