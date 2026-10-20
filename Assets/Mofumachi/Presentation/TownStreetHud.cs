using UnityEngine;
namespace Mofumachi.Presentation
{
    public sealed class TownStreetHud:MonoBehaviour
    {
        private TownLifeView life;private CanvasGroup top,next,merge;
        public void Bind(TownLifeView town,RectTransform hud,RectTransform goal,RectTransform mergeAction)
        {
            life=town;top=hud.gameObject.AddComponent<CanvasGroup>();next=goal.gameObject.AddComponent<CanvasGroup>();merge=mergeAction.gameObject.AddComponent<CanvasGroup>();Refresh();
        }
        private void Update()=>Refresh();
        public void Refresh()
        {
            if(life==null)return;
            bool walking=life.World?.Model?.Hero.IsWalking==true||life.IsManuallyDragging;
            bool speaking=life.IsSpeaking;
            // Keep global actions available; the small next-step note withdraws
            // while the player is looking at a friend or leading a walk.
            next.alpha=walking||speaking?0:1;next.blocksRaycasts=next.alpha>0;next.interactable=next.alpha>0;
            top.alpha=speaking?.58f:1;merge.alpha=speaking?.72f:1;
        }
    }
}
