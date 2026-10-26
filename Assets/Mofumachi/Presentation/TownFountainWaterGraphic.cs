using Mofumachi.Core;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // A single low-opacity child of the fountain inherits its existing Y depth.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TownFountainWaterGraphic:MaskableGraphic
    {
        private static readonly ConditionalWeakTable<UIFlowController,TownFountainWaterClock> sessions=new ConditionalWeakTable<UIFlowController,TownFountainWaterClock>();
        private TownContinuousWorldView world;private TownFountainWaterClock clock=new TownFountainWaterClock();
        public float Phase=>clock.Phase;
        public float ShowElapsed=>clock.Elapsed;
        public int ShowStage=>TownFountainWaterLayout.ShowAt(clock.Elapsed).Stage;
        protected override void Awake(){base.Awake();raycastTarget=false;}
        public void Bind(TownContinuousWorldView source){world=source;var owner=source.GetComponentInParent<UIFlowController>();if(owner!=null)clock=sessions.GetValue(owner,_=>new TownFountainWaterClock());raycastTarget=false;SetAllDirty();}
        public void NotifyViewportChanged()=>SetVerticesDirty();
        private Vector2 Local(TownPoint uv)=>world.Local(TownFountainWaterLayout.Project(uv,TownFountainWaterLayout.DisplayScale),rectTransform);
        private bool Visible()
        {
            if(world?.IsReady!=true)return false;
            var low=Local(new TownPoint(0,0));var high=Local(new TownPoint(1,1.3f));var rect=rectTransform.rect;
            return low.x<rect.xMax&&high.x>rect.xMin&&low.y<rect.yMax&&high.y>rect.yMin;
        }
        private void Update()=>AdvanceWater(Time.unscaledDeltaTime);
        public void AdvanceWater(float elapsedSeconds)
        {
            if(!isActiveAndEnabled||world==null||!world.CanAnimateWater||!Visible()||!float.IsFinite(elapsedSeconds)||elapsedSeconds<=0)return;
            if(clock.Advance(elapsedSeconds,true))SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();if(!Visible())return;
            var origin=Local(new TownPoint(0,0));var xAxis=Local(new TownPoint(1,0))-origin;var yAxis=Local(new TownPoint(0,1))-origin;
            Vector2 Project(TownPoint p)=>origin+xAxis*p.X+yAxis*p.Y;
            var show=TownFountainWaterLayout.ShowAt(clock.Elapsed);
            for(int i=0;i<TownFountainWaterLayout.QuadCount;i++)
            {
                var q=TownFountainWaterLayout.Quad(i,clock.Phase,show.Strength,show.Glow);
                var tint=q.Kind==3?new Color(1,1,.90f,q.Alpha):q.Kind==0?new Color(.72f,.95f,1,q.Alpha):new Color(.88f,1,1,q.Alpha);
                tint*=color;int start=vertices.currentVertCount;
                vertices.AddVert(Project(q.A),tint,Vector2.zero);vertices.AddVert(Project(q.B),tint,Vector2.zero);
                vertices.AddVert(Project(q.C),tint,Vector2.zero);vertices.AddVert(Project(q.D),tint,Vector2.zero);
                vertices.AddTriangle(start,start+1,start+2);vertices.AddTriangle(start,start+2,start+3);
            }
        }
    }
}
