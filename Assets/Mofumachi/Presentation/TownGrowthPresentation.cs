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
            new[]{P(TownGrowthProp.Flowers,1,.72f,.355f,.20f),P(TownGrowthProp.CafeTable,2,.30f,.465f,.28f),P(TownGrowthProp.Lamp,3,.78f,.425f,.10f),P(TownGrowthProp.Flowers,4,.15f,.57f,.17f),P(TownGrowthProp.Arch,5,.77f,.27f,.35f),P(TownGrowthProp.Garland,6,.47f,.66f,.46f)},
            new[]{P(TownGrowthProp.Flowers,1,.67f,.38f,.18f),P(TownGrowthProp.Flowers,2,.16f,.67f,.17f),P(TownGrowthProp.Lamp,3,.80f,.45f,.10f),P(TownGrowthProp.Flowers,4,.21f,.43f,.17f),P(TownGrowthProp.Arch,5,.54f,.31f,.35f),P(TownGrowthProp.Garland,6,.49f,.685f,.52f)},
            new[]{P(TownGrowthProp.Flowers,1,.43f,.335f,.20f),P(TownGrowthProp.Flowers,2,.82f,.54f,.17f),P(TownGrowthProp.Lamp,3,.76f,.36f,.10f),P(TownGrowthProp.Workshop,4,.79f,.49f,.27f),P(TownGrowthProp.Arch,5,.62f,.29f,.35f),P(TownGrowthProp.Garland,6,.57f,.63f,.46f)}};
        public static IReadOnlyList<TownGrowthPlacement> For(TownRegionId region,int level)
        {
            if(!Enum.IsDefined(typeof(TownRegionId),region))return Array.Empty<TownGrowthPlacement>();
            int stage=Math.Max(0,Math.Min(6,level));return Array.AsReadOnly(Regions[(int)region].Where(p=>p.Level<=stage).ToArray());
        }
    }
}
