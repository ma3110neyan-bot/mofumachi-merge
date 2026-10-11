using Mofumachi.Core;
using UnityEngine;
namespace Mofumachi.Presentation
{
    public static class MergeActionMenuView
    {
        public static void Build(RectTransform root,ScreenContext c)
        {
            var overlay=c.Widgets.Panel("Merge menu modal",root,new Rect(0,0,1,1),new Color(.25f,.15f,.25f,.6f)).rectTransform;
            overlay.GetComponent<RoundedPanelGraphic>().raycastTarget=true;
            var card=c.Widgets.OrnatePanel("Merge menu card",overlay,new Rect(0,0,1,1),UIWidgets.BlushPaper).rectTransform;
            card.anchorMin=card.anchorMax=new Vector2(.5f,.5f);card.sizeDelta=new Vector2(320,400);
            var title=c.Widgets.Ribbon("Merge menu title","つくるアイテム",card,new Rect(0,0,1,1),18);
            UIWidgets.Top((RectTransform)title.transform.parent,28,16,28,40);
            for(int i=0;i<ItemCatalog.All.Count;i++)
            {
                var item=ItemCatalog.All[i];
                var b=c.Widgets.Button("Produce "+item.Id,item.Name,card,new Rect(0,0,1,1),()=>c.Flow.ChooseProductionItem(item.Id),item.Id!=c.Flow.ProductionItemId);
                UIWidgets.Top((RectTransform)b.transform,24,68+i*62,24,52);
                c.Widgets.Item("Production art",item.Id,1,(RectTransform)b.transform,new Rect(.05f,.1f,.18f,.8f));
            }
            var discard=c.Widgets.Button("Discard selected","選んだアイテムを片づける",card,new Rect(0,0,1,1),()=>c.Flow.OpenDiscardConfirmation(c.Flow.SelectedCell),true);
            UIWidgets.Top((RectTransform)discard.transform,24,264,24,52);discard.interactable=c.Flow.SelectedCell>=0;
            var close=c.Widgets.Button("Cancel merge menu","戻る",card,new Rect(0,0,1,1),c.Flow.CancelMergeActions,true);
            UIWidgets.Bottom((RectTransform)close.transform,24,16,24,52);
        }
    }
}
