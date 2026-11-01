using System;
using System.Linq;
using System.Reflection;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class CharacterPassingTests
    {
        [TestCase("noah")][TestCase("momo")][TestCase("lulu")][TestCase("pom")][TestCase("fio")][TestCase("miel")]
        public void HeadOnPassingDoesNotSnapHeadingOrReverseAndStillArrives(string id)
        {
            var m=new TownWorldWalkModel(1,id);string peer=id=="momo"?"noah":"momo";
            m.Restore(id,new TownPoint(-.3f,-.3f));m.Restore(peer,new TownPoint(.3f,-.3f));
            foreach(var r in m.Residents.Where(r=>r.Id!=id&&r.Id!=peer))m.Restore(r.Id,new TownPoint(-1.8f,-.4f));
            Assert.That(m.TryGuidePoint(new TownPoint(.3f,-.3f)),Is.True);
            var other=m.Residents.Single(r=>r.Id==peer);
            Assert.That((bool)typeof(TownWorldWalkModel).GetMethod("Guide",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(m,new object[]{other,new TownPoint(-.3f,-.3f),null,true,false}),Is.True);
            float px=0,py=0,maxAngle=0;
            for(int i=0;i<900&&m.Hero.IsWalking;i++)
            {
                foreach(var r in m.Residents.Where(r=>r.Id!=id&&r.Id!=peer))m.Pause(r.Id,4);
                m.Step(1f/30);var h=m.Hero;
                Assert.That(m.Area.Contains(h.Position),Is.True);
                Assert.That(TownWalkableArea.Distance(h.Position,other.Position),Is.GreaterThanOrEqualTo(.0669f));
                float x=h.Position.X-h.Previous.X,y=(h.Position.Y-h.Previous.Y)*TownWorldLayout.ImageAspect;
                float n=(float)Math.Sqrt(x*x+y*y),prev=(float)Math.Sqrt(px*px+py*py);
                Assert.That(n,Is.LessThanOrEqualTo(.12f/30+.00001f));
                if(n>.000001f&&prev>.000001f){float dot=(x*px+y*py)/(n*prev);Assert.That(dot,Is.GreaterThanOrEqualTo(0));maxAngle=Math.Max(maxAngle,(float)Math.Acos(Math.Clamp(dot,-1,1))*180/(float)Math.PI);}
                if(n>.000001f){px=x;py=y;}
            }
            Assert.That(m.Hero.IsWalking,Is.False);Assert.That(maxAngle,Is.LessThan(25f),"Avoidance must blend rather than jump ~56 degrees in one tick.");
        }
    }
}
