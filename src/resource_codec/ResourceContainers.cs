// Independent, read-only ARZ v3 / ARC v3 implementation; no original DLL use.
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace GrimDynasty.Resources {
 public sealed class ResourceInput : IDisposable {
  readonly Stream stream;
  public long Length {get{return stream.Length;}}
  public ResourceInput(Stream input){if(!input.CanRead||!input.CanSeek)throw new ArgumentException("seekable readable stream required");stream=input;}
  public static ResourceInput OpenGameResource(string path){
   string p=Path.GetFullPath(path),root=Path.GetFullPath(@"D:\Steam\steamapps\common\Grim Dawn")+Path.DirectorySeparatorChar;
   string ext=Path.GetExtension(p).ToLowerInvariant();
   if(!p.StartsWith(root,StringComparison.OrdinalIgnoreCase)||(ext!=".arz"&&ext!=".arc"))throw new ArgumentException("only authorized installation .arz/.arc inputs");
   return new ResourceInput(new FileStream(p,FileMode.Open,FileAccess.Read,FileShare.Read));
  }
  public void Bounds(long offset,long size){if(offset<0||size<0||offset>Length||size>Length-offset)throw new InvalidDataException("resource extent outside file");}
  public byte[] Read(long offset,long count){Bounds(offset,count);if(count>Lz4Block.MaxOutput)throw new InvalidDataException("single read limit");var b=new byte[checked((int)count)];stream.Position=offset;int p=0;while(p<b.Length){int n=stream.Read(b,p,b.Length-p);if(n==0)throw new EndOfStreamException();p+=n;}return b;}
  public void Dispose(){stream.Dispose();}
 }
 internal sealed class Wire {
  readonly byte[] data;public int Position;public int Length{get{return data.Length;}}
  public Wire(byte[] b){data=b;}
  public byte[] Bytes(int n){if(n<0||n>data.Length-Position)throw new InvalidDataException("truncated table");var b=new byte[n];Buffer.BlockCopy(data,Position,b,0,n);Position+=n;return b;}
  public ushort U16(){return BitConverter.ToUInt16(Bytes(2),0);}
  public uint U32(){return BitConverter.ToUInt32(Bytes(4),0);}
  public ulong U64(){return BitConverter.ToUInt64(Bytes(8),0);}
  public int I32(){return BitConverter.ToInt32(Bytes(4),0);}
 }
 public static class ResourceUtil {
  public static readonly Encoding Utf8=new UTF8Encoding(false,true);
  public static string Key(string s){return s.Replace('\\','/').TrimEnd('\0').ToLowerInvariant();}
  public static string Ascii(byte[] b){foreach(byte x in b)if(x>127)throw new InvalidDataException("non-ASCII identifier");return Encoding.ASCII.GetString(b);}
  public static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","").ToLowerInvariant();}
  public static uint Adler(byte[] b){uint a=1,c=0;foreach(byte x in b){a=(a+x)%65521;c=(c+a)%65521;}return (c<<16)|a;}
 }
 public sealed class ArzEntry {
  public string name;public string @class;public long offset;public uint packed,unpacked;public uint[] timestamp_words;
 }
 public sealed class ArzRecord {
  public string name;public string @class;public string body_sha256;
  public Dictionary<string,object> fields=new Dictionary<string,object>(StringComparer.Ordinal);
  public Dictionary<string,ushort> field_types=new Dictionary<string,ushort>(StringComparer.Ordinal);
  // Typed digest is derived from decoded values, sorted by ordinal field name.
  public string TypedDigest(){
   using(var memory=new MemoryStream())using(var w=new BinaryWriter(memory,ResourceUtil.Utf8)){
    var keys=new List<string>(fields.Keys);keys.Sort(StringComparer.Ordinal);
    foreach(string k in keys){byte[] nameBytes=ResourceUtil.Utf8.GetBytes(k);w.Write(nameBytes.Length);w.Write(nameBytes);ushort t=field_types[k];w.Write(t);object v=fields[k];object[] values=v as object[]??new object[]{v};w.Write(values.Length);
     foreach(object x in values){if(t==2){byte[] b=ResourceUtil.Utf8.GetBytes((string)x);w.Write(b.Length);w.Write(b);}else if(t==1)w.Write(Convert.ToSingle(x));else w.Write((int)x);}
    }w.Flush();return ResourceUtil.Hash(memory.ToArray());
   }
  }
 }
 public sealed class ArzArchive : IDisposable {
  readonly ResourceInput input;readonly List<string> strings=new List<string>();
  public readonly Dictionary<string,ArzEntry> Entries=new Dictionary<string,ArzEntry>(StringComparer.Ordinal);
  public readonly Dictionary<string,object> Header=new Dictionary<string,object>();
  public ArzArchive(ResourceInput source){input=source;var h=new Wire(input.Read(0,24));ushort marker=h.U16(),version=h.U16();uint rt=h.U32(),rs=h.U32(),count=h.U32(),st=h.U32(),ss=h.U32();
   if(marker!=2||version!=3)throw new InvalidDataException("unsupported ARZ marker/version");if(count>1000000)throw new InvalidDataException("ARZ record limit");
   Header.Add("marker",marker);Header.Add("version",version);Header.Add("record_table_offset",rt);Header.Add("record_table_size",rs);Header.Add("records",count);Header.Add("string_table_offset",st);Header.Add("string_table_size",ss);
   var table=new Wire(input.Read(st,ss));uint n=table.U32();if(n>2000000)throw new InvalidDataException("ARZ string count limit");
   for(uint i=0;i<n;i++)strings.Add(ResourceUtil.Utf8.GetString(table.Bytes(checked((int)table.U32()))));if(table.Position!=table.Length)throw new InvalidDataException("ARZ string table length");
   table=new Wire(input.Read(rt,rs));for(uint i=0;i<count;i++){
    uint idx=table.U32(),ln=table.U32();string kind=ResourceUtil.Ascii(table.Bytes(checked((int)ln)));var e=new ArzEntry{name=StringAt(idx),@class=kind,offset=(long)table.U32()+24,packed=table.U32(),unpacked=table.U32(),timestamp_words=new uint[]{table.U32(),table.U32()}};
    if(e.unpacked>Lz4Block.MaxOutput)throw new InvalidDataException("ARZ record output limit");input.Bounds(e.offset,e.packed);if(e.packed>rt-e.offset)throw new InvalidDataException("ARZ record overlaps tables");Entries.Add(ResourceUtil.Key(e.name),e);
   }if(table.Position!=table.Length)throw new InvalidDataException("ARZ record table length");
  }
  string StringAt(uint index){if(index>=strings.Count)throw new InvalidDataException("ARZ string index");return strings[(int)index];}
  public ArzRecord ReadRecord(string name){var e=Entries[ResourceUtil.Key(name)];byte[] body=Lz4Block.Decode(input.Read(e.offset,e.packed),checked((int)e.unpacked));var r=new ArzRecord{name=e.name,@class=e.@class,body_sha256=ResourceUtil.Hash(body)};var b=new Wire(body);
   while(b.Position<b.Length){ushort type=b.U16(),count=b.U16();string field=StringAt(b.U32());if(type>3)throw new InvalidDataException("unknown ARZ field type");object[] values=new object[count];
    for(int i=0;i<count;i++){if(type==2)values[i]=StringAt(b.U32());else if(type==1)values[i]=(double)BitConverter.ToSingle(b.Bytes(4),0);else values[i]=b.I32();}
    r.fields.Add(field,count==1?values[0]:values);r.field_types.Add(field,type);
   }return r;
  }
  public void Dispose(){input.Dispose();}
 }
 public sealed class ArcEntry {
  public string name;public uint type,offset,packed,unpacked,checksum,parts,first;public ulong filetime;
 }
 public sealed class ArcArchive : IDisposable {
  readonly ResourceInput input;readonly List<uint[]> parts=new List<uint[]>();
  public readonly Dictionary<string,ArcEntry> Entries=new Dictionary<string,ArcEntry>(StringComparer.Ordinal);
  public readonly Dictionary<string,object> Header=new Dictionary<string,object>();
  public ArcArchive(ResourceInput source){input=source;var h=new Wire(input.Read(0,28));uint magic=h.U32(),version=h.U32(),count=h.U32(),pc=h.U32(),ps=h.U32(),ns=h.U32(),po=h.U32();
   if(magic!=0x435241||version!=3)throw new InvalidDataException("unsupported ARC marker/version");if(count>1000000||pc>4000000||ps!=(long)pc*12)throw new InvalidDataException("ARC count/part size limit");
   long namesOffset=(long)po+ps,recordsOffset=namesOffset+ns;Header.Add("magic","0x435241");Header.Add("version",version);Header.Add("entries",count);Header.Add("parts",pc);Header.Add("part_table_offset",po);Header.Add("part_table_size",ps);Header.Add("name_table_size",ns);Header.Add("entry_table_offset",recordsOffset);
   var p=new Wire(input.Read(po,ps));for(uint i=0;i<pc;i++)parts.Add(new uint[]{p.U32(),p.U32(),p.U32()});byte[] names=input.Read(namesOffset,ns);var records=new Wire(input.Read(recordsOffset,(long)count*44));
   for(uint i=0;i<count;i++){var e=new ArcEntry{type=records.U32(),offset=records.U32(),packed=records.U32(),unpacked=records.U32(),checksum=records.U32(),filetime=records.U64(),parts=records.U32(),first=records.U32()};uint length=records.U32(),offset=records.U32();if(offset>names.Length||length>names.Length-offset||(long)e.first+e.parts>pc||e.unpacked>Lz4Block.MaxOutput)throw new InvalidDataException("ARC entry bounds");var name=new byte[checked((int)length)];Buffer.BlockCopy(names,(int)offset,name,0,name.Length);e.name=ResourceUtil.Ascii(name).TrimEnd('\0');Entries.Add(ResourceUtil.Key(e.name),e);}
  }
  public byte[] ReadMember(string name){var e=Entries[ResourceUtil.Key(name)];byte[] body;
   if(e.type==1&&e.packed==e.unpacked)body=input.Read(e.offset,e.packed);
   else {using(var output=new MemoryStream()){long packed=0,unpacked=0;for(uint i=0;i<e.parts;i++){uint[] p=parts[checked((int)(e.first+i))];unpacked+=p[2];packed+=p[1];if(unpacked>e.unpacked)throw new InvalidDataException("ARC output aggregate limit");byte[] raw=input.Read(p[0],p[1]);byte[] block=p[1]==p[2]?raw:Lz4Block.Decode(raw,checked((int)p[2]));output.Write(block,0,block.Length);}if(packed!=e.packed)throw new InvalidDataException("ARC packed sum");body=output.ToArray();}}
   if(body.Length!=e.unpacked||ResourceUtil.Adler(body)!=e.checksum)throw new InvalidDataException("ARC decoded size/checksum");return body;
  }
  public void Dispose(){input.Dispose();}
 }
}
