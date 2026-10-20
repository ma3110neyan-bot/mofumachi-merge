using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // Retain the existing component name for screen/capture compatibility.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PerspectiveBoardGraphic : MaskableGraphic
    {
        public override Texture mainTexture=>Texture2D.whiteTexture;
        protected override void Awake(){base.Awake();raycastTarget=false;}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();if(r.width<=0||r.height<=0)return;
            float radius=Mathf.Min(12,Mathf.Min(r.width,r.height)*.045f);
            MergeCellSurfaceGraphic.Fill(vh,r,radius,new Color(.79f,.49f,.57f),new Color(.96f,.72f,.78f));
            var rim=new Rect(r.x+1.5f,r.y+1.5f,r.width-3,r.height-3);
            MergeCellSurfaceGraphic.Fill(vh,rim,radius-1,new Color(1,.89f,.71f),new Color(1,.96f,.87f));
            var paper=new Rect(r.x+3,r.y+3,r.width-6,r.height-6);
            MergeCellSurfaceGraphic.Fill(vh,paper,radius-2,new Color(1,.966f,.930f),new Color(1,.986f,.955f));
        }
    }
}
