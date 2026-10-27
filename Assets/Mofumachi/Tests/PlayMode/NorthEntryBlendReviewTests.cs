using System.Collections;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Mofumachi.Tests
{
    public sealed class NorthEntryBlendReviewTests:SliceUiTestFixture
    {
        private static void AssertLiveProjection(TownLifeView life,TownNorthGroundGraphic ground,TownNorthEntryBlendGraphic join)
        {
            Canvas.ForceUpdateCanvases();var mesh=join.canvasRenderer.GetMesh();
            Assert.That(mesh,Is.Not.Null);Assert.That(mesh.vertexCount,Is.EqualTo(TownNorthEntryBlendLayout.Vertices.Count));
            var positions=mesh.vertices;var uv=mesh.uv;var colors=mesh.colors32;
            for(int i=0;i<positions.Length;i++)
            {
                var vertex=TownNorthEntryBlendLayout.Vertices[i];
                Assert.That(Vector2.Distance(positions[i],life.World.Local(vertex.World,join.rectTransform)),Is.LessThan(.01f));
                Assert.That(Vector2.Distance(uv[i],new Vector2(vertex.UV.X,vertex.UV.Y)),Is.LessThan(.00001f));
                Assert.That((int)colors[i].a,Is.EqualTo(Mathf.RoundToInt(vertex.Alpha*255)).Within(1));
            }
            Assert.That(ground.canvasRenderer.GetMesh().vertexCount,Is.EqualTo(4),"Keep the original opaque North quad for an exact reconstructed baseline");
            Assert.That(ground.GetComponentInChildren<TownNorthCourtGraphic>().canvasRenderer.GetMesh().vertexCount,Is.EqualTo(4));
        }
        [UnityTest] public IEnumerator JoinFollowsPanAndRestoreAndTogglesWithoutMovingTheSceneOrSaving()
        {
            yield return EnterHome();var life=FrameTownPortrait(640);life.World.Freeze();
            life.World.LookAt(TownDistrictId.North);life.World.Camera.Jump(new TownPoint(0,1));life.World.Refresh();
            var ground=Flow.GetComponentInChildren<TownNorthGroundGraphic>();var join=ground.GetComponentInChildren<TownNorthEntryBlendGraphic>();
            Assert.That(join,Is.Not.Null);Assert.That(join.name,Is.EqualTo("North central road join"));Assert.That(join.raycastTarget,Is.False);
            Assert.That(ground.GetComponentsInChildren<TownNorthEntryBlendGraphic>().Length,Is.EqualTo(1));
            Assert.That(join.mainTexture,Is.SameAs(Resources.Load<Texture2D>(TownCentralMasterLayout.Resource)),"Reuse the accepted painting, without another texture allocation");
            AssertLiveProjection(life,ground,join);var snapshot=life.World.Capture();
            var residents=life.World.Model.Residents.Select(r=>r.Position).ToArray();
            string state=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;
            var water=Flow.GetComponentInChildren<TownFountainWaterGraphic>();float phase=water.Phase,elapsed=water.ShowElapsed;
            var pieces=life.GetComponent<TownSceneryView>().Pieces;var feet=pieces.Select(p=>p.Placement.Feet).ToArray();
            var active=pieces.Select(p=>p.Rect.gameObject.activeSelf).ToArray();
            life.World.Pan(new Vector2(25,-16));AssertLiveProjection(life,ground,join);
            life.World.Restore(snapshot);AssertLiveProjection(life,ground,join);var camera=life.World.Camera.Center;
            join.enabled=false;Canvas.ForceUpdateCanvases();join.enabled=true;Canvas.ForceUpdateCanvases();AssertLiveProjection(life,ground,join);
            Assert.That(life.World.Model.Residents.Select(r=>r.Position),Is.EqualTo(residents));Assert.That(life.World.Camera.Center,Is.EqualTo(camera));
            Assert.That(pieces.Select(p=>p.Placement.Feet),Is.EqualTo(feet));Assert.That(pieces.Select(p=>p.Rect.gameObject.activeSelf),Is.EqualTo(active));
            Assert.That(water.Phase,Is.EqualTo(phase));Assert.That(water.ShowElapsed,Is.EqualTo(elapsed));
            Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(state));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator GrowthPreviewUsesTheSameBoundedJoinAfterItsCenterChanges()
        {
            yield return EnterHome();FrameTownPortrait(640);
            var canvas=Flow.GetComponentInChildren<Canvas>();var node=UIWidgets.Node("Isolated North join preview",(RectTransform)canvas.transform,new Rect(0,0,1,1));
            try
            {
                var ground=node.gameObject.AddComponent<TownNorthGroundGraphic>();var center=new TownPoint(0,1.12f);const float width=1.1f;
                ground.BindPreview(center,width);ground.BindPreview(center,width);Canvas.ForceUpdateCanvases();
                var joins=ground.GetComponentsInChildren<TownNorthEntryBlendGraphic>();Assert.That(joins.Length,Is.EqualTo(1));var join=joins[0];
                Assert.That(join.raycastTarget,Is.False);Assert.That(ground.GetComponentsInChildren<TownNorthCourtGraphic>().Length,Is.EqualTo(1));
                foreach(var next in new[]{center,new TownPoint(.04f,1.20f)})
                {
                    ground.SetPreview(next,width);Canvas.ForceUpdateCanvases();var mesh=join.canvasRenderer.GetMesh();var rect=join.rectTransform.rect;
                    Assert.That(mesh.vertexCount,Is.EqualTo(TownNorthEntryBlendLayout.Vertices.Count));
                    for(int i=0;i<mesh.vertexCount;i++)
                    {
                        var p=TownNorthEntryBlendLayout.Vertices[i].World;
                        var expected=rect.center+new Vector2((p.X-next.X)*rect.width/width,(p.Y-next.Y)*rect.width/width);
                        Assert.That(Vector2.Distance(mesh.vertices[i],expected),Is.LessThan(.01f));
                    }
                }
            }
            finally{Object.DestroyImmediate(node.gameObject);}
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator BothBroadSideCrossingsStillCompleteActualWalkingAtEachReviewLevel()
        {
            yield return EnterHome();
            foreach(int level in new[]{1,3,6})
            {
                Flow.Game.State.townGrowthLevel=level;Flow.ShowHome();var life=FrameTownPortrait(640);
                var join=Flow.GetComponentInChildren<TownNorthEntryBlendGraphic>();Assert.That(join.enabled,Is.True);
                foreach(float side in new[]{-1f,1f})foreach(bool reverse in new[]{false,true})
                {
                    var from=new TownPoint(side*.30f,reverse?1.05f:.95f);var to=new TownPoint(side*.30f,reverse?.95f:1.05f);
                    Assert.That(life.World.Model.Area.CanTravel(from,to),Is.True,"The contact must retain its existing passing width");
                    life.World.Model.Restore(life.World.Model.Hero.Id,from);life.World.Refresh();Assert.That(life.World.GuidePoint(to),Is.True);
                    for(int frame=0;frame<1800&&life.World.Model.Hero.IsWalking;frame++)
                    {life.AdvanceFrame(1f/30);Assert.That(life.World.Model.Area.Contains(life.World.Model.Hero.Position),Is.True);}
                    Assert.That(life.World.Model.Hero.IsWalking,Is.False);Assert.That(TownWalkableArea.Distance(life.World.Model.Hero.Position,to),Is.LessThan(.002f));
                }
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
