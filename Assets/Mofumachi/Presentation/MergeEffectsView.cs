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
        private Vector2 fromPoint,toPoint,itemSize,nudge;
        private float startedAt;
        private bool targetWasEnabled,itemMotionFinished;
        private void Awake()
        {
            var root=(RectTransform)transform;
            var group=gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
            source=Create("Gathered source",root);other=Create("Gathered target",root);result=Create("Merged result",root);
            sparkle=UIWidgets.Node("Merge success light",root,new Rect(.5f,.5f,0,0)).gameObject.AddComponent<MergeSparkleGraphic>();
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
            fromPoint=Point(root,a);toPoint=Point(root,b);itemSize=new Vector2(b.rect.width*.82f,b.rect.height*.59f);
            nudge=Vector2.ClampMagnitude(fromPoint-toPoint,b.rect.width)*.09f;
            source.Bind(itemId,previousLevel);other.Bind(itemId,previousLevel);result.Bind(itemId,previousLevel+1);
            source.rectTransform.sizeDelta=other.rectTransform.sizeDelta=result.rectTransform.sizeDelta=itemSize;
            sparkle.rectTransform.anchoredPosition=toPoint;sparkle.rectTransform.sizeDelta=b.rect.size*.9f;
            hiddenTarget=b.GetComponentInChildren<ItemGraphic>();
            if(hiddenTarget!=null){targetWasEnabled=hiddenTarget.enabled;hiddenTarget.enabled=false;}
            itemMotionFinished=false;startedAt=Time.unscaledTime;IsPlaying=true;Apply(0);
        }
        private static Vector2 Point(RectTransform root,RectTransform cell)
        {
            var r=cell.rect;var center=new Vector2(r.center.x,r.yMin+r.height*.655f);
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
            source.rectTransform.anchoredPosition=Vector2.Lerp(fromPoint,toPoint,p)+Vector2.up*(itemSize.y*MergeVisualTiming.GatherLift(elapsed));source.transform.localScale=Vector3.one*(1-.2f*p);
            other.rectTransform.anchoredPosition=toPoint+nudge*Mathf.Sin(p*Mathf.PI);
            bool showingResult=!itemMotionFinished&&elapsed>=MergeVisualTiming.GatherSeconds&&elapsed<MergeVisualTiming.LockSeconds;
            result.gameObject.SetActive(showingResult);result.rectTransform.anchoredPosition=toPoint+Vector2.up*(itemSize.y*MergeVisualTiming.ResultLift(elapsed));result.transform.localScale=Vector3.one*MergeVisualTiming.ResultScale(elapsed);
            if(elapsed>=MergeVisualTiming.LockSeconds)FinishItemMotion();
            sparkle.gameObject.SetActive(true);sparkle.SetBurstFrame(elapsed);
        }
        public void FinishItemMotion()
        {
            itemMotionFinished=true;
            if(hiddenTarget!=null&&hiddenTarget.gameObject.activeInHierarchy)hiddenTarget.enabled=targetWasEnabled;hiddenTarget=null;
            if(source!=null)source.gameObject.SetActive(false);if(other!=null)other.gameObject.SetActive(false);if(result!=null)result.gameObject.SetActive(false);
        }
        public void Cancel(){ResetVisuals();if(gameObject.activeSelf)gameObject.SetActive(false);}
        private void ResetVisuals(){IsPlaying=false;FinishItemMotion();if(sparkle!=null)sparkle.gameObject.SetActive(false);}
        private void OnDisable()=>ResetVisuals();
        private void OnRectTransformDimensionsChange(){if(IsPlaying)Cancel();}
    }
}
