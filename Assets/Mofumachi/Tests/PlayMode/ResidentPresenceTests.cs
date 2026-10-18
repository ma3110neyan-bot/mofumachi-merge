using System.Collections;
using System.Linq;
using System.Reflection;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Mofumachi.Tests
{
    public sealed class ResidentPresenceTests : SliceUiTestFixture
    {
        private static readonly MethodInfo Render=typeof(TownActor).GetMethod("Render",BindingFlags.Instance|BindingFlags.NonPublic);
        private static void RenderNow(TownActor actor,float seconds=0)=>Render.Invoke(actor,new object[]{0f,seconds});
        private static void AssertFinite(Mesh mesh)
        {
            Assert.That(mesh.vertexCount,Is.GreaterThan(0));
            foreach(var vertex in mesh.vertices)Assert.That(float.IsFinite(vertex.x)&&float.IsFinite(vertex.y)&&float.IsFinite(vertex.z),Is.True);
        }

        [UnityTest] public IEnumerator LayeredShadowsStayAtNavigationFeetWhileTheBodyReacts()
        {
            yield return EnterHome();var actor=FrameTownNpc();var life=Flow.GetComponentInChildren<TownLifeView>();
            var soft=actor.transform.Find("Town actor soft shadow").GetComponent<RoundedPanelGraphic>();
            var contact=actor.transform.Find("Town actor contact shadow").GetComponent<RoundedPanelGraphic>();
            foreach(var shadow in new[]{soft,contact})
            {
                Assert.That(shadow.raycastTarget,Is.False);Assert.That(shadow.rectTransform.anchoredPosition,Is.EqualTo(Vector2.zero));
                Assert.That(shadow.transform.GetSiblingIndex(),Is.LessThan(actor.VisualRoot.GetSiblingIndex()));
            }
            var feet=actor.WorldPosition;actor.React();life.AdvanceFrame(.1f);Canvas.ForceUpdateCanvases();
            Assert.That(actor.GetComponentInChildren<CharacterBodyGraphic>().Expression,Is.EqualTo(CharacterExpression.Smile));
            Assert.That(actor.VisualRoot.anchoredPosition.y,Is.GreaterThan(0));
            Assert.That(actor.WorldPosition,Is.EqualTo(feet));
            Assert.That(Vector2.Distance(((RectTransform)actor.transform).anchoredPosition,life.World.Local(feet)),Is.LessThan(.01f));
            Assert.That(contact.color.a,Is.LessThan(.19f));Assert.That(contact.rectTransform.anchoredPosition,Is.EqualTo(Vector2.zero));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator CameraPanCannotChangeAResidentsDepthOrNavigationPosition()
        {
            yield return EnterHome();var life=FrameTownPortrait();var hero=life.Actors.Single(a=>a.Controlled);
            RenderNow(hero);var scale=hero.VisualRoot.localScale;var feet=hero.WorldPosition;var local=((RectTransform)hero.transform).anchoredPosition;
            life.World.Camera.Pan(.02f,.02f);life.World.Refresh();RenderNow(hero);
            Assert.That(hero.RegionVisible,Is.True);Assert.That(hero.WorldPosition,Is.EqualTo(feet));
            Assert.That(hero.VisualRoot.localScale,Is.EqualTo(scale));
            Assert.That(Vector2.Distance(((RectTransform)hero.transform).anchoredPosition,local),Is.GreaterThan(.1f));
            Assert.That(Vector2.Distance(((RectTransform)hero.transform).anchoredPosition,life.World.Local(feet)),Is.LessThan(.01f));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator PauseAndDisableClearReactionGaitAndKeepPositiveDepthScale()
        {
            yield return EnterHome();var actor=FrameTownNpc();var life=Flow.GetComponentInChildren<TownLifeView>();
            actor.React();life.AdvanceFrame(.1f);Assert.That(actor.VisualRoot.anchoredPosition.y,Is.GreaterThan(0));
            Flow.OpenShopPreview();life.AdvanceFrame(.1f);Canvas.ForceUpdateCanvases();
            var body=actor.GetComponentInChildren<CharacterBodyGraphic>();var mesh=body.canvasRenderer.GetMesh();
            Assert.That(actor.VisualRoot.anchoredPosition,Is.EqualTo(Vector2.zero));Assert.That(actor.VisualRoot.localRotation,Is.EqualTo(Quaternion.identity));
            Assert.That(body.Expression,Is.EqualTo(CharacterExpression.Normal));
            var scale=actor.VisualRoot.localScale;var vertices=mesh.vertices;
            for(int frame=0;frame<12;frame++)life.AdvanceFrame(.1f);Canvas.ForceUpdateCanvases();
            Assert.That(body.canvasRenderer.GetMesh().vertices.SequenceEqual(vertices),Is.True);
            Assert.That(actor.VisualRoot.localScale,Is.EqualTo(scale));Assert.That(scale.x,Is.GreaterThan(0));Assert.That(scale.y,Is.GreaterThan(0));
            actor.gameObject.SetActive(false);Assert.That(actor.VisualRoot.anchoredPosition,Is.EqualTo(Vector2.zero));
            Flow.CloseShopPreview();actor.gameObject.SetActive(true);LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator AllApprovedExpressionsRenderFiniteUnmirroredGeometryWithInvalidPoseInputs()
        {
            yield return EnterHome();var life=FrameTownPortrait();life.SetViewScale(float.NaN);
            // Keep this fixture above the mask so offscreen residents also build
            // their actual UGUI mesh, rather than accepting a culled empty mesh.
            var fixture=UIWidgets.Node("Resident geometry fixture",(RectTransform)Flow.GetComponentInChildren<Canvas>().transform,new Rect(.5f,.5f,0,0));
            fixture.sizeDelta=new Vector2(100,120);var body=fixture.gameObject.AddComponent<CharacterBodyGraphic>();body.raycastTarget=false;
            try
            {
                foreach(var character in CharacterCatalog.All)
                foreach(var expression in new[]{CharacterExpression.Normal,CharacterExpression.Smile,CharacterExpression.Surprise})
                {
                    body.Bind(character.Id,expression);body.Pose(float.NaN,float.PositiveInfinity,float.NegativeInfinity,true);
                    Canvas.ForceUpdateCanvases();AssertFinite(body.canvasRenderer.GetMesh());
                    var uv=body.canvasRenderer.GetMesh().uv;
                    Assert.That(uv[0].x,Is.LessThan(uv[3].x));
                    Assert.That(body.CharacterId,Is.EqualTo(character.Id));Assert.That(body.Expression,Is.EqualTo(expression));
                    Assert.That(body.rectTransform.localScale.x,Is.GreaterThan(0));
                }
            }
            finally{Object.DestroyImmediate(fixture.gameObject);}
            foreach(var actor in life.Actors)Assert.That(float.IsFinite(actor.VisualRoot.localScale.x),Is.True);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
