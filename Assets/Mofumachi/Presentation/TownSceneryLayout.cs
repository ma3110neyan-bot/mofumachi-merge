using System;
using System.Collections.Generic;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    public enum TownSceneryKind {Cafe,Florist,QuestCottage,Gazebo,Fountain,Tree,Blossom,Flowerbed,Fence,CottagePair,CafeTerrace,FloristCart,NoticeBoard,GardenPergola,FlowerHedge,TreeGrove,FlowerBorder}
    public sealed class TownSceneryPlacement
    {
        public TownSceneryKind Kind {get;} public TownDistrictId District {get;}
        public TownPoint Feet {get;} public TownPoint Foundation {get;}
        public float Width {get;} public float Height {get;} public int Level {get;} public TownPoint[] Footprint {get;}
        public TownSceneryPlacement(TownSceneryKind kind,TownDistrictId district,TownPoint feet,TownPoint foundation,float width,float height,int level=0,TownPoint[] footprint=null)
        {Kind=kind;District=district;Feet=feet;Foundation=foundation;Width=width;Height=height;Level=level;Footprint=footprint;}
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
            // Deliberate groups give the village a skyline without outlining every
            // floor edge with the same repeated fence/tree silhouette.
            void Tree(TownDistrictId district,float x,float y,float width,bool blossom=false)
            {
                var kind=blossom?TownSceneryKind.Blossom:TownSceneryKind.Tree;
                result.Add(new TownSceneryPlacement(kind,district,new TownPoint(x,y),new TownPoint(x,y+.03f),width,width*(blossom?622f/682:600f/690)));
            }
            Tree(TownDistrictId.West,-1.88f,-.18f,.72f);Tree(TownDistrictId.West,-1.82f,.78f,.61f,true);
            Tree(TownDistrictId.East,1.86f,.27f,.72f,true);Tree(TownDistrictId.East,1.86f,-.72f,.65f);
            Tree(TownDistrictId.Central,-.70f,-1.12f,.60f,true);Tree(TownDistrictId.Central,.73f,1.12f,.66f);
            Tree(TownDistrictId.North,-.70f,1.72f,.66f,true);Tree(TownDistrictId.North,.72f,2.48f,.69f);
            Tree(TownDistrictId.South,-.72f,-2.65f,.72f,true);Tree(TownDistrictId.South,.73f,-1.79f,.70f);
            Tree(TownDistrictId.South,-.36f,-3.08f,.65f);
            return result.AsReadOnly();
        }
    }
}
