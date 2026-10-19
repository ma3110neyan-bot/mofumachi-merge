using System;
using System.Collections.Generic;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    // Presentation only: these IDs are never serialized into the Core save.
    public enum TownDistrictId {West=0,Central=1,East=2,North=3,South=4}
    public static class TownDistrictLayout
    {
        public const float RoomWidth=1.2f,RoomHeight=2f;
        public static IReadOnlyList<TownDistrictId> All {get;}=Array.AsReadOnly(new[]{TownDistrictId.West,TownDistrictId.Central,TownDistrictId.East,TownDistrictId.North,TownDistrictId.South});
        public static TownPoint Center(TownDistrictId id)
        {
            switch(id){case TownDistrictId.West:return new TownPoint(-1.2f,0);case TownDistrictId.Central:return new TownPoint(0,0);case TownDistrictId.East:return new TownPoint(1.2f,0);case TownDistrictId.North:return new TownPoint(0,2);case TownDistrictId.South:return new TownPoint(0,-2);default:throw new ArgumentOutOfRangeException(nameof(id));}
        }
        public static TownDistrictId At(TownPoint p)
        {if(p.Y>1)return TownDistrictId.North;if(p.Y< -1)return TownDistrictId.South;if(p.X< -.6f)return TownDistrictId.West;if(p.X>.6f)return TownDistrictId.East;return TownDistrictId.Central;}
        public static TownRegionId LegacyRegion(TownDistrictId id)
        {Center(id);return id==TownDistrictId.West?TownRegionId.West:id==TownDistrictId.East?TownRegionId.East:TownRegionId.Central;}
        public static string Name(TownDistrictId id)
        {switch(id){case TownDistrictId.West:return "西のカフェ広場";case TownDistrictId.Central:return "中央の噴水広場";case TownDistrictId.East:return "東の花屋広場";case TownDistrictId.North:return "北のおねがい広場";case TownDistrictId.South:return "南の庭園広場";default:throw new ArgumentOutOfRangeException(nameof(id));}}
        public static string EntranceId(TownDistrictId id)
        {switch(id){case TownDistrictId.West:return "district.west.entry";case TownDistrictId.Central:return "district.central.entry";case TownDistrictId.East:return "district.east.entry";case TownDistrictId.North:return "district.north.entry";case TownDistrictId.South:return "district.south.entry";default:throw new ArgumentOutOfRangeException(nameof(id));}}
    }
}
