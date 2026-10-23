using System;
namespace GrimDynasty.TaskItems {
 public sealed class RecordCacheEntry {
  readonly byte[] key;
  public readonly object Table;
  public RecordCacheEntry(byte[] completeKey,object table){
   if(completeKey==null)throw new ArgumentNullException("completeKey");
   if(completeKey.Length>RecordCacheSnapshot.MaximumNameBytes)throw new ArgumentOutOfRangeException("completeKey");
   key=(byte[])completeKey.Clone();Table=table;
  }
  public byte[] CopyKey(){return (byte[])key.Clone();}
  internal byte[] Key {get{return key;}}
 }
 public sealed class RecordCacheLookupResult {
  public readonly bool Found;
  public readonly int Bucket,Visited,NodeIndex;
  public readonly RecordCacheEntry Entry;
  internal RecordCacheLookupResult(int bucket,int visited,int index,RecordCacheEntry entry){Bucket=bucket;Visited=visited;NodeIndex=index;Entry=entry;Found=entry!=null;}
 }
 public sealed class RecordCacheSnapshot {
  public const int MaximumNameBytes=65536,MaximumEntries=4096,MaximumBuckets=4096,MaximumTotalKeyBytes=1048576;
  readonly RecordCacheEntry[] entries;
  readonly int[] first,last;
  public readonly int BucketCount;
  public int Count {get{return entries.Length;}}
  public RecordCacheEntry[] CopyEntriesInListOrder(){return (RecordCacheEntry[])entries.Clone();}
  // A complete, stable own list snapshot in next-link order. Each bucket is
  // one contiguous segment. This does not populate or parse a native cache.
  public RecordCacheSnapshot(int bucketCount,RecordCacheEntry[] listOrder){
   if(bucketCount<1||bucketCount>MaximumBuckets||(bucketCount&(bucketCount-1))!=0)throw new ArgumentOutOfRangeException("bucketCount");
   if(listOrder==null)throw new ArgumentNullException("listOrder");
   if(listOrder.Length>MaximumEntries)throw new ArgumentOutOfRangeException("listOrder");
   BucketCount=bucketCount;entries=(RecordCacheEntry[])listOrder.Clone();first=new int[bucketCount];last=new int[bucketCount];
   for(int b=0;b<bucketCount;b++){first[b]=-1;last[b]=-1;}
   int total=0;
   for(int i=0;i<entries.Length;i++){
    if(entries[i]==null)throw new ArgumentException("Every cache node must be known","listOrder");
    total+=entries[i].Key.Length;if(total>MaximumTotalKeyBytes)throw new ArgumentOutOfRangeException("listOrder");
    int b=BucketFor(entries[i].Key);
    if(first[b]<0)first[b]=i;
    else if(last[b]!=i-1)throw new ArgumentException("Bucket nodes must be contiguous","listOrder");
    last[b]=i;
   }
  }
  // Unlike Object::SetObjectName's signed-byte FNV32, this uses MOVZX and
  // FNV64 across the complete stored length, including embedded NUL bytes.
  public static ulong HashBytes(byte[] completeKey){
   if(completeKey==null)throw new ArgumentNullException("completeKey");
   if(completeKey.Length>MaximumNameBytes)throw new ArgumentOutOfRangeException("completeKey");
   ulong hash=0xcbf29ce484222325UL;
   foreach(byte value in completeKey)hash=unchecked((hash^value)*0x100000001b3UL);
   return hash;
  }
  public int BucketFor(byte[] completeKey){return (int)(HashBytes(completeKey)&(ulong)(BucketCount-1));}
  static bool Equal(byte[] left,byte[] right){
   // Normal valid readable-buffer equality from the two native memcmp tests.
   // Stable storage makes their second opposite-direction equality redundant.
   if(left.Length!=right.Length)return false;
   for(int i=0;i<left.Length;i++)if(left[i]!=right[i])return false;
   return true;
  }
  public RecordCacheLookupResult Find(byte[] completeKey){
   int b=BucketFor(completeKey),visited=0;
   if(first[b]>=0)for(int i=first[b];i<=last[b];i++){
    visited++;if(Equal(entries[i].Key,completeKey))return new RecordCacheLookupResult(b,visited,i,entries[i]);
   }
   return new RecordCacheLookupResult(b,visited,-1,null);
  }
 }
}
