using Mofumachi.Core;
using UnityEngine;
namespace Mofumachi.Presentation
{
    public static class TownPlacesView
    {
        private static RectTransform Card(RectTransform root,ScreenContext c,string overlayName,string title,float height)
        {
            var overlay=c.Widgets.Panel(overlayName,root,new Rect(0,0,1,1),new Color(.24f,.16f,.24f,.43f)).rectTransform;
            overlay.GetComponent<RoundedPanelGraphic>().raycastTarget=true;
            var card=c.Widgets.StoryPanel(overlayName+" card",overlay,new Rect(.5f,.5f,0,0)).rectTransform;
            card.sizeDelta=new Vector2(Mathf.Min(340,root.rect.width-24),Mathf.Min(height,root.rect.height-24));
            var heading=c.Widgets.StoryRibbon(overlayName+" title",title,card,new Rect(0,0,1,1),20);
            UIWidgets.Top((RectTransform)heading.transform.parent,18,12,18,38);return card;
        }
        public static void BuildFlorist(RectTransform root,ScreenContext c)
        {
            var w=c.Widgets;var card=Card(root,c,"Shop preview","東の花屋 · お花のアトリエ",404);
            var line=w.NarrativeLabel("Shop status","小さなお花が、花束になるよ。\nお届けのお花を、いっしょに作ろう♪",card,new Rect(0,0,1,1),15);
            UIWidgets.Top(line.rectTransform,22,60,22,60);
            for(int level=1;level<=3;level++)
            {
                float x=.055f+(level-1)*.31f;
                w.Item("Florist flower level "+level,"flower",level,card,new Rect(x,.37f,.27f,.30f));
                w.Label("Florist flower caption "+level,"Lv."+level,card,new Rect(x,.31f,.27f,.06f),13);
            }
            var make=w.Button("Florist make flowers","マージでお花を作る",card,new Rect(0,0,1,1),c.Flow.MakeFlowersFromShop);
            UIWidgets.Bottom((RectTransform)make.transform,20,72,20,48);
            var close=w.Button("Close shop","街へ戻る",card,new Rect(0,0,1,1),c.Flow.CloseShopPreview,true);
            UIWidgets.Bottom((RectTransform)close.transform,20,16,20,48);
        }
        public static void BuildCafe(RectTransform root,ScreenContext c)
        {
            var w=c.Widgets;var card=Card(root,c,"Cafe visit","西のカフェ · ひとやすみ",408);
            string friend=c.Flow.CafeFriend();string who=friend??c.Game.State.favoriteCharacterId;
            var body=UIWidgets.Node("Cafe friend",card,new Rect(0,0,1,1));UIWidgets.Top(body,card.sizeDelta.x*.35f,62,card.sizeDelta.x*.35f,108);
            w.Body("Cafe friend body",who,body,new Rect(0,0,1,1),CharacterExpression.Smile);
            var text=w.NarrativeLabel("Cafe welcome",friend!=null?CharacterCatalog.Find(friend).Name+"も、ここでひとやすみ。\nお茶の時間に、お話しよう♪":"お花の香りと、やさしいお茶。\nひとやすみしたら、仲間に会いに行こう♪",card,new Rect(0,0,1,1),15);
            UIWidgets.Top(text.rectTransform,20,178,20,70);
            var action=w.Button("Cafe talk",friend!=null?CharacterCatalog.Find(friend).Name+"とお話する":"今日の仲間に会いに行く",card,new Rect(0,0,1,1),()=>{if(friend!=null)c.Flow.TalkAtCafe(friend);else c.Flow.VisitFromCafe(c.Flow.Daily.CharacterOfDay);});
            UIWidgets.Bottom((RectTransform)action.transform,20,72,20,48);
            var close=w.Button("Close cafe","街へ戻る",card,new Rect(0,0,1,1),c.Flow.CloseCafe,true);UIWidgets.Bottom((RectTransform)close.transform,20,16,20,48);
        }
    }
}
