using System;
using System.Collections.Generic;
using System.Linq;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    // A planted backdrop outside the five existing walking rooms, never a new
    // district or collision island. Full sprite rectangles stay off the floor.
    public static class TownBoundaryGardenLayout
    {
        public const string Resource=TownSceneryLayout.NatureResource;
        public static IReadOnlyList<TownSceneryPlacement> Dressing {get;}=Build();
        public static (int X,int Y,int Width,int Height) SpriteBounds(TownSceneryKind kind)
        {
            switch(kind)
            {
                case TownSceneryKind.Tree:return (39,8,690,600);
                case TownSceneryKind.Blossom:return (831,3,682,622);
                case TownSceneryKind.Flowerbed:return (36,606,705,376);
                case TownSceneryKind.Fence:return (782,628,742,384);
                default:throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
        private static IReadOnlyList<TownSceneryPlacement> Build()
        {
            var result=new List<TownSceneryPlacement>();var floors=new TownWalkableArea(0).FloorPolygons;
            void Add(TownSceneryKind kind,float left,float edgeY,float width,bool north,int side)
            {
                var b=SpriteBounds(kind);float height=width*b.Height/b.Width;
                float x=side*(left+width/2),y=north?edgeY:edgeY-height;
                foreach(var floor in floors)
                    if(x+width/2>floor.Min(p=>p.X)&&x-width/2<floor.Max(p=>p.X)&&y+height>floor.Min(p=>p.Y)&&y<floor.Max(p=>p.Y))return;
                var feet=new TownPoint(x,y);
                result.Add(new TownSceneryPlacement(kind,north?TownDistrictId.North:TownDistrictId.South,feet,feet,width,height));
            }
            foreach(int side in new[]{-1,1})foreach(bool north in new[]{true,false})
            {
                float bank=north?.61f:.84f;int rows=north?10:11;
                for(int row=0;row<rows;row++)for(int col=0;col<4;col++)
                {
                    float width=.50f+((row+col)%3)*.02f;
                    float y=north?1.05f+row*.22f+(col%2)*.012f:-1.01f-row*.205f;
                    Add((row+2*col)%11==0?TownSceneryKind.Fence:TownSceneryKind.Flowerbed,bank+col*.34f,y,width,north,side);
                }
                foreach(int row in new[]{0,3,6,9})foreach(int col in new[]{1,3})
                {
                    float y=north?1.09f+row*.22f:-1.01f-row*.205f;
                    Add((row+col)%3==0?TownSceneryKind.Blossom:TownSceneryKind.Tree,bank+col*.34f,y,.54f+(row%2)*.06f,north,side);
                }
            }
            return Array.AsReadOnly(result.OrderByDescending(p=>p.Feet.Y).ToArray());
        }
    }
}
