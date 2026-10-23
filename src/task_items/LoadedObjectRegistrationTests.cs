using System;
using GrimDynasty.TaskItems;
class LoadedObjectRegistrationTests {
 static int n;
 const uint Id=77,Raw=0x80000000;
 static void Need(bool value,string message){n++;if(!value)throw new Exception(message);}
 static RegistrationEntry E(uint key,object value){return new RegistrationEntry(key,value);}
 static RegistrationIndex Index(params RegistrationEntry[] entries){return new RegistrationIndex(8,8,1.0f,entries);}
 static RegistrationIndex Pending(){return Index(E(Id,new object()),E(Id,new object()),E(Id+1,new object()));}
 static ObjectRegistrationState State(RegistrationIndex pending,RegistrationIndex ids,RegistrationIndex raw){return new ObjectRegistrationState(new RegistrationGate(),pending,ids,raw);}
 static ObjectRegistrationState State(){return State(Pending(),Index(),Index());}
 static bool Contains(RegistrationIndex index,uint key){foreach(uint value in index.KeysInOrder())if(value==key)return true;return false;}
 static void Guards(){
  var result=LoadedObjectRegistration.Apply(null,null,null,null,null);
  Need(result.Status=="Known"&&result.LastCompletedStage=="NoObject"&&!result.MayHaveEffects&&!result.LockAcquired,"null object short circuits every unobserved field and dependency");
  var obj=new RegistrationObject(9,null);
  result=LoadedObjectRegistration.Apply(null,obj,0,null,null);
  Need(result.Status=="Known"&&result.LastCompletedStage=="ZeroId"&&obj.Id2C==9&&!result.IdAssigned&&!result.ReadInvoked&&!result.MayHaveEffects,"zero ID does not assign the object or require state, table, raw key, or Read");
  result=LoadedObjectRegistration.Apply(null,obj,null,null,null);
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownRegistrationId"&&obj.Id2C==9&&!result.MayHaveEffects,"unknown ID is not a known zero");
  result=LoadedObjectRegistration.Apply(null,obj,Id,null,null);
  Need(result.Status=="Unsupported"&&result.Reason=="NoKnownLoadedTable"&&obj.Id2C==9&&!result.MayHaveEffects,"table-success domain is required before the ID write");
  result=LoadedObjectRegistration.Apply(null,obj,Id,new object(),null);
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownObjectRegistrationState"&&obj.Id2C==Id&&result.IdAssigned&&!result.LockAcquired&&result.MayHaveEffects,"unknown manager preserves the ID assignment before lock acquisition");
  obj=new RegistrationObject(null,null);
  result=LoadedObjectRegistration.Apply(new ObjectRegistrationState(null,null,null,null),obj,Id,new object(),null);
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownRegistrationGate"&&obj.Id2C==Id&&result.LastCompletedStage=="IdAssigned"&&result.FailureStage=="LockAcquire","unknown gate is not rejected before writing the ID");
  var state=State(null,null,null);
  result=LoadedObjectRegistration.Apply(state,obj,Id,new object(),null);
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownPendingRegistrationIndex"&&result.LockAcquired&&result.LockReleased&&!state.Gate.IsHeldByCurrentThread&&result.PendingErase==null&&result.IdInsert==null,"unknown first container is reached under the own gate, which is then released");
 }
 static void SuccessAndDuplicates(){
  var state=State();var obj=new RegistrationObject(null,Raw);var table=new object();int reads=0;
  var result=LoadedObjectRegistration.Apply(state,obj,Id,table,delegate(RegistrationObject value,object loaded){
   reads++;
   Need(Object.ReferenceEquals(value,obj)&&Object.ReferenceEquals(loaded,table)&&value.Id2C==Id,"own Read receives this object and the exact already-loaded table");
   Need(!state.Gate.IsHeldByCurrentThread&&state.Pending128.Count==1&&state.ById30.Count==1&&state.ByRawKey70.Count==1&&Contains(state.ById30,Id)&&Contains(state.ByRawKey70,Raw),"Read runs after unlock and sees pending erasure plus both actual index insertions");
  });
  Need(result.Status=="Known"&&result.LastCompletedStage=="ReadComplete"&&result.FailureStage==null&&result.IdAssigned&&result.LockAcquired&&result.LockReleased&&result.ReadInvoked&&reads==1,"complete registration reports the successful ordered prefix");
  Need(result.PendingErase.Supported&&result.PendingErase.RemovedCount==2&&result.IdInsert.Inserted==true&&result.RawKeyInsert.Inserted==true,"pending duplicate range is erased and both unique insert flags are retained");
  Need(Object.ReferenceEquals(result.IdInsert.Value,obj)&&Object.ReferenceEquals(result.RawKeyInsert.Value,obj)&&Contains(state.Pending128,Id+1)&&!Contains(state.Pending128,Id),"new entries hold the supplied object and erasure preserves the unrelated pending node");
  result=LoadedObjectRegistration.Apply(state,obj,Id,table,delegate{reads++;});
  Need(result.Status=="Known"&&result.PendingErase.RemovedCount==0&&result.IdInsert.Inserted==false&&result.RawKeyInsert.Inserted==false&&reads==2&&state.ById30.Count==1&&state.ByRawKey70.Count==1,"repeat registration still calls Read after a nonempty pending miss and two known duplicates");
  for(int mask=1;mask<=3;mask++){
   var oldId=new object();var oldRaw=new object();obj=new RegistrationObject(123,Raw);
   var ids=(mask&1)!=0?Index(E(Id,oldId)):Index();var raw=(mask&2)!=0?Index(E(Raw,oldRaw)):Index();
   state=State(Pending(),ids,raw);RegistrationObject readObject=null;reads=0;
   result=LoadedObjectRegistration.Apply(state,obj,Id,table,delegate(RegistrationObject value,object loaded){reads++;readObject=value;});
   Need(result.Status=="Known"&&result.IdInsert.Inserted==((mask&1)==0)&&result.RawKeyInsert.Inserted==((mask&2)==0),"known duplicate flags do not abort either subsequent operation");
   Need(Object.ReferenceEquals(result.IdInsert.Value,(mask&1)!=0?oldId:(object)obj)&&Object.ReferenceEquals(result.RawKeyInsert.Value,(mask&2)!=0?oldRaw:(object)obj),"duplicate insert preserves each original mapped object without replacement");
   Need(reads==1&&Object.ReferenceEquals(readObject,obj)&&obj.Id2C==Id&&state.Pending128.Count==1&&ids.Count==1&&raw.Count==1,"Read always targets the incoming object despite either or both existing index values");
  }
  foreach(uint key in new uint[]{0,0x80000000,uint.MaxValue}){
   state=State();obj=new RegistrationObject(null,key);
   result=LoadedObjectRegistration.Apply(state,obj,Id,table,delegate{});
   Need(result.Status=="Known"&&state.ByRawKey70.KeysInOrder()[0]==key,"second index accepts the raw uint key including zero and high-bit values");
  }
  var emptyPending=new RegistrationIndex(16,16,1.0f,new RegistrationEntry[0]);state=State(emptyPending,Index(),Index());reads=0;
  result=LoadedObjectRegistration.Apply(state,new RegistrationObject(null,Raw),Id,table,delegate{reads++;});
  Need(result.Status=="Known"&&result.PendingErase.Supported&&result.PendingErase.RemovedCount==0&&emptyPending.BucketCount==8&&emptyPending.BucketCapacity==16&&result.ReadInvoked&&reads==1,"empty pending erase performs whole-chain maintenance to eight buckets without shrinking capacity, then continues to Read");
 }
 static void UnknownAfterEffects(){
  var table=new object();var obj=new RegistrationObject(null,null);var state=State();int reads=0;
  var result=LoadedObjectRegistration.Apply(state,obj,Id,table,delegate{reads++;});
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownRawRegistrationKey"&&result.FailureStage=="RawKeyRead"&&result.LastCompletedStage=="IdInsertComplete","unknown raw key is discovered only after the first insertion");
  Need(obj.Id2C==Id&&state.Pending128.Count==1&&state.ById30.Count==1&&state.ByRawKey70.Count==0&&result.PendingErase.Supported&&result.IdInsert.Inserted==true&&result.RawKeyInsert==null&&result.MayHaveEffects&&reads==0,"unknown raw key does not roll back the ID, deletion, or first inserted node");
  Need(result.LockReleased&&!state.Gate.IsHeldByCurrentThread&&!result.ReadInvoked,"own cleanup releases the gate on unsupported metadata and skips Read");
  obj=new RegistrationObject(null,Raw);state=State(Pending(),null,Index());
  result=LoadedObjectRegistration.Apply(state,obj,Id,table,delegate{reads++;});
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownObjectIdRegistrationIndex"&&result.LastCompletedStage=="PendingEraseComplete"&&state.Pending128.Count==1&&result.IdInsert==null&&result.LockReleased,"unknown first index retains the earlier pending erase");
  state=State(Pending(),Index(),null);
  result=LoadedObjectRegistration.Apply(state,obj,Id,table,delegate{reads++;});
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownRawKeyRegistrationIndex"&&result.LastCompletedStage=="RawKeyReadComplete"&&state.Pending128.Count==1&&state.ById30.Count==1&&result.RawKeyInsert==null&&result.LockReleased,"unknown second index retains the first insertion and known late raw-key read");
  state=State();obj=new RegistrationObject(null,Raw);
  result=LoadedObjectRegistration.Apply(state,obj,Id,table,null);
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownOwnReadCallback"&&result.FailureStage=="OwnRead"&&result.LastCompletedStage=="LockReleased"&&result.LockReleased&&!result.ReadInvoked,"unknown own Read is checked after both registrations and unlock");
  Need(state.Pending128.Count==1&&state.ById30.Count==1&&state.ByRawKey70.Count==1&&obj.Id2C==Id&&result.MayHaveEffects,"unknown own Read preserves both registered entries");
  state=State();int ownEffects=0;
  result=LoadedObjectRegistration.Apply(state,obj,Id,table,delegate{ownEffects++;throw new InvalidOperationException("own Read fixture");});
  Need(result.Status=="Unsupported"&&result.Reason=="OwnReadCallbackThrew:System.InvalidOperationException"&&result.ReadInvoked&&result.LockReleased&&result.MayHaveEffects&&ownEffects==1,"own callback exception is unsupported with the callback's effect retained");
  Need(state.ById30.Count==1&&state.ByRawKey70.Count==1&&state.Pending128.Count==1&&!state.Gate.IsHeldByCurrentThread,"callback failure does not unregister the object or reenter the gate");
 }
 static void GrowthAndReserveSuccess(){
  var table=new object();var obj=new RegistrationObject(null,Raw);int reads=0;
  var smallPending=new RegistrationIndex(2,2,1.0f,new[]{E(Id,new object())});
  var state=State(smallPending,Index(),Index());
  var result=LoadedObjectRegistration.Apply(state,obj,Id,table,delegate{reads++;});
  Need(result.Status=="Known"&&result.PendingErase.RemovedCount==1&&smallPending.Count==0&&smallPending.StateComplete&&smallPending.BucketCount==8&&smallPending.BucketCapacity==8&&state.ById30.Count==1&&state.ByRawKey70.Count==1&&result.ReadInvoked&&reads==1,"pending whole-chain erase reserves eight buckets and continues through both registrations to Read");
  smallPending=new RegistrationIndex(2,2,1.0f,new RegistrationEntry[0]);state=State(smallPending,Index(),Index());
  reads=0;
  result=LoadedObjectRegistration.Apply(state,obj,Id,table,delegate{reads++;});
  Need(result.Status=="Known"&&result.PendingErase.RemovedCount==0&&smallPending.StateComplete&&smallPending.BucketCount==8&&smallPending.BucketCapacity==8&&result.ReadInvoked&&reads==1,"empty small pending miss still reserves and resets buckets before Read");
  var oldId=new object();var oldRaw=new object();
  var ids=new RegistrationIndex(8,8,0.125f,new[]{E(Id+1,oldId)});
  var raw=new RegistrationIndex(8,8,0.125f,new[]{E(Raw+1,oldRaw)});
  var oldIdNode=ids.FirstInBucket(ids.BucketFor(Id+1));var oldRawNode=raw.FirstInBucket(raw.BucketFor(Raw+1));
  state=State(Pending(),ids,raw);reads=0;
  result=LoadedObjectRegistration.Apply(state,obj,Id,table,delegate(RegistrationObject value,object loaded){
   reads++;
   Need(!state.Gate.IsHeldByCurrentThread&&Object.ReferenceEquals(value,obj)&&Object.ReferenceEquals(loaded,table)&&ids.StateComplete&&raw.StateComplete&&ids.Count==2&&raw.Count==2&&ids.BucketCount==64&&raw.BucketCount==64&&Contains(ids,Id)&&Contains(raw,Raw),"Read runs once outside the gate after both actual indices finish growth and contain the new keys");
  });
  Need(result.Status=="Known"&&result.LastCompletedStage=="ReadComplete"&&result.LockReleased&&result.ReadInvoked&&reads==1&&result.IdInsert.Inserted==true&&result.RawKeyInsert.Inserted==true&&Object.ReferenceEquals(result.IdInsert.Value,obj)&&Object.ReferenceEquals(result.RawKeyInsert.Value,obj),"two successful growth returns continue to Read with the incoming object as each new payload");
  Need(ids.BucketCapacity==64&&raw.BucketCapacity==64&&Object.ReferenceEquals(ids.FirstInBucket(ids.BucketFor(Id+1)),oldIdNode)&&Object.ReferenceEquals(raw.FirstInBucket(raw.BucketFor(Raw+1)),oldRawNode)&&Object.ReferenceEquals(ids.FirstInBucket(ids.BucketFor(Id)),result.IdInsert.Node)&&Object.ReferenceEquals(raw.FirstInBucket(raw.BucketFor(Raw)),result.RawKeyInsert.Node),"reserve and rehash retain old and new node identities in the new bucket endpoints");
  ids=new RegistrationIndex(8,8,0.125f,new[]{E(Id,oldId),E(Id+1,new object())});
  raw=new RegistrationIndex(8,8,0.125f,new[]{E(Raw,oldRaw),E(Raw+1,new object())});
  state=State(Pending(),ids,raw);reads=0;bool readOutside=false;
  result=LoadedObjectRegistration.Apply(state,obj,Id,table,delegate(RegistrationObject value,object loaded){reads++;readOutside=!state.Gate.IsHeldByCurrentThread&&Object.ReferenceEquals(value,obj)&&Object.ReferenceEquals(loaded,table);});
  Need(result.Status=="Known"&&result.IdInsert.Inserted==false&&result.RawKeyInsert.Inserted==false&&!result.IdInsert.MayHaveEffects&&!result.RawKeyInsert.MayHaveEffects&&Object.ReferenceEquals(result.IdInsert.Value,oldId)&&Object.ReferenceEquals(result.RawKeyInsert.Value,oldRaw)&&ids.Count==2&&raw.Count==2&&ids.BucketCount==8&&raw.BucketCount==8&&ids.BucketCapacity==8&&raw.BucketCapacity==8,"duplicates in already overloaded indices bypass growth and keep their original payloads");
  Need(reads==1&&readOutside&&result.ReadInvoked&&obj.Id2C==Id&&state.Pending128.Count==1,"two overloaded duplicates still call Read on the incoming object after pending erasure and unlock");
 }
 static void ContainerFailures(){
  var table=new object();var obj=new RegistrationObject(null,Raw);int reads=0;
  int limit=RegistrationIndex.MaximumBuckets;
  var growingIds=new RegistrationIndex(limit,limit,1f/limit,new[]{E(Id+1,new object())});var state=State(Pending(),growingIds,Index());
  var result=LoadedObjectRegistration.Apply(state,obj,Id,table,delegate{reads++;});
  Need(result.Status=="Unsupported"&&result.FailureStage=="IdInsert"&&result.IdInsert!=null&&!result.IdInsert.Supported&&result.IdInsert.Reason=="RegistrationIndexGrowthBucketBudgetOutsideCoveredDomain"&&result.IdInsert.MayHaveEffects&&!result.IdInsert.Inserted.HasValue&&result.RawKeyInsert==null,"first-index growth beyond the bucket budget is not mistaken for an ignored duplicate");
  Need(growingIds.Count==2&&Contains(growingIds,Id)&&!growingIds.StateComplete&&growingIds.BucketCount==limit&&growingIds.BucketCapacity==limit&&state.Pending128.Count==1&&state.ByRawKey70.Count==0&&result.LockReleased&&result.MayHaveEffects&&reads==0,"first-index post-link failure preserves old bucket metadata, the newly linked node and earlier deletion");
  var growingRaw=new RegistrationIndex(limit,limit,1f/limit,new[]{E(Raw+1,new object())});state=State(Pending(),Index(),growingRaw);
  result=LoadedObjectRegistration.Apply(state,obj,Id,table,delegate{reads++;});
  Need(result.Status=="Unsupported"&&result.FailureStage=="RawKeyInsert"&&result.IdInsert.Supported&&result.IdInsert.Inserted==true&&result.RawKeyInsert!=null&&!result.RawKeyInsert.Supported&&result.RawKeyInsert.Reason=="RegistrationIndexGrowthBucketBudgetOutsideCoveredDomain"&&result.RawKeyInsert.MayHaveEffects&&!result.RawKeyInsert.Inserted.HasValue,"second-index growth beyond the bucket budget follows a successful first insertion");
  Need(state.ById30.Count==1&&growingRaw.Count==2&&Contains(growingRaw,Raw)&&!growingRaw.StateComplete&&growingRaw.BucketCount==limit&&growingRaw.BucketCapacity==limit&&state.Pending128.Count==1&&result.LockReleased&&!result.ReadInvoked&&reads==0,"second-index failure preserves old bucket metadata and both linked nodes without calling Read");
  state=State(Pending(),growingIds,Index());obj=new RegistrationObject(null,Raw);
  result=LoadedObjectRegistration.Apply(state,obj,Id,table,delegate{reads++;});
  Need(result.Status=="Unsupported"&&result.IdInsert!=null&&!result.IdInsert.Supported&&state.Pending128.Count==1&&obj.Id2C==Id&&growingIds.Count==2&&result.LockReleased&&reads==0,"already incomplete index is rejected when reached, after this call's ID write and pending erase");
 }
 static int Main(){try{Guards();SuccessAndDuplicates();UnknownAfterEffects();GrowthAndReserveSuccess();ContainerFailures();Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+n+",\"scope\":\"Own already-loaded-table registration sequence, actual bounded registration indices and supplied Read callbacks; no native ABI, LoadFile, inventory, ObjectIdAllocator integration or original execution.\"}");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
