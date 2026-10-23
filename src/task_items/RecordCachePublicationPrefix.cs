using System;
namespace GrimDynasty.TaskItems {
 public sealed class RecordCachePublicationPrefixSnapshot {
  public readonly int Count,BucketCount,CandidateNodeId;
  readonly RecordCacheEntry[] entries;
  readonly int[] next,previous,first,last;
  internal RecordCachePublicationPrefixSnapshot(int bucketCount,RecordCacheEntry[] entries,
   int[] next,int[] previous,int[] first,int[] last){
   Count=entries.Length-1;BucketCount=bucketCount;CandidateNodeId=1;
   this.entries=(RecordCacheEntry[])entries.Clone();
   this.next=(int[])next.Clone();this.previous=(int[])previous.Clone();
   this.first=(int[])first.Clone();this.last=(int[])last.Clone();
  }
  // IDs are local diagnostic identities, not native pointers. Link arrays
  // include sentinel 0; the list order excludes it. Empty bucket endpoints
  // are 0. Entries and their opaque Table references retain their identity.
  public int[] CopyListOrder(){
   var order=new int[Count];int node=next[0];
   for(int i=0;i<order.Length;i++){order[i]=node;node=next[node];}
   return order;
  }
  public int[] CopyNextLinks(){return (int[])next.Clone();}
  public int[] CopyPreviousLinks(){return (int[])previous.Clone();}
  public int[] CopyBucketFirst(){return (int[])first.Clone();}
  public int[] CopyBucketLast(){return (int[])last.Clone();}
  public RecordCacheEntry EntryForNode(int nodeId){
   if(nodeId<=0||nodeId>=entries.Length)throw new ArgumentOutOfRangeException("nodeId");
   return entries[nodeId];
  }
 }
 public sealed class RecordCachePublicationPrefixResult {
  public readonly string Status,Reason,StopAddress,FirstStopAddress;
  public readonly bool PrefixMutated;
  // PrefixMutated concerns this call only. The entry already contains the
  // caller's linked/counted candidate, including on duplicate or replay.
  public bool MayHaveEffects {get{return true;}}
  public bool RequiresResolution {get{return true;}}
  public readonly int ComparedNodes;
  // A match is an observation before the unknown removal call. Neither
  // native return field has been written at either supported stopping point.
  public readonly int? MatchedNodeId,ReturnedNodeId;
  public readonly bool? Inserted;
  internal RecordCachePublicationPrefixResult(string reason,string stop,bool mutated,int compared,int? matched,string firstStop){
   Status="Unsupported";Reason=reason;StopAddress=stop;PrefixMutated=mutated;
   ComparedNodes=compared;MatchedNodeId=matched;ReturnedNodeId=null;Inserted=null;FirstStopAddress=firstStop;
  }
 }
 public sealed class RecordCachePublicationPrefixState {
  public readonly int Count,BucketCount;
  readonly RecordCacheEntry[] entries;
  readonly int[] next,previous,first,last;
  bool attempted;
  RecordCachePublicationPrefixResult firstResult;
  public bool AdvanceAttempted {get{return attempted;}}
  public RecordCachePublicationPrefixResult FirstResult {get{return firstResult;}}
  public bool RequiresResolution {get{return true;}}
  // Explicit 02E770 entry snapshot: the caller has already linked candidate
  // at the front and counted it, but only the seed nodes are bucket-indexed.
  // This constructor does not execute 05DEB0, copy/move a native string,
  // populate a native cache, or assert that LoadFile/its caller succeeded.
  // Inputs must be complete and stable, with exclusive access. Validation
  // defines our coverage domain, not native checks for damaged storage.
  public RecordCachePublicationPrefixState(int bucketCount,RecordCacheEntry[] indexedListOrder,RecordCacheEntry alreadyLinkedCandidate){
   if(bucketCount<1||bucketCount>RecordCacheSnapshot.MaximumBuckets||(bucketCount&(bucketCount-1))!=0)
    throw new ArgumentOutOfRangeException("bucketCount");
   if(indexedListOrder==null)throw new ArgumentNullException("indexedListOrder");
   if(alreadyLinkedCandidate==null)throw new ArgumentNullException("alreadyLinkedCandidate");
   if(indexedListOrder.Length>=RecordCacheSnapshot.MaximumEntries)throw new ArgumentOutOfRangeException("indexedListOrder");
   int total=0;CheckKey(alreadyLinkedCandidate,ref total);
   for(int i=0;i<indexedListOrder.Length;i++){
    if(indexedListOrder[i]==null)throw new ArgumentException("Every indexed node must be known","indexedListOrder");
    CheckKey(indexedListOrder[i],ref total);
   }
   BucketCount=bucketCount;Count=indexedListOrder.Length+1;
   entries=new RecordCacheEntry[Count+1];entries[1]=alreadyLinkedCandidate;
   Array.Copy(indexedListOrder,0,entries,2,indexedListOrder.Length);
   next=new int[Count+1];previous=new int[Count+1];
   first=new int[bucketCount];last=new int[bucketCount];
   next[0]=1;previous[0]=Count;
   for(int node=1;node<=Count;node++){
    next[node]=node==Count?0:node+1;previous[node]=node-1;
    if(node==1)continue;
    int bucket=BucketFor(entries[node].Key);
    if(first[bucket]==0)first[bucket]=node;
    else if(last[bucket]!=node-1)throw new ArgumentException("Seed bucket nodes must be contiguous","indexedListOrder");
    last[bucket]=node;
   }
  }
  static void CheckKey(RecordCacheEntry entry,ref int total){
   int length=entry.Key.Length;
   if(length>RecordCacheSnapshot.MaximumNameBytes||length>RecordCacheSnapshot.MaximumTotalKeyBytes-total)
    throw new ArgumentOutOfRangeException("entry","Keys exceed the explicit snapshot budget");
   total+=length;
  }
  int BucketFor(byte[] key){return (int)(RecordCacheSnapshot.HashBytes(key)&(ulong)(BucketCount-1));}
  static bool Equal(byte[] left,byte[] right){
   // The native memcmp/length tests are equivalent to full-byte equality
   // over valid stable buffers. No CRT/invalid-storage behavior is modeled.
   if(left.Length!=right.Length)return false;
   for(int i=0;i<left.Length;i++)if(left[i]!=right[i])return false;
   return true;
  }
  public RecordCachePublicationPrefixSnapshot Inspect(){
   return new RecordCachePublicationPrefixSnapshot(BucketCount,entries,next,previous,first,last);
  }
  public RecordCachePublicationPrefixResult Advance(){
   if(attempted)return new RecordCachePublicationPrefixResult("RecordCachePublicationPrefixAlreadyStopped",null,false,0,null,firstResult==null?null:firstResult.FirstStopAddress);
   attempted=true;
   int bucket=BucketFor(entries[1].Key),oldFirst=first[bucket],compared=0;
   // 02E818..02E89D starts at last.next and walks prev, including first.
   // The candidate is not in the seed endpoints and is never compared here.
   int node=oldFirst==0?0:next[last[bucket]];
   while(node!=oldFirst){
    node=previous[node];compared++;
    if(Equal(entries[1].Key,entries[node].Key)){
     firstResult=new RecordCachePublicationPrefixResult("UnimplementedRecordCacheDuplicateRemoval","0x18002eb40",false,compared,node,"0x18002eb40");
     return firstResult;
    }
   }
   // In this stable complete domain exhaustion leaves RBX=oldFirst.
   // 02E8A9..02E8DA moves candidate immediately before that node. An empty
   // bucket targets sentinel, so candidate moves to the global list tail.
   if(next[1]!=oldFirst){
    int before=previous[1],after=next[1];
    next[before]=after;previous[after]=before;
    int targetPrevious=previous[oldFirst];
    next[targetPrevious]=1;previous[1]=targetPrevious;
    next[1]=oldFirst;previous[oldFirst]=1;
   }
   first[bucket]=1;
   if(oldFirst==0)last[bucket]=1;
   // 02E8EE/02E901 wrote endpoints, but 02E92B maintenance has not run.
   // Count was already written by the caller and is unchanged here.
   // No success callback, returned iterator, rollback, allocator/EH,
   // concurrency guarantee, or completed-cache snapshot is fabricated.
   firstResult=new RecordCachePublicationPrefixResult("UnimplementedRecordCachePostInsertMaintenance","0x18002ea80",true,compared,null,"0x18002ea80");
   return firstResult;
  }
 }
}
