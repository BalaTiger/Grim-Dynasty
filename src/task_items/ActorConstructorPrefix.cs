using System;
namespace GrimDynasty.TaskItems {
 // Own observation storage for the bounded Actor field projection. Nullable
 // bytes represent unknown input; this is not native memory or an Actor ABI.
 // Vtable values below are preferred-image identity tokens, never pointers.
 public sealed class ActorConstructorPrefixState {
  public const int CapturedExtent=0x530;
  readonly byte?[] bytes;
  public ActorConstructorPrefixState(){bytes=new byte?[CapturedExtent];}
  public ActorConstructorPrefixState(byte?[] initialObservation){
   if(initialObservation==null)throw new ArgumentNullException("initialObservation");
   if(initialObservation.Length!=CapturedExtent)throw new ArgumentException("Expected bounded field projection", "initialObservation");
   bytes=(byte?[])initialObservation.Clone();
  }
  public byte? ReadByte(int offset){Check(offset,1);return bytes[offset];}
  public ulong? ReadLittleEndian(int offset,int width){
   Check(offset,width);ulong value=0;
   for(int i=0;i<width;i++){if(!bytes[offset+i].HasValue)return null;value|=(ulong)bytes[offset+i].Value<<(i*8);}
   return value;
  }
  public byte?[] CopyObservation(){return (byte?[])bytes.Clone();}
  static void Check(int offset,int width){
   if(width<1||width>8||offset<0||offset>CapturedExtent-width)throw new ArgumentOutOfRangeException("offset");
  }
  internal void Store(int offset,int width,ulong value){
   Check(offset,width);
   for(int i=0;i<width;i++)bytes[offset+i]=unchecked((byte)(value>>(i*8)));
  }
 }
 public sealed class ActorConstructorPrefixResult {
  public readonly ActorConstructorPrefixState State;
  public readonly string Status,Reason;
  public readonly int StoreInstructions;
  public uint ResumeInstructionRva {get;private set;}
  public bool MayHaveEffects {get{return StoreInstructions!=0;}}
  public bool IsCompleteConstructor {get{return false;}}
  internal ActorConstructorPrefixResult(ActorConstructorPrefixState state,string reason,int count,uint resume){
   State=state;Status=reason==null?"KnownPrefix":"Unsupported";Reason=reason;StoreInstructions=count;ResumeInstructionRva=resume;
  }
 }
 public static class ActorConstructorPrefix {
  public const uint ActorEntryRva=0x259A0,RequiredPriorCallRva=0x7D020;
  public const uint PendingCallSiteRva=0x25C5F,PendingCallTargetRva=0x22CE0;
  public const int PendingThisOffset=0x250;
  public const ulong PendingSourcePreferredIdentity=0x180307E61UL;
  public const ulong PendingLength=0;
  public const uint FirstSubobjectCallSiteRva=0x7D0DC,FirstSubobjectTargetRva=0xEBDE0;
  public const int FirstSubobjectThisOffset=0x198;
  public const uint FirstSubobjectEdx=4;
  // Actor entry 0259A0 calls 07D020 with unchanged this. This entry prefix
  // follows only its 15 object stores before the first subobject call. It does
  // NOT complete 07D020 or automatically run the post-base Actor fragment.
  public static ActorConstructorPrefixResult ApplyEntryUntilFirstSubobject(ActorConstructorPrefixState state){
   if(state==null)return new ActorConstructorPrefixResult(null,"UnknownActorFieldStorage",0,ActorEntryRva);
   var result=new ActorConstructorPrefixResult(state,null,15,0x7D0C8);
   state.Store(0,8,0x180339F78UL);EmptyInline(state,8);
   state.Store(0,8,0x18033E538UL);state.Store(0x30,8,0x18033DF48UL);state.Store(0x38,8,0x18033F078UL);
   state.Store(0x78,8,0);state.Store(0x80,8,0);state.Store(0x88,8,0);
   EmptyInline(state,0xA8);state.Store(0xD8,8,0);state.Store(0x140,4,0);
   return result;
  }
  // PRECONDITION: original base-like call 07D020 has returned normally on
  // valid, exclusively owned storage. This API does not execute or certify it.
  // Only 0259C1..025C55 is modeled. Stop before the following string helper,
  // allocator, subobject construction, constant loads and global-state read.
  // No callbacks/managed exception recovery stand in for native EH/cleanup.
  public static ActorConstructorPrefixResult ApplyAfterNormalBaseReturn(ActorConstructorPrefixState state){
   if(state==null)return new ActorConstructorPrefixResult(null,"UnknownActorFieldStorage",0,0x259C1);
   // Allocate the own result before the observed write sequence.
   var result=new ActorConstructorPrefixResult(state,null,71,0x25C55);
   state.Store(0,8,0x18033A0D8UL);state.Store(0x30,8,0x18033A098UL);state.Store(0x38,8,0x18033F078UL);
   state.Store(0x1E8,8,0);state.Store(0x1F0,8,0);state.Store(0x1F8,8,0);
   EmptyInline(state,0x208);EmptyInline(state,0x228);EmptyInline(state,0x250);EmptyInline(state,0x270);
   EmptyInline(state,0x290);EmptyInline(state,0x2B0);EmptyInline(state,0x2D0);EmptyInline(state,0x2F0);
   for(int offset=0x310;offset<=0x380;offset+=8)state.Store(offset,8,0);
   EmptyInline(state,0x388);state.Store(0x3D0,4,0xFFFFFFFFUL);EmptyInline(state,0x3D8);
   state.Store(0x418,1,0);state.Store(0x420,8,0);state.Store(0x438,8,0);
   state.Store(0x404,4,0x3F4CCCCD);state.Store(0x408,8,0xAC44);state.Store(0x3F8,8,0);
   state.Store(0x414,4,0x40B00000);state.Store(0x400,1,0);state.Store(0x410,1,0);
   EmptyInline(state,0x440);EmptyInline(state,0x4E8);
   state.Store(0x510,8,0);state.Store(0x518,8,0);state.Store(0x520,8,0);state.Store(0x200,1,0);
   return result;
  }
  static void EmptyInline(ActorConstructorPrefixState state,int offset){
   // Capacity is written before the observed comparison, so valid exclusive
   // storage takes the inline arm. Only the first payload byte is initialized.
   state.Store(offset+0x18,8,15);state.Store(offset+0x10,8,0);state.Store(offset,1,0);
  }
 }
}
