using System.Collections;
using System.Linq;
using System.Reflection;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mofumachi.Tests
{
    public sealed class Noah25DTrialPresentationTests : SliceUiTestFixture
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private static readonly MethodInfo Render=typeof(TownActor).GetMethod("Render",Private);
        private static float Clock(TownActor actor,string name)=>(float)typeof(TownActor).GetField(name,Private).GetValue(actor);
        private static void RenderNow(TownActor actor,float seconds=0)=>Render.Invoke(actor,new object[]{0f,seconds});

        [UnityTest] public IEnumerator OnlyTownNoahOptsInAndTheCausalSwitchRestoresTheExactLegacyFrame()
        {
            yield return EnterHome();var life=FrameTownPortrait();life.enabled=false;
            var noah=life.Actors.Single(a=>a.CharacterId=="noah");life.World.Camera.Jump(noah.WorldPosition);life.World.Refresh();
            foreach(var actor in life.Actors){actor.SetNoahTrialEnabled(true);Assert.That(actor.NoahTrialEnabled,Is.EqualTo(actor==noah));}
            RenderNow(noah,.08f);Canvas.ForceUpdateCanvases();
            string state=JsonUtility.ToJson(Flow.Game.State);var root=((RectTransform)noah.transform).anchoredPosition;
            var points=life.World.Model.Residents.Select(r=>r.Position).ToArray();var center=life.World.Camera.Center;
            var clocks=new[]{"breath","stride","amplitude","reactionRemaining"}.Select(n=>Clock(noah,n)).ToArray();
            var body=noah.GetComponentInChildren<CharacterBodyGraphic>();
            noah.SetNoahTrialEnabled(false);Canvas.ForceUpdateCanvases();
            var baseline=body.canvasRenderer.GetMesh();var vertices=baseline.vertices;var uv=baseline.uv;
            Assert.That(vertices.Length,Is.EqualTo(224));
            noah.SetNoahTrialEnabled(true);Canvas.ForceUpdateCanvases();
            Assert.That(body.canvasRenderer.GetMesh().vertexCount,Is.GreaterThan(224));
            noah.SetNoahTrialEnabled(false);Canvas.ForceUpdateCanvases();var restored=body.canvasRenderer.GetMesh();
            Assert.That(restored.vertices.SequenceEqual(vertices),Is.True);Assert.That(restored.uv.SequenceEqual(uv),Is.True);
            Assert.That(((RectTransform)noah.transform).anchoredPosition,Is.EqualTo(root));
            Assert.That(life.World.Model.Residents.Select(r=>r.Position).ToArray(),Is.EqualTo(points));
            Assert.That(life.World.Camera.Center,Is.EqualTo(center));Assert.That(JsonUtility.ToJson(Flow.Game.State),Is.EqualTo(state));
            Assert.That(new[]{"breath","stride","amplitude","reactionRemaining"}.Select(n=>Clock(noah,n)).ToArray(),Is.EqualTo(clocks));
            noah.SetNoahTrialEnabled(true);LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator SharedPortraitDefaultsAndOtherFiveTownMeshesRetainTheLegacyAtlas()
        {
            yield return EnterHome();var life=FrameTownPortrait();life.enabled=false;
            var fixture=UIWidgets.Node("Noah trial portrait fixture",(RectTransform)Flow.GetComponentInChildren<Canvas>().transform,new Rect(.5f,.5f,0,0));
            fixture.sizeDelta=new Vector2(62,78);var body=fixture.gameObject.AddComponent<CharacterBodyGraphic>();body.raycastTarget=false;
            try
            {
                foreach(var character in CharacterCatalog.All)
                foreach(var expression in new[]{CharacterExpression.Normal,CharacterExpression.Smile,CharacterExpression.Surprise})
                {
                    body.Bind(character.Id,expression);body.Pose(.4f,.2f,-.3f,true);Canvas.ForceUpdateCanvases();
                    var mesh=body.canvasRenderer.GetMesh();Assert.That(mesh.vertexCount,Is.EqualTo(224));
                    Assert.That(mesh.uv[0].x,Is.LessThan(mesh.uv[3].x));
                    Assert.That(body.Expression,Is.EqualTo(expression));
                }
                foreach(var actor in life.Actors.Where(a=>a.CharacterId!="noah"))
                {
                    life.World.Camera.Jump(actor.WorldPosition);life.World.Refresh();RenderNow(actor,.03f);Canvas.ForceUpdateCanvases();
                    var graphic=actor.GetComponentInChildren<CharacterBodyGraphic>();var mesh=graphic.canvasRenderer.GetMesh();
                    var vertices=mesh.vertices;var uv=mesh.uv;var position=actor.VisualRoot.anchoredPosition;
                    var scale=actor.VisualRoot.localScale;var rotation=actor.VisualRoot.localRotation;
                    actor.SetNoahTrialEnabled(false);actor.SetNoahTrialEnabled(true);Canvas.ForceUpdateCanvases();
                    Assert.That(actor.NoahTrialEnabled,Is.False);mesh=graphic.canvasRenderer.GetMesh();
                    Assert.That(mesh.vertices.SequenceEqual(vertices),Is.True);Assert.That(mesh.uv.SequenceEqual(uv),Is.True);
                    Assert.That(actor.VisualRoot.anchoredPosition,Is.EqualTo(position));Assert.That(actor.VisualRoot.localScale,Is.EqualTo(scale));
                    Assert.That(actor.VisualRoot.localRotation,Is.EqualTo(rotation));
                }
            }
            finally{Object.DestroyImmediate(fixture.gameObject);}
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator NoahTalkKeepsNavigationFeetAndPauseClearsThePresentationPose()
        {
            yield return EnterHome();var life=FrameTownPortrait();life.enabled=false;
            var noah=life.Actors.Single(a=>a.CharacterId=="noah");life.World.Camera.Jump(noah.WorldPosition);life.World.Refresh();
            var feet=noah.WorldPosition;typeof(TownActor).GetMethod("Greet",Private).Invoke(noah,null);life.enabled=true;RenderNow(noah,.1f);
            Assert.That(noah.VisualRoot.anchoredPosition.y,Is.GreaterThan(.5f));
            Assert.That(noah.WorldPosition,Is.EqualTo(feet));
            var contact=noah.transform.Find("Town actor contact shadow").GetComponent<RoundedPanelGraphic>();
            Assert.That(contact.rectTransform.anchoredPosition,Is.EqualTo(Vector2.zero));Assert.That(contact.raycastTarget,Is.False);
            Flow.OpenTownMenu();life.AdvanceFrame(.1f);Canvas.ForceUpdateCanvases();
            Assert.That(noah.VisualRoot.anchoredPosition,Is.EqualTo(Vector2.zero));
            Assert.That(noah.VisualRoot.localRotation,Is.EqualTo(Quaternion.identity));
            Assert.That(noah.GetComponentInChildren<CharacterBodyGraphic>().Expression,Is.EqualTo(CharacterExpression.Normal));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator NoahTrialKeepsTheNormalBodyArtAcrossTalkAndOnlySwapsRegisteredBlinkEyes()
        {
            yield return EnterHome();
            var fixture=UIWidgets.Node("Noah original art fixture",(RectTransform)Flow.GetComponentInChildren<Canvas>().transform,new Rect(.5f,.5f,0,0));
            fixture.sizeDelta=new Vector2(62,78);var body=fixture.gameObject.AddComponent<CharacterBodyGraphic>();body.raycastTarget=false;
            var setTrial=typeof(CharacterBodyGraphic).GetMethod("SetNoahTownTrial",Private);
            var pose=typeof(CharacterBodyGraphic).GetMethod("PoseNoah",Private);
            try
            {
                body.Bind("noah");setTrial.Invoke(body,new object[]{true});pose.Invoke(body,new object[]{default(Noah25DTrialPose),false});Canvas.ForceUpdateCanvases();
                var mesh=body.canvasRenderer.GetMesh();var vertices=mesh.vertices;var normal=mesh.uv;
                Assert.That(normal.Length,Is.GreaterThan(224));
                foreach(var expression in new[]{CharacterExpression.Smile,CharacterExpression.Surprise})
                {
                    body.Bind("noah",expression);Canvas.ForceUpdateCanvases();mesh=body.canvasRenderer.GetMesh();
                    Assert.That(mesh.vertices.SequenceEqual(vertices),Is.True);Assert.That(mesh.uv.SequenceEqual(normal),Is.True);
                }
                pose.Invoke(body,new object[]{default(Noah25DTrialPose),true});Canvas.ForceUpdateCanvases();var closed=body.canvasRenderer.GetMesh().uv;
                int changed=0;
                for(int i=0;i<normal.Length;i++)
                {
                    if(closed[i]==normal[i])continue;changed++;
                    float sourceX=normal[i].x*1536,sourceY=(1-normal[i].y)*1024;
                    Assert.That(sourceY,Is.InRange(120.5f,165.9f));
                    bool left=sourceX>=1303.5f&&sourceX<=1357.1f;
                    Assert.That(left||sourceX>=1382.3f&&sourceX<=1428.2f,Is.True);
                    Assert.That((closed[i].x-normal[i].x)*1536,Is.EqualTo(left?0:-2).Within(.001));
                    Assert.That((closed[i].y-normal[i].y)*1024,Is.EqualTo(left?-336:-339).Within(.001));
                }
                Assert.That(changed,Is.GreaterThan(0));
                Assert.That(body.canvasRenderer.GetMesh().vertices.SequenceEqual(vertices),Is.True,"Blink must leave the original head geometry rigid.");
            }
            finally{Object.DestroyImmediate(fixture.gameObject);}
            LogAssert.NoUnexpectedReceived();
        }
    }
}
