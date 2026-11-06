using System;
using System.IO;
using System.Linq;
using Mofumachi.Core;
using NUnit.Framework;

namespace Mofumachi.Tests
{
    public sealed class GenerationCostTests
    {
        private GameState state;
        private GameStateManager game;
        private GenerationStore store;
        [SetUp] public void SetUp()
        {
            state = GameState.CreateInitial(); store = new GenerationStore();
            game = new GameStateManager(state, store);
        }

        [TestCase("tea")] [TestCase("bread")] [TestCase("flower")]
        public void UserGenerationAddsExactlyOneLv1AndChargesTwoOnce(string id)
        {
            int count = state.mergeBoard.Count;
            Assert.That(game.TryGenerateLv1(id), Is.True);
            Assert.That(game.MofuPoints.Balance, Is.EqualTo(22));
            Assert.That(state.mergeBoard.Count, Is.EqualTo(count + 1));
            Assert.That(game.Board.At(2).itemId, Is.EqualTo(id)); Assert.That(game.Board.At(2).level, Is.EqualTo(1));
            Assert.That(store.Attempts, Is.EqualTo(1)); Assert.That(store.Saved.economy.mofuPoints, Is.EqualTo(22));
            Assert.That(store.Saved.mergeBoard.Count, Is.EqualTo(3));
        }

        [Test] public void ThreeIndependentOperationsChargeTwoPerItem()
        {
            string[] ids = { "tea", "bread", "flower" };
            for (int i = 0; i < 3; i++)
            {
                Assert.That(game.TryGenerateLv1(ids[i]), Is.True);
                Assert.That(game.MofuPoints.Balance, Is.EqualTo(22 - i * 2));
                Assert.That(store.Attempts, Is.EqualTo(i + 1)); Assert.That(state.mergeBoard.Count, Is.EqualTo(i + 3));
            }
        }

        [Test] public void ExistingSpendApiUsesTheSamePrimitiveAndSavesOnlyValidSpending()
        {
            Assert.That(game.MofuPoints.TrySpend(2), Is.True);
            Assert.That(game.MofuPoints.TrySpend(-1), Is.False);
            Assert.That(game.MofuPoints.TrySpend(23), Is.False);
            Assert.That(game.MofuPoints.Balance, Is.EqualTo(22)); Assert.That(store.Attempts, Is.EqualTo(1));
            Assert.That(state.mergeBoard.Count, Is.EqualTo(2));
        }

        [TestCase(2, true, 0)] [TestCase(1, false, 1)] [TestCase(0, false, 0)]
        public void BoundaryBalanceNeverOverdrawsOrSavesInsufficientRequest(int balance, bool success, int expected)
        {
            state.economy.mofuPoints = balance; string before = StateCodec.Encode(state);
            Assert.That(game.TryGenerateLv1("tea"), Is.EqualTo(success));
            Assert.That(game.MofuPoints.Balance, Is.EqualTo(expected)); Assert.That(store.Attempts, Is.EqualTo(success ? 1 : 0));
            Assert.That(state.mergeBoard.Count, Is.EqualTo(success ? 3 : 2));
            if (!success)
            {
                Assert.That(StateCodec.Encode(state), Is.EqualTo(before));
                Assert.That(game.LastError, Does.Contain("もふポイント"));
            }
        }

        [Test] public void FullBoardDoesNotConsumeOrSave()
        {
            state.mergeBoard.Clear();
            for (int i = 0; i < 30; i++) state.mergeBoard.Add(new BoardItem("tea", 1, i));
            string before = StateCodec.Encode(state);
            Assert.That(game.TryGenerateLv1("bread"), Is.False);
            Assert.That(StateCodec.Encode(state), Is.EqualTo(before)); Assert.That(store.Attempts, Is.Zero);
        }

        [Test] public void LockedVacancyIsNotAUsableSpaceAndNeverCostsPoints()
        {
            Assert.That(game.TryMerge(0, 1), Is.True);
            for (int i = 2; i < 30; i++) state.mergeBoard.Add(new BoardItem("tea", 3, i));
            string before = StateCodec.Encode(state); int writes = store.Attempts;
            Assert.That(state.mergeBoard.Count, Is.EqualTo(29)); Assert.That(game.Board.IsLocked(0), Is.True);
            Assert.That(game.TryGenerateLv1("flower"), Is.False);
            Assert.That(StateCodec.Encode(state), Is.EqualTo(before)); Assert.That(store.Attempts, Is.EqualTo(writes));
        }

        [TestCase(null)] [TestCase("")] [TestCase("future-item")]
        public void UnknownGenerationKindIsRejectedWithoutSave(string id)
        {
            string before = StateCodec.Encode(state);
            Assert.That(game.TryGenerateLv1(id), Is.False); Assert.That(store.Attempts, Is.Zero);
            Assert.That(StateCodec.Encode(state), Is.EqualTo(before));
        }

        [TestCase("tea")] [TestCase("bread")] [TestCase("flower")]
        public void PackedFrameCannotGenerateOrCharge(string id)
        {
            state.productionFrameIds.Remove(id); string before = StateCodec.Encode(state);
            Assert.That(game.TryGenerateLv1(id), Is.False); Assert.That(store.Attempts, Is.Zero);
            Assert.That(StateCodec.Encode(state), Is.EqualTo(before));
        }

        [Test] public void InitialTwoTeaAreFreeAndNewBalanceRemains24()
        {
            Assert.That(state.mergeBoard.Count, Is.EqualTo(2));
            Assert.That(state.mergeBoard.All(i => i.itemId == "tea" && i.level == 1), Is.True);
            Assert.That(game.MofuPoints.Balance, Is.EqualTo(24)); Assert.That(store.Attempts, Is.Zero);
        }

        [Test] public void ZeroBalanceStillAllowsMoveMergeAndFrameManagement()
        {
            state.economy.mofuPoints = 2;
            Assert.That(game.TryGenerateLv1("bread"), Is.True); Assert.That(game.MofuPoints.Balance, Is.Zero);
            Assert.That(game.TryMove(2, 3), Is.True); Assert.That(game.TryMerge(0, 1), Is.True);
            Assert.That(game.PackProductionFrames(new[] { "tea", "bread" }), Is.True);
            Assert.That(game.PlaceProductionFrame("tea"), Is.True);
            Assert.That(game.Save(), Is.True); Assert.That(game.MofuPoints.Balance, Is.Zero);
        }

        [Test] public void DeliveryAndQuestCompletionAtZeroRemainFree()
        {
            state.economy.mofuPoints = 0;
            Assert.That(game.Quests.AcceptQuest(QuestDefinition.First), Is.True);
            Assert.That(game.TryMerge(0, 1), Is.True); game.Board.ReleaseLocks();
            Assert.That(game.Quests.TryDeliver().Success, Is.True);
            Assert.That(state.coins, Is.EqualTo(30)); Assert.That(state.completedQuestIds, Does.Contain("tea-01"));
            Assert.That(game.MofuPoints.Balance, Is.Zero);
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void GenericSetupAndRestoreItemAdditionRemainsFree(int level)
        {
            Assert.That(game.AddItem("bread", level), Is.True);
            Assert.That(game.MofuPoints.Balance, Is.EqualTo(24)); Assert.That(game.Board.At(2).level, Is.EqualTo(level));
        }

        [TestCase("tea", false)] [TestCase("bread", false)] [TestCase("flower", false)]
        [TestCase("tea", true)] [TestCase("bread", true)] [TestCase("flower", true)]
        public void SaveFailureRestoresItemWalletWholeStateAndPriorLocks(string id, bool denied)
        {
            Assert.That(game.TryMerge(0, 1), Is.True);
            state.coins = 77; state.economy.townActionTotal = 44;
            state.economy.day = "2026-11-06"; state.economy.talkRewardCharacterIds.Add("momo");
            string before = StateCodec.Encode(state); string savedBefore = StateCodec.Encode(store.Saved);
            int writes = store.Attempts; store.Fail = true; store.Denied = denied;
            Assert.That(game.TryGenerateLv1(id), Is.False);
            Assert.That(StateCodec.Encode(state), Is.EqualTo(before));
            Assert.That(StateCodec.Encode(store.Saved), Is.EqualTo(savedBefore));
            Assert.That(game.Board.IsLocked(0), Is.True); Assert.That(game.Board.IsLocked(1), Is.True);
            Assert.That(store.Attempts, Is.EqualTo(writes + 1)); Assert.That(game.LastError, Is.Not.Empty);
        }

        [Test] public void DuplicateFrameOperationAddsAndChargesOnlyOnce()
        {
            Assert.That(game.WasGenerationAttemptedInFrame(100), Is.False);
            Assert.That(game.TryGenerateLv1("tea", 100), Is.True);
            Assert.That(game.WasGenerationAttemptedInFrame(100), Is.True);
            Assert.That(game.TryGenerateLv1("tea", 100), Is.False);
            Assert.That(game.TryGenerateLv1("bread", 100), Is.False);
            Assert.That(game.MofuPoints.Balance, Is.EqualTo(22)); Assert.That(state.mergeBoard.Count, Is.EqualTo(3));
            Assert.That(store.Attempts, Is.EqualTo(1));
        }

        [Test] public void DistinctFramesAreDistinctUserOperations()
        {
            Assert.That(game.TryGenerateLv1("tea", 100), Is.True);
            Assert.That(game.TryGenerateLv1("bread", 101), Is.True);
            Assert.That(game.TryGenerateLv1("flower", 102), Is.True);
            Assert.That(game.MofuPoints.Balance, Is.EqualTo(18)); Assert.That(store.Attempts, Is.EqualTo(3));
        }

        [Test] public void ReentrantGenerationDuringSaveCannotChargeAgain()
        {
            bool? duplicate = null;
            store.DuringSave = () => { store.DuringSave = null; duplicate = game.TryGenerateLv1("flower"); };
            Assert.That(game.TryGenerateLv1("tea"), Is.True); Assert.That(duplicate, Is.False);
            Assert.That(game.MofuPoints.Balance, Is.EqualTo(22)); Assert.That(state.mergeBoard.Count, Is.EqualTo(3));
            Assert.That(store.Attempts, Is.EqualTo(1));
        }

        [Test] public void DuplicateFrameDoesNotAutomaticallyRetryFailedSave()
        {
            string before = StateCodec.Encode(state); store.Fail = true;
            Assert.That(game.TryGenerateLv1("tea", 100), Is.False);
            Assert.That(game.TryGenerateLv1("tea", 100), Is.False);
            Assert.That(store.Attempts, Is.EqualTo(1)); Assert.That(StateCodec.Encode(state), Is.EqualTo(before));
        }

        [Test] public void SaveLoadAndNewGameResetDoNotChargeGenerationFees()
        {
            string directory = Path.Combine(Path.GetTempPath(), "generation-cost-" + Guid.NewGuid());
            try
            {
                var disk = new SaveService(directory); var live = new GameStateManager(GameState.CreateInitial(), disk);
                Assert.That(live.MofuPoints.Balance, Is.EqualTo(24)); Assert.That(live.TryGenerateLv1("bread"), Is.True);
                Assert.That(live.Save(), Is.True);
                var resumed = new GameStateManager(disk.Load().State, disk);
                Assert.That(resumed.MofuPoints.Balance, Is.EqualTo(22)); Assert.That(resumed.State.mergeBoard.Count, Is.EqualTo(3));
                disk.ResetSave(); var fresh = disk.Load();
                Assert.That(fresh.State.economy.mofuPoints, Is.EqualTo(24)); Assert.That(fresh.State.mergeBoard.Count, Is.EqualTo(2));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        private sealed class GenerationStore : IStateStore
        {
            public int Attempts; public bool Fail, Denied; public GameState Saved; public Action DuringSave;
            public void Save(GameState value)
            {
                Attempts++; DuringSave?.Invoke();
                if (Fail)
                {
                    value.coins = 0; value.economy.mofuPoints = 0; value.economy.townActionTotal = 999;
                    value.economy.talkRewardCharacterIds.Clear();
                    if (Denied) throw new UnauthorizedAccessException("Expected access denial");
                    throw new IOException("Expected save failure");
                }
                StateCodec.Validate(value); Saved = value.Clone();
            }
            public LoadResult Load() => new LoadResult(Saved?.Clone());
            public void ResetSave() => throw new InvalidOperationException();
        }
    }
}
