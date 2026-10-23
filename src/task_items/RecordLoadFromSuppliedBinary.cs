using System;
using System.IO;
using GrimDynasty.Resources;
namespace GrimDynasty.TaskItems {
 public sealed class RecordSuppliedBinaryMissTrace {
  readonly string[] stages=new string[16];
  int stageCount;
  public string LastCompletedStage {get;internal set;}
  public string FailureStage {get;internal set;}
  public string Reason {get;internal set;}
  public string CleanupReason {get;internal set;}
  public bool Entered {get;internal set;}
  public bool ReaderDisposed {get;internal set;}
  public bool CacheCommitted {get;internal set;}
  public bool LoadCountIncremented {get;internal set;}
  public bool ElapsedRecorded {get;internal set;}
  // Optional explicitly supplied own clock samples. These are not readings
  // of Engine's globals or evidence that its clock initializer was executed.
  public RecordLoadTickResult StartTickConversion {get;internal set;}
  public RecordLoadTickResult EndTickConversion {get;internal set;}
  public bool RequiresResolution {get;internal set;}
  public bool UncertainEffects {get;internal set;}
  public bool WrapperReportingInterrupted {get;internal set;}
  // Own file reads can advance the borrowed stream even if format validation
  // fails. They are separate from modeled parser/cache mutation and native I/O.
  public bool OwnArzReadAttempted {get;internal set;}
  public ArzRawRecord OwnArzRecord {get;internal set;}
  public bool OwnArzReadCompleted {get{return OwnArzRecord!=null;}}
  byte[] ownRemapScratch;
  public bool OwnRemapAttempted {get{return ownRemapScratch!=null;}}
  public RecordStringRemapResult OwnRemapResult {get;internal set;}
  public bool OwnRemapStateUncertain {get;internal set;}
  // These are observable writes to this attempt's private body, separate from
  // MayHaveEffects on parser/cache/depot state. No native scratch is modeled.
  public bool OwnRemapMayHaveWrites {get{return OwnRemapStateUncertain||
   (OwnRemapResult!=null&&OwnRemapResult.WordsWritten!=0);}}
  public byte[] CopyOwnRemapBody(){return ownRemapScratch==null?null:(byte[])ownRemapScratch.Clone();}
  internal void BeginOwnRemap(byte[] ownedBody){ownRemapScratch=ownedBody;OwnRemapStateUncertain=true;}
  public uint? Elapsed {get;internal set;}
  public BinaryRecordReader Reader {get;internal set;}
  public BinaryRecordParseResult Parser {get;internal set;}
  public LoadTableBinarySnapshot Table {get;internal set;}
  public RecordCacheNodePair TemporaryPair {get;internal set;}
  public RecordCacheCallerPublicationAttempt CallerPublication {get;internal set;}
  public RecordCacheCallerPublicationResult Publication {get;internal set;}
  public bool NativeArchiveRead {get{return false;}}
  public bool NativeFactoryRecovered {get{return false;}}
  public bool MayHaveEffects {get{return UncertainEffects||(Parser!=null&&Parser.MayHaveEffects)||
   (Publication!=null&&Publication.MayHaveEffects)||
   (CallerPublication!=null&&CallerPublication.RequiresResolution)||
   CacheCommitted||LoadCountIncremented||ElapsedRecorded;}}
  internal RecordSuppliedBinaryMissTrace(){LastCompletedStage="BeforeSuppliedMiss";}
  internal void Completed(string stage){stages[stageCount++]=stage;LastCompletedStage=stage;}
  public string[] CopyCompletedStages(){var copy=new string[stageCount];Array.Copy(stages,copy,stageCount);return copy;}
 }
 // A conditional integration at the explicit verified-input boundary, not a
 // recovered archive fetch, native Binary ctor or complete general miss loader.
 // This reuses the real RecordLoadFile wrapper/gate/normalization and updates the
 // same depot, so later ordinary Apply calls see the actual completed cache.
 public sealed class RecordLoadFromSuppliedBinaryAttempt {
  readonly RecordDepotState state;
  readonly ObjectNameString requested;
  readonly RecordNameLocale locale;
  SuppliedBinaryRecord input;
  readonly ArzRecordBinarySource archiveSource;
  readonly RecordSuppliedBinaryMissTrace trace=new RecordSuppliedBinaryMissTrace();
  RecordLoadResult result;
  bool attempted;
  // Written before allocating the wrapper result. A reporting allocation must
  // not erase an already incremented hit counter or invite a second execution.
  bool wrapperCaptured,wrapperAcquired,wrapperReleased,wrapperCounted;
  string wrapperReason,wrapperLast,wrapperFailure;
  RecordCacheLookupResult wrapperLookup;
  byte[] wrapperName;
  ulong wrapperCapacity;
  public bool Attempted {get{return attempted;}}
  public RecordSuppliedBinaryMissTrace Trace {get{return trace;}}
  public RecordLoadFromSuppliedBinaryAttempt(RecordDepotState depot,ObjectNameString requestedName,
   RecordNameLocale nameLocale,SuppliedBinaryRecord suppliedRecord){
   state=depot;requested=requestedName;locale=nameLocale;input=suppliedRecord;
  }
  RecordLoadFromSuppliedBinaryAttempt(RecordDepotState depot,ObjectNameString requestedName,
   RecordNameLocale nameLocale,ArzRecordBinarySource source,bool fromArchive){
   state=depot;requested=requestedName;locale=nameLocale;archiveSource=source;
  }
  // This optional own-format source is invoked lazily by the actual cache-miss
  // branch; the ordinary Apply/default miss contract remains unchanged.
  public static RecordLoadFromSuppliedBinaryAttempt FromArzRecord(RecordDepotState depot,
   ObjectNameString requestedName,RecordNameLocale nameLocale,ArzRecordBinarySource source){
   if(source==null)throw new ArgumentNullException("source");
   return new RecordLoadFromSuppliedBinaryAttempt(depot,requestedName,nameLocale,source,true);
  }
  string Fail(string reason){
   if(trace.Reason==null)trace.Reason=reason;
   if(trace.MayHaveEffects){
    trace.RequiresResolution=true;
    state.BeginPendingMiss(trace.CallerPublication,trace);
   }
   return trace.Reason;
  }
  internal void CaptureWrapper(string reason,string last,string failure,bool acquired,bool released,
   bool counted,RecordCacheLookupResult lookup,byte[] name,ulong capacity){
   wrapperReason=reason;wrapperLast=last;wrapperFailure=failure;
   wrapperAcquired=acquired;wrapperReleased=released;wrapperCounted=counted;
   wrapperLookup=lookup;wrapperName=name;wrapperCapacity=capacity;wrapperCaptured=true;
  }
  internal string ExecuteMiss(byte[] normalized){
   trace.Entered=true;trace.FailureStage="SuppliedInput";
   if(input==null&&archiveSource!=null){
    if(normalized.Length==0)return Fail("SuppliedBinaryMissEmptyNameTakesFallback");
    if(!state.BucketCapacity.HasValue||!state.MaxLoadFactor.HasValue)
     return Fail("UnknownRecordCachePublicationConfiguration");
    uint? startTick=archiveSource.ConvertedStartTick32;
    if(archiveSource.Clock!=null){
     trace.FailureStage="CapturedMissStartTick";
     try{
      trace.StartTickConversion=archiveSource.Clock();
      startTick=trace.StartTickConversion!=null&&trace.StartTickConversion.Status==RecordLoadTickStatus.Known
       ?trace.StartTickConversion.Tick32:null;
     }catch(Exception e){return Fail("OwnMissStartClockThrew:"+e.GetType().FullName);}
    }
    if(!startTick.HasValue)return Fail(trace.StartTickConversion!=null&&trace.StartTickConversion.Status==RecordLoadTickStatus.DivideFault
     ?"CapturedMissStartTickDivideFault:"+trace.StartTickConversion.Reason:"UnknownConvertedMissStartTick");
    trace.FailureStage="OwnArzRecordRead";
    try{
     trace.OwnArzReadAttempted=true;
     trace.OwnArzRecord=archiveSource.ReadExact(normalized);
     trace.Completed("OwnArzRecordRead");
     trace.FailureStage="OwnArzContextProjection";
     input=archiveSource.ToSupplied(trace.OwnArzRecord,normalized,trace,startTick);
     trace.Completed("OwnArzContextPrepared");
     trace.FailureStage="SuppliedInput";
    }catch(InvalidDataException e){return Fail("OwnArzRecordReadUnsupported:"+e.Message);}
    catch(Exception e){return Fail("OwnArzRecordReadOperationThrew:"+e.GetType().FullName);}
   }
   if(input==null)return Fail("UnimplementedRecordArchiveFetch");
   // 1C4E90..95 takes fallback before archive lookup for an empty name;
   // supplied bytes cannot turn that unreachable Binary branch into success.
   if(normalized.Length==0)return Fail("SuppliedBinaryMissEmptyNameTakesFallback");
   if(!input.MatchesKey(normalized))return Fail("SuppliedRecordNameMismatch");
   if(!state.BucketCapacity.HasValue||!state.MaxLoadFactor.HasValue)
    return Fail("UnknownRecordCachePublicationConfiguration");
   if(!input.ConvertedStartTick32.HasValue)return Fail("UnknownConvertedMissStartTick");
   trace.Completed("SuppliedInputBound");
   bool parserInFlight=false;
   try{
    trace.FailureStage="ReaderCreate";
    trace.Reader=input.CreateReader();trace.Completed("ReaderCreated");
    try{
     trace.FailureStage="FreshTableInput";
     trace.Table=input.CreateFreshTable();trace.Completed("FreshTablePrepared");
     trace.FailureStage="RecordParse";
     parserInFlight=true;
     trace.Parser=BinaryRecordParser.DeserializeFresh(input.TableGate,trace.Reader);
     parserInFlight=false;
     if(trace.Parser.Status!="Known")return Fail(trace.Parser.Reason);
     trace.Completed("RecordParsed");
     trace.FailureStage="ParsedTableInstall";
     input.InstallParsed(trace.Table,trace.Parser.Record);trace.Completed("ParsedTableInstalled");
    }finally{
     // Own cloned-reader storage cleanup on all exits. Native input is assumed
     // borrowed (ownership byte zero); this does not assert a native free/EH.
     try{trace.Reader.Dispose();trace.ReaderDisposed=true;trace.Completed("OwnReaderDisposed");}
     catch(Exception e){trace.CleanupReason="OwnSuppliedReaderCleanupThrew:"+e.GetType().FullName;}
    }
    if(trace.CleanupReason!=null)return Fail(trace.CleanupReason);
    trace.FailureStage="TemporaryNameCopy";
    ulong capacity=normalized.Length<=15?15UL:((ulong)normalized.Length|15UL);
    trace.TemporaryPair=new RecordCacheNodePair(normalized,capacity,trace.Table);
    trace.Completed("TemporaryNameCopied");
    trace.FailureStage="CallerPublicationPrepare";
    trace.CallerPublication=new RecordCacheCallerPublicationAttempt(
     state.LastCompletedCache.BucketCount,state.BucketCapacity.Value,state.MaxLoadFactor.Value,
     state.LastCompletedCache.CopyEntriesInListOrder(),trace.TemporaryPair);
    state.BeginPendingMiss(trace.CallerPublication,trace);
    trace.FailureStage="CallerPublication";
    trace.Publication=trace.CallerPublication.RunOnce();
    if(trace.Publication.Status!="Known")return Fail(trace.Publication.Reason);
    trace.Completed("CallerPublicationComplete");
    trace.FailureStage="CompletedCacheProjection";
    var completed=trace.CallerPublication.ToCompletedSnapshot();
    int bucketCapacity=trace.CallerPublication.Inspect().BucketCapacity;
    state.InstallCompletedCache(completed,bucketCapacity);trace.CacheCommitted=true;
    trace.Completed("CompletedCacheInstalled");
    // The move reset the internal name to inline-empty, so native 1C50E8
    // skips its free. Never Dispose the opaque Table, including duplicate removal.
    trace.FailureStage="MissCounter";
    state.CountLoad();trace.LoadCountIncremented=true;trace.Completed("LoadCountIncremented");
    trace.FailureStage="ConvertedMissEndTick";
    uint? endTick=input.ConvertedEndTick32;
    if(input.Clock!=null){
     trace.FailureStage="CapturedMissEndTick";
     trace.EndTickConversion=input.Clock();
     endTick=trace.EndTickConversion!=null&&trace.EndTickConversion.Status==RecordLoadTickStatus.Known
      ?trace.EndTickConversion.Tick32:null;
    }
    if(!endTick.HasValue)return Fail(trace.EndTickConversion!=null&&trace.EndTickConversion.Status==RecordLoadTickStatus.DivideFault
     ?"CapturedMissEndTickDivideFault:"+trace.EndTickConversion.Reason:"UnknownConvertedMissEndTick");
    uint elapsed=RecordLoadTicks.Elapsed(input.ConvertedStartTick32.Value,endTick.Value);
    state.RecordElapsed(elapsed);trace.Elapsed=elapsed;trace.ElapsedRecorded=true;
    trace.Completed("ElapsedRecorded");
    state.CompletePendingMiss();trace.RequiresResolution=false;
    trace.Completed("SuppliedMissComplete");trace.FailureStage=null;return null;
   }catch(Exception e){
    // DeserializeFresh can advance its reader before allocating the returned
    // report. No report does not prove no effects; own Dispose is not rollback.
    if(parserInFlight)trace.UncertainEffects=true;
    return Fail("OwnSuppliedBinaryMissOperationThrew:"+e.GetType().FullName);
   }
  }
  RecordLoadResult InterruptedResult(){
   return new RecordLoadResult(wrapperReason??trace.Reason??"OwnRecordLoadResultReportingInterrupted",
    wrapperCaptured?wrapperLast:trace.LastCompletedStage,
    wrapperCaptured?wrapperFailure:trace.FailureStage,
    wrapperAcquired,wrapperReleased,wrapperCounted,wrapperLookup,wrapperName,wrapperCapacity,trace,
    state!=null&&state.RequiresResolution?state.PendingMiss:null);
  }
  public RecordLoadResult RunOnce(){
   if(result!=null)return result;
   if(attempted){
    // Even if reporting a previous own exception could not allocate a result,
    // no parse, source move, graph mutation or counter increment is repeated.
    return result=InterruptedResult();
   }
   attempted=true;
   try{result=RecordLoadFile.ApplyCore(state,requested,locale,this);}
   catch(Exception){
    trace.WrapperReportingInterrupted=true;
    if(!wrapperCaptured){
     // If even wrapper reporting escaped before the journal was sealed, keep
     // uncertainty explicit and prevent subsequent operations using old state.
     trace.UncertainEffects=true;trace.RequiresResolution=true;
     if(state!=null)state.BeginPendingMiss(trace.CallerPublication,trace);
    }
    result=InterruptedResult();
   }
   return result;
  }
 }
}
