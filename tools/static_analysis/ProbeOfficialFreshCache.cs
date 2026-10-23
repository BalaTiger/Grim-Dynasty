// One fixed installed ARZ, one selected record, complete original-ID pool.
// Runs independent managed code only; never loads game binaries or save data.
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using GrimDynasty.Resources;
using GrimDynasty.TaskItems;

class ProbeOfficialFreshCache {
 const string Root=@"C:\Users\zhuzi\Documents\Grim-Dynasty";
 const string Archive=@"D:\Steam\steamapps\common\Grim Dawn\database\database.arz";
 const string RecordName="records/items/questitems/scrapmetal.dbr";
 const string ArchiveHash="9e3532250cdb611a4cc989997c8f97fcec2d6c422063a845e6c43cd538c30603";
 const string PackedHash="d532c7583a8fa02523e67e2b69efec1b0b48bd1652294fa2f76648af03906e58";
 const string BodyHash="5cbd10f21b424b0bec879fe101e77772fe42965ac363a5503c46907a62d91c7f";
 const long ArchiveBytes=58529720;
 const ulong EmptyToken=0x1004,ClassToken=0x2008;
 static int assertions;
 static readonly Encoding Utf8=new UTF8Encoding(false,true);
 static readonly List<object> Samples=new List<object>();
 static Dictionary<string,object> D(params object[] items){
  var result=new Dictionary<string,object>();
  for(int i=0;i<items.Length;i+=2)result.Add((string)items[i],items[i+1]);
  return result;
 }
 static void Need(bool value,string why){assertions++;if(!value)throw new InvalidDataException(why);}
 static byte[] B(string value){return Utf8.GetBytes(value);}
 static string Hex(byte[] value){return BitConverter.ToString(value).Replace("-","").ToLowerInvariant();}
 static string Hash(byte[] value){using(var h=SHA256.Create())return Hex(h.ComputeHash(value));}
 static uint U32(byte[] b,int at){return (uint)b[at]|((uint)b[at+1]<<8)|((uint)b[at+2]<<16)|((uint)b[at+3]<<24);}
 static string FileHash(){
  using(var input=new FileStream(Archive,FileMode.Open,FileAccess.Read,FileShare.Read))
  using(var h=SHA256.Create()){
   Need(input.Length==ArchiveBytes,"Fixed archive size changed");
   var buffer=new byte[65536];int n;
   while((n=input.Read(buffer,0,buffer.Length))!=0)h.TransformBlock(buffer,0,n,null,0);
   h.TransformFinalBlock(new byte[0],0,0);return Hex(h.Hash);
  }
 }
 static ObjectNameString Requested(){
  byte[] b=B(RecordName);return new ObjectNameString(b,(ulong)Math.Max(15,b.Length));
 }
 static void Save(string output,object value){
  var serializer=new JavaScriptSerializer();serializer.MaxJsonLength=262144;
  byte[] b=Utf8.GetBytes(serializer.Serialize(value)+"\n");
  using(var f=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.None))f.Write(b,0,b.Length);
 }
 sealed class GuardedStream:Stream {
  readonly FileStream inner;
  readonly SHA256 packed=SHA256.Create();
  public readonly List<object> Reads=new List<object>();
  public int Accesses;
  public long Bytes;
  public bool Closed;
  public int PackedCount;
  public string PackedSha;
  public GuardedStream(){inner=new FileStream(Archive,FileMode.Open,FileAccess.Read,FileShare.Read);}
  void Access(){Accesses++;if(Closed)throw new ObjectDisposedException("FixedOfficialArchive");}
  public override bool CanRead{get{return !Closed;}}
  public override bool CanSeek{get{return !Closed;}}
  public override bool CanWrite{get{return false;}}
  public override long Length{get{Access();return inner.Length;}}
  public override long Position{get{Access();return inner.Position;}set{Access();inner.Position=value;}}
  public override long Seek(long offset,SeekOrigin origin){Access();return inner.Seek(offset,origin);}
  static bool In(long start,int count,long baseOffset,long length){
   return start>=baseOffset&&count>=0&&start-baseOffset<=length&&count<=length-(start-baseOffset);
  }
  public override int Read(byte[] b,int offset,int count){
   Access();long start=inner.Position;
   Need(Reads.Count<32&&count<=4*1024*1024&&Bytes+count<=6L*1024*1024,"Fixed format-read budget");
   bool payload=In(start,count,42034543,323);
   Need(In(start,count,0,24)||In(start,count,54551066,3978638)||
    In(start,count,53208441,1342625)||payload,"Read outside fixed metadata/selected payload");
   int n=inner.Read(b,offset,count);Bytes+=n;
   Reads.Add(D("offset",start,"requested_bytes",count,"read_bytes",n,"selected_payload",payload));
   if(payload){
    Need(start==42034543+PackedCount,"Selected compressed bytes must be read once in order");
    packed.TransformBlock(b,offset,n,null,0);PackedCount+=n;
    if(PackedCount==323){packed.TransformFinalBlock(new byte[0],0,0);PackedSha=Hex(packed.Hash);}
   }
   return n;
  }
  public override void Flush(){}
  public override void SetLength(long value){throw new NotSupportedException();}
  public override void Write(byte[] b,int offset,int count){throw new NotSupportedException();}
  protected override void Dispose(bool disposing){
   if(!Closed){Closed=true;if(disposing){inner.Dispose();packed.Dispose();}}
   base.Dispose(disposing);
  }
 }
 static void CompareParser(byte[] body,BinaryParsedRecord parsed){
  var descriptors=parsed.CopyDescriptors();Need(descriptors.Length==38,"Frozen field count");
  uint[][] words={parsed.CopyRawValues(0),parsed.CopyRawValues(1),parsed.CopyRawValues(2)};
  uint[] bits=parsed.CopyBooleanWords();int[] cursors=new int[4];int at=0;
  for(int i=0;i<descriptors.Length;i++){
   uint header=U32(body,at);int tag=(int)(header&65535),count=(int)(header>>16);
   var descriptor=descriptors[i];
   Need(tag>=0&&tag<=3&&descriptor.TypeTag==tag&&descriptor.Key==U32(body,at+4)&&
    descriptor.ElementCount==(uint)count&&descriptor.BufferIndex==(uint)cursors[tag],"Descriptor/original-ID mismatch");
   at+=8;
   for(int j=0;j<count;j++){
    uint value=U32(body,at);at+=4;
    if(tag==3)Need(((bits[cursors[tag]>>5]>>(cursors[tag]&31))&1U)==(value==0?0U:1U),"Boolean wire conversion mismatch");
    else Need(words[tag][cursors[tag]]==value,"Raw word/string-ID mismatch");
    cursors[tag]++;
   }
  }
  Need(at==456&&parsed.BooleanCount==7&&bits.Length==1&&bits[0]==0x30,"Frozen Boolean/body endpoint");
 }
 static int Main(string[] args){
  string outDir=Path.Combine(Root,@"docs\object-lifecycle\official-fresh-cache-validation");
  var report=D("schema","grim-dynasty-single-official-fresh-cache-v1","status","started",
   "started_utc",DateTime.UtcNow.ToString("o"),"archive",Archive,"record",RecordName,
   "native_game_executed",false,"native_clock_globals_recovered",false,"full_native_compatibility",false,
   "factory_or_native_registration_recovered",false,"save_or_cloud_access",false,
   "clock_scope","Explicit owned Stopwatch baseline/frequency, captured at the modeled start/end stages; not Engine clock initialization",
   "query_tokens_scope","Two explicit distinct own identity tokens, not native runtime pointers",
   "budgets",D("raw_array_allocation_bytes",8L*1024*1024,"source_conservative_array_bytes",24L*1024*1024,
    "format_read_bytes",6L*1024*1024,"lazy_materialized_strings",1024,"lazy_materialized_payload_bytes",262144,
    "excludes","Object headers, CLR/JIT/GC and OS file cache; process peak is measured separately"));
  string failure=null;GuardedStream stream=null;bool admittedOutput=false,matchedBefore=false;
  long initialLength=0;DateTime initialWrite=DateTime.MinValue;
  try{
   Need(args.Length==0,"No arguments accepted");
   Need(!Directory.Exists(outDir),"Preserve existing official fresh-cache evidence");
   for(string p=Path.GetDirectoryName(outDir);p!=null;p=Path.GetDirectoryName(p))
    if(Directory.Exists(p))Need((File.GetAttributes(p)&FileAttributes.ReparsePoint)==0,"Project ancestor link unsupported");
   Directory.CreateDirectory(outDir);admittedOutput=true;
   report["sha256_before"]=FileHash();Need((string)report["sha256_before"]==ArchiveHash,"Fixed archive fingerprint mismatch");
   matchedBefore=true;
   var before=new FileInfo(Archive);initialLength=before.Length;initialWrite=before.LastWriteTimeUtc;
   // This is an explicit own timer initialization, never an assumed value of
   // the four unknown Engine globals. Each sample is converted separately.
   Need(Stopwatch.IsHighResolution&&Stopwatch.Frequency>0,"Owned high-resolution clock required");
   long frequency=Stopwatch.Frequency,basis=Stopwatch.GetTimestamp();int captures=0;
   var depot=RecordDepotInitialization.CreateFresh(new RegistrationGate());
   RecordDepotState clockDepot=depot;
   stream=new GuardedStream();
   using(var input=new ResourceInput(stream)){
    var limits=new ArzRawLimits(34180,82999,3978638,1342625,259,3646638,323,456,8L*1024*1024);
    Func<RecordLoadTickResult> clock=delegate{
     long raw=Stopwatch.GetTimestamp();var converted=RecordLoadTicks.FromQpc(raw,basis,frequency,true);captures++;
     Samples.Add(D("ordinal",captures,"raw",raw,"basis",basis,"frequency",frequency,
      "converted_low32",converted.Tick32,"mode",converted.Mode.ToString(),
      "status",converted.Status.ToString(),"load_count_at_capture",clockDepot.LoadCount50,
      "committed_count_at_capture",clockDepot.LastCompletedCache.Count));
     return converted;
    };
    var source=ArzRecordBinarySource.ForFreshUniqueArchiveWithClock(input,limits,new RegistrationGate(),EmptyToken,clock);
    var attempt=RecordLoadFromSuppliedBinaryAttempt.FromArzRecord(depot,Requested(),RecordNameLocale.KnownC,source);
    Need(stream.Accesses==0&&captures==0&&depot.Cache.Count==0,"Construction must be lazy");
    var loaded=attempt.RunOnce();var trace=attempt.Trace;
    Need(loaded.Status=="Known"&&!loaded.CacheHit&&trace.CacheCommitted&&trace.LoadCountIncremented&&trace.ElapsedRecorded,
     "Official first miss must finish parser, publication and statistics");
    Need(captures==2&&depot.LoadCount50==1&&depot.HitCount54==0&&depot.Cache.Count==1&&!depot.RequiresResolution,
     "One official publication and two actual clock captures");
    Need(trace.StartTickConversion!=null&&trace.EndTickConversion!=null&&trace.Elapsed==
     RecordLoadTicks.Elapsed(trace.StartTickConversion.Tick32.Value,trace.EndTickConversion.Tick32.Value),"Separate tick conversion");
    Need(depot.ElapsedTotal5C==trace.Elapsed&&depot.ElapsedMaximum60==trace.Elapsed,"Fresh elapsed counters");
    var rawRecord=trace.OwnArzRecord;byte[] body=rawRecord.CopyBodyBytes();
    Need(rawRecord.RecordNameStringId==6883&&rawRecord.StringCount==82999&&rawRecord.DataOffset==42034543&&
     rawRecord.PackedBytes==323&&rawRecord.BodyBytes==456&&Hash(body)==BodyHash,"Frozen raw record identity");
    Need(stream.PackedCount==323&&stream.PackedSha==PackedHash,"Only frozen selected compressed record decoded");
    Need(trace.Parser.Status=="Known"&&trace.Parser.CompletedFields==38&&trace.ReaderDisposed,"Actual parser and reader cleanup");
    CompareParser(body,trace.Parser.Record);
    var table=(LoadTableBinarySnapshot)loaded.Table;var pool=table.Warm.StringPool;
    Need(pool!=null&&pool.Count==82999&&pool.EmptyId==82687&&pool.IsUnique&&pool.MaterializedCount==0,
     "Full original-ID pool remains lazy after parser/publication");
    Need(table.Warm.Names.Shift==2&&table.Warm.Names.BucketCount==16384&&table.Warm.Names.Capacity==41499&&
     table.Warm.Names.UsedCount==1&&table.Warm.Names.NativeClearedSlotBytes==497976,"Recovered pointer-cache preparation");
    Need(pool.FindExact(B("Class"))==13&&pool.FindExact(B("QuestItem"))==28355&&
     pool.FindExact(B(RecordName))==6883&&pool.FindExact(new byte[0])==82687&&pool.MaterializedCount==0,
     "Full byte index keeps original IDs without materializing strings");
    var firstGetter=RecordTableGetter.GetValue(table,B("Class\0"),null,ClassToken);
    Need(firstGetter.Status=="Known"&&!firstGetter.UsedDefault&&firstGetter.Binary.Cold&&
     firstGetter.Binary.NameId==13&&firstGetter.Binary.StringIndex==28355&&firstGetter.Value!=null&&
     Utf8.GetString(firstGetter.Value.CopyCStringBytes())=="QuestItem\0","Real Class getter through complete generated context");
    Need(!firstGetter.Binary.NameLookup.NativeLayoutKnown&&firstGetter.Binary.NameLookup.Bucket==-1,
     "Logical byte index does not fabricate native chain layout");
    Need(pool.MaterializedCount==1&&pool.MaterializedBytes==9,"Only requested value materialized on cold lookup");
    int accesses=stream.Accesses;
    var repeated=RecordLoadFromSuppliedBinaryAttempt.FromArzRecord(depot,Requested(),RecordNameLocale.KnownC,source);
    var hit=repeated.RunOnce();
    Need(hit.Status=="Known"&&hit.CacheHit&&Object.ReferenceEquals(hit.Table,table)&&stream.Accesses==accesses&&captures==2&&
     !repeated.Trace.Entered&&depot.HitCount54==1&&depot.LoadCount50==1,"Repeated hit does not read, parse or sample clocks");
    input.Dispose();accesses=stream.Accesses;Need(stream.Closed,"Borrowed source closed explicitly");
    var closedHit=RecordLoadFromSuppliedBinaryAttempt.FromArzRecord(depot,Requested(),RecordNameLocale.KnownC,source);
    var closedResult=closedHit.RunOnce();
    Need(closedResult.Status=="Known"&&closedResult.CacheHit&&Object.ReferenceEquals(closedResult.Table,table)&&
     stream.Accesses==accesses&&captures==2&&depot.HitCount54==2,"Closed-source cache hit retains same table");
    var warmGetter=RecordTableGetter.GetValue(table,B("Class\0"),null,ClassToken);
    Need(warmGetter.Status=="Known"&&!warmGetter.Binary.Cold&&Object.ReferenceEquals(warmGetter.Value,firstGetter.Value)&&
     pool.MaterializedCount==2&&pool.MaterializedBytes==14&&stream.Accesses==accesses,"Closed-source lazy warm name and stable value identity");
    var failedDepot=RecordDepotInitialization.CreateFresh(new RegistrationGate());
    clockDepot=failedDepot;
    var failed=RecordLoadFromSuppliedBinaryAttempt.FromArzRecord(failedDepot,Requested(),RecordNameLocale.KnownC,source);
    var failedResult=failed.RunOnce();int failedAccesses=stream.Accesses,failedCaptures=captures;
    Need(failedResult.Status=="Unsupported"&&failedResult.Reason=="OwnArzRecordReadOperationThrew:System.ObjectDisposedException"&&
     failed.Trace.OwnArzReadAttempted&&!failed.Trace.OwnArzReadCompleted&&failed.Trace.Parser==null&&
     failedDepot.Cache.Count==0&&failedDepot.LoadCount50==0&&!failedDepot.RequiresResolution,"Closed-source first miss fails before parser/cache effects");
    Need(Object.ReferenceEquals(failed.RunOnce(),failedResult)&&stream.Accesses==failedAccesses&&captures==failedCaptures&&
     Object.ReferenceEquals(attempt.RunOnce(),loaded),"Neither success nor failure replay repeats work");
    report["result"]=D("first_load","passed","repeat_hit","passed","closed_source_hit","passed","closed_source_miss","passed",
     "parsed_fields",38,"original_pool_count",pool.Count,"empty_original_id",pool.EmptyId,"class_original_id",13,
     "class_value_original_id",28355,"class_value","QuestItem","lazy_strings_materialized",pool.MaterializedCount,
     "lazy_payload_bytes",pool.MaterializedBytes,"pool_index_memo_array_payload_bytes",pool.RetainedArrayPayloadBytes,
     "raw_retained_array_payload_bytes",pool.RawRetainedArrayPayloadBytes,
     "loads",depot.LoadCount50,"hits",depot.HitCount54,"elapsed_total",depot.ElapsedTotal5C,
     "elapsed_maximum",depot.ElapsedMaximum60,"native_archive_read",trace.NativeArchiveRead,
     "own_source_name_index_layout","complete byte-sorted ID index, native chain layout unclaimed",
     "closed_miss_reason",failedResult.Reason);
   }
   report["sha256_after"]=FileHash();
   var after=new FileInfo(Archive);
   Need((string)report["sha256_after"]==ArchiveHash&&after.Length==initialLength&&after.LastWriteTimeUtc==initialWrite,
    "Original archive must retain its hash and metadata");
   report["archive_hash_unchanged"]=true;report["status"]="single-official-own-fresh-cache-chain-passed";
  }catch(Exception e){failure=e.ToString();report["status"]="failed";report["failure"]=failure;}
  finally{
   if(stream!=null){report["format_reads"]=stream.Reads;report["format_read_bytes"]=stream.Bytes;stream.Dispose();}
   if(matchedBefore&&!report.ContainsKey("sha256_after")){
    try{
     report["sha256_after"]=FileHash();var after=new FileInfo(Archive);
     report["archive_hash_unchanged"]=(string)report["sha256_after"]==ArchiveHash&&
      after.Length==initialLength&&after.LastWriteTimeUtc==initialWrite;
    }catch(Exception auditError){report["preservation_audit_failure"]=auditError.ToString();}
   }
   report["clock_samples"]=Samples;report["assertions"]=assertions;
   report["finished_utc"]=DateTime.UtcNow.ToString("o");
   using(var p=Process.GetCurrentProcess()){
    p.Refresh();report["own_probe_peak_working_set_bytes"]=p.PeakWorkingSet64;
    report["own_probe_private_bytes_at_finish"]=p.PrivateMemorySize64;
   }
   if(admittedOutput)Save(Path.Combine(outDir,"probe.json"),report);
  }
  Console.WriteLine(new JavaScriptSerializer().Serialize(D("status",report["status"],"assertions",assertions,
   "output",Path.Combine(outDir,"probe.json"),"failure",failure)));
  return failure==null?0:1;
 }
}
