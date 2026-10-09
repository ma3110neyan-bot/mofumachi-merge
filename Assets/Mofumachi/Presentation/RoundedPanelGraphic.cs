using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // Standard UGUI material; corners are geometry, with no edits to character pixels.
    public sealed class RoundedPanelGraphic : Image
    {
        public float radius = 18;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = GetPixelAdjustedRect(); float corner = Mathf.Min(radius, Mathf.Min(r.width, r.height)/2);
            vh.AddVert(r.center, color, Vector2.zero);
            const int steps = 8; int n = 0;
            for (int c = 0; c < 4; c++)
            {
                var center = new Vector2(c == 0 || c == 3 ? r.xMax-corner : r.xMin+corner, c < 2 ? r.yMax-corner : r.yMin+corner);
                for (int k = 0; k <= steps; k++)
                {
                    float angle = (c*90 + k*90f/steps) * Mathf.Deg2Rad;
                    var point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle))*corner;
                    Color tint = Color.Lerp(color, Color.white, Mathf.InverseLerp(r.yMin,r.yMax,point.y)*.12f);
                    vh.AddVert(point, tint, Vector2.zero); n++;
                }
            }
            for (int i=1;i<=n;i++) vh.AddTriangle(0,i,i==n?1:i+1);
        }
    }
}
