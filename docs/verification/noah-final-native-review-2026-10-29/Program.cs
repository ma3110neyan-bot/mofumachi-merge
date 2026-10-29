using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using var file=File.OpenRead(args[0]);using var pe=new PEReader(file);var md=pe.GetMetadataReader();
var type=md.TypeDefinitions.Select(h=>md.GetTypeDefinition(h)).Single(t=>md.GetString(t.Namespace)=="Mofumachi.Presentation"&&md.GetString(t.Name)=="TownActor");
var method=type.GetMethods().Select(h=>md.GetMethodDefinition(h)).Single(m=>md.GetString(m.Name)=="Bind");
var codes=typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static).Where(f=>f.FieldType==typeof(OpCode)).Select(f=>(OpCode)f.GetValue(null)!).ToDictionary(c=>(ushort)c.Value);
var il=pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!;int prefix=0,logs=0;
for(int i=0;i<il.Length;){
 ushort n=il[i++];if(n==0xfe)n=(ushort)(0xfe00|il[i++]);var op=codes[n];
 if(op.OperandType==OperandType.InlineString&&md.GetUserString(MetadataTokens.UserStringHandle(BitConverter.ToInt32(il,i)&0x00ffffff)).StartsWith("MOFUMACHI_NOAH_TRIAL_MODE "))prefix++;
 if(op.OperandType==OperandType.InlineMethod){var handle=MetadataTokens.EntityHandle(BitConverter.ToInt32(il,i));if(handle.Kind==HandleKind.MemberReference){var m=md.GetMemberReference((MemberReferenceHandle)handle);if(m.Parent.Kind==HandleKind.TypeReference){var t=md.GetTypeReference((TypeReferenceHandle)m.Parent);if(md.GetString(t.Name)=="Debug"&&md.GetString(t.Namespace)=="UnityEngine"&&md.GetString(m.Name)=="Log")logs++;}}}
 i+=op.OperandType switch{OperandType.InlineNone=>0,OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar=>1,OperandType.InlineVar=>2,OperandType.InlineI8 or OperandType.InlineR=>8,OperandType.InlineSwitch=>4+4*BitConverter.ToInt32(il,i),_=>4};
}
int expected=args[1]=="android"?1:0;
if(prefix!=expected||logs!=expected)throw new Exception($"{args[1]}: Bind emitted device-only mode log ({prefix} prefixes/{logs} Debug.Log sites), expected {expected}.");
Console.WriteLine($"PASS {args[1]}: actual compiled TownActor.Bind has {prefix} mode prefix/{logs} Debug.Log call sites. Static compilation proof only; not engine/device execution.");
