using UnityEngine;
using UnityEngine.UI;

namespace Mofumachi.Presentation
{
    // Lightweight water droplets, ripples and glints over the existing flat painting.
    // All positions follow BackgroundCover's UV crop. No tree/character art is redrawn.
    public sealed class TownAmbientGraphic : MaskableGraphic
    {
        private RawImage background;
        private float phase, nextFrame;
        protected override void Awake() { base.Awake(); raycastTarget = false; }
        public void Bind(RawImage image) => background = image;
        private void Update()
        {
            if (Time.unscaledTime < nextFrame) return;
            nextFrame = Time.unscaledTime + 1f / 30;
            phase = Time.unscaledTime; SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear(); if (background == null || background.texture == null) return;
            for (int i = 0; i < 9; i++)
            {
                float t = Mathf.Repeat(phase * .62f + i * .113f, 1);
                var source = new Vector2(.405f + (i % 3 - 1) * .022f + (t - .5f) * .014f,
                    .592f - t * .047f);
                var tint = new Color(.85f, 1, 1, Mathf.Sin(t * Mathf.PI) * .7f);
                Ellipse(vertices, Local(source), new Vector2(.0018f, .0029f), tint);
            }
            for (int i = 0; i < 3; i++)
            {
                float t = Mathf.Repeat(phase * .33f + i / 3f, 1);
                var tint = new Color(.85f, 1, 1, (1 - t) * .28f);
                Ring(vertices, Local(new Vector2(.41f, .557f)), new Vector2(.02f + t * .065f, .003f + t * .008f), tint);
            }
            for (int i = 0; i < 5; i++)
            {
                float glow = Mathf.Max(0, Mathf.Sin(phase * 1.4f + i * 1.7f));
                var p = Local(new Vector2(.13f + i * .18f, .79f - (i % 3) * .17f));
                Ellipse(vertices, p, new Vector2(.0022f, .005f) * glow, new Color(1, 1, .86f, .5f * glow));
                Ellipse(vertices, p, new Vector2(.006f, .0014f) * glow, new Color(1, 1, .86f, .5f * glow));
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
                vertices.AddVert(center + Vector2.Scale(direction, size * .94f), tint, Vector2.zero);
            }
            for (int i = 0; i < 24; i++)
            { int n = start + i * 2, next = start + (i + 1) % 24 * 2; vertices.AddTriangle(n, next, n + 1); vertices.AddTriangle(n + 1, next, next + 1); }
        }
    }
}
