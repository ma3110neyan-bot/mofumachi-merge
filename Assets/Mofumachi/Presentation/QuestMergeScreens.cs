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
            bool finished=quest==null,active=!finished && c.Game.State.activeQuestId==quest.Id && !c.Game.State.completedQuestIds.Contains(quest.Id);
            var ribbon=w.Ribbon("Quest title",finished?"すべての依頼が完了しました":quest.Title,area,new Rect(.12f,.83f,.76f,.11f),18,UIWidgets.Peach);UIWidgets.Top((RectTransform)ribbon.transform.parent,24,16,24,44);
            var requester=CharacterCatalog.Find(finished?c.Game.State.favoriteCharacterId:quest.RequesterId);
            w.Character("Quest character",requester.PortraitIndex,area,new Rect(.35f,.61f,.3f,.19f));
            w.Label("Requester",requester.Name,area,new Rect(.25f,.55f,.5f,.06f),13);
            w.Label("Quest instruction",finished?"ありがとう！ 街の仲間とお祝いしよう。":quest.Dialogue,area,new Rect(.07f,.42f,.86f,.12f),13);
            if(!finished)
            {
                var requirements=c.Game.Quests.GetProgress(quest);
                for(int i=0;i<requirements.Count;i++)
                {
                    var entry=requirements[i];var item=entry.Requirement;float width=.88f/requirements.Count;
                    var required=w.OrnatePanel("Required item "+i,area,new Rect(.06f+i*width,.23f,width-.02f,.17f),UIWidgets.BluePaper,12).rectTransform;
                    w.Item("Required art "+i,item.ItemId,item.Level,required,new Rect(.03f,.1f,.27f,.8f));
                    w.Label("Required count "+i,ItemCatalog.Find(item.ItemId).Name+" Lv."+item.Level+"\n"+System.Math.Min(entry.Available,item.Count)+" / "+item.Count,required,new Rect(.31f,.04f,.65f,.9f),12);
                }
                w.Icon("Reward coin",UIIcon.Coin,area,new Rect(.23f,.16f,.09f,.06f));
                w.Heading("Reward","報酬 "+quest.Coins+" Coins",area,new Rect(.33f,.15f,.56f,.07f),16);
            }
            var accept=w.Button("Accept quest",finished?"街へ戻る":active?"依頼を進める":"依頼を受ける",area,new Rect(0,0,1,1),()=>{c.Flow.PlayCue(AudioCue.Confirm);if(finished)c.Flow.ShowHome();else if(active)c.Flow.ShowMerge();else c.Flow.AcceptQuest();});
            UIWidgets.Bottom((RectTransform)accept.transform,20,16,20,52);
            w.Feedback(root,c,ScreenId.Quest);w.Nav(root,c,ScreenId.Quest);
        }
        public static void BuildMerge(RectTransform root,ScreenContext c)
        {
            var w=c.Widgets;w.Header(root,c,"マージ",UIWidgets.Sky);
            var progress=w.Panel("Merge progress",root,new Rect(0,0,1,1),UIWidgets.BluePaper).rectTransform;UIWidgets.Top(progress,12,68,12,38);
            var quest=QuestCatalog.NextFor(c.Game.State);
            string summary=quest==null?"すべての依頼が完了しました":c.Game.State.activeQuestId!=quest.Id?"依頼を受けて、アイテムを届けよう":string.Join("　",c.Game.Quests.GetProgress(quest).Select(p=>ItemCatalog.Find(p.Requirement.ItemId).Name+" Lv."+p.Requirement.Level+" "+System.Math.Min(p.Available,p.Requirement.Count)+" / "+p.Requirement.Count));
            w.Label("Progress",summary,progress,new Rect(.03f,0,.94f,1),12);
            var tray=w.Panel("Merge tray",root,new Rect(0,0,1,1),new Color(.73f,.92f,.84f)).rectTransform;tray.offsetMin=new Vector2(10,158);tray.offsetMax=new Vector2(-10,-112);
            w.Frame(tray,22,false);
            var grid=UIWidgets.Node("Merge board",tray,new Rect(0,0,1,1));grid.offsetMin=new Vector2(7,7);grid.offsetMax=new Vector2(-7,-7);
            var group=grid.gameObject.AddComponent<GridLayoutGroup>();group.constraint=GridLayoutGroup.Constraint.FixedColumnCount;group.constraintCount=5;group.spacing=new Vector2(4,4);group.childAlignment=TextAnchor.MiddleCenter;
            for(int i=0;i<30;i++)
            {
                int index=i;var item=c.Game.Board.At(i);var b=w.Button("Cell "+i,"",grid,new Rect(0,0,1,1),()=>{} ,true);
                var surface=b.GetComponent<RoundedPanelGraphic>();surface.radius=13;surface.borderWidth=.8f;surface.gloss=.12f;
                surface.color=c.Game.Board.IsLocked(i)?new Color(1,.89f,.59f):index==c.Flow.SelectedCell?new Color(1,.69f,.8f):UIWidgets.Cream;
                var drag=b.gameObject.AddComponent<MergeBoardView>();drag.Initialize(c.Flow,i);
                b.onClick.AddListener(()=>{if(!drag.SuppressClick)c.Flow.TapCell(index);});
                if(item!=null){if(ItemCatalog.Find(item.itemId)!=null)w.Item("Item art",item.itemId,item.level,(RectTransform)b.transform,new Rect(.09f,.36f,.82f,.59f));else w.Heading("Unknown item","?",(RectTransform)b.transform,new Rect(.09f,.36f,.82f,.59f),20);w.Heading("Item level","Lv."+item.level,(RectTransform)b.transform,new Rect(.02f,.015f,.96f,.36f),11);}
            }
            grid.gameObject.AddComponent<MergeGridLayout>();
            var toolbar=UIWidgets.Node("Merge toolbar",root,new Rect(0,0,1,1));UIWidgets.Bottom(toolbar,12,106,12,48);
            var menu=w.Button("Merge actions","…",toolbar,new Rect(0,0,0,1),c.Flow.OpenMergeActions,true);((RectTransform)menu.transform).sizeDelta=new Vector2(48,0);
            var generate=w.Button("Create tea",ItemCatalog.Find(c.Flow.ProductionItemId).Name+"を作る",toolbar,new Rect(0,0,.56f,1),()=>c.Flow.GenerateItem(c.Flow.ProductionItemId),true);((RectTransform)generate.transform).offsetMin=new Vector2(52,0);
            var delivery=w.Button("Deliver","納品する",toolbar,new Rect(.59f,0,.41f,1),()=>c.Flow.Deliver());delivery.interactable=c.Game.Quests.CheckDelivery();
            w.Feedback(root,c,ScreenId.Merge); // Compact feedback between navigation and toolbar.
            var text=root.Find("Feedback").GetComponent<Text>();UIWidgets.Bottom(text.rectTransform,14,76,14,28);
            var feedbackSurface=root.Find("Feedback surface") as RectTransform;if(feedbackSurface!=null)UIWidgets.Bottom(feedbackSurface,12,76,12,28);
            w.Nav(root,c,ScreenId.Merge);
        }
    }
    public sealed class MergeGridLayout : MonoBehaviour
    {
        private void Start()=>Resize();private void OnRectTransformDimensionsChange()=>Resize();
        private void Resize(){var g=GetComponent<GridLayoutGroup>();if(g==null)return;float size=ResponsiveUILayout.CellSize(((RectTransform)transform).rect.size,g.spacing.x);g.cellSize=new Vector2(size,size);}
    }
}
