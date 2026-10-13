using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public static class MergeActionMenuView
    {
        public static void Build(RectTransform root,ScreenContext c)
        {
            var w=c.Widgets;
            var overlay=w.Panel("Merge menu modal",root,new Rect(0,0,1,1),new Color(.25f,.15f,.25f,.6f)).rectTransform;
            overlay.GetComponent<RoundedPanelGraphic>().raycastTarget=true;
            var card=w.OrnatePanel("Merge menu card",overlay,new Rect(0,0,1,1),UIWidgets.BlushPaper).rectTransform;
            card.anchorMin=card.anchorMax=new Vector2(.5f,.5f);card.sizeDelta=new Vector2(320,480);
            bool cleanup=c.Flow.IsFrameCleanupMode;
            var title=w.StoryRibbon("Merge menu title",cleanup?"生成枠を整理":"生成枠",card,new Rect(0,0,1,1),18);
            UIWidgets.Top((RectTransform)title.transform.parent,24,16,24,40);
            var help=w.StoryLabel("Merge menu help",cleanup?"片づける生成枠を選んでください。\n盤面のアイテムは消えません。":(c.Flow.FeedbackFor(ScreenId.Merge).Length>0?c.Flow.FeedbackFor(ScreenId.Merge):UIStrings.ProductionMenuHint),card,new Rect(0,0,1,1),12);
            UIWidgets.Top(help.rectTransform,16,60,16,34);
            for(int i=0;i<ItemCatalog.All.Count;i++)
            {
                var item=ItemCatalog.All[i];bool placed=c.Game.State.productionFrameIds.Contains(item.Id),chosen=c.Flow.IsFrameSelected(item.Id);
                string copy=item.Name+"の生成枠\n"+(cleanup?(chosen?"選択中":"選ぶ"):placed?"配置中":"枠を置く");
                var b=w.Button("Produce "+item.Id,copy,card,new Rect(0,0,1,1),()=>{if(cleanup)c.Flow.ToggleFrameSelection(item.Id);else c.Flow.ChooseProductionItem(item.Id);},!chosen);
                UIWidgets.Top((RectTransform)b.transform,24,100+i*64,24,56);
                b.GetComponent<RoundedPanelGraphic>().color=chosen?UIWidgets.Pink:placed?UIWidgets.Mint:UIWidgets.Cream;
                b.interactable=!cleanup||placed;
                var text=b.GetComponentInChildren<Text>();text.fontSize=14;text.rectTransform.anchorMin=new Vector2(.28f,.06f);text.rectTransform.anchorMax=new Vector2(chosen?.72f:.94f,.94f);
                w.Item("Production art",item.Id,1,(RectTransform)b.transform,new Rect(.04f,.1f,.21f,.8f));
                if(chosen)w.Label("Frame selected badge","選択中",(RectTransform)b.transform,new Rect(.74f,.66f,.24f,.3f),9);
            }
            if(cleanup)
            {
                var pack=w.Button("Pack selected frames","選択した"+c.Flow.SelectedFrameCount+"個を片づける",card,new Rect(0,0,1,1),c.Flow.OpenPackFramesConfirmation);
                UIWidgets.Top((RectTransform)pack.transform,24,306,24,52);pack.interactable=c.Flow.SelectedFrameCount>0;
                var status=w.Label("Frame cleanup status",c.Flow.FeedbackFor(ScreenId.Merge),card,new Rect(0,0,1,1),12);UIWidgets.Top(status.rectTransform,20,364,20,42);
            }
            else
            {
                var begin=w.Button("Begin frame cleanup","生成枠を整理",card,new Rect(0,0,1,1),c.Flow.BeginFrameCleanup,true);
                UIWidgets.Top((RectTransform)begin.transform,24,296,24,48);begin.interactable=c.Game.State.productionFrameIds.Count>0;
                var selected=c.Game.Board.At(c.Flow.SelectedCell);
                var discard=w.Button("Discard selected",selected==null?"盤面のアイテムを選ぶ":"盤面："+UIStrings.ItemLabel(selected)+"を片づける",card,new Rect(0,0,1,1),()=>c.Flow.OpenDiscardConfirmation(c.Flow.SelectedCell),true);
                discard.GetComponentInChildren<Text>().fontSize=14;UIWidgets.Top((RectTransform)discard.transform,24,354,24,48);discard.interactable=selected!=null;
            }
            var close=w.Button("Cancel merge menu",cleanup?"終了":"戻る",card,new Rect(0,0,1,1),c.Flow.CancelMergeActions,true);
            UIWidgets.Bottom((RectTransform)close.transform,24,14,24,48);
        }
    }
}
