// Browser material preview only. No Unity actor/input/Save changes.
(function(root){
  'use strict';
  const rig=typeof module!=='undefined'&&module.exports
    ?require('../noah-authored-parts-2026-10-29/rig-model.cjs'):root.NoahPartsRig;
  const valid=d=>['front','left','right','back'].includes(d);
  function selectDirection(vx,vy,previous='front'){
    const x=Number.isFinite(vx)?vx:0,y=Number.isFinite(vy)?vy:0;
    if(Math.hypot(x,y)<.08)return valid(previous)?previous:'front';
    if(Math.abs(y)>Math.abs(x)*1.15)return y<0?'back':'front';
    return x<0?'left':'right';
  }
  function sampleSequence(seconds){
    const t=Number.isFinite(seconds)?Math.max(0,seconds):0;
    const phases=[
      [1.5,'idle','left'],[4.0,'walk','left'],[5.5,'idle','left'],
      [8.0,'walk','right'],[9.5,'idle','right'],[12.0,'walk','back'],
      [13.5,'idle','back'],[15.5,'greet','right'],[Infinity,'talk','right']
    ];
    const p=phases.find(p=>t<p[0]);
    return {mode:p[1],direction:p[2]};
  }
  function poseForDirection(seconds,mode,direction,amount=1){
    const p=rig.pose(seconds,mode,amount);
    if(direction==='right'){
      // Reflect bone placement/rotation, never the artwork or its coordinates.
      for(const m of Object.values(p.matrices)){
        m[1]=-m[1];m[2]=-m[2];m[4]=-m[4];
      }
      for(const f of p.feet)f.x=-f.x;
      // Asymmetric pouch stays on Noah's anatomical right hip.
      const hip=rig.point(p.matrices.body,-115,-75);
      p.matrices.pouch[4]=hip.x;p.matrices.pouch[5]=hip.y;
      p.matrices.tail[4]=-25;
    }else if(direction==='back'){
      // Independent rear art and rear layering, not a flipped front face.
      p.matrices.head[4]=0;
      p.matrices.pouch[4]=95;
      p.matrices.farArm[4]=-110;
      p.matrices.nearArm[4]=110;
      p.matrices.farArm[0]=p.matrices.farArm[3]=1;
      p.matrices.farArm[1]=p.matrices.farArm[2]=0;
      p.matrices.tail[4]=-15;
    }
    return p;
  }
  const api={selectDirection,sampleSequence,poseForDirection};
  if(typeof module!=='undefined'&&module.exports)module.exports=api;
  else root.NoahDirectionReview=api;
})(typeof globalThis!=='undefined'?globalThis:this);
