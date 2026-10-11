using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public enum CharacterExpression { Normal, Smile, Surprise }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CharacterBodyGraphic : MaskableGraphic
    {
        private static Texture2D atlas;private Rect pixels;
        public string CharacterId { get; private set; }
        public CharacterExpression Expression { get; private set; }
        public override Texture mainTexture=>atlas!=null?atlas:atlas=Resources.Load<Texture2D>("Mofumachi/CharacterBodyAtlas");
        // Atlas coordinates measured at 1536 x 1024; no image/text cropping is done at runtime.
        private static readonly Rect[] Bounds={
            new Rect(17,12,266,324),new Rect(284,12,230,324),new Rect(524,20,221,316),new Rect(771,5,237,331),new Rect(1005,31,243,306),new Rect(1263,14,254,323),
            new Rect(17,345,266,324),new Rect(283,342,231,327),new Rect(524,353,221,316),new Rect(771,338,237,331),new Rect(1005,363,243,307),new Rect(1263,346,254,325),
            new Rect(17,676,266,325),new Rect(283,674,231,327),new Rect(524,685,221,315),new Rect(772,670,236,331),new Rect(1006,695,242,307),new Rect(1263,679,254,324)};
        public void Bind(string characterId)=>Bind(characterId,CharacterExpression.Normal);
        public void Bind(string characterId,CharacterExpression expression)
        {
            var definition=CharacterCatalog.Find(characterId);CharacterId=characterId;Expression=expression;
            pixels=definition==null?Rect.zero:Bounds[(int)expression*6+definition.PortraitIndex];SetAllDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(mainTexture==null||pixels.width<=0)return;
            Rect target=GetPixelAdjustedRect();float scale=Mathf.Min(target.width/pixels.width,target.height/pixels.height);
            Vector2 size=pixels.size*scale,min=new Vector2(target.center.x-size.x/2,target.yMin),max=min+size;
            var uv=new Rect(pixels.x/1536f,1-pixels.yMax/1024f,pixels.width/1536f,pixels.height/1024f);
            var tint=new Color(1,1,1,color.a);
            vh.AddVert(new Vector3(min.x,min.y),tint,uv.min);vh.AddVert(new Vector3(min.x,max.y),tint,new Vector2(uv.xMin,uv.yMax));
            vh.AddVert(new Vector3(max.x,max.y),tint,uv.max);vh.AddVert(new Vector3(max.x,min.y),tint,new Vector2(uv.xMax,uv.yMin));vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,3);
        }
    }
}
