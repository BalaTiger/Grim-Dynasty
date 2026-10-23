using System;
namespace GrimDynasty.TaskItems {
 public sealed class FinishPartialObjectResult {
  public readonly string Status,Reason,LastCompletedStage,FailureStage;
  public readonly ObjectBaseState Instance;
  public readonly bool DiagnosticRequired,MayHaveEffects;
  public readonly RecordLoadResult Load;
  public readonly LoadedRegistrationResult Registration;
  public readonly int? CStringLength;
  public readonly ulong? TemporaryNameCapacity;
  readonly byte[] recordName;
  public byte[] CopyRecordName(){return recordName==null?null:(byte[])recordName.Clone();}
  internal FinishPartialObjectResult(string reason,string last,string failed,ObjectBaseState instance,bool diagnostic,bool uncertain,
   RecordLoadResult load,LoadedRegistrationResult registration,byte[] prefix,ulong? capacity){
   Status=reason==null?"Known":"Unsupported";Reason=reason;LastCompletedStage=last;FailureStage=reason==null?null:failed;
   Instance=instance;DiagnosticRequired=diagnostic;Load=load;Registration=registration;recordName=prefix;
   CStringLength=prefix==null?(int?)null:prefix.Length;TemporaryNameCapacity=capacity;
   MayHaveEffects=uncertain||(load!=null&&load.MayHaveEffects)||(registration!=null&&registration.MayHaveEffects);
  }
 }
 public static class FinishPartialObject {
  sealed class Run {
   internal string Last="Start",Attempt="Guards";
   internal ObjectBaseState Instance;
   internal bool Diagnostic,Uncertain;
   internal RecordLoadResult Load;
   internal LoadedRegistrationResult Registration;
   internal byte[] Prefix;
   internal ulong? Capacity;
   internal FinishPartialObjectResult Result(string reason){return new FinishPartialObjectResult(reason,Last,Attempt,Instance,Diagnostic,Uncertain,Load,Registration,Prefix,Capacity);}
  }
  // Engine 17E230: valid stable own object/string storage, explicit cache and
  // registration state, exclusive caller access, normal string allocation /
  // copy / release success. Manager/depot references are supplied inputs, not
  // recovered native manager population. ownRead is never a native Read stub.
  public static FinishPartialObjectResult Apply(ObjectRegistrationState manager,RecordDepotState depot,ObjectBaseState instance,uint? id,
   RecordNameLocale locale,Action<RegistrationObject,object> ownRead){
   var run=new Run();run.Instance=instance;
   // The caller does not touch name/depot/manager for either early return.
   if(instance==null){run.Last="NoObject";return run.Result(null);}
   if(!id.HasValue)return run.Result("UnknownRegistrationId");
   if(id.Value==0){run.Last="ZeroId";return run.Result(null);}
   try{
    run.Attempt="NameCStringRead";
    // Logical valid storage includes a terminating NUL at stored Length.
    // Length bounds safe own access; native 17E294..17E2AA scans until NUL
    // without reading stored length. Suffix bytes must not enter LoadFile.
    ObjectNameString name=instance.Name;int count=0;
    while((ulong)count<name.Length&&count<=RecordCacheSnapshot.MaximumNameBytes&&name.ByteAt(count)!=0)count++;
    if(count>RecordCacheSnapshot.MaximumNameBytes)return run.Result("FinishNameCStringOutsideCoveredDomain");
    var prefix=new byte[count];for(int i=0;i<count;i++)prefix[i]=name.ByteAt(i);
    run.Prefix=prefix;run.Capacity=count<=15?15UL:((ulong)count|15UL);
    // 022CE0 receives that explicit prefix length into fresh capacity-15
    // storage. This logical copy does not claim native heap or pointer ABI.
    var temporary=new ObjectNameString(prefix,run.Capacity.Value);run.Last="NameSnapshotCopied";
    run.Attempt="LoadFile";run.Load=RecordLoadFile.Apply(depot,temporary,locale);
    if(run.Load==null)return run.Result("MissingRecordLoadResult");
    if(run.Load.Status!="Known")return run.Result("RecordLoadUnsupported:"+run.Load.Reason);
    // Native temporary cleanup precedes the table-null check (17E2CB..2FB).
    // Its normal release contract is covered; no native free is executed or
    // managed lifetime/failure cleanup equated with native behavior here.
    run.Last="LoadFileComplete";
    if(run.Load.Table==null){
     run.Last="LoadedNullTable";run.Attempt="LoadFailureDiagnostic";run.Diagnostic=true;
     return run.Result("UnimplementedLoadFailureDiagnostic");
    }
    // Do not rehash or rename from the shorter temporary. The raw key is
    // read by the existing registration sequence after its first insertion.
    run.Attempt="Registration";
    run.Registration=LoadedObjectRegistration.Apply(manager,instance.Fields,id,run.Load.Table,ownRead);
    if(run.Registration==null)return run.Result("MissingLoadedRegistrationResult");
    if(run.Registration.Status!="Known")return run.Result("RegistrationUnsupported:"+run.Registration.Reason);
    run.Last="ReadComplete";return run.Result(null);
   }catch(Exception e){
    // Own managed reporting, not native EH/rollback. Retain completed state.
    run.Uncertain=true;return run.Result("OwnFinishPartialOperationThrew:"+e.GetType().FullName);
   }
  }
 }
}
