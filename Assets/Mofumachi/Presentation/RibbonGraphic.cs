using UnityEngine;
using UnityEngine.UI;

namespace Mofumachi.Presentation
{
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
            Quad(mesh, new Vector2(r.xMin, r.yMin + 3), new Vector2(r.xMin, r.yMax - 4),
                new Vector2(r.xMin + tail * 1.5f, r.yMax - 4), new Vector2(r.xMin + tail * 1.5f, r.yMin + 3), dark, color);
            Quad(mesh, new Vector2(r.xMax - tail * 1.5f, r.yMin + 3), new Vector2(r.xMax - tail * 1.5f, r.yMax - 4),
                new Vector2(r.xMax, r.yMax - 4), new Vector2(r.xMax, r.yMin + 3), dark, color);
            float left = r.xMin + tail, right = r.xMax - tail;
            Quad(mesh, new Vector2(left, r.yMin), new Vector2(left, r.yMax), new Vector2(right, r.yMax), new Vector2(right, r.yMin), gold, new Color(1, .94f, .73f, color.a));
            Quad(mesh, new Vector2(left + 1.5f, r.yMin + 1.5f), new Vector2(left + 1.5f, r.yMax - 1.5f), new Vector2(right - 1.5f, r.yMax - 1.5f), new Vector2(right - 1.5f, r.yMin + 1.5f), color, Color.Lerp(color, Color.white, .32f));
            Quad(mesh, new Vector2(left + 6, r.yMax - 4), new Vector2(left + 6, r.yMax - 3), new Vector2(right - 6, r.yMax - 3), new Vector2(right - 6, r.yMax - 4), new Color(1, 1, 1, .6f * color.a), new Color(1, 1, 1, .6f * color.a));
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
