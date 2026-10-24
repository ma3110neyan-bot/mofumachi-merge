using System;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class SouthEntryBlendTests
    {
        private static Type Layout(){var type=typeof(TownWorldMap).Assembly.GetType("Mofumachi.Presentation.TownSouthEntryBlendLayout");Assert.That(type,Is.Not.Null,"Blend from the actual Central edge within South, instead of exposing the raw green seam");return type;}
        [Test] public void SouthEntryExactlyMatchesTheExistingCentralBottomTexels()
        {
            var type=Layout();var sample=type.GetMethod("TexturePoint");
            float[] xs={-.6f,-.3f,0,.3f,.6f},us={.1f,.3f,.5f,.7f,.9f};
            for(int i=0;i<xs.Length;i++)
            {var uv=(TownPoint)sample.Invoke(null,new object[]{new TownPoint(xs[i],-1)});Assert.That(uv.X,Is.EqualTo(us[i]).Within(.00001f));Assert.That(uv.Y,Is.Zero.Within(.00001f));}
        }
        [Test] public void EntryIsOpaqueAtTheCutAndFadesOnlyInsideSouth()
        {
            var type=Layout();float top=(float)type.GetField("Top").GetRawConstantValue(),bottom=(float)type.GetField("Bottom").GetRawConstantValue();
            Assert.That(top,Is.EqualTo(-1));Assert.That(bottom,Is.LessThan(-1.12f),"The blend continues into the fully painted South ground");
            float Alpha(float y)=>(float)type.GetMethod("Opacity").Invoke(null,new object[]{y});
            Assert.That(Alpha(top),Is.EqualTo(1));Assert.That(Alpha(bottom),Is.Zero);
            float last=0;for(int i=0;i<=20;i++){float a=Alpha(bottom+(top-bottom)*i/20);Assert.That(a,Is.GreaterThanOrEqualTo(last-.00001f).And.InRange(0,1));last=a;}
            var uv=(TownPoint)type.GetMethod("TexturePoint").Invoke(null,new object[]{new TownPoint(0,-1.1f)});
            Assert.That(uv.Y,Is.EqualTo(.1f/2.25f).Within(.00001f),"Reflect the edge texture into South without stretching one row");
        }
        [Test] public void BlendSidesWithdrawIntoTheGardenWithoutCreatingAnotherVerticalCut()
        {
            var method=Layout().GetMethod("VertexOpacity");Assert.That(method,Is.Not.Null);
            float Alpha(float x,float y)=>(float)method.Invoke(null,new object[]{new TownPoint(x,y)});
            foreach(float x in new[]{-.6f,.6f}){Assert.That(Alpha(x,-1),Is.EqualTo(1));Assert.That(Alpha(x,-1.1f),Is.Zero.Within(.00001f));}
            Assert.That(Alpha(0,-1.1f),Is.GreaterThan(.5f));
            Assert.That(Alpha(-.55f,-1.1f),Is.GreaterThan(.5f));
        }
    }
}
