using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Mofumachi.Core;
using NUnit.Framework;

namespace Mofumachi.Tests
{
    public class DeliveryTests
    {
        private string directory;
        [SetUp] public void SetUp() => directory = Path.Combine(Path.GetTempPath(), "mofumachi-quest-" + Guid.NewGuid());
        [TearDown] public void TearDown() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        private QuestManager Manager(GameState s, IStateStore store = null, IEnumerable<QuestDefinition> catalog = null) => new QuestManager(s, store ?? new SaveService(directory), new RewardService(), new TownGrowthService(), catalog);
        private GameState ReadyState()
        {
            var s = GameState.CreateInitial(); var quests = Manager(s); quests.AcceptQuest(QuestDefinition.First);
            var board = new MergeBoard(s); board.TryMerge(0, 1); board.ReleaseLocks(); return s;
        }
        [Test] public void UnacceptedQuestCannotConsumeOrReward()
        {
            var s = GameState.CreateInitial(); s.inventory.Add(new InventoryEntry("tea", 2, 1)); var before = StateCodec.Encode(s);
            Assert.That(Manager(s).TryDeliver().Success, Is.False); Assert.That(StateCodec.Encode(s), Is.EqualTo(before));
        }
        [Test] public void MissingOneRequirementConsumesNothing()
        {
            var quest = new QuestDefinition("two-items", new[] { new ItemRequirement("tea", 1, 2), new ItemRequirement("bread", 1, 1) }, 30);
            var s = GameState.CreateInitial(); var q = Manager(s, catalog: new[] { quest });
            Assert.That(q.AcceptQuest(quest), Is.True);
            var before = StateCodec.Encode(s);
            Assert.That(q.TryDeliver().Success, Is.False); Assert.That(StateCodec.Encode(s), Is.EqualTo(before));
        }
        [Test] public void DuplicateRequirementRowsAreAggregated()
        {
            var quest = new QuestDefinition("aggregate", new[] { new ItemRequirement("tea", 1, 2), new ItemRequirement("tea", 1, 1) }, 30);
            var s = GameState.CreateInitial(); var q = Manager(s, catalog: new[] { quest });
            Assert.That(q.AcceptQuest(quest), Is.True);
            Assert.That(q.CheckDelivery(), Is.False); Assert.That(q.TryDeliver().Success, Is.False); Assert.That(s.mergeBoard.Count, Is.EqualTo(2));
        }
        [Test] public void DeliveryConsumesExactItemsPaysOnceGrowsTownAndSurvivesReload()
        {
            var s = ReadyState(); s.inventory.Add(new InventoryEntry("tea", 2, 2)); var q = Manager(s);
            Assert.That(q.TryDeliver().Success, Is.True);
            // Unity's custom NUnit does not support Assert.Multiple.
            Assert.That(s.inventory[0].count, Is.EqualTo(1)); Assert.That(s.mergeBoard.Count, Is.EqualTo(1));
            Assert.That(s.coins, Is.EqualTo(30)); Assert.That(s.townGrowthLevel, Is.EqualTo(1));
            Assert.That(s.completedQuestIds, Is.EqualTo(new[] { "tea-01" }));
            Assert.That(s.claimedRewardIds, Is.EqualTo(new[] { "quest:tea-01" }));
            Assert.That(s.questState, Is.EqualTo(QuestState.TownGrown));
            Assert.That(s.questProgress, Is.EqualTo(1));
            Assert.That(q.TryDeliver().Success, Is.False); Assert.That(s.coins, Is.EqualTo(30));
            var loaded = new SaveService(directory).Load().State;
            Assert.That(Manager(loaded).TryDeliver().Success, Is.False); Assert.That(loaded.coins, Is.EqualTo(30));
        }
        [Test] public void ClaimedRewardCannotBePaidAgain()
        {
            var s = ReadyState(); s.claimedRewardIds.Add("quest:tea-01"); var before = StateCodec.Encode(s);
            Assert.That(Manager(s).TryDeliver().Success, Is.False); Assert.That(StateCodec.Encode(s), Is.EqualTo(before));
        }
        [Test] public void DeliverySaveFailureRollsBackConsumptionAndReward()
        {
            var s = ReadyState(); var before = StateCodec.Encode(s); var q = Manager(s, new FailingStore());
            var result = q.TryDeliver();
            Assert.That(result.SaveFailed, Is.True); Assert.That(result.Success, Is.False);
            Assert.That(StateCodec.Encode(s), Is.EqualTo(before));
            Assert.That(Manager(s).TryDeliver().Success, Is.True); Assert.That(s.coins, Is.EqualTo(30));
        }
        [Test] public void AcceptSaveFailureRestoresUnacceptedState()
        {
            var s = GameState.CreateInitial(); var before = StateCodec.Encode(s);
            Assert.That(Manager(s, new FailingStore()).AcceptQuest(QuestDefinition.First), Is.False);
            Assert.That(StateCodec.Encode(s), Is.EqualTo(before));
        }
        [Test] public void RepeatedAcceptDoesNotResetProgress()
        {
            var s = ReadyState(); var q = Manager(s); q.RefreshProgress();
            Assert.That(q.AcceptQuest(QuestDefinition.First), Is.False); Assert.That(s.questState, Is.EqualTo(QuestState.Deliverable));
        }
        [Test] public void CustomCatalogQuestResumesAfterLoad()
        {
            var quest = new QuestDefinition("two-teas", new[] { new ItemRequirement("tea", 1, 2) }, 12);
            var s = GameState.CreateInitial(); var disk = new SaveService(directory);
            var q = new QuestManager(s, disk, new RewardService(), new TownGrowthService(), new[] { quest });
            Assert.That(q.AcceptQuest(quest), Is.True);
            var loaded = disk.Load().State;
            var restored = new QuestManager(loaded, disk, new RewardService(), new TownGrowthService(), new[] { quest });
            Assert.That(restored.CheckDelivery(), Is.True); Assert.That(restored.TryDeliver().Success, Is.True);
            Assert.That(loaded.coins, Is.EqualTo(12));
        }
        [Test] public void UnregisteredQuestIsRejectedBeforeSaving()
        {
            var s = GameState.CreateInitial(); var q = Manager(s); var before = StateCodec.Encode(s);
            var quest = new QuestDefinition("unknown", new[] { new ItemRequirement("tea", 1, 2) }, 12);
            Assert.That(q.AcceptQuest(quest), Is.False); Assert.That(StateCodec.Encode(s), Is.EqualTo(before));
        }
        [Test] public void CurrencyOverflowRejectsDeliveryWithoutMutation()
        {
            var s = ReadyState(); s.coins = int.MaxValue; var before = StateCodec.Encode(s);
            Assert.That(Manager(s).TryDeliver().Success, Is.False); Assert.That(StateCodec.Encode(s), Is.EqualTo(before));
        }
        [Test] public void ConcurrentDeliveryAfterLoadAwardsExactlyOnce()
        {
            // A deserialized state has no pre-initialized transaction lock.
            var s = StateCodec.Decode(StateCodec.Encode(ReadyState()));
            s.inventory.Add(new InventoryEntry("tea", 2, 16));
            var q = Manager(s); int successes = 0;
            var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            using (var start = new Barrier(16))
            {
                var threads = Enumerable.Range(0, 16).Select(_ => new Thread(() => {
                    start.SignalAndWait();
                    try { if (q.TryDeliver().Success) Interlocked.Increment(ref successes); }
                    catch (Exception error) { errors.Add(error); }
                })).ToArray();
                foreach (var thread in threads) thread.Start();
                foreach (var thread in threads) thread.Join();
            }
            Assert.That(errors, Is.Empty); Assert.That(successes, Is.EqualTo(1));
            Assert.That(s.coins, Is.EqualTo(30)); Assert.That(s.completedQuestIds.Count, Is.EqualTo(1));
        }
        [Test] public void StateManagerMergeSaveFailureRestoresBoardAndReleasesLocks()
        {
            var s = GameState.CreateInitial(); var game = new GameStateManager(s, new FailingStore());
            Assert.That(game.TryMerge(0, 1), Is.False); Assert.That(s.mergeBoard.Count, Is.EqualTo(2));
            Assert.That(game.Board.IsLocked(0), Is.False); Assert.That(game.Board.IsLocked(1), Is.False);
        }
        [Test] public void LockedMergeResultCannotBeDeliveredUntilAnimationFinishes()
        {
            var s = GameState.CreateInitial(); var game = new GameStateManager(s, new SaveService(directory));
            game.Quests.AcceptQuest(QuestDefinition.First); game.TryMerge(0, 1);
            Assert.That(game.Quests.CheckDelivery(), Is.False);
            Assert.That(game.Quests.TryDeliver().Success, Is.False); Assert.That(s.mergeBoard.Count, Is.EqualTo(1));
            game.Board.ReleaseLocks(); Assert.That(game.Quests.TryDeliver().Success, Is.True);
        }
        [Test] public void UnlockedInventoryCanDeliverWithoutConsumingLockedBoardItem()
        {
            var s = GameState.CreateInitial(); var game = new GameStateManager(s, new SaveService(directory));
            game.Quests.AcceptQuest(QuestDefinition.First); game.TryMerge(0, 1);
            s.inventory.Add(new InventoryEntry("tea", 2, 1));
            Assert.That(game.Quests.TryDeliver().Success, Is.True);
            Assert.That(s.inventory, Is.Empty); Assert.That(s.mergeBoard.Count, Is.EqualTo(1));
            Assert.That(game.Board.IsLocked(1), Is.True);
        }
        [Test] public void FailedMutationPreservesEarlierAnimationLocks()
        {
            var s = GameState.CreateInitial(); var store = new SwitchableStore(new SaveService(directory));
            var game = new GameStateManager(s, store); Assert.That(game.TryMerge(0, 1), Is.True);
            store.Fail = true;
            Assert.That(game.AddItem("tea", 1), Is.False);
            Assert.That(s.mergeBoard.Count, Is.EqualTo(1));
            Assert.That(game.Board.IsLocked(0), Is.True); Assert.That(game.Board.IsLocked(1), Is.True);
        }
        [Test] public void StandaloneSaveWaitsForFailedDeliveryToRollback()
        {
            var s = ReadyState(); var disk = new SaveService(directory); var blocking = new BlockingFailStore(disk);
            var q = Manager(s, blocking);
            var delivery = System.Threading.Tasks.Task.Run(() => q.TryDeliver());
            Assert.That(blocking.Entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
            var started = new ManualResetEventSlim();
            var saving = System.Threading.Tasks.Task.Run(() => { started.Set(); disk.Save(s); });
            bool finishedEarly;
            try { Assert.That(started.Wait(TimeSpan.FromSeconds(5)), Is.True); finishedEarly = saving.Wait(200); }
            finally { blocking.Proceed.Set(); }
            Assert.That(System.Threading.Tasks.Task.WaitAll(new System.Threading.Tasks.Task[] { delivery, saving }, TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(finishedEarly, Is.False, "Saving must wait for the delivery transaction boundary.");
            Assert.That(delivery.Result.SaveFailed, Is.True); Assert.That(disk.Load().State.coins, Is.Zero);
            Assert.That(disk.Load().State.completedQuestIds, Is.Empty);
        }
        private sealed class SwitchableStore : IStateStore
        {
            private readonly IStateStore real; public bool Fail;
            public SwitchableStore(IStateStore store) { real = store; }
            public void Save(GameState s) { if (Fail) throw new IOException("Test failure"); real.Save(s); }
            public LoadResult Load() => real.Load(); public void ResetSave() => real.ResetSave();
        }
        private sealed class BlockingFailStore : IStateStore
        {
            private readonly IStateStore real;
            public readonly ManualResetEventSlim Entered = new ManualResetEventSlim(), Proceed = new ManualResetEventSlim();
            public BlockingFailStore(IStateStore store) { real = store; }
            public void Save(GameState s) { Entered.Set(); if (!Proceed.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException(); throw new IOException("Test failed transaction"); }
            public LoadResult Load() => real.Load(); public void ResetSave() => real.ResetSave();
        }
        private sealed class FailingStore : IStateStore
        {
            public void Save(GameState s) => throw new IOException("Test storage failure");
            public LoadResult Load() => new LoadResult(GameState.CreateInitial());
            public void ResetSave() { }
        }
    }
}
