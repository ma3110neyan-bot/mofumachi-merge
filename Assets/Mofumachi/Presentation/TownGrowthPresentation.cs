using System;
using System.Collections.Generic;
using System.Linq;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    public enum TownGrowthProp { Flowers,Lamp,CafeTable,Workshop,Arch,Garland,Bench,CafeSign,FlowerDisplay,StreetBunting }
    public sealed class TownGrowthPlacement
    {
        public TownGrowthProp Prop {get;} public TownPoint Position {get;} public float Width {get;} public int Level {get;}
        internal TownGrowthPlacement(TownGrowthProp prop,int level,float x,float y,float width)
        {Prop=prop;Level=level;Position=new TownPoint(x,y);Width=width;}
    }
    // Furniture grows on planted verges in world coordinates. The floor and
    // scenery share these reserved foundations, so a bench cannot block a route.
    public static class TownGrowthPresentation
    {
        private static TownGrowthPlacement P(TownGrowthProp prop,int level,float x,float y,float width)
        {
            // Furniture reads as a destination group, while lamps/flowers stay
            // subordinate and the clear courts continue to belong to residents.
            float size=prop==TownGrowthProp.CafeTable?1.47f:prop==TownGrowthProp.FlowerDisplay?1.55f:prop==TownGrowthProp.Workshop?1.20f:prop==TownGrowthProp.Bench?1.20f:prop==TownGrowthProp.CafeSign?1.25f:prop==TownGrowthProp.Flowers?1.12f:1;
            return new TownGrowthPlacement(prop,level,x,y,width*size);
        }
        private static readonly TownGrowthPlacement[][] Regions={
            new[]{P(TownGrowthProp.Flowers,1,-1.50f,-.08f,.14f),P(TownGrowthProp.CafeTable,2,-1.50f,-.12f,.19f),P(TownGrowthProp.CafeSign,2,-.95f,.76f,.07f),
                P(TownGrowthProp.Lamp,3,-1.46f,-.10f,.045f),P(TownGrowthProp.Bench,3,-.90f,-.66f,.17f),P(TownGrowthProp.Flowers,3,-1.57f,.75f,.13f),
                P(TownGrowthProp.CafeTable,4,-.95f,.78f,.16f),P(TownGrowthProp.Flowers,4,-.93f,-.69f,.12f),P(TownGrowthProp.Lamp,4,-1.59f,.72f,.045f),
                P(TownGrowthProp.Arch,5,-1.2f,-.79f,.34f),P(TownGrowthProp.CafeSign,5,-1.43f,.24f,.07f),
                P(TownGrowthProp.StreetBunting,6,-1.2f,.72f,.56f),P(TownGrowthProp.Flowers,6,-1.53f,-.66f,.10f),P(TownGrowthProp.Garland,6,-1.2f,.60f,.43f)},
            new[]{P(TownGrowthProp.Flowers,1,-.42f,.06f,.13f),P(TownGrowthProp.Flowers,2,.42f,-.43f,.13f),P(TownGrowthProp.Bench,2,-.44f,-.31f,.16f),
                P(TownGrowthProp.Lamp,3,-.44f,.07f,.045f),P(TownGrowthProp.Bench,3,-.49f,2.36f,.16f),P(TownGrowthProp.Lamp,3,.47f,-2.48f,.045f),
                P(TownGrowthProp.Flowers,4,.22f,1.31f,.13f),P(TownGrowthProp.FlowerDisplay,4,-.20f,-2.67f,.16f),P(TownGrowthProp.Lamp,4,-.40f,-2.13f,.045f),
                P(TownGrowthProp.Arch,5,0,1.04f,.48f),P(TownGrowthProp.Flowers,5,.45f,-2.53f,.13f),
                P(TownGrowthProp.Garland,6,0,2.55f,.39f),P(TownGrowthProp.StreetBunting,6,0,TownCentralMasterLayout.FestivalFeetY,.48f),P(TownGrowthProp.Lamp,6,.44f,-.45f,.045f)},
            new[]{P(TownGrowthProp.Flowers,1,1.48f,-.08f,.14f),P(TownGrowthProp.FlowerDisplay,2,1.49f,-.14f,.16f),P(TownGrowthProp.Flowers,2,1.52f,.75f,.13f),
                P(TownGrowthProp.Lamp,3,1.45f,-.10f,.045f),P(TownGrowthProp.Bench,3,TownEastMasterLayout.BenchX,TownEastMasterLayout.BenchY,.17f),P(TownGrowthProp.CafeSign,3,.87f,.72f,.07f),
                P(TownGrowthProp.Workshop,4,.87f,.77f,.18f),P(TownGrowthProp.Flowers,4,1.51f,-.80f,.12f),P(TownGrowthProp.Lamp,4,1.54f,.73f,.045f),
                P(TownGrowthProp.Arch,5,1.2f,-.83f,.34f),P(TownGrowthProp.FlowerDisplay,5,1.01f,-.74f,.12f),
                P(TownGrowthProp.StreetBunting,6,1.2f,.74f,.53f),P(TownGrowthProp.Garland,6,1.2f,.59f,.41f),P(TownGrowthProp.Flowers,6,1.50f,-.12f,.10f)}};
        // The delivery comparison should frame the landmark just earned, not a
        // small last-listed flower that happens to share its growth level.
        public static TownGrowthPlacement FocusFor(TownRegionId region,int level)
        {
            int stage=Math.Max(0,Math.Min(6,level));
            int Priority(TownGrowthProp p)=>p==TownGrowthProp.Arch?6:p==TownGrowthProp.StreetBunting?5:p==TownGrowthProp.Workshop?4:p==TownGrowthProp.CafeTable?3:p==TownGrowthProp.FlowerDisplay?2:p==TownGrowthProp.Bench?1:0;
            return For(region,stage).Where(p=>p.Level==stage).OrderByDescending(p=>Priority(p.Prop)).FirstOrDefault();
        }
        public static IReadOnlyList<TownGrowthPlacement> ForDistrict(TownDistrictId district,int level)
        {
            if(!Enum.IsDefined(typeof(TownDistrictId),district))return Array.Empty<TownGrowthPlacement>();
            return Array.AsReadOnly(For(TownDistrictLayout.LegacyRegion(district),level).Where(p=>TownDistrictLayout.At(p.Position)==district).ToArray());
        }
        private static IEnumerable<TownSceneryPlacement> DistrictDressing(TownDistrictId district)
            =>district==TownDistrictId.North?TownNorthMasterLayout.Dressing:district==TownDistrictId.South?TownSouthMasterLayout.Dressing:Array.Empty<TownSceneryPlacement>();
        public static TownPoint FocusForDistrict(TownDistrictId district,int level)
        {
            int stage=Math.Max(0,Math.Min(6,level));
            var dressed=DistrictDressing(district).Where(p=>p.Level==stage&&p.Level>0)
                .OrderByDescending(p=>p.Kind==TownSceneryKind.FlowerArch?3:p.Kind==TownSceneryKind.DeliveryBunting?2:p.Kind==TownSceneryKind.FlowerBasket?1:0).FirstOrDefault();
            if(dressed!=null)return new TownPoint(dressed.Feet.X,dressed.Feet.Y+dressed.Height*.4f);
            var addition=ForDistrict(district,stage).Where(p=>p.Level==stage)
                .OrderByDescending(p=>p.Prop==TownGrowthProp.StreetBunting?5:p.Prop==TownGrowthProp.Arch?4:p.Prop==TownGrowthProp.CafeTable?3:p.Prop==TownGrowthProp.FlowerDisplay?2:p.Prop==TownGrowthProp.Bench?1:0).FirstOrDefault();
            return addition?.Position??TownDistrictLayout.Center(district);
        }
        public static string ChangeSummary(TownDistrictId district,int previous,int current)
        {
            string PropName(TownGrowthProp prop)=>prop==TownGrowthProp.Flowers?"花":prop==TownGrowthProp.Lamp?"街灯":prop==TownGrowthProp.CafeTable?"カフェ席":prop==TownGrowthProp.Workshop?"お店飾り":prop==TownGrowthProp.Arch?"アーチ":prop==TownGrowthProp.Garland?"花飾り":prop==TownGrowthProp.Bench?"ベンチ":prop==TownGrowthProp.CafeSign?"看板":prop==TownGrowthProp.FlowerDisplay?"花台":"ガーランド";
            string[] Changes(int from,int to)
                =>DistrictDressing(district).Where(p=>p.Level>from&&p.Level<=to)
                    .OrderByDescending(p=>p.Kind==TownSceneryKind.FlowerArch?1:0)
                    .Select(p=>p.Kind==TownSceneryKind.FlowerArch?"花アーチ":p.Kind==TownSceneryKind.StreetLamp?"街灯":p.Kind==TownSceneryKind.DeliveryParcels?"届け物":p.Kind==TownSceneryKind.DeliveryBunting?"ガーランド":"花飾り")
                    .Concat(ForDistrict(district,to).Where(p=>p.Level>from).Select(p=>PropName(p.Prop))).Distinct().ToArray();
            var changes=Changes(previous,current);if(changes.Length>0)return string.Join("・",changes.Take(2));
            for(int next=current+1;next<=6;next++)if(Changes(current,next).Length>0)return "次はLv."+next;
            return "今の景色を維持";
        }
        public static IReadOnlyList<TownGrowthPlacement> For(TownRegionId region,int level)
        {
            if(!Enum.IsDefined(typeof(TownRegionId),region))return Array.Empty<TownGrowthPlacement>();
            int stage=Math.Max(0,Math.Min(6,level));return Array.AsReadOnly(Regions[(int)region].Where(p=>p.Level<=stage).ToArray());
        }
    }
}
