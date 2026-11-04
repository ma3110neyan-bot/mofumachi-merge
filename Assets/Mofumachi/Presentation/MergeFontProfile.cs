using System;
namespace Mofumachi.Presentation
{
    public enum MergeFontVariant { A, B, C }
    public static class MergeFontProfile
    {
        public static bool IsMenu(string name)
        {
            if(name=="Back label"||name=="Merge actions label"||name=="Deliver label"||name=="Nav label")return true;
            return name!=null&&name.StartsWith("Generate frame ",StringComparison.Ordinal)&&name.EndsWith(" label",StringComparison.Ordinal);
        }
        public static string Font(MergeFontVariant variant)=>variant==MergeFontVariant.C?"UIFontStory":"UIFontAccent";
        public static bool Bold(MergeFontVariant variant)=>variant!=MergeFontVariant.C;
        public static float Outline(MergeFontVariant variant)=>variant==MergeFontVariant.B?.35f:0;
    }
}
