using System;
namespace GrimDynasty.TaskItems {
 public sealed class RecordCacheCallerPublicationResult {
  public string Status {get;internal set;}
  public string Reason {get;internal set;}
  public string StopAddress {get;internal set;}
  public string LastCompletedStage {get;internal set;}
  public bool RequiresResolution {get;internal set;}
  public RecordCacheNodeConstructionResult NodeConstruction {get;internal set;}
  public RecordCachePublicationAttemptResult Publication {get;internal set;}
  public bool CountIncremented {get;internal set;}
  public bool HeadPreviousWritten {get;internal set;}
  public bool PreviousNextWritten {get;internal set;}
  internal bool UncertainEffects;
  public bool MayHaveEffects {get{return UncertainEffects||
   (NodeConstruction!=null&&NodeConstruction.MayHaveEffects)||CountIncremented||
   HeadPreviousWritten||PreviousNextWritten||(Publication!=null&&Publication.MayHaveEffects);}}
  internal RecordCacheCallerPublicationResult(){Status="NotRun";LastCompletedStage="BeforeEntry";}
 }
 // Bounded continuation of 1C5079..1C50DA, with an explicitly supplied valid
 // temporary name/Table pair. Archive lookup, record parsing, the preceding
 // temporary-name copy, and the following load/timing counters are not run.
 // Requires stable inputs and exclusive source/container ownership throughout
 // construction and RunOnce, plus the normal native allocation/copy/release
 // contracts of the two existing helpers. No opaque Table is ever disposed.
 public sealed class RecordCacheCallerPublicationAttempt {
  readonly RecordCacheEntry[] entries;
  readonly int[] next,previous,first,last;
  readonly bool[] removed;
  readonly object[] identities;
  readonly int bucketCount,bucketCapacity,oldHead;
  readonly RecordCacheNodePair source;
  readonly ulong preparedLength,preparedCapacity;
  readonly object preparedHeap;
  readonly string coverageReason;
  readonly RecordCacheNodeConstructionAttempt construction;
  readonly RecordCachePublicationAttempt publication;
  readonly RecordCacheCallerPublicationResult result=new RecordCacheCallerPublicationResult();
  int count;
  bool attempted,publicationEntered,constructionEntered;
  string activeAddress="0x1801c5079";
  public bool Attempted {get{return attempted;}}
  public bool RequiresResolution {get{return result.RequiresResolution;}}
  public int Count {get{return publicationEntered?publication.Count:count;}}
  public RecordCacheCallerPublicationAttempt(int bucketCount,int bucketCapacity,float maxLoadFactor,
   RecordCacheEntry[] indexedListOrder,RecordCacheNodePair sourcePair){
   if(bucketCount<1||bucketCount>RecordCacheSnapshot.MaximumBuckets||(bucketCount&(bucketCount-1))!=0)
    throw new ArgumentOutOfRangeException("bucketCount");
   if(bucketCapacity<bucketCount||bucketCapacity>RecordCacheSnapshot.MaximumBuckets)
    throw new ArgumentOutOfRangeException("bucketCapacity");
   if(float.IsNaN(maxLoadFactor)||float.IsInfinity(maxLoadFactor)||maxLoadFactor<=0)
    throw new ArgumentOutOfRangeException("maxLoadFactor");
   if(indexedListOrder==null)throw new ArgumentNullException("indexedListOrder");
   if(indexedListOrder.Length>=RecordCacheSnapshot.MaximumEntries)
    throw new ArgumentOutOfRangeException("indexedListOrder");
   this.bucketCount=bucketCount;this.bucketCapacity=bucketCapacity;source=sourcePair;
   count=indexedListOrder.Length;
   // Candidate slot 1 is reserved but absent from both list and buckets.
   // Each seed has a distinct logical node identity even when entries repeat.
   entries=new RecordCacheEntry[count+2];Array.Copy(indexedListOrder,0,entries,2,count);
   next=new int[entries.Length];previous=new int[entries.Length];
   removed=new bool[entries.Length];identities=new object[entries.Length];
   first=new int[bucketCapacity];last=new int[bucketCapacity];
   identities[0]=new object();oldHead=count==0?0:2;
   next[0]=oldHead;previous[0]=count==0?0:count+1;
   int total=0;
   for(int node=2;node<entries.Length;node++){
    var entry=entries[node];
    if(entry==null)throw new ArgumentException("Every indexed node must be known","indexedListOrder");
    if(entry.Key.Length>RecordCacheSnapshot.MaximumNameBytes||
     entry.Key.Length>RecordCacheSnapshot.MaximumTotalKeyBytes-total)
     throw new ArgumentOutOfRangeException("indexedListOrder");
    total+=entry.Key.Length;identities[node]=new object();
    previous[node]=node==2?0:node-1;next[node]=node+1==entries.Length?0:node+1;
    int bucket=(int)(RecordCacheSnapshot.HashBytes(entry.Key)&(ulong)(bucketCount-1));
    if(first[bucket]==0)first[bucket]=node;
    else if(last[bucket]!=node-1)throw new ArgumentException("Seed bucket nodes must be contiguous","indexedListOrder");
    last[bucket]=node;
   }
   if(source!=null){
    preparedLength=source.Name.Length;preparedCapacity=source.Name.Capacity;preparedHeap=source.Name.HeapStorageIdentity;
    // Leave the individual name/capacity coverage checks to construction.
    // The aggregate budget must also include the not-yet-constructed entry.
    if(preparedLength<=RecordCacheNodeConstructionAttempt.MaximumNameBytes&&
     preparedLength>(ulong)(RecordCacheSnapshot.MaximumTotalKeyBytes-total))
     coverageReason="RecordCacheCallerTotalKeyBudgetOutsideCoveredDomain";
   }
   construction=new RecordCacheNodeConstructionAttempt(source,identities[oldHead],identities[0]);
   // Preallocate the inactive adopter. These are exactly the caller's arrays.
   publication=new RecordCachePublicationAttempt(bucketCount,bucketCapacity,maxLoadFactor,count+1,
    entries,next,previous,first,last,removed);
  }
  // Before construction, slot 1 is an uncreated reservation: its entry is
  // null and it is not part of Count/list order. After a stop, inspection
  // exposes the actual partial graph, not a fabricated completed snapshot.
  public RecordCachePublicationInspection Inspect(){
   if(publicationEntered)return publication.Inspect();
   return new RecordCachePublicationInspection(count,bucketCount,bucketCapacity,entries,next,previous,first,last,removed);
  }
  public RecordCacheSnapshot ToCompletedSnapshot(){
   if(!attempted||result.Status!="Known"||result.RequiresResolution)
    throw new InvalidOperationException("The caller publication attempt has not completed.");
   // This diagnostic projection can allocate. Failure here does not undo
   // already completed publication or permit the operation to run again.
   return publication.ToCompletedSnapshot();
  }
  RecordCacheCallerPublicationResult Stop(string reason,string address){
   if(result.Reason==null){result.Reason=reason;result.StopAddress=address;}
   result.Status="Unsupported";result.RequiresResolution=result.MayHaveEffects;return result;
  }
  public RecordCacheCallerPublicationResult RunOnce(){
   if(attempted)return result;
   attempted=true;
   try{
    if(coverageReason!=null)return Stop(coverageReason,activeAddress);
    // This logical name exposes no writable payload; its only mutation is
    // ClearAfterMove. Length/capacity/heap identity detect intervening moves
    // with observable state changes. Exclusive ownership remains required.
    if(source!=null&&(source.Name.Length!=preparedLength||source.Name.Capacity!=preparedCapacity||
     !Object.ReferenceEquals(source.Name.HeapStorageIdentity,preparedHeap)))
     return Stop("RecordCacheCallerSourceChangedBeforeEntry",activeAddress);
    activeAddress="0x1801c508b";constructionEntered=true;
    result.NodeConstruction=construction.RunOnce();
    constructionEntered=false;
    if(result.NodeConstruction.Status!="Known")
     return Stop(result.NodeConstruction.Reason,activeAddress);
    var node=result.NodeConstruction.Node;
    result.LastCompletedStage="NodeConstructed";
    // Node.Next/Previous retain constructor-time identity snapshots. The
    // arrays below own the live links, including later indexing and rehash.
    identities[1]=node;next[1]=oldHead;previous[1]=0;
    // Preserve the construction result BEFORE this own fallible projection.
    // If it fails, the moved source and detached node remain observable.
    entries[1]=node.ToEntrySnapshot();
    result.LastCompletedStage="CandidateEntryPrepared";
    // The bounded count (<=4096) proves the 1C5093..50AE native limit check's
    // normal branch. No native oversized-count exception is synthesized.
    activeAddress="0x1801c50b5";count++;result.CountIncremented=true;
    result.LastCompletedStage="CountIncremented";
    activeAddress="0x1801c50b9";previous[oldHead]=1;result.HeadPreviousWritten=true;
    result.LastCompletedStage="HeadPreviousWritten";
    activeAddress="0x1801c50c1";next[previous[1]]=1;result.PreviousNextWritten=true;
    result.LastCompletedStage="PreviousNextWritten";
    activeAddress="0x1801c50da";
    publication.ActivateAdoptedEntry();
    // Set this before the call: growth can replace endpoint arrays even if
    // the helper later stops or its own result materialization throws.
    publicationEntered=true;
    result.Publication=publication.RunOnce();
    result.LastCompletedStage=result.Publication.LastCompletedStage;
    if(result.Publication.Status!="Known")
     return Stop(result.Publication.Reason,result.Publication.StopAddress);
    result.Status="Known";result.RequiresResolution=false;
    result.LastCompletedStage="CallerPublicationComplete";return result;
   }catch(Exception){
    if(constructionEntered)result.UncertainEffects=true;
    return Stop("OwnRecordCacheCallerPublicationOperationThrew",activeAddress);
   }
  }
 }
}
