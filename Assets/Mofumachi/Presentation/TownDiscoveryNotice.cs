using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public sealed class TownDiscoveryNotice:MonoBehaviour
    {
        private float remaining=6;private UIFlowController flow;
        public static void Show(RectTransform root,ScreenContext c,string id)
        {
            var old=root.Find("New book discovery");if(old!=null){old.gameObject.SetActive(false);Destroy(old.gameObject);}
            var button=c.Widgets.Button("New book discovery",CharacterCatalog.Find(id).Name+"の新しい一面を発見♪",root,new Rect(0,0,1,1),()=>c.Flow.OpenCharacterBook(id),true);
            var r=(RectTransform)button.transform;UIWidgets.Top(r,18,84,18,48);button.GetComponent<RoundedPanelGraphic>().color=UIWidgets.RosePaper;
            var text=button.GetComponentInChildren<Text>();text.font=Resources.Load<Font>("Mofumachi/UIFontStory");text.fontSize=14;
            var notice=button.gameObject.AddComponent<TownDiscoveryNotice>();notice.flow=c.Flow;
        }
        private void Update(){if(flow==null||flow.IsAppPaused||flow.IsModalOpen)return;remaining-=Mathf.Min(Time.unscaledDeltaTime,.1f);if(remaining<=0){gameObject.SetActive(false);Destroy(gameObject);}}
    }
    public sealed class TownTodayBadge:MonoBehaviour
    {
        private ScreenContext context;private Text title,mark;private float next;
        public void Bind(ScreenContext c,Text label,Text badge){context=c;title=label;mark=badge;Refresh();}
        private void Update(){if(Time.unscaledTime<next)return;next=Time.unscaledTime+.25f;Refresh();}
        private void Refresh()
        {
            if(context==null)return;var d=context.Flow.Daily;title.text="今日\n"+(d.Data.Claimed?"お花♪":d.ReadyToClaim?"受取♪":d.CompletedGoals==2?"あと1つ":d.CompletedGoals+" / 3");
            mark.text=d.ShouldGreet||d.ReadyToClaim?"NEW":"";
        }
    }
}
