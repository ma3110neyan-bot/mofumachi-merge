using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class TownFacilityEntryTests
    {
        [Test] public void AmbientDialogueWaitsForTravelAndDeliberateEntryButResumesAfterCancellation()
        {
            var entry=new TownFacilityEntry();
            Assert.That(entry.CanStartAmbientConversation(false),Is.True);Assert.That(entry.CanStartAmbientConversation(true),Is.False);
            entry.Request("cafe");Assert.That(entry.CanStartAmbientConversation(true),Is.False);
            var feet=TownWorldMap.Place("west.cafe-front");entry.Step(feet,false,.1f);
            Assert.That(entry.CanStartAmbientConversation(false),Is.False,"Waiting at the door must remain readable even after walking stops");
            entry.TapDoor("cafe",feet,false);Assert.That(entry.CanStartAmbientConversation(false),Is.False);
            for(int i=0;i<10;i++)entry.Step(feet,false,.1f);
            Assert.That(entry.CanStartAmbientConversation(false),Is.False);
            entry.Cancel();Assert.That(entry.CanStartAmbientConversation(false),Is.True);
        }
        [TestCase("cafe")] [TestCase("shop")]
        public void CommercialSignAppearsForProximityOrItsOwnJourneyAndWithdrawsWhenRetargeted(string id)
        {
            var entry=new TownFacilityEntry();
            Assert.That(entry.ShowsSign(id,false),Is.False);Assert.That(entry.ShowsSign(id,true),Is.True);
            Assert.That(entry.ShowsSign("quest",false),Is.True);
            entry.Request(id);Assert.That(entry.ShowsSign(id,false),Is.True);
            entry.Step(TownWorldMap.Place(TownFacilityEntry.DestinationFor(id)),false,.1f);Assert.That(entry.ShowsSign(id,true),Is.True);
            entry.Request("quest");Assert.That(entry.ShowsSign(id,false),Is.False);
            entry.Cancel();Assert.That(entry.ShowsSign(id,false),Is.False);
        }
        [TestCase("quest")] [TestCase("shop")] [TestCase("cafe")]
        public void WalkingToADoorWaitsForADeliberateTapBeforeOpening(string id)
        {
            var entry=new TownFacilityEntry();Assert.That(entry.OwnsArrival(id),Is.False,"A manual walk still needs explicit entry");
            Assert.That(entry.Request(id),Is.True);Assert.That(entry.OwnsArrival(id),Is.True);
            Assert.That(entry.OwnsArrival(id=="quest"?"shop":"quest"),Is.False);
            entry.Step(TownWorldMap.Place(TownFacilityEntry.DestinationFor(id)),false,.1f);Assert.That(entry.Phase,Is.EqualTo(TownEntryPhase.Arrived));Assert.That(entry.OwnsArrival(id),Is.True);
            for(int i=0;i<100;i++)entry.Step(TownWorldMap.Place(TownFacilityEntry.DestinationFor(id)),false,.1f);
            Assert.That(entry.Phase,Is.EqualTo(TownEntryPhase.Arrived));Assert.That(entry.OpenAmount,Is.Zero);
            Assert.That(entry.TapDoor(id,TownWorldMap.Place(TownFacilityEntry.DestinationFor(id)),false),Is.True);
            for(int i=0;i<10;i++)entry.Step(TownWorldMap.Place(TownFacilityEntry.DestinationFor(id)),false,.1f);
            Assert.That(entry.Phase,Is.EqualTo(TownEntryPhase.Entered));Assert.That(entry.OwnsArrival(id),Is.True);
            entry.Cancel();Assert.That(entry.OwnsArrival(id),Is.False);Assert.That(entry.OwnsArrival(null),Is.False);
        }
        [Test] public void DoorCanOnlyOpenAtItsThresholdAfterWalkingStops()
        {
            var entry=new TownFacilityEntry();Assert.That(entry.Request("quest"),Is.True);
            entry.Step(TownWorldMap.Place("square"),false,10);Assert.That(entry.Phase,Is.EqualTo(TownEntryPhase.Walking));
            entry.Step(TownWorldMap.Place("quest"),true,1);Assert.That(entry.Phase,Is.EqualTo(TownEntryPhase.Walking));
            Assert.That(entry.TapDoor("quest",TownWorldMap.Place("square"),false),Is.False);
            Assert.That(entry.TapDoor("quest",TownWorldMap.Place("quest"),true),Is.False);
            entry.Step(TownWorldMap.Place("quest"),false,.1f);Assert.That(entry.Phase,Is.EqualTo(TownEntryPhase.Arrived));
            Assert.That(entry.TapDoor("quest",TownWorldMap.Place("quest"),false),Is.True);
            for(int i=0;i<10;i++)entry.Step(TownWorldMap.Place("quest"),false,.1f);
            Assert.That(entry.Phase,Is.EqualTo(TownEntryPhase.Entered));
        }
        [Test] public void PauseRetargetAndWalkingAwayCannotCompleteAStaleEntry()
        {
            foreach(string id in new[]{"quest","shop","cafe"})
            {
                var entry=new TownFacilityEntry();entry.Request(id);entry.Step(TownWorldMap.Place(TownFacilityEntry.DestinationFor(id)),false,.1f);entry.Cancel();
                entry.Step(TownWorldMap.Place(TownFacilityEntry.DestinationFor(id)),false,10);Assert.That(entry.Phase,Is.EqualTo(TownEntryPhase.Idle));
                entry.Request(id);entry.Step(TownWorldMap.Place(TownFacilityEntry.DestinationFor(id)),false,.1f);entry.Step(TownWorldMap.Place("square"),false,1);
                Assert.That(entry.Phase,Is.EqualTo(TownEntryPhase.Idle));
            }
        }
        [Test] public void InvalidTargetsAndTimeDoNotStartOrAdvanceEntry()
        {var entry=new TownFacilityEntry();Assert.That(entry.Request("unknown"),Is.False);Assert.That(entry.TapDoor("shop",new TownPoint(float.NaN,0),false),Is.False);entry.Request("shop");entry.Step(TownWorldMap.Place("shop"),false,float.NaN);Assert.That(entry.Phase,Is.EqualTo(TownEntryPhase.Walking));}
        [Test] public void AManualWalkCanOpenADoorWithOneTapWithoutStartingANewWalk()
        {
            var entry=new TownFacilityEntry();Assert.That(entry.TapDoor("shop",TownWorldMap.Place("shop"),false),Is.True);
            Assert.That(entry.Phase,Is.EqualTo(TownEntryPhase.Opening));Assert.That(entry.Facility,Is.EqualTo("shop"));
            Assert.That(entry.TapDoor("shop",TownWorldMap.Place("shop"),false),Is.False,"Repeated taps cannot restart the opening");
        }
    }
}
