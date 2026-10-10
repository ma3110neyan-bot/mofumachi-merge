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
            var w=c.Widgets;w.Header(root,c,"キャラ依頼");
            var area=w.Panel("Quest card",root,new Rect(0,0,1,1)).rectTransform;area.offsetMin=new Vector2(14,128);area.offsetMax=new Vector2(-14,-80);
            w.Flowers(area);
            w.Character("Quest character",1,area,new Rect(.35f,.66f,.3f,.28f));
            w.Heading("Quest title","お茶会の準備",area,new Rect(.04f,.51f,.92f,.12f),23);
            var required=w.Panel("Required item",area,new Rect(.065f,.32f,.87f,.17f),new Color(.94f,.985f,.96f)).rectTransform;
            w.Icon("Required tea",UIIcon.TeaPot,required,new Rect(.03f,.1f,.22f,.8f));
            long count=c.Game.State.inventory.Where(i=>i.itemId=="tea"&&i.level==2).Sum(i=>(long)i.count)+c.Game.State.mergeBoard.LongCount(i=>i.itemId=="tea"&&i.level==2&&!c.Game.Board.IsLocked(i.cellIndex));
            bool done=c.Game.State.completedQuestIds.Contains(QuestDefinition.First.Id), active=c.Game.State.activeQuestId.Length>0;
            w.Label("Required count",done?"お茶を届けました！":"お茶 Lv.2　"+System.Math.Min(count,1)+" / 1",required,new Rect(.27f,.08f,.69f,.84f),16);
            w.Icon("Reward coin",UIIcon.Coin,area,new Rect(.23f,.22f,.09f,.08f));
            w.Heading("Reward","報酬 "+QuestDefinition.First.Coins+" Coins",area,new Rect(.33f,.22f,.5f,.08f),17);
            var accept=w.Button("Accept quest",done?"依頼完了":active?"依頼を進める":"依頼を受ける",area,new Rect(.06f,.02f,.88f,.18f),()=>{c.Flow.PlayCue(AudioCue.Confirm);if(active)c.Flow.ShowMerge();else c.Flow.AcceptQuest();});
            accept.interactable=!done;
            UIWidgets.Bottom((RectTransform)accept.transform,20,16,20,52);
            w.Feedback(root,c,ScreenId.Quest);w.Nav(root,c,ScreenId.Quest);
        }
        public static void BuildMerge(RectTransform root,ScreenContext c)
        {
            var w=c.Widgets;w.Header(root,c,"マージ");
            var progress=w.Panel("Merge progress",root,new Rect(0,0,1,1)).rectTransform;UIWidgets.Top(progress,12,68,12,38);
            w.Label("Progress",c.Game.State.completedQuestIds.Contains(QuestDefinition.First.Id)?"お茶会の準備は完了しました":c.Game.State.activeQuestId.Length==0?"キャラ依頼を受けてお茶を届けよう":"お茶 Lv.2　"+c.Game.State.questProgress+" / 1",progress,new Rect(.03f,0,.94f,1),14);
            var tray=w.Panel("Merge tray",root,new Rect(0,0,1,1),new Color(.73f,.92f,.84f)).rectTransform;tray.offsetMin=new Vector2(10,158);tray.offsetMax=new Vector2(-10,-112);
            var grid=UIWidgets.Node("Merge board",tray,new Rect(0,0,1,1));grid.offsetMin=new Vector2(7,7);grid.offsetMax=new Vector2(-7,-7);
            var group=grid.gameObject.AddComponent<GridLayoutGroup>();group.constraint=GridLayoutGroup.Constraint.FixedColumnCount;group.constraintCount=5;group.spacing=new Vector2(4,4);group.childAlignment=TextAnchor.MiddleCenter;
            for(int i=0;i<30;i++)
            {
                int index=i;var item=c.Game.Board.At(i);var b=w.Button("Cell "+i,"",grid,new Rect(0,0,1,1),()=>{} ,true);
                var surface=b.GetComponent<RoundedPanelGraphic>();surface.radius=13;surface.borderWidth=.8f;surface.gloss=.12f;
                surface.color=c.Game.Board.IsLocked(i)?new Color(1,.89f,.59f):index==c.Flow.SelectedCell?new Color(1,.69f,.8f):UIWidgets.Cream;
                var drag=b.gameObject.AddComponent<MergeBoardView>();drag.Initialize(c.Flow,i);
                b.onClick.AddListener(()=>{if(!drag.SuppressClick)c.Flow.TapCell(index);});
                if(item!=null){w.Icon("Tea",item.level>=2?UIIcon.TeaPot:UIIcon.Tea,(RectTransform)b.transform,new Rect(.09f,.4f,.82f,.55f));w.Heading("Tea level","Lv."+item.level,(RectTransform)b.transform,new Rect(.02f,.015f,.96f,.36f),11);}
            }
            grid.gameObject.AddComponent<MergeGridLayout>();
            var toolbar=UIWidgets.Node("Merge toolbar",root,new Rect(0,0,1,1));UIWidgets.Bottom(toolbar,12,106,12,48);
            w.Button("Create tea","お茶を作る",toolbar,new Rect(0,0,.56f,1),c.Flow.CreateTea,true);
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
