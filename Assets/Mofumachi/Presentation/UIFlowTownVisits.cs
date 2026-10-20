using System;
using System.Linq;
using Mofumachi.Core;
using UnityEngine;
namespace Mofumachi.Presentation
{
    public sealed partial class UIFlowController
    {
        private TownLifeView ActiveTown=>page!=null?page.GetComponentInChildren<TownLifeView>():null;
        public string TownCharacterDestination(string id)
        {
            if(!CharacterCatalog.IsKnown(id))return "街の仲間";
            var resident=ActiveTown?.World?.Model?.Residents.FirstOrDefault(r=>r.Id==id);
            if(resident!=null)
            {
                string place=!resident.IsWalking&&TownWorldMap.TryPlace(resident.PlaceId,out var stop)&&TownWalkableArea.Distance(stop,resident.Position)<.09f?resident.PlaceId:null;
                return TownCharacterBook.PlaceLabel(place,resident.District);
            }
            var snapshot=HomeWorldSnapshot;
            if(snapshot!=null)
            {
                int i=Array.IndexOf(snapshot.Ids,id);
                if(i>=0)
                {
                    string place=TownWorldMap.TryPlace(snapshot.Places[i],out var stop)&&TownWalkableArea.Distance(stop,snapshot.Positions[i])<.09f?snapshot.Places[i]:null;
                    return TownCharacterBook.PlaceLabel(place,TownDistrictLayout.At(snapshot.Positions[i]));
                }
            }
            var initial=new TownWorldWalkModel(Game.State.townGrowthLevel,Game.State.favoriteCharacterId);
            var friend=initial.Residents.First(r=>r.Id==id);
            return TownCharacterBook.PlaceLabel(friend.PlaceId,friend.District);
        }
        public bool VisitTownCharacter(string id)
        {
            if(appPaused||transitioning||!Game.State.purchaseNoticeAcknowledged||!CharacterCatalog.IsKnown(id)||!CharacterCatalog.IsKnown(Game.State.favoriteCharacterId))return false;
            if(IsModalOpen)
            {if(modalKind!=ModalKind.Today&&modalKind!=ModalKind.TownMenu)return false;CloseUtilityModal();}
            if(CurrentScreen!=ScreenId.Home)ShowHome();
            var life=ActiveTown;
            if(CurrentScreen!=ScreenId.Home||life?.World?.IsReady!=true||!life.CanInteract)return false;
            if(id==life.World.Model.Hero.Id)
            {
                // Visiting oneself is not a conversation. Invite another nearby
                // friend for a walk without manufacturing a journal entry.
                id=life.World.Model.Residents.Where(r=>!r.Controlled).OrderBy(r=>TownWalkableArea.Distance(r.Position,life.World.Model.Hero.Position)).First().Id;
            }
            var journey=life.GetComponent<TownMeetingJourney>()??life.gameObject.AddComponent<TownMeetingJourney>();
            return journey.Begin(life,id);
        }
        private bool AtTownFacility(string id)
        {
            var life=ActiveTown;string destination=TownFacilityEntry.DestinationFor(id);
            return destination!=null&&life?.World?.IsReady==true&&!life.World.Model.Hero.IsWalking&&TownWalkableArea.Distance(life.World.Model.Hero.Position,TownWorldMap.Place(destination))<.055f;
        }
        public void OpenCafe()
        {if(CanPlay&&CurrentScreen==ScreenId.Home&&AtTownFacility("cafe"))OpenUtilityModal(ModalKind.Cafe,TownPlacesView.BuildCafe);}
        public void CloseCafe(){if(modalKind==ModalKind.Cafe)CloseUtilityModal();}
        internal string CafeFriend()
        {
            var life=ActiveTown;if(life?.World?.IsReady!=true)return null;
            var at=TownWorldMap.Place("west.cafe-front");
            return life.World.Model.Residents.Where(r=>!r.Controlled&&TownWalkableArea.Distance(r.Position,at)<.38f).OrderBy(r=>TownWalkableArea.Distance(r.Position,at)).FirstOrDefault()?.Id;
        }
        public bool TalkAtCafe(string id)
        {
            if(appPaused||transitioning||modalKind!=ModalKind.Cafe||!AtTownFacility("cafe")||id!=CafeFriend())return false;
            bool recorded=TalkToCharacter(id);if(!recorded)return false;
            var life=ActiveTown;var hero=life.Actors.First(a=>a.Controlled);var friend=life.Actors.First(a=>a.CharacterId==id);
            hero.FaceTowards(friend.Position);friend.FaceTowards(hero.Position);hero.Greet();friend.Greet();
            OpenUtilityModal(ModalKind.Cafe,TownPlacesView.BuildCafe);return true;
        }
        public void VisitFromCafe(string id){if(modalKind!=ModalKind.Cafe)return;CloseUtilityModal();VisitTownCharacter(id);}
        public void MakeFlowersFromShop()
        {
            if(modalKind!=ModalKind.Shop||appPaused||transitioning)return;
            CloseUtilityModal();ProductionItemId="flower";ShowMerge();
        }
    }
}
