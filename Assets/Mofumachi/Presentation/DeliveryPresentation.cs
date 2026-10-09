namespace Mofumachi.Presentation
{
    // A snapshot of a successful, already-saved transaction. Views never grant rewards.
    public sealed class DeliveryPresentation
    {
        public int CoinsAwarded { get; }
        public int PreviousTownLevel { get; }
        public int CurrentTownLevel { get; }
        public DeliveryPresentation(int coinsAwarded,int previousTownLevel,int currentTownLevel)
        { CoinsAwarded=coinsAwarded;PreviousTownLevel=previousTownLevel;CurrentTownLevel=currentTownLevel; }
    }
}
