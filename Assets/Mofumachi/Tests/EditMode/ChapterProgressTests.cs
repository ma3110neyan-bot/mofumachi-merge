using System;
using System.IO;
using System.Linq;
using Mofumachi.Core;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class ChapterProgressTests
    {
        [Test] public void SixDeliveriesProduceThreeChaptersAnd330Coins()
        {
            var state=GameState.CreateInitial();var store=new MemoryStore();var game=new GameStateManager(state,store);
            int[] totals={30,70,110,180,250,330};
            for(int i=0;i<6;i++)
            {
                var quest=QuestCatalog.NextFor(state);Assert.That(game.Quests.AcceptQuest(quest),Is.True);
                foreach(var requirement in quest.Requirements)
                    for(int count=0;count<requirement.Count;count++)Produce(game,requirement.ItemId,requirement.Level);
                Assert.That(game.Quests.TryDeliver().Success,Is.True);Assert.That(game.Quests.TryDeliver().Success,Is.False);
                var progress=ChapterProgress.For(state);
                Assert.That(progress.CompletedCount,Is.EqualTo(i+1));Assert.That(state.coins,Is.EqualTo(totals[i]));Assert.That(state.townGrowthLevel,Is.EqualTo(i+1));
                Assert.That(progress.IsComplete("chapter-0"+(i/2+1)),Is.EqualTo(i%2==1));
            }
            var final=ChapterProgress.For(state);Assert.That(final.AllCompleted,Is.True);Assert.That(final.CurrentChapter,Is.Null);
            Assert.That(final.LastCompletedChapter.Id,Is.EqualTo("chapter-03"));Assert.That(state.saveVersion,Is.EqualTo(3));
        }
        private static int Produce(GameStateManager game,string item,int level)
        {
            if(level==1){Assert.That(game.AddItem(item,1),Is.True);return game.State.mergeBoard.Last().cellIndex;}
            int a=Produce(game,item,level-1),b=Produce(game,item,level-1);
            game.Board.ReleaseLocks();Assert.That(game.TryMerge(a,b),Is.True);game.Board.ReleaseLocks();return b;
        }
        [Test] public void LegacyAndOutOfOrderProgressRemainReadOnly()
        {
            var s=GameState.CreateInitial();s.completedQuestIds.AddRange(new[]{"tea-01","legacy-extra"});s.coins=30;s.townGrowthLevel=1;
            string before=StateCodec.Encode(s);var snapshot=ChapterProgress.For(s);
            Assert.That(snapshot.CompletedCount,Is.EqualTo(1));Assert.That(snapshot.CompletedInCurrent,Is.EqualTo(1));Assert.That(snapshot.CurrentChapter.Id,Is.EqualTo("chapter-01"));
            Assert.That(snapshot.LastCompletedChapter,Is.Null);Assert.That(snapshot.IsComplete("unknown"),Is.False);Assert.That(StateCodec.Encode(s),Is.EqualTo(before));
            s.activeQuestId="flower-02";Assert.That(ChapterProgress.For(s).CurrentChapter.Id,Is.EqualTo("chapter-03"));
            s.activeQuestId="unknown-active";before=StateCodec.Encode(s);Assert.That(ChapterProgress.For(s).CurrentChapter.Id,Is.EqualTo("chapter-01"));Assert.That(StateCodec.Encode(s),Is.EqualTo(before));
            s.completedQuestIds.AddRange(new[]{"flower-02","bread-02"});Assert.That(ChapterProgress.For(s).LastCompletedChapter.Id,Is.EqualTo("chapter-03"));
            Assert.That(snapshot.CompletedCount,Is.EqualTo(1));Assert.That(snapshot.IsComplete("chapter-03"),Is.False);
            Assert.Throws<NotSupportedException>(()=>((System.Collections.Generic.IList<string>)ChapterCatalog.All[0].QuestIds)[0]="changed");
            Assert.That(ChapterCatalog.ForQuest("bread-01").Id,Is.EqualTo("chapter-01"));Assert.That(ChapterCatalog.ForQuest("unknown"),Is.Null);
        }
        private sealed class MemoryStore:IStateStore
        {public void Save(GameState s)=>StateCodec.Validate(s);public LoadResult Load()=>throw new NotSupportedException();public void ResetSave()=>throw new NotSupportedException();}
    }
}
