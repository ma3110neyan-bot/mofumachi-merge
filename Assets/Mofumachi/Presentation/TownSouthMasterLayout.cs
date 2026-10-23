using System;
using System.Collections.Generic;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    // Garden/rest role, original gazebo and reserved furniture islands retained.
    public static class TownSouthMasterLayout
    {
        public const string Resource=TownWestMasterLayout.Resource;
        public const float Left=-.60f,Right=.60f,Bottom=-3f,Top=-1f;
        public static TownPoint TexturePoint(TownPoint p)=>new TownPoint(1-(p.X+.75f)/1.5f,(p.Y+3.23f)/2.25f);
        public static IReadOnlyList<TownSceneryPlacement> Dressing {get;}=Build();
        public static IReadOnlyList<TownPoint[]> Circulation {get;}=Array.AsReadOnly(new[]{
            new[]{new TownPoint(0,-1.12f),new TownPoint(-.38f,-1.50f),new TownPoint(-.38f,-1.96f),new TownPoint(0,-2.04f)},
            new[]{new TownPoint(0,-1.12f),new TownPoint(.38f,-1.50f),new TownPoint(.38f,-1.96f),new TownPoint(0,-2.04f)},
            new[]{new TownPoint(0,-2.04f),new TownPoint(-.30f,-2.35f)},
            new[]{new TownPoint(0,-2.04f),new TownPoint(.20f,-2.20f)},
            new[]{new TownPoint(-.30f,-2.35f),new TownPoint(.20f,-2.20f)},
            new[]{new TownPoint(0,-2.04f),new TownPoint(-.38f,-1.96f),new TownPoint(-.38f,-1.50f),new TownPoint(0,-1.34f),new TownPoint(.38f,-1.50f),new TownPoint(.38f,-1.96f),new TownPoint(0,-2.04f)}});
        public static float DepthY(TownSceneryPlacement p)
        {
            if(p.District!=TownDistrictId.South)return TownNorthMasterLayout.DepthY(p);
            if(p.Kind==TownSceneryKind.FlowerArch||p.Kind==TownSceneryKind.FlowerBasket&&p.Feet.Y> -2||p.Kind==TownSceneryKind.StreetLamp&&p.Feet.Y> -2)return -1.905f;
            return p.Feet.Y;
        }
        public static bool VisibleAtLevel(TownSceneryPlacement p,int level)
        {
            if(p.Level>level)return false;if(p.District!=TownDistrictId.South)return true;
            // Low dense flower banks replace the tall foreground masses that
            // would otherwise cover the resident at the established rest stop.
            if(p.Kind==TownSceneryKind.FlowerHedge&&(p.Feet.Equals(new TownPoint(-.24f,-2.79f))||p.Feet.Equals(new TownPoint(-.21f,-2.70f))))return false;
            if(p.Kind==TownSceneryKind.GardenPergola&&p.Feet.Equals(new TownPoint(.46f,-2.54f)))return p.Level==(level>=3?3:0);
            if(p.Kind==TownSceneryKind.FlowerBasket&&p.Feet.Y> -2)return p.Level==(level>=3?3:0);
            if(p.Kind==TownSceneryKind.FlowerBorder&&(p.Feet.Equals(new TownPoint(-.21f,-2.98f))||p.Feet.Equals(new TownPoint(-.43f,-2.66f))||p.Feet.Equals(new TownPoint(.43f,-2.74f))))return p.Level==(level>=6?6:level>=3?3:0);
            return true;
        }
        private static IReadOnlyList<TownSceneryPlacement> Build()
        {
            var result=new List<TownSceneryPlacement>();
            void Add(TownSceneryKind kind,float x,float y,float width,int level=0)
            {
                var b=TownVillageDressing.SpriteBounds(kind);
                result.Add(new TownSceneryPlacement(kind,TownDistrictId.South,new TownPoint(x,y),new TownPoint(x,y),width,width*b.Height/b.Width,level));
            }
            // The original Lv3 pergola replaces this initial finished seat on
            // its already solid garden island; no new invisible foundation.
            Add(TownSceneryKind.GardenPergola,.46f,-2.54f,.23f);
            foreach(float side in new[]{-1f,1f})
            {
                Add(TownSceneryKind.StreetLamp,side*.20f,-1.87f,.045f);
                Add(TownSceneryKind.FlowerBasket,side*.18f,-1.88f,.10f);
                Add(TownSceneryKind.FlowerBasket,side*.18f,-1.88f,.14f,3);
            }
            foreach(var site in new[]{new TownPoint(-.21f,-2.98f),new TownPoint(-.43f,-2.66f),new TownPoint(.43f,-2.74f)})
            {
                bool bank=site.X>-.22f&&site.X<-.20f;
                Add(TownSceneryKind.FlowerBorder,site.X,site.Y,bank?.38f:.16f);
                Add(TownSceneryKind.FlowerBorder,site.X,site.Y,bank?.46f:.20f,3);
                Add(TownSceneryKind.FlowerBorder,site.X,site.Y,bank?.54f:.24f,6);
            }
            Add(TownSceneryKind.FlowerBorder,-.55f,-1.60f,.16f);
            Add(TownSceneryKind.StreetLamp,.52f,-2.48f,.045f,3);
            Add(TownSceneryKind.FlowerBasket,.55f,-2.46f,.08f,3);
            Add(TownSceneryKind.FlowerArch,0,-1.895f,.48f,6);
            return result.AsReadOnly();
        }
    }
}
