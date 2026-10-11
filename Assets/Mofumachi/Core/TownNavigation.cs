using System;
using System.Collections.Generic;
namespace Mofumachi.Core
{
    public readonly struct TownPoint
    {
        public float X { get; } public float Y { get; }
        public TownPoint(float x,float y){X=x;Y=y;}
        internal bool Finite=>!float.IsNaN(X)&&!float.IsInfinity(X)&&!float.IsNaN(Y)&&!float.IsInfinity(Y);
        internal static float Distance(TownPoint a,TownPoint b){float x=a.X-b.X,y=a.Y-b.Y;return (float)Math.Sqrt(x*x+y*y);}
        internal static TownPoint Towards(TownPoint a,TownPoint b,float distance){float length=Distance(a,b);if(length<=distance||length<.000001f)return b;float t=distance/length;return new TownPoint(a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t);}
    }
    public readonly struct TownEdge
    {
        public int From { get; } public int To { get; }
        public TownEdge(int from,int to){From=from;To=to;}
    }
    // Coordinates refer to the original town image. Shortest routes are cached once;
    // movement follows connected street segments without crossing buildings/water.
    public sealed class TownNavigation
    {
        private readonly TownPoint[] nodes;private readonly TownEdge[] edges;
        private readonly float[,] distances;private readonly int[,] next;private readonly float halfWidth;
        public TownNavigation(IReadOnlyList<TownPoint> nodes,IReadOnlyList<TownEdge> edges,float halfWidth)
        {
            if(nodes==null||nodes.Count<2||edges==null||edges.Count==0||!float.IsFinite(halfWidth)||halfWidth<=0)throw new ArgumentException("Invalid town paths");
            this.nodes=new TownPoint[nodes.Count];this.edges=new TownEdge[edges.Count];this.halfWidth=halfWidth;
            distances=new float[nodes.Count,nodes.Count];next=new int[nodes.Count,nodes.Count];
            for(int i=0;i<nodes.Count;i++)
            {
                if(!nodes[i].Finite)throw new ArgumentException("Invalid town point");this.nodes[i]=nodes[i];
                for(int j=0;j<nodes.Count;j++){distances[i,j]=i==j?0:float.PositiveInfinity;next[i,j]=-1;}
            }
            for(int i=0;i<edges.Count;i++)
            {
                var e=edges[i];if(e.From<0||e.To<0||e.From>=nodes.Count||e.To>=nodes.Count||e.From==e.To)throw new ArgumentException("Invalid town edge");
                this.edges[i]=e;float length=TownPoint.Distance(nodes[e.From],nodes[e.To]);if(length<.000001f)throw new ArgumentException("Zero length street");
                distances[e.From,e.To]=distances[e.To,e.From]=length;next[e.From,e.To]=e.To;next[e.To,e.From]=e.From;
            }
            for(int k=0;k<nodes.Count;k++)for(int i=0;i<nodes.Count;i++)for(int j=0;j<nodes.Count;j++)
                if(distances[i,k]+distances[k,j]<distances[i,j]){distances[i,j]=distances[i,k]+distances[k,j];next[i,j]=next[i,k];}
        }
        private TownPoint Project(TownPoint p,out int edgeIndex)
        {
            float best=float.PositiveInfinity;TownPoint projected=p;edgeIndex=-1;
            for(int i=0;i<edges.Length;i++)
            {
                var a=nodes[edges[i].From];var b=nodes[edges[i].To];float x=b.X-a.X,y=b.Y-a.Y;
                float t=Math.Max(0,Math.Min(1,((p.X-a.X)*x+(p.Y-a.Y)*y)/(x*x+y*y)));
                var q=new TownPoint(a.X+x*t,a.Y+y*t);float distance=TownPoint.Distance(p,q);
                if(distance<best){best=distance;projected=q;edgeIndex=i;}
            }
            return projected;
        }
        internal TownPoint InitialPosition(TownPoint position)
        { if(!position.Finite)throw new ArgumentException("Invalid initial position");return Project(position,out _); }
        public float DistanceAlongPath(TownPoint from,TownPoint to)
        {
            if(!from.Finite||!to.Finite)return 0;
            var start=Project(from,out int first);var end=Project(to,out int last);
            float entrance=TownPoint.Distance(from,start);
            if(entrance>halfWidth)return 0;
            if(first==last)return entrance+TownPoint.Distance(start,end);
            var a=edges[first];var b=edges[last];float best=float.PositiveInfinity;
            for(int i=0;i<2;i++)for(int j=0;j<2;j++)
            {
                int s=i==0?a.From:a.To,d=j==0?b.From:b.To;
                best=Math.Min(best,TownPoint.Distance(start,nodes[s])+distances[s,d]+TownPoint.Distance(nodes[d],end));
            }
            return float.IsFinite(best)?entrance+best:0;
        }
        public TownPoint InterpolateAlongPath(TownPoint from,TownPoint to,float alpha)
        {
            if(!float.IsFinite(alpha))return from;
            return Move(from,to,DistanceAlongPath(from,to)*Math.Max(0,Math.Min(1,alpha)));
        }
        public TownPoint Move(TownPoint from,TownPoint target,float maxDistance)
        {
            if(!from.Finite||!target.Finite||!float.IsFinite(maxDistance)||maxDistance<=0)return from;
            TownPoint start=Project(from,out int first),end=Project(target,out int last);
            if(TownPoint.Distance(from,start)>halfWidth)return from;
            float entrance=TownPoint.Distance(from,start);
            if(entrance>maxDistance)return TownPoint.Towards(from,start,maxDistance);
            float remaining=maxDistance-entrance;
            if(first==last)return TownPoint.Towards(start,end,remaining);
            var a=edges[first];var b=edges[last];int source=-1,destination=-1;float best=float.PositiveInfinity;
            for(int i=0;i<2;i++)for(int j=0;j<2;j++)
            {
                int s=i==0?a.From:a.To,d=j==0?b.From:b.To;
                float length=TownPoint.Distance(start,nodes[s])+distances[s,d]+TownPoint.Distance(nodes[d],end);
                if(length<best){best=length;source=s;destination=d;}
            }
            if(source<0)return from; // disconnected streets never teleport
            float leg=TownPoint.Distance(start,nodes[source]);if(leg>remaining)return TownPoint.Towards(start,nodes[source],remaining);
            remaining-=leg;
            int current=source;
            while(current!=destination)
            {
                int following=next[current,destination];if(following<0)return from;
                leg=TownPoint.Distance(nodes[current],nodes[following]);
                if(leg>remaining)return TownPoint.Towards(nodes[current],nodes[following],remaining);
                remaining-=leg;current=following;
            }
            return TownPoint.Towards(nodes[destination],end,remaining);
        }
    }
}
