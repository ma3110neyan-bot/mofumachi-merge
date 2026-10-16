using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public static class TownTodayView
    {
        public static void Build(RectTransform root,ScreenContext c)
        {
            var w=c.Widgets;var daily=c.Flow.Daily;
            var overlay=w.Panel("Today overlay",root,new Rect(0,0,1,1),new Color(.25f,.17f,.26f,.48f)).rectTransform;overlay.GetComponent<RoundedPanelGraphic>().raycastTarget=true;
            var card=w.OrnatePanel("Today storybook",overlay,new Rect(.5f,.5f,0,0),UIWidgets.BlushPaper,18).rectTransform;card.sizeDelta=new Vector2(Mathf.Min(340,root.rect.width-24),Mathf.Min(508,root.rect.height-24));
            var title=w.StoryRibbon("Today title","今日のもふまち",card,new Rect(0,0,1,1),18);UIWidgets.Top((RectTransform)title.transform.parent,18,12,18,36);
            var character=UIWidgets.Node("Today friend",card,new Rect(0,0,1,1));UIWidgets.Top(character,22,56,card.rect.width-102,82);w.Body("Today friend body",daily.CharacterOfDay,character,new Rect(0,0,1,1));
            var greeting=w.NarrativeLabel("Today greeting",daily.Greeting,card,new Rect(0,0,1,1),14);UIWidgets.Top(greeting.rectTransform,110,56,20,82);greeting.color=new Color(.38f,.23f,.31f);greeting.lineSpacing=1.28f;
            string[] names={"合成を3回","仲間とお話を1回","お隣の地域まで歩く"};int[] counts={daily.Data.Merges,daily.Data.Talks,daily.Data.Walks};int[] goals={3,1,1};
            for(int i=0;i<3;i++)
            {
                var row=w.OrnatePanel("Today goal "+i,card,new Rect(0,0,1,1),counts[i]>=goals[i]?UIWidgets.BluePaper:UIWidgets.Cream,10).rectTransform;UIWidgets.Top(row,18,150+i*57,18,50);
                w.Icon("Today goal flower "+i,UIIcon.Flower,row,new Rect(.035f,.24f,.10f,.52f));
                w.NarrativeLabel("Today goal name "+i,names[i],row,new Rect(.16f,.13f,.66f,.74f),14);
                w.Heading("Today goal count "+i,counts[i]+"/"+goals[i],row,new Rect(.82f,.13f,.15f,.74f),14);
            }
            var claim=w.Button("Claim today flower",daily.Data.Claimed?"今日のお花を集めたよ♪":"今日のお花を受け取る",card,new Rect(0,0,1,1),c.Flow.ClaimDaily);UIWidgets.Top((RectTransform)claim.transform,20,330,20,48);claim.interactable=daily.ReadyToClaim;
            var message=w.NarrativeLabel("Today next discovery",daily.LastError.Length>0?daily.LastError:"集めたお花 "+daily.Data.DecorationDays.Count+"こ\n"+daily.NextGoal,card,new Rect(0,0,1,1),13);UIWidgets.Top(message.rectTransform,20,384,20,46);
            var book=w.Button("Open character book","仲間の図鑑",card,new Rect(0,0,1,1),()=>{c.Flow.CloseToday();c.Flow.ShowCharacterSelection();},true);UIWidgets.Bottom((RectTransform)book.transform,18,16,card.rect.width/2+4,48);
            var close=w.Button("Close today","街へ戻る",card,new Rect(0,0,1,1),c.Flow.CloseToday,true);UIWidgets.Bottom((RectTransform)close.transform,card.rect.width/2+4,16,18,48);
        }
        public static void Welcome(RectTransform root,ScreenContext c)
        {
            if(!c.Flow.Daily.ShouldGreet||!c.Flow.Daily.MarkGreeting())return;
            var p=c.Widgets.OrnatePanel("Daily welcome",root,new Rect(0,0,1,1),UIWidgets.Cream,14).rectTransform;UIWidgets.Top(p,20,145,20,48);
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
            if(finale){title.text="今日のお願い "+daily.CompletedGoals+" / 3";hint.text=daily.Data.Claimed?TownCharacterBook.For(daily.CharacterOfDay,context.Game.State,daily.Data).NextDiscovery:daily.NextGoal;}
        }
    }
    public static class TownCharacterDialogue
    {
        private static readonly string[][] Lines={
            new[]{"今日も会えたね。いっしょに、ちいさな発見を探そう♪","北のカフェで、ふわふわのパンを半分こしよう。","あなたと歩く道は、いつもよりきらきらして見えるね。"},
            new[]{"ねえ、噴水の光を見つけたよ！いっしょに見よう♪","お茶の香りがする方へ、ちょっとおさんぽしない？","また会えた！今日はどんな発見が待っているかな。"},
            new[]{"のんびり、のんびり。ベンチでお茶にしよう。","焼きたてのパンって、待つ時間までおいしいねえ。","今日もよく歩いたね。ほっとひと息つこうか。"},
            new[]{"南の工房で、かわいい飾りを考えているんだ！","あの花の色、リボンにしたらきっとすてきだよ！","あなたと作った街、どんどん楽しくなるね！"},
            new[]{"今日のお花、とっても元気。あなたに会えてうれしいのね。","花の小道を、ゆっくり歩いてみませんか？","いつも会いに来てくれて、ありがとう。小さなお花をどうぞ♪"},
            new[]{"今日はどんなお気に入りを見つけようかな。","みんなの笑顔も、わたしの大切なコレクション！","またひとつ、あなたとの思い出が増えたね♪"}};
        public static string For(string id,int meetings)
        {var c=Mofumachi.Core.CharacterCatalog.Find(id);return c==null?"":Lines[c.PortraitIndex][System.Math.Max(0,meetings-1)%3];}
        public static System.Collections.Generic.IEnumerable<string> Text=>System.Linq.Enumerable.SelectMany(Lines,x=>x);
    }
}
