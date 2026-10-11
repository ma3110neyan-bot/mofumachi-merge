using System;
using System.Collections.Generic;
using System.Linq;
namespace Mofumachi.Core
{
    public sealed class ItemDefinition
    {
        public string Id { get; }
        public string Name { get; }
        internal ItemDefinition(string id,string name){Id=id;Name=name;}
    }
    public static class ItemCatalog
    {
        public static IReadOnlyList<ItemDefinition> All { get; }=Array.AsReadOnly(new[]{
            new ItemDefinition("tea","お茶"),new ItemDefinition("bread","パン"),new ItemDefinition("flower","お花")});
        public static ItemDefinition Find(string id)=>All.FirstOrDefault(i=>string.Equals(i.Id,id,StringComparison.Ordinal));
    }
}
