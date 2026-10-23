using System;
namespace GrimDynasty.TaskItems {
 // The fixed Actor call has count=0 and a fresh inline destination. Other
 // length/capacity states and other 022CE0 branches are outside this model.
 public sealed class ActorEmptyAssignmentResult {
  public readonly ActorConstructorPrefixState State;
  public readonly string Status,Reason;
  public readonly int StoreInstructions;
  public bool Returned {get{return Status=="KnownReturn";}}
  public bool SourcePayloadRead {get{return false;}}
  public int CalleeCalls {get{return 0;}}
  internal ActorEmptyAssignmentResult(ActorConstructorPrefixState state,string reason){
   State=state;Reason=reason;Status=reason==null?"KnownReturn":"Unsupported";StoreInstructions=reason==null?2:0;
  }
 }
 public static class ActorEmptyAssignment {
  public const uint EntryRva=0x22CE0,ReturnRva=0x22DCF;
  public const int TargetOffset=0x250;
  public const ulong SourcePreferredIdentity=0x180307E61UL,Count=0;
  public static ActorEmptyAssignmentResult ApplyFreshTarget(ActorConstructorPrefixState state){
   if(state==null)return new ActorEmptyAssignmentResult(null,"UnknownActorFieldStorage");
   if(state.ReadLittleEndian(0x260,8)!=0UL||state.ReadLittleEndian(0x268,8)!=15UL)
    return new ActorEmptyAssignmentResult(state,"UnprovenFreshInlineTarget");
   var result=new ActorEmptyAssignmentResult(state,null);
   // The empty current alias interval cannot select 023180. Count zero fits
   // capacity15 and selects 022D9B / 022DBD, without grow/copy/source loads.
   state.Store(0x260,8,0);state.Store(0x250,1,0);
   return result;
  }
 }
 public sealed class ActorContinuationResult {
  public readonly ActorArrayConstructionResult Prior;
  public readonly ActorConstructorPrefixState State;
  public string Status {get;internal set;}
  public string Reason {get;internal set;}
  public uint ResumeInstructionRva {get;internal set;}
  public int AddedActorStoreInstructions {get;internal set;}
  public int TotalActorStoreInstructions {get{return (Prior==null?0:Prior.ActorStoreInstructions)+AddedActorStoreInstructions;}}
  public bool EmptyAssignmentReturned {get;internal set;}
  public int ConstantBytesWritten {get;internal set;}
  public string ConstantProvenance {get;internal set;}
  public ulong? RequestedAllocationBytes {get;internal set;}
  public bool KnownInitialAllocationAccepted {get;internal set;}
  public ulong? PendingConstructorThisToken {get;internal set;}
  public bool ArchivedFileInitialCheckUsed {get{return Prior!=null&&Prior.ArchivedFileInitialCheckUsed;}}
  public bool MayHaveEffects {get{return AddedActorStoreInstructions!=0||(Prior!=null&&Prior.MayHaveEffects);}}
  public bool SubobjectConstructorInvoked {get{return false;}}
  public bool IsCompleteActorConstructor {get{return false;}}
  internal ActorContinuationResult(ActorArrayConstructionResult prior){
   Prior=prior;State=prior==null?null:prior.State;Status="Unsupported";ConstantProvenance="NotUsed";
   ResumeInstructionRva=prior==null?0x259A0:prior.ResumeInstructionRva;
  }
 }
 public static class ActorConstructorContinuation {
  public const uint CallSiteRva=0x25C5F,ConstantLoadRva=0x25CA2,ConstantRva=0x34C3B0;
  public const uint AllocationCallSiteRva=0x25CBC,AllocatorRva=0x2DC398;
  public const uint PendingConstructorCallSiteRva=0x25CC9,PendingConstructorRva=0xBDBE0;
  public const ulong AllocationBytes=0x358;
  // These are only the archived 16 file bytes. No assertion is made that a
  // loaded image retains them. Each returned array is caller-owned.
  public static byte[] CopyArchivedConstant(){
   return new byte[]{0,0,0x80,0x3F,0,0,0x80,0x3F,0,0,0x80,0x3F,0,0,0x80,0x3F};
  }
  public static ActorContinuationResult ContinueUsingArchivedConstant(ActorArrayConstructionResult prior,ulong? knownInitialAllocationToken){
   return ContinueCore(prior,CopyArchivedConstant(),knownInitialAllocationToken,"ArchivedFileImage");
  }
  // An explicit observation is data, not an observation of a running game.
  // Null constant/allocation observations stop at their exact dependency.
  public static ActorContinuationResult ContinueWithObservation(ActorArrayConstructionResult prior,byte[] constantObservation,ulong? knownInitialAllocationToken){
   return ContinueCore(prior,constantObservation,knownInitialAllocationToken,"SuppliedObservation");
  }
  static ActorContinuationResult ContinueCore(ActorArrayConstructionResult prior,byte[] constantObservation,ulong? token,string provenance){
   var result=new ActorContinuationResult(prior);
   if(prior==null||prior.State==null){result.Reason="MissingPriorActorState";return result;}
   if(prior.Status!="ConditionalPrefix"||prior.ResumeInstructionRva!=0x25C55||
      !prior.BaseReturned||!prior.ArrayPublished||!prior.ArchivedFileInitialCheckUsed){
    result.Reason="PriorConstructorStageIncomplete";return result;
   }
   result.ResumeInstructionRva=CallSiteRva;
   if(constantObservation!=null&&constantObservation.Length!=16){result.Reason="InvalidConstantObservation";return result;}
   // Snapshot before writes. All own managed execution must return normally;
   // no catch/rollback/result-on-OOM promise stands in for native failure/EH.
   byte[] constant=constantObservation==null?null:(byte[])constantObservation.Clone();
   ActorEmptyAssignmentResult assignment=ActorEmptyAssignment.ApplyFreshTarget(prior.State);
   if(!assignment.Returned){result.Reason=assignment.Reason;return result;}
   result.EmptyAssignmentReturned=true;result.AddedActorStoreInstructions=2;
   ActorConstructorPrefixState state=prior.State;
   state.Store(0x3B0,4,0);state.Store(0x3B4,1,0);state.Store(0x3B8,4,0x3F800000UL);
   state.Store(0x3BC,4,0x3F000000UL);state.Store(0x3C8,1,0);state.Store(0x3C0,2,1);
   state.Store(0x460,1,0);state.Store(0x3C4,4,0);result.AddedActorStoreInstructions+=8;
   result.ResumeInstructionRva=ConstantLoadRva;
   if(constant==null){result.Reason="MissingConstantObservation";return result;}
   // One native 16-byte MOVUPS store, represented by two own qword writes.
   state.Store(0x464,8,ReadBits(constant,0));state.Store(0x46C,8,ReadBits(constant,8));
   result.AddedActorStoreInstructions++;result.ConstantBytesWritten=16;result.ConstantProvenance=provenance;
   state.Store(0x474,1,0);result.AddedActorStoreInstructions++;
   result.RequestedAllocationBytes=AllocationBytes;result.ResumeInstructionRva=AllocationCallSiteRva;
   if(!token.HasValue){result.Reason="MissingKnownInitialAllocation";return result;}
   // Only known successful INITIAL malloc is represented, with no new-handler
   // retry/reentrancy. Token bounds are own observation checks, not native ones.
   if(token.Value==0||token.Value>ulong.MaxValue-(AllocationBytes-1)){
    result.Reason="InvalidKnownInitialAllocationIdentity";return result;
   }
   result.KnownInitialAllocationAccepted=true;result.PendingConstructorThisToken=token;
   result.ResumeInstructionRva=PendingConstructorCallSiteRva;result.Status="ConditionalPrefix";result.Reason=null;
   // 0BDBE0 is NOT called or replaced with identity. +1E0 receives its return,
   // not necessarily the allocation input, so publication remains untouched.
   return result;
  }
  static ulong ReadBits(byte[] bytes,int at){ulong value=0;for(int i=0;i<8;i++)value|=(ulong)bytes[at+i]<<(8*i);return value;}
 }
}
