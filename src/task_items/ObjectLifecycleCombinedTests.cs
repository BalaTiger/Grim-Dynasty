using System;
using GrimDynasty.TaskItems;
class ObjectLifecycleCombinedTests {
 static int n;
 const uint Id=77,NameKey=0xdc19c105;
 static void Need(bool value,string message){n++;if(!value)throw new Exception(message);}
 static bool Same(byte[] left,byte[] right){if(left.Length!=right.Length)return false;for(int i=0;i<left.Length;i++)if(left[i]!=right[i])return false;return true;}
 static bool Contains(RegistrationIndex index,uint key){foreach(uint value in index.KeysInOrder())if(value==key)return true;return false;}
 static byte[] NameBytes(int length){byte[] pattern={0x80,0xff,0,0x7f};var bytes=new byte[length];for(int i=0;i<length;i++)bytes[i]=pattern[i&3];return bytes;}
 static RegistrationIndex FullIndex(){
  var entries=new RegistrationEntry[8];
  // Keys 0..7 occupy eight distinct buckets at B=8, so each seed segment
  // is contiguous. These are independent explicit own container snapshots.
  for(int i=0;i<entries.Length;i++)entries[i]=new RegistrationEntry((uint)i,new object());
  return new RegistrationIndex(8,8,1.0f,entries);
 }
 static bool Grown(RegistrationIndex index){return index.StateComplete&&index.Count==9&&index.BucketCount==64&&index.BucketCapacity==64;}
 static void Run(bool supplyOwnRead){
  int constructions=0;ObjectBaseState constructed=null;byte[] className={79,119,110,66,97,115,101,0};
  // Explicit callback input, not a recovered native ClassInfo binding.
  var info=new ClassInfoSnapshot(className,null,null,delegate{constructions++;constructed=ObjectBaseConstructor.Create();return constructed;});
  var dispatched=ClassFactoryDispatch.Create(info,className);var value=dispatched.Instance as ObjectBaseState;
  Need(dispatched.Status=="Known"&&dispatched.CallbacksInvoked==1&&constructions==1&&value!=null&&Object.ReferenceEquals(value,constructed),"own factory dispatch returns the recovered base-constructor model once");
  var fields=value.Fields;var name=value.Name;
  Need(value.Dispatch==ObjectBaseDispatch.ObjectBase&&value.NameCapacity==15&&value.NameLength==0&&!fields.RawKey28.HasValue&&!fields.Id2C.HasValue,"construction leaves both native-unwritten registration fields unknown");
  byte[] firstName=NameBytes(16),finalName=NameBytes(32);
  var assigned=ObjectNameAssignment.Apply(value,new ObjectNameString(firstName,16));
  Need(assigned.Status=="Known"&&assigned.CapacityGrew&&value.NameCapacity==31&&value.NameLength==16&&fields.RawKey28==0x0499bbe5&&!fields.Id2C.HasValue&&Same(value.CopyNameBytes(),firstName),"sixteen signed/NUL bytes grow the name from capacity 15 to 31 without assigning an ID");
  assigned=ObjectNameAssignment.Apply(value,new ObjectNameString(finalName,32));
  // Literal full-length signed-byte FNV32 of [80 FF 00 7F] repeated eight
  // times; no production hash function is used to obtain the expected key.
  Need(assigned.Status=="Known"&&assigned.CapacityGrew&&value.NameCapacity==47&&value.NameLength==32&&value.Name.TerminatorByte==0&&fields.RawKey28==NameKey&&!fields.Id2C.HasValue&&Same(value.CopyNameBytes(),finalName)&&Object.ReferenceEquals(value.Name,name)&&Object.ReferenceEquals(value.Fields,fields),"second name growth preserves identity and hashes all 32 bytes including NUL");
  var ids=FullIndex();var raw=FullIndex();
  var pending=new RegistrationIndex(8,8,1.0f,new[]{new RegistrationEntry(Id,new object())});
  var manager=new ObjectRegistrationState(new RegistrationGate(),pending,ids,raw);var table=new object();
  var oldIdNode=ids.FirstInBucket(ids.BucketFor(0));var oldRawNode=raw.FirstInBucket(raw.BucketFor(0));
  Need(ids.Count==8&&raw.Count==8&&ids.BucketCount==8&&raw.BucketCount==8&&ids.BucketCapacity==8&&raw.BucketCapacity==8&&!Contains(ids,Id)&&!Contains(raw,NameKey),"both real indices start at the threshold and require growth for the new unique keys");
  int reads=0;bool callbackSawCompleteState=false;Action<RegistrationObject,object> ownRead=null;
  if(supplyOwnRead)ownRead=delegate(RegistrationObject incoming,object loaded){
   reads++;
   callbackSawCompleteState=Object.ReferenceEquals(incoming,fields)&&Object.ReferenceEquals(loaded,table)&&!manager.Gate.IsHeldByCurrentThread&&incoming.Id2C==Id&&incoming.RawKey28==NameKey&&pending.Count==0&&Grown(ids)&&Grown(raw)&&Contains(ids,Id)&&Contains(raw,NameKey)&&Object.ReferenceEquals(value.Name,name)&&value.NameCapacity==47&&Same(value.CopyNameBytes(),finalName);
  };
  // The table and callback are supplied own boundaries, not LoadFile or a
  // recovered native Read. Missing Read must not become an empty callback.
  var registered=LoadedObjectRegistration.Apply(manager,fields,Id,table,ownRead);
  Need(registered.IdAssigned&&registered.LockAcquired&&registered.LockReleased&&registered.PendingErase!=null&&registered.PendingErase.Supported&&registered.PendingErase.RemovedCount==1&&pending.Count==0&&!manager.Gate.IsHeldByCurrentThread,"registration assigns the ID, erases pending and releases its own gate");
  Need(Grown(ids)&&Grown(raw)&&registered.IdInsert!=null&&registered.RawKeyInsert!=null&&registered.IdInsert.Supported&&registered.RawKeyInsert.Supported&&registered.IdInsert.Inserted==true&&registered.RawKeyInsert.Inserted==true&&registered.IdInsert.Node.Key==Id&&registered.RawKeyInsert.Node.Key==NameKey&&Object.ReferenceEquals(registered.IdInsert.Value,fields)&&Object.ReferenceEquals(registered.RawKeyInsert.Value,fields),"the ninth entries complete both 8-to-64 growth operations with the same field object");
  var retainedId=ids.Insert(0,new object());var retainedRaw=raw.Insert(0,new object());
  Need(retainedId.Supported&&retainedRaw.Supported&&retainedId.Inserted==false&&retainedRaw.Inserted==false&&!retainedId.MayHaveEffects&&!retainedRaw.MayHaveEffects&&Object.ReferenceEquals(retainedId.Node,oldIdNode)&&Object.ReferenceEquals(retainedRaw.Node,oldRawNode)&&ids.Count==9&&raw.Count==9,"both rehashes preserve the old seed nodes without replacement");
  Need(fields.Id2C==Id&&fields.RawKey28==NameKey&&Object.ReferenceEquals(value.Fields,fields)&&Object.ReferenceEquals(value.Name,name)&&value.NameLength==32&&value.NameCapacity==47&&value.Name.TerminatorByte==0&&Same(value.CopyNameBytes(),finalName),"registration growth does not alter the already-assigned name or replace its field identity");
  if(supplyOwnRead)
   Need(registered.Status=="Known"&&registered.LastCompletedStage=="ReadComplete"&&registered.ReadInvoked&&reads==1&&callbackSawCompleteState,"explicit own Read runs once outside the gate after both growth operations are complete");
  else
   Need(registered.Status=="Unsupported"&&registered.Reason=="UnknownOwnReadCallback"&&registered.FailureStage=="OwnRead"&&registered.LastCompletedStage=="LockReleased"&&!registered.ReadInvoked&&registered.MayHaveEffects&&reads==0&&!callbackSawCompleteState,"missing native Read remains unsupported after the completed registration and growth prefix");
 }
 static int Main(){try{Run(true);Run(false);Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+n+",\"scenarios\":2,\"scope\":\"Own constructor/name/registration composition with explicit ClassInfo callback and table inputs; no native binding, LoadFile, recovered Read or original execution.\"}");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
