using System;

namespace GrimDynasty.TaskItems {
 // An explicit OWN observation of the per-element pre-call check. A normal
 // return is a coverage condition, not proof of the loaded Game guard target,
 // its policy or a no-op. False means no normal return was supplied, not native
 // fatal/EH behavior. This never executes a native function pointer.
 public sealed class Item950CheckObservation {
  public readonly bool ReturnedNormally;
  public readonly string Reason;
  public Item950CheckObservation(bool returnedNormally,string reason=null){ReturnedNormally=returnedNormally;Reason=reason;}
 }
 public interface IItem950CheckSource {
  Item950CheckObservation Check(ItemConstructorStorage state,int index,ulong callbackToken);
 }
 public sealed class ItemConstructor950Result {
  public readonly ItemConstructorBridgeResult Prior;
  public readonly ItemConstructorStorage State;
  public string Status {get;internal set;}
  public string Reason {get;internal set;}
  public string Stage {get;internal set;}
  public uint ResumeInstructionRva {get;internal set;}
  public int CallerStoreInstructions {get;internal set;}
  public int WrapperPrefixStoreInstructions {get;internal set;}
  public int ElementStoreInstructions {get;internal set;}
  public int WrapperPostStoreInstructions {get;internal set;}
  public int TotalStoreInstructions {get{return CallerStoreInstructions+WrapperPrefixStoreInstructions+ElementStoreInstructions+WrapperPostStoreInstructions;}}
  public int CompletedElements {get;internal set;}
  public int CheckSourceCalls {get;internal set;}
  public int NormalCheckReturns {get;internal set;}
  public int? PendingElementIndex {get;internal set;}
  public bool WrapperReturned {get;internal set;}
  public ulong? ReturnedSubobjectToken {get;internal set;}
  public bool ReachedItem323811 {get;internal set;}
  public bool IsCompleteItemConstructor {get{return false;}}
  internal ItemConstructor950Result(ItemConstructorBridgeResult prior){
   Prior=prior;State=prior==null?null:prior.State;
   Status="Unsupported";Reason="NotRun";Stage="NotRun";ResumeInstructionRva=0x323743;
  }
 }
 // Same-state continuation of the already completed local Item prefix. The
 // normal body of428690 uses5DAED0; that iterator checks the constructor target
 // before EACH of ten calls. Default execution stops at the first unknown check.
 // Own synchronous check callbacks may mutate fields and each new call receives
 // the current shared state. Caller otherwise owns it exclusively. Exceptions
 // propagate with prefix effects, without native EH or rollback claims.
 public sealed class ItemConstructor950Invocation {
  public const uint EntryRva=0x323743,NextRva=0x323811;
  public const uint WrapperCallRva=0x32380C,WrapperEntryRva=0x428690;
  public const uint IteratorCallRva=0x4286C0,IteratorRva=0x5DAED0;
  public const uint PreElementCheckCallRva=0x5DAF09,PreElementCheckTargetRva=0x5DB8B8;
  public const ulong ElementConstructorToken=0x180016710UL,FifthArgumentToken=0x1800165D0UL;
  public const int ElementCount=10,ElementStride=0x28,FirstElementOffset=0x968;
  public readonly ItemConstructor950Result Result;
  public bool HasRun {get;private set;}
  readonly ItemConstructorStorage state;
  public ItemConstructor950Invocation(ItemConstructorBridgeResult prior){Result=new ItemConstructor950Result(prior);state=Result.State;}
  public ItemConstructor950Result Run(IItem950CheckSource checks=null){
   if(HasRun)throw new InvalidOperationException("This Item950 continuation has already been attempted");
   HasRun=true;
   ItemConstructorBridgeResult prior=Result.Prior;
   if(prior==null||state==null)return Stop("MissingPriorItemPrefix",EntryRva,"Prior");
   if(!prior.ReachedItem323743||prior.Status!="ConditionalItemPrefix"||prior.ResumeInstructionRva!=EntryRva)
    return Stop("PriorItemPrefixIncomplete",prior.ResumeInstructionRva,"Prior");
   // Own identity/range guard only. No native success branch is invented.
   if(state.Token==0||state.Token>ulong.MaxValue-(ulong)(state.ByteCount-1))
    return Stop("InvalidItemStorageIdentity",EntryRva,"Prior");
   Result.Stage="CallerPrefix";
   // Prior323714 established RBP=0. In exclusive own storage each freshly
   // written capacity15 makes the native inline branch known; no old pointer
   // or payload is read. These calls are capacity -> length -> first byte.
   if(!Inline(0x8A8,0x32374A,0x323752,0x323760))return Result;
   if(!Inline(0x8C8,0x32376A,0x323772,0x323780))return Result;
   if(!Inline(0x8E8,0x32378A,0x323792,0x3237A0))return Result;
   if(!Caller(0x908,8,0x18069E0A8UL,0x3237AA))return Result;
   if(!Caller(0x910,8,0x18069E0B8UL,0x3237B8))return Result;
   if(!Caller(0x918,8,0x18069E0C8UL,0x3237C6))return Result;
   if(!Caller(0x920,8,0x18069DFB8UL,0x3237D4))return Result;
   if(!Inline(0x928,0x3237E2,0x3237EA,0x3237F8))return Result;
   if(!Caller(0x948,4,0,0x3237FB))return Result;
   if(state.Token>ulong.MaxValue-0x950UL)return Stop("InvalidItem950ThisIdentity",0x323805,"WrapperPrefix");
   ulong subobjectToken=state.Token+0x950UL;
   Result.Stage="WrapperPrefix";
   if(!Store(0x950,8,0,0x4286A6))return Result;Result.WrapperPrefixStoreInstructions++;
   // RCX=this+0x18, RDX=0x28, R8=10, R9=16710; the fifth argument165D0
   // is not used in the normal iterator. Its cleanup role remains unproved.
   for(int index=0;index<ElementCount;index++){
    Result.PendingElementIndex=index;Result.Stage="PreElementCheck";
    Result.ResumeInstructionRva=PreElementCheckCallRva;
    if(checks==null)return Stop("UnresolvedItem950PreElementCheck",PreElementCheckCallRva,"PreElementCheck");
    Result.Reason="Item950CheckNotCompleted";Result.CheckSourceCalls++;
    Item950CheckObservation observed=checks.Check(state,index,ElementConstructorToken);
    if(observed==null)return Stop("MissingItem950CheckObservation",PreElementCheckCallRva,"PreElementCheck");
    if(!observed.ReturnedNormally)return Stop(observed.Reason??"UnresolvedItem950PreElementCheck",PreElementCheckCallRva,"PreElementCheck");
    Result.NormalCheckReturns++;
    Result.Stage="ElementConstructor";int element=FirstElementOffset+index*ElementStride;
    // 16710 normal body is23B/5 instructions; its9 trailing CC bytes are
    // candidate-window padding. Capacity, original-this selection, length,
    // first byte, RET. Only the three stores affect the projected fields.
    if(!Store(element+0x18,8,15,0x16710))return Result;Result.ElementStoreInstructions++;
    if(!Store(element+0x10,8,0,0x1671B))return Result;Result.ElementStoreInstructions++;
    if(!Store(element,1,0,0x16723))return Result;Result.ElementStoreInstructions++;
    // The iterator advances stride and count only after the leaf returns.
    // Its returned RAX is not tested or used to choose the next element.
    Result.CompletedElements++;
   }
   Result.PendingElementIndex=null;Result.Stage="WrapperSuffix";
   if(!Store(0xAF8,4,0,0x4286C5))return Result;Result.WrapperPostStoreInstructions++;
   // 4286CB selects the original wrapper this as RAX before the last3 stores.
   if(!Store(0xB00,8,0,0x4286CE))return Result;Result.WrapperPostStoreInstructions++;
   if(!Store(0xB08,8,0,0x4286D5))return Result;Result.WrapperPostStoreInstructions++;
   if(!Store(0xB10,8,0,0x4286DC))return Result;Result.WrapperPostStoreInstructions++;
   Result.WrapperReturned=true;Result.ReturnedSubobjectToken=subobjectToken;
   Result.ReachedItem323811=true;Result.Status="ConditionalItemPrefix";Result.Reason=null;
   Result.Stage="Item323811";Result.ResumeInstructionRva=NextRva;return Result;
  }
  bool Inline(int offset,uint capacityRva,uint lengthRva,uint byteRva){
   return Caller(offset+0x18,8,15,capacityRva)&&Caller(offset+0x10,8,0,lengthRva)&&Caller(offset,1,0,byteRva);
  }
  bool Caller(int offset,int width,ulong value,uint rva){
   if(!Store(offset,width,value,rva))return false;Result.CallerStoreInstructions++;return true;
  }
  bool Store(int offset,int width,ulong value,uint rva){
   Result.ResumeInstructionRva=rva;
   if(!state.HasRange(offset,width)){Stop("MissingItem950FieldStorage",rva,Result.Stage);return false;}
   state.Store(offset,width,value);return true;
  }
  ItemConstructor950Result Stop(string reason,uint rva,string stage){
   Result.Status="Unsupported";Result.Reason=reason;Result.ResumeInstructionRva=rva;Result.Stage=stage;return Result;
  }
 }
}
