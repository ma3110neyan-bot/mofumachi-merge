using System;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    public enum TownEntryPhase { Idle,Walking,Arrived,Opening,Entered }
    // A doorway is a destination, never a remote menu shortcut.
    public sealed class TownFacilityEntry
    {
        public TownEntryPhase Phase {get;private set;}
        public string Facility {get;private set;}
        public float OpenAmount {get;private set;}
        public static string DestinationFor(string id)=>id=="cafe"?"west.cafe-front":id=="quest"||id=="shop"?id:null;
        public bool OwnsArrival(string id)=>Phase!=TownEntryPhase.Idle&&Facility==id;
        public bool CanStartAmbientConversation(bool heroWalking)=>!heroWalking&&Phase==TownEntryPhase.Idle;
        public bool ShowsSign(string id,bool near)=>id!="cafe"&&id!="shop"||near||OwnsArrival(id);
        public bool Request(string id)
        {if(DestinationFor(id)==null)return false;Facility=id;Phase=TownEntryPhase.Walking;OpenAmount=0;return true;}
        public bool TapDoor(string id,TownPoint feet,bool walking)
        {
            if(DestinationFor(id)==null||!TownWorldLayout.Finite(feet)||walking||Phase==TownEntryPhase.Opening||Phase==TownEntryPhase.Entered||TownWalkableArea.Distance(feet,TownWorldMap.Place(DestinationFor(id)))>.032f)return false;
            Facility=id;Phase=TownEntryPhase.Opening;OpenAmount=0;return true;
        }
        public void Cancel(){Facility=null;Phase=TownEntryPhase.Idle;OpenAmount=0;}
        public void Step(TownPoint feet,bool walking,float seconds)
        {
            if(Phase==TownEntryPhase.Idle||Phase==TownEntryPhase.Entered||!float.IsFinite(seconds)||seconds<=0)return;
            bool atDoor=TownWalkableArea.Distance(feet,TownWorldMap.Place(DestinationFor(Facility)))<=.032f;
            if(Phase==TownEntryPhase.Walking){if(!atDoor||walking)return;Phase=TownEntryPhase.Arrived;return;}
            if(!atDoor||walking){Cancel();return;}
            if(Phase==TownEntryPhase.Arrived)return;
            OpenAmount=Math.Min(1,OpenAmount+Math.Min(seconds,.1f)/.55f);
            if(OpenAmount>=1)Phase=TownEntryPhase.Entered;
        }
    }
}
