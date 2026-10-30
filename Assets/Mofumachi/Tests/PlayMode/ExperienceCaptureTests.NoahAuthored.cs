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
        [Serializable] private sealed class AuthoredProof
        {
            public string kind="native-noah-authored-parts-preview",gitRevision,productFingerprint,input_sha256,packed_resource_sha256;
            public bool native_unity=true,pixel_verified=false,adopted=false,front_independent_rig_complete=false,new_direction_expression_complete=false;
            public int directional_frames,sorting_display_only_examples;public bool actual_conversation_verified,tap_parent_route_verified;
            public string sequence_kind="fixed-step actual public GuidePoint and AdvanceFrame; not real-time FPS";
            public string[] observed_directions;public List<NativePair> pairs=new List<NativePair>();public List<AuthoredFrame> frames=new List<AuthoredFrame>();
        }
        [Serializable] private sealed class AuthoredFrame
        {public int index;public float simulated_seconds,world_x,world_y;public string direction;public NativeImage image;}
        [UnityTest,Timeout(120000)] public IEnumerator NoahAuthoredPartsPreviewPreservesState()
        {
            if(!Environment.GetCommandLineArgs().Contains("-mofumachiNoahAuthoredPreview"))
            {Assert.That(Resources.Load<TextAsset>("Mofumachi/NoahAuthoredTrial/layout"),Is.Null,"Normal QA must not retain experimental source resources.");yield break;}
            string PackedDigest()
            {
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"Mofumachi/Resources/Mofumachi/NoahAuthoredTrial"));
                var rows=Directory.GetFiles(folder,"*",SearchOption.AllDirectories).Where(p=>Path.GetFileName(p)!=".owner").Select(p=>p.Substring(folder.Length+1).Replace('\\','/')+":"+EvidenceSha(File.ReadAllBytes(p))).OrderBy(s=>s,StringComparer.Ordinal);
                return EvidenceSha(System.Text.Encoding.UTF8.GetBytes(string.Join("\n",rows)));
            }
            string packed=PackedDigest();
            yield return EnterHome();Assert.That(Flow.Game.SelectFavorite("noah"),Is.True);Flow.ShowHome();yield return null;
            EnsurePortraitCamera();var field=Flow.GetComponentInChildren<TownLifeView>();field.StartInteraction();
            var noah=field.Actors.Single(a=>a.CharacterId=="noah");var friend=field.Actors.Single(a=>a.CharacterId=="momo");
            Assert.That(noah.Controlled&&field.CanInteract,Is.True);
            string directory=OutputDirectory();Assert.That(directory,Is.Not.Null.And.Not.Empty);string output=Path.Combine(directory,"noah-authored");Directory.CreateDirectory(output);
            var proof=new AuthoredProof{gitRevision=EvidenceArgument("-mofumachiSourceRevision"),productFingerprint=EvidenceArgument("-mofumachiProductFingerprint"),packed_resource_sha256=packed};
            string saved=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var social=typeof(TownLifeView).GetField("nextSocial",flags);social.SetValue(field,float.PositiveInfinity);
            foreach(var r in field.World.Model.Residents.Where(r=>!r.Controlled))field.World.Model.Pause(r.Id,20);
            field.World.Camera.Jump(new TownPoint(0,.15f));field.World.Refresh();
            var warm=Portrait();UnityEngine.Object.DestroyImmediate(warm);
            var oldBody=noah.VisualRoot.GetComponent<CharacterBodyGraphic>();var oldVertices=oldBody.canvasRenderer.GetMesh().vertices;var oldUv=oldBody.canvasRenderer.GetMesh().uv;
            var root=noah.transform.parent;int order=noah.transform.GetSiblingIndex();var at=noah.WorldPosition;
            foreach(var actor in field.Actors){actor.SetNoahAuthoredEnabled(true);Assert.That(actor.NoahAuthoredEnabled,Is.EqualTo(actor==noah));}
            var image=Portrait();UnityEngine.Object.DestroyImmediate(image);
            var graphic=noah.GetComponentInChildren<NoahAuthoredGraphic>();Assert.That(graphic,Is.Not.Null);proof.input_sha256=NoahAuthoredGraphic.InputDigest;
            Assert.That(graphic.canvasRenderer.GetMesh().vertexCount,Is.EqualTo(32));Assert.That(graphic.transform.parent,Is.SameAs(noah.VisualRoot));
            Assert.That(noah.transform.parent,Is.SameAs(root));Assert.That(noah.transform.GetSiblingIndex(),Is.EqualTo(order));Assert.That(noah.WorldPosition,Is.EqualTo(at));
            Assert.That(ExecuteEvents.ExecuteHierarchy(graphic.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler),Is.SameAs(noah.gameObject));proof.tap_parent_route_verified=true;field.StartInteraction();
            noah.SetNoahAuthoredEnabled(false);image=Portrait();UnityEngine.Object.DestroyImmediate(image);
            Assert.That(oldBody.enabled,Is.True);Assert.That(oldBody.canvasRenderer.GetMesh().vertices.SequenceEqual(oldVertices),Is.True);Assert.That(oldBody.canvasRenderer.GetMesh().uv.SequenceEqual(oldUv),Is.True);
            noah.SetNoahAuthoredEnabled(true);var directions=new HashSet<NoahAuthoredDirection>();
            int unityFrame=Time.frameCount;
            for(int index=0;index<222;index++)
            {
                if(index==12)Assert.That(field.World.GuidePoint(new TownPoint(-.32f,-.25f)),Is.True);
                if(index==36){field.World.CancelHero();field.StartInteraction();}
                if(index==48)Assert.That(field.World.GuidePoint(new TownPoint(friend.WorldPosition.X-.20f,friend.WorldPosition.Y)),Is.True);
                if(index==96)
                {
                    Assert.That(field.World.Model.Hero.IsWalking,Is.False);field.StartInteraction();social.SetValue(field,0f);
                    Assert.That(field.TryBeginConversation(noah,friend),Is.True);proof.actual_conversation_verified=true;
                }
                if(index==144){field.StartInteraction();Assert.That(field.World.GuidePoint(new TownPoint(.14f,-.08f)),Is.True);}
                if(index==174){field.StartInteraction();Assert.That(field.World.GuidePoint(new TownPoint(.14f,-.30f)),Is.True);}
                if(index==210){field.World.CancelHero();field.StartInteraction();}
                field.AdvanceFrame(1f/30);field.AdvanceFrame(1f/30);field.World.Camera.Jump(new TownPoint(0,.15f));field.World.Refresh();
                directions.Add(noah.AuthoredDirection);
                Assert.That(oldBody.enabled,Is.EqualTo(noah.AuthoredDirection==NoahAuthoredDirection.Front));
                Assert.That(graphic.enabled,Is.EqualTo(noah.AuthoredDirection!=NoahAuthoredDirection.Front));
                if(new[]{11,29,44,50,101,104,158,195}.Contains(index))
                {
                    var pair=CaptureNativePair(output,field,"authored-"+index.ToString("000"),()=>noah.SetNoahAuthoredEnabled(false),()=>noah.SetNoahAuthoredEnabled(true),()=>ActorEvidenceRect(noah),true);
                    pair.mode=noah.AuthoredDirection.ToString();pair.comparison_kind="current-game-vs-independent-parts-at-same-clock";proof.pairs.Add(pair);proof.directional_frames++;
                }
                if(index%3==0)
                {
                    image=Portrait();Texture2D crop=null;
                    try{var rect=EvidenceRect(ActorEvidenceRect(noah));crop=new Texture2D(rect.width,rect.height,TextureFormat.RGB24,false);crop.SetPixels(image.GetPixels(rect.x,rect.y,rect.width,rect.height));crop.Apply();proof.frames.Add(new AuthoredFrame{index=index,simulated_seconds=(index+1)*2f/30,world_x=noah.WorldPosition.X,world_y=noah.WorldPosition.Y,direction=noah.AuthoredDirection.ToString(),image=SaveNativeEncoding(output,"sequence-"+index.ToString("000")+".jpg",crop,crop.EncodeToJPG(90))});}
                    finally{UnityEngine.Object.DestroyImmediate(image);if(crop!=null)UnityEngine.Object.DestroyImmediate(crop);}
                }
                Assert.That(Time.frameCount,Is.EqualTo(unityFrame),"Fixed samples must not be interleaved by an actual Update.");
            }
            foreach(var d in new[]{NoahAuthoredDirection.Left,NoahAuthoredDirection.Right,NoahAuthoredDirection.Back,NoahAuthoredDirection.Front})Assert.That(directions.Contains(d),Is.True,"Real walking must reach "+d);
            proof.observed_directions=directions.Select(d=>d.ToString()).OrderBy(d=>d).ToArray();
            var snapshot=field.World.Capture();
            try
            {
                foreach(var point in new[]{new TownPoint(.36f,.14f),new TownPoint(.36f,.46f),new TownPoint(-.40f,.48f),new TownPoint(-1.2f,-.25f),new TownPoint(1.2f,-.25f)})
                {
                    // Display-only placement checks; the actual walking proof is the sequence above.
                    field.World.Model.Restore("noah",point,null);typeof(TownActor).GetField("authoredDirection",flags).SetValue(noah,NoahAuthoredDirection.Left);
                    field.World.Camera.Jump(point);field.World.Refresh();image=Portrait();UnityEngine.Object.DestroyImmediate(image);
                    int actorOrder=noah.transform.GetSiblingIndex();
                    foreach(var piece in field.GetComponent<TownSceneryView>().Pieces.Where(p=>p.Rect.gameObject.activeSelf))
                    {
                        float delta=noah.WorldPosition.Y-TownSouthMasterLayout.DepthY(piece.Placement);if(Mathf.Abs(delta)<.0001f)continue;
                        Assert.That(actorOrder<piece.Rect.GetSiblingIndex(),Is.EqualTo(delta>0),"Authored parts must inherit foot-Y Sorting: "+piece.Placement.Kind);
                    }
                    var fountain=Flow.GetComponentInChildren<TownFountainGraphic>(true);
                    Assert.That(actorOrder<fountain.transform.GetSiblingIndex(),Is.EqualTo(point.Y>TownCentralMasterLayout.FountainCenterY));
                    Assert.That(graphic.transform.parent,Is.SameAs(noah.VisualRoot));proof.sorting_display_only_examples++;
                }
            }
            finally{field.World.Restore(snapshot);field.RefreshWorldPresentation();}
            Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(saved));Assert.That(Store.Writes,Is.EqualTo(writes));
            Assert.That(proof.pairs.All(p=>p.changed_pixels>0&&p.outside_allowed_pixels==0),Is.True);
            Assert.That(PackedDigest(),Is.EqualTo(packed),"Prepared art/import settings must stay fixed through the native run.");
            File.WriteAllText(Path.Combine(output,"proof.json"),JsonUtility.ToJson(proof,true));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
