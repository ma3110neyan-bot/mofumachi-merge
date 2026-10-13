using System.Collections;
using UnityEngine;
namespace Mofumachi.Presentation
{
    // The art reacts; the grid cell, label and pointer target retain their size.
    public sealed class ItemSelectionFeedback : MonoBehaviour
    {
        private Coroutine animation;
        private bool started;
        public void Play()
        {
            if(!isActiveAndEnabled)return;
            if(animation!=null)StopCoroutine(animation);
            animation=StartCoroutine(Select());
        }
        private IEnumerator Select()
        {
            float elapsed=0;transform.localScale=Vector3.one;
            // Resolve the new page's initial layout before distinguishing later viewport changes.
            yield return null;started=true;
            while(elapsed<MergeVisualTiming.SelectionSeconds)
            {
                elapsed+=Time.unscaledDeltaTime;
                transform.localScale=Vector3.one*MergeVisualTiming.SelectionScale(elapsed);
                ((RectTransform)transform).anchoredPosition=Vector2.up*((MergeVisualTiming.SelectionScale(elapsed)-1)/.06f);
                yield return null;
            }
            animation=null;started=false;
        }
        private void OnRectTransformDimensionsChange()
        {
            if(!started||animation==null)return;
            StopCoroutine(animation);animation=null;started=false;
            transform.localScale=Vector3.one*MergeVisualTiming.SelectionScale(MergeVisualTiming.SelectionSeconds);
            ((RectTransform)transform).anchoredPosition=Vector2.up;
        }
        private void OnDisable(){StopAllCoroutines();animation=null;started=false;transform.localScale=Vector3.one;((RectTransform)transform).anchoredPosition=Vector2.zero;}
    }
}
