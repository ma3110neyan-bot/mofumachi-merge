using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
namespace Mofumachi.Tests
{
    // Capture-only disk contract; not part of the game presentation assembly.
    public static class MergeCaptureArtifacts
    {
        public static readonly IReadOnlyList<string> ImageNames=Array.AsReadOnly(new[]{
            "initial-720x1480-before-reconstructed.jpg","initial-720x1480-after.jpg",
            "initial-720x1280-before-reconstructed.jpg","initial-720x1280-after.jpg",
            "initial-640x1072-before-reconstructed.jpg","initial-640x1072-after.jpg",
            "bread-request-720x1480-before-reconstructed.jpg","bread-request-720x1480-after.jpg",
            "multi-requirement-720x1480-after.jpg","inventory-ready-720x1480-after.jpg","full-board-720x1280-after.jpg"});
        public static string Hash(byte[] bytes){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        public static string RequireOutput(string[] args)
        {
            const string flag="-mofumachiMergeReward";
            if(args==null||args.Count(a=>a==flag)!=1)throw new InvalidOperationException("Exactly one -mofumachiMergeReward absolute output directory is required; in-memory capture is not success.");
            int i=Array.IndexOf(args,flag);
            if(i+1>=args.Length||string.IsNullOrWhiteSpace(args[i+1])||!Path.IsPathRooted(args[i+1]))throw new InvalidOperationException("-mofumachiMergeReward requires an absolute output directory.");
            string output=Path.GetFullPath(args[i+1]);
            if(File.Exists(output)||(Directory.Exists(output)&&((File.GetAttributes(output)&FileAttributes.ReparsePoint)!=0||Directory.EnumerateFileSystemEntries(output).Any())))throw new IOException("Capture output must be empty; preserve previous evidence and use a new run: "+output);
            Directory.CreateDirectory(output);return output;
        }
        public static void VerifyWritten(string output,IReadOnlyDictionary<string,string> images)
        {
            if(images==null||images.Count!=11||!new HashSet<string>(images.Keys,StringComparer.Ordinal).SetEquals(ImageNames))throw new IOException("Exactly the eleven expected capture images are required.");
            if(!Directory.Exists(output))throw new IOException("Capture output directory missing: "+output);
            var actual=Directory.GetFiles(output).Where(p=>string.Equals(Path.GetExtension(p),".jpg",StringComparison.OrdinalIgnoreCase)).Select(Path.GetFileName).ToArray();
            if(actual.Length!=11||!new HashSet<string>(actual,StringComparer.Ordinal).SetEquals(ImageNames))throw new IOException("Eleven JPEG files were not written to "+output);
            foreach(string name in ImageNames)
            {
                string file=Path.Combine(output,name);var info=new FileInfo(file);
                if(!info.Exists||info.Length==0||(info.Attributes&FileAttributes.ReparsePoint)!=0||Hash(File.ReadAllBytes(file))!=images[name])throw new IOException("Capture JPEG missing, empty or changed: "+file);
            }
            var proof=new FileInfo(Path.Combine(output,"merge-reward-native.json"));
            if(!proof.Exists||proof.Length==0||(proof.Attributes&FileAttributes.ReparsePoint)!=0)throw new IOException("Capture JSON was not written: "+proof.FullName);
        }
    }
}
