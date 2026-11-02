using System.Linq;
using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // Merge-local treatment of the approved D reference. It only reads progress.
    // Shared widgets/fonts/town art and all board/save services remain untouched.
    public sealed class MergeRewardView : MonoBehaviour
    {
        private Text caption,coins;
        private RectTransform reward;
        private bool hasQuest;
        public static readonly Color Cream=new Color(1,.958f,.883f),Mint=new Color(.84f,.956f,.90f),Rose=new Color(1,.69f,.79f);
        public void Initialize(ScreenContext c)
        {
            var root=(RectTransform)transform;var w=c.Widgets;var quest=QuestCatalog.NextFor(c.Game.State);hasQuest=quest!=null;
            var progress=root.Find("Merge progress") as RectTransform;
            caption=w.Heading("Current request caption","現在の依頼",progress,new Rect(0,0,1,1),11);
            coins=w.Heading("Merge reward coins",hasQuest?"ごほうび "+MergeRewardPresentation.Coins(quest):"",progress,new Rect(0,0,1,1),11);
            caption.alignment=TextAnchor.MiddleLeft;coins.alignment=TextAnchor.MiddleRight;
            var title=root.Find("Header/Page title ribbon") as RectTransform;
            if(title!=null)
            {
                title.anchorMin=new Vector2(.21f,.03f);title.anchorMax=new Vector2(.79f,.97f);
                title.GetComponent<RibbonGraphic>().color=new Color(.96f,.36f,.54f);
                var text=title.Find("Page title").GetComponent<Text>();text.text="ごほうびMerge";text.color=new Color(1,.995f,.96f);text.fontSize=18;
                text.rectTransform.anchorMin=new Vector2(.07f,.035f);text.rectTransform.anchorMax=new Vector2(.93f,.68f);
                w.Heading("Merge brand","もふまちメルジュ",title,new Rect(.12f,.68f,.76f,.28f),9).color=UIWidgets.Ink;
            }
            foreach(string name in new[]{"Back","Settings"})
            {
                var r=root.Find("Header/"+name) as RectTransform;if(r==null)continue;
                r.GetComponent<RoundedPanelGraphic>().color=new Color(.60f,.87f,.77f);w.Frame(r,13,false);
            }
            var tray=root.Find("Merge tray") as RectTransform;
            var old=tray.Find("Merge storybook board").GetComponent<PerspectiveBoardGraphic>();old.enabled=false;
            var paper=w.Panel("Reward board paper",tray,new Rect(0,0,1,1),Cream);paper.radius=15;paper.gloss=.06f;paper.shadowDepth=2;
            paper.transform.SetAsFirstSibling();var frame=w.Frame(tray,30);frame.transform.SetSiblingIndex(1);
            tray.Find("Merge board").SetAsLastSibling();
            var rows=quest==null?null:c.Game.Quests.GetProgress(quest);
            var toolbar=root.Find("Merge toolbar");
            foreach(var item in ItemCatalog.All)
            {
                var r=toolbar.Find("Generate frame "+item.Id) as RectTransform;
                r.GetComponent<RoundedPanelGraphic>().color=MergeRewardPresentation.Needs(rows,item.Id)?Rose:Cream;
                w.Frame(r,13,false);
            }
            var review=toolbar.Find("Deliver") as RectTransform;review.GetComponent<RoundedPanelGraphic>().color=Rose;w.Frame(review,16,false);
            var manage=toolbar.Find("Merge actions") as RectTransform;manage.GetComponent<RoundedPanelGraphic>().color=Cream;w.Frame(manage,13,false);
            var nav=root.Find("Navigation");
            foreach(var b in nav.GetComponentsInChildren<Button>())
            {var g=b.GetComponent<RoundedPanelGraphic>();g.radius=22;g.color=b.name=="Nav Merge"?Rose:new Color(.60f,.87f,.77f);}
            reward=w.OrnatePanel("Merge reward task",root,new Rect(0,0,1,1),new Color(1,.977f,.925f),18).rectTransform;
            var badge=w.Panel("Reward task medal",reward,new Rect(.018f,.10f,.17f,.80f),new Color(1,.90f,.65f));badge.radius=18;badge.borderWidth=1;
            w.Heading("Reward task badge","ごほうび\nタスク",badge.rectTransform,new Rect(.03f,.07f,.94f,.86f),10);
            w.Heading("Reward task description",MergeRewardPresentation.Task(quest),reward,new Rect(.205f,.43f,.52f,.54f),12);
            var track=w.Panel("Reward progress track",reward,new Rect(.22f,.12f,.47f,.21f),new Color(.95f,.89f,.79f));track.radius=5;track.gloss=.1f;
            var fill=w.Panel("Reward progress fill",track.rectTransform,new Rect(0,0,MergeRewardPresentation.Fraction(rows),1),new Color(.93f,.67f,.43f));fill.radius=4;fill.borderWidth=0;fill.shadowDepth=0;
            w.Heading("Reward amount caption","達成ごほうび",reward,new Rect(.74f,.62f,.24f,.32f),10);
            w.Icon("Reward currency coin",UIIcon.Coin,reward,new Rect(.735f,.12f,.09f,.47f));
            w.Heading("Reward amount",MergeRewardPresentation.Coins(quest),reward,new Rect(.825f,.08f,.16f,.53f),11);
        }
        public void Resize(MergeScreenLayout layout,Text feedback)
        {
            if(caption==null)return;var root=(RectTransform)transform;var progress=root.Find("Merge progress") as RectTransform;
            float extra=MergeRewardPresentation.CaptionSpace(root.rect.height,layout);
            UIWidgets.Top(progress,12,layout.ProgressTop,12,layout.ProgressHeight+extra);
            var goal=progress.Find("Merge goal content") as RectTransform;goal.offsetMax=new Vector2(-4,-2-extra);
            UIWidgets.Top(caption.rectTransform,10,1,progress.rect.width*.60f,extra>0?14:12);
            UIWidgets.Top(coins.rectTransform,progress.rect.width*.42f,1,10,extra>0?14:12);
            bool show=goal.gameObject.activeSelf;caption.gameObject.SetActive(show);coins.gameObject.SetActive(show&&hasQuest);
            foreach(var name in goal.GetComponentsInChildren<Text>(true).Where(t=>t.name.StartsWith("Requirement name ")))
            {
                name.fontSize=extra>0?14:12;
                name.rectTransform.offsetMax=new Vector2(0,extra>0?0:-12);
                name.rectTransform.offsetMin=new Vector2(0,extra>0?-24:-30);
            }
            foreach(var art in goal.GetComponentsInChildren<ItemGraphic>(true))
            {art.rectTransform.anchorMin=new Vector2(.025f,.03f);art.rectTransform.anchorMax=new Vector2(.28f,extra>0?.97f:.71f);}
            foreach(var count in goal.GetComponentsInChildren<Text>(true).Where(t=>t.name.StartsWith("Requirement count ")))
            {count.rectTransform.anchorMin=new Vector2(.025f,.01f);count.rectTransform.anchorMax=new Vector2(.975f,extra>0?.44f:.37f);}
            var tray=root.Find("Merge tray") as RectTransform;tray.offsetMax=new Vector2(-10,-layout.TrayTop-extra);
            UIWidgets.Bottom(reward,12,72,12,46);
            reward.gameObject.SetActive(layout.TwoToolbarRows&&hasQuest&&!feedback.gameObject.activeSelf);
        }
    }
}
