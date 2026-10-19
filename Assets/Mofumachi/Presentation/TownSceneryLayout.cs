using System;
using System.Collections.Generic;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    public enum TownSceneryKind {Cafe,Florist,QuestCottage,Gazebo,Fountain,Tree,Blossom,Flowerbed,Fence}
    public sealed class TownSceneryPlacement
    {
        public TownSceneryKind Kind {get;} public TownDistrictId District {get;}
        public TownPoint Feet {get;} public TownPoint Foundation {get;}
        public float Width {get;} public float Height {get;}
        public TownSceneryPlacement(TownSceneryKind kind,TownDistrictId district,TownPoint feet,TownPoint foundation,float width,float height)
        {Kind=kind;District=district;Feet=feet;Foundation=foundation;Width=width;Height=height;}
    }
    // World units, independent of source artwork dimensions and phone aspect.
    public static class TownSceneryLayout
    {
        public const float CameraLift=.42f,FountainHeight=.214f;
        public const string LandmarkResource="Mofumachi/TownLandmarksAtlas",NatureResource="Mofumachi/TownNatureAtlas";
        private static TownSceneryPlacement P(TownSceneryKind kind,TownDistrictId district,float x,float footY,float centerY,float width,float height)=>new TownSceneryPlacement(kind,district,new TownPoint(x,footY),new TownPoint(x,centerY),width,height);
        public static IReadOnlyList<TownSceneryPlacement> Landmarks {get;}=Array.AsReadOnly(new[]{
            P(TownSceneryKind.Fountain,TownDistrictId.Central,0,.18f,.30f,.36f,FountainHeight),
            P(TownSceneryKind.Cafe,TownDistrictId.West,-1.2f,.10f,.40f,.58f,.58f*560/602),
            P(TownSceneryKind.Florist,TownDistrictId.East,1.2f,.10f,.40f,.55f,.55f*610/579),
            P(TownSceneryKind.QuestCottage,TownDistrictId.North,0,2.14f,2.40f,.48f,.48f*547/510),
            P(TownSceneryKind.Gazebo,TownDistrictId.South,0,-1.90f,-1.65f,.50f,.50f*517/519)});
        public static IReadOnlyList<TownSceneryPlacement> Nature {get;}=BuildNature();
        private static IReadOnlyList<TownSceneryPlacement> BuildNature()
        {
            var result=new List<TownSceneryPlacement>();var floor=new TownWalkableArea(0);
            foreach(var hole in floor.ExcludedIslands)
            {
                if(hole.X>.16f)continue;
                result.Add(new TownSceneryPlacement(TownSceneryKind.Flowerbed,TownDistrictLayout.At(hole.Center),new TownPoint(hole.Center.X,hole.Center.Y-hole.Y*.65f),hole.Center,hole.X*2.05f,hole.X*2.05f/1.8f));
            }
            // Only outer edges receive hedges. Shared edges remain wide open.
            int count=0;
            foreach(var polygon in floor.FloorPolygons)for(int edge=0;edge<polygon.Length;edge++)
            {
                var a=polygon[edge];var b=polygon[(edge+1)%polygon.Length];float dx=b.X-a.X,dy=b.Y-a.Y;
                float length=(float)Math.Sqrt(dx*dx+dy*dy),nx=dy/length,ny=-dx/length;
                int steps=(int)Math.Ceiling(length/.20f);
                for(int step=0;step<steps;step++)
                {
                    float t=(step+.5f)/steps,x=a.X+dx*t,y=a.Y+dy*t;
                    if(floor.Contains(new TownPoint(x+nx*.02f,y+ny*.02f)))continue;
                    var hedge=new TownPoint(x+nx*.055f,y+ny*.055f);var district=TownDistrictLayout.At(new TownPoint(x-nx*.02f,y-ny*.02f));
                    result.Add(new TownSceneryPlacement(TownSceneryKind.Fence,district,new TownPoint(hedge.X,hedge.Y-.035f),hedge,.23f,.13f));
                    if(count++%2!=0)continue;
                    var tree=new TownPoint(x+nx*.20f,y+ny*.20f);
                    if(floor.Contains(tree))continue;
                    var kind=count%4==1?TownSceneryKind.Blossom:TownSceneryKind.Tree;
                    // The foundations stay outside the walking rooms; crowns
                    // overhang their verges rather than disappear offscreen.
                    const float crown=.66f;float height=crown*(kind==TownSceneryKind.Tree?600f/690:622f/682);
                    result.Add(new TownSceneryPlacement(kind,district,new TownPoint(tree.X,tree.Y-.04f),tree,crown,height));
                }
            }
            return result.AsReadOnly();
        }
    }
}
