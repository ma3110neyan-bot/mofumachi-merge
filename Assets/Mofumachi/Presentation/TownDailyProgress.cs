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
    // This journal has no GameState, coins, reward service or main-save dependency.
    public sealed class TownDailyService
    {
        private readonly ITownDailyStore store;private TownDailyData data;private readonly object gate=new object();
        public TownDailyData Data=>data;public string LastError {get;private set;}="";
        public bool ShouldGreet=>!data.GreetingShown;
        public bool ReadyToClaim=>data.Merges>=3&&data.Talks>=1&&data.Walks>=1&&!data.Claimed;
        public int CompletedGoals=>(data.Merges>=3?1:0)+(data.Talks>=1?1:0)+(data.Walks>=1?1:0);
        public string CharacterOfDay=>CharacterCatalog.All[(DayNumber()%6+6)%6].Id;
        public string Greeting=>CharacterCatalog.Find(CharacterOfDay).Name+"と、今日はどこへ行こう？\nお話とおさんぽで、小さな発見を集めよう♪";
        public string NextGoal=>data.Claimed?"今日のお花を集めたよ♪ 仲間に会いに行こう":ReadyToClaim?"今日のお花を受け取ろう♪":data.Talks<1?"あと1会話で、仲間をもっと知ろう♪":data.Walks<1?"あと1おさんぽで、今日のお花へ♪":"あと"+(3-data.Merges)+"合成で、今日のお花へ♪";
        public TownDailyService(ITownDailyStore store,DateTime today)
        {this.store=store??throw new ArgumentNullException(nameof(store));data=store.Load();TownDailyCodec.Validate(data);EnsureDay(today);}
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
        public bool RecordWalk()=>Mutate(()=>{if(data.Walks>=1)return false;data.Walks=1;return true;});
        public bool RecordTalk(string id)=>Mutate(()=>
        {var c=data.Characters.FirstOrDefault(x=>x.Id==id);if(c==null)return false;c.Meetings=Math.Min(999999,c.Meetings+1);data.Talks=1;return true;});
        public bool Claim()=>Mutate(()=>
        {if(!ReadyToClaim||data.Day.Length==0||data.DecorationDays.Contains(data.Day))return false;data.Claimed=true;data.DecorationDays.Add(data.Day);return true;});
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
