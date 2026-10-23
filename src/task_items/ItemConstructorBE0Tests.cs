using System;
using GrimDynasty.TaskItems;

// Own conditional caller integration, not native allocation/guard/EH execution.
public static class ItemConstructorBE0Tests {
 static int assertions,groups;
 const ulong Token=0x500000UL,ArrayToken=0x700000UL;
 static void Need(bool ok,string why){assertions++;if(!ok)throw new Exception(why);}
 static void Group(string name,Action body){int before=assertions;body();groups++;Console.WriteLine("PASS "+name+": "+(assertions-before)+" assertions");}
 static byte?[] Fill(int n){var a=new byte?[n];for(int i=0;i<n;i++)a[i]=0xA5;return a;}
 sealed class PriorChecks:IItem950CheckSource {
  public Item950CheckObservation Check(ItemConstructorStorage s,int index,ulong target){
   if(index<0||index>=10||target!=0x180016710UL)throw new Exception("Wrong own950 prerequisite");
   return new Item950CheckObservation(true);
  }
 }
 sealed class Checks:IItemBE0CheckSource {
  public int Calls;
  public Func<ItemConstructorStorage,ItemBE0ArrayStorage,int,ulong,ItemBE0CheckObservation> Action;
  public ItemBE0CheckObservation Check(ItemConstructorStorage s,ItemBE0ArrayStorage a,int index,ulong target){Calls++;return Action==null?new ItemBE0CheckObservation(true):Action(s,a,index,target);}
 }
 sealed class Fixture {
  public readonly ItemConstructorStorage State;
  public readonly ItemConstructorBridgeResult Bridge;
  public readonly ItemConstructor950Result Prior950;
  public readonly ItemConstructorBA8Result Prior;
  public Fixture(int end=0xC65,bool unknown=false,ulong token=Token){
   var actor=new ActorConstructorPrefixState(unknown?new byte?[0x530]:Fill(0x530));
   State=ItemConstructorStorage.CreateForBE0Continuation(token,actor,unknown?new byte?[end-0x530]:Fill(end-0x530));
   var matrix=new ActorMatrixInitializationSource(ActorMatrixInitializationState.CreateArchivedInitialState(),ActorMatrixThreadState.CreateArchivedTemplateThread(),true,true);
   Bridge=new ItemConstructorBridgeInvocation(State).RunWithArchivedActorProfile(
    new Actor198ArrayStorage(0x100000UL,Fill(0x208)),new ActorConstructorStorage(0x200000UL,Fill(0x358)),new ActorConstructorStorage(0x300000UL,Fill(0x38)),
    ActorCompletionObservations.WithArchivedConstants(null,null,null),matrix,new ActorEngineSlot(new ActorEngineContext(0x600000UL,9,9,7)));
   Prior950=new ItemConstructor950Invocation(Bridge).Run(new PriorChecks());
   Prior=new ItemConstructorBA8Invocation(Prior950).RunWithArchivedWorldVec3Body();
   if(!Prior.ReachedItem3238BE)throw new Exception("Real own Actor/Bridge/950/BA8 prerequisite stopped");
  }
 }
 static ItemBE0ArrayStorage Allocation(){return new ItemBE0ArrayStorage(ArrayToken,Fill(0x108));}
 static byte?[] Observe(ItemConstructorStorage s){var a=new byte?[s.ByteCount];for(int i=0;i<a.Length;i++)a[i]=s.ReadByte(i);return a;}
 static bool Same(ItemConstructorStorage s,byte?[] a){if(s.ByteCount!=a.Length)return false;for(int i=0;i<a.Length;i++)if(s.ReadByte(i)!=a[i])return false;return true;}
 static bool Same(ItemBE0ArrayStorage s,byte?[] a){if(a.Length!=0x108)return false;for(int i=0;i<a.Length;i++)if(s.ReadByte(i)!=a[i])return false;return true;}
 static void Put(byte?[] a,int at,int width,ulong value){for(int i=0;i<width;i++)a[at+i]=unchecked((byte)(value>>(8*i)));}
 static byte?[] ItemOracle(byte?[] before,int headers=5,bool published=false,int suffix=0,ulong arrayToken=ArrayToken,bool prefix=true){
  var a=(byte?[])before.Clone();if(prefix)Put(a,0xBD8,4,0xFFFFFFFFUL);
  if(headers>=1)Put(a,0xBE0,8,0x180339B70UL);
  if(headers>=2)Put(a,0xBE8,4,0);
  if(headers>=3)Put(a,0xBF0,8,0);
  if(headers>=4)Put(a,0xBF8,4,2);
  if(headers>=5)Put(a,0xBFC,1,0);
  if(published)Put(a,0xBF0,8,arrayToken+8);
  if(suffix>=1)Put(a,0xBE0,8,0x180770AD8UL);
  if(suffix>=2)Put(a,0xC00,1,0);
  if(suffix>=3)Put(a,0xC02,1,0);
  if(suffix>=4)Put(a,0xC04,2,0);
  if(suffix>=5)Put(a,0xC06,1,0);
  if(suffix>=6)Put(a,0xC08,4,0xFFFFFFFFUL);
  if(suffix>=7)Put(a,0xC28,8,15);
  if(suffix>=8)Put(a,0xC20,8,0);
  if(suffix>=9)Put(a,0xC10,1,0);
  for(int i=0;i<6;i++)if(suffix>=10+i)Put(a,0xC30+8*i,8,0);
  if(suffix>=16)Put(a,0xC60,4,0);
  if(suffix>=17)Put(a,0xC64,1,0);return a;
 }
 static void LeafOracle(byte?[] a,int index){
  // Independent width/value sets. Payload interiors and +64..67 stay untouched.
  int at=8+0x80*index;
  foreach(int p in new int[]{0x10,0x30,0x40,0x48,0x50,0x58,0x68,0x70,0x78})Put(a,at+p,8,0);
  foreach(int p in new int[]{0x18,0x38})Put(a,at+p,8,15);
  Put(a,at,1,0);Put(a,at+0x20,1,0);Put(a,at+0x60,4,0xFFFFFFFEUL);
 }
 static byte?[] ArrayOracle(int completed){var a=new byte?[0x108];for(int i=0;i<a.Length;i++)a[i]=0;Put(a,0,8,2);for(int i=0;i<completed;i++)LeafOracle(a,i);return a;}
 static void CaptureAndAllocationBounds(){
  var actor=new ActorConstructorPrefixState();var input=Fill(0x735);var s=ItemConstructorStorage.CreateForBE0Continuation(Token,actor,input);input[0]=7;
  Need(s.ByteCount==0xC65&&ItemConstructorStorage.ContinuationBE0Extent==0xC65&&ItemConstructorStorage.MaximumBE0TailBytes==0x735&&Object.ReferenceEquals(s.Actor,actor)&&s.ReadByte(0x530)==0xA5,"new capture retains Actor identity and isolates input tail");
  Need(new ItemConstructorStorage(Token,actor).ByteCount==0x8A0&&ItemConstructorStorage.CreateFor950Continuation(Token,actor).ByteCount==0xB18&&ItemConstructorStorage.CreateForBA8Continuation(Token,actor).ByteCount==0xBC8,"all previous capture defaults remain unchanged");
  bool high=false,longTail=false,zero=false,oldRejects=false;
  try{ItemConstructorStorage.CreateForBE0Continuation(ulong.MaxValue-0xC63UL,actor);}catch(ArgumentOutOfRangeException){high=true;}
  try{ItemConstructorStorage.CreateForBE0Continuation(Token,actor,new byte?[0x736]);}catch(ArgumentException){longTail=true;}
  try{ItemConstructorStorage.CreateForBE0Continuation(0,actor);}catch(ArgumentOutOfRangeException){zero=true;}
  try{ItemConstructorStorage.CreateForBA8Continuation(Token,actor,new byte?[0x735]);}catch(ArgumentException){oldRejects=true;}
  Need(high&&longTail&&zero&&oldRejects,"extended capture is explicit and retains exact token/tail checks");
  var bytes=Fill(0x108);var a=new ItemBE0ArrayStorage(ArrayToken,bytes);bytes[0]=1;var copy=a.CopyObservation();copy[1]=2;
  Need(ItemBE0ArrayStorage.RequestedBytes==0x108&&ItemBE0ArrayStorage.ElementCount==2&&ItemBE0ArrayStorage.ElementStride==0x80&&a.BaseToken==ArrayToken&&a.ElementsToken==ArrayToken+8&&a.ReadByte(0)==0xA5&&a.ReadByte(1)==0xA5,"allocation identity,264-byte extent and both clone boundaries are exact");
  bool nullBytes=false,shortBytes=false,longBytes=false,zeroArray=false,highArray=false,badStore=false;
  try{new ItemBE0ArrayStorage(ArrayToken,null);}catch(ArgumentNullException){nullBytes=true;}
  try{new ItemBE0ArrayStorage(ArrayToken,new byte?[0x107]);}catch(ArgumentException){shortBytes=true;}
  try{new ItemBE0ArrayStorage(ArrayToken,new byte?[0x109]);}catch(ArgumentException){longBytes=true;}
  try{new ItemBE0ArrayStorage(0);}catch(ArgumentOutOfRangeException){zeroArray=true;}
  try{new ItemBE0ArrayStorage(ulong.MaxValue-0x106UL);}catch(ArgumentOutOfRangeException){highArray=true;}
  try{a.Store(0x101,8,0);}catch(ArgumentOutOfRangeException){badStore=true;}
  Need(nullBytes&&shortBytes&&longBytes&&zeroArray&&highArray&&badStore&&Same(a,Fill(0x108)),"invalid allocation/range observations cannot partially write storage");
  var edge=new Fixture(0xC65,false,ulong.MaxValue-0xC64UL);var ordinaryArray=Allocation();var r=new ItemConstructorBE0Invocation(edge.Prior,ordinaryArray).Run(new Checks());
  var ordinaryItem=new Fixture();var edgeArray=new ItemBE0ArrayStorage(ulong.MaxValue-0x107UL);var arrayResult=new ItemConstructorBE0Invocation(ordinaryItem.Prior,edgeArray).Run(new Checks());
  Need(r.ReachedItem323968&&edge.State.Token+0xC64UL==ulong.MaxValue&&r.ReturnedSubobjectToken==edge.State.Token+0xBE0UL&&edge.State.ReadLittleEndian(0xBF0,8)==ordinaryArray.ElementsToken&&arrayResult.ReachedItem323968&&ordinaryItem.State.ReadLittleEndian(0xBF0,8)==edgeArray.ElementsToken&&edgeArray.BaseToken+0x107UL==ulong.MaxValue,"separate non-overlapping fixtures test maximum Item and allocation tokens and exact publication identities");
 }
 static void MissingAllocationAndDefaultCheck(){
  var f=new Fixture();var before=Observe(f.State);var absent=new ItemConstructorBE0Invocation(f.Prior,null).Run(new Checks());
  Need(absent.Status=="Unsupported"&&absent.Reason=="MissingKnownInitialBE0Allocation"&&absent.ResumeInstructionRva==0xEBE41&&absent.CallSiteRva==0x3238DA&&absent.Stage=="Allocation","missing allocation stops at the true allocator dependency");
  Need(absent.CallerPrefixStores==1&&absent.HeaderStores==5&&absent.ItemStoreInstructions==6&&absent.ArrayStoreInstructions==0&&absent.BulkZeroBytes==0&&!absent.KnownInitialAllocationUsed&&absent.RequestedAllocationBytes==264UL&&absent.CheckSourceCalls==0&&!absent.ArrayPublished&&Same(f.State,ItemOracle(before)),"only caller/header prefix survives without inventing allocation or its null-return branch");
  var second=new Fixture();var array=Allocation();var before2=Observe(second.State);var stopped=new ItemConstructorBE0Invocation(second.Prior,array).Run();
  Need(stopped.Reason=="UnresolvedItemBE0PreElementCheck"&&stopped.Stage=="PreElementCheck"&&stopped.ResumeInstructionRva==0x2DCC75&&stopped.CallSiteRva==0x3238DA&&stopped.PendingElementIndex==0,"default does not assume the loaded indirect check returns");
  Need(stopped.KnownInitialAllocationUsed&&stopped.BulkZeroBytes==264&&stopped.ArrayStoreInstructions==1&&stopped.TotalStoreInstructions==7&&stopped.CheckSourceCalls==0&&stopped.NormalCheckReturns==0&&stopped.ConstructedElements==0,"memset and cookie precede the unknown first check");
  Need(Same(array,ArrayOracle(0))&&Same(second.State,ItemOracle(before2))&&!stopped.ArrayPublished&&!stopped.WrapperReturned&&stopped.ReturnedSubobjectToken==null&&stopped.CallerSuffixStores==0,"default-stop byte oracles prove no leaf, publication or caller suffix occurred");
 }
 static void CompleteIndependentOracles(){
  var f=new Fixture();var array=Allocation();var before=Observe(f.State);var checks=new Checks();var r=new ItemConstructorBE0Invocation(f.Prior,array).Run(checks);
  Need(r.Status=="ConditionalItemPrefix"&&r.Stage=="Item323968"&&r.Reason==null&&r.ResumeInstructionRva==0x323968&&r.CallSiteRva==null&&r.ReachedItem323968&&!r.IsCompleteItemConstructor,"normal own prefix stops before the next unknown Item call");
  Need(r.CallerPrefixStores==1&&r.HeaderStores==5&&r.PublishedPointerStores==1&&r.CallerSuffixStores==17&&r.ItemStoreInstructions==24&&r.ArrayStoreInstructions==29&&r.TotalStoreInstructions==53&&r.BulkZeroBytes==264,"bulk zero is bytes, distinct from24 Item and29 array store instructions");
  Need(r.ConstructedElements==2&&r.CheckSourceCalls==2&&r.NormalCheckReturns==2&&checks.Calls==2&&r.PendingElementIndex==null&&r.ArrayPublished&&r.WrapperReturned&&r.ReturnedSubobjectToken==Token+0xBE0UL,"two normally returned checks/leaves precede pointer publication and wrapper return");
  Need(Object.ReferenceEquals(r.Prior,f.Prior)&&Object.ReferenceEquals(r.State,f.State)&&Object.ReferenceEquals(r.Array,array)&&Object.ReferenceEquals(r.State.Actor,f.Bridge.ActorCompletion.State),"real Actor through BA8 and BE0 preserve the same Item, Actor and allocation objects");
  Need(Same(f.State,ItemOracle(before,5,true,17))&&Same(array,ArrayOracle(2)),"entire Item and264-byte allocation match independent field/width oracles");
  Need(f.State.ReadLittleEndian(0xBE0,8)==0x180770AD8UL&&f.State.ReadLittleEndian(0xBF0,8)==ArrayToken+8&&f.State.ReadLittleEndian(0xBF8,4)==2&&f.State.ReadLittleEndian(0xBD8,4)==0xFFFFFFFFUL&&f.State.ReadLittleEndian(0xC08,4)==0xFFFFFFFFUL,"derived vptr, published elements pointer and both narrow minus-one values are distinct");
 }
 static void UnknownPaddingAndLiveViews(){
  var f=new Fixture(0xC65,true);var before=Observe(f.State);var array=new ItemBE0ArrayStorage(ArrayToken);var r=new ItemConstructorBE0Invocation(f.Prior,array).Run(new Checks());
  Need(r.ReachedItem323968&&Same(f.State,ItemOracle(before,5,true,17))&&Same(array,ArrayOracle(2)),"unknown Item padding is preserved while successful memset makes the whole allocation known");
  bool gaps=true;foreach(int p in new int[]{0xBC8,0xBD7,0xBDC,0xBDF,0xBEC,0xBEF,0xBFD,0xBFF,0xC01,0xC03,0xC07,0xC0C,0xC0F,0xC11,0xC1F})gaps=gaps&&!f.State.ReadByte(p).HasValue;
  Need(gaps&&f.State.ReadLittleEndian(0xBD8,8)==null&&f.State.ReadLittleEndian(0xC08,8)==null,"DWORD minus-one and narrow suffix writes do not fill adjacent unknown bytes");
  f.State.Store(0x52C,4,81);f.State.Store(0x6B0,4,7);f.State.Store(0x850,8,9);
  Need(f.State.Actor.ReadLittleEndian(0x52C,4)==81&&f.State.Scalars.ReplicaStack6B0==7&&f.State.Sub858.Untouched850==9,"old views still refer to the same larger Item storage after completion");
 }
 static void EveryCheckSeesLiveState(){
  var f=new Fixture();var array=Allocation();var expected=ArrayOracle(0);var before=Observe(f.State);var checks=new Checks();bool visible=true;
  checks.Action=delegate(ItemConstructorStorage s,ItemBE0ArrayStorage a,int index,ulong target){
   visible=visible&&index==checks.Calls-1&&target==0x1800EBEB0UL&&Object.ReferenceEquals(s,f.State)&&Object.ReferenceEquals(a,array)&&Same(a,expected);
   visible=visible&&s.ReadLittleEndian(0xBF8,4)==(index==0?2UL:99UL)&&s.ReadLittleEndian(0xBF0,8)==(index==0?0UL:0x1234UL)&&s.ReadLittleEndian(0xBE0,8)==0x180339B70UL;
   int at=8+0x80*index;for(int j=0;j<0x80;j++){a.Store(at+j,1,0xA5);expected[at+j]=0xA5;}
   if(index==0){a.Store(0,8,17);Put(expected,0,8,17);s.Store(0xBF8,4,99);s.Store(0x6B0,4,13);s.Store(0xC28,8,0xA5);}
   s.Store(0xBF0,8,index==0?0x1234UL:0x5678UL);LeafOracle(expected,index);return new ItemBE0CheckObservation(true);
  };
  var r=new ItemConstructorBE0Invocation(f.Prior,array).Run(checks);var expectedItem=ItemOracle(before,5,true,17);Put(expectedItem,0xBF8,4,99);Put(expectedItem,0x6B0,4,13);
  Need(visible&&r.ReachedItem323968&&checks.Calls==2&&r.ConstructedElements==2,"each callback sees current state; changing BF8 or cookie does not change the saved two-element loop");
  Need(Same(array,expected)&&array.ReadLittleEndian(0,8)==17,"leaf stores overwrite their exact widths after each callback and never restore the modified cookie");
  Need(array.ReadByte(9)==0xA5&&array.ReadByte(8+0x64)==0xA5&&array.ReadByte(0x88+0x64)==0xA5&&array.ReadLittleEndian(8+0x60,4)==0xFFFFFFFEUL,"callback-filled leaf padding survives while all14 native field writes are tested against nonzero bytes");
  Need(Same(f.State,expectedItem)&&f.State.Scalars.ReplicaStack6B0==13&&f.State.ReadLittleEndian(0xBF0,8)==array.ElementsToken,"final publication overwrites callback BF0, suffix overwrites C28, and unrelated live changes persist");
  Need(r.ArrayStoreInstructions==29&&r.ItemStoreInstructions==24&&r.TotalStoreInstructions==53,"own callback writes are excluded from archived instruction counts");
 }
 static void FalseAndNullChecksKeepPrefixes(){
  for(int stop=0;stop<2;stop++)for(int kind=0;kind<2;kind++){
   int failed=stop,mode=kind;var f=new Fixture();var before=Observe(f.State);var array=Allocation();var checks=new Checks();
   checks.Action=delegate(ItemConstructorStorage s,ItemBE0ArrayStorage a,int index,ulong target){if(index!=failed)return new ItemBE0CheckObservation(true);s.Store(0xBF0,8,0x55);a.Store(8+0x80*index+1,1,0x77);return mode==0?new ItemBE0CheckObservation(false,"OwnStop"):null;};
   var r=new ItemConstructorBE0Invocation(f.Prior,array).Run(checks);var expected=ArrayOracle(stop);Put(expected,8+0x80*stop+1,1,0x77);var expectedItem=ItemOracle(before);Put(expectedItem,0xBF0,8,0x55);
   Need(r.Status=="Unsupported"&&r.Reason==(kind==0?"OwnStop":"MissingItemBE0CheckObservation")&&r.ResumeInstructionRva==0x2DCC75&&r.PendingElementIndex==stop&&r.CheckSourceCalls==stop+1&&r.NormalCheckReturns==stop&&r.ConstructedElements==stop&&r.ArrayStoreInstructions==1+14*stop&&!r.ArrayPublished&&!r.WrapperReturned&&r.CallerSuffixStores==0,"false/null check preserves exact stopped iteration "+stop+" mode"+kind);
   Need(Same(array,expected)&&Same(f.State,expectedItem)&&r.ReturnedSubobjectToken==null&&r.PublishedPointerStores==0,"no cleanup, future leaf or publication erases callback effects at stopped iteration "+stop+" mode"+kind);
  }
  var fallback=new Fixture();var source=new Checks();source.Action=delegate{return new ItemBE0CheckObservation(false);};
  Need(new ItemConstructorBE0Invocation(fallback.Prior,Allocation()).Run(source).Reason=="UnresolvedItemBE0PreElementCheck","false without a reason remains explicitly unresolved");
 }
 static void ThrowsAndSingleInvocation(){
  for(int stop=0;stop<2;stop++){
   int failed=stop;var f=new Fixture();var array=Allocation();var checks=new Checks();var error=new Exception("own check error");var before=Observe(f.State);
   checks.Action=delegate(ItemConstructorStorage s,ItemBE0ArrayStorage a,int index,ulong target){if(index==failed){s.Store(0xBF0,8,0x66);a.Store(8+0x80*index+1,1,0x88);throw error;}return new ItemBE0CheckObservation(true);};
   var invocation=new ItemConstructorBE0Invocation(f.Prior,array);bool caught=false;try{invocation.Run(checks);}catch(Exception e){caught=Object.ReferenceEquals(e,error);}
   var r=invocation.Result;var expected=ArrayOracle(stop);Put(expected,8+0x80*stop+1,1,0x88);var expectedItem=ItemOracle(before);Put(expectedItem,0xBF0,8,0x66);
   Need(caught&&invocation.HasRun&&r.Reason=="ItemBE0CheckNotCompleted"&&r.PendingElementIndex==stop&&r.CheckSourceCalls==stop+1&&r.NormalCheckReturns==stop&&r.ConstructedElements==stop&&Same(array,expected)&&Same(f.State,expectedItem)&&!r.ArrayPublished&&!r.WrapperReturned,"own exception propagates with exact saved prefix and no native EH invention at"+stop);
   bool again=false;try{invocation.Run(new Checks());}catch(InvalidOperationException){again=true;}Need(again&&Object.ReferenceEquals(r,invocation.Result)&&r.CheckSourceCalls==stop+1,"interrupted own invocation cannot be silently replayed");
  }
  var good=new Fixture();var storage=Allocation();var own=new ItemConstructorBE0Invocation(good.Prior,storage);var nested=new Checks();bool rejected=false;
  nested.Action=delegate(ItemConstructorStorage s,ItemBE0ArrayStorage a,int index,ulong target){if(index==0){try{own.Run();}catch(InvalidOperationException){rejected=true;}}return new ItemBE0CheckObservation(true);};
  Need(!own.HasRun&&own.Result.CallerPrefixStores==0,"constructing an attempt does not execute the caller");
  var done=own.Run(nested);Need(rejected&&done.ReachedItem323968&&done.CheckSourceCalls==2,"reentrancy is rejected without preventing a normally returning outer own check");
  good.State.Store(0xBF0,8,42);storage.Store(0,8,43);bool repeated=false;try{own.Run(new Checks());}catch(InvalidOperationException){repeated=true;}
  Need(repeated&&good.State.ReadLittleEndian(0xBF0,8)==42&&storage.ReadLittleEndian(0,8)==43&&done.TotalStoreInstructions==53,"successful attempts cannot re-clear or republish on repeat");
 }
 static void NarrowStateStopsBeforeAllocation(){
  int[] ends={0xBC8,0xBDB,0xBE7,0xBEB,0xBF7,0xBFB,0xBFC};int[] headers={0,0,0,1,2,3,4};uint[] sites={0x3238C2,0x3238C2,0xEBE06,0xEBE0B,0xEBE0E,0xEBE12,0xEBE15};
  for(int i=0;i<ends.Length;i++){
   var f=new Fixture(ends[i]);var before=Observe(f.State);var array=Allocation();var checks=new Checks();var r=new ItemConstructorBE0Invocation(f.Prior,array).Run(checks);bool prefix=i>=2;
   Need(r.Status=="Unsupported"&&r.Reason=="MissingItemBE0FieldStorage"&&r.ResumeInstructionRva==sites[i]&&r.CallerPrefixStores==(prefix?1:0)&&r.HeaderStores==headers[i]&&r.ArrayStoreInstructions==0&&r.BulkZeroBytes==0&&r.CheckSourceCalls==0&&!r.KnownInitialAllocationUsed&&Same(array,Fill(0x108))&&Same(f.State,ItemOracle(before,headers[i],false,0,ArrayToken,prefix)),"short caller/header capture is atomic and never consumes allocation at"+sites[i].ToString("X"));
  }
 }
 static void SuffixOrderAndStops(){
  int[] ends={0xC00,0xC02,0xC05,0xC06,0xC0B,0xC2F,0xC37,0xC3F,0xC47,0xC4F,0xC57,0xC5F,0xC63,0xC64};
  int[] stores={1,2,3,4,5,6,9,10,11,12,13,14,15,16};
  uint[] sites={0x3238EA,0x3238F1,0x3238F8,0x323901,0x323908,0x323915,0x32392E,0x323935,0x32393C,0x323943,0x32394A,0x323951,0x323958,0x32395E};
  for(int i=0;i<ends.Length;i++){
   var f=new Fixture(ends[i]);var before=Observe(f.State);var array=Allocation();var r=new ItemConstructorBE0Invocation(f.Prior,array).Run(new Checks());
   Need(r.Reason=="MissingItemBE0FieldStorage"&&r.Stage=="CallerSuffix"&&r.ResumeInstructionRva==sites[i]&&r.CallSiteRva==null&&r.CallerSuffixStores==stores[i]&&r.WrapperReturned&&r.ReturnedSubobjectToken==Token+0xBE0UL&&r.ArrayPublished&&r.ConstructedElements==2&&r.TotalStoreInstructions==36+stores[i]&&!r.ReachedItem323968&&Same(f.State,ItemOracle(before,5,true,stores[i]))&&Same(array,ArrayOracle(2)),"suffix stop retains returned wrapper and exact out-of-address-order write prefix at"+sites[i].ToString("X"));
  }
  var gap=new Fixture(0xC2F);var prior=Observe(gap.State);var partial=new ItemConstructorBE0Invocation(gap.Prior,Allocation()).Run(new Checks());
  Need(partial.CallerSuffixStores==6&&gap.State.ReadLittleEndian(0xC20,8)==0xA5A5A5A5A5A5A5A5UL&&gap.State.ReadByte(0xC10)==prior[0xC10],"missing high capacity QWORD stops before lower-address length and first-byte writes");
 }
 static void InvalidPrerequisites(){
  var array=Allocation();var noPrior=new ItemConstructorBE0Invocation(null,array).Run(new Checks());
  Need(noPrior.Status=="Unsupported"&&noPrior.Reason=="MissingPriorItemBA8Prefix"&&noPrior.State==null&&noPrior.TotalStoreInstructions==0&&Same(array,Fill(0x108)),"null prior cannot authorize any Item/allocation mutation");
  var f=new Fixture();var pending=new ItemConstructorBA8Invocation(f.Prior950);var before=Observe(f.State);var blocked=new ItemConstructorBE0Invocation(pending.Result,array).Run(new Checks());
  Need(blocked.Reason=="PriorItemBA8PrefixIncomplete"&&blocked.TotalStoreInstructions==0&&Same(f.State,before)&&Same(array,Fill(0x108)),"unrun BA8 cannot be replaced with already initialized-looking bytes");
  var shortState=ItemConstructorStorage.CreateForBE0Continuation(Token,new ActorConstructorPrefixState(),Fill(0xBC7-0x530));
  // Obtain a real stopped BA8 through the same public prerequisites.
  var matrix=new ActorMatrixInitializationSource(ActorMatrixInitializationState.CreateArchivedInitialState(),ActorMatrixThreadState.CreateArchivedTemplateThread(),true,true);
  var bridge=new ItemConstructorBridgeInvocation(shortState).RunWithArchivedActorProfile(new Actor198ArrayStorage(0x100000UL,Fill(0x208)),new ActorConstructorStorage(0x200000UL,Fill(0x358)),new ActorConstructorStorage(0x300000UL,Fill(0x38)),ActorCompletionObservations.WithArchivedConstants(null,null,null),matrix,new ActorEngineSlot(new ActorEngineContext(0x600000UL,9,9,7)));
  var stopped=new ItemConstructorBA8Invocation(new ItemConstructor950Invocation(bridge).Run(new PriorChecks())).RunWithArchivedWorldVec3Body();var snapshot=Observe(shortState);var result=new ItemConstructorBE0Invocation(stopped,array).Run(new Checks());
  Need(!stopped.ReachedItem3238BE&&result.Reason=="PriorItemBA8PrefixIncomplete"&&result.ResumeInstructionRva==0x228AB0&&result.TotalStoreInstructions==0&&Same(shortState,snapshot)&&Same(array,Fill(0x108)),"an actual second-WorldVec3 coverage stop cannot advance into BE0");
 }
 public static int Main(){
  Group("CaptureAndAllocationBounds",CaptureAndAllocationBounds);Group("MissingAllocationAndDefaultCheck",MissingAllocationAndDefaultCheck);
  Group("CompleteIndependentOracles",CompleteIndependentOracles);Group("UnknownPaddingAndLiveViews",UnknownPaddingAndLiveViews);
  Group("EveryCheckSeesLiveState",EveryCheckSeesLiveState);Group("FalseAndNullChecksKeepPrefixes",FalseAndNullChecksKeepPrefixes);
  Group("ThrowsAndSingleInvocation",ThrowsAndSingleInvocation);Group("NarrowStateStopsBeforeAllocation",NarrowStateStopsBeforeAllocation);
  Group("SuffixOrderAndStops",SuffixOrderAndStops);Group("InvalidPrerequisites",InvalidPrerequisites);
  Console.WriteLine("ItemConstructorBE0Tests: "+assertions+" assertions, "+groups+" groups passed; own conditional prefix only");return 0;
 }
}
