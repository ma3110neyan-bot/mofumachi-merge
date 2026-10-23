using System.IO;
using System.Text;
namespace Mofumachi.Tests
{
    // Test evidence only. Keep deliberate coordinate diagnostics out of Unity's
    // log queue so NoUnexpectedReceived still catches every real engine message.
    internal static class CaptureWorldTrace
    {
        public static void Append(string directory,string trace)
        {
            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory,"capture-world.txt"),trace+System.Environment.NewLine,new UTF8Encoding(false));
        }
    }
}
