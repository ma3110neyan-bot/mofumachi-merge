using System.Collections;
using UnityEngine;
namespace Mofumachi.Presentation
{
    public static class RewardGrowthScreens
    {
        public static void BuildResult(RectTransform root,ScreenContext c,DeliveryPresentation delivery)
        {
            var w=c.Widgets;w.Header(root,c,"納品／報酬");
            var card=w.Panel("Delivery result",root,new Rect(.065f,.22f,.87f,.63f)).rectTransform;card.gameObject.AddComponent<SoftReveal>();
            w.Icon("Success",UIIcon.Check,card,new Rect(.4f,.76f,.2f,.14f),new Color(.31f,.65f,.51f));
            w.Label("Delivery title","納品完了！",card,new Rect(.05f,.61f,.9f,.11f),28);
            w.Label("Reward value",c.Flow.RewardVisible?"+"+delivery.CoinsAwarded+" Coins":"お茶を届けました",card,new Rect(.04f,.42f,.92f,.13f),30);
            w.Label("Reward description",c.Flow.RewardVisible?"街の仲間からの贈りものです":"報酬を受け取っています…",card,new Rect(.05f,.28f,.9f,.08f),15);
            var next=w.Button("Show growth","街の成長を見る",card,new Rect(.065f,.06f,.87f,.16f),c.Flow.ShowGrowth);next.interactable=c.Flow.RewardVisible;
            w.Feedback(root,c,ScreenId.Result);w.Nav(root,c,ScreenId.Result);
        }
        public static void BuildGrowth(RectTransform root,ScreenContext c,DeliveryPresentation delivery)
        {
            var w=c.Widgets;w.Header(root,c,"もふまちの成長");
            var card=w.Panel("Town growth",root,new Rect(.06f,.31f,.88f,.48f)).rectTransform;card.gameObject.AddComponent<SoftReveal>();
            w.Icon("Growth star",UIIcon.Star,card,new Rect(.36f,.72f,.28f,.21f),new Color(.95f,.69f,.22f));
            w.Label("Growth title","街が成長しました",card,new Rect(.03f,.53f,.94f,.12f),24);
            w.Label("Growth levels","街 Lv."+delivery.PreviousTownLevel+" → Lv."+delivery.CurrentTownLevel,card,new Rect(.03f,.3f,.94f,.15f),26);
            w.Label("Growth description","みんなのお茶会が、街を明るくします。",card,new Rect(.06f,.11f,.88f,.13f),16);
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
