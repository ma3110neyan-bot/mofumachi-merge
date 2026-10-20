using System;
using System.Collections.Generic;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    public static class TownMeetingApproach
    {
        public static IReadOnlyList<TownPoint> Candidates(TownWalkableArea area,TownPoint hero,TownPoint friend)
        {
            if(area==null||!TownWorldLayout.Finite(hero)||!TownWorldLayout.Finite(friend)||!area.Contains(friend))return Array.Empty<TownPoint>();
            var points=new List<TownPoint>();
            for(int ring=0;ring<2;ring++)for(int i=0;i<12;i++)
            {
                double angle=i*Math.PI/6;float radius=ring==0?.10f:.12f;
                var p=new TownPoint(friend.X+(float)Math.Cos(angle)*radius,friend.Y+(float)Math.Sin(angle)*radius);
                if(area.Contains(p)&&area.CanTravel(friend,p))points.Add(p);
            }
            points.Sort((a,b)=>TownWalkableArea.Distance(hero,a).CompareTo(TownWalkableArea.Distance(hero,b)));
            return points.AsReadOnly();
        }
    }
}
