using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public static class SettingsScreen
    {
        public static void Build(RectTransform root,ScreenContext c)
        {
            var w=c.Widgets;w.Header(root,c,"設定",UIWidgets.Mint);
            bool canReturnTitle=c.Flow.SettingsOpenedFromGame;
            // Fixed content height with vertical scrolling on short safe areas.
            var viewport=UIWidgets.Node("Settings viewport",root,new Rect(0,0,1,1));viewport.offsetMin=new Vector2(12,canReturnTitle?158:112);viewport.offsetMax=new Vector2(-12,-78);
            // Draw the card on its content, so tall displays show the town below
            // rather than an oversized empty cream viewport. Short displays scroll.
            var capture=viewport.gameObject.AddComponent<Image>();capture.color=Color.clear;capture.raycastTarget=true;viewport.gameObject.AddComponent<RectMask2D>();
            bool canReselect=Mofumachi.Core.CharacterCatalog.IsKnown(c.Game.State.favoriteCharacterId);
            var content=w.OrnatePanel("Settings content",viewport,new Rect(0,1,1,0),UIWidgets.BlushPaper).rectTransform;content.pivot=new Vector2(.5f,1);content.sizeDelta=new Vector2(0,canReselect?850:780);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
            var title=w.StoryRibbon("Sound settings title","音の設定",content,new Rect(0,0,1,1),18,UIWidgets.Lavender);UIWidgets.Top((RectTransform)title.transform.parent,48,14,48,42);
            var desc=w.NarrativeLabel("Settings description","下へスクロールすると、BGMを選べます♪",content,new Rect(0,0,1,1),14);UIWidgets.Top(desc.rectTransform,20,62,20,40);
            Slider bgm=null,se=null;
            bgm=Row(content,c,"BGM",106,c.Game.State.bgmEnabled,c.Game.State.bgmVolume,()=>c.Flow.CommitAudioPreferences(!c.Game.State.bgmEnabled,c.Game.State.seEnabled,c.Game.State.bgmVolume,c.Game.State.seVolume));
            se=Row(content,c,"SE",252,c.Game.State.seEnabled,c.Game.State.seVolume,()=>c.Flow.CommitAudioPreferences(c.Game.State.bgmEnabled,!c.Game.State.seEnabled,c.Game.State.bgmVolume,c.Game.State.seVolume));
            bgm.gameObject.AddComponent<VolumeControl>().Bind(bgm,v=>c.Flow.PreviewAudioVolumes(v,se.value),v=>c.Flow.CommitAudioPreferences(c.Game.State.bgmEnabled,c.Game.State.seEnabled,v,c.Game.State.seVolume));
            se.gameObject.AddComponent<VolumeControl>().Bind(se,v=>c.Flow.PreviewAudioVolumes(bgm.value,v),v=>c.Flow.CommitAudioPreferences(c.Game.State.bgmEnabled,c.Game.State.seEnabled,c.Game.State.bgmVolume,v));
            MusicSettingsView.Build(content,c,402);
            if(canReselect)
            {
                var partner=w.Button("Reselect partner","キャラクター図鑑をひらく",content,new Rect(0,0,1,1),c.Flow.ShowCharacterSelection,true);
                UIWidgets.Top((RectTransform)partner.transform,24,786,24,48);
            }
            var feedback=w.Label("Settings feedback",c.Flow.FeedbackFor(ScreenId.Settings),root,new Rect(0,0,1,1),13);UIWidgets.Bottom(feedback.rectTransform,16,canReturnTitle?116:68,16,36);
            if(canReturnTitle)
            {
                var titleReturn=w.Button("Return title",UIStrings.TitleReturn,root,new Rect(0,0,1,1),()=>{c.Flow.PlayCue(AudioCue.Confirm);c.Flow.ReturnToTitle();});
                UIWidgets.Bottom((RectTransform)titleReturn.transform,28,68,28,48);
            }
            var back=w.Button("Return settings","戻る",root,new Rect(0,0,1,1),c.Flow.GoBack,true);UIWidgets.Bottom((RectTransform)back.transform,28,12,28,48);
            var revision=Resources.Load<TextAsset>("Mofumachi/QaBuildRevision");
            if(revision!=null){string sha=revision.text.Trim();var build=w.Label("QA build revision","街QA · "+(sha.Length>7?sha.Substring(0,7):sha),root,new Rect(0,0,1,1),9);UIWidgets.Bottom(build.rectTransform,20,0,20,10);}
        }
        private static Slider Row(RectTransform content,ScreenContext c,string name,float top,bool enabled,float volume,UnityEngine.Events.UnityAction toggle)
        {
            var w=c.Widgets;var card=w.Panel(name+" card",content,new Rect(0,0,1,1)).rectTransform;UIWidgets.Top(card,12,top,12,136);
            w.Heading(name+" title",name,card,new Rect(.04f,.57f,.35f,.39f),23);
            var b=w.Button(name+" toggle",enabled?"ON":"OFF",card,new Rect(.57f,.57f,.38f,.39f),toggle,!enabled);b.GetComponentInChildren<Text>().fontSize=17;
            var badge=w.Panel(name+" percent badge",card,new Rect(.76f,.12f,.2f,.31f),new Color(1,.94f,.96f)).rectTransform;
            var value=w.Label(name+" percent",Mathf.RoundToInt(volume*100)+"%",badge,new Rect(0,0,1,1),15);
            var rect=UIWidgets.Node(name+" volume",card,new Rect(.05f,.07f,.69f,.42f));var ray=rect.gameObject.AddComponent<Image>();ray.color=Color.clear;
            var slider=rect.gameObject.AddComponent<Slider>();slider.minValue=0;slider.maxValue=1;slider.SetValueWithoutNotify(volume);
            var track=w.Panel(name+" track",rect,new Rect(.02f,.43f,.96f,.14f),new Color(.88f,.92f,.91f));track.decorated=false;
            var fillArea=UIWidgets.Node(name+" fill area",rect,new Rect(.02f,.43f,.96f,.14f));
            var fill=w.Panel(name+" fill",fillArea,new Rect(0,0,1,1),UIWidgets.Mint);slider.fillRect=fill.rectTransform;
            fill.decorated=false;
            var handleArea=UIWidgets.Node(name+" handle area",rect,new Rect(.07f,0,.86f,1));var handle=w.Panel(name+" handle",handleArea,new Rect(0,.28f,0,.44f),UIWidgets.Pink);handle.rectTransform.sizeDelta=new Vector2(28,0);slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;
            w.Icon(name+" handle jewel",UIIcon.Star,handle.rectTransform,new Rect(.2f,.15f,.6f,.7f));
            slider.onValueChanged.AddListener(v=>value.text=Mathf.RoundToInt(v*100)+"%");return slider;
        }
    }
}
