// Fixed official record body only. This program is prepared separately from
// execution; it never loads game code or reconstructs a trimmed string pool.
// Build against the independently implemented parser with the documented
// BooleanCount / CopyBooleanWords API, after its evidence is accepted.
using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using GrimDynasty.Resources;
using GrimDynasty.TaskItems;

class ProbeOfficialParsedBody {
 const string Root=@"C:\Users\zhuzi\Documents\Grim-Dynasty";
 const string Archive=@"D:\Steam\steamapps\common\Grim Dawn\database\database.arz";
 const string RecordName="records/items/questitems/scrapmetal.dbr";
 const string PriorRelative=@"docs\object-lifecycle\official-record-probe-retry1\probe.json";
 const string OutputRelative=@"docs\object-lifecycle\official-parsed-body-validation";
 const string PriorHash="7243603302710e44a3363d6259857e8978d14508f9ec60ead444d0cb33cf25c0";
 const string ArchiveHash="9e3532250cdb611a4cc989997c8f97fcec2d6c422063a845e6c43cd538c30603";
 const string PackedHash="d532c7583a8fa02523e67e2b69efec1b0b48bd1652294fa2f76648af03906e58";
 const string BodyHash="5cbd10f21b424b0bec879fe101e77772fe42965ac363a5503c46907a62d91c7f";
 const long ArchiveBytes=58529720,PayloadOffset=42034543;
 const int PackedBytes=323,BodyBytes=456,FrozenFieldCount=38;
 static readonly Encoding Utf8=new UTF8Encoding(false,true);

 sealed class FrozenField {
  public int Ordinal,Offset,Tag,Count;
  public uint Key;
  public uint[] Words;
 }
 static Dictionary<string,object> D(params object[] items){
  var d=new Dictionary<string,object>();
  for(int i=0;i<items.Length;i+=2)d.Add((string)items[i],items[i+1]);
  return d;
 }
 static void Need(bool condition,string reason){
  if(!condition)throw new InvalidDataException(reason);
 }
 static string Hex(byte[] bytes){return BitConverter.ToString(bytes).Replace("-","").ToLowerInvariant();}
 static string Hash(byte[] bytes){using(var h=SHA256.Create())return Hex(h.ComputeHash(bytes));}
 static uint U32(byte[] bytes,int p){
  return (uint)bytes[p]|((uint)bytes[p+1]<<8)|((uint)bytes[p+2]<<16)|((uint)bytes[p+3]<<24);
 }
 static bool Same(byte[] a,byte[] b){
  if(a.Length!=b.Length)return false;
  for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;
  return true;
 }
 static Dictionary<string,object> Obj(object value){
  var result=value as Dictionary<string,object>;
  Need(result!=null,"FrozenProbeObjectExpected");return result;
 }
 static object[] Arr(object value){
  var result=value as object[];
  Need(result!=null,"FrozenProbeArrayExpected");return result;
 }
 static int Int(object value){return Convert.ToInt32(value,CultureInfo.InvariantCulture);}
 static long Long(object value){return Convert.ToInt64(value,CultureInfo.InvariantCulture);}
 static uint UInt(object value){return Convert.ToUInt32(value,CultureInfo.InvariantCulture);}
 static string Text(object value){return Convert.ToString(value,CultureInfo.InvariantCulture);}
 static string HashStream(FileStream stream){
  stream.Position=0;
  using(var h=SHA256.Create()){
   byte[] buffer=new byte[65536];int n;
   while((n=stream.Read(buffer,0,buffer.Length))!=0)h.TransformBlock(buffer,0,n,null,0);
   h.TransformFinalBlock(new byte[0],0,0);return Hex(h.Hash);
  }
 }
 static void Save(string path,object report){
  var serializer=new JavaScriptSerializer();serializer.MaxJsonLength=262144;
  byte[] bytes=Utf8.GetBytes(serializer.Serialize(report)+Environment.NewLine);
  using(var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None))
   file.Write(bytes,0,bytes.Length);
 }
 static string ValidateOutput(string argument){
  string root=Path.GetFullPath(Root),expected=Path.GetFullPath(Path.Combine(root,OutputRelative));
  string output=Path.GetFullPath(argument);
  Need(String.Equals(output,expected,StringComparison.OrdinalIgnoreCase),"ExactProjectEvidenceDirectoryRequired");
  Need(output.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"ProjectOutputRequired");
  // A matching textual path must not resolve through an existing junction
  // outside the project. No links or directories are changed by this check.
  for(string p=output;p!=null&&p.Length>=root.Length;p=Path.GetDirectoryName(p))
   if(Directory.Exists(p))
    Need((File.GetAttributes(p)&FileAttributes.ReparsePoint)==0,"EvidenceDirectoryReparsePointUnsupported");
  Directory.CreateDirectory(output);
  Need(!File.Exists(Path.Combine(output,"probe.json")),"PreserveExistingParsedBodyProbe");
  return output;
 }
 static FrozenField[] ReadFrozenProbe(Dictionary<string,object> report){
  string path=Path.Combine(Root,PriorRelative);
  var info=new FileInfo(path);Need(info.Length>0&&info.Length<=65536,"FrozenProbeFileBudget");
  byte[] bytes=File.ReadAllBytes(path);
  Need(Hash(bytes)==PriorHash,"FrozenProbeFingerprintChanged");
  var serializer=new JavaScriptSerializer();serializer.MaxJsonLength=65536;
  var prior=Obj(serializer.DeserializeObject(Utf8.GetString(bytes)));
  Need(Text(prior["archive"])==Archive&&Text(prior["selected_record"])==RecordName,"FrozenProbeTargetChanged");
  Need(Long(prior["archive_bytes"])==ArchiveBytes&&Text(prior["sha256_before"])==ArchiveHash&&
   Text(prior["sha256_after"])==ArchiveHash,"FrozenProbeArchiveIdentityMismatch");
  var raw=Obj(prior["raw_reader"]);var decoder=Obj(prior["decoder_comparison"]);
  Need(Long(raw["data_offset"])==PayloadOffset&&Int(raw["packed_bytes"])==PackedBytes&&
   Int(raw["body_bytes"])==BodyBytes&&Text(raw["body_sha256"])==BodyHash,"FrozenProbeExtentMismatch");
  Need(Text(decoder["packed_sha256"])==PackedHash,"FrozenProbePackedFingerprintMismatch");
  var wire=Obj(prior["wire_observer"]);
  Need(Text(wire["status"])=="complete-structural-observation"&&Int(wire["field_count"])==FrozenFieldCount&&
   Int(wire["total_words"])==38&&Int(wire["cursor"])==BodyBytes,"FrozenWireSummaryMismatch");
  object[] fields=Arr(wire["fields"]);Need(fields.Length==FrozenFieldCount,"FrozenFieldCountMismatch");
  var result=new FrozenField[fields.Length];int cursor=0,total=0;var counts=new int[4];
  for(int i=0;i<fields.Length;i++){
   var f=Obj(fields[i]);var values=Arr(f["raw_uint32_words"]);
   var row=new FrozenField();
   row.Ordinal=Int(f["ordinal"]);row.Offset=Int(f["body_offset"]);row.Tag=Int(f["tag"]);
   row.Count=Int(f["element_count"]);row.Key=UInt(f["key_original_id"]);row.Words=new uint[values.Length];
   Need(row.Ordinal==i&&row.Offset==cursor&&row.Tag>=0&&row.Tag<=3&&row.Count==values.Length&&
    row.Count>0&&row.Count<=(BodyBytes-cursor-8)/4,"FrozenFieldLayoutMismatch");
   for(int j=0;j<values.Length;j++)row.Words[j]=UInt(values[j]);
   counts[row.Tag]++;total+=row.Count;cursor+=8+row.Count*4;result[i]=row;
  }
  Need(cursor==BodyBytes&&total==38&&counts[0]==8&&counts[1]==11&&counts[2]==12&&counts[3]==7,
   "FrozenFieldTotalsMismatch");
  report["prior_probe"]=D("path",PriorRelative.Replace('\\','/'),"sha256",PriorHash,"bytes",bytes.Length,
   "field_count",result.Length,"total_words",total,"tag_field_counts_0_to_3",counts);
  // Prior string annotations document Class key 13 -> value ID 28355
  // (QuestItem). They do not supply a pool or execute a getter/factory.
  report["class_annotation_only"]=D("key_original_id",13,"value_original_id",28355,
   "key_annotation","Class","value_annotation","QuestItem","source","frozen prior probe",
   "getter_invoked",false,"factory_invoked",false);
  return result;
 }
 static byte[] ReadPacked(FileStream file,List<object> reads){
  file.Position=PayloadOffset;
  byte[] result=new byte[PackedBytes];int at=0;
  while(at<result.Length){
   int requested=result.Length-at;long offset=file.Position;
   int n=file.Read(result,at,requested);
   reads.Add(D("scope","fixed-selected-packed-record","offset",offset,"requested_bytes",requested,"read_bytes",n));
   Need(n>0,"SelectedPackedExtentTruncated");at+=n;
  }
  return result;
 }
 static void CompareWire(byte[] body,FrozenField[] expected){
  Need(body.Length==BodyBytes,"BodySizeMismatch");
  foreach(var f in expected){
   uint header=U32(body,f.Offset);
   Need((header&65535U)==(uint)f.Tag&&(header>>16)==(uint)f.Count&&
    U32(body,f.Offset+4)==f.Key,"BodyFieldHeaderDiffersFromFrozenProbe");
   for(int j=0;j<f.Count;j++)
    Need(U32(body,f.Offset+8+j*4)==f.Words[j],"BodyWordBitsDifferFromFrozenProbe");
  }
 }
 static Dictionary<string,object> CompareParsed(BinaryRecordParseResult parsed,FrozenField[] expected){
  if(parsed.Status!="Known")
   return D("status","not-comparable-parser-unsupported","reason",parsed.Reason,"matches",false);
  Need(parsed.Record!=null,"KnownParserMissingRecord");
  var descriptors=parsed.Record.CopyDescriptors();
  var actualRaw=new uint[3][];var next=new int[4];
  for(int tag=0;tag<3;tag++)actualRaw[tag]=parsed.Record.CopyRawValues(tag);
  uint booleanCount=parsed.Record.BooleanCount;
  uint[] booleanWords=parsed.Record.CopyBooleanWords();
  int expectedBits=0;
  foreach(var f in expected)if(f.Tag==3)expectedBits+=f.Count;
  var expectedBooleanWords=new uint[(expectedBits+31)/32];int bit=0;
  foreach(var f in expected)if(f.Tag==3)
   for(int j=0;j<f.Count;j++,bit++)
    if(f.Words[j]!=0)expectedBooleanWords[bit/32]|=1U<<(bit%32);
  bool equal=descriptors.Length==expected.Length&&booleanCount==(uint)expectedBits&&
   booleanWords.Length==expectedBooleanWords.Length;
  var fieldResults=new List<object>();
  for(int i=0;i<expected.Length;i++){
   FrozenField f=expected[i];int start=next[f.Tag];
   bool match=i<descriptors.Length;
   if(match){
    var actual=descriptors[i];
    match=actual.Key==f.Key&&actual.TypeTag==f.Tag&&actual.ElementCount==(uint)f.Count&&
     actual.BufferIndex==(uint)start;
   }
   for(int j=0;j<f.Count;j++){
    int index=start+j;
    if(f.Tag<3)match&=index<actualRaw[f.Tag].Length&&actualRaw[f.Tag][index]==f.Words[j];
    else{
     bool readable=index/32<booleanWords.Length&&(uint)index<booleanCount;
     match&=readable&&((booleanWords[index/32]&(1U<<(index%32)))!=0)==(f.Words[j]!=0);
    }
   }
   next[f.Tag]+=f.Count;equal&=match;
   fieldResults.Add(D("ordinal",f.Ordinal,"body_offset",f.Offset,"tag",f.Tag,
    "element_count",f.Count,"key_original_id",f.Key,"raw_uint32_words",f.Words,
    "expected_buffer_or_bit_index",start,"matches",match,
    "comparison",f.Tag==3?"raw-word-nonzero-to-LSB-packed-bit":"exact-original-uint32-bits"));
  }
  for(int tag=0;tag<3;tag++)equal&=next[tag]==actualRaw[tag].Length;
  for(int i=0;i<expectedBooleanWords.Length;i++)
   equal&=i<booleanWords.Length&&booleanWords[i]==expectedBooleanWords[i];
  equal&=next[3]==expectedBits;
  return D("status",equal?"all-fields-match-frozen-wire-semantics":"mismatch","matches",equal,
   "descriptor_count",descriptors.Length,"expected_descriptor_count",expected.Length,
   "original_descriptor_ids_preserved",equal,"raw_tags_0_to_2_words_bit_exact",equal,
   "raw_tags_0_to_2_word_counts",new[]{actualRaw[0].Length,actualRaw[1].Length,actualRaw[2].Length},
   "expected_raw_tags_0_to_2_word_counts",new[]{next[0],next[1],next[2]},
   "tag3_rule","uint32 != 0, packed LSB first in uint32 words","boolean_count",booleanCount,
   "expected_boolean_count",expectedBits,"boolean_words",booleanWords,
   "expected_boolean_words",expectedBooleanWords,
   "padding_scope","zero invalid output bits are an own logical export convention, not native padding evidence",
   "fields",fieldResults);
 }
 static int Main(string[] args){
  if(args.Length!=1)throw new ArgumentException("One exact project evidence output directory required");
  string output=ValidateOutput(args[0]),reportPath=Path.Combine(output,"probe.json");
  var report=D("started_utc",DateTime.UtcNow.ToString("o"),"archive",Archive,"selected_record",RecordName,
   "scope","frozen archive identity and one fixed 323-byte packed extent; no metadata or string-pool reread",
   "target_game_code_executed",false,"rea_or_java_started",false,"pool_supplied",false,
   "getter_invoked",false,"factory_invoked",false,"source_or_import_limits_changed",false,
   "full_load_chain_compatible",false,"native_parser_executed",false,"new_synthetic_assertions",0);
  FileStream file=null;string before=null,stage="frozen-probe-preflight";Exception failure=null;
  int hashPasses=0;var reads=new List<object>();
  try{
   FrozenField[] fields=ReadFrozenProbe(report);
   stage="fixed-archive-open";
   // A one-byte FileStream buffer avoids managed read-ahead around the
   // separate 323-byte format read. HashStream supplies its own 64 KiB buffer.
   file=new FileStream(Archive,FileMode.Open,FileAccess.Read,FileShare.Read,1,FileOptions.RandomAccess);
   report["archive_bytes"]=file.Length;Need(file.Length==ArchiveBytes,"ArchiveSizeChanged");
   stage="archive-fingerprint-before";before=HashStream(file);hashPasses++;
   report["sha256_before"]=before;Need(before==ArchiveHash,"ArchiveFingerprintChanged");
   stage="fixed-selected-packed-record";
   byte[] packed=ReadPacked(file,reads);Need(Hash(packed)==PackedHash,"SelectedPackedFingerprintChanged");
   report["payload"]=D("offset",PayloadOffset,"packed_bytes",packed.Length,"packed_sha256",Hash(packed));
   stage="two-own-decoder-comparison";
   var decoded=Lz4OutputSizeProfile.DecodeChecked(packed,BodyBytes);
   report["output_size_decoder"]=D("status",decoded.Status.ToString(),"reason",decoded.Reason,
    "input_position",decoded.InputPosition,"native_return_projection",decoded.NativeReturn);
   Need(decoded.Status==Lz4OutputSizeStatus.Success,"OutputSizeDecoderDidNotSucceed");
   byte[] body=decoded.CopyOutput(),inputBounded=Lz4Block.Decode(packed,BodyBytes);
   Need(decoded.InputPosition==PackedBytes&&Same(body,inputBounded),"OwnDecoderComparisonMismatch");
   Need(Hash(body)==BodyHash&&Hash(inputBounded)==BodyHash,"DecodedBodyFingerprintChanged");
   report["decoder_comparison"]=D("status","byte-identical","body_bytes",body.Length,
    "output_size_profile_sha256",Hash(body),"input_bounded_sha256",Hash(inputBounded),
    "scope","two own decoders and frozen sample hash; not a native execution oracle");
   stage="frozen-wire-comparison";CompareWire(body,fields);
   report["wire_vs_frozen_probe"]=D("status","all-38-fields-and-original-word-bits-match","field_count",fields.Length,
    "body_bytes",body.Length,"original_ids_preserved",true);
   stage="updated-parser-on-unmodified-body";
   var gate=new RegistrationGate();
   using(var reader=new BinaryRecordReader(gate,body,0,body.Length)){
    var parsed=BinaryRecordParser.DeserializeFresh(gate,reader);
    report["parser"]=D("status",parsed.Status,"reason",parsed.Reason,"initial_position",parsed.InitialPosition,
     "final_position",parsed.FinalPosition,"first_pass_end",parsed.FirstPassEnd,"completed_fields",parsed.CompletedFields,
     "domain_rejected",parsed.DomainRejected,"reader_consumed",reader.Consumed,"may_have_effects",parsed.MayHaveEffects,
     "gate_acquired",parsed.GateAcquired,"gate_released",parsed.GateReleased,"reader_rewound",parsed.ReaderRewound);
    var comparison=CompareParsed(parsed,fields);report["parser_vs_frozen_wire"]=comparison;
    if(parsed.Status=="Known"){
     Need((bool)comparison["matches"],"ParserResultDiffersFromFrozenWire");
     Need(parsed.Record.FieldBucketCount==16&&parsed.Record.Fields.Mask==15&&
      parsed.Record.Fields.Shift==0&&parsed.Record.FieldSlotCapacity==38&&
      parsed.Record.InitialSlotBytesCleared==456,"ParsedFieldStorageMismatch");
     report["field_storage"]=D("buckets",parsed.Record.FieldBucketCount,"mask",parsed.Record.Fields.Mask,
      "shift",parsed.Record.Fields.Shift,"slot_capacity",parsed.Record.FieldSlotCapacity,
      "initial_native_slot_bytes_cleared",parsed.Record.InitialSlotBytesCleared,
      "slot_growth_needed",false,"native_padding_or_allocator_equivalence",false);
     Need(parsed.InitialPosition==0&&parsed.FinalPosition==BodyBytes&&parsed.FirstPassEnd==BodyBytes&&
      parsed.CompletedFields==FrozenFieldCount&&parsed.GateAcquired&&parsed.GateReleased&&parsed.ReaderRewound&&
      reader.Consumed&&!parsed.DomainRejected,"ParserTraversalSummaryMismatch");
     report["status"]="single-official-body-parsed-and-compared";
    }else report["status"]="single-official-body-probed-parser-unsupported";
   }
   stage="complete";
  }catch(Exception e){
   failure=e;report["status"]="stopped";
   report["failure"]=D("stage",stage,"type",e.GetType().FullName,"message",e.Message);
  }finally{
   report["format_reads"]=reads;
   if(file!=null){
    try{
     string after=HashStream(file);hashPasses++;report["sha256_after"]=after;
     bool unchanged=before==ArchiveHash&&after==ArchiveHash&&file.Length==ArchiveBytes;
     report["archive_hash_unchanged"]=unchanged;
     if(!unchanged){
      report["status"]="archive-identity-not-confirmed";
      if(failure==null)failure=new InvalidDataException("ArchiveFingerprintChangedOrNotConfirmed");
     }
    }catch(Exception e){
     failure=e;report["status"]="final-archive-fingerprint-failed";report["final_hash_failure"]=e.Message;
    }finally{file.Dispose();}
   }
   report["fingerprint_reads"]=D("completed_full_file_passes",hashPasses,"expected_bytes_per_pass",ArchiveBytes,
    "chunk_bytes",65536,"purpose","fingerprinting only; no full-file allocation or decoding");
   report["finished_utc"]=DateTime.UtcNow.ToString("o");report["last_stage"]=stage;
   report["probe_process_peak_working_set_bytes"]=Process.GetCurrentProcess().PeakWorkingSet64;
   Save(reportPath,report);
  }
  Console.WriteLine(new JavaScriptSerializer().Serialize(D("status",report["status"],"report",reportPath,
   "stage",stage,"archive_hash_unchanged",report.ContainsKey("archive_hash_unchanged")?report["archive_hash_unchanged"]:false)));
  if(failure!=null)return 1;
  return Text(report["status"])=="single-official-body-parsed-and-compared"?0:2;
 }
}
