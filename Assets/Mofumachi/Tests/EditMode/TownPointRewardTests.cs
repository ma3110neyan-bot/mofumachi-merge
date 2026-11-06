using System;
using System.IO;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;

namespace Mofumachi.Tests
{
    public sealed class TownPointRewardTests
    {
        private const string Day = "2026-11-06", Next = "2026-11-07";
        private static readonly DateTime Today = new DateTime(2026, 11, 6);
        private GameState state; private GameStateManager game; private PointStore store;
        private JournalStore journal; private TownDailyService daily;
        [SetUp] public void SetUp()
        {
            state = GameState.CreateInitial(); store = new PointStore(); game = new GameStateManager(state, store);
            journal = new JournalStore(); daily = new TownDailyService(journal, Today, game.MofuPoints); journal.Writes = 0;
        }
        private bool Reward(string kind, string day = Day) => kind == "talk" ? game.MofuPoints.TryRewardTalk(day, "momo") :
            kind == "walk" ? game.MofuPoints.TryRewardNormalWalk(day, day, "walk-1") : game.MofuPoints.TryRewardFlower(day);
        private static int Value(string kind) => kind == "talk" ? 4 : kind == "walk" ? 6 : 8;
        private void ReadyFlower() { daily.RecordTalk("momo",Today); daily.RecordWalk(); daily.RecordMerge(); daily.RecordMerge(); daily.RecordMerge(); }

        [TestCase("momo")] [TestCase("lulu")] [TestCase("pom")] [TestCase("fio")] [TestCase("miel")] [TestCase("noah")]
        public void FirstKnownConversationPaysFourAndSavesReceiptOnce(string id)
        {
            Assert.That(game.MofuPoints.TryRewardTalk(Day, id), Is.True);
            Assert.That(game.MofuPoints.Balance, Is.EqualTo(28)); Assert.That(game.MofuPoints.TownActionTotal, Is.EqualTo(4));
            Assert.That(state.economy.talkRewardCharacterIds, Is.EqualTo(new[] { id })); Assert.That(store.Writes, Is.EqualTo(1));
            Assert.That(state.economy.day, Is.EqualTo(Day)); Assert.That(state.economy.rewardsStartDay, Is.EqualTo(Day));
        }
        [Test] public void SameCharacterNeverRewardsOrSavesTwice()
        {
            Reward("talk"); string before = StateCodec.Encode(state);
            Assert.That(Reward("talk"), Is.False); Assert.That(store.Writes, Is.EqualTo(1)); Assert.That(StateCodec.Encode(state), Is.EqualTo(before));
        }
        [Test] public void OnlyThreeDistinctCharactersPayTwelve()
        {
            foreach (string id in new[] { "momo", "lulu", "noah" }) Assert.That(game.MofuPoints.TryRewardTalk(Day, id), Is.True);
            string before = StateCodec.Encode(state); Assert.That(game.MofuPoints.TryRewardTalk(Day, "pom"), Is.False);
            Assert.That(game.MofuPoints.Balance, Is.EqualTo(36)); Assert.That(game.MofuPoints.TownActionTotal, Is.EqualTo(12));
            Assert.That(store.Writes, Is.EqualTo(3)); Assert.That(StateCodec.Encode(state), Is.EqualTo(before));
        }
        [TestCase(null)] [TestCase("")] [TestCase("noa")] [TestCase("unknown")]
        public void InvalidCharacterDoesNotAdvanceDayOrSave(string id)
        {
            string before = StateCodec.Encode(state); Assert.That(game.MofuPoints.TryRewardTalk(Next, id), Is.False);
            Assert.That(StateCodec.Encode(state), Is.EqualTo(before)); Assert.That(store.Writes, Is.Zero);
        }
        [Test] public void TwoNormalWalksPayTwelveAndThirdIsFree()
        {
            Assert.That(game.MofuPoints.TryRewardNormalWalk(Day, Day, "walk-1"), Is.True);
            Assert.That(game.MofuPoints.TryRewardNormalWalk(Day, Day, "walk-2"), Is.True);
            string before = StateCodec.Encode(state); Assert.That(game.MofuPoints.TryRewardNormalWalk(Day, Day, "walk-3"), Is.False);
            Assert.That(game.MofuPoints.Balance, Is.EqualTo(36)); Assert.That(game.MofuPoints.TownActionTotal, Is.EqualTo(12));
            Assert.That(state.economy.normalWalkRewards, Is.EqualTo(2)); Assert.That(state.economy.walkRewardReceipts.Count, Is.EqualTo(2));
            Assert.That(store.Writes, Is.EqualTo(2)); Assert.That(StateCodec.Encode(state), Is.EqualTo(before));
        }
        [Test] public void DuplicateWalkIdNeverRewardsOrSavesTwice()
        {
            Reward("walk"); string before = StateCodec.Encode(state); Assert.That(Reward("walk"), Is.False);
            Assert.That(store.Writes, Is.EqualTo(1)); Assert.That(StateCodec.Encode(state), Is.EqualTo(before));
        }
        [TestCase(null)] [TestCase("")] [TestCase(" ")]
        public void InvalidWalkIdDoesNotAdvanceOrSave(string id)
        {
            string before = StateCodec.Encode(state); Assert.That(game.MofuPoints.TryRewardNormalWalk(Next, Next, id), Is.False);
            Assert.That(StateCodec.Encode(state), Is.EqualTo(before)); Assert.That(store.Writes, Is.Zero);
        }
        [Test] public void OversizedWalkIdIsRejectedWithoutMutation()
        { string before = StateCodec.Encode(state); Assert.That(game.MofuPoints.TryRewardNormalWalk(Next, Next, new string('x',129)), Is.False); Assert.That(StateCodec.Encode(state), Is.EqualTo(before)); Assert.That(store.Writes, Is.Zero); }
        [TestCase("2026-11-05")] [TestCase(Next)] [TestCase("")]
        public void WalkFromAnotherDayCannotConsumeNewDaysSlot(string start)
        {
            string before = StateCodec.Encode(state); Assert.That(game.MofuPoints.TryRewardNormalWalk(Day, start, "walk"), Is.False);
            Assert.That(StateCodec.Encode(state), Is.EqualTo(before)); Assert.That(store.Writes, Is.Zero);
        }
        [Test] public void FlowerPaysEightOnlyOncePerDay()
        {
            Assert.That(Reward("flower"), Is.True); Assert.That(Reward("flower"), Is.False);
            Assert.That(game.MofuPoints.Balance, Is.EqualTo(32)); Assert.That(game.MofuPoints.TownActionTotal, Is.EqualTo(8));
            Assert.That(state.economy.lastFlowerRewardDay, Is.EqualTo(Day)); Assert.That(store.Writes, Is.EqualTo(1));
        }
        [TestCase("talk",94)] [TestCase("walk",94)] [TestCase("flower",94)]
        [TestCase("talk",96)] [TestCase("walk",96)] [TestCase("flower",96)]
        public void WalletCapNeverReducesFaceValueTownProgress(string kind, int balance)
        {
            state.economy.mofuPoints = balance; Assert.That(Reward(kind), Is.True);
            Assert.That(game.MofuPoints.Balance, Is.EqualTo(96)); Assert.That(game.MofuPoints.TownActionTotal, Is.EqualTo(Value(kind)));
            Assert.That(Reward(kind), Is.False); Assert.That(store.Writes, Is.EqualTo(1));
        }
        [TestCase("talk")] [TestCase("walk")] [TestCase("flower")]
        public void SameDayMaintainsLedgerNextDayResetsAndClockBackCannotReward(string kind)
        {
            Reward(kind); string same = StateCodec.Encode(state); Assert.That(Reward(kind), Is.False); Assert.That(StateCodec.Encode(state), Is.EqualTo(same));
            Assert.That(Reward(kind, Next), Is.True); string future = StateCodec.Encode(state);
            Assert.That(Reward(kind, Day), Is.False); Assert.That(StateCodec.Encode(state), Is.EqualTo(future));
            Assert.That(game.MofuPoints.Balance, Is.EqualTo(24 + 2 * Value(kind))); Assert.That(game.MofuPoints.TownActionTotal, Is.EqualTo(2 * Value(kind)));
            Assert.That(state.economy.rewardsStartDay, Is.EqualTo(Day)); Assert.That(store.Writes, Is.EqualTo(2));
        }
        [Test] public void NewDayResetsOnlyDailyLedgerAndNeverGrantsRecoveryExtras()
        {
            game.MofuPoints.TryApplyDay(Day); game.MofuPoints.TryRecordTalk(Day,"noah");
            game.MofuPoints.TryRecordWalk(Day,"extra",WalkRewardKind.Additional); game.MofuPoints.TryRecordWalk(Day,"help",WalkRewardKind.Help);
            state.economy.lastRecoveryDay = Day; state.coins=55; state.gems=7; state.stamina=30; state.economy.townActionTotal=30;
            int writes=store.Writes; Assert.That(Reward("talk",Next), Is.True);
            Assert.That(state.economy.additionalWalkRewards, Is.Zero); Assert.That(state.economy.helpWalkClaimed, Is.False);
            Assert.That(state.economy.walkRewardReceipts, Is.Empty); Assert.That(state.economy.talkRewardCharacterIds, Is.EqualTo(new[]{"momo"}));
            Assert.That(state.economy.lastRecoveryDay, Is.EqualTo(Day)); Assert.That(state.coins, Is.EqualTo(55)); Assert.That(state.gems, Is.EqualTo(7)); Assert.That(state.stamina, Is.EqualTo(30));
            Assert.That(game.MofuPoints.Balance, Is.EqualTo(28)); Assert.That(game.MofuPoints.TownActionTotal, Is.EqualTo(34)); Assert.That(store.Writes, Is.EqualTo(writes+1));
        }
        [Test] public void AllNormalDailySourcesTotalThirtyTwoWithoutAutomaticRecovery()
        {
            foreach(string id in new[]{"momo","lulu","noah"})game.MofuPoints.TryRewardTalk(Day,id);
            game.MofuPoints.TryRewardNormalWalk(Day,Day,"one");game.MofuPoints.TryRewardNormalWalk(Day,Day,"two");Reward("flower");
            Assert.That(game.MofuPoints.Balance, Is.EqualTo(56)); Assert.That(game.MofuPoints.TownActionTotal, Is.EqualTo(32));
            Assert.That(state.economy.lastRecoveryDay, Is.Empty); Assert.That(state.economy.additionalWalkRewards, Is.Zero); Assert.That(state.economy.helpWalkClaimed, Is.False);
            Assert.That(store.Writes, Is.EqualTo(6));
        }
        [TestCase(null)] [TestCase("")] [TestCase("2026-02-30")] [TestCase("2026-1-6")]
        public void InvalidDateNeverMutatesOrSaves(string day)
        { string before=StateCodec.Encode(state);Assert.That(game.MofuPoints.TryRewardTalk(day,"momo"),Is.False);Assert.That(StateCodec.Encode(state),Is.EqualTo(before));Assert.That(store.Writes,Is.Zero); }
        [TestCase("talk")] [TestCase("walk")] [TestCase("flower")]
        public void CumulativeOverflowRejectsWholeRewardBeforeNewDayReset(string kind)
        {
            state.economy.townActionTotal=long.MaxValue-Value(kind)+1; string before=StateCodec.Encode(state);
            Assert.That(Reward(kind),Is.False);Assert.That(StateCodec.Encode(state),Is.EqualTo(before));Assert.That(store.Writes,Is.Zero);
        }
        [TestCase("talk",false)] [TestCase("walk",false)] [TestCase("flower",false)]
        [TestCase("talk",true)] [TestCase("walk",true)] [TestCase("flower",true)]
        public void FailedSaveRestoresWalletTotalWholeLedgerAndOtherState(string kind,bool denied)
        {
            game.MofuPoints.TryApplyDay(Day);game.MofuPoints.TryRecordTalk(Day,"noah");game.MofuPoints.TryRecordWalk(Day,"old",WalkRewardKind.Normal);
            state.coins=81;Assert.That(game.TryMerge(0,1),Is.True);string before=StateCodec.Encode(state);int writes=store.Writes;store.Fail=true;store.Denied=denied;
            Assert.That(Reward(kind,Next),Is.False);Assert.That(StateCodec.Encode(state),Is.EqualTo(before));Assert.That(game.Board.IsLocked(0),Is.True);
            Assert.That(store.Writes,Is.EqualTo(writes+1));Assert.That(game.LastError,Is.Not.Empty);
        }
        [TestCase("talk")] [TestCase("walk")] [TestCase("flower")]
        public void SaveReentryCannotDoubleRewardEvenAnotherSource(string kind)
        {
            bool? same=null,other=null;
            store.DuringSave=()=>{store.DuringSave=null;same=Reward(kind);other=Reward(kind=="talk"?"walk":"talk");};
            Assert.That(Reward(kind),Is.True);Assert.That(same,Is.False);Assert.That(other,Is.False);
            Assert.That(store.Writes,Is.EqualTo(1));Assert.That(game.MofuPoints.Balance,Is.EqualTo(24+Value(kind)));Assert.That(game.MofuPoints.TownActionTotal,Is.EqualTo(Value(kind)));
        }
        [Test] public void ExistingRawCreditStillDoesNotAddTownActionProgress()
        { int credited;Assert.That(game.MofuPoints.TryAdd(4,out credited),Is.True);Assert.That(credited,Is.EqualTo(4));Assert.That(game.MofuPoints.TownActionTotal,Is.Zero);Assert.That(game.MofuPoints.TrySpend(2),Is.True);Assert.That(game.MofuPoints.Balance,Is.EqualTo(26)); }
        [Test] public void JournalConversationSuccessPaysButRepeatedConversationRemainsPossible()
        {
            Assert.That(daily.RecordTalk("noah",Today),Is.True);Assert.That(daily.RecordTalk("noah",Today),Is.True);
            Assert.That(daily.Data.Characters.Single(c=>c.Id=="noah").Meetings,Is.EqualTo(2));Assert.That(game.MofuPoints.Balance,Is.EqualTo(28));Assert.That(store.Writes,Is.EqualTo(1));
        }
        [Test] public void JournalOneWalkGoalDoesNotBlockSecondPointReward()
        {
            Assert.That(daily.RecordWalk("first",Day,Today),Is.True);daily.RecordWalk("second",Day,Today);daily.RecordWalk("third",Day,Today);daily.RecordWalk("second",Day,Today);
            Assert.That(daily.Data.Walks,Is.EqualTo(1));Assert.That(journal.Writes,Is.EqualTo(1));Assert.That(store.Writes,Is.EqualTo(2));Assert.That(game.MofuPoints.Balance,Is.EqualTo(36));
        }
        [Test] public void JournalFlowerMustBeSavedBeforePointsAndSameDayDoesNotSaveTwice()
        {
            ReadyFlower();int writes=store.Writes;Assert.That(daily.Claim(Today),Is.True);int journalWrites=journal.Writes;Assert.That(daily.Claim(Today),Is.False);
            Assert.That(daily.Data.DecorationDays,Does.Contain(Day));Assert.That(state.economy.lastFlowerRewardDay,Is.EqualTo(Day));Assert.That(store.Writes,Is.EqualTo(writes+1));Assert.That(journal.Writes,Is.EqualTo(journalWrites));
        }
        [TestCase("talk")] [TestCase("walk")] [TestCase("flower")]
        public void JournalSaveFailureNeverPaysPoints(string kind)
        {
            if(kind=="flower")ReadyFlower();int writes=store.Writes;string before=StateCodec.Encode(state);journal.Fail=true;
            if(kind=="talk")daily.RecordTalk("noah",Today);else if(kind=="walk")daily.RecordWalk("one",Day,Today);else daily.Claim(Today);
            Assert.That(store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(state),Is.EqualTo(before));Assert.That(daily.LastError,Is.Not.Empty);
        }
        [TestCase("talk")] [TestCase("walk")] [TestCase("flower")]
        public void MainSaveFailureLeavesJournalSuccessButRollsBackAllPointsWithoutRetry(string kind)
        {
            if(kind=="flower")ReadyFlower();string before=StateCodec.Encode(state);int writes=store.Writes;store.Fail=true;
            if(kind=="talk")daily.RecordTalk("noah",Today);else if(kind=="walk")daily.RecordWalk("one",Day,Today);else daily.Claim(Today);
            Assert.That(StateCodec.Encode(state),Is.EqualTo(before));Assert.That(store.Writes,Is.EqualTo(writes+1));
            Assert.That(kind=="talk"?daily.Data.Talks:kind=="walk"?daily.Data.Walks:daily.Data.Claimed?1:0,Is.EqualTo(1));
        }
        [Test] public void StoredCurrentFlowerRecoversOnceOnExplicitStartupAfterMainSaveFailure()
        {
            ReadyFlower();store.Fail=true;daily.Claim(Today);store.Fail=false;int writes=store.Writes;
            daily=new TownDailyService(journal,Today,game.MofuPoints);Assert.That(store.Writes,Is.EqualTo(writes+1));Assert.That(state.economy.lastFlowerRewardDay,Is.EqualTo(Day));
            int balance=game.MofuPoints.Balance;daily=new TownDailyService(journal,Today,game.MofuPoints);Assert.That(store.Writes,Is.EqualTo(writes+1));Assert.That(game.MofuPoints.Balance,Is.EqualTo(balance));
        }
        [Test] public void HistoricalFlowerCannotBeConvertedOnLaterStartup()
        {
            var old=new TownDailyService(journal,Today);old.RecordTalk("momo");old.RecordWalk();old.RecordMerge();old.RecordMerge();old.RecordMerge();old.Claim();
            daily=new TownDailyService(journal,Today.AddDays(1),game.MofuPoints);Assert.That(store.Writes,Is.Zero);Assert.That(game.MofuPoints.Balance,Is.EqualTo(24));
        }
        [Test] public void FutureJournalWithClockBackNeverPaysConversationOrFlower()
        {
            var future=new TownDailyService(journal,Today.AddDays(1));future.RecordTalk("momo");future.RecordWalk();future.RecordMerge();future.RecordMerge();future.RecordMerge();future.Claim(Today.AddDays(1));
            daily=new TownDailyService(journal,Today,game.MofuPoints);daily.RecordTalk("noah",Today);daily.RecordWalk("one",Day,Today);daily.Claim(Today);
            Assert.That(store.Writes,Is.Zero);Assert.That(game.MofuPoints.Balance,Is.EqualTo(24));Assert.That(state.economy.day,Is.Empty);
        }
        [Test] public void JournalReentryFromMainSaveDoesNotRecordOrRewardAnotherConversation()
        {
            bool? duplicate=null;store.DuringSave=()=>{store.DuringSave=null;duplicate=daily.RecordTalk("lulu",Today);};daily.RecordTalk("noah",Today);
            Assert.That(duplicate,Is.False);Assert.That(daily.Data.Characters.Single(c=>c.Id=="lulu").Meetings,Is.Zero);Assert.That(store.Writes,Is.EqualTo(1));
        }
        [Test] public void OneFingerGestureKeepsOneTicketAndNewGuideReplacesIt()
        {
            var m=new TownWorldWalkModel(0,"momo",()=>Today);Assert.That(m.FollowFinger(m.Hero.Position),Is.True);string first=m.WalkId;
            Assert.That(first,Is.Not.Empty);Assert.That(m.WalkStartDay,Is.EqualTo(Day));m.FollowFinger(m.Hero.Position);Assert.That(m.WalkId,Is.EqualTo(first));
            Assert.That(m.TryGuideDistrict(TownDistrictId.West),Is.True);Assert.That(m.WalkId,Is.Not.EqualTo(first));
        }
        [TestCase("cancel")] [TestCase("restore")] [TestCase("fast")]
        public void InterruptedWalkTicketCannotBeReused(string how)
        {
            var m=new TownWorldWalkModel(0,"momo",()=>Today);m.TryGuideDistrict(TownDistrictId.West);
            if(how=="cancel")m.CancelHero();else if(how=="restore")m.Restore("momo",m.Hero.Position);else m.FastTravel(TownDistrictId.East);
            Assert.That(m.WalkId,Is.Empty);Assert.That(m.WalkStartDay,Is.Empty);Assert.That(store.Writes,Is.Zero);
        }
        [TestCase(false)] [TestCase(true)]
        public void RealCrossDistrictCompletionPaysOnceAndOvernightTicketDoesNotPay(bool overnight)
        {
            DateTime clock=Today;var m=new TownWorldWalkModel(0,"momo",()=>clock);int arrivals=0;
            m.Arrived+=(id,place,walked)=>{if(id=="momo"&&walked){arrivals++;daily.EnsureDay(clock);daily.RecordWalk(m.WalkId,m.WalkStartDay,clock);}};
            m.TryGuideDistrict(TownDistrictId.West);if(overnight)clock=clock.AddDays(1);
            for(int i=0;i<2400&&m.Hero.IsWalking;i++)m.Step(1f/30);
            Assert.That(m.Hero.IsWalking,Is.False);Assert.That(arrivals,Is.EqualTo(1));Assert.That(m.WalkId,Is.Empty);
            Assert.That(store.Writes,Is.EqualTo(overnight?0:1));Assert.That(game.MofuPoints.Balance,Is.EqualTo(overnight?24:30));
        }
        private sealed class PointStore:IStateStore
        {
            public int Writes;public bool Fail,Denied;public Action DuringSave;
            public void Save(GameState s){Writes++;DuringSave?.Invoke();if(Fail){s.coins=0;s.economy.mofuPoints=0;s.economy.townActionTotal=999;s.economy.talkRewardCharacterIds.Clear();if(Denied)throw new UnauthorizedAccessException("Expected denial");throw new IOException("Expected failure");}StateCodec.Validate(s);}
            public LoadResult Load()=>throw new InvalidOperationException();public void ResetSave()=>throw new InvalidOperationException();
        }
        private sealed class JournalStore:ITownDailyStore
        {
            private TownDailyData saved=new TownDailyData();public int Writes;public bool Fail;
            public TownDailyData Load()=>TownDailyCodec.Clone(saved);
            public void Save(TownDailyData d){Writes++;if(Fail)throw new IOException("Expected journal failure");TownDailyCodec.Validate(d);saved=TownDailyCodec.Clone(d);}
        }
    }
}
