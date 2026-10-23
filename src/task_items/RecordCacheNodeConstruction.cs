using System;
namespace GrimDynasty.TaskItems {
 // Owned logical storage for the 05DEB0 name, distinct from immutable cache
 // snapshots and Object::SetObjectName assignment. Padding bytes are unmodeled.
 public sealed class RecordCacheNodeName {
  sealed class Storage {
   internal readonly byte[] Bytes;
   internal Storage(byte[] bytes){Bytes=bytes;}
  }
  static readonly Storage Empty=new Storage(new byte[0]);
  // Disable beforefieldinit: any successfully supplied source has initialized
  // Empty before RunOnce, so ClearAfterMove cannot first allocate this storage.
  static RecordCacheNodeName(){}
  Storage storage;
  ulong capacity;
  internal RecordCacheNodeName(byte[] bytes,ulong knownCapacity){
   if(bytes==null)throw new ArgumentNullException("bytes");
   if(knownCapacity<15||knownCapacity<(ulong)bytes.Length||knownCapacity==ulong.MaxValue)
    throw new ArgumentOutOfRangeException("knownCapacity");
   storage=new Storage((byte[])bytes.Clone());capacity=knownCapacity;
  }
  RecordCacheNodeName(Storage owned,ulong knownCapacity){storage=owned;capacity=knownCapacity;}
  public ulong Length {get{return (ulong)storage.Bytes.Length;}}
  public ulong Capacity {get{return capacity;}}
  public byte TerminatorByte {get{return 0;}}
  public byte FirstByte {get{return storage.Bytes.Length==0?(byte)0:storage.Bytes[0];}}
  // Opaque identity only: no writable array, native address or allocator claim.
  public object HeapStorageIdentity {get{return capacity>=16?(object)storage:null;}}
  public byte[] CopyBytes(){return (byte[])storage.Bytes.Clone();}
  internal RecordCacheNodeName PrepareFreshDestination(){
   return capacity>=16?new RecordCacheNodeName(storage,capacity):
    new RecordCacheNodeName(storage.Bytes,capacity);
  }
  internal void ClearAfterMove(){storage=Empty;capacity=15;}
 }
 // A supplied valid pair, not the unimplemented temporary-name copy helper.
 // Exclusive access and stable Table are required for the covered call.
 public sealed class RecordCacheNodePair {
  public readonly RecordCacheNodeName Name;
  public readonly object Table;
  public RecordCacheNodePair(byte[] completeName,ulong capacity,object table){
   Name=new RecordCacheNodeName(completeName,capacity);Table=table;
  }
 }
 public sealed class RecordCacheConstructedNode {
  public const int NativeLayoutBytes=0x38;
  public readonly object Next,Previous;
  public readonly RecordCacheNodeName Name;
  object table;
  public object Table {get{return table;}}
  internal RecordCacheConstructedNode(object next,object previous,RecordCacheNodeName name){
   // 01F18F/192: a null next overrides both link arguments with this node.
   Next=next??this;Previous=next==null?this:previous;Name=name;
  }
  internal void StoreTable(object value){table=value;}
  // An explicit immutable projection. It neither links this node nor updates
  // a cache count/index; callers must separately satisfy publication's inputs.
  public RecordCacheEntry ToEntrySnapshot(){return new RecordCacheEntry(Name.CopyBytes(),Table);}
 }
 public sealed class RecordCacheNodeConstructionResult {
  public string Status {get;internal set;}
  public string Reason {get;internal set;}
  public string LastCompletedStage {get;internal set;}
  public RecordCacheConstructedNode Node {get;internal set;}
  public bool SourceNameCleared {get;internal set;}
  public bool HeapStorageTransferred {get;internal set;}
  public bool MayHaveEffects {get{return Node!=null||SourceNameCleared;}}
  internal RecordCacheNodeConstructionResult(){Status="NotRun";LastCompletedStage="BeforeEntry";}
 }
 public sealed class RecordCacheNodeConstructionAttempt {
  public const int MaximumNameBytes=65536;
  public const ulong MaximumNameCapacity=131072;
  readonly RecordCacheNodePair source;
  readonly object next,previous;
  // Allocate the result before an attempt can affect source storage.
  readonly RecordCacheNodeConstructionResult result=new RecordCacheNodeConstructionResult();
  public bool Attempted {get;private set;}
  public RecordCacheNodeConstructionAttempt(RecordCacheNodePair sourcePair,object nextLink,object previousLink){
   source=sourcePair;next=nextLink;previous=previousLink;
  }
  RecordCacheNodeConstructionResult Unsupported(string reason){
   result.Status="Unsupported";result.Reason=reason;return result;
  }
  // Normal-success logical outcome of 01F170 -> 05DEB0 -> 022860.
  // Requires valid nonnull native allocation, valid readable/writable source,
  // fresh disjoint destination, exclusive access, and normal external copy.
  // Native allocator failures/EH, bad pointers, aliasing and padding are outside
  // this contract. Own staging is not a reconstruction of native failure order.
  public RecordCacheNodeConstructionResult RunOnce(){
   if(Attempted)return result;
   Attempted=true;
   if(source==null)return Unsupported("UnknownRecordCacheNodePair");
   if(source.Name.Length>MaximumNameBytes)return Unsupported("RecordCacheNodeNameLengthOutsideCoveredDomain");
   if(source.Name.Capacity>MaximumNameCapacity)return Unsupported("RecordCacheNodeNameCapacityOutsideCoveredDomain");
   result.LastCompletedStage="InputValidated";
   bool heap=source.Name.Capacity>=16;
   RecordCacheConstructedNode node;
   try {
    // All own fallible storage work precedes source mutation. Heap storage is
    // retained by identity; short storage models memmove(dst,src,length+1),
    // including the logical terminator. IAT 180302728 is statically bound.
    var name=source.Name.PrepareFreshDestination();
    node=new RecordCacheConstructedNode(next,previous,name);
   } catch(OutOfMemoryException){
    return Unsupported("OwnRecordCacheNodeAllocationFailed");
   }
   source.Name.ClearAfterMove();
   // 05DEF1 reads pair+20 only after the move call has cleared its name.
   node.StoreTable(source.Table);
   result.Node=node;result.SourceNameCleared=true;result.HeapStorageTransferred=heap;
   result.LastCompletedStage="NameMovedAndTableStored";result.Status="Known";
   return result;
  }
 }
}
