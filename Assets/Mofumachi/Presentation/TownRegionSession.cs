using System;
using System.Collections.Generic;
using System.Linq;
using Mofumachi.Core;
using UnityEngine;

namespace Mofumachi.Presentation
{
    // Home-only state: no state store, rewards, currency, or serialized region position.
    public sealed class TownRegionSession : MonoBehaviour
    {
        private sealed class Travel
        {
            internal TownActor Actor;
            internal TownRegionJourney Journey;
            internal TownPlace Destination;
            internal TownRegionId FadeFrom,FadeTo;
            internal TownRegionId? Gate;
            internal bool Started;
            internal bool TransferredThisFrame;
        }
        private readonly Dictionary<string,Travel> journeys=new Dictionary<string,Travel>();
        private readonly Dictionary<TownRegionId,string> gates=new Dictionary<TownRegionId,string>();
        private readonly Dictionary<string,float> nextNpcVisit=new Dictionary<string,float>();
        private float now;
        private int visitChoice;
        private ScreenContext context;
        private TownLifeView life;
        private IReadOnlyList<TownActor> actors;
        private TownActor hero;
        private bool followingHero=true;
        public TownRegionId HeroRegion=>hero==null?TownRegionId.Central:hero.RegionId;
        public TownRegionId ViewingRegion { get; private set; }=TownRegionId.Central;
        public bool HasHeroJourney=>hero!=null&&journeys.ContainsKey(hero.CharacterId);
        public TownPoint HeroLegTarget=>hero!=null&&journeys.TryGetValue(hero.CharacterId,out var leg)?leg.Journey.Target:hero?.Position??default;
        public string HeroDestinationId=>hero!=null&&journeys.TryGetValue(hero.CharacterId,out var trip)?trip.Destination.Id:null;
        public TownRegionId? HeroDestinationRegion=>HeroDestinationId==null?(TownRegionId?)null:FindDestinationRegion(HeroDestinationId);
        public bool IsHeroTransitioning=>hero!=null&&journeys.TryGetValue(hero.CharacterId,out var travel)&&travel.Journey.Phase==TownRegionTravelPhase.Fading;
        public event Action<TownRegionId> RegionChanged;
        public event Action<TownRegionId,TownRegionId,float> HeroTransitionChanged;

        public void Bind(ScreenContext c,TownLifeView view,IReadOnlyList<TownActor> residents)
        {
            context=c;life=view;actors=residents;hero=actors.Single(a=>a.Controlled);int index=0;
            foreach(var actor in actors.OrderBy(a=>CharacterCatalog.Find(a.CharacterId).PortraitIndex))
            {
                if(actor.Controlled)continue;
                var region=index<2?TownRegionId.West:index==2?TownRegionId.Central:TownRegionId.East;
                string id=index==0?"west.flowers":index==1?"west.bench-front":index==2?"bench-right":index==3?"east.flowers":"east.meeting";
                TownRegionCatalog.TryFindPlace(id,c.Game.State.townGrowthLevel,out _,out var place);
                actor.TransferToRegion(TownRegionCatalog.ForLevel(region,c.Game.State.townGrowthLevel),place.Position);
                actor.SetDestination(place,1.6f+index*.4f);nextNpcVisit[actor.CharacterId]=20+index*2;index++;
            }
            life.ApplyViewingRegion(ViewingRegion);
        }
        private void SetViewingRegion(TownRegionId region)
        {
            ViewingRegion=region;life.ApplyViewingRegion(region);RegionChanged?.Invoke(region);
        }
        public bool LookAtRegion(TownRegionId region)
        {
            if(!Enum.IsDefined(typeof(TownRegionId),region)||life==null||!life.CanInteract||life.IsManuallyDragging)return false;
            life.GetComponent<TownViewportView>()?.CancelGesture();followingHero=false;SetViewingRegion(region);return true;
        }
        public void ReturnToHero()
        {
            if(life==null||!life.CanInteract)return;
            followingHero=true;SetViewingRegion(HeroRegion);
            life.GetComponent<TownViewportView>()?.Focus(hero.Position);
            if(!HasHeroJourney)life.ActorReleased(hero);
        }
        public bool GuideToRegion(TownRegionId region)
        {
            if(!Enum.IsDefined(typeof(TownRegionId),region))return false;
            return GuideToPlace(region==TownRegionId.West?"west.flowers":region==TownRegionId.East?"east.flowers":"square");
        }
        public bool GuideToPlace(string id)
        {
            if(life==null||!life.CanInteract||life.IsManuallyDragging||!TownRegionCatalog.TryFindPlace(id,context.Game.State.townGrowthLevel,out var location,out var place))return false;
            // Repeated shortcut input follows the existing journey, never restarts
            // its exit path, gate reservation or fade clock.
            if(HeroDestinationId==id){followingHero=true;if(ViewingRegion!=HeroRegion)SetViewingRegion(HeroRegion);return true;}
            CancelHeroJourney();followingHero=true;SetViewingRegion(HeroRegion);
            var travel=new Travel {Actor=hero,Journey=new TownRegionJourney(new TownRegionLocation(hero.RegionId,hero.Position),location,context.Game.State.townGrowthLevel),Destination=place};
            journeys[hero.CharacterId]=travel;
            if(!BeginLeg(travel)){journeys.Remove(hero.CharacterId);return false;}
            return true;
        }
        private bool BeginLeg(Travel travel)
        {
            if(travel.Journey.Phase==TownRegionTravelPhase.WalkingOut)
            {
                var from=travel.Journey.Region;var to=from==TownRegionId.Central?FindDestinationRegion(travel.Destination.Id):TownRegionId.Central;
                var gate=from==TownRegionId.Central?to:from;
                if(gates.TryGetValue(gate,out var owner)&&owner!=travel.Actor.CharacterId)
                {travel.Actor.HoldForTransition();travel.Started=false;return true;}
                gates[gate]=travel.Actor.CharacterId;travel.Gate=gate;
            }
            bool started=travel.Actor.GuideForJourney(travel.Journey.Target);
            travel.Started=started;
            if(travel.Actor.Controlled)life.SetGuidedDestination(travel.Destination);
            return started;
        }
        internal bool OwnsJourney(TownActor actor)=>actor!=null&&journeys.ContainsKey(actor.CharacterId);
        internal bool IsStopReserved(string id,TownActor except)=>journeys.Values.Any(t=>t.Actor!=except&&t.Destination.Id==id);
        internal void BeginPresentationFrame(){foreach(var travel in journeys.Values)travel.TransferredThisFrame=false;}
        public bool TryScheduleNpcVisit(string characterId,TownRegionId region)
        {
            if(life==null||!life.CanSimulate||!Enum.IsDefined(typeof(TownRegionId),region))return false;
            var actor=actors.FirstOrDefault(a=>a.CharacterId==characterId&&!a.Controlled);
            if(actor==null||OwnsJourney(actor)||!nextNpcVisit.TryGetValue(characterId,out var next)||now<next||
                !TownRegionCatalog.TryGetPortal(actor.RegionId,region,out _))return false;
            // Residents still walking out count in their current region. Incoming
            // reservations count once until the entry transfer is acknowledged.
            int occupied=actors.Count(a=>!a.Controlled&&a.RegionId==region)+
                journeys.Values.Count(t=>!t.Actor.Controlled&&t.Actor.RegionId!=region&&FindDestinationRegion(t.Destination.Id)==region);
            var gate=actor.RegionId==TownRegionId.Central?region:actor.RegionId;
            if(occupied>=2||gates.ContainsKey(gate))return false;
            var candidates=TownRegionCatalog.ForLevel(region,context.Game.State.townGrowthLevel).Map.Places
                .Where(p=>p.Activity!=TownActivity.Entrance&&p.Id!="square"&&
                    !actors.Any(a=>!a.Controlled&&a.RegionId==region&&a.DestinationId==p.Id)&&
                    !journeys.Values.Any(t=>t.Destination.Id==p.Id)).ToArray();
            if(candidates.Length==0)return false;
            var place=candidates[visitChoice++%candidates.Length];
            var travel=new Travel {Actor=actor,Destination=place,Journey=new TownRegionJourney(new TownRegionLocation(actor.RegionId,actor.Position),new TownRegionLocation(region,place.Position),context.Game.State.townGrowthLevel)};
            journeys[characterId]=travel;
            if(!BeginLeg(travel)){ReleaseGate(travel);journeys.Remove(characterId);return false;}
            return true;
        }
        internal bool TryStartVisitAfterRest(TownActor actor)
        {
            if(actor==null||actor.Controlled||OwnsJourney(actor)||!nextNpcVisit.TryGetValue(actor.CharacterId,out var next)||now<next)return false;
            if(actor.RegionId!=TownRegionId.Central)return TryScheduleNpcVisit(actor.CharacterId,TownRegionId.Central);
            var first=CharacterCatalog.Find(actor.CharacterId).PortraitIndex%2==0?TownRegionId.West:TownRegionId.East;
            return TryScheduleNpcVisit(actor.CharacterId,first)||TryScheduleNpcVisit(actor.CharacterId,first==TownRegionId.West?TownRegionId.East:TownRegionId.West);
        }
        private void ReleaseGate(Travel travel)
        {
            if(travel.Gate.HasValue&&gates.TryGetValue(travel.Gate.Value,out var owner)&&owner==travel.Actor.CharacterId)gates.Remove(travel.Gate.Value);
            travel.Gate=null;
        }
        public void Step(float seconds)
        {
            if(life==null||!life.CanSimulate||!float.IsFinite(seconds)||seconds<=0)return;
            now+=Mathf.Min(seconds,.1f);
            // The same six objects are simulated once; invisible regions do not clone them.
            for(int i=0;i<actors.Count;i++)
            {
                var actor=actors[i];if(!journeys.TryGetValue(actor.CharacterId,out var travel))continue;
                if(travel.TransferredThisFrame)continue;
                if(!travel.Started){BeginLeg(travel);if(!travel.Started)continue;}
                var before=travel.Journey.Phase;travel.Journey.Tick(actor.Position,seconds);
                var phase=travel.Journey.Phase;
                if(phase==TownRegionTravelPhase.Fading&&before!=TownRegionTravelPhase.Fading)
                {
                    travel.FadeFrom=actor.RegionId;
                    travel.FadeTo=actor.RegionId==TownRegionId.Central?FindDestinationRegion(travel.Destination.Id):TownRegionId.Central;
                    actor.HoldForTransition();
                }
                if(phase==TownRegionTravelPhase.Fading)
                {
                    if(travel.Journey.TryTakeTransfer(out var entry))
                    {
                        travel.TransferredThisFrame=true;
                        actor.TransferToRegion(TownRegionCatalog.ForLevel(entry.Region,context.Game.State.townGrowthLevel),entry.Local);
                        actor.HoldForTransition();
                        if(actor.Controlled&&followingHero)SetViewingRegion(entry.Region);
                        else actor.SetRegionVisible(actor.RegionId==ViewingRegion);
                    }
                    if(actor.Controlled)
                    {
                        // A preview of another region is independent of an
                        // offscreen walk. Fade only the travelling resident there.
                        HeroTransitionChanged?.Invoke(travel.FadeFrom,travel.FadeTo,followingHero?travel.Journey.FadeOpacity:0);
                        actor.SetJourneyFade(followingHero?0:travel.Journey.FadeOpacity);
                    }
                    else actor.SetJourneyFade(travel.Journey.FadeOpacity);
                }
                else if(before==TownRegionTravelPhase.Fading)
                {
                    if(actor.Controlled)HeroTransitionChanged?.Invoke(travel.FadeFrom,travel.FadeTo,0);
                    actor.SetJourneyFade(0);
                    ReleaseGate(travel);
                    BeginLeg(travel);
                }
                if(travel.Journey.Phase==TownRegionTravelPhase.Complete)
                {
                    journeys.Remove(actor.CharacterId);actor.FinishRegionalGuide(travel.Destination);
                    if(!actor.Controlled)nextNpcVisit[actor.CharacterId]=now+20;
                    if(actor.Controlled){life.SetGuidedDestination(travel.Destination);life.ActorReleased(actor);}
                }
            }
        }
        private TownRegionId FindDestinationRegion(string id)
        {TownRegionCatalog.TryFindPlace(id,context.Game.State.townGrowthLevel,out var location,out _);return location.Region;}
        internal void CancelHeroJourney()
        {
            if(hero==null||!journeys.TryGetValue(hero.CharacterId,out var travel))return;
            CancelTravel(travel);journeys.Remove(hero.CharacterId);
        }
        private void CancelTravel(Travel travel)
        {
            ReleaseGate(travel);travel.Actor.SetJourneyFade(0);
            bool fading=travel.Journey.Phase==TownRegionTravelPhase.Fading;travel.Journey.Cancel();
            if(fading)
            {
                var settled=travel.Journey.SettledLocation;
                travel.Actor.TransferToRegion(TownRegionCatalog.ForLevel(settled.Region,context.Game.State.townGrowthLevel),settled.Local);
            }
            travel.Actor.FinishRegionalGuide(null);
            if(!travel.Actor.Controlled)nextNpcVisit[travel.Actor.CharacterId]=now+20;
            if(travel.Actor.Controlled)HeroTransitionChanged?.Invoke(travel.FadeFrom,travel.FadeTo,0);
        }
        public void CancelJourneys()
        {
            if(actors==null)return;
            for(int i=0;i<actors.Count;i++)if(journeys.TryGetValue(actors[i].CharacterId,out var travel))CancelTravel(travel);
            journeys.Clear();gates.Clear();if(followingHero&&life!=null&&ViewingRegion!=HeroRegion)SetViewingRegion(HeroRegion);
        }
        public TownRegionSnapshot CaptureSettledSnapshot()
        {
            CancelJourneys();life.GetComponent<TownViewportView>()?.CancelGesture();
            var saved=new TownRegionActorSnapshot[actors.Count];
            for(int i=0;i<actors.Count;i++)saved[i]=new TownRegionActorSnapshot(actors[i].CharacterId,new TownRegionLocation(actors[i].RegionId,actors[i].Position),actors[i].DestinationId);
            var view=life.GetComponent<TownRegionView>();var centers=view==null?new[]{new TownPoint(.5f,.5f),new TownPoint(.5f,.5f),new TownPoint(.5f,.5f)}:view.CaptureCenters();
            return new TownRegionSnapshot(hero.CharacterId,ViewingRegion,saved,centers);
        }
        public void RestoreSnapshot(TownRegionSnapshot snapshot)
        {
            if(snapshot==null||hero==null||snapshot.FavoriteId!=hero.CharacterId)return;
            CancelJourneys();
            foreach(var saved in snapshot.Actors)
            {
                var actor=actors.FirstOrDefault(a=>a.CharacterId==saved.CharacterId);if(actor==null)continue;
                actor.TransferToRegion(TownRegionCatalog.ForLevel(saved.Location.Region,context.Game.State.townGrowthLevel),saved.Location.Local);
                TownPlace place=null;
                if(TownRegionCatalog.TryFindPlace(saved.PlaceId,context.Game.State.townGrowthLevel,out var location,out var found)&&location.Region==actor.RegionId)place=found;
                actor.FinishRegionalGuide(place);
            }
            followingHero=snapshot.ViewingRegion==HeroRegion;SetViewingRegion(snapshot.ViewingRegion);
            life.GetComponent<TownRegionView>()?.RestoreCenters(snapshot.CameraCenters);
        }
        private void OnDisable()=>CancelJourneys();
    }
}
