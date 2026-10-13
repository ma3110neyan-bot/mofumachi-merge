using System;
using System.IO;
using Mofumachi.Core;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class ItemRemovalTests
    {
        [Test] public void RemoveOnlyConfirmedUnlockedItemAndRollback()
        {
            var state=GameState.CreateInitial();state.mergeBoard.Clear();
            for(int i=0;i<30;i++)state.mergeBoard.Add(new BoardItem("tea",3,i));
            var store=new RemovalStore();var game=new GameStateManager(state,store);
            Assert.That(game.TryRemoveItem(0,"flower",3),Is.False);
            Assert.That(game.TryRemoveItem(0,"tea",2),Is.False);
            Assert.That(game.TryRemoveItem(-1,"tea",3),Is.False);Assert.That(store.Writes,Is.Zero);
            store.Fail=true;Assert.That(game.TryRemoveItem(0,"tea",3),Is.False);
            Assert.That(state.mergeBoard.Count,Is.EqualTo(30));Assert.That(game.Board.At(0).level,Is.EqualTo(3));
            store.Fail=false;Assert.That(game.TryRemoveItem(0,"tea",3),Is.True);
            Assert.That(state.mergeBoard.Count,Is.EqualTo(29));Assert.That(game.TryRemoveItem(0,"tea",3),Is.False);
            Assert.That(game.AddItem("flower",1),Is.True);Assert.That(game.Board.At(0).itemId,Is.EqualTo("flower"));
        }
        [Test] public void LockedAndUnknownItemsSurviveRefusedOrFailedOperations()
        {
            var state=GameState.CreateInitial();state.mergeBoard.Add(new BoardItem("legacy",2,2));
            var store=new RemovalStore();var game=new GameStateManager(state,store);
            Assert.That(game.TryMerge(0,1),Is.True);
            Assert.That(game.TryRemoveItem(1,"tea",2),Is.False);
            store.Fail=true;Assert.That(game.TryRemoveItem(2,"legacy",2),Is.False);
            Assert.That(game.Board.IsLocked(1),Is.True);Assert.That(game.Board.At(2).itemId,Is.EqualTo("legacy"));
            StateCodec.Validate(state.Clone());
            store.Fail=false;game.Board.ReleaseLocks();Assert.That(game.TryRemoveItem(2,"legacy",2),Is.True);
        }
        [Test] public void DifferentKindsNeverMerge()
        {
            var state=GameState.CreateInitial();state.mergeBoard[1].itemId="bread";var game=new GameStateManager(state,new RemovalStore());
            string before=StateCodec.Encode(state);Assert.That(game.TryMerge(0,1),Is.False);Assert.That(StateCodec.Encode(state),Is.EqualTo(before));
        }
        private static System.Collections.Generic.List<string> Frames(GameState state)=>state.productionFrameIds;
        private static bool Pack(GameStateManager game,params string[] ids)=>game.PackProductionFrames(ids);
        private static bool Place(GameStateManager game,string id)=>game.PlaceProductionFrame(id);
        [Test] public void LegacySaveGetsAllFramesAndExplicitEmptyPlacementSurvivesReload()
        {
            var original=StateCodec.Encode(GameState.CreateInitial());
            var legacy=System.Text.RegularExpressions.Regex.Replace(original,@",?""productionFrameIds"":\[[^\]]*\],?",m=>m.Value.StartsWith(",")&&m.Value.EndsWith(",")?",":"");
            var loaded=StateCodec.Decode(legacy);StateCodec.Validate(loaded);
            Assert.That(Frames(loaded),Is.EquivalentTo(new[]{"tea","bread","flower"}));
            Frames(loaded).Clear();Assert.That(Frames(StateCodec.Decode(StateCodec.Encode(loaded))),Is.Empty);
            Assert.That(loaded.saveVersion,Is.EqualTo(3));
        }
        [Test] public void BatchPackingWritesOnceAndDoesNotConsumeItemsRewardsOrFavorite()
        {
            var state=GameState.CreateInitial();state.favoriteCharacterId="momo";
            var store=new RemovalStore();var game=new GameStateManager(state,store);
            string items=string.Join(";",state.mergeBoard.ConvertAll(i=>i.itemId+":"+i.level+":"+i.cellIndex));
            Assert.That(Pack(game,"tea","bread"),Is.True);Assert.That(store.Writes,Is.EqualTo(1));
            Assert.That(Frames(state),Is.EqualTo(new[]{"flower"}));
            Assert.That(string.Join(";",state.mergeBoard.ConvertAll(i=>i.itemId+":"+i.level+":"+i.cellIndex)),Is.EqualTo(items));
            Assert.That(state.coins,Is.Zero);Assert.That(state.claimedRewardIds,Is.Empty);Assert.That(state.favoriteCharacterId,Is.EqualTo("momo"));
        }
        [Test] public void BatchPackingFailureRestoresEveryFrameAndMergeLock()
        {
            var state=GameState.CreateInitial();var store=new RemovalStore();var game=new GameStateManager(state,store);
            Assert.That(game.TryMerge(0,1),Is.True);string before=StateCodec.Encode(state);int writes=store.Writes;
            store.Fail=true;Assert.That(Pack(game,"tea","bread"),Is.False);
            Assert.That(StateCodec.Encode(state),Is.EqualTo(before));Assert.That(game.Board.IsLocked(1),Is.True);
            Assert.That(store.Writes,Is.EqualTo(writes));Assert.That(game.LastError,Is.Not.Empty);
        }
        [Test] public void InvalidAndStaleFrameSelectionsNeverPartiallyPackOrWrite()
        {
            var state=GameState.CreateInitial();var store=new RemovalStore();var game=new GameStateManager(state,store);
            string before=StateCodec.Encode(state);
            foreach(var ids in new[]{new string[0],new[]{"tea","tea"},new[]{"bread","unknown"}})
            {Assert.That(Pack(game,ids),Is.False);Assert.That(StateCodec.Encode(state),Is.EqualTo(before));Assert.That(store.Writes,Is.Zero);}
            Assert.That(Pack(game,"tea"),Is.True);before=StateCodec.Encode(state);int writes=store.Writes;
            Assert.That(Pack(game,"bread","tea"),Is.False);Assert.That(StateCodec.Encode(state),Is.EqualTo(before));Assert.That(store.Writes,Is.EqualTo(writes));
        }
        [Test] public void AllPackedFramesCanBePlacedAgainWithoutChangingTheQuest()
        {
            var state=GameState.CreateInitial();var store=new RemovalStore();var game=new GameStateManager(state,store);
            Assert.That(game.Quests.AcceptQuest(QuestCatalog.NextFor(state)),Is.True);
            Assert.That(Pack(game,"tea","bread","flower"),Is.True);Assert.That(Frames(state),Is.Empty);
            store.Fail=true;Assert.That(Place(game,"tea"),Is.False);Assert.That(Frames(state),Is.Empty);
            store.Fail=false;Assert.That(Place(game,"tea"),Is.True);Assert.That(Place(game,"tea"),Is.False);
            Assert.That(state.activeQuestId,Is.EqualTo("tea-01"));Assert.That(state.mergeBoard.Count,Is.EqualTo(2));Assert.That(state.coins,Is.Zero);
        }
        [Test] public void PackedFramesPersistThroughDiskReloadAndMalformedPlacementIsRejected()
        {
            string folder=Path.Combine(Path.GetTempPath(),"mofumachi-frames-"+Guid.NewGuid());
            try
            {
                var save=new SaveService(folder);var game=new GameStateManager(GameState.CreateInitial(),save);
                Assert.That(Pack(game,"bread","flower"),Is.True);var resumed=save.Load().State;
                Assert.That(Frames(resumed),Is.EqualTo(new[]{"tea"}));Assert.That(resumed.mergeBoard.Count,Is.EqualTo(2));
                Frames(resumed).Add("tea");Assert.Throws<InvalidDataException>(()=>StateCodec.Validate(resumed));
                Frames(resumed).Clear();Frames(resumed).Add("unknown");Assert.Throws<InvalidDataException>(()=>StateCodec.Validate(resumed));
            }
            finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
        }
        [Test] public void ReplacedFramesStillAllowBreadDeliveryOnceAndDiskResume()
        {
            string folder=Path.Combine(Path.GetTempPath(),"mofumachi-frame-loop-"+Guid.NewGuid());
            try
            {
                var save=new SaveService(folder);var game=new GameStateManager(GameState.CreateInitial(),save);
                Assert.That(game.Quests.AcceptQuest(QuestCatalog.NextFor(game.State)),Is.True);
                Assert.That(Pack(game,"tea","bread","flower"),Is.True);Assert.That(Place(game,"tea"),Is.True);
                Assert.That(game.TryMerge(0,1),Is.True);game.Board.ReleaseLocks();game.Quests.RefreshProgress();Assert.That(game.Save(),Is.True);
                Assert.That(game.Quests.TryDeliver().Success,Is.True);
                Assert.That(game.Quests.AcceptQuest(QuestCatalog.NextFor(game.State)),Is.True);Assert.That(Place(game,"bread"),Is.True);
                Assert.That(game.AddItem("bread",1),Is.True);Assert.That(game.AddItem("bread",1),Is.True);Assert.That(game.TryMerge(0,1),Is.True);
                game.Board.ReleaseLocks();game.Quests.RefreshProgress();Assert.That(game.Save(),Is.True);Assert.That(game.Quests.TryDeliver().Success,Is.True);
                Assert.That(game.Quests.TryDeliver().Success,Is.False);
                var resumed=save.Load().State;Assert.That(resumed.coins,Is.EqualTo(70));Assert.That(resumed.townGrowthLevel,Is.EqualTo(2));
                Assert.That(resumed.completedQuestIds.Count,Is.EqualTo(2));Assert.That(resumed.productionFrameIds,Is.EquivalentTo(new[]{"tea","bread"}));
                Assert.That(QuestCatalog.NextFor(resumed).Id,Is.EqualTo("flower-01"));Assert.That(resumed.mergeBoard,Is.Empty);
            }
            finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
        }
        private sealed class RemovalStore : IStateStore
        {
            public bool Fail;public int Writes;
            public void Save(GameState s){if(Fail)throw new IOException("Test failure");StateCodec.Validate(s);Writes++;}
            public LoadResult Load()=>new LoadResult(GameState.CreateInitial());public void ResetSave()=>throw new InvalidOperationException();
        }
    }
}
