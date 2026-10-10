using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
namespace Mofumachi.Presentation
{
    public sealed class UIWidgets
    {
        public static readonly Color Ink = new Color(.32f,.18f,.29f), Pink=new Color(1,.52f,.70f), Cream=new Color(1,.988f,.945f), Mint=new Color(.54f,.86f,.74f), Sky=new Color(.67f,.88f,1);
        private readonly Font font,bold; private readonly Texture2D characters;
        public UIWidgets(Font font, Texture2D characters) { this.font=font; this.characters=characters; bold=Resources.Load<Font>("Mofumachi/UIFontBold")??font; }
        public static RectTransform Node(string name, RectTransform parent, Rect anchors)
        { var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent,false); r.anchorMin=anchors.min; r.anchorMax=anchors.max; r.offsetMin=r.offsetMax=Vector2.zero; return r; }
        public static void Top(RectTransform r,float left,float top,float right,float height)
        { r.anchorMin=new Vector2(0,1); r.anchorMax=Vector2.one; r.offsetMin=new Vector2(left,-top-height); r.offsetMax=new Vector2(-right,-top); }
        public static void Bottom(RectTransform r,float left,float bottom,float right,float height)
        { r.anchorMin=Vector2.zero; r.anchorMax=new Vector2(1,0); r.offsetMin=new Vector2(left,bottom); r.offsetMax=new Vector2(-right,bottom+height); }
        public RoundedPanelGraphic Panel(string name,RectTransform parent,Rect anchors,Color? color=null)
        { var p=Node(name,parent,anchors).gameObject.AddComponent<RoundedPanelGraphic>(); p.color=color??Cream; p.raycastTarget=false; return p; }
        public Text Label(string name,string text,RectTransform parent,Rect anchors,int fontSize)
        { var t=Node(name,parent,anchors).gameObject.AddComponent<Text>(); t.font=font; t.text=text; t.fontSize=fontSize; t.color=Ink; t.alignment=TextAnchor.MiddleCenter; t.raycastTarget=false; t.horizontalOverflow=HorizontalWrapMode.Wrap; t.verticalOverflow=VerticalWrapMode.Truncate; return t; }
        public Text Heading(string name,string text,RectTransform parent,Rect anchors,int fontSize)
        { var t=Label(name,text,parent,anchors,fontSize);t.font=bold;return t; }
        public void Flowers(RectTransform panel)
        { Icon("Flower left",UIIcon.Flower,panel,new Rect(.015f,.81f,.08f,.16f));Icon("Flower right",UIIcon.Flower,panel,new Rect(.905f,.025f,.08f,.16f)); }
        public Button Button(string name,string text,RectTransform parent,Rect anchors,UnityAction action,bool secondary=false)
        {
            var p=Panel(name,parent,anchors,secondary?Mint:Pink); p.raycastTarget=true;
            p.gloss=.32f;
            var b=p.gameObject.AddComponent<Button>(); b.targetGraphic=p; var colors=b.colors; colors.disabledColor=new Color(.82f,.84f,.87f,.86f); colors.pressedColor=new Color(.94f,.87f,.91f); b.colors=colors;
            Heading(name+" label",text,p.rectTransform,new Rect(.055f,.04f,.89f,.92f),18);
            p.gameObject.AddComponent<ButtonPressFeedback>(); b.onClick.AddListener(action); return b;
        }
        public RawImage Character(string name,int index,RectTransform parent,Rect anchors)
        {
            var frame=Panel(name+" frame",parent,anchors,Cream);frame.radius=200;
            var square=frame.gameObject.AddComponent<AspectRatioFitter>();square.aspectRatio=1;square.aspectMode=AspectRatioFitter.AspectMode.FitInParent;
            var clip=Panel(name+" clip",frame.rectTransform,new Rect(.055f,.055f,.89f,.89f),Color.white);clip.radius=200;clip.decorated=false;
            clip.gameObject.AddComponent<Mask>().showMaskGraphic=true;
            var fit=Node(name,clip.rectTransform,new Rect(0,0,1,1)); var image=fit.gameObject.AddComponent<RawImage>(); image.texture=characters; image.raycastTarget=false;
            image.uvRect=new Rect((15+index*170)/1536f,104/1024f,152/1536f,150/1024f);
            var a=fit.gameObject.AddComponent<AspectRatioFitter>(); a.aspectRatio=152f/150; a.aspectMode=AspectRatioFitter.AspectMode.FitInParent; return image;
        }
        public RawImage Background(Texture2D texture,RectTransform parent)
        { var image=Node("Town background",parent,new Rect(0,0,1,1)).gameObject.AddComponent<RawImage>(); image.texture=texture; image.raycastTarget=false; image.gameObject.AddComponent<BackgroundCover>(); return image; }
        public UIIconGraphic Icon(string name,UIIcon icon,RectTransform parent,Rect anchors,Color? tint=null)
        { var g=Node(name,parent,anchors).gameObject.AddComponent<UIIconGraphic>(); g.icon=icon; g.color=tint??Ink; g.raycastTarget=false; return g; }
        public void Header(RectTransform root,ScreenContext c,string title)
        {
            var head=Panel("Header",root,new Rect(0,0,1,1)).rectTransform; Top(head,10,8,10,54);
            var back=Button("Back","戻る",head,new Rect(0,0,.22f,1),c.Flow.GoBack,true); back.GetComponentInChildren<Text>().fontSize=14;
            Heading("Page title",title,head,new Rect(.23f,0,.56f,1),20);
            var settings=Button("Settings","",head,new Rect(.81f,0,.19f,1),c.Flow.ShowSettings,true); Icon("Settings icon",UIIcon.Settings,(RectTransform)settings.transform,new Rect(.26f,.26f,.48f,.48f));
        }
        public void Nav(RectTransform root,ScreenContext c,ScreenId active)
        {
            var nav=Panel("Navigation",root,new Rect(0,0,1,1),Cream).rectTransform; Bottom(nav,8,8,8,66);
            var names=new[]{"もふまち","依頼","マージ"}; var ids=new[]{ScreenId.Home,ScreenId.Quest,ScreenId.Merge};
            var icons=new[]{UIIcon.Home,UIIcon.Mail,UIIcon.Tea};
            for(int i=0;i<3;i++){ int j=i; var b=Button("Nav "+ids[i],"",nav,new Rect(i/3f+.012f,.06f,1/3f-.024f,.88f),()=>{ c.Flow.PlayCue(AudioCue.Confirm); if(j==0)c.Flow.ShowHome();else if(j==1)c.Flow.ShowQuest();else c.Flow.ShowMerge(); },ids[i]!=active);
                Icon("Nav icon",icons[i],(RectTransform)b.transform,new Rect(.31f,.39f,.38f,.53f)); Heading("Nav label",names[i],(RectTransform)b.transform,new Rect(.02f,.025f,.96f,.34f),12); }
        }
        public void Feedback(RectTransform root,ScreenContext c,ScreenId id)
        {
            string text=c.Flow.FeedbackFor(id);
            if(text.Length>0){var surface=Panel("Feedback surface",root,new Rect(0,0,1,1),new Color(1,.988f,.945f,.96f));Bottom(surface.rectTransform,12,78,12,42);}
            var t=Label("Feedback",text,root,new Rect(0,0,1,1),13); Bottom(t.rectTransform,16,78,16,42);
        }
    }
    public sealed class BackgroundCover : MonoBehaviour
    {
        private void OnRectTransformDimensionsChange() => Fit(); private void Start()=>Fit();
        private void Fit(){ var i=GetComponent<RawImage>(); if(i==null||i.texture==null)return; var r=i.rectTransform.rect; if(r.height<=0||r.width<=0)return;
            float ratio=r.width/r.height, source=(float)i.texture.width/i.texture.height;
            i.uvRect=ratio>source?new Rect(0,(1-source/ratio)/2,1,source/ratio):new Rect((1-ratio/source)/2,0,ratio/source,1); }
    }
    public sealed class ButtonPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    { public void OnPointerDown(PointerEventData e){if(GetComponent<Button>().IsInteractable())transform.localScale=Vector3.one*.97f;} public void OnPointerUp(PointerEventData e)=>Reset(); public void OnPointerExit(PointerEventData e)=>Reset(); private void OnDisable()=>Reset(); private void Reset()=>transform.localScale=Vector3.one; }
}
