using System;
using System.IO;
using System.Linq;
using Mofumachi.Core;
using NUnit.Framework;

namespace Mofumachi.Tests
{
    public sealed class QuestCatalogTests
    {
        [Test] public void CompletedTeaSaveCanAcceptNextWithoutPayingAgain()
        {
            var state=GameState.CreateInitial();state.coins=30;state.townGrowthLevel=1;
            state.activeQuestId="tea-01";state.completedQuestIds.Add("tea-01");state.claimedRewardIds.Add("quest:tea-01");
            var bread=new QuestDefinition("bread-01",new[]{new ItemRequirement("bread",2,1)},40);
            var store=new QuestStore();var manager=new QuestManager(state,store,new RewardService(),new TownGrowthService(),new[]{QuestDefinition.First,bread});
            Assert.That(manager.AcceptQuest(bread),Is.True);
            Assert.That(state.coins,Is.EqualTo(30));Assert.That(state.townGrowthLevel,Is.EqualTo(1));
            Assert.That(state.activeQuestId,Is.EqualTo("bread-01"));
            Assert.That(manager.TryDeliver().Success,Is.False);Assert.That(state.coins,Is.EqualTo(30));
        }
        [Test] public void SixAuthoredQuestsPayOnceEach()
        {
            var state=GameState.CreateInitial();var store=new QuestStore();var manager=new GameStateManager(state,store);
            Assert.That(ItemCatalog.All.Select(i=>i.Id),Is.EqualTo(new[]{"tea","bread","flower"}));
            Assert.That(QuestCatalog.All.Select(q=>q.Coins),Is.EqualTo(new[]{30,40,40,70,70,80}));
            foreach(var quest in QuestCatalog.All)
            {
                Assert.That(QuestCatalog.NextFor(state),Is.SameAs(quest));
                Assert.That(manager.Quests.AcceptQuest(quest),Is.True);
                foreach(var r in quest.Requirements)state.inventory.Add(new InventoryEntry(r.ItemId,r.Level,r.Count));
                Assert.That(manager.Quests.TryDeliver().Success,Is.True);
                Assert.That(manager.Quests.TryDeliver().Success,Is.False);
                Assert.That(manager.Quests.AcceptQuest(quest),Is.False);
                manager=new GameStateManager(store.Load().State,store);state=manager.State;
            }
            Assert.That(state.coins,Is.EqualTo(330));Assert.That(state.townGrowthLevel,Is.EqualTo(6));
            Assert.That(state.completedQuestIds.Count,Is.EqualTo(6));Assert.That(state.claimedRewardIds.Count,Is.EqualTo(6));
            Assert.That(QuestCatalog.NextFor(state),Is.Null);
        }
        [Test] public void FailedNextAcceptRestoresCompletedActiveAndUnfinishedCannotBeReplaced()
        {
            var state=GameState.CreateInitial();state.activeQuestId="tea-01";state.coins=30;state.townGrowthLevel=1;
            state.completedQuestIds.Add("tea-01");state.claimedRewardIds.Add("quest:tea-01");
            var store=new QuestStore{Fail=true};var manager=new GameStateManager(state,store);string before=StateCodec.Encode(state);
            Assert.That(QuestCatalog.NextFor(state).Id,Is.EqualTo("bread-01"));
            Assert.That(manager.Quests.AcceptQuest(QuestCatalog.All[1]),Is.False);
            Assert.That(manager.Quests.ActiveQuest.Id,Is.EqualTo("tea-01"));Assert.That(StateCodec.Encode(state),Is.EqualTo(before));
            store.Fail=false;Assert.That(manager.Quests.AcceptQuest(QuestCatalog.All[1]),Is.True);
            Assert.That(manager.Quests.AcceptQuest(QuestCatalog.All[2]),Is.False);
            Assert.That(QuestCatalog.NextFor(state).Id,Is.EqualTo("bread-01"));
        }
        [Test] public void MultiRequirementCountsRespectInventoryAndLocks()
        {
            var state=GameState.CreateInitial();state.mergeBoard.Clear();
            state.mergeBoard.Add(new BoardItem("bread",1,0));state.mergeBoard.Add(new BoardItem("bread",1,1));
            state.inventory.Add(new InventoryEntry("tea",3,1));var store=new QuestStore();var manager=new GameStateManager(state,store);
            var quest=QuestCatalog.All[3];Assert.That(manager.Quests.AcceptQuest(quest),Is.True);
            Assert.That(manager.TryMerge(0,1),Is.True);
            Assert.That(manager.Quests.GetProgress(quest).Select(p=>p.Available),Is.EqualTo(new long[]{1,0}));
            Assert.That(manager.Quests.CheckDelivery(),Is.False);manager.Board.ReleaseLocks();
            Assert.That(manager.Quests.GetProgress(quest).Select(p=>p.Available),Is.EqualTo(new long[]{1,1}));
            string before=StateCodec.Encode(state);store.Fail=true;
            Assert.That(manager.Quests.TryDeliver().SaveFailed,Is.True);Assert.That(StateCodec.Encode(state),Is.EqualTo(before));
            store.Fail=false;Assert.That(manager.Quests.TryDeliver().Success,Is.True);
            Assert.That(state.inventory,Is.Empty);Assert.That(state.mergeBoard,Is.Empty);Assert.That(state.coins,Is.EqualTo(70));
        }
        private sealed class QuestStore : IStateStore
        {
            public bool Fail;public int Writes;private GameState saved=GameState.CreateInitial();
            public void Save(GameState s){if(Fail)throw new IOException("Test failure");StateCodec.Validate(s);saved=s.Clone();Writes++;}
            public LoadResult Load()=>new LoadResult(saved.Clone());public void ResetSave()=>throw new InvalidOperationException();
        }
    }
}
