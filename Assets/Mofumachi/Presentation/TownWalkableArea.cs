using System;
using System.Collections.Generic;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    // Shared floor geometry is used by both rendering and navigation.
    public sealed class TownWalkableArea
    {
        private static readonly TownPoint[][] polygons=BuildPolygons();
        private static readonly (TownPoint Center,float X,float Y)[] excluded={
            (new TownPoint(0,.30f),.17f,.12f),
            (new TownPoint(-1.2f,.4f),.29f,.31f),
            (new TownPoint(1.2f,.4f),.275f,.31f),
            (new TownPoint(0,2.4f),.24f,.26f),
            (new TownPoint(0,-1.65f),.25f,.225f),
            (new TownPoint(-.22f,-.65f),.11f,.13f),
            (new TownPoint(-1.50f,-.60f),.11f,.14f),
            (new TownPoint(1.00f,-.70f),.10f,.12f),
            (new TownPoint(.22f,1.35f),.12f,.13f),
            (new TownPoint(-.20f,-2.65f),.12f,.13f),
            (new TownPoint(-1.50f,-.08f),.10f,.08f),
            (new TownPoint(-.90f,-.66f),.10f,.09f),
            (new TownPoint(-1.57f,.75f),.09f,.09f),
            (new TownPoint(-.95f,.78f),.10f,.09f),
            (new TownPoint(1.48f,-.08f),.10f,.08f),
            (new TownPoint(1.51f,-.76f),.10f,.09f),
            (new TownPoint(.87f,.77f),.10f,.09f),
            (new TownPoint(1.52f,.75f),.09f,.09f),
            (new TownPoint(-.42f,.06f),.08f,.10f),
            (new TownPoint(.44f,-.45f),.07f,.09f),
            (new TownPoint(-.44f,-.31f),.08f,.09f),
            (new TownPoint(-.49f,2.36f),.08f,.10f),
            (new TownPoint(.46f,-2.50f),.08f,.09f),
            (new TownPoint(-.40f,-2.13f),.09f,.10f)};
        private static readonly IReadOnlyList<TownPoint[]> sharedPolygons=Array.AsReadOnly(polygons);
        // The painted front bases have corners; an ellipse alone leaves those
        // opaque posts walkable. Keep their footprints shared with the ground.
        private static readonly IReadOnlyList<TownPoint[]> frontBases=Array.AsReadOnly(new[]{
            Box(-.15f,.18f,.15f,.30f),Box(-1.49f,.10f,-.91f,.34f),
            Box(.925f,.10f,1.475f,.34f),Box(-.24f,2.14f,.24f,2.36f),
            Box(-.25f,-1.90f,.25f,-1.68f),
            Box(-.18f,1.03f,-.13f,1.09f),Box(.13f,1.03f,.18f,1.09f),
            Box(-1.33f,-.80f,-1.28f,-.74f),Box(-1.12f,-.80f,-1.07f,-.74f),
            Box(1.07f,-.84f,1.12f,-.78f),Box(1.28f,-.84f,1.33f,-.78f)});
        private static readonly IReadOnlyList<(TownPoint Center,float X,float Y)> islands=Array.AsReadOnly(excluded);
        private static readonly List<(TownPoint[] Points,float Left,float Right,float Bottom,float Top)> floors=BuildFloors();
        public IReadOnlyList<TownPoint[]> FloorPolygons=>sharedPolygons;
        public IReadOnlyList<TownPoint[]> ExcludedPolygons=>frontBases;
        public IReadOnlyList<(TownPoint Center,float X,float Y)> ExcludedIslands=>islands;
        [ThreadStatic] private static List<float> segmentCuts;
        public TownWalkableArea(int level) { }
        private static TownPoint P(float x,float y)=>new TownPoint(x,y);
        private static TownPoint[] Box(float left,float bottom,float right,float top)=>new[]{P(left,bottom),P(right,bottom),P(right,top),P(left,top)};
        private static TownPoint[][] BuildPolygons()
        {
            var result=new TownPoint[TownDistrictLayout.All.Count][];
            for(int i=0;i<result.Length;i++)
            {
                var c=TownDistrictLayout.Center(TownDistrictLayout.All[i]);
                // Convex rooms meet along wide shared edges; each landmark leaves
                // two independently usable routes across its surrounding floor.
                result[i]=new[]{P(c.X-.60f,c.Y-.76f),P(c.X-.44f,c.Y-1),P(c.X+.44f,c.Y-1),P(c.X+.60f,c.Y-.76f),P(c.X+.60f,c.Y+.76f),P(c.X+.44f,c.Y+1),P(c.X-.44f,c.Y+1),P(c.X-.60f,c.Y+.76f)};
            }
            return result;
        }
        private static List<(TownPoint[] Points,float Left,float Right,float Bottom,float Top)> BuildFloors()
        {
            var result=new List<(TownPoint[],float,float,float,float)>();
            foreach(var polygon in polygons)
            {
                float left=float.PositiveInfinity,right=float.NegativeInfinity,bottom=float.PositiveInfinity,top=float.NegativeInfinity;
                foreach(var p in polygon){left=Math.Min(left,p.X);right=Math.Max(right,p.X);bottom=Math.Min(bottom,p.Y);top=Math.Max(top,p.Y);}
                result.Add((polygon,left,right,bottom,top));
            }
            return result;
        }
        public static float Distance(TownPoint a,TownPoint b)
        {float x=a.X-b.X,y=(a.Y-b.Y)*TownWorldLayout.ImageAspect;return (float)Math.Sqrt(x*x+y*y);}
        private static TownPoint Closest(TownPoint p,TownPoint a,TownPoint b)
        {
            float x=b.X-a.X,y=(b.Y-a.Y)*TownWorldLayout.ImageAspect,den=x*x+y*y;
            float t=den<.0000001f?0:Math.Max(0,Math.Min(1,((p.X-a.X)*x+(p.Y-a.Y)*TownWorldLayout.ImageAspect*y)/den));
            return P(a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t);
        }
        private static bool InPolygon(TownPoint p,TownPoint[] polygon)
        {
            bool inside=false;
            for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++)
            {
                var a=polygon[i];var b=polygon[j];
                if((a.Y>p.Y)!=(b.Y>p.Y)&&p.X<(b.X-a.X)*(p.Y-a.Y)/(b.Y-a.Y)+a.X)inside=!inside;
            }
            if(inside)return true;
            // The normal interior query needs no square roots or nearest-edge
            // projection; only the boundary/outside case needs this tolerance.
            for(int i=0;i<polygon.Length;i++){var q=Closest(p,polygon[i],polygon[(i+1)%polygon.Length]);float x=p.X-q.X,y=(p.Y-q.Y)*TownWorldLayout.ImageAspect;if(x*x+y*y<.0000000025f)return true;}
            return false;
        }
        public bool Contains(TownPoint p)
        {
            if(!TownWorldLayout.Finite(p))return false;
            foreach(var shape in frontBases)
                if(p.X>=shape[0].X-.00005f&&p.X<=shape[2].X+.00005f&&p.Y>=shape[0].Y-.00005f&&p.Y<=shape[2].Y+.00005f&&InPolygon(p,shape))return false;
            foreach(var hole in islands)
            {float x=(p.X-hole.Center.X)/hole.X,y=(p.Y-hole.Center.Y)/hole.Y;if(x*x+y*y<.9999f)return false;}
            foreach(var floor in floors)if(p.X>=floor.Left-.00005f&&p.X<=floor.Right+.00005f&&p.Y>=floor.Bottom-.00005f&&p.Y<=floor.Top+.00005f&&InPolygon(p,floor.Points))return true;
            return false;
        }
        public bool CanTravel(TownPoint a,TownPoint b)
        {
            if(!Contains(a)||!Contains(b))return false;
            // Split at geometric boundaries, then test every open interval.
            // Fixed-distance samples can miss a thin gap and create a route
            // that the next tiny walking step correctly refuses to follow.
            var cuts=segmentCuts??(segmentCuts=new List<float>(128));cuts.Clear();cuts.Add(0);cuts.Add(1);
            float left=Math.Min(a.X,b.X),right=Math.Max(a.X,b.X),bottom=Math.Min(a.Y,b.Y),top=Math.Max(a.Y,b.Y);
            foreach(var floor in floors)
            {
                if(right<floor.Left||left>floor.Right||top<floor.Bottom||bottom>floor.Top)continue;
                for(int i=0;i<floor.Points.Length;i++)EdgeCuts(a,b,floor.Points[i],floor.Points[(i+1)%floor.Points.Length],cuts);
            }
            foreach(var hole in islands)EllipseCuts(a,b,hole.Center,hole.X,hole.Y,cuts);
            foreach(var shape in frontBases)for(int i=0;i<shape.Length;i++)EdgeCuts(a,b,shape[i],shape[(i+1)%shape.Length],cuts);
            cuts.Sort();for(int i=1;i<cuts.Count;i++)
            {float t=(cuts[i-1]+cuts[i])*.5f;if(!Contains(P(a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t)))return false;}
            return true;
        }
        private static void EdgeCuts(TownPoint a,TownPoint b,TownPoint p,TownPoint q,List<float> cuts)
        {
            double x=b.X-a.X,y=b.Y-a.Y,sx=q.X-p.X,sy=q.Y-p.Y,den=x*sy-y*sx;
            if(Math.Abs(den)<1e-12)return;double px=p.X-a.X,py=p.Y-a.Y,t=(px*sy-py*sx)/den,u=(px*y-py*x)/den;
            if(t>0&&t<1&&u>=0&&u<=1)cuts.Add((float)t);
        }
        private static void EllipseCuts(TownPoint a,TownPoint b,TownPoint center,float rx,float ry,List<float> cuts)
        {
            double x=(a.X-center.X)/rx,y=(a.Y-center.Y)/ry,dx=(b.X-a.X)/rx,dy=(b.Y-a.Y)/ry;
            double aa=dx*dx+dy*dy,bb=2*(x*dx+y*dy),cc=x*x+y*y-1,discriminant=bb*bb-4*aa*cc;
            if(aa<1e-15||discriminant<0)return;double root=Math.Sqrt(discriminant),t=(-bb-root)/(2*aa),u=(-bb+root)/(2*aa);
            if(t>0&&t<1)cuts.Add((float)t);if(u>0&&u<1)cuts.Add((float)u);
        }
        public bool TryClamp(TownPoint p,out TownPoint result,float maximum=.09f)
        {
            result=default;if(!TownWorldLayout.Finite(p)||!float.IsFinite(maximum)||maximum<0)return false;
            if(Contains(p)){result=p;return true;}
            float best=float.PositiveInfinity;var nearest=default(TownPoint);
            void Offer(TownPoint candidate)
            {float d=Distance(p,candidate);if(d<best&&Contains(candidate)){best=d;nearest=candidate;}}
            foreach(var floor in floors)for(int i=0;i<floor.Points.Length;i++)Offer(Closest(p,floor.Points[i],floor.Points[(i+1)%floor.Points.Length]));
            foreach(var shape in frontBases)for(int i=0;i<shape.Length;i++)
            {
                var a=shape[i];var b=shape[(i+1)%shape.Length];var q=Closest(p,a,b);float dx=b.X-a.X,dy=b.Y-a.Y,length=(float)Math.Sqrt(dx*dx+dy*dy);
                Offer(P(q.X+dy/length*.00015f,q.Y-dx/length*.00015f));
            }
            foreach(var hole in islands)
            {
                float angle=(float)Math.Atan2((p.Y-hole.Center.Y)/hole.Y,(p.X-hole.Center.X)/hole.X);
                Offer(P(hole.Center.X+(float)Math.Cos(angle)*(hole.X+.0001f),hole.Center.Y+(float)Math.Sin(angle)*(hole.Y+.0001f)));
            }
            if(best>maximum)return false;result=nearest;return true;
        }
    }
}
