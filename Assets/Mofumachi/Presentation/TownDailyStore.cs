using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    public interface ITownDailyStore {TownDailyData Load();void Save(TownDailyData data);}
    public static class TownDailyCodec
    {
        public static byte[] Encode(TownDailyData data)
        {using(var s=new MemoryStream()){new DataContractJsonSerializer(typeof(TownDailyData)).WriteObject(s,data);return s.ToArray();}}
        public static TownDailyData Decode(byte[] bytes)
        {using(var s=new MemoryStream(bytes))return (TownDailyData)new DataContractJsonSerializer(typeof(TownDailyData)).ReadObject(s);}
        public static TownDailyData Clone(TownDailyData data)=>Decode(Encode(data));
        private static bool Day(string s,bool empty=false)=>s!=null&&(empty&&s.Length==0||DateTime.TryParseExact(s,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out _));
        public static void Validate(TownDailyData d)
        {
            if(d==null||d.Version!=1||!Day(d.Day,true)||d.Merges<0||d.Merges>3||d.Talks<0||d.Talks>1||d.Walks<0||d.Walks>1||d.Characters==null||d.Characters.Count!=6||d.DecorationDays==null)throw new InvalidDataException("Invalid social journal");
            if(d.Characters.Any(c=>c==null||!CharacterCatalog.IsKnown(c.Id)||c.Meetings<0||c.Meetings>999999||c.Outings<0||c.Outings>999999||c.SeenReactions<0||c.SeenReactions>3)||d.Characters.Select(c=>c.Id).Distinct().Count()!=6||d.DecorationDays.Any(x=>!Day(x)||string.CompareOrdinal(x,d.Day)>0)||d.DecorationDays.Distinct().Count()!=d.DecorationDays.Count)throw new InvalidDataException("Invalid social collection");
            if(d.Claimed&&(d.Merges<3||d.Talks<1||d.Walks<1||!d.DecorationDays.Contains(d.Day)))throw new InvalidDataException("Invalid daily claim");
        }
    }
    public sealed class MemoryTownDailyStore:ITownDailyStore
    {
        private TownDailyData saved=new TownDailyData();
        public TownDailyData Load()=>TownDailyCodec.Clone(saved);
        public void Save(TownDailyData data){TownDailyCodec.Validate(data);saved=TownDailyCodec.Clone(data);}
    }
    public sealed class FileTownDailyStore:ITownDailyStore
    {
        private readonly string directory;private readonly object gate=new object();
        public string Path=>System.IO.Path.Combine(directory,"social-journal-v1.json");
        public FileTownDailyStore(string directory){this.directory=directory??throw new ArgumentNullException(nameof(directory));}
        public void Save(TownDailyData data)
        {
            lock(gate)
            {
                TownDailyCodec.Validate(data);Directory.CreateDirectory(directory);string temp=Path+".tmp";
                try
                {
                    var bytes=TownDailyCodec.Encode(data);using(var s=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){s.Write(bytes,0,bytes.Length);s.Flush(true);}
                    TownDailyCodec.Validate(TownDailyCodec.Decode(File.ReadAllBytes(temp)));
                    if(File.Exists(Path))File.Replace(temp,Path,Read(Path)!=null?Path+".backup":null);else File.Move(temp,Path);
                }finally{if(File.Exists(temp))File.Delete(temp);}
            }
        }
        public TownDailyData Load(){lock(gate)return Read(Path)??Read(Path+".backup")??new TownDailyData();}
        private static TownDailyData Read(string path)
        {
            try{if(!File.Exists(path))return null;var d=TownDailyCodec.Decode(File.ReadAllBytes(path));TownDailyCodec.Validate(d);return d;}
            catch(IOException){return null;}catch(UnauthorizedAccessException){return null;}catch(SerializationException){return null;}catch(ArgumentException){return null;}
        }
    }
}
