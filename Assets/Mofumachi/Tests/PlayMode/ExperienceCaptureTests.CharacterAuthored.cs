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
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
namespace Mofumachi.Tests
{
    public sealed partial class ExperienceCaptureTests
    {
        [Serializable] private sealed class CharacterPartProof
        {
            public string character="momo",kind="single-character-native-parts",gitRevision,productFingerprint,input_sha256;
            public bool native_unity=true,pixel_verified=false,adopted=false,tap_verified,conversation_verified,save_unchanged,greeting_authored,talk_authored,blink_authored;
            public NativeImage blink_image;
            public string sequence_kind="actual public GuidePoint and AdvanceFrame, fixed simulation steps, not FPS";
            public string[] observed_directions;public int sorting_examples;
            public List<NativePair> pairs=new List<NativePair>();public List<NativeImage> frames=new List<NativeImage>(),sorting_images=new List<NativeImage>();
        }
        private NativePair CaptureAdditionalPair(string output,TownLifeView field,TownActor actor,string label)
        {
            Texture2D before=null,after=null;
            try
            {
                actor.SetCharacterAuthoredEnabled(false);before=Portrait();var area=EvidenceRect(ActorEvidenceRect(actor));
                string state=StateCodec.Encode(Flow.Game.State),model=ModelEvidence(field),others=ActorEvidence(field,true,actor.CharacterId),actors=ActorEvidence(field,false);
                var camera=field.World.Camera.Center;int frame=Time.frameCount,writes=Store.Writes;
                var water=field.GetComponentInChildren<TownFountainWaterGraphic>();var ambient=field.GetComponentInChildren<TownAmbientGraphic>();float waterClock=water.ShowElapsed,waterPhase=water.Phase,ambientPhase=ambient.Phase;
                actor.SetCharacterAuthoredEnabled(true);after=Portrait();
                Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(state));Assert.That(ModelEvidence(field),Is.EqualTo(model));
                Assert.That(ActorEvidence(field,true,actor.CharacterId),Is.EqualTo(others));Assert.That(field.World.Camera.Center,Is.EqualTo(camera));
                Assert.That(Time.frameCount,Is.EqualTo(frame));Assert.That(Store.Writes,Is.EqualTo(writes));
                var graphic=actor.GetComponentInChildren<CharacterAuthoredGraphic>();
                if(graphic.enabled)foreach(var v in graphic.canvasRenderer.GetMesh().vertices)
                {float z=captureCamera.WorldToScreenPoint(graphic.transform.TransformPoint(v)).z;Assert.That(z,Is.GreaterThan(captureCamera.nearClipPlane).And.LessThan(captureCamera.farClipPlane));}
                var pair=new NativePair{id=label,mode=actor.CharacterDirection.ToString(),comparison_kind="original-vs-Noah-method-same-clock",unity_frame=frame,store_writes=writes,camera_x=camera.X,camera_y=camera.Y,
                    allowed_x=area.x,allowed_y=area.y,allowed_width=area.width,allowed_height=area.height,water_clock=waterClock,water_phase=waterPhase,original_water_phase=ambientPhase,
                    state_sha256=EvidenceSha(System.Text.Encoding.UTF8.GetBytes(state)),world_model_sha256=EvidenceSha(System.Text.Encoding.UTF8.GetBytes(model)),
                    frame_unchanged=Time.frameCount==frame,actors_unchanged=ActorEvidence(field,false)==actors,other_five_unchanged=ActorEvidence(field,true,actor.CharacterId)==others,camera_unchanged=field.World.Camera.Center.Equals(camera),
                    water_clock_unchanged=water.ShowElapsed==waterClock,water_phase_unchanged=water.Phase==waterPhase&&ambient.Phase==ambientPhase,state_unchanged=StateCodec.Encode(Flow.Game.State)==state,store_writes_unchanged=Store.Writes==writes,world_model_unchanged=ModelEvidence(field)==model};
                Assert.That(new[]{pair.frame_unchanged,pair.actors_unchanged,pair.other_five_unchanged,pair.camera_unchanged,pair.water_clock_unchanged,pair.water_phase_unchanged,pair.state_unchanged,pair.store_writes_unchanged,pair.world_model_unchanged},Is.All.True);
                var a=before.GetPixels32();var b=after.GetPixels32();for(int i=0;i<a.Length;i++)if(!a[i].Equals(b[i]))
                {pair.changed_pixels++;if(!area.Contains(new Vector2Int(i%720,i/720)))pair.outside_allowed_pixels++;}
                pair.before=SaveNativeImage(output,label+"-before.png",before);pair.after=SaveNativeImage(output,label+"-after.png",after);
                Assert.That(pair.changed_pixels,Is.GreaterThan(0));Assert.That(pair.outside_allowed_pixels,Is.Zero);return pair;
            }
            finally{actor.SetCharacterAuthoredEnabled(true);if(before!=null)UnityEngine.Object.DestroyImmediate(before);if(after!=null)UnityEngine.Object.DestroyImmediate(after);}
        }
        [UnityTest,Timeout(120000)] public IEnumerator MomoAuthoredPartsPreserveIdentityInputAndState()
        {
            if(!Environment.GetCommandLineArgs().Contains("-mofumachiCharacterPreview"))
            {Assert.That(Resources.Load<TextAsset>("Mofumachi/CharacterAuthored/momo/layout"),Is.Null);yield break;}
            yield return EnterHome();Assert.That(Flow.Game.SelectFavorite("momo"),Is.True);Flow.ShowHome();yield return null;
            EnsurePortraitCamera();var field=Flow.GetComponentInChildren<TownLifeView>();field.StartInteraction();
            var actor=field.Actors.Single(a=>a.CharacterId=="momo");var friend=field.Actors.Single(a=>a.CharacterId=="noah");
            string output=Path.Combine(OutputDirectory(),"momo-authored");Directory.CreateDirectory(output);
            var proof=new CharacterPartProof{gitRevision=EvidenceArgument("-mofumachiSourceRevision"),productFingerprint=EvidenceArgument("-mofumachiProductFingerprint")};
            var saved=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var social=typeof(TownLifeView).GetField("nextSocial",flags);social.SetValue(field,float.PositiveInfinity);
            foreach(var r in field.World.Model.Residents.Where(r=>!r.Controlled))field.World.Model.Pause(r.Id,120);
            field.World.Model.Restore("momo",CharacterAuthoredCaptureRoute.MomoStart,null);
            field.World.Model.Restore("noah",CharacterAuthoredCaptureRoute.PartnerStart,null);
            field.World.Camera.Jump(new TownPoint(0,.15f));field.World.Refresh();
            var warm=Portrait();UnityEngine.Object.DestroyImmediate(warm);
            var old=actor.VisualRoot.GetComponent<CharacterBodyGraphic>();var vertices=old.canvasRenderer.GetMesh().vertices;var uv=old.canvasRenderer.GetMesh().uv;
            var parent=actor.transform.parent;int order=actor.transform.GetSiblingIndex();var at=actor.WorldPosition;
            foreach(var a in field.Actors){Assert.That(a.SetCharacterAuthoredEnabled(true),Is.EqualTo(a==actor));Assert.That(a.CharacterAuthoredEnabled,Is.EqualTo(a==actor));}
            warm=Portrait();UnityEngine.Object.DestroyImmediate(warm);
            var graphic=actor.GetComponentInChildren<CharacterAuthoredGraphic>();Assert.That(graphic,Is.Not.Null);
            Assert.That(graphic.CharacterId,Is.EqualTo("momo"));proof.input_sha256=graphic.InputDigest;
            Assert.That(graphic.canvasRenderer.GetMesh().vertexCount,Is.EqualTo(28));
            Assert.That(graphic.transform.parent,Is.SameAs(actor.VisualRoot));Assert.That(actor.transform.parent,Is.SameAs(parent));
            Assert.That(actor.transform.GetSiblingIndex(),Is.EqualTo(order));Assert.That(actor.WorldPosition,Is.EqualTo(at));
            Assert.That(ExecuteEvents.ExecuteHierarchy(graphic.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler),Is.SameAs(actor.gameObject));proof.tap_verified=true;field.StartInteraction();
            actor.SetCharacterAuthoredEnabled(false);warm=Portrait();UnityEngine.Object.DestroyImmediate(warm);
            Assert.That(old.enabled,Is.True);Assert.That(old.canvasRenderer.GetMesh().vertices.SequenceEqual(vertices),Is.True);Assert.That(old.canvasRenderer.GetMesh().uv.SequenceEqual(uv),Is.True);
            actor.SetCharacterAuthoredEnabled(true);
            void Pair(string label)
            {
                proof.pairs.Add(CaptureAdditionalPair(output,field,actor,label));
            }
            Pair("01-idle");var directions=new HashSet<NoahAuthoredDirection>();int unityFrame=Time.frameCount;int step=0;
            foreach(var target in CharacterAuthoredCaptureRoute.Targets)
            {
                field.StartInteraction();Assert.That(field.World.GuidePoint(target),Is.True);
                bool walked=false;
                for(int i=0;i<180;i++)
                {
                    foreach(var r in field.World.Model.Residents.Where(r=>!r.Controlled))field.World.Model.Pause(r.Id,4);
                    field.AdvanceFrame(1f/30);field.World.Camera.Jump(new TownPoint(0,.15f));field.World.Refresh();step++;
                    walked|=field.World.Model.Hero.IsWalking;directions.Add(actor.CharacterDirection);
                    Assert.That(old.enabled,Is.EqualTo(actor.CharacterDirection==NoahAuthoredDirection.Front));
                    Assert.That(graphic.enabled,Is.EqualTo(actor.CharacterDirection!=NoahAuthoredDirection.Front));
                    if(graphic.enabled&&graphic.EyesClosed&&!proof.blink_authored)
                    {
                        warm=Portrait();try{proof.blink_image=SaveNativeImage(output,"natural-blink.png",warm);proof.blink_authored=true;}finally{UnityEngine.Object.DestroyImmediate(warm);}
                    }
                    if(i==12)
                    {
                        Pair("walk-"+step.ToString("0000"));
                        if(actor.CharacterDirection==NoahAuthoredDirection.Front)
                        {
                            var actual=old.canvasRenderer.GetMesh().vertices;old.Pose(0,0,0,false);Canvas.ForceUpdateCanvases();
                            Assert.That(old.canvasRenderer.GetMesh().vertices.SequenceEqual(actual),Is.True,"Authored front must use the master's undeformed original mesh.");field.RefreshWorldPresentation();
                        }
                    }
                    if(i%12==0)
                    {
                        warm=Portrait();try{proof.frames.Add(SaveNativeEncoding(output,"frame-"+step.ToString("0000")+".jpg",warm,warm.EncodeToJPG(86)));}
                        finally{UnityEngine.Object.DestroyImmediate(warm);}
                    }
                    if(i>12&&!field.World.Model.Hero.IsWalking)break;
                }
                Assert.That(walked,Is.True,"The candidate must be evaluated through actual walking.");
                Assert.That(field.World.Model.Hero.IsWalking,Is.False,"Route must finish rather than remain stuck.");
                field.World.CancelHero();field.StartInteraction();Pair("stop-"+step.ToString("0000"));
            }
            foreach(var d in new[]{NoahAuthoredDirection.Front,NoahAuthoredDirection.Back,NoahAuthoredDirection.Left,NoahAuthoredDirection.Right})Assert.That(directions.Contains(d),Is.True,"Walk direction missing: "+d);
            Assert.That(proof.blink_authored,Is.True,"Natural blinking must be observed on the new part renderer.");
            Assert.That(field.World.GuidePoint(CharacterAuthoredCaptureRoute.ConversationTarget),Is.True);
            bool approached=false;
            for(int i=0;i<180;i++)
            {
                foreach(var r in field.World.Model.Residents.Where(r=>!r.Controlled))field.World.Model.Pause(r.Id,4);
                field.AdvanceFrame(1f/30);
                if(CharacterAuthoredCaptureRoute.ReadyForConversation(field.World.Model)){approached=true;break;}
            }
            Assert.That(approached,Is.True,"Reach the partner by actual lateral walking before intentionally stopping for conversation.");
            field.World.CancelHero();field.StartInteraction();Assert.That(actor.CharacterDirection,Is.EqualTo(NoahAuthoredDirection.Right));
            social.SetValue(field,0f);Assert.That(field.TryBeginConversation(actor,friend),Is.True);proof.conversation_verified=true;
            for(int i=0;i<34;i++)
            {
                foreach(var r in field.World.Model.Residents.Where(r=>!r.Controlled))field.World.Model.Pause(r.Id,4);
                field.AdvanceFrame(1f/30);
                if(i==8||i==30)
                {
                    Assert.That(graphic.enabled,Is.True,"The actual greeting/talk must show authored parts.");
                    Assert.That(graphic.CurrentMode,Is.EqualTo(i==8?NoahAuthoredMode.Greet:NoahAuthoredMode.Talk));
                    if(i==8)proof.greeting_authored=true;else proof.talk_authored=true;Pair(i==8?"greeting":"conversation");
                }
            }
            Assert.That(Time.frameCount,Is.EqualTo(unityFrame),"Fixed-step capture must not be interleaved with Update.");
            var snapshot=field.World.Capture();
            try
            {
                foreach(var point in new[]{new TownPoint(.36f,.14f),new TownPoint(.36f,.46f),new TownPoint(-.40f,.48f),new TownPoint(-1.2f,-.25f),new TownPoint(1.2f,-.25f)})
                {
                    // Explicit display placements for Sorting; not pathfinding evidence.
                    field.World.Model.Restore("momo",point,null);typeof(TownActor).GetField("characterDirection",flags).SetValue(actor,NoahAuthoredDirection.Left);field.World.Camera.Jump(point);field.World.Refresh();
                    warm=Portrait();try{proof.sorting_images.Add(SaveNativeImage(output,"sorting-"+proof.sorting_examples+".png",warm));}finally{UnityEngine.Object.DestroyImmediate(warm);}int actorOrder=actor.transform.GetSiblingIndex();
                    Assert.That(graphic.enabled,Is.True,"Sorting placements must show the new part renderer.");
                    foreach(var piece in field.GetComponent<TownSceneryView>().Pieces.Where(p=>p.Rect.gameObject.activeSelf))
                    {float delta=actor.WorldPosition.Y-TownSouthMasterLayout.DepthY(piece.Placement);if(Mathf.Abs(delta)>.0001f)Assert.That(actorOrder<piece.Rect.GetSiblingIndex(),Is.EqualTo(delta>0));}
                    var fountain=Flow.GetComponentInChildren<TownFountainGraphic>(true);
                    Assert.That(actorOrder<fountain.transform.GetSiblingIndex(),Is.EqualTo(point.Y>TownCentralMasterLayout.FountainCenterY));proof.sorting_examples++;
                }
            }
            finally{field.World.Restore(snapshot);field.RefreshWorldPresentation();}
            Assert.That(proof.pairs.All(p=>p.outside_allowed_pixels==0),Is.True);
            Assert.That(proof.pairs.Count(p=>p.changed_pixels>0),Is.GreaterThanOrEqualTo(6));
            Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(saved));Assert.That(Store.Writes,Is.EqualTo(writes));proof.save_unchanged=true;
            proof.observed_directions=directions.Select(d=>d.ToString()).OrderBy(d=>d).ToArray();
            File.WriteAllText(Path.Combine(output,"proof.json"),JsonUtility.ToJson(proof,true));LogAssert.NoUnexpectedReceived();
        }
    }
}
