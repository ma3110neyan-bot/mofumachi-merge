using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    [Serializable] public sealed class CharacterAuthoredPart
    {
        public string id;public float width,height,scale,rotation;public Vector2 pivot,offset;public Rect uv;
    }
    [Serializable] public sealed class CharacterAuthoredView
    {public string direction;public CharacterAuthoredPart[] parts;public CharacterAuthoredPart closedHead;}
    [Serializable] public sealed class CharacterAuthoredConfig
    {
        public int version=1;public string characterId,input_sha256;public float formalWidth,formalHeight;public CharacterAuthoredView[] views;
        public static bool IsPreparedCharacter(string id)=>id=="momo"||id=="lulu";
    }

    // The accepted Noah motion and rigid part technique, with separate material
    // registration. Never changes the master, world coordinates, input or Sorting.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CharacterAuthoredGraphic : MaskableGraphic
    {
        private Texture2D atlas;private CharacterAuthoredConfig config;
        private CharacterAuthoredView view;private NoahAuthoredPose pose;private bool eyesClosed;
        public string CharacterId=>config?.characterId;
        public string InputDigest=>config?.input_sha256;
        public NoahAuthoredMode CurrentMode{get;private set;}
        public bool EyesClosed=>eyesClosed;
        public override Texture mainTexture=>atlas;
        public static bool IsPreparedCharacter(string id)=>CharacterAuthoredConfig.IsPreparedCharacter(id);
        public void Configure(string id)
        {
            if(!IsPreparedCharacter(id))throw new InvalidOperationException("Character has not passed its sequential preparation gate: "+id);
            string resource="Mofumachi/CharacterAuthored/"+id;
            atlas=Resources.Load<Texture2D>(resource+"/atlas");var text=Resources.Load<TextAsset>(resource+"/layout");
            if(atlas==null||text==null)throw new InvalidOperationException("Prepare only this character before native review: "+id);
            config=JsonUtility.FromJson<CharacterAuthoredConfig>(text.text);Validate(config,id);
            SetPose(0,0,0,NoahAuthoredMode.Idle,NoahAuthoredDirection.Left,false);SetAllDirty();
        }
        private static void ValidatePart(CharacterAuthoredPart p)
        {
            if(p==null)throw new InvalidOperationException("Missing rigid part.");default(NoahAuthoredPose).Part(p.id);
            if(!float.IsFinite(p.width)||!float.IsFinite(p.height)||!float.IsFinite(p.scale)||!float.IsFinite(p.rotation)||p.width<=0||p.height<=0||p.scale<=0||p.uv.width<=0||p.uv.height<=0||!float.IsFinite(p.offset.x)||!float.IsFinite(p.offset.y))throw new InvalidOperationException("Invalid character part registration.");
        }
        private static void Validate(CharacterAuthoredConfig c,string id)
        {
            if(c==null||c.version!=1||c.characterId!=id||string.IsNullOrEmpty(c.input_sha256)||!float.IsFinite(c.formalWidth)||!float.IsFinite(c.formalHeight)||c.formalWidth<=0||c.formalHeight<=0||c.views==null||c.views.Length!=3)throw new InvalidOperationException("Invalid character-specific layout.");
            var names=new HashSet<string>();foreach(var v in c.views)
            {
                if(v==null||!names.Add(v.direction)||(v.direction!="left"&&v.direction!="right"&&v.direction!="back")||v.parts==null||v.parts.Length!=7)throw new InvalidOperationException("Three independent seven-part directions required.");
                var ids=new HashSet<string>();foreach(var p in v.parts){ValidatePart(p);if(!ids.Add(p.id))throw new InvalidOperationException("Duplicate part.");}
                foreach(var part in new[]{"head","body","farArm","nearArm","farLeg","nearLeg","tail"})if(!ids.Contains(part))throw new InvalidOperationException("Missing part: "+part);
                if(v.direction!="back"){ValidatePart(v.closedHead);if(v.closedHead.id!="head")throw new InvalidOperationException("Closed eyelids must retain the head bone.");}
            }
        }
        public void SetPose(float seconds,float phase,float walking,NoahAuthoredMode mode,NoahAuthoredDirection direction,bool blink)
        {
            if(config==null)throw new InvalidOperationException("Configure character first.");
            string name=direction==NoahAuthoredDirection.Back?"back":direction==NoahAuthoredDirection.Right?"right":"left";
            view=null;foreach(var v in config.views)if(v.direction==name){view=v;break;}
            if(view==null)throw new InvalidOperationException("Missing direction: "+name);
            // Direct reuse: no new motion model and no image reflection.
            pose=NoahAuthoredMotion.Sample(seconds,phase,walking,mode,direction);CurrentMode=mode;eyesClosed=blink;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(view==null||atlas==null)return;var r=GetPixelAdjustedRect();
            float height=Mathf.Min(r.width/config.formalWidth,r.height/config.formalHeight)*config.formalHeight,s=height/600f;
            foreach(var open in view.parts)
            {
                var p=open.id=="head"&&eyesClosed&&view.closedHead!=null?view.closedHead:open;
                var t=pose.Part(p.id).Child(p.offset.x,p.offset.y,p.rotation);int n=vh.currentVertCount;
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
