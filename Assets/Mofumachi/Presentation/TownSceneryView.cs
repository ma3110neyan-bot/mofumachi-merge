using System.Collections.Generic;
using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // Small independently culled sprites. No room is an opaque backdrop.
    public sealed class TownSceneryView:MonoBehaviour
    {
        public sealed class Piece {public RectTransform Rect;public TownSceneryPlacement Placement;}
        public IReadOnlyList<Piece> Pieces=>pieces;
        private readonly List<Piece> pieces=new List<Piece>();private TownContinuousWorldView world;private RectTransform field;private TownPoint previewCenter;private float previewWidth;private int townLevel;
        public void Bind(TownContinuousWorldView source){world=source;field=(RectTransform)transform;Build();}
        public void BindPreview(TownPoint center,float width){previewCenter=center;previewWidth=width;field=(RectTransform)transform;Build();}
        public void SetPreview(TownPoint center,float width){previewCenter=center;previewWidth=width;Refresh();}
        public void SetTownLevel(int level){level=Mathf.Clamp(level,0,6);if(townLevel==level)return;townLevel=level;Refresh();}
        private void Build()
        {
            foreach(var p in TownSceneryLayout.Landmarks)if(p.Kind!=TownSceneryKind.Fountain)Add(p);
            foreach(var p in TownSceneryLayout.Nature)Add(p);
            foreach(var p in TownVillageDressing.All)Add(p);
            foreach(var p in TownCentralMasterLayout.Dressing)Add(p);
            foreach(var p in TownWestMasterLayout.Dressing)Add(p);
            foreach(var p in TownEastMasterLayout.Dressing)Add(p);
            foreach(var p in TownNorthMasterLayout.Dressing)Add(p);
            foreach(var p in TownSouthMasterLayout.Dressing)Add(p);Refresh();
        }
        private void Add(TownSceneryPlacement p)
        {
            var rect=UIWidgets.Node("Town scenery "+p.Kind+" "+pieces.Count,field,new Rect(.5f,.5f,0,0));rect.pivot=new Vector2(.5f,0);
            if(p.Kind>=TownSceneryKind.CottagePair)rect.gameObject.AddComponent<TownVillageDetailGraphic>().Bind(p.Kind);
            else rect.gameObject.AddComponent<TownLandmarkGraphic>().Bind(p.Kind);
            if(p.Kind==TownSceneryKind.QuestCottage)
            {
                // Lettering belongs to the original blank wooden plaque. It
                // travels with the cottage and never becomes another HUD card.
                var label=UIWidgets.Node("Cottage request plaque",rect,new Rect(.51f,.41f,.16f,.045f)).gameObject.AddComponent<Text>();
                label.font=Resources.Load<Font>("Mofumachi/UIFontStory");label.text="おねがい";label.color=new Color(.35f,.19f,.12f);
                label.alignment=TextAnchor.MiddleCenter;label.resizeTextForBestFit=true;label.resizeTextMinSize=5;label.resizeTextMaxSize=11;
                label.horizontalOverflow=HorizontalWrapMode.Overflow;label.verticalOverflow=VerticalWrapMode.Truncate;label.raycastTarget=false;
            }
            pieces.Add(new Piece{Rect=rect,Placement=p});
        }
        public void Refresh()
        {
            float scale=world!=null?world.RenderScale:field.rect.width/previewWidth;
            foreach(var entry in pieces)
            {
                var p=entry.Placement;var local=world!=null?world.Local(p.Feet,field):field.rect.center+new Vector2((p.Feet.X-previewCenter.X)*scale,(p.Feet.Y-previewCenter.Y)*scale);
                entry.Rect.anchoredPosition=local;entry.Rect.sizeDelta=new Vector2(p.Width*scale,p.Height*scale);
                var b=field.rect;bool visible=local.x+p.Width*scale/2>b.xMin&&local.x-p.Width*scale/2<b.xMax&&local.y+p.Height*scale>b.yMin&&local.y<b.yMax;
                entry.Rect.gameObject.SetActive(visible&&TownCentralMasterLayout.VisibleAtLevel(p,townLevel)&&TownWestMasterLayout.VisibleAtLevel(p,townLevel)&&TownEastMasterLayout.VisibleAtLevel(p,townLevel)&&TownNorthMasterLayout.VisibleAtLevel(p,townLevel)&&TownSouthMasterLayout.VisibleAtLevel(p,townLevel));
            }
            if(world==null){pieces.Sort((a,b)=>TownSouthMasterLayout.DepthY(b.Placement).CompareTo(TownSouthMasterLayout.DepthY(a.Placement)));foreach(var p in pieces)p.Rect.SetAsLastSibling();}
        }
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TownLandmarkGraphic:MaskableGraphic
    {
        private static Texture2D landmarks,nature;private TownSceneryKind kind;
        public override Texture mainTexture=>kind<=TownSceneryKind.Gazebo?(landmarks!=null?landmarks:landmarks=Resources.Load<Texture2D>(TownSceneryLayout.LandmarkResource)):(nature!=null?nature:nature=Resources.Load<Texture2D>(TownSceneryLayout.NatureResource));
        // Measured opaque bounds, with a small alpha gutter. Atlases stay shared.
        private static readonly Rect[] LandmarkBounds={new Rect(24,50,602,560),new Rect(665,28,579,610),new Rect(78,649,510,547),new Rect(678,653,519,517)};
        private static readonly Rect[] NatureBounds={new Rect(39,8,690,600),new Rect(831,3,682,622),new Rect(36,606,705,376),new Rect(782,628,742,384)};
        public void Bind(TownSceneryKind source){kind=source;raycastTarget=false;SetAllDirty();}
        public static Rect LandmarkUV(TownSceneryKind kind){var b=LandmarkBounds[(int)kind];return new Rect(b.x/1254f,1-b.yMax/1254f,b.width/1254f,b.height/1254f);}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(mainTexture==null)return;var r=GetPixelAdjustedRect();Rect uv;
            if(kind<=TownSceneryKind.Gazebo)uv=LandmarkUV(kind);
            else {int slot=(int)kind-(int)TownSceneryKind.Tree;var b=NatureBounds[slot];uv=new Rect(b.x/1536f,1-b.yMax/1024f,b.width/1536f,b.height/1024f);}
            vh.AddVert(new Vector2(r.xMin,r.yMin),color,uv.min);vh.AddVert(new Vector2(r.xMin,r.yMax),color,new Vector2(uv.xMin,uv.yMax));vh.AddVert(new Vector2(r.xMax,r.yMax),color,uv.max);vh.AddVert(new Vector2(r.xMax,r.yMin),color,new Vector2(uv.xMax,uv.yMin));vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,3);
        }
    }
}
