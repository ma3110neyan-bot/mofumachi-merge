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
            // The October18 east-west map, UV from top-left. Broad floor polygons
            // overlap at the central plaza. Planted islands are excluded below.
            AddMap(new[]{P(.33f,.38f),P(.67f,.38f),P(.74f,.47f),P(.74f,.59f),P(.67f,.73f),P(.59f,.83f),P(.55f,.93f),P(.59f,1),P(.40f,1),P(.42f,.92f),P(.44f,.84f),P(.39f,.77f),P(.32f,.73f),P(.25f,.66f),P(.27f,.55f),P(.30f,.45f)});
            AddMap(new[]{P(.065f,.48f),P(.15f,.48f),P(.19f,.50f),P(.30f,.48f),P(.42f,.42f),P(.44f,.54f),P(.35f,.58f),P(.27f,.56f),P(.14f,.56f),P(.08f,.55f)});
            AddMap(new[]{P(.02f,.55f),P(.09f,.53f),P(.15f,.58f),P(.26f,.59f),P(.34f,.64f),P(.45f,.71f),P(.47f,.78f),P(.38f,.79f),P(.28f,.74f),P(.17f,.71f),P(.05f,.73f),P(0,.69f)});
            AddMap(new[]{P(.62f,.39f),P(.71f,.40f),P(.74f,.45f),P(.81f,.46f),P(.90f,.50f),P(1,.45f),P(1,.53f),P(.93f,.62f),P(.84f,.65f),P(.77f,.61f),P(.68f,.59f),P(.63f,.55f)});
            AddMap(new[]{P(.61f,.66f),P(.72f,.68f),P(.80f,.65f),P(.88f,.59f),P(.96f,.59f),P(.96f,.65f),P(.88f,.71f),P(.80f,.76f),P(.70f,.78f),P(.60f,.77f),P(.57f,.73f)});
            AddMap(new[]{P(.31f,0),P(.61f,0),P(.58f,.08f),P(.53f,.14f),P(.53f,.20f),P(.61f,.23f),P(.61f,.31f),P(.67f,.35f),P(.67f,.41f),P(.42f,.43f),P(.38f,.35f),P(.36f,.28f),P(.38f,.23f),P(.40f,.19f),P(.34f,.13f),P(.30f,.07f)});
            AddMap(new[]{P(.16f,.29f),P(.19f,.29f),P(.225f,.26f),P(.28f,.24f),P(.34f,.20f),P(.42f,.23f),P(.43f,.17f),P(.36f,.15f),P(.29f,.18f),P(.25f,.205f),P(.21f,.225f),P(.17f,.25f),P(.15f,.27f)});
            // Keep a lower loop between the two large planted garden islands.
            AddMap(new[]{P(0,.73f),P(.06f,.72f),P(.15f,.81f),P(.26f,.90f),P(.40f,.95f),P(.55f,.96f),P(.70f,.93f),P(.86f,.87f),P(1,.76f),P(1,.85f),P(.86f,.94f),P(.69f,1),P(.30f,1),P(.15f,.92f),P(.04f,.84f),P(0,.80f)});
            Island(.14f,.53f,.05f,.027f);Island(.215f,.55f,.046f,.025f); // reserved cafe furniture slots at every level
            Island(.52f,.49f,.128f,.080f); // fountain and planted stone apron
            Island(.35f,.635f,.094f,.066f);Island(.70f,.642f,.105f,.074f);
            Island(.19f,.582f,.092f,.043f);
            Island(.28f,.839f,.198f,.100f);Island(.735f,.827f,.195f,.104f);
            Island(.39f,.305f,.078f,.057f);Island(.61f,.283f,.087f,.058f);

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
