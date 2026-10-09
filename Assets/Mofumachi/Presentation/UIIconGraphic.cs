using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public enum UIIcon { Home, Settings, Mail, Tea, Star, Coin, Check }
    public sealed class UIIconGraphic : MaskableGraphic
    {
        public UIIcon icon;
        protected override void OnPopulateMesh(VertexHelper v)
        {
            v.Clear();
            if (icon == UIIcon.Star || icon == UIIcon.Coin || icon == UIIcon.Settings)
            {
                int n = icon == UIIcon.Coin ? 24 : icon == UIIcon.Star ? 10 : 24;
                var r=GetPixelAdjustedRect(); v.AddVert(r.center,color,Vector2.zero);
                for(int i=0;i<n;i++) { float a=(90+i*360f/n)*Mathf.Deg2Rad; float s=icon==UIIcon.Star && i%2==1?.48f:icon==UIIcon.Settings && i%3==1?.72f:1;
                    v.AddVert(r.center+new Vector2(Mathf.Cos(a)*r.width,Mathf.Sin(a)*r.height)*.45f*s,color,Vector2.zero); }
                for(int i=1;i<=n;i++)v.AddTriangle(0,i,i==n?1:i+1);
                return;
            }
            if(icon==UIIcon.Home) { Quad(v,.2f,.1f,.8f,.58f); Triangle(v,new Vector2(.06f,.52f),new Vector2(.5f,.94f),new Vector2(.94f,.52f)); }
            else if(icon==UIIcon.Mail) { Quad(v,.08f,.15f,.92f,.8f); Triangle(v,new Vector2(.12f,.82f),new Vector2(.5f,.47f),new Vector2(.88f,.82f),new Color(1,.93f,.78f)); }
            else if(icon==UIIcon.Tea) { Quad(v,.17f,.2f,.72f,.68f); Quad(v,.7f,.34f,.9f,.58f); Quad(v,.1f,.1f,.88f,.17f); Quad(v,.36f,.76f,.42f,.95f); }
            else { Quad(v,.18f,.35f,.42f,.48f); Triangle(v,new Vector2(.32f,.32f),new Vector2(.45f,.2f),new Vector2(.91f,.86f)); }
        }
        private void Triangle(VertexHelper v,Vector2 a,Vector2 b,Vector2 c,Color? tint=null)
        { int n=v.currentVertCount; var r=GetPixelAdjustedRect(); foreach(var p in new[]{a,b,c})v.AddVert(r.min+Vector2.Scale(p,r.size),tint??color,Vector2.zero); v.AddTriangle(n,n+1,n+2); }
        private void Quad(VertexHelper v,float x,float y,float x2,float y2)
        { Triangle(v,new Vector2(x,y),new Vector2(x2,y),new Vector2(x2,y2)); Triangle(v,new Vector2(x,y),new Vector2(x2,y2),new Vector2(x,y2)); }
    }
}
