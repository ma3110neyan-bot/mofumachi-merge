using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Mofumachi.Tests
{
    // Runs in the normal native suite. Optional output is actual UGUI rendering
    // from an isolated fixture, never the developer's/Pixel player's saved town.
    public sealed class ExperienceCaptureTests : SliceUiTestFixture
    {
        [Serializable] private sealed class NativeImage
        {public string file,sha256;public int bytes,width,height;}
        [Serializable] private sealed class NativePair
        {
            public string id,mode="",comparison_kind="";public bool body_unchanged_for_shadow;public int level,changed_pixels,outside_allowed_pixels,allowed_x,allowed_y,allowed_width,allowed_height;
            public bool display_heading_only;public float display_heading_x,display_heading_y;
            public float camera_x,camera_y,water_clock,water_phase,original_water_phase;public int unity_frame,store_writes;
            public string state_sha256,world_model_sha256;
            public bool frame_unchanged,actors_unchanged,camera_unchanged,water_clock_unchanged,water_phase_unchanged,state_unchanged,store_writes_unchanged,world_model_unchanged,other_five_unchanged;
            public NativeImage before,after,comparison;
            public NativeNameControl name_control;
        }
        [Serializable] private sealed class NativeNameVisibility {public string character_id;public bool active;}
        [Serializable] private sealed class NativeNameControl
        {
            public string mode="candidate-resident-name-visibility-in-reconstructed-before-only";
            public bool before_applied,after_natural,only_name_visibility_changed,natural_visibility_changed;
            public NativeNameVisibility[] natural_before,controlled_before,product_after;
            public NativeImage natural_before_image;
        }
        [Serializable] private sealed class NativePathPoint {public float x,y,elapsed_seconds;}
        [Serializable] private sealed class NativePath
        {
            public string id;public int steps;public float distance,start_x,start_y,target_x,target_y;
            public bool arrived,all_points_walkable=true;public NativeImage image;public List<NativePathPoint> points=new List<NativePathPoint>();
        }
        [Serializable] private sealed class NoahSequenceFrame
        {public int index;public string mode;public float elapsed_seconds,world_x,world_y,facing,reaction_seconds,head_facing,depth_forward;public bool walking;public NativeImage image;}
        [Serializable] private sealed class NoahSequence
        {
            public string setup="fresh-isolated-Home-scene-after-completed-route-proof";
            public bool isolated_start_verified;
            public string kind="native-fixed-step-town-AdvanceFrame",greet_trigger="public TryBeginConversation or normal AdvanceFrame auto; verified Noah/Momo greeting",conversation_mode="mutual-noah-momo-greeting";
            public string conversation_speaker_id,conversation_partner_id,conversation_text;
            public bool manual_advance=true,state_unchanged,store_writes_unchanged,actual_conversation_verified;
            public float step_seconds=1f/30,noah_reaction_seconds,momo_reaction_seconds,noah_facing,momo_facing;public int sample_step_count=3,source_width=720,source_height=1280,crop_x,crop_y,crop_width,crop_height;
            public List<NoahSequenceFrame> frames=new List<NoahSequenceFrame>();
        }
        [Serializable] private sealed class NorthWorldBounds {public float x_min=-.6f,x_max=.6f,y_min=1,y_max=1.32f;}
        [Serializable] private sealed class NoahSortingExample
        {public string id,kind="display-only world-foot-Y sorting, not route proof";public float world_x,world_y;public int actor_sibling,prop_sibling;public bool behind,correct;public NativeImage image;}
        [Serializable] private sealed class NorthNoahProof
        {
            public int noah_quality_version=3;
            public int noah_reprototype_version;
            public float capture_canvas_distance,capture_near_clip,capture_far_clip;
            public bool capture_clip_verified,current_before_branch_verified;
            public string baseline_source_revision,original_atlas_sha256;
            public List<NoahSortingExample> noah_sorting_examples=new List<NoahSortingExample>();
            public string kind="same-state-native-north-noah-comparison",before="reconstructed causal baseline: north join disabled / Noah legacy with product name visibility at the same instant",after="current product code";
            public string source_revision,product_fingerprint,historical_before="bb3fb82 native run102109 / 09-connected-path-north.png is historical context only, not this causal baseline";
            public bool native_unity=true,pixel_verified=false,adopted=false,navigation_changed=false,teleport_rescue=false;
            public NorthWorldBounds north_world_bounds=new NorthWorldBounds();
            public List<NativePair> north_pairs=new List<NativePair>(),noah_pairs=new List<NativePair>();
            public NativeImage historical_native_before;
            public List<NativePath> routes=new List<NativePath>();public NoahSequence sequence=new NoahSequence();
        }
        private static string EvidenceSha(byte[] bytes)
        {using(var hash=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        private static string EvidenceArgument(string name)
        {var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,name);return at>=0&&at+1<args.Length?args[at+1]:"";}
        private static NativeImage SaveNativeImage(string output,string name,Texture2D image)
        {
            return SaveNativeEncoding(output,name,image,image.EncodeToPNG());
        }
        private static NativeImage SaveNativeEncoding(string output,string name,Texture2D image,byte[] bytes)
        {
            if(output!=null){string path=Path.Combine(output,name);Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,bytes);}
            return new NativeImage{file=name,bytes=bytes.Length,width=image.width,height=image.height,sha256=EvidenceSha(bytes)};
        }
        private static string ModelEvidence(TownLifeView town)=>string.Join(";",town.World.Model.Residents.OrderBy(r=>r.Id).Select(r=>FormattableString.Invariant($"{r.Id}:{r.Position.X:R},{r.Position.Y:R}/{r.Previous.X:R},{r.Previous.Y:R}/{r.Target.X:R},{r.Target.Y:R}/{r.IsWalking}/{r.Stride:R}/{r.Facing:R}/{r.Rest:R}/{r.Speed:R}/{r.PauseRemaining:R}/{r.DistanceWalked:R}/{r.PlaceId}")));
        private static string ActorEvidence(TownLifeView town,bool visual,string except="")
        {
            var text=new System.Text.StringBuilder();
            foreach(var actor in town.Actors.Where(a=>a.CharacterId!=except).OrderBy(a=>a.CharacterId))
            {
                text.Append(actor.CharacterId).Append(':').Append(JsonUtility.ToJson(new TransformEvidence(actor.transform)));
                if(!visual)continue;text.Append(JsonUtility.ToJson(new TransformEvidence(actor.VisualRoot)));
                var mesh=actor.VisualRoot.GetComponent<CharacterBodyGraphic>().canvasRenderer.GetMesh();
                if(mesh!=null)
                {
                    foreach(var v in mesh.vertices)text.Append(FormattableString.Invariant($"/{v.x:R},{v.y:R},{v.z:R}"));
                    foreach(var uv in mesh.uv)text.Append(FormattableString.Invariant($"/uv{uv.x:R},{uv.y:R}"));
                    foreach(var color in mesh.colors32)text.Append($"/c{color.r},{color.g},{color.b},{color.a}");
                }
            }
            return text.ToString();
        }
        [Serializable] private sealed class TransformEvidence
        {
            public Vector3 position,scale;public Quaternion rotation;
            public TransformEvidence(Transform t){position=t.localPosition;scale=t.localScale;rotation=t.localRotation;}
        }
        private RectInt EvidenceRect(Rect bounds)
        {
            int x=Mathf.Clamp(Mathf.FloorToInt(bounds.xMin)-3,0,719),y=Mathf.Clamp(Mathf.FloorToInt(bounds.yMin)-3,0,1279);
            return new RectInt(x,y,Mathf.Clamp(Mathf.CeilToInt(bounds.xMax)+3,x+1,720)-x,Mathf.Clamp(Mathf.CeilToInt(bounds.yMax)+3,y+1,1280)-y);
        }
        private Rect ProjectEvidence(TownLifeView town,TownPoint low,TownPoint high)
        {
            Vector2 a=captureCamera.WorldToScreenPoint(town.transform.TransformPoint(town.World.Local(low))),b=captureCamera.WorldToScreenPoint(town.transform.TransformPoint(town.World.Local(high)));
            return Rect.MinMaxRect(Mathf.Min(a.x,b.x),Mathf.Min(a.y,b.y),Mathf.Max(a.x,b.x),Mathf.Max(a.y,b.y));
        }
        private Rect ActorEvidenceRect(TownActor actor)
        {
            var corners=new Vector3[4];actor.VisualRoot.GetWorldCorners(corners);var points=corners.Select(p=>(Vector2)captureCamera.WorldToScreenPoint(p)).ToArray();
            return Rect.MinMaxRect(points.Min(p=>p.x)-20,points.Min(p=>p.y)-20,points.Max(p=>p.x)+20,points.Max(p=>p.y)+20);
        }
        [Serializable] private sealed class NativeGraphicState
        {
            public string path,type;public bool active_self,active_in_hierarchy;
            public Rect screen;public Color color;
        }
        [Serializable] private sealed class NativePairFailure
        {
            public string kind="failed-native-pair-diagnostic",coordinate_origin="bottom-left",source_revision,product_fingerprint;
            public bool diagnostic_only=true,passed=false,pixel_verified=false,adopted=false;
            public Rect before_scope,after_scope;public NativePair pair;
            public int outside_x_min,outside_y_min,outside_x_max,outside_y_max;
            public NativeGraphicState[] before_graphics,after_graphics;
        }
        private static string GraphicPath(Transform t)
        {var path=t.name;while(t.parent!=null){t=t.parent;path=t.name+"/"+path;}return path;}
        private NativeGraphicState[] GraphicStates()
        {
            return Flow.GetComponentsInChildren<Graphic>(true).Select(g=>
            {
                var corners=new Vector3[4];g.rectTransform.GetWorldCorners(corners);
                var p=corners.Select(v=>(Vector2)captureCamera.WorldToScreenPoint(v)).ToArray();
                return new NativeGraphicState{path=GraphicPath(g.transform),type=g.GetType().Name,active_self=g.gameObject.activeSelf,active_in_hierarchy=g.gameObject.activeInHierarchy,
                    screen=Rect.MinMaxRect(p.Min(v=>v.x),p.Min(v=>v.y),p.Max(v=>v.x),p.Max(v=>v.y)),color=g.color};
            }).ToArray();
        }
        private static NativeNameVisibility[] NameVisibility(TownLifeView town)=>town.Actors.OrderBy(a=>a.CharacterId,StringComparer.Ordinal)
            .Select(a=>new NativeNameVisibility{character_id=a.CharacterId,active=a.transform.Find("Town actor name panel").gameObject.activeSelf}).ToArray();
        private static bool SameNames(NativeNameVisibility[] a,NativeNameVisibility[] b)=>a.Length==b.Length&&a.Zip(b,(x,y)=>x.character_id==y.character_id&&x.active==y.active).All(v=>v);
        private static void ApplyNames(TownLifeView town,NativeNameVisibility[] values)
        {foreach(var state in values)town.Actors.Single(a=>a.CharacterId==state.character_id).transform.Find("Town actor name panel").gameObject.SetActive(state.active);}
        private static bool OnlyNameVisibilityChanged(TownLifeView town,NativeGraphicState[] before,NativeGraphicState[] after)
        {
            var permitted=new HashSet<string>(town.Actors.SelectMany(a=>a.transform.Find("Town actor name panel").GetComponentsInChildren<Graphic>(true)).Select(g=>GraphicPath(g.transform)),StringComparer.Ordinal);
            if(before.Length!=after.Length)return false;
            for(int i=0;i<before.Length;i++)
            {
                var a=before[i];var b=after[i];
                if(a.path!=b.path||a.type!=b.type||!a.screen.Equals(b.screen)||!a.color.Equals(b.color))return false;
                if(a.active_self==b.active_self&&a.active_in_hierarchy==b.active_in_hierarchy)continue;
                if(!permitted.Contains(a.path))return false;
            }
            return true;
        }
        private NativePair CaptureNativePair(string output,TownLifeView town,string id,Action beforeSwitch,Action afterSwitch,Func<Rect> allowed,bool noah=false,Action beforeRender=null)
        {
            Texture2D before=null,after=null,naturalBefore=null;
            try
            {
                NativeNameVisibility[] candidateNames=null;NativeNameControl control=null;
                if(noah)
                {
                    // Current product captions are the fixed scene context for
                    // this reconstructed body-only comparison. The actual After
                    // never receives an override; natural legacy UI is retained.
                    afterSwitch();var warm=Portrait();UnityEngine.Object.DestroyImmediate(warm);candidateNames=NameVisibility(town);
                }
                beforeSwitch();naturalBefore=Portrait(beforeRender);
                if(noah)
                {
                    var naturalNames=NameVisibility(town);var naturalGraphics=GraphicStates();
                    before=Portrait(()=>{beforeRender?.Invoke();ApplyNames(town,candidateNames);});
                    control=new NativeNameControl{natural_before=naturalNames,controlled_before=NameVisibility(town),
                        before_applied=SameNames(candidateNames,NameVisibility(town)),natural_visibility_changed=!SameNames(naturalNames,candidateNames),
                        only_name_visibility_changed=OnlyNameVisibilityChanged(town,naturalGraphics,GraphicStates())};
                    if(control.natural_visibility_changed)control.natural_before_image=SaveNativeImage(output,id+"-ui-natural-before.png",naturalBefore);
                    Assert.That(control.before_applied&&control.only_name_visibility_changed,Is.True,id+": caption control must change only resident-name visibility in reconstructed Before");
                    if(!control.natural_visibility_changed)Assert.That(before.GetPixels32().SequenceEqual(naturalBefore.GetPixels32()),Is.True,"Unchanged natural UI must retain the exact original Before pixels");
                }
                else{before=naturalBefore;naturalBefore=null;}
                var bounds=allowed();var beforeScope=bounds;
                var beforeGraphics=noah?GraphicStates():null;
                var water=town.GetComponentInChildren<TownFountainWaterGraphic>();var original=town.GetComponentInChildren<TownAmbientGraphic>();
                string state=StateCodec.Encode(Flow.Game.State),model=ModelEvidence(town),actors=ActorEvidence(town,!noah),others=ActorEvidence(town,true,"noah");
                int frame=Time.frameCount,writes=Store.Writes;bool following=town.World.FollowingHero;var camera=town.World.Camera.Center;float clock=water.ShowElapsed,phase=water.Phase,originalPhase=original.Phase;
                afterSwitch();after=Portrait();var otherBounds=allowed();bounds=Rect.MinMaxRect(Mathf.Min(bounds.xMin,otherBounds.xMin),Mathf.Min(bounds.yMin,otherBounds.yMin),Mathf.Max(bounds.xMax,otherBounds.xMax),Mathf.Max(bounds.yMax,otherBounds.yMax));var area=EvidenceRect(bounds);
                if(noah){control.product_after=NameVisibility(town);control.after_natural=SameNames(candidateNames,control.product_after);Assert.That(control.after_natural,Is.True,"After must retain the unmodified natural product caption state");}
                var pair=new NativePair{id=id,level=town.TownLevel,camera_x=camera.X,camera_y=camera.Y,unity_frame=frame,store_writes=writes,water_clock=clock,water_phase=phase,original_water_phase=originalPhase,state_sha256=EvidenceSha(System.Text.Encoding.UTF8.GetBytes(state)),world_model_sha256=EvidenceSha(System.Text.Encoding.UTF8.GetBytes(model)),allowed_x=area.x,allowed_y=area.y,allowed_width=area.width,allowed_height=area.height};
                pair.name_control=control;
                int outsideMinX=720,outsideMinY=1280,outsideMaxX=-1,outsideMaxY=-1;
                var first=before.GetPixels32();var second=after.GetPixels32();for(int i=0;i<first.Length;i++)if(!first[i].Equals(second[i]))
                {
                    pair.changed_pixels++;int x=i%720,y=i/720;if(!area.Contains(new Vector2Int(x,y)))
                    {pair.outside_allowed_pixels++;outsideMinX=Math.Min(outsideMinX,x);outsideMinY=Math.Min(outsideMinY,y);outsideMaxX=Math.Max(outsideMaxX,x);outsideMaxY=Math.Max(outsideMaxY,y);}
                }
                pair.frame_unchanged=Time.frameCount==frame;pair.actors_unchanged=actors==ActorEvidence(town,!noah);pair.other_five_unchanged=others==ActorEvidence(town,true,"noah");pair.camera_unchanged=camera.Equals(town.World.Camera.Center)&&following==town.World.FollowingHero;
                pair.water_clock_unchanged=clock==water.ShowElapsed;pair.water_phase_unchanged=phase==water.Phase&&originalPhase==original.Phase;pair.state_unchanged=state==StateCodec.Encode(Flow.Game.State);pair.store_writes_unchanged=writes==Store.Writes;pair.world_model_unchanged=model==ModelEvidence(town);
                Assert.That(new[]{pair.frame_unchanged,pair.actors_unchanged,pair.other_five_unchanged,pair.camera_unchanged,pair.water_clock_unchanged,pair.water_phase_unchanged,pair.state_unchanged,pair.store_writes_unchanged,pair.world_model_unchanged},Is.All.True,"Causal switching must preserve the exact instant and all unrelated actors/state");
                if(pair.outside_allowed_pixels>0&&output!=null)
                {
                    // Keep the real rejected pixels before throwing. Diagnostic
                    // files cannot satisfy the completed native comparison gate.
                    pair.before=SaveNativeImage(output,"failed-pairs/"+id+"-FAILED-before.png",before);
                    pair.after=SaveNativeImage(output,"failed-pairs/"+id+"-FAILED-after.png",after);
                    if(noah&&!control.natural_visibility_changed)control.natural_before_image=pair.before;
                    var failure=new NativePairFailure{source_revision=EvidenceArgument("-mofumachiSourceRevision"),product_fingerprint=EvidenceArgument("-mofumachiProductFingerprint"),
                        before_scope=beforeScope,after_scope=otherBounds,pair=pair,outside_x_min=outsideMinX,outside_y_min=outsideMinY,outside_x_max=outsideMaxX,outside_y_max=outsideMaxY,
                        before_graphics=beforeGraphics,after_graphics=noah?GraphicStates():null};
                    File.WriteAllText(Path.Combine(output,"failed-pairs/"+id+"-FAILED.json"),JsonUtility.ToJson(failure,true));
                }
                Assert.That(pair.changed_pixels,Is.GreaterThan(0));Assert.That(pair.outside_allowed_pixels,Is.Zero,
                    $"{id}: causal pixels must stay within the projected scoped geometry; outside bbox=({outsideMinX},{outsideMinY})–({outsideMaxX},{outsideMaxY}); failed-pairs retains original PNG/JSON when output is enabled");
                pair.before=SaveNativeImage(output,id+"-before-reconstructed.png",before);pair.after=SaveNativeImage(output,id+"-after.png",after);
                if(noah&&!control.natural_visibility_changed)control.natural_before_image=pair.before;
                // Browser-review derivative of these exact two native renders;
                // the PNG originals above remain the causal pixel evidence.
                var comparison=new Texture2D(1440,1280,TextureFormat.RGB24,false);
                try
                {
                    comparison.SetPixels(0,0,720,1280,before.GetPixels());comparison.SetPixels(720,0,720,1280,after.GetPixels());comparison.Apply();
                    pair.comparison=SaveNativeEncoding(output,id+"-before-left-after-right.jpg",comparison,comparison.EncodeToJPG(90));
                }
                finally{UnityEngine.Object.DestroyImmediate(comparison);}
                return pair;
            }
            finally{afterSwitch();if(noah)town.RefreshWorldPresentation();if(before!=null)UnityEngine.Object.DestroyImmediate(before);if(after!=null)UnityEngine.Object.DestroyImmediate(after);if(naturalBefore!=null)UnityEngine.Object.DestroyImmediate(naturalBefore);}
        }
        [UnityTest,Timeout(120000)] public IEnumerator NoahReprototypeContinuousSequencePreservesArtAndSavedState()
        {
            yield return EnterHome();
            var initialNoah=Flow.GetComponentInChildren<TownLifeView>().Actors.Single(a=>a.CharacterId=="noah");
            Assert.That(initialNoah.transform.Find("Noah soft ground shadow").GetComponent<CanvasRenderer>(),Is.Not.Null,"Noah's shadow must own its renderer before Home is disabled or rebuilt.");
            Assert.That(Flow.Game.SelectFavorite("noah"),Is.True);Flow.ShowHome();yield return null;
            EnsurePortraitCamera();var field=Flow.GetComponentInChildren<TownLifeView>();field.StartInteraction();
            Assert.That(field.isActiveAndEnabled&&field.CanInteract,Is.True,"Manual capture must keep the real simulation and public input enabled.");
            var noah=field.Actors.Single(a=>a.CharacterId=="noah");var friend=field.Actors.Single(a=>a.CharacterId=="momo");
            Assert.That(noah.Controlled&&noah.NoahDepthFocusEnabled,Is.True);
            bool finalRig=Environment.GetCommandLineArgs().Contains("-mofumachiNoahFinalRig");
            void Candidate(bool on){if(finalRig)noah.SetNoahFinalRigEnabled(on);else noah.SetNoahDepthFocusEnabled(on);}
            if(finalRig)noah.SetNoahFinalRigEnabled(true);
            Assert.That(TownWalkableArea.Distance(noah.WorldPosition,TownWorldMap.Place("square")),Is.LessThan(.002f));
            Assert.That(TownWalkableArea.Distance(friend.WorldPosition,TownCentralMasterLayout.ArrivalFriend),Is.LessThan(.002f));
            string rootOutput=OutputDirectory(),output=rootOutput==null?null:Path.Combine(rootOutput,"noah-reprototype");
            var proof=new NorthNoahProof{noah_reprototype_version=2,kind="noah-depth-focus-comparison",source_revision=EvidenceArgument("-mofumachiSourceRevision"),product_fingerprint=EvidenceArgument("-mofumachiProductFingerprint"),
                baseline_source_revision="22f2831d834a4be380c69692f8dfbdb2cf912181",before="reconstructed current22 Noah renderer at identical clocks; shadow-only pair labeled separately",historical_before="rejected native Noah run123240, source22f2831"};
            if(finalRig){proof.noah_reprototype_version=3;proof.kind="noah-final-rig-comparison";proof.baseline_source_revision="f45106840405e0f2ff27296efa12d6c1255c1db9";proof.before="reconstructed currentf451 Noah renderer at identical clocks; shadow-only pair labeled separately";proof.historical_before="rejected native Noah run143238, sourcef451068";proof.after="opt-in experimental Noah rig code";}
            proof.capture_canvas_distance=captureCanvas.planeDistance;proof.capture_near_clip=captureCamera.nearClipPlane;proof.capture_far_clip=captureCamera.farClipPlane;
            string repo=Directory.GetParent(Application.dataPath).FullName;
            proof.original_atlas_sha256=EvidenceSha(File.ReadAllBytes(Path.Combine(repo,"Assets/Mofumachi/Resources/Mofumachi/CharacterBodyAtlas.png")));
            var sequence=proof.sequence;sequence.setup="fresh-isolated-Home-scene-Noah-only";sequence.kind="native-continuous-fixed-step-Noah-reprototype";sequence.sample_step_count=2;
            sequence.greet_trigger="public TryBeginConversation(noah,momo); automatic social timer held only in isolated fixture";sequence.conversation_mode="speaker-noah-to-momo";
            const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var socialField=typeof(TownLifeView).GetField("nextSocial",flags);var speakerField=typeof(TownLifeView).GetField("speaker",flags);
            var simulationTimeField=typeof(TownLifeView).GetField("now",flags);
            var facingField=typeof(TownActor).GetField("facing",flags);var reactionField=typeof(TownActor).GetField("reactionRemaining",flags);
            var headField=typeof(TownActor).GetField("noahHeadFacing",flags);var forwardField=typeof(TownActor).GetField("noahForward",flags);
            socialField.SetValue(field,float.PositiveInfinity); // No shipping cooldown change; choose an actual Noah-first interaction in this fixture.
            foreach(var r in field.World.Model.Residents.Where(r=>!r.Controlled))field.World.Model.Pause(r.Id,4);
            var npcPositions=field.World.Model.Residents.Where(r=>!r.Controlled).Select(r=>r.Position).ToArray();
            string saved=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;float elapsed=0;
            field.World.Camera.Jump(new TownPoint(0,.15f));field.World.Refresh();var warm=Portrait();UnityEngine.Object.DestroyImmediate(warm);
            Candidate(false);var currentBefore=Portrait();UnityEngine.Object.DestroyImmediate(currentBefore);
            var originalBody=noah.GetComponentInChildren<CharacterBodyGraphic>();var currentMesh=originalBody.canvasRenderer.GetMesh();
            Assert.That(currentMesh.vertexCount,Is.EqualTo(720));
            float[] originalXs={0,.06f,.16f,.18f,.30f,.303f,.37f,.42f,.47f,.53f,.535f,.625f,.65f,.70f,.84f,1};
            float[] originalYs={0,.06f,.14f,.22f,.28f,.36f,.44f,.49f,.53f,.60f,.67f,.80f,1};
            var originalVertices=currentMesh.vertices;var originalUvs=currentMesh.uv;var originalColors=currentMesh.colors32;var originalRoot=noah.VisualRoot.localToWorldMatrix;
            var initialShadow=noah.transform.Find("Noah soft ground shadow").GetComponent<Graphic>();var originalShadowRect=initialShadow.rectTransform.rect;var originalShadowColor=initialShadow.color;int vertexIndex=0;
            var currentPose=(NoahDepthFocusPose)typeof(CharacterBodyGraphic).GetField("depthPose",flags).GetValue(originalBody);
            var bodyRect=originalBody.rectTransform.rect;float artScale=Mathf.Min(bodyRect.width/254f,bodyRect.height/323f);
            for(int y=0;y<originalYs.Length-1;y++)for(int x=0;x<originalXs.Length-1;x++)
                foreach(var uv in new[]{new Vector2(originalXs[x],originalYs[y]),new Vector2(originalXs[x],originalYs[y+1]),new Vector2(originalXs[x+1],originalYs[y+1]),new Vector2(originalXs[x+1],originalYs[y])})
                {
                    if(finalRig){var expected=TownNoahDepthFocus.Vertex(uv.x,uv.y,254*artScale,323*artScale,currentPose);var actual=originalVertices[vertexIndex++];Assert.That(actual.x,Is.EqualTo(expected.X).Within(.00001f));Assert.That(actual.y,Is.EqualTo(bodyRect.yMin+expected.Y).Within(.00001f));Assert.That(actual.z,Is.EqualTo(expected.Z).Within(.00001f));}
                    else Assert.That(originalVertices[vertexIndex++].z,Is.EqualTo(TownNoah25DTrial.SurfaceDepth(uv.x,uv.y)),"Before must use the intact current22 surface, not older1d or 2D.");
                }
            proof.current_before_branch_verified=true;Candidate(true);var restored=Portrait();UnityEngine.Object.DestroyImmediate(restored);
            var noahMesh=noah.GetComponentInChildren<CharacterBodyGraphic>().canvasRenderer.GetMesh();
            if(finalRig)
            {
                Assert.That(noahMesh.uv.SequenceEqual(originalUvs),Is.True,"Final rig must keep the exact current atlas/blink UVs.");
                Assert.That(noahMesh.vertices.SequenceEqual(originalVertices),Is.False,"The final switch must actually change the body geometry.");
                Candidate(false);var roundTrip=Portrait();UnityEngine.Object.DestroyImmediate(roundTrip);var exact=originalBody.canvasRenderer.GetMesh();
                Assert.That(exact.vertices.SequenceEqual(originalVertices)&&exact.uv.SequenceEqual(originalUvs)&&exact.colors32.SequenceEqual(originalColors),Is.True,"OFF must restore independent f451 mesh snapshots, not a mutable Mesh reference.");
                Assert.That(noah.VisualRoot.localToWorldMatrix,Is.EqualTo(originalRoot));Assert.That(initialShadow.rectTransform.rect,Is.EqualTo(originalShadowRect));Assert.That(initialShadow.color,Is.EqualTo(originalShadowColor));
                Candidate(true);var reenabled=Portrait();UnityEngine.Object.DestroyImmediate(reenabled);noahMesh=originalBody.canvasRenderer.GetMesh();
            }
            Assert.That(noahMesh.vertices.Min(v=>v.z),Is.LessThan(-3));
            Assert.That(noahMesh.colors32.Min(c=>c.r),Is.LessThan(237));
            Assert.That(noahMesh.colors32.All(c=>c.r==c.g&&c.g==c.b&&c.a==255),Is.True);
            var actorBounds=ActorEvidenceRect(noah);var projection=ProjectEvidence(field,new TownPoint(-.45f,-.30f),new TownPoint(.45f,-.15f));
            var crop=EvidenceRect(Rect.MinMaxRect(Mathf.Min(actorBounds.xMin,projection.xMin),actorBounds.yMin-50,Mathf.Max(actorBounds.xMax,projection.xMax),actorBounds.yMax+140));
            sequence.crop_x=crop.x;sequence.crop_y=crop.y;sequence.crop_width=crop.width;sequence.crop_height=crop.height;sequence.isolated_start_verified=true;
            int sequenceUnityFrame=Time.frameCount;
            void Step()
            {
                foreach(var r in field.World.Model.Residents.Where(r=>!r.Controlled))field.World.Model.Pause(r.Id,4);
                float previousSimulationTime=(float)simulationTimeField.GetValue(field);
                field.AdvanceFrame(1f/30);elapsed+=1f/30;
                Assert.That((float)simulationTimeField.GetValue(field)-previousSimulationTime,Is.EqualTo(1f/30).Within(.00001f),"Every recorded step must advance the actual simulation, not just the capture timestamp.");
                Assert.That(Time.frameCount,Is.EqualTo(sequenceUnityFrame),"No Unity Update may interleave with the synchronous fixed-step capture.");
                Assert.That(field.World.Model.Residents.Where(r=>!r.Controlled).Select(r=>r.Position).ToArray(),Is.EqualTo(npcPositions));
            }
            for(int index=0;index<222;index++)
            {
                string mode=index<12?"idle":index<36?"walk":index<48?"stop":index<90?"turn-and-walk":index<96?"settle":index<144?"noah-speaking":index<174?"forward-away":index<210?"back-toward":"depth-stop";
                if(index==12)Assert.That(field.World.GuidePoint(new TownPoint(-.32f,-.25f)),Is.True);
                if(index==36){field.World.CancelHero();field.StartInteraction();}
                if(index==48)Assert.That(field.World.GuidePoint(new TownPoint(friend.WorldPosition.X-.20f,friend.WorldPosition.Y)),Is.True);
                if(index==96)
                {
                    Assert.That(field.World.Model.Hero.IsWalking,Is.False,"The actual walk must arrive before speaking");field.StartInteraction();socialField.SetValue(field,0f);
                    Assert.That(field.TryBeginConversation(noah,friend),Is.True,"Noah must start the real public conversation, not a forced smile or fake bubble");
                    Assert.That(speakerField.GetValue(field),Is.SameAs(noah));
                    sequence.actual_conversation_verified=true;sequence.conversation_speaker_id="noah";sequence.conversation_partner_id="momo";
                    sequence.conversation_text=Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Town bubble").text;
                    Assert.That(sequence.conversation_text,Is.EqualTo("こんにちは、モモ♪"));
                    sequence.noah_reaction_seconds=(float)reactionField.GetValue(noah);sequence.momo_reaction_seconds=(float)reactionField.GetValue(friend);
                    sequence.noah_facing=field.World.Model.Residents.Single(r=>r.Id=="noah").Facing;sequence.momo_facing=field.World.Model.Residents.Single(r=>r.Id=="momo").Facing;
                }
                if(index==144){field.StartInteraction();Assert.That(field.World.GuidePoint(new TownPoint(.14f,-.08f)),Is.True,"Actual forward walk must accept input.");}
                if(index==174){field.StartInteraction();Assert.That(field.World.GuidePoint(new TownPoint(.14f,-.30f)),Is.True,"Actual backward walk must accept input.");}
                if(index==210){field.World.CancelHero();field.StartInteraction();}
                for(int step=0;step<2;step++)Step();
                if(index>=96&&index<144)Assert.That(field.IsSpeaking&&object.ReferenceEquals(speakerField.GetValue(field),noah),Is.True,"The sequence must retain actual Noah speech");
                field.World.Camera.Jump(new TownPoint(0,.15f));field.World.Refresh();
                if(index==11||index==29||index==44||index==50||index==101||index==104||index==158||index==195)
                {
                    string pairMode=index==11?"idle":index==29?"walk":index==44?"stop":index==50?"turn":index==101?"talk":index==104?"reaction":index==158?"forward":"back";
                    var pair=CaptureNativePair(output,field,"38-noah-"+pairMode,()=>Candidate(false),()=>Candidate(true),()=>ActorEvidenceRect(noah),true);pair.mode=pairMode;pair.comparison_kind=finalRig?"currentf451-whole-noah":"current22-whole-noah";proof.noah_pairs.Add(pair);
                }
                var portrait=Portrait();Texture2D sample=null;
                try
                {
                    var bounds=ActorEvidenceRect(noah);
                    Assert.That(bounds.xMin>=crop.x&&bounds.xMax<=crop.xMax&&bounds.yMin>=crop.y&&bounds.yMax<=crop.yMax,Is.True,$"Noah frame{index} lies outside the declared crop: {bounds}/{crop}");
                    sample=new Texture2D(crop.width,crop.height,TextureFormat.RGB24,false);sample.SetPixels(portrait.GetPixels(crop.x,crop.y,crop.width,crop.height));sample.Apply();
                    sequence.frames.Add(new NoahSequenceFrame{index=index,mode=mode,elapsed_seconds=elapsed,world_x=noah.WorldPosition.X,world_y=noah.WorldPosition.Y,facing=(float)facingField.GetValue(noah),reaction_seconds=(float)reactionField.GetValue(noah),walking=field.World.Model.Hero.IsWalking,head_facing=(float)headField.GetValue(noah),depth_forward=(float)forwardField.GetValue(noah),image=SaveNativeEncoding(output,"38-noah-frame-"+index.ToString("00")+".jpg",sample,sample.EncodeToJPG(90))});
                }
                finally{UnityEngine.Object.DestroyImmediate(portrait);if(sample!=null)UnityEngine.Object.DestroyImmediate(sample);}
            }
            var shadow=noah.transform.Find("Noah soft ground shadow").GetComponent<Graphic>();
            var configure=shadow.GetType().GetMethod("ConfigureGrounding",flags);
            var bodyBeforeShadow=originalBody.canvasRenderer.GetMesh();var bodyVertices=bodyBeforeShadow.vertices;var bodyUvs=bodyBeforeShadow.uv;var bodyColors=bodyBeforeShadow.colors32;
            var rigConfigure=shadow.GetType().GetMethod("ConfigureFinalRig",flags);
            var shadowPair=CaptureNativePair(output,field,"38-noah-ground-shadow",()=>{},()=>{if(finalRig)rigConfigure.Invoke(shadow,null);else configure.Invoke(shadow,new object[]{true});},()=>ActorEvidenceRect(noah),true,()=>configure.Invoke(shadow,new object[]{finalRig}));
            var bodyAfterShadow=originalBody.canvasRenderer.GetMesh();shadowPair.body_unchanged_for_shadow=bodyAfterShadow.vertices.SequenceEqual(bodyVertices)&&bodyAfterShadow.uv.SequenceEqual(bodyUvs)&&bodyAfterShadow.colors32.SequenceEqual(bodyColors);
            Assert.That(shadowPair.body_unchanged_for_shadow,Is.True,"Shadow-only comparison must preserve the exact After body.");
            shadowPair.mode="ground-shadow";shadowPair.comparison_kind=finalRig?"currentf451-shadow-only-current-body":"current22-shadow-only-current-body";proof.noah_pairs.Add(shadowPair);
            if(finalRig)
            {
                float oldFacing=(float)facingField.GetValue(noah),oldHead=(float)headField.GetValue(noah),oldForward=(float)forwardField.GetValue(noah);
                try
                {
                    foreach(var at in new[]{("static-left",-1f,0f),("static-right",1f,0f),("static-away",0f,1f),("static-toward",0f,-1f)})
                    {
                        // Display-only held headings, distinct from the actual walking episode above.
                        facingField.SetValue(noah,at.Item2);headField.SetValue(noah,at.Item2);forwardField.SetValue(noah,at.Item3);
                        var pair=CaptureNativePair(output,field,"38-noah-"+at.Item1,()=>Candidate(false),()=>Candidate(true),()=>ActorEvidenceRect(noah),true);
                        pair.mode=at.Item1;pair.comparison_kind="currentf451-whole-noah";pair.display_heading_only=true;pair.display_heading_x=(float)facingField.GetValue(noah);pair.display_heading_y=(float)forwardField.GetValue(noah);
                        Assert.That(pair.display_heading_x,Is.EqualTo(at.Item2));Assert.That(pair.display_heading_y,Is.EqualTo(at.Item3));proof.noah_pairs.Add(pair);
                    }
                }
                finally{facingField.SetValue(noah,oldFacing);headField.SetValue(noah,oldHead);forwardField.SetValue(noah,oldForward);Candidate(true);field.RefreshWorldPresentation();}
            }
            sequence.state_unchanged=saved==StateCodec.Encode(Flow.Game.State);sequence.store_writes_unchanged=writes==Store.Writes;Assert.That(sequence.state_unchanged&&sequence.store_writes_unchanged,Is.True);
            var snapshot=field.World.Capture();
            try
            {
                field.StartInteraction();
                foreach(var item in new[]{("fountain-front",new TownPoint(.26f,.14f)),("fountain-back",new TownPoint(.26f,.46f))})
                {
                    field.World.Model.Restore("noah",item.Item2,null);field.World.Camera.Jump(item.Item2);field.World.Refresh();field.RefreshWorldPresentation();Canvas.ForceUpdateCanvases();
                    Assert.That(TownWalkableArea.Distance(noah.WorldPosition,item.Item2),Is.LessThan(.00001f),"Sorting examples must show the declared real world feet.");
                    var fountain=Flow.GetComponentInChildren<TownFountainGraphic>(true);int actorOrder=noah.transform.GetSiblingIndex(),propOrder=fountain.transform.GetSiblingIndex();bool behind=item.Item2.Y>TownCentralMasterLayout.FountainCenterY;
                    Assert.That(actorOrder<propOrder,Is.EqualTo(behind));
                    var image=Portrait();try{proof.noah_sorting_examples.Add(new NoahSortingExample{id=item.Item1,world_x=item.Item2.X,world_y=item.Item2.Y,actor_sibling=actorOrder,prop_sibling=propOrder,behind=behind,correct=true,image=SaveNativeImage(output,"38-noah-"+item.Item1+".png",image)});}finally{UnityEngine.Object.DestroyImmediate(image);}
                }
            }
            finally{field.World.Restore(snapshot);Candidate(true);field.RefreshWorldPresentation();}
            Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(saved));Assert.That(Store.Writes,Is.EqualTo(writes));
            if(output!=null)
            {
                proof.capture_clip_verified=true; // Every Portrait checks all transformed active Noah vertices before Camera.Render.
                File.WriteAllText(Path.Combine(output,"noah-reprototype.json"),JsonUtility.ToJson(proof,true));
                var html=new System.Text.StringBuilder("<!doctype html><meta charset='utf-8'><title>Noah 2.5D reprototype</title><style>body{font:16px sans-serif;background:#fff7ef;color:#50343b;max-width:1500px;margin:auto}img{max-width:100%}section{margin:2rem 0}</style><h1>ノア1名 再試作：実Unity</h1><p>Beforeは今回不合格となった22f2831の描画を同じ時計で再現。ground-shadowは身体をAfterのまま保った影だけの比較です。比較Beforeの名札だけ製品Afterと同じ表示条件です。Afterは自然な製品表示。原画は変更していません。採用／Pixel負荷は未確認です。</p>");
                if(finalRig)html.Replace("22f2831","f451068").Append("<p>最終方式比較：Before＝浅い凹凸方式、After＝同じ原画の重み付き2Dリグと明示XY奥行き投影。新しい側面／背面の絵ではありません。static-*は向きを保持した表示用比較で、実歩行の証拠は連続標本です。差が不足なら本方式を終了し、正式パーツ／向き別原画を用意する方式へ切り替えます。</p>");
                foreach(var pair in proof.noah_pairs){html.Append("<section><h2>").Append(pair.mode).Append("</h2><a href='").Append(pair.comparison.file).Append("'><img src='").Append(pair.comparison.file).Append("'></a>");if(pair.name_control.natural_visibility_changed)html.Append("<p><a href='").Append(pair.name_control.natural_before_image.file).Append("'>名札を揃える前の自然なBefore原本</a></p>");html.Append("</section>");}
                html.Append("<h2>静止→歩行→停止→方向転換→ノアの会話→前後歩行（CUTなし）</h2><p>1/30秒の実AdvanceFrameを2ステップごとに記録した連続標本（222コマ／14.8秒、JPEG）。14.8秒の終端でループします。実時間動画・FPS測定ではありません。会話開始の順番だけ隔離fixtureの自動会話タイマーで制御しています。</p><button id='play'>再生／停止</button><p id='time'></p><img id='frame'><script>const frames=").Append(JsonUtility.ToJson(sequence)).Append(".frames;let i=0,p=true;document.querySelector('#play').onclick=()=>p=!p;function tick(){const f=frames[i];document.querySelector('#frame').src=f.image.file;document.querySelector('#time').textContent=f.index+' / '+f.mode+' / '+f.elapsed_seconds.toFixed(3)+' simulated seconds';if(p)i=(i+1)%frames.length;setTimeout(tick,1000/15)}tick()</script><h2>噴水前後の表示例</h2><p>表示位置を置いたSorting確認で、実歩行の証拠とは別です。</p>");
                foreach(var example in proof.noah_sorting_examples)html.Append("<p>").Append(example.id).Append("</p><img src='").Append(example.image.file).Append("'>");
                File.WriteAllText(Path.Combine(output,"noah-reprototype.html"),html.ToString());
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest,Timeout(120000)] public IEnumerator NorthNoahBeforeAfterAndFixedStepEvidencePreserveSavedState()
        {
            yield return EnterHome();Assert.That(Flow.Game.SelectFavorite("noah"),Is.True);string output=OutputDirectory();
            var proof=new NorthNoahProof{source_revision=EvidenceArgument("-mofumachiSourceRevision"),product_fingerprint=EvidenceArgument("-mofumachiProductFingerprint")};
            foreach(int level in new[]{1,3,6})
            {
                // Representative levels exist only in this disposable test store;
                // real progression/reload remains covered by the existing gate.
                Flow.Game.State.townGrowthLevel=level;Flow.ShowHome();yield return null;EnsurePortraitCamera();
                var town=Flow.GetComponentInChildren<TownLifeView>();town.StartInteraction();town.World.Freeze();Assert.That(town.World.LookAt(TownDistrictId.North),Is.True);
                // Ground scenery is a sibling of the actor layer (TownLifeView),
                // so resolve this existing layer from the owning screen root.
                var blend=Flow.GetComponentInChildren<TownNorthEntryBlendGraphic>(true);Assert.That(blend,Is.Not.Null);Assert.That(blend.name,Is.EqualTo("North central road join"));
                for(int view=0;view<3;view++)
                {
                    town.World.Camera.Jump(new TownPoint(0,new[]{.85f,1f,1.15f}[view]));town.World.Refresh();var warm=Portrait();UnityEngine.Object.DestroyImmediate(warm);
                    proof.north_pairs.Add(CaptureNativePair(output,town,"36-north-lv"+level+"-view"+view,()=>blend.enabled=false,()=>blend.enabled=true,()=>ProjectEvidence(town,new TownPoint(-.6f,1),new TownPoint(.6f,1.32f))));
                }
            }
            var field=Flow.GetComponentInChildren<TownLifeView>();string saved=StateCodec.Encode(Flow.Game.State);int savedWrites=Store.Writes;
            NativePath Walk(string id,TownPoint target)
            {
                field.StartInteraction();var start=field.World.Model.Hero.Position;var route=new NativePath{id=id,start_x=start.X,start_y=start.Y,target_x=target.X,target_y=target.Y};
                route.points.Add(new NativePathPoint{x=start.X,y=start.Y,elapsed_seconds=0});Assert.That(field.World.GuidePoint(target),Is.True);
                var previous=start;while(field.World.Model.Hero.IsWalking&&route.steps<6000)
                {
                    field.AdvanceFrame(1f/30);route.steps++;var p=field.World.Model.Hero.Position;route.distance+=TownWalkableArea.Distance(previous,p);route.all_points_walkable&=field.World.Model.Area.Contains(p)&&field.World.Model.Area.CanTravel(previous,p);previous=p;
                    if(route.steps%3==0||!field.World.Model.Hero.IsWalking)route.points.Add(new NativePathPoint{x=p.X,y=p.Y,elapsed_seconds=route.steps/30f});
                }
                route.arrived=!field.World.Model.Hero.IsWalking&&TownWalkableArea.Distance(field.World.Model.Hero.Position,target)<.002f;
                Assert.That(route.arrived,Is.True,"Actual model walking must arrive without Restore/FastTravel rescue: "+id);Assert.That(route.all_points_walkable,Is.True);
                if(!id.StartsWith("setup-",StringComparison.Ordinal))
                {field.World.Camera.Jump(new TownPoint(0,1));field.World.Refresh();var image=Portrait();try{route.image=SaveNativeImage(output,"36-route-"+id+".png",image);}finally{UnityEngine.Object.DestroyImmediate(image);}}return route;
            }
            proof.routes.Add(Walk("central-to-north",new TownPoint(0,1.65f)));proof.routes.Add(Walk("north-to-central",TownWorldMap.Place("square")));
            foreach(var lane in new[]{("left",-.30f),("right",.30f)})
            {Walk("setup-"+lane.Item1,new TownPoint(lane.Item2,.95f));proof.routes.Add(Walk(lane.Item1+"-out",new TownPoint(lane.Item2,1.05f)));proof.routes.Add(Walk(lane.Item1+"-return",new TownPoint(lane.Item2,.95f)));}
            Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(saved));Assert.That(Store.Writes,Is.EqualTo(savedWrites));
            // Home deliberately preserves its resident snapshot. The route proof
            // above ends in North; a separate presentation episode must load a
            // fresh fixture scene, not mistake ShowHome for a position reset.
            var routedFlow=Flow;ReleasePortraitCamera();yield return EnterHome();
            Assert.That(Flow,Is.Not.SameAs(routedFlow));Assert.That(Flow.HomeWorldSnapshot,Is.Null);
            EnsurePortraitCamera();field=Flow.GetComponentInChildren<TownLifeView>();field.StartInteraction();field.World.LookAt(TownDistrictId.Central);field.World.Camera.Jump(new TownPoint(0,-.05f));field.World.Refresh();
            var noah=field.Actors.Single(a=>a.CharacterId=="noah");Assert.That(noah.Controlled&&noah.NoahTrialEnabled,Is.True);
            var friend=field.Actors.Single(a=>a.CharacterId=="momo");
            Assert.That(TownWalkableArea.Distance(noah.WorldPosition,TownWorldMap.Place("square")),Is.LessThan(.002f),"The independent presentation episode starts at the real initial square");
            Assert.That(TownWalkableArea.Distance(friend.WorldPosition,TownCentralMasterLayout.ArrivalFriend),Is.LessThan(.002f),"The greeting uses the original central resident, not the earlier route snapshot");
            proof.sequence.isolated_start_verified=true;
            foreach(var actor in field.World.Model.Residents.Where(r=>!r.Controlled))field.World.Model.Pause(actor.Id,4);
            var npcPositions=field.World.Model.Residents.Where(r=>!r.Controlled).Select(r=>r.Position).ToArray();
            var initial=Portrait();UnityEngine.Object.DestroyImmediate(initial);var cropBounds=ActorEvidenceRect(noah);var projected=ProjectEvidence(field,new TownPoint(-.45f,-.3f),new TownPoint(.45f,-.15f));cropBounds=Rect.MinMaxRect(Mathf.Min(cropBounds.xMin,projected.xMin),cropBounds.yMin-24,Mathf.Max(cropBounds.xMax,projected.xMax),cropBounds.yMax+80);var crop=EvidenceRect(cropBounds);
            var sequence=proof.sequence;sequence.crop_x=crop.x;sequence.crop_y=crop.y;sequence.crop_width=crop.width;sequence.crop_height=crop.height;
            saved=StateCodec.Encode(Flow.Game.State);savedWrites=Store.Writes;float elapsed=0;
            void Step()
            {
                // Pause is capped at four seconds by the real model. Renew it
                // each isolated step; do not assume a longer request can hold.
                foreach(var actor in field.World.Model.Residents.Where(r=>!r.Controlled))field.World.Model.Pause(actor.Id,4);
                field.AdvanceFrame(1f/30);elapsed+=1f/30;
                Assert.That(field.World.Model.Residents.Where(r=>!r.Controlled).Select(r=>r.Position).ToArray(),Is.EqualTo(npcPositions));
            }
            const System.Reflection.BindingFlags privateFields=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var speakerField=typeof(TownLifeView).GetField("speaker",privateFields);var reactionField=typeof(TownActor).GetField("reactionRemaining",privateFields);
            bool VerifiedGreeting()
            {
                var speaker=(TownActor)speakerField.GetValue(field);if(!field.IsSpeaking||(speaker!=noah&&speaker!=friend))return false;
                var partner=speaker==noah?friend:noah;string expected="こんにちは、"+CharacterCatalog.Find(partner.CharacterId).Name+"♪";
                var bubble=Flow.GetComponentsInChildren<Text>().SingleOrDefault(t=>t.name=="Town bubble");
                return bubble!=null&&bubble.text==expected&&(float)reactionField.GetValue(noah)>0&&(float)reactionField.GetValue(friend)>0
                    &&field.World.Model.Residents.Single(r=>r.Id=="noah").Facing==1&&field.World.Model.Residents.Single(r=>r.Id=="momo").Facing==-1;
            }
            for(int index=0;index<48;index++)
            {
                string mode=index<8?"idle":index<16?"start-walk":index<32?"walk":index<40?"stop":"greet";
                if(index==8)Assert.That(field.World.GuidePoint(new TownPoint(-.32f,-.25f)),Is.True);
                if(index==24)Assert.That(field.World.GuidePoint(new TownPoint(.15f,-.25f)),Is.True,"Reverse through the real movement model, not a forced visual pose");
                if(index==32){field.World.CancelHero();field.StartInteraction();}
                if(index==40)
                {
                    // Walk to the actual, paused original central resident. The
                    // elapsed gap is recorded honestly, not edited into motion.
                    var target=new TownPoint(friend.WorldPosition.X-.20f,friend.WorldPosition.Y);
                    Assert.That(field.World.GuidePoint(target),Is.True);int budget=0;while(field.World.Model.Hero.IsWalking&&budget++<6000)Step();Assert.That(field.World.Model.Hero.IsWalking,Is.False);
                    // Arrival may have started social dialogue already. Clear
                    // it, then verify a new mutual greeting, including the real
                    // automatic path if it wins the normal social cooldown.
                    field.StartInteraction();budget=0;
                    while(!VerifiedGreeting()&&budget++<360){field.TryBeginConversation(noah,friend);if(!VerifiedGreeting())Step();}
                    Assert.That(VerifiedGreeting(),Is.True,"A solo ambient bubble is not evidence of Noah/Momo greeting");
                    var speaker=(TownActor)speakerField.GetValue(field);sequence.actual_conversation_verified=true;sequence.conversation_speaker_id=speaker.CharacterId;sequence.conversation_partner_id=speaker==noah?"momo":"noah";
                    sequence.conversation_text=Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Town bubble").text;
                    sequence.noah_reaction_seconds=(float)reactionField.GetValue(noah);sequence.momo_reaction_seconds=(float)reactionField.GetValue(friend);
                    sequence.noah_facing=field.World.Model.Residents.Single(r=>r.Id=="noah").Facing;sequence.momo_facing=field.World.Model.Residents.Single(r=>r.Id=="momo").Facing;
                }
                for(int step=0;step<3;step++)Step();
                if(index>=8&&index<32)Assert.That(field.World.Model.Hero.IsWalking,Is.True,"Walk/start samples must show actual model motion");
                // Manual simulation does not tick independent fountain Updates.
                // Camera framing is explicitly fixed for all sequence samples.
                field.World.Camera.Jump(new TownPoint(0,-.05f));field.World.Refresh();
                if(index==3||index==20||index==26||index==36||index==40||index==43)
                {
                    if(index==40)Assert.That(VerifiedGreeting(),Is.True,"The talk pair must render the verified greeting while both actors still react");
                    string pairMode=index==3?"idle":index==20?"walk":index==26?"turn":index==36?"stop":index==40?"talk":"reaction";
                    var pair=CaptureNativePair(output,field,"37-noah-"+pairMode,()=>noah.SetNoahTrialEnabled(false),()=>noah.SetNoahTrialEnabled(true),()=>ActorEvidenceRect(noah),true);pair.mode=pairMode;proof.noah_pairs.Add(pair);
                }
                var portrait=Portrait();Texture2D cropped=null;
                try
                {
                    var actual=ActorEvidenceRect(noah);var center=field.World.Camera.Center;
                    string framing=FormattableString.Invariant($"Noah frame {index}/{mode}; world=({noah.WorldPosition.X:R},{noah.WorldPosition.Y:R}), camera=({center.X:R},{center.Y:R}), crop={crop}, actor={actual}");
                    Assert.That(actual.xMin,Is.GreaterThanOrEqualTo(crop.x),framing);Assert.That(actual.xMax,Is.LessThanOrEqualTo(crop.xMax),framing);Assert.That(actual.yMin,Is.GreaterThanOrEqualTo(crop.y),framing);Assert.That(actual.yMax,Is.LessThanOrEqualTo(crop.yMax),framing);
                    cropped=new Texture2D(crop.width,crop.height,TextureFormat.RGB24,false);cropped.SetPixels(portrait.GetPixels(crop.x,crop.y,crop.width,crop.height));cropped.Apply();
                    sequence.frames.Add(new NoahSequenceFrame{index=index,mode=mode,elapsed_seconds=elapsed,world_x=noah.WorldPosition.X,world_y=noah.WorldPosition.Y,facing=(float)typeof(TownActor).GetField("facing",privateFields).GetValue(noah),reaction_seconds=(float)reactionField.GetValue(noah),walking=field.World.Model.Hero.IsWalking,image=SaveNativeImage(output,"noah-sequence/37-noah-frame-"+index.ToString("00")+".png",cropped)});
                }
                finally{UnityEngine.Object.DestroyImmediate(portrait);if(cropped!=null)UnityEngine.Object.DestroyImmediate(cropped);}
            }
            sequence.state_unchanged=saved==StateCodec.Encode(Flow.Game.State);sequence.store_writes_unchanged=savedWrites==Store.Writes;Assert.That(sequence.state_unchanged&&sequence.store_writes_unchanged,Is.True);
            if(output!=null)
            {
                Assert.That(proof.source_revision,Does.Match("^[a-f0-9]{40}$"));Assert.That(proof.product_fingerprint,Does.Match("^[a-f0-9]{64}$"));
                string repo=Directory.GetParent(Application.dataPath).FullName;var historical=File.ReadAllBytes(Path.Combine(repo,"References/Review/fountain-final-2026-10-27-candidate/native/09-connected-path-north.png"));
                Assert.That(EvidenceSha(historical),Is.EqualTo("b001ee0b7bb6a1719876b16c951a9bdcf5c58df3d7817dc797b379cf0c863324"));
                proof.historical_native_before=new NativeImage{file="36-north-historical-bb3fb82.png",bytes=historical.Length,width=720,height=1280,sha256=EvidenceSha(historical)};File.WriteAllBytes(Path.Combine(output,proof.historical_native_before.file),historical);
                File.WriteAllText(Path.Combine(output,"north-noah-comparison.json"),JsonUtility.ToJson(proof,true));WriteNorthNoahHtml(output,proof);
            }
            LogAssert.NoUnexpectedReceived();
        }
        private static void WriteNorthNoahHtml(string output,NorthNoahProof proof)
        {
            var html=new System.Text.StringBuilder("<!doctype html><meta charset='utf-8'><title>Native North / Noah causal review</title><style>body{font:16px sans-serif;background:#fff7ef;color:#50343b}img{max-width:46%;vertical-align:top}section{margin-bottom:2rem}#frame{max-width:100%}</style><h1>Actual Unity North / Noah candidate</h1><p>Native UGUI captures. Before is a reconstructed same-instant causal baseline; not a prior runtime capture. North join disabled / Noah legacy with current product name visibility held only in reconstructed Before. Natural-caption changes are disclosed in metadata and separate original PNGs; actual product After is unmodified. Current fountain and accepted North code are frozen unchanged. Noah Before uses the legacy rendering branch at the identical remaining reaction clock; its historical duration was0.65s while the candidate is1s, so this is not equal-elapsed prior reaction behavior. Pixel performance and candidate adoption remain pending.</p>");
            foreach(var pair in proof.north_pairs.Concat(proof.noah_pairs))html.Append("<section><h2>").Append(pair.id).Append("</h2><p>Before (reconstructed) left / After right; changed pixels ").Append(pair.changed_pixels).Append("; outside scoped projection ").Append(pair.outside_allowed_pixels).Append("</p><a href='").Append(pair.comparison.file).Append("'>Open actual native comparison .jpg (Before left / After right)</a><p><img style='max-width:100%' src='").Append(pair.comparison.file).Append("'></p><a href='").Append(pair.before.file).Append("'>Before original PNG</a> / <a href='").Append(pair.after.file).Append("'>After original PNG</a></section>");
            html.Append("<h2>Historical actual native Before: bb3fb82 / run102109</h2><p>09-connected-path-north.png: different historical runtime conditions; never the same-state causal baseline above.</p><img src='").Append(proof.historical_native_before.file).Append("'>");
            html.Append("<h2>Actual model walking, no navigation changes or teleport rescue</h2>");foreach(var route in proof.routes)html.Append("<p>").Append(route.id).Append(": ").Append(route.steps).Append(" fixed steps; sampled actual positions in JSON.</p><img src='").Append(route.image.file).Append("'>");
            AppendNoahSequence(html,proof.sequence);
            File.WriteAllText(Path.Combine(output,"north-noah-before-after.html"),html.ToString());
            var noah=new System.Text.StringBuilder("<!doctype html><meta charset='utf-8'><title>Noah native review</title><style>body{font:16px sans-serif;background:#fff7ef;color:#50343b;max-width:1500px;margin:auto}img{max-width:100%}section{margin:2rem 0}</style><h1>ノア1名 Before / After（実Unity）</h1><p>Beforeは原画差分確認用の再現で、名札状態を製品Afterへ揃えています。Afterは通常の製品表示です。通常時に名札が変わる場合は、そのBefore原本も別に残します。新旧の挨拶時間は異なるため、旧版の同じ経過時間を再現した映像ではありません。Pixel負荷・ユーザー採用は未確認です。</p>");
            foreach(var pair in proof.noah_pairs)
            {
                noah.Append("<section><h2>").Append(pair.mode).Append("</h2><a href='").Append(pair.comparison.file).Append("'>比較JPEG</a><p><img src='").Append(pair.comparison.file).Append("'></p><a href='").Append(pair.before.file).Append("'>比較Before原PNG</a> / <a href='").Append(pair.after.file).Append("'>通常製品After原PNG</a>");
                if(pair.name_control.natural_visibility_changed)noah.Append("<p>通常時の名札切替あり。<a href='").Append(pair.name_control.natural_before_image.file).Append("'>名札状態を揃える前のBefore原PNG</a></p>");
                noah.Append("</section>");
            }
            AppendNoahSequence(noah,proof.sequence);
            File.WriteAllText(Path.Combine(output,"noah-before-after.html"),noah.ToString());
        }
        private static void AppendNoahSequence(System.Text.StringBuilder html,NoahSequence sequence)
        {
            html.Append("<h2>Noah: idle → start → walk → turn → stop / separate greeting</h2><p>48 actual native PNG crops; manual TownLifeView.AdvanceFrame at 1/30s, sampled each 3 steps. Frames0–39 are one contiguous4-second episode including real direction reversal at2.4s. Frames40–47 are the later verified mutual greeting. The timestamp gap is shown as a cut, never a continuous/FPS or Pixel measurement.</p><button id='play'>Play / pause</button><p id='time'></p><img id='frame'><script>const data=").Append(JsonUtility.ToJson(sequence)).Append(";let i=0,playing=true;document.querySelector('#play').onclick=()=>playing=!playing;function tick(){const f=data.frames[i];const image=document.querySelector('#frame');image.onerror=()=>{image.onerror=null;image.src=f.image.file.split('/').pop()};image.src=f.image.file;document.querySelector('#time').textContent=(i===40?'CUT: later greeting / ':'')+f.index+' / '+f.mode+' / '+f.elapsed_seconds.toFixed(3)+' simulated seconds';setTimeout(()=>{if(playing)i=(i+1)%data.frames.length;tick()},i===40?1000:100)}tick()</script>");
        }
        [Serializable] private sealed class GrowthDistrictProof
        {public string id="";public bool changed=false,reload_matched=false;}
        [Serializable] private sealed class GrowthTransitionProof
        {
            public int from,to,coins,completed_quests,generated_level_one,merges,districts_checked=5;
            public string quest_id="";public string[] requirements=Array.Empty<string>();
            public bool reload_matched=false,reward_replay_rejected=false;
            public GrowthDistrictProof[] districts=Array.Empty<GrowthDistrictProof>();
        }
        [Serializable] private sealed class GrowthProgressionProof
        {
            public string kind="isolated-real-delivery-growth-and-reload";
            public int initial_level=0,initial_coins=0;
            public bool direct_level_assignment=false,pixel_verified=false,adopted=false;
            public List<GrowthTransitionProof> transitions=new List<GrowthTransitionProof>();
        }
        // Real production/delivery progression in this fixture's temporary disk
        // save. Never assigns a growth level or completion/reward collection.
        [UnityTest] public IEnumerator DeliveriesGrowFiveDistrictsAndPersistAfterSceneReload()
        {
            string output=OutputDirectory();yield return EnterHome();int totalCoins=0;
            Assert.That(Flow.Game.State.townGrowthLevel,Is.Zero);Assert.That(Flow.Game.State.coins,Is.Zero);
            Assert.That(Flow.Game.State.completedQuestIds,Is.Empty);
            var proof=new GrowthProgressionProof();
            string[] VisibleDecor(TownDistrictId district)
            {
                ReleasePortraitCamera();
                var town=FrameTownPortrait();town.StartInteraction();town.World.Freeze();town.World.LookAt(district);
                var center=TownDistrictLayout.Center(district);town.World.Camera.Jump(new TownPoint(center.X,center.Y+.20f));town.World.Refresh();Canvas.ForceUpdateCanvases();
                TownPoint Foot(TownGrowthPropGraphic prop)
                {
                    var canvas=town.GetComponentInParent<Canvas>();
                    var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
                    return town.World.Source(RectTransformUtility.WorldToScreenPoint(camera,prop.transform.position),camera);
                }
                var scenery=town.GetComponent<TownSceneryView>();
                // A viewport may contain a neighbor's flowers. Compare this
                // district's actual active objects, not the neighbor's stage.
                return scenery.Pieces.Where(p=>p.Rect.gameObject.activeInHierarchy&&p.Placement.District==district)
                    .Select(p=>FormattableString.Invariant($"scenery:{p.Placement.Kind}:{p.Placement.Feet.X:F4}:{p.Placement.Feet.Y:F4}:{p.Placement.Level}:{p.Placement.Width:F4}"))
                    .Concat(town.GetComponentsInChildren<TownGrowthPropGraphic>().Where(p=>TownDistrictLayout.At(Foot(p))==district)
                        .Select(p=>FormattableString.Invariant($"growth:{p.Prop}:{p.Region}:{Foot(p).X:F4}:{Foot(p).Y:F4}")))
                    .OrderBy(value=>value,StringComparer.Ordinal).ToArray();
            }
            foreach(var quest in QuestCatalog.All)
            {
                int previous=Flow.Game.State.townGrowthLevel;
                var row=new GrowthTransitionProof{from=previous,to=previous+1,quest_id=quest.Id,
                    requirements=quest.Requirements.Select(r=>r.ItemId+":"+r.Level+":"+r.Count).ToArray()};
                Flow.ShowHome();var before=TownDistrictLayout.All.Select(VisibleDecor).ToArray();
                Flow.ShowQuest();Assert.That(Flow.AcceptQuest(),Is.True);Assert.That(Flow.Game.Quests.ActiveQuest.Id,Is.EqualTo(quest.Id));
                foreach(var requirement in quest.Requirements)
                {
                    int guard=0;
                    while(Flow.Game.State.mergeBoard.Count(i=>i.itemId==requirement.ItemId&&i.level==requirement.Level)<requirement.Count)
                    {
                        Assert.That(++guard,Is.LessThan(40),"Generate/merge must progress without synthetic inventory");
                        var pair=Flow.Game.State.mergeBoard.Where(i=>i.itemId==requirement.ItemId&&i.level<requirement.Level).GroupBy(i=>i.level).FirstOrDefault(g=>g.Count()>=2);
                        if(pair==null){int count=Flow.Game.State.mergeBoard.Count;Flow.GenerateFromFrame(requirement.ItemId);Assert.That(Flow.Game.State.mergeBoard.Count,Is.EqualTo(count+1));row.generated_level_one++;}
                        else{var items=pair.Take(2).ToArray();Assert.That(Flow.DropItem(items[0].cellIndex,items[1].cellIndex),Is.True);row.merges++;yield return new WaitForSecondsRealtime(.30f);}
                    }
                }
                Assert.That(Flow.Deliver().Success,Is.True);totalCoins+=quest.Coins;
                Assert.That(Flow.LastDelivery.PreviousTownLevel,Is.EqualTo(previous));Assert.That(Flow.LastDelivery.CurrentTownLevel,Is.EqualTo(previous+1));
                Assert.That(Store.Load().State.townGrowthLevel,Is.EqualTo(previous+1),"Delivery commits before growth presentation");
                yield return new WaitForSecondsRealtime(1.3f);Flow.ShowGrowth();Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Growth));
                foreach(var district in TownDistrictLayout.All)
                {
                    var button=Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Growth region "+district);button.onClick.Invoke();Canvas.ForceUpdateCanvases();
                    var previews=Flow.GetComponentsInChildren<TownGrowthView>();Assert.That(previews.Length,Is.EqualTo(2));
                    Assert.That(previews[0].VisibleStage,Is.EqualTo(previous));Assert.That(previews[1].VisibleStage,Is.EqualTo(previous+1));
                    Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Growth after label").text,Is.EqualTo(TownGrowthPresentation.ChangeSummary(district,previous,previous+1)));
                    Assert.That(previews[1].IsHighlighting,Is.True,"Keep the short existing new-decoration highlight");
                    Capture(output,"32-growth-"+previous+"-to-"+(previous+1)+"-"+district);
                }
                Flow.ShowHome();var after=TownDistrictLayout.All.Select(VisibleDecor).ToArray();
                var districts=new List<GrowthDistrictProof>();
                for(int i=0;i<TownDistrictLayout.All.Count;i++)
                {
                    var district=TownDistrictLayout.All[i];
                    bool unchanged=(previous<=1&&(district==TownDistrictId.North||district==TownDistrictId.South))||
                        ((previous==3||previous==4)&&district==TownDistrictId.Central);
                    bool changed=!after[i].SequenceEqual(before[i]);Assert.That(changed,Is.EqualTo(!unchanged),district+" must match authored additions/replacements or intentional unchanged scenery");
                    districts.Add(new GrowthDistrictProof{id=district.ToString(),changed=changed});
                }
                Assert.That(Flow.Game.State.coins,Is.EqualTo(totalCoins));int completed=Flow.Game.State.completedQuestIds.Count;
                var saved=Flow.Game.State.Clone();
                Flow.SendMessage("OnApplicationPause",true);ReleasePortraitCamera();
                yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("TitleScene");yield return null;Flow.Initialize(Store);Flow.BeginGame();yield return null;yield return null;
                Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));Assert.That(Flow.Game.State.townGrowthLevel,Is.EqualTo(previous+1));
                Assert.That(Flow.Game.State.coins,Is.EqualTo(totalCoins));Assert.That(Flow.Game.State.completedQuestIds.Count,Is.EqualTo(completed));Assert.That(Flow.Game.State.claimedRewardIds.Count,Is.EqualTo(completed));
                var restored=Flow.Game.State.Clone();restored.lastSaveTime=saved.lastSaveTime;
                Assert.That(StateCodec.Encode(restored),Is.EqualTo(StateCodec.Encode(saved)),"All saved game data except the pause timestamp survives scene reload");
                var reloaded=new List<string[]>();
                foreach(var district in TownDistrictLayout.All)
                {
                    reloaded.Add(VisibleDecor(district));
                    Capture(output,"32-growth-"+(previous+1)+"-"+district+"-reloaded");
                }
                for(int i=0;i<after.Length;i++)
                {Assert.That(reloaded[i],Is.EqualTo(after[i]),TownDistrictLayout.All[i]+" persisted without a test level");districts[i].reload_matched=true;}
                Flow.ShowQuest();int replayCoins=Flow.Game.State.coins;var replayRewards=Flow.Game.State.claimedRewardIds.ToArray();
                Assert.That(Flow.Deliver().Success,Is.False,"Completed reward must remain rejected after reload");
                Assert.That(Flow.Game.State.coins,Is.EqualTo(replayCoins));Assert.That(Flow.Game.State.claimedRewardIds,Is.EqualTo(replayRewards));
                row.coins=totalCoins;row.completed_quests=completed;row.reload_matched=true;row.reward_replay_rejected=true;row.districts=districts.ToArray();proof.transitions.Add(row);
                Flow.ShowHome();
            }
            Assert.That(Flow.Game.State.townGrowthLevel,Is.EqualTo(6));Assert.That(Flow.Game.State.coins,Is.EqualTo(330));
            if(output!=null){Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"town-growth-progression.json"),JsonUtility.ToJson(proof,true));}
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator FountainWaterBeforeAfterPreservesFieldAndCharacterDepth()
        {
            string output=OutputDirectory();yield return EnterHome();Flow.Game.State.townGrowthLevel=6;Flow.ShowHome();yield return null;
            var town=Flow.GetComponentInChildren<TownLifeView>();town.StartInteraction();
            var water=Flow.GetComponentInChildren<TownFountainWaterGraphic>();var originalWater=Flow.GetComponentInChildren<TownAmbientGraphic>();var fountain=Flow.GetComponentInChildren<TownFountainGraphic>();
            Assert.That(water.transform.parent,Is.SameAs(fountain.transform));Assert.That(water.raycastTarget,Is.False);
            var pairs=new System.Text.StringBuilder();bool first=true;
            var snapshots=new Dictionary<string,TownWorldSnapshot>();var normalPixels=new Dictionary<string,Color32[]>();
            foreach(var view in new[]{("overview",new TownPoint(0,.12f),new TownPoint(0,.64f)),("front",new TownPoint(0,.12f),new TownPoint(0,.32f)),("behind",new TownPoint(0,.441f),new TownPoint(0,.55f))})
            {
                Assert.That(town.World.GuidePoint(view.Item2),Is.True);int steps=0;
                while(town.World.Model.Hero.IsWalking&&steps++<6000){town.AdvanceFrame(1f/30);if(steps%240==0)yield return null;}
                Assert.That(town.World.Model.Hero.IsWalking,Is.False);Assert.That(TownWalkableArea.Distance(town.World.Model.Hero.Position,view.Item2),Is.LessThan(.002f));
                town.World.Freeze();town.World.Camera.Jump(view.Item3);town.World.Refresh();
                snapshots.Add(view.Item1,town.World.Capture());
                // Warm font/canvas resources before the paired pixel comparison.
                var warm=Portrait();UnityEngine.Object.DestroyImmediate(warm);
                string state=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;var positions=town.World.Model.Residents.Select(r=>r.Position).ToArray();var camera=town.World.Camera.Center;
                float originalPhase=originalWater.Phase,newPhase=water.Phase;Texture2D before=null,after=null;
                int changed=0,outside=0;
                try
                {
                    water.enabled=false;before=Portrait();Capture(output,"33-fountain-"+view.Item1+"-before");
                    water.enabled=true;after=Portrait();Capture(output,"33-fountain-"+view.Item1+"-after");
                    Vector2 Project(TownPoint point)=>captureCamera.WorldToScreenPoint(town.transform.TransformPoint(town.World.Local(point)));
                    var low=Project(TownFountainWaterLayout.Project(new TownPoint(0,0),TownFountainWaterLayout.DisplayScale));
                    var high=Project(TownFountainWaterLayout.Project(new TownPoint(1,1.3f),TownFountainWaterLayout.DisplayScale));
                    var allowed=Rect.MinMaxRect(low.x-3,low.y-3,high.x+3,high.y+3);var a=before.GetPixels32();var b=after.GetPixels32();
                    for(int i=0;i<a.Length;i++)
                    {
                        if(a[i].r==b[i].r&&a[i].g==b[i].g&&a[i].b==b[i].b&&a[i].a==b[i].a)continue;
                        changed++;if(!allowed.Contains(new Vector2(i%720,i/720)))outside++;
                    }
                    Assert.That(changed,Is.GreaterThan(200),"Normal water needs a substantial visible contribution, not the former few dozen pixels");
                    Assert.That(outside,Is.Zero,"Adding water must not alter the field/UI/characters outside the fountain projection");
                    Assert.That(water.canvasRenderer.GetMesh().vertexCount,Is.EqualTo(784));normalPixels.Add(view.Item1,after.GetPixels32());
                    Assert.That(originalWater.Phase,Is.EqualTo(originalPhase));Assert.That(water.Phase,Is.EqualTo(newPhase));
                    Assert.That(town.World.Model.Residents.Select(r=>r.Position).ToArray(),Is.EqualTo(positions));Assert.That(town.World.Camera.Center,Is.EqualTo(camera));
                    Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(state));Assert.That(Store.Writes,Is.EqualTo(writes));
                    var hero=town.Actors.Single(a=>a.Controlled);
                    Assert.That(hero.transform.GetSiblingIndex()>fountain.transform.GetSiblingIndex(),Is.EqualTo(view.Item2.Y<TownCentralMasterLayout.FountainCenterY),"The entire new water layer must inherit the original fountain depth");
                }
                finally {water.enabled=true;if(before!=null)UnityEngine.Object.DestroyImmediate(before);if(after!=null)UnityEngine.Object.DestroyImmediate(after);}
                if(!first)pairs.Append(',');first=false;
                pairs.Append("{\"view\":\""+view.Item1+"\",\"changed_pixels\":"+changed+",\"outside_fountain_pixels\":"+outside+",\"state_unchanged\":true}");
            }
            var phases=new System.Text.StringBuilder();Vector3[] previous=null;
            for(int frame=0;frame<4;frame++)
            {
                if(frame>0)for(int tick=0;tick<3;tick++){water.AdvanceWater(.1f);originalWater.AdvanceWater(.1f);}
                Capture(output,"33-fountain-phase-"+frame.ToString("00"));
                var mesh=water.canvasRenderer.GetMesh().vertices;
                if(previous!=null)Assert.That(mesh.SequenceEqual(previous),Is.False,"Time samples must animate actual water geometry");previous=mesh;
                if(frame>0)phases.Append(',');phases.Append(water.Phase.ToString("R",System.Globalization.CultureInfo.InvariantCulture));
            }
            if(output!=null)
            {
                File.WriteAllText(Path.Combine(output,"fountain-water-comparison.json"),"{\"kind\":\"same-state-native-additive-water-comparison\",\"before\":\"new layer OFF; original fountain and original water retained\",\"after\":\"product code\",\"pairs\":["+pairs+"],\"phases\":["+phases+"],\"added_vertices\":784,\"max_update_hz\":30,\"pixel_verified\":false,\"adopted\":false}");
                var html=new System.Text.StringBuilder("<!doctype html><meta charset='utf-8'><title>Fountain water Before After</title><style>body{background:#fff7ef;color:#50343b;font-family:sans-serif}section{display:flex;gap:12px}img{width:360px;max-width:46vw}figure{margin:8px}</style><h1>Fountain water / same-state Unity comparison</h1><p>Before: only the new water layer OFF. Original fountain, original ripples, field, characters and camera retained. After: product code. Not a previous APK reconstruction or Pixel capture.</p>");
                foreach(string view in new[]{"overview","front","behind"})html.Append("<h2>"+view+"</h2><section><figure>Before<br><img src='33-fountain-"+view+"-before.png'></figure><figure>After<br><img src='33-fountain-"+view+"-after.png'></figure></section>");
                html.Append("<h2>Four real animation samples</h2><section>");for(int frame=0;frame<4;frame++)html.Append("<figure>"+frame+"<br><img src='33-fountain-phase-"+frame.ToString("00")+".png'></figure>");
                html.Append("</section><p>Pixel visibility, live depth transitions, temperature and performance acceptance pending.</p>");File.WriteAllText(Path.Combine(output,"fountain-water-before-after.html"),html.ToString());
            }
            CaptureFountainShow(output,town,water,originalWater,snapshots,normalPixels);
            LogAssert.NoUnexpectedReceived();
        }
        [Serializable] private sealed class FountainBeforeManifest {public string source_revision="";public FountainBeforeImage[] images=Array.Empty<FountainBeforeImage>();}
        [Serializable] private sealed class FountainBeforeImage {public string path="",file="",sha256="";}
        private void CaptureFountainShow(string output,TownLifeView town,TownFountainWaterGraphic water,TownAmbientGraphic originalWater,Dictionary<string,TownWorldSnapshot> snapshots,Dictionary<string,Color32[]> normalPixels)
        {
            void AdvanceTo(float elapsed)
            {
                int guard=0;while(water.ShowElapsed<elapsed&&guard++<2000){float dt=Math.Min(.1f,elapsed-water.ShowElapsed);water.AdvanceWater(dt);originalWater.AdvanceWater(dt);}
                Assert.That(water.ShowElapsed,Is.GreaterThanOrEqualTo(elapsed));Assert.That(guard,Is.LessThan(2000));
            }
            Assert.That(water.ShowCycleSeconds,Is.EqualTo(45));
            town.World.Restore(snapshots["overview"]);AdvanceTo(37);Assert.That(water.ShowStage,Is.EqualTo(1));Capture(output,"34-fountain-overview-prelude");
            AdvanceTo(41);Assert.That(water.ShowStage,Is.EqualTo(2));var showRows=new System.Text.StringBuilder();
            foreach(string view in new[]{"overview","front","behind"})
            {
                town.World.Restore(snapshots[view]);string state=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;
                float showClock=water.ShowElapsed,originalPhase=originalWater.Phase;var residents=town.World.Model.Residents.Select(r=>r.Position).ToArray();var center=town.World.Camera.Center;
                Texture2D without=null,pixels=null;int changed=0,jets=0,outside=0;
                try
                {
                    water.enabled=false;without=Portrait();Capture(output,"34-fountain-"+view+"-show-off");
                    water.enabled=true;Capture(output,"34-fountain-"+view+"-show");pixels=Portrait();
                    var sampled=pixels.GetPixels32();var disabled=without.GetPixels32();var normal=normalPixels[view];
                    Vector2 Project(TownPoint uv)=>captureCamera.WorldToScreenPoint(town.transform.TransformPoint(town.World.Local(TownFountainWaterLayout.Project(uv,TownFountainWaterLayout.DisplayScale))));
                    var low=Project(new TownPoint(0,0));var high=Project(new TownPoint(1,1.3f));var allowed=Rect.MinMaxRect(low.x-3,low.y-3,high.x+3,high.y+3);
                    float normalTop=Project(new TownPoint(.5f,.84f)).y;
                    for(int i=0;i<sampled.Length;i++)
                    {
                        if(!sampled[i].Equals(normal[i]))changed++;
                        if(sampled[i].Equals(disabled[i]))continue;
                        var at=new Vector2(i%720,i/720);if(at.y>normalTop)jets++;if(!allowed.Contains(at))outside++;
                    }
                }
                finally {water.enabled=true;if(without!=null)UnityEngine.Object.DestroyImmediate(without);if(pixels!=null)UnityEngine.Object.DestroyImmediate(pixels);}
                Assert.That(changed,Is.GreaterThan(300),"Show must visibly differ from normal water, not just change internal stage flags");
                Assert.That(jets,Is.GreaterThan(40),"Same-instant OFF/ON pixels above the normal water ceiling must demonstrate actual show jets");Assert.That(outside,Is.Zero);
                Assert.That(water.ShowElapsed,Is.EqualTo(showClock));Assert.That(originalWater.Phase,Is.EqualTo(originalPhase));Assert.That(town.World.Model.Residents.Select(r=>r.Position).ToArray(),Is.EqualTo(residents));Assert.That(town.World.Camera.Center,Is.EqualTo(center));
                Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(state));Assert.That(Store.Writes,Is.EqualTo(writes));
                if(showRows.Length>0)showRows.Append(',');showRows.Append("{\"view\":\""+view+"\",\"changed_pixels\":"+changed+",\"show_jet_pixels\":"+jets+",\"outside_fountain_pixels\":"+outside+",\"clock_unchanged\":true,\"original_water_phase_unchanged\":true,\"state_unchanged\":true}");
            }
            town.World.Restore(snapshots["overview"]);AdvanceTo(45-1f/30);water.AdvanceWater(.1f);Assert.That(water.ShowStage,Is.Zero);Capture(output,"34-fountain-overview-recovered");
            if(output==null)return;
            string repo=Directory.GetParent(Application.dataPath).FullName;
            string baselineText=File.ReadAllText(Path.Combine(repo,"References/VisualLock/fountain-final-current-before-2026-10-27.json"));
            var baseline=JsonUtility.FromJson<FountainBeforeManifest>(baselineText);Assert.That(baseline.source_revision,Is.EqualTo("98596daf798bcea54d39f6a41b58a565070e1370"));Assert.That(baseline.images.Length,Is.EqualTo(3));
            string beforeDir=Path.Combine(output,"fountain-before");Directory.CreateDirectory(beforeDir);
            foreach(var image in baseline.images)
            {
                byte[] bytes=File.ReadAllBytes(Path.Combine(repo,image.path));using(var hash=System.Security.Cryptography.SHA256.Create())Assert.That(BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant(),Is.EqualTo(image.sha256));
                File.WriteAllBytes(Path.Combine(beforeDir,image.file),bytes);
            }
            File.WriteAllText(Path.Combine(output,"fountain-show-comparison.json"),"{\"kind\":\"prior-native-before-normal-and-timed-show\",\"display_scale\":1.14,\"cycle_seconds\":45,\"release_cycle_seconds\":180,\"prelude_seconds\":2,\"show_seconds\":6,\"recovery_seconds\":1,\"show_elapsed\":41,\"jet_evidence\":\"same-instant-added-layer-off-on-above-normal-uv084\",\"show_pairs\":["+showRows+"],\"before\":"+baselineText+",\"pixel_verified\":false,\"adopted\":false}");
            var html=new System.Text.StringBuilder("<!doctype html><meta charset='utf-8'><title>Fountain normal and show</title><style>body{background:#fff7ef;color:#50343b;font-family:sans-serif}section{display:flex;gap:8px}img{width:240px;max-width:30vw}figure{margin:4px}</style><h1>Actual prior Before / normal candidate / short show candidate</h1><p>Before:98596daf native run20261026-192546. Normal/Show:new native fixture. Old files are SHA-preserved; not repainted. These causal stills manually advance the production clock; the separate fountain-live sequence tests actual Update for60real seconds. Pixel load and acceptance pending.</p>");
            foreach(string view in new[]{"overview","front","behind"})html.Append("<h2>"+view+"</h2><section><figure>Current Before<br><img src='fountain-before/34-fountain-"+view+"-current-before.png' onerror=\"this.onerror=null;this.src='34-fountain-"+view+"-current-before.png'\"></figure><figure>Normal After<br><img src='33-fountain-"+view+"-after.png'></figure><figure>Show After<br><img src='34-fountain-"+view+"-show.png'></figure></section>");
            html.Append("<h2>Prelude / returned to normal</h2><section><img src='34-fountain-overview-prelude.png'><img src='34-fountain-overview-recovered.png'></section>");File.WriteAllText(Path.Combine(output,"fountain-show-before-after.html"),html.ToString());
        }
        [Serializable] private sealed class FountainLiveFrame
        {
            public string file,sha256;public float realtime_seconds,clock_seconds,phase;public int stage,bytes,width,height;
        }
        [Serializable] private sealed class FountainLiveSequence
        {
            public string kind="native-realtime-update-sequence";
            public float seconds,first_show_seconds=-1,cycle_seconds,sample_period_seconds=.25f;
            public bool manual_advance=false,pixel_verified=false,adopted=false,state_unchanged;
            public int town_level=0,source_width=720,source_height=1280,crop_x,crop_y,crop_width,crop_height,normal_mesh_changes,show_mesh_changes;
            public List<FountainLiveFrame> frames=new List<FountainLiveFrame>();
        }
        // This is deliberately separate from the manually advanced causal pairs.
        // No AdvanceWater, forced stage/clock, synthetic dt or fast-forward here.
        [UnityTest,Timeout(120000)] public IEnumerator FountainShowRunsFromRealUpdatesWithinSixtySeconds()
        {
            string output=OutputDirectory();yield return EnterHome();Assert.That(Flow.Game.State.townGrowthLevel,Is.Zero);Flow.ShowHome();yield return null;
            var town=Flow.GetComponentInChildren<TownLifeView>();town.StartInteraction();town.World.LookAt(TownDistrictId.Central);
            // Exercise the deferred layout path that batch-mode Game views use.
            Flow.GetComponentInChildren<ResponsiveUILayout>().ApplyViewport(new Vector2(1280,720),new Rect(0,0,1280,720));
            Canvas.ForceUpdateCanvases();town.World.Refresh();EnsurePortraitCamera();
            // Portrait performs the actual canvas layout and world-camera fit.
            // Derive the crop only after that first settled render.
            Capture(output,"35-fountain-live-normal");
            var water=town.GetComponentInChildren<TownFountainWaterGraphic>();Assert.That(water.ShowCycleSeconds,Is.EqualTo(45));Assert.That(water.ShowElapsed,Is.LessThan(2));
            string state=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;
            var sequence=new FountainLiveSequence{cycle_seconds=water.ShowCycleSeconds,town_level=Flow.Game.State.townGrowthLevel};
            Vector2 Project(TownPoint uv)=>captureCamera.WorldToScreenPoint(town.transform.TransformPoint(town.World.Local(TownFountainWaterLayout.Project(uv,TownFountainWaterLayout.DisplayScale))));
            var low=Project(new TownPoint(.2f,.22f));var high=Project(new TownPoint(.8f,1.3f));
            int x=Mathf.Clamp(Mathf.FloorToInt(low.x)-4,0,719),y=Mathf.Clamp(Mathf.FloorToInt(low.y)-4,0,1279);
            int width=Mathf.Clamp(Mathf.CeilToInt(high.x)+4,x+1,720)-x,height=Mathf.Clamp(Mathf.CeilToInt(high.y)+4,y+1,1280)-y;
            sequence.crop_x=x;sequence.crop_y=y;sequence.crop_width=width;sequence.crop_height=height;
            string directory=output==null?null:Path.Combine(output,"fountain-live");if(directory!=null)Directory.CreateDirectory(directory);
            double start=Time.realtimeSinceStartupAsDouble,next=start;Vector3[] previous=null;bool showed=false,recovered=false;int previousStage=0;
            do
            {
                yield return null;double now=Time.realtimeSinceStartupAsDouble;
                if(now<next)continue;next=now+.25;float elapsed=(float)(now-start);
                Texture2D portrait=null,crop=null;
                try
                {
                    portrait=Portrait();var mesh=water.canvasRenderer.GetMesh().vertices;int stage=water.ShowStage;
                    Assert.That(mesh.Length,Is.EqualTo(784));
                    Assert.That(width,Is.GreaterThanOrEqualTo(64));Assert.That(height,Is.GreaterThanOrEqualTo(64));
                    foreach(var vertex in mesh)
                    {
                        var screen=captureCamera.WorldToScreenPoint(water.rectTransform.TransformPoint(vertex));
                        Assert.That(screen.x,Is.InRange((float)x,x+width));Assert.That(screen.y,Is.InRange((float)y,y+height),"Live crop must contain actual settled water geometry, including tall jets");
                    }
                    if(previous!=null&&!mesh.SequenceEqual(previous))
                    {if(stage==0&&previousStage==0)sequence.normal_mesh_changes++;if(stage==2&&previousStage==2)sequence.show_mesh_changes++;}
                    previous=mesh;previousStage=stage;
                    if(stage==2&&!showed&&TownFountainWaterLayout.ShowAt(water.ShowElapsed,45).Strength>.99f)
                    {showed=true;sequence.first_show_seconds=elapsed;Capture(output,"35-fountain-live-show");}
                    if(showed&&stage==0&&!recovered){recovered=true;Capture(output,"35-fountain-live-recovered");}
                    crop=new Texture2D(width,height,TextureFormat.RGB24,false);crop.SetPixels(portrait.GetPixels(x,y,width,height));crop.Apply();byte[] png=crop.EncodeToPNG();
                    string name="35-fountain-live-crop-"+sequence.frames.Count.ToString("000")+".png";
                    string sha;using(var hash=System.Security.Cryptography.SHA256.Create())sha=BitConverter.ToString(hash.ComputeHash(png)).Replace("-","").ToLowerInvariant();
                    if(directory!=null)File.WriteAllBytes(Path.Combine(directory,name),png);
                    sequence.frames.Add(new FountainLiveFrame{file=name,sha256=sha,bytes=png.Length,width=width,height=height,realtime_seconds=elapsed,clock_seconds=water.ShowElapsed,phase=water.Phase,stage=stage});
                    sequence.seconds=elapsed;
                }
                finally {if(portrait!=null)UnityEngine.Object.DestroyImmediate(portrait);if(crop!=null)UnityEngine.Object.DestroyImmediate(crop);}
            }while(sequence.seconds<60);
            sequence.state_unchanged=state==StateCodec.Encode(Flow.Game.State)&&writes==Store.Writes;
            if(directory!=null)
            {
                File.WriteAllText(Path.Combine(directory,"fountain-live.json"),JsonUtility.ToJson(sequence,true));
                File.WriteAllText(Path.Combine(directory,"fountain-live.html"),"<!doctype html><meta charset='utf-8'><title>Actual Unity Update / fountain60s</title><style>body{background:#fff7ef;font-family:sans-serif;color:#50343b}img{max-width:100%;image-rendering:auto}</style><h1>Actual Unity Update: normal → show → normal</h1><p>Native rendered crops at actual sample times. No manual clock advance. Not Pixel video or FPS evidence.</p><button id='play'>再生 / 一時停止</button><p id='time'></p><img id='frame'><script>const data="+JsonUtility.ToJson(sequence)+";let i=0,playing=true;document.querySelector('#play').onclick=()=>playing=!playing;function step(){const f=data.frames[i];document.querySelector('#frame').src=f.file;document.querySelector('#time').textContent=f.realtime_seconds.toFixed(2)+'s / stage '+f.stage;let dt=i+1<data.frames.length?1000*(data.frames[i+1].realtime_seconds-f.realtime_seconds):500;setTimeout(()=>{if(playing)i=(i+1)%data.frames.length;step()},dt)}step()</script>");
            }
            Assert.That(sequence.first_show_seconds,Is.InRange(30,60),"A real Update-driven visible show must occur inside the promised QA minute");
            Assert.That(recovered,Is.True);Assert.That(sequence.frames.Select(f=>f.stage).Distinct().OrderBy(s=>s),Is.EqualTo(new[]{0,1,2,3}));
            Assert.That(sequence.normal_mesh_changes,Is.GreaterThan(8));Assert.That(sequence.show_mesh_changes,Is.GreaterThan(3));Assert.That(sequence.state_unchanged,Is.True);
            Assert.That(sequence.frames.Count,Is.GreaterThan(60));LogAssert.NoUnexpectedReceived();
        }
        private static string OutputDirectory()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-mofumachiScreenshots");
            return i>=0&&i+1<args.Length?Path.GetFullPath(args[i+1]):null;
        }
        private sealed class CaptureMusicStore:IMusicPreferenceStore
        {private string value;public string Load()=>value;public void Save(string id)=>value=id;}
        private Camera captureCamera;private RenderTexture target;private Canvas captureCanvas;
        private void EnsurePortraitCamera()
        {
            captureCanvas=Flow.GetComponentInChildren<Canvas>();
            if(captureCamera==null)
            {
                var cameraObject=new GameObject("Isolated portrait capture camera",typeof(Camera));captureCamera=cameraObject.GetComponent<Camera>();
                target=new RenderTexture(720,1280,24);target.Create();captureCamera.targetTexture=target;
                captureCamera.clearFlags=CameraClearFlags.SolidColor;captureCamera.backgroundColor=new Color(.98f,.94f,.91f);captureCamera.cullingMask=1<<30;
                captureCamera.orthographic=true;captureCamera.orthographicSize=320;captureCamera.nearClipPlane=.1f;captureCamera.farClipPlane=100;captureCamera.transform.position=new Vector3(0,0,-20);
                // Orthographic XY stays identical. The original plane10 clips
                // the new original-art relief/yaw; reserve depth on both sides.
                captureCanvas.GetComponent<CanvasScaler>().enabled=false;captureCanvas.renderMode=RenderMode.ScreenSpaceCamera;captureCanvas.worldCamera=captureCamera;captureCanvas.planeDistance=30;captureCanvas.scaleFactor=2;
                Flow.GetComponentInChildren<ResponsiveUILayout>().ApplyViewport(new Vector2(720,1280),new Rect(0,0,720,1280));
            }
            foreach(var t in captureCanvas.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
        }
        private Texture2D Portrait(Action beforeRender=null)
        {
            EnsurePortraitCamera();var previous=RenderTexture.active;Texture2D image=null;
            try
            {
                Canvas.ForceUpdateCanvases();foreach(var grid in Flow.GetComponentsInChildren<MergeGridLayout>())grid.ResizeNow();foreach(var hud in Flow.GetComponentsInChildren<TownStreetHud>())hud.Refresh();
                // Manual simulation deliberately avoids yielding a frame. Run the
                // existing location-only LateUpdate at the same simulated instant.
                foreach(var region in Flow.GetComponentsInChildren<TownRegionControls>())region.SendMessage("LateUpdate",SendMessageOptions.RequireReceiver);
                foreach(var town in Flow.GetComponentsInChildren<TownLifeView>())
                {
                    town.World.Refresh();town.RefreshWorldPresentation();
                    foreach(var actor in town.Actors.Where(a=>a.RegionVisible))
                        Assert.That(Vector2.Distance(((RectTransform)actor.transform).anchoredPosition,town.World.Local(actor.WorldPosition)),Is.LessThan(.01f),"Direct capture must not mix the current scenery with stale actor positions");
                }
                Canvas.ForceUpdateCanvases();
                beforeRender?.Invoke();Canvas.ForceUpdateCanvases();
                foreach(var actor in Flow.GetComponentsInChildren<TownActor>().Where(a=>a.NoahReprototypeEnabled&&a.RegionVisible))
                {
                    var graphic=actor.GetComponentInChildren<CharacterBodyGraphic>();var mesh=graphic.canvasRenderer.GetMesh();
                    foreach(var vertex in mesh.vertices)
                    {
                        float depth=captureCamera.WorldToScreenPoint(graphic.transform.TransformPoint(vertex)).z;
                        Assert.That(depth,Is.GreaterThan(captureCamera.nearClipPlane).And.LessThan(captureCamera.farClipPlane),"The complete dimensional Noah must fit the actual capture camera, not be clipped into a misleading comparison.");
                    }
                }
                captureCamera.Render();RenderTexture.active=target;image=new Texture2D(720,1280,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,720,1280),0,0);image.Apply();return image;
            }
            catch{if(image!=null)UnityEngine.Object.DestroyImmediate(image);throw;}
            finally{RenderTexture.active=previous;}
        }
        [TearDown] public void ReleasePortraitCamera()
        {
            if(captureCanvas!=null)captureCanvas.worldCamera=null;
            if(captureCamera!=null){captureCamera.targetTexture=null;UnityEngine.Object.DestroyImmediate(captureCamera.gameObject);}
            if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}captureCamera=null;target=null;captureCanvas=null;
        }
        private void Capture(string directory,string name,TownDistrictId? expectedDistrict=null)
        {
            int writes=Store.Writes;string state=StateCodec.Encode(Flow.Game.State);var image=Portrait();
            try
            {
                var colors=Enumerable.Range(0,12).SelectMany(x=>Enumerable.Range(0,20).Select(y=>(Color32)image.GetPixel(30+x*55,30+y*60))).Distinct().Count();
                Assert.That(colors,Is.GreaterThan(12),"Portrait rendering must contain more than a clear background");
                if(expectedDistrict.HasValue)Assert.That(Flow.GetComponentsInChildren<Text>().Single(label=>label.name=="Town region name").text,Is.EqualTo(TownDistrictLayout.Name(expectedDistrict.Value)),"Cardinal arrival capture must show the current district ribbon");
                if(directory!=null)
                {
                    Directory.CreateDirectory(directory);File.WriteAllBytes(Path.Combine(directory,name+".png"),image.EncodeToPNG());
                    foreach(var town in Flow.GetComponentsInChildren<TownLifeView>())
                    {
                        var camera=town.World.Camera.Center;
                        string actors=string.Join(";",town.Actors.Select(a=>FormattableString.Invariant($"{a.CharacterId}@world({a.WorldPosition.X:F4},{a.WorldPosition.Y:F4})/view({((RectTransform)a.transform).anchoredPosition.x:F2},{((RectTransform)a.transform).anchoredPosition.y:F2})/visible={a.RegionVisible}")));
                        CaptureWorldTrace.Append(directory,FormattableString.Invariant($"MOFUMACHI_CAPTURE_WORLD {name} hero={town.World.Model.Hero.Id} camera=({camera.X:F4},{camera.Y:F4}) {actors}"));
                    }
                }
                Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(state));
            }
            finally{UnityEngine.Object.DestroyImmediate(image);}
        }
        [Serializable] private sealed class NorthRoutePoint {public float x,y;}
        [Serializable] private sealed class NorthRoute {public string id;public NorthRoutePoint[] points;}
        [Serializable] private sealed class NorthRouteEvidence
        {
            public string image="04-town-lv1-North.png",image_sha256,kind="navigation-route-overlay-not-gameplay-trace",origin="top-left";
            public int width=720,height=1280;public NorthRoute[] routes;
        }
        private static readonly string[] ReviewRouteIds={"route-1","route-2","route-3","route-4","route-5","route-6"};
        private void WriteNorthRouteEvidence(string directory,TownLifeView town)
        {
            WriteRouteEvidence(directory,town,"North",TownNorthMasterLayout.Circulation,new[]{"cottage-left","cottage-right","left-waiting","right-waiting","mosaic-cross","mosaic-loop"});
        }
        private void WriteRouteEvidence(string directory,TownLifeView town,string district,System.Collections.Generic.IReadOnlyList<TownPoint[]> links,string[] ids)
        {
            // MOFUMACHI_NORTH_DELIVERY_QUALITY: actual viewport projections from
            // the same Lv1 native capture, never hand-positioned route arrows.
            if(directory==null)return;
            var navigation=new TownAreaNavigation(town.World.Model.Area);
            NorthRoutePoint Project(TownPoint point)
            {
                var local=town.World.Local(point);var screen=captureCamera.WorldToScreenPoint(town.transform.TransformPoint(local));
                return new NorthRoutePoint{x=screen.x,y=1280-screen.y};
            }
            var evidence=new NorthRouteEvidence{image="04-town-lv1-"+district+".png",routes=links.Select((link,index)=>
            {
                var points=new System.Collections.Generic.List<TownPoint>{link[0]};
                foreach(var destination in link.Skip(1))
                {
                    var route=navigation.FindRoute(points[points.Count-1],destination);Assert.That(route,Is.Not.Empty,"Every annotated pair must exist on the real walkable area");
                    foreach(var point in route){Assert.That(town.World.Model.Area.CanTravel(points[points.Count-1],point),Is.True);points.Add(point);}
                }
                return new NorthRoute{id=ids[index],points=points.Select(Project).ToArray()};
            }).ToArray()};
            using(var sha=System.Security.Cryptography.SHA256.Create())
                evidence.image_sha256=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(directory,evidence.image)))).Replace("-","").ToLowerInvariant();
            File.WriteAllText(Path.Combine(directory,district.ToLowerInvariant()+"-route-projection.json"),JsonUtility.ToJson(evidence,true));
        }
        private IEnumerator CaptureBoundaryComparisons(string output)
        {
            Flow.Game.State.townGrowthLevel=6;Flow.ShowHome();yield return null;
            var town=Flow.GetComponentInChildren<TownLifeView>();town.StartInteraction();town.World.Freeze();
            var curve=Flow.GetComponentInChildren<TownBoundaryCurveGraphic>();
            Assert.That(curve,Is.Not.Null);bool curveEnabled=curve.enabled;
            Assert.That(Flow.GetComponentInChildren<TownSouthEntryBlendGraphic>().enabled,Is.True);
            Assert.That(Flow.GetComponentInChildren<TownBoundaryGardenGraphic>().enabled,Is.True);
            var html=new System.Text.StringBuilder("<!doctype html><meta charset='utf-8'><title>Street boundary comparison</title><style>body{font-family:sans-serif;background:#fff7ef;color:#50343b}section{display:flex;gap:12px}figure{margin:8px}img{width:360px;max-width:45vw}h2{clear:both}</style><h1>Street boundary / same-state Unity comparison</h1><p>Before disables only the curved band in the current field-stability runtime. Existing planting, entry bands and the depth/camera/navigation fixes stay active. This is a boundary-only comparison, NOT a complete old renderer or previous APK capture. After uses product code. Neither is a Pixel capture or acceptance.</p>");
            try
            {
                foreach(var view in new[]{
                    ("west-upper",new TownPoint(-1.05f,.80f)),("east-upper",new TownPoint(1.05f,.80f)),
                    ("west-lower",new TownPoint(-1.05f,-.85f)),("east-lower",new TownPoint(1.05f,-.85f)),
                    ("north-left",new TownPoint(-.55f,.92f)),("north-right",new TownPoint(.55f,.92f)),
                    ("south-left",new TownPoint(-.60f,-.90f)),("south-right",new TownPoint(.60f,-.90f)),
                    ("south-entry",new TownPoint(0,-1)),("west-join",new TownPoint(-.60f,0)),("east-join",new TownPoint(.60f,0))})
                {
                    town.World.LookAt(TownDistrictId.Central);town.World.Camera.Jump(view.Item2);town.World.Refresh();
                    var positions=town.World.Model.Residents.Select(r=>r.Position).ToArray();
                    curve.enabled=false;
                    string before="30-boundary-"+view.Item1+"-before-reconstructed",after="30-boundary-"+view.Item1+"-after";
                    Capture(output,before);curve.enabled=true;Capture(output,after);
                    Assert.That(town.World.Model.Residents.Select(r=>r.Position).ToArray(),Is.EqualTo(positions),"Only rendering layers may change within a comparison pair");
                    html.Append("<h2>"+view.Item1+"</h2><section><figure><figcaption>Before / frozen renderer reconstruction</figcaption><img loading='lazy' src='"+before+".png'></figure><figure><figcaption>After / product candidate</figcaption><img loading='lazy' src='"+after+".png'></figure></section>");
                }
                html.Append("<h1>Five areas / candidate overview</h1>");
                foreach(int level in new[]{1,3,6})foreach(var district in TownDistrictLayout.All)
                    html.Append("<figure><figcaption>"+district+" Lv."+level+" / After</figcaption><img loading='lazy' src='04-town-lv"+level+"-"+district+".png'></figure>");
                File.WriteAllText(Path.Combine(output,"street-boundary-before-after.html"),html.ToString());
                File.WriteAllText(Path.Combine(output,"street-boundary-comparison.json"),"{\"kind\":\"same-state-native-renderer-comparison\",\"before\":\"current field-stability runtime with curved band off; not historicalAPK\",\"pairs\":11,\"after\":\"product code\",\"pixel_verified\":false,\"adopted\":false}");
            }
            finally {curve.enabled=curveEnabled;}
        }
        private IEnumerator CaptureFieldStability(string output)
        {
            Flow.Game.State.townGrowthLevel=6;Flow.Game.State.favoriteCharacterId="miel";Flow.ShowHome();yield return null;EnsurePortraitCamera();
            var town=Flow.GetComponentInChildren<TownLifeView>();var hero=town.Actors.Single(a=>a.Controlled);
            IEnumerator Walk(TownPoint target)
            {
                Assert.That(town.World.GuidePoint(target),Is.True);int steps=0;
                while(town.World.Model.Hero.IsWalking&&steps<6000){town.AdvanceFrame(1f/30);steps++;if(steps%240==0)yield return null;}
                Assert.That(town.World.Model.Hero.IsWalking,Is.False,$"Stalled at {hero.WorldPosition.X},{hero.WorldPosition.Y}");
                Assert.That(TownWalkableArea.Distance(hero.WorldPosition,target),Is.LessThan(.002f),"Walking arrival, never Restore/FastTravel rescue");
            }
            foreach(var view in new[]{("front",new TownPoint(0,.12f)),("behind",new TownPoint(0,.441f))})
            {
                yield return Walk(view.Item2);for(int settle=0;settle<45;settle++)town.AdvanceFrame(1f/30);
                town.RefreshWorldPresentation();var fountain=town.GetComponentInChildren<TownFountainGraphic>();
                Assert.That(hero.transform.GetSiblingIndex()>fountain.transform.GetSiblingIndex(),Is.EqualTo(view.Item1=="front"));
                if(view.Item1=="behind")
                {
                    Assert.That(town.World.Model.Area.Contains(hero.WorldPosition),Is.True,"Far-side proof must stand outside the real fountain obstacle");
                    var body=RectTransformUtility.CalculateRelativeRectTransformBounds((RectTransform)town.transform,hero.VisualRoot);
                    // The fountain Graphic's RectTransform is the whole field;
                    // use its actual mesh projection, not that oversized rect.
                    float waterTop=town.World.Local(TownFountainWaterLayout.Project(new TownPoint(.5f,1),TownFountainWaterLayout.DisplayScale)).y;
                    Assert.That(body.min.y,Is.LessThan(waterTop),"Far-side evidence must overlap the fountain mesh bounds, not stand well above it");
                }
                Capture(output,"31-field-fountain-"+view.Item1);
            }
            var entry=TownWorldMap.Place("east.entry");yield return Walk(entry);yield return Walk(new TownPoint(1.5999999f,.45000005f));Capture(output,"31-field-east-corner-out");
            yield return Walk(entry);Capture(output,"31-field-east-corner-return");
            foreach(var edge in new[]{("East",new TownPoint(1.8f,.68f)),("West",new TownPoint(-1.8f,.68f)),("North",new TownPoint(.43f,2.98f)),("South",new TownPoint(.43f,-2.98f)),("Central",new TownPoint(0,.97f))})
            {
                yield return Walk(edge.Item2);Capture(output,"31-field-"+edge.Item1+"-edge");
                yield return Walk(TownWorldMap.Place("square"));Capture(output,"31-field-"+edge.Item1+"-return");
            }
            File.WriteAllText(Path.Combine(output,"field-stability.json"),"{\"kind\":\"native-walking-and-depth-fixture\",\"edge_return_pairs\":5,\"east_corner_return\":true,\"fountain_front_and_behind\":true,\"teleport_rescue\":false,\"pixel_verified\":false,\"adopted\":false}");
        }
        [UnityTest] public IEnumerator PortraitScreensRenderFromIsolatedStateWithoutWritingProgress()
        {
            yield return EnterHome();string output=OutputDirectory();
            bool fiveAreaReview=Environment.GetCommandLineArgs().Contains("-mofumachiFiveAreaReview");
            bool southReview=fiveAreaReview||Environment.GetCommandLineArgs().Contains("-mofumachiSouthReview");
            bool northReview=southReview||Environment.GetCommandLineArgs().Contains("-mofumachiNorthReview");
            bool eastReview=northReview||Environment.GetCommandLineArgs().Contains("-mofumachiEastReview");
            bool westReview=eastReview||Environment.GetCommandLineArgs().Contains("-mofumachiWestReview");
            if(Environment.GetCommandLineArgs().Contains("-mofumachiCentralReview")||westReview)
            {
                // Configure portrait before building the first town, rather than
                // changing its projection for the first time inside Capture().
                EnsurePortraitCamera();Canvas.ForceUpdateCanvases();yield return null;
                foreach(int level in new[]{1,3,6})
                {
                    Flow.Game.State.townGrowthLevel=level;Flow.ShowHome();EnsurePortraitCamera();Canvas.ForceUpdateCanvases();
                    var life=Flow.GetComponentInChildren<TownLifeView>();life.World.LookAt(TownDistrictId.Central);life.World.Refresh();yield return null;Canvas.ForceUpdateCanvases();
                    foreach(var piece in life.GetComponent<TownSceneryView>().Pieces.Where(p=>p.Rect.gameObject.activeSelf))
                        Assert.That(Vector2.Distance(piece.Rect.anchoredPosition,life.World.Local(piece.Placement.Feet,(RectTransform)life.transform)),Is.LessThan(.01f),"First portrait must not retain scenery coordinates from the Editor viewport");
                    foreach(var actor in life.Actors.Where(a=>a.RegionVisible))
                        Assert.That(Vector2.Distance(((RectTransform)actor.transform).anchoredPosition,life.World.Local(actor.WorldPosition)),Is.LessThan(.01f),"Portrait actor and ground must use the same settled projection");
                    var guest=life.Actors.Single(a=>!a.Controlled&&TownDistrictLayout.At(a.WorldPosition)==TownDistrictId.Central);
                    var name=guest.transform.Find("Town actor name panel");Assert.That(name.gameObject.activeInHierarchy,Is.True);
                    var plate=RectTransformUtility.CalculateRelativeRectTransformBounds(life.transform,name);
                    foreach(var piece in life.GetComponent<TownSceneryView>().Pieces.Where(p=>p.Rect.gameObject.activeSelf&&p.Placement.Feet.Y<guest.WorldPosition.Y))
                        Assert.That(plate.Intersects(RectTransformUtility.CalculateRelativeRectTransformBounds(life.transform,piece.Rect)),Is.False,"Arrival nameplate must not be covered by foreground scenery");
                    if(level==6)
                    {
                        var bunting=Flow.GetComponentsInChildren<TownGrowthPropGraphic>().Single(p=>p.Prop==TownGrowthProp.StreetBunting&&p.Region==TownRegionId.Central);
                        var ribbon=Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Town region name");
                        var festive=RectTransformUtility.CalculateRelativeRectTransformBounds(life.transform,bunting.transform);
                        var caption=RectTransformUtility.CalculateRelativeRectTransformBounds(life.transform,ribbon.transform);
                        Assert.That(festive.max.y,Is.LessThan(caption.min.y),"The festival display must fit below the location ribbon");
                    }
                    Capture(output,"04-town-lv"+level+"-Central",TownDistrictId.Central);
                    if(fiveAreaReview&&level==1)WriteRouteEvidence(output,life,"Central",TownReviewRoutes.For(TownDistrictId.Central),ReviewRouteIds);
                }
                if(westReview)
                {
                    string westOutput=output==null?null:Path.Combine(output,"west");
                    foreach(int level in new[]{1,3,6})
                    {
                        Flow.Game.State.townGrowthLevel=level;Flow.ShowHome();EnsurePortraitCamera();Canvas.ForceUpdateCanvases();
                        var town=Flow.GetComponentInChildren<TownLifeView>();town.World.LookAt(TownDistrictId.West);town.World.Refresh();yield return null;Canvas.ForceUpdateCanvases();
                        foreach(var piece in town.GetComponent<TownSceneryView>().Pieces.Where(p=>p.Rect.gameObject.activeSelf))
                            Assert.That(Vector2.Distance(piece.Rect.anchoredPosition,town.World.Local(piece.Placement.Feet,(RectTransform)town.transform)),Is.LessThan(.01f));
                        Capture(westOutput,"04-town-lv"+level+"-West",TownDistrictId.West);
                        if(fiveAreaReview&&level==1)WriteRouteEvidence(westOutput,town,"West",TownReviewRoutes.For(TownDistrictId.West),ReviewRouteIds);
                    }
                    Flow.Game.State.townGrowthLevel=1;Flow.ShowHome();EnsurePortraitCamera();var west=Flow.GetComponentInChildren<TownLifeView>();
                    west.World.LookAt(TownDistrictId.West);west.World.Camera.Jump(new TownPoint(-.60f,0));west.World.Refresh();Capture(westOutput,"west-connection");
                    west.World.FastTravel(TownDistrictId.Central);Assert.That(west.World.GuideDistrict(TownDistrictId.West),Is.True);
                    for(int i=0;i<3000&&west.World.Model.Hero.Position.X>-.85f;i++)west.AdvanceFrame(1f/30);
                    Assert.That(west.World.Model.Hero.Position.X,Is.LessThanOrEqualTo(-.85f));
                    Assert.That(west.IsSpeaking,Is.False,"Walking must stay visible without unsolicited dialogue");Capture(westOutput,"west-walking");
                    for(int i=0;i<3000&&west.World.Model.Hero.IsWalking;i++)west.AdvanceFrame(1f/30);
                    Assert.That(west.World.Model.Hero.IsWalking,Is.False);
                    var facility=west.GetComponent<TownFacilityView>();Assert.That(facility.RequestEnter("cafe"),Is.True);
                    for(int i=0;i<3000&&facility.Entry.Phase==TownEntryPhase.Walking;i++)west.AdvanceFrame(1f/30);
                    Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Arrived));
                    Assert.That(west.IsSpeaking,Is.False,"The cafe arrival cue must not be covered by dialogue");
                    Assert.That(west.transform.Find("Facility entrance cafe/Facility sign cafe").gameObject.activeInHierarchy,Is.True);
                    Capture(westOutput,"west-door-arrived");
                    Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home),"Walking to the cafe does not automatically enter");
                }
                if(eastReview)
                {
                    string eastOutput=output==null?null:Path.Combine(output,"east");
                    foreach(int level in new[]{1,3,6})
                    {
                        Flow.Game.State.townGrowthLevel=level;Flow.ShowHome();EnsurePortraitCamera();Canvas.ForceUpdateCanvases();
                        var town=Flow.GetComponentInChildren<TownLifeView>();town.World.FastTravel(TownDistrictId.Central);town.World.LookAt(TownDistrictId.East);town.World.Refresh();yield return null;Canvas.ForceUpdateCanvases();
                        foreach(var piece in town.GetComponent<TownSceneryView>().Pieces.Where(p=>p.Rect.gameObject.activeSelf))
                            Assert.That(Vector2.Distance(piece.Rect.anchoredPosition,town.World.Local(piece.Placement.Feet,(RectTransform)town.transform)),Is.LessThan(.01f));
                        EastMasterReviewTests.AssertNameClearOfBenches(town,town.Actors.Single(a=>a.CharacterId=="fio"));
                        Capture(eastOutput,"04-town-lv"+level+"-East",TownDistrictId.East);
                        if(fiveAreaReview&&level==1)WriteRouteEvidence(eastOutput,town,"East",TownReviewRoutes.For(TownDistrictId.East),ReviewRouteIds);
                    }
                    Flow.Game.State.townGrowthLevel=1;Flow.ShowHome();EnsurePortraitCamera();var east=Flow.GetComponentInChildren<TownLifeView>();
                    east.World.LookAt(TownDistrictId.East);east.World.Camera.Jump(new TownPoint(.60f,0));east.World.Refresh();Capture(eastOutput,"east-connection");
                    east.World.FastTravel(TownDistrictId.Central);Assert.That(east.World.GuideDistrict(TownDistrictId.East),Is.True);
                    for(int i=0;i<3000&&east.World.Model.Hero.Position.X<.85f;i++)east.AdvanceFrame(1f/30);
                    Assert.That(east.World.Model.Hero.Position.X,Is.GreaterThanOrEqualTo(.85f));
                    Assert.That(east.IsSpeaking,Is.False,"Walking must stay visible without unsolicited dialogue");Capture(eastOutput,"east-walking");
                    for(int i=0;i<3000&&east.World.Model.Hero.IsWalking;i++)east.AdvanceFrame(1f/30);
                    Assert.That(east.World.Model.Hero.IsWalking,Is.False);
                    var facility=east.GetComponent<TownFacilityView>();Assert.That(facility.RequestEnter("shop"),Is.True);
                    for(int i=0;i<3000&&facility.Entry.Phase==TownEntryPhase.Walking;i++)east.AdvanceFrame(1f/30);
                    Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Arrived));
                    Assert.That(east.IsSpeaking,Is.False,"The florist arrival cue must not be covered by dialogue");
                    Assert.That(east.transform.Find("Facility entrance shop/Facility sign shop").gameObject.activeInHierarchy,Is.True);
                    Capture(eastOutput,"east-door-arrived");
                    Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home),"Walking to the florist does not automatically enter");
                }
                if(northReview)
                {
                    string northOutput=output==null?null:Path.Combine(output,"north");
                    foreach(int level in new[]{1,3,6})
                    {
                        Flow.Game.State.townGrowthLevel=level;Flow.ShowHome();EnsurePortraitCamera();Canvas.ForceUpdateCanvases();
                        var town=Flow.GetComponentInChildren<TownLifeView>();town.World.FastTravel(TownDistrictId.Central);town.World.LookAt(TownDistrictId.North);town.World.Refresh();yield return null;Canvas.ForceUpdateCanvases();
                        foreach(var piece in town.GetComponent<TownSceneryView>().Pieces.Where(p=>p.Rect.gameObject.activeSelf))
                            Assert.That(Vector2.Distance(piece.Rect.anchoredPosition,town.World.Local(piece.Placement.Feet,(RectTransform)town.transform)),Is.LessThan(.01f));
                        foreach(var resident in town.Actors.Where(a=>a.RegionVisible&&a.transform.Find("Town actor name panel").gameObject.activeInHierarchy))
                            EastMasterReviewTests.AssertNameClearOfBenches(town,resident);
                        Capture(northOutput,"04-town-lv"+level+"-North",TownDistrictId.North);
                        if(level==1)WriteNorthRouteEvidence(northOutput,town);
                    }
                    Flow.Game.State.townGrowthLevel=1;Flow.ShowHome();EnsurePortraitCamera();var north=Flow.GetComponentInChildren<TownLifeView>();
                    north.World.LookAt(TownDistrictId.North);north.World.Camera.Jump(new TownPoint(0,1f));north.World.Refresh();Capture(northOutput,"north-connection");
                    north.World.FastTravel(TownDistrictId.Central);Assert.That(north.World.GuideDistrict(TownDistrictId.North),Is.True);
                    for(int i=0;i<3000&&north.World.Model.Hero.Position.Y<1.32f;i++)north.AdvanceFrame(1f/30);
                    Assert.That(north.World.Model.Hero.Position.Y,Is.GreaterThanOrEqualTo(1.32f));
                    Assert.That(north.IsSpeaking,Is.False,"Walking must stay visible without unsolicited dialogue");Capture(northOutput,"north-walking");
                    for(int i=0;i<3000&&north.World.Model.Hero.IsWalking;i++)north.AdvanceFrame(1f/30);
                    Assert.That(north.World.Model.Hero.IsWalking,Is.False);
                    var facility=north.GetComponent<TownFacilityView>();Assert.That(facility.RequestEnter("quest"),Is.True);
                    for(int i=0;i<3000&&facility.Entry.Phase==TownEntryPhase.Walking;i++)north.AdvanceFrame(1f/30);
                    Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Arrived));
                    Assert.That(north.IsSpeaking,Is.False,"The cottage arrival cue must not be covered by dialogue");
                    Assert.That(north.transform.Find("Facility entrance quest/Facility sign quest").gameObject.activeInHierarchy,Is.True);
                    Capture(northOutput,"north-door-arrived");
                    Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home),"Walking to the cottage does not automatically enter");
                }
                if(southReview)
                {
                    string southOutput=output==null?null:Path.Combine(output,"south");
                    foreach(int level in new[]{1,3,6})
                    {
                        Flow.Game.State.townGrowthLevel=level;Flow.ShowHome();EnsurePortraitCamera();var town=Flow.GetComponentInChildren<TownLifeView>();town.World.FastTravel(TownDistrictId.Central);town.World.LookAt(TownDistrictId.South);town.World.Refresh();yield return null;Canvas.ForceUpdateCanvases();
                        foreach(var resident in town.Actors.Where(a=>a.RegionVisible&&a.transform.Find("Town actor name panel").gameObject.activeInHierarchy))EastMasterReviewTests.AssertNameClearOfBenches(town,resident);
                        Capture(southOutput,"04-town-lv"+level+"-South",TownDistrictId.South);
                        if(level==1)WriteRouteEvidence(southOutput,town,"South",TownSouthMasterLayout.Circulation,new[]{"gazebo-left","gazebo-right","left-rest","right-rest","rest-cross","garden-loop"});
                    }
                    Flow.Game.State.townGrowthLevel=1;Flow.ShowHome();EnsurePortraitCamera();var south=Flow.GetComponentInChildren<TownLifeView>();
                    south.World.LookAt(TownDistrictId.South);south.World.Camera.Jump(new TownPoint(0,-1f));south.World.Refresh();Capture(southOutput,"south-connection");
                    south.World.FastTravel(TownDistrictId.Central);Assert.That(south.World.GuidePoint(TownWorldMap.Place("front-promenade")),Is.True);
                    for(int i=0;i<3000&&south.World.Model.Hero.Position.Y> -1.32f;i++)south.AdvanceFrame(1f/30);
                    Assert.That(south.World.Model.Hero.Position.Y,Is.LessThanOrEqualTo(-1.32f));Assert.That(south.IsSpeaking,Is.False);Capture(southOutput,"south-walking");
                    for(int i=0;i<3000&&south.World.Model.Hero.IsWalking;i++)south.AdvanceFrame(1f/30);
                    Assert.That(south.World.Model.Hero.IsWalking,Is.False);Assert.That(TownWalkableArea.Distance(south.World.Model.Hero.Position,TownWorldMap.Place("front-promenade")),Is.LessThan(.002f));
                    south.World.Model.Pause(south.World.Model.Hero.Id,2);south.RefreshWorldPresentation();Capture(southOutput,"south-rest-arrived");
                    Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));
                }
                if(output!=null)File.WriteAllLines(Path.Combine(output,"texture-memory.txt"),new[]{TownCentralMasterLayout.Resource,TownFountainGraphic.Resource,TownWestMasterLayout.Resource,TownSouthMasterLayout.Resource}.Select(key=>key+": "+UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(Resources.Load<Texture2D>(key))+" bytes"));
                LogAssert.NoUnexpectedReceived();yield break;
            }
            Flow.ShowMerge();yield return null;Capture(output,"01-merge-start");
            if(output!=null)
            {
                Flow.Game.State.mergeBoard.Clear();int cell=0;
                foreach(var item in ItemCatalog.All)for(int level=1;level<=3;level++)Flow.Game.State.mergeBoard.Add(new BoardItem(item.Id,level,cell++));
                Flow.ShowMerge();Capture(output,"02-merge-all-items");Flow.TapCell(0);Capture(output,"03-merge-selection");
                foreach(int level in new[]{1,3,6})foreach(var district in TownDistrictLayout.All)
                {
                    Flow.Game.State.townGrowthLevel=level;Flow.ShowHome();var life=Flow.GetComponentInChildren<TownLifeView>();life.World.LookAt(district);yield return null;Canvas.ForceUpdateCanvases();
                    Capture(output,"04-town-lv"+level+"-"+district);
                }
                Flow.ShowHome();var town=Flow.GetComponentInChildren<TownLifeView>();town.Regions.ReturnToHero();town.GetComponent<TownStreetWalkView>().Arm();Capture(output,"05-street-selected");
                town.GuideToPlace("fountain");
                for(int frame=0;frame<8;frame++){for(int step=0;step<4;step++)town.AdvanceFrame(1f/30);Capture(output,"06-walking-"+frame.ToString("D2"));}
                Flow.ShowQuest();Capture(output,"07-quest-story-goal");Flow.ShowSettings();
                var scroll=Flow.GetComponentsInChildren<ScrollRect>().Single();scroll.verticalNormalizedPosition=.12f;Canvas.ForceUpdateCanvases();Capture(output,"08-music-selection");
                Flow.GoBack();Flow.ShowHome();town=Flow.GetComponentInChildren<TownLifeView>();
                foreach(var seam in new[]{("west",new TownPoint(-.60f,0)),("east",new TownPoint(.60f,0)),("north",new TownPoint(0,1)),("south",new TownPoint(0,-1))})
                {town.World.Camera.Jump(seam.Item2);town.World.Refresh();Canvas.ForceUpdateCanvases();Capture(output,"09-connected-path-"+seam.Item1);}
                Flow.ShowCharacterSelection();
                foreach(var character in CharacterCatalog.All){Flow.PreviewFavorite(character.Id);var bookScroll=Flow.GetComponentsInChildren<ScrollRect>().Single();bookScroll.verticalNormalizedPosition=.85f;Canvas.ForceUpdateCanvases();Capture(output,"10-book-"+character.Id);}
                Flow.GetComponentsInChildren<ScrollRect>().Single().verticalNormalizedPosition=.12f;Canvas.ForceUpdateCanvases();Capture(output,"10-book-discovery-details");
                Flow.ShowHome();Flow.ShowToday();Capture(output,"11-today-start");Flow.CloseToday();
                Flow.TalkToCharacter("lulu");Flow.Daily.RecordWalk();for(int n=0;n<3;n++)Flow.Daily.RecordMerge();Flow.ShowToday();Capture(output,"12-today-ready");Flow.ClaimDaily();Capture(output,"13-today-flower-collected");Flow.CloseToday();
                Flow.Game.State.activeQuestId="";Flow.Game.State.completedQuestIds.Clear();Flow.Game.State.completedQuestIds.AddRange(QuestCatalog.All.Select(q=>q.Id));Flow.ShowHome();Flow.GetComponentInChildren<TownLifeView>().Regions.ReturnToHero();Capture(output,"14-finale-still-has-daily-goals");
                // Major Polish: same native renderer, actual simulation and actual
                // pointer handlers. These are fixture captures, never Pixel claims.
                Flow.ShowHome();town=Flow.GetComponentInChildren<TownLifeView>();
                foreach(var side in new[]{("left",new TownPoint(-.30f,.30f)),("right",new TownPoint(.30f,.30f))})
                {town.World.Model.Restore("momo",side.Item2);town.World.Camera.Jump(TownWorldMap.Place("square"));town.World.Refresh();town.AdvanceFrame(1f/30);Capture(output,"15-wide-park-"+side.Item1);}
                town.World.Model.Restore("momo",new TownPoint(-.30f,-.20f));town.World.Model.Restore("lulu",new TownPoint(.30f,-.20f));town.World.Model.TryGuidePoint(new TownPoint(.40f,-.20f));town.World.Model.TryGuide("lulu","flowers");
                for(int frame=0;frame<4;frame++){for(int step=0;step<30;step++)town.AdvanceFrame(1f/30);Capture(output,"16-residents-passing-"+frame.ToString("D2"));}
                town.World.Model.FastTravel(TownRegionId.Central);town.AdvanceFrame(1f/30);EnsurePortraitCamera();
                var controlled=town.Actors.Single(a=>a.Controlled);var drag=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerId=777};
                drag.pointerPressRaycast=new UnityEngine.EventSystems.RaycastResult{module=captureCanvas.GetComponent<GraphicRaycaster>()};
                controlled.OnPointerDown(drag);controlled.OnBeginDrag(drag);
                drag.position=RectTransformUtility.WorldToScreenPoint(captureCamera,town.transform.TransformPoint(town.World.Local(new TownPoint(.40f,-.15f))));controlled.OnDrag(drag);
                for(int frame=0;frame<3;frame++){for(int step=0;step<20;step++)town.AdvanceFrame(1f/30);Capture(output,"17-finger-follow-"+frame.ToString("D2"));}
                controlled.OnPointerUp(drag);for(int step=0;step<15;step++)town.AdvanceFrame(1f/30);Capture(output,"17-finger-released");
                town.World.FastTravel(TownRegionId.West);Assert.That(town.GuideToPlace("east.flowers"),Is.True);
                var checkpoints=new[]{("west",-1.20f),("west-join",-.60f),("central",0f),("east-join",.60f),("east",1.20f)};
                foreach(var checkpoint in checkpoints)
                {
                    for(int step=0;step<6000&&town.World.Model.Hero.IsWalking&&town.World.Model.Hero.Position.X<checkpoint.Item2;step++)town.AdvanceFrame(1f/30);
                    Assert.That(town.World.Model.Hero.Position.X,Is.GreaterThanOrEqualTo(checkpoint.Item2-.01f),checkpoint.Item1+" must be reached by walking");
                    Capture(output,"18-continuous-walk-"+checkpoint.Item1);
                }
                Flow.ShowHome();Flow.TalkToCharacter("noah");Flow.TalkToCharacter("noah");Flow.TalkToCharacter("noah");Capture(output,"19-book-new-in-town");
                Flow.OpenCharacterBook("noah");Capture(output,"19-book-discovery-first-page");
                // Three physical facilities: arrival, deliberate door tap and opening, not
                // repositioned actors or generated mockups of a future screen.
                foreach(string facilityId in new[]{"quest","shop","cafe"})
                {
                    Flow.ShowHome();town=Flow.GetComponentInChildren<TownLifeView>();town.World.FastTravel(facilityId=="quest"?TownDistrictId.North:facilityId=="cafe"?TownDistrictId.West:TownDistrictId.East);
                    string prefix=facilityId=="quest"?"20-quest":facilityId=="cafe"?"21b-cafe":"21-shop";Capture(output,prefix+"-cabin");
                    var facility=town.GetComponent<TownFacilityView>();Assert.That(facility.RequestEnter(facilityId),Is.True);
                    for(int step=0;step<18;step++)town.AdvanceFrame(1f/30);Capture(output,prefix+"-walking");
                    for(int step=0;step<3000&&facility.Entry.Phase==TownEntryPhase.Walking;step++)town.AdvanceFrame(1f/30);
                    Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Arrived));Capture(output,prefix+"-arrived-door-prompt");
                    Assert.That(facility.RequestEnter(facilityId),Is.True);Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Opening));
                    for(int step=0;step<8;step++)town.AdvanceFrame(1f/30);Capture(output,prefix+"-door-opening");
                    for(int step=0;step<30&&Flow.CurrentScreen==ScreenId.Home&&!Flow.IsModalOpen;step++)town.AdvanceFrame(1f/30);
                    Assert.That(facilityId=="quest"?Flow.CurrentScreen==ScreenId.Quest:Flow.IsModalOpen,Is.True);Capture(output,prefix+"-inside");
                    if(Flow.IsModalOpen)Flow.GoBack();
                }
                // Keep editor preferences untouched while exercising the real
                // candidate selection and context playback code.
                AudioManager.Instance.UseMusicRolePreferences(new CaptureMusicStore(),new CaptureMusicStore(),new CaptureMusicStore());
                Flow.ShowSettings();Flow.GetComponentsInChildren<ScrollRect>().Single().verticalNormalizedPosition=.55f;Canvas.ForceUpdateCanvases();Capture(output,"22-title-candidates");
                Flow.GetComponentsInChildren<ScrollRect>().Single().verticalNormalizedPosition=.10f;Canvas.ForceUpdateCanvases();Capture(output,"22-merge-candidates");
                foreach(string song in new[]{"title-letter","title-garden","merge-picnic","merge-ribbon","merge-pop-promenade"})
                {
                    if(Flow.CurrentScreen!=ScreenId.Settings)Flow.ShowSettings();Flow.ListenMusicInScene(song);yield return null;
                    Assert.That(AudioManager.Instance.SelectedMusicId,Is.EqualTo(song));Assert.That(AudioManager.Instance.GetComponents<AudioSource>().Single(s=>s.loop).clip,Is.SameAs(Resources.Load<AudioClip>("Mofumachi/Audio/"+MusicCatalog.Find(song).Resource)));
                    Capture(output,"23-"+song);
                }
                Flow.ShowHome();town=Flow.GetComponentInChildren<TownLifeView>();town.World.FastTravel(TownRegionId.Central);town.AdvanceFrame(1f/30);
                Capture(output,"24-home-unobstructed");Flow.OpenTownMenu();Capture(output,"24-menu-open");Flow.GoBack();Capture(output,"24-menu-return");
                var representative=town.Actors.Single(a=>a.Controlled);for(int step=0;step<30;step++)town.AdvanceFrame(1f/30);Capture(output,"25-character-idle");
                Assert.That(town.World.GuidePlace("bench-right"),Is.True);for(int step=0;step<8;step++)town.AdvanceFrame(1f/30);Capture(output,"25-character-step-a");
                for(int step=0;step<12;step++)town.AdvanceFrame(1f/30);Capture(output,"25-character-step-b");typeof(TownActor).GetMethod("Surprise",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(representative,null);for(int step=0;step<4;step++)town.AdvanceFrame(1f/30);Capture(output,"25-character-greeting");
                foreach(var depth in new[]{("behind",new TownPoint(0,.53f)),("in-front",new TownPoint(0,.06f))})
                {town.World.Model.Restore(representative.CharacterId,depth.Item2);town.World.ReturnToHero();town.AdvanceFrame(1f/30);Capture(output,"25-character-"+depth.Item1+"-fountain");}
                foreach(var district in new[]{TownDistrictId.North,TownDistrictId.South})
                {
                    town.World.FastTravel(TownDistrictId.Central);Assert.That(town.World.GuideDistrict(district),Is.True);
                    bool north=district==TownDistrictId.North;float direction=north?1:-1;
                    for(int step=0;step<5000&&town.World.Model.Hero.IsWalking&&town.World.Model.Hero.Position.Y*direction<.99f;step++)town.AdvanceFrame(1f/30);
                    Assert.That(town.World.Model.Hero.Position.Y*direction,Is.GreaterThanOrEqualTo(.99f));Capture(output,"26-cardinal-"+district+"-join");
                    for(int step=0;step<5000&&town.World.Model.Hero.IsWalking;step++)town.AdvanceFrame(1f/30);
                    Assert.That(town.World.Model.Hero.District,Is.EqualTo(district));Assert.That(town.World.Model.Hero.IsWalking,Is.False);Capture(output,"26-cardinal-"+district+"-arrival",district);
                }
                Flow.ShowMerge();Flow.TapCell(0);Capture(output,"27-merge-finale-live-purpose");
                Flow.ShowHome();town=Flow.GetComponentInChildren<TownLifeView>();
                string visiting=town.World.Model.Residents.Where(r=>!r.Controlled).OrderByDescending(r=>TownWalkableArea.Distance(r.Position,town.World.Model.Hero.Position)).First().Id;
                int meetings=Flow.Daily.Data.Characters.Single(c=>c.Id==visiting).Meetings;
                Assert.That(Flow.VisitTownCharacter(visiting),Is.True);var meeting=town.GetComponent<TownMeetingJourney>();
                for(int step=0;step<12;step++){town.AdvanceFrame(1f/30);meeting.Advance(1f/30);}Capture(output,"28-meet-friend-walking");
                for(int step=0;step<6000&&meeting.IsActive;step++){town.AdvanceFrame(1f/30);meeting.Advance(1f/30);}
                Assert.That(meeting.IsActive,Is.False);Assert.That(Flow.Daily.Data.Characters.Single(c=>c.Id==visiting).Meetings,Is.EqualTo(meetings));Capture(output,"28-meet-friend-arrived");
                // Use the real generate/drag handlers in this isolated save. No
                // player's sparse board is filled to make a prettier screenshot.
                Flow.ShowMerge();Flow.GenerateFromFrame("tea");yield return null;EnsurePortraitCamera();Canvas.ForceUpdateCanvases();captureCamera.Render();
                var mergeSource=Flow.GetComponentsInChildren<MergeBoardView>().Single(v=>v.name=="Cell 0");
                int targetCell=Flow.Game.State.mergeBoard.First(b=>b.cellIndex!=0&&b.itemId=="tea"&&b.level==1).cellIndex;
                var mergeTarget=Flow.GetComponentsInChildren<MergeBoardView>().Single(v=>v.name=="Cell "+targetCell);
                Vector2 PointerAt(Transform item)=>RectTransformUtility.WorldToScreenPoint(captureCamera,item.TransformPoint(((RectTransform)item).rect.center));
                var mergeDrag=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerId=778,position=PointerAt(mergeSource.transform)};
                mergeDrag.pointerPressRaycast=new UnityEngine.EventSystems.RaycastResult{module=captureCanvas.GetComponent<GraphicRaycaster>()};
                mergeSource.OnBeginDrag(mergeDrag);Capture(output,"29-merge-drag-lift");
                mergeDrag.position=PointerAt(mergeTarget.transform);mergeSource.OnDrag(mergeDrag);Capture(output,"29-merge-drag-match");mergeSource.OnEndDrag(mergeDrag);
                yield return new WaitForSecondsRealtime(.8f);Assert.That(Flow.Game.Board.At(targetCell).level,Is.EqualTo(2));Capture(output,"29-merge-landed");
                yield return CaptureBoundaryComparisons(output);
                yield return CaptureFieldStability(output);
                string[] art={"TownBackground","TownWestBackground","TownEastBackground","CharacterBodyAtlas","TownGrowthPropsAtlas","TownGardenSurface","TownSeamlessMap","TownStreetPropsAtlas","TownLandmarksAtlas","TownNatureAtlas","TownVillageDetailsAtlas","MergeBoardFrame","TownCentralGround","TownCentralFountain","TownWestGround"};
                File.WriteAllLines(Path.Combine(output,"texture-memory.txt"),art.Select(key=>key+": "+UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(Resources.Load<Texture2D>("Mofumachi/"+key))+" bytes"));
                File.WriteAllText(Path.Combine(output,"README.txt"),"Actual Unity UGUI RenderTexture captures, 720x1280. Isolated test-only save state; not Pixel 3a captures. Full suite165 NEW full PNG plus real-Update fountain-live crops/timestamps/player: existing102 integrated frames,22 boundary comparison frames,14 field-stability frames,6 actual delivery-growth/reload frames,10 normal fountain OFF/ON and animation frames,8 timed-show frames including3same-instant show OFF controls. Three separately identified actual prior5edfa Before PNGs are copied under fountain-before with their original hashes, not relabeled as new captures. Fountain Before disables only the new water layer and retains the original fountain/ripples; not a previous APK reconstruction. 30-boundary Before disables only the curved band in the CURRENT runtime, retaining the new field-stability depth/camera/navigation fixes; it is a boundary-only comparison, NOT a complete frozen5a49 renderer or previous APK/Pixel capture. 31-field shows actual walking arrival/return in all5areas and the East building corner, plus front/behind fountain depth; no teleport rescue. 30Hz simulation is not device FPS; Editor texture memory is not Pixel RAM. Stable region/place/save IDs retained. Pixel save retention, audio,10minuteplay and subjective quality still require actual acceptance.\n");
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
