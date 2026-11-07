using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Mofumachi.Tests
{
    // Catches stale/optimistic UGUI values, mixed progress sources and lost load/rollback state.
    public sealed class ProgressUiIntegrationTests
    {
        private const string Day="2026-11-07", Next="2026-11-08";
        private GameState state;private GameStateManager game;private PointStore store;
        private GameObject root;private Text wallet,title,detail,level;private ProgressUiBinding binding;
        private string directory;
        [SetUp] public void SetUp()
        {
            state=GameState.CreateInitial();store=new PointStore();game=new GameStateManager(state,store);
            root=new GameObject("Progress UI fixture",typeof(RectTransform));
            var font=Resources.Load<Font>("Mofumachi/UIFont");Assert.That(font,Is.Not.Null);
            var w=new UIWidgets(font,null);var parent=(RectTransform)root.transform;parent.sizeDelta=new Vector2(360,80);
            wallet=w.Label("Merge selected hint","",parent,new Rect(0,0,1,1),11);
            title=w.StoryLabel("Next goal title","",parent,new Rect(0,0,1,1),12);
            detail=w.Label("Town growth hint","",parent,new Rect(0,0,1,1),12);
            level=w.Label("Town value","",parent,new Rect(0,0,1,1),13);
            binding=root.AddComponent<ProgressUiBinding>();Rebind();
        }
        [TearDown] public void TearDown()
        {if(root!=null)UnityEngine.Object.DestroyImmediate(root);if(directory!=null&&Directory.Exists(directory))Directory.Delete(directory,true);}
        private void Rebind()=>binding.Bind(game,wallet,title,detail,level);
        private void Complete(int count){state.completedQuestIds=QuestCatalog.All.Take(count).Select(q=>q.Id).ToList();}
        private void HelpReady(int balance=0)
        {
            game.MofuPoints.TryApplyDay(Day);state.economy.talkRewardCharacterIds.AddRange(new[]{"momo","lulu","pom"});
            state.economy.normalWalkRewards=2;
            state.economy.walkRewardReceipts.AddRange(new[]{new WalkRewardReceipt(Day,"normal1",WalkRewardKind.Normal),new WalkRewardReceipt(Day,"normal2",WalkRewardKind.Normal)});
            state.economy.lastFlowerRewardDay=Day;state.economy.lastRecoveryDay=Day;
            state.economy.townActionTotal=32;state.economy.mofuPoints=balance;Rebind();store.Writes=0;
        }
        private bool Reward(string kind)=>kind=="talk"?game.MofuPoints.TryRewardTalk(Day,"momo"):
            kind=="walk"?game.MofuPoints.TryRewardNormalWalk(Day,Day,"normal"):
            kind=="flower"?game.MofuPoints.TryRewardFlower(Day):kind=="recovery"?game.MofuPoints.TryRewardDailyRecovery(Day):
            kind=="extra"?game.MofuPoints.TryRewardAdditionalWalk(Day,Day,"extra"):game.MofuPoints.TryRewardHelpWalk(Day,Day,"help");
        private string[] Visible()=>new[]{wallet.text,title.text,detail.text,level.text};
        [Test] public void NewStateShows24FreeTeaPairExpandedRouteAndFirstRequest()
        {
            Assert.That(wallet.text,Is.EqualTo("所持 24P ・ 生成 2P"));Assert.That(state.mergeBoard.Count,Is.EqualTo(2));
            Assert.That(state.mergeBoard.All(i=>i.itemId=="tea"&&i.level==1),Is.True);
            Assert.That(state.economy.questRoute,Is.EqualTo(QuestProgressionRoute.ExpandedNewPlayer));Assert.That(QuestCatalog.NextFor(state).Id,Is.EqualTo("tea-01"));
            Assert.That(state.townGrowthLevel,Is.Zero);Assert.That(title.text,Is.EqualTo("Lv1まで おねがい 0/1"));Assert.That(detail.text,Is.EqualTo("まち交流 0/4"));Assert.That(store.Writes,Is.Zero);
        }
        [TestCase("tea")] [TestCase("bread")] [TestCase("flower")]
        public void SuccessfulUserGenerationImmediatelyShows22AndOnlyOneAddedItem(string id)
        {Assert.That(game.TryGenerateLv1(id),Is.True);Assert.That(wallet.text,Is.EqualTo("所持 22P ・ 生成 2P"));Assert.That(state.mergeBoard.Count,Is.EqualTo(3));Assert.That(store.Writes,Is.EqualTo(1));}
        [TestCase(0)] [TestCase(1)]
        public void InsufficientBalanceDoesNotSaveOrChangeDisplayedState(int balance)
        {state.economy.mofuPoints=balance;Rebind();string[] before=Visible();Assert.That(game.TryGenerateLv1("tea"),Is.False);Assert.That(game.LastError,Does.Contain("もふポイントが足りません"));Assert.That(Visible(),Is.EqualTo(before));Assert.That(store.Writes,Is.Zero);}
        [Test] public void TwoPointsGenerateToZeroWhileFreeMergeAndMovementRemainAvailable()
        {state.economy.mofuPoints=2;Rebind();Assert.That(game.TryGenerateLv1("bread"),Is.True);Assert.That(wallet.text,Is.EqualTo("所持 0P ・ 生成 2P"));Assert.That(game.TryMerge(0,1),Is.True);Assert.That(game.TryMove(2,3),Is.True);Assert.That(wallet.text,Is.EqualTo("所持 0P ・ 生成 2P"));}
        [Test] public void FullBoardGenerationNeverChangesTheVisibleWallet()
        {state.mergeBoard.Clear();for(int i=0;i<30;i++)state.mergeBoard.Add(new BoardItem("tea",1,i));string[] before=Visible();Assert.That(game.TryGenerateLv1("tea"),Is.False);Assert.That(Visible(),Is.EqualTo(before));Assert.That(store.Writes,Is.Zero);}
        [TestCase("talk",28,4L)] [TestCase("walk",30,6L)] [TestCase("flower",32,8L)]
        public void NormalRewardsUpdateWalletAndFaceValueProgressAndDuplicateDoesNotSave(string kind,int balance,long actions)
        {
            Assert.That(Reward(kind),Is.True);Assert.That(wallet.text,Is.EqualTo("所持 "+balance+"P ・ 生成 2P"));Assert.That(detail.text,Is.EqualTo("まち交流 "+actions+"/4"));
            string[] before=Visible();int writes=store.Writes;Assert.That(Reward(kind),Is.False);Assert.That(Visible(),Is.EqualTo(before));Assert.That(store.Writes,Is.EqualTo(writes));
        }
        [TestCase("talk",4L)] [TestCase("walk",6L)] [TestCase("flower",8L)]
        public void FullWalletDisplays96WhileActionProgressStillIncreases(string kind,long actions)
        {state.economy.mofuPoints=96;Rebind();Assert.That(Reward(kind),Is.True);Assert.That(wallet.text,Is.EqualTo("所持 96P ・ 生成 2P"));Assert.That(detail.text,Is.EqualTo("まち交流 "+actions+"/4"));}
        [Test] public void ThreeCharactersAndTwoWalksKeepTheirCapsInTheBoundView()
        {
            foreach(string id in new[]{"momo","lulu","pom"})Assert.That(game.MofuPoints.TryRewardTalk(Day,id),Is.True);
            Assert.That(game.MofuPoints.TryRewardTalk(Day,"fio"),Is.False);
            Assert.That(game.MofuPoints.TryRewardNormalWalk(Day,Day,"w1"),Is.True);Assert.That(game.MofuPoints.TryRewardNormalWalk(Day,Day,"w2"),Is.True);Assert.That(game.MofuPoints.TryRewardNormalWalk(Day,Day,"w3"),Is.False);
            Assert.That(wallet.text,Is.EqualTo("所持 48P ・ 生成 2P"));Assert.That(detail.text,Is.EqualTo("まち交流 24/4"));Assert.That(store.Writes,Is.EqualTo(5));
        }
        [TestCase("recovery",40)] [TestCase("extra",26)]
        public void RecoveryAndExtraWalkChangeOnlyWalletInProgressDisplay(string kind,int balance)
        {string before=detail.text;Assert.That(Reward(kind),Is.True);Assert.That(wallet.text,Is.EqualTo("所持 "+balance+"P ・ 生成 2P"));Assert.That(detail.text,Is.EqualTo(before));}
        [TestCase(0)] [TestCase(1)]
        public void EligibleHelpChangesOnlyWalletAndCannotRepeatAfterSpending(int balance)
        {HelpReady(balance);Assert.That(Reward("help"),Is.True);Assert.That(wallet.text,Is.EqualTo("所持 "+(balance+4)+"P ・ 生成 2P"));Assert.That(detail.text,Is.EqualTo("まち交流 32/4"));Assert.That(game.MofuPoints.TrySpend(4),Is.True);Assert.That(Reward("help"),Is.False);Assert.That(state.economy.helpWalkClaimed,Is.True);}
        [TestCase(0,0,0L,"Lv1まで おねがい 0/1","まち交流 0/4")]
        [TestCase(1,1,4L,"Lv2まで おねがい 1/2","まち交流 4/8")]
        [TestCase(2,3,32L,"Lv3まで おねがい 3/4","まち交流 32/40")]
        [TestCase(3,4,40L,"Lv4まで おねがい 4/6","まち交流 40/96")]
        [TestCase(4,6,96L,"Lv5まで おねがい 6/9","まち交流 96/160")]
        [TestCase(5,9,160L,"Lv6まで おねがい 9/12","まち交流 160/224")]
        public void ExistingLevelUsesFormalNextThresholdAndActualUnique12Count(int saved,int requests,long actions,string heading,string progress)
        {Complete(requests);state.townGrowthLevel=saved;state.economy.townActionTotal=actions;Rebind();Assert.That(title.text,Is.EqualTo(heading));Assert.That(detail.text,Is.EqualTo(progress));}
        [Test] public void DuplicateUnknownIdsDoNotEnterTheDisplayedRequestCount()
        {Complete(3);state.completedQuestIds.Add("unknown");state.completedQuestIds.Add(state.completedQuestIds[0]);state.townGrowthLevel=2;Rebind();Assert.That(title.text,Is.EqualTo("Lv3まで おねがい 3/4"));}
        [Test] public void FourthRequestAnd40ActionsAdvanceLevelAndItsNextConditionsTogether()
        {Complete(4);state.economy.townActionTotal=40;Assert.That(game.ReevaluateTownLevel(),Is.True);Assert.That(level.text,Is.EqualTo("街 Lv.3"));Assert.That(title.text,Is.EqualTo("Lv4まで おねがい 4/6"));Assert.That(detail.text,Is.EqualTo("まち交流 40/96"));}
        [Test] public void StorySixRemainsCompleteWithAdditionalSixStillPending()
        {state.completedQuestIds=QuestCatalog.Story.Select(q=>q.Id).ToList();state.economy.townActionTotal=96;Assert.That(game.ReevaluateTownLevel(),Is.True);Assert.That(ChapterProgress.For(state).AllCompleted,Is.True);Assert.That(QuestCatalog.All12Complete(state),Is.False);Assert.That(title.text,Is.EqualTo("Lv5まで おねがい 6/9"));}
        [Test] public void TwelveRequestsAnd224ActionsShowMaxAndNoNextConditions()
        {Complete(12);state.economy.townActionTotal=224;Assert.That(game.ReevaluateTownLevel(),Is.True);Assert.That(title.text,Is.EqualTo("街Lv6 MAX"));Assert.That(detail.text,Does.Not.Contain("/"));Assert.That(ChapterProgress.For(state).AllCompleted,Is.True);Assert.That(QuestCatalog.All12Complete(state),Is.True);}
        [Test] public void SavedMaxIsNeverShownAsLevelSevenEvenWhenNewRequestsRemain()
        {state.townGrowthLevel=6;Rebind();Assert.That(title.text,Is.EqualTo("街Lv6 MAX"));Assert.That(detail.text,Does.Not.Contain("/"));Assert.That(ChapterProgress.For(state).AllCompleted,Is.False);}
        [Test] public void TwelveDoneWithoutEnoughActionsStillShowsGrowthInsteadOfFalseMax()
        {Complete(12);state.townGrowthLevel=4;state.economy.townActionTotal=100;Rebind();Assert.That(title.text,Is.EqualTo("Lv5まで おねがい 12/9"));Assert.That(detail.text,Is.EqualTo("まち交流 100/160"));}
        [Test] public void DeliveryUpdatesCoinsCountNextQuestAndViewWithoutDuplicateReward()
        {
            Assert.That(game.Quests.AcceptQuest(QuestCatalog.NextFor(state)),Is.True);Assert.That(game.TryMerge(0,1),Is.True);game.Board.ReleaseLocks();Assert.That(Reward("talk"),Is.True);
            Assert.That(game.Quests.TryDeliver().Success,Is.True);Assert.That(state.coins,Is.EqualTo(30));Assert.That(title.text,Is.EqualTo("Lv2まで おねがい 1/2"));Assert.That(level.text,Is.EqualTo("街 Lv.1"));Assert.That(QuestCatalog.NextFor(state).Id,Is.EqualTo("bread-01"));
            int writes=store.Writes;Assert.That(game.Quests.TryDeliver().Success,Is.False);Assert.That(state.coins,Is.EqualTo(30));Assert.That(store.Writes,Is.EqualTo(writes));
        }
        [TestCase("generation")] [TestCase("talk")] [TestCase("walk")] [TestCase("flower")]
        [TestCase("recovery")] [TestCase("extra")] [TestCase("help")] [TestCase("delivery")] [TestCase("growth")]
        public void FailedSaveNeverPublishesTentativeUiAndRestoresCompleteState(string kind)
        {
            if(kind=="help")HelpReady();
            if(kind=="delivery"){game.Quests.AcceptQuest(QuestCatalog.NextFor(state));game.TryMerge(0,1);game.Board.ReleaseLocks();Reward("talk");}
            if(kind=="growth"){Complete(4);state.economy.townActionTotal=40;Rebind();}
            string[] before=Visible();string encoded=StateCodec.Encode(state);store.Writes=0;
            store.DuringSave=()=>Assert.That(Visible(),Is.EqualTo(before),"Optimistic UI inside Save");store.Fail=true;
            bool success=kind=="generation"?game.TryGenerateLv1("tea"):kind=="delivery"?game.Quests.TryDeliver().Success:kind=="growth"?game.ReevaluateTownLevel():Reward(kind);
            Assert.That(success,Is.False);Assert.That(Visible(),Is.EqualTo(before));Assert.That(StateCodec.Encode(state),Is.EqualTo(encoded));Assert.That(store.Writes,Is.EqualTo(1));
        }
        [Test] public void SuccessfulSavePublishesOnlyAfterStoreReturnsAndObserverFailureCannotUndoIt()
        {string[] before=Visible();store.DuringSave=()=>Assert.That(Visible(),Is.EqualTo(before));game.StateCommitted+=()=>throw new IOException("Synthetic observer only");Assert.That(game.TryGenerateLv1("tea"),Is.True);Assert.That(wallet.text,Is.EqualTo("所持 22P ・ 生成 2P"));Assert.That(store.Saved.economy.mofuPoints,Is.EqualTo(22));}
        [Test] public void RebindAndDisableDoNotLeaveAStaleSubscription()
        {var old=game;game=new GameStateManager(GameState.CreateInitial(),new PointStore());Rebind();Assert.That(old.TryGenerateLv1("tea"),Is.True);Assert.That(wallet.text,Is.EqualTo("所持 24P ・ 生成 2P"));root.SetActive(false);Assert.That(game.TryGenerateLv1("tea"),Is.True);root.SetActive(true);Assert.That(wallet.text,Is.EqualTo("所持 22P ・ 生成 2P"));}
        [Test] public void DailyJournalAndRewardsUseTheSameCommittedView()
        {
            var today=new DateTime(2026,11,7);var daily=new TownDailyService(new MemoryTownDailyStore(),today,game.MofuPoints);
            Assert.That(daily.RecordTalk("momo",today),Is.True);Assert.That(daily.RecordWalk("walk",Day,today),Is.True);for(int i=0;i<3;i++)Assert.That(daily.RecordMerge(),Is.True);
            Assert.That(daily.Claim(today),Is.True);Assert.That(wallet.text,Is.EqualTo("所持 42P ・ 生成 2P"));Assert.That(detail.text,Is.EqualTo("まち交流 18/4"));
            Assert.That(daily.Claim(today),Is.False);Assert.That(wallet.text,Is.EqualTo("所持 42P ・ 生成 2P"));
        }
        [Test] public void NewDayKeepsProgressAndClockRollbackCannotResetOrCreditAgain()
        {
            Reward("talk");Reward("walk");Reward("flower");Complete(2);Assert.That(game.ReevaluateTownLevel(),Is.True);
            string[] before=Visible();Assert.That(game.MofuPoints.TryApplyDay(Next),Is.True);Assert.That(Visible(),Is.EqualTo(before));
            Assert.That(state.economy.talkRewardCharacterIds,Is.Empty);Assert.That(state.economy.normalWalkRewards,Is.Zero);
            Assert.That(game.MofuPoints.TryRewardTalk(Next,"momo"),Is.True);string[] next=Visible();Assert.That(game.MofuPoints.TryRewardTalk(Day,"momo"),Is.False);Assert.That(Visible(),Is.EqualTo(next));
        }
        [Test] public void DiskSaveLoadKeepsEconomyLedgerRequestsRouteAndBoundUi()
        {
            directory=Path.Combine(Path.GetTempPath(),"mofu-progress-ui-"+Guid.NewGuid().ToString("N"));var disk=new SaveService(directory);
            game=new GameStateManager(state,disk);Rebind();Reward("talk");Reward("walk");Reward("flower");Reward("recovery");
            game.Quests.AcceptQuest(QuestCatalog.NextFor(state));game.TryMerge(0,1);game.Board.ReleaseLocks();Assert.That(game.Quests.TryDeliver().Success,Is.True);
            string encoded=StateCodec.Encode(state);string[] before=Visible();var loaded=disk.Load();Assert.That(loaded.IsBlocked,Is.False);game=new GameStateManager(loaded.State,disk);Rebind();
            Assert.That(StateCodec.Encode(game.State),Is.EqualTo(encoded));Assert.That(Visible(),Is.EqualTo(before));Assert.That(game.State.economy.questRoute,Is.EqualTo(QuestProgressionRoute.ExpandedNewPlayer));Assert.That(QuestCatalog.NextFor(game.State).Id,Is.EqualTo("bread-01"));
        }
        [TestCase(LoadStatus.NewGame,24)] [TestCase(LoadStatus.Loaded,40)]
        public void InitializeDistinguishesNew24FromResumedDailyRecovery(LoadStatus status,int balance)
        {
            var session=typeof(UIFlowController).GetField("session",BindingFlags.NonPublic|BindingFlags.Static);var social=typeof(UIFlowController).GetField("socialSession",BindingFlags.NonPublic|BindingFlags.Static);
            object previous=session.GetValue(null),previousDaily=social.GetValue(null);var node=new GameObject("Inactive initialization fixture");node.SetActive(false);
            try
            {
                var flow=node.AddComponent<UIFlowController>();var memory=new PointStore{Status=status};flow.Initialize(memory,new MemoryTownDailyStore());
                Assert.That(flow.IsLoadBlocked,Is.False);binding.Bind(flow.Game,wallet,title,detail,level);Assert.That(wallet.text,Is.EqualTo("所持 "+balance+"P ・ 生成 2P"));
                if(status==LoadStatus.NewGame){Assert.That(flow.TalkToCharacter("momo"),Is.True);Assert.That(wallet.text,Is.EqualTo("所持 28P ・ 生成 2P"));flow.RecordWorldWalk("new-walk",DateTime.Now.ToString("yyyy-MM-dd"));Assert.That(wallet.text,Is.EqualTo("所持 34P ・ 生成 2P"));Assert.That(flow.Daily.RecoverDay(DateTime.Now),Is.True);Assert.That(wallet.text,Is.EqualTo("所持 50P ・ 生成 2P"));}
            }
            finally{UnityEngine.Object.DestroyImmediate(node);session.SetValue(null,previous);social.SetValue(null,previousDaily);}
        }
        [TestCase(304f)] [TestCase(360f)] [TestCase(411f)]
        public void ExistingPhoneTextSlotsFitWithNoPositionOrSizeChange(float width)
        {
            Complete(9);state.townGrowthLevel=5;state.economy.townActionTotal=160;state.economy.mofuPoints=96;
            wallet.rectTransform.anchorMin=wallet.rectTransform.anchorMax=Vector2.zero;
            title.rectTransform.anchorMin=title.rectTransform.anchorMax=Vector2.zero;
            detail.rectTransform.anchorMin=detail.rectTransform.anchorMax=Vector2.zero;
            wallet.rectTransform.sizeDelta=new Vector2((width-24)*.64f-8,20);title.rectTransform.sizeDelta=new Vector2((width-24)*.69f*.9f,21.6f);detail.rectTransform.sizeDelta=new Vector2((width-24)*.69f*.9f,20.16f);
            var sizes=new[]{wallet.rectTransform.sizeDelta,title.rectTransform.sizeDelta,detail.rectTransform.sizeDelta};Rebind();
            var texts=new[]{wallet,title,detail};
            for(int i=0;i<texts.Length;i++){var text=texts[i];ReleaseTypography.Apply(text);text.GetComponent<HachiFontFit>()?.ApplyNow();Assert.That(text.preferredWidth,Is.LessThanOrEqualTo(text.rectTransform.rect.width+.5f),text.text);Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+.5f),text.text);Assert.That(text.rectTransform.sizeDelta,Is.EqualTo(sizes[i]));var settings=text.GetGenerationSettings(text.rectTransform.rect.size);Assert.That(text.cachedTextGenerator.Populate(text.text,settings),Is.True);Assert.That(text.cachedTextGenerator.vertexCount,Is.GreaterThan(0));}
        }
        [TestCase(640f,24)] [TestCase(740f,24)] [TestCase(640f,1)] [TestCase(640f,0)]
        public void ProductionLayoutKeepsCommittedWalletVisibleAfterGenerationOrInsufficientFeedback(float height,int balance)
        {
            var parent=(RectTransform)root.transform;parent.sizeDelta=new Vector2(360,height);
            var w=new UIWidgets(Resources.Load<Font>("Mofumachi/UIFont"),null);
            w.Panel("Header",parent,new Rect(0,0,1,1));
            var progress=w.Panel("Merge progress",parent,new Rect(0,0,1,1)).rectTransform;
            UIWidgets.Node("Merge goal content",progress,new Rect(0,0,1,1));
            w.Label("Merge selected item","",progress,new Rect(0,0,1,1),12);
            var hint=w.Label("Merge selected hint","",progress,new Rect(.36f,0,.64f,0),11);
            binding.Bind(game,walletText:hint);
            var tray=w.Panel("Merge tray",parent,new Rect(0,0,1,1)).rectTransform;
            UIWidgets.Node("Merge board",tray,new Rect(0,0,1,1));
            var toolbar=UIWidgets.Node("Merge toolbar",parent,new Rect(0,0,1,1));
            foreach(var item in ItemCatalog.All)
            {
                var button=w.Button("Generate frame "+item.Id,item.Name+"\n作る",toolbar,new Rect(0,0,1,1),()=>{});
                UIWidgets.Node("Frame item art",(RectTransform)button.transform,new Rect(0,0,1,1));
            }
            w.Button("Merge actions","生成枠\n整理",toolbar,new Rect(0,0,1,1),()=>{});
            w.Button("Deliver","納品する",toolbar,new Rect(0,0,1,1),()=>{});
            w.Panel("Navigation",parent,new Rect(0,0,1,1));
            state.economy.mofuPoints=balance;
            bool generated=game.TryGenerateLv1("tea");string message=UIStrings.GenerationFeedback(game,generated);
            var feedback=w.Label("Feedback",message,parent,new Rect(0,0,1,1),14);
            var surface=w.Panel("Feedback surface",parent,new Rect(0,0,1,1)).rectTransform;
            var layout=root.AddComponent<ProductionFrameLayout>();layout.Bind(tray,toolbar,feedback,surface,"");
            int expected=balance>=2?balance-2:balance;
            string wanted="所持 "+expected+"P ・ 生成 2P";
            Assert.That(hint.gameObject.activeInHierarchy&&hint.text==wanted||feedback.gameObject.activeInHierarchy&&feedback.text.Contains(wanted),Is.True);
            Assert.That(tray.offsetMin,Is.EqualTo(new Vector2(10,height<700?102:228)));
            Assert.That(tray.offsetMax,Is.EqualTo(new Vector2(-10,height<700?-124:-146)));
            ReleaseTypography.Apply(feedback);feedback.GetComponent<HachiFontFit>()?.ApplyNow();
            Assert.That(feedback.preferredHeight,Is.LessThanOrEqualTo(feedback.rectTransform.rect.height+.5f));
            if(balance<2)Assert.That(feedback.text,Does.Contain("もふポイントが足りません"));
        }
        private sealed class PointStore:IStateStore
        {
            public int Writes;public bool Fail;public Action DuringSave;public GameState Saved;public LoadStatus Status=LoadStatus.Loaded;
            public LoadResult Load()=>new LoadResult(Saved?.Clone()??GameState.CreateInitial(),Status);
            public void ResetSave(){}
            public void Save(GameState s){Writes++;DuringSave?.Invoke();if(Fail)throw new IOException("Expected integration Save failure");Saved=s.Clone();}
        }
    }
}
