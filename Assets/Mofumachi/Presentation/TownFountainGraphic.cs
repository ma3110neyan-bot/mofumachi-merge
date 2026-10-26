using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // An alpha-cut fountain, independent of the ground material. Residents above
    // its feet pass behind it without an opaque patch of the old meadow.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TownFountainGraphic:MaskableGraphic
    {
        private TownContinuousWorldView world;private Texture2D painting;private TownPoint previewCenter;private float previewWidth;
        public const string Resource="Mofumachi/TownCentralFountain";
        public TownPoint Feet=>TownFountainWaterLayout.Project(new TownPoint(.5f,0),TownFountainWaterLayout.DisplayScale);
        private TownPoint Location(Vector2 uv)=>TownFountainWaterLayout.Project(new TownPoint(uv.x,uv.y),TownFountainWaterLayout.DisplayScale);
        public override Texture mainTexture=>painting;
        public void Bind(TownContinuousWorldView source){world=source;painting=Resources.Load<Texture2D>(Resource);raycastTarget=false;SetAllDirty();}
        public void BindPreview(TownPoint center,float width){previewCenter=center;previewWidth=width;painting=Resources.Load<Texture2D>(Resource);raycastTarget=false;SetAllDirty();}
        public void SetPreview(TownPoint center,float width){previewCenter=center;previewWidth=width;SetVerticesDirty();}
        public void Refresh()=>SetVerticesDirty();
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if((world==null&&previewWidth<=0)||painting==null)return;
            void Vertex(Vector2 uv){var p=Location(uv);var at=world!=null?world.Local(p,rectTransform):rectTransform.rect.center+new Vector2((p.X-previewCenter.X)*rectTransform.rect.width/previewWidth,(p.Y-previewCenter.Y)*rectTransform.rect.width/previewWidth);vh.AddVert(at,Color.white,uv);}
            Vertex(Vector2.zero);Vertex(Vector2.up);Vertex(Vector2.one);Vertex(Vector2.right);
            vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,3);
        }
    }
}
