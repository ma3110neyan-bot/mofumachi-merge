using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    [DataContract] public sealed class TownCharacterVisit
    {
        [DataMember(IsRequired=true)] public string Id;
        [DataMember(IsRequired=true)] public int Meetings;
        [DataMember(EmitDefaultValue=false)] public int Outings;
        [DataMember(EmitDefaultValue=false)] public int SeenReactions;
        public TownCharacterVisit(string id){Id=id;}
    }
    [DataContract] public sealed class TownDailyData
    {
        [DataMember(IsRequired=true)] public int Version=1;
        [DataMember(IsRequired=true)] public string Day="";
        [DataMember(IsRequired=true)] public bool GreetingShown;
        [DataMember(IsRequired=true)] public bool Claimed;
        [DataMember(IsRequired=true)] public int Merges,Talks,Walks;
        [DataMember(IsRequired=true)] public List<TownCharacterVisit> Characters=CharacterCatalog.All.Select(c=>new TownCharacterVisit(c.Id)).ToList();
        [DataMember(IsRequired=true)] public List<string> DecorationDays=new List<string>();
    }
    // The journal is saved first; optional point receipts are committed by the main-save owner.
    public sealed class TownDailyService
    {
        private readonly ITownDailyStore store;private TownDailyData data;private readonly object gate=new object();
        private readonly MofuPointService points;private bool processingRewardEvent;
        public TownDailyData Data=>data;public string LastError {get;private set;}="";
        public bool ShouldGreet=>!data.GreetingShown;
        public bool ReadyToClaim=>data.Merges>=3&&data.Talks>=1&&data.Walks>=1&&!data.Claimed;
        public int CompletedGoals=>(data.Merges>=3?1:0)+(data.Talks>=1?1:0)+(data.Walks>=1?1:0);
        public int NewDiscoveryCount=>data.Characters.Count(c=>TownCharacterBook.ReactionCount(c.Meetings,c.Outings)>c.SeenReactions);
        public string FirstNewDiscovery=>data.Characters.FirstOrDefault(c=>TownCharacterBook.ReactionCount(c.Meetings,c.Outings)>c.SeenReactions)?.Id;
        public string CharacterOfDay=>CharacterCatalog.All[(DayNumber()%6+6)%6].Id;
        public string Greeting=>CharacterCatalog.Find(CharacterOfDay).Name+"と、今日はどこへ？\nお話とおさんぽで、\n小さな発見を集めよう♪";
        public string NextGoal=>data.Claimed?"今日のお花を集めたよ♪ 仲間に会いに行こう":ReadyToClaim?"今日のお花を受け取ろう♪":data.Talks<1?"あと1会話で、仲間をもっと知ろう♪":data.Walks<1?"あと1おさんぽで、今日のお花へ♪":"あと"+(3-data.Merges)+"合成で、今日のお花へ♪";
        public TownDailyService(ITownDailyStore store,DateTime today,MofuPointService points=null)
        {this.store=store??throw new ArgumentNullException(nameof(store));this.points=points;data=store.Load();TownDailyCodec.Validate(data);if(EnsureDay(today))ReconcileFlower(today);}
        private int DayNumber()=>DateTime.TryParseExact(data.Day,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var d)?(int)(d-new DateTime(2020,1,1)).TotalDays:0;
        public bool EnsureDay(DateTime day)
        {
            string key=day.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
            // ISO order is chronological; moving a device clock backwards never reopens a claim.
            if(string.CompareOrdinal(key,data.Day)<=0)return true;
            return Mutate(()=>{data.Day=key;data.GreetingShown=data.Claimed=false;data.Merges=data.Talks=data.Walks=0;return true;});
        }
        public bool MarkGreeting()=>Mutate(()=>{if(data.GreetingShown)return false;data.GreetingShown=true;return true;});
        public bool RecordMerge()=>Mutate(()=>{if(data.Merges>=3)return false;data.Merges++;return true;});
        // Explicit lifecycle/action entry only. EnsureDay and rendering never award recovery.
        public bool RecoverDay(DateTime today)=>RewardEvent(()=>
        {string day=Key(today);return data.Day==day&&points!=null&&points.TryRewardDailyRecovery(day);});
        public bool ResumeDay(DateTime today,bool wasPaused)=>wasPaused&&EnsureDay(today)&&RecoverDay(today);
        public bool RecoverAfterAction(DateTime today,bool mainPersistenceHealthy)=>mainPersistenceHealthy&&LastError.Length==0&&RecoverDay(today);
        public bool RecordWalk(string walkId=null,string startedDay=null,DateTime? today=null,WalkRewardKind? rewardKind=WalkRewardKind.Normal)=>RewardEvent(()=>
        {
            string day=Key(today??DateTime.Now);
            if(points!=null&&walkId!=null&&startedDay!=day)return false;
            bool already=data.Walks>=1;
            bool recorded=already?false:Mutate(()=>{data.Walks=1;return true;});
            bool rewarded=(already||recorded)&&data.Day==day&&points!=null&&rewardKind.HasValue&&points.TryRewardWalk(day,startedDay,walkId,rewardKind.Value);
            return recorded||rewarded;
        });
        public bool RecordTalk(string id,DateTime? today=null)=>RewardEvent(()=>
        {
            string day=Key(today??DateTime.Now);
            bool recorded=Mutate(()=>{var c=data.Characters.FirstOrDefault(x=>x.Id==id);if(c==null)return false;c.Meetings=Math.Min(999999,c.Meetings+1);data.Talks=1;return true;});
            if(recorded&&data.Day==day)points?.TryRewardTalk(day,id);
            return recorded;
        });
        public bool RecordOuting(string id)=>Mutate(()=>
        {var c=data.Characters.FirstOrDefault(x=>x.Id==id);if(c==null||c.Outings>=999999)return false;c.Outings++;return true;});
        public bool MarkBookSeen(string id)=>Mutate(()=>
        {var c=data.Characters.FirstOrDefault(x=>x.Id==id);if(c==null)return false;int count=TownCharacterBook.ReactionCount(c.Meetings,c.Outings);if(c.SeenReactions>=count)return false;c.SeenReactions=count;return true;});
        public bool Claim(DateTime? today=null)=>RewardEvent(()=>
        {
            bool claimed=Mutate(()=>{if(!ReadyToClaim||data.Day.Length==0||data.DecorationDays.Contains(data.Day))return false;data.Claimed=true;data.DecorationDays.Add(data.Day);return true;});
            ReconcileFlower(today??DateTime.Now);
            return claimed;
        });
        private static string Key(DateTime day)=>day.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
        private void ReconcileFlower(DateTime today)
        {
            string day=Key(today);
            if(data.Day==day&&data.Claimed&&data.DecorationDays.Contains(day))points?.TryRewardFlower(day);
        }
        private bool RewardEvent(Func<bool> action)
        {
            lock(gate)
            {
                if(processingRewardEvent)return false;processingRewardEvent=true;
                try{return action();}finally{processingRewardEvent=false;}
            }
        }
        private bool Mutate(Func<bool> action)
        {
            lock(gate)
            {
                var before=TownDailyCodec.Clone(data);if(!action())return false;
                try{store.Save(data);LastError="";return true;}
                catch(IOException){data=before;LastError="今日の記録を保存できませんでした。もう一度試してください。";return false;}
                catch(UnauthorizedAccessException){data=before;LastError="今日の記録の保存先を確認してください。";return false;}
            }
        }
    }
}
