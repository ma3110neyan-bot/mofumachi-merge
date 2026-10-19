using System;
using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // All occasional street actions live here; the default street remains open.
    public static class TownMenuView
    {
        public static void Build(RectTransform root,ScreenContext c)
        {
            var w=c.Widgets;
            var overlay=w.Panel("Town menu overlay",root,new Rect(0,0,1,1),new Color(.22f,.18f,.24f,.44f)).rectTransform;
            overlay.GetComponent<RoundedPanelGraphic>().raycastTarget=true;
            var card=w.StoryPanel("Town menu card",overlay,new Rect(.5f,.5f,0,0)).rectTransform;
            card.sizeDelta=new Vector2(Mathf.Min(340,root.rect.width-20),Mathf.Min(506,root.rect.height-20));
            var title=w.StoryRibbon("Town menu title","街のてちょう",card,new Rect(0,0,1,1),18);UIWidgets.Top((RectTransform)title.transform.parent,18,12,18,36);
            var viewport=UIWidgets.Node("Town menu viewport",card,new Rect(0,0,1,1));viewport.offsetMin=new Vector2(12,70);viewport.offsetMax=new Vector2(-12,-54);viewport.gameObject.AddComponent<RectMask2D>();
            var content=UIWidgets.Node("Town menu content",viewport,new Rect(0,1,1,0));content.pivot=new Vector2(.5f,1);content.sizeDelta=new Vector2(0,506);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
            var ray=viewport.gameObject.AddComponent<Image>();ray.color=new Color(1,1,1,.001f);
            void Button(string name,string text,float x,float y,float width,Action action,bool secondary=true)
            {
                var b=w.Button(name,text,content,new Rect(x,1,0,0),()=>{c.Flow.CloseTownMenu();action();},secondary);
                b.GetComponent<ButtonPressFeedback>().PreserveHitbox=true;
                var r=(RectTransform)b.transform;r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x*content.rect.width,-y);r.sizeDelta=new Vector2(width,48);b.GetComponentInChildren<Text>().fontSize=13;
            }
            var life=c.Flow.GetComponentInChildren<TownLifeView>();float width=card.sizeDelta.x-24,gap=6,half=(width-gap)/2,third=(width-gap*2)/3;
            w.StoryLabel("Fast travel help","すぐ行く · ファストトラベル",content,new Rect(0,1,1,0),12).rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top,0,22);
            string[] places={"西のカフェ","中央広場","東の花屋","北の依頼小屋","南の庭園"};
            foreach(var district in TownDistrictLayout.All)
            {
                int i=(int)district;float x=district==TownDistrictId.West?0:district==TownDistrictId.East?2*(third+gap):third+gap;
                float y=district==TownDistrictId.North?26:district==TownDistrictId.South?134:80;
                Button("Region walk "+district,places[i],0,y,third,()=>life.World.FastTravel(district));((RectTransform)content.Find("Region walk "+district)).anchoredPosition=new Vector2(x,-y);
            }
            Button("Walk to cafe","カフェまで歩く",0,194,half,()=>{if(life.TownLevel>=2)life.World.GuidePlace("west.cafe-front");else life.World.GuidePoint(TownWorldMap.Place("west.cafe-front"));});
            Button("Nav Quest","依頼小屋へ歩く",0,194,half,()=>life.GuideToFacility("quest"));((RectTransform)content.Find("Nav Quest")).anchoredPosition=new Vector2(half+gap,-194);
            Button("Walk to florist","花屋まで歩く",0,252,half,()=>life.GuideToFacility("shop"));
            Button("Town today","今日のお願い",0,252,half,c.Flow.ShowToday);((RectTransform)content.Find("Town today")).anchoredPosition=new Vector2(half+gap,-252);
            Button("Town book","仲間の図鑑",0,310,half,c.Flow.ShowCharacterSelection);
            Button("Nav Merge","マージ",0,310,half,c.Flow.ShowMerge,false);((RectTransform)content.Find("Nav Merge")).anchoredPosition=new Vector2(half+gap,-310);
            Button("Review chapter","物語のおもいで",0,368,half,()=>c.Flow.ReviewLastChapter());content.Find("Review chapter").GetComponent<UnityEngine.UI.Button>().interactable=ChapterProgress.For(c.Game.State).LastCompletedChapter!=null;
            Button("Settings","設定",0,368,half,c.Flow.ShowSettings);((RectTransform)content.Find("Settings")).anchoredPosition=new Vector2(half+gap,-368);
            w.NarrativeLabel("Town menu help","道をタップして、おさんぽ。\nキャラを押したままでも歩けるよ♪",content,new Rect(0,1,1,0),13).rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top,430,48);
            var close=w.Button("Close town menu","街へ戻る",card,new Rect(0,0,1,1),c.Flow.CloseTownMenu,true);UIWidgets.Bottom((RectTransform)close.transform,20,12,20,48);
        }
    }
}
