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
        public static IReadOnlyList<TownPoint[]> PavingPolygons {get;}=BuildPaving();
        public const float LawnTileWidth=1,LawnTileHeight=1;
        public const float PathTileLength=.64f,PathHalfWidth=.16f,PathFeather=.025f;
        private static IReadOnlyList<TownPoint[]> BuildPaving()
        {
            var patches=new List<TownPoint[]>();
            TownPoint[] Box(float left,float bottom,float right,float top)=>new[]{new TownPoint(left,bottom),new TownPoint(right,bottom),new TownPoint(right,top),new TownPoint(left,top)};
            // Broad streets join the same four exits. They do not constrain the
            // independent walking surface or replace a room with a narrow lane.
            patches.Add(Box(TownWorldMap.Left,-.39f,TownWorldMap.Right,-.01f));
            patches.Add(Box(-.18f,TownWorldMap.Bottom,.18f,TownWorldMap.Top));
            foreach(var district in TownDistrictLayout.All)
            {
                var c=TownDistrictLayout.Center(district);bool central=district==TownDistrictId.Central;
                float y=c.Y+(central?-.06f:-.20f),radiusX=central?.44f:district==TownDistrictId.South?.41f:.43f;
                float radiusY=central?.62f:district==TownDistrictId.South?.43f:.42f;
                var plaza=new TownPoint[24];
                for(int i=0;i<plaza.Length;i++)
                {float angle=i*(float)Math.PI*2/plaza.Length;plaza[i]=new TownPoint(c.X+(float)Math.Cos(angle)*radiusX,y+(float)Math.Sin(angle)*radiusY);}
                patches.Add(plaza);
            }
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
