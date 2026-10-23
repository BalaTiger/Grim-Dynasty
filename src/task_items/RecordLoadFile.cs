using System;
namespace GrimDynasty.TaskItems {
 // KnownC is an explicit caller precondition, NOT a detected game locale.
 public enum RecordNameLocale { Unknown, KnownC }
 public sealed class RecordDepotState {
  public readonly RegistrationGate Gate;
  RecordCacheSnapshot cache;
  uint hits,loads,elapsedTotal,elapsedMaximum;
  bool unresolved;
  int? bucketCapacity;
  readonly float? maxLoadFactor;
  RecordCacheCallerPublicationAttempt pendingPublication;
  RecordSuppliedBinaryMissTrace pendingMiss;
  // A pending partial miss must not expose an old complete view as live.
  public RecordCacheSnapshot Cache {get{return unresolved?null:cache;}}
  public RecordCacheSnapshot LastCompletedCache {get{return cache;}}
  public bool RequiresResolution {get{return unresolved;}}
  public RecordCacheCallerPublicationAttempt PendingPublication {get{return pendingPublication;}}
  public RecordSuppliedBinaryMissTrace PendingMiss {get{return pendingMiss;}}
  public int? CurrentCacheCount {get{return pendingPublication!=null?(int?)pendingPublication.Count:cache==null?null:(int?)cache.Count;}}
  public int? BucketCapacity {get{return bucketCapacity;}}
  public float? MaxLoadFactor {get{return maxLoadFactor;}}
  public uint HitCount54 {get{return hits;}}
  public uint LoadCount50 {get{return loads;}}
  public uint ElapsedTotal5C {get{return elapsedTotal;}}
  public uint ElapsedMaximum60 {get{return elapsedMaximum;}}
  public RecordDepotState(RegistrationGate gate,RecordCacheSnapshot cache,uint initialHitCount){
   Gate=gate;this.cache=cache;hits=initialHitCount;
  }
  // Publication configuration and tail statistics are explicit model inputs;
  // the existing hit-only constructor does not invent them.
  public RecordDepotState(RegistrationGate gate,RecordCacheSnapshot cache,uint initialHitCount,
   int knownBucketCapacity,float knownMaxLoadFactor,uint initialLoadCount,uint initialElapsedTotal,uint initialElapsedMaximum){
   if(cache==null)throw new ArgumentNullException("cache");
   if(knownBucketCapacity<cache.BucketCount||knownBucketCapacity>RecordCacheSnapshot.MaximumBuckets)
    throw new ArgumentOutOfRangeException("knownBucketCapacity");
   if(float.IsNaN(knownMaxLoadFactor)||float.IsInfinity(knownMaxLoadFactor)||knownMaxLoadFactor<=0)
    throw new ArgumentOutOfRangeException("knownMaxLoadFactor");
   Gate=gate;this.cache=cache;hits=initialHitCount;bucketCapacity=knownBucketCapacity;maxLoadFactor=knownMaxLoadFactor;
   loads=initialLoadCount;elapsedTotal=initialElapsedTotal;elapsedMaximum=initialElapsedMaximum;
  }
  internal void CountHit(){hits=unchecked(hits+1);}
  internal void BeginPendingMiss(RecordCacheCallerPublicationAttempt publication,RecordSuppliedBinaryMissTrace trace){unresolved=true;pendingPublication=publication;pendingMiss=trace;}
  internal void CompletePendingMiss(){unresolved=false;pendingPublication=null;pendingMiss=null;}
  internal void InstallCompletedCache(RecordCacheSnapshot completed,int capacity){cache=completed;bucketCapacity=capacity;}
  internal void CountLoad(){loads=unchecked(loads+1);}
  internal void RecordElapsed(uint elapsed){elapsedTotal=unchecked(elapsedTotal+elapsed);if(elapsed>elapsedMaximum)elapsedMaximum=elapsed;}
 }
 public sealed class RecordLoadResult {
  public readonly string Status,Reason,LastCompletedStage,FailureStage;
  public readonly bool LockAcquired,LockReleased,HitCountIncremented;
  public readonly RecordCacheLookupResult Lookup;
  public readonly object Table;
  public readonly ulong LocalNameCapacity;
  public readonly RecordSuppliedBinaryMissTrace SuppliedMiss;
  public readonly RecordSuppliedBinaryMissTrace PendingMiss;
  public bool RequiresResolution {get{return PendingMiss!=null||(SuppliedMiss!=null&&SuppliedMiss.RequiresResolution);}}
  readonly byte[] normalized;
  public bool CacheHit {get{return Lookup!=null&&Lookup.Found;}}
  public bool MayHaveEffects {get{return HitCountIncremented||(SuppliedMiss!=null&&SuppliedMiss.MayHaveEffects)||
   (PendingMiss!=null&&PendingMiss.MayHaveEffects);}}
  public byte[] CopyNormalizedName(){return normalized==null?null:(byte[])normalized.Clone();}
  internal RecordLoadResult(string reason,string last,string failed,bool acquired,bool released,bool counted,RecordCacheLookupResult lookup,byte[] name,ulong cap,RecordSuppliedBinaryMissTrace suppliedMiss,RecordSuppliedBinaryMissTrace pendingMiss){
   Status=reason==null?"Known":"Unsupported";Reason=reason;LastCompletedStage=last;FailureStage=reason==null?null:failed;
   LockAcquired=acquired;LockReleased=released;HitCountIncremented=counted;Lookup=lookup;
   SuppliedMiss=suppliedMiss;PendingMiss=pendingMiss;
   Table=lookup!=null&&lookup.Found?lookup.Entry.Table:
    reason==null&&suppliedMiss!=null?suppliedMiss.Table:null;
   normalized=name;LocalNameCapacity=cap;
  }
 }
 public static class RecordLoadFile {
  // Covered path: stable complete cache and byte strings, exclusive own gate,
  // normal successful native local-string allocation/copy/release. There is
  // no default locale, native-cache population, miss loader or derived Read.
  public static RecordLoadResult Apply(RecordDepotState state,ObjectNameString requested,RecordNameLocale locale){
   return ApplyCore(state,requested,locale,null);
  }
  internal static RecordLoadResult ApplyCore(RecordDepotState state,ObjectNameString requested,RecordNameLocale locale,RecordLoadFromSuppliedBinaryAttempt supplied){
   string reason=null,last="Start",attempt="Guards";bool acquired=false,released=false,counted=false;
   RecordCacheLookupResult lookup=null;byte[] normalized=null;ulong cap=0;
   if(state==null)reason="UnknownRecordDepot";
   else if(state.Gate==null)reason="UnknownRecordDepotGate";
   else if(requested==null)reason="UnknownRequestedRecordName";
   else if(requested.Length>RecordCacheSnapshot.MaximumNameBytes)reason="RecordNameOutsideCoveredDomain";
   if(reason==null){
    try{
     attempt="LockAcquire";state.Gate.Enter();acquired=true;last="LockAcquired";
     attempt="LocalNameCopy";byte[] local=requested.CopyBytes();cap=local.Length<=15?15UL:((ulong)local.Length|15UL);last="LocalNameCopied";
     attempt="NameProcessing";
     // Reject unsupported coverage before any local transformation. The
     // original's invalid-char/unknown-locale prefix is not predicted.
     for(int i=0;i<local.Length;i++){
      if(local[i]==0x5c)continue; // This branch does not call tolower.
      if(local[i]>=0x80){reason="SignedHighByteToLowerOutsideCoveredDomain";break;}
      if(locale!=RecordNameLocale.KnownC){reason="UnknownCrtToLowerLocale";break;}
     }
     if(reason==null){
      for(int i=0;i<local.Length;i++){
       byte b=local[i];local[i]=b==0x5c?(byte)0x2f:(b>=0x41&&b<=0x5a?(byte)(b+0x20):b);
      }
      normalized=local;last="NameProcessed";attempt="CacheLookup";
      if(state.RequiresResolution)reason="RecordDepotMissRequiresResolution";
      else if(state.Cache==null)reason="UnknownRecordCacheSnapshot";
      else{
       lookup=state.Cache.Find(normalized);last="CacheLookupComplete";
       if(!lookup.Found){
        attempt="CacheMiss";
        if(supplied==null)reason="UnimplementedRecordCacheMissLoader";
        else{
         reason=supplied.ExecuteMiss(normalized);
         last=supplied.Trace.LastCompletedStage;attempt=supplied.Trace.FailureStage;
        }
       }
       else {state.CountHit();counted=true;last="CacheHitComplete";}
      }
     }
    }catch(Exception e){reason="OwnRecordLoadOperationThrew:"+e.GetType().FullName;}
    finally{
     if(acquired){
      // Own cleanup on Unsupported/managed exceptions, not a recovered
      // native exception handler, allocator behavior or rollback guarantee.
      try{state.Gate.Exit();released=true;}
      catch(Exception e){attempt="LockRelease";reason="OwnRecordUnlockThrew:"+e.GetType().FullName;}
     }
    }
   }
   if(reason==null)last="LockReleased";
   if(supplied!=null)supplied.CaptureWrapper(reason,last,attempt,acquired,released,counted,lookup,normalized,cap);
   return new RecordLoadResult(reason,last,attempt,acquired,released,counted,lookup,normalized,cap,
    supplied==null?null:supplied.Trace,state!=null&&state.RequiresResolution?state.PendingMiss:null);
  }
 }
}
