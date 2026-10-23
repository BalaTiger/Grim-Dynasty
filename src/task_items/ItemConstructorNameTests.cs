using System;
using GrimDynasty.TaskItems;

// Own bounded observations and explicit normal memcpy contract only.
// No original code, allocator, alias helper, fatal handler or global is executed.
public static class ItemConstructorNameTests {
 static int assertions,groups;
 const ulong Token=0x500000UL,Source=0x900000UL,Destination=0xA00000UL;
 static readonly int[] Offsets={0x540,0x560,0x580,0x5A8,0x5C8,0x5E8,0x610};
 static readonly uint[] Calls={0x32399D,0x3239B3,0x3239C9,0x3239E5,0x3239FB,0x323A11,0x323A2D};
 static void Need(bool ok,string why){assertions++;if(!ok)throw new Exception(why);}
 static void Group(string name,Action test){int before=assertions;test();groups++;Console.WriteLine("PASS "+name+": "+(assertions-before)+" assertions");}
 static byte?[] Fill(int length){var a=new byte?[length];for(int i=0;i<a.Length;i++)a[i]=0xA5;return a;}
 static void Put(byte?[] a,int offset,int width,ulong value){for(int i=0;i<width;i++)a[offset+i]=unchecked((byte)(value>>(8*i)));}
 static ItemConstructorStorage State(byte?[] bytes=null,bool unknownTail=false){return ItemConstructorStorage.CreateForBE0Continuation(Token,new ActorConstructorPrefixState(bytes??Fill(0x530)),unknownTail?new byte?[0x735]:Fill(0x735));}
 static byte?[] Observe(ItemConstructorStorage s){var a=new byte?[s.ByteCount];for(int i=0;i<a.Length;i++)a[i]=s.ReadByte(i);return a;}
 static bool Same(ItemConstructorStorage s,byte?[] expected){if(s.ByteCount!=expected.Length)return false;for(int i=0;i<expected.Length;i++)if(s.ReadByte(i)!=expected[i])return false;return true;}
 static bool SameArray(ItemBE0ArrayStorage s,byte?[] expected){for(int i=0;i<expected.Length;i++)if(s.ReadByte(i)!=expected[i])return false;return true;}
 static void Inline(ItemConstructorStorage s,int offset,ulong oldLength=0){s.Store(offset+0x10,8,oldLength);s.Store(offset+0x18,8,15);}
 static void Name(ItemConstructorStorage s,params byte[] data){s.Store(0x20,8,15);for(int i=0;i<data.Length;i++)s.Store(8+i,1,data[i]);}
 sealed class Prior950Checks:IItem950CheckSource {
  public Item950CheckObservation Check(ItemConstructorStorage s,int index,ulong target){if(index<0||index>=10||target!=0x180016710UL)throw new Exception("Unexpected950 prerequisite");return new Item950CheckObservation(true);}
 }
 sealed class PriorBE0Checks:IItemBE0CheckSource {
  int calls;
  public ItemBE0CheckObservation Check(ItemConstructorStorage s,ItemBE0ArrayStorage a,int index,ulong target){if(index!=calls++||target!=0x1800EBEB0UL)throw new Exception("UnexpectedBE0 prerequisite");return new ItemBE0CheckObservation(true);}
 }
 sealed class Fixture {
  public readonly ItemConstructorStorage State;
  public readonly ItemConstructorBridgeResult Bridge;
  public readonly ItemBE0ArrayStorage Array;
  public readonly ItemConstructorBE0Result BE0;
  public readonly ItemConstructorIdentityResult Prior;
  public Fixture(bool finishIdentity=true,bool finishBE0=true){
   var bytes=Fill(0x530);Put(bytes,0x2C,4,0x89ABCDEFUL);State=ItemConstructorNameTests.State(bytes);
   var matrix=new ActorMatrixInitializationSource(ActorMatrixInitializationState.CreateArchivedInitialState(),ActorMatrixThreadState.CreateArchivedTemplateThread(),true,true);
   Bridge=new ItemConstructorBridgeInvocation(State).RunWithArchivedActorProfile(new Actor198ArrayStorage(0x100000UL,Fill(0x208)),new ActorConstructorStorage(0x200000UL,Fill(0x358)),new ActorConstructorStorage(0x300000UL,Fill(0x38)),ActorCompletionObservations.WithArchivedConstants(null,null,null),matrix,new ActorEngineSlot(new ActorEngineContext(0x600000UL,9,9,7)));
   var p950=new ItemConstructor950Invocation(Bridge).Run(new Prior950Checks());var ba8=new ItemConstructorBA8Invocation(p950).RunWithArchivedWorldVec3Body();
   Array=new ItemBE0ArrayStorage(0x700000UL,Fill(0x108));BE0=new ItemConstructorBE0Invocation(ba8,Array).Run(finishBE0?new PriorBE0Checks():null);
   var identity=new ItemConstructorIdentityInvocation(BE0);Prior=finishIdentity?identity.Run():identity.Result;
   if(finishIdentity&&finishBE0&&!Prior.ReachedItem323977)throw new Exception("Real own prior did not reach323977");
  }
 }
 static void GetterSelectsAddressWithoutReadingPayload(){
  foreach(ulong capacity in new ulong[]{0,15}){
   var bytes=new byte?[0x530];Put(bytes,0x20,8,capacity);var state=State(bytes);var before=Observe(state);var r=ItemObjectNameGetter.Read(state);
   Need(r.Returned&&r.ReturnedPointer==Token+8&&r.FieldReads==1&&r.ResumeInstructionRva==0x3A76E,"unsigned inline capacity selects LEA+8 without source data");
   Need(Same(state,before)&&state.ReadLittleEndian(0x18,8)==null&&state.ReadByte(8)==null,"unknown length and pointee are not read by the getter");
  }
  foreach(ulong pointer in new ulong[]{0,Source,ulong.MaxValue}){
   var bytes=new byte?[0x530];Put(bytes,0x20,8,16);Put(bytes,8,8,pointer);var state=State(bytes);var r=ItemObjectNameGetter.Read(state);
   Need(r.Returned&&r.ReturnedPointer==pointer&&r.FieldReads==2&&r.ResumeInstructionRva==0x3A76E,"heap selection returns the observed address, including zero, without dereferencing it");
  }
  var unknownPointer=new byte?[0x530];Put(unknownPointer,0x20,8,ulong.MaxValue);var missing=ItemObjectNameGetter.Read(State(unknownPointer));
  Need(!missing.Returned&&missing.ReturnedPointer==null&&missing.ResumeInstructionRva==0x3A76B,"unknown heap pointer stops at the actual MOV, not a fabricated inline fallback");
  var unknownCapacity=ItemObjectNameGetter.Read(State(new byte?[0x530]));
  Need(!unknownCapacity.Returned&&unknownCapacity.ReturnedPointer==null&&unknownCapacity.ResumeInstructionRva==0x3A760,"unknown selector cannot choose a pointer");
  Need(!ItemObjectNameGetter.Read(null).Returned,"null Item does not return an invented name");
 }
 static void ReturnedPointerStaysLiveAndDoesNotReselect(){
  var state=State();Name(state,(byte)'a',0);var memory=new ItemNameMemory(state);var getter=ItemObjectNameGetter.Read(state);
  state.Store(8,1,(byte)'z');var scan=ItemNameScan.Scan(memory,getter.ReturnedPointer.Value);
  Need(scan.Terminated&&scan.Length==1&&scan.ByteReads==3,"scan observes mutation after getter return and repeats index0 in the native loop");
  var heap=new ItemNameRegion(Source+0x100,new byte?[]{(byte)'h',(byte)'i',0});var mapped=new ItemNameMemory(state,heap);
  state.Store(8,8,Source+0x100);state.Store(0x20,8,16);
  var old=ItemNameScan.Scan(mapped,getter.ReturnedPointer.Value);var current=ItemObjectNameGetter.Read(state);
  Need(old.Terminated&&old.Length==0&&current.ReturnedPointer==Source+0x100,"old inline pointer retains+8 identity after its header is changed to a heap pointer");
  state.Store(8,8,Source+0x200);heap.SetByte(0,(byte)'x');
  Need(ItemNameScan.Scan(mapped,current.ReturnedPointer.Value).Length==2,"already returned heap address does not reload the Item's later pointer field");
  heap.Invalidate();var invalid=ItemNameScan.Scan(mapped,current.ReturnedPointer.Value);
  Need(!invalid.Terminated&&invalid.Length==null,"invalidated source lifetime is an unknown observation, not an empty string");
  state.Store(8,8,0);var zero=ItemObjectNameGetter.Read(state);
  Need(zero.Returned&&zero.ReturnedPointer==0&&!ItemNameScan.Scan(mapped,0).Terminated,"getter may return known zero while the consumer cannot dereference it");
 }
 static void NulScanHasAnExplicitObservationBudget(){
  var state=State();var region=new ItemNameRegion(Source,new byte?[]{0x80,0xFF,0,null,0xA5});var memory=new ItemNameMemory(state,region);
  var r=ItemNameScan.Scan(memory,Source);
  Need(r.Terminated&&r.Length==2&&r.ByteReads==4,"high-bit bytes are data and first NUL ends the scan after the repeated index0 read, before unknown trailing data");
  region.SetByte(0,null);var first=ItemNameScan.Scan(memory,Source);
  Need(!first.Terminated&&first.Length==null&&first.ResumeInstructionRva==0x32397D,"unknown first byte stops at first CMP");
  region.SetByte(0,1);region.SetByte(1,null);var later=ItemNameScan.Scan(memory,Source);
  Need(!later.Terminated&&later.Length==null&&later.ResumeInstructionRva==0x32398A,"unknown later byte stops at the loop CMP");
  region.SetByte(0,0);Need(ItemNameScan.Scan(memory,Source,1).Terminated,"a terminator within the one-byte budget succeeds");
  var bytes=new byte?[65536];for(int i=0;i<bytes.Length-1;i++)bytes[i]=1;bytes[65535]=0;
  var large=new ItemNameMemory(state,new ItemNameRegion(Destination,bytes));
  var exact=ItemNameScan.Scan(large,Destination,65536);
  Need(exact.Terminated&&exact.Length==65535&&exact.ByteReads==65537,"last observed position may be NUL at the65536-position budget; repeated index0 is one extra read");
  var shortBudget=ItemNameScan.Scan(large,Destination,65535);
  Need(!shortBudget.Terminated&&shortBudget.Length==null&&shortBudget.ByteReads==65536,"position-budget exhaustion is not a native length limit or a discovered terminator");
  var unmapped=ItemNameScan.Scan(memory,Source+5);
  Need(!unmapped.Terminated&&unmapped.Length==null,"address outside the declared region cannot borrow adjacent storage");
 }
 static void ZeroAssignmentPreservesOrderedPrefixes(){
  var actor=new byte?[0x530];var state=State(actor);Inline(state,0x540,7);state.Store(0x540,8,0x8877665544332211UL);var memory=new ItemNameMemory(state);var expected=Observe(state);Put(expected,0x550,8,0);Put(expected,0x540,1,0);
  var zero=new ItemNameAssignInvocation(memory,0x540,0,0).Run();
  Need(zero.Returned&&zero.ReturnedTarget==Token+0x540&&zero.FieldStoreInstructions==2&&zero.CopyBytes==0&&!zero.MemcpyUsed&&Same(state,expected),"null source zero assignment performs only length then inline terminator writes");
  var unknownOld=State(null,true);unknownOld.Store(0x558,8,15); // old length is unknown and irrelevant for source below buffer
  var low=new ItemNameAssignInvocation(new ItemNameMemory(unknownOld),0x540,1,0).Run();
  Need(low.Returned&&low.FieldReads==3&&low.FieldStoreInstructions==2&&unknownOld.ReadLittleEndian(0x550,8)==0,"source below buffer reads only the three capacities, skipping unknown old length and source contents");
  var unknownHeap=State(null,true);unknownHeap.Store(0x558,8,16);unknownHeap.Store(0x550,8,9);var unknownBefore=Observe(unknownHeap);var unknownExpected=(byte?[])unknownBefore.Clone();Put(unknownExpected,0x550,8,0);
  var missingPointer=new ItemNameAssignInvocation(new ItemNameMemory(unknownHeap),0x540,0,0).Run();
  Need(!missingPointer.Returned&&missingPointer.ResumeInstructionRva==0x16521&&missingPointer.FieldStoreInstructions==1&&Same(unknownHeap,unknownExpected),"unknown heap pointer is read only after the zero length store when source is null");
  var bytes=new byte?[0x530];var missingHeap=State(bytes);missingHeap.Store(0x558,8,16);missingHeap.Store(0x550,8,9);
  // The Item tail is initially known. Replace the pointer with an unmapped address;
  // its lack of backing is discovered at the byte store AFTER length is written.
  missingHeap.Store(0x540,8,Destination);var before=Observe(missingHeap);var heapExpected=(byte?[])before.Clone();Put(heapExpected,0x550,8,0);
  var unresolved=new ItemNameAssignInvocation(new ItemNameMemory(missingHeap),0x540,0,0).Run();
  Need(!unresolved.Returned&&unresolved.FieldStoreInstructions==1&&unresolved.CopyBytes==0&&unresolved.ResumeInstructionRva==0x16524&&Same(missingHeap,heapExpected),"unmapped heap zero terminator retains the already written length prefix");
  var heap=new ItemNameRegion(Destination,Fill(8));var heapMemory=new ItemNameMemory(missingHeap,heap);var success=new ItemNameAssignInvocation(heapMemory,0x540,0,0).Run();
  Need(success.Returned&&success.FieldStoreInstructions==2&&heap.ReadByte(0)==0&&heap.ReadByte(1)==0xA5&&missingHeap.ReadLittleEndian(0x558,8)==16,"heap zero assignment retains capacity and all bytes after its terminator");
 }
 static void PositiveCopyRequiresItsOwnNormalContract(){
  var state=State();Inline(state,0x540);var source=new ItemNameRegion(Source,new byte?[]{0x80,0,0xFF,0x77});var memory=new ItemNameMemory(state,source);var before=Observe(state);
  var withheld=new ItemNameAssignInvocation(memory,0x540,Source,3).Run();
  Need(!withheld.Returned&&withheld.ResumeInstructionRva==0x1655E&&withheld.CopyBytes==0&&withheld.FieldStoreInstructions==0&&Same(state,before),"normal memcpy is an explicit external success condition, never a default callback");
  var expected=(byte?[])before.Clone();expected[0x540]=0x80;expected[0x541]=0;expected[0x542]=0xFF;expected[0x543]=0;Put(expected,0x550,8,3);
  var copied=new ItemNameAssignInvocation(memory,0x540,Source,3).Run(true);
  Need(copied.Returned&&copied.MemcpyUsed&&copied.CopyBytes==3&&copied.FieldStoreInstructions==2&&Same(state,expected),"helper copies exactly explicit count, including embedded NUL, then length and terminator");
  Need(source.ReadByte(0)==0x80&&source.ReadByte(1)==0&&source.ReadByte(2)==0xFF&&source.ReadByte(3)==0x77,"copy never mutates its disjoint source");
  var heapState=State();heapState.Store(0x540,8,Destination);heapState.Store(0x550,8,0);heapState.Store(0x558,8,16);var destination=new ItemNameRegion(Destination,Fill(3));
  var heapMemory=new ItemNameMemory(heapState,source,destination);var itemBefore=Observe(heapState);var itemExpected=(byte?[])itemBefore.Clone();Put(itemExpected,0x550,8,3);
  var partial=new ItemNameAssignInvocation(heapMemory,0x540,Source,3).Run(true);
  Need(!partial.Returned&&partial.CopyBytes==3&&partial.FieldStoreInstructions==1&&partial.ResumeInstructionRva==0x16576&&Same(heapState,itemExpected),"missing terminal observation preserves successful copy and following length store");
  Need(destination.ReadByte(0)==0x80&&destination.ReadByte(1)==0&&destination.ReadByte(2)==0xFF,"copy prefix is still inspectable after the terminal store cannot be observed");
  source.SetByte(1,null);var fresh=State();Inline(fresh,0x540);var freshBefore=Observe(fresh);var unknown=new ItemNameAssignInvocation(new ItemNameMemory(fresh,source),0x540,Source,3).Run(true);
  Need(!unknown.Returned&&unknown.ResumeInstructionRva==0x1655E&&unknown.CopyBytes==0&&unknown.FieldStoreInstructions==0&&Same(fresh,freshBefore),"unobserved memcpy input lies outside the explicit successful-copy domain, not an invented native error result");
  // Deliberate OWN raw-memory observations, not claimed valid C++ strings:
  // ordinary byte copying can change metadata which the saved code rereads.
  var capacityState=State();capacityState.Store(0x540,8,Token+0x558);capacityState.Store(0x550,8,0);capacityState.Store(0x558,8,16);
  var zeroBytes=new byte?[8];for(int i=0;i<8;i++)zeroBytes[i]=0;
  var capacityMemory=new ItemNameMemory(capacityState,new ItemNameRegion(Source,zeroBytes));var capacityExpected=Observe(capacityState);Put(capacityExpected,0x558,8,0);Put(capacityExpected,0x550,8,8);Put(capacityExpected,0x548,1,0);
  var changedCapacity=new ItemNameAssignInvocation(capacityMemory,0x540,Source,8).Run(true);
  Need(changedCapacity.Returned&&changedCapacity.CopyBytes==8&&changedCapacity.FieldStoreInstructions==2&&Same(capacityState,capacityExpected),"raw copy into capacity changes terminal selection from heap to inline without a fake callback");
  var pointerState=State();pointerState.Store(0x540,8,Token+0x540);pointerState.Store(0x550,8,0);pointerState.Store(0x558,8,16);var pointerBytes=new byte?[8];Put(pointerBytes,0,8,Destination);
  var terminalRegion=new ItemNameRegion(Destination,Fill(9));var pointerMemory=new ItemNameMemory(pointerState,new ItemNameRegion(Source,pointerBytes),terminalRegion);var pointerExpected=Observe(pointerState);Put(pointerExpected,0x540,8,Destination);Put(pointerExpected,0x550,8,8);
  var changedPointer=new ItemNameAssignInvocation(pointerMemory,0x540,Source,8).Run(true);
  Need(changedPointer.Returned&&Same(pointerState,pointerExpected)&&terminalRegion.ReadByte(7)==0xA5&&terminalRegion.ReadByte(8)==0,"raw pointer overwrite is observed after copy and length write when choosing the terminal address");
 }
 static void AliasGrowthFatalAndOverlapRemainFrontiers(){
  var state=State();Inline(state,0x540,3);var memory=new ItemNameMemory(state);var before=Observe(state);
  var alias=new ItemNameAssignInvocation(memory,0x540,Token+0x541,0).Run(true);
  Need(!alias.Returned&&alias.ResumeInstructionRva==0x164D2&&alias.Reason=="UnresolvedStringAliasHelper"&&alias.FieldStoreInstructions==0&&Same(state,before),"unexecuted alias tail JMP takes precedence even for a requested zero length");
  var aliasFatal=new ItemNameAssignInvocation(memory,0x540,Token+0x540,ulong.MaxValue).Run(true);
  Need(!aliasFatal.Returned&&aliasFatal.ResumeInstructionRva==0x164D2&&Same(state,before),"alias dispatch occurs before the maximal-count fatal branch");
  var heapAlias=State();heapAlias.Store(0x540,8,Destination);heapAlias.Store(0x550,8,3);heapAlias.Store(0x558,8,16);var heapBefore=Observe(heapAlias);
  var thirdRead=new ItemNameAssignInvocation(new ItemNameMemory(heapAlias),0x540,Destination+1,0).Run(true);
  Need(!thirdRead.Returned&&thirdRead.ResumeInstructionRva==0x164D2&&thirdRead.FieldReads==5&&Same(heapAlias,heapBefore),"heap alias path performs the third pointer read before its tail JMP, without reading payload");
  var end=new ItemNameAssignInvocation(memory,0x540,Token+0x543,0).Run();
  Need(end.Returned&&end.FieldStoreInstructions==2,"source at old end is excluded from the half-open alias range");
  Inline(state,0x540);before=Observe(state);var growth=new ItemNameAssignInvocation(memory,0x540,0,16).Run(true);
  Need(!growth.Returned&&growth.ResumeInstructionRva==0x164FB&&growth.Reason=="UnresolvedStringGrowth"&&growth.FieldStoreInstructions==0&&growth.CopyBytes==0&&Same(state,before),"count exceeding existing capacity stops before the growth call with no new destination writes");
  var fatal=new ItemNameAssignInvocation(memory,0x540,0,ulong.MaxValue).Run(true);
  Need(!fatal.Returned&&fatal.ResumeInstructionRva==0x164E4&&fatal.Reason=="UnresolvedStringLengthError"&&fatal.FieldStoreInstructions==0&&fatal.CopyBytes==0&&Same(state,before),"maximal unsigned count stops at the fatal call without wrapping into success");
  var overlap=new ItemNameAssignInvocation(memory,0x540,Token+0x541,4).Run(true);
  Need(!overlap.Returned&&overlap.ResumeInstructionRva==0x1655E&&overlap.CopyBytes==0&&overlap.FieldStoreInstructions==0&&Same(state,before),"old length zero bypasses alias helper, but overlapping memcpy is not replaced with memmove");
 }
 static void SevenAssignmentsMatchAWholeItemOracle(){
  foreach(bool empty in new bool[]{false,true}){
   var f=new Fixture();Name(f.State,empty?new byte[]{0}:new byte[]{0x80,(byte)'b',0});f.State.Store(0x18,8,ulong.MaxValue);
   foreach(int offset in Offsets){Inline(f.State,offset,4);f.State.Store(offset,8,0x8877665544332211UL);}
   f.State.Store(0x5A0,4,0xAABBCCDDUL);f.State.Store(0x608,4,0xAABBCCDDUL);f.State.Store(0x630,8,ulong.MaxValue);f.State.Store(0x698,4,0xAABBCCDDUL);f.State.Store(0x6B0,4,0xAABBCCDDUL);
   var before=Observe(f.State);var expected=(byte?[])before.Clone();var arrayBefore=f.Array.CopyObservation();
   for(int i=0;i<Offsets.Length;i++){Put(expected,Offsets[i]+0x10,8,i==0&&!empty?2UL:0UL);Put(expected,Offsets[i],1,0);}
   if(!empty){expected[0x540]=0x80;expected[0x541]=(byte)'b';expected[0x542]=0;}
   Put(expected,0x5A0,4,0);Put(expected,0x608,4,0);Put(expected,0x630,8,0);Put(expected,0x698,4,0);Put(expected,0x6B0,4,1);
   var memory=new ItemNameMemory(f.State);var r=new ItemConstructorNameInvocation(f.Prior,memory).Run(!empty);
   Need(r.Status=="ConditionalItemPrefix"&&r.ReachedItem323A49&&r.ResumeInstructionRva==0x323A49&&r.CallSiteRva==null&&!r.IsCompleteItemConstructor,"seven assignments stop before the unobserved global dependency");
   Need(r.CompletedAssignments==7&&r.Assignments.Count==7&&r.ScalarStoreInstructions==5&&Same(f.State,expected)&&SameArray(f.Array,arrayBefore),"entire Item oracle preserves source, padding, capacity and prior allocation while allowing exactly the proven changes");
   bool assignments=true;for(int i=0;i<7;i++)assignments &= r.Assignments[i].Returned&&r.Assignments[i].TargetOffset==Offsets[i]&&r.Assignments[i].FieldStoreInstructions==2&&r.Assignments[i].Count==(i==0&&!empty?2UL:0UL)&&r.Assignments[i].CopyBytes==(i==0&&!empty?2:0);
   Need(assignments,"all zero-length calls execute their two stores; only the nonempty first call copies bytes");
   Need(Object.ReferenceEquals(r.State,f.State)&&Object.ReferenceEquals(r.Prior,f.Prior)&&Object.ReferenceEquals(r.Memory,memory)&&Object.ReferenceEquals(r.State.Actor,f.Bridge.ActorCompletion.State)&&r.Getter.ReturnedPointer==Token+8,"composition uses original Item, Actor, memory and returned pointer identities");
  }
 }
 static void LaterFailureKeepsEveryCompletedPrefix(){
  for(int fail=1;fail<7;fail++){
   var f=new Fixture();Name(f.State,(byte)'n',0);foreach(int offset in Offsets)Inline(f.State,offset,0);
   int broken=Offsets[fail];f.State.Store(broken,8,Destination);f.State.Store(broken+0x10,8,9);f.State.Store(broken+0x18,8,16);f.State.Store(0x5A0,4,0x11223344UL);f.State.Store(0x608,4,0x55667788UL);
   var expected=Observe(f.State);for(int i=0;i<fail;i++){Put(expected,Offsets[i]+0x10,8,i==0?1UL:0UL);Put(expected,Offsets[i],1,i==0?(ulong)'n':0UL);}expected[0x541]=0;Put(expected,broken+0x10,8,0);
   if(fail>=3)Put(expected,0x5A0,4,0);if(fail>=6)Put(expected,0x608,4,0);
   var invocation=new ItemConstructorNameInvocation(f.Prior,new ItemNameMemory(f.State));var r=invocation.Run(true);
   Need(!r.ReachedItem323A49&&r.Status=="Unsupported"&&r.CompletedAssignments==fail&&r.Assignments.Count==fail+1&&r.CallSiteRva==Calls[fail]&&r.ResumeInstructionRva==0x16524,"later unresolved heap store reports its precise call and helper frontier at index"+fail);
   Need(r.Assignments[fail].FieldStoreInstructions==1&&r.ScalarStoreInstructions==(fail>=6?2:fail>=3?1:0)&&Same(f.State,expected),"stopped call retains length plus all earlier copies and only already reached scalar stores at index"+fail);
   var saved=Observe(f.State);bool again=false;try{invocation.Run(true);}catch(InvalidOperationException){again=true;}
   Need(again&&Object.ReferenceEquals(r,invocation.Result)&&Same(f.State,saved),"partial composition is never silently replayed by the same own attempt");
  }
 }
 static void RunSamplingAndPriorGuards(){
  var f=new Fixture();Name(f.State,(byte)'a',0);var memory=new ItemNameMemory(f.State);var before=Observe(f.State);var invocation=new ItemConstructorNameInvocation(f.Prior,memory);
  Need(!invocation.HasRun&&invocation.Result.Getter==null&&invocation.Result.Assignments.Count==0&&Same(f.State,before),"invocation creation does not snapshot a name or modify its target");
  Name(f.State,(byte)'x',(byte)'y',0);var r=invocation.Run(true);
  Need(r.ReachedItem323A49&&r.Scan.Length==2&&f.State.ReadLittleEndian(0x540,3)==0x7978UL,"Run sees the source changed since invocation construction");
  f.State.Store(8,1,(byte)'q');Need(r.Scan.Length==2&&f.State.ReadByte(0x540)==(byte)'x',"completed copy does not track later source mutations");
  var noPrior=new ItemConstructorNameInvocation(null,memory).Run(true);
  Need(noPrior.Status=="Unsupported"&&noPrior.Getter==null&&noPrior.Assignments.Count==0,"missing prior cannot authorize a getter even with supplied memory");
  var pending=new Fixture(false);var pendingBefore=Observe(pending.State);var notRun=new ItemConstructorNameInvocation(pending.Prior,new ItemNameMemory(pending.State)).Run(true);
  Need(notRun.Status=="Unsupported"&&notRun.Getter==null&&Same(pending.State,pendingBefore),"initialized-looking state does not replace a completed Identity result");
  var stopped=new Fixture(true,false);var stoppedBefore=Observe(stopped.State);var failedPrior=new ItemConstructorNameInvocation(stopped.Prior,new ItemNameMemory(stopped.State)).Run(true);
  Need(failedPrior.Getter==null&&!failedPrior.ReachedItem323A49&&Same(stopped.State,stoppedBefore)&&stopped.Prior.ResumeInstructionRva==0x2DCC75,"real unresolved BE0 check cannot be skipped through an incomplete Identity wrapper");
  var other=State();var mismatch=new ItemConstructorNameInvocation(f.Prior,new ItemNameMemory(other)).Run(true);
  Need(mismatch.Status=="Unsupported"&&mismatch.Getter==null&&mismatch.Assignments.Count==0,"memory tied to a different Item is not an interchangeable snapshot");
  var longSource=new Fixture();longSource.State.Store(8,8,Source);longSource.State.Store(0x20,8,16);var data=Fill(17);data[16]=0;var longMemory=new ItemNameMemory(longSource.State,new ItemNameRegion(Source,data));var longBefore=Observe(longSource.State);
  var growth=new ItemConstructorNameInvocation(longSource.Prior,longMemory).Run(true);
  Need(growth.Scan.Terminated&&growth.Scan.Length==16&&growth.CompletedAssignments==0&&growth.ResumeInstructionRva==0x164FB&&Same(longSource.State,longBefore),"successful name scan does not imply unimplemented destination growth succeeded");
 }
 public static int Main(){
  Group("GetterSelectsAddressWithoutReadingPayload",GetterSelectsAddressWithoutReadingPayload);Group("ReturnedPointerStaysLiveAndDoesNotReselect",ReturnedPointerStaysLiveAndDoesNotReselect);
  Group("NulScanHasAnExplicitObservationBudget",NulScanHasAnExplicitObservationBudget);Group("ZeroAssignmentPreservesOrderedPrefixes",ZeroAssignmentPreservesOrderedPrefixes);
  Group("PositiveCopyRequiresItsOwnNormalContract",PositiveCopyRequiresItsOwnNormalContract);Group("AliasGrowthFatalAndOverlapRemainFrontiers",AliasGrowthFatalAndOverlapRemainFrontiers);
  Group("SevenAssignmentsMatchAWholeItemOracle",SevenAssignmentsMatchAWholeItemOracle);Group("LaterFailureKeepsEveryCompletedPrefix",LaterFailureKeepsEveryCompletedPrefix);Group("RunSamplingAndPriorGuards",RunSamplingAndPriorGuards);
  Console.WriteLine("ItemConstructorNameTests: "+assertions+" assertions, "+groups+" groups passed; own conditional prefix only");return 0;
 }
}
