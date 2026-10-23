using System;
using GrimDynasty.TaskItems;

// Own conditional integration. No native ID allocation, registration or getter execution.
public static class ItemConstructorIdentityTests {
 static int assertions,groups;
 const ulong Token=0x500000UL;
 static void Need(bool ok,string why){assertions++;if(!ok)throw new Exception(why);}
 static void Group(string name,Action test){int before=assertions;test();groups++;Console.WriteLine("PASS "+name+": "+(assertions-before)+" assertions");}
 static byte?[] Fill(int n){var a=new byte?[n];for(int i=0;i<n;i++)a[i]=0xA5;return a;}
 static void Put(byte?[] a,int offset,int width,ulong value){for(int i=0;i<width;i++)a[offset+i]=unchecked((byte)(value>>(8*i)));}
 static byte?[] ActorBytes(uint value){var a=Fill(0x530);Put(a,0x2C,4,value);return a;}
 static byte?[] Observe(ItemConstructorStorage s){var a=new byte?[s.ByteCount];for(int i=0;i<a.Length;i++)a[i]=s.ReadByte(i);return a;}
 static bool Same(ItemConstructorStorage s,byte?[] a){if(s.ByteCount!=a.Length)return false;for(int i=0;i<a.Length;i++)if(s.ReadByte(i)!=a[i])return false;return true;}
 static bool SameArray(ItemBE0ArrayStorage s,byte?[] a){for(int i=0;i<a.Length;i++)if(s.ReadByte(i)!=a[i])return false;return true;}
 sealed class PriorChecks:IItem950CheckSource {
  public Item950CheckObservation Check(ItemConstructorStorage s,int index,ulong target){
   if(index<0||index>=10||target!=0x180016710UL)throw new Exception("Unexpected own950 prerequisite");
   return new Item950CheckObservation(true);
  }
 }
 sealed class Checks:IItemBE0CheckSource {
  public int Calls;
  public Action<ItemConstructorStorage,int> Action;
  public ItemBE0CheckObservation Check(ItemConstructorStorage s,ItemBE0ArrayStorage a,int index,ulong target){
   if(index!=Calls||target!=0x1800EBEB0UL)throw new Exception("Unexpected ownBE0 prerequisite");
   Calls++;if(Action!=null)Action(s,index);return new ItemBE0CheckObservation(true);
  }
 }
 sealed class Fixture {
  public readonly ItemConstructorStorage State;
  public readonly ItemConstructorBridgeResult Bridge;
  public readonly ItemConstructorBA8Result BA8;
  public readonly ItemBE0ArrayStorage Array;
  public readonly ItemConstructorBE0Result Prior;
  public Fixture(byte?[] actorBytes,Checks checks=null,bool finishBE0=true,bool unknownTail=false){
   State=ItemConstructorStorage.CreateForBE0Continuation(Token,new ActorConstructorPrefixState(actorBytes),unknownTail?new byte?[0x735]:Fill(0x735));
   var matrix=new ActorMatrixInitializationSource(ActorMatrixInitializationState.CreateArchivedInitialState(),ActorMatrixThreadState.CreateArchivedTemplateThread(),true,true);
   Bridge=new ItemConstructorBridgeInvocation(State).RunWithArchivedActorProfile(new Actor198ArrayStorage(0x100000UL,Fill(0x208)),
    new ActorConstructorStorage(0x200000UL,Fill(0x358)),new ActorConstructorStorage(0x300000UL,Fill(0x38)),ActorCompletionObservations.WithArchivedConstants(null,null,null),matrix,new ActorEngineSlot(new ActorEngineContext(0x600000UL,9,9,7)));
   var prior950=new ItemConstructor950Invocation(Bridge).Run(new PriorChecks());
   BA8=new ItemConstructorBA8Invocation(prior950).RunWithArchivedWorldVec3Body();
   Array=new ItemBE0ArrayStorage(0x700000UL,Fill(0x108));
   Prior=new ItemConstructorBE0Invocation(BA8,Array).Run(finishBE0?(checks??new Checks()):null);
   if(!BA8.ReachedItem3238BE||(finishBE0&&!Prior.ReachedItem323968))throw new Exception("Real own constructor prerequisites did not reach required boundary");
  }
 }
 static void GetterReadsOnlyTheDword(){
  uint[] ids={0,1,0x7FFFFFFFU,0x80000000U,0xFFFFFFFFU,0x11223344U};
  foreach(uint id in ids){
   var bytes=new byte?[0x530];Put(bytes,0x2C,4,id);
   var state=ItemConstructorStorage.CreateForBE0Continuation(Token,new ActorConstructorPrefixState(bytes));var before=Observe(state);
   var r=ItemObjectIdGetter.Read(state);
   Need(r.Status=="KnownReturn"&&r.Reason==null&&r.Returned&&r.FieldReads==1&&r.ReturnedValue==id&&r.ReturnedRax==(ulong)id&&r.ResumeInstructionRva==0x3A743&&ItemObjectIdGetter.EntryRva==0x3A740&&ItemObjectIdGetter.ReturnRva==0x3A743,"DWORD value including its high bit is returned with zero-extended RAX for "+id);
   Need(Object.ReferenceEquals(r.State,state)&&Same(state,before)&&state.ReadByte(0x28)==null&&state.ReadByte(0x30)==null,"getter needs no adjacent bytes and never modifies its observed state for "+id);
  }
  var missing=ItemObjectIdGetter.Read(null);
  Need(missing.Status=="Unsupported"&&missing.Reason=="UnknownItemConstructorStorage"&&!missing.Returned&&missing.ReturnedValue==null&&missing.ReturnedRax==null&&missing.FieldReads==0&&missing.ResumeInstructionRva==0x3A740,"null storage is not an observed zero ID");
 }
 static void RealChainCopiesExactlyFourBytes(){
  foreach(uint id in new uint[]{0,0x80000000U,0xFFFFFFFFU,0x12345678U}){
   var f=new Fixture(ActorBytes(id));f.State.Store(0x538,4,~id);var before=Observe(f.State);var arrayBefore=f.Array.CopyObservation();var expected=(byte?[])before.Clone();Put(expected,0x538,4,id);
   var r=new ItemConstructorIdentityInvocation(f.Prior).Run();
   Need(r.Status=="ConditionalItemPrefix"&&r.Reason==null&&r.Stage=="Item323977"&&r.ResumeInstructionRva==0x323977&&r.CallSiteRva==null&&r.ReachedItem323977&&!r.IsCompleteItemConstructor&&r.CallerStoreInstructions==1,"identity prefix stops before the next Item call for "+id);
   Need(r.Getter.ReturnedValue==id&&r.Getter.ReturnedRax==(ulong)id&&r.Getter.FieldReads==1&&Same(f.State,expected)&&SameArray(f.Array,arrayBefore),"full Item and allocation oracle allows only the destination DWORD change for "+id);
   Need(Object.ReferenceEquals(r.State,f.State)&&Object.ReferenceEquals(r.Prior,f.Prior)&&Object.ReferenceEquals(r.Getter.State,f.State)&&Object.ReferenceEquals(r.State.Actor,f.Bridge.ActorCompletion.State),"getter and caller use the same real-chain Item and Actor for "+id);
  }
 }
 static void AnyUnknownSourceByteStops(){
  for(int missing=-1;missing<4;missing++){
   var initial=ActorBytes(0x12345678U);
   if(missing<0){for(int i=0;i<4;i++)initial[0x2C+i]=null;}else initial[0x2C+missing]=null;
   var f=new Fixture(initial);f.State.Store(0x538,4,0x13572468UL);var before=Observe(f.State);var arrayBefore=f.Array.CopyObservation();
   var r=new ItemConstructorIdentityInvocation(f.Prior).Run();
   Need(r.Status=="Unsupported"&&r.Reason=="UnknownObjectIdField"&&r.Stage=="Getter"&&r.CallSiteRva==0x323968&&r.ResumeInstructionRva==0x3A740&&r.Getter.FieldReads==1&&!r.Getter.Returned&&r.Getter.ReturnedValue==null&&r.Getter.ReturnedRax==null&&r.CallerStoreInstructions==0&&!r.ReachedItem323977,"unknown source byte cannot fabricate a zero or partial ID at position "+missing);
   Need(Same(f.State,before)&&SameArray(f.Array,arrayBefore)&&f.State.ReadLittleEndian(0x538,4)==0x13572468UL,"unknown getter leaves the old destination and all prior effects intact at position "+missing);
  }
 }
 static void SamplesAtRunAndKeepsTheResult(){
  var f=new Fixture(ActorBytes(1));var beforeCreation=Observe(f.State);var invocation=new ItemConstructorIdentityInvocation(f.Prior);
  Need(!invocation.HasRun&&invocation.Result.Getter==null&&invocation.Result.CallerStoreInstructions==0&&Same(f.State,beforeCreation),"creating an invocation neither reads the source nor writes a destination");
  f.State.Store(0x2C,4,0xFEDCBA98UL);var beforeRun=Observe(f.State);var expected=(byte?[])beforeRun.Clone();Put(expected,0x538,4,0xFEDCBA98UL);
  var result=invocation.Run();
  Need(result.Getter.ReturnedValue==0xFEDCBA98U&&result.Getter.ReturnedRax==0xFEDCBA98UL&&Same(f.State,expected),"Run sees a source change after invocation creation");
  f.State.Store(0x2C,4,7);
  Need(result.Getter.ReturnedValue==0xFEDCBA98U&&f.State.ReadLittleEndian(0x538,4)==0xFEDCBA98UL&&f.State.Actor.ReadLittleEndian(0x2C,4)==7,"later source changes do not retroactively rewrite returned value or destination");
  f.State.Store(0x538,4,9);var snapshot=Observe(f.State);bool again=false;try{invocation.Run();}catch(InvalidOperationException){again=true;}
  Need(again&&invocation.HasRun&&Object.ReferenceEquals(result,invocation.Result)&&result.Getter.FieldReads==1&&Same(f.State,snapshot),"one own invocation does not silently resample and recopy on a second Run");
 }
 static void PriorCallbackAndUnknownDestinationPadding(){
  var checks=new Checks();ItemConstructorStorage callbackState=null;
  checks.Action=delegate(ItemConstructorStorage state,int index){callbackState=state;state.Store(0x2C,4,index==0?0x11111111UL:0x87654321UL);};
  var f=new Fixture(ActorBytes(3),checks,true,true);var before=Observe(f.State);var expected=(byte?[])before.Clone();Put(expected,0x538,4,0x87654321UL);
  var r=new ItemConstructorIdentityInvocation(f.Prior).Run();
  Need(checks.Calls==2&&Object.ReferenceEquals(callbackState,f.State)&&r.Getter.ReturnedValue==0x87654321U&&Same(f.State,expected),"both real BE0 check observations can mutate the ID that the later getter reads");
  Need(f.State.ReadByte(0x53C)==null&&f.State.ReadByte(0x53F)==null&&f.State.ReadLittleEndian(0x538,8)==null&&f.State.ReadLittleEndian(0x538,4)==0x87654321UL,"caller DWORD store preserves adjacent unknown destination bytes");
  Need(f.State.ReadLittleEndian(0x28,4)==f.State.Actor.ReadLittleEndian(0x28,4)&&f.State.ReadLittleEndian(0x30,8)==0x1806D5B18UL&&Object.ReferenceEquals(r.State.Actor,f.Bridge.ActorCompletion.State),"source neighbors and the Item's shared Actor vptr are preserved");
 }
 static void PriorFrontiersCannotBeSkipped(){
  var missing=new ItemConstructorIdentityInvocation(null).Run();
  Need(missing.Status=="Unsupported"&&missing.Reason=="MissingPriorItemBE0Prefix"&&missing.State==null&&missing.Getter==null&&missing.CallerStoreInstructions==0&&!missing.ReachedItem323977,"no prior cannot manufacture an Item or a getter observation");
  var f=new Fixture(ActorBytes(9));var pending=new ItemConstructorBE0Invocation(f.BA8,f.Array);var before=Observe(f.State);var arrayBefore=f.Array.CopyObservation();
  var notRun=new ItemConstructorIdentityInvocation(pending.Result).Run();
  Need(notRun.Reason=="PriorItemBE0PrefixIncomplete"&&notRun.Getter==null&&notRun.CallerStoreInstructions==0&&Same(f.State,before)&&SameArray(f.Array,arrayBefore),"unrun BE0 cannot be substituted with initialized-looking state");
  var stopped=new Fixture(ActorBytes(11),null,false);var stoppedBefore=Observe(stopped.State);var stoppedArray=stopped.Array.CopyObservation();var blocked=new ItemConstructorIdentityInvocation(stopped.Prior).Run();
  Need(!stopped.Prior.ReachedItem323968&&stopped.Prior.ResumeInstructionRva==0x2DCC75&&blocked.Reason=="PriorItemBE0PrefixIncomplete"&&blocked.ResumeInstructionRva==0x2DCC75&&blocked.Getter==null&&blocked.CallerStoreInstructions==0,"a real unresolved BE0 check remains the frontier even with a known ID");
  Need(Same(stopped.State,stoppedBefore)&&SameArray(stopped.Array,stoppedArray)&&stopped.Prior.BulkZeroBytes==264&&stopped.Prior.ArrayStoreInstructions==1,"rejected continuation preserves BE0's actual memset/cookie prefix without progressing it");
 }
 static void UnknownAttemptDoesNotRetryAfterRepair(){
  var bytes=ActorBytes(0x12345678U);bytes[0x2D]=null;var f=new Fixture(bytes);f.State.Store(0x538,4,0xAABBCCDDUL);
  var invocation=new ItemConstructorIdentityInvocation(f.Prior);var stopped=invocation.Run();
  Need(invocation.HasRun&&stopped.Getter.FieldReads==1&&stopped.Getter.ReturnedValue==null&&stopped.CallerStoreInstructions==0,"unknown-source attempt records its own read without a returned ID");
  f.State.Store(0x2C,4,0x12345678UL);var before=Observe(f.State);bool again=false;try{invocation.Run();}catch(InvalidOperationException){again=true;}
  Need(again&&Object.ReferenceEquals(stopped,invocation.Result)&&stopped.Getter.ReturnedValue==null&&Same(f.State,before)&&f.State.ReadLittleEndian(0x538,4)==0xAABBCCDDUL,"repairing input does not silently replay a previously attempted continuation");
  var fresh=new ItemConstructorIdentityInvocation(f.Prior).Run();
  Need(fresh.ReachedItem323977&&fresh.Getter.ReturnedValue==0x12345678U&&f.State.ReadLittleEndian(0x538,4)==0x12345678UL,"a separately explicit own invocation can observe repaired state; single-use is not native idempotence");
 }
 public static int Main(){
  Group("GetterReadsOnlyTheDword",GetterReadsOnlyTheDword);Group("RealChainCopiesExactlyFourBytes",RealChainCopiesExactlyFourBytes);
  Group("AnyUnknownSourceByteStops",AnyUnknownSourceByteStops);Group("SamplesAtRunAndKeepsTheResult",SamplesAtRunAndKeepsTheResult);
  Group("PriorCallbackAndUnknownDestinationPadding",PriorCallbackAndUnknownDestinationPadding);Group("PriorFrontiersCannotBeSkipped",PriorFrontiersCannotBeSkipped);
  Group("UnknownAttemptDoesNotRetryAfterRepair",UnknownAttemptDoesNotRetryAfterRepair);
  Console.WriteLine("ItemConstructorIdentityTests: "+assertions+" assertions, "+groups+" groups passed; own conditional prefix only");return 0;
 }
}
