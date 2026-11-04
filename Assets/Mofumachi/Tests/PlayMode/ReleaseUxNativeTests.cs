using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Mofumachi.Tests
{
    public sealed class ReleaseUxNativeTests:SliceUiTestFixture
    {
        private void CheckText()
        {
            Canvas.ForceUpdateCanvases();Canvas.ForceUpdateCanvases();Canvas.ForceUpdateCanvases();
            var failures=new List<string>();
            foreach(var text in Flow.GetComponentsInChildren<Text>().Where(t=>!string.IsNullOrEmpty(t.text)&&ReleaseFontPolicy.For(t.name,t.fontSize,t.text)!=ReleaseFontRole.Unchanged))
            {
                try{
                HachiFontCapture.AssertTypeface(text);
                Assert.That(text.resizeTextForBestFit,Is.False,text.name+": never shrink whole UI");
                foreach(char glyph in text.text.Where(c=>!char.IsWhiteSpace(c)).Distinct())Assert.That(text.font.HasCharacter(glyph),Is.True,text.name+": missing "+glyph);
                Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+1),text.name+": text must fit its existing bounds");
                }catch(AssertionException error){failures.Add(text.name+" [font="+text.font.name+", size="+text.fontSize+", rect="+text.rectTransform.rect+"]: "+error.Message);}
            }
            Assert.That(failures,Is.Empty,string.Join("\n",failures));
        }
        private void CheckStep(string context,Action check,List<string> failures)
        {
            try{check();}catch(AssertionException error){failures.Add(context+": "+error.Message);}
        }
        private void CheckScreen(string context,Action show,List<string> failures)=>CheckStep(context,()=>{show();CheckText();},failures);
        [UnityTest] public IEnumerator ScopedFontsFitTitleTownMergeBookSettingsAndTodayAtPhoneSizes()
        {
            yield return LoadTitle();CheckText();
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Start label").font,Is.EqualTo(Resources.Load<Font>("Mofumachi/UIFontAccent")));
            yield return EnterHome();
            var failures=new List<string>();
            CheckStep("numeric fallback probe",HachiFontCapture.VerifyMeasuredFallback,failures);
            CheckStep("compact Merge header probe",HachiFontCapture.VerifyCompactHeaders,failures);
            CheckStep("settings description probe",HachiFontCapture.VerifyDescriptionFit,failures);
            CheckStep("narrow book title probe",HachiFontCapture.VerifyNarrowBookTitle,failures);
            var canvas=Flow.GetComponentInChildren<Canvas>();canvas.GetComponent<CanvasScaler>().enabled=false;canvas.renderMode=RenderMode.WorldSpace;
            foreach(var size in new[]{new Vector2(320,536),new Vector2(320,592),new Vector2(360,640),new Vector2(360,740)})
            {
                ((RectTransform)canvas.transform).sizeDelta=size;
                Flow.GetComponentInChildren<ResponsiveUILayout>().ApplyViewport(size,new Rect(0,24,size.x,size.y-48));
                string context=size.x+"x"+size.y;
                CheckScreen(context+" town",()=>Flow.ShowHome(),failures);CheckScreen(context+" merge",()=>Flow.ShowMerge(),failures);CheckScreen(context+" quest",()=>Flow.ShowQuest(),failures);CheckScreen(context+" settings",()=>Flow.ShowSettings(),failures);
                CheckScreen(context+" book",()=>{Flow.ShowHome();Flow.OpenCharacterBook("momo");},failures);CheckScreen(context+" today",()=>{Flow.ShowHome();Flow.ShowToday();},failures);Flow.CloseToday();
            }
            Assert.That(failures,Is.Empty,string.Join("\n",failures));
            string output=HachiFontCapture.Argument("-mofumachiFontOnly");
            if(output!=null)yield return HachiFontCapture.Run(Flow,output,()=>Store.Writes);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ReceiptShowsActualDateCountAndClaimResultWithoutMainSaveWrites()
        {
            yield return EnterHome();FrameTownPortrait();
            for(int i=0;i<3;i++)Flow.Daily.RecordMerge();Flow.Daily.RecordTalk("momo");Flow.Daily.RecordWalk();
            int writes=Store.Writes;Flow.ShowToday();
            Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Claim today flower").onClick.Invoke();CheckText();
            var receipt=Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Today next discovery");
            Assert.That(receipt.text,Does.Contain("受け取りました"));Assert.That(receipt.text,Does.Contain(Flow.Daily.Data.Day.Replace('-','/')));
            Assert.That(receipt.text,Does.Contain("翌日も残ります"));Assert.That(Store.Writes,Is.EqualTo(writes));
            Assert.That(Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Claim today flower").interactable,Is.False);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
