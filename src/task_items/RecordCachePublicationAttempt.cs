using System;
namespace GrimDynasty.TaskItems {
 public sealed class RecordCachePublicationInspection {
  public readonly int Count,BucketCount,BucketCapacity,CandidateNodeId;
  readonly RecordCacheEntry[] entries;
  readonly int[] next,previous,first,last;
  readonly bool[] removed;
  internal RecordCachePublicationInspection(int count,int bucketCount,int capacity,RecordCacheEntry[] entries,
   int[] next,int[] previous,int[] first,int[] last,bool[] removed){
   Count=count;BucketCount=bucketCount;BucketCapacity=capacity;CandidateNodeId=1;
   this.entries=(RecordCacheEntry[])entries.Clone();
   this.next=(int[])next.Clone();this.previous=(int[])previous.Clone();
   this.removed=(bool[])removed.Clone();
   this.first=new int[bucketCount];this.last=new int[bucketCount];
   Array.Copy(first,this.first,bucketCount);Array.Copy(last,this.last,bucketCount);
  }
  // IDs and removed-node tombstones are own diagnostics, not native memory.
  // Link arrays include sentinel 0 and retain their initial length. Bucket
  // copies expose only active endpoints, not unobserved spare-capacity slots.
  public int[] CopyListOrder(){
   var order=new int[Count];int node=next[0];
   for(int i=0;i<order.Length;i++){order[i]=node;node=next[node];}
   return order;
  }
  public int[] CopyNextLinks(){return (int[])next.Clone();}
  public int[] CopyPreviousLinks(){return (int[])previous.Clone();}
  public int[] CopyBucketFirst(){return (int[])first.Clone();}
  public int[] CopyBucketLast(){return (int[])last.Clone();}
  public bool[] CopyRemovedNodes(){return (bool[])removed.Clone();}
  public RecordCacheEntry EntryForNode(int nodeId){
   if(nodeId<=0||nodeId>=entries.Length)throw new ArgumentOutOfRangeException("nodeId");
   return entries[nodeId];
  }
 }
 public sealed class RecordCachePublicationAttemptResult {
  public readonly string Status,Reason,StopAddress,LastCompletedStage;
  public readonly bool MayHaveEffects,RequiresResolution;
  public readonly bool? Inserted;
  public readonly int? ReturnedNodeId;
  public readonly int RemovedNodeCount,CandidateNodeId;
  internal RecordCachePublicationAttemptResult(string reason,string stop,string stage,bool unresolved,
   bool? inserted,int? returned,int removed){
   Status=reason==null?"Known":"Unsupported";Reason=reason;StopAddress=stop;
   LastCompletedStage=stage;MayHaveEffects=true;RequiresResolution=unresolved;
   Inserted=reason==null?inserted:null;ReturnedNodeId=reason==null?returned:null;
   RemovedNodeCount=removed;CandidateNodeId=1;
  }
 }
 // Complete bounded normal-return behavior of 02E770/02EA80/02EB40 from an
 // explicit already-linked, already-counted candidate. This does not execute
 // the caller, recover 05DEB0/01F170/022860, or populate a native LoadFile cache.
 // Stable valid storage, exclusive ownership and normal allocation/copy/
 // release success are prerequisites. Native OOM, EH and allocator internals
 // are not recovered; own failures retain state and never retry the operation.
 public sealed class RecordCachePublicationAttempt {
  readonly RecordCacheEntry[] entries;
  readonly int[] next,previous;
  readonly bool[] removed;
  int[] first,last;
  int count,bucketCount,bucketCapacity,removedCount;
  readonly float maxLoadFactor;
  bool attempted,requiresResolution=true;
  bool adoptedEntryReady=true;
  string firstReason,firstStop,lastStage="EntrySnapshot",activeAddress="0x18002e770";
  RecordCachePublicationAttemptResult result;
  public int Count {get{return count;}}
  public int BucketCount {get{return bucketCount;}}
  public int BucketCapacity {get{return bucketCapacity;}}
  public float MaxLoadFactor {get{return maxLoadFactor;}}
  public bool Attempted {get{return attempted;}}
  public bool RequiresResolution {get{return requiresResolution;}}
  public RecordCachePublicationAttempt(int bucketCount,int bucketCapacity,float maxLoadFactor,
   RecordCacheEntry[] indexedListOrder,RecordCacheEntry alreadyLinkedCandidate){
   if(bucketCapacity<bucketCount||bucketCapacity>RecordCacheSnapshot.MaximumBuckets)
    throw new ArgumentOutOfRangeException("bucketCapacity");
   if(float.IsNaN(maxLoadFactor)||float.IsInfinity(maxLoadFactor)||maxLoadFactor<=0)
    throw new ArgumentOutOfRangeException("maxLoadFactor");
   // Reuse only validation and the supplied entry graph, never Advance().
   var initial=new RecordCachePublicationPrefixState(bucketCount,indexedListOrder,alreadyLinkedCandidate).Inspect();
   count=initial.Count;this.bucketCount=bucketCount;this.bucketCapacity=bucketCapacity;
   this.maxLoadFactor=maxLoadFactor;
   entries=new RecordCacheEntry[count+1];
   for(int node=1;node<entries.Length;node++)entries[node]=initial.EntryForNode(node);
   next=initial.CopyNextLinks();previous=initial.CopyPreviousLinks();removed=new bool[count+1];
   first=new int[bucketCapacity];last=new int[bucketCapacity];
   Array.Copy(initial.CopyBucketFirst(),first,bucketCount);
   Array.Copy(initial.CopyBucketLast(),last,bucketCount);
  }
  // Trusted caller-owned storage. Prepared before node construction so this
  // constructor cannot allocate a replacement graph after the name is moved.
  // The caller validates all inputs and owns every array exclusively. Count
  // here is the future already-linked entry count, not a performed increment.
  internal RecordCachePublicationAttempt(int bucketCount,int bucketCapacity,float maxLoadFactor,
   int linkedCount,RecordCacheEntry[] entries,int[] next,int[] previous,
   int[] first,int[] last,bool[] removed){
   this.bucketCount=bucketCount;this.bucketCapacity=bucketCapacity;this.maxLoadFactor=maxLoadFactor;
   count=linkedCount;this.entries=entries;this.next=next;this.previous=previous;
   this.first=first;this.last=last;this.removed=removed;adoptedEntryReady=false;
  }
  internal void ActivateAdoptedEntry(){
   // Both neighboring writes and the count increment must have completed.
   // No cloning, relinking, counting or indexing occurs at this handoff.
   if(adoptedEntryReady||attempted||entries[1]==null||
    next[previous[1]]!=1||previous[next[1]]!=1)
    throw new InvalidOperationException("The caller entry is not ready for publication.");
   adoptedEntryReady=true;
  }
  public RecordCachePublicationInspection Inspect(){
   return new RecordCachePublicationInspection(count,bucketCount,bucketCapacity,entries,next,previous,first,last,removed);
  }
  public RecordCacheSnapshot ToCompletedSnapshot(){
   if(requiresResolution||result==null||result.Status!="Known")
    throw new InvalidOperationException("The publication attempt has not completed.");
   var order=new RecordCacheEntry[count];int node=next[0];
   for(int i=0;i<order.Length;i++){order[i]=entries[node];node=next[node];}
   return new RecordCacheSnapshot(bucketCount,order);
  }
  int BucketFor(int node){return (int)(RecordCacheSnapshot.HashBytes(entries[node].Key)&(ulong)(bucketCount-1));}
  static bool Equal(byte[] left,byte[] right){
   // Full stored bytes and length, including NUL/high bytes. Equality under
   // the valid-buffer CRT contract does not model memcmp's internal reads.
   if(left.Length!=right.Length)return false;
   for(int i=0;i<left.Length;i++)if(left[i]!=right[i])return false;
   return true;
  }
  bool Stop(string reason,string address){
   if(firstReason==null){firstReason=reason;firstStop=address;}
   requiresResolution=true;return false;
  }
  void RemoveUnindexedNode(int node){
   activeAddress="0x18002eb40";
   // 02EB58/62 detach before 02EB66 count--. This node has not entered
   // bucket endpoints, so those endpoints are not changed.
   next[previous[node]]=next[node];previous[next[node]]=previous[node];count--;
   // Native 02EB6A..02EBA5 uses string capacity to release its owned heap,
   // then releases node storage. We assume valid ownership/normal release;
   // Length does not determine capacity or a native allocation/free count.
   // Opaque Table is never disposed, destroyed or unregistered here.
   removed[node]=true;removedCount++;
   next[node]=0;previous[node]=0; // Own tombstone, not native zeroing.
   lastStage="DuplicateRemovalComplete";
  }
  bool IndexNode(int candidate,out bool inserted,out int returned){
   activeAddress="0x18002e770";inserted=false;returned=0;
   int bucket=BucketFor(candidate),oldFirst=first[bucket];
   int scan=oldFirst==0?0:next[last[bucket]];
   while(scan!=oldFirst){
    scan=previous[scan];
    if(Equal(entries[candidate].Key,entries[scan].Key)){
     RemoveUnindexedNode(candidate);returned=scan;return true;
    }
   }
   // Unique exhausted lookup leaves the target at oldFirst, or sentinel.
   if(next[candidate]!=oldFirst){
    int before=previous[candidate],after=next[candidate];
    next[before]=after;previous[after]=before;
    int targetPrevious=previous[oldFirst];
    next[targetPrevious]=candidate;previous[candidate]=targetPrevious;
    next[candidate]=oldFirst;previous[oldFirst]=candidate;
   }
   first[bucket]=candidate;if(oldFirst==0)last[bucket]=candidate;
   lastStage="BucketPrefixWritten";
   if(!Maintain())return false;
   inserted=true;returned=candidate;return true;
  }
  void ResetBuckets(int target){
   activeAddress="0x18001b4f0";
   // 01B4F0/01BFB0 reserve exactly 2*target qword slots only if needed.
   // Copy the old logical endpoint range before reset and retain larger
   // existing capacity. Normal native allocation/release remains conditional.
   if(bucketCapacity<target){
    var newFirst=new int[target];var newLast=new int[target];
    Array.Copy(first,newFirst,bucketCount);Array.Copy(last,newLast,bucketCount);
    first=newFirst;last=newLast;bucketCapacity=target;
   }
   for(int b=0;b<target;b++){first[b]=0;last[b]=0;}
   bucketCount=target;lastStage="BucketsReset";
  }
  bool Maintain(){
   activeAddress="0x18002ea80";
   // All bounded counts and power-of-two ratios are exact binary32 values.
   // 02EABF/02EAC2 grows only on strict >, including equality as no-op.
   if((float)count/(float)bucketCount<=maxLoadFactor)return true;
   int target=bucketCount<512?bucketCount*8:bucketCount*2;
   // Own coverage gates AFTER unique splice/endpoint writes and BEFORE the
   // 02EAE9 reset call. These are not native failure or rollback semantics.
   if(target>RecordCacheSnapshot.MaximumBuckets)
    return Stop("RecordCacheGrowthBucketBudgetOutsideCoveredDomain","0x18002eae9");
   if((float)count/(float)target>maxLoadFactor)
    return Stop("RecordCacheRecursiveGrowthOutsideCoveredDomain","0x18002eae9");
   ResetBuckets(target);
   if(next[0]!=0){
    int oldTail=previous[0];
    for(;;){
     // 02EB10..26 fetches the CURRENT head every time. Saved-next traversal
     // is incorrect after moving a node into the processed bucket segments.
     int current=next[0];bool inserted;int returned;
     if(!IndexNode(current,out inserted,out returned))return false;
     // The saved tail may have just been removed. Compare its stable ID.
     if(current==oldTail)break;
    }
   }
   // Nested maintenance was a no-op: count/target was already <= threshold
   // and duplicate removals can only reduce count during this one layer.
   lastStage="RehashComplete";return true;
  }
  RecordCachePublicationAttemptResult SaveUnsupported(){
   result=new RecordCachePublicationAttemptResult(firstReason,firstStop,lastStage,true,null,null,removedCount);
   return result;
  }
  public RecordCachePublicationAttemptResult RunOnce(){
   if(!adoptedEntryReady)throw new InvalidOperationException("The caller entry has not been linked.");
   if(result!=null)return result;
   // Even if a prior own failure prevented result allocation, no graph
   // operation may run again. Preserve its first reason/stop when available.
   if(attempted){
    if(firstReason==null)Stop("RecordCachePublicationAttemptInterrupted",activeAddress);
    return SaveUnsupported();
   }
   attempted=true;
   try{
    bool inserted;int returned;
    if(!IndexNode(1,out inserted,out returned))return SaveUnsupported();
    var completed=new RecordCachePublicationAttemptResult(null,null,"PublicationComplete",false,inserted,returned,removedCount);
    result=completed;lastStage="PublicationComplete";requiresResolution=false;return result;
   }catch(Exception){
    // Own managed reporting, not native EH. A coverage stop remains the
    // first cause even if materializing its result subsequently fails.
    if(firstReason==null)Stop("OwnRecordCachePublicationOperationThrew",activeAddress);
    return SaveUnsupported();
   }
  }
 }
}
