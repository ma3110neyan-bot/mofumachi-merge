using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
namespace Mofumachi.Presentation
{
    public sealed class UIWidgets
    {
        public static readonly Color Ink = new Color(.32f,.18f,.29f), Pink=new Color(1,.52f,.70f), Cream=new Color(1,.988f,.945f), Mint=new Color(.54f,.86f,.74f), Sky=new Color(.67f,.88f,1);
        public static readonly Color BlushPaper=new Color(1,.965f,.975f), BluePaper=new Color(.945f,.985f,1), Lavender=new Color(.83f,.76f,.98f), Peach=new Color(1,.78f,.62f);
        public static readonly Color StoryPaper=new Color(1,.977f,.914f),RosePaper=new Color(.98f,.79f,.81f),StoryInk=new Color(.36f,.24f,.29f);
        private readonly Font font,bold,story,bodyFont; private readonly Texture2D characters;
        public UIWidgets(Font font, Texture2D characters) { this.font=font; this.characters=characters; bold=Resources.Load<Font>("Mofumachi/UIFontBold")??font; story=Resources.Load<Font>("Mofumachi/UIFontStory")??font;bodyFont=Resources.Load<Font>("Mofumachi/UIFontBody")??story; }
        public static RectTransform Node(string name, RectTransform parent, Rect anchors)
        { var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent,false); r.anchorMin=anchors.min; r.anchorMax=anchors.max; r.offsetMin=r.offsetMax=Vector2.zero; return r; }
        public static void Top(RectTransform r,float left,float top,float right,float height)
        { r.anchorMin=new Vector2(0,1); r.anchorMax=Vector2.one; r.offsetMin=new Vector2(left,-top-height); r.offsetMax=new Vector2(-right,-top); }
        public static void Bottom(RectTransform r,float left,float bottom,float right,float height)
        { r.anchorMin=Vector2.zero; r.anchorMax=new Vector2(1,0); r.offsetMin=new Vector2(left,bottom); r.offsetMax=new Vector2(-right,bottom+height); }
        public RoundedPanelGraphic Panel(string name,RectTransform parent,Rect anchors,Color? color=null)
        { var p=Node(name,parent,anchors).gameObject.AddComponent<RoundedPanelGraphic>(); p.color=color??Cream; p.raycastTarget=false; return p; }
        public DecorativeFrameGraphic Frame(RectTransform parent,float cornerSize=36,bool corners=true)
        { var g=Node("Floral gold frame",parent,new Rect(0,0,1,1)).gameObject.AddComponent<DecorativeFrameGraphic>();g.cornerSize=cornerSize;g.showCorners=corners;g.raycastTarget=false;return g; }
        public RoundedPanelGraphic OrnatePanel(string name,RectTransform parent,Rect anchors,Color? color=null,float cornerSize=36)
        { var p=Panel(name,parent,anchors,color);Frame(p.rectTransform,cornerSize);return p; }
        public RoundedPanelGraphic StoryPanel(string name,RectTransform parent,Rect anchors,bool speech=false)
        {var p=Panel(name,parent,anchors,StoryPaper);p.radius=12;p.gloss=.035f;p.borderWidth=1;p.shadowDepth=3;Node("Story paper grain",p.rectTransform,new Rect(0,0,1,1)).gameObject.AddComponent<StoryPaperGraphic>().Speech=speech;return p;}
        public RectTransform ScrollPaper(string name,RectTransform parent,float bottom,float top,float minimumHeight,Color tint)
        {
            var viewport=Node(name+" viewport",parent,new Rect(0,0,1,1));viewport.offsetMin=new Vector2(14,bottom);viewport.offsetMax=new Vector2(-14,-top);
            viewport.gameObject.AddComponent<RectMask2D>();var capture=viewport.gameObject.AddComponent<Image>();capture.color=Color.clear;
            var paper=StoryPanel(name,viewport,new Rect(0,0,1,1)).rectTransform;paper.pivot=new Vector2(.5f,1);Top(paper,0,0,0,minimumHeight);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=paper;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            viewport.gameObject.AddComponent<PaperViewportLayout>().Bind(paper,minimumHeight);return paper;
        }
        public Text Ribbon(string name,string text,RectTransform parent,Rect anchors,int fontSize,Color? color=null)
        { var r=Node(name+" ribbon",parent,anchors);var g=r.gameObject.AddComponent<RibbonGraphic>();g.color=color??Pink;g.raycastTarget=false;
            var t=Heading(name,text,r,new Rect(.09f,.04f,.82f,.92f),fontSize);return t; }
        public void Divider(RectTransform parent,float y)
        { var r=Node("Floral divider",parent,new Rect(.13f,y,.74f,.04f));
            foreach(float x in new[]{0f,.6f}){var line=Node("Gold line",r,new Rect(x,.47f,.4f,.06f)).gameObject.AddComponent<Image>();line.color=new Color(.9f,.72f,.39f,.65f);line.raycastTarget=false;}
            Icon("Divider flower",UIIcon.Flower,r,new Rect(.44f,0,.12f,1)); }
        public Text Label(string name,string text,RectTransform parent,Rect anchors,int fontSize)
        { var t=Node(name,parent,anchors).gameObject.AddComponent<Text>(); t.font=font; t.text=text; t.fontSize=fontSize; t.color=Ink; t.alignment=TextAnchor.MiddleCenter; t.raycastTarget=false; t.horizontalOverflow=HorizontalWrapMode.Wrap; t.verticalOverflow=VerticalWrapMode.Truncate; return t; }
        public Text Heading(string name,string text,RectTransform parent,Rect anchors,int fontSize)
        { var t=Label(name,text,parent,anchors,fontSize);t.font=bold;var shadow=t.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.84f,.65f,.33f,.35f);shadow.effectDistance=new Vector2(0,-.6f);return t; }
        public Text StoryLabel(string name,string text,RectTransform parent,Rect anchors,int fontSize)
        { var t=Label(name,text,parent,anchors,fontSize);t.font=story;t.lineSpacing=1.28f;t.color=StoryInk;return t; }
        public Text StoryHeading(string name,string text,RectTransform parent,Rect anchors,int fontSize)
        { return StoryLabel(name,text,parent,anchors,fontSize); }
        public Text NarrativeLabel(string name,string text,RectTransform parent,Rect anchors,int fontSize)
        {var t=Label(name,text,parent,anchors,fontSize);t.font=bodyFont;t.lineSpacing=1.28f;t.color=StoryInk;return t;}
        public Text StoryRibbon(string name,string text,RectTransform parent,Rect anchors,int fontSize,Color? tint=null)
        { var r=Node(name+" ribbon",parent,anchors);var g=r.gameObject.AddComponent<RibbonGraphic>();g.color=tint??RosePaper;g.raycastTarget=false;return StoryHeading(name,text,r,new Rect(.09f,.04f,.82f,.92f),fontSize); }
        public void Flowers(RectTransform panel)
        { Icon("Flower left",UIIcon.Flower,panel,new Rect(.015f,.81f,.08f,.16f));Icon("Flower right",UIIcon.Flower,panel,new Rect(.905f,.025f,.08f,.16f)); }
        public Button Button(string name,string text,RectTransform parent,Rect anchors,UnityAction action,bool secondary=false)
        {
            var p=Panel(name,parent,anchors,secondary?Mint:Pink); p.raycastTarget=true;
            p.gloss=.42f;p.borderWidth=1.8f;
            var b=p.gameObject.AddComponent<Button>(); b.targetGraphic=p; var colors=b.colors; colors.disabledColor=new Color(.82f,.84f,.87f,.86f); colors.pressedColor=new Color(.94f,.87f,.91f); b.colors=colors;
            Heading(name+" label",text,p.rectTransform,new Rect(.055f,.04f,.89f,.92f),18);
            p.gameObject.AddComponent<ButtonPressFeedback>(); b.onClick.AddListener(action); return b;
        }
        public RawImage Character(string name,int index,RectTransform parent,Rect anchors)
        {
            // FitInParent always uses the whole immediate parent. Give each
            // portrait its own anchored slot before fitting the square frame.
            var region=Node(name+" region",parent,anchors);
            var frame=Panel(name+" frame",region,new Rect(0,0,1,1),Cream);frame.radius=200;
            var square=frame.gameObject.AddComponent<AspectRatioFitter>();square.aspectRatio=1;square.aspectMode=AspectRatioFitter.AspectMode.FitInParent;
            var clip=Panel(name+" clip",frame.rectTransform,new Rect(.055f,.055f,.89f,.89f),Color.white);clip.radius=200;clip.decorated=false;
            clip.gameObject.AddComponent<Mask>().showMaskGraphic=true;
            var fit=Node(name,clip.rectTransform,new Rect(0,0,1,1)); var image=fit.gameObject.AddComponent<RawImage>(); image.texture=characters; image.raycastTarget=false;
            image.uvRect=new Rect((15+index*170)/1536f,104/1024f,152/1536f,150/1024f);
            var a=fit.gameObject.AddComponent<AspectRatioFitter>(); a.aspectRatio=152f/150; a.aspectMode=AspectRatioFitter.AspectMode.FitInParent; return image;
        }
        public CharacterBodyGraphic Body(string name,string characterId,RectTransform parent,Rect anchors,CharacterExpression expression=CharacterExpression.Normal)
        {var body=Node(name,parent,anchors).gameObject.AddComponent<CharacterBodyGraphic>();body.Bind(characterId,expression);body.raycastTarget=false;return body;}
        public RawImage Background(Texture2D texture,RectTransform parent)
        { var image=Node("Town background",parent,new Rect(0,0,1,1)).gameObject.AddComponent<RawImage>(); image.texture=texture; image.raycastTarget=false; image.gameObject.AddComponent<BackgroundCover>(); return image; }
        public UIIconGraphic Icon(string name,UIIcon icon,RectTransform parent,Rect anchors,Color? tint=null)
        { var g=Node(name,parent,anchors).gameObject.AddComponent<UIIconGraphic>(); g.icon=icon; g.color=tint??Ink; g.raycastTarget=false; return g; }
        public ItemGraphic Item(string name,string itemId,int level,RectTransform parent,Rect anchors)
        {var graphic=Node(name,parent,anchors).gameObject.AddComponent<ItemGraphic>();graphic.Bind(itemId,level);return graphic;}
        public void Header(RectTransform root,ScreenContext c,string title,Color? tint=null,float height=54)
        {
            var head=Panel("Header",root,new Rect(0,0,1,1)).rectTransform; Top(head,10,8,10,height);Frame(head,14,false);
            var back=Button("Back","戻る",head,new Rect(0,0,.22f,1),c.Flow.GoBack,true); back.GetComponentInChildren<Text>().fontSize=14;
            StoryRibbon("Page title",title,head,new Rect(.22f,.12f,.58f,.76f),18,tint);
            var settings=Button("Settings","",head,new Rect(.81f,0,.19f,1),c.Flow.ShowSettings,true); Icon("Settings icon",UIIcon.Settings,(RectTransform)settings.transform,new Rect(.26f,.26f,.48f,.48f));
        }
        public void Nav(RectTransform root,ScreenContext c,ScreenId active,float height=66)
        {
            var nav=Panel("Navigation",root,new Rect(0,0,1,1),Cream).rectTransform; Bottom(nav,8,8,8,height);Frame(nav,14,false);
            var names=new[]{"もふまち",active==ScreenId.Home?"北の依頼小屋":"依頼",UIStrings.MergeTitle}; var ids=new[]{ScreenId.Home,ScreenId.Quest,ScreenId.Merge};
            var icons=new[]{UIIcon.Home,UIIcon.Mail,UIIcon.Tea};
            for(int i=0;i<3;i++){ int j=i; var b=Button("Nav "+ids[i],"",nav,new Rect(i/3f+.012f,.06f,1/3f-.024f,.88f),()=>{ c.Flow.PlayCue(AudioCue.Confirm); if(j==0)c.Flow.ShowHome();else if(j==1)c.Flow.VisitQuestCabin();else c.Flow.ShowMerge(); },ids[i]!=active);
                Frame((RectTransform)b.transform,12);
                Icon("Nav icon",icons[i],(RectTransform)b.transform,new Rect(.31f,.39f,.38f,.53f)); Heading("Nav label",names[i],(RectTransform)b.transform,new Rect(.02f,.005f,.96f,.38f),12); }
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
        private void OnRectTransformDimensionsChange() {if(isActiveAndEnabled)FitNow();} private void Start(){if(isActiveAndEnabled)FitNow();}
        public void FitNow(){ var i=GetComponent<RawImage>(); if(i==null||i.texture==null)return; var r=i.rectTransform.rect; if(r.height<=0||r.width<=0)return;
            float ratio=r.width/r.height, source=(float)i.texture.width/i.texture.height;
            i.uvRect=ratio>source?new Rect(0,(1-source/ratio)/2,1,source/ratio):new Rect((1-ratio/source)/2,0,ratio/source,1); }
        private void Fit(){if(isActiveAndEnabled)FitNow();}
    }
    public sealed class PaperViewportLayout : MonoBehaviour
    {
        private RectTransform paper;private float minimum;
        public void Bind(RectTransform content,float minimumHeight){paper=content;minimum=minimumHeight;Resize();}
        private void OnRectTransformDimensionsChange()=>Resize();
        private void Resize(){if(paper!=null)UIWidgets.Top(paper,0,0,0,Mathf.Max(minimum,((RectTransform)transform).rect.height));}
    }
    public sealed class ButtonPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool PreserveHitbox {get;set;}
        private Transform pressed;
        public void OnPointerDown(PointerEventData e)
        {Reset();if(GetComponent<Button>().IsInteractable()){pressed=PreserveHitbox?GetComponentInChildren<Text>(true)?.transform:transform;if(pressed!=null)pressed.localScale=Vector3.one*.97f;}}
        public void OnPointerUp(PointerEventData e)=>Reset();
        public void OnPointerExit(PointerEventData e)=>Reset();
        private void OnDisable()=>Reset();
        private void Reset(){if(pressed!=null)pressed.localScale=Vector3.one;pressed=null;}
    }
}
