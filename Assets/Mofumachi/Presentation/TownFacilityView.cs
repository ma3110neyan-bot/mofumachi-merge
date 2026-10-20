using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public sealed class TownFacilityView:MonoBehaviour
    {
        private sealed class Door
        {public string Id;public RectTransform Hit,Leaf,Frame,Sign;public RoundedPanelGraphic Light;public Text Caption;public Mofumachi.Core.TownPoint Art;public Vector2 Size;}
        private readonly List<Door> doors=new List<Door>();private ScreenContext context;private TownLifeView life;private bool dispatched;
        public TownFacilityEntry Entry {get;}=new TownFacilityEntry();
        public void Bind(ScreenContext c,TownLifeView town)
        {
            context=c;life=town;Build("quest","北の依頼小屋",TownSceneryKind.QuestCottage,new Rect(352f/1254,1-1145f/1254,68f/1254,115f/1254));Build("shop","東の花屋",TownSceneryKind.Florist,new Rect(796f/1254,1-588f/1254,78f/1254,130f/1254));Build("cafe","西のカフェ",TownSceneryKind.Cafe,new Rect(410f/1254,1-550f/1254,53f/1254,147f/1254));Refresh();
        }
        private void Build(string id,string title,TownSceneryKind kind,Rect uv)
        {
            var landmark=TownSceneryLayout.Landmarks.Single(p=>p.Kind==kind);var source=TownLandmarkGraphic.LandmarkUV(kind);
            var art=new Mofumachi.Core.TownPoint(landmark.Feet.X+((uv.center.x-source.xMin)/source.width-.5f)*landmark.Width,landmark.Feet.Y+(uv.center.y-source.yMin)/source.height*landmark.Height);
            var door=new Door{Id=id,Art=art,Size=new Vector2(uv.width/source.width*landmark.Width,uv.height/source.height*landmark.Height)};
            var frame=UIWidgets.Node("Facility doorway "+id,(RectTransform)life.transform,new Rect(.5f,.5f,0,0));door.Frame=frame;
            door.Light=context.Widgets.Panel("Warm doorway "+id,frame,new Rect(0,0,1,1),new Color(.29f,.15f,.12f)).GetComponent<RoundedPanelGraphic>();door.Light.decorated=false;door.Light.radius=8;door.Light.raycastTarget=false;
            var leaf=UIWidgets.Node("Opening door leaf "+id,frame,new Rect(0,0,1,1));leaf.pivot=new Vector2(0,.5f);door.Leaf=leaf;
            var image=leaf.gameObject.AddComponent<RawImage>();image.texture=Resources.Load<Texture2D>(TownSceneryLayout.LandmarkResource);image.uvRect=uv;image.raycastTarget=false;
            var hit=UIWidgets.Node("Facility entrance "+id,(RectTransform)life.transform,new Rect(.5f,.5f,0,0));hit.sizeDelta=new Vector2(72,80);door.Hit=hit;
            var ray=hit.gameObject.AddComponent<Image>();ray.color=new Color(1,1,1,1f/255);var button=hit.gameObject.AddComponent<Button>();button.targetGraphic=ray;button.transition=Selectable.Transition.None;button.onClick.AddListener(()=>RequestEnter(id));
            var sign=context.Widgets.StoryPanel("Facility sign "+id,hit,new Rect(-.40f,1,1.80f,.37f),true).rectTransform;door.Sign=sign;sign.anchorMin=sign.anchorMax=new Vector2(.5f,1);sign.pivot=new Vector2(.5f,0);sign.anchoredPosition=Vector2.zero;sign.sizeDelta=new Vector2(146,28);
            door.Caption=context.Widgets.NarrativeLabel("Facility name "+id,title,sign,new Rect(.03f,.02f,.94f,.96f),12);doors.Add(door);
        }
        internal void AddDepthEntries(List<(Transform Transform,float Feet)> depth)
        {foreach(var door in doors)if(door.Frame.gameObject.activeSelf)depth.Add((door.Frame,door.Id=="quest"?2.139f:.099f));}
        internal void RaiseControls()
        {
            int visible=0;foreach(var door in doors)if(door.Hit.gameObject.activeSelf)visible++;
            bool correct=true;int index=transform.childCount-visible;
            foreach(var door in doors)if(door.Hit.gameObject.activeSelf&&door.Hit.GetSiblingIndex()!=index++)correct=false;
            if(!correct)foreach(var door in doors)if(door.Hit.gameObject.activeSelf)door.Hit.SetAsLastSibling();
        }
        public bool RequestEnter(string id)
        {
            if(!life.CanInteract||life.IsManuallyDragging||TownFacilityEntry.DestinationFor(id)==null)return false;
            if(Entry.Phase==TownEntryPhase.Opening||Entry.Phase==TownEntryPhase.Entered)return false;
            var hero=life.World.Model.Hero;
            if(!hero.IsWalking&&TownWalkableArea.Distance(hero.Position,TownWorldMap.Place(TownFacilityEntry.DestinationFor(id)))<=.032f)
            {
                life.StartInteraction();dispatched=false;bool opened=Entry.TapDoor(id,hero.Position,false);Refresh();return opened;
            }
            bool guided=id=="cafe"?(life.TownLevel>=2?life.GuideToPlace(TownFacilityEntry.DestinationFor(id)):life.World.GuidePoint(TownWorldMap.Place(TownFacilityEntry.DestinationFor(id)))):life.GuideToFacility(id);
            if(!guided)return false;
            dispatched=false;return Entry.Request(id);
        }
        public void Cancel(){Entry.Cancel();dispatched=false;}
        public void Advance(float seconds)
        {
            if(!life.CanSimulate){Cancel();return;}
            Entry.Step(life.World.Model.Hero.Position,life.World.Model.Hero.IsWalking,seconds);Refresh();
            if(Entry.Phase!=TownEntryPhase.Entered||dispatched)return;dispatched=true;
            context.Flow.PlayCue(AudioCue.Confirm);
            if(Entry.Facility=="quest")context.Flow.ShowQuest();else if(Entry.Facility=="cafe")context.Flow.OpenCafe();else context.Flow.OpenShopPreview();
        }
        public void Refresh()
        {
            if(life?.World?.IsReady!=true)return;float scale=life.World.RenderScale;
            foreach(var door in doors)
            {
                door.Frame.anchoredPosition=life.World.Local(door.Art);door.Frame.sizeDelta=door.Size*scale;
                door.Hit.anchoredPosition=life.World.Local(door.Art);door.Hit.sizeDelta=new Vector2(Mathf.Max(48,door.Frame.sizeDelta.x+18),Mathf.Max(48,door.Frame.sizeDelta.y+10));
                var p=door.Hit.anchoredPosition;var bounds=((RectTransform)life.transform).rect;
                bool visible=p.x>bounds.xMin-64&&p.x<bounds.xMax+64&&p.y>bounds.yMin-80&&p.y<bounds.yMax-10;
                door.Hit.gameObject.SetActive(visible);door.Frame.gameObject.SetActive(visible);
                bool near=TownWalkableArea.Distance(life.World.Model.Hero.Position,TownWorldMap.Place(TownFacilityEntry.DestinationFor(door.Id)))<.23f;
                float amount=Entry.Facility==door.Id?Entry.OpenAmount:0;
                door.Leaf.localScale=new Vector3(1-amount,1,1);door.Light.color=Color.Lerp(new Color(.28f,.14f,.10f),new Color(1,.86f,.48f),Mathf.Max(amount,near?.35f:0));
                string title=door.Id=="quest"?"北の依頼小屋":door.Id=="cafe"?"西のカフェ":"東の花屋";
                door.Caption.text=Entry.Facility==door.Id?(Entry.Phase==TownEntryPhase.Walking?"入口までおさんぽ中♪":Entry.Phase==TownEntryPhase.Arrived?title+"\nドアをタップして入る":"扉がひらくよ♪"):near?title+"\nドアをタップして入る":title;
                float captionHeight=near||Entry.Facility==door.Id?44:28;door.Sign.sizeDelta=new Vector2(146,captionHeight);
                float captionX=TownWorldLayout.LabelCenter(p.x,bounds.xMin,bounds.xMax,146)-p.x;
                door.Sign.anchoredPosition=new Vector2(captionX,TownWorldLayout.CaptionDrop(p.y+door.Hit.rect.height/2,captionHeight,bounds.yMax));
            }
        }
    }
}
