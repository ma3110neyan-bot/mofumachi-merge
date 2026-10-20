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
            yield return EnterHome();
            var hint=Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Town growth hint").text;
            Assert.That(hint,Does.Contain("Lv.1"));Assert.That(hint,Does.Contain("お茶会のお花"),"The compact street HUD still promises the actual next growth");
            Flow.ShowQuest();var detail=Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Next goal detail").text;
            Assert.That(detail,Does.Contain("ルル"));Assert.That(detail,Does.Contain("Lv.1"));Assert.That(detail,Does.Contain("お茶会のお花"),"The full quest card retains the requester and growth detail");
            Flow.AcceptQuest();Flow.DropItem(0,1);yield return new WaitForSecondsRealtime(.3f);Assert.That(Flow.Deliver().Success,Is.True);
            yield return new WaitForSecondsRealtime(1.3f);Flow.ShowGrowth();int writes=Store.Writes;
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Next goal detail").text,Does.Contain(TownNextGoal.UnlockAt(2)));
            Assert.That(Flow.Game.State.coins,Is.EqualTo(30));Assert.That(Flow.Game.State.claimedRewardIds.Count,Is.EqualTo(1));Assert.That(Store.Writes,Is.EqualTo(writes));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator AFlowerLightLayerGrowsWithoutInterceptingInputOrSavingProgress()
        {
            yield return EnterHome();int writes=Store.Writes;string before=StateCodec.Encode(Flow.Game.State);var growth=Flow.GetComponentInChildren<TownGrowthView>();
            foreach(int height in new[]{592,740,808})
            {
                var life=FrameTownPortrait(height);
                foreach(TownRegionId region in System.Enum.GetValues(typeof(TownRegionId)))
                {
                    var light=growth.GetComponentInChildren<TownBloomGraphic>();
                    int VisibleAcrossAllDistricts(int level)
                    {
                        growth.Apply(level);var seen=new System.Collections.Generic.HashSet<TownGrowthPropGraphic>();
                        foreach(var district in TownDistrictLayout.All)
                        {
                            Assert.That(life.World.LookAt(district),Is.True);var arrival=life.World.Camera.Center;
                            // A room is taller than the phone's field mask. Survey
                            // its lower/upper gardens too, without moving residents
                            // or requiring offscreen furniture to remain active.
                            foreach(float offset in new[]{0f,-.30f,.30f})
                            {
                                life.World.Camera.Jump(new TownPoint(arrival.X,arrival.Y+offset));life.World.Refresh();
                                foreach(var prop in life.GetComponentsInChildren<TownGrowthPropGraphic>().Where(p=>p.Region==region))
                                {
                                    var rect=(RectTransform)prop.transform;var corners=new Vector3[4];rect.GetWorldCorners(corners);
                                    var field=(RectTransform)life.transform;var min=field.InverseTransformPoint(corners[0]);var max=field.InverseTransformPoint(corners[2]);
                                    Assert.That(new Rect(min.x,min.y,max.x-min.x,max.y-min.y).Overlaps(field.rect),Is.True,
                                        region+" "+prop.Prop+" must intersect the actual field at height "+height);
                                    Assert.That(prop.raycastTarget,Is.False);seen.Add(prop);
                                }
                            }
                        }
                        string missing=string.Join(", ",life.GetComponentsInChildren<TownGrowthPropGraphic>(true)
                            .Where(p=>p.Region==region&&!seen.Contains(p)).Select(p=>p.Prop+" "+p.name));
                        Assert.That(seen.Count,Is.EqualTo(TownGrowthPresentation.For(region,level).Count),
                            region+" stage "+level+" at height "+height+"; unseen: "+missing);
                        return seen.Count;
                    }
                    int small=VisibleAcrossAllDistricts(1);Assert.That(light.PetalCount,Is.Zero);
                    int grown=VisibleAcrossAllDistricts(6);
                    Assert.That(grown-small,Is.GreaterThanOrEqualTo(10),"New decorations remain visible across the five real rooms");Assert.That(light.PetalCount,Is.EqualTo(12));Assert.That(light.raycastTarget,Is.False);
                }
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
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Next goal title").text,Does.Contain("仲間"),"After the story, the invitation returns to the town's friends");
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Next goal detail").text,Does.Contain("会話"),"An incomplete daily conversation remains an actionable next step");
            int writes=Store.Writes;string beforeGoal=StateCodec.Encode(Flow.Game.State);var finale=TownNextGoal.For(Flow.Game.State);
            Assert.That(finale.NextLevel,Is.Zero);Assert.That(finale.RemainingDeliveries,Is.Zero);Assert.That(finale.QuestId,Is.Empty);Assert.That(finale.IsFinale,Is.True);
            Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(beforeGoal));Assert.That(Store.Writes,Is.EqualTo(writes));LogAssert.NoUnexpectedReceived();
        }
    }
}
