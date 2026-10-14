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
        }
        private readonly Dictionary<string,Travel> journeys=new Dictionary<string,Travel>();
        private ScreenContext context;
        private TownLifeView life;
        private IReadOnlyList<TownActor> actors;
        private TownActor hero;
        private bool followingHero=true;
        public TownRegionId HeroRegion=>hero==null?TownRegionId.Central:hero.RegionId;
        public TownRegionId ViewingRegion { get; private set; }=TownRegionId.Central;
        public bool HasHeroJourney=>hero!=null&&journeys.ContainsKey(hero.CharacterId);
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
                actor.SetDestination(place,1.6f+index*.4f);index++;
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
            CancelHeroJourney();followingHero=true;SetViewingRegion(HeroRegion);
            var travel=new Travel {Actor=hero,Journey=new TownRegionJourney(new TownRegionLocation(hero.RegionId,hero.Position),location,context.Game.State.townGrowthLevel),Destination=place};
            journeys[hero.CharacterId]=travel;
            if(!BeginLeg(travel)){journeys.Remove(hero.CharacterId);return false;}
            return true;
        }
        private bool BeginLeg(Travel travel)
        {
            bool started=travel.Actor.GuideForJourney(travel.Journey.Target);
            if(travel.Actor.Controlled)life.SetGuidedDestination(travel.Destination);
            return started;
        }
        internal bool OwnsJourney(TownActor actor)=>actor!=null&&journeys.ContainsKey(actor.CharacterId);
        public void Step(float seconds)
        {
            if(life==null||!life.CanSimulate||!float.IsFinite(seconds)||seconds<=0)return;
            // The same six objects are simulated once; invisible regions do not clone them.
            for(int i=0;i<actors.Count;i++)
            {
                var actor=actors[i];if(!journeys.TryGetValue(actor.CharacterId,out var travel))continue;
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
                        actor.TransferToRegion(TownRegionCatalog.ForLevel(entry.Region,context.Game.State.townGrowthLevel),entry.Local);
                        actor.HoldForTransition();
                        if(actor.Controlled&&followingHero)SetViewingRegion(entry.Region);
                        else actor.SetRegionVisible(actor.RegionId==ViewingRegion);
                    }
                    if(actor.Controlled)HeroTransitionChanged?.Invoke(travel.FadeFrom,travel.FadeTo,travel.Journey.FadeOpacity);
                }
                else if(before==TownRegionTravelPhase.Fading)
                {
                    if(actor.Controlled)HeroTransitionChanged?.Invoke(travel.FadeFrom,travel.FadeTo,0);
                    BeginLeg(travel);
                }
                if(travel.Journey.Phase==TownRegionTravelPhase.Complete)
                {
                    journeys.Remove(actor.CharacterId);actor.FinishRegionalGuide(travel.Destination);
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
            bool fading=travel.Journey.Phase==TownRegionTravelPhase.Fading;travel.Journey.Cancel();
            if(fading)
            {
                var settled=travel.Journey.SettledLocation;
                travel.Actor.TransferToRegion(TownRegionCatalog.ForLevel(settled.Region,context.Game.State.townGrowthLevel),settled.Local);
            }
            travel.Actor.FinishRegionalGuide(null);
            if(travel.Actor.Controlled)HeroTransitionChanged?.Invoke(travel.FadeFrom,travel.FadeTo,0);
        }
        public void CancelJourneys()
        {
            if(actors==null)return;
            for(int i=0;i<actors.Count;i++)if(journeys.TryGetValue(actors[i].CharacterId,out var travel))CancelTravel(travel);
            journeys.Clear();if(followingHero&&life!=null)SetViewingRegion(HeroRegion);
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
