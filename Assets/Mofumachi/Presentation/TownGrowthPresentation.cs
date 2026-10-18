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
    // Authored map UV coordinates; widths are world units, independent of the camera.
    // All additions sit beside the existing street graph. Painted fixtures remain.
    public static class TownGrowthPresentation
    {
        private static TownGrowthPlacement P(TownGrowthProp prop,int level,float x,float y,float width)=>new TownGrowthPlacement(prop,level,x,y,width);
        private static readonly TownGrowthPlacement[][] Regions={
            new[]{P(TownGrowthProp.Flowers,1,.24f,.61f,.16f),P(TownGrowthProp.CafeTable,2,.14f,.53f,.23f),P(TownGrowthProp.CafeSign,2,.295f,.445f,.09f),
                P(TownGrowthProp.Lamp,3,.24f,.60f,.07f),P(TownGrowthProp.Bench,3,.345f,.66f,.22f),P(TownGrowthProp.Flowers,3,.20f,.603f,.15f),
                P(TownGrowthProp.CafeTable,4,.215f,.55f,.22f),P(TownGrowthProp.Flowers,4,.15f,.60f,.16f),P(TownGrowthProp.Lamp,4,.17f,.73f,.07f),
                P(TownGrowthProp.Arch,5,.19f,.32f,.28f),P(TownGrowthProp.CafeSign,5,.37f,.29f,.08f),
                P(TownGrowthProp.StreetBunting,6,.255f,.42f,.28f),P(TownGrowthProp.Flowers,6,.20f,.30f,.17f),P(TownGrowthProp.Garland,6,.17f,.21f,.26f)},
            new[]{P(TownGrowthProp.Flowers,1,.41f,.65f,.16f),P(TownGrowthProp.Flowers,2,.635f,.655f,.16f),P(TownGrowthProp.Bench,2,.36f,.67f,.22f),
                P(TownGrowthProp.Lamp,3,.40f,.66f,.07f),P(TownGrowthProp.Bench,3,.685f,.67f,.22f),P(TownGrowthProp.Lamp,3,.65f,.65f,.07f),
                P(TownGrowthProp.Flowers,4,.45f,.49f,.16f),P(TownGrowthProp.FlowerDisplay,4,.59f,.49f,.16f),P(TownGrowthProp.Lamp,4,.33f,.81f,.07f),
                P(TownGrowthProp.Arch,5,.51f,.33f,.75f),P(TownGrowthProp.Flowers,5,.57f,.83f,.17f),
                P(TownGrowthProp.Garland,6,.735f,.827f,.38f),P(TownGrowthProp.StreetBunting,6,.51f,.87f,.50f),P(TownGrowthProp.Lamp,6,.64f,.82f,.07f)},
            new[]{P(TownGrowthProp.Flowers,1,.73f,.665f,.16f),P(TownGrowthProp.FlowerDisplay,2,.89f,.47f,.19f),P(TownGrowthProp.Flowers,2,.69f,.67f,.16f),
                P(TownGrowthProp.Lamp,3,.74f,.67f,.07f),P(TownGrowthProp.Bench,3,.80f,.78f,.22f),P(TownGrowthProp.CafeSign,3,.72f,.39f,.08f),
                P(TownGrowthProp.Workshop,4,.90f,.47f,.24f),P(TownGrowthProp.Flowers,4,.78f,.81f,.15f),P(TownGrowthProp.Lamp,4,.90f,.73f,.07f),
                P(TownGrowthProp.Arch,5,.815f,.43f,.34f),P(TownGrowthProp.FlowerDisplay,5,.76f,.43f,.16f),
                P(TownGrowthProp.StreetBunting,6,.81f,.445f,.34f),P(TownGrowthProp.Garland,6,.735f,.827f,.30f),P(TownGrowthProp.Flowers,6,.68f,.75f,.16f)}};
        public static IReadOnlyList<TownGrowthPlacement> For(TownRegionId region,int level)
        {
            if(!Enum.IsDefined(typeof(TownRegionId),region))return Array.Empty<TownGrowthPlacement>();
            int stage=Math.Max(0,Math.Min(6,level));return Array.AsReadOnly(Regions[(int)region].Where(p=>p.Level<=stage).ToArray());
        }
    }
}
