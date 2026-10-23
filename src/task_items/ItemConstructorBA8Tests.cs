using System;
using GrimDynasty.TaskItems;

// Own conditional integration. No native execution, loaded-IAT or type-size claim.
public static class ItemConstructorBA8Tests {
 static int assertions,groups;
 const ulong Token=0x500000UL;
 static void Need(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Group(string name,Action test){int before=assertions;test();groups++;Console.WriteLine("PASS "+name+": "+(assertions-before)+" assertions");}
 static byte?[] Fill(int size){var bytes=new byte?[size];for(int i=0;i<size;i++)bytes[i]=0xA5;return bytes;}
 sealed class Checks:IItem950CheckSource {
  public int Calls;
  public Action<ItemConstructorStorage,int> BeforeReturn;
  public Item950CheckObservation Check(ItemConstructorStorage state,int index,ulong callbackToken){
   if(index!=Calls||callbackToken!=0x180016710UL)throw new Exception("Unexpected prerequisite check identity/order");
   Calls++;if(BeforeReturn!=null)BeforeReturn(state,index);return new Item950CheckObservation(true);
  }
 }
 sealed class Fixture {
  public readonly ItemConstructorStorage State;
  public readonly ItemConstructorBridgeResult Bridge;
  public readonly ItemConstructor950Result Prior;
  public readonly Checks CheckSource;
  public Fixture(int end=0xBC8,bool unknown=false,ulong token=Token,Checks checks=null){
   var actor=new ActorConstructorPrefixState(unknown?new byte?[0x530]:Fill(0x530));
   State=ItemConstructorStorage.CreateForBA8Continuation(token,actor,unknown?new byte?[end-0x530]:Fill(end-0x530));
   var matrix=new ActorMatrixInitializationSource(ActorMatrixInitializationState.CreateArchivedInitialState(),
    ActorMatrixThreadState.CreateArchivedTemplateThread(),true,true);
   Bridge=new ItemConstructorBridgeInvocation(State).RunWithArchivedActorProfile(
    new Actor198ArrayStorage(0x100000UL,Fill(0x208)),new ActorConstructorStorage(0x200000UL,Fill(0x358)),
    new ActorConstructorStorage(0x300000UL,Fill(0x38)),ActorCompletionObservations.WithArchivedConstants(null,null,null),
    matrix,new ActorEngineSlot(new ActorEngineContext(0x600000UL,9,9,7)));
   if(!Bridge.ReachedItem323743)throw new Exception("Real own Actor/Item prerequisite did not reach323743");
   CheckSource=checks??new Checks();Prior=new ItemConstructor950Invocation(Bridge).Run(CheckSource);
   if(!Prior.ReachedItem323811)throw new Exception("Real own950 prerequisite did not reach323811");
  }
 }
 static byte?[] Observe(ItemConstructorStorage state){var result=new byte?[state.ByteCount];for(int i=0;i<result.Length;i++)result[i]=state.ReadByte(i);return result;}
 static bool Same(ItemConstructorStorage state,byte?[] expected){if(state.ByteCount!=expected.Length)return false;for(int i=0;i<expected.Length;i++)if(state.ReadByte(i)!=expected[i])return false;return true;}
 static bool SameRange(ItemConstructorStorage state,byte?[] before,int first,int end){for(int i=first;i<end;i++)if(state.ReadByte(i)!=before[i])return false;return true;}
 static void Put(byte?[] bytes,int offset,int width,ulong value,int cutoff){
  if(offset+width>cutoff)return;
  if(offset+width>bytes.Length)throw new Exception("Oracle attempts to write outside captured bytes");
  for(int i=0;i<width;i++)bytes[offset+i]=unchecked((byte)(value>>(8*i)));
 }
 static byte?[] Oracle(byte?[] before,int callerCutoff=0xBA8,int leaves=2){
  // Independent grouped footprint, not the model's instruction list.
  var expected=(byte?[])before.Clone();
  foreach(int offset in new int[]{0xB28,0xB30,0xB38,0xB48,0xB50,0xB58,0xB70,0xB78,0xB80,0xB90,0xBA0})Put(expected,offset,8,0,callerCutoff);
  foreach(int offset in new int[]{0xB20,0xB44})Put(expected,offset,4,0,callerCutoff);
  foreach(int offset in new int[]{0xB24,0xB88})Put(expected,offset,2,0,callerCutoff);
  Put(expected,0xB40,2,0x0101,callerCutoff);Put(expected,0xB60,2,1,callerCutoff);
  foreach(int offset in new int[]{0xB26,0xB62,0xB98})Put(expected,offset,1,0,callerCutoff);
  if(leaves>=1)Put(expected,0xBA8,8,0,0xBB0);
  if(leaves>=2)Put(expected,0xBC0,8,0,0xBC8);
  return expected;
 }
 static void CaptureIdentityAndBounds(){
  var actor=new ActorConstructorPrefixState();var input=Fill(0x698);
  var state=ItemConstructorStorage.CreateForBA8Continuation(Token,actor,input);input[0]=7;
  Need(state.ByteCount==0xBC8&&ItemConstructorStorage.ContinuationBA8Extent==0xBC8&&ItemConstructorStorage.MaximumBA8TailBytes==0x698,"capture ends at the second observed QWORD, not an inferred object size");
  Need(Object.ReferenceEquals(state.Actor,actor)&&state.ReadByte(0x530)==0xA5,"same Actor is retained and caller tail is cloned");
  state.Store(0x52C,8,0x1122334455667788UL);
  Need(actor.ReadLittleEndian(0x52C,4)==0x55667788UL&&state.ReadLittleEndian(0x530,4)==0x11223344UL,"Actor/tail seam remains a single live observation");
  state.Store(0x6B0,4,7);state.Store(0x850,8,9);state.Store(0x88C,8,0x0000000500000003UL);
  Need(state.Scalars.ReplicaStack6B0==7&&state.Sub858.Untouched850==9&&state.Scalars.Stack88C==3&&state.Scalars.Word890==5,"all existing Item views stay live in the new capture");
  var exported=state.CopyTailObservation();exported[0]=8;
  Need(state.ReadLittleEndian(0x530,4)==0x11223344UL,"exported tail cannot mutate shared backing");
  var old=new ItemConstructorStorage(ulong.MaxValue-0x89FUL,actor);
  var old950=ItemConstructorStorage.CreateFor950Continuation(ulong.MaxValue-0xB17UL,actor);
  Need(old.ByteCount==0x8A0&&old.Token+0x89FUL==ulong.MaxValue&&old950.ByteCount==0xB18&&old950.Token+0xB17UL==ulong.MaxValue,"both prior profile defaults and token bounds remain unchanged");
  bool high=false,longTail=false,zero=false,missing=false,oldRejects=false;
  try{ItemConstructorStorage.CreateForBA8Continuation(ulong.MaxValue-0xBC6UL,actor);}catch(ArgumentOutOfRangeException){high=true;}
  try{ItemConstructorStorage.CreateForBA8Continuation(Token,actor,new byte?[0x699]);}catch(ArgumentException){longTail=true;}
  try{ItemConstructorStorage.CreateForBA8Continuation(0,actor);}catch(ArgumentOutOfRangeException){zero=true;}
  try{ItemConstructorStorage.CreateForBA8Continuation(Token,null);}catch(ArgumentNullException){missing=true;}
  try{ItemConstructorStorage.CreateFor950Continuation(Token,actor,new byte?[0x698]);}catch(ArgumentException){oldRejects=true;}
  Need(high&&longTail&&zero&&missing&&oldRejects,"new explicit capture does not weaken token, tail or old-profile bounds");
  var edge=new Fixture(0xBC8,false,ulong.MaxValue-0xBC7UL);var r=new ItemConstructorBA8Invocation(edge.Prior).RunWithArchivedWorldVec3Body();
  Need(r.ReachedItem3238BE&&edge.State.Token+0xBC7UL==ulong.MaxValue&&r.SecondSubobject.ReturnedThisToken==ulong.MaxValue-7UL,"maximum admitted token supports both exact subobject return identities without overflow");
 }
 static void FullFootprintAndReturns(){
  var f=new Fixture();var before=Observe(f.State);var invocation=new ItemConstructorBA8Invocation(f.Prior);var r=invocation.RunWithArchivedWorldVec3Body();
  Need(r.Status=="ConditionalItemPrefix"&&r.Reason==null&&r.Stage=="Item3238BE"&&r.ResumeInstructionRva==0x3238BE&&r.CallSiteRva==null&&r.ReachedItem3238BE&&!r.IsCompleteItemConstructor,"normal continuation reaches only the next Item boundary");
  Need(r.CallerStoreInstructions==20&&r.TotalStoreInstructions==22&&r.FirstSubobject.StoreInstructions==1&&r.SecondSubobject.StoreInstructions==1,"twenty caller stores plus two single-QWORD bodies are counted");
  Need(r.FirstSubobject.Returned&&r.SecondSubobject.Returned&&r.FirstSubobject.Status=="KnownReturn"&&r.SecondSubobject.Status=="KnownReturn"&&r.FirstSubobject.ResumeInstructionRva==0x228ABA&&r.SecondSubobject.ResumeInstructionRva==0x228ABA,"both archived leaf returns are reached");
  Need(r.FirstSubobject.ReturnedThisToken==Token+0xBA8UL&&r.SecondSubobject.ReturnedThisToken==Token+0xBC0UL&&r.FirstSubobject.Offset==0xBA8&&r.SecondSubobject.Offset==0xBC0,"the second identity is recomputed from original this, not first returned this");
  Need(Object.ReferenceEquals(r.Prior,f.Prior)&&Object.ReferenceEquals(r.State,f.State)&&Object.ReferenceEquals(r.FirstSubobject.Storage,f.State)&&Object.ReferenceEquals(r.SecondSubobject.Storage,f.State)&&Object.ReferenceEquals(r.State.Actor,f.Bridge.ActorCompletion.State),"the entire real own chain keeps one shared state and Actor identity");
  Need(Same(f.State,Oracle(before)),"complete byte oracle includes all written fields and every preserved gap");
  int changed=0;for(int i=0;i<before.Length;i++)if(f.State.ReadByte(i)!=before[i])changed++;
  Need(changed==123&&f.State.ReadLittleEndian(0xB40,2)==0x0101&&f.State.ReadLittleEndian(0xB60,2)==0x0001,"exactly107 caller bytes and16 leaf bytes change; the two WORD constants differ");
  Need(SameRange(f.State,before,0,0xB20)&&SameRange(f.State,before,0xBB0,0xBC0)&&f.CheckSource.Calls==10&&invocation.HasRun,"older fields and the16-byte inter-call gap are retained without extra prerequisite calls");
 }
 static void UnknownPaddingRemainsUnknown(){
  var f=new Fixture(0xBC8,true);var before=Observe(f.State);var r=new ItemConstructorBA8Invocation(f.Prior).RunWithArchivedWorldVec3Body();
  Need(r.ReachedItem3238BE&&Same(f.State,Oracle(before)),"unknown capture gains exactly the proved writes");
  int newlyKnown=0;for(int i=0;i<before.Length;i++)if(!before[i].HasValue&&f.State.ReadByte(i).HasValue)newlyKnown++;
  Need(newlyKnown==123,"unwritten unknown bytes are not silently initialized");
  bool holes=true;foreach(int at in new int[]{0xB18,0xB1F,0xB27,0xB42,0xB43,0xB63,0xB68,0xB6F,0xB8A,0xB8F,0xB99,0xB9F,0xBB0,0xBB7,0xBB8,0xBBF})holes=holes&&!f.State.ReadByte(at).HasValue;
  Need(holes,"narrow-write padding and all BB0..BBF sample boundaries remain unknown");
  Need(f.State.ReadLittleEndian(0xB60,4)==null&&f.State.ReadLittleEndian(0xB60,2)==1&&f.State.ReadByte(0xB62)==0,"adjacent WORD and BYTE writes do not manufacture a known fourth byte");
 }
 static void EarlierCallbacksKeepOneState(){
  var checks=new Checks();ItemConstructorStorage seen=null;
  checks.BeforeReturn=delegate(ItemConstructorStorage s,int index){
   seen=s;if(index==9){s.Store(0xBB0,8,0x1122334455667788UL);s.Store(0xBB8,8,0x8877665544332211UL);s.Store(0xB40,2,0xFFFF);s.Store(0x52C,4,77);s.Store(0x6B0,4,7);s.Store(0x850,8,9);}
  };
  var f=new Fixture(0xBC8,false,Token,checks);var before=Observe(f.State);var r=new ItemConstructorBA8Invocation(f.Prior).RunWithArchivedWorldVec3Body();
  Need(Object.ReferenceEquals(seen,f.State)&&Object.ReferenceEquals(r.State,f.Prior.State)&&Same(f.State,Oracle(before)),"prerequisite callback mutations are carried through the same backing, not a copied prior result");
  Need(f.State.ReadLittleEndian(0xBB0,8)==0x1122334455667788UL&&f.State.ReadLittleEndian(0xBB8,8)==0x8877665544332211UL,"both QWORDs between constructor calls survive exactly");
  Need(f.State.ReadLittleEndian(0xB40,2)==0x0101&&f.State.Actor.ReadLittleEndian(0x52C,4)==77&&f.State.Scalars.ReplicaStack6B0==7&&f.State.Sub858.Untouched850==9,"later proved write wins while earlier Actor and Item view changes persist");
  Need(checks.Calls==10&&r.TotalStoreInstructions==22,"old own check mutations do not become BA8 native stores or invented callbacks");
 }
 static void EveryCallerWriteIsAtomic(){
  int[] offsets={0xB20,0xB24,0xB26,0xB28,0xB30,0xB38,0xB40,0xB44,0xB48,0xB50,0xB58,0xB60,0xB62,0xB70,0xB78,0xB80,0xB88,0xB90,0xB98,0xBA0};
  int[] widths={4,2,1,8,8,8,2,4,8,8,8,2,1,8,8,8,2,8,1,8};
  uint[] rvas={0x323812,0x323818,0x323821,0x323828,0x32382F,0x323836,0x32383D,0x323846,0x32384C,0x323853,0x32385A,0x323861,0x32386A,0x323871,0x323878,0x32387F,0x323886,0x32388F,0x323896,0x32389D};
  for(int i=0;i<offsets.Length;i++){
   var f=new Fixture(offsets[i]+widths[i]-1);var before=Observe(f.State);var r=new ItemConstructorBA8Invocation(f.Prior).RunWithArchivedWorldVec3Body();
   Need(r.Status=="Unsupported"&&r.Reason=="MissingItemBA8FieldStorage"&&r.Stage=="CallerPrefix"&&r.ResumeInstructionRva==rvas[i]&&r.CallSiteRva==null&&r.CallerStoreInstructions==i&&r.TotalStoreInstructions==i&&r.FirstSubobject==null&&r.SecondSubobject==null&&!r.ReachedItem3238BE&&Same(f.State,Oracle(before,offsets[i],0)),"short capture rejects an entire store and preserves exact previous prefix at instruction "+rvas[i].ToString("X"));
  }
  var oldEnd=new Fixture(0xB18);var original=Observe(oldEnd.State);var stopped=new ItemConstructorBA8Invocation(oldEnd.Prior).RunWithArchivedWorldVec3Body();
  Need(stopped.TotalStoreInstructions==0&&stopped.ResumeInstructionRva==0x323812&&Same(oldEnd.State,original),"the old950 endpoint can finish950 but cannot authorize even the first BA8 caller store");
 }
 static void FirstAndSecondStorageBoundaries(){
  int[] ends={0xBA8,0xBAF,0xBB0,0xBC0,0xBC7};
  for(int i=0;i<ends.Length;i++){
   var f=new Fixture(ends[i]);var before=Observe(f.State);var r=new ItemConstructorBA8Invocation(f.Prior).RunWithArchivedWorldVec3Body();int complete=ends[i]>=0xBB0?1:0;
   Need(r.Status=="Unsupported"&&r.Reason=="MissingWorldVec3Storage"&&r.ResumeInstructionRva==0x228AB0&&r.CallSiteRva==(complete==0?0x3238ABU:0x3238B8U)&&r.CallerStoreInstructions==20&&r.TotalStoreInstructions==20+complete&&!r.ReachedItem3238BE&&Same(f.State,Oracle(before,0xBA8,complete)),"leaf range failure preserves all caller and completed-leaf writes for extent "+ends[i].ToString("X"));
   Need(complete==0 ? r.Stage=="FirstSubobject"&&!r.FirstSubobject.Returned&&r.FirstSubobject.ReturnedThisToken==null&&r.SecondSubobject==null : r.Stage=="SecondSubobject"&&r.FirstSubobject.Returned&&r.FirstSubobject.ReturnedThisToken==Token+0xBA8UL&&!r.SecondSubobject.Returned&&r.SecondSubobject.ReturnedThisToken==null,"a second call is represented only after the first returns normally");
  }
 }
 static void LeafContractHasNoInventedLayout(){
  var state=ItemConstructorStorage.CreateForBA8Continuation(Token,new ActorConstructorPrefixState(),Fill(0x698));var before=Observe(state);
  var first=WorldVec3DefaultConstructor.Apply(state,0xBA8);
  Need(first.Status=="KnownReturn"&&first.Returned&&first.StoreInstructions==1&&first.ReturnedThisToken==Token+0xBA8UL&&first.ResumeInstructionRva==0x228ABA&&state.ReadLittleEndian(0xBA8,8)==0&&SameRange(state,before,0,0xBA8)&&SameRange(state,before,0xBB0,0xBC8),"standalone archived body changes exactly one QWORD and returns its input identity");
  var snapshot=Observe(state);var wrong=WorldVec3DefaultConstructor.Apply(state,0xBB0);
  Need(wrong.Status=="Unsupported"&&wrong.Reason=="UnsupportedWorldVec3Offset"&&wrong.StoreInstructions==0&&!wrong.Returned&&wrong.ReturnedThisToken==null&&Same(state,snapshot),"call-site coverage is explicit, not an arbitrary vector-layout helper");
  var missing=WorldVec3DefaultConstructor.Apply(null,0xBA8);
  Need(missing.Reason=="UnknownItemConstructorStorage"&&missing.StoreInstructions==0&&!missing.Returned&&missing.ResumeInstructionRva==0x228AB0,"absent storage is not a successful zero initialization");
  var shortState=ItemConstructorStorage.CreateForBA8Continuation(Token,new ActorConstructorPrefixState(),Fill(0xBAF-0x530));var shortBefore=Observe(shortState);var shortResult=WorldVec3DefaultConstructor.Apply(shortState,0xBA8);
  Need(shortResult.Reason=="MissingWorldVec3Storage"&&shortResult.StoreInstructions==0&&!shortResult.Returned&&Same(shortState,shortBefore),"seven available bytes cannot cause a partial QWORD store");
 }
 static void PrerequisitesAndSingleAttempt(){
  var absent=new ItemConstructorBA8Invocation(null).RunWithArchivedWorldVec3Body();
  Need(absent.Status=="Unsupported"&&absent.Reason=="MissingPriorItem950Prefix"&&absent.State==null&&absent.TotalStoreInstructions==0&&absent.FirstSubobject==null,"missing950 result cannot invent a completed prerequisite");
  var f=new Fixture();var pending=new ItemConstructor950Invocation(f.Bridge);var before=Observe(f.State);var notRun=new ItemConstructorBA8Invocation(pending.Result).RunWithArchivedWorldVec3Body();
  Need(notRun.Reason=="PriorItem950PrefixIncomplete"&&notRun.TotalStoreInstructions==0&&Same(f.State,before),"a newly created but unrun950 result does not authorize BA8 even when storage has old initialized bytes");
  var stopped950=pending.Run();var stoppedBefore=Observe(f.State);var blocked=new ItemConstructorBA8Invocation(stopped950).RunWithArchivedWorldVec3Body();
  Need(blocked.Reason=="PriorItem950PrefixIncomplete"&&blocked.ResumeInstructionRva==0x5DAF09&&blocked.TotalStoreInstructions==0&&blocked.FirstSubobject==null&&Same(f.State,stoppedBefore),"actual unknown950 check frontier is preserved and not skipped");
  var success=new Fixture();var invocation=new ItemConstructorBA8Invocation(success.Prior);
  Need(!invocation.HasRun&&invocation.Result.Reason=="NotRun"&&invocation.Result.TotalStoreInstructions==0,"constructing an invocation does not execute it");
  var done=invocation.RunWithArchivedWorldVec3Body();success.State.Store(0xBA8,8,42);bool repeated=false;
  try{invocation.RunWithArchivedWorldVec3Body();}catch(InvalidOperationException){repeated=true;}
  Need(repeated&&invocation.HasRun&&Object.ReferenceEquals(done,invocation.Result)&&success.State.ReadLittleEndian(0xBA8,8)==42&&done.TotalStoreInstructions==22,"successful own invocation cannot silently repeat native writes");
  var shortFixture=new Fixture(0xBC7);var shortAttempt=new ItemConstructorBA8Invocation(shortFixture.Prior);var partial=shortAttempt.RunWithArchivedWorldVec3Body();shortFixture.State.Store(0xBA8,8,43);bool partialAgain=false;
  try{shortAttempt.RunWithArchivedWorldVec3Body();}catch(InvalidOperationException){partialAgain=true;}
  Need(partialAgain&&Object.ReferenceEquals(partial,shortAttempt.Result)&&partial.TotalStoreInstructions==21&&shortFixture.State.ReadLittleEndian(0xBA8,8)==43,"partial failure is also sticky, without rewriting its successful prefix");
 }
 public static int Main(){
  Group("CaptureIdentityAndBounds",CaptureIdentityAndBounds);
  Group("FullFootprintAndReturns",FullFootprintAndReturns);
  Group("UnknownPaddingRemainsUnknown",UnknownPaddingRemainsUnknown);
  Group("EarlierCallbacksKeepOneState",EarlierCallbacksKeepOneState);
  Group("EveryCallerWriteIsAtomic",EveryCallerWriteIsAtomic);
  Group("FirstAndSecondStorageBoundaries",FirstAndSecondStorageBoundaries);
  Group("LeafContractHasNoInventedLayout",LeafContractHasNoInventedLayout);
  Group("PrerequisitesAndSingleAttempt",PrerequisitesAndSingleAttempt);
  Console.WriteLine("ItemConstructorBA8Tests: "+assertions+" assertions, "+groups+" groups passed; own conditional prefix only");return 0;
 }
}
