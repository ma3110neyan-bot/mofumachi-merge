using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public static class ReleaseTypography
    {
        private static Font accent,reading,readingBold;
        public static void ApplyTo(Transform root)
        {
            Refresh(root);
            if(root.GetComponent<ReleaseTypographyScope>()==null)root.gameObject.AddComponent<ReleaseTypographyScope>();
        }
        internal static void Refresh(Transform root)
        {foreach(var text in root.GetComponentsInChildren<Text>(true)){if(text.name=="Town bubble"||text.name=="Speaker name")ApplyConversation(text);else Apply(text);}}
        public static void Apply(Text text)
        {
            if(MergeTypography.TryApply(text))return;
            var role=ReleaseFontPolicy.For(text.name,text.fontSize,text.text);
            if(role==ReleaseFontRole.Unchanged)return;
            if(accent==null)accent=Resources.Load<Font>("Mofumachi/UIFontAccent");
            if(reading==null)reading=Resources.Load<Font>("Mofumachi/UIFontReading");
            if(readingBold==null)readingBold=Resources.Load<Font>("Mofumachi/UIFontReadingBold");
            var chosen=role==ReleaseFontRole.Accent?accent:role==ReleaseFontRole.ReadingBold?readingBold:reading;
            if(chosen==null)return;
            text.font=chosen;
            // Preserve the existing rectangle and font size. Long/compact UI
            // stays readable instead of shrinking a decorative face to fit.
            if(role==ReleaseFontRole.Accent && readingBold!=null && text.rectTransform.rect.height>0 && text.preferredHeight>text.rectTransform.rect.height+1)
                text.font=readingBold;
        }
        public static void ApplyConversation(Text text)
        {
            // Only runtime-created conversation text; no actor/motion changes.
            if(text.name!="Town bubble"&&text.name!="Speaker name")return;
            if(accent==null)accent=Resources.Load<Font>("Mofumachi/UIFontAccent");
            if(accent==null)return;
            int originalSize=text.fontSize;text.font=accent;text.fontSize=Mathf.Max(16,originalSize);
            if(text.rectTransform.rect.height>0 && text.preferredHeight>text.rectTransform.rect.height+1)
            {text.fontSize=originalSize;Apply(text);}
        }
    }
    // A single dirty pass after UGUI layout, also on viewport/safe-area resize.
    // No per-frame text traversal and no font-driven layout/position changes.
    public sealed class ReleaseTypographyScope:MonoBehaviour
    {
        private bool dirty=true;
        private void OnEnable(){dirty=true;Canvas.willRenderCanvases+=RefreshIfDirty;}
        private void OnDisable(){Canvas.willRenderCanvases-=RefreshIfDirty;}
        private void OnRectTransformDimensionsChange(){dirty=true;}
        private void RefreshIfDirty(){if(!dirty)return;dirty=false;ReleaseTypography.Refresh(transform);}
    }
}
