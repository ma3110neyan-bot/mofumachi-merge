using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public enum CharacterExpression { Normal, Smile, Surprise }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CharacterBodyGraphic : MaskableGraphic
    {
        private static Texture2D atlas;private Rect pixels;private int portrait=-1;
        public string CharacterId { get; private set; }
        public CharacterExpression Expression { get; private set; }
        private static readonly float[] rows={0,.12f,.26f,.40f,.52f,.65f,.80f,1};
        private float foot,wave,facing;private bool blink;
        private bool noahTownTrial;private Noah25DTrialPose noahPose;
        private static readonly float[] noahColumns={0,.06f,.16f,.18f,.30f,.37f,.42f,.47f,.53f,.625f,.65f,.70f,.84f,1};
        private static readonly float[] noahRows={0,.06f,.14f,.22f,.28f,.36f,.44f,.49f,.53f,.60f,.67f,.80f,1};
        internal Vector2 NoahArtSize
        {
            get{var r=GetPixelAdjustedRect();float scale=Mathf.Min(r.width/254f,r.height/323f);return new Vector2(254*scale,323*scale);}
        }
        internal void SetNoahTownTrial(bool enabled)
        {
            enabled=TownNoah25DTrial.IsEnabled(CharacterId,enabled);if(noahTownTrial==enabled)return;
            noahTownTrial=enabled;pixels=portrait<0?Rect.zero:Bounds[(noahTownTrial?0:(int)Expression)*6+portrait];SetAllDirty();
        }
        internal void PoseNoah(Noah25DTrialPose pose,bool eyesClosed)
        {
            if(noahPose.Hop==pose.Hop&&noahPose.Lean==pose.Lean&&noahPose.Foot==pose.Foot&&noahPose.Breath==pose.Breath&&
                noahPose.Hand==pose.Hand&&noahPose.Look==pose.Look&&noahPose.Travel==pose.Travel&&blink==eyesClosed)return;
            noahPose=pose;blink=eyesClosed;SetVerticesDirty();
        }
        public void Pose(float stepping,float greeting,float direction,bool eyesClosed)
        {
            if(noahTownTrial){PoseNoah(default,eyesClosed);return;}
            stepping=float.IsFinite(stepping)?Mathf.Clamp(stepping,-1,1):0;
            greeting=float.IsFinite(greeting)?Mathf.Clamp(greeting,-1,1):0;
            direction=float.IsFinite(direction)?Mathf.Clamp(direction,-1,1):0;
            if(foot==stepping&&wave==greeting&&facing==direction&&blink==eyesClosed)return;
            foot=stepping;wave=greeting;facing=direction;blink=eyesClosed;SetVerticesDirty();
        }
        public override Texture mainTexture=>atlas!=null?atlas:atlas=Resources.Load<Texture2D>("Mofumachi/CharacterBodyAtlas");
        // Atlas coordinates measured at 1536 x 1024; no image/text cropping is done at runtime.
        private static readonly Rect[] Bounds={
            new Rect(17,12,266,324),new Rect(284,12,230,324),new Rect(524,20,221,316),new Rect(771,5,237,331),new Rect(1005,31,243,306),new Rect(1263,14,254,323),
            new Rect(17,345,266,324),new Rect(283,342,231,327),new Rect(524,353,221,316),new Rect(771,338,237,331),new Rect(1005,363,243,307),new Rect(1263,346,254,325),
            new Rect(17,676,266,325),new Rect(283,674,231,327),new Rect(524,685,221,315),new Rect(772,670,236,331),new Rect(1006,695,242,307),new Rect(1263,679,254,324)};
        public void Bind(string characterId)=>Bind(characterId,CharacterExpression.Normal);
        public void Bind(string characterId,CharacterExpression expression)
        {
            var definition=CharacterCatalog.Find(characterId);CharacterId=characterId;Expression=expression;portrait=definition?.PortraitIndex??-1;
            noahTownTrial=TownNoah25DTrial.IsEnabled(characterId,noahTownTrial);
            pixels=portrait<0?Rect.zero:Bounds[(noahTownTrial?0:(int)expression)*6+portrait];SetAllDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(mainTexture==null||pixels.width<=0)return;
            if(noahTownTrial){PopulateNoah(vh);return;}
            Rect target=GetPixelAdjustedRect();float scale=Mathf.Min(target.width/pixels.width,target.height/pixels.height);
            Vector2 size=pixels.size*scale,min=new Vector2(target.center.x-size.x/2,target.yMin),max=min+size;
            var uv=new Rect(pixels.x/1536f,1-pixels.yMax/1024f,pixels.width/1536f,pixels.height/1024f);
            var tint=new Color(1,1,1,color.a);
            // Deform the same formal atlas very slightly at feet/hands. The head
            // keeps its original silhouette; blink uses only the approved smiling
            // eye strip, rather than painting a different face.
            const int columns=8;
            var closed=Bounds[6+portrait];
            var closedUv=new Rect(closed.x/1536f,1-closed.yMax/1024f,closed.width/1536f,closed.height/1024f);
            for(int y=0;y<rows.Length-1;y++)for(int x=0;x<columns;x++)
            {
                float left=x/(float)columns,right=(x+1)/(float)columns,bottom=rows[y],top=rows[y+1];
                var source=blink&&Expression==CharacterExpression.Normal&&bottom>=.52f&&top<=.65f?closedUv:uv;
                int n=vh.currentVertCount;
                void Vertex(float u,float v)
                {
                    var offset=TownCharacterMotion.VertexOffset(u,v,foot,wave,facing);
                    vh.AddVert(new Vector3(min.x+size.x*u+offset.X,min.y+size.y*v+offset.Y),tint,new Vector2(source.x+u*source.width,source.y+v*source.height));
                }
                Vertex(left,bottom);Vertex(left,top);Vertex(right,top);Vertex(right,bottom);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
            }
        }
        private void PopulateNoah(VertexHelper vh)
        {
            Rect target=GetPixelAdjustedRect();Vector2 size=NoahArtSize;
            // Register the actual two-paw midpoint to the navigation/shadow
            // anchor. Keep the asymmetric original tail, coat and face intact.
            Vector2 min=new Vector2(target.center.x-size.x*TownNoah25DTrial.FootCenterU,target.yMin);
            var uv=new Rect(1263f/1536f,1-337f/1024f,254f/1536f,323f/1024f);
            var tint=new Color(1,1,1,color.a);
            for(int y=0;y<noahRows.Length-1;y++)for(int x=0;x<noahColumns.Length-1;x++)
            {
                float left=noahColumns[x],right=noahColumns[x+1],bottom=noahRows[y],top=noahRows[y+1];
                var eyes=blink?TownNoah25DTrial.BlinkUvOffset((left+right)*.5f,(bottom+top)*.5f):default;
                int n=vh.currentVertCount;
                void Vertex(float u,float v)
                {
                    var offset=TownNoah25DTrial.VertexOffset(u,v,noahPose);
                    vh.AddVert(new Vector3(min.x+size.x*u+offset.X,min.y+size.y*v+offset.Y),tint,
                        new Vector2(uv.x+u*uv.width+eyes.X,uv.y+v*uv.height+eyes.Y));
                }
                Vertex(left,bottom);Vertex(left,top);Vertex(right,top);Vertex(right,bottom);
                vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
            }
        }
    }
}
