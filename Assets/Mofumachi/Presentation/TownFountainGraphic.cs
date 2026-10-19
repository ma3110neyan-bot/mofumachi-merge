using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // Reuses exact pixels from the opaque map as a small foreground silhouette.
    // Residents above its feet are rendered behind the fountain; no new texture.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TownFountainGraphic:MaskableGraphic
    {
        private TownContinuousWorldView world;private Texture2D painting;
        private static readonly Vector2[] Outline={new Vector2(.514f,.355f),new Vector2(.529f,.360f),new Vector2(.531f,.385f),new Vector2(.548f,.395f),new Vector2(.558f,.425f),new Vector2(.591f,.432f),new Vector2(.611f,.449f),new Vector2(.627f,.475f),new Vector2(.622f,.514f),new Vector2(.606f,.534f),new Vector2(.570f,.547f),new Vector2(.520f,.557f),new Vector2(.465f,.548f),new Vector2(.421f,.530f),new Vector2(.402f,.503f),new Vector2(.400f,.476f),new Vector2(.416f,.449f),new Vector2(.451f,.432f),new Vector2(.480f,.416f),new Vector2(.485f,.394f),new Vector2(.507f,.380f)};
        public TownPoint Feet=>TownWorldMap.Point(.52f,.565f);
        public override Texture mainTexture=>painting;
        public void Bind(TownContinuousWorldView source){world=source;painting=Resources.Load<Texture2D>(TownWorldMap.Resource);raycastTarget=false;SetAllDirty();}
        public void Refresh()=>SetVerticesDirty();
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(world?.IsReady!=true||painting==null)return;
            void Vertex(Vector2 uv)=>vh.AddVert(world.Local(TownWorldMap.Point(uv.x,uv.y),rectTransform),Color.white,new Vector2(uv.x,1-uv.y));
            Vertex(new Vector2(.52f,.48f));foreach(var p in Outline)Vertex(p);
            for(int i=0;i<Outline.Length;i++)vh.AddTriangle(0,i+1,(i+1)%Outline.Length+1);
        }
    }
}
