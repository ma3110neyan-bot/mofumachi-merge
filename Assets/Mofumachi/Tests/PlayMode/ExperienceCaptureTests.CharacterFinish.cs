using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Mofumachi.Tests
{
    public sealed partial class ExperienceCaptureTests
    {
        [Serializable] private sealed class FinishFrame{public int tick;public string kind;public NativeImage image;}
        [Serializable] private sealed class FinishMotionSample{public int tick,reversals_in_half_second;public string scenario;public float x,y,dx,dy,clearance;}
        private void CaptureCharacterFinish(string output,TownLifeView field,TownActor[] actors,SixProof proof)
        {
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var snapshot=field.World.Capture();var frames=new List<FinishFrame>();var motion=new List<FinishMotionSample>();
            var parts=actors.Select(a=>a.CharacterId=="noah"?(Graphic)a.GetComponentInChildren<NoahAuthoredGraphic>():a.GetComponentInChildren<CharacterAuthoredGraphic>()).ToArray();
            var legacy=parts.Select(p=>p.GetType().GetField("captureLegacyPose",flags)).ToArray();foreach(var f in legacy)Assert.That(f,Is.Not.Null);
            void FrameAll()
            {
                field.World.Camera.Jump(new TownPoint(0,.12f));field.World.Refresh();field.RefreshWorldPresentation();Canvas.ForceUpdateCanvases();
                var layer=(RectTransform)actors[0].transform.parent;
                foreach(var actor in actors)
                {
                    var point=field.World.Local(actor.WorldPosition);
                    Assert.That(point.x,Is.InRange(layer.rect.xMin+38,layer.rect.xMax-38),actor.CharacterId+": six-character framing must retain tappable actors.");
                    Assert.That(point.y,Is.InRange(layer.rect.yMin+24,layer.rect.yMax-86));
                }
            }
            void Pose(NoahAuthoredMode mode,NoahAuthoredDirection direction,bool before)
            {
                for(int i=0;i<actors.Length;i++)
                {
                    legacy[i].SetValue(parts[i],before);
                    if(actors[i].CharacterId=="noah")((NoahAuthoredGraphic)parts[i]).SetPose(.37f,1.4f,mode==NoahAuthoredMode.Walk?1:0,mode,direction);
                    else ((CharacterAuthoredGraphic)parts[i]).SetPose(.37f,1.4f,mode==NoahAuthoredMode.Walk?1:0,mode,direction,false);
                }
            }
            try
            {
                FrameAll();
                foreach(var direction in new[]{NoahAuthoredDirection.Left,NoahAuthoredDirection.Right,NoahAuthoredDirection.Back})
                {
                    foreach(var actor in actors)typeof(TownActor).GetField(actor.CharacterId=="noah"?"authoredDirection":"characterDirection",flags).SetValue(actor,direction);
                    foreach(var mode in new[]{NoahAuthoredMode.Idle,NoahAuthoredMode.Walk,NoahAuthoredMode.Talk,NoahAuthoredMode.Greet})
                    {
                        string id="finish-"+direction.ToString().ToLowerInvariant()+"-"+mode.ToString().ToLowerInvariant();
                        var before=Portrait(()=>Pose(mode,direction,true));var after=Portrait(()=>Pose(mode,direction,false));
                        try
                        {
                            var allowed=actors.Select(actor=>EvidenceRect(ActorEvidenceRect(actor))).ToArray();var first=before.GetPixels32();var second=after.GetPixels32();int outside=0,changed=0;
                            for(int pixel=0;pixel<first.Length;pixel++)if(!first[pixel].Equals(second[pixel])){changed++;var point=new Vector2Int(pixel%720,pixel/720);if(!allowed.Any(rect=>rect.Contains(point)))outside++;}
                            Assert.That(outside,Is.Zero,"Head repair comparison cannot change the town or UI outside the six actor scopes.");
                            var a=SaveNativeImage(output,id+"-before.png",before);var b=SaveNativeImage(output,id+"-after.png",after);proof.images.Add(a);proof.images.Add(b);
                            proof.finish_pairs.Add(new NativePair{id=id,mode=mode.ToString(),comparison_kind="previous-2.5D-head-vs-attached-head-same-registered-art-fixed-pose",changed_pixels=changed,outside_allowed_pixels=outside,before=a,after=b});
                        }
                        finally{UnityEngine.Object.DestroyImmediate(before);UnityEngine.Object.DestroyImmediate(after);}
                    }
                }
                Assert.That(proof.finish_pairs.Count,Is.EqualTo(12));Assert.That(proof.finish_pairs.Any(pair=>pair.changed_pixels>0),Is.True);
                proof.attachment_comparison_verified=true;
                field.StartInteraction();
                for(int tick=0;tick<15;tick++){foreach(var waiting in field.World.Model.Residents)field.World.Model.Pause(waiting.Id,4);field.AdvanceFrame(1f/30);}
                FrameAll();
                // The real event raycast/drag checks run separately above; these
                // explicit pointer clicks cover every identity and duplicate gate.
                foreach(var actor in actors)
                {
                    field.StartInteraction();FrameAll();
                    var pointer=new PointerEventData(EventSystem.current){pointerId=71};
                    actor.OnPointerDown(pointer);actor.OnPointerUp(pointer);actor.OnPointerClick(pointer);FrameAll();
                    Assert.That(field.IsSpeaking,Is.True,actor.CharacterId+": tap must speak");
                    var text=Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Speaker name");Assert.That(text.text,Is.EqualTo(CharacterCatalog.Find(actor.CharacterId).Name));
                    var bubble=Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Town bubble");
                    actor.OnPointerDown(pointer);actor.OnPointerUp(pointer);actor.OnPointerClick(pointer);FrameAll();
                    Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Town bubble"),Is.SameAs(bubble));
                    for(int tick=0;tick<18;tick++){foreach(var waiting in field.World.Model.Residents)field.World.Model.Pause(waiting.Id,4);field.AdvanceFrame(1f/30);}
                    actor.OnPointerDown(pointer);actor.OnPointerUp(pointer);actor.OnPointerClick(pointer);FrameAll();
                    Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Town bubble"),Is.SameAs(bubble),"Repeated full pointer input must preserve visible speech beyond the cooldown.");
                    Assert.That(bubble.GetComponentInParent<CanvasGroup>().blocksRaycasts,Is.False);
                    var image=Portrait();try{proof.images.Add(SaveNativeImage(output,"finish-tap-"+actor.CharacterId+".png",image));}finally{UnityEngine.Object.DestroyImmediate(image);}
                }
                proof.tap_only_verified=true;field.StartInteraction();
                var hero=actors.Single(a=>a.Controlled);var peer=actors.First(a=>!a.Controlled);
                foreach(bool stationary in new[]{false,true})
                {
                foreach(var actor in actors.Where(a=>a!=hero&&a!=peer))field.World.Model.Restore(actor.CharacterId,new TownPoint(-1.8f,-.4f),null);
                field.World.Model.Restore(hero.CharacterId,stationary?new TownPoint(-.20f,-.22f):new TownPoint(-.3f,-.3f),null);
                field.World.Model.Restore(peer.CharacterId,stationary?new TownPoint(0,-.30f):new TownPoint(.3f,-.3f),null);
                var goal=stationary?new TownPoint(.16f,-.22f):new TownPoint(.3f,-.3f);
                Assert.That(field.World.GuidePoint(goal),Is.True);
                var resident=field.World.Model.Residents.Single(a=>a.Id==peer.CharacterId);
                if(!stationary)Assert.That((bool)typeof(TownWorldWalkModel).GetMethod("Guide",flags).Invoke(field.World.Model,new object[]{resident,new TownPoint(-.3f,-.3f),null,true,false}),Is.True);
                float px=0,py=0;var reversals=new Queue<int>();string scenario=stationary?"stationary-neighbour":"head-on";
                for(int tick=0;tick<900&&field.World.Model.Hero.IsWalking;tick++)
                {
                    foreach(var actor in actors.Where(a=>a!=hero&&(a!=peer||stationary)))field.World.Model.Pause(actor.CharacterId,4);
                    field.AdvanceFrame(1f/30);field.World.Camera.Jump(new TownPoint(0,.12f));field.World.Refresh();
                    Assert.That(field.IsSpeaking,Is.False,"Passing cannot trigger speech.");
                    var h=field.World.Model.Hero;float x=h.Position.X-h.Previous.X,y=h.Position.Y-h.Previous.Y;
                    float distance=(float)Math.Sqrt(x*x+y*y),previous=(float)Math.Sqrt(px*px+py*py);
                    if(distance>.00001f&&previous>.00001f&&(x*px+y*py)/(distance*previous)<-.1f)reversals.Enqueue(tick);
                    while(reversals.Count>0&&tick-reversals.Peek()>15)reversals.Dequeue();
                    Assert.That(reversals.Count,Is.LessThanOrEqualTo(1),"Actual movement must not alternate rapidly, "+scenario+" tick="+tick);
                    if(distance>.00001f){px=x;py=y;}
                    float clearance=TownWalkableArea.Distance(hero.WorldPosition,peer.WorldPosition);
                    Assert.That(clearance,Is.GreaterThanOrEqualTo(.0669f));
                    motion.Add(new FinishMotionSample{tick=tick,scenario=scenario,x=h.Position.X,y=h.Position.Y,dx=x,dy=y,clearance=clearance,reversals_in_half_second=reversals.Count});
                    if(tick%10==0){var image=Portrait();try{frames.Add(new FinishFrame{tick=tick,kind=scenario+"-real-world-fixed-simulation-not-FPS",image=SaveNativeEncoding(output,"finish-"+(stationary?"contact":"cross")+"-"+tick.ToString("0000")+".jpg",image,image.EncodeToJPG(86))});}finally{UnityEngine.Object.DestroyImmediate(image);}}
                }
                Assert.That(field.World.Model.Hero.IsWalking,Is.False);
                Assert.That(TownWalkableArea.Distance(hero.WorldPosition,goal),Is.LessThan(.001f));
                }
                Assert.That(frames.Count,Is.GreaterThan(2));proof.passing_verified=proof.contact_stability_verified=true;
                File.WriteAllText(Path.Combine(output,"finish-motion.json"),JsonUtility.ToJson(new FinishMotion{samples=motion},true));
                File.WriteAllText(Path.Combine(output,"finish-sequence.json"),JsonUtility.ToJson(new FinishSequence{frames=frames},true));
                string data=JsonUtility.ToJson(proof).Replace("<","\\u003c"),sequence=JsonUtility.ToJson(new FinishSequence{frames=frames}).Replace("<","\\u003c");
                File.WriteAllText(Path.Combine(output,"finish-review.html"),"<!doctype html><meta charset=utf-8><title>6名の最終確認</title><p>Unity隔離fixture：頭の接続Before/After、タップ会話、実モデルすれ違い。固定刻み標本でありPixel FPSではありません。</p><button id=play>すれ違い再生／停止</button><img id=frame width=280><div id=images></div><script>const p="+data+",s="+sequence+";let running=true,index=0;play.onclick=()=>running=!running;setInterval(()=>{frame.src=s.frames[index].image.file;if(running)index=(index+1)%s.frames.length},333);p.images.filter(i=>i.file.startsWith('finish-')).forEach(i=>{let f=document.createElement('figure'),c=document.createElement('figcaption'),im=document.createElement('img');c.textContent=i.file;im.src=i.file;im.style.width='280px';f.append(c,im);images.append(f)})</script>");
            }
            finally{for(int i=0;i<parts.Length;i++)legacy[i].SetValue(parts[i],false);field.World.Restore(snapshot);field.RefreshWorldPresentation();}
        }
        [Serializable] private sealed class FinishSequence{public string kind="actual world model, fixed1/30s simulation, not device FPS";public List<FinishFrame> frames;}
        [Serializable] private sealed class FinishMotion{public string kind="actual displacement each1/30s, not device FPS";public List<FinishMotionSample> samples;}
    }
}
