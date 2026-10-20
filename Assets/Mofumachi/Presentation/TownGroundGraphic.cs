using System;
using System.Collections.Generic;
using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // Wide walking rooms contain both lawns and paved plazas/roads. Planted
    // holes and building bases remain shared with navigation.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TownGroundGraphic:MaskableGraphic
    {
        private sealed class Patch {public TownPoint[] Points;public Vector2[] UV;public Color Tint;public float Left,Right,Bottom,Top;}
        private readonly List<Patch> patches=new List<Patch>();
        private TownContinuousWorldView world;private Texture2D atlas;private TownMeadowGraphic meadow;private TownPoint previewCenter;private float previewWidth=1.10f;
        public override Texture mainTexture=>atlas;
        private float Scale=>world!=null?world.RenderScale:rectTransform.rect.width/previewWidth;
        private TownPoint Center=>world!=null?world.Camera.Center:previewCenter;
        private Vector2 Local(TownPoint p)=>world!=null?world.Local(p,rectTransform):rectTransform.rect.center+new Vector2((p.X-previewCenter.X)*Scale,(p.Y-previewCenter.Y)*Scale);
        public void Bind(TownContinuousWorldView source){world=source;Initialize();}
        public void BindPreview(TownPoint center,float width){world=null;previewCenter=center;previewWidth=width;Initialize();}
        public void SetPreview(TownPoint center,float width){previewCenter=center;previewWidth=width;meadow?.SetPreview(center,width);SetVerticesDirty();}
        public void Refresh(){meadow?.Refresh();SetVerticesDirty();}
        private void Initialize()
        {
            raycastTarget=false;atlas=Resources.Load<Texture2D>(TownWorldSurface.AtlasResource);patches.Clear();
            if(meadow==null)
            {
                var baseRect=UIWidgets.Node("Town meadow colour",(RectTransform)transform.parent,new Rect(0,0,1,1));
                baseRect.SetSiblingIndex(transform.GetSiblingIndex());meadow=baseRect.gameObject.AddComponent<TownMeadowGraphic>();
            }
            if(world!=null)meadow.Bind(world);else meadow.BindPreview(previewCenter,previewWidth);
            var floor=new TownWalkableArea(0);
            Tile(new[]{new TownPoint(TownWorldMap.Left-.8f,TownWorldMap.Bottom-.8f),new TownPoint(TownWorldMap.Right+.8f,TownWorldMap.Bottom-.8f),new TownPoint(TownWorldMap.Right+.8f,TownWorldMap.Top+.8f),new TownPoint(TownWorldMap.Left-.8f,TownWorldMap.Top+.8f)},false);
            foreach(var polygon in TownWorldSurface.PavingPolygons)Tile(polygon,true);
            foreach(var hole in floor.ExcludedIslands)
            {
                var ring=new TownPoint[40];for(int i=0;i<ring.Length;i++){float a=i*Mathf.PI*2/ring.Length;ring[i]=new TownPoint(hole.Center.X+Mathf.Cos(a)*hole.X,hole.Center.Y+Mathf.Sin(a)*hole.Y);}
                Tile(ring,false);
            }
            foreach(var footprint in floor.ExcludedPolygons)Tile(footprint,false);
            SetAllDirty();
        }
        private void Tile(IReadOnlyList<TownPoint> polygon,bool stone)
        {
            float left=float.PositiveInfinity,right=float.NegativeInfinity,bottom=float.PositiveInfinity,top=float.NegativeInfinity;
            foreach(var p in polygon){left=Math.Min(left,p.X);right=Math.Max(right,p.X);bottom=Math.Min(bottom,p.Y);top=Math.Max(top,p.Y);}
            float tileWidth=stone?.26f:TownWorldSurface.LawnTileWidth,tileHeight=stone?tileWidth*2:TownWorldSurface.LawnTileHeight;
            for(int x=(int)Math.Floor(left/tileWidth);x<(int)Math.Ceiling(right/tileWidth);x++)
            for(int y=(int)Math.Floor(bottom/tileHeight);y<(int)Math.Ceiling(top/tileHeight);y++)
            {
                float l=x*tileWidth,b=y*tileHeight,r=l+tileWidth,t=b+tileHeight;var points=new List<TownPoint>(polygon);
                points=Clip(points,0,l,true);points=Clip(points,0,r,false);points=Clip(points,1,b,true);points=Clip(points,1,t,false);if(points.Count<3)continue;
                var uv=new Vector2[points.Count];float pl=float.PositiveInfinity,pr=float.NegativeInfinity,pb=float.PositiveInfinity,pt=float.NegativeInfinity;
                for(int i=0;i<points.Count;i++)
                {var p=points[i];float u=(p.X-l)/tileWidth,v=(p.Y-b)/tileHeight;
                    if(stone){if((x&1)!=0)u=1-u;if((y&1)!=0)v=1-v;uv[i]=new Vector2(.502f+u*.496f,.002f+v*.996f);}
                    else {var lawn=TownWorldSurface.LawnUV(p.X,p.Y);uv[i]=new Vector2(lawn.X,lawn.Y);}
                    pl=Math.Min(pl,p.X);pr=Math.Max(pr,p.X);pb=Math.Min(pb,p.Y);pt=Math.Max(pt,p.Y);}
                patches.Add(new Patch{Points=points.ToArray(),UV=uv,Tint=stone?Color.white:new Color(1,1,1,.32f),Left=pl,Right=pr,Bottom=pb,Top=pt});
            }
        }
        private static List<TownPoint> Clip(List<TownPoint> input,int axis,float limit,bool greater)
        {
            var output=new List<TownPoint>(input.Count+2);if(input.Count==0)return output;
            float Coord(TownPoint p)=>axis==0?p.X:p.Y;bool Inside(TownPoint p)=>greater?Coord(p)>=limit:Coord(p)<=limit;
            var previous=input[input.Count-1];bool was=Inside(previous);
            foreach(var p in input)
            {
                bool now=Inside(p);if(now!=was){float t=(limit-Coord(previous))/(Coord(p)-Coord(previous));output.Add(new TownPoint(previous.X+(p.X-previous.X)*t,previous.Y+(p.Y-previous.Y)*t));}
                if(now)output.Add(p);previous=p;was=now;
            }
            return output;
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(atlas==null||Scale<=0)return;
            // Work in the graphic's real rectangle; a preview and the live field
            // both use this geometry, with only their projection differing.
            var center=Center;var origin=Local(center);var rect=rectTransform.rect;
            float left=center.X+(rect.xMin-origin.x)/Scale,right=center.X+(rect.xMax-origin.x)/Scale;
            float bottom=center.Y+(rect.yMin-origin.y)/Scale,top=center.Y+(rect.yMax-origin.y)/Scale;
            foreach(var patch in patches)
            {
                if(patch.Right<left||patch.Left>right||patch.Top<bottom||patch.Bottom>top)continue;
                int start=vh.currentVertCount;for(int i=0;i<patch.Points.Length;i++)vh.AddVert(Local(patch.Points[i]),patch.Tint,patch.UV[i]);
                for(int i=1;i<patch.Points.Length-1;i++)vh.AddTriangle(start,start+i,start+i+1);
            }
        }
    }
    // A restrained low-frequency colour layer holds the painted grass
    // together. Its coordinates belong to the world, so panning never slides
    // the material or changes the meadow's lighting under a resident.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TownMeadowGraphic:MaskableGraphic
    {
        private TownContinuousWorldView world;private TownPoint center;private float width=1.10f;
        public void Bind(TownContinuousWorldView source){world=source;raycastTarget=false;SetVerticesDirty();}
        public void BindPreview(TownPoint source,float sourceWidth){world=null;SetPreview(source,sourceWidth);raycastTarget=false;}
        public void SetPreview(TownPoint source,float sourceWidth){center=source;width=sourceWidth;SetVerticesDirty();}
        public void Refresh()=>SetVerticesDirty();
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;float scale=world!=null?world.RenderScale:r.width/width;if(scale<=0)return;
            var c=world!=null?world.Camera.Center:center;var origin=world!=null?world.Local(c,rectTransform):r.center;
            float left=c.X+(r.xMin-origin.x)/scale,right=c.X+(r.xMax-origin.x)/scale,bottom=c.Y+(r.yMin-origin.y)/scale,top=c.Y+(r.yMax-origin.y)/scale;
            const float cell=.24f;
            Vector2 Local(float x,float y)=>origin+new Vector2((x-c.X)*scale,(y-c.Y)*scale);
            Color Shade(float x,float y)
            {float light=Mathf.Sin(x*3.1f+y*.8f)*.017f+Mathf.Sin(y*2.7f-x*.6f)*.014f;return new Color(.76f+light,.82f+light,.49f+light*.75f,1);}
            for(int x=Mathf.FloorToInt(left/cell);x<Mathf.CeilToInt(right/cell);x++)for(int y=Mathf.FloorToInt(bottom/cell);y<Mathf.CeilToInt(top/cell);y++)
            {
                float l=x*cell,b=y*cell,t=b+cell,rr=l+cell;int start=vh.currentVertCount;
                vh.AddVert(Local(l,b),Shade(l,b),Vector2.zero);vh.AddVert(Local(l,t),Shade(l,t),Vector2.zero);vh.AddVert(Local(rr,t),Shade(rr,t),Vector2.zero);vh.AddVert(Local(rr,b),Shade(rr,b),Vector2.zero);
                vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
            }
        }
    }
}
