using System;
using System.Collections.Generic;
using System.Linq;
namespace Mofumachi.Core
{
    public sealed class ChapterProgress
    {
        private readonly HashSet<string> completed;
        public int CompletedCount { get; }
        public ChapterDefinition CurrentChapter { get; }
        public int CompletedInCurrent { get; }
        public bool AllCompleted { get; }
        public ChapterDefinition LastCompletedChapter { get; }
        private ChapterProgress(GameState state)
        {
            completed=new HashSet<string>(state.completedQuestIds,StringComparer.Ordinal);
            CompletedCount=QuestCatalog.All.Count(q=>completed.Contains(q.Id));AllCompleted=CompletedCount==QuestCatalog.All.Count;
            var active=!completed.Contains(state.activeQuestId)?ChapterCatalog.ForQuest(state.activeQuestId):null;
            CurrentChapter=active??ChapterCatalog.All.FirstOrDefault(c=>!IsComplete(c.Id));
            CompletedInCurrent=CurrentChapter==null?0:CurrentChapter.QuestIds.Count(completed.Contains);
            LastCompletedChapter=ChapterCatalog.All.LastOrDefault(c=>IsComplete(c.Id));
        }
        public static ChapterProgress For(GameState state)=>new ChapterProgress(state??throw new ArgumentNullException(nameof(state)));
        public bool IsComplete(string id){var c=ChapterCatalog.Find(id);return c!=null&&c.QuestIds.All(completed.Contains);}
    }
}
