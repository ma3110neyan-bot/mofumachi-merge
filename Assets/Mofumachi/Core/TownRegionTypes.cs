using System;
using System.Collections.Generic;

namespace Mofumachi.Core
{
    public enum TownRegionId { West, Central, East }

    public readonly struct TownRegionLocation
    {
        public TownRegionId Region { get; }
        public TownPoint Local { get; }
        public TownRegionLocation(TownRegionId region,TownPoint local)
        {
            if(!Enum.IsDefined(typeof(TownRegionId),region))throw new ArgumentOutOfRangeException(nameof(region));
            if(!local.Finite||local.X<0||local.X>1||local.Y<0||local.Y>1)throw new ArgumentOutOfRangeException(nameof(local));
            Region=region;Local=local;
        }
    }

    public readonly struct TownRegionPortal
    {
        public TownRegionLocation From { get; }
        public TownRegionLocation To { get; }
        internal TownRegionPortal(TownRegionLocation from,TownRegionLocation to){From=from;To=to;}
    }

    public sealed class TownRegionDefinition
    {
        public TownRegionId Id { get; }
        public string Name { get; }
        public TownPoint WorldOrigin { get; }
        public string BackgroundResource { get; }
        public TownStreetMap Map { get; }
        internal TownRegionDefinition(TownRegionId id,string name,TownPoint origin,string resource,TownStreetMap map)
        {Id=id;Name=name;WorldOrigin=origin;BackgroundResource=resource;Map=map;}
    }

    public readonly struct TownRegionActorSnapshot
    {
        public string CharacterId { get; }
        public TownRegionLocation Location { get; }
        public string PlaceId { get; }
        public TownRegionActorSnapshot(string characterId,TownRegionLocation location,string placeId)
        {
            if(!CharacterCatalog.IsKnown(characterId))throw new ArgumentException("Unknown resident",nameof(characterId));
            CharacterId=characterId;Location=location;PlaceId=placeId??"";
        }
    }
    // Transient scene reconstruction data. Never registered with the save codec.
    public sealed class TownRegionSnapshot
    {
        public string FavoriteId { get; }
        public TownRegionId ViewingRegion { get; }
        public IReadOnlyList<TownRegionActorSnapshot> Actors { get; }
        public IReadOnlyList<TownPoint> CameraCenters { get; }
        public TownRegionSnapshot(string favoriteId,TownRegionId viewingRegion,IReadOnlyList<TownRegionActorSnapshot> actors,IReadOnlyList<TownPoint> cameraCenters)
        {
            if(!CharacterCatalog.IsKnown(favoriteId)||!Enum.IsDefined(typeof(TownRegionId),viewingRegion)||actors==null||actors.Count!=6||cameraCenters==null||cameraCenters.Count!=3)throw new ArgumentException("Invalid transient town snapshot");
            var copy=new TownRegionActorSnapshot[6];var ids=new HashSet<string>();
            for(int i=0;i<copy.Length;i++)
            {
                if(!CharacterCatalog.IsKnown(actors[i].CharacterId)||!ids.Add(actors[i].CharacterId))throw new ArgumentException("Invalid resident snapshot");
                copy[i]=actors[i];
            }
            if(!ids.Contains(favoriteId))throw new ArgumentException("Favorite resident is missing");
            var centers=new TownPoint[3];
            for(int i=0;i<centers.Length;i++)
            {
                var point=cameraCenters[i];if(!point.Finite||point.X<0||point.X>1||point.Y<0||point.Y>1)throw new ArgumentException("Invalid camera center");centers[i]=point;
            }
            FavoriteId=favoriteId;ViewingRegion=viewingRegion;Actors=Array.AsReadOnly(copy);CameraCenters=Array.AsReadOnly(centers);
        }
    }
}
