using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public sealed class VolumeControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IEndDragHandler, ISubmitHandler, IDeselectHandler
    {
        private Slider slider;private Action<float> preview,commit;private float saved;private bool holding,pending;
        public void Bind(Slider slider,Action<float> preview,Action<float> commit)
        { this.slider=slider;this.preview=preview;this.commit=commit;saved=slider.value;slider.onValueChanged.AddListener(Changed); }
        private void Changed(float value){pending=true;preview(value);}
        // Slider may receive pointerDown before this component. Defer keyboard commits
        // until LateUpdate so pointerDown still marks the entire gesture as one edit.
        private void LateUpdate(){if(pending && !holding)Commit();}
        public void OnPointerDown(PointerEventData e){holding=true;}
        public void OnPointerUp(PointerEventData e){holding=false;Commit();}
        public void OnEndDrag(PointerEventData e){holding=false;Commit();}
        public void OnSubmit(BaseEventData e)=>Commit();
        public void OnDeselect(BaseEventData e){if(!holding)Commit();}
        private void Commit(){if(!pending)return;pending=false;saved=slider.value;commit(saved);}
        public void CancelPending(){holding=false;if(!pending || slider==null)return;pending=false;slider.SetValueWithoutNotify(saved);preview(saved);}
        private void OnDisable()=>CancelPending();
        private void OnDestroy(){if(slider!=null)slider.onValueChanged.RemoveListener(Changed);}
    }
}
