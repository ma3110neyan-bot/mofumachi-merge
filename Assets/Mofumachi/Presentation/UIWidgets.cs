using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
namespace Mofumachi.Presentation
{
    public sealed class UIWidgets
    {
        public static readonly Color Ink = new Color(.31f,.22f,.38f), Pink=new Color(1,.68f,.79f), Cream=new Color(1,.98f,.92f), Mint=new Color(.61f,.86f,.75f);
        private readonly Font font; private readonly Texture2D characters;
        public UIWidgets(Font font, Texture2D characters) { this.font=font; this.characters=characters; }
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
        public Button Button(string name,string text,RectTransform parent,Rect anchors,UnityAction action,bool secondary=false)
        {
            var p=Panel(name,parent,anchors,secondary?Mint:Pink); p.raycastTarget=true;
            var shadow=p.gameObject.AddComponent<Shadow>(); shadow.effectColor=new Color(.23f,.16f,.26f,.22f); shadow.effectDistance=new Vector2(0,-3);
            var b=p.gameObject.AddComponent<Button>(); b.targetGraphic=p; var colors=b.colors; colors.disabledColor=new Color(.75f,.74f,.72f,.85f); colors.pressedColor=new Color(.89f,.89f,.89f); b.colors=colors;
            Label(name+" label",text,p.rectTransform,new Rect(.04f,.04f,.92f,.92f),18);
            p.gameObject.AddComponent<ButtonPressFeedback>(); b.onClick.AddListener(action); return b;
        }
        public RawImage Character(string name,int index,RectTransform parent,Rect anchors)
        {
            var slot=Panel(name+" frame",parent,anchors,Color.white).rectTransform;
            var fit=Node(name,slot,new Rect(.035f,.035f,.93f,.93f)); var image=fit.gameObject.AddComponent<RawImage>(); image.texture=characters; image.raycastTarget=false;
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
            Label("Page title",title,head,new Rect(.23f,0,.56f,1),20);
            var settings=Button("Settings","",head,new Rect(.81f,0,.19f,1),c.Flow.ShowSettings,true); Icon("Settings icon",UIIcon.Settings,(RectTransform)settings.transform,new Rect(.26f,.26f,.48f,.48f));
        }
        public void Nav(RectTransform root,ScreenContext c,ScreenId active)
        {
            var nav=Panel("Navigation",root,new Rect(0,0,1,1),Cream).rectTransform; Bottom(nav,8,8,8,66);
            var names=new[]{"もふまち","キャラ依頼","Merge"}; var ids=new[]{ScreenId.Home,ScreenId.Quest,ScreenId.Merge};
            var icons=new[]{UIIcon.Home,UIIcon.Mail,UIIcon.Tea};
            for(int i=0;i<3;i++){ int j=i; var b=Button("Nav "+ids[i],"",nav,new Rect(i/3f+.012f,.06f,1/3f-.024f,.88f),()=>{ c.Flow.PlayCue(AudioCue.Confirm); if(j==0)c.Flow.ShowHome();else if(j==1)c.Flow.ShowQuest();else c.Flow.ShowMerge(); },ids[i]!=active);
                Icon("Nav icon",icons[i],(RectTransform)b.transform,new Rect(.38f,.45f,.24f,.44f)); Label("Nav label",names[i],(RectTransform)b.transform,new Rect(.02f,.01f,.96f,.4f),12); }
        }
        public void Feedback(RectTransform root,ScreenContext c,ScreenId id)
        { var t=Label("Feedback",c.Flow.FeedbackFor(id),root,new Rect(0,0,1,1),13); Bottom(t.rectTransform,16,78,16,42); }
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
