using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using GrimDynasty.Resources;

public static class M3CatalogProbe {
 const string Input=@"D:\Steam\steamapps\common\Grim Dawn\database\database.arz";
 const string Report=@"C:\Users\zhuzi\Documents\Grim-Dynasty\docs\m3-evidence\content-candidates.json";
 const string SliceReport=@"C:\Users\zhuzi\Documents\Grim-Dynasty\docs\m3-evidence\m3-content-slice\content-slice.json";
 const string ExpectedHash="9e3532250cdb611a4cc989997c8f97fcec2d6c422063a845e6c43cd538c30603";
 const long ExpectedBytes=58529720;
 const int MaximumCandidates=8;
 static string Hash(Stream stream){
  stream.Position=0;var buffer=new byte[64*1024];
  using(var sha=SHA256.Create()){
   int count;while((count=stream.Read(buffer,0,buffer.Length))!=0)sha.TransformBlock(buffer,0,count,buffer,0);
   sha.TransformFinalBlock(new byte[0],0,0);return BitConverter.ToString(sha.Hash).Replace("-","").ToLowerInvariant();
  }
 }
 static void NoReparse(string path){
  for(string p=path;!String.IsNullOrEmpty(p);p=Path.GetDirectoryName(p))
   if(File.Exists(p)||Directory.Exists(p))if((File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Reparse path rejected: "+p);
 }
 static object Category(List<ArzEntry> entries,string filter){
  entries.Sort((a,b)=>StringComparer.Ordinal.Compare(a.name,b.name));var chosen=new List<object>();
  for(int i=0;i<Math.Min(MaximumCandidates,entries.Count);i++)chosen.Add(new{name=entries[i].name,@class=entries[i].@class});
  return new{filter=filter,order="record name, Ordinal ascending",total_matches=entries.Count,limit=MaximumCandidates,entries=chosen};
 }
 static void DecodeFirst(ArzArchive archive,List<ArzEntry> sorted,string category,List<object> decoded,ref int calls){
  if(sorted.Count==0)return;
  if(calls>=2)throw new InvalidDataException("Fixed two-record body budget exceeded");
  ArzEntry selected=sorted[0];
  if(selected.packed>8*1024*1024||selected.unpacked>8*1024*1024)throw new InvalidDataException("Selected record exceeds 8MiB payload bound");
  calls++;ArzRecord record=archive.ReadRecord(selected.name);
  var fields=new Dictionary<string,object>(record.fields,StringComparer.Ordinal);
  var bits=new Dictionary<string,object>(StringComparer.Ordinal);var nonfinite=new List<string>();
  foreach(var pair in record.fields){
   if(record.field_types[pair.Key]!=1)continue;
   object[] values=pair.Value as object[]??new object[]{pair.Value};var raw=new List<string>();var finite=new List<object>();bool special=false;
   foreach(object value in values){double n=(double)value;raw.Add("0x"+BitConverter.ToUInt32(BitConverter.GetBytes((float)n),0).ToString("X8"));
    if(Double.IsNaN(n)||Double.IsInfinity(n)){finite.Add(null);special=true;}else finite.Add(n);
   }
   bits.Add(pair.Key,pair.Value is object[]?(object)raw:raw[0]);
   if(special){nonfinite.Add(pair.Key);fields[pair.Key]=pair.Value is object[]?(object)finite:finite[0];}
  }
  decoded.Add(new{category=category,name=record.name,@class=record.@class,body_sha256=record.body_sha256,
   fields=fields,field_types=record.field_types,float_bits=bits,nonfinite_fields=nonfinite,
   float_view="binary32 bits are authoritative; finite numeric JSON is a convenience view, non-finite values are null with bits retained"});
 }
 public static int Main(string[] args){
  try{
   bool slice=args.Length==1&&args[0]=="-slice";
   if(args.Length!=0&&!slice)throw new ArgumentException("Only the fixed optional -slice plan is accepted");
   string reportPath=slice?SliceReport:Report;
   NoReparse(Input);NoReparse(reportPath);
   if(File.Exists(reportPath))throw new IOException("Evidence report already exists; refusing overwrite");
   if(!Directory.Exists(Path.GetDirectoryName(reportPath)))throw new DirectoryNotFoundException("Reviewed report directory must already exist");
   string before,after;object enemies,armor;int indexed,bodyCalls=0;var decoded=new List<object>();
   // Hold one read-only/share-read handle across both hashes and index parsing.
   // No original executable or DLL is loaded. The optional slice plan alone
   // calls ReadRecord, at most once for each of its two sorted candidate groups.
   using(var file=new FileStream(Input,FileMode.Open,FileAccess.Read,FileShare.Read,64*1024,FileOptions.SequentialScan)){
    if(file.Length!=ExpectedBytes)throw new InvalidDataException("Original ARZ byte length differs from approved input");
    before=Hash(file);if(before!=ExpectedHash)throw new InvalidDataException("Original ARZ fingerprint differs from approved input");
    using(var archive=new ArzArchive(new ResourceInput(file))){
     var enemyEntries=new List<ArzEntry>();var armorEntries=new List<ArzEntry>();indexed=archive.Entries.Count;
     foreach(var entry in archive.Entries.Values){
      if(String.Equals(entry.@class,"Monster",StringComparison.OrdinalIgnoreCase)&&(!slice||entry.name.IndexOf("zombie",StringComparison.OrdinalIgnoreCase)>=0))enemyEntries.Add(entry);
      if(slice?String.Equals(entry.@class,"ArmorProtective_Chest",StringComparison.OrdinalIgnoreCase)&&entry.name.StartsWith("records/items/",StringComparison.Ordinal):entry.@class.StartsWith("ArmorProtective",StringComparison.OrdinalIgnoreCase))armorEntries.Add(entry);
     }
     enemies=Category(enemyEntries,slice?"class equals Monster AND name contains zombie (both OrdinalIgnoreCase)":"class equals Monster (OrdinalIgnoreCase)");
     armor=Category(armorEntries,slice?"class equals ArmorProtective_Chest (OrdinalIgnoreCase) AND name starts records/items/ (Ordinal)":"class starts with ArmorProtective (OrdinalIgnoreCase)");
     if(slice){DecodeFirst(archive,enemyEntries,"enemy",decoded,ref bodyCalls);DecodeFirst(archive,armorEntries,"armor",decoded,ref bodyCalls);}
     after=Hash(file);if(after!=before)throw new InvalidDataException("Original ARZ changed during read-only evidence collection");
    }
   }
   object evidence;
   if(slice)evidence=new{schema="grim-dynasty-m3-content-slice-v1",status="Collected",utc=DateTime.UtcNow.ToString("o"),
    input=Input,input_bytes=ExpectedBytes,sha256_before=before,sha256_after=after,hash_buffer_bytes=64*1024,indexed_records=indexed,
    bodyreadcount=bodyCalls,bodyreadcount_definition="ArzArchive.ReadRecord calls, maximum 2; full-file hashing necessarily streams all bytes",
    enemy_candidates=enemies,armor_candidates=armor,decoded_records=decoded,
    scope="Two fixed catalog filters; decode only the first real name of each nonempty sorted group, no recursive references or texture reads. No original executable, game DLL, saves, or network.",
    interpretation="Names, classes and decoded typed fields are evidence only. Level-1 suitability, runtime scaling/formulas, hostility, equipment behaviour and native damage algorithms remain to be reviewed."};
   else evidence=new{schema="grim-dynasty-m3-content-candidates-v1",status="Collected",utc=DateTime.UtcNow.ToString("o"),
    input=Input,input_bytes=ExpectedBytes,sha256_before=before,sha256_after=after,hash_buffer_bytes=64*1024,
    indexed_records=indexed,bodyreadcount=0,bodyreadcount_definition="ArzArchive.ReadRecord calls; full-file hashing necessarily streams all bytes",
    enemy_candidates=enemies,armor_candidates=armor,
    scope="Index metadata only: no record payload decoding/export, textures, original executable, game DLL, saves, or network.",
    interpretation="Candidate labels use only the documented class filters. No level-1 suitability, hostile faction, HP, attack, armour value, or native combat algorithm is inferred."};
   string json=new JavaScriptSerializer{MaxJsonLength=1024*1024,RecursionLimit=20}.Serialize(evidence);
   byte[] bytes=new UTF8Encoding(false,true).GetBytes(json);
   NoReparse(reportPath);
   using(var report=new FileStream(reportPath,FileMode.CreateNew,FileAccess.Write,FileShare.None)){report.Write(bytes,0,bytes.Length);report.Flush(true);}
   Console.WriteLine("M3_CATALOG_COLLECTED index_records="+indexed+" bodyreadcount="+bodyCalls+" report="+reportPath);return 0;
  }catch(Exception e){Console.Error.WriteLine(e.GetType().Name+": "+e.Message);return 1;}
 }
}
