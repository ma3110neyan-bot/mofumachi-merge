using UnityEngine;
namespace Mofumachi.Presentation
{
    public sealed class ResponsiveUILayout : MonoBehaviour
    {
        public RectTransform FullScreenRoot { get; private set; }
        public RectTransform SafeRoot { get; private set; }
        public RectTransform ContentRoot { get; private set; }
        public void Initialize(Canvas canvas)
        {
            FullScreenRoot = UIWidgets.Node("Full screen background", (RectTransform)canvas.transform, new Rect(0, 0, 1, 1));
            SafeRoot = UIWidgets.Node("Safe area", (RectTransform)canvas.transform, new Rect(0, 0, 1, 1));
            ContentRoot = UIWidgets.Node("Portrait viewport", SafeRoot, new Rect(0, 0, 1, 1));
        }
        public void ApplyViewport(Vector2 pixels, Rect safePixels)
        {
            if (pixels.x <= 0 || pixels.y <= 0) return;
            SafeRoot.anchorMin = safePixels.min / pixels; SafeRoot.anchorMax = safePixels.max / pixels;
            SafeRoot.offsetMin = SafeRoot.offsetMax = Vector2.zero;
            ContentRoot.anchorMin = Vector2.zero; ContentRoot.anchorMax = Vector2.one;
            if (pixels.x > pixels.y && safePixels.width > 0 && safePixels.height > 0)
            {
                float width = Mathf.Min(1, safePixels.height * 9 / (16 * safePixels.width));
                float height = Mathf.Min(1, safePixels.width * 16 / (9 * safePixels.height));
                ContentRoot.anchorMin = new Vector2((1-width)/2, (1-height)/2);
                ContentRoot.anchorMax = Vector2.one - ContentRoot.anchorMin;
            }
            ContentRoot.offsetMin = ContentRoot.offsetMax = Vector2.zero;
        }
        public RectTransform CreatePage(string name) => UIWidgets.Node(name, ContentRoot, new Rect(0, 0, 1, 1));
        public static float CellSize(Vector2 area, float gap = 6) => Mathf.Max(0, Mathf.Min((area.x - 4*gap)/5, (area.y - 5*gap)/6));
    }
}
