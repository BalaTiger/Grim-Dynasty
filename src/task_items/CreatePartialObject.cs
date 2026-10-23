using System;
namespace GrimDynasty.TaskItems {
 public sealed class CreatePartialObjectResult {
  public readonly string Status,Reason,LastCompletedStage,FailureStage;
  // Retain even an unsupported constructed instance; do not replace it with
  // null, undo its callback effects, or mistake a type mismatch for failure
  // to find/construct a class.
  public readonly object Instance;
  public readonly bool DiagnosticRequired,MayHaveEffects;
  public readonly RecordLoadResult Load;
  public readonly RecordTableGetterResult Getter;
  public readonly ClassFactoryResult Factory;
  public readonly ObjectNameAssignmentResult NameAssignment;
  internal CreatePartialObjectResult(string reason,string last,string failed,object instance,bool diagnostic,bool uncertainEffects,
   RecordLoadResult load,RecordTableGetterResult getter,ClassFactoryResult factory,ObjectNameAssignmentResult name){
   Status=reason==null?"Known":"Unsupported";Reason=reason;LastCompletedStage=last;FailureStage=reason==null?null:failed;
   Instance=instance;DiagnosticRequired=diagnostic;Load=load;Getter=getter;Factory=factory;NameAssignment=name;
   // Persistent effects include the load hit count, a completed Binary stale
   // cache-ID write, own callbacks and name assignment. An own
   // exception with no returned subresult is conservatively not a no-op.
   MayHaveEffects=uncertainEffects||(load!=null&&load.MayHaveEffects)||(getter!=null&&getter.MayHaveEffects)||(factory!=null&&factory.MayHaveCallbackSideEffects)||(name!=null&&name.MayHaveEffects);
  }
 }
 public static class CreatePartialObject {
  sealed class Run {
   internal string Last="Start",Attempt="LoadFile";
   internal object Instance;
   internal bool Diagnostic,UncertainEffects;
   internal RecordLoadResult Load;
   internal RecordTableGetterResult Getter;
   internal ClassFactoryResult Factory;
   internal ObjectNameAssignmentResult Name;
   internal CreatePartialObjectResult Result(string reason){return new CreatePartialObjectResult(reason,Last,Attempt,Instance,Diagnostic,UncertainEffects,Load,Getter,Factory,Name);}
  }
  // Engine 0x18017e170 caller order over own models and explicit class graph
  // callbacks. This does not identify a native class registry/constructor,
  // implement a spawn diagnostic, allocate an ID, register indices or Read.
  // Keep the original name object alive: SetObjectName uses that reference
  // after factory dispatch, not the normalized cache key or a prior copy.
  public static CreatePartialObjectResult Apply(RecordDepotState depot,ObjectNameString original,RecordNameLocale locale,ClassInfoSnapshot classes){
   return Apply(depot,original,locale,classes,null);
  }
  // Explicit caller binding for the Class literal's logical address. Bytes
  // remain fixed here; no default preferred/runtime address is manufactured.
  public static CreatePartialObjectResult Apply(RecordDepotState depot,ObjectNameString original,RecordNameLocale locale,ClassInfoSnapshot classes,ulong? classQueryAddress){
   var run=new Run();
   try{
    run.Load=RecordLoadFile.Apply(depot,original,locale);
    if(run.Load==null)return run.Result("MissingRecordLoadResult");
    if(run.Load.Status!="Known")return run.Result("RecordLoadUnsupported:"+run.Load.Reason);
    run.Last="LoadFileComplete";
    // A known null table is a caller return, including a counted null cache
    // hit. It does not require a getter or any class metadata to be known.
    if(run.Load.Table==null){run.Last="NoLoadedTable";return run.Result(null);}
    run.Attempt="ClassGetter";
    run.Getter=RecordTableGetter.GetValue(run.Load.Table,new byte[]{67,108,97,115,115,0},new TableString(new byte[0]),classQueryAddress);
    if(run.Getter==null)return run.Result("MissingClassGetterResult");
    if(run.Getter.Status!="Known")return run.Result("ClassGetterUnsupported:"+run.Getter.Reason);
    run.Last="ClassGetterComplete";run.Attempt="ClassFactory";
    // Empty C-strings still enter the factory. A known null returned pointer
    // also reaches its own null-query coverage guard; the caller's later
    // diagnostic checks are not a native precondition check for this call.
    byte[] className=run.Getter.Value==null?null:run.Getter.Value.CopyCStringBytes();
    run.Factory=ClassFactoryDispatch.Create(classes,className);
    if(run.Factory==null)return run.Result("MissingClassFactoryResult");
    run.Instance=run.Factory.Instance;
    if(run.Factory.Status!="Known")return run.Result("ClassFactoryUnsupported:"+run.Factory.Reason);
    run.Last="ClassFactoryComplete";
    if(run.Instance==null){
     run.Last="NullFactoryResult";
     // This is a C-string first-byte check, not the stored payload length.
     // A stored nonempty payload beginning in NUL needs no diagnostic.
     if(className!=null&&className.Length>0&&className[0]!=0){
      run.Diagnostic=true;run.Attempt="SpawnDiagnostic";
      return run.Result("UnimplementedSpawnDiagnostic");
     }
     return run.Result(null);
    }
    run.Attempt="SetObjectName";
    var instance=run.Instance as ObjectBaseState;
    if(instance==null)return run.Result("ConstructedObjectOutsideCoveredModel");
    run.Name=ObjectNameAssignment.Apply(instance,original);
    if(run.Name==null)return run.Result("MissingObjectNameAssignmentResult");
    if(run.Name.Status!="Known")return run.Result("ObjectNameAssignmentUnsupported:"+run.Name.Reason);
    run.Last="NameAssigned";return run.Result(null);
   }catch(Exception e){
    // Own exception reporting, not native exception propagation or rollback.
    // Keep every completed subresult and any instance already constructed.
    run.UncertainEffects=true;
    return run.Result("OwnCreatePartialOperationThrew:"+e.GetType().FullName);
   }
  }
 }
}
