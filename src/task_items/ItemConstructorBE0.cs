using System;

namespace GrimDynasty.TaskItems {
 // Own observation of the successful initial264B allocation for exactly two
 // HookPack elements. This storage is shared with the check source; tokens are
 // identities only, never addresses to dereference. This does not implement
 // malloc/new-handler retry, native exception cleanup, or the native null arm.
 public sealed class ItemBE0ArrayStorage {
  public const int RequestedBytes=0x108,ElementCount=2,ElementStride=0x80;
  public readonly ulong BaseToken;
  readonly byte?[] bytes;
  public ItemBE0ArrayStorage(ulong baseToken):this(baseToken,new byte?[RequestedBytes]){}
  public ItemBE0ArrayStorage(ulong baseToken,byte?[] observation){
   if(baseToken==0||baseToken>ulong.MaxValue-(RequestedBytes-1))throw new ArgumentOutOfRangeException("baseToken");
   if(observation==null)throw new ArgumentNullException("observation");
   if(observation.Length!=RequestedBytes)throw new ArgumentException("Expected the fixed BE0 allocation observation","observation");
   BaseToken=baseToken;bytes=(byte?[])observation.Clone();
  }
  public ulong ElementsToken {get{return BaseToken+8;}}
  public byte? ReadByte(int offset){Check(offset,1);return bytes[offset];}
  public ulong? ReadLittleEndian(int offset,int width){
   Check(offset,width);ulong value=0;
   for(int i=0;i<width;i++){if(!bytes[offset+i].HasValue)return null;value|=(ulong)bytes[offset+i].Value<<(8*i);}
   return value;
  }
  public byte?[] CopyObservation(){return (byte?[])bytes.Clone();}
  public static int ElementOffset(int index){
   if(index<0||index>=ElementCount)throw new ArgumentOutOfRangeException("index");
   return 8+index*ElementStride;
  }
  public void Store(int offset,int width,ulong value){
   Check(offset,width);for(int i=0;i<width;i++)bytes[offset+i]=unchecked((byte)(value>>(8*i)));
  }
  internal void ZeroRequestedSpan(){for(int i=0;i<RequestedBytes;i++)bytes[i]=0;}
  static void Check(int offset,int width){
   if(width<1||width>8||offset<0||offset>RequestedBytes-width)throw new ArgumentOutOfRangeException("offset");
  }
 }
 // One supplied OWN observation of the Engine pre-element check. A normal
 // return does not prove a loaded guard target, policy, or no-op. False/null
 // leaves coverage unresolved; neither is a simulated native fatal/EH path.
 public sealed class ItemBE0CheckObservation {
  public readonly bool ReturnedNormally;
  public readonly string Reason;
  public ItemBE0CheckObservation(bool returnedNormally,string reason=null){ReturnedNormally=returnedNormally;Reason=reason;}
 }
 public interface IItemBE0CheckSource {
  ItemBE0CheckObservation Check(ItemConstructorStorage state,ItemBE0ArrayStorage allocation,int index,ulong callbackToken);
 }
 public sealed class ItemConstructorBE0Result {
  public readonly ItemConstructorBA8Result Prior;
  public readonly ItemConstructorStorage State;
  public readonly ItemBE0ArrayStorage Array;
  public int CallerPrefixStores {get;internal set;}
  public int HeaderStores {get;internal set;}
  public int PublishedPointerStores {get;internal set;}
  public int CallerSuffixStores {get;internal set;}
  public int ItemStoreInstructions {get{return CallerPrefixStores+HeaderStores+PublishedPointerStores+CallerSuffixStores;}}
  public int ArrayStoreInstructions {get;internal set;}
  public int TotalStoreInstructions {get{return ItemStoreInstructions+ArrayStoreInstructions;}}
  public int BulkZeroBytes {get;internal set;}
  public int ConstructedElements {get;internal set;}
  public int CheckSourceCalls {get;internal set;}
  public int NormalCheckReturns {get;internal set;}
  public int? PendingElementIndex {get;internal set;}
  public bool KnownInitialAllocationUsed {get;internal set;}
  public ulong? RequestedAllocationBytes {get;internal set;}
  public bool ArrayPublished {get;internal set;}
  public bool WrapperReturned {get;internal set;}
  public ulong? ReturnedSubobjectToken {get;internal set;}
  public string Stage {get;internal set;}
  public string Status {get;internal set;}
  public string Reason {get;internal set;}
  public uint ResumeInstructionRva {get;internal set;}
  public uint? CallSiteRva {get;internal set;}
  public bool ReachedItem323968 {get;internal set;}
  public bool IsCompleteItemConstructor {get{return false;}}
  internal ItemConstructorBE0Result(ItemConstructorBA8Result prior,ItemBE0ArrayStorage array){
   Prior=prior;State=prior==null?null:prior.State;Array=array;
   Status="Unsupported";Reason="NotRun";Stage="NotRun";ResumeInstructionRva=ItemConstructorBE0Invocation.EntryRva;
  }
 }
 // Conditional on the Game import invoking the archived Engine EBDE0 normal
 // body and on the existing Actor/Item prerequisites. No DLL or native pointer
 // is executed. Missing coverage preserves completed writes. Own callbacks may
 // mutate the SAME Item and array; exceptions propagate without rollback or an
 // invented native cleanup path. One attempt also prevents reentrant replay.
 public sealed class ItemConstructorBE0Invocation {
  public const uint EntryRva=0x3238BE,NextRva=0x323968,WrapperCallRva=0x3238DA,WrapperEntryRva=0xEBDE0;
  public const uint AllocatorCallRva=0xEBE41,AllocatorRva=0x2DC900,IteratorRva=0x2DCC3C;
  public const uint PreElementCheckCallRva=0x2DCC75,PreElementCheckTargetRva=0x2DD4C0,PreElementCheckSlotRva=0x302D38;
  public const ulong ElementConstructorToken=0x1800EBEB0UL,FifthArgumentToken=0x1800EBF00UL;
  public readonly ItemConstructorBE0Result Result;
  public bool HasRun {get;private set;}
  readonly ItemConstructorStorage state;
  readonly ItemBE0ArrayStorage array;
  public ItemConstructorBE0Invocation(ItemConstructorBA8Result prior,ItemBE0ArrayStorage knownInitialAllocation){
   Result=new ItemConstructorBE0Result(prior,knownInitialAllocation);state=Result.State;array=knownInitialAllocation;
  }
  public ItemConstructorBE0Result Run(IItemBE0CheckSource checks=null){
   if(HasRun)throw new InvalidOperationException("This ItemBE0 continuation has already been attempted");
   HasRun=true;
   ItemConstructorBA8Result prior=Result.Prior;
   if(prior==null||state==null)return Stop("MissingPriorItemBA8Prefix",EntryRva,"Prior");
   if(!prior.ReachedItem3238BE||prior.Status!="ConditionalItemPrefix"||prior.ResumeInstructionRva!=EntryRva)
    return Stop("PriorItemBA8PrefixIncomplete",prior.ResumeInstructionRva,"Prior");
   if(state.Token==0||state.Token>ulong.MaxValue-(ulong)(state.ByteCount-1))
    return Stop("InvalidItemStorageIdentity",EntryRva,"Prior");
   Result.Stage="CallerPrefix";
   // RSI=-1; the narrow ESI store does not overwrite adjacent bytes. EDX is
   // formed by LEA [RSI+3], yielding count2, not a memory read or a type switch.
   if(!Store(0xBD8,4,0xFFFFFFFFUL,0x3238C2))return Result;Result.CallerPrefixStores++;
   Result.CallSiteRva=WrapperCallRva;Result.Stage="HookPackHeader";
   ulong originalSubobject=state.Token+0xBE0UL;
   if(!Header(0xBE0,8,0x180339B70UL,0xEBE06))return Result;
   if(!Header(0xBE8,4,0,0xEBE0B))return Result;
   if(!Header(0xBF0,8,0,0xEBE0E))return Result;
   if(!Header(0xBF8,4,2,0xEBE12))return Result;
   if(!Header(0xBFC,1,0,0xEBE15))return Result;
   // EBDE0 saves EDX in EBP before the callbacks:2*128+8=264. Subsequent
   // changes to [this+18], the cookie or pointer field cannot change this loop.
   Result.RequestedAllocationBytes=ItemBE0ArrayStorage.RequestedBytes;
   Result.Stage="Allocation";Result.ResumeInstructionRva=AllocatorCallRva;
   if(array==null)return Stop("MissingKnownInitialBE0Allocation",AllocatorCallRva,"Allocation");
   Result.KnownInitialAllocationUsed=true;
   // Explicit successful initial malloc + valid exclusive264B storage + normal
   // memset return. Null observation does not select the native null arm:
   // the allocator helper may retry/new-handler/throw before a normal return.
   array.ZeroRequestedSpan();Result.BulkZeroBytes=ItemBE0ArrayStorage.RequestedBytes;
   array.Store(0,8,ItemBE0ArrayStorage.ElementCount);Result.ArrayStoreInstructions=1;
   for(int index=0;index<ItemBE0ArrayStorage.ElementCount;index++){
    Result.PendingElementIndex=index;Result.Stage="PreElementCheck";Result.ResumeInstructionRva=PreElementCheckCallRva;
    if(checks==null)return Stop("UnresolvedItemBE0PreElementCheck",PreElementCheckCallRva,"PreElementCheck");
    Result.Reason="ItemBE0CheckNotCompleted";Result.CheckSourceCalls++;
    ItemBE0CheckObservation observed=checks.Check(state,array,index,ElementConstructorToken);
    if(observed==null)return Stop("MissingItemBE0CheckObservation",PreElementCheckCallRva,"PreElementCheck");
    if(!observed.ReturnedNormally)return Stop(observed.Reason??"UnresolvedItemBE0PreElementCheck",PreElementCheckCallRva,"PreElementCheck");
    Result.NormalCheckReturns++;Result.Stage="ElementConstructor";Result.ResumeInstructionRva=0xEBEB0;
    ApplyElement(index);Result.ArrayStoreInstructions+=14;Result.ConstructedElements++;
   }
   Result.PendingElementIndex=null;Result.Stage="Publish";
   if(!Store(0xBF0,8,array.ElementsToken,0xEBE8B))return Result;Result.PublishedPointerStores++;
   Result.ArrayPublished=true;Result.ReturnedSubobjectToken=originalSubobject;Result.WrapperReturned=true;
   // The caller uses its original saved Item+BE0 (RBX), ignoring returned RAX.
   Result.CallSiteRva=null;Result.Stage="CallerSuffix";
   if(!Suffix(0xBE0,8,0x180770AD8UL,0x3238E7))return Result;
   if(!Suffix(0xC00,1,0,0x3238EA))return Result;
   if(!Suffix(0xC02,1,0,0x3238F1))return Result;
   if(!Suffix(0xC04,2,0,0x3238F8))return Result;
   if(!Suffix(0xC06,1,0,0x323901))return Result;
   if(!Suffix(0xC08,4,0xFFFFFFFFUL,0x323908))return Result;
   // No callback intervenes between the new capacity15 and native inline test.
   // Do not initialize the other15 inline bytes or the unmentioned padding.
   if(!Suffix(0xC28,8,15,0x323915))return Result;
   if(!Suffix(0xC20,8,0,0x32391D))return Result;
   if(!Suffix(0xC10,1,0,0x32392B))return Result;
   if(!Suffix(0xC30,8,0,0x32392E))return Result;
   if(!Suffix(0xC38,8,0,0x323935))return Result;
   if(!Suffix(0xC40,8,0,0x32393C))return Result;
   if(!Suffix(0xC48,8,0,0x323943))return Result;
   if(!Suffix(0xC50,8,0,0x32394A))return Result;
   if(!Suffix(0xC58,8,0,0x323951))return Result;
   if(!Suffix(0xC60,4,0,0x323958))return Result;
   if(!Suffix(0xC64,1,0,0x32395E))return Result;
   Result.ReachedItem323968=true;Result.Status="ConditionalItemPrefix";Result.Reason=null;
   Result.Stage="Item323968";Result.ResumeInstructionRva=NextRva;return Result;
  }
  void ApplyElement(int index){
   int at=ItemBE0ArrayStorage.ElementOffset(index);
   // Saved EBEB0 normal body:14 stores, no calls. Neither padding nor whole
   // elements are cleared here; initial zeros came from the explicit memset.
   array.Store(at+0x18,8,15);array.Store(at+0x10,8,0);array.Store(at,1,0);
   array.Store(at+0x30,8,0);array.Store(at+0x38,8,15);array.Store(at+0x20,1,0);
   array.Store(at+0x40,8,0);array.Store(at+0x48,8,0);array.Store(at+0x50,8,0);array.Store(at+0x58,8,0);
   array.Store(at+0x60,4,0xFFFFFFFEUL);
   array.Store(at+0x68,8,0);array.Store(at+0x70,8,0);array.Store(at+0x78,8,0);
  }
  bool Header(int offset,int width,ulong value,uint rva){
   if(!Store(offset,width,value,rva))return false;Result.HeaderStores++;return true;
  }
  bool Suffix(int offset,int width,ulong value,uint rva){
   if(!Store(offset,width,value,rva))return false;Result.CallerSuffixStores++;return true;
  }
  bool Store(int offset,int width,ulong value,uint rva){
   Result.ResumeInstructionRva=rva;
   if(!state.HasRange(offset,width)){Stop("MissingItemBE0FieldStorage",rva,Result.Stage);return false;}
   state.Store(offset,width,value);return true;
  }
  ItemConstructorBE0Result Stop(string reason,uint rva,string stage){
   Result.Status="Unsupported";Result.Reason=reason;Result.ResumeInstructionRva=rva;Result.Stage=stage;return Result;
  }
 }
}
