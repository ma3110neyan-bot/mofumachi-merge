using System;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    public enum StreetTargetKind { Road, Place, Exit }
    public readonly struct StreetTarget
    {
        public StreetTargetKind Kind {get;} public TownPoint Point {get;}
        public string PlaceId {get;} public TownRegionId Region {get;}
        public StreetTarget(StreetTargetKind kind,TownPoint point,TownRegionId region,string placeId=null)
        {Kind=kind;Point=point;Region=region;PlaceId=placeId;}
    }
    // Resolve painted street taps before starting movement. Buildings and locked
    // paths are rejected, rather than silently projecting them to a distant road.
    public static class TownStreetSelection
    {
        private static float Distance(TownPoint a,TownPoint b)
        {float x=a.X-b.X,y=a.Y-b.Y;return (float)Math.Sqrt(x*x+y*y);}
        public static bool TryResolve(TownRegionId region,int level,TownPoint tap,out StreetTarget target)
        {
            target=default;
            if(!Enum.IsDefined(typeof(TownRegionId),region)||!float.IsFinite(tap.X)||!float.IsFinite(tap.Y))return false;
            foreach(var next in new[]{TownRegionId.West,TownRegionId.Central,TownRegionId.East})
                if(TownRegionCatalog.TryGetPortal(region,next,out var portal)&&Distance(tap,portal.From.Local)<.047f)
                {target=new StreetTarget(StreetTargetKind.Exit,portal.From.Local,next);return true;}
            var map=TownRegionCatalog.ForLevel(region,level).Map;
            TownPlace nearest=null;float best=.036f;
            foreach(var place in map.Places)
            {float distance=Distance(tap,place.Position);if(distance<best){best=distance;nearest=place;}}
            if(nearest!=null){target=new StreetTarget(StreetTargetKind.Place,nearest.Position,region,nearest.Id);return true;}
            if(!TryProjectRoad(region,level,tap,out var point))return false;
            target=new StreetTarget(StreetTargetKind.Road,point,region);return true;
        }
        // Projection must be idempotent: a road can run close to a named stop.
        public static bool TryProjectRoad(TownRegionId region,int level,TownPoint tap,out TownPoint point)
        {
            point=default;
            if(!Enum.IsDefined(typeof(TownRegionId),region)||!float.IsFinite(tap.X)||!float.IsFinite(tap.Y))return false;
            var map=TownRegionCatalog.ForLevel(region,level).Map;
            float best=.035f;bool found=false;
            foreach(var edge in map.Edges)
            {
                var a=map.Nodes[edge.From];var b=map.Nodes[edge.To];float x=b.X-a.X,y=b.Y-a.Y;
                float t=Math.Max(0,Math.Min(1,((tap.X-a.X)*x+(tap.Y-a.Y)*y)/(x*x+y*y)));
                var p=new TownPoint(a.X+x*t,a.Y+y*t);float distance=Distance(tap,p);
                if(distance<best){found=true;best=distance;point=p;}
            }
            return found;
        }
    }
}
