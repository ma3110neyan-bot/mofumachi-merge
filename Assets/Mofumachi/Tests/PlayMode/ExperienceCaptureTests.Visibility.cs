using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private static Rect BoundsInRootCanvas(RectTransform rect,Canvas canvas)
        {
            var corners=new Vector3[4];rect.GetWorldCorners(corners);
            var matrix=canvas.rootCanvas.transform.worldToLocalMatrix;
            var points=corners.Select(p=>matrix.MultiplyPoint(p)).ToArray();
            return Rect.MinMaxRect(points.Min(p=>p.x),points.Min(p=>p.y),points.Max(p=>p.x),points.Max(p=>p.y));
        }
        private void AssertPortraitActorGeometry(TownActor actor)
        {
            MaskableGraphic graphic=actor.NoahAuthoredEnabled&&actor.AuthoredDirection!=NoahAuthoredDirection.Front
                ?(MaskableGraphic)actor.GetComponentInChildren<NoahAuthoredGraphic>():actor.GetComponentInChildren<CharacterBodyGraphic>();
            Assert.That(graphic,Is.Not.Null,"Capture must inspect the active character renderer: "+actor.CharacterId);
            Assert.That(graphic.isActiveAndEnabled,Is.True,"Disabled/stale graphics are not visual evidence: "+actor.CharacterId);
            var mask=MaskUtilities.GetRectMaskForClippable(graphic);
            Assert.That(graphic.maskable&&mask!=null,Is.True,"Town actors must retain their native mask: "+actor.CharacterId);
            var masks=new List<RectMask2D>();MaskUtilities.GetRectMasksForClip(mask,masks);
            bool valid;var clip=Clipping.FindCullAndClipWorldRect(masks,out valid);
            // Match RectMask2D's additional screen-space mask-bounds check.
            var mode=graphic.canvas.rootCanvas.renderMode;
            if((mode==RenderMode.ScreenSpaceCamera||mode==RenderMode.ScreenSpaceOverlay)&&!clip.Overlaps(mask.canvasRect,true))valid=false;
            var bounds=BoundsInRootCanvas(graphic.rectTransform,graphic.canvas);
            bool visible=valid&&clip.Overlaps(bounds,true);
            string context=actor.CharacterId+" / "+graphic.name+" bounds="+bounds+" clip="+clip+" valid="+valid+" cull="+graphic.canvasRenderer.cull;
            // RegionVisible includes a prewarm margin. A fully masked graphic
            // can legitimately have no mesh because Graphic.Rebuild skips it.
            // Derive visibility independently: cull alone must never bypass a
            // missing mesh or accidental culling of an on-screen character.
            Assert.That(graphic.canvasRenderer.cull,Is.EqualTo(!visible),context);
            if(!visible)return;
            Assert.That(graphic.canvasRenderer.hasRectClipping,Is.True,context);
            var mesh=graphic.canvasRenderer.GetMesh();Assert.That(mesh,Is.Not.Null,"Visible actor has no mesh: "+context);
            Assert.That(mesh.vertexCount,Is.GreaterThan(0),"Visible actor has empty geometry: "+context);
            foreach(var vertex in mesh.vertices)
            {
                float depth=captureCamera.WorldToScreenPoint(graphic.transform.TransformPoint(vertex)).z;
                Assert.That(depth,Is.GreaterThan(captureCamera.nearClipPlane).And.LessThan(captureCamera.farClipPlane),"Complete dimensional actor must fit the actual capture camera: "+context);
            }
        }

        [UnityTest] public IEnumerator PortraitAcceptsPrewarmedCullingButRejectsVisibleMissingGeometry()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();life.enabled=false;
            var noah=life.Actors.Single(a=>a.CharacterId=="noah");
            var image=Portrait();Object.DestroyImmediate(image);
            var saved=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;
            // Deterministic clipping fixture, not evidence of walking. The
            // normal South spawn has a deliberately narrow camera envelope.
            life.World.Model.Restore("noah",new TownPoint(.36f,.14f),null);
            var at=noah.WorldPosition;var field=(RectTransform)life.transform;
            // Pan around that fixed position without advancing the actor pose.
            life.World.LookAt(TownDistrictId.East);
            float scale=TownWorldLayout.ScaleForViewport(field.rect.width,field.rect.height);
            life.World.Camera.Jump(new TownPoint(at.X+(field.rect.width*.5f+60)/scale,at.Y));
            life.World.Refresh();Canvas.ForceUpdateCanvases();
            Assert.That(noah.RegionVisible,Is.True,"Exercise the real 64-unit prewarm margin.");
            var body=noah.GetComponentInChildren<CharacterBodyGraphic>();
            Assert.That(body.canvasRenderer.cull,Is.True,"The complete body must be outside the native mask.");
            body.canvasRenderer.Clear();Assert.DoesNotThrow(()=>AssertPortraitActorGeometry(noah));
            image=Portrait();Object.DestroyImmediate(image);
            life.World.Camera.Jump(at);life.World.Refresh();Canvas.ForceUpdateCanvases();
            image=Portrait();Object.DestroyImmediate(image);
            Assert.That(body.canvasRenderer.cull,Is.False);AssertPortraitActorGeometry(noah);
            body.canvasRenderer.cull=true;
            Assert.Throws<AssertionException>(()=>AssertPortraitActorGeometry(noah),"Accidental culling of a visible actor must not bypass geometry validation.");
            body.canvasRenderer.cull=false;
            body.canvasRenderer.Clear();
            var missing=Assert.Throws<AssertionException>(()=>AssertPortraitActorGeometry(noah),"Visible missing geometry must still fail capture.");
            Assert.That(missing.Message,Does.Contain("Visible actor has"),"The rejection must specifically detect missing geometry.");
            body.SetVerticesDirty();Canvas.ForceUpdateCanvases();AssertPortraitActorGeometry(noah);
            Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(saved));Assert.That(Store.Writes,Is.EqualTo(writes));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
