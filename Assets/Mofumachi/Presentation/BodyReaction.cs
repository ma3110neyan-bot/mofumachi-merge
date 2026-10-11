using UnityEngine;
using UnityEngine.EventSystems;
namespace Mofumachi.Presentation
{
    public sealed class BodyReaction : MonoBehaviour,IPointerClickHandler
    {
        private ScreenContext context;private CharacterBodyGraphic body;private float remaining;
        public void Bind(ScreenContext c){context=c;body=GetComponent<CharacterBodyGraphic>();}
        public void OnPointerClick(PointerEventData e){if(!isActiveAndEnabled||context==null||context.Flow.IsModalOpen||context.Flow.IsAppPaused)return;remaining=.65f;body.Bind(body.CharacterId,CharacterExpression.Smile);context.Flow.PlayCue(AudioCue.Character);}
        private void Update(){if(body==null||context.Flow.IsAppPaused)return;remaining=Mathf.Max(0,remaining-Time.unscaledDeltaTime);if(remaining>0)transform.localScale=Vector3.one*(1+.025f*Mathf.Sin((1-remaining/.65f)*Mathf.PI));else if(body.Expression!=CharacterExpression.Normal){body.Bind(body.CharacterId);transform.localScale=Vector3.one;}}
        private void OnDisable(){remaining=0;transform.localScale=Vector3.one;if(body!=null)body.Bind(body.CharacterId);}
    }
}
