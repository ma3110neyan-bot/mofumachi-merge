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
        private sealed class RemovalStore : IStateStore
        {
            public bool Fail;public int Writes;
            public void Save(GameState s){if(Fail)throw new IOException("Test failure");StateCodec.Validate(s);Writes++;}
            public LoadResult Load()=>new LoadResult(GameState.CreateInitial());public void ResetSave()=>throw new InvalidOperationException();
        }
    }
}
