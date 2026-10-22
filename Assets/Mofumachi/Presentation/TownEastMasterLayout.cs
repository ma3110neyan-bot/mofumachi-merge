using System;
using System.Collections.Generic;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    // The same ground painting joins the opposite court; the florist and its
    // flower displays remain independent original art, not a copied backdrop.
    public static class TownEastMasterLayout
    {
        public const string Resource=TownWestMasterLayout.Resource;
        public const float Left=.60f,Right=1.8f,Bottom=-1f,Top=1.23f;
        public static TownPoint TexturePoint(TownPoint p)=>new TownPoint(1-(p.X-.45f)/1.5f,(p.Y+1)/2.25f);
        public static IReadOnlyList<TownSceneryPlacement> Dressing {get;}=Build();
        public static bool VisibleAtLevel(TownSceneryPlacement p,int level)
        {
            if(p.Level>level)return false;
            if(p.District!=TownDistrictId.East)return true;
            if(p.Kind==TownSceneryKind.Flowerbed&&p.Foundation.Equals(new TownPoint(1.48f,-.08f)))return false;
            if(p.Kind==TownSceneryKind.FlowerHedge&&p.Feet.Equals(new TownPoint(1.52f,-.84f)))return false;
            if(p.Kind==TownSceneryKind.Tree&&p.Feet.Equals(new TownPoint(1.86f,-.72f))&&p.Width>.5f)return false;
            if(p.Kind==TownSceneryKind.FlowerBorder&&p.Feet.Equals(new TownPoint(1.49f,-.12f)))return false;
            // The existing Lv2 display occupies this initial, already finished site.
            if(p.Kind==TownSceneryKind.FloristCart&&p.Feet.Equals(new TownPoint(1.50f,-.14f)))return level<2;
            if((Math.Abs(p.Feet.X-1.70f)<.001f&&Math.Abs(p.Feet.Y+.67f)<.001f)||
               (Math.Abs(p.Feet.X-.70f)<.001f&&Math.Abs(p.Feet.Y+.58f)<.001f))
                return p.Level==(level>=6?6:level>=3?3:0);
            return true;
        }
        private static IReadOnlyList<TownSceneryPlacement> Build()
        {
            var result=new List<TownSceneryPlacement>();
            void Add(TownSceneryKind kind,float x,float y,float width,int level=0,float solidWidth=0,float depth=0)
            {
                var pixels=TownVillageDressing.SpriteBounds(kind);
                TownPoint[] footprint=solidWidth<=0?null:new[]{new TownPoint(x-solidWidth/2,y),new TownPoint(x+solidWidth/2,y),new TownPoint(x+solidWidth/2,y+depth),new TownPoint(x-solidWidth/2,y+depth)};
                result.Add(new TownSceneryPlacement(kind,TownDistrictId.East,new TownPoint(x,y),new TownPoint(x,y+depth/2),width,width*pixels.Height/pixels.Width,level,footprint));
            }
            Add(TownSceneryKind.FloristCart,1.50f,-.14f,.25f,0,.14f,.05f);
            Add(TownSceneryKind.ParkBench,1.67f,-.47f,.18f,0,.075f,.035f);
            Add(TownSceneryKind.StreetLamp,1.73f,-.32f,.08f,0,.02f,.025f);
            Add(TownSceneryKind.StreetLamp,.72f,-.36f,.08f,0,.02f,.025f);
            foreach(var site in new[]{new TownPoint(1.70f,-.67f),new TownPoint(.70f,-.58f)})
            {
                Add(TownSceneryKind.FlowerBorder,site.X,site.Y,.16f,0,.08f,.025f);
                Add(TownSceneryKind.FlowerBorder,site.X,site.Y,.20f,3);
                Add(TownSceneryKind.FlowerBorder,site.X,site.Y,.24f,6);
            }
            Add(TownSceneryKind.FlowerBorder,1.52f,-.84f,.30f);
            Add(TownSceneryKind.TreeGrove,1.94f,-.72f,.32f);
            Add(TownSceneryKind.NoticeBoard,1.12f,.68f,.12f,0,.06f,.035f);
            Add(TownSceneryKind.StreetLamp,1.40f,.64f,.055f,0,.02f,.02f);
            Add(TownSceneryKind.StreetLamp,1.00f,.64f,.055f,0,.02f,.02f);
            Add(TownSceneryKind.FlowerBorder,1.77f,.09f,.18f);
            Add(TownSceneryKind.FlowerBorder,.69f,.50f,.17f);
            Add(TownSceneryKind.TreeGrove,.63f,.86f,.31f);
            return result.AsReadOnly();
        }
    }
}
