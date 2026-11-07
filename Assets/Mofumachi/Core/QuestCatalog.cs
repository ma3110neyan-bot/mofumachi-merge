using System;
using System.Collections.Generic;
using System.Linq;
namespace Mofumachi.Core
{
    public static class QuestCatalog
    {
        public static IReadOnlyList<QuestDefinition> Story { get; }=Array.AsReadOnly(new[]{
            QuestDefinition.First,
            new QuestDefinition("bread-01",new[]{new ItemRequirement("bread",2,1)},40,"momo","焼きたてのおすそわけ","みんなで、おいしいパンを食べよう！"),
            new QuestDefinition("flower-01",new[]{new ItemRequirement("flower",2,1)},40,"miel","小さな花束","お茶会のお部屋に、お花を飾りたいな。"),
            new QuestDefinition("tea-02",new[]{new ItemRequirement("tea",3,1),new ItemRequirement("bread",2,1)},70,"pom","のんびりお茶会","香りのよいお茶とパンで、ひと休みしよう。"),
            new QuestDefinition("flower-02",new[]{new ItemRequirement("flower",3,1),new ItemRequirement("tea",2,1)},70,"fio","花いっぱいの広場","広場をお花で彩って、みんなを迎えよう！"),
            new QuestDefinition("bread-02",new[]{new ItemRequirement("bread",3,1),new ItemRequirement("flower",2,1)},80,"noah","街のごちそう会","パンと花束をそろえて、街のお祝いをしよう。")});
        public static IReadOnlyList<QuestDefinition> Additional { get; }=Array.AsReadOnly(new[]{
            Extra("loop-a-01",0,30,new ItemRequirement("tea",1,2),new ItemRequirement("bread",1,2),new ItemRequirement("flower",2,1)),
            Extra("loop-a-02",1,40,new ItemRequirement("tea",2,1),new ItemRequirement("bread",2,2),new ItemRequirement("flower",2,2)),
            Extra("loop-a-03",2,50,new ItemRequirement("tea",3,1),new ItemRequirement("bread",3,2),new ItemRequirement("flower",2,2)),
            Extra("loop-a-04",3,60,new ItemRequirement("tea",3,2),new ItemRequirement("bread",2,2),new ItemRequirement("flower",3,2)),
            Extra("loop-a-05",4,70,new ItemRequirement("tea",2,2),new ItemRequirement("bread",3,3),new ItemRequirement("flower",3,2)),
            Extra("loop-a-06",5,80,new ItemRequirement("tea",3,3),new ItemRequirement("bread",3,2),new ItemRequirement("flower",3,2))});
        private static QuestDefinition Extra(string id,int presentation,int coins,params ItemRequirement[] requirements)
        {var copy=Story[presentation];return new QuestDefinition(id,requirements,coins,copy.RequesterId,copy.Title,copy.Dialogue);}
        public static IReadOnlyList<QuestDefinition> All { get; }=Array.AsReadOnly(Story.Concat(Additional).ToArray());
        public static IReadOnlyList<QuestDefinition> NewPlayerOrder { get; }=Array.AsReadOnly(new[]{
            Story[0],Story[1],Additional[0],Story[2],Additional[1],Story[3],Additional[2],Additional[3],Story[4],Additional[4],Additional[5],Story[5]});
        public static IReadOnlyList<QuestDefinition> OrderFor(GameState state)=>
            state.economy.questRoute==QuestProgressionRoute.ExpandedNewPlayer?NewPlayerOrder:All;
        public static int CompletedCount(GameState state)
        {var done=new HashSet<string>(state.completedQuestIds,StringComparer.Ordinal);return All.Count(q=>done.Contains(q.Id));}
        public static bool Story6Complete(GameState state)=>Story.All(q=>state.completedQuestIds.Contains(q.Id));
        public static bool All12Complete(GameState state)=>CompletedCount(state)==All.Count;
        public static QuestDefinition NextFor(GameState state)
        {
            if(state==null)throw new ArgumentNullException(nameof(state));
            if(state.activeQuestId.Length>0 && !state.completedQuestIds.Contains(state.activeQuestId))
                return All.FirstOrDefault(q=>q.Id==state.activeQuestId);
            return OrderFor(state).FirstOrDefault(q=>!state.completedQuestIds.Contains(q.Id));
        }
    }
}
