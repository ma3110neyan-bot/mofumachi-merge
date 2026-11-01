using Mofumachi.Presentation;
using UnityEngine;
namespace Mofumachi.Editor
{
    // Read existing cropped/masked pixels only; never alter source art or atlas.
    internal static class CharacterNeckRegistration
    {
        internal static Vector2 Find(Color[] pixels,int width,int height,Vector2 pivot,float scale,bool chin)
        {
            var alpha=new byte[pixels.Length];
            for(int row=0;row<height;row++)for(int col=0;col<width;col++)alpha[row*width+col]=(byte)Mathf.RoundToInt(pixels[(height-1-row)*width+col].a*255);
            CharacterNeckBinding.FindJoint(alpha,width,height,pivot.x,pivot.y,scale,chin,out float x,out float y);
            return new Vector2(x,y);
        }
    }
}
