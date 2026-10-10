using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public static class SettingsScreen
    {
        public static void Build(RectTransform root,ScreenContext c)
        {
            var w=c.Widgets;w.Header(root,c,"設定");
            // Fixed content height with vertical scrolling on short safe areas.
            var viewport=UIWidgets.Node("Settings viewport",root,new Rect(0,0,1,1));viewport.offsetMin=new Vector2(12,112);viewport.offsetMax=new Vector2(-12,-78);
            var bg=viewport.gameObject.AddComponent<RoundedPanelGraphic>();bg.color=UIWidgets.Cream;viewport.gameObject.AddComponent<RectMask2D>();
            var content=UIWidgets.Node("Settings content",viewport,new Rect(0,1,1,0));content.pivot=new Vector2(.5f,1);content.sizeDelta=new Vector2(0,410);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
            var desc=w.Label("Settings description","音の大きさを、お好みに合わせて調整できます。",content,new Rect(0,0,1,1),16);UIWidgets.Top(desc.rectTransform,16,16,16,56);
            Slider bgm=null,se=null;
            bgm=Row(content,c,"BGM",84,c.Game.State.bgmEnabled,c.Game.State.bgmVolume,()=>c.Flow.CommitAudioPreferences(!c.Game.State.bgmEnabled,c.Game.State.seEnabled,c.Game.State.bgmVolume,c.Game.State.seVolume));
            se=Row(content,c,"SE",234,c.Game.State.seEnabled,c.Game.State.seVolume,()=>c.Flow.CommitAudioPreferences(c.Game.State.bgmEnabled,!c.Game.State.seEnabled,c.Game.State.bgmVolume,c.Game.State.seVolume));
            bgm.gameObject.AddComponent<VolumeControl>().Bind(bgm,v=>c.Flow.PreviewAudioVolumes(v,se.value),v=>c.Flow.CommitAudioPreferences(c.Game.State.bgmEnabled,c.Game.State.seEnabled,v,c.Game.State.seVolume));
            se.gameObject.AddComponent<VolumeControl>().Bind(se,v=>c.Flow.PreviewAudioVolumes(bgm.value,v),v=>c.Flow.CommitAudioPreferences(c.Game.State.bgmEnabled,c.Game.State.seEnabled,c.Game.State.bgmVolume,v));
            var feedback=w.Label("Settings feedback",c.Flow.FeedbackFor(ScreenId.Settings),root,new Rect(0,0,1,1),13);UIWidgets.Bottom(feedback.rectTransform,16,68,16,40);
            var back=w.Button("Return settings","戻る",root,new Rect(0,0,1,1),c.Flow.GoBack,true);UIWidgets.Bottom((RectTransform)back.transform,28,12,28,48);
        }
        private static Slider Row(RectTransform content,ScreenContext c,string name,float top,bool enabled,float volume,UnityEngine.Events.UnityAction toggle)
        {
            var w=c.Widgets;var card=w.Panel(name+" card",content,new Rect(0,0,1,1)).rectTransform;UIWidgets.Top(card,12,top,12,136);
            w.Heading(name+" title",name,card,new Rect(.04f,.57f,.35f,.39f),23);
            var b=w.Button(name+" toggle",enabled?"ON":"OFF",card,new Rect(.57f,.57f,.38f,.39f),toggle,!enabled);b.GetComponentInChildren<Text>().fontSize=17;
            var value=w.Label(name+" percent",Mathf.RoundToInt(volume*100)+"%",card,new Rect(.76f,.07f,.2f,.42f),16);
            var rect=UIWidgets.Node(name+" volume",card,new Rect(.05f,.07f,.69f,.42f));var ray=rect.gameObject.AddComponent<Image>();ray.color=Color.clear;
            var slider=rect.gameObject.AddComponent<Slider>();slider.minValue=0;slider.maxValue=1;slider.SetValueWithoutNotify(volume);
            var track=w.Panel(name+" track",rect,new Rect(.02f,.43f,.96f,.14f),new Color(.88f,.92f,.91f));track.decorated=false;
            var fillArea=UIWidgets.Node(name+" fill area",rect,new Rect(.02f,.43f,.96f,.14f));
            var fill=w.Panel(name+" fill",fillArea,new Rect(0,0,1,1),UIWidgets.Mint);slider.fillRect=fill.rectTransform;
            fill.decorated=false;
            var handleArea=UIWidgets.Node(name+" handle area",rect,new Rect(.07f,0,.86f,1));var handle=w.Panel(name+" handle",handleArea,new Rect(0,.18f,0,.64f),UIWidgets.Pink);handle.rectTransform.sizeDelta=new Vector2(28,0);slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;
            slider.onValueChanged.AddListener(v=>value.text=Mathf.RoundToInt(v*100)+"%");return slider;
        }
    }
}
