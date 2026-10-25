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
        private static readonly HashSet<TownSceneryPlacement> mirrored=new HashSet<TownSceneryPlacement>();
        public static IReadOnlyList<TownSceneryPlacement> Dressing {get;}=Build();
        public static bool IsMirrored(TownSceneryPlacement placement)=>mirrored.Contains(placement);
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
            void Place(TownSceneryKind kind,float x,float y,float width)
            {
                var b=SpriteBounds(kind);float height=width*b.Height/b.Width;
                foreach(var floor in floors)
                    if(x+width/2>floor.Min(p=>p.X)&&x-width/2<floor.Max(p=>p.X)&&y+height>floor.Min(p=>p.Y)&&y<floor.Max(p=>p.Y))return;
                var feet=new TownPoint(x,y);
                result.Add(new TownSceneryPlacement(kind,TownDistrictLayout.At(feet),feet,feet,width,height));
            }
            void Add(TownSceneryKind kind,float left,float edgeY,float width,bool north,int side)
            {
                var b=SpriteBounds(kind);float height=width*b.Height/b.Width;
                Place(kind,side*(left+width/2),north?edgeY:edgeY-height,width);
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
            // Preserve the UV orientation of every original piece before adding
            // new sorted entries. Final ordinal is no longer a stable art key.
            int originalCount=result.Count;var originalOrder=result.OrderByDescending(p=>p.Feet.Y).ToArray();
            for(int i=1;i<originalOrder.Length;i+=2)mirrored.Add(originalOrder[i]);
            // Tracked edge views can look beyond the old diagonal banks. Finish
            // the North cap and East/West outside strips with the same atlas;
            // the entire rectangles remain outside every physical walking room.
            for(int row=0;row<5;row++)foreach(float x in new[]{-.55f,0,.55f})
                Place(TownSceneryKind.Flowerbed,x,3.15f+row*.24f,.56f);
            Place(TownSceneryKind.Tree,-.46f,3.62f,.56f);
            Place(TownSceneryKind.Blossom,.46f,3.70f,.56f);
            foreach(float side in new[]{-1f,1f})for(int row=0;row<13;row++)
                Place(TownSceneryKind.Flowerbed,side*2.035f,-1.15f+row*.20f,.46f);
            var addedOrder=result.Skip(originalCount).OrderByDescending(p=>p.Feet.Y).ToArray();
            for(int i=1;i<addedOrder.Length;i+=2)mirrored.Add(addedOrder[i]);
            return Array.AsReadOnly(result.OrderByDescending(p=>p.Feet.Y).ToArray());
        }
    }
    // Only the exposed outside edges, using the same painting as the original
    // floor. Geometry is cached once; walking and existing dressing are untouched.
    public static class TownBoundaryCurveLayout
    {
        public const string Resource=TownWestMasterLayout.Resource;
        public const int Patches=7,Columns=17,Rows=4;
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
            else if(patch<4)
            {
                float side=patch==2?-1:1,y=1.23f+2*along;
                world=new TownPoint(side*(.6f+d),y);sample=new TownPoint(side*(.6f-Math.Abs(d)),y);
            }
            else if(patch<6)
            {
                float side=patch==4?-1:1,y=-1+2.23f*along;
                world=new TownPoint(side*(1.8f+d),y);sample=new TownPoint(side*(1.8f-Math.Abs(d)),y);
            }
            else
            {
                float x=-.6f+1.2f*along;world=new TownPoint(x,3.23f+d);sample=new TownPoint(x,3.23f-Math.Abs(d));
            }
            var uv=patch==0||patch==4?TownWestMasterLayout.TexturePoint(sample):patch==1||patch==5?TownEastMasterLayout.TexturePoint(sample):TownNorthMasterLayout.TexturePoint(sample);
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
