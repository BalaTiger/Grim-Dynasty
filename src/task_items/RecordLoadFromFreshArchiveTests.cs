using System;
using System.IO;
using GrimDynasty.Resources;
using GrimDynasty.TaskItems;

// Entirely self-authored ARZv3 bytes. No original files, native archive
// execution, inferred clock globals or item-factory compatibility claims.
class RecordLoadFromFreshArchiveTests {
 static int assertions,scenarios;
 const ulong EmptyAddress=0x12345000,ClassAddress=0x12345008,HighAddress=0x12345010;
 static void Need(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static byte[] B(string s){return ResourceUtil.Utf8.GetBytes(s);}
 static bool Same(byte[] a,byte[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
 static ObjectNameString Name(string s){byte[] bytes=B(s);return new ObjectNameString(bytes,(ulong)Math.Max(15,bytes.Length));}
 static ArzRawLimits Limits(int count=131072,int wire=8388608,int pool=4194304,
  int packed=2097152,int body=1048576,long working=8388608){
  return new ArzRawLimits(16,count,wire,65536,65536,pool,packed,body,working);
 }
 static byte[][] Pool(int count){
  var result=new byte[count][];
  result[0]=B("rec");result[1]=B("Class");result[2]=B("Item");result[3]=B("raw");result[4]=new byte[0];
  for(int i=5;i<count;i++)result[i]=B("string-"+i);
  return result;
 }
 static byte[] Body(int count){
  if(count==5)return ArzRawFixture.Join(ArzRawFixture.Field(2,1,2),
   ArzRawFixture.Field(0,3,0xdeadbeefU,0x80000000U));
  return ArzRawFixture.Join(ArzRawFixture.Field(2,1,(uint)(count-1)),
   ArzRawFixture.Field(2,(uint)(count-2),2),ArzRawFixture.Field(0,3,0xdeadbeefU));
 }
 static ArzRawFixtureData Fixture(byte[][] pool){return ArzRawFixture.Build(pool,new ArzRawFixtureRecord(0,Body(pool.Length)));}
 static RecordDepotState Depot(){return RecordDepotInitialization.CreateFresh(new RegistrationGate());}
 static ArzRecordBinarySource Source(ResourceInput input){
  return ArzRecordBinarySource.ForFreshUniqueArchive(input,Limits(),new RegistrationGate(),EmptyAddress,10,17);
 }
 static RecordLoadFromSuppliedBinaryAttempt Attempt(RecordDepotState depot,ArzRecordBinarySource source){
  return RecordLoadFromSuppliedBinaryAttempt.FromArzRecord(depot,Name("REC"),RecordNameLocale.KnownC,source);
 }
 sealed class ObservedStream:Stream {
  readonly Stream inner;
  public int Accesses;
  public bool Closed;
  public ObservedStream(byte[] bytes){inner=new MemoryStream(bytes,false);}
  public override bool CanRead{get{return inner.CanRead;}}
  public override bool CanSeek{get{return inner.CanSeek;}}
  public override bool CanWrite{get{return false;}}
  public override long Length{get{Accesses++;return inner.Length;}}
  public override long Position{get{Accesses++;return inner.Position;}set{Accesses++;inner.Position=value;}}
  public override int Read(byte[] bytes,int offset,int count){Accesses++;return inner.Read(bytes,offset,count);}
  public override long Seek(long offset,SeekOrigin origin){Accesses++;return inner.Seek(offset,origin);}
  public override void Flush(){}
  public override void SetLength(long length){throw new NotSupportedException();}
  public override void Write(byte[] bytes,int offset,int count){throw new NotSupportedException();}
  protected override void Dispose(bool disposing){Closed=true;if(disposing)inner.Dispose();base.Dispose(disposing);}
 }
 static void FullPoolLoadAndHit(int count){
  var pool=Pool(count);var fixture=Fixture(pool);var original=(byte[])fixture.Bytes.Clone();
  var stream=new ObservedStream(fixture.Bytes);
  using(var input=new ResourceInput(stream)){
   var depot=Depot();var source=Source(input);var attempt=Attempt(depot,source);
   Need(stream.Accesses==0&&!attempt.Attempted,"constructing fresh source does not read input");
   Need(source.DecodeProfile==ArzRawDecodeProfile.ObservedEngineOutputSize&&!source.NativeArchiveFetchRecovered&&
    source.RemapContext==null,"fresh source is an explicit output-size logical projection with no supplied map");
   Need(source.FreshChainArrayPayloadAllowanceBytes>0&&
    source.FreshChainArrayPayloadAllowanceBytes<=ArzRecordBinarySource.MaximumFreshChainArrayPayloadBytes,
    "one chain has an explicit conservative array-payload allowance");
   var result=attempt.RunOnce();var trace=attempt.Trace;
   Need(result.Status=="Known"&&!result.CacheHit&&trace.Parser.Status=="Known"&&
    trace.OwnArzReadCompleted&&trace.CacheCommitted,"complete unique pool reaches actual parser and cache publication");
   var table=(LoadTableBinarySnapshot)result.Table;var strings=table.Warm.StringPool;
   Need(trace.OwnArzRecord.StringCount==count&&strings.Count==count&&strings.IsUnique&&
    strings.IsRawBacked&&strings.EmptyId==4,"the entire original-ID pool is retained, including the original empty slot");
   Need(strings.MaterializedCount==0&&strings.MaterializedBytes==0,
    "initialization, complete-name indexing and parsing do not eagerly materialize strings");
   Need(strings.RetainedArrayPayloadBytes==12L*count&&strings.RawRetainedArrayPayloadBytes>0,
    "index/memo payload and raw ownership are separately accounted");
   Need(table.Warm.Names.Shift==2&&table.Warm.Names.UsedCount==1&&
    table.Warm.Names.Capacity==(uint)(count/2)&&table.Warm.Names.GetSlot(0).Address==EmptyAddress&&
    table.Warm.Names.GetSlot(0).CachedId==4,"derived pointer cache retains completed initial empty-lookup state");
   Need(table.Warm.Names.BucketCount==(count==5?4:1024),
    "normal non-boundary original count selects the saved initial sizing domain");
   var value=RecordTableGetter.GetValue(table,B("Class\0"),new TableString(B("fallback")),ClassAddress);
   byte[] expected=count==5?B("Item\0"):B("string-5000\0");
   Need(value.Status=="Known"&&!value.UsedDefault&&Same(value.Value.CopyCStringBytes(),expected)&&
    value.Binary.NameId==1&&value.Binary.StringIndex==(count==5?2:count-1),
    "real cold getter uses complete name lookup and unmodified original value IDs");
   Need(value.Binary.NameLookup!=null&&!value.Binary.NameLookup.NativeLayoutKnown&&
    value.CacheNodeInserted,"own complete-name projection is explicit while the actual pointer cache is updated");
   if(count>4096){
    var high=RecordTableGetter.GetValue(table,B("string-4999\0"),new TableString(B("fallback")),HighAddress);
    Need(high.Status=="Known"&&!high.UsedDefault&&high.Binary.NameId==4999&&
     high.Binary.StringIndex==2&&Same(high.Value.CopyCStringBytes(),B("Item\0")),
     "high original field-key ID resolves through the full name index without a trimmed pool");
    Need(strings.FindExact(B("string-5000"))==5000&&strings.FindExact(B("String-5000"))==-1,
     "full-pool lookup preserves exact bytes and case");
   }
   Need(strings.MaterializedCount>0&&strings.MaterializedCount<=2&&strings.MaterializedBytes<64,
    "only requested value strings are materialized in the first cold lookups");
   Need(depot.Cache.Count==1&&depot.LoadCount50==1&&depot.ElapsedTotal5C==7&&
    depot.ElapsedMaximum60==7&&!depot.RequiresResolution&&!trace.NativeArchiveRead&&result.LockReleased,
    "real fresh depot miss completes publication and converted tick accounting");
   input.Dispose();int accesses=stream.Accesses;
   var next=Attempt(depot,source);var hit=next.RunOnce();
   Need(stream.Closed&&hit.Status=="Known"&&hit.CacheHit&&Object.ReferenceEquals(hit.Table,table)&&
    !next.Trace.OwnArzReadAttempted&&stream.Accesses==accesses,
    "a new attempt hits the actual cache after the borrowed source is closed");
   var again=RecordTableGetter.GetValue(hit.Table,B("Class\0"),new TableString(B("fallback")),ClassAddress);
   Need(again.Status=="Known"&&Object.ReferenceEquals(again.Value,value.Value)&&strings.MaterializedCount<=3,
    "detached getter preserves stored string identity and lazy materialization");
   Need(Object.ReferenceEquals(attempt.RunOnce(),result)&&depot.LoadCount50==1&&depot.HitCount54==1&&
    stream.Accesses==accesses,"once replay does not reread or duplicate publication");
   Need(Same(fixture.Bytes,original),"complete synthetic input bytes remain unchanged");
  }
  scenarios++;
 }
 static void InitializationFailure(byte[][] pool,string failureStage,string reasonPrefix){
  var stream=new ObservedStream(Fixture(pool).Bytes);
  using(var input=new ResourceInput(stream)){
   var depot=Depot();var cache=depot.Cache;var attempt=Attempt(depot,Source(input));var result=attempt.RunOnce();var trace=attempt.Trace;
   Need(result.Status=="Unsupported"&&result.Reason.StartsWith(reasonPrefix,StringComparison.Ordinal)&&
    trace.FailureStage==failureStage,"initialization gap is reported at its actual guard");
   Need(trace.OwnArzReadCompleted&&trace.OwnArzRecord!=null&&trace.OwnArzRecord.StringCount==pool.Length,
    "failed generated context preserves the full detached raw read trace");
   Need(trace.Reader==null&&trace.Parser==null&&trace.Table==null&&trace.CallerPublication==null&&
    trace.Publication==null&&!trace.CacheCommitted,"initialization rejection occurs before parser/table/publication");
   Need(Object.ReferenceEquals(depot.Cache,cache)&&depot.Cache.Count==0&&depot.LoadCount50==0&&
    depot.HitCount54==0&&!depot.RequiresResolution&&!result.MayHaveEffects&&result.LockReleased,
    "initialization rejection does not contaminate cache or count a load");
   input.Dispose();int accesses=stream.Accesses;
   Need(Object.ReferenceEquals(attempt.RunOnce(),result)&&stream.Accesses==accesses,
    "failed initialization is sticky with a closed borrowed source");
  }
  scenarios++;
 }
 static void InitializationGaps(){
  var duplicate=Pool(6);duplicate[5]=B("Item");
  InitializationFailure(duplicate,"OwnArzFreshCompletePool","OwnArzRecordReadUnsupported:BinaryStringPoolDuplicateRawString");
  var noEmpty=Pool(5);noEmpty[4]=B("nonempty");
  InitializationFailure(noEmpty,"OwnArzFreshCompletePool","OwnArzRecordReadUnsupported:BinaryStringPoolMissingRawEmptyString");
  InitializationFailure(Pool(32),"OwnArzFreshPointerCache","OwnArzRecordReadUnsupported:OwnArzFreshPointerCacheOutsideCoveredDomain:");
 }
 static void ConstructionGuards(){
  var stream=new ObservedStream(new byte[0]);
  using(var input=new ResourceInput(stream)){
   ArzRawLimits[] bad={Limits(count:131073),Limits(wire:8388609),Limits(pool:4194305),
    Limits(packed:2097153),Limits(body:1048577),Limits(working:8388609)};
   foreach(var limits in bad){
    bool refused=false;try{ArzRecordBinarySource.ForFreshUniqueArchive(input,limits,new RegistrationGate(),EmptyAddress,1,2);}
    catch(ArgumentOutOfRangeException e){refused=e.ParamName=="readLimits";}
    Need(refused&&stream.Accesses==0,"fresh source budget is rejected before I/O");
   }
   bool zero=false;try{ArzRecordBinarySource.ForFreshUniqueArchive(input,Limits(),new RegistrationGate(),0,1,2);}
   catch(ArgumentOutOfRangeException e){zero=e.ParamName=="knownEmptyQueryAddress";}
   Need(zero,"unknown empty-query identity cannot be replaced with a null address");
   bool nullClock=false;try{ArzRecordBinarySource.ForFreshUniqueArchiveWithClock(input,Limits(),new RegistrationGate(),EmptyAddress,null);}
   catch(ArgumentNullException e){nullClock=e.ParamName=="captureClock";}
   Need(nullClock&&stream.Accesses==0,"clock profile requires its explicit callback without sampling");
   bool old=false;try{
    new ArzRecordBinarySource(input,Limits(count:5001,wire:131072,pool:131072),new RegistrationGate(),
     new BinaryPointerCacheSnapshot(0,0,new BinaryNameCacheEntry[0][]),
     new BinaryNameIdSnapshot(1,new BinaryNameIdEntry[0]),1,2);
   }catch(ArgumentOutOfRangeException e){old=e.ParamName=="readLimits";}
   Need(old&&stream.Accesses==0,"old source constructor retains its 4096-string budget");
  }
  scenarios++;
 }
 static void ExplicitClock(){
  var stream=new ObservedStream(Fixture(Pool(5)).Bytes);
  using(var input=new ResourceInput(stream)){
   int samples=0;var depot=Depot();
   Func<RecordLoadTickResult> clock=delegate{
    samples++;
    if(samples==1)Need(stream.Accesses==0&&depot.Cache.Count==0,"start clock precedes own raw acquisition");
    else Need(depot.LoadCount50==1&&depot.LastCompletedCache.Count==1,"end clock follows cache commit and load increment");
    return RecordLoadTicks.FromTimeGetTime((uint)(100+7*(samples-1)),0);
   };
   var source=ArzRecordBinarySource.ForFreshUniqueArchiveWithClock(input,Limits(),new RegistrationGate(),EmptyAddress,clock);
   var attempt=Attempt(depot,source);Need(samples==0&&stream.Accesses==0,"constructing an owned clock source does not invoke it");
   var result=attempt.RunOnce();
   Need(result.Status=="Known"&&samples==2&&attempt.Trace.StartTickConversion.Tick32==100&&
    attempt.Trace.EndTickConversion.Tick32==107&&depot.ElapsedTotal5C==7,
    "explicit converted clock results reach the real miss tail");
   input.Dispose();int accesses=stream.Accesses;
   Need(Attempt(depot,source).RunOnce().CacheHit&&samples==2&&stream.Accesses==accesses,
    "cache hit does not sample the owned clock or reopen input");
   Need(Object.ReferenceEquals(attempt.RunOnce(),result)&&samples==2,"once replay does not resample the clock");
  }
  scenarios++;
 }
 static void UnknownClock(bool atEnd){
  var stream=new ObservedStream(Fixture(Pool(5)).Bytes);
  using(var input=new ResourceInput(stream)){
   int samples=0;var depot=Depot();
   var source=ArzRecordBinarySource.ForFreshUniqueArchiveWithClock(input,Limits(),new RegistrationGate(),EmptyAddress,
    delegate{samples++;return atEnd&&samples==1?RecordLoadTicks.FromTimeGetTime(100,0):RecordLoadTicks.FromQpc(null,0,1,true);});
   var attempt=Attempt(depot,source);var result=attempt.RunOnce();var trace=attempt.Trace;
   Need(result.Status=="Unsupported"&&result.Reason==(atEnd?"UnknownConvertedMissEndTick":"UnknownConvertedMissStartTick"),
    "an unknown clock sample remains unknown instead of silently becoming zero");
   if(atEnd){
    Need(samples==2&&trace.CacheCommitted&&trace.LoadCountIncremented&&!trace.ElapsedRecorded&&
     depot.RequiresResolution&&depot.Cache==null&&depot.LastCompletedCache.Count==1&&
     depot.LoadCount50==1&&depot.ElapsedTotal5C==0,"unknown end preserves the actual completed publication prefix");
   }else{
    Need(samples==1&&stream.Accesses==0&&!trace.OwnArzReadAttempted&&trace.Parser==null&&
     depot.Cache.Count==0&&depot.LoadCount50==0&&!depot.RequiresResolution,
     "unknown start stops before source, parsing and cache changes");
   }
   Need(result.LockReleased,"clock gap releases the depot gate");
   input.Dispose();int accesses=stream.Accesses;
   Need(Object.ReferenceEquals(attempt.RunOnce(),result)&&stream.Accesses==accesses&&samples==(atEnd?2:1),
    "clock-gap replay preserves sample and publication counts");
  }
  scenarios++;
 }
 static int Main(){
  try{
   FullPoolLoadAndHit(5);FullPoolLoadAndHit(5001);InitializationGaps();ConstructionGuards();
   ExplicitClock();UnknownClock(false);UnknownClock(true);
   Console.WriteLine("{\"suite\":\"RecordLoadFromFreshArchiveTests\",\"status\":\"passed\",\"fixed_assertions\":"+assertions+
    ",\"scenarios\":"+scenarios+",\"scope\":\"own complete unique ARZ pools through derived initialization, parser, getter and cache; explicit owned clocks and retained failure prefixes; no native execution or official compatibility claim\"}");
   return 0;
  }catch(Exception error){Console.Error.WriteLine(error);return 1;}
 }
}
