using UnityEngine;
namespace Mofumachi.Presentation
{
    public static class DiscardConfirmationView
    {
        public static void Build(RectTransform root,ScreenContext c)
        {
            var overlay=c.Widgets.Panel("Discard modal",root,new Rect(0,0,1,1),new Color(.25f,.15f,.25f,.6f)).rectTransform;
            overlay.GetComponent<RoundedPanelGraphic>().raycastTarget=true;
            var card=c.Widgets.OrnatePanel("Discard card",overlay,new Rect(0,0,1,1),UIWidgets.BlushPaper).rectTransform;
            card.anchorMin=card.anchorMax=new Vector2(.5f,.5f);card.sizeDelta=new Vector2(320,360);
            c.Widgets.Ribbon("Discard title","片づけますか？",card,new Rect(.1f,.82f,.8f,.13f),18);
            c.Widgets.Item("Discard item",c.Flow.DiscardItemId,c.Flow.DiscardItemLevel,card,new Rect(.35f,.57f,.3f,.22f));
            c.Widgets.Label("Discard item level","Lv."+c.Flow.DiscardItemLevel,card,new Rect(.1f,.48f,.8f,.08f),16);
            c.Widgets.Label("Discard warning","このアイテム1個を取り除きます。",card,new Rect(.06f,.33f,.88f,.12f),14);
            c.Widgets.Label("Discard feedback",c.Flow.FeedbackFor(ScreenId.Merge),card,new Rect(.06f,.2f,.88f,.12f),12);
            var yes=c.Widgets.Button("Confirm discard","片づける",card,new Rect(0,0,1,1),()=>c.Flow.ConfirmDiscard());
            UIWidgets.Bottom((RectTransform)yes.transform,16,12,168,52);
            var no=c.Widgets.Button("Cancel discard","戻る",card,new Rect(0,0,1,1),c.Flow.CancelDiscard,true);
            UIWidgets.Bottom((RectTransform)no.transform,168,12,16,52);
        }
    }
}
