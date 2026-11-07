using System;
using System.IO;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;

namespace Mofumachi.Tests
{
    public sealed class RecoveryExtraHelpTests
    {
        private const string Day="2026-11-07", Next="2026-11-08", Past="2026-11-06";
        private static readonly DateTime Today=new DateTime(2026,11,7);
        private GameState state;private GameStateManager game;private PointStore store;private TownDailyService daily;
        private MofuPointService Points=>game.MofuPoints;
        [SetUp] public void SetUp()
        {
            state=GameState.CreateInitial();store=new PointStore();game=new GameStateManager(state,store);
            daily=new TownDailyService(new MemoryTownDailyStore(),Today,Points);
        }
        private bool Reward(string kind,string day=Day,string id="test-walk")=>kind=="recovery"?Points.TryRewardDailyRecovery(day):kind=="extra"?Points.TryRewardAdditionalWalk(day,day,id):Points.TryRewardHelpWalk(day,day,id);
        private static int Amount(string kind)=>kind=="recovery"?16:kind=="extra"?2:4;
        private void ReadyHelp(string day=Day,int balance=0,string missing=null)
        {
            foreach(string id in (missing=="talk"?new[]{"momo","lulu"}:new[]{"momo","lulu","noah"}))Assert.That(Points.TryRewardTalk(day,id),Is.True);
            Assert.That(Points.TryRewardNormalWalk(day,day,"normal-1"),Is.True);
            if(missing!="normal")Assert.That(Points.TryRewardNormalWalk(day,day,"normal-2"),Is.True);
            if(missing!="flower")Assert.That(Points.TryRewardFlower(day),Is.True);
            if(missing!="recovery")Assert.That(Points.TryRewardDailyRecovery(day),Is.True);
            if(Points.Balance>balance)Assert.That(Points.TrySpend(Points.Balance-balance),Is.True);
            else if(Points.Balance<balance){int credited;Assert.That(Points.TryAdd(balance-Points.Balance,out credited),Is.True);}
            if(missing=="used"){Assert.That(Points.TryRewardHelpWalk(day,day,"used-help"),Is.True);Assert.That(Points.TrySpend(4),Is.True);}
            store.Writes=0;
        }
        [Test] public void Initial24AndFirstRecovery16AreSeparateAndNeverAddTownProgress()
        {
            Assert.That(Points.Balance,Is.EqualTo(24));Assert.That(state.economy.lastRecoveryDay,Is.Empty);
            Assert.That(Reward("recovery"),Is.True);Assert.That(Points.Balance,Is.EqualTo(40));Assert.That(Points.TownActionTotal,Is.Zero);
            Assert.That(state.economy.lastRecoveryDay,Is.EqualTo(Day));Assert.That(store.Writes,Is.EqualTo(1));Assert.That(state.mergeBoard.Count,Is.EqualTo(2));
        }
        [Test] public void DuplicateRecoveryDoesNotMutateOrSave()
        {Reward("recovery");string before=StateCodec.Encode(state);Assert.That(Reward("recovery"),Is.False);Assert.That(store.Writes,Is.EqualTo(1));Assert.That(StateCodec.Encode(state),Is.EqualTo(before));}
        [TestCase(0)] [TestCase(90)] [TestCase(95)] [TestCase(96)]
        public void RecoveryClampsWalletAndConsumesFullWalletReceipt(int balance)
        {
            state.economy.mofuPoints=balance;state.economy.townActionTotal=42;
            Assert.That(Reward("recovery"),Is.True);Assert.That(Points.Balance,Is.EqualTo(Math.Min(96,balance+16)));Assert.That(Points.TownActionTotal,Is.EqualTo(42));
            Assert.That(state.economy.lastRecoveryDay,Is.EqualTo(Day));Assert.That(Reward("recovery"),Is.False);Assert.That(store.Writes,Is.EqualTo(1));
        }
        [Test] public void RecoveryNextDayWorksButOldDateAndClockBackNeverReopenIt()
        {
            Reward("recovery");Assert.That(Reward("recovery",Next),Is.True);string before=StateCodec.Encode(state);
            Assert.That(Reward("recovery",Day),Is.False);Assert.That(Reward("recovery",Next),Is.False);Assert.That(Reward("recovery",Past),Is.False);
            Assert.That(StateCodec.Encode(state),Is.EqualTo(before));Assert.That(Points.Balance,Is.EqualTo(56));Assert.That(store.Writes,Is.EqualTo(2));
        }
        [TestCase(0)] [TestCase(2)]
        public void ExtraHasTwoIndependentSlotsRegardlessOfNormalQuota(int normal)
        {
            for(int i=0;i<normal;i++)Assert.That(Points.TryRewardNormalWalk(Day,Day,"n"+i),Is.True);
            long total=Points.TownActionTotal;int before=Points.Balance;store.Writes=0;
            Assert.That(Points.TryRewardAdditionalWalk(Day,Day,"a"),Is.True);Assert.That(Points.TryRewardAdditionalWalk(Day,Day,"b"),Is.True);
            string snapshot=StateCodec.Encode(state);Assert.That(Points.TryRewardAdditionalWalk(Day,Day,"c"),Is.False);
            Assert.That(Points.Balance,Is.EqualTo(before+4));Assert.That(Points.TownActionTotal,Is.EqualTo(total));Assert.That(state.economy.normalWalkRewards,Is.EqualTo(normal));
            Assert.That(state.economy.additionalWalkRewards,Is.EqualTo(2));Assert.That(store.Writes,Is.EqualTo(2));Assert.That(StateCodec.Encode(state),Is.EqualTo(snapshot));
        }
        [Test] public void ExtraReceiptDuplicateDoesNotConsumeSecondSlotOrSave()
        {
            Reward("extra");string before=StateCodec.Encode(state);Assert.That(Reward("extra"),Is.False);Assert.That(StateCodec.Encode(state),Is.EqualTo(before));
            Assert.That(state.economy.additionalWalkRewards,Is.EqualTo(1));Assert.That(store.Writes,Is.EqualTo(1));
        }
        [TestCase(94)] [TestCase(95)] [TestCase(96)]
        public void ExtraCapStillConsumesBothReceiptsWithoutTownProgress(int balance)
        {
            state.economy.mofuPoints=balance;state.economy.townActionTotal=35;
            Assert.That(Reward("extra",Day,"a"),Is.True);Assert.That(Reward("extra",Day,"b"),Is.True);Assert.That(Reward("extra",Day,"c"),Is.False);
            Assert.That(Points.Balance,Is.EqualTo(96));Assert.That(Points.TownActionTotal,Is.EqualTo(35));Assert.That(state.economy.additionalWalkRewards,Is.EqualTo(2));Assert.That(store.Writes,Is.EqualTo(2));
        }
        [TestCase(WalkRewardKind.Normal)] [TestCase(WalkRewardKind.Additional)] [TestCase(WalkRewardKind.Help)]
        public void OneSuccessfulWalkIdCannotPayAnotherKind(WalkRewardKind kind)
        {
            if(kind==WalkRewardKind.Help)ReadyHelp();
            Assert.That(Points.TryRewardWalk(Day,Day,"shared",kind),Is.True);string before=StateCodec.Encode(state);int writes=store.Writes;
            foreach(var other in new[]{WalkRewardKind.Normal,WalkRewardKind.Additional,WalkRewardKind.Help})Assert.That(Points.TryRewardWalk(Day,Day,"shared",other),Is.False);
            Assert.That(StateCodec.Encode(state),Is.EqualTo(before));Assert.That(store.Writes,Is.EqualTo(writes));Assert.That(state.economy.walkRewardReceipts.Single(r=>r.walkId=="shared").kind,Is.EqualTo(kind));
        }
        [TestCase(0)] [TestCase(1)]
        public void HelpAtZeroOrOnePaysFourOnceAndNeverAddsTownProgress(int balance)
        {
            ReadyHelp(balance:balance);long total=Points.TownActionTotal;
            Assert.That(Reward("help"),Is.True);Assert.That(Points.Balance,Is.EqualTo(balance+4));Assert.That(Points.TownActionTotal,Is.EqualTo(total));
            Assert.That(state.economy.helpWalkClaimed,Is.True);Assert.That(state.economy.walkRewardReceipts.Last().kind,Is.EqualTo(WalkRewardKind.Help));
            Assert.That(Reward("help",Day,"other"),Is.False);Assert.That(store.Writes,Is.EqualTo(1));
        }
        [TestCase(2)] [TestCase(3)] [TestCase(96)]
        public void HelpAtTwoOrMoreNeverMutatesOrSaves(int balance)
        {
            ReadyHelp(balance:balance);string before=StateCodec.Encode(state);Assert.That(Reward("help"),Is.False);Assert.That(StateCodec.Encode(state),Is.EqualTo(before));Assert.That(store.Writes,Is.Zero);
        }
        [TestCase("talk")] [TestCase("normal")] [TestCase("flower")] [TestCase("recovery")] [TestCase("used")]
        public void EachMissingHelpPreconditionRejectsEntireReward(string missing)
        {ReadyHelp(missing:missing);string before=StateCodec.Encode(state);Assert.That(Reward("help"),Is.False);Assert.That(StateCodec.Encode(state),Is.EqualTo(before));Assert.That(store.Writes,Is.Zero);}
        [Test] public void HelpCannotBeRepeatedAfterSpendingAndExtraAlsoStopsAtTwo()
        {
            ReadyHelp();long total=Points.TownActionTotal;Assert.That(Reward("help"),Is.True);
            Assert.That(game.TryGenerateLv1("tea"),Is.True);Assert.That(game.TryGenerateLv1("tea"),Is.True);Assert.That(Points.Balance,Is.Zero);
            Assert.That(Reward("help",Day,"help-again"),Is.False);
            foreach(string id in new[]{"extra-1","extra-2"}){Assert.That(Reward("extra",Day,id),Is.True);Assert.That(game.TryGenerateLv1("bread"),Is.True);}
            Assert.That(Points.Balance,Is.Zero);int writes=store.Writes;string before=StateCodec.Encode(state);
            Assert.That(Reward("help",Day,"help-third"),Is.False);Assert.That(Reward("extra",Day,"extra-third"),Is.False);Assert.That(Reward("recovery"),Is.False);
            Assert.That(Points.SelectWalkRewardKind(Day),Is.Null);Assert.That(Points.TownActionTotal,Is.EqualTo(total));Assert.That(store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(state),Is.EqualTo(before));
            Assert.That(state.economy.walkRewardReceipts.Count,Is.EqualTo(5));
        }
        [Test] public void HelpNeedsTodaysFlowerAndRecoveryNotHistoricalDates()
        {
            ReadyHelp();state.economy.lastFlowerRewardDay=Past;Assert.That(Reward("help"),Is.False);state.economy.lastFlowerRewardDay=Day;state.economy.lastRecoveryDay=Past;
            Assert.That(Reward("help"),Is.False);Assert.That(store.Writes,Is.Zero);Assert.That(state.economy.helpWalkClaimed,Is.False);
        }
        [Test] public void NextDayRequiresAllPrerequisitesAgainBeforeHelpCanPay()
        {
            ReadyHelp();Reward("help");Assert.That(Reward("help",Next,"early"),Is.False);
            ReadyHelp(Next);long total=Points.TownActionTotal;Assert.That(Reward("help",Next,"next-help"),Is.True);Assert.That(Points.Balance,Is.EqualTo(4));Assert.That(Points.TownActionTotal,Is.EqualTo(total));
            string before=StateCodec.Encode(state);Assert.That(Reward("help",Day,"back"),Is.False);Assert.That(StateCodec.Encode(state),Is.EqualTo(before));Assert.That(store.Writes,Is.EqualTo(1));
        }
        [TestCase("recovery")] [TestCase("extra")] [TestCase("help")]
        public void LongMaxTownTotalDoesNotBlockNonGrowthRewards(string kind)
        {
            if(kind=="help")ReadyHelp();state.economy.townActionTotal=long.MaxValue;
            int before=Points.Balance;Assert.That(Reward(kind),Is.True);Assert.That(Points.Balance,Is.EqualTo(before+Amount(kind)));Assert.That(Points.TownActionTotal,Is.EqualTo(long.MaxValue));Assert.That(store.Writes,Is.EqualTo(1));
        }
        [Test] public void NormalSourcesKeepTheirExactFaceValueAfterRecoveryAndExtras()
        {
            Reward("recovery");Reward("extra",Day,"extra-first");long total=Points.TownActionTotal;
            Assert.That(Points.TryRewardTalk(Day,"momo"),Is.True);Assert.That(Points.TryRewardNormalWalk(Day,Day,"normal"),Is.True);Assert.That(Points.TryRewardFlower(Day),Is.True);
            Assert.That(Points.TownActionTotal,Is.EqualTo(total+18));Assert.That(Points.Balance,Is.EqualTo(24+16+2+4+6+8));
        }
        [TestCase("recovery",false)] [TestCase("extra",false)] [TestCase("help",false)]
        [TestCase("recovery",true)] [TestCase("extra",true)] [TestCase("help",true)]
        public void SaveFailureRestoresAllReceiptsWalletDayAndOtherGameState(string kind,bool denied)
        {
            if(kind=="help")ReadyHelp();else{Points.TryApplyDay(Past);Points.TryRecordTalk(Past,"noah");Points.TryRecordWalk(Past,"old",WalkRewardKind.Additional);Points.TryRecordDailyRecovery(Past);}
            state.coins=73;Assert.That(game.TryMerge(0,1),Is.True);string before=StateCodec.Encode(state);store.Writes=0;store.Fail=true;store.Denied=denied;
            Assert.That(Reward(kind),Is.False);Assert.That(StateCodec.Encode(state),Is.EqualTo(before));Assert.That(game.Board.IsLocked(0),Is.True);Assert.That(game.Board.IsLocked(1),Is.True);
            Assert.That(store.Writes,Is.EqualTo(1));Assert.That(game.LastError,Is.Not.Empty);
        }
        [TestCase("recovery")] [TestCase("extra")] [TestCase("help")]
        public void SaveReentryCannotPaySameOrDifferentRewardAgain(string kind)
        {
            if(kind=="help")ReadyHelp();int balance=Points.Balance;long total=Points.TownActionTotal;bool? same=null,other=null;
            store.DuringSave=()=>{store.DuringSave=null;same=Reward(kind);other=Reward("extra",Day,"reentrant-new");};
            Assert.That(Reward(kind),Is.True);Assert.That(same,Is.False);Assert.That(other,Is.False);Assert.That(store.Writes,Is.EqualTo(1));
            Assert.That(Points.Balance,Is.EqualTo(balance+Amount(kind)));Assert.That(Points.TownActionTotal,Is.EqualTo(total));
        }
        [TestCase("extra")] [TestCase("help")]
        public void WalkFromAnotherDateNeverResetsOrConsumesReceipts(string kind)
        {
            if(kind=="help")ReadyHelp();string before=StateCodec.Encode(state);
            bool result=kind=="extra"?Points.TryRewardAdditionalWalk(Next,Day,"old-ticket"):Points.TryRewardHelpWalk(Next,Day,"old-ticket");
            Assert.That(result,Is.False);Assert.That(StateCodec.Encode(state),Is.EqualTo(before));Assert.That(store.Writes,Is.Zero);
        }
        [TestCase(null)] [TestCase("")] [TestCase("2026-02-30")] [TestCase("2026-1-7")]
        public void MalformedDatesDoNotMutateOrSaveAnyNewReward(string day)
        {
            string before=StateCodec.Encode(state);foreach(string kind in new[]{"recovery","extra","help"})Assert.That(Reward(kind,day),Is.False);
            Assert.That(StateCodec.Encode(state),Is.EqualTo(before));Assert.That(store.Writes,Is.Zero);
        }
        [TestCase(null)] [TestCase("")] [TestCase(" ")]
        public void InvalidWalkIdDoesNotPayExtraOrHelp(string id)
        {
            ReadyHelp();string before=StateCodec.Encode(state);Assert.That(Reward("extra",Day,id),Is.False);Assert.That(Reward("help",Day,id),Is.False);
            Assert.That(StateCodec.Encode(state),Is.EqualTo(before));Assert.That(store.Writes,Is.Zero);
        }
        [Test] public void OversizedWalkIdAndUnknownKindNeverResetOrSave()
        {
            string before=StateCodec.Encode(state);Assert.That(Reward("extra",Next,new string('x',129)),Is.False);
            Assert.That(Points.TryRewardWalk(Next,Next,"unknown",(WalkRewardKind)999),Is.False);Assert.That(StateCodec.Encode(state),Is.EqualTo(before));Assert.That(store.Writes,Is.Zero);
        }
        [Test] public void SameDayMaintainsExtraLedgerNextDayResetsButPastDayDoesNot()
        {
            Reward("extra",Day,"a");Reward("extra",Day,"b");Assert.That(Reward("extra",Day,"c"),Is.False);
            Assert.That(Reward("extra",Next,"a"),Is.True);Assert.That(state.economy.additionalWalkRewards,Is.EqualTo(1));Assert.That(state.economy.walkRewardReceipts.Count,Is.EqualTo(1));
            string before=StateCodec.Encode(state);Assert.That(Reward("extra",Day,"back"),Is.False);Assert.That(StateCodec.Encode(state),Is.EqualTo(before));Assert.That(store.Writes,Is.EqualTo(3));Assert.That(Points.TownActionTotal,Is.Zero);
        }
        [Test] public void RecoveryNextDayResetsOnlyDailyFieldsWithoutResettingWalletOrProgress()
        {
            ReadyHelp();Reward("help");Reward("extra",Day,"extra");state.coins=80;long total=Points.TownActionTotal;int balance=Points.Balance;
            Assert.That(Reward("recovery",Next),Is.True);Assert.That(Points.Balance,Is.EqualTo(balance+16));Assert.That(Points.TownActionTotal,Is.EqualTo(total));Assert.That(state.coins,Is.EqualTo(80));
            Assert.That(state.economy.talkRewardCharacterIds,Is.Empty);Assert.That(state.economy.normalWalkRewards,Is.Zero);Assert.That(state.economy.additionalWalkRewards,Is.Zero);Assert.That(state.economy.helpWalkClaimed,Is.False);Assert.That(state.economy.walkRewardReceipts,Is.Empty);Assert.That(state.economy.lastFlowerRewardDay,Is.EqualTo(Day));
        }
        [Test] public void RewardOrderDoesNotAffectWalletOrTownFaceValue()
        {
            foreach(string id in new[]{"momo","lulu","noah"})Points.TryRewardTalk(Day,id);
            Points.TryRewardNormalWalk(Day,Day,"n1");Points.TryRewardNormalWalk(Day,Day,"n2");Points.TryRewardFlower(Day);Reward("recovery");Reward("extra",Day,"a1");Reward("extra",Day,"a2");
            Assert.That(Points.Balance,Is.EqualTo(76));Assert.That(Points.TownActionTotal,Is.EqualTo(32));
        }
        [Test] public void RecoveryRequiresAnExplicitDailyEntryNotEnsureDayOrConstruction()
        {
            Assert.That(Points.Balance,Is.EqualTo(24));daily.EnsureDay(Today);daily.EnsureDay(Today);Assert.That(store.Writes,Is.Zero);
            Assert.That(daily.RecoverDay(Today),Is.True);Assert.That(daily.RecoverDay(Today),Is.False);Assert.That(Points.Balance,Is.EqualTo(40));Assert.That(store.Writes,Is.EqualTo(1));
        }
        [Test] public void DailyEntryUsesActualDateAndRefusesFutureJournalOnClockBack()
        {
            daily.EnsureDay(Today.AddDays(1));Assert.That(daily.RecoverDay(Today.AddDays(1)),Is.True);string before=StateCodec.Encode(state);
            Assert.That(daily.RecoverDay(Today),Is.False);Assert.That(StateCodec.Encode(state),Is.EqualTo(before));Assert.That(store.Writes,Is.EqualTo(1));Assert.That(Points.TownActionTotal,Is.Zero);
        }
        [Test] public void DailyEntrySaveFailureDoesNotRetryInternally()
        {
            string before=StateCodec.Encode(state);store.Fail=true;Assert.That(daily.RecoverDay(Today),Is.False);Assert.That(store.Writes,Is.EqualTo(1));Assert.That(StateCodec.Encode(state),Is.EqualTo(before));
        }
        [Test] public void SelectionIsReadOnlyAndUsesNormalHelpExtraNonePriority()
        {
            string before=StateCodec.Encode(state);Assert.That(Points.SelectWalkRewardKind(Next),Is.EqualTo(WalkRewardKind.Normal));Assert.That(StateCodec.Encode(state),Is.EqualTo(before));Assert.That(store.Writes,Is.Zero);
            ReadyHelp();Assert.That(Points.SelectWalkRewardKind(Day),Is.EqualTo(WalkRewardKind.Help));Reward("help");Assert.That(Points.SelectWalkRewardKind(Day),Is.EqualTo(WalkRewardKind.Additional));
            Reward("extra",Day,"a1");Reward("extra",Day,"a2");Assert.That(Points.SelectWalkRewardKind(Day),Is.Null);Assert.That(Points.SelectWalkRewardKind(Past),Is.Null);
        }
        [Test] public void RepeatedFingerUpdatesCannotChangeSelectedWalkKind()
        {
            var m=new TownWorldWalkModel(0,"momo",()=>Today,Points.SelectWalkRewardKind);Assert.That(m.FollowFinger(m.Hero.Position),Is.True);string id=m.WalkId;
            Assert.That(m.WalkReward,Is.EqualTo(WalkRewardKind.Normal));ReadyHelp();Assert.That(Points.SelectWalkRewardKind(Day),Is.EqualTo(WalkRewardKind.Help));
            m.FollowFinger(m.Hero.Position);Assert.That(m.WalkId,Is.EqualTo(id));Assert.That(m.WalkReward,Is.EqualTo(WalkRewardKind.Normal));m.CancelHero();Assert.That(m.WalkReward,Is.Null);
        }
        [TestCase(WalkRewardKind.Normal)] [TestCase(WalkRewardKind.Additional)] [TestCase(WalkRewardKind.Help)]
        public void ExistingHeroCompletionRoutesExactlyOneFrozenKind(WalkRewardKind kind)
        {
            if(kind==WalkRewardKind.Help)ReadyHelp();else if(kind==WalkRewardKind.Additional){Points.TryRewardNormalWalk(Day,Day,"n1");Points.TryRewardNormalWalk(Day,Day,"n2");}
            int balance=Points.Balance;long total=Points.TownActionTotal;store.Writes=0;var m=new TownWorldWalkModel(0,"momo",()=>Today,Points.SelectWalkRewardKind);
            m.Arrived+=(id,place,walked)=>{if(id=="momo"&&walked)daily.RecordWalk(m.WalkId,m.WalkStartDay,Today,m.WalkReward);};
            Assert.That(m.TryGuideDistrict(TownDistrictId.West),Is.True);Assert.That(m.WalkReward,Is.EqualTo(kind));
            for(int i=0;i<2400&&m.Hero.IsWalking;i++)m.Step(1f/30);
            Assert.That(m.Hero.IsWalking,Is.False);Assert.That(m.WalkReward,Is.Null);Assert.That(store.Writes,Is.EqualTo(1));
            Assert.That(Points.Balance,Is.EqualTo(balance+(kind==WalkRewardKind.Normal?6:kind==WalkRewardKind.Additional?2:4)));Assert.That(Points.TownActionTotal,Is.EqualTo(total+(kind==WalkRewardKind.Normal?6:0)));Assert.That(state.economy.walkRewardReceipts.Last().kind,Is.EqualTo(kind));
        }
        [Test] public void HelpConditionsLostDuringWalkNeverFallBackToExtra()
        {
            ReadyHelp();var m=new TownWorldWalkModel(0,"momo",()=>Today,Points.SelectWalkRewardKind);
            m.Arrived+=(id,place,walked)=>{if(id=="momo"&&walked)daily.RecordWalk(m.WalkId,m.WalkStartDay,Today,m.WalkReward);};
            m.TryGuideDistrict(TownDistrictId.West);Assert.That(m.WalkReward,Is.EqualTo(WalkRewardKind.Help));int credited;Points.TryAdd(2,out credited);store.Writes=0;
            for(int i=0;i<2400&&m.Hero.IsWalking;i++)m.Step(1f/30);
            Assert.That(m.Hero.IsWalking,Is.False);Assert.That(Points.Balance,Is.EqualTo(2));Assert.That(state.economy.helpWalkClaimed,Is.False);Assert.That(state.economy.additionalWalkRewards,Is.Zero);Assert.That(store.Writes,Is.Zero);Assert.That(Points.SelectWalkRewardKind(Day),Is.EqualTo(WalkRewardKind.Additional));
        }
        [Test] public void NoRewardTicketStillAllowsWalkingAndDailyGoalWithoutMainSave()
        {
            ReadyHelp();Reward("help");Reward("extra",Day,"a1");Reward("extra",Day,"a2");store.Writes=0;
            Assert.That(daily.RecordWalk("unrewarded",Day,Today,null),Is.True);Assert.That(daily.Data.Walks,Is.EqualTo(1));Assert.That(store.Writes,Is.Zero);
        }
        [Test] public void InitialUnpausedCallbackCannotRetryFailedStartupRecovery()
        {
            store.Fail=true;Assert.That(daily.RecoverDay(Today),Is.False);
            Assert.That(daily.ResumeDay(Today,false),Is.False);Assert.That(store.Writes,Is.EqualTo(1));Assert.That(Points.Balance,Is.EqualTo(24));
        }
        [Test] public void GenuineResumeRecoversOnlyOnceAndCanApplyTheNextDay()
        {
            Assert.That(daily.ResumeDay(Today,false),Is.False);Assert.That(daily.ResumeDay(Today,true),Is.True);
            Assert.That(daily.ResumeDay(Today,true),Is.False);Assert.That(daily.ResumeDay(Today.AddDays(1),true),Is.True);
            Assert.That(store.Writes,Is.EqualTo(2));Assert.That(Points.Balance,Is.EqualTo(56));Assert.That(Points.TownActionTotal,Is.Zero);
        }
        [TestCase("talk")] [TestCase("walk")] [TestCase("flower")]
        public void JournalSaveFailureBlocksFollowOnRecoveryWrite(string kind)
        {
            var badStore=new FailingJournalStore();var d=new TownDailyService(badStore,Today,Points);
            if(kind=="flower"){d.RecordTalk("momo",Today);d.RecordWalk();d.RecordMerge();d.RecordMerge();d.RecordMerge();}
            int balance=Points.Balance;store.Writes=0;badStore.Fail=true;
            if(kind=="talk")d.RecordTalk("noah",Today);else if(kind=="walk")d.RecordWalk("failed",Day,Today);else d.Claim(Today);
            Assert.That(d.LastError,Is.Not.Empty);Assert.That(d.RecoverAfterAction(Today,true),Is.False);Assert.That(store.Writes,Is.Zero);Assert.That(Points.Balance,Is.EqualTo(balance));
        }
        [Test] public void MainSaveFailureBlocksFollowOnRecoveryWrite()
        {
            store.Fail=true;Assert.That(Reward("extra"),Is.False);
            Assert.That(daily.RecoverAfterAction(Today,game.LastError.Length==0),Is.False);Assert.That(store.Writes,Is.EqualTo(1));Assert.That(Points.Balance,Is.EqualTo(24));
        }
        private sealed class FailingJournalStore:ITownDailyStore
        {
            private readonly MemoryTownDailyStore inner=new MemoryTownDailyStore();public bool Fail;
            public TownDailyData Load()=>inner.Load();
            public void Save(TownDailyData data){if(Fail)throw new IOException("Expected journal failure");inner.Save(data);}
        }
        private sealed class PointStore:IStateStore
        {
            public int Writes;public bool Fail,Denied;public Action DuringSave;
            public void Save(GameState s)
            {
                Writes++;DuringSave?.Invoke();
                if(Fail){s.coins=0;s.economy.mofuPoints=0;s.economy.townActionTotal=999;s.economy.lastRecoveryDay="";s.economy.additionalWalkRewards=0;s.economy.helpWalkClaimed=false;s.economy.walkRewardReceipts.Clear();s.economy.talkRewardCharacterIds.Clear();if(Denied)throw new UnauthorizedAccessException("Expected access denial");throw new IOException("Expected Save failure");}
                StateCodec.Validate(s);
            }
            public LoadResult Load()=>throw new InvalidOperationException();public void ResetSave()=>throw new InvalidOperationException();
        }
    }
}
