using System;
using System.Threading;
namespace GrimDynasty.TaskItems {
 public sealed class RegistrationObject {
  // Own state fields corresponding to the observed uint reads/writes. These
  // are not native object storage, names, constructors, or a recovered ABI.
  public uint? Id2C,RawKey28;
  public RegistrationObject(uint? id2C,uint? rawKey28){Id2C=id2C;RawKey28=rawKey28;}
 }
 public sealed class RegistrationGate {
  readonly object sync=new object();
  public void Enter(){Monitor.Enter(sync);}
  public void Exit(){Monitor.Exit(sync);}
  public bool IsHeldByCurrentThread {get{return Monitor.IsEntered(sync);}}
 }
 public sealed class ObjectRegistrationState {
  public readonly RegistrationGate Gate;
  public readonly RegistrationIndex Pending128,ById30,ByRawKey70;
  // Stable own references, independent of ObjectIdAllocator and its key set.
  public ObjectRegistrationState(RegistrationGate gate,RegistrationIndex pending128,RegistrationIndex byId30,RegistrationIndex byRawKey70){
   Gate=gate;Pending128=pending128;ById30=byId30;ByRawKey70=byRawKey70;
  }
 }
 public sealed class LoadedRegistrationResult {
  public readonly string Status,Reason,LastCompletedStage,FailureStage;
  public readonly bool IdAssigned,LockAcquired,LockReleased,ReadInvoked,MayHaveEffects;
  // Null means no result was obtained at that stage. Unsupported containers
  // may retain a modified prefix; inspect their result and current own state.
  public readonly RegistrationIndexResult PendingErase,IdInsert,RawKeyInsert;
  internal LoadedRegistrationResult(string reason,string last,string failed,bool assigned,bool acquired,bool released,bool read,
   RegistrationIndexResult pending,RegistrationIndexResult id,RegistrationIndexResult raw){
   Status=reason==null?"Known":"Unsupported";Reason=reason;LastCompletedStage=last;FailureStage=failed;
   IdAssigned=assigned;LockAcquired=acquired;LockReleased=released;ReadInvoked=read;
   PendingErase=pending;IdInsert=id;RawKeyInsert=raw;
   MayHaveEffects=assigned||read||(pending!=null&&pending.MayHaveEffects)||(id!=null&&id.MayHaveEffects)||(raw!=null&&raw.MayHaveEffects);
  }
 }
 public static class LoadedObjectRegistration {
  sealed class Run {
   internal string Last="Start",Attempt="Guards";
   internal bool Assigned,Acquired,Released,Read;
   internal RegistrationIndexResult Pending,Id,Raw;
   internal LoadedRegistrationResult Result(string reason){return new LoadedRegistrationResult(reason,Last,reason==null?null:Attempt,Assigned,Acquired,Released,Read,Pending,Id,Raw);}
  }
  static string CompleteInsert(RegistrationIndexResult result,string label){
   if(result==null)return "Missing"+label+"Result";
   if(!result.Supported)return label+"Unsupported:"+result.Reason;
   // Both known true and known false proceed. Unknown is not a duplicate.
   return result.Inserted.HasValue?null:"Unknown"+label+"Flag";
  }
  static string RegisterLocked(ObjectRegistrationState state,RegistrationObject obj,uint id,Run run){
   run.Attempt="PendingErase";
   if(state.Pending128==null)return "UnknownPendingRegistrationIndex";
   run.Pending=state.Pending128.Erase(id);
   if(run.Pending==null)return "MissingPendingEraseResult";
   if(!run.Pending.Supported)return "PendingEraseUnsupported:"+run.Pending.Reason;
   run.Last="PendingEraseComplete";
   run.Attempt="IdInsert";
   if(state.ById30==null)return "UnknownObjectIdRegistrationIndex";
   run.Id=state.ById30.Insert(id,obj);string reason=CompleteInsert(run.Id,"IdInsert");
   if(reason!=null)return reason;run.Last="IdInsertComplete";
   // Engine 0x18017e34f reads this field AFTER the first insert call.
   run.Attempt="RawKeyRead";
   if(!obj.RawKey28.HasValue)return "UnknownRawRegistrationKey";
   uint rawKey=obj.RawKey28.Value;run.Last="RawKeyReadComplete";
   run.Attempt="RawKeyInsert";
   if(state.ByRawKey70==null)return "UnknownRawKeyRegistrationIndex";
   run.Raw=state.ByRawKey70.Insert(rawKey,obj);reason=CompleteInsert(run.Raw,"RawKeyInsert");
   if(reason!=null)return reason;run.Last="RawKeyInsertComplete";return null;
  }
  // Covered entry: a nonnull table has already been loaded. This does not
  // implement LoadFile, name storage, a concrete native Read, or FinishPartial.
  // The caller supplies stable object metadata and exclusive access through
  // this own gate; arbitrary concurrent writes to fields/indices are uncovered.
  public static LoadedRegistrationResult Apply(ObjectRegistrationState state,RegistrationObject obj,uint? id,object knownLoadedTable,Action<RegistrationObject,object> ownRead){
   var run=new Run();
   if(obj==null){run.Last="NoObject";return run.Result(null);}
   if(!id.HasValue)return run.Result("UnknownRegistrationId");
   if(id.Value==0){run.Last="ZeroId";return run.Result(null);}
   if(knownLoadedTable==null)return run.Result("NoKnownLoadedTable");
   obj.Id2C=id.Value;run.Assigned=true;run.Last="IdAssigned";
   run.Attempt="LockAcquire";
   if(state==null)return run.Result("UnknownObjectRegistrationState");
   if(state.Gate==null)return run.Result("UnknownRegistrationGate");
   string reason=null;
   try{
    state.Gate.Enter();run.Acquired=true;run.Last="LockAcquired";
    reason=RegisterLocked(state,obj,id.Value,run);
   }catch(Exception e){reason="OwnRegistrationOperationThrew:"+e.GetType().FullName;}
   finally{
    if(run.Acquired){
     // Releasing our Monitor even after Unsupported/managed exceptions is an
     // own cleanup policy, not evidence of native exception/rollback behavior.
     try{state.Gate.Exit();run.Released=true;}
     catch(Exception e){run.Attempt="LockRelease";reason="OwnRegistrationUnlockThrew:"+e.GetType().FullName;}
    }
   }
   if(reason!=null)return run.Result(reason);
   run.Last="LockReleased";run.Attempt="OwnRead";
   if(ownRead==null)return run.Result("UnknownOwnReadCallback");
   run.Read=true;
   try{ownRead(obj,knownLoadedTable);run.Last="ReadComplete";return run.Result(null);}
   catch(Exception e){return run.Result("OwnReadCallbackThrew:"+e.GetType().FullName);}
  }
 }
}
