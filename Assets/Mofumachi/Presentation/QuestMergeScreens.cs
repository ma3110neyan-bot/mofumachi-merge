using System.Linq;
using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public static class QuestMergeScreens
    {
        public static void BuildQuest(RectTransform root,ScreenContext c)
        {
            var w=c.Widgets;w.Header(root,c,"キャラ依頼",UIWidgets.Peach);
            var paper=w.ScrollPaper("Quest card",root,208,80,360,UIWidgets.BlushPaper);
            var area=UIWidgets.Node("Quest story layout",paper,new Rect(0,0,1,1));UIWidgets.Top(area,0,0,0,360);
            TownGoalCard.Build(c,root,0);UIWidgets.Bottom(root.Find("Next town goal") as RectTransform,14,132,14,68);
            var quest=QuestCatalog.NextFor(c.Game.State);
            bool unavailable=quest==null,finished=unavailable&&ChapterProgress.For(c.Game.State).AllCompleted,active=!unavailable && c.Game.State.activeQuestId==quest.Id && !c.Game.State.completedQuestIds.Contains(quest.Id);
            var ribbon=w.StoryRibbon("Quest title",unavailable?(finished?"お届け、ありがとう":"依頼を確認"):quest.Title,area,new Rect(.12f,.83f,.76f,.11f),18,UIWidgets.Peach);UIWidgets.Top((RectTransform)ribbon.transform.parent,24,10,24,38);
            if(!unavailable)w.StoryLabel("Quest chapter",ChapterCatalog.ForQuest(quest.Id)?.Title??"",area,new Rect(.08f,.775f,.84f,.065f),14);
            var requester=CharacterCatalog.Find(unavailable?c.Game.State.favoriteCharacterId:quest.RequesterId);
            w.Body("Quest character",requester.Id,area,new Rect(.35f,.61f,.3f,.16f));
            w.NarrativeLabel("Requester",requester.Name,area,new Rect(.25f,.535f,.5f,.07f),14);
            w.NarrativeLabel("Quest instruction",unavailable?(finished?"ありがとう！ 街の仲間とお祝いしよう。":UIStrings.UnknownQuest):QuestStoryPresentation.For(quest.Id)?.Before??quest.Dialogue,area,new Rect(.07f,.385f,.86f,.145f),16);
            if(!unavailable)
            {
                var requirements=c.Game.Quests.GetProgress(quest);
                for(int i=0;i<requirements.Count;i++)
                {
                    var entry=requirements[i];var item=entry.Requirement;float width=.88f/requirements.Count;
                    var required=w.OrnatePanel("Required item "+i,area,new Rect(.06f+i*width,.225f,width-.02f,.155f),UIWidgets.BluePaper,12).rectTransform;
                    w.Item("Required art "+i,item.ItemId,item.Level,required,new Rect(.03f,.1f,.27f,.8f));
                    w.Label("Required name "+i,ItemCatalog.Find(item.ItemId).Name+" Lv."+item.Level,required,new Rect(.31f,.50f,.65f,.47f),14);
                    w.Label("Required count "+i,entry.Available+" / "+item.Count,required,new Rect(.31f,.03f,.65f,.44f),14);
                }
                w.Icon("Reward coin",UIIcon.Coin,area,new Rect(.23f,.125f,.09f,.085f));
                w.Heading("Reward","報酬 "+quest.Coins+" Coins",area,new Rect(.33f,.125f,.56f,.085f),16);
            }
            var accept=w.Button("Accept quest",unavailable?"街へ戻る":active?"依頼を進める":"依頼を受ける",root,new Rect(0,0,1,1),()=>{c.Flow.PlayCue(AudioCue.Confirm);if(unavailable)c.Flow.ShowHome();else if(active)c.Flow.ShowMerge();else c.Flow.AcceptQuest();});
            UIWidgets.Bottom((RectTransform)accept.transform,28,78,28,48);
            w.Feedback(root,c,ScreenId.Quest);w.Nav(root,c,ScreenId.Quest);
            var feedback=root.Find("Feedback").GetComponent<Text>();float errorHeight=Mathf.Max(48,Mathf.Ceil(feedback.preferredHeight+4));UIWidgets.Bottom(feedback.rectTransform,16,208,16,errorHeight);
            var error=root.Find("Feedback surface") as RectTransform;if(error!=null)UIWidgets.Bottom(error,12,208,12,errorHeight);
            if(feedback.text.Length>0)((RectTransform)paper.parent).offsetMin=new Vector2(14,208+errorHeight+6);
        }
        public static void BuildMerge(RectTransform root,ScreenContext c,bool animateSelection=false)
        {
            var w=c.Widgets;w.Header(root,c,UIStrings.MergeTitle,UIWidgets.Sky,48);
            var progress=w.OrnatePanel("Merge progress",root,new Rect(0,0,1,1),UIWidgets.Cream,12).rectTransform;UIWidgets.Top(progress,12,62,12,60);
            var quest=QuestCatalog.NextFor(c.Game.State);bool accepted=UIStrings.IsAccepted(c.Game.State,quest),deliverable=accepted&&c.Game.Quests.CheckDelivery();
            if(quest!=null)
            {
                var requirements=c.Game.Quests.GetProgress(quest);
                for(int i=0;i<requirements.Count;i++)
                {
                    var entry=requirements[i];var req=entry.Requirement;string id=req.ItemId;float span=1f/requirements.Count;
                    var card=w.Button("Requirement "+id,"",progress,new Rect(i*span+.006f,0,span-.012f,1),()=>c.Flow.FocusRequirement(id),true);card.GetComponent<RoundedPanelGraphic>().color=entry.Available>=req.Count?UIWidgets.Mint:UIWidgets.Cream;
                    w.Item("Requirement art "+id,id,req.Level,(RectTransform)card.transform,new Rect(.025f,.06f,.26f,.88f));
                    w.Label("Requirement name "+id,ItemCatalog.Find(id).Name+" Lv."+req.Level,(RectTransform)card.transform,new Rect(.31f,.49f,.66f,.49f),14);
                    w.Label("Requirement count "+id,entry.Available+" / "+req.Count+(entry.Available>=req.Count?" 完了":" あと"+Mathf.Max(0,req.Count-entry.Available)),(RectTransform)card.transform,new Rect(.31f,.01f,.66f,.48f),12);
                }
            }
            else w.StoryLabel("Merge complete hint",UIStrings.DeliveryHint(c.Game),progress,new Rect(.04f,.02f,.92f,.96f),14);
            var tray=w.Panel("Merge tray",root,new Rect(0,0,1,1),new Color(.92f,.79f,.64f)).rectTransform;tray.offsetMin=new Vector2(10,156);tray.offsetMax=new Vector2(-10,-124);
            var traySurface=tray.GetComponent<RoundedPanelGraphic>();traySurface.shadowDepth=9;traySurface.gloss=.32f;traySurface.borderWidth=2;
            var boardArt=UIWidgets.Node("Merge storybook board",tray,new Rect(0,0,1,1)).gameObject.AddComponent<PerspectiveBoardGraphic>();boardArt.raycastTarget=false;
            var grid=UIWidgets.Node("Merge board",tray,new Rect(0,0,1,1));grid.offsetMin=new Vector2(16,12);grid.offsetMax=new Vector2(-16,-22);
            var group=grid.gameObject.AddComponent<GridLayoutGroup>();group.constraint=GridLayoutGroup.Constraint.FixedColumnCount;group.constraintCount=5;group.spacing=new Vector2(4,4);group.childAlignment=TextAnchor.MiddleCenter;
            group.enabled=false;
            var selectedItem=c.Game.Board.At(c.Flow.SelectedCell);
            for(int i=0;i<30;i++)
            {
                int index=i;var item=c.Game.Board.At(i);var b=w.Button("Cell "+i,"",grid,new Rect(0,0,1,1),()=>{} ,true);
                var surface=b.GetComponent<RoundedPanelGraphic>();surface.radius=100;surface.borderWidth=item==null?.5f:1f;surface.gloss=item==null?.16f:.4f;surface.shadowDepth=item==null?1:4;
                surface.color=c.Game.Board.IsLocked(i)?new Color(1,.89f,.59f):index==c.Flow.SelectedCell?new Color(1,.74f,.82f):item==null?new Color(.98f,.93f,.84f,.84f):UIWidgets.Cream;
                UIWidgets.Node("Cell porcelain dish",(RectTransform)b.transform,new Rect(0,0,1,1)).gameObject.AddComponent<MergeCellSurfaceGraphic>().Bind(surface.color);
                if(index!=c.Flow.SelectedCell&&selectedItem!=null&&item!=null&&item.itemId==selectedItem.itemId&&item.level==selectedItem.level&&item.level<GameState.MaxLevel&&!c.Game.Board.IsLocked(index)&&!c.Game.Board.IsLocked(c.Flow.SelectedCell))
                    UIWidgets.Node("Merge candidate glow",(RectTransform)b.transform,new Rect(0,0,1,1)).gameObject.AddComponent<MergeSparkleGraphic>().BindMatch();
                var drag=b.gameObject.AddComponent<MergeBoardView>();drag.Initialize(c.Flow,i);
                b.onClick.AddListener(()=>{if(!drag.SuppressClick)c.Flow.TapCell(index);});
                if(item!=null)
                {
                    var cell=(RectTransform)b.transform;
                    if(ItemCatalog.Find(item.itemId)!=null)
                    {
                        if(item.level>1)
                        {
                            var halo=w.Panel("Item level halo",cell,new Rect(.13f,.35f,.74f,.52f),new Color(1,.89f,.57f,item.level==3?.16f:.08f));halo.radius=100;halo.decorated=false;
                        }
                        bool raised=index==c.Flow.SelectedCell;
                        var ground=w.Panel("Item contact shadow",cell,new Rect(raised?.27f:.19f,.22f,raised?.46f:.62f,.10f),new Color(.25f,.2f,.25f,raised?.14f:.27f));ground.radius=100;ground.decorated=false;
                        float depthScale=MergePerspective.ArtScale(index);
                        var art=w.Item("Item art",item.itemId,item.level,cell,new Rect(.5f-.46f*depthScale,.53f-.35f*depthScale,.92f*depthScale,.70f*depthScale));art.SetPolished(true);
                        if(item.level==3&&index!=c.Flow.SelectedCell)
                            UIWidgets.Node("High level item glint",art.rectTransform,new Rect(0,0,1,1)).gameObject.AddComponent<MergeSparkleGraphic>().BindSelection();
                        if(index==c.Flow.SelectedCell)
                        {
                            if(animateSelection)art.gameObject.AddComponent<ItemSelectionFeedback>().Play();
                            else {art.transform.localScale=Vector3.one*MergeVisualTiming.SelectionScale(MergeVisualTiming.SelectionSeconds);art.rectTransform.anchoredPosition=Vector2.up*MergeVisualTiming.SelectionLift(MergeVisualTiming.SelectionSeconds);}
                            UIWidgets.Node("Selected item glint",art.rectTransform,new Rect(0,0,1,1)).gameObject.AddComponent<MergeSparkleGraphic>().BindSelection();
                        }
                    }
                    else w.Heading("Unknown item","?",cell,new Rect(.09f,.36f,.82f,.59f),20);
                    var tint=item.itemId=="tea"?new Color(.9f,.97f,.94f):item.itemId=="bread"?new Color(1,.94f,.84f):new Color(.99f,.91f,.96f);
                    var badge=w.Panel("Item level badge",cell,new Rect(.35f,.015f,.63f,.31f),tint);badge.radius=8;badge.borderWidth=.8f;badge.gloss=.24f;
                    w.Heading("Item level","Lv."+item.level,cell,new Rect(.34f,.005f,.65f,.35f),11);
                }
            }
            grid.gameObject.AddComponent<MergeGridLayout>();
            var toolbar=UIWidgets.Node("Merge toolbar",root,new Rect(0,0,1,1));UIWidgets.Bottom(toolbar,12,106,12,48);
            float x=0;
            foreach(var item in ItemCatalog.All)
            {
                string id=item.Id;bool placed=c.Game.State.productionFrameIds.Contains(id);
                var generate=w.Button("Generate frame "+id,item.Name+"\n"+(placed?"作る":"置く"),toolbar,new Rect(0,0,0,1),()=>{if(placed)c.Flow.GenerateFromFrame(id);else c.Flow.OpenMergeActions();},true);
                var r=(RectTransform)generate.transform;r.pivot=new Vector2(0,.5f);r.anchoredPosition=new Vector2(x,0);r.sizeDelta=new Vector2(62,0);x+=66;
                generate.GetComponent<RoundedPanelGraphic>().color=id==c.Flow.ProductionItemId?new Color(1,.79f,.84f):UIWidgets.Cream;
                var copy=generate.GetComponentInChildren<Text>();copy.fontSize=12;copy.rectTransform.anchorMin=new Vector2(.30f,.04f);copy.rectTransform.anchorMax=new Vector2(.98f,.96f);
                w.Item("Frame item art",id,1,r,new Rect(.025f,.12f,.30f,.76f)).SetPolished(true);
            }
            var menu=w.Button("Merge actions","生成枠\n整理",toolbar,new Rect(0,0,0,1),c.Flow.OpenMergeActions,true);var mr=(RectTransform)menu.transform;mr.pivot=new Vector2(0,.5f);mr.anchoredPosition=new Vector2(198,0);mr.sizeDelta=new Vector2(48,0);menu.GetComponentInChildren<Text>().fontSize=12;
            bool preview=quest!=null&&!accepted;
            var delivery=w.Button("Deliver",preview?"依頼を見る":"納品する",toolbar,new Rect(0,0,1,1),()=>{if(preview)c.Flow.ShowQuest();else c.Flow.Deliver();});((RectTransform)delivery.transform).offsetMin=new Vector2(250,0);delivery.GetComponentInChildren<Text>().fontSize=14;delivery.interactable=preview||deliverable;
            w.Feedback(root,c,ScreenId.Merge);
            var text=root.Find("Feedback").GetComponent<Text>();text.fontSize=14;text.lineSpacing=1f;
            var feedbackSurface=root.Find("Feedback surface") as RectTransform;
            if(feedbackSurface==null)feedbackSurface=w.Panel("Feedback surface",root,new Rect(0,0,1,1),new Color(1,.988f,.945f,.97f)).rectTransform;
            feedbackSurface.SetSiblingIndex(text.transform.GetSiblingIndex());
            w.Nav(root,c,ScreenId.Merge,58);
            root.gameObject.AddComponent<ProductionFrameLayout>().Bind(tray,toolbar,text,feedbackSurface,c.Game.State.mergeBoard.Count>=30?UIStrings.BoardFull:UIStrings.DeliveryHint(c.Game));
        }
    }
    public sealed class ProductionFrameLayout : MonoBehaviour
    {
        private RectTransform tray,toolbar,surface;private Text feedback;private string hint,message;
        public void Bind(RectTransform boardTray,RectTransform frameStrip,Text feedbackText,RectTransform feedbackSurface,string defaultHint)
        {tray=boardTray;toolbar=frameStrip;feedback=feedbackText;surface=feedbackSurface;hint=defaultHint;message=feedback.text;ResizeNow();}
        public void SetMessage(string value){message=value??"";ResizeNow();}
        private void OnRectTransformDimensionsChange()=>ResizeNow();
        public void ResizeNow()
        {
            if(tray==null||feedback==null)return;var root=(RectTransform)transform;
            feedback.text=message.Length>0?message:hint;
            // Reserve readable lines before allocating the board; never shrink text/cells.
            float height=Mathf.Max(46,Mathf.Ceil(feedback.preferredHeight+4));
            float toolbarBottom=72+height+2;
            bool compact=root.rect.height<112+toolbarBottom+50+322;
            UIWidgets.Top(root.Find("Header") as RectTransform,10,compact?4:8,10,48);
            UIWidgets.Top(root.Find("Merge progress") as RectTransform,12,compact?54:62,12,compact?48:60);
            if(compact)toolbarBottom=54;
            UIWidgets.Bottom(toolbar,12,toolbarBottom,12,48);
            tray.offsetMin=new Vector2(10,toolbarBottom+50);tray.offsetMax=new Vector2(-10,compact?-104:-124);
            var grid=tray.Find("Merge board") as RectTransform;grid.offsetMin=new Vector2(16,compact?4:12);grid.offsetMax=new Vector2(-16,compact?-10:-22);
            var nav=root.Find("Navigation") as RectTransform;UIWidgets.Bottom(nav,8,compact?4:8,8,compact?48:58);
            foreach(var button in nav.GetComponentsInChildren<Button>())
            {var r=(RectTransform)button.transform;r.anchorMin=new Vector2(r.anchorMin.x,compact?0:.06f);r.anchorMax=new Vector2(r.anchorMax.x,compact?1:.94f);}
            // On short screens, feedback is a nonblocking toast over the board only after an action.
            feedback.text=message.Length>0?message:compact?"":hint;
            UIWidgets.Bottom(feedback.rectTransform,14,compact?108:72,14,height);
            UIWidgets.Bottom(surface,12,compact?108:72,12,height);
            feedback.gameObject.SetActive(feedback.text.Length>0);surface.gameObject.SetActive(feedback.text.Length>0);
        }
    }
    public sealed class MergeGridLayout : MonoBehaviour
    {
        private void Start()=>ResizeNow();private void OnRectTransformDimensionsChange()=>ResizeNow();
        public void ResizeNow()
        {
            var root=(RectTransform)transform;var bounds=root.rect;if(bounds.width<256||bounds.height<308)return;
            var g=GetComponent<GridLayoutGroup>();if(g!=null){g.enabled=false;g.cellSize=new Vector2((bounds.width-16)/5,(bounds.height-20)/6);}
            for(int i=0;i<30;i++)
            {
                var child=transform.Find("Cell "+i) as RectTransform;if(child==null)continue;var p=MergePerspective.Cell(i,bounds.width,bounds.height);
                child.anchorMin=child.anchorMax=Vector2.zero;child.pivot=Vector2.zero;child.anchoredPosition=new Vector2(p.X,p.Y);child.sizeDelta=new Vector2(p.Width,p.Height);
            }
        }
    }
}
