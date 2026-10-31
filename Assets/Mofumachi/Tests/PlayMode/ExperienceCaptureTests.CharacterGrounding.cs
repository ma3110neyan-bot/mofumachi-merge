using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Tests
{
    public sealed partial class ExperienceCaptureTests
    {
        private NativePair CaptureFinalGrounding(string output,TownLifeView field,TownActor actor,string label)
        {
            var shadow=actor.transform.Find("Character authored ground shadow").GetComponent<Graphic>();
            Assert.That(shadow,Is.Not.Null);Assert.That(shadow.enabled,Is.True);Assert.That(shadow.raycastTarget,Is.False);
            var configure=shadow.GetType().GetMethod("ConfigureCharacterGrounding",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.That(configure,Is.Not.Null);
            void Configure(bool refined)=>configure.Invoke(shadow,new object[]{actor.CharacterId,refined});
            Rect Bounds()
            {
                var corners=new Vector3[4];shadow.rectTransform.GetWorldCorners(corners);
                var points=corners.Select(v=>captureCamera.WorldToScreenPoint(v)).ToArray();
                return Rect.MinMaxRect(points.Min(v=>v.x),points.Min(v=>v.y),points.Max(v=>v.x),points.Max(v=>v.y));
            }
            var graphic=actor.CharacterDirection==NoahAuthoredDirection.Front
                ?(Graphic)actor.VisualRoot.GetComponent<CharacterBodyGraphic>():actor.GetComponentInChildren<CharacterAuthoredGraphic>();
            var warm=Portrait();UnityEngine.Object.DestroyImmediate(warm);
            // Copy arrays before any canvas rebuild: GetMesh() is borrowed.
            var mesh=graphic.canvasRenderer.GetMesh();Assert.That(mesh,Is.Not.Null);
            var vertices=mesh.vertices;var uv=mesh.uv;var colors=mesh.colors32;
            var pair=CaptureNativePair(output,field,label,()=>Configure(false),()=>Configure(true),Bounds);
            pair.comparison_kind="accepted-2.5D-shadow-vs-final-shadow-same-instant";
            pair.mode=actor.CharacterDirection.ToString();
            Canvas.ForceUpdateCanvases();mesh=graphic.canvasRenderer.GetMesh();Assert.That(mesh,Is.Not.Null);
            Assert.That(mesh.vertices.SequenceEqual(vertices)&&mesh.uv.SequenceEqual(uv)&&mesh.colors32.SequenceEqual(colors),Is.True,"Shadow polish must not change the character mesh, UVs or colours.");
            Assert.That(shadow.canvasRenderer.GetMesh().vertexCount,Is.EqualTo(97),"No added per-frame shadow geometry.");
            return pair;
        }
    }
}
