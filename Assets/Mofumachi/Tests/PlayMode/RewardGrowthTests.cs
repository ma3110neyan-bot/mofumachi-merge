using System.Collections;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Mofumachi.Tests
{
    public sealed class RewardGrowthTests : SliceUiTestFixture
    {
        private IEnumerator Complete()
        { yield return EnterHome();Assert.That(Flow.AcceptQuest(),Is.True);Assert.That(Flow.DropItem(0,1),Is.True);yield return new WaitForSecondsRealtime(.3f);Assert.That(Flow.Deliver().Success,Is.True); }
        [UnityTest] public IEnumerator DeliveryClicksAndPresentationReentryNeverPayTwice()
        {
            yield return Complete();Assert.That(Flow.LastDelivery.CoinsAwarded,Is.EqualTo(30));Assert.That(Flow.LastDelivery.PreviousTownLevel,Is.EqualTo(0));Assert.That(Flow.LastDelivery.CurrentTownLevel,Is.EqualTo(1));
            Assert.That(Flow.Deliver().Success,Is.False);yield return new WaitForSecondsRealtime(1.3f);Flow.ShowSettings();Flow.GoBack();Flow.ShowGrowth();Flow.ShowSettings();Flow.GoBack();Flow.ShowGrowth();
            Assert.That(Flow.Game.State.coins,Is.EqualTo(30));Assert.That(Flow.Game.State.townGrowthLevel,Is.EqualTo(1));Assert.That(Flow.Game.State.claimedRewardIds.Count,Is.EqualTo(1));Assert.That(Flow.Game.State.completedQuestIds.Count,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator PauseOrTerminateDuringRewardResumesCommittedProgress()
        {
            yield return Complete();Flow.SendMessage("OnApplicationPause",true);yield return SceneManager.LoadSceneAsync("TitleScene");yield return null;Flow.Initialize(Store);
            Assert.That(Flow.Game.State.coins,Is.EqualTo(30));Assert.That(Flow.Game.State.purchaseNoticeAcknowledged,Is.True);
            Flow.BeginGame();yield return null;yield return null;Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));Assert.That(Flow.LastDelivery,Is.Null);
            Assert.That(Flow.Deliver().Success,Is.False);yield return new WaitForSecondsRealtime(1.4f);Assert.That(Flow.Game.State.coins,Is.EqualTo(30));
        }
        [UnityTest] public IEnumerator FailedDeliveryKeepsQuestAndReportsOnlyOnThatScreen()
        {
            yield return EnterHome();Flow.AcceptQuest();Flow.DropItem(0,1);yield return new WaitForSecondsRealtime(.3f);Store.Fail=true;
            Assert.That(Flow.Deliver().SaveFailed,Is.True);Assert.That(Flow.LastDelivery,Is.Null);Assert.That(Flow.Game.State.coins,Is.EqualTo(0));Assert.That(Flow.Game.Board.At(1).level,Is.EqualTo(2));
            Store.Fail=false;Flow.ShowSettings();Assert.That(Flow.FeedbackFor(ScreenId.Settings),Is.Empty);Assert.That(Flow.Deliver().Success,Is.False,"Settings cannot submit a hidden delivery");
        }
    }
}
