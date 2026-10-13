using UnityEngine;
using UnityEngine.UI;

namespace Mofumachi.Presentation
{
    // Lightweight flowing water, droplets, ripples and glints over the painting.
    // All positions follow BackgroundCover's UV crop. No tree/character art is redrawn.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TownAmbientGraphic : MaskableGraphic
    {
        private RawImage background;
        private float phase,meshElapsed;
        public float Phase=>phase;
        protected override void Awake() { base.Awake(); raycastTarget = false; }
        public void Bind(RawImage image) => background = image;
        public void NotifyViewportChanged()=>SetVerticesDirty();
        private void Update()=>AdvanceWater(Time.unscaledDeltaTime);
        public void AdvanceWater(float elapsedSeconds)
        {
            if(!isActiveAndEnabled||!float.IsFinite(elapsedSeconds)||elapsedSeconds<=0)return;
            float dt=Mathf.Min(elapsedSeconds,.1f);phase+=dt;meshElapsed+=dt;
            if(meshElapsed<1f/30)return;
            meshElapsed=Mathf.Repeat(meshElapsed,1f/30);SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear(); if (background == null || background.texture == null) return;
            // The rear waterfall and the two fountain bowls have separate falls.
            // Coordinates are in the source painting, so safe-area/aspect changes
            // cannot shift the water away from the original fountain.
            Fall(vertices, .435f, .48f, .754f, .710f, 7, .47f);
            Fall(vertices, .377f, .444f, .622f, .586f, 8, .8f);
            Fall(vertices, .357f, .462f, .586f, .548f, 9, .75f);
            Jet(vertices,new Vector2(.405f,.622f),new Vector2(.32f,.66f),new Vector2(.346f,.552f));
            Jet(vertices,new Vector2(.415f,.622f),new Vector2(.50f,.66f),new Vector2(.476f,.552f));
            for (int i = 0; i < 4; i++)
            {
                float t = Mathf.Repeat(phase * .43f + i / 4f, 1);
                var tint = new Color(.87f, 1, 1, (1 - t) * .7f);
                Ring(vertices, Local(new Vector2(.41f, .548f)), new Vector2(.013f + t * .077f, .0025f + t * .0105f), tint);
            }
            for (int i = 0; i < 5; i++)
            {
                float glow = Mathf.Max(0, Mathf.Sin(phase * 1.4f + i * 1.7f));
                var p = Local(new Vector2(.13f + i * .18f, .79f - (i % 3) * .17f));
                Ellipse(vertices, p, new Vector2(.0022f, .005f) * glow, new Color(1, 1, .86f, .5f * glow));
                Ellipse(vertices, p, new Vector2(.006f, .0014f) * glow, new Color(1, 1, .86f, .5f * glow));
            }
        }
        private void Jet(VertexHelper vertices,Vector2 start,Vector2 control,Vector2 end)
        {
            const int steps=12;
            for(int i=0;i<steps;i++)
            {
                float t=i/(float)steps,u=(i+1f)/steps;
                var a=(1-t)*(1-t)*start+2*(1-t)*t*control+t*t*end;
                var b=(1-u)*(1-u)*start+2*(1-u)*u*control+u*u*end;
                float glow=.48f+.22f*Mathf.Sin(phase*6-i*.65f);
                var tint=new Color(.84f,.98f,1,glow);int n=vertices.currentVertCount;
                var width=new Vector2(.0016f,0);
                vertices.AddVert(Local(a-width),tint,Vector2.zero);vertices.AddVert(Local(b-width),tint,Vector2.zero);
                vertices.AddVert(Local(b+width),tint,Vector2.zero);vertices.AddVert(Local(a+width),tint,Vector2.zero);
                vertices.AddTriangle(n,n+1,n+2);vertices.AddTriangle(n,n+2,n+3);
            }
            float drop=Mathf.Repeat(phase*.7f,1);var p=(1-drop)*(1-drop)*start+2*(1-drop)*drop*control+drop*drop*end;
            Ellipse(vertices,Local(p),new Vector2(.003f,.004f),new Color(.93f,1,1,.9f));
        }
        private void Fall(VertexHelper vertices, float left, float right, float top, float bottom, int count, float opacity)
        {
            for (int i = 0; i < count; i++)
            {
                float t = Mathf.Repeat(phase * .92f + i * .137f, 1);
                float x = Mathf.Lerp(left, right, (i + .5f) / count);
                float y = Mathf.Lerp(top, bottom, t);
                float alpha = Mathf.Sin(t * Mathf.PI) * opacity;
                float halfWidth = .002f + (i % 3) * .0004f;
                float length = (top - bottom) * .18f;
                var tail = new Color(.55f, .91f, 1, alpha * .4f);
                var head = new Color(.9f, 1, 1, alpha);
                int start = vertices.currentVertCount;
                vertices.AddVert(Local(new Vector2(x - halfWidth, y)), head, Vector2.zero);
                vertices.AddVert(Local(new Vector2(x - halfWidth, Mathf.Min(top, y + length))), tail, Vector2.zero);
                vertices.AddVert(Local(new Vector2(x + halfWidth, Mathf.Min(top, y + length))), tail, Vector2.zero);
                vertices.AddVert(Local(new Vector2(x + halfWidth, y)), head, Vector2.zero);
                vertices.AddTriangle(start, start + 1, start + 2); vertices.AddTriangle(start, start + 2, start + 3);
                Ellipse(vertices, Local(new Vector2(x, y)), new Vector2(.0025f, .0037f), head);
            }
        }
        private Vector2 Local(Vector2 source)
        {
            var uv = background.uvRect; var rect = background.rectTransform.rect;
            var point = rect.min + Vector2.Scale((source - uv.min) / uv.size, rect.size);
            return rectTransform.InverseTransformPoint(background.rectTransform.TransformPoint(point));
        }
        private Vector2 Radius(Vector2 sourceSize)
        {
            var center = Local(new Vector2(.5f, .5f));
            return Local(new Vector2(.5f, .5f) + sourceSize) - center;
        }
        private void Ellipse(VertexHelper vertices, Vector2 center, Vector2 sourceSize, Color tint)
        {
            var size = Radius(sourceSize); int start = vertices.currentVertCount;
            vertices.AddVert(center, tint, Vector2.zero);
            for (int i = 0; i < 12; i++)
            { float a = i * Mathf.PI / 6; vertices.AddVert(center + Vector2.Scale(new Vector2(Mathf.Cos(a), Mathf.Sin(a)), size), tint, Vector2.zero); }
            for (int i = 0; i < 12; i++) vertices.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % 12);
        }
        private void Ring(VertexHelper vertices, Vector2 center, Vector2 sourceSize, Color tint)
        {
            var size = Radius(sourceSize); int start = vertices.currentVertCount;
            for (int i = 0; i < 24; i++)
            {
                float a = i * Mathf.PI / 12; var direction = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                vertices.AddVert(center + Vector2.Scale(direction, size), tint, Vector2.zero);
                vertices.AddVert(center + Vector2.Scale(direction, size * .91f), tint, Vector2.zero);
            }
            for (int i = 0; i < 24; i++)
            { int n = start + i * 2, next = start + (i + 1) % 24 * 2; vertices.AddTriangle(n, next, n + 1); vertices.AddTriangle(n + 1, next, next + 1); }
        }
    }
}
