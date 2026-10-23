using System;
using GrimDynasty.TaskItems;
class ObjectNameAssignmentTests {
 static int n;
 static void Need(bool value,string message){n++;if(!value)throw new Exception(message);}
 static bool Same(byte[] left,byte[] right){if(left.Length!=right.Length)return false;for(int i=0;i<left.Length;i++)if(left[i]!=right[i])return false;return true;}
 static byte[] Bytes(int count){var bytes=new byte[count];for(int i=0;i<count;i++)bytes[i]=(byte)(i&255);return bytes;}
 // This is explicit valid model input, not a recovered string constructor.
 static ObjectNameString Source(byte[] bytes){return new ObjectNameString(bytes,(ulong)Math.Max(15,bytes.Length));}
 static RegistrationIndex Index(params RegistrationEntry[] entries){return new RegistrationIndex(8,8,1.0f,entries);}
 static ObjectRegistrationState Manager(uint id){return new ObjectRegistrationState(new RegistrationGate(),Index(new RegistrationEntry(id,new object())),Index(),Index());}
 static bool Contains(RegistrationIndex index,uint key){foreach(uint value in index.KeysInOrder())if(value==key)return true;return false;}
 static void Goldens(){
  // Fixed full-length signed-byte FNV32 vectors, not values computed by the
  // production helper. Embedded NUL is data; the trailing terminator is not.
  byte[][] inputs={new byte[0],new byte[]{0x41},new byte[]{0x41,0,0x42},new byte[]{0},new byte[]{0x80},new byte[]{0xff},new byte[]{0x80,0xff,0,0x7f}};
  uint[] expected={0x811c9dc5,0xc40bf6cc,0x941dd992,0x050c5d1f,0x04f3b29f,0xf9f3a14e,0xa9f464cd};
  for(int i=0;i<inputs.Length;i++){
   var value=ObjectBaseConstructor.Create();var result=ObjectNameAssignment.Apply(value,Source(inputs[i]));
   Need(result.Status=="Known"&&result.NameWritten&&result.KeyWritten&&!result.AssignmentSkipped&&!result.CapacityGrew&&value.Fields.RawKey28==expected[i]&&!value.Fields.Id2C.HasValue&&value.NameLength==(ulong)inputs[i].Length&&value.NameCapacity==15&&value.Name.TerminatorByte==0&&Same(value.CopyNameBytes(),inputs[i]),"literal signed-byte full-length hash vector preserves unknown ID "+i);
  }
 }
 static void AliasesAndSourceCapacity(){
  var fields=new RegistrationObject(99,null);var value=ObjectBaseConstructor.Initialize(fields);var name=value.Name;
  var result=ObjectNameAssignment.Apply(value,name);
  Need(result.Status=="Known"&&result.AssignmentSkipped&&!result.NameWritten&&result.KeyWritten&&!result.CapacityGrew&&result.MayHaveEffects&&fields.RawKey28==0x811c9dc5&&value.NameLength==0&&value.NameCapacity==15,"empty outer self-alias still writes the empty-name hash");
  var source=new ObjectNameString(new byte[]{0x41},ObjectNameAssignment.MaximumNameCapacity+1);
  result=ObjectNameAssignment.Apply(value,source);
  Need(result.Status=="Known"&&!result.AssignmentSkipped&&result.NameWritten&&!result.CapacityGrew&&value.NameCapacity==15&&fields.RawKey28==0xc40bf6cc&&source.Capacity==ObjectNameAssignment.MaximumNameCapacity+1&&source.Length==1,"large source capacity is not a target-capacity requirement and is not copied");
  fields.RawKey28=0;
  result=ObjectNameAssignment.Apply(value,name);
  Need(result.Status=="Known"&&result.AssignmentSkipped&&!result.NameWritten&&result.KeyWritten&&!result.CapacityGrew&&result.MayHaveEffects&&fields.RawKey28==0xc40bf6cc&&value.NameLength==1&&value.FirstNameByte==0x41,"nonempty self-alias rehashes instead of retaining a stale raw key");
  result=ObjectNameAssignment.Apply(value,new ObjectNameString(new byte[]{0x41},63));
  Need(result.Status=="Known"&&!result.AssignmentSkipped&&result.NameWritten&&result.KeyWritten&&!result.CapacityGrew&&value.NameCapacity==15&&fields.Id2C==99&&Object.ReferenceEquals(value.Name,name)&&Object.ReferenceEquals(value.Fields,fields)&&!Object.ReferenceEquals(value.Name,source),"equal content in distinct string storage takes assignment while preserving target name and field identity");
 }
 static void ArrayIsolation(){
  byte[] original={0x80,0xff,0,0x7f};var input=(byte[])original.Clone();var source=Source(input);input[0]=0;
  var fields=new RegistrationObject(uint.MaxValue,0);var value=ObjectBaseConstructor.Initialize(fields);var name=value.Name;
  var result=ObjectNameAssignment.Apply(value,source);
  Need(result.Status=="Known"&&Same(source.CopyBytes(),original)&&Same(value.CopyNameBytes(),original)&&fields.RawKey28==0xa9f464cd,"source construction isolates later changes to its input array");
  byte[] copiedSource=source.CopyBytes(),copiedTarget=value.CopyNameBytes();copiedSource[1]=0;copiedTarget[0]=0;
  Need(Same(source.CopyBytes(),original)&&Same(value.CopyNameBytes(),original)&&fields.RawKey28==0xa9f464cd,"neither source nor target exposes writable payload through a copy getter");
  var other=ObjectBaseConstructor.Create();result=ObjectNameAssignment.Apply(other,value.Name);
  Need(result.Status=="Known"&&!Object.ReferenceEquals(other.Name,value.Name)&&Same(other.CopyNameBytes(),original)&&other.Fields.RawKey28==0xa9f464cd,"assignment from another object's name copies disjoint payload storage");
  result=ObjectNameAssignment.Apply(value,Source(new byte[]{0x41}));
  Need(result.Status=="Known"&&Same(other.CopyNameBytes(),original)&&other.Fields.RawKey28==0xa9f464cd&&fields.RawKey28==0xc40bf6cc&&fields.Id2C==uint.MaxValue&&Object.ReferenceEquals(value.Name,name)&&Object.ReferenceEquals(value.Fields,fields),"later source-name mutation cannot alter the earlier copy or replace name and field identity");
 }
 static void CapacityTransitions(){
  var value=ObjectBaseConstructor.Create();var name=value.Name;
  int[] lengths={15,16,31,32,48,71,106,158,336};
  ulong[] capacities={15,31,31,47,70,105,157,235,351};
  bool[] grows={false,true,false,true,true,true,true,true,true};
  for(int i=0;i<lengths.Length;i++){
   byte[] payload=Bytes(lengths[i]);var result=ObjectNameAssignment.Apply(value,Source(payload));
   Need(result.Status=="Known"&&result.CapacityGrew==grows[i]&&value.NameCapacity==capacities[i]&&value.NameLength==(ulong)lengths[i]&&value.Name.TerminatorByte==0&&Same(value.CopyNameBytes(),payload)&&Object.ReferenceEquals(value.Name,name),"observed growth boundary "+lengths[i]+" gives capacity "+capacities[i]+"; the 235/336 equality case must give 351, not 352");
  }
  value=ObjectBaseConstructor.Create();ObjectNameAssignment.Apply(value,Source(Bytes(16)));name=value.Name;
  var shortened=ObjectNameAssignment.Apply(value,Source(new byte[]{0x41,0,0x42}));
  Need(shortened.Status=="Known"&&!shortened.CapacityGrew&&value.NameCapacity==31&&value.NameLength==3&&value.FirstNameByte==0x41&&value.Fields.RawKey28==0x941dd992&&Same(value.CopyNameBytes(),new byte[]{0x41,0,0x42}),"short assignment retains the existing heap capacity and hashes through NUL");
  var cleared=ObjectNameAssignment.Apply(value,Source(new byte[0]));
  Need(cleared.Status=="Known"&&cleared.NameWritten&&cleared.KeyWritten&&!cleared.CapacityGrew&&value.NameCapacity==31&&value.NameLength==0&&value.FirstNameByte==0&&value.Name.TerminatorByte==0&&value.CopyNameBytes().Length==0&&value.Fields.RawKey28==0x811c9dc5&&Object.ReferenceEquals(value.Name,name),"clearing heap storage preserves capacity and its name identity");
 }
 static void CoverageBounds(){
  var fields=new RegistrationObject(uint.MaxValue,null);var value=ObjectBaseConstructor.Initialize(fields);var name=value.Name;
  byte[] payload=Bytes(65536);var result=ObjectNameAssignment.Apply(value,Source(payload));
  Need(ObjectNameAssignment.MaximumNameBytes==65536&&result.Status=="Known"&&result.CapacityGrew&&result.KeyWritten&&value.NameLength==65536&&value.NameCapacity==65551&&value.Name.TerminatorByte==0&&Same(value.CopyNameBytes(),payload)&&fields.RawKey28.HasValue,"the inclusive model length budget is covered by a normal allocation result");
  uint? oldKey=fields.RawKey28;var tooLong=Source(Bytes(65537));result=ObjectNameAssignment.Apply(value,tooLong);
  Need(result.Status=="Unsupported"&&result.Reason=="NameLengthOutsideCoveredDomain"&&!result.AssignmentSkipped&&!result.NameWritten&&!result.KeyWritten&&!result.CapacityGrew&&!result.MayHaveEffects,"65537 is an own coverage limit, not a simulated native string failure");
  Need(value.NameLength==65536&&value.NameCapacity==65551&&value.Name.TerminatorByte==0&&Same(value.CopyNameBytes(),payload)&&fields.RawKey28==oldKey&&fields.Id2C==uint.MaxValue&&Object.ReferenceEquals(value.Name,name)&&Object.ReferenceEquals(value.Fields,fields),"budget rejection leaves all previously known state and identity unchanged");
  result=ObjectNameAssignment.Apply(null,Source(new byte[0]));
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownObjectBaseState"&&!result.MayHaveEffects,"missing target state is not a known empty object");
  result=ObjectNameAssignment.Apply(value,null);
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownSourceName"&&!result.MayHaveEffects&&fields.RawKey28==oldKey&&value.NameLength==65536&&value.NameCapacity==65551,"missing source does not silently assign an empty name");
 }
 static void RegistrationBoundary(){
  const uint id=77,oldKey=0x941dd992,newKey=0xc40bf6cc;
  var value=ObjectBaseConstructor.Create();var name=value.Name;var fields=value.Fields;
  var assigned=ObjectNameAssignment.Apply(value,Source(new byte[]{0x41,0,0x42}));
  var manager=Manager(id);var table=new object();int reads=0;bool observed=false;
  var registered=LoadedObjectRegistration.Apply(manager,fields,id,table,delegate(RegistrationObject incoming,object loaded){
   reads++;observed=Object.ReferenceEquals(incoming,fields)&&Object.ReferenceEquals(loaded,table)&&!manager.Gate.IsHeldByCurrentThread&&incoming.RawKey28==oldKey&&manager.Pending128.Count==0&&Contains(manager.ById30,id)&&Contains(manager.ByRawKey70,oldKey);
  });
  Need(assigned.Status=="Known"&&registered.Status=="Known"&&registered.ReadInvoked&&registered.LockReleased&&reads==1&&observed,"an explicitly supplied own Read sees the computed raw key after actual index updates and unlock");
  Need(registered.PendingErase.RemovedCount==1&&registered.IdInsert.Inserted==true&&registered.RawKeyInsert.Inserted==true&&registered.RawKeyInsert.Node.Key==oldKey&&Object.ReferenceEquals(registered.IdInsert.Value,fields)&&Object.ReferenceEquals(registered.RawKeyInsert.Value,fields)&&fields.Id2C==id,"registration stores the same field object under its ID and the complete-name hash");
  var rawNode=registered.RawKeyInsert.Node;var idNode=registered.IdInsert.Node;
  assigned=ObjectNameAssignment.Apply(value,Source(new byte[]{0x41}));
  Need(assigned.Status=="Known"&&fields.RawKey28==newKey&&fields.Id2C==id&&Object.ReferenceEquals(value.Name,name)&&Object.ReferenceEquals(value.Fields,fields)&&reads==1,"SetObjectName itself only changes the name and key and does not perform another Read");
  // SetObjectName does not provide maintenance for already-registered names.
  // The old index key remains; its same object value now has a different key.
  Need(manager.ByRawKey70.Count==1&&Contains(manager.ByRawKey70,oldKey)&&!Contains(manager.ByRawKey70,newKey)&&rawNode.Key==oldKey&&Object.ReferenceEquals(rawNode.Value,fields)&&Object.ReferenceEquals(manager.ByRawKey70.FirstInBucket(manager.ByRawKey70.BucketFor(oldKey)),rawNode)&&manager.ById30.Count==1&&Object.ReferenceEquals(manager.ById30.FirstInBucket(manager.ById30.BucketFor(id)),idNode),"renaming does not automatically synchronize the old raw-key index or replace either registered node");
  value=ObjectBaseConstructor.Create();assigned=ObjectNameAssignment.Apply(value,Source(new byte[]{0xff}));manager=Manager(id);
  registered=LoadedObjectRegistration.Apply(manager,value.Fields,id,table,null);
  Need(assigned.Status=="Known"&&registered.Status=="Unsupported"&&registered.Reason=="UnknownOwnReadCallback"&&registered.FailureStage=="OwnRead"&&registered.LastCompletedStage=="LockReleased"&&registered.LockReleased&&!registered.ReadInvoked&&!manager.Gate.IsHeldByCurrentThread,"a computed name key does not invent a native Read or a successful empty callback");
  Need(registered.MayHaveEffects&&registered.IdInsert.Inserted==true&&registered.RawKeyInsert.Inserted==true&&manager.Pending128.Count==0&&manager.ById30.Count==1&&manager.ByRawKey70.Count==1&&Contains(manager.ByRawKey70,0xf9f3a14e)&&value.Fields.Id2C==id&&value.Fields.RawKey28==0xf9f3a14e,"unknown Read retains both completed registrations using the signed-byte name hash");
 }
 static int Main(){try{Goldens();AliasesAndSourceCapacity();ArrayIsolation();CapacityTransitions();CoverageBounds();RegistrationBoundary();Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+n+",\"scope\":\"Own bounded full-name assignment and supplied registration callbacks; normal allocation success only, no native memory, Read, LoadFile, generic substring or registered-name maintenance.\"}");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
