using System;

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
        public const float YawDegrees=18f;
        public static bool SmilingEyes(float remaining)=>float.IsFinite(remaining)&&remaining>.34f&&remaining<.65f;
        public static Noah25DTrialPose Sample(float stride,float walking,float facing,float resting,float elapsed,float reactionRemaining,bool arrival,bool moving)
        {
            double phase=float.IsFinite(stride)?stride:0,time=float.IsFinite(elapsed)&&elapsed>=0?elapsed:0;
            walking=Clamp(walking,0,1);facing=Clamp(facing,-1,1);resting=Clamp(resting,0,1);
            float step=(float)Math.Sin(phase),idle=1-walking,settle=4*walking*idle;
            var reaction=TownCharacterMotion.Reaction(reactionRemaining,ReactionSeconds,arrival);
            float progress=Clamp(1-reactionRemaining/ReactionSeconds,0,1);
            float greeting=float.IsFinite(reactionRemaining)&&reactionRemaining>0&&reactionRemaining<ReactionSeconds?
                (float)Math.Sin(progress*Math.PI)*(float)Math.Sin(progress*Math.PI*4):0;
            return new Noah25DTrialPose(Math.Abs(step)*3.1f*walking+reaction.Hop*.4f,
                Clamp((step*1.2f+facing*.3f)*walking+(float)Math.Sin(time*.9+3.085)*(.24f+.08f*resting)*idle+
                    facing*settle*.12f+reaction.Lean*.4f-facing*2.8f,-4.2f,4.2f),
                // The smoothed walking envelope controls the settle on both
                // sides of a start/stop; the discrete moving flag cannot snap it.
                step*walking,(float)Math.Sin(time*1.8+3.085)*(.0045f+.0025f*idle)+reaction.Breath*.15f-settle*.0005f,
                Clamp((float)Math.Sin(time*1.3+3.085)*.055f*idle+step*.18f*walking+greeting*.95f,-1,1),
                facing,(float)Math.Cos(phase)*walking*facing*1.8f);
        }
        public static StreetCharacterOffset VertexOffset(float u,float v,Noah25DTrialPose pose)
        {
            u=Clamp(u,0,1);v=Clamp(v,0,1);
            float paws=Smooth((u-.12f)/.04f)*(1-Smooth((u-.60f)/.05f));
            float side=Clamp((u-FootCenterU)/.095f,-1,1),footBand=(1-Smooth(v/.18f))*paws;
            float lift=(Math.Max(0,side*pose.Foot)*5.25f-pose.Hop)*footBand;
            // Two small regions in the original illustration. Neither the
            // centre of the coat nor the squirrel's tail is an arm.
            float left=Island(u,v,.16f,.43f,.15f,.15f)*(1-Smooth((v-.44f)/.05f));
            float right=Island(u,v,.50f,.33f,.10f,.10f);
            float hand=pose.Hand*(left*.9f-right*.45f);
            // Face/hat and upper tail curl overlap in the original drawing.
            // Turn the complete illustration about its planted paw in the
            // actor transform; do not shear a horizontal "head" strip.
            return new StreetCharacterOffset(-side*pose.Travel*footBand+hand*5.2f,lift+hand*1.2f);
        }
        // A shallow relief of the unchanged original image, not a replacement
        // character model. Feet and the asymmetric outer tail remain on the
        // source plane. UVs are never resampled or mirrored by the relief.
        public static float SurfaceDepth(float u,float v)
        {
            u=Clamp(u,0,1);v=Clamp(v,0,1);
            return -Math.Max(8*Island(u,v,.40f,.31f,.25f,.24f),5.5f*Island(u,v,.36f,.67f,.28f,.24f));
        }
        public static float SurfaceLight(float u,float v,Noah25DTrialPose pose)
        {
            u=Clamp(u,0,1);v=Clamp(v,0,1);
            float face=VolumeLight(.36f,.67f,.30f,.26f),body=VolumeLight(.40f,.31f,.25f,.25f);
            float chin=Island(u,v,.36f,.49f,.22f,.055f)*.065f;
            return Clamp(Math.Min(face,body)-chin,.78f,1);
            float VolumeLight(float cx,float cy,float rx,float ry)
            {
                float x=(u-cx)/rx,y=(v-cy)/ry,q=1-x*x-y*y;
                if(q<=0)return 1;
                float weight=Smooth(q*5),normal= (float)Math.Sqrt(Math.Max(0,1-x*x));
                float illumination=.85f+.15f*(.95f*normal+(-.32f-Clamp(pose.Look,-1,1)*.14f)*x);
                return 1+(Clamp(illumination,.78f,1)-1)*weight;
            }
        }
        public static StreetCharacterOffset SupportTranslation(float artWidth,Noah25DTrialPose pose,float scaleX,float scaleY)
        {
            artWidth=Clamp(artWidth,0,512);scaleX=Clamp(scaleX,0,2);scaleY=Clamp(scaleY,0,2);
            double radians=Clamp(pose.Lean,-4.2f,4.2f)*Math.PI/180,sin=Math.Sin(radians),cos=Math.Cos(radians);
            double yawCos=Math.Cos(Clamp(pose.Look,-1,1)*YawDegrees*Math.PI/180);
            var left=VertexOffset(LeftFootU,0,pose);var right=VertexOffset(RightFootU,0,pose);
            double lx=(artWidth*(LeftFootU-FootCenterU)+left.X)*scaleX,ly=left.Y*scaleY;
            double rx=(artWidth*(RightFootU-FootCenterU)+right.X)*scaleX,ry=right.Y*scaleY;
            double leftY=lx*yawCos*sin+ly*cos,rightY=rx*yawCos*sin+ry*cos;
            bool supportLeft=leftY<=rightY;double x=supportLeft?lx:rx,y=supportLeft?ly:ry;
            // Solve after rotation and breathing, so an actual paw touches the
            // actor's navigation floor even during a greeting or a gentle lean.
            return new StreetCharacterOffset((float)(x-(x*yawCos*cos-y*sin)),(float)-Math.Min(leftY,rightY));
        }
        // Immutable rejected-candidate renderer for an honest same-instant
        // Before (1d11629); never selected by product UI or persistence.
        public static Noah25DTrialPose PreviousSample(float stride,float walking,float facing,float resting,float elapsed,float reactionRemaining,bool arrival,bool moving)
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
        public static StreetCharacterOffset PreviousVertexOffset(float u,float v,Noah25DTrialPose pose)
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
        public static StreetCharacterOffset PreviousSupportTranslation(float artWidth,Noah25DTrialPose pose,float scaleX,float scaleY)
        {
            artWidth=Clamp(artWidth,0,512);scaleX=Clamp(scaleX,0,2);scaleY=Clamp(scaleY,0,2);
            double radians=Clamp(pose.Lean,-2,2)*Math.PI/180,sin=Math.Sin(radians),cos=Math.Cos(radians);
            var left=PreviousVertexOffset(LeftFootU,0,pose);var right=PreviousVertexOffset(RightFootU,0,pose);
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
