using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mofumachi.Core;
using NUnit.Framework;

namespace Mofumachi.Tests
{
    public sealed class TownRegionCatalogTests
    {
        private static Type Type(string name)
        {
            var type=typeof(GameState).Assembly.GetType("Mofumachi.Core."+name);
            Assert.That(type,Is.Not.Null,name+" is missing");return type;
        }
        private static object Region(string name)=>Enum.Parse(Type("TownRegionId"),name);
        private static T Read<T>(object value,string property)=>(T)value.GetType().GetProperty(property).GetValue(value);
        private static object Definition(string region,int level)=>Type("TownRegionCatalog").GetMethod("ForLevel").Invoke(null,new[]{Region(region),(object)level});
        private static TownStreetMap Map(string region,int level)=>Read<TownStreetMap>(Definition(region,level),"Map");
        private static object Location(string region,TownPoint point)=>Activator.CreateInstance(Type("TownRegionLocation"),Region(region),point);
        private static bool Find(string id,int level,out object location,out TownPlace place)
        {
            var args=new object[]{id,level,null,null};bool found=(bool)Type("TownRegionCatalog").GetMethod("TryFindPlace").Invoke(null,args);
            location=args[2];place=(TownPlace)args[3];return found;
        }
        private static bool Portal(string from,string to,out object portal)
        {
            var args=new[]{Region(from),Region(to),null};bool found=(bool)Type("TownRegionCatalog").GetMethod("TryGetPortal").Invoke(null,args);
            portal=args[2];return found;
        }
        [Test] public void ThreeRegionsKeepOriginalSizeAndSeparateWorldOrigins()
        {
            foreach(var item in new[]{("West",-1f,"TownWestBackground"),("Central",0f,"TownBackground"),("East",1f,"TownEastBackground")})
            {
                var definition=Definition(item.Item1,0);var origin=Read<TownPoint>(definition,"WorldOrigin");
                Assert.That(origin.X,Is.EqualTo(item.Item2));Assert.That(origin.Y,Is.Zero);
                Assert.That(Read<string>(definition,"BackgroundResource"),Is.EqualTo("Mofumachi/"+item.Item3));
                var world=(TownPoint)Type("TownRegionCatalog").GetMethod("ToWorld").Invoke(null,new[]{Location(item.Item1,new TownPoint(.2f,.3f))});
                Assert.That(world.X,Is.EqualTo(item.Item2+.2f).Within(.0001f));Assert.That(world.Y,Is.EqualTo(.3f));
            }
            foreach(int level in new[]{0,2,4,6})Assert.That(Map("Central",level),Is.SameAs(TownPlaceCatalog.ForLevel(level)));
        }
        [Test] public void PortalsAreAdjacentAndNeverStreetEdges()
        {
            foreach(var link in new[]{("Central","West",.15f,.52f,.94f,.55f),("West","Central",.94f,.55f,.15f,.52f),
                ("Central","East",.68f,.515f,.09f,.50f),("East","Central",.09f,.50f,.68f,.515f)})
            {
                Assert.That(Portal(link.Item1,link.Item2,out var portal),Is.True);
                var from=Read<object>(portal,"From");var to=Read<object>(portal,"To");
                Assert.That(Read<object>(from,"Region").ToString(),Is.EqualTo(link.Item1));Assert.That(Read<object>(to,"Region").ToString(),Is.EqualTo(link.Item2));
                Assert.That(Read<TownPoint>(from,"Local").X,Is.EqualTo(link.Item3));Assert.That(Read<TownPoint>(from,"Local").Y,Is.EqualTo(link.Item4));
                Assert.That(Read<TownPoint>(to,"Local").X,Is.EqualTo(link.Item5));Assert.That(Read<TownPoint>(to,"Local").Y,Is.EqualTo(link.Item6));
            }
            Assert.That(Portal("West","East",out _),Is.False);Assert.That(Portal("Central","Central",out _),Is.False);
            Assert.That(Map("Central",6).Edges,Is.EqualTo(TownPlaceCatalog.ForLevel(6).Edges),"No fictitious edge through central flower beds");
        }
        [Test] public void OriginalFacilitiesKeepTheirIdsAndCoordinates()
        {
            foreach(var item in new[]{("quest",.18f,.635f),("shop",.76f,.605f)})foreach(int level in new[]{0,2,4,6})
            {
                Assert.That(Find(item.Item1,level,out var location,out var place),Is.True);
                Assert.That(Read<object>(location,"Region").ToString(),Is.EqualTo("Central"));
                Assert.That(place.Id,Is.EqualTo(item.Item1));Assert.That(place.Position.X,Is.EqualTo(item.Item2));Assert.That(place.Position.Y,Is.EqualTo(item.Item3));
            }
        }
        // Independently surveyed bounds, not the production catalog's own exclusion data.
        private static readonly (string region,float left,float right,float bottom,float top,string name)[] Obstacles={
            ("West",.18f,.46f,.60f,.71f,"cafe table and chairs"),
            ("West",.54f,.64f,.57f,.62f,"cafe flower baskets"),
            ("West",.72f,.89f,.46f,.535f,"upper garden island"),
            ("West",.13f,.43f,.39f,.475f,"bench and bordering flowers"),
            ("West",.50f,.97f,.12f,.34f,"lower garden and reserved lawn"),
            ("East",.29f,.57f,.51f,.60f,"central flower island"),
            ("East",.71f,.95f,.45f,.52f,"bench and plants"),
            ("East",.68f,.98f,.58f,.73f,"workshop and furniture"),
            ("East",.65f,.97f,.14f,.38f,"reserved lawn")};
        [Test] public void SideStopsStayOnConnectedSurveyedPaths()
        {
            foreach(string region in new[]{"West","East"})foreach(int level in new[]{0,2,4,6})
            {
                var map=Map(region,level);var reached=new HashSet<int>{0};bool changed;
                do {changed=false;foreach(var edge in map.Edges)if(reached.Contains(edge.From)||reached.Contains(edge.To)){changed|=reached.Add(edge.From);changed|=reached.Add(edge.To);}}while(changed);
                Assert.That(reached.Count,Is.EqualTo(map.Nodes.Count));
                foreach(var edge in map.Edges)for(int i=0;i<=100;i++)
                {
                    var a=map.Nodes[edge.From];var b=map.Nodes[edge.To];float x=a.X+(b.X-a.X)*i/100f,y=a.Y+(b.Y-a.Y)*i/100f;
                    Assert.That(x,Is.InRange(0,1));Assert.That(y,Is.InRange(0,1));
                    foreach(var obstacle in Obstacles.Where(o=>o.region==region))
                        Assert.That(x>obstacle.left-.012f&&x<obstacle.right+.012f&&y>obstacle.bottom-.012f&&y<obstacle.top+.012f,Is.False,region+" "+obstacle.name);
                }
                foreach(var place in map.Places)Assert.That(map.Nodes.Contains(place.Position),Is.True,place.Id);
            }
        }
        [Test] public void LevelsRetainEarlierSideStops()
        {
            foreach(string region in new[]{"West","East"})
            {
                var previous=Map(region,0);foreach(int level in new[]{2,4,6})
                {
                    var next=Map(region,level);Assert.That(next.Places.Select(p=>p.Id),Is.SupersetOf(previous.Places.Select(p=>p.Id)));
                    Assert.That(next.Edges,Is.SupersetOf(previous.Edges));previous=next;
                }
                Assert.That(Map(region,int.MinValue).Places.Select(p=>p.Id),Is.EqualTo(Map(region,0).Places.Select(p=>p.Id)));
            }
            Assert.That(Find("west.cafe-front",1,out _,out _),Is.False);Assert.That(Find("west.cafe-front",2,out _,out _),Is.True);
            Assert.That(Find("east.workshop-front",3,out _,out _),Is.False);Assert.That(Find("east.workshop-front",4,out _,out _),Is.True);
            foreach(var id in new[]{"west.bench-front","west.flowers","west.meeting","east.bench-front","east.flowers","east.meeting"})Assert.That(Find(id,0,out _,out _),Is.True,id);
        }
        [Test] public void UnknownRegionsAndPlacesAreRejected()
        {
            Assert.That(Find("unknown",6,out _,out _),Is.False);Assert.That(Find(null,6,out _,out _),Is.False);
            var invalid=Enum.ToObject(Type("TownRegionId"),42);
            var error=Assert.Throws<TargetInvocationException>(()=>Type("TownRegionCatalog").GetMethod("ForLevel").Invoke(null,new[]{invalid,(object)0}));
            Assert.That(error.InnerException,Is.InstanceOf<ArgumentOutOfRangeException>());
        }
        [Test] public void CatalogCannotChangeProgressOrSave()
        {
            var state=GameState.CreateInitial();string before=StateCodec.Encode(state);
            foreach(string region in new[]{"West","Central","East"})
            {
                var map=Map(region,6);var list=map.Nodes as IList<TownPoint>;
                Assert.That(list,Is.Not.Null);Assert.That(()=>list[0]=new TownPoint(1,1),Throws.InstanceOf<NotSupportedException>());
            }
            Assert.That(StateCodec.Encode(state),Is.EqualTo(before));
        }
    }
}
