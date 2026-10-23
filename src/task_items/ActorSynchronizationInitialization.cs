using System;
using System.Collections.Generic;
namespace GrimDynasty.TaskItems {
 // Projection of Engine 2DC908's observed caller stores and call order only.
 // No OS object, native function pointer, TLS or real thread is created here.
 public enum ActorSyncInitOperation {
  InitializeCriticalSection, GetModule, GetInitializeCallback, GetWaitCallback,
  GetWakeCallback, CheckIndirectTarget, InitializeCondition, CreateEvent,
  RegisterCleanup, FatalHandler
 }
 public sealed class ActorSyncInitCall {
  public readonly ActorSyncInitOperation Operation;
  public readonly uint Rva;
  readonly ulong[] arguments;
  public ActorSyncInitCall(ActorSyncInitOperation operation,uint rva,params ulong[] args){
   Operation=operation;Rva=rva;arguments=(ulong[])args.Clone();
  }
  public ulong[] Arguments {get{return (ulong[])arguments.Clone();}}
 }
 public sealed class ActorSyncCallObservation {
  public readonly bool ReturnedNormally;
  public readonly ulong? Value;
  ActorSyncCallObservation(bool returned,ulong? value){ReturnedNormally=returned;Value=value;}
  public static ActorSyncCallObservation Returned(ulong? value){return new ActorSyncCallObservation(true,value);}
  public static ActorSyncCallObservation Unresolved(){return new ActorSyncCallObservation(false,null);}
 }
 public interface IActorSyncInitializationCalls {
  ActorSyncCallObservation Invoke(ActorSyncInitCall call,ActorSyncInitializationState state);
 }
 public sealed class ActorSyncSlotStore {
  public readonly uint InstructionRva,SlotRva;
  public readonly ulong? Value;
  internal ActorSyncSlotStore(uint instruction,uint slot,ulong? value){InstructionRva=instruction;SlotRva=slot;Value=value;}
 }
 public sealed class ActorSyncInitializationState {
  // Mutable observations allow an own callback to update the cookie before the
  // native caller's post-callback read. They are not observations of Engine RAM.
  public ulong? Cookie {get;set;}
  public ulong? EventSlot {get;private set;}
  public ulong? EncodedWait {get;private set;}
  public ulong? EncodedWake {get;private set;}
  public bool CriticalSectionCallReturned {get;internal set;}
  public bool ConditionCallReturned {get;internal set;}
  public string Provenance {get;private set;}
  readonly List<ActorSyncSlotStore> stores=new List<ActorSyncSlotStore>();
  public ActorSyncInitializationState(ulong? cookie,ulong? eventSlot,ulong? wait,ulong? wake){
   Cookie=cookie;EventSlot=eventSlot;EncodedWait=wait;EncodedWake=wake;Provenance="SuppliedOwnObservations";
  }
  public static ActorSyncInitializationState CreateOwnStorage(ulong cookie){
   var state=new ActorSyncInitializationState(cookie,0,0,0);
   state.Provenance="OwnZeroedSlotsWithExplicitCookie";return state;
  }
  public ActorSyncSlotStore[] Stores {get{return stores.ToArray();}}
  internal void Write(uint instruction,uint slot,ulong? value){
   if(slot==0x420DB8)EventSlot=value;
   else if(slot==0x420DC0)EncodedWait=value;
   else if(slot==0x420DC8)EncodedWake=value;
   else throw new ArgumentException("Unproved slot");
   stores.Add(new ActorSyncSlotStore(instruction,slot,value));
  }
 }
 public sealed class ActorSyncInitializationResult {
  public readonly bool ReturnedNormally;
  public readonly string Path,Reason;
  public readonly uint StopRva;
  public readonly int? ReturnValue;
  public bool IsNativeSynchronizationReady {get{return false;}}
  internal ActorSyncInitializationResult(bool returned,string path,string reason,uint stop){
   ReturnedNormally=returned;Path=path;Reason=reason;StopRva=stop;ReturnValue=returned?(int?)0:null;
  }
 }
 public sealed class ActorSynchronizationInitialization {
  readonly ActorSyncInitializationState state;
  readonly IActorSyncInitializationCalls calls;
  readonly List<ActorSyncInitCall> trace=new List<ActorSyncInitCall>();
  bool started;
  ActorSyncInitializationResult stopped;
  public ActorSynchronizationInitialization(ActorSyncInitializationState state,IActorSyncInitializationCalls calls){
   if(state==null||calls==null)throw new ArgumentNullException();this.state=state;this.calls=calls;
  }
  public ActorSyncInitCall[] Trace {get{return trace.ToArray();}}
  // Only integer encoding. The tokens are never converted to callable addresses.
  public static ulong Encode(ulong target,ulong cookie){
   int count=(int)(cookie&63UL);
   return ((target<<count)|(target>>((64-count)&63)))^cookie;
  }
  public static ulong Decode(ulong encoded,ulong cookie){
   int count=(int)(cookie&63UL);ulong value=encoded^cookie;
   return (value>>count)|(value<<((64-count)&63));
  }
  ActorSyncCallObservation Call(ActorSyncInitOperation op,uint rva,params ulong[] args){
   var call=new ActorSyncInitCall(op,rva,args);trace.Add(call);
   var observed=calls.Invoke(call,state);
   if(observed==null||!observed.ReturnedNormally){
    stopped=Stop("UnresolvedExternalReturn",rva);return null;
   }
   return observed;
  }
  ActorSyncInitializationResult Stop(string reason,uint rva){return new ActorSyncInitializationResult(false,"Stopped",reason,rva);}
  ActorSyncInitializationResult Fatal(uint callRva,uint trapRva){
   // A call followed by INT3 is not a recoverable return code. No trap is executed.
   if(Call(ActorSyncInitOperation.FatalHandler,callRva,7)==null)return stopped;
   return Stop("FatalHandlerReturnedToInt3",trapRva);
  }
  public ActorSyncInitializationResult Run(){
   if(started)throw new InvalidOperationException("Use a fresh invocation for each initializer call");started=true;
   // 2DC926's return value is ignored; normal return does not prove a valid lock.
   if(Call(ActorSyncInitOperation.InitializeCriticalSection,0x2DC926,0x420D88,4000,0)==null)return stopped;
   state.CriticalSectionCallReturned=true;
   var module=Call(ActorSyncInitOperation.GetModule,0x2DC932,0x307388);
   if(module==null)return stopped;
   if(!module.Value.HasValue)return Stop("UnknownModuleResult",0x2DC93B);
   if(module.Value.Value==0)return Fatal(0x2DCA15,0x2DCA1A);
   var initialize=Call(ActorSyncInitOperation.GetInitializeCallback,0x2DC94E,module.Value.Value,0x3073A8);
   if(initialize==null)return stopped;
   var wait=Call(ActorSyncInitOperation.GetWaitCallback,0x2DC961,module.Value.Value,0x3073C8);
   if(wait==null)return stopped;
   var wake=Call(ActorSyncInitOperation.GetWakeCallback,0x2DC974,module.Value.Value,0x3073E8);
   if(wake==null)return stopped;
   // Native checks occur only after all three queries. Respect their short circuit
   // order: a known earlier zero can select fallback despite a later unknown.
   bool fallback;
   if(!initialize.Value.HasValue)return Stop("UnknownInitializeCallback",0x2DC97D);
   if(initialize.Value.Value==0)fallback=true;
   else {
    if(!wait.Value.HasValue)return Stop("UnknownWaitCallback",0x2DC982);
    if(wait.Value.Value==0)fallback=true;
    else {
     if(!wake.Value.HasValue)return Stop("UnknownWakeCallback",0x2DC987);
     fallback=wake.Value.Value==0;
    }
   }
   if(fallback){
    var created=Call(ActorSyncInitOperation.CreateEvent,0x2DC9FC,0,1,0,0);
    if(created==null)return stopped;
    state.Write(0x2DCA02,0x420DB8,created.Value);
    if(!created.Value.HasValue)return Stop("UnknownEventResult",0x2DCA09);
    if(created.Value.Value==0)return Fatal(0x2DCA20,0x2DCA25);
   } else {
    state.Write(0x2DC98C,0x420DB8,0);
    if(Call(ActorSyncInitOperation.CheckIndirectTarget,0x2DC997,initialize.Value.Value)==null)return stopped;
    if(Call(ActorSyncInitOperation.InitializeCondition,0x2DC9A3,initialize.Value.Value,0x420DB0)==null)return stopped;
    state.ConditionCallReturned=true;
    // Read AFTER the callback. Preserve event clear and callback prefix on unknown.
    if(!state.Cookie.HasValue)return Stop("UnknownCookieAfterCallback",0x2DC9A5);
    ulong cookie=state.Cookie.Value;
    state.Write(0x2DC9C4,0x420DC0,Encode(wait.Value.Value,cookie));
    state.Write(0x2DC9CB,0x420DC8,Encode(wake.Value.Value,cookie));
   }
   // Cleanup is registered, not called. Its return (including -1) is ignored.
   if(Call(ActorSyncInitOperation.RegisterCleanup,0x2DC9D9,0x2DCA28)==null)return stopped;
   return new ActorSyncInitializationResult(true,fallback?"Event":"ConditionVariable","",0);
  }
 }
}
