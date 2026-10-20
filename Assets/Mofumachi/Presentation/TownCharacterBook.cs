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
        public int Meetings {get;} public int Outings {get;} public int CompletedRequests {get;} public int Reactions {get;} public bool IsNew {get;}
        public string LatestDiscovery=>Reactions>=3?"とっておきのお話":Reactions>=2?"手を振るリアクション":Reactions>=1?"笑顔のリアクション":"まだ知らない一面";
        public string NextDiscovery=>Meetings+Outings<1?"あと1交流で、笑顔のページへ":Meetings+Outings<3?"あと"+(3-Meetings-Outings)+"交流で、手を振るページへ":Meetings<6?"あと"+(6-Meetings)+"会話で、とっておきのお話へ":"とっておきのお話を見つけたよ♪";
        internal TownBookEntry(CharacterDefinition c,string likes,string talent,string place,GameState game,TownDailyData data)
        {Id=c.Id;Name=c.Name;Personality=c.Description;Likes=likes;Talent=talent;FavoritePlace=place;var visits=data?.Characters.FirstOrDefault(x=>x.Id==c.Id);Meetings=visits?.Meetings??0;Outings=visits?.Outings??0;CompletedRequests=QuestCatalog.All.Count(q=>q.RequesterId==c.Id&&game.completedQuestIds.Contains(q.Id));Reactions=TownCharacterBook.ReactionCount(Meetings,Outings);IsNew=Reactions>(visits?.SeenReactions??0);}
    }
    public static class TownCharacterBook
    {
        public static int ReactionCount(int meetings,int outings)=>meetings>=6?3:meetings+outings>=3?2:meetings+outings>=1?1:0;
        public static string PlaceLabel(string placeId,TownDistrictId district)
        {
            if(!string.IsNullOrEmpty(placeId))
            {
                if(placeId=="quest")return "北の依頼小屋の前";
                if(placeId=="shop"||placeId=="east.workshop-front")return "東の花屋の前";
                if(placeId=="west.cafe-front")return "西のカフェの前";
                if(placeId.Contains("bench"))return DistrictLabel(district)+"のベンチ";
                if(placeId.Contains("flowers")||placeId=="flower-walk")return DistrictLabel(district)+"のお花のそば";
                if(placeId=="fountain")return "中央広場の噴水";
            }
            return DistrictLabel(district)+"をおさんぽ中";
        }
        public static string DistrictLabel(TownDistrictId district)=>district==TownDistrictId.West?"西のカフェ通り":district==TownDistrictId.East?"東の花屋通り":district==TownDistrictId.North?"北の小広場":district==TownDistrictId.South?"南の庭園":"中央広場";
        private static readonly string[] Likes={"ふわふわのパン、お友だち","お茶、新しい発見","焼きたてパン、のんびりお茶会","手作りの飾り、花束","お花、やさしい風","かわいい小物、みんなの笑顔"};
        private static readonly string[] Talents={"仲間をお茶会に誘うこと","小さな発見を見つけること","おいしいものを作ること","飾りや小物を作ること","お花を大切に育てること","お気に入りを集めること"};
        private static readonly string[] Places={"西のカフェ","中央広場の噴水","西の木陰のベンチ","東の花屋","東の花屋通り","中央広場のお祝いの道"};
        public static TownBookEntry For(string id,GameState game,TownDailyData data)
        {var c=CharacterCatalog.Find(id)??throw new ArgumentException("Unknown character",nameof(id));int i=c.PortraitIndex;return new TownBookEntry(c,Likes[i],Talents[i],Places[i],game,data);}
        public static IEnumerable<string> Text=>Likes.Concat(Talents).Concat(Places).Concat(new[]{"もふまちキャラクター図鑑","好きなもの","得意なこと","好きな場所","出会った回数","達成した依頼","リアクション","？？？","笑顔","手を振る","とっておきのお話","まだ知らない一面","笑顔のリアクション","手を振るリアクション","会いに行く","お店の前で、ドアをタップ。","次のお願いを見に行く","Mergeで届けものづくり","街で会って、お話しよう♪","おさんぽでも新しい一面が見つかるよ","この子に届けたお願い","お届けのおもいでも、このページに♪","北の小広場","南の庭園","をおさんぽ中","お花のそば"});
    }
}
