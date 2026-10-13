using System;
using Mofumachi.Core;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class TownViewportTests
    {
        private static Type TypeOf(string name){var t=typeof(GameState).Assembly.GetType("Mofumachi.Core."+name);Assert.That(t,Is.Not.Null,"Bounded town camera model is missing");return t;}
        private static object New()=>Activator.CreateInstance(TypeOf("TownViewport"),new object[]{875f/1798});
        private static bool Call(object v,string name,params object[] args)=>(bool)v.GetType().GetMethod(name).Invoke(v,args);
        private static object Rect(object v)=>v.GetType().GetProperty("UvRect").GetValue(v);
        private static float P(object r,string name)=>(float)r.GetType().GetProperty(name).GetValue(r);
        private static object Frame(float x,float y,float w,float h)=>Activator.CreateInstance(TypeOf("TownViewportRect"),new object[]{x,y,w,h});
        private static void InImage(object r){Assert.That(P(r,"X"),Is.GreaterThanOrEqualTo(0));Assert.That(P(r,"Y"),Is.GreaterThanOrEqualTo(0));Assert.That(P(r,"X")+P(r,"Width"),Is.LessThanOrEqualTo(1.00001f));Assert.That(P(r,"Y")+P(r,"Height"),Is.LessThanOrEqualTo(1.00001f));}
        [Test] public void CoverPreservesSourceAspect()
        {foreach(float aspect in new[]{.45f,.61f,1.4f}){var v=New();Assert.That(Call(v,"Configure",aspect,1.10f),Is.True);var r=Rect(v);InImage(r);Assert.That(P(r,"Width")*(875f/1798)/P(r,"Height"),Is.EqualTo(aspect).Within(.0001f));}}
        [Test] public void PanNeverLeavesOriginalImage()
        {var v=New();Call(v,"Configure",.61f,1.1f);foreach(float d in new[]{-100f,100f}){Call(v,"TryPan",d,d);InImage(Rect(v));}}
        [Test] public void PanAndProjectionRoundTrip()
        {var v=New();Call(v,"Configure",.61f,1.1f);var before=Rect(v);Call(v,"TryPan",.01f,-.02f);var after=Rect(v);Assert.That(P(after,"X")-P(before,"X"),Is.EqualTo(.01f*P(before,"Width")).Within(.00001f));Assert.That(P(after,"Y")-P(before,"Y"),Is.EqualTo(-.02f*P(before,"Height")).Within(.00001f));float x=P(after,"X")+.3f*P(after,"Width");Assert.That((x-P(after,"X"))/P(after,"Width"),Is.EqualTo(.3f).Within(.00001f));}
        [Test] public void FrameKeepsHeroInVisibleWorld()
        {var v=New();Call(v,"Configure",.61f,1.1f);var point=new TownPoint(.30f,.28f);Assert.That(Call(v,"TryFrame",point,Frame(.1f,.3f,.8f,.45f)),Is.True);var r=Rect(v);Assert.That((point.Y-P(r,"Y"))/P(r,"Height"),Is.InRange(.2999f,.7501f));InImage(r);}
        [Test] public void ImpossibleFrameClampsWithoutTeleport()
        {var v=New();Call(v,"Configure",.61f,1.1f);Call(v,"TryFrame",new TownPoint(0,0),Frame(.1f,.3f,.8f,.4f));var r=Rect(v);Assert.That(P(r,"X"),Is.Zero);Assert.That(P(r,"Y"),Is.Zero);InImage(r);}
        [Test] public void ResizePreservesAndClampsCenter()
        {var v=New();Call(v,"Configure",.61f,1.1f);Call(v,"TryPan",.02f,.01f);var before=Rect(v);Call(v,"Configure",.65f,1.1f);var r=Rect(v);Assert.That(P(r,"X")+P(r,"Width")/2,Is.EqualTo(P(before,"X")+P(before,"Width")/2).Within(.00001f));InImage(r);}
        [Test] public void InvalidInputsLeaveViewUnchanged()
        {var v=New();Call(v,"Configure",.61f,1.1f);var before=Rect(v);foreach(float f in new[]{float.NaN,float.PositiveInfinity,0,-1})Assert.That(Call(v,"Configure",f,1.1f),Is.False);Assert.That(Call(v,"TryPan",float.NaN,0f),Is.False);Assert.That(Call(v,"TryFrame",new TownPoint(float.NaN,0),Frame(0,0,1,1)),Is.False);Assert.That(Call(v,"TryFrame",new TownPoint(.5f,.5f),Frame(0,0,0,1)),Is.False);Assert.That(Rect(v),Is.EqualTo(before));}
        [Test] public void ZoomCannotExceedApprovedLimit()
        {var v=New();Call(v,"Configure",.61f,100f);Assert.That(P(Rect(v),"Width"),Is.EqualTo(1/1.15f).Within(.00001f));Assert.That(Call(v,"Configure",.61f,0f),Is.False);}
    }
}
