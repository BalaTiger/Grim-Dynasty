using System;
using GrimDynasty.TaskItems;
class ObjectBaseConstructorTests {
 static int n;
 static void Need(bool value,string message){n++;if(!value)throw new Exception(message);}
 static RegistrationIndex Index(params RegistrationEntry[] entries){return new RegistrationIndex(8,8,1.0f,entries);}
 static ObjectRegistrationState Manager(uint id){return new ObjectRegistrationState(new RegistrationGate(),Index(new RegistrationEntry(id,new object())),Index(),Index());}
 static void Initialization(){
  var value=ObjectBaseConstructor.Create();
  Need(value.Dispatch==ObjectBaseDispatch.ObjectBase&&value.NameLength==0&&value.NameCapacity==15&&value.FirstNameByte==0&&value.CopyNameBytes().Length==0,"closed constructor writes describe the base dispatch and empty short string");
  Need(!value.Fields.RawKey28.HasValue&&!value.Fields.Id2C.HasValue,"untouched registration fields remain unknown despite managed zero defaults");
  var other=ObjectBaseConstructor.Create();value.Fields.Id2C=19;
  Need(!other.Fields.Id2C.HasValue&&!Object.ReferenceEquals(value.Fields,other.Fields),"separate construction creates independent own field storage");
  bool rejected=false;try{ObjectBaseConstructor.Initialize(null);}catch(ArgumentNullException){rejected=true;}
  Need(rejected,"missing own field storage is rejected rather than reported as a successful construction");
  foreach(uint? id in new uint?[]{null,0,0x80000000,uint.MaxValue}){
   foreach(uint? key in new uint?[]{null,0,0x811c9dc5,0x80000000,uint.MaxValue}){
    var fields=new RegistrationObject(id,key);value=ObjectBaseConstructor.Initialize(fields);
    Need(Object.ReferenceEquals(value.Fields,fields)&&fields.Id2C==id&&fields.RawKey28==key,"base initialization preserves known and unknown bits and field-storage identity");
   }
  }
 }
 static void FactoryAndUnknownRegistration(){
  int constructions=0;byte[] name={79,98,106,101,99,116,0};
  var info=new ClassInfoSnapshot(name,0,new ClassInfoSnapshot[0],delegate{constructions++;return ObjectBaseConstructor.Create();});
  var dispatched=ClassFactoryDispatch.Create(info,name);
  var value=dispatched.Instance as ObjectBaseState;
  Need(dispatched.Status=="Known"&&constructions==1&&value!=null,"explicit own ClassInfo callback constructs only the recovered logical base state");
  Need(!value.Fields.RawKey28.HasValue,"an empty name is not evidence for a zero key or the FNV empty-string basis");
  var guard=LoadedObjectRegistration.Apply(null,value.Fields,0,null,null);
  Need(guard.Status=="Known"&&guard.LastCompletedStage=="ZeroId"&&!value.Fields.Id2C.HasValue,"zero-ID guard after construction leaves the untouched ID unknown");
  uint id=77;var manager=Manager(id);bool read=false;
  var result=LoadedObjectRegistration.Apply(manager,value.Fields,id,new object(),delegate{read=true;});
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownRawRegistrationKey"&&result.FailureStage=="RawKeyRead","construction cannot silently complete registration with an invented raw key");
  Need(value.Fields.Id2C==id&&result.IdAssigned&&result.PendingErase.RemovedCount==1&&manager.Pending128.Count==0,"the native-observed prefix still assigns ID and erases pending before discovering the unknown key");
  Need(result.IdInsert.Inserted==true&&Object.ReferenceEquals(result.IdInsert.Value,value.Fields)&&manager.ById30.Count==1&&manager.ByRawKey70.Count==0,"the first index keeps its completed insertion without manufacturing a second key");
  Need(result.LockReleased&&!manager.Gate.IsHeldByCurrentThread&&!result.ReadInvoked&&!read,"own cleanup releases the gate and an unsupported prefix does not invoke any Read callback");
 }
 static void SeededRegistration(){
  foreach(uint key in new uint[]{0,0x80000000,uint.MaxValue}){
   var fields=new RegistrationObject(99,key);var value=ObjectBaseConstructor.Initialize(fields);uint id=88;
   var manager=Manager(id);var table=new object();int reads=0;
   var result=LoadedObjectRegistration.Apply(manager,value.Fields,id,table,delegate(RegistrationObject incoming,object loaded){
    reads++;
    Need(Object.ReferenceEquals(incoming,fields)&&Object.ReferenceEquals(loaded,table)&&!manager.Gate.IsHeldByCurrentThread,"explicit own callback sees preserved field identity after registration and unlock");
   });
   Need(result.Status=="Known"&&reads==1&&fields.Id2C==id&&fields.RawKey28==key&&result.RawKeyInsert.Inserted==true,"a separately known raw key remains usable without claiming SetObjectName or native Read recovery");
   Need(value.NameLength==0&&value.NameCapacity==15&&value.Dispatch==ObjectBaseDispatch.ObjectBase,"registration does not replace the separately modelled base initialization metadata");
  }
 }
 static void Main(){Initialization();FactoryAndUnknownRegistration();SeededRegistration();Console.WriteLine("ObjectBaseConstructorTests: "+n+" assertions passed");}
}
