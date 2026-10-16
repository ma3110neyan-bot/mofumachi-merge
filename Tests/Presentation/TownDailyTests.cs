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
    }
}
