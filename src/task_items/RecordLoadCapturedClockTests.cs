using System;
using System.IO;
using GrimDynasty.Resources;
using GrimDynasty.TaskItems;

// Complete own ARZv3 fixtures and pure raw-sample conversions only.
// Delegates below observe capture ordering; they do not call a clock API or
// restore native global initialization, API failure handling or exception ABI.
class RecordLoadCapturedClockTests {
 static int assertions,scenarios;
 const ulong EmptyQueryAddress=0x1008,ClassQueryAddress=0x2008;
 static void Need(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static byte[] B(string text){return ResourceUtil.Utf8.GetBytes(text);}
 static bool Same(byte[] a,byte[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
 static ObjectNameString Name(string text){byte[] bytes=B(text);return new ObjectNameString(bytes,(ulong)Math.Max(15,bytes.Length));}
 static byte[] Body(){
  return ArzRawFixture.Join(ArzRawFixture.Field(2,1,2),
   ArzRawFixture.Field(0,3,0xdeadbeefU,0x80000000U));
 }
 static ArzRawFixtureData Fixture(){
  var pool=new[]{B("dir/file"),B("Class"),B("Item"),B("raw"),new byte[0]};
  return ArzRawFixture.Build(pool,new ArzRawFixtureRecord(0,Body()));
 }
 static ArzRawLimits Limits(){return new ArzRawLimits(1,5,1024,1024,64,1024,1024,1024,8192);}
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
 static ArzRecordBinarySource Source(ResourceInput input,Func<RecordLoadTickResult> capture){
  return ArzRecordBinarySource.ForFreshUniqueArchiveWithClock(
   input,Limits(),new RegistrationGate(),EmptyQueryAddress,capture);
 }
 static RecordLoadFromSuppliedBinaryAttempt Attempt(RecordDepotState depot,ArzRecordBinarySource source){
  return RecordLoadFromSuppliedBinaryAttempt.FromArzRecord(depot,Name("Dir\\FILE"),RecordNameLocale.KnownC,source);
 }
 static RecordTableGetterResult Class(object table){
  return RecordTableGetter.GetValue(table,B("Class\0"),new TableString(B("fallback")),ClassQueryAddress);
 }
 static void FirstMissThenDetachedHit(){
  var stream=new ObservedStream(Fixture().Bytes);
  using(var input=new ResourceInput(stream)){
   var depot=RecordDepotInitialization.CreateFresh(new RegistrationGate());
   int calls=0;bool startBeforeRead=false,endAfterCommit=false;object tableAtEnd=null;
   RecordLoadTickResult start=null,end=null;
   Func<RecordLoadTickResult> capture=delegate{
    calls++;
    if(calls==1){
     startBeforeRead=stream.Accesses==0&&depot.LastCompletedCache.Count==0&&depot.LoadCount50==0&&
      !depot.RequiresResolution&&depot.Gate.IsHeldByCurrentThread;
     return start=Start();
    }
    if(calls==2){
     var found=depot.LastCompletedCache.Find(B("dir/file"));
     tableAtEnd=found.Found?found.Entry.Table:null;
     endAfterCommit=depot.LastCompletedCache.Count==1&&tableAtEnd!=null&&depot.LoadCount50==1&&
      depot.ElapsedTotal5C==0&&depot.ElapsedMaximum60==0&&depot.Gate.IsHeldByCurrentThread;
     return end=End();
    }
    throw new InvalidOperationException("Clock unexpectedly sampled again");
   };
   var source=Source(input,capture);
   Need(calls==0&&stream.Accesses==0,"source construction neither captures a clock nor reads the archive");
   var attempt=Attempt(depot,source);
   Need(calls==0&&!attempt.Attempted&&stream.Accesses==0,"attempt construction is also lazy");
   var result=attempt.RunOnce();var trace=attempt.Trace;
   Need(result.Status=="Known"&&!result.CacheHit&&calls==2&&startBeforeRead&&endAfterCommit,"actual miss captures start before I/O and end after cache commit and load increment");
   Need(start.Status==RecordLoadTickStatus.Known&&start.Tick32==333&&end.Status==RecordLoadTickStatus.Known&&end.Tick32==1000&&
    Object.ReferenceEquals(trace.StartTickConversion,start)&&Object.ReferenceEquals(trace.EndTickConversion,end),"trace retains the exact independently converted raw QPC results");
   Need(trace.OwnArzReadCompleted&&trace.OwnArzRecord.StringCount==5&&Same(trace.OwnArzRecord.CopyBodyBytes(),Body())&&
    trace.Parser.Status=="Known"&&trace.Parser.CompletedFields==2&&stream.ReadCalls>0,"complete unique-pool ARZ supplies the actual two-field parser");
   Need(Object.ReferenceEquals(result.Table,tableAtEnd)&&Object.ReferenceEquals(trace.Table,result.Table)&&
    trace.CacheCommitted&&trace.LoadCountIncremented&&trace.ElapsedRecorded&&trace.Elapsed==667,
    "end callback observes the same completed core table later returned by the wrapper");
   var getter=Class(result.Table);
   Need(getter.Status=="Known"&&!getter.UsedDefault&&Same(getter.Value.CopyCStringBytes(),B("Item\0")),
    "fresh unique archive context drives the real Class getter");
   Need(depot.LoadCount50==1&&depot.HitCount54==0&&depot.ElapsedTotal5C==667&&depot.ElapsedMaximum60==667&&
    !depot.RequiresResolution&&trace.ReaderDisposed&&result.LockReleased&&!depot.Gate.IsHeldByCurrentThread,
    "successful captures complete the miss tail and release own state");
   input.Dispose();int accesses=stream.Accesses;
   Need(stream.Closed&&calls==2,"closing the borrowed source does not sample the clock");
   var hitAttempt=Attempt(depot,source);
   Need(calls==2&&stream.Accesses==accesses,"new attempt construction against the closed source remains lazy");
   var hit=hitAttempt.RunOnce();
   Need(hit.Status=="Known"&&hit.CacheHit&&Object.ReferenceEquals(hit.Table,result.Table)&&
    !hitAttempt.Trace.Entered&&!hitAttempt.Trace.OwnArzReadAttempted&&hitAttempt.Trace.StartTickConversion==null&&
    hitAttempt.Trace.EndTickConversion==null&&calls==2&&stream.Accesses==accesses,
    "same-key hit keeps table identity without reading or capturing either endpoint");
   Need(Object.ReferenceEquals(attempt.RunOnce(),result)&&Object.ReferenceEquals(hitAttempt.RunOnce(),hit)&&
    calls==2&&stream.Accesses==accesses&&depot.LoadCount50==1&&depot.HitCount54==1&&depot.ElapsedTotal5C==667,
    "original and hit replay cannot recapture or repeat counters");
   var ordinary=RecordLoadFile.Apply(depot,Name("DIR\\FILE"),RecordNameLocale.KnownC);
   Need(ordinary.Status=="Known"&&ordinary.CacheHit&&Object.ReferenceEquals(ordinary.Table,result.Table)&&
    calls==2&&stream.Accesses==accesses&&depot.HitCount54==2,"ordinary cache-only hit has no clock dependency");
  }
  scenarios++;
 }
 // Each failure kind uses a distinct fresh depot and complete own archive.
 // 0=unknown slot, 1=predicted IDIV fault, 2=own delegate exception.
 static RecordLoadTickResult FailureValue(int kind,bool ending){
  if(kind==0)return RecordLoadTicks.FromQpc(null,0,3,false);
  if(kind==1)return ending?RecordLoadTicks.FromQpc(1152921504606846976L,0,-1,true):
   RecordLoadTicks.FromQpc(1,0,0,true);
  return null;
 }
 static string ExpectedReason(int kind,bool ending){
  if(kind==0)return ending?"UnknownConvertedMissEndTick":"UnknownConvertedMissStartTick";
  if(kind==1)return ending?"CapturedMissEndTickDivideFault:SignedDivisionOverflow":
   "CapturedMissStartTickDivideFault:SignedDivisionByZero";
  return ending?"OwnSuppliedBinaryMissOperationThrew:System.InvalidOperationException":
   "OwnMissStartClockThrew:System.InvalidOperationException";
 }
 static void StartFailure(int kind){
  var stream=new ObservedStream(Fixture().Bytes);
  using(var input=new ResourceInput(stream)){
   var depot=RecordDepotInitialization.CreateFresh(new RegistrationGate());var old=depot.Cache;
   int calls=0;bool beforeRead=false;var expected=FailureValue(kind,false);
   var source=Source(input,delegate{
    calls++;beforeRead=stream.Accesses==0&&depot.LastCompletedCache.Count==0&&depot.LoadCount50==0;
    if(kind==2)throw new InvalidOperationException("Own start clock failure");
    return expected;
   });
   var attempt=Attempt(depot,source);
   Need(calls==0&&stream.Accesses==0,"failure source and attempt construction remain lazy");
   var result=attempt.RunOnce();var trace=attempt.Trace;
   Need(result.Status=="Unsupported"&&result.Reason==ExpectedReason(kind,false)&&trace.FailureStage=="CapturedMissStartTick"&&
    calls==1&&beforeRead&&stream.Accesses==0&&!trace.OwnArzReadAttempted,
    "start refusal or exception stops before archive I/O: "+kind);
   Need(Object.ReferenceEquals(trace.StartTickConversion,expected)&&trace.EndTickConversion==null&&
    (kind==2||!trace.StartTickConversion.Tick32.HasValue),"start trace preserves the returned conversion or absence after a throw");
   Need(trace.Reader==null&&trace.Parser==null&&trace.Table==null&&trace.CallerPublication==null&&
    !trace.CacheCommitted&&!result.MayHaveEffects&&!depot.RequiresResolution&&Object.ReferenceEquals(depot.Cache,old)&&old.Count==0,
    "failed start leaves the core unentered and cache usable");
   Need(depot.LoadCount50==0&&depot.HitCount54==0&&depot.ElapsedTotal5C==0&&depot.ElapsedMaximum60==0&&
    result.LockReleased&&!depot.Gate.IsHeldByCurrentThread,"start failure changes neither statistics nor gate ownership");
   input.Dispose();int accesses=stream.Accesses;
   Need(Object.ReferenceEquals(attempt.RunOnce(),result)&&calls==1&&stream.Accesses==accesses,
    "start failure replay neither recaptures nor reads a closed source");
  }
  scenarios++;
 }
 static void EndFailure(int kind){
  var stream=new ObservedStream(Fixture().Bytes);
  using(var input=new ResourceInput(stream)){
   var depot=RecordDepotInitialization.CreateFresh(new RegistrationGate());var old=depot.Cache;
   int calls=0;bool beforeRead=false,afterCommit=false;object tableAtEnd=null;
   RecordLoadTickResult start=null;var expected=FailureValue(kind,true);
   var source=Source(input,delegate{
    calls++;
    if(calls==1){
     beforeRead=stream.Accesses==0&&depot.LastCompletedCache.Count==0&&depot.LoadCount50==0;
     return start=Start();
    }
    var found=depot.LastCompletedCache.Find(B("dir/file"));tableAtEnd=found.Found?found.Entry.Table:null;
    afterCommit=depot.LastCompletedCache.Count==1&&tableAtEnd!=null&&depot.LoadCount50==1&&
     depot.ElapsedTotal5C==0&&depot.ElapsedMaximum60==0;
    if(kind==2)throw new InvalidOperationException("Own end clock failure");
    return expected;
   });
   var attempt=Attempt(depot,source);
   Need(calls==0&&stream.Accesses==0,"end-failure configuration does not eagerly sample or read");
   var result=attempt.RunOnce();var trace=attempt.Trace;
   Need(result.Status=="Unsupported"&&result.Reason==ExpectedReason(kind,true)&&trace.FailureStage=="CapturedMissEndTick"&&
    calls==2&&beforeRead&&afterCommit,"end failure occurs after the actual committed core: "+kind);
   Need(Object.ReferenceEquals(trace.StartTickConversion,start)&&start.Tick32==333&&
    Object.ReferenceEquals(trace.EndTickConversion,expected)&&(kind==2||!trace.EndTickConversion.Tick32.HasValue),
    "end trace retains known start and exact failed conversion without inventing a zero endpoint");
   Need(trace.OwnArzReadCompleted&&trace.Parser.Status=="Known"&&trace.Parser.CompletedFields==2&&
    trace.Publication.Status=="Known"&&trace.CacheCommitted&&trace.LoadCountIncremented&&
    Object.ReferenceEquals(trace.Table,tableAtEnd),"failed timing does not erase completed parse, publication or table identity");
   Need(depot.LoadCount50==1&&depot.HitCount54==0&&depot.ElapsedTotal5C==0&&depot.ElapsedMaximum60==0&&
    !trace.ElapsedRecorded&&!trace.Elapsed.HasValue,"end failure preserves load count while leaving elapsed unwritten");
   Need(result.Table==null&&result.MayHaveEffects&&depot.RequiresResolution&&depot.Cache==null&&
    depot.LastCompletedCache.Count==1&&old.Count==0&&Object.ReferenceEquals(depot.PendingMiss,trace)&&
    Object.ReferenceEquals(depot.LastCompletedCache.Find(B("dir/file")).Entry.Table,trace.Table),
    "committed prefix remains diagnostic pending state rather than a false successful outer result");
   Need(trace.ReaderDisposed&&result.LockReleased&&!depot.Gate.IsHeldByCurrentThread&&Class(trace.Table).Status=="Known",
    "own cleanup releases the gate and the retained completed table remains inspectable");
   input.Dispose();int accesses=stream.Accesses;var blocked=Attempt(depot,source);var blockedResult=blocked.RunOnce();
   Need(blockedResult.Status=="Unsupported"&&blockedResult.Reason=="RecordDepotMissRequiresResolution"&&
    !blocked.Trace.Entered&&!blocked.Trace.OwnArzReadAttempted&&calls==2&&stream.Accesses==accesses,
    "pending tail blocks later hit attempts without clock capture or source access");
   Need(Object.ReferenceEquals(attempt.RunOnce(),result)&&Object.ReferenceEquals(blocked.RunOnce(),blockedResult)&&
    calls==2&&stream.Accesses==accesses&&depot.LoadCount50==1&&depot.LastCompletedCache.Count==1,
    "end failure and blocked-hit replay preserve the prefix without recapturing");
  }
  scenarios++;
 }
 static void Main(){
  FirstMissThenDetachedHit();
  for(int kind=0;kind<3;kind++){StartFailure(kind);EndFailure(kind);}
  Console.WriteLine("RecordLoadCapturedClockTests: "+assertions+" assertions passed; "+scenarios+" scenarios");
 }
}
