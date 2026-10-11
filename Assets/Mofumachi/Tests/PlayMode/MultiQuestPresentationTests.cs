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
    public sealed class MultiQuestPresentationTests : SliceUiTestFixture
    {
        [UnityTest] public IEnumerator OldCompletedQuestShowsNextRequestAndActualRewards()
        {
            yield return EnterHome();Flow.AcceptQuest();Flow.DropItem(0,1);yield return new WaitForSecondsRealtime(.3f);
            Assert.That(Flow.Deliver().Success,Is.True);Flow.ShowHome();Flow.ShowQuest();
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Requester").text,Is.EqualTo("モモ"));
            Assert.That(Flow.AcceptQuest(),Is.True);
            Flow.GenerateItem("bread");Flow.GenerateItem("bread");Assert.That(Flow.DropItem(0,1),Is.True);
            yield return new WaitForSecondsRealtime(.3f);Assert.That(Flow.Deliver().Success,Is.True);
            Assert.That(Flow.LastDelivery.RequesterId,Is.EqualTo("momo"));Assert.That(Flow.LastDelivery.CoinsAwarded,Is.EqualTo(40));
            Assert.That(Flow.Game.State.coins,Is.EqualTo(70));Assert.That(Flow.Game.State.townGrowthLevel,Is.EqualTo(2));
            Assert.That(Flow.Deliver().Success,Is.False);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator AllItemKindsUseTheirOwnBoardAndDragArt()
        {
            yield return EnterHome();Flow.ShowMerge();Flow.Game.State.mergeBoard.Clear();
            foreach(var kind in ItemCatalog.All)for(int level=1;level<=3;level++)Flow.Game.AddItem(kind.Id,level);
            Flow.ShowMerge();Canvas.ForceUpdateCanvases();
            var icons=Flow.GetComponentsInChildren<ItemGraphic>();Assert.That(icons.Length,Is.EqualTo(9));
            Assert.That(icons.Select(i=>i.UvRect).Distinct().Count(),Is.EqualTo(9));
            var item=Flow.GetComponentsInChildren<MergeBoardView>()[3];
            item.OnBeginDrag(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
            var ghost=GameObject.Find("Dragged tea").GetComponent<ItemGraphic>();
            Assert.That(ghost.ItemId,Is.EqualTo("bread"));Assert.That(ghost.Level,Is.EqualTo(1));
            Assert.That(Flow.DropItem(0,3),Is.False);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator DiscardCancelFailureAndStaleTargetConsumeNothing()
        {
            yield return EnterHome();Flow.ShowMerge();int count=Flow.Game.State.mergeBoard.Count;
            Flow.OpenDiscardConfirmation(0);Assert.That(Flow.IsModalOpen,Is.True);Flow.GenerateItem("bread");
            Assert.That(Flow.Game.State.mergeBoard.Count,Is.EqualTo(count));Flow.CancelDiscard();
            Flow.OpenDiscardConfirmation(0);Store.Fail=true;Assert.That(Flow.ConfirmDiscard(),Is.False);
            Assert.That(Flow.Game.State.mergeBoard.Count,Is.EqualTo(count));Store.Fail=false;Flow.CancelDiscard();
            Flow.OpenDiscardConfirmation(0);Assert.That(Flow.Game.TryMove(0,2),Is.True);
            Assert.That(Flow.Game.AddItem("flower",1),Is.True);Assert.That(Flow.ConfirmDiscard(),Is.False);
            Assert.That(Flow.Game.Board.At(0).itemId,Is.EqualTo("flower"));Flow.CancelDiscard();
            Flow.OpenDiscardConfirmation(0);Assert.That(Flow.ConfirmDiscard(),Is.True);
            Assert.That(Flow.Game.Board.At(0),Is.Null);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RewardSnapshotSurvivesSettingsAndReselection()
        {
            yield return EnterHome();Flow.AcceptQuest();Flow.DropItem(0,1);yield return new WaitForSecondsRealtime(.3f);
            Assert.That(Flow.Deliver().Success,Is.True);var delivery=Flow.LastDelivery;
            Flow.ShowSettings();Flow.ShowCharacterSelection();Flow.PreviewFavorite("noah");Flow.ConfirmFavorite();Flow.GoBack();
            Assert.That(Flow.LastDelivery,Is.SameAs(delivery));Assert.That(delivery.RequesterId,Is.EqualTo("lulu"));
            Assert.That(delivery.DeliveredItems[0].ItemId,Is.EqualTo("tea"));Assert.That(Flow.Game.State.coins,Is.EqualTo(30));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
