using System;
using System.Collections.Generic;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    // Existing request cottage/board and quest anchors stay independent of this
    // north court dressing. Share the accepted ground, not another big texture.
    public static class TownNorthMasterLayout
    {
        public const string Resource=TownWestMasterLayout.Resource;
        public const float Left=-.60f,Right=.60f,Bottom=1f,Top=3.23f;
        public static TownPoint TexturePoint(TownPoint p)=>new TownPoint((p.X+.75f)/1.5f,(p.Y-1f)/2.25f);
        public static IReadOnlyList<TownSceneryPlacement> Dressing {get;}=Build();
        // Actual area-navigation requests between destinations, not a graph
        // restricting movement. None of these visits returns to the entry.
        public static IReadOnlyList<TownPoint[]> Circulation {get;}=Array.AsReadOnly(new[]{
            new[]{new TownPoint(0,2.08f),new TownPoint(-.42f,2.60f)},
            new[]{new TownPoint(0,2.08f),new TownPoint(.42f,2.60f)},
            new[]{new TownPoint(-.42f,2.60f),new TownPoint(.32f,2.05f)},
            new[]{new TownPoint(.42f,2.60f),new TownPoint(.32f,2.05f)},
            new[]{new TownPoint(-.24f,1.90f),new TownPoint(.24f,1.90f)},
            new[]{new TownPoint(-.24f,1.80f),new TownPoint(.24f,1.80f),new TownPoint(.24f,2.04f),new TownPoint(-.24f,2.04f),new TownPoint(-.24f,1.80f)}});
        // Attached flowers use the cottage's existing solid base, but must be
        // drawn over its facade and behind the visitor standing at the door.
        public static float DepthY(TownSceneryPlacement p)
        {
            if(p.District!=TownDistrictId.North)return p.Feet.Y;
            if(p.Level==3&&(p.Kind==TownSceneryKind.FlowerBasket&&p.Feet.X<-.3f||p.Kind==TownSceneryKind.StreetLamp&&p.Feet.Y>2.3f&&p.Feet.X<-.3f))return 2.275f;
            if(p.Kind==TownSceneryKind.FlowerArch)return 2.135f;
            if(p.Kind==TownSceneryKind.DeliveryParcels||p.Kind==TownSceneryKind.FlowerBasket||p.Kind==TownSceneryKind.StreetLamp&&Math.Abs(p.Feet.Y-2.22f)<.001f)return 2.13f;
            return p.Feet.Y;
        }
        public static bool VisibleAtLevel(TownSceneryPlacement p,int level)
        {
            if(p.Level>level)return false;
            if(p.District!=TownDistrictId.North)return true;
            if(p.Kind==TownSceneryKind.FlowerHedge&&p.Feet.Equals(new TownPoint(.48f,1.24f)))return false;
            if(p.Kind==TownSceneryKind.FlowerBorder&&p.Feet.Equals(new TownPoint(-.45f,2.28f)))return false;
            if(p.Kind==TownSceneryKind.Blossom&&p.Feet.Equals(new TownPoint(-.70f,1.72f))&&p.Width>.5f)return false;
            if(p.Kind==TownSceneryKind.DeliveryParcels||p.Kind==TownSceneryKind.FlowerBasket)
                return p.Level==(level>=3?3:0);
            if(Math.Abs(Math.Abs(p.Feet.X)-.43f)<.001f&&Math.Abs(p.Feet.Y-1.72f)<.001f)
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
                result.Add(new TownSceneryPlacement(kind,TownDistrictId.North,new TownPoint(x,y),new TownPoint(x,y+depth/2),width,width*pixels.Height/pixels.Width,level,footprint));
            }
            Add(TownSceneryKind.ParkBench,.40f,1.55f,.18f,0,.09f,.035f);
            foreach(float side in new[]{-1f,1f})
            {
                Add(TownSceneryKind.StreetLamp,side*.43f,1.78f,.08f,0,.025f,.025f);
                Add(TownSceneryKind.FlowerBorder,side*.43f,1.72f,.16f,0,.08f,.025f);
                Add(TownSceneryKind.FlowerBorder,side*.43f,1.72f,.20f,3);
                Add(TownSceneryKind.FlowerBorder,side*.43f,1.72f,.24f,6);
            }
            // Keep planted boundaries, but lower the bank and original blossom
            // so the seat and the waiting resident's name have breathing room.
            Add(TownSceneryKind.FlowerBorder,.48f,1.24f,.31f);
            result.Add(new TownSceneryPlacement(TownSceneryKind.Blossom,TownDistrictId.North,new TownPoint(-.70f,1.72f),new TownPoint(-.70f,1.75f),.40f,.40f*622/682));
            Add(TownSceneryKind.TreeGrove,.64f,1.44f,.28f);
            Add(TownSceneryKind.FlowerBorder,.54f,2.03f,.16f);
            Add(TownSceneryKind.StreetLamp,.35f,2.63f,.05f,0,.02f,.02f);
            Add(TownSceneryKind.FlowerBorder,.32f,2.61f,.15f);
            // A delivery doorstep: parcels and planted lanterns sit against
            // the existing cottage foundation, above the open waiting apron.
            // Stage upgrades replace the same display, never consume more floor.
            Add(TownSceneryKind.DeliveryParcels,.165f,2.16f,.10f);
            Add(TownSceneryKind.DeliveryParcels,.165f,2.16f,.15f,3);
            Add(TownSceneryKind.FlowerBasket,-.14f,2.16f,.12f);
            Add(TownSceneryKind.FlowerBasket,-.14f,2.16f,.18f,3);
            foreach(float side in new[]{-1f,1f})Add(TownSceneryKind.StreetLamp,side*.20f,2.22f,.045f);
            // The side pennants announce a lively request/delivery hub at Lv3;
            // the broad floral doorway becomes its distinct Lv6 silhouette.
            Add(TownSceneryKind.DeliveryBunting,.40f,2.44f,.40f,3);
            // A restrained intermediate upgrade on the existing board frame,
            // not new freestanding objects or new invisible floor blockers.
            Add(TownSceneryKind.FlowerBasket,-.47f,2.29f,.12f,3);
            Add(TownSceneryKind.StreetLamp,-.35f,2.35f,.045f,3);
            Add(TownSceneryKind.FlowerArch,.01f,2.15f,.46f,6);
            return result.AsReadOnly();
        }
    }
}
