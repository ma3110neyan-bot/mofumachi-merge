using UnityEngine;
using UnityEngine.EventSystems;
namespace Mofumachi.Presentation
{
    public sealed class BodyReaction : MonoBehaviour,IPointerClickHandler
    {
        private ScreenContext context;private CharacterBodyGraphic body;private float until;
        public void Bind(ScreenContext c){context=c;body=GetComponent<CharacterBodyGraphic>();}
        public void OnPointerClick(PointerEventData e){if(context==null||context.Flow.IsModalOpen)return;until=Time.unscaledTime+.65f;body.Bind(body.CharacterId,CharacterExpression.Smile);context.Flow.PlayCue(AudioCue.Character);}
        private void Update(){if(body==null)return;if(Time.unscaledTime<until)transform.localScale=Vector3.one*(1+.035f*Mathf.Sin((until-Time.unscaledTime)/.65f*Mathf.PI));else if(body.Expression!=CharacterExpression.Normal){body.Bind(body.CharacterId);transform.localScale=Vector3.one;}}
        private void OnDisable(){until=0;transform.localScale=Vector3.one;}
    }
}
