using System;
using System.Collections.Generic;
using System.Linq;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    // Rendering coordinates only. Stable region/place IDs and savev3 remain unchanged.
    public static class TownWorldLayout
    {
        public const float RegionSpacing=.9f,ArtHeight=.75f,ImageAspect=1798f/875f;
        public static float Origin(TownRegionId r)=>r==TownRegionId.West?RegionSpacing:r==TownRegionId.East?-RegionSpacing:0;
        public static string Name(TownRegionId r)=>r==TownRegionId.West?"北のカフェ":r==TownRegionId.East?"南の花の小道":"中央公園";
        public static TownPoint ToWorld(TownRegionId r,TownPoint p)=>new TownPoint(p.X,p.Y+Origin(r));
        public static TownPoint ToLocal(TownRegionId r,TownPoint p)=>new TownPoint(p.X,p.Y-Origin(r));
        public static bool Finite(TownPoint p)=>float.IsFinite(p.X)&&float.IsFinite(p.Y);
        public static float Distance(TownPoint a,TownPoint b)=>(float)Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));
        public static TownRegionId RegionAt(TownPoint p)=>p.Y>.82f?TownRegionId.West:p.Y<-.12f?TownRegionId.East:TownRegionId.Central;
        public static readonly TownPoint[] NorthConnection={new TownPoint(.18f,.60f),new TownPoint(.13f,.72f),new TownPoint(.23f,.80f),new TownPoint(.40f,.89f),new TownPoint(.52f,1.04f),new TownPoint(.70f,1.22f),new TownPoint(.94f,1.45f)};
        public static readonly TownPoint[] SouthConnection={new TownPoint(.42f,.47f),new TownPoint(.52f,.32f),new TownPoint(.50f,.12f),new TownPoint(.46f,.01f),new TownPoint(.39f,-.09f),new TownPoint(.23f,-.23f),new TownPoint(.09f,-.40f)};
        public static void BuildGraph(int level,out TownPoint[] nodes,out TownEdge[] edges)
        {
            var n=new List<TownPoint>();var e=new List<TownEdge>();
            foreach(TownRegionId region in Enum.GetValues(typeof(TownRegionId)))
            {int offset=n.Count;var map=TownRegionCatalog.ForLevel(region,level).Map;n.AddRange(map.Nodes.Select(p=>ToWorld(region,p)));e.AddRange(map.Edges.Select(x=>new TownEdge(offset+x.From,offset+x.To)));}
            foreach(var path in new[]{NorthConnection,SouthConnection})
            {
                int previous=Index(n,path[0]);
                for(int i=1;i<path.Length;i++){int next=Index(n,path[i]);e.Add(new TownEdge(previous,next));previous=next;}
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
        public TownPoint Center {get;private set;}=new TownPoint(.5f,.45f);
        public void Jump(TownPoint p){if(TownWorldLayout.Finite(p))Center=Clamp(p);}
        public void Follow(TownPoint p,float dt)
        {if(!TownWorldLayout.Finite(p)||!float.IsFinite(dt)||dt<=0)return;float t=1-(float)Math.Exp(-Math.Min(dt,.1f)*8);var target=Clamp(p);Center=new TownPoint(Center.X+(target.X-Center.X)*t,Center.Y+(target.Y-Center.Y)*t);}
        public void Pan(float x,float y){if(float.IsFinite(x)&&float.IsFinite(y))Jump(new TownPoint(Center.X+x,Center.Y+y));}
        private static TownPoint Clamp(TownPoint p)=>new TownPoint(Math.Max(.42f,Math.Min(.58f,p.X)),Math.Max(-.63f,Math.Min(1.42f,p.Y)));
    }
}
