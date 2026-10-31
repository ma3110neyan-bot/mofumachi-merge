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
        [Serializable] private sealed class SixInput{public string characterId=null,input_sha256=null;}
        [Serializable] private sealed class SixGeometry
        {
            public string character;public float foot_anchor_screen_y,mesh_bottom_local,mesh_height_local,scale;
            public int body_vertices,shadow_vertices;public Rect shadow_anchors;
        }
        [Serializable] private sealed class SixProof
        {
            public string gitRevision=null,productFingerprint=null,packed_resource_sha256=null;
            public SixInput[] inputs=null;public bool native_unity=true,all_six_enabled,save_unchanged,tap_verified,pixel_verified=false,adopted=false;
            public string placements="isolated six-character display fixture; individual walking is in separate native methods; not Pixel FPS";
            public bool front_verified,geometry_verified;
            public string geometry_kind="rendered mesh bounds and anchors; opaque contact/silhouette still require visual review";
            public List<SixGeometry> geometry=new List<SixGeometry>();
            public List<NativeImage> images=new List<NativeImage>();
        }
        private void VerifyAuthoredScreenInput(TownLifeView field,TownActor actor,UnityEngine.UI.Graphic graphic)
        {
            var street=field.GetComponent<TownStreetWalkView>();street.CancelSelection();field.StartInteraction();
            Vector2 point=RectTransformUtility.WorldToScreenPoint(captureCamera,graphic.rectTransform.TransformPoint(graphic.rectTransform.rect.center));
            var pointer=new PointerEventData(EventSystem.current){pointerId=-1,button=PointerEventData.InputButton.Left,position=point};
            List<RaycastResult> Hits(){var results=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,results);return results;}
            void Blocked(){Assert.That(Hits().Any(h=>h.gameObject.GetComponentInParent<TownActor>()==actor),Is.False,"Blocked or masked candidate must not receive input.");}
            var group=actor.GetComponent<CanvasGroup>();bool block=group.blocksRaycasts;
            try{group.blocksRaycasts=false;Blocked();}finally{group.blocksRaycasts=block;}
            bool raycast=graphic.raycastTarget;
            try{graphic.raycastTarget=false;Blocked();}finally{graphic.raycastTarget=raycast;}
            var rect=graphic.rectTransform;var parent=rect.parent;int sibling=rect.GetSiblingIndex();
            var min=rect.anchorMin;var max=rect.anchorMax;var pivot=rect.pivot;var size=rect.sizeDelta;var position=rect.anchoredPosition;var scale=rect.localScale;var rotation=rect.localRotation;
            var maskObject=new GameObject("Isolated input mask",typeof(RectTransform),typeof(UnityEngine.UI.RectMask2D));
            try
            {
                var mask=(RectTransform)maskObject.transform;mask.SetParent(parent,false);mask.anchorMin=mask.anchorMax=Vector2.zero;mask.sizeDelta=Vector2.one;mask.anchoredPosition=new Vector2(10000,10000);
                rect.SetParent(mask,true);Canvas.ForceUpdateCanvases();maskObject.GetComponent<UnityEngine.UI.RectMask2D>().PerformClipping();Blocked();
            }
            finally
            {
                rect.SetParent(parent,false);rect.SetSiblingIndex(sibling);rect.anchorMin=min;rect.anchorMax=max;rect.pivot=pivot;rect.sizeDelta=size;rect.anchoredPosition=position;rect.localScale=scale;rect.localRotation=rotation;
                UnityEngine.Object.DestroyImmediate(maskObject);Canvas.ForceUpdateCanvases();
            }
            var hits=Hits();Assert.That(hits,Is.Not.Empty);Assert.That(hits[0].gameObject.GetComponentInParent<TownActor>(),Is.SameAs(actor),"The foremost screen hit must be the candidate.");
            pointer.pointerPressRaycast=hits[0];pointer.pointerCurrentRaycast=hits[0];
            Assert.That(ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerDownHandler),Is.SameAs(actor.gameObject));Assert.That(actor.Movement.OwnedPointerId,Is.EqualTo(-1));
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerUpHandler);Assert.That(actor.Movement.OwnedPointerId,Is.Null);
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);Assert.That(street.IsArmed,Is.True);street.CancelSelection();
        }
        [UnityTest,Timeout(120000)] public IEnumerator SixAuthoredCharactersPreserveWorldAndInput()
        {
            if(!Environment.GetCommandLineArgs().Contains("-mofumachiCharactersSixPreview"))yield break;
            string run=EvidenceArgument("-mofumachiCharactersDirectory");
            var proof=JsonUtility.FromJson<SixProof>(File.ReadAllText(Path.Combine(run,"prepared-six.json")));
            proof.images=new List<NativeImage>();
            Assert.That(proof.gitRevision,Is.EqualTo(EvidenceArgument("-mofumachiSourceRevision")));
            Assert.That(proof.productFingerprint,Is.EqualTo(EvidenceArgument("-mofumachiProductFingerprint")));
            Assert.That(proof.inputs.Select(p=>p.characterId),Is.EquivalentTo(new[]{"momo","lulu","pom","fio","miel","noah"}));
            yield return EnterHome();Assert.That(Flow.Game.SelectFavorite("momo"),Is.True);Flow.ShowHome();yield return null;
            EnsurePortraitCamera();var field=Flow.GetComponentInChildren<TownLifeView>();field.StartInteraction();
            string saved=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(TownLifeView).GetField("nextSocial",flags).SetValue(field,float.PositiveInfinity);
            string output=Path.Combine(OutputDirectory(),"six-authored");Directory.CreateDirectory(output);
            var actors=field.Actors.OrderBy(a=>Array.IndexOf(new[]{"momo","lulu","pom","fio","miel","noah"},a.CharacterId)).ToArray();
            Assert.That(actors.Length,Is.EqualTo(6));
            for(int i=0;i<actors.Length;i++)
            {
                var actor=actors[i];field.World.Model.Restore(actor.CharacterId,new TownPoint(-.30f+(i%3)*.30f,-.12f-(i/3)*.26f),null);
                if(actor.CharacterId=="noah")actor.SetNoahAuthoredEnabled(true);else Assert.That(actor.SetCharacterAuthoredEnabled(true),Is.True);
            }
            field.World.Camera.Jump(new TownPoint(0,.12f));field.World.Refresh();
            foreach(var direction in new[]{NoahAuthoredDirection.Front,NoahAuthoredDirection.Left,NoahAuthoredDirection.Right,NoahAuthoredDirection.Back})
            {
                foreach(var actor in actors)typeof(TownActor).GetField(actor.CharacterId=="noah"?"authoredDirection":"characterDirection",flags).SetValue(actor,direction);
                field.RefreshWorldPresentation();var image=Portrait();
                try{proof.images.Add(SaveNativeImage(output,"six-"+direction.ToString().ToLowerInvariant()+".png",image));}
                finally{UnityEngine.Object.DestroyImmediate(image);}
                foreach(var actor in actors)
                {
                    var parts=actor.CharacterId=="noah"?(UnityEngine.UI.Graphic)actor.GetComponentInChildren<NoahAuthoredGraphic>():actor.GetComponentInChildren<CharacterAuthoredGraphic>();
                    bool front=direction==NoahAuthoredDirection.Front;
                    var graphic=front?(UnityEngine.UI.Graphic)actor.VisualRoot.GetComponent<CharacterBodyGraphic>():parts;
                    Assert.That(graphic,Is.Not.Null);Assert.That(graphic.enabled,Is.True);
                    Assert.That(parts.enabled,Is.EqualTo(!front));
                    if(!front){Assert.That(graphic.transform.parent,Is.SameAs(actor.VisualRoot));Assert.That(graphic.canvasRenderer.GetMesh().vertexCount,Is.EqualTo(actor.CharacterId=="noah"?32:28));}
                    else
                    {
                        var mesh=graphic.canvasRenderer.GetMesh();Assert.That(mesh,Is.Not.Null);Assert.That(mesh.vertexCount,Is.GreaterThan(0));
                        var vertices=mesh.vertices.Select(v=>actor.VisualRoot.InverseTransformPoint(graphic.transform.TransformPoint(v))).ToArray();
                        var shadow=actor.transform.Find(actor.CharacterId=="noah"?"Noah soft ground shadow":"Character authored ground shadow").GetComponent<UnityEngine.UI.Graphic>();
                        Assert.That(shadow.enabled,Is.True);Assert.That(shadow.raycastTarget,Is.False);Assert.That(shadow.canvasRenderer.GetMesh().vertexCount,Is.EqualTo(97));
                        proof.geometry.Add(new SixGeometry{character=actor.CharacterId,foot_anchor_screen_y=captureCamera.WorldToScreenPoint(actor.VisualRoot.position).y,
                            mesh_bottom_local=vertices.Min(v=>v.y),mesh_height_local=vertices.Max(v=>v.y)-vertices.Min(v=>v.y),scale=actor.VisualRoot.localScale.y,
                            body_vertices=mesh.vertexCount,shadow_vertices=97,shadow_anchors=Rect.MinMaxRect(shadow.rectTransform.anchorMin.x,shadow.rectTransform.anchorMin.y,shadow.rectTransform.anchorMax.x,shadow.rectTransform.anchorMax.y)});
                    }
                    string digest=actor.CharacterId=="noah"?NoahAuthoredGraphic.InputDigest:((CharacterAuthoredGraphic)parts).InputDigest;
                    Assert.That(digest,Is.EqualTo(proof.inputs.Single(p=>p.characterId==actor.CharacterId).input_sha256));
                    Assert.That(actor.NoahAuthoredEnabled||actor.CharacterAuthoredEnabled,Is.True);
                    Assert.That(actor.RegionVisible,Is.True);
                    var corners=new Vector3[4];((RectTransform)actor.VisualRoot).GetWorldCorners(corners);
                    foreach(var corner in corners){var p=captureCamera.WorldToViewportPoint(corner);Assert.That(p.x,Is.InRange(0f,1f));Assert.That(p.y,Is.InRange(0f,1f));}
                }
            }
            Assert.That(proof.geometry.Count,Is.EqualTo(6));
            Assert.That(proof.geometry.Max(g=>g.mesh_height_local)/proof.geometry.Min(g=>g.mesh_height_local),Is.LessThan(1.15f),"Front display heights must remain comparable without erasing individual design.");
            foreach(var g in proof.geometry)Assert.That(Math.Abs(g.mesh_bottom_local),Is.LessThan(.5f),g.character+": front foot anchor");
            for(int row=0;row<2;row++){var line=proof.geometry.Skip(row*3).Take(3);Assert.That(line.Max(g=>g.foot_anchor_screen_y)-line.Min(g=>g.foot_anchor_screen_y),Is.LessThan(1f),"Equal-world-Y actors must share a foot line.");}
            proof.front_verified=true;proof.geometry_verified=true;
            var hero=actors.Single(a=>a.Controlled);var target=hero.GetComponentInChildren<CharacterAuthoredGraphic>();
            VerifyAuthoredScreenInput(field,hero,target);proof.tap_verified=true;
            Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(saved));Assert.That(Store.Writes,Is.EqualTo(writes));
            proof.native_unity=true;proof.all_six_enabled=true;proof.save_unchanged=true;
            File.WriteAllText(Path.Combine(output,"six-proof.json"),JsonUtility.ToJson(proof,true));LogAssert.NoUnexpectedReceived();
        }
    }
}
