using System;
using System.IO;
using GrimDynasty.Resources;
using GrimDynasty.TaskItems;

// Fresh depot integration with explicit own archive context and raw clock
// samples. No native clock, archive initialization, allocation or game runs.
class RecordDepotInitializationTests {
 static int assertions,scenarios;
 const ulong QueryAddress=0x12345008;
 static void Need(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static byte[] B(string value){return ResourceUtil.Utf8.GetBytes(value);}
 static bool Same(byte[] a,byte[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
 static ObjectNameString Name(string value){byte[] b=B(value);return new ObjectNameString(b,(ulong)Math.Max(15,b.Length));}
 static byte[][] Pool(){return new[]{B("dir/file"),B("Class"),B("Item"),B("raw"),new byte[0]};}
 static byte[] Body(){
  return ArzRawFixture.Join(ArzRawFixture.Field(2,1,2),
   ArzRawFixture.Field(0,3,0xdeadbeefU,0x80000000U));
 }
 static ArzRawFixtureData Fixture(){return ArzRawFixture.Build(Pool(),new ArzRawFixtureRecord(0,Body()));}
 static ArzRawLimits Limits(){return new ArzRawLimits(1,5,1024,1024,64,1024,1024,1024,8192);}
 sealed class Context {
  public readonly RegistrationGate Gate=new RegistrationGate();
  public readonly TableString[] Strings;
  public readonly BinaryNameIdSnapshot Ids;
  // Complete explicitly supplied own storage, not a recovered archive ctor.
  public readonly BinaryPointerCacheSnapshot Names=new BinaryPointerCacheSnapshot(
   0,0,new[]{new BinaryNameCacheEntry[0]},0,8,new BinaryNameCacheEntry[8]);
  public Context(){
   byte[][] pool=Pool();Strings=new TableString[pool.Length];var entries=new BinaryNameIdEntry[pool.Length];
   for(int i=0;i<pool.Length;i++){Strings[i]=new TableString(pool[i]);entries[i]=new BinaryNameIdEntry(pool[i],i);}
   // One bucket contains every unique pool entry, including record/empty names.
   Ids=new BinaryNameIdSnapshot(1,entries);
  }
 }
 sealed class ObservedStream:Stream {
  readonly MemoryStream inner;
  public int Accesses,ReadCalls;
  public bool Closed;
  public ObservedStream(byte[] bytes){inner=new MemoryStream(bytes,false);}
  public override bool CanRead{get{return inner.CanRead;}}
  public override bool CanSeek{get{return inner.CanSeek;}}
  public override bool CanWrite{get{return false;}}
  public override long Length{get{Accesses++;return inner.Length;}}
  public override long Position{get{Accesses++;return inner.Position;}set{Accesses++;inner.Position=value;}}
  public override int Read(byte[] bytes,int offset,int count){Accesses++;ReadCalls++;return inner.Read(bytes,offset,count);}
  public override long Seek(long offset,SeekOrigin origin){Accesses++;return inner.Seek(offset,origin);}
  public override void Flush(){}
  public override void SetLength(long value){throw new NotSupportedException();}
  public override void Write(byte[] bytes,int offset,int count){throw new NotSupportedException();}
  protected override void Dispose(bool disposing){Closed=true;if(disposing)inner.Dispose();base.Dispose(disposing);}
 }
 static RecordLoadTickResult Start(){return RecordLoadTicks.FromQpc(1,0,3,true);}
 static RecordLoadTickResult End(){return RecordLoadTicks.FromQpc(3,0,3,true);}
 static RecordLoadTickResult Unknown(){return RecordLoadTicks.FromQpc(null,0,3,false);}
 static SuppliedBinaryRecord Supplied(Context context,RecordLoadTickResult start,RecordLoadTickResult end){
  byte[] body=Body();
  return new SuppliedBinaryRecord(B("dir/file"),body,0,body.Length,context.Gate,context.Names,
   context.Strings,context.Ids,start.Tick32,end.Tick32);
 }
 static ArzRecordBinarySource Source(ResourceInput input,Context context,RecordLoadTickResult start,RecordLoadTickResult end){
  return new ArzRecordBinarySource(input,Limits(),context.Gate,context.Names,context.Ids,
   start.Tick32,end.Tick32,ArzRawDecodeProfile.ObservedEngineOutputSize);
 }
 static RecordLoadFromSuppliedBinaryAttempt ArzAttempt(RecordDepotState depot,ArzRecordBinarySource source){
  return RecordLoadFromSuppliedBinaryAttempt.FromArzRecord(depot,Name("Dir\\FILE"),RecordNameLocale.KnownC,source);
 }
 static RecordTableGetterResult Class(object table){
  return RecordTableGetter.GetValue(table,B("Class\0"),new TableString(B("fallback")),QueryAddress);
 }
 static void Initialization(){
  bool rejected=false;try{RecordDepotInitialization.CreateFresh(null);}catch(ArgumentNullException){rejected=true;}
  Need(rejected,"fresh creation requires an explicit own gate");
  var gate=new RegistrationGate();var depot=RecordDepotInitialization.CreateFresh(gate);
  Need(Object.ReferenceEquals(depot.Gate,gate)&&!gate.IsHeldByCurrentThread,"fresh creation preserves the supplied gate without executing a lock API");
  Need(depot.Cache!=null&&depot.Cache.Count==0&&depot.Cache.BucketCount==8&&depot.BucketCapacity==8&&depot.MaxLoadFactor==1.0f,"evidenced empty container has eight buckets/capacity and load factor one");
  Need(depot.LoadCount50==0&&depot.HitCount54==0&&depot.ElapsedTotal5C==0&&depot.ElapsedMaximum60==0,"evidenced constructor writes zero all four modeled statistics");
  Need(!depot.RequiresResolution&&depot.PendingMiss==null&&depot.PendingPublication==null&&depot.CurrentCacheCount==0&&Object.ReferenceEquals(depot.Cache,depot.LastCompletedCache),"fresh own state has no pending publication or hidden cache");
  var other=RecordDepotInitialization.CreateFresh(gate);
  Need(!Object.ReferenceEquals(depot,other)&&!Object.ReferenceEquals(depot.Cache,other.Cache)&&Object.ReferenceEquals(other.Gate,gate),"separate fresh states do not share cache storage even when a caller explicitly shares a gate");
  Need(!depot.Cache.Find(B("dir/file")).Found&&!depot.Cache.Find(new byte[0]).Found,"empty native list projection does not invent a fallback cache entry");
 }
 static void SuppliedMissAndHitScenario(){
  var depot=RecordDepotInitialization.CreateFresh(new RegistrationGate());var untouched=RecordDepotInitialization.CreateFresh(new RegistrationGate());
  var context=new Context();var start=Start();var end=End();
  Need(start.Status==RecordLoadTickStatus.Known&&start.Tick32==333&&end.Status==RecordLoadTickStatus.Known&&end.Tick32==1000,"explicit raw samples supply separately converted ticks");
  var original=Name("Dir\\FILE");var attempt=new RecordLoadFromSuppliedBinaryAttempt(depot,original,RecordNameLocale.KnownC,Supplied(context,start,end));
  var result=attempt.RunOnce();var trace=attempt.Trace;
  Need(result.Status=="Known"&&!result.CacheHit&&Object.ReferenceEquals(result.Table,trace.Table)&&trace.Parser.Status=="Known"&&trace.Parser.CompletedFields==2,"first supplied miss on fresh depot actually parses and publishes the table");
  Need(trace.Parser.Record.CopyValues()[0]==2&&trace.Parser.Record.CopyRawValues(0)[0]==0xdeadbeefU&&trace.Parser.Record.CopyRawValues(0)[1]==0x80000000U,"two-field parser preserves string ID and raw words");
  var getter=Class(result.Table);
  Need(getter.Status=="Known"&&!getter.UsedDefault&&Object.ReferenceEquals(getter.Value,context.Strings[2])&&getter.Binary.Cold&&getter.Binary.NameLookup.Found&&getter.Binary.NameLookup.Entry.Id==1,"cold getter uses the complete explicit name index and preserves shared pool identity");
  Need(context.Names.UsedCount==1&&context.Names.GetSlot(0).CachedId==1&&getter.CacheNodeInserted&&getter.GateReleased&&!context.Gate.IsHeldByCurrentThread,"first actual getter populates the independently supplied own name cache");
  Need(depot.Cache.Count==1&&depot.LoadCount50==1&&depot.HitCount54==0&&depot.ElapsedTotal5C==667&&depot.ElapsedMaximum60==667&&trace.Elapsed==667,"fresh statistics record the evidenced difference of separately truncated ticks");
  Need(trace.ReaderDisposed&&!depot.RequiresResolution&&trace.Publication.Status=="Known"&&result.LockReleased&&!depot.Gate.IsHeldByCurrentThread&&Same(original.CopyBytes(),B("Dir\\FILE")),"completed miss releases own gates and leaves requested storage intact");
  var hit=RecordLoadFile.Apply(depot,Name("DIR\\FILE"),RecordNameLocale.KnownC);
  Need(hit.Status=="Known"&&hit.CacheHit&&Object.ReferenceEquals(hit.Table,result.Table)&&depot.HitCount54==1&&depot.LoadCount50==1&&depot.ElapsedTotal5C==667,"ordinary repeat hits the newly published table without a default miss loader");
  Need(Object.ReferenceEquals(attempt.RunOnce(),result)&&depot.HitCount54==1&&depot.LoadCount50==1,"same attempt replay does not repeat parsing, publication or statistics");
  Need(untouched.Cache.Count==0&&untouched.LoadCount50==0&&untouched.HitCount54==0&&!untouched.RequiresResolution,"one initialized depot cannot mutate another fresh depot");
  scenarios++;
 }
 static void OwnArzMissAndDetachedHitScenario(){
  var fixture=Fixture();var stream=new ObservedStream(fixture.Bytes);var context=new Context();
  using(var input=new ResourceInput(stream)){
   var depot=RecordDepotInitialization.CreateFresh(new RegistrationGate());var source=Source(input,context,Start(),End());
   var attempt=ArzAttempt(depot,source);
   Need(stream.Accesses==0&&!attempt.Attempted,"explicit archive source and fresh attempt construction do not read the borrowed stream");
   var result=attempt.RunOnce();var trace=attempt.Trace;
   Need(result.Status=="Known"&&trace.OwnArzReadCompleted&&stream.ReadCalls>0&&trace.OwnArzRecord.StringCount==5&&Same(trace.OwnArzRecord.CopyBodyBytes(),Body()),"complete own ARZv3 supplies the real selected compressed body and all five original pool IDs");
   Need(trace.OwnArzRecord.DecodeProfile==ArzRawDecodeProfile.ObservedEngineOutputSize&&trace.OwnArzRecord.PackedBytesConsumed==(int)trace.OwnArzRecord.PackedBytes&&trace.Parser.CompletedFields==2,"explicit output-size profile and actual two-pass parser complete the literal fixture");
   var getter=Class(result.Table);
   Need(getter.Status=="Known"&&!getter.UsedDefault&&Same(getter.Value.CopyCStringBytes(),B("Item\0"))&&getter.Binary.Cold&&getter.Binary.NameLookup.Entry.Id==1,"ARZ-projected shared pool and complete own names drive the actual Class getter");
   Need(depot.LoadCount50==1&&depot.HitCount54==0&&depot.ElapsedTotal5C==667&&depot.ElapsedMaximum60==667&&trace.CacheCommitted&&!trace.NativeArchiveRead&&!source.NativeArchiveFetchRecovered,"fresh depot records its first own-format load without claiming native archive initialization or fetch");
   input.Dispose();int accesses=stream.Accesses;
   Need(stream.Closed&&trace.ReaderDisposed&&trace.Reader.IsDisposed,"caller closes the borrowed source after the distinct own parser reader is cleaned up");
   var hitAttempt=ArzAttempt(depot,source);var hit=hitAttempt.RunOnce();
   Need(hit.Status=="Known"&&hit.CacheHit&&Object.ReferenceEquals(hit.Table,result.Table)&&!hitAttempt.Trace.Entered&&!hitAttempt.Trace.OwnArzReadAttempted&&stream.Accesses==accesses,"repeat hit after source disposal cannot perform another archive read");
   Need(depot.HitCount54==1&&depot.LoadCount50==1&&depot.ElapsedTotal5C==667&&Object.ReferenceEquals(attempt.RunOnce(),result)&&stream.Accesses==accesses,"detached hit and original-attempt replay preserve the single load and timing record");
  }
  scenarios++;
 }
 static void InvalidStartNoReadScenario(){
  var fixture=Fixture();var stream=new ObservedStream(fixture.Bytes);
  using(var input=new ResourceInput(stream)){
   var depot=RecordDepotInitialization.CreateFresh(new RegistrationGate());var old=depot.Cache;var start=Unknown();
   Need(start.Status==RecordLoadTickStatus.Unsupported&&!start.Tick32.HasValue,"unknown raw QPC slot remains an unknown converted start");
   var attempt=ArzAttempt(depot,Source(input,new Context(),start,End()));var result=attempt.RunOnce();var trace=attempt.Trace;
   Need(result.Status=="Unsupported"&&result.Reason=="UnknownConvertedMissStartTick"&&stream.Accesses==0&&!trace.OwnArzReadAttempted,"invalid start stops the real miss before every archive access");
   Need(trace.Reader==null&&trace.Parser==null&&trace.Table==null&&trace.CallerPublication==null&&!result.MayHaveEffects,"start guard creates neither parsed table nor publication prefix");
   Need(Object.ReferenceEquals(depot.Cache,old)&&depot.Cache.Count==0&&!depot.RequiresResolution&&depot.PendingMiss==null&&depot.LoadCount50==0&&depot.HitCount54==0&&depot.ElapsedTotal5C==0&&depot.ElapsedMaximum60==0,"invalid start preserves the fully usable empty initialized state");
   Need(result.LockReleased&&!depot.Gate.IsHeldByCurrentThread&&Object.ReferenceEquals(attempt.RunOnce(),result)&&stream.Accesses==0,"failed attempt releases the gate and does not retry the source");
   var recovered=ArzAttempt(depot,Source(input,new Context(),Start(),End())).RunOnce();
   Need(recovered.Status=="Known"&&depot.Cache.Count==1&&depot.LoadCount50==1&&depot.ElapsedTotal5C==667,"a distinct valid attempt can use the same unaffected fresh depot");
  }
  scenarios++;
 }
 static void UnknownEndAfterCommitScenario(){
  var fixture=Fixture();var stream=new ObservedStream(fixture.Bytes);
  using(var input=new ResourceInput(stream)){
   var depot=RecordDepotInitialization.CreateFresh(new RegistrationGate());var old=depot.Cache;var end=Unknown();
   Need(end.Status==RecordLoadTickStatus.Unsupported&&!end.Tick32.HasValue,"end conversion keeps unknown raw slot distinct from a valid zero");
   var source=Source(input,new Context(),Start(),end);var attempt=ArzAttempt(depot,source);
   var result=attempt.RunOnce();var trace=attempt.Trace;
   Need(result.Status=="Unsupported"&&result.Reason=="UnknownConvertedMissEndTick"&&trace.FailureStage=="ConvertedMissEndTick"&&trace.OwnArzReadCompleted&&trace.Parser.Status=="Known","unknown end is encountered only after the actual ARZ parser and publication");
   Need(trace.CacheCommitted&&trace.LoadCountIncremented&&!trace.ElapsedRecorded&&!trace.Elapsed.HasValue&&depot.LoadCount50==1&&depot.HitCount54==0&&depot.ElapsedTotal5C==0&&depot.ElapsedMaximum60==0,"end guard preserves the first load but does not invent elapsed statistics");
   Need(depot.RequiresResolution&&depot.Cache==null&&depot.LastCompletedCache.Count==1&&old.Count==0&&Object.ReferenceEquals(depot.PendingMiss,trace)&&Object.ReferenceEquals(depot.LastCompletedCache.Find(B("dir/file")).Entry.Table,trace.Table),"committed diagnostic cache differs from the old empty snapshot and remains hidden as live state");
   Need(result.Table==null&&result.MayHaveEffects&&trace.ReaderDisposed&&result.LockReleased&&!depot.Gate.IsHeldByCurrentThread&&Class(trace.Table).Status=="Known","inspectable retained table and cleanup do not turn incomplete timing into successful outer return");
   input.Dispose();int accesses=stream.Accesses;var blocked=ArzAttempt(depot,source);var blockedResult=blocked.RunOnce();
   Need(blockedResult.Status=="Unsupported"&&blockedResult.Reason=="RecordDepotMissRequiresResolution"&&!blocked.Trace.Entered&&!blocked.Trace.OwnArzReadAttempted&&stream.Accesses==accesses,"pending end state prevents even a same-key hit without rereading the closed source");
   Need(Object.ReferenceEquals(attempt.RunOnce(),result)&&depot.LoadCount50==1&&depot.HitCount54==0&&depot.LastCompletedCache.Count==1&&stream.Accesses==accesses,"end failure replay retains the committed prefix without doubling it");
  }
  scenarios++;
 }
 static void Main(){
  Initialization();SuppliedMissAndHitScenario();OwnArzMissAndDetachedHitScenario();
  InvalidStartNoReadScenario();UnknownEndAfterCommitScenario();
  Console.WriteLine("RecordDepotInitializationTests: "+assertions+" assertions passed; "+scenarios+" scenarios");
 }
}
