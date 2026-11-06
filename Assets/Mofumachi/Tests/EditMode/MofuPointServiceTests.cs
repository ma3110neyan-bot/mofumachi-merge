using System;
using System.IO;
using System.Linq;
using Mofumachi.Core;
using NUnit.Framework;

namespace Mofumachi.Tests
{
    public sealed class MofuPointServiceTests
    {
        private const string Today = "2026-11-06", Tomorrow = "2026-11-07";
        private GameState state;
        private GameStateManager game;
        private PointStore store;
        private MofuPointService Points => game.MofuPoints;

        [SetUp] public void SetUp()
        {
            state = GameState.CreateInitial(); store = new PointStore();
            game = new GameStateManager(state, store);
        }

        [Test] public void InitialReadsDoNotGrantOrSaveAnything()
        {
            string before = StateCodec.Encode(state);
            Assert.That(Points.Balance, Is.EqualTo(24)); Assert.That(Points.TownActionTotal, Is.Zero);
            Assert.That(Points.AppliedDay, Is.Empty); Assert.That(Points.CanSpend(2), Is.True);
            Assert.That(store.Attempts, Is.Zero); Assert.That(StateCodec.Encode(state), Is.EqualTo(before));
        }

        [TestCase(24, 10, 34, 10)] [TestCase(90, 10, 96, 6)] [TestCase(96, 4, 96, 0)]
        [TestCase(24, int.MaxValue, 96, 72)] [TestCase(24, 0, 24, 0)]
        public void AdditionCapsWalletAndReportsOnlyPersistedCredit(int balance, int amount, int expected, int credit)
        {
            state.economy.mofuPoints = balance; state.economy.townActionTotal = 7;
            Assert.That(Points.TryAdd(amount, out int actual), Is.True);
            Assert.That(actual, Is.EqualTo(credit)); Assert.That(Points.Balance, Is.EqualTo(expected));
            Assert.That(Points.TownActionTotal, Is.EqualTo(7)); Assert.That(store.Saved.economy.mofuPoints, Is.EqualTo(expected));
        }

        [TestCase(-1)] [TestCase(int.MinValue)]
        public void NegativeAdditionDoesNotMutateOrSave(int amount)
        {
            string before = StateCodec.Encode(state);
            Assert.That(Points.TryAdd(amount, out int actual), Is.False); Assert.That(actual, Is.Zero);
            Assert.That(store.Attempts, Is.Zero); Assert.That(StateCodec.Encode(state), Is.EqualTo(before));
        }

        [TestCase(24, 2, true, 22)] [TestCase(1, 2, false, 1)] [TestCase(0, 0, true, 0)]
        [TestCase(24, -1, false, 24)] [TestCase(24, int.MinValue, false, 24)]
        [TestCase(24, int.MaxValue, false, 24)] [TestCase(0, 1, false, 0)] [TestCase(24, 24, true, 0)]
        public void SpendingNeverOverdrawsOrAutomaticallyRefills(int balance, int cost, bool success, int expected)
        {
            state.economy.mofuPoints = balance; state.economy.townActionTotal = 13;
            Assert.That(Points.CanSpend(cost), Is.EqualTo(success));
            Assert.That(Points.TrySpend(cost), Is.EqualTo(success));
            Assert.That(Points.Balance, Is.EqualTo(expected)); Assert.That(Points.TownActionTotal, Is.EqualTo(13));
            Assert.That(store.Attempts, Is.EqualTo(success ? 1 : 0));
        }

        [Test] public void FullWalletDoesNotLimitIndependentTownActionTotal()
        {
            state.economy.mofuPoints = 96;
            Assert.That(Points.TryAdd(4, out int actual), Is.True); Assert.That(actual, Is.Zero);
            Assert.That(Points.TryAddTownActionTotal(4), Is.True);
            Assert.That(Points.Balance, Is.EqualTo(96)); Assert.That(Points.TownActionTotal, Is.EqualTo(4));
        }

        [Test] public void LongOverflowIsRejectedInsteadOfClampedOrWrapped()
        {
            state.economy.townActionTotal = long.MaxValue - 1;
            Assert.That(Points.TryAddTownActionTotal(1), Is.True);
            Assert.That(Points.TownActionTotal, Is.EqualTo(long.MaxValue));
            int writes = store.Attempts;
            Assert.That(Points.TryAddTownActionTotal(1), Is.False);
            Assert.That(Points.TryAddTownActionTotal(long.MaxValue), Is.False);
            Assert.That(Points.TownActionTotal, Is.EqualTo(long.MaxValue));
            Assert.That(Points.Balance, Is.EqualTo(24)); Assert.That(store.Attempts, Is.EqualTo(writes));
        }

        [TestCase(0L)] [TestCase(-1L)] [TestCase(long.MinValue)]
        public void CumulativeRequiresStrictlyPositiveAmount(long amount)
        {
            Assert.That(Points.TryAddTownActionTotal(amount), Is.False);
            Assert.That(Points.TownActionTotal, Is.Zero); Assert.That(store.Attempts, Is.Zero);
        }

        private void FillDay()
        {
            Assert.That(Points.TryApplyDay(Today), Is.True);
            Assert.That(Points.TryRecordTalk(Today, "momo"), Is.True);
            Assert.That(Points.TryRecordWalk(Today, "normal-one", WalkRewardKind.Normal), Is.True);
            Assert.That(Points.TryRecordWalk(Today, "additional-one", WalkRewardKind.Additional), Is.True);
            Assert.That(Points.TryRecordWalk(Today, "help-one", WalkRewardKind.Help), Is.True);
            Assert.That(Points.TryRecordDailyRecovery(Today), Is.True);
            Assert.That(Points.TryRecordFlowerDay(Today), Is.True);
        }

        [TestCase(Today)] [TestCase("2026-11-05")]
        public void SameOrPastDayPreservesEveryLedgerWithoutSaving(string day)
        {
            FillDay(); string before = StateCodec.Encode(state); int writes = store.Attempts;
            Assert.That(Points.TryApplyDay(day), Is.False);
            Assert.That(StateCodec.Encode(state), Is.EqualTo(before)); Assert.That(store.Attempts, Is.EqualTo(writes));
        }

        [Test] public void NextDayResetsOnlyDailyLedgerAndPreservesHistoryAndProgress()
        {
            state.coins = 123; state.gems = 9; state.stamina = 17; state.townGrowthLevel = 5;
            state.economy.mofuPoints = 0; state.economy.townActionTotal = 44;
            FillDay(); state.economy.rewardsStartDay = Today;
            var expected = state.Clone();
            expected.economy.day = Tomorrow; expected.economy.talkRewardCharacterIds.Clear();
            expected.economy.normalWalkRewards = 0; expected.economy.additionalWalkRewards = 0;
            expected.economy.helpWalkClaimed = false; expected.economy.walkRewardReceipts.Clear();
            Assert.That(Points.TryApplyDay(Tomorrow), Is.True);
            Assert.That(StateCodec.Encode(state), Is.EqualTo(StateCodec.Encode(expected)));
            Assert.That(Points.Balance, Is.Zero); Assert.That(Points.TownActionTotal, Is.EqualTo(44));
            Assert.That(state.economy.lastRecoveryDay, Is.EqualTo(Today)); Assert.That(state.economy.lastFlowerRewardDay, Is.EqualTo(Today));
            Assert.That(state.economy.rewardsStartDay, Is.EqualTo(Today)); Assert.That(state.saveVersion, Is.EqualTo(4));
        }

        [TestCase(null)] [TestCase("")] [TestCase("2026-1-01")] [TestCase("2026-02-30")]
        [TestCase("not-a-day")] [TestCase(" 2026-11-06")]
        public void InvalidAppliedDateDoesNotChangeState(string day)
        {
            string before = StateCodec.Encode(state);
            Assert.That(Points.TryApplyDay(day), Is.False); Assert.That(store.Attempts, Is.Zero);
            Assert.That(StateCodec.Encode(state), Is.EqualTo(before));
        }

        [Test] public void ApplyingDayAndRecordingReceiptsNeverGrantsPoints()
        {
            FillDay(); Assert.That(Points.Balance, Is.EqualTo(24)); Assert.That(Points.TownActionTotal, Is.Zero);
            Assert.That(state.economy.rewardsStartDay, Is.Empty);
        }

        [TestCase("momo")] [TestCase("lulu")] [TestCase("pom")] [TestCase("fio")] [TestCase("miel")] [TestCase("noah")]
        public void TalkAcceptsFormalCharacterIds(string id)
        {
            Points.TryApplyDay(Today);
            Assert.That(Points.TryRecordTalk(Today, id), Is.True);
            Assert.That(state.economy.talkRewardCharacterIds, Is.EqualTo(new[] { id }));
        }

        [TestCase(null)] [TestCase("")] [TestCase(" ")] [TestCase("noa")] [TestCase("unknown")] [TestCase("MOMO")]
        public void TalkRejectsInvalidIds(string id)
        {
            Points.TryApplyDay(Today); int writes = store.Attempts;
            Assert.That(Points.TryRecordTalk(Today, id), Is.False);
            Assert.That(state.economy.talkRewardCharacterIds, Is.Empty); Assert.That(store.Attempts, Is.EqualTo(writes));
        }

        [Test] public void TalkRejectsDuplicateAndFourthCharacter()
        {
            Points.TryApplyDay(Today);
            Assert.That(Points.TryRecordTalk(Today, "momo"), Is.True);
            Assert.That(Points.TryRecordTalk(Today, "lulu"), Is.True);
            Assert.That(Points.TryRecordTalk(Today, "pom"), Is.True);
            int writes = store.Attempts;
            Assert.That(Points.TryRecordTalk(Today, "momo"), Is.False);
            Assert.That(Points.TryRecordTalk(Today, "fio"), Is.False);
            Assert.That(state.economy.talkRewardCharacterIds, Is.EqualTo(new[] { "momo", "lulu", "pom" }));
            Assert.That(store.Attempts, Is.EqualTo(writes));
        }

        [TestCase(null)] [TestCase("")] [TestCase("2026-11-05")] [TestCase(Tomorrow)]
        public void DelayedOrFutureReceiptsCannotEnterCurrentDay(string day)
        {
            Points.TryApplyDay(Today); string before = StateCodec.Encode(state); int writes = store.Attempts;
            Assert.That(Points.TryRecordTalk(day, "momo"), Is.False);
            Assert.That(Points.TryRecordWalk(day, "walk", WalkRewardKind.Normal), Is.False);
            Assert.That(Points.TryRecordDailyRecovery(day), Is.False); Assert.That(Points.TryRecordFlowerDay(day), Is.False);
            Assert.That(StateCodec.Encode(state), Is.EqualTo(before)); Assert.That(store.Attempts, Is.EqualTo(writes));
        }

        [Test] public void WalkCapsAndAllFiveReceiptsArePersistedTogether()
        {
            Points.TryApplyDay(Today);
            Assert.That(Points.TryRecordWalk(Today, "n1", WalkRewardKind.Normal), Is.True);
            Assert.That(Points.TryRecordWalk(Today, "n2", WalkRewardKind.Normal), Is.True);
            Assert.That(Points.TryRecordWalk(Today, "n3", WalkRewardKind.Normal), Is.False);
            Assert.That(Points.TryRecordWalk(Today, "a1", WalkRewardKind.Additional), Is.True);
            Assert.That(Points.TryRecordWalk(Today, "a2", WalkRewardKind.Additional), Is.True);
            Assert.That(Points.TryRecordWalk(Today, "a3", WalkRewardKind.Additional), Is.False);
            Assert.That(Points.TryRecordWalk(Today, "h1", WalkRewardKind.Help), Is.True);
            Assert.That(Points.TryRecordWalk(Today, "h2", WalkRewardKind.Help), Is.False);
            Assert.That(state.economy.walkRewardReceipts.Count, Is.EqualTo(5));
            Assert.That(store.Saved.economy.normalWalkRewards, Is.EqualTo(2));
            Assert.That(store.Saved.economy.additionalWalkRewards, Is.EqualTo(2));
            Assert.That(store.Saved.economy.helpWalkClaimed, Is.True);
            Assert.That(store.Attempts, Is.EqualTo(6)); StateCodec.Validate(store.Saved);
        }

        [Test] public void DuplicateWalkIdIsRejectedAcrossKindsAndCanBeReusedOnlyOnNewDay()
        {
            Points.TryApplyDay(Today);
            Assert.That(Points.TryRecordWalk(Today, "same", WalkRewardKind.Normal), Is.True);
            Assert.That(Points.TryRecordWalk(Today, "same", WalkRewardKind.Additional), Is.False);
            Assert.That(Points.TryRecordWalk(Today, "same", WalkRewardKind.Help), Is.False);
            Assert.That(state.economy.additionalWalkRewards, Is.Zero); Assert.That(state.economy.helpWalkClaimed, Is.False);
            Points.TryApplyDay(Tomorrow);
            Assert.That(Points.TryRecordWalk(Today, "late", WalkRewardKind.Normal), Is.False);
            Assert.That(Points.TryRecordWalk(Tomorrow, "same", WalkRewardKind.Normal), Is.True);
            Assert.That(state.economy.walkRewardReceipts.Single().day, Is.EqualTo(Tomorrow));
        }

        [TestCase(null)] [TestCase("")] [TestCase(" ")]
        public void WalkRejectsEmptyIds(string id)
        {
            Points.TryApplyDay(Today); int writes = store.Attempts;
            Assert.That(Points.TryRecordWalk(Today, id, WalkRewardKind.Normal), Is.False);
            Assert.That(state.economy.walkRewardReceipts, Is.Empty); Assert.That(store.Attempts, Is.EqualTo(writes));
        }

        [Test] public void WalkRejectsOversizedIdAndUnknownKind()
        {
            Points.TryApplyDay(Today);
            Assert.That(Points.TryRecordWalk(Today, new string('x', 129), WalkRewardKind.Normal), Is.False);
            Assert.That(Points.TryRecordWalk(Today, "known-id", (WalkRewardKind)999), Is.False);
            Assert.That(state.economy.walkRewardReceipts, Is.Empty);
        }

        [Test] public void RecoveryAndFlowerAreOncePerAppliedDayAndKeepHistoricalDates()
        {
            Points.TryApplyDay(Today);
            Assert.That(Points.TryRecordDailyRecovery(Today), Is.True); Assert.That(Points.TryRecordDailyRecovery(Today), Is.False);
            Assert.That(Points.TryRecordFlowerDay(Today), Is.True); Assert.That(Points.TryRecordFlowerDay(Today), Is.False);
            Points.TryApplyDay(Tomorrow);
            Assert.That(state.economy.lastRecoveryDay, Is.EqualTo(Today)); Assert.That(state.economy.lastFlowerRewardDay, Is.EqualTo(Today));
            Assert.That(Points.TryRecordDailyRecovery(Tomorrow), Is.True); Assert.That(Points.TryRecordFlowerDay(Tomorrow), Is.True);
        }

        [Test] public void ReceiptsRequireAnAppliedDay()
        {
            Assert.That(Points.TryRecordTalk(Today, "momo"), Is.False);
            Assert.That(Points.TryRecordWalk(Today, "walk", WalkRewardKind.Normal), Is.False);
            Assert.That(Points.TryRecordDailyRecovery(Today), Is.False); Assert.That(Points.TryRecordFlowerDay(Today), Is.False);
            Assert.That(store.Attempts, Is.Zero);
        }

        [Test] public void PointOperationsDoNotRefreshQuestOrChangeUnrelatedProgress()
        {
            state.activeQuestId = "tea-01"; game = new GameStateManager(state, store);
            state.questProgress = 17; state.questState = QuestState.Accepted; state.townGrowthLevel = 4;
            state.coins = 77; state.gems = 11; state.stamina = 3;
            Assert.That(Points.TryAdd(2, out _), Is.True); Assert.That(Points.TrySpend(1), Is.True);
            Assert.That(Points.TryAddTownActionTotal(4), Is.True); FillDay();
            Assert.That(state.questProgress, Is.EqualTo(17)); Assert.That(state.questState, Is.EqualTo(QuestState.Accepted));
            Assert.That(state.coins, Is.EqualTo(77)); Assert.That(state.gems, Is.EqualTo(11)); Assert.That(state.stamina, Is.EqualTo(3));
            Assert.That(state.townGrowthLevel, Is.EqualTo(4)); Assert.That(state.saveVersion, Is.EqualTo(4));
        }

        [TestCase("add")] [TestCase("spend")] [TestCase("total")] [TestCase("day")]
        [TestCase("talk")] [TestCase("walk")] [TestCase("recovery")] [TestCase("flower")]
        public void SaveFailureRestoresWholeEconomyAndMergeLocks(string operation)
        {
            Points.TryApplyDay(Today); Points.TryRecordTalk(Today, "momo");
            Points.TryRecordWalk(Today, "existing", WalkRewardKind.Normal);
            state.economy.lastRecoveryDay = "2026-11-05"; state.economy.lastFlowerRewardDay = "2026-11-05";
            Assert.That(game.TryMerge(0, 1), Is.True);
            string before = StateCodec.Encode(state); int writes = store.Attempts;
            store.Fail = true; store.CorruptBeforeFailure = true;
            bool result;
            switch (operation)
            {
                case "add": result = Points.TryAdd(4, out int credited); Assert.That(credited, Is.Zero); break;
                case "spend": result = Points.TrySpend(2); break;
                case "total": result = Points.TryAddTownActionTotal(4); break;
                case "day": result = Points.TryApplyDay(Tomorrow); break;
                case "talk": result = Points.TryRecordTalk(Today, "lulu"); break;
                case "walk": result = Points.TryRecordWalk(Today, "new", WalkRewardKind.Additional); break;
                case "recovery": result = Points.TryRecordDailyRecovery(Today); break;
                default: result = Points.TryRecordFlowerDay(Today); break;
            }
            Assert.That(result, Is.False); Assert.That(StateCodec.Encode(state), Is.EqualTo(before));
            Assert.That(store.Attempts, Is.EqualTo(writes + 1)); Assert.That(game.LastError, Is.Not.Empty);
            Assert.That(game.Board.IsLocked(0), Is.True); Assert.That(game.Board.IsLocked(1), Is.True);
        }

        [Test] public void AccessDeniedAlsoRestoresBalanceAndReportsZeroCredit()
        {
            store.Denied = true; string before = StateCodec.Encode(state);
            Assert.That(Points.TryAdd(10, out int credit), Is.False); Assert.That(credit, Is.Zero);
            Assert.That(StateCodec.Encode(state), Is.EqualTo(before)); Assert.That(store.Attempts, Is.EqualTo(1));
        }

        [Test] public void InvalidExistingEconomyIsRejectedWithoutRepairOrWrite()
        {
            state.economy.mofuPoints = 97;
            Assert.That(Points.TrySpend(1), Is.False); Assert.That(state.economy.mofuPoints, Is.EqualTo(97));
            Assert.That(store.Attempts, Is.Zero);
        }

        [Test] public void SuccessfulOperationsRoundTripThroughExistingV4SaveService()
        {
            string directory = Path.Combine(Path.GetTempPath(), "mofu-points-" + Guid.NewGuid());
            try
            {
                var disk = new SaveService(directory); game = new GameStateManager(GameState.CreateInitial(), disk);
                Assert.That(Points.TryApplyDay(Today), Is.True); Assert.That(Points.TryAdd(10, out _), Is.True);
                Assert.That(Points.TrySpend(2), Is.True); Assert.That(Points.TryAddTownActionTotal(4), Is.True);
                Assert.That(Points.TryRecordTalk(Today, "noah"), Is.True);
                Assert.That(Points.TryRecordWalk(Today, "walk-1", WalkRewardKind.Normal), Is.True);
                var loaded = new SaveService(directory).Load();
                Assert.That(loaded.IsBlocked, Is.False); Assert.That(loaded.State.saveVersion, Is.EqualTo(4));
                Assert.That(loaded.State.economy.mofuPoints, Is.EqualTo(32)); Assert.That(loaded.State.economy.townActionTotal, Is.EqualTo(4));
                Assert.That(loaded.State.economy.talkRewardCharacterIds, Is.EqualTo(new[] { "noah" }));
                Assert.That(loaded.State.economy.walkRewardReceipts.Single().walkId, Is.EqualTo("walk-1"));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        private sealed class PointStore : IStateStore
        {
            public int Attempts; public bool Fail, Denied, CorruptBeforeFailure; public GameState Saved;
            public void Save(GameState value)
            {
                Attempts++;
                if (Denied) throw new UnauthorizedAccessException("Expected access denial");
                if (Fail)
                {
                    if (CorruptBeforeFailure)
                    {
                        value.economy.mofuPoints = 0; value.economy.townActionTotal = 999;
                        value.economy.talkRewardCharacterIds.Clear(); value.economy.walkRewardReceipts.Clear();
                        value.economy.normalWalkRewards = 0; value.economy.day = Tomorrow;
                        value.economy.lastRecoveryDay = Tomorrow; value.economy.lastFlowerRewardDay = Tomorrow;
                    }
                    throw new IOException("Expected write failure");
                }
                StateCodec.Validate(value); Saved = value.Clone();
            }
            public LoadResult Load() => new LoadResult(Saved?.Clone());
            public void ResetSave() => throw new InvalidOperationException("Not used by point service tests");
        }
    }
}
