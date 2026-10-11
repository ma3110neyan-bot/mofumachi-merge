using System;
using System.IO;
using System.Linq;
using Mofumachi.Core;
using NUnit.Framework;

namespace Mofumachi.Tests
{
    public sealed class CharacterPreferenceTests
    {
        private const string OldSave = "{\"saveVersion\":2,\"coins\":30,\"gems\":7,\"stamina\":12,\"playerLevel\":4," +
            "\"townGrowthLevel\":1,\"activeQuestId\":\"tea-01\",\"questState\":5,\"questProgress\":1," +
            "\"inventory\":[{\"itemId\":\"tea\",\"level\":2,\"count\":3}]," +
            "\"mergeBoard\":[{\"itemId\":\"tea\",\"level\":1,\"cellIndex\":4}]," +
            "\"completedQuestIds\":[\"tea-01\"],\"claimedRewardIds\":[\"quest:tea-01\"]," +
            "\"bgmEnabled\":false,\"seEnabled\":true,\"bgmVolume\":0.35,\"seVolume\":0.36," +
            "\"purchaseNoticeAcknowledged\":true,\"lastSaveTime\":\"2026-10-09T06:00:00+00:00\"}";

        [TestCase(1)] [TestCase(2)]
        public void V1AndV2MigrationKeepOldProgress(int version)
        {
            string json = OldSave.Replace("\"saveVersion\":2", "\"saveVersion\":" + version);
            if (version == 1) json = json.Replace("\"bgmVolume\":0.35,\"seVolume\":0.36,", "")
                .Replace("\"purchaseNoticeAcknowledged\":true,", "");
            var state = StateCodec.Decode(json);
            StateCodec.Validate(state);
            Assert.That(state.saveVersion, Is.EqualTo(3));
            Assert.That(state.favoriteCharacterId, Is.Empty);
            Assert.That(state.coins, Is.EqualTo(30)); Assert.That(state.townGrowthLevel, Is.EqualTo(1));
            Assert.That(state.activeQuestId, Is.EqualTo("tea-01"));
            Assert.That(state.completedQuestIds, Is.EqualTo(new[] { "tea-01" }));
            Assert.That(state.claimedRewardIds, Is.EqualTo(new[] { "quest:tea-01" }));
            Assert.That(state.mergeBoard[0].cellIndex, Is.EqualTo(4)); Assert.That(state.inventory[0].count, Is.EqualTo(3));
            Assert.That(state.bgmEnabled, Is.False); Assert.That(state.seEnabled, Is.True);
            Assert.That(state.bgmVolume, Is.EqualTo(version == 1 ? .75f : .35f));
            Assert.That(state.seVolume, Is.EqualTo(version == 1 ? .65f : .36f));
            Assert.That(state.purchaseNoticeAcknowledged, Is.EqualTo(version == 2));
        }

        [Test] public void SelectFavoriteCommitsOnlyKnownIdAndRollsBackOnFailure()
        {
            Assert.That(CharacterCatalog.All.Select(c => c.Name), Is.EqualTo(new[] { "モモ", "ルル", "ポム", "フィオ", "ミエル", "ノア" }));
            Assert.That(CharacterCatalog.All.Select(c => c.PortraitIndex), Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5 }));
            var store = new PreferenceStore(); var game = new GameStateManager(StateCodec.Decode(OldSave), store);
            foreach (string id in new[] { "momo", "lulu", "pom", "fio", "miel", "noah" })
            {
                Assert.That(game.SelectFavorite(id), Is.True);
                Assert.That(store.Load().State.favoriteCharacterId, Is.EqualTo(id));
            }
            int writes = store.Writes;
            Assert.That(game.SelectFavorite("old-name"), Is.False);
            Assert.That(game.SelectFavorite(null), Is.False);
            Assert.That(store.Writes, Is.EqualTo(writes));
            Assert.That(CharacterCatalog.Find("old-name"), Is.Null);
            Assert.That(game.SelectFavorite("lulu"), Is.True);
            string before = StateCodec.Encode(game.State); store.Fail = true;
            Assert.That(game.SelectFavorite("momo"), Is.False);
            Assert.That(StateCodec.Encode(game.State), Is.EqualTo(before));
            Assert.That(game.LastError, Is.Not.Empty);
            var copy = GameState.CreateInitial(); copy.CopyFrom(game.State);
            Assert.That(copy.favoriteCharacterId, Is.EqualTo("lulu"));
            Assert.That(copy.coins, Is.EqualTo(30));
        }

        [TestCase(null)] [TestCase("unknown")] [TestCase("missing")]
        public void V3RejectsMissingOrUnknownFavoriteAndKeepsBackup(string favorite)
        {
            string directory = Path.Combine(Path.GetTempPath(), "mofumachi-favorite-" + Guid.NewGuid());
            try
            {
                var disk = new SaveService(directory); var state = StateCodec.Decode(OldSave);
                state.favoriteCharacterId = "lulu"; disk.Save(state); state.favoriteCharacterId = "momo"; disk.Save(state);
                string json = File.ReadAllText(disk.PrimaryPath);
                string invalid = favorite == "missing" ? json.Replace("\"favoriteCharacterId\":\"momo\",", "") :
                    json.Replace("\"favoriteCharacterId\":\"momo\"", "\"favoriteCharacterId\":" + (favorite == null ? "null" : "\"" + favorite + "\""));
                Assert.That(invalid, Is.Not.EqualTo(json)); File.WriteAllText(disk.PrimaryPath, invalid);
                var loaded = disk.Load();
                Assert.That(loaded.Recovered, Is.True); Assert.That(loaded.State.favoriteCharacterId, Is.EqualTo("lulu"));
                Assert.That(loaded.State.coins, Is.EqualTo(30));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        [Test] public void V2MissingVolumeRemainsInvalidAfterMigration()
        {
            var state = StateCodec.Decode(OldSave.Replace("\"bgmVolume\":0.35,", ""));
            Assert.Throws<InvalidDataException>(() => StateCodec.Validate(state));
        }

        private sealed class PreferenceStore : IStateStore
        {
            public int Writes; public bool Fail; private GameState saved;
            public void Save(GameState state) { if (Fail) throw new IOException("Simulated failure"); StateCodec.Validate(state); saved = state.Clone(); Writes++; }
            public LoadResult Load() => new LoadResult(saved.Clone());
            public void ResetSave() => throw new InvalidOperationException("Do not reset progress");
        }
    }
}
