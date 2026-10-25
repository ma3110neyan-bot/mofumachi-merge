using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // A single low-opacity child of the fountain inherits its existing Y depth.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TownFountainWaterGraphic:MaskableGraphic
    {
        private TownContinuousWorldView world;private float phase,meshElapsed;
        public float Phase=>phase;
        protected override void Awake(){base.Awake();raycastTarget=false;}
        public void Bind(TownContinuousWorldView source){world=source;raycastTarget=false;SetAllDirty();}
        public void NotifyViewportChanged()=>SetVerticesDirty();
        private Vector2 Local(TownPoint uv)=>world.Local(new TownPoint((uv.X-.5f)*TownCentralMasterLayout.FountainWidth,TownCentralMasterLayout.FountainFeetY+uv.Y*TownSceneryLayout.FountainHeight),rectTransform);
        private bool Visible()
        {
            if(world?.IsReady!=true)return false;
            var low=Local(new TownPoint(0,0));var high=Local(new TownPoint(1,1));var rect=rectTransform.rect;
            return low.x<rect.xMax&&high.x>rect.xMin&&low.y<rect.yMax&&high.y>rect.yMin;
        }
        private void Update()=>AdvanceWater(Time.unscaledDeltaTime);
        public void AdvanceWater(float elapsedSeconds)
        {
            if(!isActiveAndEnabled||world==null||!world.CanAnimateWater||!Visible()||!float.IsFinite(elapsedSeconds)||elapsedSeconds<=0)return;
            float dt=Mathf.Min(elapsedSeconds,.1f);meshElapsed+=dt;
            if(meshElapsed<TownFountainWaterLayout.FrameInterval)return;
            float remainder=Mathf.Repeat(meshElapsed,TownFountainWaterLayout.FrameInterval);
            phase=Mathf.Repeat(phase+meshElapsed-remainder,TownFountainWaterLayout.LoopSeconds);meshElapsed=remainder;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();if(!Visible())return;
            var origin=Local(new TownPoint(0,0));var xAxis=Local(new TownPoint(1,0))-origin;var yAxis=Local(new TownPoint(0,1))-origin;
            Vector2 Project(TownPoint p)=>origin+xAxis*p.X+yAxis*p.Y;
            for(int i=0;i<TownFountainWaterLayout.QuadCount;i++)
            {
                var q=TownFountainWaterLayout.Quad(i,phase);
                var tint=q.Kind==3?new Color(1,1,.90f,q.Alpha):q.Kind==0?new Color(.72f,.95f,1,q.Alpha):new Color(.88f,1,1,q.Alpha);
                tint*=color;int start=vertices.currentVertCount;
                vertices.AddVert(Project(q.A),tint,Vector2.zero);vertices.AddVert(Project(q.B),tint,Vector2.zero);
                vertices.AddVert(Project(q.C),tint,Vector2.zero);vertices.AddVert(Project(q.D),tint,Vector2.zero);
                vertices.AddTriangle(start,start+1,start+2);vertices.AddTriangle(start,start+2,start+3);
            }
        }
    }
}
