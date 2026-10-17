using System;
using System.Collections.Generic;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    // Feet-space floor polygons measured against the three original paintings.
    // Stable places/save IDs are unchanged. Furniture/fountain islands are holes.
    public sealed class TownWalkableArea
    {
        private readonly List<(TownPoint[] Points,float Left,float Right,float Bottom,float Top)> floors=new List<(TownPoint[],float,float,float,float)>();
        private readonly List<(TownPoint Center,float X,float Y)> islands=new List<(TownPoint,float,float)>();
        private readonly TownPoint[][] connections={TownWorldLayout.NorthConnection,TownWorldLayout.SouthConnection};
        private const float Corridor=.115f;
        [ThreadStatic] private static List<float> segmentCuts;
        public TownWalkableArea(int level)
        {
            Add(TownRegionId.Central,new[]{P(.10f,.50f),P(.12f,.61f),P(.17f,.66f),P(.36f,.675f),P(.58f,.655f),P(.80f,.64f),P(.86f,.60f),P(.84f,.51f),P(.73f,.45f),P(.62f,.41f),P(.59f,.29f),P(.47f,.25f),P(.37f,.31f),P(.32f,.40f),P(.23f,.425f),P(.12f,.46f)});
            Add(TownRegionId.West,new[]{P(.17f,.27f),P(.33f,.26f),P(.50f,.29f),P(.66f,.36f),P(.93f,.46f),P(.99f,.53f),P(.95f,.61f),P(.80f,.625f),P(.65f,.61f),P(.52f,.56f),P(.42f,.51f),P(.28f,.49f),P(.19f,.40f)});
            Add(TownRegionId.East,new[]{P(.045f,.41f),P(.065f,.52f),P(.18f,.565f),P(.30f,.58f),P(.40f,.57f),P(.51f,.54f),P(.64f,.54f),P(.76f,.49f),P(.80f,.44f),P(.70f,.38f),P(.52f,.365f),P(.38f,.37f),P(.21f,.38f)});
            islands.Add((P(.42f,.555f),.19f,.045f));
            islands.Add((TownWorldLayout.ToWorld(TownRegionId.West,P(.60f,.515f)),.075f,.025f));
            islands.Add((TownWorldLayout.ToWorld(TownRegionId.East,P(.42f,.525f)),.105f,.034f));
        }
        private static TownPoint P(float x,float y)=>new TownPoint(x,y);
        private void Add(TownRegionId region,TownPoint[] polygon)
        {
            float left=float.PositiveInfinity,right=float.NegativeInfinity,bottom=float.PositiveInfinity,top=float.NegativeInfinity;
            for(int i=0;i<polygon.Length;i++){polygon[i]=TownWorldLayout.ToWorld(region,polygon[i]);left=Math.Min(left,polygon[i].X);right=Math.Max(right,polygon[i].X);bottom=Math.Min(bottom,polygon[i].Y);top=Math.Max(top,polygon[i].Y);}
            floors.Add((polygon,left,right,bottom,top));
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
            foreach(var hole in islands)
            {float x=(p.X-hole.Center.X)/hole.X,y=(p.Y-hole.Center.Y)/hole.Y;if(x*x+y*y<.9999f)return false;}
            foreach(var floor in floors)if(p.X>=floor.Left-.00005f&&p.X<=floor.Right+.00005f&&p.Y>=floor.Bottom-.00005f&&p.Y<=floor.Top+.00005f&&InPolygon(p,floor.Points))return true;
            foreach(var path in connections)for(int i=1;i<path.Length;i++)if(Distance(p,Closest(p,path[i-1],path[i]))<=Corridor)return true;
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
            foreach(var path in connections)for(int i=1;i<path.Length;i++)
            {
                var p=path[i-1];var q=path[i];float ry=Corridor/TownWorldLayout.ImageAspect;
                if(right<Math.Min(p.X,q.X)-Corridor||left>Math.Max(p.X,q.X)+Corridor||top<Math.Min(p.Y,q.Y)-ry||bottom>Math.Max(p.Y,q.Y)+ry)continue;
                EllipseCuts(a,b,p,Corridor,ry,cuts);EllipseCuts(a,b,q,Corridor,ry,cuts);
                float dx=q.X-p.X,dy=(q.Y-p.Y)*TownWorldLayout.ImageAspect,length=(float)Math.Sqrt(dx*dx+dy*dy);
                if(length<.000001f)continue;float nx=-dy/length*Corridor,ny=dx/length*ry;
                EdgeCuts(a,b,P(p.X+nx,p.Y+ny),P(q.X+nx,q.Y+ny),cuts);
                EdgeCuts(a,b,P(p.X-nx,p.Y-ny),P(q.X-nx,q.Y-ny),cuts);
            }
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
            foreach(var path in connections)for(int i=1;i<path.Length;i++)
            {
                var center=Closest(p,path[i-1],path[i]);float distance=Distance(p,center),t=distance>0?Math.Min(1,(Corridor-.0001f)/distance):0;
                Offer(P(center.X+(p.X-center.X)*t,center.Y+(p.Y-center.Y)*t));
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
