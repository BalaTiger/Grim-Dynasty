using System;
namespace GrimDynasty.TaskItems {
 // Own observation of one known, successful initial allocation. Tokens encode
 // identity/offset relationships only; nothing dereferences native addresses.
 // This type does not implement malloc, new-handler retries or native EH.
 public sealed class Actor198ArrayStorage {
  public const int RequestedBytes=0x208,ElementCount=4,ElementStride=0x80;
  public readonly ulong BaseToken;
  readonly byte?[] bytes;
  public Actor198ArrayStorage(ulong baseToken):this(baseToken,new byte?[RequestedBytes]){}
  public Actor198ArrayStorage(ulong baseToken,byte?[] observation){
   if(baseToken==0||baseToken>ulong.MaxValue-(RequestedBytes-1))throw new ArgumentOutOfRangeException("baseToken");
   if(observation==null)throw new ArgumentNullException("observation");
   if(observation.Length!=RequestedBytes)throw new ArgumentException("Expected the fixed allocation observation", "observation");
   BaseToken=baseToken;bytes=(byte?[])observation.Clone();
  }
  public ulong ElementsToken {get{return BaseToken+8;}}
  public byte? ReadByte(int offset){Check(offset,1);return bytes[offset];}
  public ulong? ReadLittleEndian(int offset,int width){
   Check(offset,width);ulong value=0;
   for(int i=0;i<width;i++){if(!bytes[offset+i].HasValue)return null;value|=(ulong)bytes[offset+i].Value<<(i*8);}
   return value;
  }
  public byte?[] CopyObservation(){return (byte?[])bytes.Clone();}
  public static int ElementOffset(int index){
   if(index<0||index>=ElementCount)throw new ArgumentOutOfRangeException("index");
   return 8+index*ElementStride;
  }
  static void Check(int offset,int width){
   if(width<1||width>8||offset<0||offset>RequestedBytes-width)throw new ArgumentOutOfRangeException("offset");
  }
  internal void Store(int offset,int width,ulong value){
   Check(offset,width);for(int i=0;i<width;i++)bytes[offset+i]=unchecked((byte)(value>>(i*8)));
  }
  internal void ZeroRequestedSpan(){for(int i=0;i<RequestedBytes;i++)bytes[i]=0;}
 }
 public static class Actor198ElementConstructor {
  public const uint EntryRva=0xEBEB0,ReturnRva=0xEBEF5;
  // Valid storage only. The leaf body contains fourteen field stores and no
  // callees. It does not clear the other inline bytes or padding by itself.
  public static void Apply(Actor198ArrayStorage storage,int index){
   if(storage==null)throw new ArgumentNullException("storage");
   int at=Actor198ArrayStorage.ElementOffset(index);
   storage.Store(at+0x18,8,15);storage.Store(at+0x10,8,0);storage.Store(at,1,0);
   storage.Store(at+0x30,8,0);storage.Store(at+0x38,8,15);storage.Store(at+0x20,1,0);
   storage.Store(at+0x40,8,0);storage.Store(at+0x48,8,0);storage.Store(at+0x50,8,0);storage.Store(at+0x58,8,0);
   storage.Store(at+0x60,4,0xFFFFFFFEUL);
   storage.Store(at+0x68,8,0);storage.Store(at+0x70,8,0);storage.Store(at+0x78,8,0);
  }
 }
 public sealed class ActorArrayConstructionResult {
  public readonly ActorConstructorPrefixState State;
  public readonly Actor198ArrayStorage Array;
  public string Status {get;internal set;}
  public string Reason {get;internal set;}
  public uint ResumeInstructionRva {get;internal set;}
  public int ActorStoreInstructions {get;internal set;}
  public int ArrayStoreInstructions {get;internal set;}
  public int BulkZeroBytes {get;internal set;}
  public int ConstructedElements {get;internal set;}
  public int PreElementCalls {get;internal set;}
  public bool ArrayPublished {get;internal set;}
  public bool BaseReturned {get;internal set;}
  public bool KnownInitialAllocationUsed {get;internal set;}
  public bool ArchivedFileInitialCheckUsed {get;internal set;}
  public ulong? RequestedAllocationBytes {get;internal set;}
  public bool MayHaveEffects {get{return ActorStoreInstructions!=0||ArrayStoreInstructions!=0||BulkZeroBytes!=0;}}
  public bool IsCompleteActorConstructor {get{return false;}}
  internal ActorArrayConstructionResult(ActorConstructorPrefixState state,Actor198ArrayStorage array){
   State=state;Array=array;Status="Unsupported";ResumeInstructionRva=0x259A0;
  }
 }
 public static class ActorConstructorArray {
  public const uint SubobjectEntryRva=0xEBDE0,AllocatorRva=0x2DC900,IteratorRva=0x2DCC3C;
  public const uint NextCallSiteRva=0x25C5F,NextCallTargetRva=0x22CE0;
  public const uint PreElementCallSiteRva=0x2DCC75,PreElementTargetRva=0x2DD4C0,PreElementSlotRva=0x302D38;
  public const uint ArchivedFileInitialCheckTargetRva=0xCE70;
  // The default API stops before the unresolved loaded indirect check. Supplying
  // own allocation storage establishes only the successful INITIAL malloc case;
  // it does not assert that an allocation/new-handler call actually occurred.
  public static ActorArrayConstructionResult ApplyUntilUnresolvedCheck(ActorConstructorPrefixState state,Actor198ArrayStorage knownInitialAllocation){
   var result=new ActorArrayConstructionResult(state,knownInitialAllocation);
   if(state==null){result.Reason="UnknownActorFieldStorage";return result;}
   ActorConstructorPrefix.ApplyEntryUntilFirstSubobject(state);result.ActorStoreInstructions=15;
   state.Store(0x198,8,0x180339B70UL);state.Store(0x1A0,4,0);state.Store(0x1A8,8,0);
   state.Store(0x1B0,4,4);state.Store(0x1B4,1,0);result.ActorStoreInstructions+=5;
   result.RequestedAllocationBytes=Actor198ArrayStorage.RequestedBytes;
   result.ResumeInstructionRva=0xEBE41;
   if(knownInitialAllocation==null){result.Reason="MissingKnownInitialAllocation";return result;}
   result.KnownInitialAllocationUsed=true;
   // Standard memset binding, valid exclusive storage and normal return only.
   // No CRT fault/exception, allocation retry, callback or cleanup is invented.
   knownInitialAllocation.ZeroRequestedSpan();result.BulkZeroBytes=Actor198ArrayStorage.RequestedBytes;
   knownInitialAllocation.Store(0,8,Actor198ArrayStorage.ElementCount);result.ArrayStoreInstructions=1;
   result.Reason="UnresolvedLoadedPreElementCheck";result.ResumeInstructionRva=PreElementCallSiteRva;
   return result;
  }
  // Explicit FILE-INITIALIZER profile: saved slot 302D38 -> CE70, C2 0000.
  // This data-only projection invokes no native pointer and changes no security
  // setting. The loaded guard target/validation/failure behavior is NOT known.
  // Preconditions: successful initial malloc, normal memset, valid exclusive
  // storage, and the archived file-initializer check behavior on all four calls.
  // No allocation-retry/new-handler side effects or native EH are represented.
  // Own managed execution must also return normally. A managed exception is
  // not converted to Unsupported or rolled back. In particular the reused
  // post-base API allocates its report after the array/base writes; failure of
  // that allocation can leave changed caller-owned storage without a result.
  public static ActorArrayConstructionResult ApplyWithArchivedFileInitialCheckTarget(ActorConstructorPrefixState state,Actor198ArrayStorage knownInitialAllocation){
   ActorArrayConstructionResult result=ApplyUntilUnresolvedCheck(state,knownInitialAllocation);
   if(result.Reason!="UnresolvedLoadedPreElementCheck")return result;
   result.ArchivedFileInitialCheckUsed=true;
   for(int i=0;i<Actor198ArrayStorage.ElementCount;i++){
    // The archived target returns without an additional stack adjustment or
    // object writes. This is not an assertion about the loaded check target.
    result.PreElementCalls++;
    Actor198ElementConstructor.Apply(knownInitialAllocation,i);
    result.ArrayStoreInstructions+=14;result.ConstructedElements++;
   }
   state.Store(0x1A8,8,knownInitialAllocation.ElementsToken);result.ActorStoreInstructions++;
   result.ArrayPublished=true;
   CompleteBaseFields(state);result.ActorStoreInstructions+=38;result.BaseReturned=true;
   ActorConstructorPrefix.ApplyAfterNormalBaseReturn(state);result.ActorStoreInstructions+=71;
   result.Status="ConditionalPrefix";result.Reason=null;result.ResumeInstructionRva=0x25C55;
   return result;
  }
  // 07D020 continuation after EBDE0 NORMAL return. No call remains before its
  // RET; keep byte/word/unaligned-qword widths and the actual store order.
  static void CompleteBaseFields(ActorConstructorPrefixState s){
   s.Store(0x198,8,0x18033E828UL);s.Store(0x1DC,4,0);
   s.Store(0x44,8,0);s.Store(0x4C,4,0);s.Store(0x50,8,0);s.Store(0x58,4,0);
   s.Store(0x5C,8,0);s.Store(0x64,4,0);s.Store(0x68,8,0);s.Store(0x70,4,0);
   s.Store(0x74,1,0);s.Store(0xD0,8,0);s.Store(0x128,8,0);s.Store(0x120,8,0);
   s.Store(0x118,1,1);s.Store(0x130,8,0);s.Store(0x138,4,0);s.Store(0x174,2,0x100);
   s.Store(0x178,8,0);s.Store(0x180,4,0);s.Store(0x188,4,3);s.Store(0x90,8,0);
   s.Store(0x13C,2,0);s.Store(0x13E,1,0);s.Store(0xCB,2,0);s.Store(0x18C,1,1);
   s.Store(0x40,1,0);s.Store(0x98,8,0);s.Store(0xA0,8,0);s.Store(0x11A,1,0);
   s.Store(0xC8,2,0);s.Store(0x1B8,2,0);s.Store(0x1BC,8,0);s.Store(0xCA,1,0);
   s.Store(0x184,1,0);s.Store(0x1C8,8,0);s.Store(0x1D0,8,0);s.Store(0x1D8,4,0);
  }
 }
}
