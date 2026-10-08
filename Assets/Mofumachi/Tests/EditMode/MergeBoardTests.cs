using Mofumachi.Core;
using NUnit.Framework;

namespace Mofumachi.Tests
{
    public class MergeBoardTests
    {
        [Test] public void EqualItemsBecomeOneHigherLevelAndRemainLockedDuringAnimation()
        {
            var state = GameState.CreateInitial(); var board = new MergeBoard(state);
            Assert.That(board.TryMerge(0, 1), Is.True);
            Assert.That(state.mergeBoard.Count, Is.EqualTo(1)); Assert.That(state.mergeBoard[0].level, Is.EqualTo(2));
            Assert.That(state.mergeBoard[0].cellIndex, Is.EqualTo(1));
            Assert.That(board.IsLocked(0), Is.True); Assert.That(board.IsLocked(1), Is.True);
            Assert.That(board.TryMove(1, 2), Is.False);
            board.ReleaseLocks(); Assert.That(board.TryMove(1, 2), Is.True);
        }
        [TestCase("bread", 1)] [TestCase("tea", 2)] [TestCase("tea", 3)]
        public void DifferentItemsOrLevelsLeaveBoardUnchanged(string id, int level)
        {
            var state = GameState.CreateInitial(); state.mergeBoard[1].itemId = id; state.mergeBoard[1].level = level;
            var before = StateCodec.Encode(state);
            Assert.That(new MergeBoard(state).TryMerge(0, 1), Is.False);
            Assert.That(StateCodec.Encode(state), Is.EqualTo(before));
        }
        [Test] public void MaximumLevelItemsCannotMerge()
        {
            var state = GameState.CreateInitial(); state.mergeBoard.ForEach(i => i.level = 3);
            Assert.That(new MergeBoard(state).TryMerge(0, 1), Is.False); Assert.That(state.mergeBoard.Count, Is.EqualTo(2));
        }
        [TestCase(0, 0)] [TestCase(-1, 1)] [TestCase(0, 30)] [TestCase(0, 3)]
        public void InvalidMergeDoesNotConsumeItems(int from, int to)
        {
            var state = GameState.CreateInitial(); Assert.That(new MergeBoard(state).TryMerge(from, to), Is.False);
            Assert.That(state.mergeBoard.Count, Is.EqualTo(2));
        }
        [Test] public void MoveToEmptyCellPreservesItemAndOccupiedCellRejectsMove()
        {
            var state = GameState.CreateInitial(); var board = new MergeBoard(state);
            Assert.That(board.TryMove(0, 1), Is.False); Assert.That(state.mergeBoard[0].cellIndex, Is.Zero);
            Assert.That(board.TryMove(0, 4), Is.True); Assert.That(state.mergeBoard[0].cellIndex, Is.EqualTo(4));
            Assert.That(state.mergeBoard[0].level, Is.EqualTo(1));
        }
        [Test] public void FullBoardAndInvalidItemsRejectGeneration()
        {
            var state = GameState.CreateInitial(); var board = new MergeBoard(state);
            Assert.That(board.AddItem("", 1), Is.False); Assert.That(board.AddItem("tea", 4), Is.False);
            for (int i = 2; i < 30; i++) Assert.That(board.AddItem("tea", 1), Is.True);
            Assert.That(board.AddItem("tea", 1), Is.False); Assert.That(state.mergeBoard.Count, Is.EqualTo(30));
        }
    }
}
