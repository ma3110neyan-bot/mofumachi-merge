using UnityEngine;
using UnityEngine.EventSystems;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    public sealed class BodyReaction : MonoBehaviour,IPointerClickHandler
    {
        private ScreenContext context;private CharacterBodyGraphic body;private float remaining,elapsed;
        private Vector3 restScale=Vector3.one;private Quaternion restRotation=Quaternion.identity;
        public void Bind(ScreenContext c)
        {context=c;body=GetComponent<CharacterBodyGraphic>();restScale=transform.localScale;restRotation=transform.localRotation;}
        public void Celebrate()
        {
            if(!isActiveAndEnabled||body==null||context==null||context.Flow.IsModalOpen||context.Flow.IsAppPaused)return;
            remaining=.65f;body.Bind(body.CharacterId,CharacterExpression.Smile);
        }
        public void OnPointerClick(PointerEventData e)
        {
            if(!isActiveAndEnabled||body==null||context==null||context.Flow.IsModalOpen||context.Flow.IsAppPaused)return;
            Celebrate();context.Flow.PlayCue(AudioCue.Character);
        }
        private void Update()
        {
            if(body==null||context==null||context.Flow.IsModalOpen||context.Flow.IsAppPaused)return;
            float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);elapsed+=dt;remaining=Mathf.Max(0,remaining-dt);
            int resident=CharacterCatalog.Find(body.CharacterId)?.PortraitIndex??0;
            var idle=TownCharacterMotion.Sample(0,0,0,0,elapsed,resident);
            var reaction=TownCharacterMotion.Reaction(remaining,.65f,false);
            float scale=1+idle.Breath+reaction.Hop*.003f;
            transform.localScale=new Vector3(restScale.x*scale,restScale.y*scale,restScale.z);
            transform.localRotation=restRotation*Quaternion.Euler(0,0,idle.Lean*.45f+reaction.Lean*.65f);
            body.Pose(0,idle.Hand+reaction.Hand,idle.Look,remaining<=0&&TownCharacterMotion.Blink(elapsed,resident));
            if(remaining<=0&&body.Expression!=CharacterExpression.Normal)body.Bind(body.CharacterId);
        }
        private void OnDisable()
        {
            remaining=elapsed=0;transform.localScale=restScale;transform.localRotation=restRotation;
            if(body!=null){body.Bind(body.CharacterId);body.Pose(0,0,0,false);}
        }
    }
}
