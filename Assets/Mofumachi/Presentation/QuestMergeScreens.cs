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
            w.Character("Quest character",1,area,new Rect(.3f,.62f,.4f,.33f));
            w.Label("Quest title","お茶会の準備",area,new Rect(.04f,.5f,.92f,.1f),24);
            w.Icon("Required tea",UIIcon.Tea,area,new Rect(.13f,.32f,.15f,.13f),new Color(.36f,.64f,.49f));
            long count=c.Game.State.inventory.Where(i=>i.itemId=="tea"&&i.level==2).Sum(i=>(long)i.count)+c.Game.State.mergeBoard.LongCount(i=>i.itemId=="tea"&&i.level==2&&!c.Game.Board.IsLocked(i.cellIndex));
            bool done=c.Game.State.completedQuestIds.Contains(QuestDefinition.First.Id), active=c.Game.State.activeQuestId.Length>0;
            w.Label("Required count",done?"お茶を届けました！":"お茶 Lv.2　"+System.Math.Min(count,1)+" / 1",area,new Rect(.3f,.31f,.66f,.14f),18);
            w.Label("Reward","報酬 30 Coins",area,new Rect(.05f,.23f,.9f,.08f),20);
            var accept=w.Button("Accept quest",done?"依頼完了":active?"依頼を進める":"依頼を受ける",area,new Rect(.06f,.02f,.88f,.18f),()=>{c.Flow.PlayCue(AudioCue.Confirm);if(active)c.Flow.ShowMerge();else c.Flow.AcceptQuest();});
            accept.interactable=!done;
            w.Feedback(root,c,ScreenId.Quest);w.Nav(root,c,ScreenId.Quest);
        }
        public static void BuildMerge(RectTransform root,ScreenContext c)
        {
            var w=c.Widgets;w.Header(root,c,"Merge");
            var progress=w.Panel("Merge progress",root,new Rect(0,0,1,1)).rectTransform;UIWidgets.Top(progress,12,68,12,38);
            w.Label("Progress",c.Game.State.completedQuestIds.Contains(QuestDefinition.First.Id)?"お茶会の準備は完了しました":c.Game.State.activeQuestId.Length==0?"キャラ依頼を受けてお茶を届けよう":"お茶 Lv.2　"+c.Game.State.questProgress+" / 1",progress,new Rect(.03f,0,.94f,1),14);
            var grid=UIWidgets.Node("Merge board",root,new Rect(0,0,1,1));grid.offsetMin=new Vector2(12,160);grid.offsetMax=new Vector2(-12,-112);
            var group=grid.gameObject.AddComponent<GridLayoutGroup>();group.constraint=GridLayoutGroup.Constraint.FixedColumnCount;group.constraintCount=5;group.spacing=new Vector2(6,6);group.childAlignment=TextAnchor.MiddleCenter;
            for(int i=0;i<30;i++)
            {
                int index=i;var item=c.Game.Board.At(i);var b=w.Button("Cell "+i,"",grid,new Rect(0,0,1,1),()=>{} ,true);
                b.GetComponent<Image>().color=c.Game.Board.IsLocked(i)?new Color(1,.86f,.5f):index==c.Flow.SelectedCell?new Color(1,.76f,.83f):new Color(.86f,.94f,.86f);
                var drag=b.gameObject.AddComponent<MergeBoardView>();drag.Initialize(c.Flow,i);
                b.onClick.AddListener(()=>{if(!drag.SuppressClick)c.Flow.TapCell(index);});
                if(item!=null){w.Icon("Tea",UIIcon.Tea,(RectTransform)b.transform,new Rect(.28f,.35f,.44f,.5f),new Color(.32f,.59f,.46f));w.Label("Tea level","Lv."+item.level,(RectTransform)b.transform,new Rect(.02f,.01f,.96f,.31f),13);}
            }
            grid.gameObject.AddComponent<MergeGridLayout>();
            var toolbar=UIWidgets.Node("Merge toolbar",root,new Rect(0,0,1,1));UIWidgets.Bottom(toolbar,12,106,12,48);
            w.Button("Create tea","お茶を作る",toolbar,new Rect(0,0,.56f,1),c.Flow.CreateTea,true);
            var delivery=w.Button("Deliver","納品する",toolbar,new Rect(.59f,0,.41f,1),()=>c.Flow.Deliver());delivery.interactable=c.Game.Quests.CheckDelivery();
            w.Feedback(root,c,ScreenId.Merge); // Compact feedback between navigation and toolbar.
            var text=root.Find("Feedback").GetComponent<Text>();UIWidgets.Bottom(text.rectTransform,14,76,14,28);
            w.Nav(root,c,ScreenId.Merge);
        }
    }
    public sealed class MergeGridLayout : MonoBehaviour
    {
        private void Start()=>Resize();private void OnRectTransformDimensionsChange()=>Resize();
        private void Resize(){var g=GetComponent<GridLayoutGroup>();if(g==null)return;float size=ResponsiveUILayout.CellSize(((RectTransform)transform).rect.size);g.cellSize=new Vector2(size,size);}
    }
}
