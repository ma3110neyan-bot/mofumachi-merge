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

    public readonly struct NoahDepthVertex
    {
        public float X {get;} public float Y {get;} public float Z {get;} public float Light {get;}
        public NoahDepthVertex(float x,float y,float z,float light){X=x;Y=y;Z=z;Light=light;}
    }
    public readonly struct NoahDepthFocusPose
    {
        public Noah25DTrialPose Motion {get;}
        public float HeadFacing {get;} public float Forward {get;} public float Sway {get;} public float Nod {get;}
        public float Yaw {get;} public float Pitch {get;} public float Roll {get;}
        internal readonly double BodyCosY,BodySinY,BodyCosX,BodySinX,HeadCosY,HeadSinY,HeadCosX,HeadSinX,RootCosY,RootSinY,RootCosX,RootSinX,RootCosZ,RootSinZ;
        internal readonly float HeadDepth;
        internal readonly bool Prepared;
        internal NoahDepthFocusPose(Noah25DTrialPose motion,float head,float forward,float sway,float nod,float yaw,float pitch,float roll)
        {
            Motion=motion;HeadFacing=head;Forward=forward;Sway=sway;Nod=nod;Yaw=yaw;Pitch=pitch;Roll=roll;
            Prepared=true;
            const double radians=Math.PI/180;
            BodyCosY=Math.Cos(motion.Look*2*radians);BodySinY=Math.Sin(motion.Look*2*radians);BodyCosX=Math.Cos(sway*.8*radians);BodySinX=Math.Sin(sway*.8*radians);
            HeadCosY=Math.Cos(-head*6*radians);HeadSinY=Math.Sin(-head*6*radians);HeadCosX=Math.Cos((-pitch*.65+nod)*radians);HeadSinX=Math.Sin((-pitch*.65+nod)*radians);
            RootCosY=Math.Cos(yaw*radians);RootSinY=Math.Sin(yaw*radians);RootCosX=Math.Cos(pitch*radians);RootSinX=Math.Sin(pitch*radians);RootCosZ=Math.Cos(roll*radians);RootSinZ=Math.Sin(roll*radians);
            HeadDepth=-10+motion.Foot*.45f;
        }
    }
    public readonly struct NoahGroundingProfile
    {
        public float X {get;} public float Y {get;} public float Width {get;} public float Height {get;} public float Alpha {get;}
        internal NoahGroundingProfile(float x,float y,float w,float h,float alpha){X=x;Y=y;Width=w;Height=h;Alpha=alpha;}
    }
    // A layered pose of the same formal pixels. Keep the preceding source22
    // renderer intact for an honest current-Before, including its original UVs.
    public static class TownNoahDepthFocus
    {
        public static NoahDepthFocusPose Pose(Noah25DTrialPose motion,float stride,float walking,float headFacing,float forward,float remaining)
        {
            walking=C(walking,0,1);headFacing=C(headFacing,-1,1);forward=C(forward,-1,1)*walking;
            motion=new Noah25DTrialPose(C(motion.Hop,0,5),C(motion.Lean,-4.2f,4.2f),C(motion.Foot,-1,1),C(motion.Breath,-.01f,.01f),C(motion.Hand,-1,1),C(motion.Look,-1,1),C(motion.Travel,-2,2));
            float phase=float.IsFinite(stride)?stride:0,sway=(float)Math.Cos(phase)*walking;
            float nod=float.IsFinite(remaining)&&remaining>0&&remaining<1?(float)Math.Sin((1-remaining)*Math.PI)*2.4f:0;
            return new NoahDepthFocusPose(motion,headFacing,forward,sway,nod,-motion.Look*12*(1-Math.Abs(forward)*.65f),-forward*4+sway*1.2f,C(motion.Lean+motion.Look*2.2f,-3.5f,3.5f));
        }
        public static NoahDepthVertex Vertex(float u,float v,float width,float height,NoahDepthFocusPose pose)
        {
            if(!pose.Prepared)pose=Pose(default,0,0,0,0,0);
            u=C(u,0,1);v=C(v,0,1);width=C(width,0,512);height=C(height,0,512);
            var m=pose.Motion;var offset=TownNoah25DTrial.VertexOffset(u,v,m);
            var original=new NoahDepthVertex(width*(u-TownNoah25DTrial.FootCenterU)+offset.X,height*v+offset.Y,0,1);
            float upperHead=S(u/.16f)*(1-S((u-.65f)/.05f))*S((v-.42f)/.11f);
            // Keep the original mouth and chin on the same rigid head plane
            // as the eyes. Plateau boundaries coincide with existing grid
            // columns .30/.625 and row .44, so triangle interpolation cannot
            // blend the muzzle back into the torso. The raised paw is outside.
            float muzzle=S((u-.18f)/.12f)*(1-S((u-.625f)/.075f))*S((v-.36f)/.08f);
            float head=Math.Max(upperHead,muzzle);
            float torso=Island(u,v,.40f,.32f,.25f,.24f);
            var body=Rotate(original,width*(.40f-.419f),height*.32f,-5+pose.Sway*1.2f,pose.BodyCosY,pose.BodySinY,pose.BodyCosX,pose.BodySinX);
            var point=Lerp(original,body,torso);
            var face=Rotate(original,width*(.36f-.419f),height*.53f,pose.HeadDepth,pose.HeadCosY,pose.HeadSinY,pose.HeadCosX,pose.HeadSinX);
            face=new NoahDepthVertex(face.X+pose.HeadFacing*.7f,face.Y,face.Z,1);
            point=Lerp(point,face,head);
            float left=Island(u,v,.16f,.43f,.15f,.15f)*(1-S((v-.44f)/.05f));
            float right=Island(u,v,.50f,.33f,.10f,.10f);
            float paws=S((u-.12f)/.04f)*(1-S((u-.60f)/.05f)),foot=(1-S(v/.18f))*paws;
            float side=C((u-.419f)/.095f,-1,1);
            float z=point.Z-(left*(5+m.Foot*2)+right*(5-m.Foot*2))*(1-head)-side*m.Foot*1.4f*foot;
            float nx=C((u-.36f)/.30f,-1,1);
            float faceShade=.025f+.10f*S(Math.Abs(nx))+.04f*Math.Max(0,nx+pose.HeadFacing*.10f);
            float bodyShade=.055f+.10f*S(Math.Abs((u-.4f)/.25f))+.04f*Math.Max(0,(u-.4f)/.25f+m.Look*.1f);
            float light=C(1-head*faceShade-torso*(1-head)*bodyShade-(left+right)*(1-head)*.025f,.80f,1);
            return new NoahDepthVertex(point.X,point.Y,z,light);
        }
        private static NoahDepthVertex Rotate(NoahDepthVertex point,float pivotX,float pivotY,float depth,double cy,double sy,double cx,double sx)
        {
            double x=point.X-pivotX,y=point.Y-pivotY,rx=x*cy,rz=-x*sy;
            return new NoahDepthVertex((float)rx+pivotX,(float)(y*cx-rz*sx)+pivotY,(float)(y*sx+rz*cx)+depth,1);
        }
        private static NoahDepthVertex Lerp(NoahDepthVertex a,NoahDepthVertex b,float t)
            =>new NoahDepthVertex(a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t,a.Z+(b.Z-a.Z)*t,1);
        public static NoahDepthVertex Project(NoahDepthVertex point,NoahDepthFocusPose pose,float sx,float sy,float sz)
        {
            if(!pose.Prepared)pose=Pose(default,0,0,0,0,0);
            sx=C(sx,0,2);sy=C(sy,0,2);sz=C(sz,0,2);
            double x=point.X*sx*pose.RootCosY+point.Z*sz*pose.RootSinY,z=-point.X*sx*pose.RootSinY+point.Z*sz*pose.RootCosY;
            double y=point.Y*sy*pose.RootCosX-z*pose.RootSinX,depth=point.Y*sy*pose.RootSinX+z*pose.RootCosX;
            return new NoahDepthVertex((float)(x*pose.RootCosZ-y*pose.RootSinZ),(float)(x*pose.RootSinZ+y*pose.RootCosZ),(float)depth,point.Light);
        }
        public static StreetCharacterOffset Support(float width,float height,NoahDepthFocusPose pose,float sx,float sy,float sz)
        {
            var left=Vertex(.303f,0,width,height,pose);var right=Vertex(.535f,0,width,height,pose);
            var a=Project(left,pose,sx,sy,sz);var b=Project(right,pose,sx,sy,sz);
            return a.Y<=b.Y?new StreetCharacterOffset(left.X*C(sx,0,2)-a.X,-a.Y):new StreetCharacterOffset(right.X*C(sx,0,2)-b.X,-b.Y);
        }
        public static NoahGroundingProfile Grounding(bool focus)=>focus?new NoahGroundingProfile(.04f,-.06f,.92f,.16f,.38f):new NoahGroundingProfile(.18f,-.06f,.64f,.12f,.28f);
        public static float GroundOpacity(float radius,bool focus)
        {
            radius=Math.Abs(radius);float previousR=0,previousA=1;
            for(int i=0;i<4;i++)
            {
                float r=i==0?.25f:i==1?.55f:i==2?.80f:1;
                float a=focus?(i==0?.98f:i==1?.68f:i==2?.23f:0):(i==0?.90f:i==1?.50f:i==2?.15f:0);
                if(radius<=r)return previousA+(a-previousA)*(radius-previousR)/(r-previousR);
                previousR=r;previousA=a;
            }
            return 0;
        }
        public static float GroundAlpha(float x,float y,float width,float height,bool focus)
        {
            var p=Grounding(focus);if(width<=0||height<=0)return 0;
            float nx=x/(p.Width*width*.5f),ny=(y-(p.Y+p.Height*.5f)*height)/(p.Height*height*.5f);
            return GroundOpacity((float)Math.Sqrt(nx*nx+ny*ny),focus)*p.Alpha;
        }
        private static float Island(float u,float v,float x,float y,float w,float h){float a=(u-x)/w,b=(v-y)/h;return 1-S(a*a+b*b);}
        private static float S(float x){x=C(x,0,1);return x*x*(3-2*x);}
        private static float C(float x,float min,float max)=>float.IsFinite(x)?Math.Max(min,Math.Min(max,x)):0;
    }

    // Final, opt-in weighted 2D rig. Each bone is an affine transform of the
    // SAME formal atlas, not new side/back artwork or a 3D character model.
    // Depth is explicitly projected into XY, so an orthographic UI camera does
    // not silently discard the separation as it did with a shallow Z relief.
    internal readonly struct NoahRigBone
    {
        private readonly float xx,xy,yx,yy,tx,ty,depth,light;
        internal NoahRigBone(float u,float v,float yaw,float roll,float z,float shiftY,float shade)
        {
            double y=yaw*Math.PI/180,r=roll*Math.PI/180;
            xx=(float)(Math.Cos(y)*Math.Cos(r));xy=(float)-Math.Sin(r);
            yx=(float)(Math.Cos(y)*Math.Sin(r));yy=(float)Math.Cos(r);
            float px=u-.419f;
            tx=px-xx*px-xy*v;ty=v-yx*px-yy*v;
            // Depth uses art-height units, independent of the actor's size.
            tx-=z*(.36f+(float)Math.Sin(y)*.7f);ty+=z*.08f+shiftY;depth=z;light=shade;
        }
        internal NoahDepthVertex Apply(float x,float y,float width,float height)
            =>new NoahDepthVertex((xx*x+xy*y+tx)*width,(yx*x+yy*y+ty)*height,depth*height,light);
    }
    public readonly struct NoahFinalRigPose
    {
        internal readonly NoahRigBone Head,Body,Tail,LeftArm,RightArm,LeftFoot,RightFoot;
        internal readonly float RootCos,RootSin;
        internal readonly bool Prepared;
        internal NoahFinalRigPose(Noah25DTrialPose m,float head,float forward)
        {
            Prepared=true;
            // All seven bones inherit ONE root tilt. Rotating each bind pivot
            // independently adds incompatible translations across skin seams.
            double root=m.Lean*.3*Math.PI/180;RootCos=(float)Math.Cos(root);RootSin=(float)Math.Sin(root);
            float lean=0,tilt=m.Foot*.55f;
            Head=new NoahRigBone(.36f,.50f,12+head*15,lean+m.Hand*.65f,-.18f,-forward*.012f,1);
            Body=new NoahRigBone(.40f,.31f,28+m.Look*12,lean,-.05f,0,.91f);
            Tail=new NoahRigBone(.73f,.27f,3+m.Look*3,lean*.4f,.025f,0,.89f);
            LeftArm=new NoahRigBone(.22f,.38f,15+m.Look*10,lean+m.Hand*7,-.10f-m.Foot*.015f,0,.96f);
            RightArm=new NoahRigBone(.53f,.36f,22+m.Look*10,lean-m.Hand*4,-.08f+m.Foot*.015f,0,.93f);
            LeftFoot=new NoahRigBone(.303f,.10f,18+m.Look*10,lean+tilt,-m.Foot*.018f,Math.Max(0,-m.Foot)*.024f,1);
            RightFoot=new NoahRigBone(.535f,.10f,18+m.Look*10,lean-tilt,m.Foot*.018f,Math.Max(0,m.Foot)*.024f,1);
        }
    }
    public static class TownNoahFinalRig
    {
        public static bool IsEnabled(string id,bool requested)=>requested&&id=="noah";
        public static NoahFinalRigPose Pose(Noah25DTrialPose motion,float headFacing,float forward)
        {
            var m=new Noah25DTrialPose(C(motion.Hop,0,5),C(motion.Lean,-4.2f,4.2f),C(motion.Foot,-1,1),C(motion.Breath,-.01f,.01f),C(motion.Hand,-1,1),C(motion.Look,-1,1),C(motion.Travel,-2,2));
            return new NoahFinalRigPose(m,C(headFacing,-1,1),C(forward,-1,1));
        }
        public static float HeadWeight(float u,float v)
        {
            float upper=S(u/.16f)*(1-S((u-.65f)/.19f))*S((v-.42f)/.11f);
            float chin=S((u-.18f)/.12f)*(1-S((u-.625f)/.215f))*S((v-.36f)/.08f);
            return Math.Max(upper,chin);
        }
        public static NoahDepthVertex Vertex(float u,float v,float width,float height,NoahFinalRigPose pose)
        {
            if(!pose.Prepared)pose=Pose(default,0,0);
            u=C(u,0,1);v=C(v,0,1);width=C(width,0,512);height=C(height,0,512);
            float x=u-.419f,head=HeadWeight(u,v),remaining=1-head;
            float tail=remaining*S((u-.625f)/.375f);remaining-=tail;
            float feet=remaining*(1-S(v/.18f))*S(u/.18f)*(1-S((u-.60f)/.18f));remaining-=feet;
            float leftFoot=feet*(1-S((u-.37f)/.10f)),rightFoot=feet-leftFoot;
            float leftArm=remaining*Island(u,v,.16f,.43f,.15f,.15f)*(1-S((v-.44f)/.05f));remaining-=leftArm;
            float rightArm=remaining*Island(u,v,.50f,.33f,.10f,.10f);remaining-=rightArm;
            var h=pose.Head.Apply(x,v,width,height);var b=pose.Body.Apply(x,v,width,height);var t=pose.Tail.Apply(x,v,width,height);
            var la=pose.LeftArm.Apply(x,v,width,height);var ra=pose.RightArm.Apply(x,v,width,height);var lf=pose.LeftFoot.Apply(x,v,width,height);var rf=pose.RightFoot.Apply(x,v,width,height);
            float px=h.X*head+b.X*remaining+t.X*tail+la.X*leftArm+ra.X*rightArm+lf.X*leftFoot+rf.X*rightFoot;
            float py=h.Y*head+b.Y*remaining+t.Y*tail+la.Y*leftArm+ra.Y*rightArm+lf.Y*leftFoot+rf.Y*rightFoot;
            return new NoahDepthVertex(px*pose.RootCos-py*pose.RootSin,px*pose.RootSin+py*pose.RootCos,
                h.Z*head+b.Z*remaining+t.Z*tail+la.Z*leftArm+ra.Z*rightArm+lf.Z*leftFoot+rf.Z*rightFoot,
                h.Light*head+b.Light*remaining+t.Light*tail+la.Light*leftArm+ra.Light*rightArm+lf.Light*leftFoot+rf.Light*rightFoot);
        }
        public static StreetCharacterOffset Support(float width,float height,NoahFinalRigPose pose)
        {
            var a=Vertex(.303f,0,width,height,pose);var b=Vertex(.535f,0,width,height,pose);
            return new StreetCharacterOffset(0,-Math.Min(a.Y,b.Y));
        }
        public static NoahGroundingProfile Grounding=>new NoahGroundingProfile(.01f,-.04f,.98f,.16f,.54f);
        public static float GroundOpacity(float radius)
        {
            radius=C(Math.Abs(radius),0,1);
            // Broad soft ellipse plus a compact contact core in the same mesh.
            return .65f*(1-S(radius*radius))+.35f*(1-S(radius/.42f));
        }
        private static float Island(float u,float v,float x,float y,float rx,float ry)
        {float dx=(u-x)/rx,dy=(v-y)/ry;return 1-S(dx*dx+dy*dy);}
        private static float S(float x){x=C(x,0,1);return x*x*(3-2*x);}
        private static float C(float x,float lo,float hi)=>float.IsFinite(x)?Math.Max(lo,Math.Min(hi,x)):0;
    }
}
