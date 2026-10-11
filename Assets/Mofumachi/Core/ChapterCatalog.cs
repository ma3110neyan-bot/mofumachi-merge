using System;
using System.Collections.Generic;
using System.Linq;
namespace Mofumachi.Core
{
    public sealed class ChapterDefinition
    {
        public string Id { get; } public string Title { get; } public string Objective { get; } public string ClosingLine { get; }
        public IReadOnlyList<string> QuestIds { get; }
        internal ChapterDefinition(string id,string title,string objective,string closing,params string[] quests)
        {Id=id;Title=title;Objective=objective;ClosingLine=closing;QuestIds=Array.AsReadOnly((string[])quests.Clone());}
    }
    public static class ChapterCatalog
    {
        public static IReadOnlyList<ChapterDefinition> All { get; }=Array.AsReadOnly(new[]{
            new ChapterDefinition("chapter-01","はじめてのお茶会","お茶とパンをそろえて、仲間を迎えよう","仲間と囲む、最初のお茶会ができました。","tea-01","bread-01"),
            new ChapterDefinition("chapter-02","花の香る広場","花を飾って、ひと休みできる広場へ","花の香る広場で、みんながひと休み。","flower-01","tea-02"),
            new ChapterDefinition("chapter-03","もふまちのお祝い","彩った広場で、みんなのお祝いをしよう","みんなのお茶会がひらけました！","flower-02","bread-02")});
        public static ChapterDefinition Find(string id)=>All.FirstOrDefault(c=>c.Id==id);
        public static ChapterDefinition ForQuest(string id)=>All.FirstOrDefault(c=>c.QuestIds.Contains(id));
    }
}
