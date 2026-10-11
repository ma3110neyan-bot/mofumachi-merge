using System.Collections;
using Mofumachi.Core;
using UnityEngine;
namespace Mofumachi.Presentation
{
    public static class RewardGrowthScreens
    {
        public static void BuildResult(RectTransform root,ScreenContext c,DeliveryPresentation delivery)
        {
            var w=c.Widgets;w.Header(root,c,"納品／報酬");
            var card=w.OrnatePanel("Delivery result",root,new Rect(.065f,.22f,.87f,.63f),UIWidgets.BlushPaper).rectTransform;card.gameObject.AddComponent<SoftReveal>();
            w.Ribbon("Delivery title","納品完了！",card,new Rect(.12f,.8f,.76f,.12f),23);
            w.Body("Delivery requester",delivery.RequesterId,card,new Rect(.335f,.57f,.33f,.21f),CharacterExpression.Smile);
            w.StoryHeading("Delivery thanks",CharacterCatalog.Find(delivery.RequesterId).Name+"："+UIStrings.DeliveryThanks,card,new Rect(.18f,.49f,.64f,.075f),18);
            for(int i=0;i<delivery.DeliveredItems.Count;i++)
            {
                var item=delivery.DeliveredItems[i];w.Item("Delivered item "+i,item.ItemId,item.Level,card,new Rect(.25f+i*.27f,.39f,.22f,.09f));
            }
            w.Divider(card,.38f);
            w.Icon("Success",c.Flow.RewardVisible?UIIcon.Coin:UIIcon.Check,card,new Rect(.1f,.31f,.13f,.1f));
            w.Heading("Reward value",c.Flow.RewardVisible?"+"+delivery.CoinsAwarded+" Coins":"納品が完了しました",card,new Rect(.25f,.3f,.7f,.13f),22);
            w.StoryLabel("Reward description",c.Flow.RewardVisible?"街の仲間からの贈りものです":"報酬を受け取っています…",card,new Rect(.08f,.205f,.84f,.085f),14);
            var next=w.Button("Show growth","街の成長を見る",card,new Rect(0,0,1,1),c.Flow.ShowGrowth);next.interactable=c.Flow.RewardVisible;
            UIWidgets.Bottom((RectTransform)next.transform,20,14,20,48);
            w.Feedback(root,c,ScreenId.Result);w.Nav(root,c,ScreenId.Result);
        }
        public static void BuildGrowth(RectTransform root,ScreenContext c,DeliveryPresentation delivery)
        {
            var w=c.Widgets;w.Header(root,c,"もふまちの成長",UIWidgets.Peach);
            var card=w.OrnatePanel("Town growth",root,new Rect(.06f,.31f,.88f,.48f),UIWidgets.BluePaper).rectTransform;card.gameObject.AddComponent<SoftReveal>();
            w.Ribbon("Growth title","街が成長しました",card,new Rect(.09f,.81f,.82f,.13f),20,UIWidgets.Peach);
            w.Icon("Growth star",UIIcon.Star,card,new Rect(.38f,.55f,.24f,.2f));
            w.Divider(card,.48f);
            w.Heading("Growth levels","街 Lv."+delivery.PreviousTownLevel+" → Lv."+delivery.CurrentTownLevel,card,new Rect(.03f,.3f,.94f,.15f),25);
            w.StoryLabel("Growth description","みんなへのお届けが、街を明るくします。",card,new Rect(.06f,.075f,.88f,.2f),16);
            var home=w.Button("Return town","街へ戻る",root,new Rect(0,0,1,1),c.Flow.ShowHome);UIWidgets.Bottom((RectTransform)home.transform,28,138,28,56);
            w.Feedback(root,c,ScreenId.Growth);w.Nav(root,c,ScreenId.Growth);
        }
    }
    public sealed class SoftReveal : MonoBehaviour
    {
        private IEnumerator Start()
        { float time=0;while(time<.35f){time+=Time.unscaledDeltaTime;transform.localScale=Vector3.one*Mathf.Lerp(.95f,1,Mathf.SmoothStep(0,1,time/.35f));yield return null;}transform.localScale=Vector3.one; }
        private void OnDisable(){StopAllCoroutines();transform.localScale=Vector3.one;}
    }
}
