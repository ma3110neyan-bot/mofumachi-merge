using System;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class ExperienceGoalTests
    {
        [Test] public void EachDeliveryPromisesTheNextRealGrowthStageWithoutWritingState()
        {
            var state=GameState.CreateInitial();
            for(int stage=0;stage<6;stage++)
            {
                state.townGrowthLevel=stage;state.completedQuestIds=QuestCatalog.All.Take(stage).Select(q=>q.Id).ToList();state.activeQuestId="";
                string before=StateCodec.Encode(state);var goal=TownNextGoal.For(state);
                Assert.That(goal.RemainingDeliveries,Is.EqualTo(1));Assert.That(goal.NextLevel,Is.EqualTo(stage+1));
                Assert.That(goal.Detail,Does.Contain(TownNextGoal.UnlockAt(stage+1)));Assert.That(goal.Title,Does.Contain("あと1"));
                Assert.That(goal.QuestId,Is.EqualTo(QuestCatalog.All[stage].Id));Assert.That(StateCodec.Encode(state),Is.EqualTo(before));
            }
        }
        [Test] public void UnknownActiveQuestNeverPromisesARewardOrFinale()
        {
            var state=GameState.CreateInitial();state.activeQuestId="unrecognized";
            var goal=TownNextGoal.For(state);Assert.That(goal.RemainingDeliveries,Is.Zero);Assert.That(goal.IsFinale,Is.False);Assert.That(goal.Detail,Is.EqualTo("保存された依頼を確認できません。"));
        }
        [Test] public void CompletedStoryOffersReunionsInsteadOfAnInventedSeventhQuest()
        {
            var state=GameState.CreateInitial();state.townGrowthLevel=6;state.completedQuestIds.AddRange(QuestCatalog.All.Select(q=>q.Id));
            var goal=TownNextGoal.For(state);Assert.That(goal.IsFinale,Is.True);Assert.That(goal.NextLevel,Is.Zero);Assert.That(goal.RemainingDeliveries,Is.Zero);Assert.That(goal.QuestId,Is.Empty);Assert.That(goal.Detail,Does.Contain("会いに"));
        }
        [TestCase(20)] [TestCase(int.MaxValue)] public void HighLevelWithRemainingQuestsShowsTheirStoryRatherThanAFalseUnlock(int level)
        {
            var state=GameState.CreateInitial();state.townGrowthLevel=level;
            var goal=TownNextGoal.For(state);Assert.That(goal.NextLevel,Is.Zero);Assert.That(goal.IsFinale,Is.False);Assert.That(goal.QuestId,Is.EqualTo("tea-01"));Assert.That(goal.Title,Does.Contain("つづき"));
        }
        [Test] public void EveryRegionBuildsSeveralClustersAndHasAStrongOneToSixDifference()
        {
            foreach(TownRegionId region in Enum.GetValues(typeof(TownRegionId)))
            {
                int previous=0;
                for(int stage=1;stage<=6;stage++)
                {
                    var props=TownGrowthPresentation.For(region,stage);Assert.That(props.Count,Is.GreaterThan(previous));previous=props.Count;
                    Assert.That(props.All(p=>p.Position.X>TownWorldMap.Left&&p.Position.X<TownWorldMap.Right&&p.Position.Y>TownWorldMap.Bottom&&p.Position.Y<TownWorldMap.Top),Is.True);
                    Assert.That(props.Select(p=>(p.Prop,p.Position.X,p.Position.Y)).Distinct().Count(),Is.EqualTo(props.Count));
                }
                Assert.That(TownGrowthPresentation.For(region,6).Count,Is.GreaterThanOrEqualTo(TownGrowthPresentation.For(region,1).Count+10));
                Assert.That(TownGrowthPresentation.For(region,6).Count,Is.LessThanOrEqualTo(18),"Bound active sprite count on Pixel 3a");
            }
        }
    }
}
