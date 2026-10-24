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
    // Only the exposed outside edges, using the same painting as the original
    // floor. Geometry is cached once; walking and existing dressing are untouched.
    public static class TownBoundaryCurveLayout
    {
        public const string Resource=TownWestMasterLayout.Resource;
        public const int Patches=4,Columns=17,Rows=4;
        public static IReadOnlyList<(TownPoint World,TownPoint UV,float Alpha)> Vertices {get;}=BuildVertices();
        public static (TownPoint World,TownPoint UV,float Alpha) GetVertex(int patch,float along,int row)
        {
            if(patch<0||patch>=Patches||row<0||row>=Rows||!float.IsFinite(along)||along<0||along>1)throw new ArgumentOutOfRangeException();
            float wave=(float)Math.Sin(Math.PI*along),detail=(float)Math.Sin(3*Math.PI*along+patch*.7);
            float depth=.065f+.055f*wave*wave+.012f*detail*detail;
            float d=row==0?-.006f:row==1?0:depth*(row==2?.7f:1);
            TownPoint world,sample;
            if(patch<2)
            {
                float x=patch==0?-1.8f+.88f*along:.92f+.88f*along;
                world=new TownPoint(x,-1-d);sample=new TownPoint(x,-1+Math.Abs(d));
            }
            else
            {
                float side=patch==2?-1:1,y=1.23f+2*along;
                world=new TownPoint(side*(.6f+d),y);sample=new TownPoint(side*(.6f-Math.Abs(d)),y);
            }
            var uv=patch==0?TownWestMasterLayout.TexturePoint(sample):patch==1?TownEastMasterLayout.TexturePoint(sample):TownNorthMasterLayout.TexturePoint(sample);
            float ends=Math.Min(1,along/.06f)*Math.Min(1,(1-along)/.06f);
            return (world,uv,row==3?0:ends);
        }
        private static IReadOnlyList<(TownPoint World,TownPoint UV,float Alpha)> BuildVertices()
        {
            var result=new List<(TownPoint,TownPoint,float)>();
            for(int p=0;p<Patches;p++)for(int row=0;row<Rows;row++)for(int col=0;col<Columns;col++)result.Add(GetVertex(p,col/(float)(Columns-1),row));
            return result.AsReadOnly();
        }
    }
}
