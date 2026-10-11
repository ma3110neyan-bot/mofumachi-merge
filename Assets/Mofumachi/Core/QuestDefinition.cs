using System;
using System.Collections.Generic;
using System.Linq;

namespace Mofumachi.Core
{
    public sealed class ItemRequirement
    {
        public string ItemId { get; }
        public int Level { get; }
        public int Count { get; }
        public ItemRequirement(string id, int level, int count)
        {
            if (string.IsNullOrWhiteSpace(id) || level < 1 || level > GameState.MaxLevel || count < 1) throw new ArgumentException("Invalid item requirement.");
            ItemId = id; Level = level; Count = count;
        }
    }
    public sealed class QuestDefinition
    {
        public static readonly QuestDefinition First = new QuestDefinition("tea-01", new[] { new ItemRequirement("tea", 2, 1) }, 30, dialogue:"お茶を届けてくれる？ みんなでお茶会をしよう！");
        public string Id { get; }
        public IReadOnlyList<ItemRequirement> Requirements { get; }
        public int Coins { get; }
        public string RequesterId { get; }
        public string Title { get; }
        public string Dialogue { get; }
        public QuestDefinition(string id, IEnumerable<ItemRequirement> requirements, int coins,string requesterId="lulu",string title="お茶会の準備",string dialogue="")
        {
            if (string.IsNullOrWhiteSpace(id) || requirements == null || coins < 0) throw new ArgumentException("Invalid quest.");
            var list = requirements.ToList();
            if (list.Count == 0 || list.Any(i => i == null)) throw new ArgumentException("Quest must have requirements.");
            Id = id; Coins = coins;
            if(!CharacterCatalog.IsKnown(requesterId) || string.IsNullOrWhiteSpace(title) || dialogue==null)
                throw new ArgumentException("Invalid quest presentation.");
            RequesterId=requesterId;Title=title;Dialogue=dialogue;
            Requirements = list.GroupBy(i => (i.ItemId, i.Level)).Select(g => new ItemRequirement(g.Key.ItemId, g.Key.Level, checked(g.Sum(i => i.Count)))).ToArray();
        }
    }
}
