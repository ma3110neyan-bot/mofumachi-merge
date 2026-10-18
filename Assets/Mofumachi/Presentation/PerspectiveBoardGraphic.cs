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
            MergeCellSurfaceGraphic.Fill(vh,r,radius,new Color(.46f,.31f,.20f,.19f),new Color(.46f,.31f,.20f,.08f));
            var rim=new Rect(r.x+1,r.y+2,r.width-2,r.height-2);
            MergeCellSurfaceGraphic.Fill(vh,rim,radius,new Color(.77f,.58f,.37f),new Color(.91f,.76f,.54f));
            var paper=new Rect(rim.x+2,rim.y+2,rim.width-4,rim.height-4);
            MergeCellSurfaceGraphic.Fill(vh,paper,radius-2,new Color(.98f,.94f,.84f),new Color(1,.985f,.94f));
        }
    }
}
