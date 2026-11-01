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
            public string kind="six-character-head-body-sync-only",gitRevision,productFingerprint,packed_resource_sha256;
            public string setup="isolated actual UGUI meshes; sampled current motion, no navigation/FPS claim";
            public bool native_unity=true,save_unchanged,non_head_unchanged,head_art_size_angle_unchanged,full_qa_verified=false,pixel_verified=false,formal_fixed=false,adopted=false;
            public int misalignments,unmet_characters;public string[] characters;
            public List<HeadSyncSample> samples=new List<HeadSyncSample>();public List<NativeImage> images=new List<NativeImage>();
        }
        [UnityTest,Timeout(180000)] public IEnumerator SixCharactersKeepHeadAndBodySynchronized()
        {
            if(!Environment.GetCommandLineArgs().Contains("-mofumachiHeadSync"))yield break;
            string run=EvidenceArgument("-mofumachiHeadSyncDirectory"),output=Path.Combine(run,"screenshots","head-sync");Directory.CreateDirectory(output);
            var proof=new HeadSyncProof{gitRevision=EvidenceArgument("-mofumachiSourceRevision"),productFingerprint=EvidenceArgument("-mofumachiProductFingerprint"),packed_resource_sha256=EvidenceArgument("-mofumachiPackedDigest")};
            yield return EnterHome();EnsurePortraitCamera();var field=Flow.GetComponentInChildren<TownLifeView>();field.StartInteraction();
            string saved=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;
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
                    if(tick==0||tick==7){var image=Portrait();try{proof.images.Add(SaveNativeImage(output,states[state]+"-"+tick+"-before.png",image));}finally{UnityEngine.Object.DestroyImmediate(image);}}
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
                            Vector3 HeadPoint(int index,float width,float height,float scale,Vector2 pivot,Vector2 joint)
                            {float u=(joint.x/scale+pivot.x*width)/width,v=(joint.y/scale+pivot.y*height)/height;return vertices[index+1]+(vertices[index+2]-vertices[index+1])*u+(vertices[index]-vertices[index+1])*v;}
                            Vector3 head=default,body=default;int headIndex=-1;
                            if(actors[i].CharacterId=="noah")
                            {
                                var view=(NoahAuthoredView)graphics[i].GetType().GetField("view",flags).GetValue(graphics[i]);
                                for(int part=0;part<view.parts.Length;part++){var p=view.parts[part];if(p.id=="head"){headIndex=part*4;head=HeadPoint(headIndex,p.width,p.height,p.scale,p.pivot,p.neckJoint);}if(p.id=="body")body=HeadPoint(part*4,p.width,p.height,p.scale,p.pivot,p.neckJoint);}
                            }
                            else
                            {
                                var view=(CharacterAuthoredView)graphics[i].GetType().GetField("view",flags).GetValue(graphics[i]);sample.blink=((CharacterAuthoredGraphic)graphics[i]).EyesClosed;
                                for(int part=0;part<view.parts.Length;part++){var p=view.PartFor(view.parts[part],sample.blink);if(p.id=="head"){headIndex=part*4;head=HeadPoint(headIndex,p.width,p.height,p.scale,p.pivot,p.neckJoint);}if(p.id=="body")body=HeadPoint(part*4,p.width,p.height,p.scale,p.pivot,p.neckJoint);}
                            }
                            Assert.That(headIndex,Is.GreaterThanOrEqualTo(0));Assert.That(before[i],Is.Not.Null);Assert.That(vertices.Length,Is.EqualTo(before[i].Length));
                            Assert.That(mesh.uv,Is.EqualTo(beforeUv[i]),"Source art and UVs must be identical.");
                            for(int v=0;v<vertices.Length;v++)if(v<headIndex||v>=headIndex+4)Assert.That(vertices[v],Is.EqualTo(before[i][v]),"Only head placement may change.");
                            for(int v=headIndex+1;v<headIndex+4;v++)Assert.That(Vector3.Distance(vertices[v]-vertices[headIndex],before[i][v]-before[i][headIndex]),Is.LessThan(.0002f),"Keep head size and animation angle.");
                            sample.head_x=head.x;sample.head_y=head.y;sample.body_x=body.x;sample.body_y=body.y;sample.error=Vector3.Distance(head,body);
                            if(sample.error>.002f)proof.misalignments++;
                            Assert.That(sample.error,Is.LessThanOrEqualTo(.002f),sample.character+" "+sample.state+" tick="+tick);
                        }
                        proof.samples.Add(sample);
                    }
                    if(tick==0||tick==7){var image=Portrait();try{proof.images.Add(SaveNativeImage(output,states[state]+"-"+tick+"-after.png",image));}finally{UnityEngine.Object.DestroyImmediate(image);}}
                }
                Assert.That(proof.samples.Count,Is.EqualTo(432));
                proof.unmet_characters=proof.characters.Count(id=>proof.samples.Count(s=>s.character==id)!=72||proof.samples.Any(s=>s.character==id&&s.error>.002f));
                Assert.That(proof.unmet_characters,Is.Zero);Assert.That(proof.misalignments,Is.Zero);
                foreach(string id in proof.characters)Assert.That(proof.samples.Count(s=>s.character==id),Is.EqualTo(72));
                Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(saved));Assert.That(Store.Writes,Is.EqualTo(writes));proof.save_unchanged=proof.non_head_unchanged=proof.head_art_size_angle_unchanged=true;
                File.WriteAllText(Path.Combine(output,"head-sync-proof.json"),JsonUtility.ToJson(proof,true));
                File.WriteAllText(Path.Combine(output,"head-sync-review.html"),"<!doctype html><meta charset=utf-8><title>頭胴体の同期確認</title><p>実Unityの限定状態・同一素材比較。全件／Pixel負荷の結果ではありません。</p><div id=images></div><script>const p="+JsonUtility.ToJson(proof).Replace("<","\\u003c")+";p.images.forEach(i=>{let f=document.createElement('figure'),c=document.createElement('figcaption'),m=document.createElement('img');c.textContent=i.file;m.src=i.file;m.style.width='300px';f.append(c,m);images.append(f)})</script>");
                LogAssert.NoUnexpectedReceived();
            }
            finally{for(int i=0;i<graphics.Length;i++)previous[i].SetValue(graphics[i],false);}
        }
    }
}
