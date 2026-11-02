using System;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class MergeRewardPresentationTests
    {
        [Test] public void RewardShowsTheActualCatalogCoinsWithoutCreatingGemRewards()
        {
            foreach(var quest in QuestCatalog.All)Assert.That(MergeRewardPresentation.Coins(quest),Is.EqualTo(quest.Coins+" Coins"));
            Assert.That(MergeRewardPresentation.Coins(null),Is.Empty);
        }
        [Test] public void CountsAreCappedOnlyForTheVisualProgressAndNeverNarrowLongInventory()
        {
            var rows=new[]{new RequirementProgress(new ItemRequirement("tea",2,2),long.MaxValue),new RequirementProgress(new ItemRequirement("bread",2,2),1)};
            Assert.That(MergeRewardPresentation.Fraction(rows),Is.EqualTo(.75).Within(.00001));
            Assert.That(rows[0].Available,Is.EqualTo(long.MaxValue));
            Assert.That(MergeRewardPresentation.Fraction(Array.Empty<RequirementProgress>()),Is.Zero);
            Assert.That(MergeRewardPresentation.Fraction(null),Is.Zero);
        }
        [Test] public void GeneratorAccentFollowsOnlyUnmetRealRequirements()
        {
            var rows=new[]{new RequirementProgress(new ItemRequirement("tea",3,1),1),new RequirementProgress(new ItemRequirement("bread",2,1),0)};
            Assert.That(MergeRewardPresentation.Needs(rows,"tea"),Is.False);
            Assert.That(MergeRewardPresentation.Needs(rows,"bread"),Is.True);
            Assert.That(MergeRewardPresentation.Needs(rows,"flower"),Is.False);
        }
        [TestCase(true,false,false,true)] [TestCase(true,true,false,true)]
        [TestCase(true,true,true,false)] [TestCase(false,false,false,false)]
        public void IncompleteRequestsHaveAReviewRouteAndOnlyCompletedRequestsOfferDelivery(bool hasQuest,bool accepted,bool complete,bool review)
        {Assert.That(MergeRewardPresentation.ReviewBeforeDelivery(hasQuest,accepted,complete),Is.EqualTo(review));}
        [TestCase(536)] [TestCase(592)] [TestCase(640)] [TestCase(700)] [TestCase(740)] [TestCase(808)]
        public void DecorativeCaptionNeverShrinksAnyOfTheThirtyTargetsBelowFortyEight(float height)
        {
            var layout=MergeScreenLayout.For(height,46);float extra=MergeRewardPresentation.CaptionSpace(height,layout);
            Assert.That(height-layout.TrayTop-extra-layout.TrayBottom-layout.GridInset*2,Is.GreaterThanOrEqualTo(308));
            if(height==536)Assert.That(extra,Is.Zero);if(height>=740)Assert.That(extra,Is.EqualTo(14));
        }
        [Test] public void ReadingTheQuestForRewardUiDoesNotChangeBoardInventoryRewardsOrSaveFields()
        {
            var state=GameState.CreateInitial();string before=StateCodec.Encode(state);
            var quest=QuestCatalog.All.First();var rows=new[]{new RequirementProgress(quest.Requirements[0],0)};
            MergeRewardPresentation.Coins(quest);MergeRewardPresentation.Fraction(rows);MergeRewardPresentation.Needs(rows,"tea");MergeRewardPresentation.Task(quest);
            Assert.That(StateCodec.Encode(state),Is.EqualTo(before));
        }
    }
}
