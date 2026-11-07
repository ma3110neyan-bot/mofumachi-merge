using System;
using System.IO;
using System.Linq;
using System.Text;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;

namespace Mofumachi.Tests
{
    public sealed class QuestTownLevelTests
    {
        private static readonly string[] Old={"tea-01","bread-01","flower-01","tea-02","flower-02","bread-02"};
        private static readonly string[] New={"loop-a-01","loop-a-02","loop-a-03","loop-a-04","loop-a-05","loop-a-06"};
        private static readonly string[] Mixed={"tea-01","bread-01","loop-a-01","flower-01","loop-a-02","tea-02","loop-a-03","loop-a-04","flower-02","loop-a-05","loop-a-06","bread-02"};
        private static string Requirements(QuestDefinition q)=>string.Join(",",q.Requirements.Select(r=>r.ItemId+":"+r.Level+":"+r.Count));
        private static int Units(QuestDefinition q)=>q.Requirements.Sum(r=>r.Count*(1<<(r.Level-1)));
        private static void Complete(GameState s,int count){s.completedQuestIds.Clear();s.completedQuestIds.AddRange(QuestCatalog.All.Take(count).Select(q=>q.Id));}
        private static QuestDefinition Q(string id)=>QuestCatalog.All.Single(q=>q.Id==id);
        private static void Fill(GameState s,QuestDefinition q){foreach(var r in q.Requirements)s.inventory.Add(new InventoryEntry(r.ItemId,r.Level,r.Count));}
        private static string Dir()=>Path.Combine(Path.GetTempPath(),"quest-town-"+Guid.NewGuid().ToString("N"));
        [Test] public void CatalogHasTwelveUniqueKnownIdsAndImmutableLists()
        {
            Assert.That(QuestCatalog.All.Count,Is.EqualTo(12));Assert.That(QuestCatalog.All.Select(q=>q.Id).Distinct().Count(),Is.EqualTo(12));
            Assert.That(QuestCatalog.All.Select(q=>q.Id),Is.EquivalentTo(Old.Concat(New)));Assert.That(QuestCatalog.Story.Select(q=>q.Id),Is.EqualTo(Old));
            Assert.That(QuestCatalog.Additional.Select(q=>q.Id),Is.EqualTo(New));Assert.Throws<NotSupportedException>(()=>((System.Collections.Generic.IList<QuestDefinition>)QuestCatalog.All)[0]=Q("loop-a-01"));
        }
        [TestCase("tea-01","tea:2:1",30,"chapter-01")]
        [TestCase("bread-01","bread:2:1",40,"chapter-01")]
        [TestCase("flower-01","flower:2:1",40,"chapter-02")]
        [TestCase("tea-02","tea:3:1,bread:2:1",70,"chapter-02")]
        [TestCase("flower-02","flower:3:1,tea:2:1",70,"chapter-03")]
        [TestCase("bread-02","bread:3:1,flower:2:1",80,"chapter-03")]
        public void OldSixRequirementsCoinsAndChapterRemainExact(string id,string req,int coins,string chapter)
        {var q=Q(id);Assert.That(Requirements(q),Is.EqualTo(req));Assert.That(q.Coins,Is.EqualTo(coins));Assert.That(ChapterCatalog.ForQuest(id).Id,Is.EqualTo(chapter));}
        [Test] public void OldRequesterTitleDialogueAndStoryMembershipRemainUnchanged()
        {
            string[] who={"lulu","momo","miel","pom","fio","noah"};
            string[] titles={"お茶会の準備","焼きたてのおすそわけ","小さな花束","のんびりお茶会","花いっぱいの広場","街のごちそう会"};
            string[] lines={"お茶を届けてくれる？ みんなでお茶会をしよう！","みんなで、おいしいパンを食べよう！","お茶会のお部屋に、お花を飾りたいな。","香りのよいお茶とパンで、ひと休みしよう。","広場をお花で彩って、みんなを迎えよう！","パンと花束をそろえて、街のお祝いをしよう。"};
            Assert.That(QuestCatalog.Story.Select(q=>q.RequesterId),Is.EqualTo(who));Assert.That(QuestCatalog.Story.Select(q=>q.Title),Is.EqualTo(titles));Assert.That(QuestCatalog.Story.Select(q=>q.Dialogue),Is.EqualTo(lines));Assert.That(QuestCatalog.Story[0],Is.SameAs(QuestDefinition.First));
            Assert.That(ChapterCatalog.All.SelectMany(c=>c.QuestIds),Is.EqualTo(Old));Assert.That(QuestStoryPresentation.All.Select(q=>q.QuestId),Is.EqualTo(Old));
        }
        [TestCase("loop-a-01","tea:1:2,bread:1:2,flower:2:1",30,6)]
        [TestCase("loop-a-02","tea:2:1,bread:2:2,flower:2:2",40,10)]
        [TestCase("loop-a-03","tea:3:1,bread:3:2,flower:2:2",50,16)]
        [TestCase("loop-a-04","tea:3:2,bread:2:2,flower:3:2",60,20)]
        [TestCase("loop-a-05","tea:2:2,bread:3:3,flower:3:2",70,24)]
        [TestCase("loop-a-06","tea:3:3,bread:3:2,flower:3:2",80,28)]
        public void NewSixRequirementsCoinsAndUnitsAreExact(string id,string req,int coins,int units)
        {var q=Q(id);Assert.That(Requirements(q),Is.EqualTo(req));Assert.That(q.Coins,Is.EqualTo(coins));Assert.That(Units(q),Is.EqualTo(units));Assert.That(ChapterCatalog.ForQuest(id),Is.Null);Assert.That(QuestStoryPresentation.For(id),Is.Null);}
        [Test] public void ExactTotalsAre128Units126Generated252PointsAnd84Merges()
        {
            Assert.That(QuestCatalog.Story.Sum(Units),Is.EqualTo(24));Assert.That(QuestCatalog.Additional.Sum(Units),Is.EqualTo(104));Assert.That(QuestCatalog.All.Sum(Units),Is.EqualTo(128));
            Assert.That(QuestCatalog.All.Sum(Units)-2,Is.EqualTo(126));Assert.That((QuestCatalog.All.Sum(Units)-2)*2,Is.EqualTo(252));Assert.That(QuestCatalog.All.Sum(q=>Units(q)-q.Requirements.Sum(r=>r.Count)),Is.EqualTo(84));
        }
        [Test] public void NewPlayerUsesExactMixedOrderAndEachIdOnlyOnce()
        {
            var s=GameState.CreateInitial();Assert.That(s.economy.questRoute,Is.EqualTo(QuestProgressionRoute.ExpandedNewPlayer));Assert.That(QuestCatalog.NewPlayerOrder.Select(q=>q.Id),Is.EqualTo(Mixed));
            foreach(string id in Mixed){Assert.That(QuestCatalog.NextFor(s).Id,Is.EqualTo(id));s.activeQuestId=id;s.completedQuestIds.Add(id);}
            Assert.That(QuestCatalog.NextFor(s),Is.Null);Assert.That(QuestCatalog.All12Complete(s),Is.True);
        }
        [TestCase(QuestProgressionRoute.NewPlayer)] [TestCase(QuestProgressionRoute.LegacyPlayer)]
        public void ExistingV4AndMigratedPlayersFinishOldStoryBeforeAnyNewRequest(QuestProgressionRoute route)
        {
            var s=GameState.CreateInitial();s.economy.questRoute=route;s.completedQuestIds.AddRange(Old.Take(2));s.activeQuestId="flower-01";s.questState=QuestState.Producing;s.questProgress=1;
            foreach(string id in Old.Skip(2).Concat(New)){Assert.That(QuestCatalog.NextFor(s).Id,Is.EqualTo(id));s.completedQuestIds.Add(id);}
            Assert.That(QuestCatalog.NextFor(s),Is.Null);Assert.That(QuestCatalog.CompletedCount(s),Is.EqualTo(12));
        }
        [TestCase(QuestProgressionRoute.NewPlayer)] [TestCase(QuestProgressionRoute.LegacyPlayer)] [TestCase(QuestProgressionRoute.ExpandedNewPlayer)]
        public void ActiveOldRequestAndProgressAreReadOnlyAndSurviveCodecReload(QuestProgressionRoute route)
        {
            var s=GameState.CreateInitial();s.economy.questRoute=route;s.activeQuestId="tea-02";s.questState=QuestState.Producing;s.questProgress=2;s.completedQuestIds.AddRange(Old.Take(3));s.coins=110;
            string before=StateCodec.Encode(s);Assert.That(QuestCatalog.NextFor(s).Id,Is.EqualTo("tea-02"));Assert.That(StateCodec.Encode(s),Is.EqualTo(before));
            var loaded=StateCodec.Decode(before);StateCodec.Validate(loaded);Assert.That(loaded.economy.questRoute,Is.EqualTo(route));Assert.That(QuestCatalog.NextFor(loaded).Id,Is.EqualTo("tea-02"));Assert.That(StateCodec.Encode(loaded),Is.EqualTo(before));
        }
        [Test] public void MixedRouteDoesNotTurnLegacyAfterSaveAndReload()
        {
            string dir=Dir();try{var save=new SaveService(dir);var s=GameState.CreateInitial();s.completedQuestIds.AddRange(Old.Take(2));save.Save(s);var loaded=save.Load().State;Assert.That(loaded.saveVersion,Is.EqualTo(4));Assert.That(loaded.economy.questRoute,Is.EqualTo(QuestProgressionRoute.ExpandedNewPlayer));Assert.That(QuestCatalog.NextFor(loaded).Id,Is.EqualTo("loop-a-01"));}finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
        }
        [Test] public void UnknownActiveQuestIsNeverSilentlyReplaced()
        {var s=GameState.CreateInitial();s.activeQuestId="unsupported-active";string before=StateCodec.Encode(s);Assert.That(QuestCatalog.NextFor(s),Is.Null);Assert.That(StateCodec.Encode(s),Is.EqualTo(before));}
        [Test] public void StorySixRemainsCompleteWhileAdditionalSixAreStillPending()
        {
            var s=GameState.CreateInitial();s.economy.questRoute=QuestProgressionRoute.LegacyPlayer;s.completedQuestIds.AddRange(Old);s.activeQuestId="bread-02";s.questState=QuestState.TownGrown;s.townGrowthLevel=6;
            var p=ChapterProgress.For(s);Assert.That(p.AllCompleted,Is.True);Assert.That(p.Story6Complete,Is.True);Assert.That(p.All12Complete,Is.False);Assert.That(p.CompletedCount,Is.EqualTo(6));Assert.That(p.CurrentChapter,Is.Null);Assert.That(p.LastCompletedChapter.Id,Is.EqualTo("chapter-03"));
            Assert.That(QuestCatalog.NextFor(s).Id,Is.EqualTo("loop-a-01"));Assert.That(ChapterPresentation.Create(s,"chapter-03").IsFinale,Is.True);
            s.completedQuestIds.AddRange(New);Assert.That(ChapterProgress.For(s).All12Complete,Is.True);Assert.That(ChapterProgress.For(s).AllCompleted,Is.True);
        }
        [Test] public void AdditionalQuestsCannotCompleteOrReplayAnyOldChapter()
        {
            var s=GameState.CreateInitial();s.completedQuestIds.AddRange(New);Assert.That(QuestCatalog.Story6Complete(s),Is.False);Assert.That(QuestCatalog.All12Complete(s),Is.False);Assert.That(ChapterProgress.For(s).CompletedCount,Is.Zero);
            foreach(string id in New)Assert.That(ChapterCatalog.ForQuest(id),Is.Null);
        }
        [Test] public void AllTwelveRequiredForAllCompleteAndUnknownIdsDoNotCount()
        {var s=GameState.CreateInitial();Complete(s,11);s.completedQuestIds.Add("unknown");s.completedQuestIds.Add(s.completedQuestIds[0]);Assert.That(QuestCatalog.CompletedCount(s),Is.EqualTo(11));Assert.That(QuestCatalog.All12Complete(s),Is.False);s.completedQuestIds.Add(QuestCatalog.All[11].Id);Assert.That(QuestCatalog.CompletedCount(s),Is.EqualTo(12));Assert.That(QuestCatalog.All12Complete(s),Is.True);}
        [TestCase(1,4L,1)] [TestCase(2,8L,2)] [TestCase(4,40L,3)] [TestCase(6,96L,4)] [TestCase(9,160L,5)] [TestCase(12,224L,6)]
        public void ExactBothConditionsReachTheSpecifiedLevel(int requests,long actions,int expected)
        {var s=GameState.CreateInitial();Complete(s,requests);s.economy.townActionTotal=actions;new TownGrowthService().EvaluateTownGrowth(s);Assert.That(s.townGrowthLevel,Is.EqualTo(expected));}
        [TestCase(1,0L)] [TestCase(2,0L)] [TestCase(4,0L)] [TestCase(6,0L)] [TestCase(9,0L)] [TestCase(12,0L)]
        [TestCase(0,4L)] [TestCase(0,8L)] [TestCase(0,40L)] [TestCase(0,96L)] [TestCase(0,160L)] [TestCase(0,224L)]
        public void EitherConditionAloneCannotGrowTown(int requests,long actions)
        {var s=GameState.CreateInitial();Complete(s,requests);s.economy.townActionTotal=actions;new TownGrowthService().EvaluateTownGrowth(s);Assert.That(s.townGrowthLevel,Is.Zero);}
        [TestCase(0,4L,0)] [TestCase(1,3L,0)] [TestCase(1,8L,1)] [TestCase(2,7L,1)]
        [TestCase(3,40L,2)] [TestCase(4,39L,2)] [TestCase(5,96L,3)] [TestCase(6,95L,3)]
        [TestCase(8,160L,4)] [TestCase(9,159L,4)] [TestCase(11,224L,5)] [TestCase(12,223L,5)]
        public void OneBelowEitherThresholdDoesNotReachNextLevel(int requests,long actions,int expected)
        {var s=GameState.CreateInitial();Complete(s,requests);s.economy.townActionTotal=actions;new TownGrowthService().EvaluateTownGrowth(s);Assert.That(s.townGrowthLevel,Is.EqualTo(expected));}
        [TestCase(5,4,40L,5)] [TestCase(6,0,0L,6)] [TestCase(6,12,224L,6)] [TestCase(1,6,100L,4)]
        public void SavedFloorAndDirectLevelJumpArePreserved(int saved,int requests,long actions,int expected)
        {var s=GameState.CreateInitial();s.townGrowthLevel=saved;Complete(s,requests);s.economy.townActionTotal=actions;new TownGrowthService().EvaluateTownGrowth(s);Assert.That(s.townGrowthLevel,Is.EqualTo(expected));}
        [Test] public void UnknownAndDuplicateCompletedIdsCannotCreateLevelSeven()
        {var s=GameState.CreateInitial();Complete(s,12);s.completedQuestIds.AddRange(s.completedQuestIds.ToArray());s.completedQuestIds.Add("foreign");s.economy.townActionTotal=long.MaxValue;new TownGrowthService().EvaluateTownGrowth(s);Assert.That(s.townGrowthLevel,Is.EqualTo(6));Assert.That(QuestCatalog.CompletedCount(s),Is.EqualTo(12));}
        [Test] public void LoadOwnerReevaluationSavesOnlyIfLevelActuallyIncreases()
        {
            var s=GameState.CreateInitial();Complete(s,6);s.economy.townActionTotal=100;var store=new PointStore();var game=new GameStateManager(s,store);
            Assert.That(s.townGrowthLevel,Is.EqualTo(4));Assert.That(store.Writes,Is.EqualTo(1));Assert.That(game.ReevaluateTownLevel(),Is.False);Assert.That(store.Writes,Is.EqualTo(1));
            var again=new GameStateManager(store.Load().State,store);Assert.That(again.State.townGrowthLevel,Is.EqualTo(4));Assert.That(store.Writes,Is.EqualTo(1));Assert.That(again.MofuPoints.Balance,Is.EqualTo(24));
        }
        [Test] public void NewGameStartsAtZeroAndExistingSavedSixNeverFallsOnReload()
        {
            var s=GameState.CreateInitial();var store=new PointStore();var game=new GameStateManager(s,store);Assert.That(s.townGrowthLevel,Is.Zero);Assert.That(store.Writes,Is.Zero);
            s.townGrowthLevel=6;store.Save(s);var loaded=new GameStateManager(store.Load().State,store);Assert.That(loaded.State.townGrowthLevel,Is.EqualTo(6));Assert.That(store.Writes,Is.EqualTo(1));
        }
        [TestCase(false)] [TestCase(true)]
        public void LoadReevaluationSaveFailureRestoresOriginalInsteadOfNewState(bool denied)
        {
            var s=GameState.CreateInitial();Complete(s,6);s.economy.townActionTotal=100;s.coins=91;string before=StateCodec.Encode(s);var store=new PointStore{Fail=true,Denied=denied};var game=new GameStateManager(s,store);
            Assert.That(StateCodec.Encode(s),Is.EqualTo(before));Assert.That(s.coins,Is.EqualTo(91));Assert.That(store.Writes,Is.EqualTo(1));Assert.That(game.LastError,Is.Not.Empty);
        }
        [TestCase("talk")] [TestCase("walk")] [TestCase("flower")]
        public void GrowthIncludedInNormalActionSingleSaveEvenAtWalletCap(string kind)
        {
            var s=GameState.CreateInitial();var store=new PointStore();var game=new GameStateManager(s,store);s.completedQuestIds.Add("tea-01");s.economy.mofuPoints=96;
            bool ok=kind=="talk"?game.MofuPoints.TryRewardTalk("2026-11-07","momo"):kind=="walk"?game.MofuPoints.TryRewardNormalWalk("2026-11-07","2026-11-07","walk"):game.MofuPoints.TryRewardFlower("2026-11-07");
            Assert.That(ok,Is.True);Assert.That(s.townGrowthLevel,Is.EqualTo(1));Assert.That(store.Saved.townGrowthLevel,Is.EqualTo(1));Assert.That(store.Writes,Is.EqualTo(1));Assert.That(game.MofuPoints.Balance,Is.EqualTo(96));
        }
        [TestCase(false)] [TestCase(true)]
        public void FailedActionSaveRollsBackNewLevelWithWalletAndLedger(bool denied)
        {
            var s=GameState.CreateInitial();var store=new PointStore();var game=new GameStateManager(s,store);s.completedQuestIds.Add("tea-01");s.coins=88;string before=StateCodec.Encode(s);store.Fail=true;store.Denied=denied;
            Assert.That(game.MofuPoints.TryRewardTalk("2026-11-07","momo"),Is.False);Assert.That(StateCodec.Encode(s),Is.EqualTo(before));Assert.That(store.Writes,Is.EqualTo(1));
        }
        [Test] public void ExplicitTownActionTotalIncreaseAlsoReevaluatesWithinOneSave()
        {var s=GameState.CreateInitial();var store=new PointStore();var game=new GameStateManager(s,store);s.completedQuestIds.Add("tea-01");Assert.That(game.MofuPoints.TryAddTownActionTotal(4),Is.True);Assert.That(s.townGrowthLevel,Is.EqualTo(1));Assert.That(store.Writes,Is.EqualTo(1));}
        [TestCase("recovery")] [TestCase("extra")] [TestCase("help")]
        public void NonGrowthRewardsDoNotTriggerOrContributeToTownLevel(string kind)
        {
            var s=GameState.CreateInitial();var store=new PointStore();var game=new GameStateManager(s,store);
            if(kind=="help")
            {
                foreach(string id in new[]{"momo","lulu","noah"})game.MofuPoints.TryRewardTalk("2026-11-07",id);
                game.MofuPoints.TryRewardNormalWalk("2026-11-07","2026-11-07","n1");game.MofuPoints.TryRewardNormalWalk("2026-11-07","2026-11-07","n2");game.MofuPoints.TryRewardFlower("2026-11-07");game.MofuPoints.TryRewardDailyRecovery("2026-11-07");game.MofuPoints.TrySpend(game.MofuPoints.Balance);
            }
            s.completedQuestIds.Add("tea-01");s.economy.townActionTotal=40;long total=s.economy.townActionTotal;store.Writes=0;
            bool ok=kind=="recovery"?game.MofuPoints.TryRewardDailyRecovery("2026-11-07"):kind=="extra"?game.MofuPoints.TryRewardAdditionalWalk("2026-11-07","2026-11-07","extra"):game.MofuPoints.TryRewardHelpWalk("2026-11-07","2026-11-07","help");
            Assert.That(ok,Is.True);Assert.That(s.townGrowthLevel,Is.Zero);Assert.That(s.economy.townActionTotal,Is.EqualTo(total));Assert.That(store.Writes,Is.EqualTo(1));
        }
        [TestCase(QuestProgressionRoute.ExpandedNewPlayer)] [TestCase(QuestProgressionRoute.LegacyPlayer)] [TestCase(QuestProgressionRoute.NewPlayer)]
        public void EndToEndTwelveDeliveriesPayCoinsOnceAndNeverRevertStory(QuestProgressionRoute route)
        {
            var s=GameState.CreateInitial();s.economy.questRoute=route;s.economy.townActionTotal=224;var store=new PointStore();var game=new GameStateManager(s,store);var expected=route==QuestProgressionRoute.ExpandedNewPlayer?Mixed:Old.Concat(New).ToArray();bool storyDone=false;
            foreach(string id in expected)
            {
                var q=QuestCatalog.NextFor(s);Assert.That(q.Id,Is.EqualTo(id));Assert.That(game.Quests.AcceptQuest(q),Is.True);Fill(s,q);int wallet=game.MofuPoints.Balance;
                Assert.That(game.Quests.TryDeliver().Success,Is.True);Assert.That(game.Quests.TryDeliver().Success,Is.False);Assert.That(game.Quests.AcceptQuest(q),Is.False);
                Assert.That(game.MofuPoints.Balance,Is.EqualTo(wallet));Assert.That(s.coins,Is.EqualTo(s.completedQuestIds.Sum(done=>Q(done).Coins)));
                if(storyDone)Assert.That(ChapterProgress.For(s).AllCompleted,Is.True);storyDone=ChapterProgress.For(s).AllCompleted;
            }
            Assert.That(s.coins,Is.EqualTo(660));Assert.That(s.townGrowthLevel,Is.EqualTo(6));Assert.That(s.completedQuestIds.Count,Is.EqualTo(12));Assert.That(s.claimedRewardIds.Count,Is.EqualTo(12));Assert.That(QuestCatalog.All12Complete(s),Is.True);Assert.That(QuestCatalog.Story6Complete(s),Is.True);Assert.That(s.saveVersion,Is.EqualTo(4));
        }
        [Test] public void StandardAcceptanceCannotSkipNewOrLegacyOrderButCustomCatalogStillWorks()
        {
            var s=GameState.CreateInitial();var store=new PointStore();var game=new GameStateManager(s,store);Assert.That(game.Quests.AcceptQuest(Q("loop-a-01")),Is.False);Assert.That(store.Writes,Is.Zero);
            s.economy.questRoute=QuestProgressionRoute.LegacyPlayer;Assert.That(game.Quests.AcceptQuest(Q("loop-a-01")),Is.False);
            var q=new QuestDefinition("custom",new[]{new ItemRequirement("tea",1,1)},1);var custom=new QuestManager(s,store,new RewardService(),new TownGrowthService(),new[]{q});Assert.That(custom.AcceptQuest(q),Is.True);
        }
        [TestCase(false)] [TestCase(true)]
        public void FailedDeliveryRestoresCoinsItemsCompletionAndNewTownLevel(bool denied)
        {
            var s=GameState.CreateInitial();s.economy.townActionTotal=4;var store=new PointStore();var game=new GameStateManager(s,store);var q=QuestCatalog.NextFor(s);game.Quests.AcceptQuest(q);Fill(s,q);string before=StateCodec.Encode(s);int writes=store.Writes;store.Fail=true;store.Denied=denied;
            Assert.That(game.Quests.TryDeliver().SaveFailed,Is.True);Assert.That(StateCodec.Encode(s),Is.EqualTo(before));Assert.That(store.Writes,Is.EqualTo(writes+1));Assert.That(s.townGrowthLevel,Is.Zero);
        }
        [Test] public void ReentrantDeliveryCannotCompleteOrRewardTwice()
        {
            var s=GameState.CreateInitial();s.economy.townActionTotal=4;var store=new PointStore();var game=new GameStateManager(s,store);var q=QuestCatalog.NextFor(s);game.Quests.AcceptQuest(q);Fill(s,q);store.Writes=0;bool? duplicate=null;
            store.DuringSave=()=>{store.DuringSave=null;duplicate=game.Quests.TryDeliver().Success;};Assert.That(game.Quests.TryDeliver().Success,Is.True);Assert.That(duplicate,Is.False);Assert.That(s.coins,Is.EqualTo(30));Assert.That(s.townGrowthLevel,Is.EqualTo(1));Assert.That(store.Writes,Is.EqualTo(1));
        }
        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void OldSaveMigrationKeepsLegacyRouteRawProtectionAndSavedLevel(int version)
        {
            string dir=Dir();try
            {
                Directory.CreateDirectory(dir);var s=GameState.CreateInitial();s.saveVersion=version;s.economy=null;s.coins=77;s.townGrowthLevel=5;s.activeQuestId="flower-01";s.questState=QuestState.Producing;s.questProgress=1;s.completedQuestIds.AddRange(Old.Take(2));
                byte[] raw=Encoding.UTF8.GetBytes(StateCodec.Encode(s));var save=new SaveService(dir);File.WriteAllBytes(save.PrimaryPath,raw);var loaded=save.Load();Assert.That(loaded.IsBlocked,Is.False);var game=new GameStateManager(loaded.State,save);
                Assert.That(game.State.saveVersion,Is.EqualTo(4));Assert.That(game.State.economy.questRoute,Is.EqualTo(QuestProgressionRoute.LegacyPlayer));Assert.That(game.State.townGrowthLevel,Is.EqualTo(5));Assert.That(game.State.coins,Is.EqualTo(77));Assert.That(QuestCatalog.NextFor(game.State).Id,Is.EqualTo("flower-01"));Assert.That(File.ReadAllBytes(save.PreV4Path),Is.EqualTo(raw));
            }finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
        }
        [Test] public void InvalidSavedLevelSevenIsBlockedWithoutClampingOrOverwrite()
        {
            string dir=Dir();try{Directory.CreateDirectory(dir);var s=GameState.CreateInitial();s.townGrowthLevel=7;var save=new SaveService(dir);string raw=StateCodec.Encode(s);File.WriteAllText(save.PrimaryPath,raw);Assert.That(save.Load().IsBlocked,Is.True);Assert.That(File.ReadAllText(save.PrimaryPath),Is.EqualTo(raw));}finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
        }
        private sealed class PointStore:IStateStore
        {
            public bool Fail,Denied;public int Writes;public Action DuringSave;public GameState Saved=GameState.CreateInitial();
            public void Save(GameState s){Writes++;DuringSave?.Invoke();if(Fail){s.coins=0;s.townGrowthLevel=0;s.economy.mofuPoints=0;if(Denied)throw new UnauthorizedAccessException("Expected denial");throw new IOException("Expected save failure");}StateCodec.Validate(s);Saved=s.Clone();}
            public LoadResult Load()=>new LoadResult(Saved.Clone());public void ResetSave()=>throw new InvalidOperationException();
        }
    }
}
