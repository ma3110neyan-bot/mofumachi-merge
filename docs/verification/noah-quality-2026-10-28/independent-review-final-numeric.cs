using System;

namespace Mofumachi.Presentation
{
    public readonly struct StreetCharacterOffset
    {
        public float X {get;} public float Y {get;}
        public StreetCharacterOffset(float x,float y){X=x;Y=y;}
    }
    public readonly struct StreetCharacterPose
    {
        public float Hop {get;} public float Lean {get;} public float Foot {get;}
        public float Breath {get;} public float Hand {get;} public float Look {get;}
        public StreetCharacterPose(float hop,float lean,float foot):this(hop,lean,foot,0,0,0){}
        public StreetCharacterPose(float hop,float lean,float foot,float breath,float hand,float look)
        {Hop=hop;Lean=lean;Foot=foot;Breath=breath;Hand=hand;Look=look;}
    }
    public static class TownCharacterMotion
    {
        public static StreetCharacterPose Sample(float stride,float walking,float facing,float resting)
            =>Sample(stride,walking,facing,resting,Math.Max(0,stride),0);
        public static StreetCharacterPose Sample(float stride,float walking,float facing,float resting,float elapsed,int resident)
        {
            if(!float.IsFinite(stride)||!float.IsFinite(walking))return default;
            walking=Clamp(walking,0,1);facing=Clamp(facing,-1,1);resting=Clamp(resting,0,1);
            // Double intermediates keep even a finite float.MaxValue time finite.
            double time=float.IsFinite(elapsed)&&elapsed>=0?elapsed:0;
            double phase=((resident%6+6)%6)*.617;
            float step=(float)Math.Sin(stride),idle=1-walking;
            float sway=(float)Math.Sin(time*.9+phase)*(.45f+.2f*resting)*idle;
            return new StreetCharacterPose(Math.Abs(step)*2.2f*walking,
                (step*2.2f+facing*.8f)*walking+sway,step*walking,
                (float)Math.Sin(time*1.9+phase)*(.002f+.003f*idle),
                (float)Math.Sin(time*1.3+phase)*.12f*idle,
                Clamp(facing+(float)Math.Sin(time*.55+phase)*.22f*idle,-1,1));
        }
        // Map depth is measured from the painting's top edge, never the viewport.
        public static float DepthScale(float mapDepth)=>.94f+.12f*(float.IsFinite(mapDepth)?Clamp(mapDepth,0,1):.5f);
        public static float Turn(float current,float target,float seconds)
        {
            current=Clamp(current,-1,1);target=Clamp(target,-1,1);
            if(!float.IsFinite(seconds)||seconds<=0)return current;
            float distance=target-current,dt=Math.Min(seconds,.1f);
            if(Math.Abs(distance)<.0001f)return target;
            float eased=distance*(1-(float)Math.Exp(-dt*12));
            return current+Clamp(eased,-dt*6,dt*6);
        }
        public static float BlendMovement(float current,float target,float seconds)
        {
            current=Clamp(current,0,1);target=Clamp(target,0,1);
            if(!float.IsFinite(seconds)||seconds<=0)return current;
            float distance=target-current;
            if(Math.Abs(distance)<.0001f)return target;
            return current+distance*(1-(float)Math.Exp(-Math.Min(seconds,.1f)*(distance>0?10:14)));
        }
        public static StreetCharacterPose Reaction(float remaining,float duration,bool arrival)
        {
            if(!float.IsFinite(remaining)||!float.IsFinite(duration)||duration<=0||remaining<=0||remaining>=duration)return default;
            float progress=1-remaining/duration,envelope=(float)Math.Sin(progress*Math.PI);
            return new StreetCharacterPose(envelope*(arrival?4.6f:8),
                (float)Math.Sin(progress*Math.PI*2)*envelope*(arrival?1.2f:2.8f),0,
                (float)Math.Sin(progress*Math.PI*2)*envelope*.012f,
                (float)Math.Sin(progress*Math.PI*3)*envelope*(arrival?.35f:.85f),0);
        }
        public static StreetCharacterOffset VertexOffset(float u,float v,float stepping,float greeting,float facing)
        {
            u=Clamp(u,0,1);v=Clamp(v,0,1);stepping=Clamp(stepping,-1,1);greeting=Clamp(greeting,-1,1);facing=Clamp(facing,-1,1);
            float side=Clamp((u-.5f)/.18f,-1,1);
            float footBand=v<.26f?1-v/.26f:0;
            // Cancel the body's rise at the supporting foot. Only the other
            // original foot lifts; neither shoe sinks through the contact shadow.
            float lift=(Math.Max(0,side*stepping)*4.7f-Math.Abs(stepping)*2.2f)*footBand;
            float outer=Clamp((Math.Abs(u-.5f)-.125f)/.25f,0,1);
            float hand=v>=.26f&&v<=.52f?(greeting+stepping*.24f)*side*(float)Math.Sin((v-.26f)/.26f*Math.PI)*outer*1.8f:0;
            float head=v>=.52f?1:Clamp((v-.40f)/.12f,0,1);
            // Translate the head as one piece; retain the atlas UVs and asymmetry.
            float turn=facing*.9f*head*head*(3-2*head);
            return new StreetCharacterOffset(hand+turn+stepping*1.1f*footBand,lift);
        }
        private static float Clamp(float value,float min,float max)=>float.IsFinite(value)?Math.Max(min,Math.Min(max,value)):0;
        public static bool Blink(float elapsed,int resident)
        {
            if(!float.IsFinite(elapsed)||elapsed<0)return false;
            int index=(resident%6+6)%6;
            double period=4.1+index*.17,time=elapsed,cycle=(time+index*.617)%period;
            bool doubleBlink=(Math.Floor(time/period)+index)%3==2;
            return cycle<.1||doubleBlink&&cycle>.22&&cycle<.28;
        }
    }
}



namespace Mofumachi.Presentation
{
    public readonly struct Noah25DTrialPose
    {
        public float Hop {get;} public float Lean {get;} public float Foot {get;}
        public float Breath {get;} public float Hand {get;} public float Look {get;} public float Travel {get;}
        public Noah25DTrialPose(float hop,float lean,float foot,float breath,float hand,float look,float travel)
        {Hop=hop;Lean=lean;Foot=foot;Breath=breath;Hand=hand;Look=look;Travel=travel;}
    }
    // Presentation-only opt-in: no favorite, world, dialogue or save state lives here.
    public static class TownNoah25DTrial
    {
        public static bool IsEnabled(string characterId,bool requested)=>requested&&characterId=="noah";
        // Measured on the unchanged normal Noah crop (1263,14,254,323).
        // The large tail is outside the two paw centres.
        public const float LeftFootU=.303f,RightFootU=.535f,FootCenterU=.419f;
        public const float ReactionSeconds=1f;
        public static bool SmilingEyes(float remaining)=>float.IsFinite(remaining)&&remaining>.34f&&remaining<.65f;
        public static Noah25DTrialPose Sample(float stride,float walking,float facing,float resting,float elapsed,float reactionRemaining,bool arrival,bool moving)
        {
            double phase=float.IsFinite(stride)?stride:0,time=float.IsFinite(elapsed)&&elapsed>=0?elapsed:0;
            walking=Clamp(walking,0,1);facing=Clamp(facing,-1,1);resting=Clamp(resting,0,1);
            float step=(float)Math.Sin(phase),idle=1-walking,settle=4*walking*idle;
            var reaction=TownCharacterMotion.Reaction(reactionRemaining,ReactionSeconds,arrival);
            float progress=Clamp(1-reactionRemaining/ReactionSeconds,0,1);
            float greeting=float.IsFinite(reactionRemaining)&&reactionRemaining>0&&reactionRemaining<ReactionSeconds?
                (float)Math.Sin(progress*Math.PI)*(.7f+.3f*(float)Math.Sin(progress*Math.PI*4)):0;
            return new Noah25DTrialPose(Math.Abs(step)*2.6f*walking+reaction.Hop*.4f,
                Clamp((step*.95f+facing*.3f)*walking+(float)Math.Sin(time*.9+3.085)*(.24f+.08f*resting)*idle+
                    facing*settle*.12f+reaction.Lean*.4f-facing*1.45f,-2,2),
                // The smoothed walking envelope controls the settle on both
                // sides of a start/stop; the discrete moving flag cannot snap it.
                step*walking,(float)Math.Sin(time*1.8+3.085)*(.002f+.002f*idle)+reaction.Breath*.15f-settle*.0005f,
                Clamp((float)Math.Sin(time*1.3+3.085)*.055f*idle+step*.24f*walking+greeting*.9f,-1,1),
                facing,(float)Math.Cos(phase)*walking*facing*1.8f);
        }
        public static StreetCharacterOffset VertexOffset(float u,float v,Noah25DTrialPose pose)
        {
            u=Clamp(u,0,1);v=Clamp(v,0,1);
            float paws=Smooth((u-.12f)/.04f)*(1-Smooth((u-.60f)/.05f));
            float side=Clamp((u-FootCenterU)/.095f,-1,1),footBand=(1-Smooth(v/.18f))*paws;
            float lift=(Math.Max(0,side*pose.Foot)*5.4f-pose.Hop)*footBand;
            // Two small regions in the original illustration. Neither the
            // centre of the coat nor the squirrel's tail is an arm.
            float left=Island(u,v,.16f,.43f,.15f,.15f)*(1-Smooth((v-.44f)/.05f));
            float right=Island(u,v,.50f,.33f,.10f,.10f);
            float hand=pose.Hand*(left*.9f-right*.45f);
            // Face/hat and upper tail curl overlap in the original drawing.
            // Turn the complete illustration about its planted paw in the
            // actor transform; do not shear a horizontal "head" strip.
            return new StreetCharacterOffset(-side*pose.Travel*footBand+hand*2.4f,lift+hand*3.1f);
        }
        public static StreetCharacterOffset SupportTranslation(float artWidth,Noah25DTrialPose pose,float scaleX,float scaleY)
        {
            artWidth=Clamp(artWidth,0,512);scaleX=Clamp(scaleX,0,2);scaleY=Clamp(scaleY,0,2);
            double radians=Clamp(pose.Lean,-2,2)*Math.PI/180,sin=Math.Sin(radians),cos=Math.Cos(radians);
            var left=VertexOffset(LeftFootU,0,pose);var right=VertexOffset(RightFootU,0,pose);
            double lx=(artWidth*(LeftFootU-FootCenterU)+left.X)*scaleX,ly=left.Y*scaleY;
            double rx=(artWidth*(RightFootU-FootCenterU)+right.X)*scaleX,ry=right.Y*scaleY;
            double leftY=lx*sin+ly*cos,rightY=rx*sin+ry*cos;
            bool supportLeft=leftY<=rightY;double x=supportLeft?lx:rx,y=supportLeft?ly:ry;
            // Solve after rotation and breathing, so an actual paw touches the
            // actor's navigation floor even during a greeting or a gentle lean.
            return new StreetCharacterOffset((float)(x-(x*cos-y*sin)),(float)-Math.Min(leftY,rightY));
        }
        public static StreetCharacterOffset BlinkUvOffset(float u,float v)
        {
            if(!float.IsFinite(u)||!float.IsFinite(v)||v<.53f||v>.67f)return default;
            // Normal eye centres (1330,144)/(1405,148) registered to the
            // existing approved smiling eyes (1330,480)/(1403,487).
            // Atlas UV Y points upward, while the measured PNG Y points down.
            if(u>=.16f&&u<=.37f)return new StreetCharacterOffset(0,-336f/1024f);
            if(u>=.47f&&u<=.65f)return new StreetCharacterOffset(-2f/1536f,-339f/1024f);
            return default;
        }
        private static float Island(float u,float v,float x,float y,float width,float height)
        {float dx=(u-x)/width,dy=(v-y)/height;return 1-Smooth(dx*dx+dy*dy);}
        private static float Smooth(float value){value=Clamp(value,0,1);return value*value*(3-2*value);}
        private static float Clamp(float value,float min,float max)=>float.IsFinite(value)?Math.Max(min,Math.Min(max,value)):0;
    }
}

public static class NoahIndependentReviewNumeric {
 public static void Run() {
  float[] xs={0,.06f,.16f,.18f,.30f,.37f,.42f,.47f,.53f,.625f,.65f,.70f,.84f,1};
  float[] ys={0,.06f,.14f,.22f,.28f,.36f,.44f,.49f,.53f,.60f,.67f,.80f,1};
  float minArea=float.MaxValue,maxFootError=0,maxTailOffset=0;int states=0;
  for(int phase=0;phase<=60;phase++)for(int remain=0;remain<=20;remain++)
  foreach(float walk in new[]{0f,.5f,1f})foreach(float facing in new[]{-1f,1f}) {
   var p=Mofumachi.Presentation.TownNoah25DTrial.Sample(phase*(float)Math.PI/30,walk,facing,0,2,remain/20f,false,walk>0);states++;
   Mofumachi.Presentation.StreetCharacterOffset Point(float x,float y) {
    var o=Mofumachi.Presentation.TownNoah25DTrial.VertexOffset(x,y,p);
    if(x>=.70f)maxTailOffset=Math.Max(maxTailOffset,Math.Max(Math.Abs(o.X),Math.Abs(o.Y)));
    return new Mofumachi.Presentation.StreetCharacterOffset(61.33746f*x+o.X,78*y+o.Y);
   }
   float Area(Mofumachi.Presentation.StreetCharacterOffset a,Mofumachi.Presentation.StreetCharacterOffset b,Mofumachi.Presentation.StreetCharacterOffset c)=>(b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);
   for(int y=0;y<ys.Length-1;y++)for(int x=0;x<xs.Length-1;x++) {
    var a=Point(xs[x],ys[y]);var b=Point(xs[x+1],ys[y]);var c=Point(xs[x+1],ys[y+1]);var d=Point(xs[x],ys[y+1]);
    minArea=Math.Min(minArea,Math.Min(Area(a,b,c),Area(a,c,d)));
   }
   float sx=(1-p.Breath*.35f)*(1-.035f*Math.Abs(p.Look)),sy=1+p.Breath;
   var root=Mofumachi.Presentation.TownNoah25DTrial.SupportTranslation(61.33746f,p,sx,sy);
   double rad=p.Lean*Math.PI/180;
   double Foot(float u) {var o=Mofumachi.Presentation.TownNoah25DTrial.VertexOffset(u,0,p);return (61.33746*(u-.419)+o.X)*sx*Math.Sin(rad)+o.Y*sy*Math.Cos(rad)+root.Y;}
   maxFootError=Math.Max(maxFootError,(float)Math.Abs(Math.Min(Foot(.303f),Foot(.535f))));
  }
  Console.WriteLine($"Cartesian numeric check: {states} independent phase/reaction/walk/facing states; minimum triangle area={minArea:R}; maximum planted-foot Y error={maxFootError:R}; maximum tail mesh-corner offset={maxTailOffset:R}");
  if(minArea<=.02f||maxFootError>1e-5||maxTailOffset!=0)throw new Exception("Independent numeric verification failed");
  foreach(float facing in new[]{-1f,-.5f,0f,.5f,1f}) {
   var p=Mofumachi.Presentation.TownNoah25DTrial.Sample(0,0,facing,0,1,0,false,false);
   float sx=(1-p.Breath*.35f)*(1-.035f*Math.Abs(p.Look)),sy=1+p.Breath;
   var root=Mofumachi.Presentation.TownNoah25DTrial.SupportTranslation(61.33746f,p,sx,sy);double rad=p.Lean*Math.PI/180;
   double x=61.33746*(.3-.419)*sx,y=78*.67*sy;
   Console.WriteLine($"Idle facing={facing}: lean={p.Lean:R}; positive yaw scale={sx:R}; actual face point X={x*Math.Cos(rad)-y*Math.Sin(rad)+root.X:R}");
  }
 }
}
