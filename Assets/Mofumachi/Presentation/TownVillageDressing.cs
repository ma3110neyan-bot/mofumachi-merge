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
        public static (string Resource,int Width,int Height) SpriteSource(TownSceneryKind kind)
        {
            if(kind==TownSceneryKind.ParkBench)return ("Mofumachi/TownStreetPropsAtlas",1254,1254);
            if(kind==TownSceneryKind.StreetLamp)return ("Mofumachi/TownGrowthPropsAtlas",1536,1024);
            if(kind==TownSceneryKind.Fence)return (TownSceneryLayout.NatureResource,1536,1024);
            return (AtlasResource,1536,1024);
        }
        public static IReadOnlyList<TownSceneryPlacement> All {get;}=Build();
        public static IReadOnlyList<TownPoint[]> SolidFootprints {get;}=Array.AsReadOnly(All.Where(p=>p.Footprint!=null).Select(p=>p.Footprint).ToArray());
        // Source pixels, top-left origin. Rendering and world proportions share
        // these measurements; Android downscaling preserves their normalized UVs.
        public static (int X,int Y,int Width,int Height) SpriteBounds(TownSceneryKind kind)
        {
            switch(kind)
            {
                case TownSceneryKind.ParkBench:return (52,92,617,492);
                case TownSceneryKind.StreetLamp:return (1090,3,161,351);
                case TownSceneryKind.Fence:return (782,628,742,384);
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
            Add(TownSceneryKind.CottagePair,TownDistrictId.Central,.45f,.74f,.46f,0,.38f,.10f);
            Add(TownSceneryKind.CottagePair,TownDistrictId.West,-1.66f,.70f,.55f,0,.47f,.12f);
            Add(TownSceneryKind.CottagePair,TownDistrictId.East,1.65f,.70f,.56f,0,.48f,.12f);
            Add(TownSceneryKind.CottagePair,TownDistrictId.North,.43f,2.70f,.54f,0,.46f,.12f);
            Add(TownSceneryKind.CottagePair,TownDistrictId.South,-.43f,-1.28f,.55f,0,.47f,.12f);
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
            // The fountain's two seats establish the park at the initial level.
            // Keep their foundations outside both broad fountain-side routes.
            Add(TownSceneryKind.ParkBench,TownDistrictId.Central,-.40f,.31f,.23f,0,.10f,.05f);
            Add(TownSceneryKind.ParkBench,TownDistrictId.Central,.40f,.31f,.23f,0,.10f,.05f);
            Add(TownSceneryKind.StreetLamp,TownDistrictId.Central,-.47f,.35f,.085f,0,.025f,.025f);
            Add(TownSceneryKind.StreetLamp,TownDistrictId.Central,.47f,.35f,.085f,0,.025f,.025f);
            Add(TownSceneryKind.TreeGrove,TownDistrictId.Central,-.66f,.52f,.40f);
            Add(TownSceneryKind.TreeGrove,TownDistrictId.Central,.66f,.52f,.37f);
            Add(TownSceneryKind.FlowerBorder,TownDistrictId.Central,-.46f,-.08f,.24f,0,.13f,.04f);
            Add(TownSceneryKind.FlowerHedge,TownDistrictId.Central,.47f,.02f,.27f,0,.14f,.04f);
            Add(TownSceneryKind.FlowerBorder,TownDistrictId.Central,-.48f,.55f,.21f,0,.13f,.045f);
            Add(TownSceneryKind.FlowerBorder,TownDistrictId.Central,.48f,.55f,.21f,0,.13f,.045f);
            Add(TownSceneryKind.FlowerBorder,TownDistrictId.Central,.45f,-.73f,.34f,0,.20f,.05f);
            Add(TownSceneryKind.Fence,TownDistrictId.Central,-.47f,-.93f,.34f,0,.22f,.035f);
            Add(TownSceneryKind.Fence,TownDistrictId.Central,.46f,-.98f,.28f,0,.18f,.035f);
            // One vocabulary crosses all four joins. These are world objects,
            // not separate room backdrops or a fence across an exit.
            foreach(int side in new[]{-1,1})
            {
                Add(TownSceneryKind.FlowerBorder,TownDistrictId.Central,side*.62f,-.06f,.25f,0,.15f,.04f);
                Add(TownSceneryKind.FlowerHedge,TownDistrictId.Central,side*.70f,-.64f,.30f,0,.18f,.045f);
                Add(TownSceneryKind.StreetLamp,TownDistrictId.Central,side*.70f,-.53f,.045f,0,.025f,.025f);
                Add(TownSceneryKind.FlowerBorder,TownDistrictId.Central,side*.44f,.91f,.24f,0,.15f,.04f);
                Add(TownSceneryKind.FlowerBorder,TownDistrictId.South,side*.39f,-1.20f,.22f,0,.13f,.035f);
                Add(TownSceneryKind.StreetLamp,TownDistrictId.Central,side*.36f,.95f,.045f,0,.025f,.025f);
            }
            // Give each side walk a visible reason to end: a seat or display
            // beside its open circular apron, never on the walking centreline.
            Add(TownSceneryKind.ParkBench,TownDistrictId.West,-1.48f,.57f,.18f,0,.09f,.045f);
            Add(TownSceneryKind.CafeTerrace,TownDistrictId.West,-.73f,.61f,.18f,0,.09f,.04f);
            Add(TownSceneryKind.ParkBench,TownDistrictId.East,1.48f,.57f,.18f,0,.09f,.045f);
            Add(TownSceneryKind.FloristCart,TownDistrictId.East,.73f,.61f,.18f,0,.09f,.04f);
            Add(TownSceneryKind.ParkBench,TownDistrictId.North,.56f,2.59f,.18f,0,.09f,.04f);
            foreach(int side in new[]{-1,1})
            {
                var district=side<0?TownDistrictId.West:TownDistrictId.East;
                Add(TownSceneryKind.FlowerBorder,district,side*1.53f,.26f,.22f,0,.12f,.04f);
                Add(TownSceneryKind.FlowerBorder,district,side*.65f,.16f,.20f,0,.10f,.035f);
                Add(TownSceneryKind.TreeGrove,district,side*1.78f,-.36f,.34f,0,.12f,.04f);
                Add(TownSceneryKind.Fence,district,side*1.20f,-.63f,.28f,0,.16f,.035f);
                Add(TownSceneryKind.StreetLamp,district,side*1.53f,.32f,.045f,0,.025f,.025f);
                Add(TownSceneryKind.FlowerBorder,district,side*.97f,-.94f,.32f,0,.19f,.04f);
            }
            Add(TownSceneryKind.FlowerBorder,TownDistrictId.North,-.46f,1.45f,.30f,0,.17f,.04f);
            Add(TownSceneryKind.Fence,TownDistrictId.North,-.36f,2.78f,.30f,0,.16f,.035f);
            Add(TownSceneryKind.FlowerBorder,TownDistrictId.North,.48f,1.86f,.20f,0,.11f,.035f);
            Add(TownSceneryKind.StreetLamp,TownDistrictId.North,-.52f,2.10f,.05f,0,.025f,.025f);
            Add(TownSceneryKind.ParkBench,TownDistrictId.South,-.47f,-2.25f,.22f,0,.12f,.05f);
            Add(TownSceneryKind.FlowerBorder,TownDistrictId.South,.48f,-2.10f,.30f,0,.17f,.04f);
            Add(TownSceneryKind.TreeGrove,TownDistrictId.South,-.65f,-1.96f,.37f);
            Add(TownSceneryKind.Fence,TownDistrictId.South,.46f,-2.83f,.30f,0,.18f,.04f);
            Add(TownSceneryKind.StreetLamp,TownDistrictId.South,-.50f,-2.19f,.05f,0,.025f,.025f);
            return list.AsReadOnly();
        }
    }
}
