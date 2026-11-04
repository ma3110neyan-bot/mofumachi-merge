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
            ApplyHachi(text,role);
            if(role!=ReleaseFontRole.Accent&&text.GetComponent<HachiFontFit>()==null)text.gameObject.AddComponent<HachiFontFit>();
        }
        public static bool Fits(Text text)
        {
            var rect=text.rectTransform.rect;
            return rect.width<=0||rect.height<=0||text.preferredHeight<=rect.height+.5f&&(text.horizontalOverflow==HorizontalWrapMode.Wrap||text.preferredWidth<=rect.width+.5f);
        }
        internal static void ApplyHachi(Text text,ReleaseFontRole role)
        {
            if(role==ReleaseFontRole.Unchanged)return;
            if(accent==null)accent=Resources.Load<Font>("Mofumachi/UIFontAccent");
            if(accent==null)return;
            text.font=accent;text.fontStyle=FontStyle.Bold;
            string resource=ReleaseFontPolicy.Resource(role,Fits(text));
            if(resource=="UIFontAccent")return;
            if(reading==null)reading=Resources.Load<Font>("Mofumachi/UIFontReading");
            if(readingBold==null)readingBold=Resources.Load<Font>("Mofumachi/UIFontReadingBold");
            var fallback=resource=="UIFontReadingBold"?readingBold:reading;
            if(fallback!=null){text.font=fallback;text.fontStyle=FontStyle.Normal;}
        }
        public static void ApplyConversation(Text text)
        {
            // Only runtime-created conversation text; no actor/motion changes.
            if(text.name!="Town bubble"&&text.name!="Speaker name")return;
            if(accent==null)accent=Resources.Load<Font>("Mofumachi/UIFontAccent");
            if(accent==null)return;
            // Retain the existing conversation-size rule, including its local overflow rollback.
            int originalSize=text.fontSize;text.font=accent;text.fontStyle=FontStyle.Bold;text.fontSize=Mathf.Max(16,originalSize);
            if(!Fits(text))text.fontSize=originalSize;
            Apply(text);
        }
    }
    public sealed class HachiFontFit:MonoBehaviour
    {
        private Text text;private bool dirty=true,applying;
        private void OnEnable(){text=GetComponent<Text>();text.RegisterDirtyLayoutCallback(MarkDirty);Canvas.preWillRenderCanvases+=Fit;dirty=true;}
        private void OnDisable(){if(text!=null)text.UnregisterDirtyLayoutCallback(MarkDirty);Canvas.preWillRenderCanvases-=Fit;}
        private void OnRectTransformDimensionsChange(){dirty=true;Fit();}
        private void MarkDirty(){dirty=true;}
        private void Fit(){if(!dirty||text==null||applying)return;applying=true;try{ReleaseTypography.ApplyHachi(text,ReleaseFontPolicy.For(text.name,text.fontSize,text.text));}finally{applying=false;dirty=false;}}
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
