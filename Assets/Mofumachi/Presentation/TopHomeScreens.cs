using UnityEngine;
using UnityEngine.UI;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    public static class TopHomeScreens
    {
        public static void BuildTitle(RectTransform root,ScreenContext c)
        {
            var w=c.Widgets;
            var settings=w.Button("Settings","",root,new Rect(.82f,.91f,.14f,.07f),c.Flow.ShowSettings,true); var sr=(RectTransform)settings.transform;sr.anchorMin=sr.anchorMax=Vector2.one;sr.pivot=Vector2.one;sr.anchoredPosition=new Vector2(-12,-10);sr.sizeDelta=new Vector2(48,48);
            w.Icon("Settings icon",UIIcon.Settings,(RectTransform)settings.transform,new Rect(.26f,.26f,.48f,.48f));
            var logo=w.Panel("Logo",root,new Rect(.1f,.65f,.8f,.23f),new Color(1,1,1,.88f)).rectTransform;
            var line1=w.Label("Logo first","もふまち",logo,new Rect(0,.46f,1,.48f),40); line1.color=new Color(.88f,.35f,.52f);
            var line2=w.Label("Logo second","メルジュ",logo,new Rect(0,.02f,1,.47f),40); line2.color=new Color(.27f,.64f,.53f);
            var tag=w.Panel("Tagline",root,new Rect(.08f,.585f,.84f,.057f),new Color(1,1,1,.85f)).rectTransform;
            w.Label("Tagline text","もふもふの街で、ちいさなお茶会を。",tag,new Rect(0,0,1,1),14);
            w.Character("Character 0",0,root,new Rect(.29f,.32f,.42f,.24f));
            for(int i=1;i<6;i++)w.Character("Character "+i,i,root,new Rect(.075f+(i-1)*.174f,.19f,.155f,.09f));
            var start=w.Button("Start","はじめる",root,new Rect(0,0,1,1),()=>{c.Flow.PlayCue(AudioCue.Confirm);c.Flow.BeginGame();}); UIWidgets.Bottom((RectTransform)start.transform,28,70,28,58);
            var footer=w.Label("Title footer","つくって、届けて、街を育てよう。",root,new Rect(0,0,1,1),14); UIWidgets.Bottom(footer.rectTransform,14,22,14,36);
            var feedback=w.Label("Title feedback",c.Flow.FeedbackFor(ScreenId.Title),root,new Rect(.05f,.13f,.9f,.055f),12);
        }
        public static void BuildHome(RectTransform root,ScreenContext c)
        {
            var w=c.Widgets;
            var hud=w.Panel("HUD",root,new Rect(0,0,1,1),new Color(1,.98f,.92f,.96f)).rectTransform; UIWidgets.Top(hud,10,8,10,70);
            w.Label("Town value","街 Lv."+c.Game.State.townGrowthLevel,hud,new Rect(.02f,.48f,.77f,.48f),16);
            w.Icon("Coin",UIIcon.Coin,hud,new Rect(.04f,.12f,.05f,.24f),new Color(.9f,.62f,.12f));
            w.Label("Coins value",c.Game.State.coins.ToString("N0"),hud,new Rect(.11f,.01f,.67f,.46f),19);
            var settings=w.Button("Settings","",hud,new Rect(.82f,.12f,.16f,.72f),c.Flow.ShowSettings,true); w.Icon("Settings icon",UIIcon.Settings,(RectTransform)settings.transform,new Rect(.27f,.27f,.46f,.46f));
            var points=new[]{new Vector2(.13f,.66f),new Vector2(.64f,.68f),new Vector2(.36f,.52f),new Vector2(.7f,.4f),new Vector2(.08f,.36f),new Vector2(.42f,.29f)};
            for(int i=0;i<6;i++){var pos=points[i];var image=w.Character("Character "+i,i,root,new Rect(pos.x,pos.y,.19f,.105f));var b=image.transform.parent.gameObject.AddComponent<Button>(); var g=b.GetComponent<RoundedPanelGraphic>();g.raycastTarget=true;b.targetGraphic=g;b.onClick.AddListener(()=>c.Flow.PlayCue(AudioCue.Character));}
            bool complete=c.Game.State.completedQuestIds.Contains(QuestDefinition.First.Id);
            var quest=w.Panel("Home quest",root,new Rect(0,0,1,1)).rectTransform; UIWidgets.Bottom(quest,14,124,14,92);
            w.Character("Quest character",1,quest,new Rect(.025f,.13f,.2f,.74f));
            w.Label("Home quest title","お茶会の準備",quest,new Rect(.24f,.54f,.72f,.38f),20);
            w.Label("Home quest summary",complete?"お茶会の準備ができました！":"お茶を合成して、仲間に届けよう",quest,new Rect(.24f,.08f,.72f,.44f),13);
            var tap=quest.gameObject.AddComponent<Button>();quest.GetComponent<RoundedPanelGraphic>().raycastTarget=true;tap.targetGraphic=quest.GetComponent<RoundedPanelGraphic>();tap.onClick.AddListener(c.Flow.ShowQuest);
            w.Feedback(root,c,ScreenId.Home); w.Nav(root,c,ScreenId.Home);
        }
    }
}
