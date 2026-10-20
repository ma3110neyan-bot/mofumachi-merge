using System;
using System.Collections.Generic;
using System.Linq;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    // One declaration supplies artwork, depth, growth stage and solid foundations.
    // Staged furniture grows on already visible planted islands; new building
    // foundations belong to stage zero so no invisible blocker appears early.
    public static class TownVillageDressing
    {
        public const string AtlasResource="Mofumachi/TownVillageDetailsAtlas";
        public static IReadOnlyList<TownSceneryPlacement> All {get;}=Build();
        public static IReadOnlyList<TownPoint[]> SolidFootprints {get;}=Array.AsReadOnly(All.Where(p=>p.Footprint!=null).Select(p=>p.Footprint).ToArray());
        // Source pixels, top-left origin. Rendering and world proportions share
        // these measurements; Android downscaling preserves their normalized UVs.
        public static (int X,int Y,int Width,int Height) SpriteBounds(TownSceneryKind kind)
        {
            switch(kind)
            {
                case TownSceneryKind.CottagePair:return (7,75,451,396);
                case TownSceneryKind.CafeTerrace:return (458,106,335,407);
                case TownSceneryKind.FloristCart:return (805,134,349,367);
                case TownSceneryKind.NoticeBoard:return (1160,99,362,409);
                case TownSceneryKind.GardenPergola:return (11,545,435,374);
                case TownSceneryKind.FlowerHedge:return (455,555,355,379);
                case TownSceneryKind.TreeGrove:return (828,533,302,399);
                case TownSceneryKind.FlowerBorder:return (1130,640,399,288);
                default:throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
        private static float Aspect(TownSceneryKind kind){var b=SpriteBounds(kind);return (float)b.Width/b.Height;}
        private static TownPoint[] Box(float x,float y,float width,float depth)=>new[]{new TownPoint(x-width/2,y),new TownPoint(x+width/2,y),new TownPoint(x+width/2,y+depth),new TownPoint(x-width/2,y+depth)};
        private static IReadOnlyList<TownSceneryPlacement> Build()
        {
            var list=new List<TownSceneryPlacement>();
            void Add(TownSceneryKind kind,TownDistrictId district,float x,float y,float width,int level=0,float solidWidth=0,float solidDepth=0)
            {list.Add(new TownSceneryPlacement(kind,district,new TownPoint(x,y),new TownPoint(x,y+solidDepth/2),width,width/Aspect(kind),level,solidWidth>0?Box(x,y,solidWidth,solidDepth):null));}
            Add(TownSceneryKind.CottagePair,TownDistrictId.Central,-.40f,.66f,.56f,0,.49f,.12f);
            Add(TownSceneryKind.CottagePair,TownDistrictId.West,-1.66f,.78f,.57f,0,.49f,.12f);
            Add(TownSceneryKind.CottagePair,TownDistrictId.East,1.65f,.78f,.58f,0,.50f,.12f);
            Add(TownSceneryKind.CottagePair,TownDistrictId.North,.43f,2.78f,.57f,0,.49f,.12f);
            Add(TownSceneryKind.CottagePair,TownDistrictId.South,-.43f,-1.25f,.55f,0,.47f,.12f);
            Add(TownSceneryKind.NoticeBoard,TownDistrictId.North,-.45f,2.28f,.24f,0,.14f,.07f);
            // The hedge banks make frontcourts feel planted while leaving
            // the continuous east/west and north/south openings broad.
            Add(TownSceneryKind.FlowerHedge,TownDistrictId.West,-1.50f,-.72f,.33f,0,.25f,.06f);
            Add(TownSceneryKind.FlowerHedge,TownDistrictId.East,1.52f,-.84f,.33f,0,.25f,.06f);
            Add(TownSceneryKind.FlowerHedge,TownDistrictId.Central,-.46f,-.72f,.33f,0,.25f,.06f);
            Add(TownSceneryKind.FlowerHedge,TownDistrictId.North,.48f,1.24f,.31f,0,.23f,.06f);
            Add(TownSceneryKind.FlowerHedge,TownDistrictId.South,-.24f,-2.79f,.38f,0,.29f,.07f);
            Add(TownSceneryKind.TreeGrove,TownDistrictId.West,-1.90f,-.72f,.58f);
            Add(TownSceneryKind.TreeGrove,TownDistrictId.East,1.92f,.68f,.58f);
            Add(TownSceneryKind.TreeGrove,TownDistrictId.Central,.73f,-1.35f,.54f);
            Add(TownSceneryKind.TreeGrove,TownDistrictId.North,-.73f,2.64f,.57f);
            Add(TownSceneryKind.TreeGrove,TownDistrictId.South,.72f,-2.77f,.58f);
            // Lv3 adds a usable destination silhouette beside each clear court.
            Add(TownSceneryKind.FlowerBorder,TownDistrictId.West,-1.57f,.71f,.30f,3);
            Add(TownSceneryKind.FlowerBorder,TownDistrictId.East,1.52f,.69f,.31f,3);
            Add(TownSceneryKind.FlowerBorder,TownDistrictId.Central,-.43f,.01f,.30f,3);
            Add(TownSceneryKind.FlowerBorder,TownDistrictId.North,.23f,1.28f,.32f,3);
            Add(TownSceneryKind.GardenPergola,TownDistrictId.South,.46f,-2.54f,.28f,3);
            // Lv6 banks have a clear mass and colour difference at the local focus.
            Add(TownSceneryKind.FlowerBorder,TownDistrictId.West,-.94f,.71f,.34f,6);
            Add(TownSceneryKind.FlowerBorder,TownDistrictId.East,1.49f,-.12f,.32f,6);
            Add(TownSceneryKind.FlowerHedge,TownDistrictId.Central,-.43f,.01f,.31f,6);
            Add(TownSceneryKind.FlowerBorder,TownDistrictId.North,-.45f,2.28f,.31f,6);
            Add(TownSceneryKind.FlowerHedge,TownDistrictId.South,-.21f,-2.70f,.39f,6);
            return list.AsReadOnly();
        }
    }
}
