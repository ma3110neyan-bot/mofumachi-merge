using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public static class TownTodayView
    {
        public static void Build(RectTransform root,ScreenContext c)
        {
            var w=c.Widgets;var daily=c.Flow.Daily;string friend=daily.CharacterOfDay;
            var overlay=w.Panel("Today overlay",root,new Rect(0,0,1,1),new Color(.25f,.17f,.26f,.48f)).rectTransform;overlay.GetComponent<RoundedPanelGraphic>().raycastTarget=true;
            var card=w.StoryPanel("Today storybook",overlay,new Rect(.5f,.5f,0,0)).rectTransform;card.sizeDelta=new Vector2(Mathf.Min(340,root.rect.width-24),Mathf.Min(508,root.rect.height-24));
            var title=w.StoryRibbon("Today title","今日のもふまち",card,new Rect(0,0,1,1),18);UIWidgets.Top((RectTransform)title.transform.parent,18,12,18,36);
            bool pinnedFlower=daily.ReadyToClaim||daily.Data.Claimed;
            var content=w.ScrollPaper("Today pages",card,pinnedFlower?136:76,54,pinnedFlower?534:650,UIWidgets.StoryPaper);
            var character=UIWidgets.Node("Today friend",content,new Rect(0,0,1,1));UIWidgets.Top(character,8,4,content.rect.width-80,pinnedFlower?68:84);w.Body("Today friend body",friend,character,new Rect(0,0,1,1));
            var greeting=w.NarrativeLabel("Today greeting",daily.Greeting,content,new Rect(0,0,1,1),13);UIWidgets.Top(greeting.rectTransform,88,4,8,pinnedFlower?68:84);greeting.color=new Color(.38f,.23f,.31f);greeting.lineSpacing=1.20f;
            var visit=w.Button("Visit today friend","会いに行く",content,new Rect(0,0,1,1),()=>c.Flow.VisitTownCharacter(friend));UIWidgets.Top((RectTransform)visit.transform,8,pinnedFlower?76:94,8,48);
            var destination=w.NarrativeLabel("Today friend destination",c.Flow.TownCharacterDestination(friend),content,new Rect(0,0,1,1),12);UIWidgets.Top(destination.rectTransform,8,pinnedFlower?126:144,8,pinnedFlower?20:30);
            var life=c.Flow.GetComponentInChildren<TownLifeView>();
            var district=life?.World?.Model?.Hero.District==TownDistrictId.West?TownDistrictId.East:TownDistrictId.West;
            string[] names={"Mergeで届けものづくり","仲間に会って、お話しよう",TownCharacterBook.DistrictLabel(district)+"へ歩こう"};
            string[] hints={"同じものを3回合成 · Mergeへ","キャラをタップして、お話しよう","道を歩いてお隣のエリアへ"};
            int[] counts={daily.Data.Merges,daily.Data.Talks,daily.Data.Walks};int[] goals={3,1,1};
            for(int i=0;i<3;i++)
            {
                int goalIndex=i;
                var row=w.Button("Today goal "+i,"",content,new Rect(0,0,1,1),()=>
                {
                    if(goalIndex==1){c.Flow.VisitTownCharacter(friend);return;}
                    c.Flow.CloseToday();
                    if(goalIndex==0)c.Flow.ShowMerge();else if(life?.World?.IsReady==true)life.World.GuideDistrict(district);
                },true);var rect=(RectTransform)row.transform;UIWidgets.Top(rect,8,(pinnedFlower?150:184)+i*(pinnedFlower?56:68),8,pinnedFlower?52:60);
                row.GetComponent<ButtonPressFeedback>().PreserveHitbox=true;row.GetComponent<RoundedPanelGraphic>().color=counts[i]>=goals[i]?UIWidgets.BluePaper:UIWidgets.StoryPaper;
                w.NarrativeLabel("Today goal name "+i,names[i],rect,new Rect(.04f,.47f,.76f,.46f),13);
                w.NarrativeLabel("Today goal hint "+i,hints[i],rect,new Rect(.04f,.03f,.92f,.40f),11);
                w.Heading("Today goal count "+i,Mathf.Min(counts[i],goals[i])+"/"+goals[i],rect,new Rect(.81f,.47f,.16f,.46f),13);
            }
            var claim=w.Button("Claim today flower",daily.Data.Claimed?"今日のお花を集めたよ♪\n集めたお花 "+daily.Data.DecorationDays.Count+"こ":"今日のお花を受け取る",pinnedFlower?card:content,new Rect(0,0,1,1),c.Flow.ClaimDaily);
            if(pinnedFlower)UIWidgets.Bottom((RectTransform)claim.transform,22,76,22,48);else UIWidgets.Top((RectTransform)claim.transform,8,394,8,48);claim.interactable=daily.ReadyToClaim;
            if(daily.Data.Claimed)claim.GetComponentInChildren<Text>().fontSize=14;
            string discovery=daily.NewDiscoveryCount>0?"NEW · 仲間の図鑑に新しい発見♪":daily.NextGoal;
            var message=w.NarrativeLabel("Today next discovery",daily.LastError.Length>0?daily.LastError:"集めたお花 "+daily.Data.DecorationDays.Count+"こ\n"+discovery,content,new Rect(0,0,1,1),13);UIWidgets.Top(message.rectTransform,8,pinnedFlower?334:450,8,58);
            var goal=TownNextGoal.For(c.Game.State);
            var promise=w.StoryPanel("Today town promise",content,new Rect(0,0,1,1)).rectTransform;UIWidgets.Top(promise,8,pinnedFlower?402:518,8,64);
            w.StoryLabel("Today town next",goal.IsFinale?"明日も、仲間の新しい一面に会おう♪":goal.Title+"\n"+goal.Detail,promise,new Rect(.04f,.05f,.92f,.90f),13);
            var request=w.Button("Today visit request",goal.IsFinale?"新しい一面を図鑑で見る":"次のお願いを見に行く",content,new Rect(0,0,1,1),()=>
            {
                c.Flow.CloseToday();
                if(goal.IsFinale)c.Flow.OpenCharacterBook(daily.FirstNewDiscovery??friend);else if(life!=null)life.GetComponent<TownFacilityView>().RequestEnter("quest");
            },true);UIWidgets.Top((RectTransform)request.transform,8,pinnedFlower?474:590,8,48);request.GetComponentInChildren<Text>().fontSize=14;
            var book=w.Button("Open character book",daily.NewDiscoveryCount>0?"図鑑 NEW":"仲間の図鑑",card,new Rect(0,0,1,1),()=>{c.Flow.CloseToday();c.Flow.OpenCharacterBook(daily.FirstNewDiscovery??friend);},true);UIWidgets.Bottom((RectTransform)book.transform,18,16,card.sizeDelta.x/2+4,48);book.GetComponentInChildren<Text>().fontSize=14;
            var close=w.Button("Close today","街へ戻る",card,new Rect(0,0,1,1),c.Flow.CloseToday,true);UIWidgets.Bottom((RectTransform)close.transform,card.sizeDelta.x/2+4,16,18,48);close.GetComponentInChildren<Text>().fontSize=14;
        }
        public static void Welcome(RectTransform root,ScreenContext c)
        {
            if(!c.Flow.Daily.ShouldGreet||!c.Flow.Daily.MarkGreeting())return;
            var p=c.Widgets.OrnatePanel("Daily welcome",root,new Rect(0,0,1,1),UIWidgets.Cream,14).rectTransform;UIWidgets.Top(p,20,84,20,48);
            string who=Mofumachi.Core.CharacterCatalog.Find(c.Flow.Daily.CharacterOfDay).Name;
            c.Widgets.NarrativeLabel("Daily welcome line","今日は"+who+"と、おさんぽしよう♪",p,new Rect(.04f,.10f,.92f,.80f),14);
            p.gameObject.AddComponent<TownWelcomeLifetime>();
        }
    }
    public sealed class TownWelcomeLifetime:MonoBehaviour
    {
        private float elapsed;
        private void Update(){elapsed+=Mathf.Min(Time.unscaledDeltaTime,.1f);if(elapsed>=4){gameObject.SetActive(false);Destroy(gameObject);}}
    }
    public sealed class TownDailyHomeHint:MonoBehaviour
    {
        private ScreenContext context;private Text title,hint;private bool finale;private float nextCheck;
        public void Bind(ScreenContext c,bool completed,Text heading,Text detail){context=c;finale=completed;title=heading;hint=detail;}
        private void Update()
        {
            if(context==null||Time.unscaledTime<nextCheck)return;nextCheck=Time.unscaledTime+.25f;
            var daily=context.Flow.Daily;daily.EnsureDay(System.DateTime.Now);
            if(finale){title.text="今日のお願い "+daily.CompletedGoals+" / 3";hint.text=TownNextGoal.CompactDetail(context.Game.State,daily);}
        }
    }
}
