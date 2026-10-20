using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // A single reusable presentation layer survives the .22-second board rebuild.
    // It reads committed results and never changes the board, locks, audio or save data.
    public sealed class MergeEffectsView : MonoBehaviour
    {
        public bool IsPlaying { get; private set; }
        private ItemGraphic source,other,result,hiddenTarget;
        private MergeSparkleGraphic sparkle;
        private Vector2 fromPoint,toPoint,itemSize,meetingPoint;
        private float startedAt;
        private Text levelLabel;
        private int landingCell;
        private bool targetWasEnabled,itemMotionFinished;
        private void Awake()
        {
            var root=(RectTransform)transform;
            var group=gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
            source=Create("Gathered source",root);other=Create("Gathered target",root);result=Create("Merged result",root);
            sparkle=UIWidgets.Node("Merge success light",root,new Rect(.5f,.5f,0,0)).gameObject.AddComponent<MergeSparkleGraphic>();
            levelLabel=UIWidgets.Node("Merged level label",root,new Rect(.5f,.5f,0,0)).gameObject.AddComponent<Text>();levelLabel.font=Resources.Load<Font>("Mofumachi/UIFontBold");levelLabel.fontSize=16;levelLabel.alignment=TextAnchor.MiddleCenter;levelLabel.raycastTarget=false;levelLabel.rectTransform.sizeDelta=new Vector2(112,28);
            var labelShadow=levelLabel.gameObject.AddComponent<Shadow>();labelShadow.effectColor=new Color(1,1,.94f,.9f);labelShadow.effectDistance=new Vector2(0,-1);
            ResetVisuals();
        }
        private static ItemGraphic Create(string name,RectTransform root)
        {
            var image=UIWidgets.Node(name,root,new Rect(.5f,.5f,0,0)).gameObject.AddComponent<ItemGraphic>();image.SetPolished(true);image.raycastTarget=false;return image;
        }
        public void Begin(RectTransform board,int from,int to,string itemId,int previousLevel)
        {
            Cancel();gameObject.SetActive(true);transform.SetAsLastSibling();
            // Layout must be resolved before projecting the cells into this overlay.
            Canvas.ForceUpdateCanvases();board.GetComponent<MergeGridLayout>().ResizeNow();LayoutRebuilder.ForceRebuildLayoutImmediate(board);
            var a=board.Find("Cell "+from) as RectTransform;var b=board.Find("Cell "+to) as RectTransform;
            if(a==null||b==null){Cancel();return;}
            var root=(RectTransform)transform;
            fromPoint=Point(root,a);toPoint=Point(root,b);itemSize=new Vector2(b.rect.width*.92f,b.rect.height*.80f);landingCell=to;
            meetingPoint=(fromPoint+toPoint)*.5f;
            source.Bind(itemId,previousLevel);other.Bind(itemId,previousLevel);result.Bind(itemId,previousLevel+1);
            source.rectTransform.sizeDelta=other.rectTransform.sizeDelta=result.rectTransform.sizeDelta=itemSize;
            sparkle.rectTransform.anchoredPosition=toPoint;sparkle.rectTransform.sizeDelta=b.rect.size*.9f;
            hiddenTarget=b.GetComponentInChildren<ItemGraphic>();
            if(hiddenTarget!=null){targetWasEnabled=hiddenTarget.enabled;hiddenTarget.enabled=false;}
            itemMotionFinished=false;startedAt=Time.unscaledTime;IsPlaying=true;Apply(0);
            levelLabel.text="Lv.UP!  "+(previousLevel+1);
        }
        private static Vector2 Point(RectTransform root,RectTransform cell)
        {
            var r=cell.rect;var center=new Vector2(r.center.x,r.yMin+r.height*.53f);
            return root.InverseTransformPoint(cell.TransformPoint(center));
        }
        private void LateUpdate()
        {
            if(!IsPlaying)return;float elapsed=Time.unscaledTime-startedAt;
            if(!MergeVisualTiming.IsActive(elapsed)){Cancel();return;}Apply(elapsed);
        }
        private void Apply(float elapsed)
        {
            float p=MergeVisualTiming.GatherProgress(elapsed);
            bool gathering=!itemMotionFinished&&elapsed<MergeVisualTiming.GatherSeconds;
            source.gameObject.SetActive(gathering);other.gameObject.SetActive(gathering);
            var lift=Vector2.up*(itemSize.y*MergeVisualTiming.GatherLift(elapsed));
            source.rectTransform.anchoredPosition=Vector2.Lerp(fromPoint,meetingPoint,p)+lift;source.transform.localScale=Vector3.one*(1-.15f*p);
            other.rectTransform.anchoredPosition=Vector2.Lerp(toPoint,meetingPoint,p)+lift;other.transform.localScale=Vector3.one*(1-.15f*p);
            bool showingResult=!itemMotionFinished&&elapsed>=MergeVisualTiming.GatherSeconds&&elapsed<MergeVisualTiming.LockSeconds;
            var landing=Vector2.Lerp(meetingPoint,toPoint,MergeVisualTiming.LandingProgress(elapsed));
            result.gameObject.SetActive(showingResult);result.rectTransform.anchoredPosition=landing+Vector2.up*(itemSize.y*MergeVisualTiming.ResultLift(elapsed));result.transform.localScale=Vector3.one*MergeVisualTiming.ResultScale(elapsed);
            if(elapsed>=MergeVisualTiming.LockSeconds)FinishItemMotion();
            sparkle.rectTransform.anchoredPosition=landing;sparkle.gameObject.SetActive(true);sparkle.SetBurstFrame(elapsed);
            levelLabel.gameObject.SetActive(elapsed>=MergeVisualTiming.GatherSeconds);levelLabel.color=new Color(.81f,.25f,.43f,Mathf.Clamp01(MergeVisualTiming.SparkleAlpha(elapsed)*1.5f));
            var labelPoint=toPoint+Vector2.up*(itemSize.y*.5f+12+elapsed*12);var bounds=((RectTransform)transform).rect;
            labelPoint.x=Mathf.Clamp(labelPoint.x,bounds.xMin+58,bounds.xMax-58);labelPoint.y=Mathf.Min(labelPoint.y,bounds.yMax-16);levelLabel.rectTransform.anchoredPosition=labelPoint;
        }
        public void FinishItemMotion()
        {
            itemMotionFinished=true;
            if(hiddenTarget!=null&&hiddenTarget.gameObject.activeInHierarchy)hiddenTarget.enabled=targetWasEnabled;hiddenTarget=null;
            if(source!=null)source.gameObject.SetActive(false);if(other!=null)other.gameObject.SetActive(false);if(result!=null)result.gameObject.SetActive(false);
        }
        public void SettleOnto(RectTransform board)
        {
            if(!IsPlaying||board==null)return;var cell=board.Find("Cell "+landingCell);var image=cell==null?null:cell.GetComponentInChildren<ItemGraphic>();
            if(image!=null)image.gameObject.AddComponent<ItemLandingFeedback>();
        }
        public void Cancel(){ResetVisuals();if(gameObject.activeSelf)gameObject.SetActive(false);}
        private void ResetVisuals(){IsPlaying=false;FinishItemMotion();if(sparkle!=null)sparkle.gameObject.SetActive(false);if(levelLabel!=null)levelLabel.gameObject.SetActive(false);}
        private void OnDisable()=>ResetVisuals();
        private void OnRectTransformDimensionsChange(){if(IsPlaying)Cancel();}
    }
    public sealed class ItemLandingFeedback : MonoBehaviour
    {
        private float started;private void Awake()=>started=Time.unscaledTime;
        private void Update()
        {float t=(Time.unscaledTime-started)/.25f;if(t>=1){transform.localScale=Vector3.one;Destroy(this);return;}float bounce=Mathf.Sin(t*Mathf.PI*2)*Mathf.Exp(-t*3);transform.localScale=new Vector3(1-.08f*bounce,1+.1f*bounce,1);}
        private void OnDisable()=>transform.localScale=Vector3.one;
    }
}
