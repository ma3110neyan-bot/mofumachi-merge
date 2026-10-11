using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public enum TownDecorationKind { WelcomeRibbon,FlowerArch,CelebrationGarland }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TownDecorationGraphic:MaskableGraphic
    {
        private static Texture2D atlas;
        public TownDecorationKind Kind { get; private set; }
        public override Texture mainTexture=>atlas!=null?atlas:atlas=Resources.Load<Texture2D>("Mofumachi/TownDecorationAtlas");
        private static readonly Rect[] Pixels={new Rect(48,190,622,358),new Rect(710,48,704,614),new Rect(1418,188,754,342)};
        public static Rect Region(TownDecorationKind kind)
        {var p=Pixels[(int)kind];return new Rect(p.x/2172f,1-p.yMax/724f,p.width/2172f,p.height/724f);}
        public static float Aspect(TownDecorationKind kind){var p=Pixels[(int)kind];return p.width/p.height;}
        public void Bind(TownDecorationKind kind){Kind=kind;raycastTarget=false;SetAllDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(mainTexture==null)return;var rect=GetPixelAdjustedRect();var uv=Region(Kind);
            vh.AddVert(new Vector3(rect.xMin,rect.yMin),color,uv.min);vh.AddVert(new Vector3(rect.xMin,rect.yMax),color,new Vector2(uv.xMin,uv.yMax));
            vh.AddVert(new Vector3(rect.xMax,rect.yMax),color,uv.max);vh.AddVert(new Vector3(rect.xMax,rect.yMin),color,new Vector2(uv.xMax,uv.yMin));
            vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,3);
        }
    }
}
