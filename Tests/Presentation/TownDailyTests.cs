using System;
using System.IO;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class TownDailyTests
    {
        private static readonly DateTime Day=new DateTime(2026,10,16);
        private static TownDailyService New()=>new TownDailyService(new MemoryTownDailyStore(),Day);
        [Test] public void ACompletedSixQuestSaveStillHasThreeAchievableDailyGoals()
        {
            var game=GameState.CreateInitial();game.completedQuestIds.AddRange(QuestCatalog.All.Select(q=>q.Id));game.townGrowthLevel=6;string before=StateCodec.Encode(game);
            var daily=New();for(int i=0;i<3;i++)Assert.That(daily.RecordMerge(),Is.True);daily.RecordTalk("lulu");daily.RecordWalk();Assert.That(daily.ReadyToClaim,Is.True);
            Assert.That(daily.Claim(),Is.True);Assert.That(daily.Claim(),Is.False);Assert.That(daily.Data.DecorationDays.Count,Is.EqualTo(1));Assert.That(StateCodec.Encode(game),Is.EqualTo(before));
        }
        [Test] public void GreetingsAndClaimsAreOncePerDayAndClockRollbackCannotResetThem()
        {
            var d=New();Assert.That(d.MarkGreeting(),Is.True);Assert.That(d.ShouldGreet,Is.False);d.RecordTalk("momo");d.RecordWalk();for(int i=0;i<3;i++)d.RecordMerge();d.Claim();
            d.EnsureDay(Day.AddDays(-1));Assert.That(d.Data.Claimed,Is.True);Assert.That(d.ShouldGreet,Is.False);Assert.That(d.Claim(),Is.False);
            d.EnsureDay(Day.AddDays(1));Assert.That(d.ShouldGreet,Is.True);Assert.That(d.Data.Merges,Is.Zero);Assert.That(d.Data.DecorationDays.Count,Is.EqualTo(1));
        }
        [Test] public void UnknownCharactersAndIncompleteClaimDoNotCreateCollectionProgress()
        {var d=New();Assert.That(d.RecordTalk("unknown"),Is.False);Assert.That(d.Claim(),Is.False);Assert.That(d.Data.Characters.Sum(c=>c.Meetings),Is.Zero);Assert.That(d.Data.DecorationDays,Is.Empty);}
        private sealed class FailingStore:ITownDailyStore
        {public TownDailyData Load()=>new TownDailyData();public void Save(TownDailyData data)=>throw new IOException("Test social write failure");}
        [Test] public void FailedWritesRollBackCountersAndNeverPretendTheRewardWasClaimed()
        {var d=new TownDailyService(new FailingStore(),Day);Assert.That(d.RecordMerge(),Is.False);Assert.That(d.Data.Merges,Is.Zero);Assert.That(d.LastError,Is.Not.Empty);Assert.That(d.Data.Claimed,Is.False);}
        [Test] public void BookProfilesUseOfficialCharactersAndOnlyActualCompletedQuests()
        {
            var d=New();var game=GameState.CreateInitial();game.completedQuestIds.Add("tea-01");d.RecordTalk("lulu");
            var lulu=TownCharacterBook.For("lulu",game,d.Data);Assert.That(lulu.CompletedRequests,Is.EqualTo(1));Assert.That(lulu.Meetings,Is.EqualTo(1));Assert.That(lulu.Likes,Is.Not.Empty);Assert.That(lulu.NextDiscovery,Does.Contain("あと"));
            Assert.That(CharacterCatalog.All.Select(c=>TownCharacterBook.For(c.Id,game,d.Data).Name),Is.EqualTo(CharacterCatalog.All.Select(c=>c.Name)));
        }
        [Test] public void AuxiliaryFileRoundTripsAndRecoversItsOwnBackupWithoutTouchingMainSave()
        {
            string dir=Path.Combine(Path.GetTempPath(),"mofumachi-daily-"+Guid.NewGuid());Directory.CreateDirectory(dir);
            try
            {
                var main=Path.Combine(dir,"save.json");File.WriteAllText(main,"main-save-must-stay-untouched");var store=new FileTownDailyStore(dir);var d=new TownDailyService(store,Day);d.RecordMerge();d.RecordTalk("pom");
                var loaded=new TownDailyService(new FileTownDailyStore(dir),Day);Assert.That(loaded.Data.Merges,Is.EqualTo(1));Assert.That(loaded.Data.Characters.Single(c=>c.Id=="pom").Meetings,Is.EqualTo(1));
                File.WriteAllText(store.Path,"broken auxiliary data");var recovered=store.Load();Assert.That(recovered.Merges,Is.EqualTo(1));Assert.That(File.ReadAllText(main),Is.EqualTo("main-save-must-stay-untouched"));
            }finally{Directory.Delete(dir,true);}
        }
        [Test] public void WalkingDiscoversExpressionsWithoutInventingAConversationOrDailyReward()
        {
            var daily=New();var game=GameState.CreateInitial();string before=StateCodec.Encode(game);
            Assert.That(daily.RecordOuting("noah"),Is.True);var entry=TownCharacterBook.For("noah",game,daily.Data);
            Assert.That(entry.Reactions,Is.EqualTo(1));Assert.That(entry.IsNew,Is.True);Assert.That(entry.Meetings,Is.Zero);
            Assert.That(daily.Data.Talks,Is.Zero);Assert.That(daily.ReadyToClaim,Is.False);
            Assert.That(daily.MarkBookSeen("noah"),Is.True);Assert.That(TownCharacterBook.For("noah",game,daily.Data).IsNew,Is.False);
            daily.RecordOuting("noah");daily.RecordOuting("noah");Assert.That(TownCharacterBook.For("noah",game,daily.Data).IsNew,Is.True);
            Assert.That(StateCodec.Encode(game),Is.EqualTo(before));
        }
        [Test] public void OptionalCollectionFieldsLoadOldJournalsAndKeepDiscoveryAcknowledgementOnRestart()
        {
            var store=new MemoryTownDailyStore();var d=new TownDailyService(store,Day);d.RecordOuting("momo");d.MarkBookSeen("momo");
            var reload=new TownDailyService(store,Day);Assert.That(TownCharacterBook.For("momo",GameState.CreateInitial(),reload.Data).IsNew,Is.False);
            var old=System.Text.Encoding.UTF8.GetString(TownDailyCodec.Encode(d.Data));
            old=System.Text.RegularExpressions.Regex.Replace(old,",?\"(?:Outings|SeenReactions)\":\\d+","");
            var decoded=TownDailyCodec.Decode(System.Text.Encoding.UTF8.GetBytes(old));TownDailyCodec.Validate(decoded);
            Assert.That(decoded.Characters.Single(c=>c.Id=="momo").Outings,Is.Zero);
        }
        [Test] public void BookDiscoveryNamesMatchTheActualUnlockedTierAndNeverInventAnInteraction()
        {
            var d=New();var game=GameState.CreateInitial();var before=TownDailyCodec.Encode(d.Data);
            Assert.That(TownCharacterBook.For("fio",game,d.Data).LatestDiscovery,Is.EqualTo("まだ知らない一面"));
            Assert.That(TownDailyCodec.Encode(d.Data),Is.EqualTo(before));
            d.RecordOuting("fio");Assert.That(TownCharacterBook.For("fio",game,d.Data).LatestDiscovery,Is.EqualTo("笑顔のリアクション"));
            d.RecordTalk("fio");d.RecordTalk("fio");Assert.That(TownCharacterBook.For("fio",game,d.Data).LatestDiscovery,Is.EqualTo("手を振るリアクション"));
            for(int i=0;i<4;i++)d.RecordTalk("fio");Assert.That(TownCharacterBook.For("fio",game,d.Data).LatestDiscovery,Is.EqualTo("とっておきのお話"));
        }
        [Test] public void CurrentLocationLabelsUseActualDistrictWhenAResidentIsBetweenPlaces()
        {
            Assert.That(TownCharacterBook.PlaceLabel(null,TownDistrictId.South),Is.EqualTo("南の庭園をおさんぽ中"));
            Assert.That(TownCharacterBook.PlaceLabel("west.cafe-front",TownDistrictId.West),Is.EqualTo("西のカフェの前"));
            Assert.That(TownCharacterBook.PlaceLabel("shop",TownDistrictId.East),Is.EqualTo("東の花屋の前"));
            Assert.That(TownCharacterBook.PlaceLabel("quest",TownDistrictId.North),Is.EqualTo("北の依頼小屋の前"));
        }
    }
}
