using System;
using System.Collections.Generic;
using System.Linq;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    public sealed class TownWorldResident
    {
        public string Id {get;} public bool Controlled {get;}
        public TownPoint Position {get;internal set;} public TownPoint Previous {get;internal set;}
        public TownPoint Target {get;internal set;} public string PlaceId {get;internal set;}
        public bool IsWalking {get;internal set;} public float Rest {get;internal set;}
        public float Facing {get;internal set;} public float Speed {get;internal set;} public float Stride {get;internal set;}
        public float DistanceWalked {get;internal set;}
        public TownRegionId Region=>TownWorldLayout.RegionAt(Position);
        public TownDistrictId District=>TownDistrictLayout.At(Position);
        internal bool CrossedDistrict;
        internal IReadOnlyList<TownPoint> Route=Array.Empty<TownPoint>();internal int Waypoint;internal float Waiting,NoProgress,BestRemaining;internal bool AvoidingResidents;
        public float PauseRemaining {get;internal set;}
        internal TownWorldResident(string id,bool controlled,TownPoint p){Id=id;Controlled=controlled;Position=Previous=Target=p;}
    }
    public sealed class TownWorldWalkModel
    {
        private static readonly TownWalkableArea SharedArea=new TownWalkableArea(0);
        private static readonly TownAreaNavigation SharedNavigation=new TownAreaNavigation(SharedArea);
        private readonly TownAreaNavigation navigation;
        private readonly List<TownWorldResident> residents=new List<TownWorldResident>();private readonly int level;
        private int choice;public IReadOnlyList<TownWorldResident> Residents=>residents;
        public TownWorldResident Hero {get;}
        public TownWalkableArea Area {get;}
        public bool IsFollowingFinger {get;private set;}
        private bool finishingFinger;
        public event Action<string,string,bool> Arrived;
        public TownWorldWalkModel(int level,string favorite)
        {
            if(!CharacterCatalog.IsKnown(favorite))throw new ArgumentException("Unknown resident",nameof(favorite));this.level=level;
            Area=SharedArea;navigation=SharedNavigation;int npc=0;
            string[] starts={"square","west.bench-front","east.flowers","bench-left","front-promenade"};
            foreach(var c in CharacterCatalog.All)
            {
                bool hero=c.Id==favorite;string place=hero?"square":starts[npc++];TownRegionCatalog.TryFindPlace(place,level,out var loc,out _);
                var p=!hero&&place=="square"?TownCentralMasterLayout.ArrivalFriend:TownWorldMap.Place(place);if(Area.TryClamp(p,out var floor,.12f))p=floor;
                var actor=new TownWorldResident(c.Id,hero,p){PlaceId=place,Rest=2+residents.Count*.5f};residents.Add(actor);if(hero)Hero=actor;
            }
        }
        public bool TryGuide(string id,string place)
        {
            var actor=residents.FirstOrDefault(a=>a.Id==id);if(actor==null||!TownRegionCatalog.TryFindPlace(place,level,out var loc,out _))return false;
            if(actor.Controlled){IsFollowingFinger=finishingFinger=false;GiveWay(TownWorldMap.Place(place));}
            return Guide(actor,TownWorldMap.Place(place),place);
        }
        public bool TryGuideDistrict(TownDistrictId district)
        {
            if(!Enum.IsDefined(typeof(TownDistrictId),district))return false;
            string id=TownDistrictLayout.EntranceId(district);IsFollowingFinger=finishingFinger=false;
            GiveWay(TownWorldMap.Place(id));return Guide(Hero,TownWorldMap.Place(id),id);
        }
        private bool Guide(TownWorldResident actor,TownPoint target,string place,bool restart=true,bool avoidResidents=false)
        {
            if(!Area.TryClamp(target,out target,.09f))return false;
            var occupied=avoidResidents?residents.Where(a=>a!=actor).Select(a=>a.Position).ToArray():null;
            var route=navigation.FindRoute(actor.Position,target,occupied);if(route.Count==0)return false;
            if(restart){actor.CrossedDistrict=false;actor.DistanceWalked=0;}actor.Target=target;actor.PlaceId=place;actor.Rest=0;actor.PauseRemaining=0;
            actor.Route=route;actor.AvoidingResidents=avoidResidents;actor.Waypoint=0;actor.Waiting=actor.NoProgress=0;actor.BestRemaining=Remaining(actor);actor.IsWalking=TownWorldLayout.Distance(actor.Position,target)>.0001f;return true;
        }
        public bool TryGuidePoint(TownPoint point)
        {
            if(!Area.TryClamp(point,out var projected,.075f))return false;
            foreach(TownRegionId region in Enum.GetValues(typeof(TownRegionId)))foreach(var place in TownRegionCatalog.ForLevel(region,level).Map.Places)
                if(TownWorldLayout.Distance(point,TownWorldMap.Place(place.Id))<.025f)return TryGuide(Hero.Id,place.Id);
            IsFollowingFinger=finishingFinger=false;GiveWay(projected);return Guide(Hero,projected,null);
        }
        // Direct following has no tap-exit shortcut: the character follows the
        // moving finger inside floor boundaries, including across region joins.
        public bool FollowFinger(TownPoint point)
        {
            if(!Area.TryClamp(point,out var target,.8f))return false;
            if(!IsFollowingFinger){Hero.CrossedDistrict=false;Hero.DistanceWalked=0;IsFollowingFinger=true;finishingFinger=false;}
            if(TownWalkableArea.Distance(Hero.Target,target)<.018f&&Hero.IsWalking)return true;
            return Guide(Hero,target,null,false);
        }
        public void ReleaseFinger()
        {
            if(!IsFollowingFinger)return;IsFollowingFinger=false;finishingFinger=true;
            var to=Hero.Waypoint<Hero.Route.Count?Hero.Route[Hero.Waypoint]:Hero.Target;float distance=TownWorldLayout.Distance(Hero.Position,to);
            float coast=Math.Min(.010f,Hero.Speed*.08f);
            var stop=distance<.0001f?Hero.Position:new TownPoint(Hero.Position.X+(to.X-Hero.Position.X)*coast/distance,Hero.Position.Y+(to.Y-Hero.Position.Y)*coast/distance);
            if(distance<coast||!Area.CanTravel(Hero.Position,stop))stop=Hero.Position;
            if(TownWorldLayout.Distance(Hero.Position,stop)<.0001f){Hero.IsWalking=false;Hero.Speed=0;Complete(Hero);}
            else Guide(Hero,stop,null,false);
        }
        public TownPoint SampleHeroRoute(float alpha)
        {
            alpha=Math.Max(0,Math.Min(1,alpha));float length=0;var p=Hero.Position;
            for(int i=Hero.Waypoint;i<Hero.Route.Count;i++){length+=TownWorldLayout.Distance(p,Hero.Route[i]);p=Hero.Route[i];}
            float remaining=length*alpha;p=Hero.Position;
            for(int i=Hero.Waypoint;i<Hero.Route.Count;i++){var next=Hero.Route[i];float d=TownWorldLayout.Distance(p,next);if(remaining<=d&&d>0)return new TownPoint(p.X+(next.X-p.X)*remaining/d,p.Y+(next.Y-p.Y)*remaining/d);remaining-=d;p=next;}
            return p;
        }
        public void Pause(string id,float seconds)
        {if(!float.IsFinite(seconds)||seconds<=0)return;var actor=residents.FirstOrDefault(a=>a.Id==id);if(actor!=null){actor.PauseRemaining=Math.Max(actor.PauseRemaining,Math.Min(4,seconds));actor.Speed=0;}}
        public void Face(string id,TownPoint other)
        {var actor=residents.FirstOrDefault(a=>a.Id==id);if(actor!=null&&TownWorldLayout.Finite(other)&&Math.Abs(other.X-actor.Position.X)>.002f)actor.Facing=Math.Sign(other.X-actor.Position.X);}
        public void CancelHero(){IsFollowingFinger=finishingFinger=false;Hero.IsWalking=false;Hero.Speed=0;Hero.Target=Hero.Position;Hero.Route=Array.Empty<TownPoint>();}
        public void Restore(string id,TownPoint p,string place=null)
        {
            var actor=residents.FirstOrDefault(a=>a.Id==id);if(actor==null||!Area.TryClamp(p,out var point,.12f))return;
            actor.Position=actor.Previous=actor.Target=point;actor.PlaceId=place;actor.IsWalking=false;actor.Speed=0;actor.Rest=2;actor.PauseRemaining=0;actor.Route=Array.Empty<TownPoint>();
        }
        public void FastTravel(TownRegionId region)
        {
            if(!Enum.IsDefined(typeof(TownRegionId),region))return;
            TravelTo(region==TownRegionId.West?"west.entry":region==TownRegionId.East?"east.entry":"square");
        }
        public void FastTravel(TownDistrictId district)
        {
            if(!Enum.IsDefined(typeof(TownDistrictId),district))return;
            TravelTo(TownDistrictLayout.EntranceId(district));
        }
        private void TravelTo(string id)
        {
            CancelHero();Hero.Position=Hero.Previous=Hero.Target=TownWorldMap.Place(id);Hero.PlaceId=id;Hero.PauseRemaining=0;
            Hero.CrossedDistrict=false;Hero.DistanceWalked=0;GiveWay(Hero.Position);Arrived?.Invoke(Hero.Id,id,false);
        }
        public void Step(float seconds)
        {
            if(!float.IsFinite(seconds)||seconds<=0)return;float dt=Math.Min(seconds,1f/30);
            foreach(var actor in residents)
            {
                actor.Previous=actor.Position;
                if(actor.PauseRemaining>0){actor.PauseRemaining=Math.Max(0,actor.PauseRemaining-dt);continue;}
                if(!actor.IsWalking)
                {
                    actor.Speed=0;actor.Rest=Math.Max(0,actor.Rest-dt);if(actor.Controlled)continue;
                    if(actor.Rest==0)ChooseStop(actor);continue;
                }
                while(!actor.AvoidingResidents&&actor.Waypoint<actor.Route.Count-1&&Area.CanTravel(actor.Position,actor.Route[actor.Waypoint+1]))actor.Waypoint++;
                var waypoint=actor.Waypoint<actor.Route.Count?actor.Route[actor.Waypoint]:actor.Target;
                // Local passing must never strand an actor on the wrong side of
                // a flower bed with an obsolete waypoint behind that obstacle.
                if(!Area.CanTravel(actor.Position,waypoint))
                {if(!Guide(actor,actor.Target,actor.PlaceId,false))continue;waypoint=actor.Route[0];}
                float distance=TownWorldLayout.Distance(actor.Position,waypoint),top=actor.Controlled?.12f:.047f;
                actor.Speed=Math.Min(top,actor.Speed+top*dt/.16f);float speed=actor.Speed*Math.Max(.3f,Math.Min(1,TownWorldLayout.Distance(actor.Position,actor.Target)/.025f));
                float budget=Math.Min(distance,speed*dt);var next=Steer(actor,waypoint,budget);
                float moved=TownWorldLayout.Distance(actor.Position,next);actor.Waiting=moved<.000001f?actor.Waiting+dt:0;
                if(moved>.000001f){float x=next.X-actor.Position.X;if(Math.Abs(x)>.000001f)actor.Facing=Math.Sign(x);actor.Stride=(actor.Stride+moved*140)%(float)(Math.PI*2);}
                if(TownDistrictLayout.At(next)!=actor.District)actor.CrossedDistrict=true;
                actor.Position=next;actor.DistanceWalked+=moved;
                if(TownWorldLayout.Distance(next,waypoint)<.001f&&actor.Waypoint<actor.Route.Count-1)actor.Waypoint++;
                if(TownWorldLayout.Distance(next,actor.Target)<.001f)
                {actor.Position=actor.Target;actor.IsWalking=false;actor.Speed=0;actor.Rest=actor.PlaceId?.Contains("bench")==true?4:2;Complete(actor);}
                else if(!actor.Controlled&&actor.Waiting>1.5f){actor.IsWalking=false;actor.Rest=.2f;actor.PlaceId=null;}
                else
                {
                    float remaining=Remaining(actor);
                    if(remaining<actor.BestRemaining-.003f){actor.BestRemaining=remaining;actor.NoProgress=0;}else actor.NoProgress+=dt;
                    if(actor.NoProgress>2)
                    {actor.NoProgress=0;if(actor.Controlled)GiveWay(waypoint);Guide(actor,actor.Target,actor.PlaceId,false,true);}
                }
            }
        }
        private void Complete(TownWorldResident actor)
        {
            if(actor.Controlled&&IsFollowingFinger)return;
            bool walked=actor.CrossedDistrict;actor.CrossedDistrict=false;if(actor.Controlled&&finishingFinger)finishingFinger=false;
            Arrived?.Invoke(actor.Id,actor.PlaceId,walked);
        }
        private static float Remaining(TownWorldResident actor)
        {float length=0;var previous=actor.Position;for(int i=actor.Waypoint;i<actor.Route.Count;i++){length+=TownWorldLayout.Distance(previous,actor.Route[i]);previous=actor.Route[i];}return length;}
        private TownPoint Steer(TownWorldResident actor,TownPoint target,float budget)
        {
            if(budget<=0)return actor.Position;
            float dx=target.X-actor.Position.X,dy=(target.Y-actor.Position.Y)*TownWorldLayout.ImageAspect;
            float length=(float)Math.Sqrt(dx*dx+dy*dy);if(length<.000001f)return actor.Position;dx/=length;dy/=length;
            float sideways=0;
            foreach(var other in residents)if(other!=actor)
            {
                float x=other.Position.X-actor.Position.X,y=(other.Position.Y-actor.Position.Y)*TownWorldLayout.ImageAspect;
                float along=x*dx+y*dy,across=x*dy-y*dx;
                // Start yielding while the bodies are still visibly apart.
                // Keep the established hard foot clearance for entrances and
                // conversations; widening that hard radius strands short visits.
                if(along>-.018f&&along<.26f&&Math.Abs(across)<.15f)sideways=Math.Max(sideways,(.26f-Math.Max(0,along))/.26f*2f);
            }
            for(int option=0;option<4;option++)
            {
                var heading=option==0?new TownPoint(dx+dy*sideways,(dy-dx*sideways)/TownWorldLayout.ImageAspect):option==1?new TownPoint(dx,dy/TownWorldLayout.ImageAspect):option==2?new TownPoint(dy,-dx/TownWorldLayout.ImageAspect):new TownPoint(-dy,dx/TownWorldLayout.ImageAspect);
                float h=TownWorldLayout.Distance(default,heading);if(h<.00001f)continue;
                var next=new TownPoint(actor.Position.X+heading.X/h*budget,actor.Position.Y+heading.Y/h*budget);
                if(TownWorldLayout.Distance(actor.Position,target)<=budget+.000001f&&sideways==0)next=target;
                if(!Area.CanTravel(actor.Position,next)||!Area.CanTravel(next,target))continue;bool clear=true;
                foreach(var other in residents)if(other!=actor)
                {
                    float before=TownWalkableArea.Distance(actor.Position,other.Position),after=TownWalkableArea.Distance(next,other.Position);
                    if(after<Math.Min(.067f,before)-.00001f){clear=false;break;}
                }
                if(clear)return next;
            }
            return actor.Position;
        }
        private void GiveWay(TownPoint target)
        {
            foreach(var other in residents)if(!other.Controlled&&TownWalkableArea.Distance(other.Position,target)<.105f)
            foreach(var offset in new[]{new TownPoint(.10f,0),new TownPoint(-.10f,0),new TownPoint(0,.055f),new TownPoint(0,-.055f)})
            {
                var p=new TownPoint(target.X+offset.X,target.Y+offset.Y);
                if(Area.Contains(p)&&residents.All(a=>a==other||TownWalkableArea.Distance(a.Position,p)>.08f)&&Guide(other,p,null))break;
            }
        }
        private bool Reserved(string place,TownWorldResident actor)=>residents.Any(a=>a!=actor&&a.PlaceId==place);
        private void ChooseStop(TownWorldResident actor)
        {
            var region=actor.Region;int turn=choice++;
            if(turn%7==0)
            {
                var next=region==TownRegionId.Central?(turn%2==0?TownRegionId.West:TownRegionId.East):TownRegionId.Central;
                var visit=TownRegionCatalog.ForLevel(next,level).Map.Places.FirstOrDefault(p=>p.Activity!=TownActivity.Entrance&&!Reserved(p.Id,actor));
                if(visit!=null&&residents.Count(a=>!a.Controlled&&a!=actor&&TownWorldLayout.RegionAt(a.Target)==next)<2&&TryGuide(actor.Id,visit.Id))return;
            }
            var places=TownRegionCatalog.ForLevel(region,level).Map.Places.Where(p=>p.Activity!=TownActivity.Entrance&&p.Id!=actor.PlaceId&&!Reserved(p.Id,actor)).ToArray();
            for(int i=0;i<places.Length;i++)if(TryGuide(actor.Id,places[(turn+CharacterCatalog.Find(actor.Id).PortraitIndex+i)%places.Length].Id))return;
            actor.Rest=2;
        }
    }
}
