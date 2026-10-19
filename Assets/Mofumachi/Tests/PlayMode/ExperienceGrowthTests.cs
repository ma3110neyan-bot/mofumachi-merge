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
            var life=FrameTownPortrait();
            foreach(TownRegionId region in System.Enum.GetValues(typeof(TownRegionId)))
            {
                var light=growth.GetComponentInChildren<TownBloomGraphic>();
                int VisibleAcrossAllDistricts(int level)
                {
                    growth.Apply(level);var seen=new System.Collections.Generic.HashSet<TownGrowthPropGraphic>();
                    foreach(var district in TownDistrictLayout.All)
                    {
                        Assert.That(life.World.LookAt(district),Is.True);life.World.Refresh();
                        foreach(var prop in life.GetComponentsInChildren<TownGrowthPropGraphic>().Where(p=>p.Region==region))seen.Add(prop);
                    }
                    return seen.Count;
                }
                int small=VisibleAcrossAllDistricts(1);Assert.That(small,Is.EqualTo(TownGrowthPresentation.For(region,1).Count));Assert.That(light.PetalCount,Is.Zero);
                int grown=VisibleAcrossAllDistricts(6);Assert.That(grown,Is.EqualTo(TownGrowthPresentation.For(region,6).Count));
                Assert.That(grown-small,Is.GreaterThanOrEqualTo(10),"New decorations remain visible across the five real rooms");Assert.That(light.PetalCount,Is.EqualTo(12));Assert.That(light.raycastTarget,Is.False);
            }
            Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));Assert.That(Store.Writes,Is.EqualTo(writes));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator UnknownQuestAndFinaleNeverInventAnotherUnlock()
        {
            yield return EnterHome();
            void OpenQuestWithoutChangingProgress()
            {
                var before=Flow.Game.State.Clone();int writes=Store.Writes;
                Flow.ShowQuest();
                Assert.That(Store.Writes,Is.EqualTo(writes+1),"Preserve exactly one existing navigation save; goal rendering must not save again");
                var after=Flow.Game.State.Clone();
                Assert.That(System.DateTimeOffset.TryParse(after.lastSaveTime,out _),Is.True,"Navigation keeps valid save metadata");
                Assert.That(StateCodec.Encode(Store.Load().State),Is.EqualTo(StateCodec.Encode(after)),"The saved snapshot must match the live state");
                after.lastSaveTime=before.lastSaveTime;
                Assert.That(StateCodec.Encode(after),Is.EqualTo(StateCodec.Encode(before)),"Quest preview must not change progress, rewards, or the board");
            }
            Flow.Game.State.activeQuestId="unknown-quest";OpenQuestWithoutChangingProgress();
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Next goal detail").text,Is.EqualTo(UIStrings.UnknownQuest));
            var unknown=TownNextGoal.For(Flow.Game.State);Assert.That(unknown.NextLevel,Is.Zero);Assert.That(unknown.IsFinale,Is.False);Assert.That(unknown.QuestId,Is.Empty);
            Flow.Game.State.activeQuestId="";Flow.Game.State.completedQuestIds.AddRange(QuestCatalog.All.Select(q=>q.Id));Flow.Game.State.townGrowthLevel=6;OpenQuestWithoutChangingProgress();
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Next goal title").text,Does.Contain("お茶会"));
            int writes=Store.Writes;string beforeGoal=StateCodec.Encode(Flow.Game.State);var finale=TownNextGoal.For(Flow.Game.State);
            Assert.That(finale.NextLevel,Is.Zero);Assert.That(finale.RemainingDeliveries,Is.Zero);Assert.That(finale.QuestId,Is.Empty);Assert.That(finale.IsFinale,Is.True);
            Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(beforeGoal));Assert.That(Store.Writes,Is.EqualTo(writes));LogAssert.NoUnexpectedReceived();
        }
    }
}
