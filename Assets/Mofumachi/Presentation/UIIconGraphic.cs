using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public enum UIIcon { Home, Settings, Mail, Tea, Star, Coin, Check, TeaPot, Flower }
    public sealed class UIIconGraphic : MaskableGraphic
    {
        public UIIcon icon;
        private static Texture2D atlas;
        public override Texture mainTexture => Atlas;
        private static Texture2D Atlas
        {
            get { if (atlas == null) atlas = Resources.Load<Texture2D>("Mofumachi/UIIconAtlas"); return atlas; }
        }
        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear(); if (Atlas == null) return;
            var source = SourcePixels(icon); var rect = GetPixelAdjustedRect();
            float scale = Mathf.Min(rect.width / source.width, rect.height / source.height);
            var size = source.size * scale; var min = rect.center - size / 2; var max = min + size;
            // Source bounds are measured from the atlas's top left. Preserve art aspect.
            // UVs use original dimensions even if a platform resizes the texture.
            const float sourceSize = 1254;
            var uv = new Rect(source.x / sourceSize, 1 - source.yMax / sourceSize,
                source.width / sourceSize, source.height / sourceSize);
            var tint = new Color(1, 1, 1, color.a);
            vertices.AddVert(new Vector2(min.x, min.y), tint, uv.min);
            vertices.AddVert(new Vector2(min.x, max.y), tint, new Vector2(uv.xMin, uv.yMax));
            vertices.AddVert(new Vector2(max.x, max.y), tint, uv.max);
            vertices.AddVert(new Vector2(max.x, min.y), tint, new Vector2(uv.xMax, uv.yMin));
            vertices.AddTriangle(0, 1, 2); vertices.AddTriangle(0, 2, 3);
        }
        private static Rect SourcePixels(UIIcon type)
        {
            switch (type)
            {
                case UIIcon.Home: return new Rect(14, 60, 425, 398);
                case UIIcon.Mail: return new Rect(445, 112, 395, 334);
                case UIIcon.Tea: return new Rect(843, 117, 404, 319);
                case UIIcon.TeaPot: return new Rect(17, 484, 428, 371);
                case UIIcon.Settings: return new Rect(446, 475, 393, 392);
                case UIIcon.Coin: return new Rect(850, 493, 387, 371);
                case UIIcon.Star: return new Rect(30, 866, 401, 372);
                case UIIcon.Check: return new Rect(447, 882, 391, 355);
                default: return new Rect(857, 889, 388, 348);
            }
        }
    }
}
