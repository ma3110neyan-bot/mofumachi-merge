using System;
using System.Linq;

namespace Mofumachi.Core
{
    // Local painting coordinates and connections remain independent of how the camera
    // presents them. A future continuous world can use ToWorld without rewriting visits.
    public static class TownRegionCatalog
    {
        private static readonly TownPoint[] WestPoints={
            new TownPoint(.94f,.55f),new TownPoint(.85f,.57f),new TownPoint(.70f,.56f),new TownPoint(.50f,.50f),
            new TownPoint(.59f,.46f),new TownPoint(.52f,.40f),new TownPoint(.39f,.36f),new TownPoint(.42f,.30f)};
        private static readonly TownPoint[] EastPoints={
            new TownPoint(.09f,.50f),new TownPoint(.22f,.48f),new TownPoint(.35f,.42f),
            new TownPoint(.48f,.43f),new TownPoint(.61f,.46f),new TownPoint(.66f,.415f)};
        private static readonly TownEdge[] WestEdges={new TownEdge(0,1),new TownEdge(1,2),new TownEdge(2,3),new TownEdge(3,4),new TownEdge(3,5),new TownEdge(5,6),new TownEdge(5,7)};
        private static readonly TownEdge[] EastEdges={new TownEdge(0,1),new TownEdge(1,2),new TownEdge(2,3),new TownEdge(3,4),new TownEdge(3,5)};
        private static TownPlace Place(string id,string name,TownPoint point,int level,TownActivity activity,string dialogue)=>new TownPlace(id,name,point,level,activity,dialogue);
        private static readonly TownPlace[] WestPlaces={
            Place("west.entry","中央へ",WestPoints[0],0,TownActivity.Entrance,"噴水の広場へもどろう♪"),
            Place("west.cafe-front","カフェ前",WestPoints[3],2,TownActivity.HomeFront,"カフェからお茶のいい香りがするね♪"),
            Place("west.flowers","花の庭",WestPoints[4],0,TownActivity.Flowers,"お花に囲まれて、のんびりしよう。"),
            Place("west.bench-front","木陰のベンチ",WestPoints[6],0,TownActivity.Bench,"木陰でひと休み。風がやさしいね。"),
            Place("west.meeting","庭の待ち合わせ",WestPoints[7],0,TownActivity.Square,"一緒にお茶会のお話をしよう♪")};
        private static readonly TownPlace[] EastPlaces={
            Place("east.entry","中央へ",EastPoints[0],0,TownActivity.Entrance,"中央の街へもどろう♪"),
            Place("east.meeting","小道の待ち合わせ",EastPoints[2],0,TownActivity.Square,"どんな飾りをつくろうかな？"),
            Place("east.flowers","花の小道",EastPoints[3],0,TownActivity.Flowers,"きれいな花を見つけたよ♪"),
            Place("east.workshop-front","工房前",EastPoints[4],4,TownActivity.HomeFront,"工房には、かわいい飾りがいっぱい！"),
            Place("east.bench-front","小道のベンチ",EastPoints[5],0,TownActivity.Bench,"おさんぽのあとにひと休み♪")};
        private static readonly TownRegionDefinition[,] Stages=BuildStages();
        private static TownRegionDefinition[,] BuildStages()
        {
            var stages=new TownRegionDefinition[3,4];int[] levels={0,2,4,6};
            for(int i=0;i<levels.Length;i++)
            {
                int level=levels[i];
                stages[(int)TownRegionId.West,i]=new TownRegionDefinition(TownRegionId.West,"西のカフェ",new TownPoint(-1,0),"Mofumachi/TownWestBackground",new TownStreetMap(WestPoints,WestEdges,WestPlaces.Where(p=>p.MinLevel<=level).ToArray()));
                stages[(int)TownRegionId.Central,i]=new TownRegionDefinition(TownRegionId.Central,"中央の街",new TownPoint(0,0),"Mofumachi/TownBackground",TownPlaceCatalog.ForLevel(level));
                stages[(int)TownRegionId.East,i]=new TownRegionDefinition(TownRegionId.East,"東の小道",new TownPoint(1,0),"Mofumachi/TownEastBackground",new TownStreetMap(EastPoints,EastEdges,EastPlaces.Where(p=>p.MinLevel<=level).ToArray()));
            }
            return stages;
        }
        public static TownRegionDefinition ForLevel(TownRegionId region,int level)
        {
            if(!Enum.IsDefined(typeof(TownRegionId),region))throw new ArgumentOutOfRangeException(nameof(region));
            return Stages[(int)region,level>=6?3:level>=4?2:level>=2?1:0];
        }
        public static bool TryFindPlace(string id,int level,out TownRegionLocation location,out TownPlace place)
        {
            foreach(var region in new[]{TownRegionId.Central,TownRegionId.West,TownRegionId.East})
                if(TownPlaceCatalog.TryFind(ForLevel(region,level).Map,id,out place))
                {location=new TownRegionLocation(region,place.Position);return true;}
            location=default;place=null;return false;
        }
        public static bool TryGetPortal(TownRegionId from,TownRegionId to,out TownRegionPortal portal)
        {
            if(from==TownRegionId.Central&&to==TownRegionId.West){portal=new TownRegionPortal(new TownRegionLocation(from,new TownPoint(.15f,.52f)),new TownRegionLocation(to,WestPoints[0]));return true;}
            if(from==TownRegionId.Central&&to==TownRegionId.East){portal=new TownRegionPortal(new TownRegionLocation(from,new TownPoint(.68f,.515f)),new TownRegionLocation(to,EastPoints[0]));return true;}
            if(to==TownRegionId.Central&&(from==TownRegionId.West||from==TownRegionId.East))
            {TryGetPortal(to,from,out var reverse);portal=new TownRegionPortal(reverse.To,reverse.From);return true;}
            portal=default;return false;
        }
        public static TownPoint ToWorld(TownRegionLocation location)
        {
            var origin=ForLevel(location.Region,0).WorldOrigin;
            return new TownPoint(origin.X+location.Local.X,origin.Y+location.Local.Y);
        }
    }
}
