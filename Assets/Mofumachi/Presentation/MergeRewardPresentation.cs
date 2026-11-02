using System;
using System.Collections.Generic;
using System.Linq;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    // Read-only copy/layout for the approved reward screen. No game rules or saves.
    public static class MergeRewardPresentation
    {
        // Only native editor evidence can reconstruct the preceding UI. Players
        // always use the candidate; this switch is never saved or shipped writable.
        public static bool CaptureBefore
        {
#if UNITY_EDITOR
            get;set;
#else
            get=>false;
#endif
        }
        public static string Coins(QuestDefinition quest)=>quest==null?"":quest.Coins+" Coins";
        public static bool Needs(IReadOnlyList<RequirementProgress> progress,string id)
            =>progress!=null&&progress.Any(p=>p.Requirement.ItemId==id&&p.Available<p.Requirement.Count);
        public static float Fraction(IReadOnlyList<RequirementProgress> progress)
        {
            if(progress==null||progress.Count==0)return 0;
            double total=0,done=0;foreach(var p in progress){total+=p.Requirement.Count;done+=Math.Min(Math.Max(0,p.Available),(long)p.Requirement.Count);}
            return total<=0?0:(float)Math.Min(1,Math.Max(0,done/total));
        }
        public static bool ReviewBeforeDelivery(bool hasQuest,bool accepted,bool deliverable)=>hasQuest&&(!accepted||!deliverable);
        public static float CaptionSpace(float height,MergeScreenLayout layout)
            =>layout.TwoToolbarRows&&height-layout.TrayTop-layout.TrayBottom-layout.GridInset*2>=322?14:0;
        public static string Task(QuestDefinition quest)
        {
            if(quest==null)return "街で続きを見よう！";
            if(quest.Requirements.Count!=1)return "依頼のアイテムを作ろう！";
            var r=quest.Requirements[0];return (ItemCatalog.Find(r.ItemId)?.Name??"アイテム")+" Lv."+r.Level+"を"+r.Count+"個作ろう！";
        }
    }
}
