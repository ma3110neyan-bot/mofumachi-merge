using System;
using System.Reflection;
using Mofumachi.Core;
using NUnit.Framework;

namespace Mofumachi.Tests
{
    public sealed class TownRegionJourneyTests
    {
        private static Type ModelType
        {
            get {var type=typeof(GameState).Assembly.GetType("Mofumachi.Core.TownRegionJourney");Assert.That(type,Is.Not.Null,"Regional journey model is missing");return type;}
        }
        private static object Journey(TownRegionLocation from,TownRegionLocation to)=>Activator.CreateInstance(ModelType,from,to,6);
        private static T Read<T>(object model,string property)=>(T)ModelType.GetProperty(property).GetValue(model);
        private static string Phase(object model)=>ModelType.GetProperty("Phase").GetValue(model).ToString();
        private static void Tick(object model,TownPoint point,float seconds)=>ModelType.GetMethod("Tick").Invoke(model,new object[]{point,seconds});
        private static void Cancel(object model)=>ModelType.GetMethod("Cancel").Invoke(model,null);
        private static bool Take(object model,out TownRegionLocation entry)
        {var args=new object[]{null};bool transferred=(bool)ModelType.GetMethod("TryTakeTransfer").Invoke(model,args);entry=(TownRegionLocation)args[0];return transferred;}
        private static readonly TownPoint CentralWestGate=new TownPoint(.15f,.52f);
        private static readonly TownRegionLocation WestBench=new TownRegionLocation(TownRegionId.West,new TownPoint(.39f,.36f));
        private static object AtWestGate()
        {
            var model=Journey(new TownRegionLocation(TownRegionId.Central,CentralWestGate),WestBench);
            Tick(model,CentralWestGate,1f/30);Assert.That(Phase(model),Is.EqualTo("Fading"));return model;
        }
        [Test] public void EntryMustBeReachedBeforeFade()
        {
            var start=new TownPoint(.42f,.47f);var model=Journey(new TownRegionLocation(TownRegionId.Central,start),WestBench);
            Tick(model,start,.1f);Assert.That(Phase(model),Is.EqualTo("WalkingOut"));Assert.That(Read<float>(model,"FadeOpacity"),Is.Zero);
            Assert.That(Take(model,out _),Is.False);Tick(model,CentralWestGate,1f/30);Assert.That(Phase(model),Is.EqualTo("Fading"));
            Assert.That(Read<float>(model,"FadeOpacity"),Is.Zero);Assert.That(Take(model,out _),Is.False);
        }
        [Test] public void FadeTransfersExactlyOnceAtMidpoint()
        {
            Assert.That((float)ModelType.GetField("FadeSeconds").GetRawConstantValue(),Is.EqualTo(.30f));
            var model=AtWestGate();Tick(model,CentralWestGate,.1f);Assert.That(Take(model,out _),Is.False);
            Tick(model,CentralWestGate,.05f);Assert.That(Read<float>(model,"FadeOpacity"),Is.EqualTo(1));
            Assert.That(Take(model,out var entry),Is.True);Assert.That(entry.Region,Is.EqualTo(TownRegionId.West));
            Assert.That(entry.Local.X,Is.EqualTo(.94f));Assert.That(entry.Local.Y,Is.EqualTo(.55f));
            Assert.That(Take(model,out _),Is.False);Assert.That(Read<float>(model,"FadeOpacity"),Is.EqualTo(1));
            Tick(model,entry.Local,.1f);Tick(model,entry.Local,.05f);
            Assert.That(Phase(model),Is.EqualTo("WalkingIn"));Assert.That(Read<float>(model,"FadeOpacity"),Is.Zero);
            Assert.That(Read<TownPoint>(model,"Target").Equals(WestBench.Local),Is.True);
        }
        [Test] public void WestToEastVisitsCentralBeforeEast()
        {
            var destination=new TownRegionLocation(TownRegionId.East,new TownPoint(.48f,.43f));var model=Journey(WestBench,destination);
            Assert.That(Read<TownRegionId>(model,"Region"),Is.EqualTo(TownRegionId.West));
            var gate=Read<TownPoint>(model,"Target");Assert.That(gate.X,Is.EqualTo(.94f));Tick(model,gate,1f/30);
            Tick(model,gate,.1f);Tick(model,gate,.05f);Assert.That(Take(model,out var central),Is.True);
            Assert.That(central.Region,Is.EqualTo(TownRegionId.Central));Assert.That(central.Local.Equals(CentralWestGate),Is.True);
            Tick(model,central.Local,.1f);Tick(model,central.Local,.05f);Assert.That(Phase(model),Is.EqualTo("WalkingOut"));
            gate=Read<TownPoint>(model,"Target");Assert.That(gate.X,Is.EqualTo(.68f));Assert.That(gate.Y,Is.EqualTo(.515f));
            Tick(model,gate,1f/30);Tick(model,gate,.1f);Tick(model,gate,.05f);Assert.That(Take(model,out var east),Is.True);
            Assert.That(east.Region,Is.EqualTo(TownRegionId.East));Assert.That(east.Local.X,Is.EqualTo(.09f));
            Tick(model,east.Local,.1f);Tick(model,east.Local,.05f);Tick(model,destination.Local,1f/30);
            Assert.That(Phase(model),Is.EqualTo("Complete"));Assert.That(Take(model,out _),Is.False);
        }
        [Test] public void CancelledBeforeAndAfterMidpointSettlesOneRegion()
        {
            foreach(bool after in new[]{false,true})
            {
                var model=AtWestGate();Tick(model,CentralWestGate,.1f);TownRegionLocation entry=default;
                if(after){Tick(model,CentralWestGate,.05f);Assert.That(Take(model,out entry),Is.True);}
                Cancel(model);Assert.That(Phase(model),Is.EqualTo("Cancelled"));Assert.That(Read<float>(model,"FadeOpacity"),Is.Zero);
                Assert.That(Read<TownRegionId>(model,"Region"),Is.EqualTo(after?TownRegionId.West:TownRegionId.Central));
                var settled=Read<TownRegionLocation>(model,"SettledLocation");Assert.That(settled.Local.Equals(after?entry.Local:CentralWestGate),Is.True);
                Tick(model,CentralWestGate,.1f);Cancel(model);Assert.That(Take(model,out _),Is.False);
                Assert.That(Read<TownRegionId>(model,"Region"),Is.EqualTo(settled.Region));
            }
        }
        [Test] public void InvalidDeltaDoesNotAdvanceOrUnlock()
        {
            var model=AtWestGate();foreach(float delta in new[]{0f,-1f,float.NaN,float.PositiveInfinity})
            {Tick(model,CentralWestGate,delta);Assert.That(Read<float>(model,"FadeOpacity"),Is.Zero);Assert.That(Take(model,out _),Is.False);}
            Tick(model,new TownPoint(float.NaN,.52f),.1f);Assert.That(Read<float>(model,"FadeOpacity"),Is.Zero);
            Tick(model,CentralWestGate,100);Assert.That(Take(model,out _),Is.False,"Large elapsed input is bounded, not a jump to the destination");
            Assert.That(Read<float>(model,"FadeOpacity"),Is.InRange(.6f,.7f));
        }
        [Test] public void ARepeatedRequestCannotReplayArrival()
        {
            var location=new TownRegionLocation(TownRegionId.Central,new TownPoint(.42f,.47f));var model=Journey(location,location);
            Tick(model,location.Local,1f/30);Assert.That(Phase(model),Is.EqualTo("Complete"));
            var state=GameState.CreateInitial();var before=StateCodec.Encode(state);
            for(int i=0;i<60;i++){Tick(model,location.Local,.1f);Assert.That(Take(model,out _),Is.False);}
            Assert.That(StateCodec.Encode(state),Is.EqualTo(before));Assert.That(Phase(model),Is.EqualTo("Complete"));
        }
    }
}
