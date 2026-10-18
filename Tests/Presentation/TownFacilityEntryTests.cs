using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class TownFacilityEntryTests
    {
        [Test] public void DoorCanOnlyOpenAtItsThresholdAfterWalkingStops()
        {
            var entry=new TownFacilityEntry();Assert.That(entry.Request("quest"),Is.True);
            entry.Step(TownWorldMap.Place("square"),false,10);Assert.That(entry.Phase,Is.EqualTo(TownEntryPhase.Walking));
            entry.Step(TownWorldMap.Place("quest"),true,1);Assert.That(entry.Phase,Is.EqualTo(TownEntryPhase.Walking));
            entry.Step(TownWorldMap.Place("quest"),false,.1f);Assert.That(entry.Phase,Is.EqualTo(TownEntryPhase.Opening));
            for(int i=0;i<10;i++)entry.Step(TownWorldMap.Place("quest"),false,.1f);
            Assert.That(entry.Phase,Is.EqualTo(TownEntryPhase.Entered));
        }
        [Test] public void PauseRetargetAndWalkingAwayCannotCompleteAStaleEntry()
        {
            foreach(string id in new[]{"quest","shop"})
            {
                var entry=new TownFacilityEntry();entry.Request(id);entry.Step(TownWorldMap.Place(id),false,.1f);entry.Cancel();
                entry.Step(TownWorldMap.Place(id),false,10);Assert.That(entry.Phase,Is.EqualTo(TownEntryPhase.Idle));
                entry.Request(id);entry.Step(TownWorldMap.Place(id),false,.1f);entry.Step(TownWorldMap.Place("square"),false,1);
                Assert.That(entry.Phase,Is.EqualTo(TownEntryPhase.Idle));
            }
        }
        [Test] public void InvalidTargetsAndTimeDoNotStartOrAdvanceEntry()
        {var entry=new TownFacilityEntry();Assert.That(entry.Request("unknown"),Is.False);entry.Request("shop");entry.Step(TownWorldMap.Place("shop"),false,float.NaN);Assert.That(entry.Phase,Is.EqualTo(TownEntryPhase.Walking));}
    }
}
