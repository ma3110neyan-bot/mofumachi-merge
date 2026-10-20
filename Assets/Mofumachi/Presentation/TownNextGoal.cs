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
            if(quest==null)
            {
                if(state.activeQuestId.Length>0&&!state.completedQuestIds.Contains(state.activeQuestId)||!ChapterProgress.For(state).AllCompleted)
                    return new TownNextGoal("依頼を確認しよう","保存された依頼を確認できません。");
                return new TownNextGoal("今日も、仲間に会いに行こう♪","街のあちこちで、新しい一面に会いに行こう♪",finale:true);
            }
            if(state.townGrowthLevel>=6)return new TownNextGoal("物語のつづきへ",CharacterCatalog.Find(quest.RequesterId).Name+"の「"+quest.Title+"」",quest.Id);
            int next=Math.Max(0,state.townGrowthLevel)+1;
            return new TownNextGoal("あと1お届けで、街に変化♪",CharacterCatalog.Find(quest.RequesterId).Name+"へ → Lv."+next+" · "+UnlockAt(next),quest.Id,next,1);
        }
        // A one-line invitation on the compact street bar; full details stay in
        // the actual quest, book and Today screens.
        public static string CompactDetail(GameState state,TownDailyService daily)
        {
            var goal=For(state);
            if(!goal.IsFinale)
            {
                string[] shortUnlocks={"","お茶会のお花","カフェの席","街灯とベンチ","花屋の飾り","お祝いのアーチ","街のリボン"};
                return goal.NextLevel>0&&goal.NextLevel<shortUnlocks.Length?"Lv."+goal.NextLevel+" · "+shortUnlocks[goal.NextLevel]:"依頼小屋で物語のつづきを";
            }
            if(daily.Data.Claimed)return daily.NewDiscoveryCount>0?"図鑑に新しい発見があるよ♪":"仲間の新しい一面を探そう";
            if(daily.ReadyToClaim)return "今日のお花を受け取ろう♪";
            if(daily.Data.Talks<1)return "あと1会話で、小さな発見♪";
            if(daily.Data.Walks<1)return "東西の通りを歩いてみよう";
            return "あと"+(3-daily.Data.Merges)+"合成で今日のお花♪";
        }
        public static IEnumerable<string> Text=>Unlocks.Concat(new[]{"依頼を確認しよう","今日も、仲間に会いに行こう♪","街のあちこちで、新しい一面に会いに行こう♪","物語のつづきへ","あと1お届けで、街に変化♪"});
    }
}
