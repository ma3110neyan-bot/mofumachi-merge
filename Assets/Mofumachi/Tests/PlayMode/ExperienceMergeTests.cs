using System.Collections;
using System.Linq;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Mofumachi.Tests
{
    public sealed class ExperienceMergeTests : SliceUiTestFixture
    {
        [UnityTest] public IEnumerator FlatIvoryBoardAndSquareCellsKeepThirtyIndependentTargets()
        {
            yield return EnterHome();Flow.ShowMerge();yield return null;Canvas.ForceUpdateCanvases();
            var frame=Flow.GetComponentsInChildren<PerspectiveBoardGraphic>().Single(g=>g.name=="Merge storybook board");
            Assert.That(frame.mainTexture,Is.SameAs(Texture2D.whiteTexture));Assert.That(frame.raycastTarget,Is.False);
            var panel=frame.rectTransform.rect;var panelVertices=frame.canvasRenderer.GetMesh().vertices;
            Assert.That(panelVertices.Min(v=>v.y),Is.GreaterThanOrEqualTo(panel.yMin-.01f),"The panel has no thick platform below it");
            Assert.That(panelVertices.Max(v=>v.y),Is.LessThanOrEqualTo(panel.yMax+.01f));
            float topWidth=panelVertices.Where(v=>v.y>=panel.yMax-.01f).Max(v=>v.x)-panelVertices.Where(v=>v.y>=panel.yMax-.01f).Min(v=>v.x);
            Assert.That(topWidth,Is.GreaterThan(panel.width*.88f),"Top and bottom edges remain aligned");
            var cells=Flow.GetComponentsInChildren<MergeBoardView>();Assert.That(cells.Length,Is.EqualTo(30));
            Assert.That(cells.All(v=>v.GetComponentInChildren<MergeCellSurfaceGraphic>()!=null),Is.True);
            foreach(var cell in cells)
            {
                var surface=cell.GetComponentInChildren<MergeCellSurfaceGraphic>();var rect=surface.rectTransform.rect;var vertices=surface.canvasRenderer.GetMesh().vertices;
                foreach(float x in new[]{rect.xMin+rect.width*.08f,rect.xMax-rect.width*.08f})
                foreach(float y in new[]{rect.yMin+rect.height*.08f,rect.yMax-rect.height*.08f})
                    Assert.That(vertices.Min(v=>Vector2.Distance(v,new Vector2(x,y))),Is.LessThan(rect.width*.05f),"Rounded squares keep visible corners");
            }
            Assert.That(Flow.GetComponentsInChildren<ItemGraphic>().Where(g=>g.name=="Requirement art tea"||g.name=="Frame item art").All(g=>!g.raycastTarget),Is.True);
            Assert.That(Flow.Game.State.mergeBoard.Count,Is.EqualTo(2));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ItemsUseTheSameSizeInAllSixRows()
        {
            yield return EnterHome();
            Flow.Game.State.mergeBoard.Clear();
            for(int i=0;i<30;i++)Flow.Game.State.mergeBoard.Add(new Mofumachi.Core.BoardItem("tea",1,i));
            Flow.ShowMerge();yield return null;Canvas.ForceUpdateCanvases();
            var cells=Flow.GetComponentsInChildren<MergeBoardView>();
            var first=cells.Single(c=>c.name=="Cell 0").GetComponentInChildren<ItemGraphic>().rectTransform.rect.size;
            foreach(var cell in cells)
            {
                var rect=(RectTransform)cell.transform;
                Assert.That(rect.rect.width,Is.EqualTo(rect.rect.height).Within(.001f));
                Assert.That(cell.GetComponentInChildren<ItemGraphic>().rectTransform.rect.size.x,Is.EqualTo(first.x).Within(.001f));
                Assert.That(cell.GetComponentInChildren<ItemGraphic>().rectTransform.rect.size.y,Is.EqualTo(first.y).Within(.001f));
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CommittedMergeLeavesATactileLandingWithoutExtendingInputLock()
        {
            yield return EnterHome();Flow.ShowMerge();Assert.That(Flow.DropItem(0,1),Is.True);
            yield return new WaitForSecondsRealtime(.27f);Assert.That(Flow.CanInteractBoard,Is.True);
            Assert.That(Flow.Game.Board.At(1).level,Is.EqualTo(2));Assert.That(Flow.Game.State.coins,Is.Zero);Assert.That(Flow.Game.State.claimedRewardIds,Is.Empty);
            Assert.That(Flow.GetComponentsInChildren<Graphic>().Where(g=>g.name=="Merged level label").All(g=>!g.raycastTarget),Is.True);
            Flow.ShowSettings();Flow.GoBack();Assert.That(Flow.GetComponentsInChildren<MergeEffectsView>().Any(e=>e.IsPlaying),Is.False);LogAssert.NoUnexpectedReceived();
        }
    }
}
