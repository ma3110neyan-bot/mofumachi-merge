using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class CharacterNeckSyncTests
    {
        private static Type BindingType=>typeof(NoahAuthoredMotion).Assembly.GetType("Mofumachi.Presentation.CharacterNeckBinding");
        [TestCase("noah")][TestCase("momo")][TestCase("lulu")][TestCase("pom")][TestCase("fio")][TestCase("miel")]
        public void AnatomicalNeckStaysInTheBodyFrameThroughTurnsAndStops(string id)
        {
            Assert.That(BindingType,Is.Not.Null,"A shared body-neck binding is required, not a rectangular head-bottom anchor.");
            int views=0;
            using(var json=JsonDocument.Parse(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,"character-neck-contacts-2026-11-01.json"))))
            {
            foreach(var row in json.RootElement.EnumerateArray())if(row.GetProperty("id").GetString()==id)
            {
                views++;
                var direction=row.GetProperty("direction").GetString();var bodyJoint=row.GetProperty("body");var headJoint=row.GetProperty("head");
                float X(JsonElement e)=>e.GetProperty("x").GetSingle();float Y(JsonElement e)=>e.GetProperty("y").GetSingle();
                var binding=Activator.CreateInstance(BindingType);
                foreach(var field in new[]{("bodyX",X(bodyJoint)),("bodyY",Y(bodyJoint)),("headX",X(headJoint)),("headY",Y(headJoint))})BindingType.GetField(field.Item1).SetValue(binding,field.Item2);
                BindingType.GetField("version").SetValue(binding,1);
                var method=BindingType.GetMethod("Resolve");Assert.That(method,Is.Not.Null);
                var face=direction=="left"?NoahAuthoredDirection.Left:direction=="right"?NoahAuthoredDirection.Right:NoahAuthoredDirection.Back;
                // Abrupt phase/direction/start-stop changes expose cached or delayed head updates.
                for(int tick=0;tick<180;tick++)
                {
                    var mode=(NoahAuthoredMode)(tick%4);float walking=tick%30<3?0:tick%30<8?(tick%30-3)/5f:tick%30<25?1:(30-tick%30)/5f;
                    var pose=NoahAuthoredMotion.Sample(tick*.031f,tick*.27f,walking,mode,face);
                    var body=pose.Body;var actual=(NoahPartTransform)method.Invoke(binding,new object[]{body,pose.Head.Rotation,false});
                    actual.Point(X(headJoint),Y(headJoint),out float hx,out float hy);body.Point(X(bodyJoint),Y(bodyJoint),out float bx,out float by);
                    Assert.That(hx,Is.EqualTo(bx).Within(.0001),id+" "+face+" tick="+tick);
                    Assert.That(hy,Is.EqualTo(by).Within(.0001));Assert.That(actual.Rotation,Is.EqualTo(pose.Head.Rotation),"Keep the existing head animation angle.");
                }
            }
            }
            Assert.That(views,Is.EqualTo(3),"Each identity needs independent left/right/back source contacts.");
        }
        [Test] public void BlinkCropUsesItsOwnJointWithTheSameBodySocket()
        {
            Assert.That(BindingType,Is.Not.Null);var b=Activator.CreateInstance(BindingType);
            foreach(var p in new[]{("bodyX",12f),("bodyY",-225f),("headX",-7f),("headY",-4f),("closedHeadX",3f),("closedHeadY",8f)})BindingType.GetField(p.Item1).SetValue(b,p.Item2);
            BindingType.GetField("version").SetValue(b,1);BindingType.GetField("hasClosedHead").SetValue(b,true);
            var body=new NoahPartTransform(21,-33,.17f);var m=BindingType.GetMethod("Resolve");
            var open=(NoahPartTransform)m.Invoke(b,new object[]{body,.23f,false});var closed=(NoahPartTransform)m.Invoke(b,new object[]{body,.23f,true});
            open.Point(-7,-4,out float ox,out float oy);closed.Point(3,8,out float cx,out float cy);
            Assert.That(cx,Is.EqualTo(ox).Within(.0001));Assert.That(cy,Is.EqualTo(oy).Within(.0001));
        }
        [TestCase(false,-15f)][TestCase(true,-3f)] public void JointUsesTheCentralOpaqueContourInsteadOfGutters(bool chin,float expectedY)
        {
            Assert.That(BindingType,Is.Not.Null);var alpha=new byte[100];alpha[0]=alpha[99]=255;
            foreach(int row in new[]{2,3,7,8}){alpha[row*10+4]=alpha[row*10+5]=255;}
            var args=new object[]{alpha,10,10,.5f,1f,2f,chin,0f,0f};BindingType.GetMethod("FindJoint").Invoke(null,args);
            Assert.That(args[7],Is.EqualTo(-1f));Assert.That(args[8],Is.EqualTo(expectedY));
        }
        [Test] public void EmptyNeckSourceAndNonfiniteBindingAreRejected()
        {
            Assert.That(BindingType,Is.Not.Null);
            var e=Assert.Throws<TargetInvocationException>(()=>BindingType.GetMethod("FindJoint").Invoke(null,new object[]{new byte[100],10,10,.5f,1f,1f,true,0f,0f}));
            Assert.That(e.InnerException,Is.TypeOf<InvalidOperationException>());
            var b=Activator.CreateInstance(BindingType);BindingType.GetField("version").SetValue(b,1);BindingType.GetField("bodyX").SetValue(b,float.NaN);
            e=Assert.Throws<TargetInvocationException>(()=>BindingType.GetMethod("Validate").Invoke(b,null));Assert.That(e.InnerException,Is.TypeOf<InvalidOperationException>());
        }
        [TestCase("noah")][TestCase("momo")][TestCase("lulu")][TestCase("pom")][TestCase("fio")][TestCase("miel")]
        public void ActualSourceNeckJointIsOnAnOpaquePixel(string id)
        {
            using(var json=JsonDocument.Parse(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,"character-neck-contacts-2026-11-01.json"))))
            foreach(var row in json.RootElement.EnumerateArray())if(row.GetProperty("id").GetString()==id)
            foreach(string kind in new[]{"head","body","closedHead"})if(row.TryGetProperty(kind,out var part))
            {
                byte[] alpha;using(var stream=File.OpenRead(Path.Combine(TestContext.CurrentContext.TestDirectory,part.GetProperty("alpha_file").GetString())))
                using(var gzip=new GZipStream(stream,CompressionMode.Decompress))using(var data=new MemoryStream()){gzip.CopyTo(data);alpha=data.ToArray();}
                int width=part.GetProperty("width").GetInt32(),height=part.GetProperty("height").GetInt32();float px=part.GetProperty("pivot")[0].GetSingle(),py=part.GetProperty("pivot")[1].GetSingle(),scale=part.GetProperty("scale").GetSingle();
                var args=new object[]{alpha,width,height,px,py,scale,kind!="body",0f,0f};BindingType.GetMethod("FindJoint").Invoke(null,args);
                int x=(int)Math.Floor((float)args[7]/scale+px*width),y=(int)Math.Floor((float)args[8]/scale+py*height);
                Assert.That(x,Is.InRange((int)(width*.4f),(int)(width*.6f)-1));Assert.That(y,Is.InRange(0,height-1));
                Assert.That(alpha[y*width+x],Is.GreaterThanOrEqualTo(128),id+" "+row.GetProperty("direction").GetString()+" "+kind+": cannot bind invisible padding.");
            }
        }
    }
}
