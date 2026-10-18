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
        public const float Left=-.3f,Bottom=-.75f,Width=1.6f,Height=2.4f;
        public const float Right=Left+Width,Top=Bottom+Height;
        public static TownPoint Point(float u,float v)=>new TownPoint(Left+Width*u,Top-Height*v);
        private static readonly Dictionary<string,TownPoint> anchors=new Dictionary<string,TownPoint>{
            {"square",Point(.50f,.57f)},{"fountain",Point(.50f,.365f)},{"flowers",Point(.19f,.45f)},
            {"quest",Point(.795f,.253f)},{"shop",Point(.805f,.824f)},
            {"bench-left",Point(.32f,.59f)},{"bench-right",Point(.68f,.59f)},
            {"home-front",Point(.29f,.36f)},{"flower-walk",Point(.80f,.48f)},
            {"front-promenade",Point(.50f,.66f)},{"celebration",Point(.50f,.91f)},
            {"west.entry",Point(.52f,.30f)},{"west.cafe-front",Point(.34f,.20f)},
            {"west.flowers",Point(.435f,.285f)},{"west.bench-front",Point(.33f,.32f)},{"west.meeting",Point(.55f,.235f)},
            {"east.entry",Point(.50f,.66f)},{"east.meeting",Point(.48f,.86f)},
            {"east.flowers",Point(.435f,.805f)},{"east.workshop-front",Point(.30f,.74f)},{"east.bench-front",Point(.57f,.88f)}};
        public static TownPoint Place(string id)=>anchors.TryGetValue(id??"",out var p)?p:throw new ArgumentException("Unknown map place",nameof(id));
        public static TownPoint Growth(TownRegionId region,TownGrowthPlacement prop)
        {
            // Early growth must appear beside the regional arrival path, inside
            // a short phone's masked field, rather than only at the map edges.
            if(prop.Prop==TownGrowthProp.Flowers&&prop.Level==1)
                return region==TownRegionId.West?Point(.48f,.24f):region==TownRegionId.East?Point(.42f,.68f):Point(.68f,.58f);
            if(prop.Prop==TownGrowthProp.Lamp&&prop.Level==3&&prop.Width>=.10f)
                return region==TownRegionId.West?Point(.62f,.29f):region==TownRegionId.East?Point(.62f,.72f):Point(.72f,.575f);
            float v=region==TownRegionId.West?.27f:region==TownRegionId.East?.83f:.50f;
            float side=prop.Position.X<.5f?.10f:.90f;
            if(prop.Prop==TownGrowthProp.Arch||prop.Prop==TownGrowthProp.Garland)return Point(.50f,v-.11f-(prop.Level%2)*.08f);
            if(prop.Prop==TownGrowthProp.CafeTable)return prop.Level<4?Point(.165f,.219f):Point(.380f,.192f);
            if(prop.Prop==TownGrowthProp.Workshop)return Point(.22f,.70f);
            return Point(side+(prop.Level%2==0?.015f:-.015f),v+(prop.Position.Y-.5f)*.42f);
        }
        public static bool TryPlace(string id,out TownPoint p)=>anchors.TryGetValue(id??"",out p);
    }
}
