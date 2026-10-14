using System;

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
}
