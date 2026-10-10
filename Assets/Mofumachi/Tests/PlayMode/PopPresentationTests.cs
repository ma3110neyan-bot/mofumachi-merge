using System.Collections;
using System.Linq;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Mofumachi.Tests
{
    public sealed class PopPresentationTests : SliceUiTestFixture
    {
        [UnityTest] public IEnumerator CharacterReactionDoesNotWriteOrChangeGameProgress()
        {
            yield return EnterHome();
            var character = Flow.GetComponentsInChildren<CharacterReaction>().First();
            int writes = Store.Writes, coins = Flow.Game.State.coins;
            var start = character.transform.localScale;
            character.GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(.12f);
            Assert.That(character.transform.localScale, Is.Not.EqualTo(start));
            Assert.That(Flow.CurrentScreen, Is.EqualTo(ScreenId.Home));
            Assert.That(Flow.Game.State.coins, Is.EqualTo(coins));
            Assert.That(Store.Writes, Is.EqualTo(writes));
            Flow.SendMessage("OnApplicationPause", true);
            Assert.That(Flow.GetComponentsInChildren<CharacterReaction>().All(r => !r.enabled), Is.True);
            Assert.That(Flow.GetComponentsInChildren<TownAmbientGraphic>().All(r => !r.enabled), Is.True);
            Flow.SendMessage("OnApplicationPause", false);
            Assert.That(Flow.GetComponentsInChildren<CharacterReaction>().All(r => r.enabled), Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator SuccessfulDeliveryClearsOldMergeMessageAndFailureKeepsReason()
        {
            yield return EnterHome();
            Assert.That(Flow.AcceptQuest(), Is.True);
            Assert.That(Flow.DropItem(0, 1), Is.True);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(Flow.FeedbackFor(ScreenId.Merge), Is.Not.Empty);
            Store.Fail = true;
            Assert.That(Flow.Deliver().Success, Is.False);
            Assert.That(Flow.FeedbackFor(ScreenId.Merge), Is.Not.Empty);
            Assert.That(Flow.Game.State.coins, Is.EqualTo(0));
            Store.Fail = false;
            Assert.That(Flow.Deliver().Success, Is.True);
            Flow.ShowMerge();
            Assert.That(Flow.FeedbackFor(ScreenId.Merge), Is.Empty);
            Assert.That(Flow.Game.State.coins, Is.EqualTo(30));
            Assert.That(Flow.Game.State.mergeBoard, Is.Empty);
        }
    }
}
