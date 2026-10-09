using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public sealed class PurchaseNoticeView : MonoBehaviour
    {
        public static void Build(RectTransform root,ScreenContext c)
        {
            var overlay=UIWidgets.Node("Purchase notice",root,new Rect(0,0,1,1));overlay.gameObject.AddComponent<PurchaseNoticeView>();
            var dim=overlay.gameObject.AddComponent<Image>();dim.color=new Color(.14f,.11f,.2f,.68f);dim.raycastTarget=true;
            var card=c.Widgets.Panel("Notice card",overlay,new Rect(.045f,.24f,.91f,.53f)).rectTransform;
            c.Widgets.Label("Notice title",UIStrings.NoticeTitle,card,new Rect(.05f,.75f,.9f,.18f),23);
            c.Widgets.Label("Notice body",UIStrings.NoticeBody,card,new Rect(.08f,.31f,.84f,.4f),17);
            c.Widgets.Label("Notice error",c.Flow.FeedbackFor(ScreenId.Title),card,new Rect(.08f,.17f,.84f,.13f),12);
            var b=c.Widgets.Button("Notice confirm",UIStrings.NoticeConfirm,card,new Rect(.08f,.02f,.84f,.14f),()=>c.Flow.ConfirmPurchaseNotice());
            UIWidgets.Bottom((RectTransform)b.transform,20,16,20,52);
        }
    }
}
