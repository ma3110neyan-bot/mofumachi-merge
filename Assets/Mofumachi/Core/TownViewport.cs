using System;
namespace Mofumachi.Core
{
    public readonly struct TownViewportRect
    {
        public float X {get;} public float Y {get;} public float Width {get;} public float Height {get;}
        public TownViewportRect(float x,float y,float width,float height){X=x;Y=y;Width=width;Height=height;}
        internal bool Valid=>float.IsFinite(X)&&float.IsFinite(Y)&&float.IsFinite(Width)&&float.IsFinite(Height)&&Width>0&&Height>0;
    }
    public sealed class TownViewport
    {
        public const float MaximumZoom=1.15f;
        private readonly float sourceAspect;
        public TownViewportRect UvRect {get;private set;}=new TownViewportRect(0,0,1,1);
        public TownViewport(float sourceAspect){if(!float.IsFinite(sourceAspect)||sourceAspect<=0)throw new ArgumentOutOfRangeException(nameof(sourceAspect));this.sourceAspect=sourceAspect;}
        public bool Configure(float displayAspect,float zoom)
        {
            if(!float.IsFinite(displayAspect)||displayAspect<=0||!float.IsFinite(zoom)||zoom<=0)return false;
            zoom=Math.Max(1,Math.Min(MaximumZoom,zoom));
            float width=Math.Min(1,displayAspect/sourceAspect)/zoom,height=Math.Min(1,sourceAspect/displayAspect)/zoom;
            Set(UvRect.X+UvRect.Width/2-width/2,UvRect.Y+UvRect.Height/2-height/2,width,height);return true;
        }
        public bool TryPan(float normalizedDeltaX,float normalizedDeltaY)
        {if(!float.IsFinite(normalizedDeltaX)||!float.IsFinite(normalizedDeltaY))return false;Set(UvRect.X+normalizedDeltaX*UvRect.Width,UvRect.Y+normalizedDeltaY*UvRect.Height,UvRect.Width,UvRect.Height);return true;}
        public bool TryFrame(TownPoint point,TownViewportRect visibleFrame)
        {
            if(!point.Finite||!visibleFrame.Valid||visibleFrame.X<0||visibleFrame.Y<0||visibleFrame.X+visibleFrame.Width>1||visibleFrame.Y+visibleFrame.Height>1)return false;
            float x=UvRect.X,y=UvRect.Y,px=(point.X-x)/UvRect.Width,py=(point.Y-y)/UvRect.Height;
            if(px<visibleFrame.X)x=point.X-visibleFrame.X*UvRect.Width;else if(px>visibleFrame.X+visibleFrame.Width)x=point.X-(visibleFrame.X+visibleFrame.Width)*UvRect.Width;
            if(py<visibleFrame.Y)y=point.Y-visibleFrame.Y*UvRect.Height;else if(py>visibleFrame.Y+visibleFrame.Height)y=point.Y-(visibleFrame.Y+visibleFrame.Height)*UvRect.Height;
            Set(x,y,UvRect.Width,UvRect.Height);return true;
        }
        private void Set(float x,float y,float width,float height)=>UvRect=new TownViewportRect(Math.Max(0,Math.Min(1-width,x)),Math.Max(0,Math.Min(1-height,y)),width,height);
    }
}
