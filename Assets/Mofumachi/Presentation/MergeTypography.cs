using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // Merge-only font candidate. Existing colors, geometry and information faces stay intact.
    public static class MergeTypography
    {
        private static Font hachi,kiwi,level;
        public static MergeFontVariant Variant
        {
            get
            {
#if UNITY_EDITOR
                return PreviewVariant;
#else
                return MergeFontVariant.B;
#endif
            }
        }
#if UNITY_EDITOR
        public static MergeFontVariant PreviewVariant=MergeFontVariant.B;
#endif
        public static bool TryApply(Text text)
        {
            if(text.GetComponentInParent<MergeRewardView>()==null||MergeRewardPresentation.CapturePinkBaseline)return false;
            if(text.name=="Item level")
            {
                if(level==null)level=Resources.Load<Font>("Mofumachi/UIFontReadingBold");
                if(level==null)return false;text.font=level;
                if(text.GetComponent<MergeLevelFontFit>()==null)text.gameObject.AddComponent<MergeLevelFontFit>();
                FitLevel(text);
                return true;
            }
            bool title=text.name=="Page title";
            if(!title&&!MergeFontProfile.IsMenu(text.name))return false;
            var choice=title?Variant:MergeFontVariant.A;
            if(hachi==null)hachi=Resources.Load<Font>("Mofumachi/UIFontAccent");
            if(kiwi==null)kiwi=Resources.Load<Font>("Mofumachi/UIFontStory");
            var font=choice==MergeFontVariant.C?kiwi:hachi;if(font==null)return false;
            text.font=font;text.fontStyle=MergeFontProfile.Bold(choice)?FontStyle.Bold:FontStyle.Normal;
            // Remove the old decorative shadow only from these Merge labels.
            var edge=text.GetComponent<MergeFontEdge>();
            foreach(var shadow in text.GetComponents<Shadow>())if(shadow!=edge)shadow.enabled=false;
            float distance=title?MergeFontProfile.Outline(choice):0;
            if(distance>0)
            {
                if(edge==null)edge=text.gameObject.AddComponent<MergeFontEdge>();
                edge.effectColor=new Color(.58f,.19f,.31f,.68f);edge.effectDistance=new Vector2(distance,-distance);edge.useGraphicAlpha=true;edge.enabled=true;
            }
            else if(edge!=null)edge.enabled=false;
            return true;
        }
        internal static void FitLevel(Text text)
        {
            // The existing narrow badge can be shorter than a 9px line.
            // Keep its rectangle fixed and use 8px only when 9px clips.
            while(text.fontSize>8&&text.rectTransform.rect.height>0&&text.preferredHeight>text.rectTransform.rect.height+.05f)text.fontSize--;
        }
    }
    public sealed class MergeFontEdge:Outline { }
    public sealed class MergeLevelFontFit:MonoBehaviour
    {
        private Text text;private bool dirty=true;
        private void OnEnable(){text=GetComponent<Text>();text.RegisterDirtyLayoutCallback(MarkDirty);Canvas.willRenderCanvases+=Fit;dirty=true;}
        private void OnDisable(){if(text!=null)text.UnregisterDirtyLayoutCallback(MarkDirty);Canvas.willRenderCanvases-=Fit;}
        private void OnRectTransformDimensionsChange(){dirty=true;}
        private void MarkDirty(){dirty=true;}
        private void Fit(){if(!dirty||text==null)return;MergeTypography.FitLevel(text);dirty=false;}
    }
}
