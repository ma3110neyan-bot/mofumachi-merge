using System;

namespace Mofumachi.Core
{
    public enum TownRegionTravelPhase { WalkingOut, Fading, WalkingIn, Complete, Cancelled }

    // A portal transition is not a walk edge. Its midpoint must be acknowledged
    // by the owner before any movement in the next painting can begin.
    public sealed class TownRegionJourney
    {
        public const float FadeSeconds=.30f;
        private const float Midpoint=FadeSeconds/2;
        private readonly TownRegionLocation destination;
        private TownRegionPortal portal;
        private TownPoint lastPosition;
        private float fadeElapsed;
        private bool transferTaken;
        public TownRegionId Region { get; private set; }
        public TownPoint Target { get; private set; }
        public TownRegionTravelPhase Phase { get; private set; }
        public float FadeOpacity { get; private set; }
        public TownRegionLocation SettledLocation=>new TownRegionLocation(Region,lastPosition);
        public bool IsActive=>Phase!=TownRegionTravelPhase.Complete&&Phase!=TownRegionTravelPhase.Cancelled;

        public TownRegionJourney(TownRegionLocation start,TownRegionLocation destination,int level)
        {
            // Validate region values even when a caller supplies a default struct.
            TownRegionCatalog.ForLevel(start.Region,level);TownRegionCatalog.ForLevel(destination.Region,level);
            Region=start.Region;lastPosition=start.Local;this.destination=destination;SelectLeg();
        }
        private void SelectLeg()
        {
            fadeElapsed=0;transferTaken=false;FadeOpacity=0;
            if(Region==destination.Region){Target=destination.Local;Phase=TownRegionTravelPhase.WalkingIn;return;}
            var next=Region==TownRegionId.Central?destination.Region:TownRegionId.Central;
            if(!TownRegionCatalog.TryGetPortal(Region,next,out portal))throw new InvalidOperationException("No regional entrance connection");
            Target=portal.From.Local;Phase=TownRegionTravelPhase.WalkingOut;
        }
        public void Tick(TownPoint currentLocalPosition,float seconds)
        {
            if(!IsActive||!currentLocalPosition.Finite||currentLocalPosition.X<0||currentLocalPosition.X>1||currentLocalPosition.Y<0||currentLocalPosition.Y>1||!float.IsFinite(seconds)||seconds<=0)return;
            lastPosition=currentLocalPosition;float dt=Math.Min(seconds,.1f);
            if(Phase==TownRegionTravelPhase.WalkingOut)
            {
                if(TownPoint.Distance(currentLocalPosition,Target)<.001f){Phase=TownRegionTravelPhase.Fading;fadeElapsed=0;FadeOpacity=0;}
                return;
            }
            if(Phase==TownRegionTravelPhase.WalkingIn)
            {
                if(TownPoint.Distance(currentLocalPosition,Target)<.001f)Phase=TownRegionTravelPhase.Complete;
                return;
            }
            if(!transferTaken)
            {
                fadeElapsed=Math.Min(Midpoint,fadeElapsed+dt);FadeOpacity=fadeElapsed/Midpoint;
            }
            else
            {
                fadeElapsed=Math.Min(FadeSeconds,fadeElapsed+dt);FadeOpacity=1-(fadeElapsed-Midpoint)/Midpoint;
                if(fadeElapsed>=FadeSeconds)SelectLeg();
            }
        }
        public bool TryTakeTransfer(out TownRegionLocation entry)
        {
            entry=default;
            if(Phase!=TownRegionTravelPhase.Fading||transferTaken||fadeElapsed<Midpoint)return false;
            entry=portal.To;Region=entry.Region;lastPosition=entry.Local;transferTaken=true;FadeOpacity=1;return true;
        }
        public void Cancel()
        {
            if(!IsActive)return;
            Phase=TownRegionTravelPhase.Cancelled;FadeOpacity=0;Target=lastPosition;
        }
    }
}
