using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GrimDynasty.TaskItems {
 // Own field projection only. Token is an identity in the caller's domain; no
 // native object is allocated, dereferenced or published by this model.
 public sealed class ActorEngineContext {
  public readonly ulong Token;
  public ulong? Current668,Server678,Stub680;
  readonly List<ActorEngineFieldWrite> writes=new List<ActorEngineFieldWrite>();
  readonly ReadOnlyCollection<ActorEngineFieldWrite> view;
  public bool IsCompleteNativeEngine {get{return false;}}
  // Only stores performed by this projection are logged. Direct own callback
  // mutations of the public fields are observable but are not native stores.
  public ReadOnlyCollection<ActorEngineFieldWrite> Writes {get{return view;}}
  public ActorEngineContext(ulong token,ulong? current668,ulong? server678,ulong? stub680){
   if(token==0||token>ulong.MaxValue-0x687UL)throw new ArgumentOutOfRangeException("token");
   Token=token;Current668=current668;Server678=server678;Stub680=stub680;view=writes.AsReadOnly();
  }
  // Local postconditions of the observed ctor stores, conditional on reaching
  // them normally. This neither executes the ctor nor establishes gEngine.
  public static ActorEngineContext CreateOwnConstructedFields(ulong token){
   var state=new ActorEngineContext(token,null,null,null);
   state.Store(0x72C26,0x668,0);state.Store(0x72C34,0x678,0);state.Store(0x72C3B,0x680,0);
   return state;
  }
  internal ActorEngineFieldWrite Store(uint rva,int offset,ulong? value){
   if(offset==0x668)Current668=value;
   else if(offset==0x678)Server678=value;
   else if(offset==0x680)Stub680=value;
   else throw new ArgumentOutOfRangeException("offset");
   var write=new ActorEngineFieldWrite(rva,offset,value);writes.Add(write);return write;
  }
 }
 public sealed class ActorEngineFieldWrite {
  public readonly uint InstructionRva;
  public readonly int Offset;
  public readonly ulong? Value;
  internal ActorEngineFieldWrite(uint rva,int offset,ulong? value){InstructionRva=rva;Offset=offset;Value=value;}
 }
 public sealed class ActorEngineCall {
  public readonly string Role;
  public readonly bool IsAllocation;
  public readonly uint CallSiteRva,TargetRva;
  // AllocationSize is an allocator argument only; ThisToken is a constructor
  // argument only. A known zero allocation remains a known constructor input.
  public readonly ulong? AllocationSize,ThisToken;
  internal ActorEngineCall(string role,bool allocation,uint site,uint target,ulong? size,ulong? token){
   Role=role;IsAllocation=allocation;CallSiteRva=site;TargetRva=target;AllocationSize=size;ThisToken=token;
  }
 }
 public sealed class ActorEngineCallObservation {
  public readonly bool ReturnedNormally;
  public readonly ulong? Value;
  public ActorEngineCallObservation(bool returnedNormally,ulong? value){ReturnedNormally=returnedNormally;Value=value;}
 }
 public interface IActorEngineCallSource {
  ActorEngineCallObservation Invoke(ActorEngineCall descriptor,ActorEngineContext state);
 }
 public sealed class ActorEngineCallRecord {
  public readonly ActorEngineCall Descriptor;
  // Null while the callback is active, if it throws, or if it returns no report.
  public ActorEngineCallObservation Observation {get;internal set;}
  internal ActorEngineCallRecord(ActorEngineCall descriptor){Descriptor=descriptor;}
 }
 public sealed class ActorEngineInitializationResult {
  public readonly ActorEngineContext Context;
  public bool ProjectionReturned {get;internal set;}
  public string Reason {get;internal set;}
  public uint ResumeInstructionRva {get;internal set;}
  public ReadOnlyCollection<ActorEngineCallRecord> Calls {get;private set;}
  public ReadOnlyCollection<ActorEngineFieldWrite> Writes {get;private set;}
  internal ActorEngineInitializationResult(ActorEngineContext state,List<ActorEngineCallRecord> calls,List<ActorEngineFieldWrite> writes){
   Context=state;Reason="NotRun";ResumeInstructionRva=0x74D98;Calls=calls.AsReadOnly();Writes=writes.AsReadOnly();
  }
 }
 // One invocation of the field-relevant interval [74D98,74E4B). Dependencies
 // are explicit observations, not recovered constructor implementations. Aux
 // return values and other Engine fields are outside this projection. Caller
 // owns the context exclusively except for deliberate synchronous callbacks.
 // Own exceptions propagate, preserving already performed effects and records;
 // this makes no promise about native EH, rollback or managed allocation failure.
 public sealed class ActorEngineInitializationInvocation {
  readonly ActorEngineContext state;
  readonly IActorEngineCallSource source;
  readonly List<ActorEngineCallRecord> calls=new List<ActorEngineCallRecord>();
  readonly List<ActorEngineFieldWrite> writes=new List<ActorEngineFieldWrite>();
  public readonly ActorEngineInitializationResult Result;
  public bool HasRun {get;private set;}
  public ActorEngineInitializationInvocation(ActorEngineContext state,IActorEngineCallSource source){
   if(state==null)throw new ArgumentNullException("state");
   if(source==null)throw new ArgumentNullException("source");
   this.state=state;this.source=source;Result=new ActorEngineInitializationResult(state,calls,writes);
  }
  public ActorEngineInitializationResult Run(){
   if(HasRun)throw new InvalidOperationException("This invocation has already been attempted");
   HasRun=true;
   if(!Pair("Server",0x340,0x74D9D,0x74DAC,0x15A8B0,0x678,0x74DB2,0x74DB9))return Result;
   if(!Pair("Stub",0x2A0,0x74DBE,0x74DCD,0x15A7D0,0x680,0x74DD3,0x74DDA))return Result;
   if(!Pair("AuxFirst",0x20,0x74DDF,0x74DEE,0x172F30,0,0,0))return Result;
   if(!Pair("AuxSecond",0x20,0x74E00,0x74E0F,0x172F30,0,0,0))return Result;
   if(!Pair("AuxThird",0x20,0x74E21,0x74E30,0x172ED0,0,0,0))return Result;
   // 74E3D reads the current field after all intervening calls; a callback may
   // have changed it. Unknown remains unknown, including after this final store.
   ulong? current=state.Stub680;Write(0x74E44,0x668,current);
   Result.ProjectionReturned=true;Result.Reason=null;Result.ResumeInstructionRva=0x74E4B;return Result;
  }
  bool Pair(string role,ulong size,uint allocationSite,uint ctorSite,uint ctorTarget,int offset,uint storeSite,uint afterStore){
   ActorEngineCallObservation allocation;
   if(!Call(new ActorEngineCall(role,true,allocationSite,0x2DC398,size,null),out allocation))return false;
   if(!allocation.Value.HasValue){Stop("MissingEngineAllocationIdentity",ctorSite);return false;}
   ActorEngineCallObservation constructed;
   if(!Call(new ActorEngineCall(role,false,ctorSite,ctorTarget,null,allocation.Value),out constructed))return false;
   if(offset!=0){
    Write(storeSite,offset,constructed.Value);
    if(!constructed.Value.HasValue){Stop("MissingEngineConstructorReturnIdentity",afterStore);return false;}
   }
   return true;
  }
  bool Call(ActorEngineCall descriptor,out ActorEngineCallObservation observation){
   var record=new ActorEngineCallRecord(descriptor);calls.Add(record);
   Stop("EngineCallNotCompleted",descriptor.CallSiteRva);
   observation=source.Invoke(descriptor,state);record.Observation=observation;
   if(observation==null){Stop("MissingEngineCallObservation",descriptor.CallSiteRva);return false;}
   if(!observation.ReturnedNormally){Stop("UnresolvedEngineCallReturn",descriptor.CallSiteRva);return false;}
   return true;
  }
  void Write(uint rva,int offset,ulong? value){
   Result.ResumeInstructionRva=rva;writes.Add(state.Store(rva,offset,value));
  }
  void Stop(string reason,uint rva){Result.Reason=reason;Result.ResumeInstructionRva=rva;}
 }
 // Publish and direct Context assignment bind an OWN slot, not a recovered
 // writer of native421428. Read captures the context reference once, then its
 // two fields in Actor order. This requires exclusive own access for the whole
 // sampling operation; it provides no atomicity guarantee for native threads.
 public sealed class ActorEngineSlot:IActorGlobalStateSource {
  public ActorEngineContext Context;
  public ActorEngineSlot(ActorEngineContext context){Context=context;}
  public void Publish(ActorEngineContext context){Context=context;}
  public ActorGlobalObservation Read(){
   ActorEngineContext captured=Context;
   if(captured==null)return new ActorGlobalObservation(null,null,null);
   ulong token=captured.Token;ulong? server=captured.Server678;ulong? current=captured.Current668;
   return new ActorGlobalObservation(token,server,current);
  }
 }
}
