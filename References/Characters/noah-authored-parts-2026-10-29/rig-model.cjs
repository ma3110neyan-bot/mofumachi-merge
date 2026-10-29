// Independent rigid-part bones for the isolated material review only.
// No Unity actor, Save, input, navigation or formal texture is changed.
(function(root){
  'use strict';
  const clean=v=>v===0?0:v;
  function matrix(x,y,r){const c=Math.cos(r),s=Math.sin(r);return [c,s,-s,c,x,y].map(clean);}
  function multiply(a,b){return [
    a[0]*b[0]+a[2]*b[1],a[1]*b[0]+a[3]*b[1],
    a[0]*b[2]+a[2]*b[3],a[1]*b[2]+a[3]*b[3],
    a[0]*b[4]+a[2]*b[5]+a[4],a[1]*b[4]+a[3]*b[5]+a[5]
  ].map(clean);}
  function point(m,x,y){return {x:m[0]*x+m[2]*y+m[4],y:m[1]*x+m[3]*y+m[5]};}
  function pose(seconds,mode='idle',amount=1){
    const t=Number.isFinite(seconds)?Math.max(0,seconds):0;
    const a=Number.isFinite(amount)?Math.min(1,Math.max(0,amount)):0;
    const walk=mode==='walk',talk=mode==='talk',greet=mode==='greet';
    const stride=Math.sin(t*8),breath=Math.sin(t*2.3);
    const lean=a*(walk?stride*.018:greet?.022:breath*.004);
    const bob=a*(walk?Math.abs(stride)*2.4:breath*.9);
    const body=matrix(0,-30-bob,lean);
    const child=(x,y,r)=>multiply(body,matrix(x,y,r));
    const nearLift=walk?a*Math.max(0,stride)*7:0;
    const farLift=walk?a*Math.max(0,-stride)*7:0;
    const nearX=54+(walk?stride*5*a:0),farX=-56-(walk?stride*5*a:0);
    return {matrices:{
      body,
      head:child(-20,-238,a*(talk?Math.sin(t*4)*.018:greet?-.035+Math.sin(t*3)*.01:breath*.006)),
      farArm:child(-97,-180,.70+a*(walk?stride*.15:greet?Math.sin(t*10)*.13:breath*.018)),
      nearArm:child(96,-185,a*(walk?-stride*.12:talk?Math.sin(t*3)*.025:breath*.012)),
      tail:child(58,-54,a*(walk?-stride*.035:breath*.012)),
      pouch:child(-90,-110,0),
      nearLeg:matrix(nearX,-nearLift,0),farLeg:matrix(farX,-farLift,0)
    },feet:[{x:nearX,y:clean(-nearLift)},{x:farX,y:clean(-farLift)}]};
  }
  function blend(from,to,amount){
    const t=Number.isFinite(amount)?Math.min(1,Math.max(0,amount)):0;
    if(t===0)return from;if(t===1)return to;
    const lerp=(a,b)=>a+(b-a)*t,matrices={};
    for(const id of Object.keys(from.matrices)){
      const a=from.matrices[id],b=to.matrices[id];
      const first=Math.atan2(a[1],a[0]),last=Math.atan2(b[1],b[0]);
      const delta=Math.atan2(Math.sin(last-first),Math.cos(last-first));
      matrices[id]=matrix(lerp(a[4],b[4]),lerp(a[5],b[5]),first+delta*t);
    }
    const feet=from.feet.map((p,i)=>({x:lerp(p.x,to.feet[i].x),y:lerp(p.y,to.feet[i].y)}));
    // Smoothing between opposite steps must not leave both feet airborne.
    const floor=Math.max(...feet.map(p=>p.y));
    for(const [i,id] of ['nearLeg','farLeg'].entries()){
      feet[i].y=clean(feet[i].y-floor);matrices[id][5]=feet[i].y;
    }
    return {matrices,feet};
  }
  const api={pose,point,blend};
  if(typeof module!=='undefined'&&module.exports)module.exports=api;
  else root.NoahPartsRig=api;
})(typeof globalThis!=='undefined'?globalThis:this);
