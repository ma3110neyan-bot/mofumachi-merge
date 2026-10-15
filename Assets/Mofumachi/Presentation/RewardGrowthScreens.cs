using System.Collections;
using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public static class RewardGrowthScreens
    {
        public static void BuildResult(RectTransform root,ScreenContext c,DeliveryPresentation delivery)
        {
            var w=c.Widgets;w.Header(root,c,"納品／報酬");
            var card=w.OrnatePanel("Delivery result",root,new Rect(0,0,1,1),UIWidgets.BlushPaper).rectTransform;card.offsetMin=new Vector2(14,130);card.offsetMax=new Vector2(-14,-78);card.gameObject.AddComponent<SoftReveal>();
            var title=w.StoryRibbon("Delivery title","納品完了！",card,new Rect(0,0,1,1),23);UIWidgets.Top((RectTransform)title.transform.parent,24,10,24,38);
            var conversation=UIWidgets.Node("Delivery conversation",card,new Rect(0,0,1,1));UIWidgets.Top(conversation,12,56,12,98);
            w.Body("Delivery requester",delivery.RequesterId,conversation,new Rect(.015f,.23f,.27f,.77f),CharacterExpression.Smile);
            w.NarrativeLabel("Delivery requester name",CharacterCatalog.Find(delivery.RequesterId).Name,conversation,new Rect(.01f,0,.28f,.22f),14);
            w.NarrativeLabel("Delivery thanks",QuestStoryPresentation.For(delivery.QuestId)?.Thanks??UIStrings.DeliveryThanks,conversation,new Rect(.32f,.05f,.65f,.9f),16);
            var delivered=UIWidgets.Node("Delivered items and reward",card,new Rect(0,0,1,1));UIWidgets.Top(delivered,14,164,14,38);
            for(int i=0;i<delivery.DeliveredItems.Count;i++)
            {
                var item=delivery.DeliveredItems[i];w.Item("Delivered item "+i,item.ItemId,item.Level,delivered,new Rect(i*.18f,0,.17f,1));
            }
            w.Icon("Success",c.Flow.RewardVisible?UIIcon.Coin:UIIcon.Check,delivered,new Rect(.4f,.18f,.10f,.64f));
            w.Heading("Reward value",c.Flow.RewardVisible?"+"+delivery.CoinsAwarded+" Coins":"お届け完了",delivered,new Rect(.52f,0,.47f,1),18);
            var description=w.NarrativeLabel("Reward description",c.Flow.RewardVisible?"あなたのお届けが、街の景色になっていきます。":"報酬を受け取っています…",card,new Rect(0,0,1,1),14);UIWidgets.Top(description.rectTransform,16,210,16,48);
            var next=w.Button("Show growth","街の成長を見る",card,new Rect(0,0,1,1),c.Flow.ShowGrowth);next.interactable=c.Flow.RewardVisible;
            UIWidgets.Bottom((RectTransform)next.transform,20,14,20,48);
            w.Feedback(root,c,ScreenId.Result);w.Nav(root,c,ScreenId.Result);
        }
        public static void BuildGrowth(RectTransform root,ScreenContext c,DeliveryPresentation delivery)
        {
            var w=c.Widgets;w.Header(root,c,"もふまちの成長",UIWidgets.Peach);
            var card=w.ScrollPaper("Town growth",root,136,78,398,UIWidgets.BluePaper);var viewport=(RectTransform)card.parent;
            var title=w.StoryRibbon("Growth title","街が成長しました",card,new Rect(0,0,1,1),20,UIWidgets.Peach);UIWidgets.Top((RectTransform)title.transform.parent,12,10,12,38);
            card.gameObject.AddComponent<TownGrowthComparisonView>().Bind(c,delivery);
            var levels=w.Heading("Growth levels","街 Lv."+delivery.PreviousTownLevel+" → Lv."+delivery.CurrentTownLevel,card,new Rect(0,0,1,1),18);UIWidgets.Top(levels.rectTransform,12,232,12,28);
            var description=w.NarrativeLabel("Growth description",QuestStoryPresentation.For(delivery.QuestId)?.TownMoment??"みんなへのお届けが、街を明るくします。",card,new Rect(0,0,1,1),14);UIWidgets.Top(description.rectTransform,16,264,16,48);
            TownGoalCard.Build(c,card,320);
            var chapter=ChapterCatalog.ForQuest(delivery.QuestId);bool complete=chapter!=null&&ChapterProgress.For(c.Game.State).IsComplete(chapter.Id);
            var home=w.Button("Return town",complete?"章のおもいでへ":"街へ戻る",root,new Rect(0,0,1,1),c.Flow.ContinueAfterGrowth);UIWidgets.Bottom((RectTransform)home.transform,28,84,28,48);
            w.Feedback(root,c,ScreenId.Growth);w.Nav(root,c,ScreenId.Growth);
            var feedback=root.Find("Feedback").GetComponent<Text>();float feedbackHeight=Mathf.Max(48,Mathf.Ceil(feedback.preferredHeight+4));
            UIWidgets.Bottom(feedback.rectTransform,16,138,16,feedbackHeight);
            var feedbackSurface=root.Find("Feedback surface") as RectTransform;if(feedbackSurface!=null)UIWidgets.Bottom(feedbackSurface,12,138,12,feedbackHeight);
            if(feedback.text.Length>0)viewport.offsetMin=new Vector2(14,138+feedbackHeight+6);
        }
    }
    public sealed class SoftReveal : MonoBehaviour
    {
        private IEnumerator Start()
        { float time=0;while(time<.35f){time+=Time.unscaledDeltaTime;transform.localScale=Vector3.one*Mathf.Lerp(.95f,1,Mathf.SmoothStep(0,1,time/.35f));yield return null;}transform.localScale=Vector3.one; }
        private void OnDisable(){StopAllCoroutines();transform.localScale=Vector3.one;}
    }
}
