// Explicit-file, read-only adapters around the existing independent decoders.
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using GrimDynasty.Resources;
using GrimDynasty.Quests;

namespace GrimDynasty.Offline {
 public static class CodecCommands {
  const long MaxArchiveBytes=512L*1024*1024;
  const uint MaxTableBytes=8*1024*1024,MaxPayloadBytes=8*1024*1024;
  const uint MaxEntries=100000,MaxStrings=250000;

  public static CommandResult Run(string command,string[] args){
   if(command=="inspect-record"){
    if(args.Length!=2)throw new ArgumentException("inspect-record <explicit .arz path> <record-name>");
    RequireName(args[1]);return Record(command,PathFor(args[0],".arz"),args[1]);
   }
   if(command=="inspect-quest"){
    if(args.Length<1||args.Length>2)throw new ArgumentException("inspect-quest <explicit .qst path> OR <explicit .arc path> <member-name>");
    string extension=Path.GetExtension(args[0]).ToLowerInvariant();
    if(extension==".qst"&&args.Length==1)return QuestFile(command,PathFor(args[0],".qst"));
    if(extension==".arc"&&args.Length==2){RequireName(args[1]);return QuestArchive(command,PathFor(args[0],".arc"),args[1]);}
    throw new ArgumentException("inspect-quest requires a .qst file alone or an .arc file and member-name");
   }
   throw new ArgumentException("unknown codec command: "+command);
  }
  static void RequireName(string name){if(String.IsNullOrWhiteSpace(name)||name.IndexOf('\0')>=0)throw new ArgumentException("nonempty member/record name without NUL required");}
  internal static string PathFor(string path,string extension){
   if(String.IsNullOrWhiteSpace(path)||!String.Equals(Path.GetExtension(path),extension,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("explicit "+extension+" file required");
   string full=Path.GetFullPath(path);
   if(full.Length<3||!Char.IsLetter(full[0])||full[1]!=':'||(full[2]!='\\'&&full[2]!='/')||full.IndexOf(':',2)>=0)
    throw new ArgumentException("M1 requires a local drive path; UNC, device paths, and alternate streams are not accepted");
   foreach(string part in full.Substring(3).Split(new[]{'\\','/'})){
    string stem=part.TrimEnd(' ','.').Split('.')[0].TrimEnd(' ').ToUpperInvariant();
    bool numbered=stem.Length==4&&(stem.StartsWith("COM",StringComparison.Ordinal)||stem.StartsWith("LPT",StringComparison.Ordinal))&&("123456789\u00B9\u00B2\u00B3".IndexOf(stem[3])>=0);
    if(stem=="CON"||stem=="PRN"||stem=="AUX"||stem=="NUL"||stem=="CONIN$"||stem=="CONOUT$"||numbered)
     throw new ArgumentException("M1 does not accept DOS device names");
   }
   if(new DriveInfo(Path.GetPathRoot(full)).DriveType==DriveType.Network)throw new ArgumentException("M1 does not accept network drives");
   return full;
  }
  static ResourceInput Open(string path){return new ResourceInput(new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read));}
  static CommandResult Limit(string command,string subject){return CommandResult.Unsupported(command,"M1 limit: "+subject);}

  static CommandResult Record(string command,string path,string name){
   using(var input=Open(path)){
    if(input.Length>MaxArchiveBytes)return Limit(command,"archive exceeds 512 MiB");
    byte[] h=input.Read(0,24);
    if(BitConverter.ToUInt16(h,0)!=2)throw new InvalidDataException("invalid ARZ marker");
    if(BitConverter.ToUInt16(h,2)!=3)return CommandResult.Unsupported(command,"ARZ version must be 3");
    uint rs=BitConverter.ToUInt32(h,8),count=BitConverter.ToUInt32(h,12),st=BitConverter.ToUInt32(h,16),ss=BitConverter.ToUInt32(h,20);
    if(rs>MaxTableBytes||ss>MaxTableBytes||count>MaxEntries)return Limit(command,"ARZ tables exceed 8 MiB or 100000 records");
    if(ss<4)throw new InvalidDataException("ARZ string table lacks count");
    input.Bounds(st,ss);
    if(BitConverter.ToUInt32(input.Read(st,4),0)>MaxStrings)return Limit(command,"ARZ string count exceeds 250000");
    using(var archive=new ArzArchive(input)){
     ArzEntry entry;
     if(!archive.Entries.TryGetValue(ResourceUtil.Key(name),out entry))throw new ArgumentException("record not found: "+name);
     if(entry.packed>MaxPayloadBytes||entry.unpacked>MaxPayloadBytes)return Limit(command,"record payload exceeds 8 MiB");
     ArzRecord record;
     try{record=archive.ReadRecord(name);}
     catch(InvalidDataException ex){if(ex.Message=="unknown ARZ field type")return CommandResult.Unsupported(command,ex.Message);throw;}
     var bits=new Dictionary<string,object>(StringComparer.Ordinal);
     foreach(var field in record.fields){
      if(record.field_types[field.Key]!=1)continue;
      object[] values=field.Value as object[]??new object[]{field.Value};var hex=new List<string>();
      foreach(object value in values){
       double number=(double)value;
       if(Double.IsNaN(number)||Double.IsInfinity(number))return CommandResult.Unsupported(command,"non-finite ARZ float cannot be represented by the M1 numeric JSON view",new{field=field.Key});
       hex.Add("0x"+BitConverter.ToUInt32(BitConverter.GetBytes((float)number),0).ToString("X8"));
      }
      bits.Add(field.Key,field.Value is object[]?(object)hex:hex[0]);
     }
     return CommandResult.Known(command,new{input=path,record=record,float_bits=bits,view="decoded fields; no object construction"});
    }
   }
  }

  static CommandResult QuestFile(string command,string path){
   using(var input=Open(path)){
    if(input.Length>MaxPayloadBytes)return Limit(command,"QST payload exceeds 8 MiB");
    return DecodeQuest(command,path,null,input.Read(0,input.Length));
   }
  }
  static CommandResult QuestArchive(string command,string path,string name){
   if(!String.Equals(Path.GetExtension(name),".qst",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("quest archive member must have .qst extension");
   using(var input=Open(path)){
    if(input.Length>MaxArchiveBytes)return Limit(command,"archive exceeds 512 MiB");
    byte[] h=input.Read(0,28);
    if(BitConverter.ToUInt32(h,0)!=0x435241)throw new InvalidDataException("invalid ARC marker");
    if(BitConverter.ToUInt32(h,4)!=3)return CommandResult.Unsupported(command,"ARC version must be 3");
    uint count=BitConverter.ToUInt32(h,8),pc=BitConverter.ToUInt32(h,12),ps=BitConverter.ToUInt32(h,16),ns=BitConverter.ToUInt32(h,20);
    if((long)pc*12!=ps)throw new InvalidDataException("ARC part table size mismatch");
    if(count>MaxEntries||ps>MaxTableBytes||ns>MaxTableBytes)return Limit(command,"ARC tables exceed 8 MiB or 100000 entries");
    using(var archive=new ArcArchive(input)){
     ArcEntry entry;
     if(!archive.Entries.TryGetValue(ResourceUtil.Key(name),out entry))throw new ArgumentException("member not found: "+name);
     if(entry.type!=1&&entry.type!=3)return CommandResult.Unsupported(command,"unknown ARC entry type: "+entry.type);
     if(entry.packed>MaxPayloadBytes||entry.unpacked>MaxPayloadBytes)return Limit(command,"quest member exceeds 8 MiB");
     if(entry.parts>4096)return Limit(command,"ARC member exceeds 4096 blocks");
     // Reject oversized individual block reads before the decoder allocates them.
     for(uint i=0;i<entry.parts;i++){
      byte[] part=input.Read((long)BitConverter.ToUInt32(h,24)+((long)entry.first+i)*12,12);
      if(BitConverter.ToUInt32(part,4)>MaxPayloadBytes||BitConverter.ToUInt32(part,8)>MaxPayloadBytes)return Limit(command,"ARC member block exceeds 8 MiB");
     }
     return DecodeQuest(command,path,entry.name,archive.ReadMember(name));
    }
   }
  }
  static CommandResult DecodeQuest(string command,string path,string member,byte[] payload){
   if(payload.Length<16)throw new InvalidDataException("QST truncated header");
   if(payload[0]!='Q'||payload[1]!='S'||payload[2]!='T'||payload[3]!='2')throw new InvalidDataException("invalid QST marker");
   if(BitConverter.ToUInt32(payload,4)!=9)return CommandResult.Unsupported(command,"QST2 version must be 9");
   Dictionary<string,object> quest;
   try{quest=QuestDefinition.Read(payload);}
   catch(InvalidDataException ex){
    if(ex.Message.StartsWith("unknown QST ",StringComparison.Ordinal)||ex.Message=="unknown Lua argument type")return CommandResult.Unsupported(command,ex.Message);
    throw;
   }
   if(!Finite(quest))return CommandResult.Unsupported(command,"non-finite QST number cannot be represented by the M1 numeric JSON view");
   return CommandResult.Known(command,new{input=path,member=member,quest=quest,view="definition only; conditions/actions and Lua are not executed"});
  }
  static bool Finite(object value){
   if(value is double){double x=(double)value;return !Double.IsNaN(x)&&!Double.IsInfinity(x);}
   var dictionary=value as IDictionary;
   if(dictionary!=null){foreach(DictionaryEntry pair in dictionary)if(!Finite(pair.Value))return false;return true;}
   var sequence=value as IEnumerable;
   if(sequence!=null&&!(value is string))foreach(object item in sequence)if(!Finite(item))return false;
   return true;
  }
 }
}
