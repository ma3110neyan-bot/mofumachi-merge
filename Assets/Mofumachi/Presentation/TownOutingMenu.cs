using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Mofumachi.Presentation
{
    // Home-only, no persistence. A compact two-column card fits the short portrait
    // safe area; scenery browsing is deliberately separate from walking.
    public sealed class TownOutingMenu : MonoBehaviour
    {
        public static GameObject Open(ScreenContext c,TownLifeView life,RectTransform root)
        {
            var overlay=UIWidgets.Node("Town outing choices",root,new Rect(0,0,1,1));
            UIWidgets.Top(overlay,0,58,0,Mathf.Max(1,root.rect.height-136));
            var shade=overlay.gameObject.AddComponent<Image>();shade.color=new Color(.23f,.17f,.29f,.26f);shade.raycastTarget=true;
            var card=c.Widgets.OrnatePanel("Outing card",overlay,new Rect(.5f,.5f,0,0),UIWidgets.Cream,18).rectTransform;
            card.sizeDelta=new Vector2(Mathf.Min(340,root.rect.width-24),Mathf.Min(344,root.rect.height-144));
            card.GetComponent<RoundedPanelGraphic>().raycastTarget=true;
            var name=CharacterCatalog.Find(c.Game.State.favoriteCharacterId).Name;
            c.Widgets.StoryHeading("Outing heading",name+"と、どこへ行こう？",card,new Rect(0,0,1,1),18);
            UIWidgets.Top((RectTransform)card.Find("Outing heading"),12,10,12,30);
            var hint=c.Widgets.Label("Outing hint","入口まで歩いて、お隣の街へ♪",card,new Rect(0,0,1,1),13);UIWidgets.Top(hint.rectTransform,10,42,10,24);
            var viewport=UIWidgets.Node("Outing viewport",card,new Rect(0,0,1,1));UIWidgets.Top(viewport,10,72,10,168);
            viewport.gameObject.AddComponent<RectMask2D>();var capture=viewport.gameObject.AddComponent<Image>();capture.color=Color.clear;
            var choices=UIWidgets.Node("Outing destinations",viewport,new Rect(0,0,1,1));choices.pivot=new Vector2(.5f,1);UIWidgets.Top(choices,0,0,0,life.TownLevel>=2?224:168);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=choices;scroll.horizontal=false;scroll.vertical=life.TownLevel>=2;scroll.movementType=ScrollRect.MovementType.Clamped;
            void Walk(string id){if(life.GuideToPlace(id))life.DismissOutings();}
            void Add(string id,string text,int column,int row,UnityEngine.Events.UnityAction action)
            {
                var b=c.Widgets.Button(id,text,choices,new Rect(column*.5f,0,.5f,1),action,true);
                var r=(RectTransform)b.transform;r.anchorMin=new Vector2(column*.5f,1);r.anchorMax=new Vector2((column+1)*.5f,1);
                r.offsetMin=new Vector2(3,-row*56-52);r.offsetMax=new Vector2(-3,-row*56);b.GetComponentInChildren<Text>().fontSize=14;
            }
            Add("Visit west cafe",life.TownLevel>=2?"西のカフェへ":"西のカフェのお庭へ",0,0,()=>Walk(TownOutingDestinations.ForRegion(TownRegionId.West,life.TownLevel)));
            Add("Visit central fountain","中央広場の噴水へ",1,0,()=>Walk(TownOutingDestinations.ForRegion(TownRegionId.Central,life.TownLevel)));
            Add("Visit east flowers","東の花屋通りへ",0,1,()=>Walk(TownOutingDestinations.ForRegion(TownRegionId.East,life.TownLevel)));
            Add("Visit bench","近くのベンチへ",1,1,()=>{if(life.GuideToRestStop("bench"))life.DismissOutings();});
            Add("Guide to quest","西の依頼小屋へ",0,2,()=>Walk("quest"));
            Add("Guide to shop","東のお花ショップへ",1,2,()=>Walk("shop"));
            if(life.TownLevel>=2)
            {
                string place=life.TownLevel>=6?"celebration":life.TownLevel>=4?"front-promenade":"flower-walk";
                Add("Expanded outing",life.TownLevel>=6?"お祝いの道へ":life.TownLevel>=4?"散歩道の先へ":"育った花の小道へ",0,3,()=>Walk(place));
                ((RectTransform)choices.GetChild(choices.childCount-1)).anchorMax=Vector2.one;hint.text="行き先を選ぼう · 下にもスクロールできます";
            }
            var browse=c.Widgets.Button("Browse scenery","景色だけ見る",card,new Rect(.03f,0,.59f,1),()=>
            {
                foreach(Transform t in choices)t.gameObject.SetActive(false);
                UIWidgets.Top(choices,0,0,0,168);scroll.vertical=false;scroll.verticalNormalizedPosition=1;
                hint.text="主人公は動かず、景色を眺めます";
                for(int i=0;i<3;i++)
                {
                    var region=(TownRegionId)i;
                    Add("Region preview "+region,i==0?"西の庭を眺める":i==1?"中央広場を眺める":"東の通りを眺める",0,i,()=>{life.Regions.LookAtRegion(region);life.DismissOutings();});
                    var preview=(RectTransform)choices.GetChild(choices.childCount-1);preview.anchorMax=Vector2.one;
                }
                card.Find("Browse scenery").GetComponent<Button>().interactable=false;
            },true);
            UIWidgets.Bottom((RectTransform)browse.transform,10,12,card.sizeDelta.x*.4f,48);browse.GetComponentInChildren<Text>().fontSize=14;
            var close=c.Widgets.Button("Close outings","閉じる",card,new Rect(0,0,1,1),()=>life.DismissOutings(),true);
            UIWidgets.Bottom((RectTransform)close.transform,card.sizeDelta.x*.62f,12,10,48);close.GetComponentInChildren<Text>().fontSize=14;
            return overlay.gameObject;
        }
    }
}
