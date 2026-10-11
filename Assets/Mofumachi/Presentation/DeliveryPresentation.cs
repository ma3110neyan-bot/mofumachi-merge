using System;
using System.Collections.Generic;
using System.Linq;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    // A snapshot of a successful, already-saved transaction. Views never grant rewards.
    public sealed class DeliveryPresentation
    {
        public int CoinsAwarded { get; }
        public int PreviousTownLevel { get; }
        public int CurrentTownLevel { get; }
        public string QuestId { get; }
        public string RequesterId { get; }
        public string Title { get; }
        public IReadOnlyList<ItemRequirement> DeliveredItems { get; }
        public DeliveryPresentation(int coinsAwarded,int previousTownLevel,int currentTownLevel)
            :this(QuestDefinition.First,coinsAwarded,previousTownLevel,currentTownLevel) { }
        public DeliveryPresentation(QuestDefinition quest,int coinsAwarded,int previousTownLevel,int currentTownLevel)
        {
            if(quest==null)throw new ArgumentNullException(nameof(quest));
            CoinsAwarded=coinsAwarded;PreviousTownLevel=previousTownLevel;CurrentTownLevel=currentTownLevel;
            QuestId=quest.Id;RequesterId=quest.RequesterId;Title=quest.Title;
            DeliveredItems=Array.AsReadOnly(quest.Requirements.Select(r=>new ItemRequirement(r.ItemId,r.Level,r.Count)).ToArray());
        }
    }
}
