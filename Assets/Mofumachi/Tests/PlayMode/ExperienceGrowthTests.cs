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
    public sealed class ExperienceGrowthTests : SliceUiTestFixture
    {
        [UnityTest] public IEnumerator NextUnlockIsVisibleAndUpdatesFromTheExistingSuccessfulDelivery()
        {
            yield return EnterHome();var goal=TownNextGoal.For(Flow.Game.State);
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Town growth hint").text,Is.EqualTo(goal.Detail));
            Flow.AcceptQuest();Flow.DropItem(0,1);yield return new WaitForSecondsRealtime(.3f);Assert.That(Flow.Deliver().Success,Is.True);
            yield return new WaitForSecondsRealtime(1.3f);Flow.ShowGrowth();int writes=Store.Writes;
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Next goal detail").text,Does.Contain(TownNextGoal.UnlockAt(2)));
            Assert.That(Flow.Game.State.coins,Is.EqualTo(30));Assert.That(Flow.Game.State.claimedRewardIds.Count,Is.EqualTo(1));Assert.That(Store.Writes,Is.EqualTo(writes));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator AFlowerLightLayerGrowsWithoutInterceptingInputOrSavingProgress()
        {
            yield return EnterHome();int writes=Store.Writes;string before=StateCodec.Encode(Flow.Game.State);var growth=Flow.GetComponentInChildren<TownGrowthView>();
            foreach(TownRegionId region in System.Enum.GetValues(typeof(TownRegionId)))
            {
                growth.SetRegion(region);growth.Apply(1);var light=growth.GetComponentInChildren<TownBloomGraphic>();Assert.That(light.PetalCount,Is.Zero);
                int small=growth.GetComponentsInChildren<TownGrowthPropGraphic>().Length;growth.Apply(6);
                Assert.That(growth.GetComponentsInChildren<TownGrowthPropGraphic>().Length-small,Is.GreaterThanOrEqualTo(10));Assert.That(light.PetalCount,Is.EqualTo(12));Assert.That(light.raycastTarget,Is.False);
            }
            Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));Assert.That(Store.Writes,Is.EqualTo(writes));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator UnknownQuestAndFinaleNeverInventAnotherUnlock()
        {
            yield return EnterHome();Flow.Game.State.activeQuestId="unknown-quest";Flow.ShowQuest();int writes=Store.Writes;
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Next goal detail").text,Is.EqualTo(UIStrings.UnknownQuest));
            Flow.Game.State.activeQuestId="";Flow.Game.State.completedQuestIds.AddRange(QuestCatalog.All.Select(q=>q.Id));Flow.Game.State.townGrowthLevel=6;Flow.ShowQuest();
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Next goal title").text,Does.Contain("お茶会"));
            Assert.That(TownNextGoal.For(Flow.Game.State).NextLevel,Is.Zero);Assert.That(Store.Writes,Is.EqualTo(writes));LogAssert.NoUnexpectedReceived();
        }
    }
}
