using Mofumachi.Presentation;
using NUnit.Framework;

namespace Mofumachi.Tests
{
    public sealed class MergeFeedbackLayoutTests
    {
        [TestCase(536)] [TestCase(592)] [TestCase(808)]
        public void LiveRequirementsKeepAFortyEightUnitHitHeight(float height)
        {
            var layout=MergeScreenLayout.For(height,46);
            Assert.That(layout.ProgressHeight-layout.DetailsHeight-2,Is.GreaterThanOrEqualTo(48));
            Assert.That(height-layout.TrayTop-layout.TrayBottom-2*layout.GridInset,Is.GreaterThanOrEqualTo(308));
        }
        private const string SelectionAdvice="同じ種類・Lv.を重ねよう";
        [Test] public void CompactAcceptanceAndDeselectionDoNotRepeatTheRequirementCard()
        {
            const string hint="お茶 Lv.2：あと1個。同じ種類・Lv.を合成しよう。";
            Assert.That(MergePresentation.CompactFeedback(hint,hint,SelectionAdvice),Is.Empty);
        }
        [Test] public void CompactGenerationAndFrameChoiceKeepTheResultWithoutRepeatingAdvice()
        {
            foreach(var item in Mofumachi.Core.ItemCatalog.All)
            {
                string created=item.Name+" Lv.1を作りました。",chosen=item.Name+"を作れます。";
                Assert.That(MergePresentation.CompactFeedback(created+SelectionAdvice,"",SelectionAdvice),Is.EqualTo(created));
                Assert.That(MergePresentation.CompactFeedback(chosen+SelectionAdvice,"",SelectionAdvice),Is.EqualTo(chosen));
            }
        }
        [Test] public void CompactFeedbackPreservesFailuresAndOtherMessagesVerbatim()
        {
            foreach(string message in new[]{"セーブできませんでした。保存先を確認してください。","満杯です。\n整理して空きを作ろう。","種類が違います。同じ種類を重ねよう。","キラッ！ アイテムが育ちました。","移動しました。"})
                Assert.That(MergePresentation.CompactFeedback(message,"案内",SelectionAdvice),Is.EqualTo(message));
        }
        [TestCase(536, 41)] [TestCase(536, 61)] [TestCase(592, 41)] [TestCase(640, 61)]
        [TestCase(740, 41)] [TestCase(808, 61)]
        public void FeedbackNeverCoversAnyOfTheThirtyBoardCells(float height,float textHeight)
        {
            var layout=MergeScreenLayout.For(height,textHeight+4);
            var feedback=layout.FeedbackRect(360,height,textHeight);
            var board=new MergeCellRect(18,layout.TrayBottom+layout.GridInset,324,height-layout.TrayTop-layout.TrayBottom-2*layout.GridInset);
            Assert.That(feedback.Overlaps(board),Is.False,$"height={height}, textHeight={textHeight}");
            Assert.That(feedback.Height,Is.GreaterThanOrEqualTo(textHeight));
            Assert.That(feedback.Y,Is.GreaterThanOrEqualTo(0));
            Assert.That(feedback.Y+feedback.Height,Is.LessThanOrEqualTo(height));
            Assert.That(board.Height,Is.GreaterThanOrEqualTo(308));
        }

        [TestCase(536)] [TestCase(592)] [TestCase(640)]
        public void CompactOneLineFeedbackKeepsTheLiveRequirementAreaUncovered(float height)
        {
            var layout=MergeScreenLayout.For(height,46);
            var feedback=layout.FeedbackRect(360,height,20);
            float progressBottom=height-layout.ProgressTop-layout.ProgressHeight;
            var goal=new MergeCellRect(16,progressBottom+22,328,layout.ProgressHeight-24);
            Assert.That(feedback.Y,Is.GreaterThanOrEqualTo(progressBottom));
            Assert.That(feedback.Y+feedback.Height,Is.LessThanOrEqualTo(progressBottom+22));
            Assert.That(feedback.Overlaps(goal),Is.False,"A normal merge/move notice must not hide live delivery counts");
        }

        [Test] public void LongCompactFeedbackUsesTheWholeProgressCardWithoutShrinkingTheBoard()
        {
            var layout=MergeScreenLayout.For(536,65);
            var feedback=layout.FeedbackRect(360,536,61);
            Assert.That(feedback.Height,Is.GreaterThanOrEqualTo(61));
            Assert.That(feedback.Height,Is.LessThanOrEqualTo(layout.ProgressHeight));
            Assert.That(feedback.Y,Is.GreaterThanOrEqualTo(536-layout.ProgressTop-layout.ProgressHeight));
        }
    }
}
