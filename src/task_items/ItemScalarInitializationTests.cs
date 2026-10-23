using System;
using GrimDynasty.TaskItems;
class ItemScalarInitializationTests {
 static int n;
 static void Need(bool value,string message){n++;if(!value)throw new Exception(message);}
 static void UnknownStorage(){
  var state=new ItemScalarInitializationState();
  Need(!state.Raw870.HasValue&&!state.Raw88C.HasValue&&!state.Gender874.HasValue&&!state.Stack88C.HasValue,
   "creating the local snapshot does not silently execute construction or invent known zero storage");
  var result=ItemScalarInitialization.Apply(state);
  Need(result.Status=="Known"&&result.Reason==null&&Object.ReferenceEquals(result.State,state)&&result.CompletedStores==6,
   "the segment updates the exact supplied local state and reports its complete six-store write set");
  Need(state.Raw870==0&&state.Raw878==0&&state.Raw880==0&&state.Gender874==0,
   "the observed zero QWORD run establishes the overlapping gender DWORD without a supplied register value");
  Need(state.Raw88C==1&&state.Stack88C==1&&state.Word890==0&&state.Raw894==0&&state.Word898==0&&state.Raw89C==0,
   "the stack initialization is a full QWORD store and the following scalar region is known zero");
  Need(!state.Untouched888.HasValue&&!state.ReplicaStack6B0.HasValue,
   "the four-byte hole and the later replica field remain unknown instead of inheriting nearby zeros or stack one");
  Need(result.NextInstructionAddress==0x180323743&&!result.IsCompleteConstructor&&result.MayHaveEffects,
   "success ends at the next instruction and makes no complete-object or constructor-return claim");
 }
 static void ExistingStorageAndOverlap(){
  var state=new ItemScalarInitializationState(0x12345678abcdef01UL,0x1111222233334444UL,
   0x5555666677778888UL,0x89abcdef76543210UL,0xfedcba9876543210UL,0xdeadbeef,
   0x76543210,0xffffffff);
  Need(state.Gender874==0x12345678&&state.Stack88C==0x76543210&&state.Word890==0x89abcdef&&state.Word898==0xfedcba98,
   "overlapping views use the correct QWORD halves even when high bits are set under checked compilation");
  var alias=state;var result=ItemScalarInitialization.Apply(state);
  Need(alias.Gender874==0&&alias.Stack88C==1&&alias.Word890==0&&alias.Word898==0,
   "a retained alias observes full-width writes rather than independently cached DWORD values");
  Need(state.Raw878==0&&state.Raw880==0&&state.Raw894==0&&state.Raw89C==0,
   "nonzero existing scalar contents are overwritten by the covered segment");
  Need(state.Untouched888==0x76543210&&state.ReplicaStack6B0==0xffffffff,
   "existing gap and replica values survive; the segment does not initialize a whole object or synchronize stack replicas");
  Need(Object.ReferenceEquals(result.State,alias)&&result.CompletedStores==6&&result.MayHaveEffects,
   "initialization neither replaces the state identity nor treats the stores as a pure read");
  var again=ItemScalarInitialization.Apply(state);
  Need(again.Status=="Known"&&again.CompletedStores==6&&again.MayHaveEffects&&Object.ReferenceEquals(again.State,state),
   "re-entering this explicit instruction segment executes stores even when their values already match");
  Need(state.Untouched888==0x76543210&&state.ReplicaStack6B0==0xffffffff&&!again.IsCompleteConstructor,
   "re-entry still preserves untouched data and cannot promote the result into a complete constructor");
 }
 static void BoundariesAndIsolation(){
  var absent=ItemScalarInitialization.Apply(null);
  Need(absent.Status=="Unsupported"&&absent.Reason=="UnknownItemScalarInitializationState"&&absent.State==null,
   "missing local storage is an explicit unsupported boundary");
  Need(absent.CompletedStores==0&&!absent.MayHaveEffects&&!absent.NextInstructionAddress.HasValue&&!absent.IsCompleteConstructor,
   "a rejected entry does not fabricate writes, fallthrough progress or a constructor result");
  var left=new ItemScalarInitializationState(untouched888:0,replicaStack6B0:7);
  var right=new ItemScalarInitializationState(initial870:ulong.MaxValue,initial88C:ulong.MaxValue,
   untouched888:9,replicaStack6B0:11);
  ItemScalarInitialization.Apply(left);
  Need(right.Raw870==ulong.MaxValue&&right.Raw88C==ulong.MaxValue&&right.Gender874==uint.MaxValue&&right.Word890==uint.MaxValue,
   "initializing one own snapshot cannot modify another object's overlapping storage");
  Need(left.Untouched888==0&&left.ReplicaStack6B0==7&&right.Untouched888==9&&right.ReplicaStack6B0==11,
   "known zero, unknown and nonzero untouched fields are not conflated across snapshots");
  Need(ItemScalarInitialization.EntryInstructionAddress==0x180323714&&ItemScalarInitialization.NextInstructionAddress==0x180323743,
   "the local operation excludes both the predecessor helper and the following string initialization");
 }
 static int Main(){
  try{
   UnknownStorage();ExistingStorageAndOverlap();BoundariesAndIsolation();
   Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+n+",\"scenarios\":0,\"scope\":\"Own Item scalar initialization segment only; six exact stores, overlapping DWORD views and untouched-field preservation. No complete Item/Actor/QuestItem constructor, ClassInfo callback, original execution or rendering.\"}");
   return 0;
  }catch(Exception e){Console.Error.WriteLine(e);return 1;}
 }
}
