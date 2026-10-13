using System;
using System.Collections.Generic;
using System.Linq;
namespace Mofumachi.Core
{
    public enum TownActivity { Square, Bench, Flowers, HomeFront, ShopFront, Fountain, Celebration, Entrance }
    public sealed class TownPlace
    {
        public string Id {get;} public string Name {get;} public string Dialogue {get;}
        public TownPoint Position {get;} public int MinLevel {get;} public TownActivity Activity {get;}
        internal TownPlace(string id,string name,TownPoint point,int level,TownActivity activity,string dialogue)
        {Id=id;Name=name;Position=point;MinLevel=level;Activity=activity;Dialogue=dialogue;}
    }
    public sealed class TownStreetMap
    {
        public IReadOnlyList<TownPoint> Nodes {get;} public IReadOnlyList<TownEdge> Edges {get;} public IReadOnlyList<TownPlace> Places {get;}
        internal TownStreetMap(TownPoint[] nodes,TownEdge[] edges,TownPlace[] places)
        {Nodes=Array.AsReadOnly(nodes);Edges=Array.AsReadOnly(edges);Places=Array.AsReadOnly(places);}
    }
    // Presentation-independent street data. Growth unlocks walks, never access to existing quests.
    public static class TownPlaceCatalog
    {
        public static readonly TownPoint RequestEntrance=new TownPoint(.18f,.635f),ShopEntrance=new TownPoint(.76f,.605f);
        private static readonly TownPoint[] Points={
            new TownPoint(.42f,.47f),new TownPoint(.68f,.515f),new TownPoint(.68f,.60f),new TownPoint(.42f,.645f),
            new TownPoint(.18f,.60f),new TownPoint(.15f,.52f),RequestEntrance,ShopEntrance,
            new TownPoint(.30f,.425f),new TownPoint(.66f,.46f),new TownPoint(.28f,.60f),
            new TownPoint(.70f,.545f),new TownPoint(.54f,.40f),new TownPoint(.49f,.34f),new TownPoint(.50f,.275f)};
        private static readonly TownEdge[] Streets={new TownEdge(0,1),new TownEdge(1,2),new TownEdge(2,3),new TownEdge(3,4),new TownEdge(4,5),new TownEdge(5,0),new TownEdge(4,6),new TownEdge(2,7),new TownEdge(0,8),new TownEdge(1,9),new TownEdge(4,10),new TownEdge(1,11),new TownEdge(0,12),new TownEdge(12,13),new TownEdge(13,14)};
        private static TownPlace Place(string id,string name,int node,int level,TownActivity activity,string copy)=>new TownPlace(id,name,Points[node],level,activity,copy);
        private static readonly TownPlace[] Places={
            Place("square","広場",0,0,TownActivity.Square,"広場で待ち合わせしよう♪"),
            Place("fountain","噴水",3,0,TownActivity.Fountain,"噴水の音が心地いいね。"),
            Place("flowers","お花",5,0,TownActivity.Flowers,"お花を眺めて、のんびり。"),
            Place("quest","依頼部屋",6,0,TownActivity.Entrance,"今日は何を届けようかな？"),
            Place("shop","ショップ",7,0,TownActivity.ShopFront,"お店の前で待ち合わせ♪"),
            Place("bench-left","左のベンチ",8,0,TownActivity.Bench,"ベンチのそばでひと休み♪"),
            Place("bench-right","右のベンチ",9,0,TownActivity.Bench,"ベンチのそばでひと休み♪"),
            Place("home-front","家の前",10,0,TownActivity.HomeFront,"お茶のいい香りがするね。"),
            Place("flower-walk","花の小道",11,2,TownActivity.Flowers,"花の小道が広がったね！"),
            Place("front-promenade","石畳の散歩道",13,4,TownActivity.Square,"街の先までおさんぽしよう♪"),
            Place("celebration","お祝いの道",14,6,TownActivity.Celebration,"みんなで街のお祝いをしよう！")};
        private static readonly TownStreetMap[] Stages={Build(0),Build(2),Build(4),Build(6)};
        private static TownStreetMap Build(int level)
        {
            int count=level>=6?15:level>=4?14:level>=2?12:11;
            return new TownStreetMap(Points.Take(count).ToArray(),Streets.Where(e=>e.From<count&&e.To<count).ToArray(),Places.Where(p=>p.MinLevel<=level).ToArray());
        }
        public static TownStreetMap ForLevel(int level)=>Stages[level>=6?3:level>=4?2:level>=2?1:0];
        public static bool TryFind(TownStreetMap map,string id,out TownPlace place)
        {place=map?.Places.FirstOrDefault(p=>p.Id==id);return place!=null;}
        public static string GrowthHint(int level)=>level<2?"街 Lv.2で花の小道へ":level<4?"街 Lv.4で石畳の散歩道へ":level<6?"街 Lv.6でお祝いの道へ":"お祝いの道まで広がりました♪";
    }
}
