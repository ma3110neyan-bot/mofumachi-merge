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
        private void CaptureCharacterFinish(string output,TownLifeView field,TownActor[] actors,SixProof proof)
        {
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var snapshot=field.World.Capture();var frames=new List<FinishFrame>();
            var parts=actors.Select(a=>a.CharacterId=="noah"?(Graphic)a.GetComponentInChildren<NoahAuthoredGraphic>():a.GetComponentInChildren<CharacterAuthoredGraphic>()).ToArray();
            var legacy=parts.Select(p=>p.GetType().GetField("captureLegacyPose",flags)).ToArray();foreach(var f in legacy)Assert.That(f,Is.Not.Null);
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
                field.RefreshWorldPresentation();
                // The real event raycast/drag checks run separately above; these
                // explicit pointer clicks cover every identity and duplicate gate.
                foreach(var actor in actors)
                {
                    field.StartInteraction();actor.OnPointerClick(new PointerEventData(EventSystem.current));
                    Assert.That(field.IsSpeaking,Is.True,actor.CharacterId+": tap must speak");
                    var text=Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Speaker name");Assert.That(text.text,Is.EqualTo(CharacterCatalog.Find(actor.CharacterId).Name));
                    var bubble=Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Town bubble");actor.OnPointerClick(new PointerEventData(EventSystem.current));
                    Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Town bubble"),Is.SameAs(bubble));
                    Assert.That(bubble.GetComponentInParent<CanvasGroup>().blocksRaycasts,Is.False);
                    var image=Portrait();try{proof.images.Add(SaveNativeImage(output,"finish-tap-"+actor.CharacterId+".png",image));}finally{UnityEngine.Object.DestroyImmediate(image);}
                }
                proof.tap_only_verified=true;field.StartInteraction();
                var hero=actors.Single(a=>a.Controlled);var peer=actors.First(a=>!a.Controlled);
                foreach(var actor in actors.Where(a=>a!=hero&&a!=peer))field.World.Model.Restore(actor.CharacterId,new TownPoint(-1.8f,-.4f),null);
                field.World.Model.Restore(hero.CharacterId,new TownPoint(-.3f,-.3f),null);field.World.Model.Restore(peer.CharacterId,new TownPoint(.3f,-.3f),null);
                Assert.That(field.World.GuidePoint(new TownPoint(.3f,-.3f)),Is.True);
                var resident=field.World.Model.Residents.Single(a=>a.Id==peer.CharacterId);
                Assert.That((bool)typeof(TownWorldWalkModel).GetMethod("Guide",flags).Invoke(field.World.Model,new object[]{resident,new TownPoint(-.3f,-.3f),null,true,false}),Is.True);
                for(int tick=0;tick<600&&field.World.Model.Hero.IsWalking;tick++)
                {
                    foreach(var actor in actors.Where(a=>a!=hero&&a!=peer))field.World.Model.Pause(actor.CharacterId,4);
                    field.AdvanceFrame(1f/30);field.World.Camera.Jump(new TownPoint(0,.12f));field.World.Refresh();
                    Assert.That(field.IsSpeaking,Is.False,"Passing cannot trigger speech.");
                    Assert.That(TownWalkableArea.Distance(hero.WorldPosition,peer.WorldPosition),Is.GreaterThanOrEqualTo(.0669f));
                    if(tick%10==0){var image=Portrait();try{frames.Add(new FinishFrame{tick=tick,kind="real-guided-passing-fixed-simulation-not-FPS",image=SaveNativeEncoding(output,"finish-cross-"+tick.ToString("0000")+".jpg",image,image.EncodeToJPG(86))});}finally{UnityEngine.Object.DestroyImmediate(image);}}
                }
                Assert.That(field.World.Model.Hero.IsWalking,Is.False);Assert.That(frames.Count,Is.GreaterThan(2));proof.passing_verified=true;
                File.WriteAllText(Path.Combine(output,"finish-sequence.json"),JsonUtility.ToJson(new FinishSequence{frames=frames},true));
                string data=JsonUtility.ToJson(proof).Replace("<","\\u003c"),sequence=JsonUtility.ToJson(new FinishSequence{frames=frames}).Replace("<","\\u003c");
                File.WriteAllText(Path.Combine(output,"finish-review.html"),"<!doctype html><meta charset=utf-8><title>6名の最終確認</title><p>Unity隔離fixture：頭の接続Before/After、タップ会話、実モデルすれ違い。固定刻み標本でありPixel FPSではありません。</p><button id=play>すれ違い再生／停止</button><img id=frame width=280><div id=images></div><script>const p="+data+",s="+sequence+";let running=true,index=0;play.onclick=()=>running=!running;setInterval(()=>{frame.src=s.frames[index].image.file;if(running)index=(index+1)%s.frames.length},333);p.images.filter(i=>i.file.startsWith('finish-')).forEach(i=>{let f=document.createElement('figure'),c=document.createElement('figcaption'),im=document.createElement('img');c.textContent=i.file;im.src=i.file;im.style.width='280px';f.append(c,im);images.append(f)})</script>");
            }
            finally{for(int i=0;i<parts.Length;i++)legacy[i].SetValue(parts[i],false);field.World.Restore(snapshot);field.RefreshWorldPresentation();}
        }
        [Serializable] private sealed class FinishSequence{public string kind="actual world model, fixed1/30s simulation, not device FPS";public List<FinishFrame> frames;}
    }
}
