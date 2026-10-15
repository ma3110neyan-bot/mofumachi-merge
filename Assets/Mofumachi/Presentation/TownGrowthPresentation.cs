using System;
using System.Collections.Generic;
using System.Linq;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    public enum TownGrowthProp { Flowers,Lamp,CafeTable,Workshop,Arch,Garland }
    public sealed class TownGrowthPlacement
    {
        public TownGrowthProp Prop {get;} public TownPoint Position {get;} public float Width {get;} public int Level {get;}
        internal TownGrowthPlacement(TownGrowthProp prop,int level,float x,float y,float width)
        {Prop=prop;Level=level;Position=new TownPoint(x,y);Width=width;}
    }
    // Source-image coordinates, independent of camera or future continuous maps.
    // All additions sit beside the existing street graph. Painted fixtures remain.
    public static class TownGrowthPresentation
    {
        private static TownGrowthPlacement P(TownGrowthProp prop,int level,float x,float y,float width)=>new TownGrowthPlacement(prop,level,x,y,width);
        private static readonly TownGrowthPlacement[][] Regions={
            new[]{P(TownGrowthProp.Flowers,1,.72f,.355f,.20f),P(TownGrowthProp.CafeTable,2,.30f,.465f,.28f),P(TownGrowthProp.Lamp,3,.78f,.425f,.10f),P(TownGrowthProp.Flowers,4,.15f,.57f,.17f),P(TownGrowthProp.Arch,5,.77f,.27f,.35f),P(TownGrowthProp.Garland,6,.47f,.66f,.46f),
                P(TownGrowthProp.Flowers,2,.22f,.39f,.16f),P(TownGrowthProp.Flowers,2,.44f,.39f,.14f),
                P(TownGrowthProp.Lamp,3,.20f,.58f,.08f),P(TownGrowthProp.CafeTable,4,.49f,.44f,.22f),P(TownGrowthProp.Flowers,4,.79f,.54f,.14f),
                P(TownGrowthProp.Flowers,5,.67f,.27f,.15f),P(TownGrowthProp.Lamp,5,.85f,.27f,.08f),
                P(TownGrowthProp.Flowers,6,.29f,.63f,.17f),P(TownGrowthProp.Garland,6,.35f,.79f,.38f),P(TownGrowthProp.Lamp,6,.60f,.60f,.08f)},
            new[]{P(TownGrowthProp.Flowers,1,.67f,.38f,.18f),P(TownGrowthProp.Flowers,2,.16f,.67f,.17f),P(TownGrowthProp.Lamp,3,.80f,.45f,.10f),P(TownGrowthProp.Flowers,4,.21f,.43f,.17f),P(TownGrowthProp.Arch,5,.54f,.31f,.35f),P(TownGrowthProp.Garland,6,.49f,.685f,.52f),
                P(TownGrowthProp.Flowers,2,.67f,.60f,.14f),P(TownGrowthProp.Flowers,2,.30f,.66f,.13f),
                P(TownGrowthProp.Lamp,3,.20f,.56f,.08f),P(TownGrowthProp.Flowers,4,.58f,.39f,.16f),P(TownGrowthProp.Lamp,4,.72f,.34f,.08f),
                P(TownGrowthProp.Flowers,5,.37f,.31f,.15f),P(TownGrowthProp.Flowers,5,.69f,.31f,.15f),
                P(TownGrowthProp.Garland,6,.57f,.82f,.39f),P(TownGrowthProp.Flowers,6,.76f,.58f,.14f),P(TownGrowthProp.Lamp,6,.19f,.70f,.09f)},
            new[]{P(TownGrowthProp.Flowers,1,.43f,.335f,.20f),P(TownGrowthProp.Flowers,2,.82f,.54f,.17f),P(TownGrowthProp.Lamp,3,.76f,.36f,.10f),P(TownGrowthProp.Workshop,4,.79f,.49f,.27f),P(TownGrowthProp.Arch,5,.62f,.29f,.35f),P(TownGrowthProp.Garland,6,.57f,.63f,.46f),
                P(TownGrowthProp.Flowers,2,.34f,.59f,.15f),P(TownGrowthProp.Flowers,2,.68f,.32f,.14f),
                P(TownGrowthProp.Lamp,3,.31f,.58f,.08f),P(TownGrowthProp.Flowers,4,.72f,.49f,.14f),P(TownGrowthProp.Flowers,4,.90f,.49f,.12f),
                P(TownGrowthProp.Lamp,5,.83f,.29f,.08f),P(TownGrowthProp.Flowers,5,.47f,.29f,.16f),
                P(TownGrowthProp.Garland,6,.65f,.79f,.39f),P(TownGrowthProp.Lamp,6,.35f,.69f,.08f),P(TownGrowthProp.Flowers,6,.72f,.65f,.15f)}};
        public static IReadOnlyList<TownGrowthPlacement> For(TownRegionId region,int level)
        {
            if(!Enum.IsDefined(typeof(TownRegionId),region))return Array.Empty<TownGrowthPlacement>();
            int stage=Math.Max(0,Math.Min(6,level));return Array.AsReadOnly(Regions[(int)region].Where(p=>p.Level<=stage).ToArray());
        }
    }
}
