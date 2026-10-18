using System;
using System.Collections.Generic;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    // Feet-space polygons measured against the single seamless town painting.
    // Stable places/save IDs are unchanged. Furniture/fountain islands are holes.
    public sealed class TownWalkableArea
    {
        private readonly List<(TownPoint[] Points,float Left,float Right,float Bottom,float Top)> floors=new List<(TownPoint[],float,float,float,float)>();
        private readonly List<(TownPoint Center,float X,float Y)> islands=new List<(TownPoint,float,float)>();
        private readonly TownPoint[][] connections=Array.Empty<TownPoint[]>();
        private const float Corridor=.115f;
        [ThreadStatic] private static List<float> segmentCuts;
        public TownWalkableArea(int level)
        {
            // Paving outlines traced on TownSeamlessMap, in image UV coordinates.
            // These overlap at plazas; planted beds and furniture remain islands.
            AddMap(new[]{P(.43f,0),P(.60f,0),P(.61f,.06f),P(.52f,.10f),P(.53f,.16f),P(.62f,.23f),P(.62f,.29f),P(.58f,.34f),P(.65f,.37f),P(.52f,.40f),P(.38f,.37f),P(.41f,.32f),P(.50f,.26f),P(.50f,.19f),P(.44f,.14f),P(.40f,.10f),P(.42f,.05f)});
            AddMap(new[]{P(.14f,.185f),P(.30f,.175f),P(.43f,.18f),P(.46f,.21f),P(.49f,.23f),P(.52f,.25f),P(.49f,.28f),P(.43f,.27f),P(.40f,.25f),P(.36f,.25f),P(.28f,.24f),P(.19f,.23f),P(.13f,.218f)});
            AddMap(new[]{P(.57f,.28f),P(.69f,.25f),P(.77f,.24f),P(.85f,.24f),P(.91f,.28f),P(.88f,.32f),P(.80f,.35f),P(.72f,.37f),P(.61f,.34f)});
            AddMap(new[]{P(.37f,.33f),P(.46f,.35f),P(.54f,.35f),P(.64f,.34f),P(.78f,.36f),P(.86f,.40f),P(.83f,.48f),P(.82f,.53f),P(.88f,.58f),P(.76f,.61f),P(.65f,.60f),P(.58f,.64f),P(.42f,.64f),P(.32f,.60f),P(.23f,.60f),P(.16f,.57f),P(.18f,.51f),P(.15f,.47f),P(.16f,.42f),P(.25f,.38f)});
            AddMap(new[]{P(.42f,.60f),P(.58f,.60f),P(.62f,.65f),P(.58f,.72f),P(.58f,.80f),P(.56f,.85f),P(.61f,.91f),P(.61f,1),P(.42f,1),P(.40f,.94f),P(.44f,.88f),P(.40f,.82f),P(.39f,.75f),P(.43f,.70f),P(.39f,.65f)});
            AddMap(new[]{P(.43f,.70f),P(.43f,.78f),P(.39f,.82f),P(.29f,.81f),P(.22f,.78f),P(.27f,.73f),P(.31f,.71f)});
            AddMap(new[]{P(.56f,.79f),P(.66f,.80f),P(.76f,.80f),P(.87f,.81f),P(.89f,.84f),P(.82f,.88f),P(.71f,.87f),P(.60f,.89f),P(.54f,.86f)});
            AddMap(new[]{P(.42f,.275f),P(.50f,.275f),P(.47f,.32f),P(.36f,.355f),P(.30f,.37f),P(.24f,.40f),P(.19f,.43f),P(.13f,.41f),P(.17f,.37f),P(.22f,.34f),P(.31f,.315f),P(.35f,.29f)});
            Island(.68f,.275f,.085f,.024f);Island(.88f,.282f,.025f,.020f);
            Island(.5f,.455f,.157f,.075f); // fountain, stone lip and planted apron
            Island(.325f,.407f,.063f,.035f);Island(.681f,.405f,.070f,.037f);
            Island(.361f,.552f,.070f,.029f);Island(.649f,.554f,.074f,.031f);
            Island(.165f,.219f,.035f,.014f);Island(.262f,.232f,.034f,.014f);Island(.380f,.192f,.037f,.014f);

        }
        private static TownPoint P(float x,float y)=>new TownPoint(x,y);
        private void Add(TownRegionId region,TownPoint[] polygon)
        {
            float left=float.PositiveInfinity,right=float.NegativeInfinity,bottom=float.PositiveInfinity,top=float.NegativeInfinity;
            for(int i=0;i<polygon.Length;i++){polygon[i]=TownWorldLayout.ToWorld(region,polygon[i]);left=Math.Min(left,polygon[i].X);right=Math.Max(right,polygon[i].X);bottom=Math.Min(bottom,polygon[i].Y);top=Math.Max(top,polygon[i].Y);}
            floors.Add((polygon,left,right,bottom,top));
        }
        private void AddMap(TownPoint[] polygon)
        {for(int i=0;i<polygon.Length;i++)polygon[i]=TownWorldMap.Point(polygon[i].X,polygon[i].Y);Add(TownRegionId.Central,polygon);}
        private void Island(float u,float v,float rx,float ry)=>islands.Add((TownWorldMap.Point(u,v),rx*TownWorldMap.Width,ry*TownWorldMap.Height));
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
