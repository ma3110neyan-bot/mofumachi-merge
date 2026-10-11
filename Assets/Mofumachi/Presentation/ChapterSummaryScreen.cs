using Mofumachi.Core;
using UnityEngine;
namespace Mofumachi.Presentation
{
    public static class ChapterSummaryScreen
    {
        public static void Build(RectTransform root,ScreenContext c,ChapterPresentation chapter)
        {
            var w=c.Widgets;w.Header(root,c,chapter.IsFinale?"お祝いのひととき":"章のおもいで");
            var card=w.OrnatePanel("Chapter card",root,new Rect(0,0,1,1),UIWidgets.BlushPaper,24).rectTransform;
            card.offsetMin=new Vector2(14,166);card.offsetMax=new Vector2(-14,-80);
            var title=w.StoryRibbon("Chapter title",chapter.Title,card,new Rect(0,0,1,1),20);UIWidgets.Top((RectTransform)title.transform.parent,12,12,12,42);
            if(chapter.IsFinale)
            {
                var row=UIWidgets.Node("Finale row",card,new Rect(0,0,1,1));UIWidgets.Top(row,8,64,8,94);
                for(int i=0;i<6;i++)
                {
                    var actor=CharacterCatalog.All[i];var region=UIWidgets.Node("Celebrating "+actor.Id,row,new Rect(i/6f+.006f,0,.155f,1));
                    w.Body("Finale partner",actor.Id,region,new Rect(0,.2f,1,.8f),CharacterExpression.Smile);
                    w.StoryLabel("Finale name",actor.Name,region,new Rect(0,0,1,.19f),12);
                }
            }
            else {var body=w.Body("Chapter partner",chapter.FavoriteCharacterId,card,new Rect(0,0,1,1),CharacterExpression.Smile);UIWidgets.Top(body.rectTransform,100,62,100,94);}
            var closing=w.StoryHeading("Chapter closing",chapter.ClosingLine,card,new Rect(0,0,1,1),18);UIWidgets.Top(closing.rectTransform,16,162,16,58);
            // Two story-font lines need 44 units in Unity. Keep a small margin above the totals at 592 height.
            var objective=w.StoryLabel("Chapter objective",chapter.IsFinale?UIStrings.FinaleDescription:chapter.NextChapterTitle.Length>0?"つぎのおもいで："+chapter.NextChapterTitle:chapter.Objective,card,new Rect(0,0,1,1),14);UIWidgets.Top(objective.rectTransform,16,226,16,48);
            var total=w.Label("Chapter total",chapter.CompletedCount+" / 6　　街 Lv."+chapter.TownLevel+"\n"+chapter.Coins.ToString("N0")+" Coins",card,new Rect(0,0,1,1),15);UIWidgets.Bottom(total.rectTransform,12,12,12,54);
            var home=w.Button("Chapter home",chapter.IsFinale?"街へ戻る":"次の章へ",root,new Rect(0,0,1,1),c.Flow.ShowHome);UIWidgets.Bottom((RectTransform)home.transform,28,78,28,48);
            var top=w.Button("Chapter top","保存してTOPへ",root,new Rect(0,0,1,1),()=>c.Flow.ReturnToTitle(),true);UIWidgets.Bottom((RectTransform)top.transform,28,14,28,48);
            var error=w.Label("Chapter error",c.Flow.FeedbackFor(ScreenId.ChapterSummary),root,new Rect(0,0,1,1),13);UIWidgets.Bottom(error.rectTransform,12,130,12,32);
        }
    }
}
