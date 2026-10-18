using System;
using System.Collections.Generic;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    // A small cached 2D grid for obstacle routing; only a requested route runs A*.
    // There is no NavMesh bake, physics engine, per-frame grid rebuild or SDK.
    public sealed class TownAreaNavigation
    {
        private readonly TownWalkableArea area;
        private readonly List<TownPoint> points=new List<TownPoint>();
        private readonly List<int>[] neighbours;
        public TownAreaNavigation(TownWalkableArea floor)
        {
            area=floor;const int columns=47,rows=70;var grid=new int[columns,rows];
            for(int x=0;x<columns;x++)for(int y=0;y<rows;y++)
            {var p=new TownPoint(TownWorldMap.Left+x*.035f,TownWorldMap.Bottom+y*.035f/TownWorldLayout.ImageAspect);grid[x,y]=-1;if(area.Contains(p)){grid[x,y]=points.Count;points.Add(p);}}
            neighbours=new List<int>[points.Count];for(int i=0;i<points.Count;i++)neighbours[i]=new List<int>(8);
            for(int x=0;x<columns;x++)for(int y=0;y<rows;y++)if(grid[x,y]>=0)
            for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)
            {
                int nx=x+dx,ny=y+dy;if((dx==0&&dy==0)||nx<0||nx>=columns||ny<0||ny>=rows||grid[nx,ny]<0)continue;
                int a=grid[x,y],b=grid[nx,ny];if(area.CanTravel(points[a],points[b]))neighbours[a].Add(b);
            }
        }
        public IReadOnlyList<TownPoint> FindRoute(TownPoint from,TownPoint to,IReadOnlyList<TownPoint> occupied=null)
        {
            if(!area.Contains(from)||!area.Contains(to))return Array.Empty<TownPoint>();
            if(area.CanTravel(from,to)&&ClearOfResidents(from,to,occupied,from))return new[]{to};
            int start=ClosestNode(from,occupied,from),goal=ClosestNode(to,occupied,from);if(start<0||goal<0)return Array.Empty<TownPoint>();
            var cost=new float[points.Count];var parent=new int[points.Count];var closed=new bool[points.Count];
            for(int i=0;i<cost.Length;i++){cost[i]=float.PositiveInfinity;parent[i]=-1;}
            var heap=new MinHeap();cost[start]=0;heap.Add(start,TownWalkableArea.Distance(points[start],to));
            while(heap.Count>0)
            {
                int at=heap.Pop();if(closed[at])continue;closed[at]=true;if(at==goal)break;
                foreach(int next in neighbours[at])
                {
                    if(!ClearOfResidents(points[at],points[next],occupied,from))continue;
                    float distance=cost[at]+TownWalkableArea.Distance(points[at],points[next]);
                    if(distance>=cost[next])continue;cost[next]=distance;parent[next]=at;heap.Add(next,distance+TownWalkableArea.Distance(points[next],to));
                }
            }
            if(!closed[goal])return Array.Empty<TownPoint>();
            var reversed=new List<TownPoint>{to};for(int at=goal;at>=0;at=parent[at]){reversed.Add(points[at]);if(at==start)break;}
            reversed.Reverse();var result=new List<TownPoint>();var previous=from;
            for(int i=0;i<reversed.Count;)
            {
                int furthest=i;for(int j=i;j<reversed.Count;j++)if(area.CanTravel(previous,reversed[j])&&ClearOfResidents(previous,reversed[j],occupied,from))furthest=j;
                result.Add(reversed[furthest]);previous=reversed[furthest];i=furthest+1;
            }
            return result;
        }
        private int ClosestNode(TownPoint p,IReadOnlyList<TownPoint> occupied,TownPoint origin)
        {
            int best=-1;float distance=float.PositiveInfinity;
            for(int i=0;i<points.Count;i++){float d=TownWalkableArea.Distance(p,points[i]);if(d<distance&&area.CanTravel(p,points[i])&&ClearOfResidents(p,points[i],occupied,origin)){distance=d;best=i;}}
            return best;
        }
        private static bool ClearOfResidents(TownPoint a,TownPoint b,IReadOnlyList<TownPoint> occupied,TownPoint origin)
        {
            if(occupied==null)return true;
            const float radius=.078f;float x=b.X-a.X,y=(b.Y-a.Y)*TownWorldLayout.ImageAspect,length=x*x+y*y;
            foreach(var c in occupied)
            {
                // An initially overlapping resident may be left without moving
                // that resident. All other bodies are temporary route obstacles.
                if(TownWalkableArea.Distance(origin,c)<radius)continue;
                float cx=c.X-a.X,cy=(c.Y-a.Y)*TownWorldLayout.ImageAspect,t=length==0?0:Math.Max(0,Math.Min(1,(cx*x+cy*y)/length));
                float dx=cx-x*t,dy=cy-y*t;if(dx*dx+dy*dy<radius*radius)return false;
            }
            return true;
        }
        private sealed class MinHeap
        {
            private readonly List<(int Node,float Cost)> data=new List<(int,float)>();public int Count=>data.Count;
            public void Add(int node,float cost)
            {data.Add((node,cost));int i=data.Count-1;while(i>0){int p=(i-1)/2;if(data[p].Cost<=cost)break;data[i]=data[p];i=p;}data[i]=(node,cost);}
            public int Pop()
            {
                int node=data[0].Node;var tail=data[data.Count-1];data.RemoveAt(data.Count-1);if(data.Count==0)return node;int at=0;
                while(at*2+1<data.Count){int child=at*2+1;if(child+1<data.Count&&data[child+1].Cost<data[child].Cost)child++;if(data[child].Cost>=tail.Cost)break;data[at]=data[child];at=child;}data[at]=tail;return node;
            }
        }
    }
}
