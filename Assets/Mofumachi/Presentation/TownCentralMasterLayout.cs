using System;
using System.Collections.Generic;
using System.Linq;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    // Central production only. No save, district topology or other-region data.
    // The painted socket and cross street are calibrated to the existing world
    // fountain and -0.24 east/west approach, independent of the camera.
    public static class TownCentralMasterLayout
    {
        public const string Resource="Mofumachi/TownCentralGround";
        public const float SourceLeft=-.75f,SourceBottom=-1f,SourceWidth=1.5f,SourceHeight=2.25f;
        public const float Left=-.60f,Right=.60f,Bottom=-1f,Top=1.23f;
        public static TownPoint TexturePoint(TownPoint world)=>new TownPoint((world.X-SourceLeft)/SourceWidth,(world.Y-SourceBottom)/SourceHeight);
        public static IReadOnlyList<TownSceneryPlacement> Dressing {get;}=Build();
        public static bool VisibleAtLevel(TownSceneryPlacement piece,int level)
        {
            if(piece.Level>level)return false;
            // These two gardens are replacements, not three overlapping banks.
            if(Math.Abs(Math.Abs(piece.Feet.X)-.50f)<.001f&&Math.Abs(piece.Feet.Y+.89f)<.001f)
                return piece.Level==(level>=6?6:level>=3?3:0);
            return true;
        }
        private static IReadOnlyList<TownSceneryPlacement> Build()
        {
            var result=new List<TownSceneryPlacement>();
            void Add(TownSceneryKind kind,float x,float y,float width,int level=0,float solidWidth=0,float depth=0)
            {
                var pixels=TownVillageDressing.SpriteBounds(kind);
                TownPoint[] footprint=solidWidth<=0?null:new[]{new TownPoint(x-solidWidth/2,y),new TownPoint(x+solidWidth/2,y),new TownPoint(x+solidWidth/2,y+depth),new TownPoint(x-solidWidth/2,y+depth)};
                result.Add(new TownSceneryPlacement(kind,TownDistrictId.Central,new TownPoint(x,y),new TownPoint(x,y+depth/2),width,width*pixels.Height/pixels.Width,level,footprint));
            }
            foreach(int side in new[]{-1,1})
            {
                // Corner facades/tree feet stay in the landscaped space outside
                // all five walking rooms; their crowns frame the central garden.
                Add(TownSceneryKind.CottagePair,side*.74f,-1.08f,.54f);
                Add(TownSceneryKind.TreeGrove,side*.68f,-.98f,.40f);
                // The base is already a little finished flower bank at Lv1.
                Add(TownSceneryKind.FlowerBorder,side*.50f,-.89f,.20f,0,.12f,.04f);
                Add(TownSceneryKind.FlowerHedge,side*.50f,-.89f,.25f,3);
                Add(TownSceneryKind.FlowerBorder,side*.50f,-.89f,.31f,6);
                Add(TownSceneryKind.StreetLamp,side*.55f,-.60f,.062f,0,.025f,.025f);
                Add(TownSceneryKind.FlowerBorder,side*.56f,.12f,.18f,0,.07f,.035f);
            }
            return result.AsReadOnly();
        }
    }
}
