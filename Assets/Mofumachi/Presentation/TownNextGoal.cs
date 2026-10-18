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
                return new TownNextGoal("みんなのお茶会ができました♪","街のあちこちの仲間に会いに行こう♪",finale:true);
            }
            if(state.townGrowthLevel>=6)return new TownNextGoal("物語のつづきへ",CharacterCatalog.Find(quest.RequesterId).Name+"の「"+quest.Title+"」",quest.Id);
            int next=Math.Max(0,state.townGrowthLevel)+1;
            return new TownNextGoal("あと1お届けで、街に変化♪","Lv."+next+" · "+UnlockAt(next),quest.Id,next,1);
        }
        public static IEnumerable<string> Text=>Unlocks.Concat(new[]{"依頼を確認しよう","みんなのお茶会ができました♪","街のあちこちの仲間に会いに行こう♪","物語のつづきへ","あと1お届けで、街に変化♪"});
    }
}
