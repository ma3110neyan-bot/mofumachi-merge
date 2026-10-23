using System;
using System.Collections.Generic;
using Mofumachi.Core;
using Mofumachi.Presentation;
namespace Mofumachi.Tests
{
    // Review annotations only. The walk model still routes freely on its plane;
    // these meaningful pairs never replace it with a scripted track network.
    public static class TownReviewRoutes
    {
        public static IReadOnlyList<TownPoint[]> For(TownDistrictId district)
        {
            if(district==TownDistrictId.North)return TownNorthMasterLayout.Circulation;
            if(district==TownDistrictId.South)return TownSouthMasterLayout.Circulation;
            if(district==TownDistrictId.Central)
                return Array.AsReadOnly(new[]{
                    new[]{TownWorldMap.Place("square"),TownWorldMap.Place("flowers")},
                    new[]{TownWorldMap.Place("square"),TownWorldMap.Place("bench-right")},
                    new[]{TownWorldMap.Place("flowers"),new TownPoint(-.34f,.36f)},
                    new[]{TownWorldMap.Place("bench-right"),new TownPoint(.34f,.36f)},
                    new[]{new TownPoint(-.34f,.36f),new TownPoint(.34f,.36f)},
                    new[]{TownWorldMap.Place("flowers"),new TownPoint(-.34f,.36f),new TownPoint(.34f,.36f),TownWorldMap.Place("bench-right"),TownWorldMap.Place("square")}});
            if(district!=TownDistrictId.West&&district!=TownDistrictId.East)throw new ArgumentOutOfRangeException(nameof(district));
            float side=district==TownDistrictId.West?-1:1;
            var front=TownWorldMap.Place(side<0?"west.cafe-front":"shop");
            var rest=TownWorldMap.Place(side<0?"west.bench-front":"east.meeting");
            var outer=new TownPoint(side*1.70f,-.22f);var topOuter=new TownPoint(side*1.70f,.50f);
            var inner=new TownPoint(side*.80f,.03f);var topInner=new TownPoint(side*.84f,.50f);
            return Array.AsReadOnly(new[]{new[]{front,outer},new[]{front,inner},new[]{outer,rest},new[]{inner,rest},new[]{outer,front,inner},new[]{outer,topOuter,topInner,inner,rest,outer}});
        }
    }
}
