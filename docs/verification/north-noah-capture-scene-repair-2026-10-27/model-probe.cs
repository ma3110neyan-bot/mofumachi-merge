using System;using System.Linq;using System.Text.Json;using Mofumachi.Core;using Mofumachi.Presentation;
var routed=new TownWorldWalkModel(6,"noah");var target=new TownPoint(.30f,.95f);
if(!routed.TryGuidePoint(target))throw new Exception("Route unavailable");int steps=0;
while(routed.Hero.IsWalking&&steps++<6000)routed.Step(1f/30);
if(routed.Hero.IsWalking||TownWalkableArea.Distance(routed.Hero.Position,target)>.002f)throw new Exception("Route did not arrive");
var fresh=new TownWorldWalkModel(6,"noah");var friend=fresh.Residents.Single(r=>r.Id=="momo");
if(TownWalkableArea.Distance(fresh.Hero.Position,TownWorldMap.Place("square"))>.002f||TownWalkableArea.Distance(friend.Position,TownCentralMasterLayout.ArrivalFriend)>.002f)throw new Exception("Fresh original anchors differ");
Console.WriteLine(JsonSerializer.Serialize(new{kind="actual-production-model-cpu-probe-not-Unity",route_steps=steps,routed_hero=new[]{routed.Hero.Position.X,routed.Hero.Position.Y},fresh_hero=new[]{fresh.Hero.Position.X,fresh.Hero.Position.Y},fresh_momo=new[]{friend.Position.X,friend.Position.Y},fresh_anchors_verified=true}));