using System;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    [Serializable] public sealed class NoahAuthoredPart
    {public string id;public float width,height,scale;public Vector2 pivot;public Rect uv;}
    [Serializable] public sealed class NoahAuthoredView
    {public string direction;public NoahAuthoredPart[] parts;}
    [Serializable] public sealed class NoahAuthoredRigConfig
    {public int version=1;public string input_sha256;public NoahAuthoredView[] views;}

    // A single shared atlas and32vertices. It stays inside the existing actor's
    // hierarchy/masks/Sorting; no world position or game state is owned here.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class NoahAuthoredGraphic : MaskableGraphic
    {
        private static Texture2D texture;
        private static NoahAuthoredRigConfig config;
        private NoahAuthoredView view;private NoahAuthoredPose pose;
        public string DirectionName=>view?.direction??"unconfigured";
        public override Texture mainTexture=>texture;
        public static string InputDigest=>config?.input_sha256;
        public void Configure()
        {
            if(texture==null||config==null)
            {
                texture=Resources.Load<Texture2D>("Mofumachi/NoahAuthoredTrial/atlas");
                var text=Resources.Load<TextAsset>("Mofumachi/NoahAuthoredTrial/layout");
                if(texture==null||text==null)throw new InvalidOperationException("Noah authored trial resources missing. Use its preparation/build route.");
                config=JsonUtility.FromJson<NoahAuthoredRigConfig>(text.text);
                if(config==null||config.version!=1||config.views==null||config.views.Length!=3||string.IsNullOrEmpty(config.input_sha256))throw new InvalidOperationException("Invalid Noah trial layout.");
                foreach(var v in config.views)
                {
                    if(v.parts==null||v.parts.Length!=8)throw new InvalidOperationException("Eight independent Noah parts required.");
                    foreach(var p in v.parts)
                    {
                        default(NoahAuthoredPose).Part(p.id);
                        if(!float.IsFinite(p.width)||!float.IsFinite(p.height)||!float.IsFinite(p.scale)||p.width<=0||p.height<=0||p.scale<=0||p.uv.width<=0||p.uv.height<=0)throw new InvalidOperationException("Invalid Noah part registration.");
                    }
                }
            }
            SetPose(0,0,0,NoahAuthoredMode.Idle,NoahAuthoredDirection.Left);SetAllDirty();
        }
        public void SetPose(float seconds,float phase,float walking,NoahAuthoredMode mode,NoahAuthoredDirection direction)
        {
            if(config==null)throw new InvalidOperationException("Configure Noah parts first.");
            string name=direction==NoahAuthoredDirection.Back?"back":direction==NoahAuthoredDirection.Right?"right":"left";
            view=null;foreach(var candidate in config.views)if(candidate.direction==name){view=candidate;break;}
            if(view==null)throw new InvalidOperationException("Noah direction missing: "+name);
            pose=NoahAuthoredMotion.Sample(seconds,phase,walking,mode,direction);SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(view==null||texture==null)return;
            var r=GetPixelAdjustedRect();float height=Mathf.Min(r.width/254f,r.height/323f)*323f,s=height/600f;
            foreach(var p in view.parts)
            {
                var t=pose.Part(p.id);int n=vh.currentVertCount;
                void Vertex(float x,float y,float u,float v)
                {
                    t.Point((x-p.pivot.x*p.width)*p.scale,(y-p.pivot.y*p.height)*p.scale,out float px,out float py);
                    vh.AddVert(new Vector3(r.center.x+px*s,r.yMin-py*s),new Color(1,1,1,color.a),new Vector2(u,v));
                }
                Vertex(0,p.height,p.uv.xMin,p.uv.yMin);Vertex(0,0,p.uv.xMin,p.uv.yMax);
                Vertex(p.width,0,p.uv.xMax,p.uv.yMax);Vertex(p.width,p.height,p.uv.xMax,p.uv.yMin);
                vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
            }
        }
    }
}
