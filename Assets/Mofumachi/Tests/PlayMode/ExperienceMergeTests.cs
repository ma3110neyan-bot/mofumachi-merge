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
        [UnityTest] public IEnumerator NewBoardArtworkAndServingDishesKeepThirtyIndependentTargets()
        {
            yield return EnterHome();Flow.ShowMerge();yield return null;Canvas.ForceUpdateCanvases();
            var frame=Flow.GetComponentsInChildren<RawImage>().Single(g=>g.name=="Merge storybook board");
            Assert.That(frame.texture,Is.SameAs(Resources.Load<Texture2D>("Mofumachi/MergeBoardFrame")));Assert.That(frame.raycastTarget,Is.False);
            var cells=Flow.GetComponentsInChildren<MergeBoardView>();Assert.That(cells.Length,Is.EqualTo(30));
            Assert.That(cells.All(v=>v.GetComponentInChildren<MergeCellSurfaceGraphic>()!=null),Is.True);
            Assert.That(Flow.GetComponentsInChildren<ItemGraphic>().Where(g=>g.name=="Requirement art tea"||g.name=="Frame item art").All(g=>!g.raycastTarget),Is.True);
            Assert.That(Flow.Game.State.mergeBoard.Count,Is.EqualTo(2));LogAssert.NoUnexpectedReceived();
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
