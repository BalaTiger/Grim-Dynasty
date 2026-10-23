using System;

namespace GrimDynasty.TaskItems {
 public sealed class WorldVec3DefaultConstructorResult {
  public readonly ItemConstructorStorage Storage;
  public readonly int Offset;
  public int StoreInstructions {get;internal set;}
  public ulong? ReturnedThisToken {get;internal set;}
  public bool Returned {get;internal set;}
  public string Status {get;internal set;}
  public string Reason {get;internal set;}
  public uint ResumeInstructionRva {get;internal set;}
  internal WorldVec3DefaultConstructorResult(ItemConstructorStorage storage,int offset){
   Storage=storage;Offset=offset;Status="Unsupported";ResumeInstructionRva=WorldVec3DefaultConstructor.EntryRva;
  }
 }
 public static class WorldVec3DefaultConstructor {
  public const uint EntryRva=0x228AB0,ReturnRva=0x228ABA;
  // The exact exported constructor entry has no exact .pdata start. Its saved
  // 48B candidate contains this complete11B normal prefix, five CC bytes and
  // subsequent linear code cut at the window end. The whole-window parser
  // report remains failed; only bytes48c70100000000488bc1c3 are projected here.
  // They store one QWORD, return the original RCX, and do not read old fields,
  // call anything, or clear a presumed24B type. No loaded-IAT behavior is proved.
  public static WorldVec3DefaultConstructorResult Apply(ItemConstructorStorage storage,int offset){
   var result=new WorldVec3DefaultConstructorResult(storage,offset);
   if(storage==null){result.Reason="UnknownItemConstructorStorage";return result;}
   if(offset!=0xBA8&&offset!=0xBC0){result.Reason="UnsupportedWorldVec3Offset";return result;}
   if(!storage.HasRange(offset,8)){result.Reason="MissingWorldVec3Storage";return result;}
   storage.Store(offset,8,0);result.StoreInstructions=1;
   result.ReturnedThisToken=storage.Token+(ulong)offset;result.Returned=true;
   result.Status="KnownReturn";result.ResumeInstructionRva=ReturnRva;return result;
  }
 }
 public sealed class ItemConstructorBA8Result {
  public readonly ItemConstructor950Result Prior;
  public readonly ItemConstructorStorage State;
  public int CallerStoreInstructions {get;internal set;}
  public WorldVec3DefaultConstructorResult FirstSubobject {get;internal set;}
  public WorldVec3DefaultConstructorResult SecondSubobject {get;internal set;}
  public int TotalStoreInstructions {get{return CallerStoreInstructions+
   (FirstSubobject==null?0:FirstSubobject.StoreInstructions)+(SecondSubobject==null?0:SecondSubobject.StoreInstructions);}}
  public string Stage {get;internal set;}
  public string Status {get;internal set;}
  public string Reason {get;internal set;}
  public uint ResumeInstructionRva {get;internal set;}
  public uint? CallSiteRva {get;internal set;}
  public bool ReachedItem3238BE {get;internal set;}
  public bool IsCompleteItemConstructor {get{return false;}}
  internal ItemConstructorBA8Result(ItemConstructor950Result prior){
   Prior=prior;State=prior==null?null:prior.State;
   Status="Unsupported";Reason="NotRun";Stage="NotRun";ResumeInstructionRva=ItemConstructorBA8Invocation.EntryRva;
  }
 }
 // One attempt, on the same own fields, conditional on the Game import calling
 // the archived Engine constructor body. This does not load a DLL, resolve the
 // runtime IAT, execute native code, or complete the Item constructor. Missing
 // captured bytes are own coverage stops, not invented native failure branches.
 // There are no callbacks here. Own exceptions propagate without rollback or
 // a native EH/result-on-OOM promise; previously completed writes remain.
 public sealed class ItemConstructorBA8Invocation {
  public const uint EntryRva=0x323811,NextRva=0x3238BE;
  public const uint FirstCallSiteRva=0x3238AB,SecondCallSiteRva=0x3238B8;
  public readonly ItemConstructorBA8Result Result;
  public bool HasRun {get;private set;}
  readonly ItemConstructorStorage state;
  public ItemConstructorBA8Invocation(ItemConstructor950Result prior){Result=new ItemConstructorBA8Result(prior);state=Result.State;}
  public ItemConstructorBA8Result RunWithArchivedWorldVec3Body(){
   if(HasRun)throw new InvalidOperationException("This ItemBA8 continuation has already been attempted");
   HasRun=true;
   ItemConstructor950Result prior=Result.Prior;
   if(prior==null||state==null)return Stop("MissingPriorItem950Prefix",EntryRva,"Prior");
   if(!prior.ReachedItem323811||prior.Status!="ConditionalItemPrefix"||prior.ResumeInstructionRva!=EntryRva)
    return Stop("PriorItem950PrefixIncomplete",prior.ResumeInstructionRva,"Prior");
   // Own identity consistency only; no native return-value check is inserted.
   if(state.Token==0||state.Token>ulong.MaxValue-(ulong)(state.ByteCount-1))
    return Stop("InvalidItemStorageIdentity",EntryRva,"Prior");
   Result.Stage="CallerPrefix";
   // The saved caller starts with NOP323811; RBP is the established zero.
   // Preserve all unmentioned bytes, including gaps beside the narrow writes.
   // Source: offline-item-ba8-caller-import/323811-instructions.json,
   // SHA2565ab09b30029d33231d2d144326a7e9102b75dfdc209dcd85640bf1cd1dfb89cc.
   if(!Caller(0xB20,4,0,0x323812))return Result;
   if(!Caller(0xB24,2,0,0x323818))return Result;
   if(!Caller(0xB26,1,0,0x323821))return Result;
   if(!Caller(0xB28,8,0,0x323828))return Result;
   if(!Caller(0xB30,8,0,0x32382F))return Result;
   if(!Caller(0xB38,8,0,0x323836))return Result;
   if(!Caller(0xB40,2,0x0101,0x32383D))return Result;
   if(!Caller(0xB44,4,0,0x323846))return Result;
   if(!Caller(0xB48,8,0,0x32384C))return Result;
   if(!Caller(0xB50,8,0,0x323853))return Result;
   if(!Caller(0xB58,8,0,0x32385A))return Result;
   if(!Caller(0xB60,2,1,0x323861))return Result;
   if(!Caller(0xB62,1,0,0x32386A))return Result;
   if(!Caller(0xB70,8,0,0x323871))return Result;
   if(!Caller(0xB78,8,0,0x323878))return Result;
   if(!Caller(0xB80,8,0,0x32387F))return Result;
   if(!Caller(0xB88,2,0,0x323886))return Result;
   if(!Caller(0xB90,8,0,0x32388F))return Result;
   if(!Caller(0xB98,1,0,0x323896))return Result;
   if(!Caller(0xBA0,8,0,0x32389D))return Result;
   Result.Stage="FirstSubobject";Result.CallSiteRva=FirstCallSiteRva;
   Result.ResumeInstructionRva=WorldVec3DefaultConstructor.EntryRva;
   Result.FirstSubobject=WorldVec3DefaultConstructor.Apply(state,0xBA8);
   if(!Result.FirstSubobject.Returned)return Stop(Result.FirstSubobject.Reason,Result.FirstSubobject.ResumeInstructionRva,"FirstSubobject");
   // Caller keeps the original RDI, ignoring the first returned RAX, then
   // computes RCX=RDI+BC0 for the second call through the same import slot.
   Result.Stage="SecondSubobject";Result.CallSiteRva=SecondCallSiteRva;
   Result.ResumeInstructionRva=WorldVec3DefaultConstructor.EntryRva;
   Result.SecondSubobject=WorldVec3DefaultConstructor.Apply(state,0xBC0);
   if(!Result.SecondSubobject.Returned)return Stop(Result.SecondSubobject.Reason,Result.SecondSubobject.ResumeInstructionRva,"SecondSubobject");
   Result.ReachedItem3238BE=true;Result.Status="ConditionalItemPrefix";Result.Reason=null;
   Result.Stage="Item3238BE";Result.ResumeInstructionRva=NextRva;Result.CallSiteRva=null;return Result;
  }
  bool Caller(int offset,int width,ulong value,uint rva){
   Result.ResumeInstructionRva=rva;
   if(!state.HasRange(offset,width)){Stop("MissingItemBA8FieldStorage",rva,"CallerPrefix");return false;}
   state.Store(offset,width,value);Result.CallerStoreInstructions++;return true;
  }
  ItemConstructorBA8Result Stop(string reason,uint rva,string stage){
   Result.Status="Unsupported";Result.Reason=reason;Result.ResumeInstructionRva=rva;Result.Stage=stage;return Result;
  }
 }
}
