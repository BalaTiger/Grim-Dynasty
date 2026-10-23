using System;
using System.Collections.Generic;
using GrimDynasty.TaskItems;

// Own conditional integration only. No native guard, cleanup, allocation or ABI claim.
public static class ItemConstructor950Tests {
 static int assertions,groups;
 const ulong Token=0x500000UL;
 static void Need(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Group(string name,Action test){int before=assertions;test();groups++;Console.WriteLine("PASS "+name+": "+(assertions-before)+" assertions");}
 static byte?[] Fill(int size){var bytes=new byte?[size];for(int i=0;i<size;i++)bytes[i]=0xA5;return bytes;}
 sealed class Fixture {
  public readonly ItemConstructorStorage State;
  public readonly ItemConstructorBridgeResult Prior;
  public Fixture(int end=0xB18,bool unknown=false,ulong token=Token){
   var actor=new ActorConstructorPrefixState(unknown?new byte?[0x530]:Fill(0x530));
   State=ItemConstructorStorage.CreateFor950Continuation(token,actor,unknown?new byte?[end-0x530]:Fill(end-0x530));
   var matrix=new ActorMatrixInitializationSource(ActorMatrixInitializationState.CreateArchivedInitialState(),
    ActorMatrixThreadState.CreateArchivedTemplateThread(),true,true);
   Prior=new ItemConstructorBridgeInvocation(State).RunWithArchivedActorProfile(
    new Actor198ArrayStorage(0x100000UL,Fill(0x208)),new ActorConstructorStorage(0x200000UL,Fill(0x358)),
    new ActorConstructorStorage(0x300000UL,Fill(0x38)),ActorCompletionObservations.WithArchivedConstants(null,null,null),
    matrix,new ActorEngineSlot(new ActorEngineContext(0x600000UL,9,9,7)));
   if(!Prior.ReachedItem323743)throw new Exception("Own Actor/Item prerequisite fixture did not reach its real boundary");
  }
 }
 sealed class Checks:IItem950CheckSource {
  public readonly List<int> Indices=new List<int>();
  public readonly List<ulong> Tokens=new List<ulong>();
  public Func<ItemConstructorStorage,int,ulong,Item950CheckObservation> Action;
  public Item950CheckObservation Check(ItemConstructorStorage state,int index,ulong callbackToken){
   Indices.Add(index);Tokens.Add(callbackToken);
   return Action==null?new Item950CheckObservation(true):Action(state,index,callbackToken);
  }
 }
 static byte?[] Observe(ItemConstructorStorage state){var result=new byte?[state.ByteCount];for(int i=0;i<result.Length;i++)result[i]=state.ReadByte(i);return result;}
 static bool Same(ItemConstructorStorage state,byte?[] expected){if(state.ByteCount!=expected.Length)return false;for(int i=0;i<expected.Length;i++)if(state.ReadByte(i)!=expected[i])return false;return true;}
 static bool SameRange(ItemConstructorStorage state,byte?[] before,int first,int end){for(int i=first;i<end;i++)if(state.ReadByte(i)!=before[i])return false;return true;}
 static void Put(byte?[] bytes,int offset,int width,ulong value){for(int i=0;i<width;i++)bytes[offset+i]=unchecked((byte)(value>>(i*8)));}
 static byte?[] Oracle(byte?[] before,int elements,bool suffix){
  // Independent footprint oracle: grouped by field width, not model call order.
  var expected=(byte?[])before.Clone();
  foreach(int offset in new int[]{0x8B8,0x8D8,0x8F8,0x938})Put(expected,offset,8,0);
  foreach(int offset in new int[]{0x8C0,0x8E0,0x900,0x940})Put(expected,offset,8,15);
  foreach(int offset in new int[]{0x8A8,0x8C8,0x8E8,0x928})Put(expected,offset,1,0);
  Put(expected,0x908,8,0x18069E0A8UL);Put(expected,0x910,8,0x18069E0B8UL);
  Put(expected,0x918,8,0x18069E0C8UL);Put(expected,0x920,8,0x18069DFB8UL);
  Put(expected,0x948,4,0);Put(expected,0x950,8,0);
  for(int i=0;i<elements;i++){int element=0x968+0x28*i;Put(expected,element,1,0);Put(expected,element+0x10,8,0);Put(expected,element+0x18,8,15);}
  if(suffix){Put(expected,0xAF8,4,0);foreach(int offset in new int[]{0xB00,0xB08,0xB10})Put(expected,offset,8,0);}
  return expected;
 }
 static void FactoryIdentityAndBounds(){
  var actor=new ActorConstructorPrefixState();var tail=Fill(0x5E8);
  var state=ItemConstructorStorage.CreateFor950Continuation(Token,actor,tail);tail[0]=7;
  Need(state.ByteCount==0xB18&&ItemConstructorStorage.Continuation950Extent==0xB18&&ItemConstructorStorage.Maximum950TailBytes==0x5E8,"explicit profile covers exactly the normal write extent");
  Need(Object.ReferenceEquals(state.Actor,actor)&&state.ReadByte(0x530)==0xA5,"Actor identity is retained while caller input tail is isolated");
  state.Store(0x52C,8,0x1122334455667788UL);
  Need(actor.ReadLittleEndian(0x52C,4)==0x55667788UL&&state.ReadLittleEndian(0x530,4)==0x11223344UL,"Actor/tail seam is one routed observation");
  state.Store(0x6B0,4,7);state.Store(0x850,8,9);state.Store(0x88C,8,0x0000000500000003UL);
  Need(state.Scalars.ReplicaStack6B0==7&&state.Sub858.Untouched850==9&&state.Scalars.Stack88C==3&&state.Scalars.Word890==5,"old views remain live under the larger profile");
  var exported=state.CopyTailObservation();exported[0]=8;
  Need(state.ReadLittleEndian(0x530,4)==0x11223344UL,"exported observation does not mutate backing storage");
  var legacy=new ItemConstructorStorage(ulong.MaxValue-0x89FUL,actor);
  Need(legacy.ByteCount==0x8A0&&legacy.Token+0x89FUL==ulong.MaxValue,"legacy default extent and old token upper bound are unchanged");
  bool tooHigh=false,tooLong=false,zero=false,missingActor=false;
  try{ItemConstructorStorage.CreateFor950Continuation(ulong.MaxValue-0xB16UL,actor);}catch(ArgumentOutOfRangeException){tooHigh=true;}
  try{ItemConstructorStorage.CreateFor950Continuation(Token,actor,new byte?[0x5E9]);}catch(ArgumentException){tooLong=true;}
  try{ItemConstructorStorage.CreateFor950Continuation(0,actor);}catch(ArgumentOutOfRangeException){zero=true;}
  try{ItemConstructorStorage.CreateFor950Continuation(Token,null);}catch(ArgumentNullException){missingActor=true;}
  Need(tooHigh&&tooLong&&zero&&missingActor,"new profile rejects invalid identity/extent without weakening legacy bounds");
  var edge=new Fixture(0xB18,false,ulong.MaxValue-0xB17UL);var result=new ItemConstructor950Invocation(edge.Prior).Run(new Checks());
  Need(result.ReachedItem323811&&edge.State.Token+0xB17UL==ulong.MaxValue&&result.ReturnedSubobjectToken==edge.State.Token+0x950UL,"maximum admitted token preserves exact subobject identity without checked overflow");
 }
 static void DefaultUnknownCheck(){
  var f=new Fixture();var before=Observe(f.State);var invocation=new ItemConstructor950Invocation(f.Prior);var r=invocation.Run();
  Need(r.Status=="Unsupported"&&!r.ReachedItem323811&&!r.WrapperReturned&&r.ReturnedSubobjectToken==null,"missing check observation cannot fabricate a normal return");
  Need(r.ResumeInstructionRva==0x5DAF09&&r.PendingElementIndex==0&&r.Stage=="PreElementCheck","default stops at the first actual Game check");
  Need(r.TotalStoreInstructions==18&&r.CallerStoreInstructions==17&&r.WrapperPrefixStoreInstructions==1&&r.ElementStoreInstructions==0&&r.WrapperPostStoreInstructions==0,"eighteen earlier writes survive the unresolved dependency");
  Need(r.CheckSourceCalls==0&&r.NormalCheckReturns==0&&r.CompletedElements==0&&invocation.HasRun,"no callback or constructed element is invented");
  Need(Same(f.State,Oracle(before,0,false)),"all caller/wrapper writes and every untouched byte match the default-stop oracle");
 }
 static void CompleteFootprintAndOwnership(){
  var f=new Fixture();var before=Observe(f.State);var checks=new Checks();var r=new ItemConstructor950Invocation(f.Prior).Run(checks);
  Need(r.Status=="ConditionalItemPrefix"&&r.ReachedItem323811&&r.ResumeInstructionRva==0x323811&&!r.IsCompleteItemConstructor,"normal modeled prefix stops before remaining Item construction");
  Need(r.TotalStoreInstructions==52&&r.CallerStoreInstructions==17&&r.WrapperPrefixStoreInstructions==1&&r.ElementStoreInstructions==30&&r.WrapperPostStoreInstructions==4,"native field-store accounting excludes stack bookkeeping");
  Need(r.CompletedElements==10&&r.CheckSourceCalls==10&&r.NormalCheckReturns==10&&r.PendingElementIndex==null,"ten checks and ten normal leaf returns are required");
  bool exact=checks.Indices.Count==10;for(int i=0;i<checks.Indices.Count;i++)exact=exact&&checks.Indices[i]==i&&checks.Tokens[i]==0x180016710UL;
  Need(exact,"constructor target is checked once per sequential element, never the unused fifth-argument token");
  Need(r.WrapperReturned&&r.ReturnedSubobjectToken==Token+0x950UL&&Object.ReferenceEquals(r.Prior,f.Prior)&&Object.ReferenceEquals(r.State,f.State),"wrapper result and prior retain original shared identity");
  Need(Same(f.State,Oracle(before,10,true)),"full byte oracle verifies all writes, payload holes, stride tails and wrapper padding");
  Need(SameRange(f.State,before,0,0x8A8)&&f.State.ReadByte(0xAFC)==0xA5&&f.State.ReadByte(0xAFF)==0xA5,"prior Actor/Item fields and suffix DWORD padding are preserved");
 }
 static void UnknownBytesRemainUnknown(){
  var f=new Fixture(0xB18,true);var before=Observe(f.State);var r=new ItemConstructor950Invocation(f.Prior).Run(new Checks());
  Need(r.ReachedItem323811&&Same(f.State,Oracle(before,10,true)),"fresh unknown storage only gains the proved written bytes");
  Need(f.State.ReadByte(0x8A9)==null&&f.State.ReadByte(0x94C)==null&&f.State.ReadByte(0x958)==null&&f.State.ReadByte(0x960)==null,"caller payload and wrapper gaps remain unknown");
  bool padding=true;for(int i=0;i<10;i++){int at=0x968+0x28*i;padding=padding&&f.State.ReadByte(at+1)==null&&f.State.ReadByte(at+0xF)==null&&f.State.ReadByte(at+0x20)==null&&f.State.ReadByte(at+0x27)==null;}
  Need(padding,"every leaf preserves its payload interior and final eight stride bytes");
  Need(f.State.ReadLittleEndian(0xAF8,8)==null&&f.State.ReadLittleEndian(0xAF8,4)==0&&f.State.ReadByte(0xAFC)==null,"DWORD suffix zero does not manufacture known upper DWORD");
 }
 static void EachCheckCanStop(){
  for(int failed=0;failed<10;failed++){
   int stop=failed;var f=new Fixture();var before=Observe(f.State);var checks=new Checks();
   checks.Action=delegate(ItemConstructorStorage s,int index,ulong token){return new Item950CheckObservation(index!=stop,"OwnCheckBoundary");};
   var r=new ItemConstructor950Invocation(f.Prior).Run(checks);
   Need(r.Status=="Unsupported"&&r.Reason=="OwnCheckBoundary"&&r.PendingElementIndex==stop&&r.ResumeInstructionRva==0x5DAF09&&
    r.CheckSourceCalls==stop+1&&r.NormalCheckReturns==stop&&r.CompletedElements==stop&&r.TotalStoreInstructions==18+3*stop&&r.WrapperPostStoreInstructions==0&&!r.WrapperReturned,"check stop preserves exact previously completed count at index "+stop);
   Need(Same(f.State,Oracle(before,stop,false)),"check failure neither clears future elements nor invokes cleanup at index "+stop);
  }
  var empty=new Fixture();var source=new Checks();source.Action=delegate{return null;};
  var missing=new ItemConstructor950Invocation(empty.Prior).Run(source);
  Need(missing.Reason=="MissingItem950CheckObservation"&&missing.CheckSourceCalls==1&&missing.NormalCheckReturns==0&&missing.TotalStoreInstructions==18,"null callback result is unknown rather than a successful check");
 }
 static void CallbackSeesAndChangesLiveState(){
  var f=new Fixture();var checks=new Checks();bool visible=true;
  checks.Action=delegate(ItemConstructorStorage s,int index,ulong callback){
   int at=0x968+0x28*index;
   visible=visible&&Object.ReferenceEquals(s,f.State)&&callback==0x180016710UL&&s.ReadLittleEndian(at+0x18,8)==0xA5A5A5A5A5A5A5A5UL;
   if(index>0)visible=visible&&s.ReadLittleEndian(at-0x28+0x18,8)==15&&s.ReadByte(at-0x28+1)==(byte)(0x40+index-1);
   s.Store(at+1,1,(ulong)(0x40+index));s.Store(at+0x20,8,0x1122334400000000UL+(ulong)index);
   s.Store(at+0x18,8,0x100);s.Store(at+0x10,8,9);s.Store(at,1,0xCC);
   if(index==0){s.Store(0x950,8,7);s.Store(0xB00,8,0xCC);s.Store(0x88C,4,12);}
   return new Item950CheckObservation(true);
  };
  var r=new ItemConstructor950Invocation(f.Prior).Run(checks);
  Need(visible&&r.ReachedItem323811,"each callback sees the current element before initialization and the preceding completed element");
  bool values=true;for(int i=0;i<10;i++){int at=0x968+0x28*i;values=values&&f.State.ReadByte(at)==0&&f.State.ReadLittleEndian(at+0x10,8)==0&&f.State.ReadLittleEndian(at+0x18,8)==15&&f.State.ReadByte(at+1)==(byte)(0x40+i)&&f.State.ReadLittleEndian(at+0x20,8)==0x1122334400000000UL+(ulong)i;}
  Need(values,"leaf overwrites only its three proved fields even after callback installs heap-like capacity");
  Need(f.State.ReadLittleEndian(0x950,8)==7&&f.State.ReadLittleEndian(0xB00,8)==0,"callbacks can change earlier fields while suffix writes remain delayed until all leaves return");
  Need(f.State.Scalars.Stack88C==12&&Object.ReferenceEquals(r.State.Actor,f.Prior.ActorCompletion.State),"callback changes reach existing shared views without copying Actor state");
  Need(r.TotalStoreInstructions==52&&r.CheckSourceCalls==10,"own callback mutations are not miscounted as archived field stores");
 }
 static void ProjectionStopsInActualOrder(){
  int[] ends={0x8A0,0x8C7,0x8C8,0x950,0x968,0x988,0xAF0,0xAFC,0xB08,0xB10};
  int[] stores={0,0,3,17,18,21,48,49,50,51};
  int[] completed={0,0,0,0,0,1,10,10,10,10};
  int[] calls={0,0,0,0,1,2,10,10,10,10};
  uint[] sites={0x32374A,0x32374A,0x32376A,0x4286A6,0x16710,0x16710,0x4286C5,0x4286CE,0x4286D5,0x4286DC};
  for(int i=0;i<ends.Length;i++){
   var f=new Fixture(ends[i]);var before=Observe(f.State);var r=new ItemConstructor950Invocation(f.Prior).Run(new Checks());
   Need(r.Status=="Unsupported"&&r.ResumeInstructionRva==sites[i]&&r.TotalStoreInstructions==stores[i]&&r.CompletedElements==completed[i]&&r.CheckSourceCalls==calls[i]&&r.NormalCheckReturns==calls[i]&&!r.WrapperReturned&&!r.ReachedItem323811,"bounded storage stops at the first missing store for extent "+ends[i]);
   Need(SameRange(f.State,before,0,0x8A8<ends[i]?0x8A8:ends[i]),"short continuation never rewrites the earlier Actor/Item prefix for extent "+ends[i]);
  }
  var legacy=new ItemConstructorStorage(Token,new ActorConstructorPrefixState());
  bool rejects=false;try{new ItemConstructorStorage(Token,new ActorConstructorPrefixState(),Fill(0x5E8));}catch(ArgumentException){rejects=true;}
  Need(legacy.ByteCount==0x8A0&&rejects,"legacy constructor does not silently admit the new extended profile");
 }
 static void ExceptionsAndSingleInvocation(){
  var f=new Fixture();var checks=new Checks();var error=new Exception("owned check exception");
  checks.Action=delegate(ItemConstructorStorage s,int index,ulong token){if(index==3){s.Store(0x950,8,99);throw error;}return new Item950CheckObservation(true);};
  var invocation=new ItemConstructor950Invocation(f.Prior);bool caught=false;
  try{invocation.Run(checks);}catch(Exception e){caught=Object.ReferenceEquals(e,error);}
  var r=invocation.Result;
  Need(caught&&invocation.HasRun&&r.CheckSourceCalls==4&&r.NormalCheckReturns==3&&r.CompletedElements==3&&r.TotalStoreInstructions==27,"throwing check preserves completed prefix without converting native EH");
  Need(r.PendingElementIndex==3&&r.ResumeInstructionRva==0x5DAF09&&!r.WrapperReturned&&r.WrapperPostStoreInstructions==0&&r.Reason=="Item950CheckNotCompleted","interrupted invocation retains its active check boundary");
  Need(f.State.ReadLittleEndian(0x950,8)==99&&f.State.ReadLittleEndian(0x980,8)==15&&f.State.ReadLittleEndian(0x9F8,8)==0xA5A5A5A5A5A5A5A5UL,"callback effects and earlier leaf survive without cleanup or a future leaf");
  bool again=false;try{invocation.Run(new Checks());}catch(InvalidOperationException){again=true;}
  Need(again&&r.CheckSourceCalls==4&&f.State.ReadLittleEndian(0x950,8)==99,"throwing attempt cannot be replayed silently");
  var next=new Fixture();var own=new ItemConstructor950Invocation(next.Prior);var nested=new Checks();bool nestedRejected=false;
  nested.Action=delegate(ItemConstructorStorage s,int index,ulong token){if(index==0){try{own.Run();}catch(InvalidOperationException){nestedRejected=true;}}return new Item950CheckObservation(true);};
  var done=own.Run(nested);
  Need(nestedRejected&&done.ReachedItem323811&&done.CheckSourceCalls==10,"reentrant invocation is rejected while the outer own callback can return normally");
  next.State.Store(0xB10,8,42);bool repeated=false;try{own.Run();}catch(InvalidOperationException){repeated=true;}
  Need(repeated&&next.State.ReadLittleEndian(0xB10,8)==42,"successful invocation also cannot rerun archived writes");
 }
 static void InvalidPriorDoesNotAuthorizeWrites(){
  var missing=new ItemConstructor950Invocation(null).Run(new Checks());
  Need(missing.Status=="Unsupported"&&missing.TotalStoreInstructions==0&&missing.CheckSourceCalls==0&&missing.State==null,"absent prerequisite is rejected without an invented Item");
  var state=ItemConstructorStorage.CreateFor950Continuation(Token,new ActorConstructorPrefixState(),Fill(0x5E8));
  var incomplete=new ItemConstructorBridgeInvocation(state).RunWithArchivedActorProfile(null,null,null,null,null,null);
  var before=Observe(state);var result=new ItemConstructor950Invocation(incomplete).Run(new Checks());
  Need(!result.ReachedItem323811&&result.TotalStoreInstructions==0&&result.CheckSourceCalls==0&&result.ResumeInstructionRva==incomplete.ResumeInstructionRva,"actual stopped Actor prerequisite cannot authorize Item950");
  Need(Same(state,before)&&Object.ReferenceEquals(result.State,state),"invalid prior retains exactly the existing prefix and same storage");
 }
 public static int Main(){
  Group("FactoryIdentityAndBounds",FactoryIdentityAndBounds);
  Group("DefaultUnknownCheck",DefaultUnknownCheck);
  Group("CompleteFootprintAndOwnership",CompleteFootprintAndOwnership);
  Group("UnknownBytesRemainUnknown",UnknownBytesRemainUnknown);
  Group("EachCheckCanStop",EachCheckCanStop);
  Group("CallbackSeesAndChangesLiveState",CallbackSeesAndChangesLiveState);
  Group("ProjectionStopsInActualOrder",ProjectionStopsInActualOrder);
  Group("ExceptionsAndSingleInvocation",ExceptionsAndSingleInvocation);
  Group("InvalidPriorDoesNotAuthorizeWrites",InvalidPriorDoesNotAuthorizeWrites);
  Console.WriteLine("ItemConstructor950Tests: "+assertions+" assertions, "+groups+" groups passed; own conditional prefix only");return 0;
 }
}
