using System;
using System.Collections.Generic;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    // One authored map. UVs below are measured from its top-left corner.
    // Stable Core IDs remain unchanged; only their presentation anchors move.
    public static class TownWorldMap
    {
        public const string Resource="Mofumachi/TownSeamlessMap";
        public const float Left=-1f,Bottom=-.6f,Width=3f,Height=2f;
        public const float Right=Left+Width,Top=Bottom+Height;
        public static TownPoint Point(float u,float v)=>new TownPoint(Left+Width*u,Top-Height*v);
        private static readonly Dictionary<string,TownPoint> anchors=new Dictionary<string,TownPoint>{
            {"square",Point(.52f,.68f)},{"fountain",Point(.52f,.595f)},{"flowers",Point(.43f,.60f)},
            {"quest",Point(.171f,.290f)},{"shop",Point(.815f,.485f)},
            {"bench-left",Point(.43f,.73f)},{"bench-right",Point(.63f,.73f)},
            {"home-front",Point(.45f,.37f)},{"flower-walk",Point(.69f,.52f)},
            {"front-promenade",Point(.52f,.80f)},{"celebration",Point(.51f,.83f)},
            {"west.entry",Point(.31f,.55f)},{"west.cafe-front",Point(.261f,.50f)},
            {"west.flowers",Point(.25f,.68f)},{"west.bench-front",Point(.33f,.73f)},{"west.meeting",Point(.36f,.53f)},
            {"east.entry",Point(.80f,.58f)},{"east.meeting",Point(.84f,.57f)},
            {"east.flowers",Point(.80f,.70f)},{"east.workshop-front",Point(.817f,.49f)},{"east.bench-front",Point(.84f,.70f)}};
        public static TownPoint Place(string id)=>anchors.TryGetValue(id??"",out var p)?p:throw new ArgumentException("Unknown map place",nameof(id));
        public static TownPoint Growth(TownRegionId region,TownGrowthPlacement prop)
        {
            // Placement coordinates are authored on the same painting as the paving.
            // Props grow on verges/patios, never as navigation obstacles added later.
            return Point(prop.Position.X,prop.Position.Y);
        }
        public static bool TryPlace(string id,out TownPoint p)=>anchors.TryGetValue(id??"",out p);
    }
}
