using UnityEngine;
using UnityEngine.UI;

namespace Mofumachi.Presentation
{
    // Eight shared-texture quads; the transparent centre is never drawn.
    public sealed class DecorativeFrameGraphic : MaskableGraphic
    {
        public float cornerSize = 36;
        public bool showCorners = true;
        private Texture2D frame;
        public override Texture mainTexture => frame != null ? frame : base.mainTexture;
        protected override void OnEnable()
        {
            frame = Resources.Load<Texture2D>("Mofumachi/UIPanelFrame");
            raycastTarget = false;
            base.OnEnable();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (frame == null) return;
            Rect r = GetPixelAdjustedRect();
            float size = Mathf.Max(0, Mathf.Min(cornerSize, Mathf.Min(r.width, r.height) * .49f));
            if (size <= 0) return;
            float[] x = { r.xMin, r.xMin + size, r.xMax - size, r.xMax };
            float[] y = { r.yMin, r.yMin + size, r.yMax - size, r.yMax };
            // Normalized source borders also work after Android downsampling.
            float[] uv = { 0, .30f, .70f, 1 };
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 3; col++)
                {
                    if (row == 1 && col == 1) continue;
                    if (!showCorners && row != 1 && col != 1) continue;
                    int first = mesh.currentVertCount;
                    mesh.AddVert(new Vector2(x[col], y[row]), color, new Vector2(uv[col], uv[row]));
                    mesh.AddVert(new Vector2(x[col], y[row + 1]), color, new Vector2(uv[col], uv[row + 1]));
                    mesh.AddVert(new Vector2(x[col + 1], y[row + 1]), color, new Vector2(uv[col + 1], uv[row + 1]));
                    mesh.AddVert(new Vector2(x[col + 1], y[row]), color, new Vector2(uv[col + 1], uv[row]));
                    mesh.AddTriangle(first, first + 1, first + 2);
                    mesh.AddTriangle(first, first + 2, first + 3);
                }
        }
    }
}
