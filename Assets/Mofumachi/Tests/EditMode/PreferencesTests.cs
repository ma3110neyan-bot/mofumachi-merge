using System;
using System.IO;
using Mofumachi.Core;
using NUnit.Framework;

namespace Mofumachi.Tests
{
    public sealed class PreferencesTests
    {
        private string directory;
        // Genuine v1 schema: no notice or volume members. Keep this independent of the new encoder.
        private const string V1 = "{\"saveVersion\":1,\"coins\":30,\"gems\":7,\"stamina\":12,\"playerLevel\":4," +
            "\"townGrowthLevel\":1,\"activeQuestId\":\"tea-01\",\"questState\":5,\"questProgress\":1," +
            "\"inventory\":[{\"itemId\":\"tea\",\"level\":2,\"count\":3}]," +
            "\"mergeBoard\":[{\"itemId\":\"tea\",\"level\":1,\"cellIndex\":4}]," +
            "\"completedQuestIds\":[\"tea-01\"],\"claimedRewardIds\":[\"quest:tea-01\"]," +
            "\"bgmEnabled\":false,\"seEnabled\":true,\"lastSaveTime\":\"2026-10-09T06:00:00+00:00\"}";

        [SetUp] public void SetUp() => directory = Path.Combine(Path.GetTempPath(), "mofumachi-preferences-" + Guid.NewGuid());
        [TearDown] public void TearDown() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

        [Test]
        public void V1MigrationPreservesCompletedProgressAndMutedAudio()
        {
            var s = StateCodec.Decode(V1);
            StateCodec.Validate(s);
            Assert.That(s.saveVersion, Is.EqualTo(2));
            Assert.That(s.coins, Is.EqualTo(30)); Assert.That(s.gems, Is.EqualTo(7));
            Assert.That(s.stamina, Is.EqualTo(12)); Assert.That(s.playerLevel, Is.EqualTo(4));
            Assert.That(s.townGrowthLevel, Is.EqualTo(1)); Assert.That(s.activeQuestId, Is.EqualTo("tea-01"));
            Assert.That(s.questState, Is.EqualTo(QuestState.TownGrown)); Assert.That(s.questProgress, Is.EqualTo(1));
            Assert.That(s.inventory[0].count, Is.EqualTo(3)); Assert.That(s.inventory[0].level, Is.EqualTo(2));
            Assert.That(s.mergeBoard[0].cellIndex, Is.EqualTo(4)); Assert.That(s.mergeBoard[0].level, Is.EqualTo(1));
            Assert.That(s.completedQuestIds, Is.EqualTo(new[] { "tea-01" }));
            Assert.That(s.claimedRewardIds, Is.EqualTo(new[] { "quest:tea-01" }));
            Assert.That(s.bgmEnabled, Is.False); Assert.That(s.seEnabled, Is.True);
            Assert.That(s.lastSaveTime, Is.EqualTo("2026-10-09T06:00:00+00:00"));
            Assert.That(s.bgmVolume, Is.EqualTo(.75f)); Assert.That(s.seVolume, Is.EqualTo(.65f));
            Assert.That(s.purchaseNoticeAcknowledged, Is.False);
        }

        [Test] public void PreferencesRoundTripPreservesZeroAndOff()
        {
            var disk = new SaveService(directory);
            var game = new GameStateManager(StateCodec.Decode(V1), disk);
            Assert.That(game.AcknowledgePurchaseNotice(), Is.True);
            Assert.That(game.SetAudioPreferences(false, true, 0, .43f), Is.True);
            var resumed = disk.Load().State;
            Assert.That(resumed.purchaseNoticeAcknowledged, Is.True);
            Assert.That(resumed.bgmEnabled, Is.False); Assert.That(resumed.bgmVolume, Is.Zero);
            Assert.That(resumed.seEnabled, Is.True); Assert.That(resumed.seVolume, Is.EqualTo(.43f));
            Assert.That(resumed.coins, Is.EqualTo(30)); Assert.That(resumed.townGrowthLevel, Is.EqualTo(1));
            var next = new GameStateManager(resumed, disk);
            Assert.That(next.SetAudio(true, false), Is.True);
            Assert.That(resumed.bgmVolume, Is.Zero); Assert.That(resumed.seVolume, Is.EqualTo(.43f));
            Assert.That(next.Quests.TryDeliver().Success, Is.False);
            Assert.That(resumed.coins, Is.EqualTo(30));
        }

        [Test] public void PreferencesSaveFailureRollsBackWithMergeLocks()
        {
            var disk = new SwitchableStore(new SaveService(directory));
            var game = new GameStateManager(GameState.CreateInitial(), disk);
            Assert.That(game.TryMerge(0, 1), Is.True);
            string before = StateCodec.Encode(game.State);
            disk.Fail = true;
            Assert.That(game.SetAudioPreferences(false, false, .1f, .2f), Is.False);
            Assert.That(StateCodec.Encode(game.State), Is.EqualTo(before));
            Assert.That(game.Board.IsLocked(0), Is.True); Assert.That(game.Board.IsLocked(1), Is.True);
            Assert.That(game.LastError, Is.Not.Empty);
        }

        [Test] public void NoticeSaveFailureLeavesPlayerUnacknowledged()
        {
            var disk = new SwitchableStore(new SaveService(directory)) { Fail = true };
            var game = new GameStateManager(GameState.CreateInitial(), disk);
            Assert.That(game.AcknowledgePurchaseNotice(), Is.False);
            Assert.That(game.State.purchaseNoticeAcknowledged, Is.False);
            Assert.That(game.State.lastSaveTime, Is.Empty);
            disk.Fail = false;
            Assert.That(game.AcknowledgePurchaseNotice(), Is.True);
            Assert.That(disk.Load().State.purchaseNoticeAcknowledged, Is.True);
        }

        [Test] public void NoticeAcknowledgmentIsSavedOnlyOnce()
        {
            var disk = new SwitchableStore(new SaveService(directory));
            var game = new GameStateManager(GameState.CreateInitial(), disk);
            Assert.That(game.AcknowledgePurchaseNotice(), Is.True);
            Assert.That(game.AcknowledgePurchaseNotice(), Is.True);
            Assert.That(disk.Writes, Is.EqualTo(1));
            Assert.That(disk.Load().State.purchaseNoticeAcknowledged, Is.True);
        }

        [TestCase(-.01f)] [TestCase(1.01f)] [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)] [TestCase(float.NegativeInfinity)]
        public void InvalidPreferenceVolumesAreRejectedBeforeWrite(float value)
        {
            var disk = new SwitchableStore(new SaveService(directory));
            var game = new GameStateManager(GameState.CreateInitial(), disk);
            string before = StateCodec.Encode(game.State);
            Assert.That(game.SetAudioPreferences(true, true, value, .5f), Is.False);
            Assert.That(game.SetAudioPreferences(true, true, .5f, value), Is.False);
            Assert.That(StateCodec.Encode(game.State), Is.EqualTo(before));
            Assert.That(disk.Writes, Is.Zero);
            Assert.That(game.LastError, Is.Not.Empty);
        }

        [TestCase("-0.01")] [TestCase("1.01")] [TestCase("\"NaN\"")]
        [TestCase("\"INF\"")] [TestCase("\"-INF\"")]
        public void InvalidV2VolumeUsesHealthyBackup(string value)
        {
            var disk = new SaveService(directory); disk.Save(GameState.CreateInitial());
            File.WriteAllText(disk.BackupPath, V1);
            string before = File.ReadAllText(disk.PrimaryPath);
            string invalid = before.Replace("\"bgmVolume\":0.75", "\"bgmVolume\":" + value);
            Assert.That(invalid, Is.Not.EqualTo(before), "The corrupt fixture must actually change the JSON.");
            File.WriteAllText(disk.PrimaryPath, invalid);
            var loaded = disk.Load();
            Assert.That(loaded.Recovered, Is.True); Assert.That(loaded.State.coins, Is.EqualTo(30));
            Assert.That(loaded.State.saveVersion, Is.EqualTo(2)); Assert.That(loaded.State.bgmVolume, Is.EqualTo(.75f));
        }

        [TestCase("bgmVolume", "0.75")] [TestCase("seVolume", "0.65")]
        public void MissingV2VolumeUsesHealthyV1Backup(string member, string value)
        {
            var disk = new SaveService(directory); disk.Save(GameState.CreateInitial());
            File.WriteAllText(disk.BackupPath, V1);
            string before = File.ReadAllText(disk.PrimaryPath);
            string incomplete = before.Replace("\"" + member + "\":" + value + ",", "");
            Assert.That(incomplete, Is.Not.EqualTo(before));
            File.WriteAllText(disk.PrimaryPath, incomplete);
            var loaded = disk.Load();
            Assert.That(loaded.Recovered, Is.True); Assert.That(loaded.State.coins, Is.EqualTo(30));
        }

        [TestCase("coins", "0")] [TestCase("gems", "0")]
        [TestCase("stamina", "30")] [TestCase("questProgress", "0")]
        public void MissingProgressMemberCannotSilentlyResetPlayerData(string member, string value)
        {
            var disk = new SaveService(directory); disk.Save(GameState.CreateInitial());
            File.WriteAllText(disk.BackupPath, V1);
            string before = File.ReadAllText(disk.PrimaryPath);
            string incomplete = before.Replace("\"" + member + "\":" + value + ",", "");
            Assert.That(incomplete, Is.Not.EqualTo(before));
            File.WriteAllText(disk.PrimaryPath, incomplete);
            var loaded = disk.Load();
            Assert.That(loaded.Recovered, Is.True); Assert.That(loaded.State.coins, Is.EqualTo(30));
        }

        [Test] public void V1MigrationRetainsActiveQuestAndCanWriteValidV2()
        {
            string activeJson = V1.Replace("\"questState\":5", "\"questState\":2")
                .Replace("\"townGrowthLevel\":1", "\"townGrowthLevel\":0")
                .Replace("[\"tea-01\"]", "[]").Replace("[\"quest:tea-01\"]", "[]");
            var s = StateCodec.Decode(activeJson); var disk = new SaveService(directory);
            var game = new GameStateManager(s, disk);
            Assert.That(game.Quests.CheckDelivery(), Is.True);
            Assert.That(game.AcknowledgePurchaseNotice(), Is.True);
            Assert.That(disk.Load().State.activeQuestId, Is.EqualTo("tea-01"));
            Assert.That(game.Quests.TryDeliver().Success, Is.True);
            Assert.That(disk.Load().State.coins, Is.EqualTo(60));
        }

        private sealed class SwitchableStore : IStateStore
        {
            private readonly IStateStore disk;
            public bool Fail;
            public int Writes;
            public SwitchableStore(IStateStore disk) { this.disk = disk; }
            public void Save(GameState state) { if (Fail) throw new IOException("Test write failure"); disk.Save(state); Writes++; }
            public LoadResult Load() => disk.Load();
            public void ResetSave() => disk.ResetSave();
        }
    }
}
