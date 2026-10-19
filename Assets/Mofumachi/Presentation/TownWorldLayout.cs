using System;
using System.Collections.Generic;
using System.Linq;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    // Rendering coordinates only. Stable region/place IDs and savev3 remain unchanged.
    public static class TownWorldLayout
    {
        public const float RegionSpacing=1.2f,ArtHeight=2f,ImageAspect=1f,ViewWidth=1.10f;
        public static float ScaleForViewport(float width,float height)
        {if(!float.IsFinite(width)||!float.IsFinite(height)||width<=0||height<=0)return 1;return Math.Max(width/ViewWidth,height/(TownWorldMap.Height*ImageAspect));}
        public static float LabelCenter(float x,float left,float right,float width)
        {float half=Math.Min(width/2,(right-left)/2);return Math.Max(left+half,Math.Min(right-half,x));}
        public static float CaptionDrop(float doorTop,float captionHeight,float viewportTop)=>Math.Min(0,viewportTop-4-doorTop-captionHeight);
        public static bool LabelOverlapsResident(TownViewportRect label,TownPoint feet,float width,float height)=>
            label.X<feet.X+width/2&&label.X+label.Width>feet.X-width/2&&label.Y<feet.Y+height&&label.Y+label.Height>feet.Y;
        public static float Origin(TownRegionId r)=>r==TownRegionId.West?-RegionSpacing:r==TownRegionId.East?RegionSpacing:0;
        public static string Name(TownRegionId r)=>r==TownRegionId.West?"西のカフェ通り":r==TownRegionId.East?"東の花屋通り":"中央広場";
        public static TownPoint ToWorld(TownRegionId r,TownPoint p)=>new TownPoint(p.X+Origin(r),p.Y);
        public static TownPoint ToLocal(TownRegionId r,TownPoint p)=>new TownPoint(p.X-Origin(r),p.Y);
        public static bool Finite(TownPoint p)=>float.IsFinite(p.X)&&float.IsFinite(p.Y);
        public static float Distance(TownPoint a,TownPoint b)=>(float)Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));
        public static bool WithinConversationRange(TownPoint a,TownPoint b,float viewportWidth,float actorWidth)
        {
            if(!Finite(a)||!Finite(b)||!float.IsFinite(viewportWidth)||viewportWidth<=0||!float.IsFinite(actorWidth)||actorWidth<=0)return false;
            // Match the field projection, including its taller vertical world units.
            // Residents can greet across a half-body gap without calling across the street.
            float scale=viewportWidth/ViewWidth,dx=(a.X-b.X)*scale,dy=(a.Y-b.Y)*scale*ImageAspect;
            float reach=actorWidth*1.5f;
            return dx*dx+dy*dy<=reach*reach;
        }
        public static TownRegionId RegionAt(TownPoint p)=>TownDistrictLayout.LegacyRegion(TownDistrictLayout.At(p));
        public static readonly TownPoint[] NorthConnection={new TownPoint(0,.65f),new TownPoint(0,1),new TownPoint(0,1.40f)};
        public static readonly TownPoint[] SouthConnection={new TownPoint(0,-.65f),new TownPoint(0,-1),new TownPoint(0,-1.40f)};
        public static void BuildGraph(int level,out TownPoint[] nodes,out TownEdge[] edges)
        {
            var n=new List<TownPoint>{TownWorldMap.Place("square")};var e=new List<TownEdge>();
            var area=new TownWalkableArea(level);var navigation=new TownAreaNavigation(area);
            var targets=new List<TownPoint>();
            foreach(TownRegionId region in Enum.GetValues(typeof(TownRegionId)))
                targets.AddRange(TownRegionCatalog.ForLevel(region,level).Map.Places.Select(p=>TownWorldMap.Place(p.Id)));
            foreach(var district in TownDistrictLayout.All)targets.Add(TownWorldMap.Place(TownDistrictLayout.EntranceId(district)));
            foreach(var target in targets)
            {
                int previous=0;
                foreach(var p in navigation.FindRoute(n[0],target))
                {int next=Index(n,p);if(next!=previous)e.Add(new TownEdge(previous,next));previous=next;}
            }
            nodes=n.ToArray();edges=e.ToArray();
        }
        private static int Index(List<TownPoint> nodes,TownPoint p)
        {int i=nodes.FindIndex(n=>Distance(n,p)<.0001f);if(i>=0)return i;nodes.Add(p);return nodes.Count-1;}
        public static bool TryProject(TownPoint point,IReadOnlyList<TownPoint> nodes,IReadOnlyList<TownEdge> edges,out TownPoint projected,float width=.045f)
        {
            projected=default;if(!Finite(point))return false;float best=float.PositiveInfinity;
            foreach(var e in edges)
            {var a=nodes[e.From];var b=nodes[e.To];float dx=b.X-a.X,dy=b.Y-a.Y;float t=Math.Max(0,Math.Min(1,((point.X-a.X)*dx+(point.Y-a.Y)*dy)/(dx*dx+dy*dy)));var p=new TownPoint(a.X+dx*t,a.Y+dy*t);float d=Distance(point,p);if(d<best){best=d;projected=p;}}
            return best<=width;
        }
    }
    public sealed class TownWorldCamera
    {
        private float halfWidth=TownWorldLayout.ViewWidth/2,halfHeight=.70f;
        public TownPoint Center {get;private set;}=new TownPoint(0,-.25f);
        public void Fit(float width,float height)
        {if(!float.IsFinite(width)||!float.IsFinite(height)||width<=0||height<=0)return;halfWidth=Math.Min(width,TownWorldMap.Width)/2;halfHeight=Math.Min(height,TownWorldMap.Height)/2;Center=Clamp(Center);}
        public void Jump(TownPoint p){if(TownWorldLayout.Finite(p))Center=Clamp(p);}
        public void Follow(TownPoint p,float dt)
        {if(!TownWorldLayout.Finite(p)||!float.IsFinite(dt)||dt<=0)return;float t=1-(float)Math.Exp(-Math.Min(dt,.1f)*8);var target=Clamp(p);Center=new TownPoint(Center.X+(target.X-Center.X)*t,Center.Y+(target.Y-Center.Y)*t);}
        public void Pan(float x,float y){if(float.IsFinite(x)&&float.IsFinite(y))Jump(new TownPoint(Center.X+x,Center.Y+y));}
        private TownPoint Clamp(TownPoint p)=>new TownPoint(Math.Max(TownWorldMap.Left+halfWidth,Math.Min(TownWorldMap.Right-halfWidth,p.X)),Math.Max(TownWorldMap.Bottom+halfHeight,Math.Min(TownWorldMap.Top-halfHeight,p.Y)));
    }
}
