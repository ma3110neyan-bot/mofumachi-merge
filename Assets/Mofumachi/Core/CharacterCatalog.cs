using System;
using System.Collections.Generic;
using System.Linq;

namespace Mofumachi.Core
{
    public sealed class CharacterDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public int PortraitIndex { get; }
        internal CharacterDefinition(string id, string name, string description, int portraitIndex)
        { Id = id; Name = name; Description = description; PortraitIndex = portraitIndex; }
    }

    public static class CharacterCatalog
    {
        public static IReadOnlyList<CharacterDefinition> All { get; } = Array.AsReadOnly(new[] {
            new CharacterDefinition("momo", "モモ", "やさしくて、がんばり屋。みんなと仲良くなれる子。", 0),
            new CharacterDefinition("lulu", "ルル", "好奇心いっぱい。新しい発見が大好き。", 1),
            new CharacterDefinition("pom", "ポム", "のんびりマイペース。料理が得意な子。", 2),
            new CharacterDefinition("fio", "フィオ", "明るくて元気。ものづくりが大好き。", 3),
            new CharacterDefinition("miel", "ミエル", "おっとりやさしい、お花好きの子。", 4),
            new CharacterDefinition("noah", "ノア", "しっかり者。お気に入りを集めるのが得意。", 5)
        });
        public static CharacterDefinition Find(string id) => All.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.Ordinal));
        public static bool IsKnown(string id) => Find(id) != null;
    }
}
