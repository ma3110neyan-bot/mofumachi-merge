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
        public TownRegionId Region=>TownWorldLayout.RegionAt(Position);
        internal TownRegionId StartedRegion;
        public float PauseRemaining {get;internal set;}
        internal TownWorldResident(string id,bool controlled,TownPoint p){Id=id;Controlled=controlled;Position=Previous=Target=p;}
    }
    public sealed class TownWorldWalkModel
    {
        private readonly TownPoint[] nodes;private readonly TownEdge[] edges;private readonly TownNavigation navigation;
        private readonly List<TownWorldResident> residents=new List<TownWorldResident>();private readonly int level;
        private int choice;public IReadOnlyList<TownWorldResident> Residents=>residents;
        public TownWorldResident Hero {get;}
        public event Action<string,string,bool> Arrived;
        public TownWorldWalkModel(int level,string favorite)
        {
            if(!CharacterCatalog.IsKnown(favorite))throw new ArgumentException("Unknown resident",nameof(favorite));this.level=level;
            TownWorldLayout.BuildGraph(level,out nodes,out edges);navigation=new TownNavigation(nodes,edges,.06f);int npc=0;
            string[] starts={"west.flowers","west.bench-front","bench-right","east.flowers","east.meeting"};
            foreach(var c in CharacterCatalog.All)
            {
                bool hero=c.Id==favorite;string place=hero?"square":starts[npc++];TownRegionCatalog.TryFindPlace(place,level,out var loc,out _);
                var actor=new TownWorldResident(c.Id,hero,TownWorldLayout.ToWorld(loc.Region,loc.Local)){PlaceId=place,Rest=2+residents.Count*.5f};residents.Add(actor);if(hero)Hero=actor;
            }
        }
        public bool TryGuide(string id,string place)
        {
            var actor=residents.FirstOrDefault(a=>a.Id==id);if(actor==null||!TownRegionCatalog.TryFindPlace(place,level,out var loc,out _))return false;
            return Guide(actor,TownWorldLayout.ToWorld(loc.Region,loc.Local),place);
        }
        private bool Guide(TownWorldResident actor,TownPoint target,string place)
        {if(!TownWorldLayout.Finite(target))return false;actor.StartedRegion=actor.Region;actor.Target=target;actor.PlaceId=place;actor.Rest=0;actor.PauseRemaining=0;actor.IsWalking=TownWorldLayout.Distance(actor.Position,target)>.0001f;return true;}
        public bool TryGuidePoint(TownPoint point)
        {
            if(!TownWorldLayout.TryProject(point,nodes,edges,out var projected))return false;
            // Road exits are part of the painting, not duplicated buttons. Keep
            // the north exit beyond the request-room doorway so both remain tappable.
            var regionNow=Hero.Region;
            if(regionNow==TownRegionId.Central&&TownWorldLayout.Distance(projected,new TownPoint(.13f,.72f))<.06f)return TryGuide(Hero.Id,"west.flowers");
            if(regionNow==TownRegionId.Central&&TownWorldLayout.Distance(projected,new TownPoint(.52f,.32f))<.06f)return TryGuide(Hero.Id,"east.flowers");
            if(regionNow==TownRegionId.West&&TownWorldLayout.Distance(projected,new TownPoint(.94f,1.45f))<.06f)return TryGuide(Hero.Id,"square");
            if(regionNow==TownRegionId.East&&TownWorldLayout.Distance(projected,new TownPoint(.09f,-.40f))<.06f)return TryGuide(Hero.Id,"square");
            foreach(TownRegionId region in Enum.GetValues(typeof(TownRegionId)))foreach(var place in TownRegionCatalog.ForLevel(region,level).Map.Places)
                if(TownWorldLayout.Distance(point,TownWorldLayout.ToWorld(region,place.Position))<.035f)return TryGuide(Hero.Id,place.Id);
            return Guide(Hero,projected,null);
        }
        public TownPoint SampleHeroRoute(float alpha)=>navigation.InterpolateAlongPath(Hero.Position,Hero.Target,alpha);
        public void Pause(string id,float seconds)
        {if(!float.IsFinite(seconds)||seconds<=0)return;var actor=residents.FirstOrDefault(a=>a.Id==id);if(actor!=null){actor.PauseRemaining=Math.Max(actor.PauseRemaining,Math.Min(4,seconds));actor.Speed=0;}}
        public void Face(string id,TownPoint other)
        {var actor=residents.FirstOrDefault(a=>a.Id==id);if(actor!=null&&TownWorldLayout.Finite(other)&&Math.Abs(other.X-actor.Position.X)>.002f)actor.Facing=Math.Sign(other.X-actor.Position.X);}
        public void CancelHero(){Hero.IsWalking=false;Hero.Speed=0;Hero.Target=Hero.Position;}
        public void Restore(string id,TownPoint p,string place=null)
        {
            var actor=residents.FirstOrDefault(a=>a.Id==id);if(actor==null||!TownWorldLayout.TryProject(p,nodes,edges,out var point,.12f))return;
            actor.Position=actor.Previous=actor.Target=point;actor.PlaceId=place;actor.IsWalking=false;actor.Speed=0;actor.Rest=2;actor.PauseRemaining=0;
        }
        public void FastTravel(TownRegionId region)
        {
            if(!Enum.IsDefined(typeof(TownRegionId),region))return;
            string id=region==TownRegionId.West?"west.entry":region==TownRegionId.East?"east.entry":"square";
            TownRegionCatalog.TryFindPlace(id,level,out var loc,out _);Hero.Position=Hero.Previous=Hero.Target=TownWorldLayout.ToWorld(region,loc.Local);Hero.PlaceId=id;Hero.IsWalking=false;Hero.Speed=0;Hero.PauseRemaining=0;Arrived?.Invoke(Hero.Id,id,false);
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
                    actor.Speed=0;if(actor.Controlled)continue;actor.Rest=Math.Max(0,actor.Rest-dt);
                    if(actor.Rest==0)ChooseStop(actor);continue;
                }
                float top=actor.Controlled?.12f:.035f;actor.Speed=Math.Min(top,actor.Speed+top*dt/.16f);
                float remaining=navigation.DistanceAlongPath(actor.Position,actor.Target);float speed=actor.Speed*Math.Max(.35f,Math.Min(1,remaining/.025f));
                // Yield slightly while passing, but never stop on a shared path.
                if(!actor.Controlled&&residents.Any(a=>a!=actor&&TownWorldLayout.Distance(a.Position,actor.Position)<.025f&&(a.Controlled||CharacterCatalog.Find(a.Id).PortraitIndex<CharacterCatalog.Find(actor.Id).PortraitIndex)))speed*=.5f;
                var next=navigation.Move(actor.Position,actor.Target,speed*dt);float distance=TownWorldLayout.Distance(actor.Position,next);
                if(distance>.000001f){float x=next.X-actor.Position.X;if(Math.Abs(x)>.000001f)actor.Facing=Math.Sign(x);actor.Stride=(actor.Stride+distance*140)%(float)(Math.PI*2);}
                actor.Position=next;
                if(TownWorldLayout.Distance(next,actor.Target)<.0001f)
                {actor.Position=actor.Target;actor.IsWalking=false;actor.Speed=0;actor.Rest=actor.PlaceId?.Contains("bench")==true?4:2;Arrived?.Invoke(actor.Id,actor.PlaceId,actor.StartedRegion!=actor.Region);}
            }
        }
        private bool Reserved(string place,TownWorldResident actor)=>residents.Any(a=>a!=actor&&!a.Controlled&&a.PlaceId==place);
        private void ChooseStop(TownWorldResident actor)
        {
            var region=actor.Region;int turn=choice++;
            if(turn%7==0)
            {
                var next=region==TownRegionId.Central?(turn%2==0?TownRegionId.West:TownRegionId.East):TownRegionId.Central;
                string visit=next==TownRegionId.West?"west.flowers":next==TownRegionId.East?"east.flowers":"square";
                if(residents.Count(a=>!a.Controlled&&a!=actor&&TownWorldLayout.RegionAt(a.Target)==next)<2&&!Reserved(visit,actor))
                {TryGuide(actor.Id,visit);return;}
            }
            var places=TownRegionCatalog.ForLevel(region,level).Map.Places.Where(p=>p.Activity!=TownActivity.Entrance&&p.Id!=actor.PlaceId&&!Reserved(p.Id,actor)).ToArray();
            if(places.Length==0){actor.Rest=2;return;}
            var place=places[(turn+CharacterCatalog.Find(actor.Id).PortraitIndex)%places.Length];TryGuide(actor.Id,place.Id);
        }
    }
}
