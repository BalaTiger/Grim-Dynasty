using System;
using System.Collections.Generic;
using GrimDynasty.TaskItems;

// Own conditional integration, not native object execution or ABI compatibility.
public static class ItemConstructorBridgeTests {
 static int assertions,groups;
 const ulong ItemToken=0x500000UL;
 static void Need(bool ok,string why){assertions++;if(!ok)throw new Exception(why);}
 static void Group(string name,Action body){int before=assertions;body();groups++;Console.WriteLine("PASS "+name+": "+(assertions-before)+" assertions");}
 static byte?[] Fill(int n){var a=new byte?[n];for(int i=0;i<n;i++)a[i]=0xA5;return a;}
 static ItemConstructorStorage Storage(){return StorageTo(0x8A0);}
 static ItemConstructorStorage StorageTo(int end){return new ItemConstructorStorage(ItemToken,new ActorConstructorPrefixState(Fill(0x530)),Fill(end-0x530));}
 sealed class Matrix:IActorMatrixStateSource {
  public int Count;public Action<int> Hook;public bool Fail;
  readonly ActorMatrixInitializationSource inner=new ActorMatrixInitializationSource(
   ActorMatrixInitializationState.CreateArchivedInitialState(),ActorMatrixThreadState.CreateArchivedTemplateThread(),true,true);
  public ActorMatrixReadResult Read(){Count++;if(Hook!=null)Hook(Count);return Fail?ActorMatrixReadResult.Unsupported("OwnMatrixStop",0x273AEA):inner.Read();}
 }
 static Actor198ArrayStorage ArrayStorage(){return new Actor198ArrayStorage(0x100000UL,Fill(0x208));}
 static ActorConstructorStorage Sub(){return new ActorConstructorStorage(0x200000UL,Fill(0x358));}
 static ActorConstructorStorage Sentinel(){return new ActorConstructorStorage(0x300000UL,Fill(0x38));}
 static ActorCompletionObservations Data(){return ActorCompletionObservations.WithArchivedConstants(null,null,null);}
 static ActorEngineSlot Slot(){return new ActorEngineSlot(new ActorEngineContext(0x600000UL,9,9,7));}
 static ItemConstructorBridgeResult Run(ItemConstructorStorage s){return new ItemConstructorBridgeInvocation(s).RunWithArchivedActorProfile(ArrayStorage(),Sub(),Sentinel(),Data(),new Matrix(),Slot());}
 static ActorCompletionResult ActorFor(ItemConstructorStorage s,ulong token){
  var a=ActorConstructorArray.ApplyWithArchivedFileInitialCheckTarget(s.Actor,ArrayStorage());
  var b=ActorConstructorContinuation.ContinueUsingArchivedConstant(a,0x200000UL);
  return ActorConstructorCompletion.ContinueWithStateSources(b,token,Sub(),Sentinel(),Data(),new Matrix(),Slot());
 }
 static void WriteExpected(byte?[] a,int offset,int width,ulong value){for(int i=0;i<width;i++)a[offset+i]=unchecked((byte)(value>>(8*i)));}
 static byte?[] LeafOracle(){
  // Independent width/value sets from the retained native instruction evidence.
  var a=Fill(0x190);
  foreach(int p in new int[]{0,0x68,0xD0,0x160,0x188})WriteExpected(a,p,4,0);
  foreach(int p in new int[]{0x18,0x38,0x58,0x80,0xA0,0xC0,0xE8,0xF8,0x110,0x130,0x150,0x168,0x170,0x180})WriteExpected(a,p,8,0);
  foreach(int p in new int[]{8,0x28,0x48,0x70,0x90,0xB0,0xD8,0x100,0x120,0x140})WriteExpected(a,p,1,0);
  foreach(int p in new int[]{0x20,0x40,0x60,0x88,0xA8,0xC8,0xF0,0x118,0x138,0x158})WriteExpected(a,p,8,15);
  WriteExpected(a,0x164,1,1);WriteExpected(a,0x178,8,1);return a;
 }
 static bool SameAt(ItemConstructorStorage s,int offset,byte?[] expected){for(int i=0;i<expected.Length;i++)if(s.ReadByte(offset+i)!=expected[i])return false;return true;}
 static bool Filled(ItemConstructorStorage s,int start,int length){for(int i=0;i<length;i++)if(s.ReadByte(start+i)!=0xA5)return false;return true;}
 static void LeafWritesAndPadding(){
  var s=Storage();var oracle=LeafOracle();var first=ItemSubobject0410Initialization.Apply(s,0x538);
  Need(first.Returned&&first.StoreInstructions==41&&first.ReturnedThisToken==ItemToken+0x538UL,"first leaf returns exact this after41 stores");
  Need(SameAt(s,0x538,oracle),"every first-subobject byte matches independent native width oracle");
  Need(Filled(s,0x6C8,0x190)&&Filled(s,0x530,8),"first leaf leaves second object and preceding vptr alone");
  var second=ItemSubobject0410Initialization.Apply(s,0x6C8);
  Need(second.Returned&&second.StoreInstructions==41&&second.ReturnedThisToken==ItemToken+0x6C8UL,"second leaf identity is distinct");
  Need(SameAt(s,0x6C8,oracle)&&SameAt(s,0x538,oracle),"second leaf has identical local semantics without altering first");
  Need(s.ReadLittleEndian(0x6B0,8)==1&&s.ReadLittleEndian(0x850,8)==0xA5A5A5A500000000UL,"first178 isQWORD1, second188 is onlyDWORD0");
  Need(s.ReadByte(0x6C4)==0xA5&&s.ReadByte(0x857)==0xA5&&s.ReadByte(0x541)==0xA5,"end padding and inline payload are retained");
  var again=ItemSubobject0410Initialization.Apply(s,0x538);
  Need(again.Returned&&SameAt(s,0x538,oracle),"explicit reapplication affects only the same proved stores");
 }
 static void SharedViewsAndUnknownWidths(){
  var s=Storage();var scalar=s.Scalars;var sub=s.Sub858;
  Need(scalar.ReplicaStack6B0==0xA5A5A5A5U&&sub.Untouched850==0xA5A5A5A5A5A5A5A5UL,"views initially see common bytes");
  ItemSubobject0410Initialization.Apply(s,0x538);ItemSubobject0410Initialization.Apply(s,0x6C8);
  Need(scalar.ReplicaStack6B0==1&&sub.Untouched850==0xA5A5A5A500000000UL,"previously created views observe inline constructor changes");
  s.Store(0x6B0,4,7);s.Store(0x854,4,0x11223344);
  Need(scalar.ReplicaStack6B0==7&&sub.Untouched850==0x1122334400000000UL,"direct writes remain visible without synchronizing snapshots");
  var unknown=new ItemConstructorStorage(ItemToken,new ActorConstructorPrefixState());
  unknown.Store(0x88C,4,3);
  Need(unknown.Scalars.Raw88C==null&&unknown.Scalars.Stack88C==3&&unknown.Scalars.Word890==null,"known DWORD does not require adjacent unknown DWORD");
  unknown.Store(0x890,4,5);unknown.Store(0x874,4,11);unknown.Store(0x898,4,13);
  Need(unknown.Scalars.Raw88C==0x0000000500000003UL&&unknown.Scalars.Gender874==11&&unknown.Scalars.Word898==13,"partial width aliases use exactly requested bytes");
  ItemSubobject0410Initialization.Apply(unknown,0x6C8);
  Need(unknown.ReadLittleEndian(0x850,4)==0&&unknown.Sub858.Untouched850==null,"zero lower DWORD does not fabricate unknown high DWORD");
  bool a=false,b=false;try{ItemScalarInitializationState.CreateSharedView(null);}catch(ArgumentNullException){a=true;}
  try{ItemConstructor858Subobject.CreateSharedView(null);}catch(ArgumentNullException){b=true;}
  Need(a&&b,"shared views require actual own storage");
 }
 sealed class Trace:IItemConstructorFields {
  public readonly ItemConstructorStorage State;public readonly List<int> Offsets=new List<int>();public readonly List<int> Widths=new List<int>();
  public int ThrowOn;public readonly Exception Error=new Exception("own storage failure");
  public Trace(ItemConstructorStorage state){State=state;}
  public ulong? ReadLittleEndian(int offset,int width){return State.ReadLittleEndian(offset,width);}
  public void Store(int offset,int width,ulong value){Offsets.Add(offset);Widths.Add(width);if(Offsets.Count==ThrowOn)throw Error;State.Store(offset,width,value);}
 }
 static void SharedWriteOrderAndOwnException(){
  var state=Storage();var trace=new Trace(state);
  var r=ItemConstructor858Prefix.Apply(ItemConstructor858Subobject.CreateSharedView(trace),ItemScalarInitializationState.CreateSharedView(trace));
  int[] expected={0x858,0x860,0x868,0x858,0x860,0x868,0x870,0x878,0x880,0x88C,0x894,0x89C};bool order=trace.Offsets.Count==12;
  for(int i=0;i<expected.Length&&order;i++)order=trace.Offsets[i]==expected[i]&&trace.Widths[i]==(i==11?4:8);
  Need(order&&r.HelperStoreInstructions==6&&r.ScalarStoreInstructions==6,"two fixed triplets precede exact scalar widths");
  Need(state.Sub858.Raw858==0&&state.Sub858.Raw868==0&&state.Scalars.Raw88C==1&&state.Scalars.Word890==0,"shared helper and scalar views agree with writes");
  Need(state.Sub858.Untouched850==0xA5A5A5A5A5A5A5A5UL&&state.Scalars.Untouched888==0xA5A5A5A5U,"this local segment preserves its neighbors");
  var failingState=Storage();var failing=new Trace(failingState){ThrowOn=4};bool caught=false;
  try{ItemConstructor858Prefix.Apply(ItemConstructor858Subobject.CreateSharedView(failing),ItemScalarInitializationState.CreateSharedView(failing));}catch(Exception e){caught=Object.ReferenceEquals(e,failing.Error);}
  Need(caught&&failing.Offsets.Count==4,"own backing-store exception propagates without invented native EH");
  Need(failingState.Sub858.Raw858==0&&failingState.Sub858.Raw860==0&&failingState.Sub858.Raw868==0&&failingState.Scalars.Raw870==0xA5A5A5A5A5A5A5A5UL,"already written triplet survives; later scalar work is absent");
 }
 static void CompleteConditionalIntegration(){
  var state=Storage();var result=Run(state);
  Need(result.Status=="ConditionalItemPrefix"&&result.ReachedItem323743&&result.ResumeInstructionRva==0x323743&&!result.IsCompleteItemConstructor,"chain stops at next Item segment, not a full constructor return");
  Need(result.ActorCompletion.NormalReturnReached&&Object.ReferenceEquals(result.ActorCompletion.State,state.Actor)&&Object.ReferenceEquals(result.State,state),"Actor result and Item keep identical mutable state");
  Need(result.ItemVtableStores==4&&state.Actor.ReadLittleEndian(0,8)==0x18076A4C0UL&&state.Actor.ReadLittleEndian(0x30,8)==0x1806D5B18UL&&state.Actor.ReadLittleEndian(0x38,8)==0x18079F408UL&&state.ReadLittleEndian(0x530,8)==0x180771F40UL,"Item overwrites all four vptr tokens in real order on shared storage");
  Need(result.FirstSubobject.Returned&&result.SecondSubobject.Returned&&result.FirstSubobject.StoreInstructions==41&&result.SecondSubobject.StoreInstructions==41,"both direct constructors completed after Actor");
  Need(result.Item858.HelperCompleted&&result.Item858.HelperStoreInstructions==6&&result.Item858.ScalarStoreInstructions==6,"existing858 andscalar modules are actually reused");
  Need(Object.ReferenceEquals(result.Item858.Subobject,state.Sub858)&&Object.ReferenceEquals(result.Item858.Scalars,state.Scalars),"reused modules hold live views of the same Item");
  Need(state.ReadLittleEndian(0x6B0,8)==1&&state.ReadLittleEndian(0x88C,8)==1&&state.Scalars.ReplicaStack6B0==1,"different stack fields are initialized independently");
  Need(state.ReadByte(0x4E1)==1&&state.Sub858.Untouched850==0xA5A5A5A500000000UL&&state.Scalars.Untouched888==0xA5A5A5A5U,"Actor flag and preserved neighbor widths survive bridge");
  Need(result.ActorCompletion.MatrixInitializationReturns==1&&result.ActorCompletion.MatrixWarmReturns==1&&result.ActorCompletion.GlobalStateSourceReads==1,"own matrix cold/warm and Engine live source participate in actual integration");
  state.Store(0x30,8,0xABCDEF);Need(result.ActorCompletion.State.ReadLittleEndian(0x30,8)==0xABCDEFUL,"later Item writes are visible through earlier Actor reference");
 }
 static void CallbackVisibility(){
  var state=Storage();var slot=Slot();slot.Context.Current668=1;var matrix=new Matrix();bool beforeItem=false;
  matrix.Hook=delegate(int n){if(n==2){beforeItem=state.Actor.ReadLittleEndian(0,8)==0x18033A0D8UL&&state.ReadLittleEndian(0x530,8)==0xA5A5A5A5A5A5A5A5UL;state.Store(0x888,4,0x11223344);slot.Context.Current668=slot.Context.Server678;}};
  var result=new ItemConstructorBridgeInvocation(state).RunWithArchivedActorProfile(ArrayStorage(),Sub(),Sentinel(),Data(),matrix,slot);
  Need(beforeItem&&result.ReachedItem323743,"matrix callback runs before Item vptr publication");
  Need(state.ReadByte(0x4E1)==1&&state.Scalars.Untouched888==0x11223344U,"late global sampling and future Item view retain callback changes");
  Need(matrix.Count==2&&result.ActorCompletion.GlobalStateSourceReads==1,"no extra callbacks introduced for leaf initialization");
 }
 static void ActorFailurePrefixes(){
  var arrayMissing=Storage();var r0=new ItemConstructorBridgeInvocation(arrayMissing).RunWithArchivedActorProfile(null,Sub(),Sentinel(),Data(),new Matrix(),Slot());
  Need(!r0.ReachedItem323743&&r0.ItemVtableStores==0&&r0.ActorArray!=null&&r0.ActorContinuation==null&&Filled(arrayMissing,0x530,0x370),"array stop cannot start Item tail");
  var allocMissing=Storage();var r1=new ItemConstructorBridgeInvocation(allocMissing).RunWithArchivedActorProfile(ArrayStorage(),null,Sentinel(),Data(),new Matrix(),Slot());
  Need(!r1.ReachedItem323743&&r1.ItemVtableStores==0&&r1.ActorContinuation!=null&&r1.ActorCompletion==null&&Filled(allocMissing,0x530,0x370),"later Actor allocation stop keeps its prefix but no Item writes");
  var sentinelMissing=Storage();var r2=new ItemConstructorBridgeInvocation(sentinelMissing).RunWithArchivedActorProfile(ArrayStorage(),Sub(),null,Data(),new Matrix(),Slot());
  Need(!r2.ReachedItem323743&&r2.ActorCompletion!=null&&!r2.ActorCompletion.NormalReturnReached&&r2.FirstSubobject==null&&Filled(sentinelMissing,0x530,0x370),"Actor subobject stop is preserved");
  var matrixMissing=Storage();var r3=new ItemConstructorBridgeInvocation(matrixMissing).RunWithArchivedActorProfile(ArrayStorage(),Sub(),Sentinel(),Data(),new Matrix{Fail=true},Slot());
  Need(r3.Reason=="OwnMatrixStop"&&r3.ResumeInstructionRva==0x273AEA&&r3.ItemVtableStores==0,"matrix failure reports its real dependency boundary");
  var globalMissing=Storage();var r4=new ItemConstructorBridgeInvocation(globalMissing).RunWithArchivedActorProfile(ArrayStorage(),Sub(),Sentinel(),Data(),new Matrix(),new ActorEngineSlot(null));
  Need(r4.Reason=="MissingGlobalSlotObservation"&&r4.ActorCompletion.ActorObjectPublished&&r4.ItemVtableStores==0&&Filled(globalMissing,0x530,0x370),"late Actor failure leaves already published Actor subobject without Item vptrs");
 }
 static void OwnProjectionCoverageStops(){
  var noTail=StorageTo(0x530);var a=Run(noTail);
  Need(a.ItemVtableStores==3&&a.ResumeInstructionRva==0x3236E1&&a.FirstSubobject==null,"missing fourth vptr projection preserves three earlier writes");
  Need(noTail.Actor.ReadLittleEndian(0,8)==0x18076A4C0UL&&a.ActorCompletion.NormalReturnReached,"own missing projection is distinct from Actor failure");
  var firstMissing=StorageTo(0x538);var b=Run(firstMissing);
  Need(b.ItemVtableStores==4&&b.ResumeInstructionRva==0x3236EF&&!b.ReachedItem323743,"first leaf needs full proven write extent");
  var secondMissing=StorageTo(0x6C4);var c=Run(secondMissing);
  Need(c.FirstSubobject.Returned&&c.ResumeInstructionRva==0x3236FC&&!c.ReachedItem323743&&secondMissing.ReadLittleEndian(0x6B0,8)==1,"second missing projection retains completed first leaf");
  var at858=StorageTo(0x854);var d=Run(at858);
  Need(d.FirstSubobject.Returned&&d.SecondSubobject.Returned&&d.Item858!=null&&!d.Item858.HelperCompleted&&d.ResumeInstructionRva==0x323702,"both leaves survive missing858 projection");
  var noScalar=StorageTo(0x870);var e=Run(noScalar);
  Need(e.Item858.HelperCompleted&&e.Item858.HelperStoreInstructions==6&&e.Item858.ScalarStoreInstructions==0&&e.ResumeInstructionRva==0x323714,"858 writes survive missing scalar projection");
  Need(noScalar.Sub858.Raw858==0&&noScalar.Sub858.Raw860==0&&noScalar.Sub858.Raw868==0,"missing later projection never rolls back earlier triplets");
 }
 static void PriorIdentityAndReplay(){
  var state=Storage();var other=Storage();var wrong=ActorFor(other,ItemToken);var rejected=new ItemConstructorBridgeInvocation(state).ContinueAfterActor(wrong);
  Need(!rejected.ReachedItem323743&&rejected.ItemVtableStores==0&&Filled(state,0,0x8A0),"cannot copy another Actor result into this Item");
  var tokenWrong=Storage();var returnedWrong=ActorFor(tokenWrong,ItemToken+0x10000UL);var guard=new ItemConstructorBridgeInvocation(tokenWrong).ContinueAfterActor(returnedWrong);
  Need(guard.ItemVtableStores==0&&!guard.ReachedItem323743&&Filled(tokenWrong,0x530,0x370),"own input identity inconsistency rejected before tail; native caller itself ignores RAX");
  var s=Storage();var actor=ActorFor(s,ItemToken);var invocation=new ItemConstructorBridgeInvocation(s);var ok=invocation.ContinueAfterActor(actor);
  Need(ok.ReachedItem323743&&Object.ReferenceEquals(ok.ActorCompletion,actor),"explicit completed Actor continuation uses the same report and storage");
  s.Store(0x530,8,42);bool twice=false;try{invocation.ContinueAfterActor(actor);}catch(InvalidOperationException){twice=true;}
  Need(twice&&invocation.HasRun&&s.ReadLittleEndian(0x530,8)==42,"one invocation cannot silently replay previous construction");
  var absent=new ItemConstructorBridgeInvocation(Storage()).ContinueAfterActor(null);
  Need(!absent.ReachedItem323743&&absent.ItemVtableStores==0,"missing Actor report cannot authorize Item continuation");
 }
 static void StorageIdentityAndIsolation(){
  var actor=new ActorConstructorPrefixState();var tail=Fill(0x370);var s=new ItemConstructorStorage(ItemToken,actor,tail);tail[0]=7;
  Need(Object.ReferenceEquals(s.Actor,actor)&&s.ReadByte(0x530)==0xA5,"initial tail is copied once; Actor identity is shared");
  s.Store(0x52C,8,0x1122334455667788UL);
  Need(actor.ReadLittleEndian(0x52C,4)==0x55667788UL&&s.ReadLittleEndian(0x530,4)==0x11223344UL&&s.ReadLittleEndian(0x52C,8)==0x1122334455667788UL,"cross-boundary reads route both pieces without duplicating Actor");
  bool zero=false,overflow=false,shortWrite=false;
  try{new ItemConstructorStorage(0,actor);}catch(ArgumentOutOfRangeException){zero=true;}
  try{new ItemConstructorStorage(ulong.MaxValue-0x89EUL,actor);}catch(ArgumentOutOfRangeException){overflow=true;}
  var shortState=StorageTo(0x538);try{shortState.Store(0x538,1,0);}catch(ArgumentOutOfRangeException){shortWrite=true;}
  Need(zero&&overflow&&shortWrite,"invalid own identity and out-of-projection stores rejected");
  var edge=new ItemConstructorStorage(ulong.MaxValue-0x89FUL,actor);
  Need(edge.Token+0x89FUL==ulong.MaxValue&&edge.HasRange(0x89C,4)&&!shortState.HasRange(0x538,1),"extent checks use actual captured storage");
  var untouched=Storage();ItemSubobject0410Initialization.Apply(s,0x538);
  Need(Filled(untouched,0,0x8A0),"separate own Items do not alias accidentally");
 }
 public static int Main(){
  Group("LeafWritesAndPadding",LeafWritesAndPadding);
  Group("SharedViewsAndUnknownWidths",SharedViewsAndUnknownWidths);
  Group("SharedWriteOrderAndOwnException",SharedWriteOrderAndOwnException);
  Group("CompleteConditionalIntegration",CompleteConditionalIntegration);
  Group("CallbackVisibility",CallbackVisibility);
  Group("ActorFailurePrefixes",ActorFailurePrefixes);
  Group("OwnProjectionCoverageStops",OwnProjectionCoverageStops);
  Group("PriorIdentityAndReplay",PriorIdentityAndReplay);
  Group("StorageIdentityAndIsolation",StorageIdentityAndIsolation);
  Console.WriteLine("ItemConstructorBridgeTests: "+assertions+" assertions, "+groups+" groups passed; own conditional prefix only");return 0;
 }
}
