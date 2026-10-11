using System.Collections;
using UnityEngine;
namespace Mofumachi.Presentation
{
    public static class RewardGrowthScreens
    {
        public static void BuildResult(RectTransform root,ScreenContext c,DeliveryPresentation delivery)
        {
            var w=c.Widgets;w.Header(root,c,"納品／報酬");
            var card=w.OrnatePanel("Delivery result",root,new Rect(.065f,.22f,.87f,.63f)).rectTransform;card.gameObject.AddComponent<SoftReveal>();
            w.Ribbon("Delivery title","納品完了！",card,new Rect(.12f,.8f,.76f,.12f),23);
            w.Icon("Success",c.Flow.RewardVisible?UIIcon.Coin:UIIcon.Check,card,new Rect(.37f,.59f,.26f,.18f));
            w.Divider(card,.55f);
            w.Heading("Reward value",c.Flow.RewardVisible?"+"+delivery.CoinsAwarded+" Coins":"お茶を届けました",card,new Rect(.04f,.4f,.92f,.13f),24);
            w.Label("Reward description",c.Flow.RewardVisible?"街の仲間からの贈りものです":"報酬を受け取っています…",card,new Rect(.08f,.28f,.84f,.1f),14);
            var next=w.Button("Show growth","街の成長を見る",card,new Rect(.065f,.06f,.87f,.16f),c.Flow.ShowGrowth);next.interactable=c.Flow.RewardVisible;
            w.Feedback(root,c,ScreenId.Result);w.Nav(root,c,ScreenId.Result);
        }
        public static void BuildGrowth(RectTransform root,ScreenContext c,DeliveryPresentation delivery)
        {
            var w=c.Widgets;w.Header(root,c,"もふまちの成長");
            var card=w.OrnatePanel("Town growth",root,new Rect(.06f,.31f,.88f,.48f)).rectTransform;card.gameObject.AddComponent<SoftReveal>();
            w.Ribbon("Growth title","街が成長しました",card,new Rect(.09f,.81f,.82f,.13f),20);
            w.Icon("Growth star",UIIcon.Star,card,new Rect(.38f,.55f,.24f,.2f));
            w.Divider(card,.48f);
            w.Heading("Growth levels","街 Lv."+delivery.PreviousTownLevel+" → Lv."+delivery.CurrentTownLevel,card,new Rect(.03f,.3f,.94f,.15f),25);
            w.Label("Growth description","みんなのお茶会が、街を明るくします。",card,new Rect(.06f,.075f,.88f,.2f),16);
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
