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
            float x=direction=="left"?-20:direction=="right"?20:0,y;
            switch(character)
            {
                case "noah":y=direction=="left"?-219.0f:direction=="right"?-216.5f:-224.0f;break;
                case "momo":y=direction=="left"?-206.5f:direction=="right"?-201.5f:-218.0f;break;
                case "lulu":y=direction=="left"?-197.5f:direction=="right"?-194.0f:-193.0f;break;
                case "pom":y=direction=="left"?-197.5f:direction=="right"?-199.0f:-207.5f;break;
                case "fio":y=direction=="left"?-196.0f:direction=="right"?-197.0f:-206.5f;break;
                case "miel":y=direction=="left"?-202.5f:direction=="right"?-196.0f:-199.0f;break;
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
