using System;
using System.Collections.Generic;
using System.Linq;
namespace Mofumachi.Core
{
    public static class QuestCatalog
    {
        public static IReadOnlyList<QuestDefinition> All { get; }=Array.AsReadOnly(new[]{
            QuestDefinition.First,
            new QuestDefinition("bread-01",new[]{new ItemRequirement("bread",2,1)},40,"momo","焼きたてのおすそわけ","みんなで、おいしいパンを食べよう！"),
            new QuestDefinition("flower-01",new[]{new ItemRequirement("flower",2,1)},40,"miel","小さな花束","お茶会のお部屋に、お花を飾りたいな。"),
            new QuestDefinition("tea-02",new[]{new ItemRequirement("tea",3,1),new ItemRequirement("bread",2,1)},70,"pom","のんびりお茶会","香りのよいお茶とパンで、ひと休みしよう。"),
            new QuestDefinition("flower-02",new[]{new ItemRequirement("flower",3,1),new ItemRequirement("tea",2,1)},70,"fio","花いっぱいの広場","広場をお花で彩って、みんなを迎えよう！"),
            new QuestDefinition("bread-02",new[]{new ItemRequirement("bread",3,1),new ItemRequirement("flower",2,1)},80,"noah","街のごちそう会","パンと花束をそろえて、街のお祝いをしよう。")});
        public static QuestDefinition NextFor(GameState state)
        {
            if(state==null)throw new ArgumentNullException(nameof(state));
            if(state.activeQuestId.Length>0 && !state.completedQuestIds.Contains(state.activeQuestId))
                return All.FirstOrDefault(q=>q.Id==state.activeQuestId);
            return All.FirstOrDefault(q=>!state.completedQuestIds.Contains(q.Id));
        }
    }
}
