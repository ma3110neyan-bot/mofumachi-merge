using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mofumachi.Core;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class TownPlaceCatalogTests
    {
        // Reflection lets the first red run exercise a missing feature, rather than fail compilation.
        private static Type Catalog { get { var t=typeof(GameState).Assembly.GetType("Mofumachi.Core.TownPlaceCatalog");Assert.That(t,Is.Not.Null,"Growth-aware street catalog is missing");return t; } }
        private static object Map(int level)=>Catalog.GetMethod("ForLevel").Invoke(null,new object[]{level});
        private static T Read<T>(object value,string name)=>(T)value.GetType().GetProperty(name).GetValue(value);
        private static object[] Places(object map)=>Read<IEnumerable>(map,"Places").Cast<object>().ToArray();
        private static string[] Ids(object map)=>Places(map).Select(p=>Read<string>(p,"Id")).ToArray();
        [Test] public void ExistingEntrancesRemainAvailableAtLevelZero()
        {var m=Map(0);Assert.That(Ids(m),Does.Contain("quest").And.Contain("shop"));foreach(var p in Places(m).Where(p=>new[]{"quest","shop"}.Contains(Read<string>(p,"Id"))))Assert.That(Read<int>(p,"MinLevel"),Is.Zero);}
        [Test] public void EveryStageHasConnectedRoutes()
        {foreach(int level in new[]{0,2,4,6}){var m=Map(level);var nodes=Read<IReadOnlyList<TownPoint>>(m,"Nodes");var edges=Read<IReadOnlyList<TownEdge>>(m,"Edges");var reached=new HashSet<int>{0};bool changed;do{changed=false;foreach(var e in edges)if(reached.Contains(e.From)||reached.Contains(e.To)){changed|=reached.Add(e.From);changed|=reached.Add(e.To);}}while(changed);Assert.That(reached.Count,Is.EqualTo(nodes.Count),"level "+level);}}
        [Test] public void HigherLevelsRetainEarlierStopsAndEdges()
        {var previous=Map(0);foreach(int level in new[]{2,4,6}){var current=Map(level);Assert.That(Ids(current),Is.SupersetOf(Ids(previous)));Assert.That(Read<IReadOnlyList<TownPoint>>(current,"Nodes").Take(Read<IReadOnlyList<TownPoint>>(previous,"Nodes").Count),Is.EqualTo(Read<IReadOnlyList<TownPoint>>(previous,"Nodes")));Assert.That(Read<IReadOnlyList<TownEdge>>(current,"Edges"),Is.SupersetOf(Read<IReadOnlyList<TownEdge>>(previous,"Edges")));previous=current;}}
        [Test] public void StreetSegmentsAvoidSurveyedObstacles()
        {
            var map=Map(6);var n=Read<IReadOnlyList<TownPoint>>(map,"Nodes");
            foreach(var edge in Read<IReadOnlyList<TownEdge>>(map,"Edges"))for(int step=0;step<=100;step++)
            {float t=step/100f,x=n[edge.From].X+(n[edge.To].X-n[edge.From].X)*t,y=n[edge.From].Y+(n[edge.To].Y-n[edge.From].Y)*t;
             Assert.That(x>=0&&x<=1&&y>=0&&y<=1,Is.True);Assert.That(x>.32f&&x<.49f&&y>.525f&&y<.62f,Is.False,"fountain water");Assert.That(x>.33f&&x<.39f&&y>.35f&&y<.41f,Is.False,"foreground lamp plinth");}
        }
        [Test] public void NewStopsHaveExpectedUnlockLevels()
        {Assert.That(Ids(Map(0)),Does.Contain("bench-left").And.Contain("bench-right").And.Contain("home-front"));foreach(var pair in new[]{("flower-walk",2),("front-promenade",4),("celebration",6)}){Assert.That(Ids(Map(pair.Item2-1)),Does.Not.Contain(pair.Item1));Assert.That(Ids(Map(pair.Item2)),Does.Contain(pair.Item1));}}
        [Test] public void InvalidLevelsUseSafeDisplayStages()
        {Assert.That(Ids(Map(int.MinValue)),Is.EqualTo(Ids(Map(0))));Assert.That(Ids(Map(int.MaxValue)),Is.EqualTo(Ids(Map(6))));}
        [Test] public void UnknownPlacesAreRejected()
        {var args=new object[]{Map(6),"not-a-place",null};Assert.That(Catalog.GetMethod("TryFind").Invoke(null,args),Is.False);Assert.That(args[2],Is.Null);}
        [Test] public void CatalogCannotMutateGameState()
        {var s=GameState.CreateInitial();s.townGrowthLevel=4;string before=StateCodec.Encode(s);Map(s.townGrowthLevel);Assert.That(StateCodec.Encode(s),Is.EqualTo(before));var nodes=Read<IReadOnlyList<TownPoint>>(Map(0),"Nodes") as IList<TownPoint>;Assert.That(nodes,Is.Not.Null);Assert.That(()=>nodes[0]=new TownPoint(1,1),Throws.InstanceOf<NotSupportedException>());}
    }
}
