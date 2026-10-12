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
            var area=w.OrnatePanel("Quest card",root,new Rect(0,0,1,1),UIWidgets.BlushPaper).rectTransform;area.offsetMin=new Vector2(14,128);area.offsetMax=new Vector2(-14,-80);
            var quest=QuestCatalog.NextFor(c.Game.State);
            bool unavailable=quest==null,finished=unavailable&&ChapterProgress.For(c.Game.State).AllCompleted,active=!unavailable && c.Game.State.activeQuestId==quest.Id && !c.Game.State.completedQuestIds.Contains(quest.Id);
            var ribbon=w.Ribbon("Quest title",unavailable?(finished?"すべての依頼が完了しました":UIStrings.UnknownQuest):quest.Title,area,new Rect(.12f,.83f,.76f,.11f),18,UIWidgets.Peach);UIWidgets.Top((RectTransform)ribbon.transform.parent,24,16,24,44);
            if(!unavailable)w.StoryLabel("Quest chapter",ChapterCatalog.ForQuest(quest.Id)?.Title??"",area,new Rect(.08f,.78f,.84f,.05f),14);
            var requester=CharacterCatalog.Find(unavailable?c.Game.State.favoriteCharacterId:quest.RequesterId);
            w.Body("Quest character",requester.Id,area,new Rect(.35f,.57f,.3f,.20f));
            w.StoryLabel("Requester",requester.Name,area,new Rect(.25f,.515f,.5f,.055f),14);
            w.StoryLabel("Quest instruction",unavailable?(finished?"ありがとう！ 街の仲間とお祝いしよう。":UIStrings.UnknownQuest):quest.Dialogue,area,new Rect(.07f,.405f,.86f,.105f),14);
            if(!unavailable)
            {
                var requirements=c.Game.Quests.GetProgress(quest);
                for(int i=0;i<requirements.Count;i++)
                {
                    var entry=requirements[i];var item=entry.Requirement;float width=.88f/requirements.Count;
                    var required=w.OrnatePanel("Required item "+i,area,new Rect(.06f+i*width,.23f,width-.02f,.17f),UIWidgets.BluePaper,12).rectTransform;
                    w.Item("Required art "+i,item.ItemId,item.Level,required,new Rect(.03f,.1f,.27f,.8f));
                    w.Label("Required count "+i,ItemCatalog.Find(item.ItemId).Name+" Lv."+item.Level+"\n"+entry.Available+" / "+item.Count,required,new Rect(.31f,.04f,.65f,.9f),12);
                }
                w.Icon("Reward coin",UIIcon.Coin,area,new Rect(.23f,.16f,.09f,.06f));
                w.Heading("Reward","報酬 "+quest.Coins+" Coins",area,new Rect(.33f,.15f,.56f,.07f),16);
            }
            var accept=w.Button("Accept quest",unavailable?"街へ戻る":active?"依頼を進める":"依頼を受ける",area,new Rect(0,0,1,1),()=>{c.Flow.PlayCue(AudioCue.Confirm);if(unavailable)c.Flow.ShowHome();else if(active)c.Flow.ShowMerge();else c.Flow.AcceptQuest();});
            UIWidgets.Bottom((RectTransform)accept.transform,20,16,20,52);
            w.Feedback(root,c,ScreenId.Quest);w.Nav(root,c,ScreenId.Quest);
        }
        public static void BuildMerge(RectTransform root,ScreenContext c,bool animateSelection=false)
        {
            var w=c.Widgets;w.Header(root,c,"マージ",UIWidgets.Sky);
            var progress=w.Panel("Merge progress",root,new Rect(0,0,1,1),UIWidgets.BluePaper).rectTransform;UIWidgets.Top(progress,12,68,12,38);
            var quest=QuestCatalog.NextFor(c.Game.State);
            bool accepted=UIStrings.IsAccepted(c.Game.State,quest),deliverable=accepted&&c.Game.Quests.CheckDelivery();
            string summary=quest==null?UIStrings.DeliveryHint(c.Game):UIStrings.RequiredSummary(c.Game,quest);
            string status=quest==null?"":!accepted?"未受注：まず「依頼を見る」へ":deliverable?"納品できます！":!c.Flow.CanInteractBoard?"合成中です":("受注中："+(ChapterCatalog.ForQuest(quest.Id)?.Title??quest.Title));
            w.StoryLabel("Merge chapter",status,progress,new Rect(.03f,.5f,.94f,.5f),12);
            w.Label("Progress",summary,progress,new Rect(.03f,0,.94f,.5f),12);
            var tray=w.Panel("Merge tray",root,new Rect(0,0,1,1),new Color(.73f,.92f,.84f)).rectTransform;tray.offsetMin=new Vector2(10,158);tray.offsetMax=new Vector2(-10,-112);
            w.Frame(tray,22,false);
            var grid=UIWidgets.Node("Merge board",tray,new Rect(0,0,1,1));grid.offsetMin=new Vector2(7,7);grid.offsetMax=new Vector2(-7,-7);
            var group=grid.gameObject.AddComponent<GridLayoutGroup>();group.constraint=GridLayoutGroup.Constraint.FixedColumnCount;group.constraintCount=5;group.spacing=new Vector2(4,4);group.childAlignment=TextAnchor.MiddleCenter;
            for(int i=0;i<30;i++)
            {
                int index=i;var item=c.Game.Board.At(i);var b=w.Button("Cell "+i,"",grid,new Rect(0,0,1,1),()=>{} ,true);
                var surface=b.GetComponent<RoundedPanelGraphic>();surface.radius=13;surface.borderWidth=1;surface.gloss=.2f;
                surface.color=c.Game.Board.IsLocked(i)?new Color(1,.89f,.59f):index==c.Flow.SelectedCell?new Color(1,.69f,.8f):UIWidgets.Cream;
                var drag=b.gameObject.AddComponent<MergeBoardView>();drag.Initialize(c.Flow,i);
                b.onClick.AddListener(()=>{if(!drag.SuppressClick)c.Flow.TapCell(index);});
                if(item!=null)
                {
                    var cell=(RectTransform)b.transform;
                    if(ItemCatalog.Find(item.itemId)!=null)
                    {
                        var art=w.Item("Item art",item.itemId,item.level,cell,new Rect(.09f,.36f,.82f,.59f));art.SetPolished(true);
                        if(index==c.Flow.SelectedCell)
                        {
                            if(animateSelection)art.gameObject.AddComponent<ItemSelectionFeedback>().Play();
                            else art.transform.localScale=Vector3.one*MergeVisualTiming.SelectionScale(MergeVisualTiming.SelectionSeconds);
                            UIWidgets.Node("Selected item glint",art.rectTransform,new Rect(0,0,1,1)).gameObject.AddComponent<MergeSparkleGraphic>().BindSelection();
                        }
                    }
                    else w.Heading("Unknown item","?",cell,new Rect(.09f,.36f,.82f,.59f),20);
                    var tint=item.itemId=="tea"?new Color(.9f,.97f,.94f):item.itemId=="bread"?new Color(1,.94f,.84f):new Color(.99f,.91f,.96f);
                    var badge=w.Panel("Item level badge",cell,new Rect(.13f,.045f,.74f,.28f),tint);badge.radius=5;badge.borderWidth=.6f;badge.gloss=.15f;
                    w.Heading("Item level","Lv."+item.level,cell,new Rect(.02f,.015f,.96f,.36f),11);
                }
            }
            grid.gameObject.AddComponent<MergeGridLayout>();
            var toolbar=UIWidgets.Node("Merge toolbar",root,new Rect(0,0,1,1));UIWidgets.Bottom(toolbar,12,106,12,48);
            var menu=w.Button("Merge actions","種類\n整理",toolbar,new Rect(0,0,0,1),c.Flow.OpenMergeActions,true);((RectTransform)menu.transform).sizeDelta=new Vector2(48,0);
            var menuText=menu.GetComponentInChildren<Text>();menuText.fontSize=14;menuText.lineSpacing=.9f;
            var generate=w.Button("Create tea",ItemCatalog.Find(c.Flow.ProductionItemId).Name+"を作る",toolbar,new Rect(0,0,.56f,1),()=>c.Flow.GenerateItem(c.Flow.ProductionItemId),true);((RectTransform)generate.transform).offsetMin=new Vector2(52,0);
            bool preview=quest!=null&&!accepted;
            var delivery=w.Button("Deliver",preview?"依頼を見る":"納品する",toolbar,new Rect(.59f,0,.41f,1),()=>{if(preview)c.Flow.ShowQuest();else c.Flow.Deliver();});delivery.interactable=preview||deliverable;
            w.Feedback(root,c,ScreenId.Merge); // Compact feedback between navigation and toolbar.
            var text=root.Find("Feedback").GetComponent<Text>();UIWidgets.Bottom(text.rectTransform,14,76,14,28);text.fontSize=11;text.lineSpacing=.82f;
            if(text.text.Length==0)text.text=c.Game.State.mergeBoard.Count>=30?UIStrings.BoardFull:UIStrings.DeliveryHint(c.Game);
            var feedbackSurface=root.Find("Feedback surface") as RectTransform;if(feedbackSurface!=null)UIWidgets.Bottom(feedbackSurface,12,76,12,28);
            w.Nav(root,c,ScreenId.Merge);
        }
    }
    public sealed class MergeGridLayout : MonoBehaviour
    {
        private void Start()=>ResizeNow();private void OnRectTransformDimensionsChange()=>ResizeNow();
        public void ResizeNow(){var g=GetComponent<GridLayoutGroup>();if(g==null)return;float size=ResponsiveUILayout.CellSize(((RectTransform)transform).rect.size,g.spacing.x);g.cellSize=new Vector2(size,size);}
    }
}
