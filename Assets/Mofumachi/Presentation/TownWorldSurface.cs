using System;
using System.Collections.Generic;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    public readonly struct TownPathSection
    {
        public TownPoint Center {get;} public TownPoint Normal {get;} public float Distance {get;}
        public float TextureV {get;}
        public TownPathSection(TownPoint center,TownPoint normal,float distance)
        {
            Center=center;Normal=normal;Distance=distance;
            TextureV=TownWorldSurface.Mirror(distance/TownWorldSurface.PathTileLength);
        }
        public TownPoint Point(float across)=>new TownPoint(Center.X+Normal.X*across,Center.Y+Normal.Y*across);
    }
    // Display geometry only: never rewrites the navigation graph or camera state.
    public static class TownWorldSurface
    {
        // The shared atlas is lawn on the left and paving on the right. Use the
        // whole panels at the same texel scale, rather than magnify painting crops.
        public const string AtlasResource="Mofumachi/TownGardenSurface";
        // Paving is a visual material, not the limit of movement. The same wide
        // five rooms include walkable lawns, with the existing planted obstacles.
        public const float LocalWalkHalfWidth=.065f,LocalCourtRadius=.075f;
        public static IReadOnlyList<TownPoint[]> LocalWalks {get;}=BuildLocalWalks();
        public static IReadOnlyList<TownPoint[]> PavingPolygons {get;}=BuildPaving();
        public const float LawnTileWidth=.72f,LawnTileHeight=1.44f;
        public const float PathTileLength=.64f,PathHalfWidth=.16f,PathFeather=.025f;
        private static IReadOnlyList<TownPoint[]> BuildLocalWalks()
        {
            var walks=new List<TownPoint[]>();TownPoint P(float x,float y)=>new TownPoint(x,y);
            foreach(float shop in new[]{-1.2f,1.2f})
            {
                float outer=shop<0?-1:1;
                // The outer walk passes the café seating/florist display and
                // stops at the cottage apron, below its planted foundation.
                walks.Add(new[]{P(shop,-.24f),P(shop+outer*.34f,-.40f),P(shop+outer*.50f,-.22f),P(shop+outer*.50f,.12f),P(shop+outer*.44f,.58f)});
                // The inner walk reaches the shaded display/table nook. It
                // ends before the flower bank instead of drawing through it.
                walks.Add(new[]{P(shop,-.24f),P(shop-outer*.34f,-.40f),P(shop-outer*.40f,.03f),P(shop-outer*.38f,.42f),P(shop-outer*.33f,.58f)});
            }
            walks.Add(new[]{P(0,1.66f),P(-.34f,1.85f),P(-.34f,2.12f),P(-.43f,2.19f)});
            walks.Add(new[]{P(0,1.66f),P(.35f,1.87f),P(.35f,2.18f),P(.38f,2.41f),P(.43f,2.58f)});
            return walks.AsReadOnly();
        }
        private static IReadOnlyList<TownPoint[]> BuildPaving()
        {
            var patches=new List<TownPoint[]>();
            void Court(float x,float y,float rx,float ry)
            {
                // Uneven stone skirts meet the garden rather than forming five
                // identical large discs. These are convex for the shared mesh.
                var shape=new TownPoint[16];
                for(int i=0;i<shape.Length;i++)
                {float angle=i*(float)Math.PI*2/shape.Length,edge=1+(i%3-1)*.035f;shape[i]=new TownPoint(x+(float)Math.Cos(angle)*rx*edge,y+(float)Math.Sin(angle)*ry*edge);}
                patches.Add(shape);
            }
            void Route(float half,params TownPoint[] path)
            {
                for(int i=1;i<path.Length;i++)
                {
                    var a=path[i-1];var b=path[i];float dx=b.X-a.X,dy=b.Y-a.Y,length=(float)Math.Sqrt(dx*dx+dy*dy);
                    if(length<.0001f)continue;float nx=-dy/length*half,ny=dx/length*half;
                    patches.Add(new[]{new TownPoint(a.X+nx,a.Y+ny),new TownPoint(a.X-nx,a.Y-ny),new TownPoint(b.X-nx,b.Y-ny),new TownPoint(b.X+nx,b.Y+ny)});
                    if(i<path.Length-1)Court(b.X,b.Y,half,half);
                }
            }
            TownPoint P(float x,float y)=>new TownPoint(x,y);
            // A shared street, distinct forecourts and branching garden routes.
            // Lawns beside these stones remain freely walkable.
            Route(.115f,P(TownWorldMap.Left,-.24f),P(-1.2f,-.24f),P(-.60f,-.20f),P(0,-.22f),P(.60f,-.20f),P(1.2f,-.24f),P(TownWorldMap.Right,-.24f));
            Court(0,-.02f,.31f,.34f);Court(0,.30f,.30f,.24f);
            // The two fountain-side walks meet as one forecourt. Without this
            // overlap their V-shaped join left an isolated grass triangle.
            Court(0,.57f,.15f,.17f);
            Court(-1.2f,-.11f,.29f,.22f);Court(1.2f,-.11f,.29f,.22f);
            foreach(var walk in LocalWalks)
            {
                Route(LocalWalkHalfWidth,walk);var end=walk[walk.Length-1];
                Court(end.X,end.Y,LocalCourtRadius,LocalCourtRadius);
            }
            Route(.085f,P(0,-.36f),P(-.29f,.06f),P(-.28f,.44f),P(0,.70f));
            Route(.085f,P(0,-.36f),P(.29f,.06f),P(.28f,.44f),P(0,.70f));
            Route(.115f,P(0,.67f),P(.05f,1.04f),P(-.04f,1.43f),P(0,1.70f),P(0,2.09f));
            Court(0,1.87f,.28f,.28f);
            Court(0,2.09f,.11f,.10f);
            Route(.115f,P(0,-.30f),P(.04f,-.78f),P(-.02f,-1.12f),P(0,-1.30f));
            Route(.09f,P(0,-1.28f),P(-.34f,-1.50f),P(-.34f,-1.94f),P(0,-2.20f));
            Route(.09f,P(0,-1.28f),P(.34f,-1.50f),P(.34f,-1.94f),P(0,-2.20f));
            Court(0,-2.30f,.28f,.24f);
            Court(0,-2.16f,.24f,.18f);
            Route(.065f,P(-.12f,-2.23f),P(-.31f,-2.45f),P(-.08f,-2.66f),P(.25f,-2.53f),P(.30f,-2.28f),P(.10f,-2.15f));
            return patches.AsReadOnly();
        }
        public static TownPoint LawnUV(float x,float y)=>new TownPoint(.002f+Mirror(x/LawnTileWidth)*.496f,.002f+Mirror(y/LawnTileHeight)*.996f);
        public static TownPoint PavingUV(float across,float v)=>new TownPoint(.502f+(across+1)*.248f,.002f+v*.996f);
        internal static float Mirror(float value)
        {float phase=value%2;if(phase<0)phase+=2;return phase<=1?phase:2-phase;}
        public static float PaintingOpacity(float x,float y)
        {
            if(!float.IsFinite(x)||!float.IsFinite(y))return 0;
            return Smooth(y/.035f)*Smooth((1-y)/.035f);
        }
        public static float PathOpacity(float y,bool north)=>north?Smooth((y-.62f)/.1f)*Smooth((1.13f-y)/.1f):Smooth((y+.33f)/.1f)*Smooth((.17f-y)/.1f);
        private static float Smooth(float t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
        public static IReadOnlyList<TownPathSection> BuildPath(IReadOnlyList<TownPoint> path)
        {
            var result=new List<TownPathSection>();if(path==null||path.Count<2)return result;
            var normals=new TownPoint[path.Count];var distances=new float[path.Count];
            for(int i=0;i<path.Count;i++)
            {
                if(!TownWorldLayout.Finite(path[i]))return result;
                var before=Normal(path[Math.Max(0,i-1)],path[i]);var after=Normal(path[i],path[Math.Min(path.Count-1,i+1)]);
                if(i==0)before=after;if(i==path.Count-1)after=before;
                float x=before.X+after.X,y=(before.Y+after.Y)*TownWorldLayout.ImageAspect;
                float length=(float)Math.Sqrt(x*x+y*y);
                if(length<.00001f){normals[i]=after;continue;}
                x/=length;y/=length;float dot=x*after.X+y*after.Y*TownWorldLayout.ImageAspect;
                float miter=1/Math.Max(2f/3,dot);normals[i]=new TownPoint(x*miter,y*miter/TownWorldLayout.ImageAspect);
            }
            result.Add(new TownPathSection(path[0],normals[0],0));float nextTile=PathTileLength;
            for(int i=1;i<path.Count;i++)
            {
                float dx=path[i].X-path[i-1].X,dy=(path[i].Y-path[i-1].Y)*TownWorldLayout.ImageAspect;
                float length=(float)Math.Sqrt(dx*dx+dy*dy);if(length<.00001f)continue;
                distances[i]=distances[i-1]+length;
                while(nextTile<=distances[i-1]+.00001f)nextTile+=PathTileLength;
                while(nextTile<distances[i]-.00001f)
                {
                    float t=(nextTile-distances[i-1])/length;
                    result.Add(new TownPathSection(Lerp(path[i-1],path[i],t),Lerp(normals[i-1],normals[i],t),nextTile));nextTile+=PathTileLength;
                }
                result.Add(new TownPathSection(path[i],normals[i],distances[i]));
            }
            return result;
        }
        private static TownPoint Lerp(TownPoint a,TownPoint b,float t)=>new TownPoint(a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t);
        private static TownPoint Normal(TownPoint a,TownPoint b)
        {
            float dx=b.X-a.X,dy=(b.Y-a.Y)*TownWorldLayout.ImageAspect,length=(float)Math.Sqrt(dx*dx+dy*dy);
            return length<.00001f?default:new TownPoint(-dy/length,dx/length/TownWorldLayout.ImageAspect);
        }
    }
}
