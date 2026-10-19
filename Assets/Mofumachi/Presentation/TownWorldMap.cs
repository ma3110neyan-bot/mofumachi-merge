using System;
using System.Collections.Generic;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    // Continuous presentation coordinates. Stable Core place IDs retain their activities.
    public static class TownWorldMap
    {
        public const string Resource="Mofumachi/TownSeamlessMap";
        public const float Left=-1.8f,Bottom=-3f,Width=3.6f,Height=6f;
        public const float Right=Left+Width,Top=Bottom+Height;
        public static TownPoint Point(float u,float v)=>new TownPoint(Left+Width*u,Top-Height*v);
        private static readonly Dictionary<string,TownPoint> anchors=new Dictionary<string,TownPoint>{
            {"square",new TownPoint(0,-.25f)},{"fountain",new TownPoint(0,.08f)},{"flowers",new TownPoint(-.34f,-.10f)},
            {"quest",new TownPoint(0,2.08f)},{"shop",new TownPoint(1.2f,.04f)},
            {"bench-left",new TownPoint(-.30f,1.55f)},{"bench-right",new TownPoint(.32f,-.40f)},
            {"home-front",new TownPoint(.32f,2.05f)},{"flower-walk",new TownPoint(1.47f,-.20f)},
            {"front-promenade",new TownPoint(-.30f,-2.35f)},{"celebration",new TownPoint(0,-2.50f)},
            {"west.entry",new TownPoint(-1.2f,-.30f)},{"west.cafe-front",new TownPoint(-1.2f,.04f)},
            {"west.flowers",new TownPoint(-1.48f,-.25f)},{"west.bench-front",new TownPoint(-.98f,-.40f)},{"west.meeting",new TownPoint(-1.02f,-.12f)},
            {"east.entry",new TownPoint(1.2f,-.30f)},{"east.meeting",new TownPoint(1.0f,-.40f)},
            {"east.flowers",new TownPoint(1.48f,-.24f)},{"east.workshop-front",new TownPoint(1.46f,.04f)},{"east.bench-front",new TownPoint(1.48f,-.60f)},
            {"district.west.entry",new TownPoint(-1.2f,-.30f)},{"district.central.entry",new TownPoint(0,-.25f)},
            {"district.east.entry",new TownPoint(1.2f,-.30f)},{"district.north.entry",new TownPoint(0,1.65f)},{"district.south.entry",new TownPoint(0,-2.25f)}};
        public static TownPoint Place(string id)=>anchors.TryGetValue(id??"",out var p)?p:throw new ArgumentException("Unknown map place",nameof(id));
        public static TownPoint Growth(TownRegionId region,TownGrowthPlacement prop)=>prop.Position;
        public static bool TryPlace(string id,out TownPoint p)=>anchors.TryGetValue(id??"",out p);
    }
}
