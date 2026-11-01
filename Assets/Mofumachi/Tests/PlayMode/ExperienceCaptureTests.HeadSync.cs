using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Mofumachi.Tests
{
    public sealed partial class ExperienceCaptureTests
    {
        [Serializable] private sealed class HeadSyncSample{public string character,state,direction;public int tick;public float head_x,head_y,body_x,body_y,error;public bool front_single_mesh,blink;}
        [Serializable] private sealed class HeadSyncProof
        {
            public string kind="six-character-approved-head-body-restore",gitRevision,productFingerprint,packed_resource_sha256;
            public string reference_revision="7c98804a78f07546fb8176b320904e8af44f6542",oracle="approved-noah-body-local-rest-frame";
            public string setup="isolated actual UGUI meshes; Noah-first continuous pose frames, no navigation/Pixel/FPS claim";
            public bool native_unity=true,save_unchanged,non_head_unchanged,head_art_size_angle_unchanged,continuous_verified,noah_first_verified,full_qa_verified=false,pixel_verified=false,formal_fixed=false,adopted=false;
            public int misalignments,unmet_characters;public string[] characters,verification_order;
            public List<HeadSyncSample> samples=new List<HeadSyncSample>(),continuous_samples=new List<HeadSyncSample>();public List<NativeImage> images=new List<NativeImage>();
        }
        private static NoahAuthoredDirection RestoreDirection(int frame)
        {
            if(frame<30)return NoahAuthoredDirection.Front;if(frame<60)return NoahAuthoredDirection.Left;
            if(frame<90)return NoahAuthoredDirection.Right;if(frame<150)return NoahAuthoredDirection.Back;
            if(frame<180)return NoahAuthoredDirection.Left;
            return new[]{NoahAuthoredDirection.Right,NoahAuthoredDirection.Back,NoahAuthoredDirection.Front,NoahAuthoredDirection.Left}[frame%4];
        }
        private static void MeasureApprovedHead(Graphic graphic,string character,HeadSyncSample sample,out int headIndex)
        {
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var mesh=graphic.canvasRenderer.GetMesh();Assert.That(mesh,Is.Not.Null);var vertices=mesh.vertices;
            var pose=(NoahAuthoredPose)graphic.GetType().GetField("pose",flags).GetValue(graphic);
            float width=0,height=0,scale=0,ox=0,oy=0,angle=0,formalWidth=254,formalHeight=323;Vector2 pivot=default;headIndex=-1;
            if(character=="noah")
            {
                var view=(NoahAuthoredView)graphic.GetType().GetField("view",flags).GetValue(graphic);
                for(int n=0;n<view.parts.Length;n++){var p=view.parts[n];if(p.id!="head")continue;headIndex=n*4;width=p.width;height=p.height;scale=p.scale;pivot=p.pivot;}
            }
            else
            {
                var view=(CharacterAuthoredView)graphic.GetType().GetField("view",flags).GetValue(graphic);
                var config=(CharacterAuthoredConfig)graphic.GetType().GetField("config",flags).GetValue(graphic);
                formalWidth=config.formalWidth;formalHeight=config.formalHeight;sample.blink=((CharacterAuthoredGraphic)graphic).EyesClosed;
                for(int n=0;n<view.parts.Length;n++){var p=view.PartFor(view.parts[n],sample.blink);if(p.id!="head")continue;headIndex=n*4;width=p.width;height=p.height;scale=p.scale;pivot=p.pivot;ox=p.offset.x;oy=p.offset.y;angle=p.rotation;}
            }
            Assert.That(headIndex,Is.GreaterThanOrEqualTo(0));Assert.That(width*height*scale,Is.GreaterThan(0));
            // Independent approved rest-frame oracle: NEVER use the new head's
            // X/Y, or match two alpha-edge points invented by the old resolver.
            float localX=sample.direction=="Right"?20:sample.direction=="Left"?-20:0;
            var expected=pose.Body.Child(localX,-238,pose.Head.Rotation-pose.Body.Rotation).Child(ox,oy,angle);
            var r=graphic.GetPixelAdjustedRect();float s=Mathf.Min(r.width/formalWidth,r.height/formalHeight)*formalHeight/600f;
            Vector3 actual=vertices[headIndex+1]+(vertices[headIndex+2]-vertices[headIndex+1])*pivot.x+(vertices[headIndex]-vertices[headIndex+1])*pivot.y;
            var target=new Vector3(r.center.x+expected.X*s,r.yMin-expected.Y*s);
            sample.head_x=actual.x;sample.head_y=actual.y;sample.body_x=target.x;sample.body_y=target.y;sample.error=Vector3.Distance(actual,target);
            Assert.That(sample.error,Is.LessThanOrEqualTo(.002f),sample.character+" "+sample.state+" tick="+sample.tick+" differs from approved rest placement");
        }
        [UnityTest,Timeout(180000)] public IEnumerator SixCharactersKeepHeadAndBodySynchronized()
        {
            if(!Environment.GetCommandLineArgs().Contains("-mofumachiHeadSync"))yield break;
            string run=EvidenceArgument("-mofumachiHeadSyncDirectory"),output=Path.Combine(run,"screenshots","head-sync");Directory.CreateDirectory(output);
            var proof=new HeadSyncProof{gitRevision=EvidenceArgument("-mofumachiSourceRevision"),productFingerprint=EvidenceArgument("-mofumachiProductFingerprint"),packed_resource_sha256=EvidenceArgument("-mofumachiPackedDigest")};
            yield return EnterHome();EnsurePortraitCamera();var field=Flow.GetComponentInChildren<TownLifeView>();field.StartInteraction();
            string saved=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;bool wasEnabled=field.enabled;field.enabled=false;
            var actors=field.Actors.OrderBy(a=>a.CharacterId,StringComparer.Ordinal).ToArray();proof.characters=actors.Select(a=>a.CharacterId).ToArray();
            Assert.That(proof.characters,Is.EquivalentTo(CharacterCatalog.All.Select(c=>c.Id)));
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var graphics=new Graphic[6];var previous=new FieldInfo[6];
            for(int i=0;i<actors.Length;i++)
            {
                var a=actors[i];field.World.Model.Restore(a.CharacterId,new TownPoint(-.30f+(i%3)*.30f,-.12f-(i/3)*.26f));
                if(a.CharacterId=="noah"){a.SetNoahAuthoredEnabled(true);graphics[i]=a.GetComponentInChildren<NoahAuthoredGraphic>();}
                else{Assert.That(a.SetCharacterAuthoredEnabled(true),Is.True);graphics[i]=a.GetComponentInChildren<CharacterAuthoredGraphic>();}
                previous[i]=graphics[i].GetType().GetField("capturePreviousNeck",flags);Assert.That(previous[i],Is.Not.Null);
            }
            field.World.Camera.Jump(new TownPoint(0,.12f));field.World.Refresh();
            string[] states={"front-idle","front-to-left-start","left-walk","left-to-right-walk","right-walk","right-to-back-walk","back-walk","back-to-front-stop-before","front-stop-after"};
            NoahAuthoredDirection[] directions={NoahAuthoredDirection.Front,NoahAuthoredDirection.Left,NoahAuthoredDirection.Left,NoahAuthoredDirection.Right,NoahAuthoredDirection.Right,NoahAuthoredDirection.Back,NoahAuthoredDirection.Back,NoahAuthoredDirection.Front,NoahAuthoredDirection.Front};
            try
            {
                var order=Enumerable.Range(0,actors.Length).OrderBy(i=>actors[i].CharacterId=="noah"?0:1).ThenBy(i=>actors[i].CharacterId,StringComparer.Ordinal).ToArray();
                proof.verification_order=order.Select(i=>actors[i].CharacterId).ToArray();
                foreach(int i in order)
                {
                    var actor=actors[i];Assert.That(graphics[i].transform.parent,Is.SameAs(actor.VisualRoot),"One common visual root.");
                    Assert.That(actor.VisualRoot.GetComponentsInChildren<Animator>(true).Length,Is.Zero,"No separate head animator.");
                    for(int frame=0;frame<240;frame++)
                    {
                        var direction=RestoreDirection(frame);bool walking=frame<120||frame>=150;
                        typeof(TownActor).GetField(actor.CharacterId=="noah"?"authoredDirection":"characterDirection",flags).SetValue(actor,direction);
                        foreach(var value in new[]{("breath",frame*.017f),("stride",frame*.24f),("amplitude",walking?Mathf.Min(1,(frame%30+1)/8f):0),("reactionRemaining",0f)})typeof(TownActor).GetField(value.Item1,flags).SetValue(actor,value.Item2);
                        typeof(TownActor).GetField("moving",flags).SetValue(actor,walking);field.RefreshWorldPresentation();
                        if(direction!=NoahAuthoredDirection.Front&&graphics[i] is CharacterAuthoredGraphic g)g.SetPose(frame*.017f,frame*.24f,walking?Mathf.Min(1,(frame%30+1)/8f):0,walking?NoahAuthoredMode.Walk:NoahAuthoredMode.Idle,direction,frame%17==0);
                        Canvas.ForceUpdateCanvases();
                        var sample=new HeadSyncSample{character=actor.CharacterId,state="continuous",direction=direction.ToString(),tick=frame,front_single_mesh=direction==NoahAuthoredDirection.Front};
                        if(sample.front_single_mesh){Assert.That(graphics[i].enabled,Is.False);Assert.That(actor.VisualRoot.GetComponent<CharacterBodyGraphic>().enabled,Is.True);}
                        else MeasureApprovedHead(graphics[i],actor.CharacterId,sample,out _);
                        proof.continuous_samples.Add(sample);yield return null;
                    }
                    if(actor.CharacterId=="noah")proof.noah_first_verified=true;
                    Assert.That(proof.noah_first_verified,Is.True,"No other character before Noah passes.");
                }
                proof.continuous_verified=true;
                for(int state=0;state<states.Length;state++)for(int tick=0;tick<8;tick++)
                {
                    bool moving=state>0&&state<8;float amplitude=state==1?(tick+1)/8f:state==7?(8-tick)/8f:moving?1:0;
                    for(int i=0;i<actors.Length;i++)
                    {
                        var a=actors[i];typeof(TownActor).GetField(a.CharacterId=="noah"?"authoredDirection":"characterDirection",flags).SetValue(a,directions[state]);
                        foreach(var value in new[]{("breath",state*.31f+tick*.037f),("stride",state*.47f+tick*.29f),("amplitude",amplitude),("reactionRemaining",0f)})typeof(TownActor).GetField(value.Item1,flags).SetValue(a,value.Item2);
                        typeof(TownActor).GetField("moving",flags).SetValue(a,moving);
                    }
                    void Pose(bool old)
                    {
                        for(int i=0;i<graphics.Length;i++)previous[i].SetValue(graphics[i],old);field.RefreshWorldPresentation();
                        // Pin both eye-material states in the same motion sample;
                        // the game's natural blink timing is untouched.
                        if(directions[state]!=NoahAuthoredDirection.Front)foreach(var g in graphics.OfType<CharacterAuthoredGraphic>())g.SetPose(state*.31f+tick*.037f,state*.47f+tick*.29f,amplitude,moving?NoahAuthoredMode.Walk:NoahAuthoredMode.Idle,directions[state],tick==7);
                        Canvas.ForceUpdateCanvases();
                    }
                    Pose(true);var before=graphics.Select(g=>g.canvasRenderer.GetMesh()?.vertices).ToArray();
                    var beforeUv=graphics.Select(g=>g.canvasRenderer.GetMesh()?.uv).ToArray();
                    void AssertImagePose()
                    {if(directions[state]!=NoahAuthoredDirection.Front)foreach(var g in graphics.OfType<CharacterAuthoredGraphic>())Assert.That(g.EyesClosed,Is.EqualTo(tick==7),"PNG and measured head material must be the same pose.");}
                    if(tick==0||tick==7){var image=Portrait(()=>Pose(true));try{AssertImagePose();proof.images.Add(SaveNativeImage(output,states[state]+"-"+tick+"-before.png",image));}finally{UnityEngine.Object.DestroyImmediate(image);}}
                    Pose(false);
                    for(int i=0;i<actors.Length;i++)
                    {
                        var sample=new HeadSyncSample{character=actors[i].CharacterId,state=states[state],direction=directions[state].ToString(),tick=tick,front_single_mesh=directions[state]==NoahAuthoredDirection.Front};
                        Assert.That(actors[i].RegionVisible,Is.True);var corners=new Vector3[4];actors[i].VisualRoot.GetWorldCorners(corners);
                        foreach(var corner in corners){var screen=captureCamera.WorldToViewportPoint(corner);Assert.That(screen.x,Is.InRange(0f,1f));Assert.That(screen.y,Is.InRange(0f,1f));}
                        if(sample.front_single_mesh)
                        {Assert.That(graphics[i].enabled,Is.False);Assert.That(actors[i].VisualRoot.GetComponent<CharacterBodyGraphic>().enabled,Is.True);}
                        else
                        {
                            var mesh=graphics[i].canvasRenderer.GetMesh();Assert.That(mesh,Is.Not.Null);var vertices=mesh.vertices;
                            MeasureApprovedHead(graphics[i],actors[i].CharacterId,sample,out int headIndex);
                            Assert.That(headIndex,Is.GreaterThanOrEqualTo(0));Assert.That(before[i],Is.Not.Null);Assert.That(vertices.Length,Is.EqualTo(before[i].Length));
                            Assert.That(mesh.uv,Is.EqualTo(beforeUv[i]),"Source art and UVs must be identical.");
                            for(int v=0;v<vertices.Length;v++)if(v<headIndex||v>=headIndex+4)Assert.That(vertices[v],Is.EqualTo(before[i][v]),"Only head placement may change.");
                            for(int v=headIndex+1;v<headIndex+4;v++)Assert.That(Vector3.Distance(vertices[v]-vertices[headIndex],before[i][v]-before[i][headIndex]),Is.LessThan(.0002f),"Keep head size and animation angle.");
                            if(sample.error>.002f)proof.misalignments++;
                            Assert.That(sample.error,Is.LessThanOrEqualTo(.002f),sample.character+" "+sample.state+" tick="+tick);
                        }
                        proof.samples.Add(sample);
                    }
                    if(tick==0||tick==7){var image=Portrait(()=>Pose(false));try{AssertImagePose();proof.images.Add(SaveNativeImage(output,states[state]+"-"+tick+"-after.png",image));}finally{UnityEngine.Object.DestroyImmediate(image);}}
                }
                Assert.That(proof.samples.Count,Is.EqualTo(432));Assert.That(proof.continuous_samples.Count,Is.EqualTo(1440));
                proof.unmet_characters=proof.characters.Count(id=>proof.samples.Count(s=>s.character==id)!=72||proof.samples.Any(s=>s.character==id&&s.error>.002f));
                Assert.That(proof.unmet_characters,Is.Zero);Assert.That(proof.misalignments,Is.Zero);
                foreach(string id in proof.characters)Assert.That(proof.samples.Count(s=>s.character==id),Is.EqualTo(72));
                Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(saved));Assert.That(Store.Writes,Is.EqualTo(writes));proof.save_unchanged=proof.non_head_unchanged=proof.head_art_size_angle_unchanged=true;
                File.WriteAllText(Path.Combine(output,"head-sync-proof.json"),JsonUtility.ToJson(proof,true));
                File.WriteAllText(Path.Combine(output,"head-sync-review.html"),"<!doctype html><meta charset=utf-8><title>頭胴体の同期確認</title><p>実Unityの限定状態・同一素材比較。全件／Pixel負荷の結果ではありません。</p><div id=images></div><script>const p="+JsonUtility.ToJson(proof).Replace("<","\\u003c")+";p.images.forEach(i=>{let f=document.createElement('figure'),c=document.createElement('figcaption'),m=document.createElement('img');c.textContent=i.file;m.src=i.file;m.style.width='300px';f.append(c,m);images.append(f)})</script>");
                LogAssert.NoUnexpectedReceived();
            }
            finally{for(int i=0;i<graphics.Length;i++)previous[i].SetValue(graphics[i],false);field.enabled=wasEnabled;}
        }
    }
}
