using System;
using System.Collections.Generic;
using System.Linq;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    public sealed class TownBookEntry
    {
        public string Id {get;} public string Name {get;} public string Personality {get;}
        public string Likes {get;} public string Talent {get;} public string FavoritePlace {get;}
        public int Meetings {get;} public int CompletedRequests {get;} public int Reactions {get;}
        public string NextDiscovery=>Meetings<1?"あと1会話で、笑顔のページへ":Meetings<3?"あと"+(3-Meetings)+"会話で、手を振るページへ":Meetings<6?"あと"+(6-Meetings)+"会話で、とっておきのお話へ":"とっておきのお話を見つけたよ♪";
        internal TownBookEntry(CharacterDefinition c,string likes,string talent,string place,GameState game,TownDailyData data)
        {Id=c.Id;Name=c.Name;Personality=c.Description;Likes=likes;Talent=talent;FavoritePlace=place;Meetings=data?.Characters.FirstOrDefault(x=>x.Id==c.Id)?.Meetings??0;CompletedRequests=QuestCatalog.All.Count(q=>q.RequesterId==c.Id&&game.completedQuestIds.Contains(q.Id));Reactions=Meetings>=6?3:Meetings>=3?2:Meetings>=1?1:0;}
    }
    public static class TownCharacterBook
    {
        private static readonly string[] Likes={"ふわふわのパン、お友だち","お茶、新しい発見","焼きたてパン、のんびりお茶会","手作りの飾り、花束","お花、やさしい風","かわいい小物、みんなの笑顔"};
        private static readonly string[] Talents={"仲間をお茶会に誘うこと","小さな発見を見つけること","おいしいものを作ること","飾りや小物を作ること","お花を大切に育てること","お気に入りを集めること"};
        private static readonly string[] Places={"北のカフェ","中央公園の噴水","北の木陰のベンチ","南の工房","南の花の小道","中央公園のお祝いの道"};
        public static TownBookEntry For(string id,GameState game,TownDailyData data)
        {var c=CharacterCatalog.Find(id)??throw new ArgumentException("Unknown character",nameof(id));int i=c.PortraitIndex;return new TownBookEntry(c,Likes[i],Talents[i],Places[i],game,data);}
        public static IEnumerable<string> Text=>Likes.Concat(Talents).Concat(Places).Concat(new[]{"もふまちキャラクター図鑑","好きなもの","得意なこと","好きな場所","出会った回数","達成した依頼","リアクション","？？？","笑顔","手を振る","とっておきのお話"});
    }
}
