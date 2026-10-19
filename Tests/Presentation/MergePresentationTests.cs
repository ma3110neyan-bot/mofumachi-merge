using System;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class MergePresentationTests
    {
        private static TownDailyService Daily()=>new TownDailyService(new MemoryTownDailyStore(),new DateTime(2026,10,19));
        [Test] public void FinalePurposeUsesActualDailyMergesThenNextAchievableAction()
        {
            var daily=Daily();
            Assert.That(MergePresentation.DailySummary(daily.Data),Is.EqualTo("今日の合成　0 / 3"));
            Assert.That(MergePresentation.DailyPurpose(daily.Data),Does.Contain("あと3回"));
            daily.RecordMerge();daily.RecordMerge();
            Assert.That(MergePresentation.DailySummary(daily.Data),Is.EqualTo("今日の合成　2 / 3"));
            Assert.That(MergePresentation.DailyPurpose(daily.Data),Does.Contain("あと1回"));
            daily.RecordMerge();Assert.That(MergePresentation.DailyPurpose(daily.Data),Does.Contain("話しかけ"));
            daily.RecordTalk("momo");Assert.That(MergePresentation.DailyPurpose(daily.Data),Does.Contain("おさんぽ"));
            daily.RecordWalk();Assert.That(MergePresentation.DailyPurpose(daily.Data),Does.Contain("受け取"));
            daily.Claim();Assert.That(MergePresentation.DailyPurpose(daily.Data),Does.Contain("仲間"));
            Assert.That(MergePresentation.DailyPurpose(daily.Data),Does.Not.Contain("すべての依頼"));
        }
        [Test] public void SelectingAnItemNamesItsRealKindAndLevelWithoutChangingEitherSave()
        {
            var game=GameState.CreateInitial();var daily=Daily();game.mergeBoard[0].itemId="bread";game.mergeBoard[0].level=2;
            string before=StateCodec.Encode(game);var journal=TownDailyCodec.Encode(daily.Data);
            Assert.That(MergePresentation.SelectionName(game.mergeBoard[0]),Is.EqualTo("パン Lv.2"));
            Assert.That(MergePresentation.SelectionHint(game.mergeBoard[0]),Does.Contain("同じ"));
            game.mergeBoard[0].level=3;Assert.That(MergePresentation.SelectionHint(game.mergeBoard[0]),Does.Contain("最高Lv."));
            Assert.That(MergePresentation.SelectionHint(game.mergeBoard[0]),Does.Not.Contain("納品"),"The same item detail is valid after all deliveries have finished");game.mergeBoard[0].level=2;
            Assert.That(MergePresentation.SelectionName(null),Is.EqualTo("アイテムを選ぶ"));
            Assert.That(MergePresentation.SelectionHint(null),Does.Contain("名前"));
            MergePresentation.DailySummary(daily.Data);MergePresentation.DailyPurpose(daily.Data);
            Assert.That(StateCodec.Encode(game),Is.EqualTo(before));Assert.That(TownDailyCodec.Encode(daily.Data),Is.EqualTo(journal));
        }
        [TestCase(536)] [TestCase(584)] [TestCase(592)] [TestCase(640)] [TestCase(700)] [TestCase(740)] [TestCase(808)]
        public void PortraitCompositionReservesThirtyCellsAndSeparateControls(float height)
        {
            var layout=MergeScreenLayout.For(height,46);
            float boardHeight=height-layout.TrayTop-layout.TrayBottom;
            Assert.That(boardHeight-2*layout.GridInset,Is.GreaterThanOrEqualTo(308));
            Assert.That(layout.TrayTop,Is.GreaterThanOrEqualTo(layout.ProgressTop+layout.ProgressHeight+2));
            Assert.That(layout.TrayBottom,Is.GreaterThanOrEqualTo(layout.ToolbarBottom+layout.ToolbarHeight+4));
            Assert.That(layout.ToolbarBottom,Is.GreaterThanOrEqualTo(layout.NavBottom+layout.NavHeight+2));
            Assert.That(layout.ProgressHeight,Is.GreaterThanOrEqualTo(64),"Requirements/purpose and selected name stay visible");
            Assert.That(layout.ToolbarHeight,Is.EqualTo(layout.TwoToolbarRows?100:48));
            if(layout.TwoToolbarRows)Assert.That(layout.ToolbarBottom,Is.GreaterThanOrEqualTo(72+46+4));
        }
        [TestCase(296,536)] [TestCase(336,536)] [TestCase(336,740)] [TestCase(680,808)]
        public void AllFiveToolbarActionsHaveIndependentFortyEightUnitTargets(float width,float height)
        {
            var layout=MergeScreenLayout.For(height,46);var actions=new MergeCellRect[5];
            for(int i=0;i<5;i++)
            {
                var action=actions[i]=layout.ToolbarAction(i,width);
                Assert.That(action.Width,Is.GreaterThanOrEqualTo(48));Assert.That(action.Height,Is.GreaterThanOrEqualTo(48));
                Assert.That(action.X,Is.GreaterThanOrEqualTo(0));Assert.That(action.Y,Is.GreaterThanOrEqualTo(0));
                Assert.That(action.X+action.Width,Is.LessThanOrEqualTo(width+.01f));Assert.That(action.Y+action.Height,Is.LessThanOrEqualTo(layout.ToolbarHeight+.01f));
                if(layout.TwoToolbarRows&&i<3)Assert.That(action.Width,Is.GreaterThanOrEqualTo(96),"Tall screens give each production action readable space");
            }
            for(int i=0;i<5;i++)for(int j=i+1;j<5;j++)Assert.That(actions[i].Overlaps(actions[j]),Is.False);
        }
        [Test] public void GrowingFeedbackSwitchesToCompactBeforeItShrinksTheBoard()
        {
            var layout=MergeScreenLayout.For(740,130);
            Assert.That(740-layout.TrayTop-layout.TrayBottom-2*layout.GridInset,Is.GreaterThanOrEqualTo(308));
            Assert.That(layout.TwoToolbarRows,Is.False);
        }
    }
}
