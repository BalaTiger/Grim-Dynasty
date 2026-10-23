// One named installed data archive and one named small record. No game code.
// Uses already-built own assemblies; does not change their coverage budgets.
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using GrimDynasty.Resources;
using GrimDynasty.TaskItems;

class ProbeOfficialArzRecord {
 const string Archive=@"D:\Steam\steamapps\common\Grim Dawn\database\database.arz";
 const string RecordName="records/items/questitems/scrapmetal.dbr";
 const long WorkingBudget=8L*1024*1024;
 const int SmallRecordBudget=65536;
 static readonly Encoding Utf8=new UTF8Encoding(false,true);
 static Dictionary<string,object> D(params object[] items){
  var d=new Dictionary<string,object>();
  for(int i=0;i<items.Length;i+=2)d.Add((string)items[i],items[i+1]);return d;
 }
 static uint U32(byte[] b,int p){return (uint)b[p]|((uint)b[p+1]<<8)|((uint)b[p+2]<<16)|((uint)b[p+3]<<24);}
 static string Hex(byte[] b){return BitConverter.ToString(b).Replace("-","").ToLowerInvariant();}
 static string Hash(byte[] b){using(var h=SHA256.Create())return Hex(h.ComputeHash(b));}
 static bool Same(byte[] a,byte[] b){
  if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;
 }
 static void Need(bool value,string reason){if(!value)throw new InvalidDataException(reason);}
 static void Save(string path,object value){
  var serializer=new JavaScriptSerializer();serializer.MaxJsonLength=2*1024*1024;
  byte[] bytes=Utf8.GetBytes(serializer.Serialize(value)+Environment.NewLine);
  using(var output=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None))
   output.Write(bytes,0,bytes.Length);
 }
 static void SaveBytes(string path,byte[] bytes){
  using(var output=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None))
   output.Write(bytes,0,bytes.Length);
 }
 sealed class ObservedStream:Stream {
  readonly Stream inner;
  public string Scope="preflight";
  public readonly List<object> Reads=new List<object>();
  public ObservedStream(Stream stream){inner=stream;}
  public override bool CanRead{get{return true;}}
  public override bool CanSeek{get{return true;}}
  public override bool CanWrite{get{return false;}}
  public override long Length{get{return inner.Length;}}
  public override long Position{get{return inner.Position;}set{inner.Position=value;}}
  public override long Seek(long offset,SeekOrigin origin){return inner.Seek(offset,origin);}
  public override int Read(byte[] b,int offset,int count){
   long start=inner.Position;int n=inner.Read(b,offset,count);
   Reads.Add(D("scope",Scope,"offset",start,"requested_bytes",count,"read_bytes",n));
   if(Reads.Count>64)throw new InvalidDataException("ProbeReadCallBudget");
   return n;
  }
  public override void Flush(){}
  public override void SetLength(long value){throw new NotSupportedException();}
  public override void Write(byte[] b,int offset,int count){throw new NotSupportedException();}
 }
 static string HashStream(Stream stream){
  stream.Position=0;
  using(var h=SHA256.Create()){
   byte[] buffer=new byte[65536];int n;
   while((n=stream.Read(buffer,0,buffer.Length))!=0)h.TransformBlock(buffer,0,n,null,0);
   h.TransformFinalBlock(new byte[0],0,0);return Hex(h.Hash);
  }
 }
 // Structural observation only. This does not emulate native indexing,
 // pointer caches, tag-3 bit storage or any native initialization.
 static Dictionary<string,object> ObserveWire(ArzRawRecord raw,byte[] body,
  out List<uint> keys,out List<int> tags,out List<uint[]> words){
  keys=new List<uint>();tags=new List<int>();words=new List<uint[]>();
  var fields=new List<object>();var referenced=new SortedSet<uint>();
  int cursor=0,total=0;int[] tagCounts=new int[4];var seen=new HashSet<uint>();int duplicates=0;
  string reason=null;
  while(cursor<body.Length){
   if(body.Length-cursor<8){reason="DiagnosticTruncatedHeader";break;}
   int start=cursor,tag=body[cursor]|(body[cursor+1]<<8),count=body[cursor+2]|(body[cursor+3]<<8);
   uint key=U32(body,cursor+4);
   if(tag>3){reason="DiagnosticUnknownTag";break;}
   if(count>(body.Length-cursor-8)/4){reason="DiagnosticTruncatedWords";break;}
   if(fields.Count>=4096||count>65536-total){reason="DiagnosticObservationBudget";break;}
   cursor+=8;var values=new uint[count];
   for(int i=0;i<count;i++){values[i]=U32(body,cursor);cursor+=4;if(tag==2)referenced.Add(values[i]);}
   referenced.Add(key);if(!seen.Add(key))duplicates++;total+=count;tagCounts[tag]++;
   keys.Add(key);tags.Add(tag);words.Add(values);
   fields.Add(D("ordinal",fields.Count,"body_offset",start,"tag",tag,"element_count",count,
    "key_original_id",key,"raw_uint32_words",values));
  }
  var strings=new List<object>();
  foreach(uint id in referenced){
   if(id>=(uint)raw.StringCount){strings.Add(D("original_id",id,"status","outside-original-pool"));continue;}
   byte[] b=raw.CopyStringBytes(id);
   strings.Add(D("original_id",id,"byte_length",b.Length,"sha256",Hash(b),"utf8",Utf8.GetString(b)));
  }
  return D("status",reason==null?"complete-structural-observation":"stopped","reason",reason,
   "field_count",fields.Count,"total_words",total,"tag_counts_0_to_3",tagCounts,"cursor",cursor,
   "duplicate_key_occurrences",duplicates,"fields",fields,"referenced_original_strings",strings,
   "reindexes_or_trims_pool",false,"native_parser_or_getter_compatibility_claim",false);
 }
 static object CompareParsed(BinaryRecordParseResult parsed,List<uint> keys,List<int> tags,List<uint[]> words){
  if(parsed.Status!="Known")return D("status","not-comparable-parser-rejected","reason",parsed.Reason);
  var desc=parsed.Record.CopyDescriptors();bool equal=desc.Length==keys.Count;
  var next=new int[3];var actual=new uint[3][];
  for(int i=0;i<3;i++)actual[i]=parsed.Record.CopyRawValues(i);
  for(int i=0;i<desc.Length&&i<keys.Count;i++){
   int tag=tags[i];if(tag>2){equal=false;continue;}
   equal&=desc[i].Key==keys[i]&&desc[i].TypeTag==tag&&desc[i].ElementCount==(uint)words[i].Length&&desc[i].BufferIndex==(uint)next[tag];
   for(int j=0;j<words[i].Length;j++){
    if(next[tag]>=actual[tag].Length){equal=false;break;}
    equal&=actual[tag][next[tag]++]==words[i][j];
   }
  }
  return D("status",equal?"all-original-ids-and-word-bits-match":"mismatch","original_ids_preserved",equal);
 }
 static int Main(string[] args){
  if(args.Length!=1)throw new ArgumentException("One project evidence output directory required");
  string output=Path.GetFullPath(args[0]),root=Path.GetFullPath(@"C:\Users\zhuzi\Documents\Grim-Dynasty")+Path.DirectorySeparatorChar;
  Need(output.StartsWith(root,StringComparison.OrdinalIgnoreCase),"ProjectOutputRequired");
  Directory.CreateDirectory(output);
  string reportPath=Path.Combine(output,"probe.json");
  Need(!File.Exists(reportPath),"PreserveExistingProbe");
  var report=D("started_utc",DateTime.UtcNow.ToString("o"),"archive",Archive,"selected_record",RecordName,
   "scope","one installed base database, complete bounded metadata and one selected payload",
   "target_game_code_executed",false,"rea_or_java_started",false,"production_code_or_limits_changed",false,
   "official_full_load_chain_compatible",false,"game_build_version","not inferred from ARZ format version",
   "new_synthetic_assertions",0);
  FileStream file=null;ObservedStream observed=null;string before=null;string stage="open";Exception failure=null;
  try{
   file=new FileStream(Archive,FileMode.Open,FileAccess.Read,FileShare.Read,65536,FileOptions.RandomAccess);
   long size=file.Length;Need(size>24&&size<=128L*1024*1024,"ArchiveOutsideProbeFileBudget");
   report["archive_bytes"]=size;report["last_write_utc"]=File.GetLastWriteTimeUtc(Archive).ToString("o");
   before=HashStream(file);report["sha256_before"]=before;
   observed=new ObservedStream(file);var input=new ResourceInput(observed);
   stage="header-and-count";
   byte[] h=input.Read(0,24);uint signature=U32(h,0),rt=U32(h,4),rs=U32(h,8),count=U32(h,12),st=U32(h,16),ss=U32(h,20);
   Need(signature==0x00030002U,"UnsupportedArchiveMarkerVersion");
   Need(rt>=24&&st>=24&&(long)rt+rs<=size&&(long)st+ss<=size&&ss>=4,"ArchiveMetadataExtent");
   Need((long)rt+rs<=st||(long)st+ss<=rt,"ArchiveMetadataOverlap");
   // These bounds are decided from the preceding 28-byte inspection. A
   // changed archive stops; this probe never silently raises its budgets.
   Need(count<=34180&&rs<=1342625&&ss<=3978638,"MetadataExceedsPreflightEstimate");
   byte[] sc=input.Read(st,4);uint strings=U32(sc,0);
   Need(strings<=82999,"StringCountExceedsPreflightEstimate");
   long poolBytes=(long)ss-4-strings*4L;
   Need(poolBytes>=0&&poolBytes<=3646638,"PoolPayloadExceedsPreflightEstimate");
   long arrays=28L+ss+rs+strings*8L;
   long worst=arrays+Utf8.GetByteCount(RecordName)+SmallRecordBudget*4L;
   Need(worst<=WorkingBudget,"MetadataAndSingleRecordOutsideWorkingBudget");
   report["header"]=D("marker",signature&65535U,"version",signature>>16,"record_table_offset",rt,
    "record_table_bytes",rs,"record_count",count,"string_table_offset",st,"string_table_bytes",ss,"string_count",strings,
    "pool_payload_bytes",poolBytes,"header_hex",Hex(h));
   report["memory_preflight"]=D("raw_metadata_array_bytes",arrays,"upper_array_charge_for_selected_small_record",worst,
    "raw_reader_working_budget",WorkingBudget,"max_selected_packed_bytes",SmallRecordBudget,"max_selected_body_bytes",SmallRecordBudget,
    "additional_hash_buffer_bytes",65536,"not_a_clr_or_process_peak_bound",true);
   var fullLimits=new ArzRawLimits((int)count,(int)strings,(int)ss,(int)rs,65536,(int)poolBytes,
    SmallRecordBudget,SmallRecordBudget,WorkingBudget);
   // Show the actual chain guard without fabricating a huge parser pool.
   stage="source-construction-guard";
   try{
    new ArzRecordBinarySource(input,fullLimits,new RegistrationGate(),
     new BinaryPointerCacheSnapshot(0,0,new BinaryNameCacheEntry[0][]),null,null,null,
     ArzRawDecodeProfile.ObservedEngineOutputSize);
    report["source_constructor"]=D("status","accepted");
   }catch(ArgumentOutOfRangeException e){
    report["source_constructor"]=D("status","rejected","exception",e.GetType().FullName,"parameter",e.ParamName,"message",e.Message,
     "actual_string_count",strings,"source_maximum_strings",BinaryWarmState.MaximumStrings,
     "actual_pool_payload_bytes",poolBytes,"source_maximum_pool_bytes",BinaryWarmState.MaximumPoolBytes);
   }
   stage="reader-under-unchanged-source-pool-budgets";observed.Scope=stage;
   var chainLimits=new ArzRawLimits((int)count,BinaryWarmState.MaximumStrings,(int)ss,(int)rs,
    65536,BinaryWarmState.MaximumPoolBytes,SmallRecordBudget,SmallRecordBudget,WorkingBudget);
   try{
    ArzRawRecordReader.ReadExact(input,Utf8.GetBytes(RecordName),chainLimits,ArzRawDecodeProfile.ObservedEngineOutputSize);
    report["reader_with_chain_pool_limits"]=D("status","accepted");
   }catch(InvalidDataException e){report["reader_with_chain_pool_limits"]=D("status","rejected","reason",e.Message);}
   stage="bounded-full-metadata-single-record";observed.Scope=stage;
   ArzRawRecord raw=ArzRawRecordReader.ReadExact(input,Utf8.GetBytes(RecordName),fullLimits,ArzRawDecodeProfile.ObservedEngineOutputSize);
   byte[] body=raw.CopyBodyBytes();
   report["raw_reader"]=D("status","accepted","record_name_original_id",raw.RecordNameStringId,
    "record_class",Utf8.GetString(raw.CopyClassBytes()),"data_offset",raw.DataOffset,"packed_bytes",raw.PackedBytes,
    "body_bytes",raw.BodyBytes,"packed_bytes_consumed",raw.PackedBytesConsumed,"profile",raw.DecodeProfile.ToString(),
    "timestamp_low",raw.TimestampLow,"timestamp_high",raw.TimestampHigh,"body_sha256",Hash(body),
    "original_pool_count",raw.StringCount,"original_string_table_bytes",raw.StringTableBytes);
   // Read the same selected packed extent once more, never another record.
   stage="same-record-decoder-comparison";observed.Scope=stage;
   byte[] packed=input.Read(raw.DataOffset,raw.PackedBytes);
   byte[] independent=Lz4Block.Decode(packed,(int)raw.BodyBytes);
   report["decoder_comparison"]=D("status",Same(body,independent)?"byte-identical":"mismatch",
    "input_bounded_sha256",Hash(independent),"output_size_profile_sha256",Hash(body),"packed_sha256",Hash(packed),
    "comparison_scope","two existing own decoder implementations; no native decoder oracle");
   SaveBytes(Path.Combine(output,"selected-packed.bin"),packed);
   SaveBytes(Path.Combine(output,"selected-body.bin"),body);
   stage="existing-parser-on-unmodified-body";
   var gate=new RegistrationGate();
   using(var reader=new BinaryRecordReader(gate,body,0,body.Length)){
    var parsed=BinaryRecordParser.DeserializeFresh(gate,reader);
    report["existing_parser"]=D("status",parsed.Status,"reason",parsed.Reason,"initial_position",parsed.InitialPosition,
     "final_position",parsed.FinalPosition,"first_pass_end",parsed.FirstPassEnd,"completed_fields",parsed.CompletedFields,
     "domain_rejected",parsed.DomainRejected,"reader_consumed",reader.Consumed,"may_have_effects",parsed.MayHaveEffects,
     "gate_acquired",parsed.GateAcquired,"gate_released",parsed.GateReleased,"pool_supplied",false);
    List<uint> keys;List<int> tags;List<uint[]> words;
    var diagnostic=ObserveWire(raw,body,out keys,out tags,out words);
    report["wire_observer"]=diagnostic;
    report["parser_vs_wire_observer"]=CompareParsed(parsed,keys,tags,words);
   }
   stage="complete";
   report["status"]="single-official-record-observed-with-explicit-coverage-results";
  }catch(Exception e){
   failure=e;report["status"]="stopped";report["failure"]=D("stage",stage,"type",e.GetType().FullName,"message",e.Message);
  }finally{
   if(observed!=null)report["format_reads"]=observed.Reads;
   if(file!=null){
    try{
     string after=HashStream(file);report["sha256_after"]=after;report["archive_hash_unchanged"]=before==after;
     report["fingerprint_reads"]=D("full_file_passes",before==null?1:2,"bytes_per_pass",file.Length,"chunk_bytes",65536,
      "purpose","fingerprinting only; no full-file allocation or decoding");
     if(before!=after){report["status"]="archive-hash-changed";failure=new InvalidDataException("ArchiveHashChanged");}
    }catch(Exception e){failure=e;report["final_hash_failure"]=e.Message;}
    file.Dispose();
   }
   report["finished_utc"]=DateTime.UtcNow.ToString("o");report["last_stage"]=stage;
   report["probe_process_peak_working_set_bytes"]=Process.GetCurrentProcess().PeakWorkingSet64;
   Save(reportPath,report);
  }
  Console.WriteLine(new JavaScriptSerializer().Serialize(D("status",report["status"],"report",reportPath,"stage",stage,
   "archive_hash_unchanged",report.ContainsKey("archive_hash_unchanged")?report["archive_hash_unchanged"]:false)));
  return failure==null?0:1;
 }
}
