using System;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    public enum TownEntryPhase { Idle,Walking,Opening,Entered }
    // A doorway is a destination, never a remote menu shortcut.
    public sealed class TownFacilityEntry
    {
        public TownEntryPhase Phase {get;private set;}
        public string Facility {get;private set;}
        public float OpenAmount {get;private set;}
        public bool Request(string id)
        {if(id!="quest"&&id!="shop")return false;Facility=id;Phase=TownEntryPhase.Walking;OpenAmount=0;return true;}
        public void Cancel(){Facility=null;Phase=TownEntryPhase.Idle;OpenAmount=0;}
        public void Step(TownPoint feet,bool walking,float seconds)
        {
            if(Phase==TownEntryPhase.Idle||Phase==TownEntryPhase.Entered||!float.IsFinite(seconds)||seconds<=0)return;
            bool atDoor=TownWalkableArea.Distance(feet,TownWorldMap.Place(Facility))<=.032f;
            if(Phase==TownEntryPhase.Walking){if(!atDoor||walking)return;Phase=TownEntryPhase.Opening;return;}
            if(!atDoor||walking){Cancel();return;}
            OpenAmount=Math.Min(1,OpenAmount+Math.Min(seconds,.1f)/.55f);
            if(OpenAmount>=1)Phase=TownEntryPhase.Entered;
        }
    }
}
