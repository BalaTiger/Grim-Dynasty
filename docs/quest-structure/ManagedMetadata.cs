using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text.Json;

public static class ManagedMetadata {
 static MetadataReader md;
 static string S(StringHandle h)=>md.GetString(h);
 static string Name(EntityHandle h) {
  try {
   switch(h.Kind) {
    case HandleKind.TypeDefinition:var t=md.GetTypeDefinition((TypeDefinitionHandle)h);return S(t.Namespace)+"."+S(t.Name);
    case HandleKind.TypeReference:var tr=md.GetTypeReference((TypeReferenceHandle)h);return S(tr.Namespace)+"."+S(tr.Name);
    case HandleKind.MethodDefinition:var m=md.GetMethodDefinition((MethodDefinitionHandle)h);return Name(m.GetDeclaringType())+"::"+S(m.Name);
    case HandleKind.MemberReference:var mr=md.GetMemberReference((MemberReferenceHandle)h);return Name(mr.Parent)+"::"+S(mr.Name);
    case HandleKind.FieldDefinition:var f=md.GetFieldDefinition((FieldDefinitionHandle)h);return Name(f.GetDeclaringType())+"::"+S(f.Name);
    default:return h.Kind+":"+MetadataTokens.GetToken(h).ToString("x8");
   }
  }catch{return "unresolved:"+MetadataTokens.GetToken(h).ToString("x8");}
 }
 static Dictionary<ushort,OpCode> ops=typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static).Where(f=>f.FieldType==typeof(OpCode)).Select(f=>(OpCode)f.GetValue(null)).ToDictionary(o=>unchecked((ushort)o.Value));
 static object[] IL(byte[] b) {
  var rows=new List<object>();int p=0;
  while(p<b.Length){int off=p;ushort code=b[p++];if(code==0xfe)code=(ushort)(0xfe00|b[p++]);var op=ops[code];object arg=null;int n=0;
   switch(op.OperandType){
    case OperandType.InlineNone:break;
    case OperandType.ShortInlineI:arg=(sbyte)b[p];n=1;break;
    case OperandType.ShortInlineVar:arg=b[p];n=1;break;
    case OperandType.InlineVar:arg=BitConverter.ToUInt16(b,p);n=2;break;
    case OperandType.InlineI:arg=BitConverter.ToInt32(b,p);n=4;break;
    case OperandType.InlineI8:arg=BitConverter.ToInt64(b,p);n=8;break;
    case OperandType.ShortInlineR:arg=BitConverter.ToSingle(b,p);n=4;break;
    case OperandType.InlineR:arg=BitConverter.ToDouble(b,p);n=8;break;
    case OperandType.ShortInlineBrTarget:arg="IL_"+(p+1+(sbyte)b[p]).ToString("x4");n=1;break;
    case OperandType.InlineBrTarget:arg="IL_"+(p+4+BitConverter.ToInt32(b,p)).ToString("x4");n=4;break;
    case OperandType.InlineSwitch:int cnt=BitConverter.ToInt32(b,p);n=4+cnt*4;var targets=new string[cnt];for(int i=0;i<cnt;i++)targets[i]="IL_"+(p+n+BitConverter.ToInt32(b,p+4+i*4)).ToString("x4");arg=targets;break;
    case OperandType.InlineString:arg=md.GetUserString(MetadataTokens.UserStringHandle(BitConverter.ToInt32(b,p)&0xffffff));n=4;break;
    default:int token=BitConverter.ToInt32(b,p);arg=new {token=token.ToString("x8"),name=Name(MetadataTokens.EntityHandle(token))};n=4;break;
   }p+=n;rows.Add(new {offset=off,opcode=op.Name,operand=arg});
  }return rows.ToArray();
 }
 public static string Dump(string path,string pattern,string resourceOut) {
  using(var stream=File.OpenRead(path))using(var pe=new PEReader(stream)){
   md=pe.GetMetadataReader();var types=new List<object>();
   foreach(var h in md.TypeDefinitions){var td=md.GetTypeDefinition(h);string name=Name(h);if(!System.Text.RegularExpressions.Regex.IsMatch(name,pattern))continue;
    var methods=new List<object>();foreach(var mh in td.GetMethods()){var m=md.GetMethodDefinition(mh);object body=null;if(m.RelativeVirtualAddress!=0){var mb=pe.GetMethodBody(m.RelativeVirtualAddress);var bytes=mb.GetILBytes();body=new {bytes=bytes.Length,max_stack=mb.MaxStack,instructions=IL(bytes),exception_regions=mb.ExceptionRegions.Select(e=>new{kind=e.Kind.ToString(),try_offset=e.TryOffset,try_length=e.TryLength,handler_offset=e.HandlerOffset,handler_length=e.HandlerLength}).ToArray()};}methods.Add(new {name=S(m.Name),token=MetadataTokens.GetToken(mh).ToString("x8"),rva=m.RelativeVirtualAddress,signature=Convert.ToHexString(md.GetBlobBytes(m.Signature)),body});}
    var fields=td.GetFields().Select(fh=>{var f=md.GetFieldDefinition(fh);var c=f.GetDefaultValue();return new{name=S(f.Name),signature=Convert.ToHexString(md.GetBlobBytes(f.Signature)),constant=c.IsNil?null:Convert.ToHexString(md.GetBlobBytes(md.GetConstant(c).Value))};}).ToArray();
    types.Add(new {name,token=MetadataTokens.GetToken(h).ToString("x8"),base_type=td.BaseType.IsNil?null:Name(td.BaseType),fields,methods});
   }
   var resources=new List<object>();foreach(var rh in md.ManifestResources){var r=md.GetManifestResource(rh);var name=S(r.Name);string extracted=null;
    if(resourceOut!=null&&r.Implementation.IsNil&&(name=="costura.utility.dll.zip"||name=="costura.common.dll.zip")){
     var blob=pe.GetSectionData(pe.PEHeaders.CorHeader.ResourcesDirectory.RelativeVirtualAddress).GetContent();int offset=checked((int)r.Offset);var all=blob.ToArray();int size=BitConverter.ToInt32(all,offset);
     Directory.CreateDirectory(resourceOut);extracted=Path.Combine(resourceOut,name=="costura.common.dll.zip"?"Common.dll":"Utility.dll");using(var input=new MemoryStream(all,offset+4,size,false))using(var zip=new DeflateStream(input,CompressionMode.Decompress))using(var output=new FileStream(extracted,FileMode.CreateNew))zip.CopyTo(output);
    }resources.Add(new{name,offset=r.Offset,embedded=r.Implementation.IsNil,extracted});
   }
   return JsonSerializer.Serialize(new{path,operation="PE metadata and full IL bodies only; target never loaded or executed",types,resources},new JsonSerializerOptions{WriteIndented=true});
  }
 }
}

