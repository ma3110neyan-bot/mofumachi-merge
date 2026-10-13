using UnityEngine;
namespace Mofumachi.Presentation
{
    public static class DiscardConfirmationView
    {
        public static void BuildFrames(RectTransform root,ScreenContext c)
        {
            var w=c.Widgets;
            var overlay=w.Panel("Pack frames modal",root,new Rect(0,0,1,1),new Color(.25f,.15f,.25f,.6f)).rectTransform;overlay.GetComponent<RoundedPanelGraphic>().raycastTarget=true;
            var card=w.OrnatePanel("Pack frames card",overlay,new Rect(0,0,1,1),UIWidgets.BlushPaper).rectTransform;
            card.anchorMin=card.anchorMax=new Vector2(.5f,.5f);card.sizeDelta=new Vector2(320,390);
            w.StoryRibbon("Pack title","生成枠を片づけますか？",card,new Rect(.08f,.83f,.84f,.12f),17);
            for(int i=0;i<c.Flow.PendingFrameIds.Count;i++)
            {
                string id=c.Flow.PendingFrameIds[i];var row=w.Panel("Pack target row",card,new Rect(0,0,1,1),UIWidgets.BluePaper).rectTransform;
                UIWidgets.Top(row,24,72+i*48,24,42);
                w.Item("Pack target art",id,1,row,new Rect(.035f,.05f,.17f,.9f));
                w.StoryLabel("Pack target",Mofumachi.Core.ItemCatalog.Find(id).Name+"の生成枠",row,new Rect(.23f,.05f,.72f,.9f),15);
            }
            var warning=w.StoryLabel("Pack warning","生成枠"+c.Flow.PendingFrameIds.Count+"個を片づけます。\n盤面のアイテムは残ります。",card,new Rect(0,0,1,1),14);UIWidgets.Top(warning.rectTransform,16,226,16,44);
            var feedback=w.Label("Pack feedback",c.Flow.FeedbackFor(ScreenId.Merge),card,new Rect(0,0,1,1),12);UIWidgets.Top(feedback.rectTransform,16,272,16,42);
            var yes=w.Button("Confirm pack frames","片づける",card,new Rect(0,0,1,1),()=>c.Flow.ConfirmPackFrames());UIWidgets.Bottom((RectTransform)yes.transform,16,14,168,52);
            var no=w.Button("Cancel pack frames","戻る",card,new Rect(0,0,1,1),c.Flow.CancelPackFrames,true);UIWidgets.Bottom((RectTransform)no.transform,168,14,16,52);
        }
        public static void Build(RectTransform root,ScreenContext c)
        {
            var overlay=c.Widgets.Panel("Discard modal",root,new Rect(0,0,1,1),new Color(.25f,.15f,.25f,.6f)).rectTransform;
            overlay.GetComponent<RoundedPanelGraphic>().raycastTarget=true;
            var card=c.Widgets.OrnatePanel("Discard card",overlay,new Rect(0,0,1,1),UIWidgets.BlushPaper).rectTransform;
            card.anchorMin=card.anchorMax=new Vector2(.5f,.5f);card.sizeDelta=new Vector2(320,360);
            c.Widgets.StoryRibbon("Discard title","片づけますか？",card,new Rect(.1f,.82f,.8f,.13f),18);
            c.Widgets.Item("Discard item",c.Flow.DiscardItemId,c.Flow.DiscardItemLevel,card,new Rect(.35f,.57f,.3f,.22f));
            c.Widgets.StoryLabel("Discard item level",(Mofumachi.Core.ItemCatalog.Find(c.Flow.DiscardItemId)?.Name??"アイテム")+" Lv."+c.Flow.DiscardItemLevel,card,new Rect(.1f,.48f,.8f,.08f),16);
            c.Widgets.StoryLabel("Discard warning","このアイテム1個を取り除きます。",card,new Rect(.06f,.33f,.88f,.12f),14);
            c.Widgets.Label("Discard feedback",c.Flow.FeedbackFor(ScreenId.Merge),card,new Rect(.06f,.2f,.88f,.12f),12);
            var yes=c.Widgets.Button("Confirm discard","片づける",card,new Rect(0,0,1,1),()=>c.Flow.ConfirmDiscard());
            UIWidgets.Bottom((RectTransform)yes.transform,16,12,168,52);
            var no=c.Widgets.Button("Cancel discard","戻る",card,new Rect(0,0,1,1),c.Flow.CancelDiscard,true);
            UIWidgets.Bottom((RectTransform)no.transform,168,12,16,52);
        }
    }
}
