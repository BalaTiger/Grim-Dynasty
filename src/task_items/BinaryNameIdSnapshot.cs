using System;
namespace GrimDynasty.TaskItems {
 public sealed class BinaryNameIdEntry {
  readonly byte[] key;
  public readonly int Id;
  public BinaryNameIdEntry(byte[] fullStoredKey,int id){
   if(fullStoredKey==null)throw new ArgumentNullException("fullStoredKey");
   if(fullStoredKey.Length>RecordCacheSnapshot.MaximumNameBytes)throw new ArgumentOutOfRangeException("fullStoredKey");
   key=(byte[])fullStoredKey.Clone();Id=id;
  }
  public byte[] CopyKey(){return (byte[])key.Clone();}
  internal byte[] Key {get{return key;}}
 }
 public sealed class BinaryNameIdLookupResult {
  public readonly BinaryNameIdEntry Entry;
  public readonly int Bucket,Visited,NodeIndex;
  public readonly bool NativeLayoutKnown;
  public bool Found {get{return Entry!=null;}}
  internal BinaryNameIdLookupResult(RecordCacheLookupResult found,BinaryNameIdEntry entry){Entry=entry;Bucket=found.Bucket;Visited=found.Visited;NodeIndex=found.NodeIndex;NativeLayoutKnown=true;}
  internal BinaryNameIdLookupResult(BinaryNameIdEntry entry){Entry=entry;Bucket=-1;Visited=-1;NodeIndex=-1;NativeLayoutKnown=false;}
 }
 public sealed class BinaryNameIdSnapshot {
  readonly BinaryNameIdEntry[] entries;
  readonly RecordCacheSnapshot keys;
  readonly BinaryStringPool completeUniquePool;
  // Reuse only 03C8E0's full-byte hash/chain lookup. IDs are typed int32,
  // never interpreted from an opaque table. Complete stable list order and
  // native mask==bucketCount-1 (power of two <=4096) are explicit premises.
  public BinaryNameIdSnapshot(int bucketCount,BinaryNameIdEntry[] listOrder){
   if(bucketCount<1||bucketCount>RecordCacheSnapshot.MaximumBuckets||(bucketCount&(bucketCount-1))!=0)throw new ArgumentOutOfRangeException("bucketCount");
   if(listOrder==null)throw new ArgumentNullException("listOrder");
   if(listOrder.Length>RecordCacheSnapshot.MaximumEntries)throw new ArgumentOutOfRangeException("listOrder");
   entries=(BinaryNameIdEntry[])listOrder.Clone();int total=0;
   // Reject the aggregate budget before duplicating any key storage.
   for(int i=0;i<entries.Length;i++){
    if(entries[i]==null)throw new ArgumentException("Complete name-to-ID nodes required");
    total+=entries[i].Key.Length;if(total>RecordCacheSnapshot.MaximumTotalKeyBytes)throw new ArgumentOutOfRangeException("listOrder");
   }
   var index=new RecordCacheEntry[entries.Length];
   for(int i=0;i<entries.Length;i++)index[i]=new RecordCacheEntry(entries[i].Key,null);
   keys=new RecordCacheSnapshot(bucketCount,index);
  }
  // This own logical index preserves exact full-byte keys and original IDs.
  // A sorted pool lookup does not recover the native node/list/bucket layout.
  // Uniqueness was checked without materializing strings by the pool factory.
  public static BinaryNameIdSnapshot FromCompleteUniquePool(BinaryStringPool pool){
   if(pool==null)throw new ArgumentNullException("pool");
   if(!pool.IsUnique)throw new ArgumentException("A complete unique original-ID pool is required","pool");
   return new BinaryNameIdSnapshot(pool);
  }
  BinaryNameIdSnapshot(BinaryStringPool pool){completeUniquePool=pool;}
  internal BinaryNameIdLookupResult Find(byte[] key){
   if(completeUniquePool!=null){
    int id=completeUniquePool.FindExact(key);
    return new BinaryNameIdLookupResult(id<0?null:new BinaryNameIdEntry(key,id));
   }
   var found=keys.Find(key);
   return new BinaryNameIdLookupResult(found,found.Found?entries[found.NodeIndex]:null);
  }
 }
}
