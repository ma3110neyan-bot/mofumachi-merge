// Geometry-only compatibility surface. This is not Unity and never claims to be.
using System;
using System.Collections.Generic;
using System.Text.Json;
namespace UnityEngine
{
    public class Texture{} public class Texture2D:Texture{} public class CanvasRenderer{}
    public sealed class TextAsset{public string text;public TextAsset(string value){text=value;}}
    public sealed class RequireComponent:Attribute{public RequireComponent(Type type){}}
    public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
    public struct Vector3{public float x,y,z;public Vector3(float x,float y):this(x,y,0){}public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}
    public struct Color{public float r,g,b,a;public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}}
    public struct Rect
    {
        public float x,y,width,height;public Rect(float x,float y,float width,float height){this.x=x;this.y=y;this.width=width;this.height=height;}
        public float xMin=>x;public float yMin=>y;public float xMax=>x+width;public float yMax=>y+height;
        public Vector2 center=>new Vector2(x+width/2,y+height/2);
    }
    public static class Mathf{public static float Min(float a,float b)=>Math.Min(a,b);}
    public static class Resources
    {
        private static readonly Dictionary<string,object> Values=new Dictionary<string,object>();
        public static void Add(string path,object value)=>Values[path]=value;
        public static T Load<T>(string path) where T:class=>Values.TryGetValue(path,out var value)?value as T:null;
    }
    public static class JsonUtility{public static T FromJson<T>(string text)=>JsonSerializer.Deserialize<T>(text,new JsonSerializerOptions{IncludeFields=true});}
}
namespace UnityEngine.UI
{
    using UnityEngine;
    public class MaskableGraphic
    {
        public static Rect Bounds=new Rect(0,0,254,323);
        public Color color=new Color(1,1,1,1);public virtual Texture mainTexture=>null;
        protected Rect GetPixelAdjustedRect()=>Bounds;protected void SetAllDirty(){}protected void SetVerticesDirty(){}
        protected virtual void OnPopulateMesh(VertexHelper v){}
        public VertexHelper Render(){var v=new VertexHelper();OnPopulateMesh(v);return v;}
    }
    public sealed class VertexHelper
    {
        public readonly List<Vector3> Positions=new List<Vector3>();public readonly List<Vector2> Uvs=new List<Vector2>();
        public int currentVertCount=>Positions.Count;public void Clear(){Positions.Clear();Uvs.Clear();}
        public void AddVert(Vector3 p,Color c,Vector2 uv){Positions.Add(p);Uvs.Add(uv);}
        public void AddTriangle(int a,int b,int c){}
    }
}
