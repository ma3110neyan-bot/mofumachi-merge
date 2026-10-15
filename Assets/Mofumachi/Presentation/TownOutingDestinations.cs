using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    // Shared by the character menu and shortcuts, so an advertised facility
    // cannot drift away from its actual unlocked walking destination.
    public static class TownOutingDestinations
    {
        public static string ForRegion(TownRegionId region,int townLevel)=>region==TownRegionId.West?(townLevel>=2?"west.cafe-front":"west.flowers"):
            region==TownRegionId.Central?"fountain":region==TownRegionId.East?"east.flowers":null;
    }
}
