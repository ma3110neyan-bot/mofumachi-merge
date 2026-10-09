using System;
using System.IO;
using Mofumachi.Core;
using NUnit.Framework;

namespace Mofumachi.Tests
{
    public class SaveServiceTests
    {
        private string directory;
        [SetUp] public void SetUp() => directory = Path.Combine(Path.GetTempPath(), "mofumachi-test-" + Guid.NewGuid());
        [TearDown] public void TearDown() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

        [Test] public void RoundTripPreservesAllRequiredFields()
        {
            var state = GameState.CreateInitial();
            state.coins = 42; state.gems = 7; state.stamina = 12; state.playerLevel = 3; state.townGrowthLevel = 2;
            state.activeQuestId = "tea-01"; state.questState = QuestState.Producing; state.questProgress = 1;
            state.inventory.Add(new InventoryEntry("tea", 2, 3));
            state.completedQuestIds.Add("earlier"); state.claimedRewardIds.Add("quest:earlier");
            state.bgmEnabled = false; state.seEnabled = false;
            var save = new SaveService(directory); save.Save(state);
            var loaded = save.Load();
            Assert.That(loaded.Recovered, Is.False);
            var s = loaded.State;
            // Unity's custom NUnit does not support Assert.Multiple.
            Assert.That(s.saveVersion, Is.EqualTo(2)); Assert.That(s.coins, Is.EqualTo(42));
            Assert.That(s.gems, Is.EqualTo(7)); Assert.That(s.stamina, Is.EqualTo(12));
            Assert.That(s.playerLevel, Is.EqualTo(3)); Assert.That(s.townGrowthLevel, Is.EqualTo(2));
            Assert.That(s.activeQuestId, Is.EqualTo("tea-01")); Assert.That(s.questState, Is.EqualTo(QuestState.Producing));
            Assert.That(s.questProgress, Is.EqualTo(1)); Assert.That(s.inventory[0].count, Is.EqualTo(3));
            Assert.That(s.inventory[0].itemId, Is.EqualTo("tea")); Assert.That(s.inventory[0].level, Is.EqualTo(2));
            Assert.That(s.mergeBoard[0].cellIndex, Is.EqualTo(0)); Assert.That(s.mergeBoard[0].itemId, Is.EqualTo("tea"));
            Assert.That(s.completedQuestIds, Is.EqualTo(new[] { "earlier" }));
            Assert.That(s.claimedRewardIds, Is.EqualTo(new[] { "quest:earlier" }));
            Assert.That(s.bgmEnabled, Is.False); Assert.That(s.seEnabled, Is.False);
            Assert.That(DateTimeOffset.Parse(s.lastSaveTime).Offset, Is.EqualTo(TimeSpan.Zero));
        }
        [Test] public void CorruptPrimaryRestoresPreviousValidSnapshot()
        {
            var save = new SaveService(directory); var state = GameState.CreateInitial();
            state.coins = 10; save.Save(state); state.coins = 20; save.Save(state);
            File.WriteAllText(save.PrimaryPath, "{ broken");
            var result = save.Load();
            Assert.That(result.Recovered, Is.True); Assert.That(result.State.coins, Is.EqualTo(10));
            Assert.That(result.Reason, Is.Not.Empty);
        }
        [Test] public void SavingAfterRecoveryKeepsHealthyBackup()
        {
            var save = new SaveService(directory); var state = GameState.CreateInitial();
            state.coins = 10; save.Save(state); state.coins = 20; save.Save(state);
            File.WriteAllText(save.PrimaryPath, "broken");
            state = save.Load().State; state.coins = 30; save.Save(state);
            File.WriteAllText(save.PrimaryPath, "broken again");
            Assert.That(save.Load().State.coins, Is.EqualTo(10));
        }
        [Test] public void TwoCorruptFilesReturnSafeInitialState()
        {
            var save = new SaveService(directory); save.Save(GameState.CreateInitial()); save.Save(GameState.CreateInitial());
            File.WriteAllText(save.PrimaryPath, "invalid"); File.WriteAllText(save.BackupPath, "invalid");
            var result = save.Load();
            Assert.That(result.Recovered, Is.True); Assert.That(result.State.coins, Is.Zero);
            Assert.That(result.State.completedQuestIds, Is.Empty);
        }
        [Test] public void UnknownVersionIsNotLoaded()
        {
            var save = new SaveService(directory); save.Save(GameState.CreateInitial());
            string original = File.ReadAllText(save.PrimaryPath);
            string unsupported = original.Replace("\"saveVersion\":2", "\"saveVersion\":99");
            Assert.That(unsupported, Is.Not.EqualTo(original));
            File.WriteAllText(save.PrimaryPath, unsupported);
            Assert.That(save.Load().Recovered, Is.True); Assert.That(save.Load().State.saveVersion, Is.EqualTo(2));
        }
        [TestCase(-1)] [TestCase(30)] public void InvalidCellIsRejectedBeforeSave(int index)
        {
            var state = GameState.CreateInitial(); state.mergeBoard[0].cellIndex = index;
            Assert.Throws<InvalidDataException>(() => new SaveService(directory).Save(state));
            Assert.That(File.Exists(Path.Combine(directory, "save.json")), Is.False);
        }
        [Test] public void DuplicateCellIsRejected()
        {
            var state = GameState.CreateInitial(); state.mergeBoard[1].cellIndex = 0;
            Assert.Throws<InvalidDataException>(() => new SaveService(directory).Save(state));
        }
        [Test] public void NegativeCurrencyIsRejected()
        {
            var state = GameState.CreateInitial(); state.coins = -1;
            Assert.Throws<InvalidDataException>(() => new SaveService(directory).Save(state));
        }
        [Test] public void WriteFailureDoesNotChangeSaveTimestamp()
        {
            Directory.CreateDirectory(directory); var file = Path.Combine(directory, "not-a-directory"); File.WriteAllText(file, "occupied");
            var state = GameState.CreateInitial();
            Assert.Throws<IOException>(() => new SaveService(file).Save(state));
            Assert.That(state.lastSaveTime, Is.Empty);
        }
        [Test] public void ResetRemovesPrimaryBackupAndTemporarySave()
        {
            var save = new SaveService(directory); save.Save(GameState.CreateInitial()); save.Save(GameState.CreateInitial());
            File.WriteAllText(save.PrimaryPath + ".tmp", "partial"); save.ResetSave();
            Assert.That(Directory.GetFiles(directory), Is.Empty); Assert.That(save.Load().Recovered, Is.False);
        }
    }
}
