using System;
using System.Collections.Generic;
using System.Linq;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    // A promise derived from existing progress, never a second quest/reward system.
    public sealed class TownNextGoal
    {
        public string Title {get;} public string Detail {get;} public string QuestId {get;}
        public int NextLevel {get;} public int RemainingDeliveries {get;} public bool IsFinale {get;}
        private TownNextGoal(string title,string detail,string quest="",int level=0,int remaining=0,bool finale=false)
        {Title=title;Detail=detail;QuestId=quest;NextLevel=level;RemainingDeliveries=remaining;IsFinale=finale;}
        private static readonly string[] Unlocks={"","お茶会のお花","カフェの席と花の小道","お花の街灯","工房の飾りと広場の散歩道","お祝いのアーチ","リボンとお祝いの道"};
        public static string UnlockAt(int level)=>level>0&&level<Unlocks.Length?Unlocks[level]:"";
        public static TownNextGoal For(GameState state)
        {
            if(state==null)throw new ArgumentNullException(nameof(state));
            var quest=QuestCatalog.NextFor(state);
            bool finale=quest==null&&ChapterProgress.For(state).AllCompleted;
            if(!TownGrowthService.TryGetNextRequirements(state.townGrowthLevel,out int next,out int requests,out long actions))
                return new TownNextGoal("街Lv6 MAX","今日も、仲間に会いに行こう♪",quest?.Id??"",finale:finale);
            int completed=QuestCatalog.CompletedCount(state);
            return new TownNextGoal("Lv"+next+"まで おねがい "+completed+"/"+requests,
                "まち交流 "+state.economy.townActionTotal+"/"+actions,quest?.Id??"",next,Math.Max(0,requests-completed),finale);
        }
        // A one-line invitation on the compact street bar; full details stay in
        // the actual quest, book and Today screens.
        public static string CompactDetail(GameState state,TownDailyService daily)
        {
            return For(state).Detail;
        }
        public static IEnumerable<string> Text=>Unlocks.Concat(new[]{"依頼を確認しよう","今日も、仲間に会いに行こう♪","Lvまで おねがい まち交流 街Lv6 MAX 所持 P ・ 生成"});
    }
}
