using UnityEngine;
using UnityEngine.UI;

namespace Mofumachi.Presentation
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RibbonGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = GetPixelAdjustedRect();
            if (r.width <= 0 || r.height <= 0) return;
            float tail = Mathf.Min(14, r.width * .08f);
            var gold = new Color(.94f, .73f, .36f, color.a);
            var dark = Color.Lerp(color, UIWidgets.Ink, .17f);
            Tail(mesh, r, tail, false, dark); Tail(mesh, r, tail, true, dark);
            float left = r.xMin + tail, right = r.xMax - tail;
            float radius = Mathf.Min(8, r.height * .26f);
            var band = new Rect(left, r.yMin, right - left, r.height);
            var shadow = band; shadow.y -= 2;
            Rounded(mesh, shadow, radius, new Color(.4f, .2f, .3f, .2f * color.a), new Color(.4f, .2f, .3f, .2f * color.a));
            Rounded(mesh, band, radius, gold, new Color(1, .97f, .78f, color.a));
            var inset = new Rect(left + 1.5f, r.yMin + 1.5f, band.width - 3, band.height - 3);
            Rounded(mesh, inset, radius - 1.5f, color, Color.Lerp(color, Color.white, .38f));
            Quad(mesh, new Vector2(left + 6, r.yMax - 4), new Vector2(left + 6, r.yMax - 3), new Vector2(right - 6, r.yMax - 3), new Vector2(right - 6, r.yMax - 4), new Color(1, 1, 1, .6f * color.a), new Color(1, 1, 1, .6f * color.a));
        }
        private static void Tail(VertexHelper mesh, Rect r, float tail, bool right, Color tint)
        {
            float sign = right ? -1 : 1, edge = right ? r.xMax : r.xMin;
            var points = new[] { new Vector2(edge, r.yMin + 2), new Vector2(edge + sign * tail * .5f, r.center.y),
                new Vector2(edge, r.yMax - 4), new Vector2(edge + sign * tail * 1.6f, r.yMax - 4), new Vector2(edge + sign * tail * 1.6f, r.yMin + 2) };
            // The notched polygon is triangulated from its inner edge.
            int start = mesh.currentVertCount;
            foreach (var p in points) mesh.AddVert(p, tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 4);
            mesh.AddTriangle(start + 1, start + 3, start + 4);
            mesh.AddTriangle(start + 1, start + 2, start + 3);
        }
        private static void Rounded(VertexHelper mesh, Rect r, float radius, Color bottom, Color top)
        {
            if (r.width <= 0 || r.height <= 0) return;
            radius = Mathf.Clamp(radius, 0, Mathf.Min(r.width, r.height) / 2);
            int start = mesh.currentVertCount;
            mesh.AddVert(r.center, Color.Lerp(bottom, top, .5f), Vector2.zero);
            const int steps = 5; int n = 0;
            for (int c = 0; c < 4; c++)
            {
                var center = new Vector2(c == 0 || c == 3 ? r.xMax - radius : r.xMin + radius, c < 2 ? r.yMax - radius : r.yMin + radius);
                for (int k = 0; k <= steps; k++)
                {
                    float angle = (c * 90 + k * 90f / steps) * Mathf.Deg2Rad;
                    var p = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    mesh.AddVert(p, Color.Lerp(bottom, top, Mathf.InverseLerp(r.yMin, r.yMax, p.y)), Vector2.zero); n++;
                }
            }
            for (int i = 1; i <= n; i++) mesh.AddTriangle(start, start + i, start + (i == n ? 1 : i + 1));
        }
        private static void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color bottom, Color top)
        {
            int i = mesh.currentVertCount;
            mesh.AddVert(a, bottom, Vector2.zero); mesh.AddVert(b, top, Vector2.zero);
            mesh.AddVert(c, top, Vector2.zero); mesh.AddVert(d, bottom, Vector2.zero);
            mesh.AddTriangle(i, i + 1, i + 2); mesh.AddTriangle(i, i + 2, i + 3);
        }
    }
}
