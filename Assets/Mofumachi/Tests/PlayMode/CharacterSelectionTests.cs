using System.Collections;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mofumachi.Tests
{
    public sealed class CharacterSelectionTests : SliceUiTestFixture
    {
        private IEnumerator FirstSelection()
        {
            yield return LoadTitle(); Flow.BeginGame();
            Assert.That(Flow.ConfirmPurchaseNotice(), Is.True);
            yield return null; yield return null;
            Assert.That(Flow.CurrentScreen, Is.EqualTo(ScreenId.CharacterSelection));
        }
        [UnityTest] public IEnumerator FirstStartSelectsAndSecondStartResumesFavorite()
        {
            yield return FirstSelection(); int writes = Store.Writes;
            Flow.PreviewFavorite("miel"); Flow.PreviewFavorite("not-a-character");
            Assert.That(Flow.PreviewFavoriteId, Is.EqualTo("miel"));
            Assert.That(Store.Writes, Is.EqualTo(writes)); Assert.That(Flow.Game.State.favoriteCharacterId, Is.Empty);
            Assert.That(Flow.ConfirmFavorite(), Is.True);
            Assert.That(Flow.CurrentScreen, Is.EqualTo(ScreenId.Home));
            Assert.That(Store.Load().State.favoriteCharacterId, Is.EqualTo("miel"));
            Assert.That(Flow.ReturnToTitle(), Is.True); yield return null; yield return null;
            Flow.Initialize(Store); Flow.BeginGame(); yield return null; yield return null;
            Assert.That(Flow.CurrentScreen, Is.EqualTo(ScreenId.Home));
            Assert.That(Flow.Game.State.favoriteCharacterId, Is.EqualTo("miel"));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator SelectionSaveFailureAndCancelKeepProgress()
        {
            yield return FirstSelection(); var before = Store.Load().State;
            Store.Fail = true; Flow.PreviewFavorite("noah");
            Assert.That(Flow.ConfirmFavorite(), Is.False);
            Assert.That(Flow.CurrentScreen, Is.EqualTo(ScreenId.CharacterSelection));
            Assert.That(Flow.Game.State.favoriteCharacterId, Is.Empty);
            Assert.That(Flow.AcceptQuest(), Is.False); Assert.That(Flow.DropItem(0, 1), Is.False);
            Flow.CreateTea(); Assert.That(Flow.Game.State.mergeBoard.Count, Is.EqualTo(before.mergeBoard.Count));
            Store.Fail = false; Flow.ShowHome();
            Assert.That(Flow.CurrentScreen, Is.EqualTo(ScreenId.CharacterSelection));
            Flow.GoBack(); yield return null; yield return null;
            Assert.That(Flow.CurrentScreen, Is.EqualTo(ScreenId.Title));
            Assert.That(Store.Load().State.favoriteCharacterId, Is.Empty);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ReselectionReturnsThroughSettingsToItsOrigin()
        {
            yield return EnterHome(); Flow.AcceptQuest(); Flow.DropItem(0, 1);
            yield return new WaitForSecondsRealtime(.3f); Assert.That(Flow.Deliver().Success, Is.True);
            yield return new WaitForSecondsRealtime(1.3f);
            var delivery = Flow.LastDelivery;
            foreach (var origin in new[] { ScreenId.Result, ScreenId.Growth, ScreenId.Quest })
            {
                if (origin == ScreenId.Growth) Flow.ShowGrowth();
                if (origin == ScreenId.Quest) Flow.ShowQuest();
                Flow.ShowSettings(); Flow.ShowCharacterSelection(); Flow.PreviewFavorite("fio");
                Assert.That(Flow.ConfirmFavorite(), Is.True);
                Assert.That(Flow.CurrentScreen, Is.EqualTo(ScreenId.Settings)); Flow.GoBack();
                Assert.That(Flow.CurrentScreen, Is.EqualTo(origin));
                Assert.That(Flow.LastDelivery, Is.SameAs(delivery)); Assert.That(Flow.Game.State.coins, Is.EqualTo(30));
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
