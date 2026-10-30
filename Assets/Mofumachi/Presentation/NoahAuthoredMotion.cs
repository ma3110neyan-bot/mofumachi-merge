using System;
namespace Mofumachi.Presentation
{
    public enum NoahAuthoredDirection { Front,Left,Right,Back }
    public enum NoahAuthoredMode { Idle,Walk,Talk,Greet }
    public readonly struct NoahPartTransform
    {
        public readonly float X,Y,Rotation;
        public NoahPartTransform(float x,float y,float rotation=0){X=x;Y=y;Rotation=rotation;}
        public void Point(float x,float y,out float px,out float py)
        {float c=(float)Math.Cos(Rotation),s=(float)Math.Sin(Rotation);px=c*x-s*y+X;py=s*x+c*y+Y;}
        public NoahPartTransform Child(float x,float y,float rotation=0)
        {Point(x,y,out float px,out float py);return new NoahPartTransform(px,py,Rotation+rotation);}
        public NoahPartTransform OppositePlacement()=>new NoahPartTransform(-X,Y,-Rotation);
    }
    // Positions use the material sheet's downward Y; Graphic converts once to Unity upward Y.
    public struct NoahAuthoredPose
    {
        public NoahPartTransform Body,Head,FarArm,NearArm,FarLeg,NearLeg,Tail,Pouch;
        public NoahPartTransform Part(string id)
        {
            switch(id){case "body":return Body;case "head":return Head;case "farArm":return FarArm;
                case "nearArm":return NearArm;case "farLeg":return FarLeg;case "nearLeg":return NearLeg;
                case "tail":return Tail;case "pouch":return Pouch;default:throw new ArgumentException("Unknown Noah part: "+id);}
        }
    }
    public static class NoahAuthoredMotion
    {
        private static float Finite(float v)=>float.IsFinite(v)?v:0;
        // World coordinates have north/up positive Y, unlike the browser canvas.
        public static NoahAuthoredDirection Direction(float dx,float dy,NoahAuthoredDirection previous)
        {
            dx=Finite(dx);dy=Finite(dy);
            if(Math.Abs(dx)+Math.Abs(dy)<.000001f)return previous;
            if(Math.Abs(dy)>Math.Abs(dx)*1.15f)return dy>0?NoahAuthoredDirection.Back:NoahAuthoredDirection.Front;
            return dx<0?NoahAuthoredDirection.Left:NoahAuthoredDirection.Right;
        }
        public static NoahAuthoredPose Sample(float seconds,float phase,float walking,NoahAuthoredMode mode,NoahAuthoredDirection direction)
        {
            float t=Math.Max(0,Finite(seconds)),w=Math.Max(0,Math.Min(1,Finite(walking)));
            float step=(float)Math.Sin(Finite(phase)),breath=(float)Math.Sin(t*2.3),idle=1-w;
            bool talk=mode==NoahAuthoredMode.Talk,greet=mode==NoahAuthoredMode.Greet;
            float wave=greet?(float)Math.Sin(t*10):0;
            var body=new NoahPartTransform(0,-30-Math.Abs(step)*2.4f*w-breath*.9f*idle,step*.018f*w+breath*.004f*idle+(greet?.022f:0));
            var p=new NoahAuthoredPose{Body=body,
                Head=body.Child(-20,-238,talk?(float)Math.Sin(t*4)*.018f:greet?-.035f+(float)Math.Sin(t*3)*.01f:breath*.006f),
                FarArm=body.Child(-97,-180,.70f+step*.15f*w+wave*.22f+breath*.018f*idle),
                NearArm=body.Child(96,-185,-step*.12f*w+(talk?(float)Math.Sin(t*3)*.025f:breath*.012f*idle)),
                Tail=body.Child(58,-54,-step*.035f*w+breath*.012f*idle),Pouch=body.Child(-90,-110),
                NearLeg=new NoahPartTransform(54+step*5*w,-Math.Max(0,step)*7*w),
                FarLeg=new NoahPartTransform(-56-step*5*w,-Math.Max(0,-step)*7*w)};
            if(direction==NoahAuthoredDirection.Right)
            {
                p.Body=p.Body.OppositePlacement();p.Head=p.Head.OppositePlacement();p.FarArm=p.FarArm.OppositePlacement();p.NearArm=p.NearArm.OppositePlacement();
                p.FarLeg=p.FarLeg.OppositePlacement();p.NearLeg=p.NearLeg.OppositePlacement();
                var tail=p.Tail.OppositePlacement();p.Tail=new NoahPartTransform(-25,tail.Y,tail.Rotation);p.Pouch=p.Body.Child(-115,-75);
            }
            else if(direction==NoahAuthoredDirection.Back)
            {
                p.Head=new NoahPartTransform(0,p.Head.Y,p.Head.Rotation);p.Pouch=body.Child(95,-110);
                p.FarArm=body.Child(-110,-180,step*.15f*w+wave*.22f);
                p.NearArm=body.Child(110,-185,-step*.12f*w-wave*.22f);
                p.Tail=new NoahPartTransform(-15,p.Tail.Y,p.Tail.Rotation);
            }
            return p;
        }
    }
}
