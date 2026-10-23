using System;
using System.Collections.Generic;
namespace GrimDynasty.TaskItems {
 public sealed class BinaryNameCacheEntry {
  public readonly ulong Address;
  ulong cachedId;
  public ulong CachedId {get{return cachedId;}}
  public BinaryNameCacheEntry(ulong address,ulong cachedId){Address=address;this.cachedId=cachedId;}
  // All shared access requires the same table gate or exclusive ownership.
  // An entry's address stays stable; stale reads can rewrite only its ID.
  internal void WriteId(ulong value){cachedId=value;}
 }
 public sealed class BinaryFieldValue {
  public readonly int TypeTag;
  public readonly uint BufferIndex;
  public BinaryFieldValue(int typeTag,uint bufferIndex){TypeTag=typeTag;BufferIndex=bufferIndex;}
 }
 public sealed class BinaryFieldEntry {
  public readonly ulong Key;
  // Null represents unknown payload storage, not an absent field node.
  public readonly BinaryFieldValue Value;
  public BinaryFieldEntry(ulong key,BinaryFieldValue value){Key=key;Value=value;}
 }
 internal static class BinaryBuckets {
  internal const int MaximumBuckets=4096,MaximumEntries=4096;
  internal static uint Bucket(ulong key,uint shift,uint mask){return unchecked((uint)(key>>(int)(shift&63)))&mask;}
  internal static T[][] Copy<T>(uint shift,uint mask,T[][] source,Func<T,ulong> key) where T:class {
   return Copy(shift,mask,source,key,MaximumBuckets,MaximumEntries);
  }
  internal static T[][] Copy<T>(uint shift,uint mask,T[][] source,Func<T,ulong> key,int maximumBuckets,int maximumEntries) where T:class {
   if(source==null)throw new ArgumentNullException("source");
   if(source.Length>maximumBuckets||(source.Length!=0&&mask>=(uint)source.Length))throw new ArgumentOutOfRangeException("source");
   var copy=new T[source.Length][];int count=0;
   for(int i=0;i<source.Length;i++){
    if(source[i]==null)throw new ArgumentException("Complete bucket chains required");
    if(source[i].Length>maximumEntries-count)throw new ArgumentOutOfRangeException("source");
    count+=source[i].Length;copy[i]=(T[])source[i].Clone();
    foreach(T node in copy[i])if(node==null||Bucket(key(node),shift,mask)!=(uint)i)throw new ArgumentException("Unknown node or wrong bucket");
   }
   return copy;
  }
 }
 public sealed class BinaryPointerCacheSnapshot {
  public readonly uint Shift,Mask;
  readonly BinaryNameCacheEntry[][] buckets;
  readonly int maximumEntries;
  readonly uint? nativeClearedSlotBytes;
  BinaryNameCacheEntry[] slots;
  uint? usedCount,capacity;
  public uint? Capacity {get{return capacity;}}
  public uint? UsedCount {get{return usedCount;}}
  public int BucketCount {get{return buckets.Length;}}
  // Initial 284080 memset prefix only, not a claim that every unused cell
  // was zeroed. Null means the old explicit-snapshot API did not supply it.
  public uint? NativeClearedSlotBytes {get{return nativeClearedSlotBytes;}}
  // Array order is chain order. For allocated storage, outer length is the
  // explicit native +04 bucket count, never inferred from mask/capacity.
  // Empty outer array represents a null native bucket-array pointer;
  // empty rows are known empty buckets.
  public BinaryPointerCacheSnapshot(uint shift,uint mask,BinaryNameCacheEntry[][] completeBuckets):this(shift,mask,completeBuckets,BinaryBuckets.MaximumBuckets,BinaryBuckets.MaximumEntries,null){}
  BinaryPointerCacheSnapshot(uint shift,uint mask,BinaryNameCacheEntry[][] completeBuckets,int maximumBuckets,int maximumEntries,uint? nativeClearedSlotBytes){
   Shift=shift;Mask=mask;this.maximumEntries=maximumEntries;this.nativeClearedSlotBytes=nativeClearedSlotBytes;
   buckets=BinaryBuckets.Copy(shift,mask,completeBuckets,delegate(BinaryNameCacheEntry node){return node.Address;},maximumBuckets,maximumEntries);
  }
  // Growth preserves slot indices but relocates entries into new storage.
  // Unused null cells represent writable storage, not native zeroed bytes.
  public BinaryPointerCacheSnapshot(uint shift,uint mask,BinaryNameCacheEntry[][] completeBuckets,uint usedCount,uint capacity,BinaryNameCacheEntry[] completeSlots):this(shift,mask,completeBuckets){
   SetStorage(usedCount,capacity,completeSlots);
  }
  void SetStorage(uint usedCount,uint capacity,BinaryNameCacheEntry[] completeSlots){
   if(capacity>(uint)maximumEntries||usedCount>capacity)throw new ArgumentOutOfRangeException("capacity");
   if(completeSlots==null||completeSlots.Length!=capacity)throw new ArgumentException("Complete allocated slot storage required");
   var seen=new HashSet<BinaryNameCacheEntry>();
   for(int i=0;i<completeSlots.Length;i++){
    if((uint)i<usedCount){if(completeSlots[i]==null||!seen.Add(completeSlots[i]))throw new ArgumentException("Used slot identities must be unique and known");}
    else if(completeSlots[i]!=null)throw new ArgumentException("Unused slots must be empty");
   }
   foreach(var chain in buckets)foreach(var node in chain)if(!seen.Remove(node))throw new ArgumentException("Bucket chains must contain each used slot exactly once");
   if(seen.Count!=0)throw new ArgumentException("Used slots missing from bucket chains");
   slots=(BinaryNameCacheEntry[])completeSlots.Clone();this.usedCount=usedCount;this.capacity=capacity;
  }
  // Explicit normal completed initial import with unique original IDs and
  // an already present empty string. The address is a caller-owned logical
  // token, not an inferred preferred-image or managed runtime address.
  // Accurate finite native log is a premise. Exact-power boundaries >=8
  // remain uncovered; integer floor-log selects only the strict intervals.
  public static BinaryPointerCacheSnapshot FromArchiveInitializedUniquePool(int originalCount,int emptyId,ulong knownEmptyQueryAddress){
   if(originalCount<4||originalCount>131072)throw new ArgumentOutOfRangeException("originalCount");
   if(emptyId<0||emptyId>=originalCount)throw new ArgumentOutOfRangeException("emptyId");
   if(knownEmptyQueryAddress==0)throw new ArgumentOutOfRangeException("knownEmptyQueryAddress");
   int requested=originalCount/4;
   if(requested>=8&&(requested&(requested-1))==0)throw new ArgumentOutOfRangeException("originalCount","Native log exact-power rounding boundary is outside the covered domain");
   int exponent=0;for(int left=requested;left>1;left>>=1)exponent++;
   exponent=Math.Max(2,Math.Min(16,exponent));int bucketCount=1<<exponent;
   uint mask=(uint)(bucketCount-1),capacity=(uint)(originalCount/2);
   var buckets=new BinaryNameCacheEntry[bucketCount][];
   for(int i=0;i<bucketCount;i++)buckets[i]=new BinaryNameCacheEntry[0];
   var slots=new BinaryNameCacheEntry[(int)capacity];
   var empty=new BinaryNameCacheEntry(knownEmptyQueryAddress,(ulong)emptyId);slots[0]=empty;
   buckets[BinaryBuckets.Bucket(knownEmptyQueryAddress,2,mask)]=new[]{empty};
   var result=new BinaryPointerCacheSnapshot(2,mask,buckets,32768,65536,(uint)requested*24);
   result.SetStorage(1,capacity,slots);return result;
  }
  public BinaryNameCacheEntry GetSlot(uint index){
   if(slots==null)throw new InvalidOperationException("Unknown slot storage");
   if(index>=(uint)slots.Length)throw new ArgumentOutOfRangeException("index");
   return slots[index];
  }
  public BinaryNameCacheEntry[] CopyBucketChain(uint bucket){
   if(bucket>=(uint)buckets.Length)throw new ArgumentOutOfRangeException("bucket");
   return (BinaryNameCacheEntry[])buckets[bucket].Clone();
  }
  internal BinaryNameCacheEntry Find(ulong address){
   if(buckets.Length==0)return null;
   foreach(var node in buckets[BinaryBuckets.Bucket(address,Shift,Mask)])if(node.Address==address)return node;
   return null;
  }
  string GrowFullStorage(BinaryLookupEffects effects){
   uint old=capacity.Value;
   // 284160 doubles uint32 capacity; 0 remains 0 and recurses again.
   // This bounded model does not invent a bootstrap capacity or simulate
   // unbounded recursion, overflowing capacities, native OOM or free/EH.
   if(old==0)return "BinaryPointerCacheZeroCapacityGrowthOutsideCoveredDomain";
   uint next=old*2;
   if(next>(uint)maximumEntries)return "BinaryPointerCacheGrowthOutsideCoveredDomain";
   var nextSlots=new BinaryNameCacheEntry[(int)next];
   var moved=new Dictionary<BinaryNameCacheEntry,BinaryNameCacheEntry>();
   // The only caller is full insertion, so old capacity == used count.
   // 284200 copies capacity slots, not merely a guessed list of live nodes.
   for(uint i=0;i<old;i++){
    var source=slots[i];var target=new BinaryNameCacheEntry(source.Address,source.CachedId);
    nextSlots[i]=target;moved.Add(source,target);
   }
   var nextChains=new BinaryNameCacheEntry[buckets.Length][];
   for(int i=0;i<buckets.Length;i++){
    var chain=buckets[i];var relocated=new BinaryNameCacheEntry[chain.Length];
    for(int j=0;j<chain.Length;j++)relocated[j]=moved[chain[j]];
    nextChains[i]=relocated;
   }
   // Native relocates next/head pointers by slot index, preserving chain
   // order and the bucket array, shift, mask and count. Prepare all own
   // allocations before publishing heads, then the new slots and capacity.
   // Detached old entries no longer represent cells in the current cache.
   for(int i=0;i<buckets.Length;i++)buckets[i]=nextChains[i];
   slots=nextSlots;capacity=next;
   effects.Grown=true;effects.PreviousCapacity=old;effects.CurrentCapacity=next;
   return null;
  }
  internal string Insert(ulong address,ulong id,BinaryLookupEffects effects,out BinaryNameCacheEntry inserted){
   inserted=null;
   if(!usedCount.HasValue)return "UnknownBinaryPointerCacheInsertionStorage";
   uint old=usedCount.Value;
   if(old>=Capacity.Value){string failure=GrowFullStorage(effects);if(failure!=null)return failure;}
   if(buckets.Length==0)return "UnknownBinaryPointerCacheBucketStorage";
   uint bucket=BinaryBuckets.Bucket(address,Shift,Mask);var previous=buckets[bucket];
   // Prepare the own representation before the first native-visible write;
   // no native allocation/exception ABI is claimed for this managed storage.
   var node=new BinaryNameCacheEntry(address,id);var chain=new BinaryNameCacheEntry[previous.Length+1];
   chain[0]=node;Array.Copy(previous,0,chain,1,previous.Length);
   usedCount=old+1;effects.CountWritten=true;effects.PreviousCount=old;effects.CurrentCount=old+1;effects.SlotIndex=old;
   slots[old]=node;buckets[bucket]=chain;effects.Inserted=true;inserted=node;
   return null;
  }
 }
 public sealed class BinaryFieldSnapshot {
  public readonly uint Shift,Mask;
  readonly BinaryFieldEntry[][] buckets;
  public BinaryFieldSnapshot(uint shift,uint mask,BinaryFieldEntry[][] completeBuckets){
   Shift=shift;Mask=mask;buckets=BinaryBuckets.Copy(shift,mask,completeBuckets,delegate(BinaryFieldEntry node){return node.Key;});
  }
  internal BinaryFieldEntry Find(ulong signedId){
   if(buckets.Length==0)return null;
   foreach(var node in buckets[BinaryBuckets.Bucket(signedId,Shift,Mask)])if(node.Key==signedId)return node;
   return null;
  }
 }
 public sealed class BinaryWarmState {
  public const int MaximumStrings=4096,MaximumPoolBytes=2097152,MaximumBufferValues=65536;
  public readonly BinaryPointerCacheSnapshot Names;
  public readonly BinaryFieldSnapshot Fields;
  public readonly BinaryNameIdSnapshot NameIds;
  internal readonly BinaryStringPool Strings;
  internal readonly int[] Values;
  public BinaryStringPool StringPool {get{return Strings;}}
  // One shared pool: name context+40 and numeric archive+C8 read the same
  // begin-pointer field. Null components mean unknown, not known empty.
  public BinaryWarmState(BinaryPointerCacheSnapshot names,TableString[] sharedStringPool,BinaryFieldSnapshot fields,int[] intBuffer):this(names,sharedStringPool,fields,intBuffer,null){}
  public BinaryWarmState(BinaryPointerCacheSnapshot names,TableString[] sharedStringPool,BinaryFieldSnapshot fields,int[] intBuffer,BinaryNameIdSnapshot nameIds):this(names,sharedStringPool==null?null:BinaryStringPool.FromExisting(sharedStringPool),fields,intBuffer,nameIds,true){}
  // The explicit factory shares the same original-ID pool with name lookup;
  // it neither eagerly materializes the archive nor constructs native layout.
  public static BinaryWarmState FromPool(BinaryPointerCacheSnapshot names,BinaryStringPool sharedStringPool,BinaryFieldSnapshot fields,int[] intBuffer,BinaryNameIdSnapshot nameIds){
   return new BinaryWarmState(names,sharedStringPool,fields,intBuffer,nameIds,true);
  }
  BinaryWarmState(BinaryPointerCacheSnapshot names,BinaryStringPool sharedStringPool,BinaryFieldSnapshot fields,int[] intBuffer,BinaryNameIdSnapshot nameIds,bool fromPool){
   Names=names;Fields=fields;NameIds=nameIds;Strings=sharedStringPool;
   if(intBuffer!=null){if(intBuffer.Length>MaximumBufferValues)throw new ArgumentOutOfRangeException("intBuffer");Values=(int[])intBuffer.Clone();}
  }
 }
 // Allocated before lookup so the outer getter retains completed writes
 // even if a later own operation throws before a detailed result returns.
 internal sealed class BinaryLookupEffects {
  internal bool Written,Changed,CountWritten,Inserted,Grown;
  internal ulong? Previous,Current;
  internal uint? PreviousCount,CurrentCount,SlotIndex,PreviousCapacity,CurrentCapacity;
 }
 public sealed class BinaryWarmLookupResult {
  public readonly string Status,Reason,LastCompletedStage,FailureStage;
  public readonly BinaryNameCacheEntry PointerEntry;
  public readonly BinaryFieldEntry FieldEntry;
  public readonly TableString Value;
  public readonly bool UsedDefault;
  public readonly int? CStringLength,NameId,StringIndex;
  public readonly bool Cold,Stale,NameTemporaryReleased,CacheIdWritten,CacheIdChanged,CacheCountWritten,CacheNodeInserted,CacheStorageGrown;
  public readonly ulong? LocalNameCapacity,PreviousCachedId,WrittenCachedId;
  public readonly uint? PreviousCacheCount,CurrentCacheCount,InsertedSlotIndex,PreviousCacheCapacity,CurrentCacheCapacity;
  public readonly BinaryNameIdLookupResult NameLookup;
  public bool MayHaveEffects {get{return CacheIdWritten||CacheCountWritten||CacheNodeInserted||CacheStorageGrown;}}
  internal BinaryWarmLookupResult(string reason,string last,string failed,BinaryNameCacheEntry pointer,BinaryFieldEntry field,TableString value,bool usedDefault,int? length,int? id,int? index,
   bool cold,bool stale,ulong? capacity,bool released,BinaryNameIdLookupResult lookup,BinaryLookupEffects effects){
   Status=reason==null?"Known":"Unsupported";Reason=reason;LastCompletedStage=last;FailureStage=reason==null?null:failed;
   PointerEntry=pointer;FieldEntry=field;Value=value;UsedDefault=usedDefault;CStringLength=length;NameId=id;StringIndex=index;
   Cold=cold;Stale=stale;LocalNameCapacity=capacity;NameTemporaryReleased=released;NameLookup=lookup;
   CacheIdWritten=effects.Written;CacheIdChanged=effects.Changed;PreviousCachedId=effects.Previous;WrittenCachedId=effects.Current;
   CacheCountWritten=effects.CountWritten;CacheNodeInserted=effects.Inserted;PreviousCacheCount=effects.PreviousCount;CurrentCacheCount=effects.CurrentCount;InsertedSlotIndex=effects.SlotIndex;
   CacheStorageGrown=effects.Grown;PreviousCacheCapacity=effects.PreviousCapacity;CurrentCacheCapacity=effects.CurrentCapacity;
  }
 }
 // Only used inside the table gate. All name paths share the numeric getter;
 // Full-cache growth relocates slots before the existing cold insertion.
 internal static class BinaryWarmLookup {
  internal static BinaryWarmLookupResult Read(BinaryWarmState state,ulong? address,byte[] query,TableString fallback,BinaryLookupEffects effects){
   string last="Start",stage="PointerCache";BinaryNameCacheEntry pointer=null;BinaryFieldEntry field=null;
   TableString value=null;bool usedDefault=false;int? length=null,id=null,index=null;
   bool cold=false,stale=false,tempReleased=false;ulong? capacity=null;BinaryNameIdLookupResult nameLookup=null;
   Func<string,BinaryWarmLookupResult> result=delegate(string reason){return new BinaryWarmLookupResult(reason,last,stage,pointer,field,value,usedDefault,length,id,index,cold,stale,capacity,tempReleased,nameLookup,effects);};
   if(state.Names==null)return result("UnknownBinaryPointerCache");
   if(state.Names.BucketCount==0&&state.NameIds==null){cold=true;return result("UnimplementedBinaryPointerCacheMiss");}
   stage="QueryAddress";
   if(!address.HasValue)return result("UnknownBinaryQueryAddress");
   if(address.Value==0)return result("NullBinaryQueryAddressOutsideCoveredDomain");
   stage="PointerLookup";pointer=state.Names.Find(address.Value);
   cold=pointer==null;
   if(cold){
    if(state.NameIds==null)return result("UnimplementedBinaryPointerCacheMiss");
    last="PointerCacheMiss";
   }else{
    last="PointerCacheHit";stage="CachedNameRead";
    if(state.Strings==null)return result("UnknownBinarySharedStringPool");
    // Native uses the full qword for this name read, before returning int32.
    if(pointer.CachedId>=(ulong)state.Strings.Count)return result("CachedBinaryNameIndexOutsideCoveredDomain");
    TableString cached=state.Strings.Get((int)pointer.CachedId);stage="NameContentCompare";
    if(query==null)return result("UnknownBinaryQueryCString");
    for(int i=0;;i++){
     if(i>TableString.MaximumBytes||i>=query.Length)return result("BinaryQueryReadOutsideCoveredDomain");
     byte stored=i==cached.Length?(byte)0:cached.Bytes[i];
     if(stored!=query[i]){stale=true;break;}
     if(stored==0){length=i;break;}
    }
   }
   if(cold||stale){
    // The old overload explicitly lacks the complete name-to-ID snapshot;
    // preserve its coverage boundary rather than fabricating a miss.
    if(state.NameIds==null)return result("UnimplementedBinaryStalePointerLookup");
    last=cold?"PointerCacheMiss":"StaleNameDetected";stage=cold?"ColdQueryCStringRead":"StaleQueryCStringRead";
    if(query==null)return result("UnknownBinaryQueryCString");
    int count=0;while(count<query.Length&&count<=TableString.MaximumBytes&&query[count]!=0)count++;
    if(count>TableString.MaximumBytes||count==query.Length)return result(cold?"BinaryColdQueryReadOutsideCoveredDomain":"BinaryStaleQueryReadOutsideCoveredDomain");
    length=count;stage=cold?"ColdNameCopy":"StaleNameCopy";
    var local=new byte[count];Array.Copy(query,local,count);capacity=count<=15?15UL:((ulong)count|15UL);last=cold?"ColdNameCopied":"StaleNameCopied";
    stage=cold?"ColdNameLookup":"StaleNameLookup";nameLookup=state.NameIds.Find(local);id=nameLookup.Found?nameLookup.Entry.Id:-1;last=cold?"ColdNameLookupComplete":"StaleNameLookupComplete";
    // Normal successful temporary release precedes both the -1 branch and
    // cache write (2839CA..9E8). No native allocation/free/OOM ABI is modeled.
    local=null;tempReleased=true;last="NameTemporaryReleased";
    if(id.Value!=-1){
     ulong next=unchecked((ulong)(long)id.Value);
     if(cold){
      stage="CacheInsert";string failure=state.Names.Insert(address.Value,next,effects,out pointer);
      if(failure!=null)return result(failure);
      last="CacheNodeInserted";
     }else{
      stage="CacheIdWrite";ulong previous=pointer.CachedId;
      pointer.WriteId(next);effects.Written=true;effects.Changed=previous!=next;effects.Previous=previous;effects.Current=next;
      last="CacheIdWritten";
     }
    }
   }else {last="NameContentMatched";id=unchecked((int)pointer.CachedId);}
   stage="NumericFieldLookup";
   if(state.Fields==null)return result("UnknownBinaryFieldSnapshot");
   ulong signedId=unchecked((ulong)(long)id.Value);field=state.Fields.Find(signedId);
   if(field==null){value=fallback;usedDefault=true;last="DefaultSelected";return result(null);}
   last="NumericFieldMatched";stage="FieldPayloadRead";
   if(field.Value==null)return result("UnknownBinaryFieldPayload");
   if(field.Value.TypeTag!=2){value=fallback;usedDefault=true;last="DefaultSelected";return result(null);}
   stage="ValueBufferRead";
   if(state.Values==null)return result("UnknownBinaryValueBuffer");
   if(field.Value.BufferIndex>=(uint)state.Values.Length)return result("BinaryValueBufferReadOutsideCoveredDomain");
   index=state.Values[field.Value.BufferIndex];last="StringIndexRead";
   // This bounds check IS native. It returns known null, unlike the earlier
   // unchecked buffer/cached-name reads that are own coverage boundaries.
   stage="NumericStringPoolRead";
   if(state.Strings==null)return result("UnknownBinarySharedStringPool");
   if(index.Value<0||index.Value>=state.Strings.Count){last="NullStringSelected";return result(null);}
   value=state.Strings.Get(index.Value);last="StoredStringSelected";return result(null);
  }
 }
}
