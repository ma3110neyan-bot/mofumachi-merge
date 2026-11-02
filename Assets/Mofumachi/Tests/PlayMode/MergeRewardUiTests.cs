using System.Collections;
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
    public sealed class MergeRewardUiTests : SliceUiTestFixture
    {
        [UnityTest] public IEnumerator CurrentQuestUsesOfficialArtAndActualCoinsWithoutSaving()
        {
            yield return EnterHome();Flow.ShowMerge();yield return null;Canvas.ForceUpdateCanvases();
            int writes=Store.Writes;string state=StateCodec.Encode(Flow.Game.State);
            var page=Flow.GetComponentInChildren<MergeRewardView>();Assert.That(page,Is.Not.Null);
            Assert.That(page.GetComponentsInChildren<Text>().Single(t=>t.name=="Current request caption").text,Is.EqualTo("現在の依頼"));
            Assert.That(page.GetComponentsInChildren<Text>(true).Single(t=>t.name=="Reward amount").text,Is.EqualTo(QuestDefinition.First.Coins+" Coins"));
            Assert.That(page.GetComponentsInChildren<ItemGraphic>().Single(a=>a.name=="Requirement art tea").mainTexture,Is.SameAs(Resources.Load<Texture2D>("Mofumachi/ItemAtlas")));
            Assert.That(page.GetComponentsInChildren<Button>().Single(b=>b.name=="Generate frame tea").GetComponent<RoundedPanelGraphic>().color,Is.EqualTo(MergeRewardView.Rose));
            Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(state));Assert.That(Store.Writes,Is.EqualTo(writes));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator AcceptedIncompleteRequestStillHasALargeReviewAction()
        {
            yield return EnterHome();Flow.ShowQuest();Assert.That(Flow.AcceptQuest(),Is.True);Flow.ShowMerge();yield return null;
            var review=Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Deliver");
            Assert.That(review.interactable,Is.True);Assert.That(review.GetComponentInChildren<Text>().text,Is.EqualTo("依頼を見る"));
            int coins=Flow.Game.State.coins,count=Flow.Game.State.mergeBoard.Count;
            review.onClick.Invoke();yield return null;Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Quest));
            Assert.That(Flow.Game.State.coins,Is.EqualTo(coins));Assert.That(Flow.Game.State.mergeBoard.Count,Is.EqualTo(count));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CompletedRequestUsesExistingDeliveryAndExistingRewardOnly()
        {
            yield return EnterHome();Flow.ShowQuest();Assert.That(Flow.AcceptQuest(),Is.True);Flow.ShowMerge();
            Assert.That(Flow.DropItem(0,1),Is.True);yield return new WaitForSecondsRealtime(.3f);
            var deliver=Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Deliver");Assert.That(deliver.interactable,Is.True);
            Assert.That(deliver.GetComponentInChildren<Text>().text,Is.EqualTo("納品する"));deliver.onClick.Invoke();yield return null;
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Result));Assert.That(Flow.Game.State.coins,Is.EqualTo(QuestDefinition.First.Coins));
            Assert.That(Flow.Game.State.claimedRewardIds.Count(x=>x=="quest:"+QuestDefinition.First.Id),Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator EmptyMotifsNeverCoverSavedItemsOrReceiveInput()
        {
            yield return EnterHome();Flow.ShowMerge();yield return null;Canvas.ForceUpdateCanvases();
            var cells=Flow.GetComponentsInChildren<MergeBoardView>();Assert.That(cells.Length,Is.EqualTo(30));
            var motif=typeof(MergeCellSurfaceGraphic).GetField("motif",BindingFlags.NonPublic|BindingFlags.Instance);
            foreach(var cell in cells)
            {
                int index=int.Parse(cell.name.Substring(5));var paper=cell.GetComponentInChildren<MergeCellSurfaceGraphic>();
                Assert.That(paper.raycastTarget,Is.False);Assert.That((int)motif.GetValue(paper),Is.EqualTo(Flow.Game.Board.At(index)==null?index:-1));
                Assert.That(paper.canvasRenderer.GetMesh().vertexCount,Is.LessThan(400),"Bounded lightweight cell decoration");
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
