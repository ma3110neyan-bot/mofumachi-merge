using System;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    public sealed class ChapterPresentation
    {
        public string ChapterId { get; } public string Title { get; } public string Objective { get; } public string ClosingLine { get; }
        public string FavoriteCharacterId { get; } public string NextChapterTitle { get; }
        public int CompletedCount { get; } public int Coins { get; } public int TownLevel { get; } public bool IsFinale { get; }
        private ChapterPresentation(GameState state,ChapterDefinition chapter,ChapterProgress progress)
        {ChapterId=chapter.Id;Title=chapter.Title;Objective=chapter.Objective;ClosingLine=chapter.ClosingLine;FavoriteCharacterId=state.favoriteCharacterId;NextChapterTitle=progress.CurrentChapter?.Title??"";CompletedCount=progress.CompletedCount;Coins=state.coins;TownLevel=state.townGrowthLevel;IsFinale=progress.AllCompleted&&chapter.Id=="chapter-03";}
        public static ChapterPresentation Create(GameState state,string chapterId)
        {
            var progress=ChapterProgress.For(state);var chapter=ChapterCatalog.Find(chapterId);
            if(chapter==null||!progress.IsComplete(chapterId))throw new ArgumentException("Chapter must be known and completed.",nameof(chapterId));
            return new ChapterPresentation(state,chapter,progress);
        }
    }
}
