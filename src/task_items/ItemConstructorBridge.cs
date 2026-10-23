using System;

namespace GrimDynasty.TaskItems {
 // Own bounded Item fields. The Actor region is the SAME live object, not a
 // copied observation. Tail length is a coverage limit, not a native allocation
 // size. Tokens and vtable values are identities only; nothing is dereferenced.
 public sealed class ItemConstructorStorage:IItemConstructorFields {
  public const int TailStart=0x530,CapturedExtent=0x8A0,MaximumTailBytes=0x370;
  public const int Continuation950Extent=0xB18,Maximum950TailBytes=0x5E8;
  public const int ContinuationBA8Extent=0xBC8,MaximumBA8TailBytes=0x698;
  public const int ContinuationBE0Extent=0xC65,MaximumBE0TailBytes=0x735;
  public readonly ulong Token;
  public readonly ActorConstructorPrefixState Actor;
  public readonly ItemConstructor858Subobject Sub858;
  public readonly ItemScalarInitializationState Scalars;
  readonly byte?[] tail;
  public int ByteCount {get{return TailStart+tail.Length;}}
  // Keep the legacy default extent, tail limit and token bound unchanged.
  public ItemConstructorStorage(ulong token,ActorConstructorPrefixState actor,byte?[] tail=null)
   :this(token,actor,tail,CapturedExtent){}
  ItemConstructorStorage(ulong token,ActorConstructorPrefixState actor,byte?[] tail,int extent){
   if(token==0||token>ulong.MaxValue-(ulong)(extent-1))throw new ArgumentOutOfRangeException("token");
   if(actor==null)throw new ArgumentNullException("actor");
   int maximumTail=extent-TailStart;
   if(tail!=null&&tail.Length>maximumTail)throw new ArgumentException("Tail exceeds captured Item extent","tail");
   Token=token;Actor=actor;this.tail=tail==null?new byte?[maximumTail]:(byte?[])tail.Clone();
   Sub858=ItemConstructor858Subobject.CreateSharedView(this);Scalars=ItemScalarInitializationState.CreateSharedView(this);
  }
  // Select this capture BEFORE running the existing bridge. The same Storage,
  // Actor and shared views then survive through the950 continuation; no completed
  // object is copied or reconstructed. Short explicit tails remain own limits.
  public static ItemConstructorStorage CreateFor950Continuation(ulong token,ActorConstructorPrefixState actor,byte?[] tail=null){
   return new ItemConstructorStorage(token,actor,tail,Continuation950Extent);
  }
  // This is the last observed write endpoint, not a WorldVec3 type-size claim.
  // Choose the capture before the bridge; every continuation keeps this state.
  public static ItemConstructorStorage CreateForBA8Continuation(ulong token,ActorConstructorPrefixState actor,byte?[] tail=null){
   return new ItemConstructorStorage(token,actor,tail,ContinuationBA8Extent);
  }
  // Capture the caller's final observed byte before323968 from the start of
  // the chain. This extends coverage, not the claimed native Item type size.
  public static ItemConstructorStorage CreateForBE0Continuation(ulong token,ActorConstructorPrefixState actor,byte?[] tail=null){
   return new ItemConstructorStorage(token,actor,tail,ContinuationBE0Extent);
  }
  public bool HasRange(int offset,int width){return offset>=0&&width>=0&&width<=ByteCount&&offset<=ByteCount-width;}
  public byte? ReadByte(int offset){Check(offset,1);return offset<TailStart?Actor.ReadByte(offset):tail[offset-TailStart];}
  public ulong? ReadLittleEndian(int offset,int width){
   Check(offset,width);ulong value=0;
   for(int i=0;i<width;i++){byte? part=ReadByte(offset+i);if(!part.HasValue)return null;value|=(ulong)part.Value<<(8*i);}
   return value;
  }
  public byte?[] CopyTailObservation(){return (byte?[])tail.Clone();}
  public void Store(int offset,int width,ulong value){
   Check(offset,width);
   // Validate the complete range before any byte changes, including a write
   // crossing530. No padding or unknown adjacent byte is filled implicitly.
   if(offset<TailStart&&width<=TailStart-offset){Actor.Store(offset,width,value);return;}
   for(int i=0;i<width;i++){
    byte part=unchecked((byte)(value>>(8*i)));int at=offset+i;
    if(at<TailStart)Actor.Store(at,1,part);else tail[at-TailStart]=part;
   }
  }
  void Check(int offset,int width){
   if(width<1||width>8||!HasRange(offset,width))throw new ArgumentOutOfRangeException("offset");
  }
 }
 public sealed class ItemSubobject0410InitializationResult {
  public readonly ItemConstructorStorage Storage;
  public readonly int Offset;
  public string Status {get;internal set;}
  public string Reason {get;internal set;}
  public int StoreInstructions {get;internal set;}
  public ulong? ReturnedThisToken {get;internal set;}
  public bool Returned {get;internal set;}
  internal ItemSubobject0410InitializationResult(ItemConstructorStorage storage,int offset){
   Storage=storage;Offset=offset;Status="Unsupported";
  }
 }
 public static class ItemSubobject0410Initialization {
  public const uint EntryRva=0x410E0,ReturnRva=0x411FC;
  public const int RequiredExtent=0x18C;
  // Source: offline-item-subobjects/0410e0-candidate-instructions.json,
  // SHA256 69fe251a5df08f4ffed626cc61fd17b61025a0ac583376e2d62d86f2a06e7e4e.
  // Only the first44 instructions, [410E0,411FD), are the285B normal body.
  // The remaining three CC bytes belong to the bounded candidate window, not
  // this execution. No call, branch or input-memory read occurs in the body.
  public static ItemSubobject0410InitializationResult Apply(ItemConstructorStorage storage,int offset){
   var result=new ItemSubobject0410InitializationResult(storage,offset);
   if(storage==null){result.Reason="UnknownItemConstructorStorage";return result;}
   if(offset!=0x538&&offset!=0x6C8){result.Reason="UnsupportedItemSubobjectOffset";return result;}
   if(!storage.HasRange(offset,RequiredExtent)){result.Reason="MissingItemSubobjectStorage";return result;}
   // Length -> capacity -> first byte, preserving all other inline bytes.
   storage.Store(offset,4,0);
   Inline(storage,offset+0x08);Inline(storage,offset+0x28);Inline(storage,offset+0x48);
   storage.Store(offset+0x68,4,0);
   Inline(storage,offset+0x70);Inline(storage,offset+0x90);Inline(storage,offset+0xB0);
   storage.Store(offset+0xD0,4,0);Inline(storage,offset+0xD8);
   storage.Store(offset+0xF8,8,0);
   Inline(storage,offset+0x100);Inline(storage,offset+0x120);Inline(storage,offset+0x140);
   storage.Store(offset+0x160,4,0);storage.Store(offset+0x164,1,1);
   storage.Store(offset+0x168,8,0);storage.Store(offset+0x170,8,0);
   storage.Store(offset+0x180,8,0);storage.Store(offset+0x188,4,0);
   // 411EE selects the original RCX as the return identity BEFORE the final
   // store411F1. There is no return until that QWORD+178 store has completed.
   ulong returnedThis=storage.Token+(ulong)offset;storage.Store(offset+0x178,8,1);
   result.StoreInstructions=41;result.ReturnedThisToken=returnedThis;result.Returned=true;
   result.Status="KnownReturn";return result;
  }
  static void Inline(ItemConstructorStorage storage,int offset){
   storage.Store(offset+0x10,8,0);storage.Store(offset+0x18,8,15);storage.Store(offset,1,0);
  }
 }
 public sealed class ItemConstructorBridgeResult {
  public readonly ItemConstructorStorage State;
  public ActorArrayConstructionResult ActorArray {get;internal set;}
  public ActorContinuationResult ActorContinuation {get;internal set;}
  public ActorCompletionResult ActorCompletion {get;internal set;}
  public ItemSubobject0410InitializationResult FirstSubobject {get;internal set;}
  public ItemSubobject0410InitializationResult SecondSubobject {get;internal set;}
  public ItemConstructor858PrefixResult Item858 {get;internal set;}
  public int ItemVtableStores {get;internal set;}
  public string Status {get;internal set;}
  public string Reason {get;internal set;}
  public uint ResumeInstructionRva {get;internal set;}
  public string Stage {get;internal set;}
  public bool ReachedItem323743 {get;internal set;}
  public bool IsCompleteItemConstructor {get{return false;}}
  internal ItemConstructorBridgeResult(ItemConstructorStorage state){
   State=state;Status="Unsupported";Reason="NotRun";Stage="NotRun";ResumeInstructionRva=0x323690;
  }
 }
 // One own invocation, including a stopped or throwing attempt. The explicit
 // archived Actor profile retains all its allocation, synchronization, matrix,
 // global and file-initializer limitations. This is not a complete Item ctor,
 // factory or native EH implementation. Own exceptions propagate; previously
 // written state is not rolled back and a result-on-OOM contract is not made.
 public sealed class ItemConstructorBridgeInvocation {
  readonly ItemConstructorStorage state;
  public readonly ItemConstructorBridgeResult Result;
  public bool HasRun {get;private set;}
  public ItemConstructorBridgeInvocation(ItemConstructorStorage state){this.state=state;Result=new ItemConstructorBridgeResult(state);}
  public ItemConstructorBridgeResult RunWithArchivedActorProfile(Actor198ArrayStorage array,
   ActorConstructorStorage subobject,ActorConstructorStorage sentinel,ActorCompletionObservations data,
   IActorMatrixStateSource matrix,IActorGlobalStateSource global){
   Begin();if(state==null)return Stop("UnknownItemConstructorStorage",0x323690,"Storage");
   Result.Stage="ActorArray";
   Result.ActorArray=ActorConstructorArray.ApplyWithArchivedFileInitialCheckTarget(state.Actor,array);
   if(Result.ActorArray.Status!="ConditionalPrefix"||Result.ActorArray.ResumeInstructionRva!=0x25C55||
      !Result.ActorArray.BaseReturned||!Result.ActorArray.ArrayPublished||!Result.ActorArray.ArchivedFileInitialCheckUsed)
    return Stop(Result.ActorArray.Reason??"ActorArrayStageIncomplete",Result.ActorArray.ResumeInstructionRva,"ActorArray",Result.ActorArray.Status);
   Result.Stage="ActorContinuation";
   Result.ActorContinuation=ActorConstructorContinuation.ContinueUsingArchivedConstant(Result.ActorArray,
    subobject==null?(ulong?)null:subobject.BaseToken);
   if(Result.ActorContinuation.Status!="ConditionalPrefix"||Result.ActorContinuation.ResumeInstructionRva!=0x25CC9||
      !Result.ActorContinuation.KnownInitialAllocationAccepted)
    return Stop(Result.ActorContinuation.Reason??"ActorContinuationStageIncomplete",Result.ActorContinuation.ResumeInstructionRva,"ActorContinuation",Result.ActorContinuation.Status);
   Result.Stage="ActorCompletion";
   Result.ActorCompletion=ActorConstructorCompletion.ContinueWithStateSources(Result.ActorContinuation,state.Token,
    subobject,sentinel,data,matrix,global);
   return AfterActor(Result.ActorCompletion);
  }
  public ItemConstructorBridgeResult ContinueAfterActor(ActorCompletionResult actorResult){
   Begin();if(state==null)return Stop("UnknownItemConstructorStorage",0x323690,"Storage");
   return AfterActor(actorResult);
  }
  void Begin(){
   if(HasRun)throw new InvalidOperationException("This Item constructor invocation has already been attempted");
   HasRun=true;
  }
  ItemConstructorBridgeResult AfterActor(ActorCompletionResult actorResult){
   Result.Stage="ActorCompletion";Result.ActorCompletion=actorResult;
   if(actorResult==null)return Stop("MissingActorCompletion",0x3236B3,"ActorCompletion");
   Result.ActorContinuation=actorResult.Prior;Result.ActorArray=actorResult.Prior==null?null:actorResult.Prior.Prior;
   if(!Object.ReferenceEquals(actorResult.State,state.Actor))return Stop("ActorStateIdentityMismatch",0x3236B3,"ActorCompletion");
   if(!actorResult.NormalReturnReached||actorResult.Status!="ConditionalNormalReturn"||actorResult.ResumeInstructionRva!=0x25D8C)
    return Stop(actorResult.Reason??"ActorConstructorStageIncomplete",actorResult.ResumeInstructionRva,"ActorCompletion",actorResult.Status);
   // This is an OWN result-consistency guard, not a native RAX test. Item's
   // caller ignores Actor RAX and continues to use the original saved RDI.
   if(actorResult.ReturnedActorToken!=state.Token)return Stop("ActorReturnIdentityMismatch",0x3236B9,"ActorCompletion");
   Result.Stage="ItemVtables";
   state.Store(0,8,0x18076A4C0UL);Result.ItemVtableStores++;
   state.Store(0x30,8,0x1806D5B18UL);Result.ItemVtableStores++;
   state.Store(0x38,8,0x18079F408UL);Result.ItemVtableStores++;
   if(!state.HasRange(0x530,8))return Stop("MissingItemVtableStorage",0x3236E1,"ItemVtables");
   state.Store(0x530,8,0x180771F40UL);Result.ItemVtableStores++;
   Result.Stage="FirstSubobject";Result.FirstSubobject=ItemSubobject0410Initialization.Apply(state,0x538);
   if(!Result.FirstSubobject.Returned)return Stop(Result.FirstSubobject.Reason,0x3236EF,"FirstSubobject");
   Result.Stage="SecondSubobject";Result.SecondSubobject=ItemSubobject0410Initialization.Apply(state,0x6C8);
   if(!Result.SecondSubobject.Returned)return Stop(Result.SecondSubobject.Reason,0x3236FC,"SecondSubobject");
   Result.Stage="Item858";
   Result.Item858=ItemConstructor858Prefix.Apply(state.HasRange(0x858,0x18)?state.Sub858:null,
    state.HasRange(0x870,0x30)?state.Scalars:null);
   if(Result.Item858.Status!="Known"){
    uint rva=Result.Item858.NextInstructionAddress.HasValue?
     checked((uint)(Result.Item858.NextInstructionAddress.Value-0x180000000UL)):0x323702;
    return Stop(Result.Item858.Reason,rva,"Item858",Result.Item858.Status);
   }
   Result.Status="ConditionalItemPrefix";Result.Reason=null;Result.Stage="Item323743";
   Result.ResumeInstructionRva=0x323743;Result.ReachedItem323743=true;return Result;
  }
  ItemConstructorBridgeResult Stop(string reason,uint rva,string stage,string status="Unsupported"){
   Result.Status=status;Result.Reason=reason;Result.ResumeInstructionRva=rva;Result.Stage=stage;return Result;
  }
 }
}
