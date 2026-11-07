using System;
using System.IO;
using System.Text;
using System.Linq;
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
            Assert.That(s.saveVersion, Is.EqualTo(4)); Assert.That(s.coins, Is.EqualTo(42));
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
        [Test] public void TwoCorruptFilesBlockWithoutCreatingInitialState()
        {
            var save = new SaveService(directory); save.Save(GameState.CreateInitial()); save.Save(GameState.CreateInitial());
            File.WriteAllText(save.PrimaryPath, "invalid"); File.WriteAllText(save.BackupPath, "invalid");
            var result = save.Load();
            Assert.That(result.IsBlocked, Is.True); Assert.That(result.State, Is.Null);
            Assert.Throws<IOException>(() => save.Save(GameState.CreateInitial()));
            Assert.That(File.ReadAllText(save.PrimaryPath), Is.EqualTo("invalid"));
        }
        [Test] public void UnknownVersionIsNotLoaded()
        {
            var save = new SaveService(directory); save.Save(GameState.CreateInitial());
            string original = File.ReadAllText(save.PrimaryPath);
            string unsupported = original.Replace("\"saveVersion\":4", "\"saveVersion\":99");
            Assert.That(unsupported, Is.Not.EqualTo(original));
            File.WriteAllText(save.PrimaryPath, unsupported);
            Assert.That(save.Load().IsBlocked, Is.True);
            Assert.That(File.ReadAllText(save.PrimaryPath), Is.EqualTo(unsupported));
            Assert.Throws<IOException>(() => save.Save(GameState.CreateInitial()));
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

        private const string LegacyV3 = "{\"saveVersion\":3,\"coins\":274,\"gems\":7,\"stamina\":11,\"playerLevel\":4," +
            "\"townGrowthLevel\":5,\"activeQuestId\":\"bread-02\",\"questState\":2,\"questProgress\":1," +
            "\"inventory\":[{\"itemId\":\"flower\",\"level\":3,\"count\":2}]," +
            "\"mergeBoard\":[{\"itemId\":\"tea\",\"level\":1,\"cellIndex\":4},{\"itemId\":\"bread\",\"level\":3,\"cellIndex\":29}]," +
            "\"completedQuestIds\":[\"tea-01\",\"bread-01\",\"flower-01\",\"tea-02\",\"flower-02\"]," +
            "\"claimedRewardIds\":[\"quest:tea-01\",\"quest:bread-01\",\"quest:flower-01\",\"quest:tea-02\",\"quest:flower-02\"]," +
            "\"bgmEnabled\":false,\"seEnabled\":true,\"bgmVolume\":0.25,\"seVolume\":0.75,\"purchaseNoticeAcknowledged\":true," +
            "\"favoriteCharacterId\":\"miel\",\"productionFrameIds\":[\"tea\",\"flower\"],\"lastSaveTime\":\"2026-10-09T06:00:00+00:00\"}";

        [Test] public void NewV4HasUnclaimedEconomyAnd24Points()
        {
            var result = new SaveService(directory).Load(); var s = result.State;
            Assert.That(result.IsBlocked, Is.False); Assert.That(s.saveVersion, Is.EqualTo(4));
            Assert.That(s.economy.initialized, Is.True); Assert.That(s.economy.mofuPoints, Is.EqualTo(24));
            Assert.That(s.economy.townActionTotal, Is.Zero); Assert.That(s.economy.questRoute, Is.EqualTo(QuestProgressionRoute.ExpandedNewPlayer));
            Assert.That(s.economy.day, Is.Empty); Assert.That(s.economy.rewardsStartDay, Is.Empty);
            Assert.That(s.economy.lastRecoveryDay, Is.Empty); Assert.That(s.economy.lastFlowerRewardDay, Is.Empty);
            Assert.That(s.economy.talkRewardCharacterIds, Is.Empty); Assert.That(s.economy.walkRewardReceipts, Is.Empty);
            Assert.That(s.economy.normalWalkRewards, Is.Zero); Assert.That(s.economy.additionalWalkRewards, Is.Zero);
            Assert.That(s.economy.helpWalkClaimed, Is.False); StateCodec.Validate(s);
        }
        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void LegacyDiskMigrationKeepsEveryFieldAndOriginalBytes(int version)
        {
            Directory.CreateDirectory(directory); var save = new SaveService(directory);
            string raw = LegacyV3.Replace("\"saveVersion\":3", "\"saveVersion\":" + version);
            File.WriteAllText(save.PrimaryPath, "\r\n" + raw + "\r\n", new UTF8Encoding(true));
            byte[] original = File.ReadAllBytes(save.PrimaryPath);
            var loaded = save.Load(); var s = loaded.State;
            Assert.That(loaded.IsBlocked, Is.False, loaded.Reason); Assert.That(s.saveVersion, Is.EqualTo(4));
            Assert.That(File.ReadAllBytes(save.PreV4Path), Is.EqualTo(original));
            Assert.That(s.economy.mofuPoints, Is.EqualTo(24)); Assert.That(s.economy.townActionTotal, Is.Zero);
            Assert.That(s.economy.questRoute, Is.EqualTo(QuestProgressionRoute.LegacyPlayer));
            Assert.That(s.coins, Is.EqualTo(274)); Assert.That(s.gems, Is.EqualTo(7)); Assert.That(s.stamina, Is.EqualTo(11));
            Assert.That(s.playerLevel, Is.EqualTo(4)); Assert.That(s.townGrowthLevel, Is.EqualTo(5));
            Assert.That(s.activeQuestId, Is.EqualTo("bread-02")); Assert.That(s.questState, Is.EqualTo(QuestState.Producing));
            Assert.That(s.questProgress, Is.EqualTo(1)); Assert.That(s.inventory.Single().count, Is.EqualTo(2));
            Assert.That(s.inventory.Single().itemId, Is.EqualTo("flower")); Assert.That(s.inventory.Single().level, Is.EqualTo(3));
            Assert.That(s.mergeBoard.Select(i => i.cellIndex), Is.EqualTo(new[] {4,29}));
            Assert.That(s.mergeBoard.Select(i => i.itemId), Is.EqualTo(new[] {"tea","bread"}));
            Assert.That(s.mergeBoard.Select(i => i.level), Is.EqualTo(new[] {1,3}));
            Assert.That(s.completedQuestIds, Is.EqualTo(new[] {"tea-01","bread-01","flower-01","tea-02","flower-02"}));
            Assert.That(s.claimedRewardIds, Is.EqualTo(s.completedQuestIds.Select(id => "quest:" + id)));
            Assert.That(s.favoriteCharacterId, Is.EqualTo("miel")); Assert.That(s.productionFrameIds, Is.EqualTo(new[] {"tea","flower"}));
            Assert.That(s.bgmEnabled, Is.False); Assert.That(s.seEnabled, Is.True);
            Assert.That(s.bgmVolume, Is.EqualTo(.25f)); Assert.That(s.seVolume, Is.EqualTo(.75f));
            Assert.That(s.purchaseNoticeAcknowledged, Is.True); Assert.That(s.lastSaveTime, Is.EqualTo("2026-10-09T06:00:00+00:00"));
            Assert.That(StateCodec.Decode(File.ReadAllText(save.PrimaryPath)).saveVersion, Is.EqualTo(4));
            s.economy.mofuPoints = 0; save.Save(s);
            Assert.That(new SaveService(directory).Load().State.economy.mofuPoints, Is.Zero);
            Assert.That(File.ReadAllBytes(save.PreV4Path), Is.EqualTo(original));
        }
        [Test] public void V4ZeroBalanceIsNotInitializedAgain()
        {
            var save = new SaveService(directory); var s = GameState.CreateInitial(); s.economy.mofuPoints = 0;
            save.Save(s); Assert.That(save.Load().State.economy.mofuPoints, Is.Zero);
            Assert.That(new SaveService(directory).Load().State.economy.mofuPoints, Is.Zero);
        }
        [Test] public void CopyAndFailedMutationRestoreCompleteEconomyWithoutAliasing()
        {
            var s = GameState.CreateInitial(); s.economy.mofuPoints = 7; s.economy.townActionTotal = 12;
            s.economy.rewardsStartDay = s.economy.day = s.economy.lastRecoveryDay = s.economy.lastFlowerRewardDay = "2026-11-05";
            s.economy.talkRewardCharacterIds.Add("momo"); s.economy.normalWalkRewards = 1;
            s.economy.walkRewardReceipts.Add(new WalkRewardReceipt("2026-11-05", "walk-one", WalkRewardKind.Normal));
            var before = s.Clone(); s.economy.mofuPoints = 0; s.economy.talkRewardCharacterIds.Clear();
            s.CopyFrom(before); Assert.That(StateCodec.Encode(s), Is.EqualTo(StateCodec.Encode(before)));
            s.economy.talkRewardCharacterIds.Add("lulu"); Assert.That(before.economy.talkRewardCharacterIds.Count, Is.EqualTo(1));
            s.CopyFrom(before); var manager = new GameStateManager(s, new EconomyFailureStore());
            Assert.That(manager.AddItem("bread",1), Is.False);
            Assert.That(StateCodec.Encode(s), Is.EqualTo(StateCodec.Encode(before)));
        }
        private sealed class EconomyFailureStore : IStateStore
        {
            public void Save(GameState s) { s.economy.mofuPoints = 0; s.economy.talkRewardCharacterIds.Clear(); throw new IOException("Injected save failure"); }
            public LoadResult Load() => throw new InvalidOperationException();
            public void ResetSave() => throw new InvalidOperationException();
        }
        [TestCase(false)] [TestCase(true)]
        public void ExistingPreV4IsNeverOverwrittenOrRetried(bool matching)
        {
            Directory.CreateDirectory(directory); var save = new SaveService(directory);
            File.WriteAllText(save.PrimaryPath, LegacyV3);
            File.WriteAllText(save.PreV4Path, matching ? LegacyV3 : "incomplete protection");
            byte[] original = File.ReadAllBytes(save.PrimaryPath), protectedBytes = File.ReadAllBytes(save.PreV4Path);
            var first = save.Load(); Assert.That(first.IsBlocked, Is.True); Assert.That(first.State, Is.Null);
            Assert.That(save.Load().Reason, Is.EqualTo(first.Reason));
            Assert.Throws<IOException>(() => save.Save(GameState.CreateInitial()));
            Assert.That(File.ReadAllBytes(save.PrimaryPath), Is.EqualTo(original));
            Assert.That(File.ReadAllBytes(save.PreV4Path), Is.EqualTo(protectedBytes));
            Assert.That(new SaveService(directory).Load().IsBlocked, Is.True);
        }
        [Test] public void ProtectionCreationFailureDoesNotTouchLegacyPrimary()
        {
            Directory.CreateDirectory(directory); var save = new SaveService(directory);
            File.WriteAllText(save.PrimaryPath, LegacyV3); Directory.CreateDirectory(save.PreV4Path);
            Assert.That(save.Load().IsBlocked, Is.True); Assert.That(File.ReadAllText(save.PrimaryPath), Is.EqualTo(LegacyV3));
            Assert.That(File.Exists(save.PrimaryPath + ".tmp"), Is.False);
        }
        [TestCase("missing")] [TestCase("uninitialized")] [TestCase("range")] [TestCase("walks")]
        public void InvalidV4BlocksEvenWhenHealthyBackupExists(string kind)
        {
            var save = new SaveService(directory); var s = GameState.CreateInitial(); save.Save(s); save.Save(s);
            if(kind == "missing")s.economy = null;
            else if(kind == "uninitialized")s.economy.initialized = false;
            else if(kind == "range")s.economy.mofuPoints = 97;
            else s.economy.normalWalkRewards = 1;
            string invalid = StateCodec.Encode(s); File.WriteAllText(save.PrimaryPath, invalid);
            Assert.That(save.Load().IsBlocked, Is.True); Assert.That(save.Load().State, Is.Null);
            Assert.Throws<IOException>(() => save.Save(GameState.CreateInitial()));
            Assert.That(File.ReadAllText(save.PrimaryPath), Is.EqualTo(invalid));
        }
        [Test] public void MissingRequiredV4EconomyMemberCannotFallbackOrRepair()
        {
            var save = new SaveService(directory); save.Save(GameState.CreateInitial()); save.Save(GameState.CreateInitial());
            string invalid = File.ReadAllText(save.PrimaryPath).Replace("\"mofuPoints\":24,", "");
            Assert.That(invalid, Does.Not.Contain("\"mofuPoints\"")); File.WriteAllText(save.PrimaryPath, invalid);
            Assert.That(save.Load().IsBlocked, Is.True); Assert.That(File.ReadAllText(save.PrimaryPath), Is.EqualTo(invalid));
        }
        [Test] public void PendingTemporarySaveBlocksAndPreservesAllFiles()
        {
            Directory.CreateDirectory(directory); var save = new SaveService(directory);
            File.WriteAllText(save.PrimaryPath, LegacyV3); File.WriteAllText(save.PrimaryPath + ".tmp", "partial");
            Assert.That(save.Load().IsBlocked, Is.True);
            Assert.That(File.ReadAllText(save.PrimaryPath), Is.EqualTo(LegacyV3));
            Assert.That(File.ReadAllText(save.PrimaryPath + ".tmp"), Is.EqualTo("partial"));
        }
        [Test] public void OutOfRangeLegacyTownLevelStopsWithoutClamping()
        {
            Directory.CreateDirectory(directory); var save = new SaveService(directory);
            string raw = LegacyV3.Replace("\"townGrowthLevel\":5", "\"townGrowthLevel\":7"); File.WriteAllText(save.PrimaryPath, raw);
            Assert.That(save.Load().IsBlocked, Is.True); Assert.That(File.ReadAllText(save.PrimaryPath), Is.EqualTo(raw));
            Assert.That(File.Exists(save.PreV4Path), Is.False);
        }
        [Test] public void AlteredProtectionIsDetectedAfterSuccessfulMigration()
        {
            Directory.CreateDirectory(directory); var save = new SaveService(directory); File.WriteAllText(save.PrimaryPath, LegacyV3);
            Assert.That(save.Load().IsBlocked, Is.False);
            File.WriteAllText(save.PreV4Path, LegacyV3.Replace("\"coins\":274", "\"coins\":275"));
            Assert.That(new SaveService(directory).Load().IsBlocked, Is.True);
        }
        [Test] public void UnknownExtensionFieldsSurviveMigration()
        {
            Directory.CreateDirectory(directory); var save = new SaveService(directory);
            File.WriteAllText(save.PrimaryPath, LegacyV3.Replace("\"coins\":274", "\"futureNote\":\"keep-me\",\"coins\":274"));
            Assert.That(save.Load().IsBlocked, Is.False);
            Assert.That(File.ReadAllText(save.PrimaryPath), Does.Contain("\"futureNote\":\"keep-me\""));
        }
        [Test] public void FailedCommitKeepsLegacySourceProtectionAndCandidate()
        {
            Directory.CreateDirectory(directory); var save = new SaveService(directory);
            File.WriteAllText(save.PrimaryPath, LegacyV3); Directory.CreateDirectory(save.BackupPath);
            byte[] original = File.ReadAllBytes(save.PrimaryPath);
            Assert.That(save.Load().IsBlocked, Is.True);
            Assert.That(File.ReadAllBytes(save.PrimaryPath), Is.EqualTo(original));
            Assert.That(File.ReadAllBytes(save.PreV4Path), Is.EqualTo(original));
            Assert.That(File.Exists(save.PrimaryPath + ".tmp"), Is.True);
            Assert.That(new SaveService(directory).Load().IsBlocked, Is.True);
        }
        [TestCase(1, "volume")] [TestCase(1, "favorite")] [TestCase(2, "favorite")]
        public void PresentInvalidLegacyPreferencesAreNotTreatedAsAbsent(int version, string member)
        {
            Directory.CreateDirectory(directory); var save = new SaveService(directory);
            string raw = LegacyV3.Replace("\"saveVersion\":3", "\"saveVersion\":" + version);
            raw = member == "volume" ? raw.Replace("\"bgmVolume\":0.25", "\"bgmVolume\":\"NaN\"") :
                raw.Replace("\"favoriteCharacterId\":\"miel\"", "\"favoriteCharacterId\":null");
            File.WriteAllText(save.PrimaryPath, raw);
            Assert.That(save.Load().IsBlocked, Is.True);
            Assert.That(File.ReadAllText(save.PrimaryPath), Is.EqualTo(raw));
            Assert.That(File.Exists(save.PreV4Path), Is.False);
        }

    }
}
