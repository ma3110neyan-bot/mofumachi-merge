using System.Linq;
using UnityEngine;
namespace Mofumachi.Presentation
{
    // A short-lived invitation in this scene, not a saved quest or a remote talk.
    public sealed class TownMeetingJourney:MonoBehaviour
    {
        private TownLifeView life;private string friendId;private float elapsed;
        public bool IsActive=>friendId!=null;
        public bool Begin(TownLifeView source,string id)
        {
            Cancel();life=source;
            if(!life.CanInteract||life.World?.IsReady!=true||!life.World.Model.Residents.Any(r=>r.Id==id&&!r.Controlled))return false;
            friendId=id;elapsed=.5f;
            if(!Guide()){Cancel();return false;}return true;
        }
        private bool Guide()
        {
            var model=life.World.Model;var friend=model.Residents.First(r=>r.Id==friendId);
            model.Pause(friendId,.8f);
            foreach(var at in TownMeetingApproach.Candidates(model.Area,model.Hero.Position,friend.Position))if(life.World.GuidePoint(at,true))return true;
            return false;
        }
        private void Update()=>Advance(Time.unscaledDeltaTime);
        public void Advance(float seconds)
        {
            if(!IsActive)return;
            if(!float.IsFinite(seconds)||seconds<=0)return;
            if(life==null||!life.CanInteract||life.IsManuallyDragging){Cancel();return;}
            var model=life.World.Model;var friend=model.Residents.First(r=>r.Id==friendId);
            model.Pause(friendId,.8f);elapsed+=Mathf.Min(.1f,seconds);
            if(!model.Hero.IsWalking&&TownWalkableArea.Distance(model.Hero.Position,friend.Position)<.145f)
            {
                model.Face(model.Hero.Id,friend.Position);model.Face(friend.Id,model.Hero.Position);
                life.Actors.First(a=>a.CharacterId==friend.Id).Greet();Cancel();return;
            }
            if(elapsed>=.5f){elapsed=0;if(!model.Hero.IsWalking&&!Guide())Cancel();}
        }
        public void Cancel(){friendId=null;elapsed=0;}
        private void OnDisable()=>Cancel();
    }
}
