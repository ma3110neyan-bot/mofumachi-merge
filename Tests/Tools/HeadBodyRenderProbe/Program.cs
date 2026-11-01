using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Mofumachi.Presentation;
using UnityEngine;
using UnityEngine.UI;
using Approved=Mofumachi.ApprovedNoah;

string root=Path.GetFullPath(args[0]),id=args[1];
using var contacts=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"Tests/Fixtures/character-neck-contacts-2026-11-01.json")));
string relative=id=="noah"?"noah-directional-parts-2026-10-30/directions-layout.json":id+"-authored-parts-2026-10-30/layout.json";
using var source=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"References/Characters",relative)));
var views=new List<object>();var staticParts=new Dictionary<string,JsonElement[]>();var closedParts=new Dictionary<string,JsonElement>();
foreach(string direction in new[]{"left","right","back"})
{
    var raw=source.RootElement.GetProperty("views").GetProperty(direction);
    var contact=contacts.RootElement.EnumerateArray().Single(r=>r.GetProperty("id").GetString()==id&&r.GetProperty("direction").GetString()==direction);
    object Part(JsonElement part,bool closed=false)
    {
        string name=part.GetProperty("id").GetString();var crop=part.GetProperty("crop");var pivot=part.GetProperty("pivot");
        var values=new Dictionary<string,object>{{"id",name},{"width",crop[2].GetSingle()},{"height",crop[3].GetSingle()},{"pivot",new{x=pivot[0].GetSingle(),y=pivot[1].GetSingle()}},{"scale",part.GetProperty("scale").GetSingle()},{"uv",new{x=0f,y=0f,width=1f,height=1f}}};
        values["rotation"]=part.TryGetProperty("rotation",out var rotation)?rotation.GetSingle():0;
        values["offset"]=part.TryGetProperty("offset",out var offset)?new{x=offset[0].GetSingle(),y=offset[1].GetSingle()}:new{x=0f,y=0f};
        if(name=="head"||name=="body"){var p=contact.GetProperty(closed?"closedHead":name);values["neckJoint"]=new{x=p.GetProperty("x").GetSingle(),y=p.GetProperty("y").GetSingle()};values["hasNeckJoint"]=true;}
        return values;
    }
    staticParts[direction]=raw.GetProperty("parts").EnumerateArray().ToArray();
    var view=new Dictionary<string,object>{{"direction",direction},{"parts",staticParts[direction].Select(p=>Part(p)).ToArray()}};
    if(direction!="back"&&id!="noah"){closedParts[direction]=raw.GetProperty("closedHead");view["closedHead"]=Part(closedParts[direction],true);}
    views.Add(view);
}
float formalWidth=id=="noah"?254:source.RootElement.GetProperty("formalWidth").GetSingle(),formalHeight=id=="noah"?323:source.RootElement.GetProperty("formalHeight").GetSingle();
string json=JsonSerializer.Serialize(new{version=1,characterId=id,input_sha256=new string('a',64),formalWidth,formalHeight,views});
string resource=id=="noah"?"Mofumachi/NoahAuthoredTrial":"Mofumachi/CharacterAuthored/"+id;
Resources.Add(resource+"/atlas",new Texture2D());Resources.Add(resource+"/layout",new TextAsset(json));
var actualNoah=id=="noah"?new NoahAuthoredGraphic():null;var actualFive=id!="noah"?new CharacterAuthoredGraphic():null;
if(actualNoah!=null)actualNoah.Configure();else actualFive.Configure(id);
Approved.NoahAuthoredGraphic approved=null;
if(id=="noah"){approved=new Approved.NoahAuthoredGraphic();approved.Configure();}
MaskableGraphic.Bounds=new Rect(0,0,formalWidth,formalHeight);
int failures=0,frames=0;float maximum=0;string first="";
// Walking down is the unchanged formal single sprite; split-mesh transitions
// start with left, right, back, stop/restart, then change direction every frame.
for(int frame=0;frame<720;frame++)
{
    var direction=frame<120?NoahAuthoredDirection.Left:frame<240?NoahAuthoredDirection.Right:frame<360?NoahAuthoredDirection.Back:(NoahAuthoredDirection)(1+frame%3);
    string name=direction==NoahAuthoredDirection.Left?"left":direction==NoahAuthoredDirection.Right?"right":"back";
    float t=frame/60f,phase=frame*.19f,walking=frame%120<5?0:frame%120<15?(frame%120-5)/10f:frame%120<110?1:(120-frame%120)/10f;
    var mode=frame%240<120?NoahAuthoredMode.Walk:frame%240<180?NoahAuthoredMode.Idle:frame%240<210?NoahAuthoredMode.Talk:NoahAuthoredMode.Greet;bool blink=frame%17==0;
    if(actualNoah!=null)actualNoah.SetPose(t,phase,walking,mode,direction);else actualFive.SetPose(t,phase,walking,mode,direction,blink);
    VertexHelper mesh=(actualNoah as MaskableGraphic??actualFive).Render();
    var expected=Approved.NoahAuthoredMotion.Sample(t,phase,walking,(Approved.NoahAuthoredMode)(int)mode,(Approved.NoahAuthoredDirection)(int)direction);
    if(approved!=null){approved.SetPose(t,phase,walking,(Approved.NoahAuthoredMode)(int)mode,(Approved.NoahAuthoredDirection)(int)direction);}
    VertexHelper reference=approved?.Render();
    for(int part=0;part<staticParts[name].Length;part++)
    {
        var p=staticParts[name][part];string partId=p.GetProperty("id").GetString();if(blink&&name!="back"&&partId=="head"&&actualFive!=null)p=closedParts[name];
        var transform=expected.Part(partId);
        // Preserve the already-existing coherent body-local back anchor. It
        // differs from the approved version's world-X clamp; no new pose change.
        if(partId=="head"&&name=="back")transform=expected.Body.Child(0,-238,expected.Head.Rotation-expected.Body.Rotation);
        float ox=p.TryGetProperty("offset",out var offset)?offset[0].GetSingle():0,oy=offset.ValueKind==JsonValueKind.Array?offset[1].GetSingle():0;
        float angle=p.TryGetProperty("rotation",out var rotation)?rotation.GetSingle():0;transform=transform.Child(ox,oy,angle);
        var crop=p.GetProperty("crop");var pivot=p.GetProperty("pivot");float w=crop[2].GetSingle(),h=crop[3].GetSingle(),scale=p.GetProperty("scale").GetSingle(),s=formalHeight/600f;
        for(int corner=0;corner<4;corner++)
        {
            float x=corner>=2?w:0,y=corner==0||corner==3?h:0;
            transform.Point((x-pivot[0].GetSingle()*w)*scale,(y-pivot[1].GetSingle()*h)*scale,out float px,out float py);
            float ex=formalWidth/2+px*s,ey=-py*s;var v=mesh.Positions[part*4+corner];float error=Math.Max(Math.Abs(v.x-ex),Math.Abs(v.y-ey));
            if(reference!=null&&(name!="back"||partId!="head")){var a=reference.Positions[part*4+corner];if(Math.Abs(a.x-ex)>.0001||Math.Abs(a.y-ey)>.0001)throw new Exception("Pinned baseline fixture/reference disagrees.");}
            maximum=Math.Max(maximum,error);
            if(error>.0001){failures++;if(first=="")first=$"{id} frame{frame} {name} {partId} shift={error}";}
            var uv=mesh.Uvs[part*4+corner];if(uv.x!=(corner>=2?1:0)||uv.y!=(corner==0||corner==3?0:1))throw new Exception("UV changed.");
        }
    }
    frames++;
}
Console.WriteLine($"{id}: frames={frames}, vertex mismatches={failures}, maximum_error={maximum}; {first}");
if(failures>0)Environment.Exit(1);
