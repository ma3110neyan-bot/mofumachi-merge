using System;
namespace Mofumachi.Presentation
{
    // Static, individually registered collar pivots in the body's material frame.
    // Front uses the unchanged complete sprite. No fitting, temporal state,
    // separate head animator or world-space movement is introduced here.
    public static class CharacterHeadBodyCalibration
    {
        public static NoahPartTransform Resolve(string character,string direction,NoahPartTransform body,float headAngle)
        {
            float x,y;
            switch(character)
            {
                case "noah":x=direction=="left"?16.401f:direction=="right"?20f:0f;y=direction=="left"?-219f:direction=="right"?-216.5f:-224f;break;
                case "momo":x=direction=="left"?11.505f:direction=="right"?-12.378f:0f;y=direction=="left"?-206.5f:direction=="right"?-201.5f:-218f;break;
                case "lulu":x=direction=="left"?8.953f:direction=="right"?20f:0f;y=direction=="left"?-197.5f:direction=="right"?-194f:-193f;break;
                case "pom":x=direction=="left"?22.146f:direction=="right"?-1.908f:0f;y=direction=="left"?-197.5f:direction=="right"?-199f:-207.5f;break;
                case "fio":x=direction=="left"?-12f:direction=="right"?12f:0f;y=direction=="left"?-196f:direction=="right"?-197f:-206.5f;break;
                case "miel":x=direction=="left"?7.482f:direction=="right"?20f:0f;y=direction=="left"?-202.5f:direction=="right"?-196f:-199f;break;
                default:throw new ArgumentException("Unregistered character: "+character);
            }
            if(direction!="left"&&direction!="right"&&direction!="back")throw new ArgumentException("Split head direction required: "+direction);
            // Apply the entire static translation through Body, never through
            // the head's independently animated nod angle. One current pose.
            body.Point(x,y,out float px,out float py);
            return new NoahPartTransform(px,py,headAngle);
        }
    }
}
