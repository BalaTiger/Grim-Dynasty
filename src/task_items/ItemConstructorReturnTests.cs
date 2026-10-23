using System;
using GrimDynasty.TaskItems;

// Explicit own observations only. Archived scale is1000; raw-register and
// alternative-scale edges do not claim actual CRT rand outputs or native state.
// No RNG, MXCSR mutation, GameEngine initialization or original code is run.
public static class ItemConstructorReturnTests {
 static int assertions,groups;
 const ulong Token=0x500000UL,GlobalToken=0xD00000UL;
 const uint Zero=0,One=0x3F800000U,Two=0x40000000U,Three=0x40400000U,Five=0x40A00000U,Six=0x40C00000U,Scale1000=0x447A0000U;
 static void Need(bool ok,string why){assertions++;if(!ok)throw new Exception(why);}
 static void Group(string name,Action test){int before=assertions;test();groups++;Console.WriteLine("PASS "+name+": "+(assertions-before)+" assertions");}
 static byte?[] Fill(int length){var a=new byte?[length];for(int i=0;i<a.Length;i++)a[i]=0xA5;return a;}
 static void Put(byte?[] a,int offset,int width,ulong value){for(int i=0;i<width;i++)a[offset+i]=unchecked((byte)(value>>(8*i)));}
 static byte?[] Observe(ItemConstructorStorage state){var a=new byte?[state.ByteCount];for(int i=0;i<a.Length;i++)a[i]=state.ReadByte(i);return a;}
 static bool Same(ItemConstructorStorage state,byte?[] expected){if(state.ByteCount!=expected.Length)return false;for(int i=0;i<expected.Length;i++)if(state.ReadByte(i)!=expected[i])return false;return true;}
 static bool SameArray(ItemBE0ArrayStorage array,byte?[] expected){for(int i=0;i<expected.Length;i++)if(array.ReadByte(i)!=expected[i])return false;return true;}
 static byte?[] FieldBytes(uint lower,uint upper){var a=new byte?[12];Put(a,0,4,lower);Put(a,4,4,upper);return a;}
 static bool Invalid(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
 sealed class Checks950:IItem950CheckSource {
  public Item950CheckObservation Check(ItemConstructorStorage state,int index,ulong target){if(index<0||index>=10||target!=0x180016710UL)throw new Exception("Unexpected950 prerequisite");return new Item950CheckObservation(true);}
 }
 sealed class ChecksBE0:IItemBE0CheckSource {
  int count;
  public ItemBE0CheckObservation Check(ItemConstructorStorage state,ItemBE0ArrayStorage array,int index,ulong target){if(index!=count++||target!=0x1800EBEB0UL)throw new Exception("UnexpectedBE0 prerequisite");return new ItemBE0CheckObservation(true);}
 }
 sealed class Fixture {
  public readonly ItemConstructorStorage State;
  public readonly ItemConstructorBridgeResult Bridge;
  public readonly ItemBE0ArrayStorage Array;
  public readonly ItemConstructorNameResult Prior;
  public Fixture(bool runName=true,bool unresolvedCopy=false){
   var bytes=Fill(0x530);Put(bytes,0x2C,4,0x89ABCDEFUL);State=ItemConstructorStorage.CreateForBE0Continuation(Token,new ActorConstructorPrefixState(bytes),Fill(0x735));
   var matrix=new ActorMatrixInitializationSource(ActorMatrixInitializationState.CreateArchivedInitialState(),ActorMatrixThreadState.CreateArchivedTemplateThread(),true,true);
   Bridge=new ItemConstructorBridgeInvocation(State).RunWithArchivedActorProfile(new Actor198ArrayStorage(0x100000UL,Fill(0x208)),new ActorConstructorStorage(0x200000UL,Fill(0x358)),new ActorConstructorStorage(0x300000UL,Fill(0x38)),ActorCompletionObservations.WithArchivedConstants(null,null,null),matrix,new ActorEngineSlot(new ActorEngineContext(0x600000UL,9,9,7)));
   var p950=new ItemConstructor950Invocation(Bridge).Run(new Checks950());var ba8=new ItemConstructorBA8Invocation(p950).RunWithArchivedWorldVec3Body();Array=new ItemBE0ArrayStorage(0x700000UL,Fill(0x108));
   var be0=new ItemConstructorBE0Invocation(ba8,Array).Run(new ChecksBE0());var identity=new ItemConstructorIdentityInvocation(be0).Run();
   if(unresolvedCopy){State.Store(8,1,(byte)'n');State.Store(9,1,0);}
   var name=new ItemConstructorNameInvocation(identity,new ItemNameMemory(State));Prior=runName?name.Run():name.Result;
   if(runName&&!unresolvedCopy&&!Prior.ReachedItem323A49)throw new Exception("Real own prior did not reach323A49");
  }
 }
 sealed class Case {
  public readonly Fixture Fixture;
  public readonly ItemGameEngineFields Fields;
  public readonly ItemGameEngineSlot Slot;
  public readonly ItemReturnNumericInputs Numeric;
  public readonly ItemConstructorReturnInvocation Invocation;
  public Case(uint lower,uint upper,uint? scale=Scale1000,bool normalEnvironment=true){
   Fixture=new Fixture();Fields=new ItemGameEngineFields(GlobalToken,FieldBytes(lower,upper));Slot=new ItemGameEngineSlot(GlobalToken,Fields);Numeric=new ItemReturnNumericInputs(scale,normalEnvironment);
   Invocation=new ItemConstructorReturnInvocation(Fixture.Prior,Slot,Numeric);
  }
 }
 static void ArchivedScaleReturnsTheOriginalItem(){
  var c=new Case(Three,Five);var state=c.Fixture.State;state.Store(0xB64,4,0xAABBCCDDUL);var expected=Observe(state);var array=c.Fixture.Array.CopyObservation();Put(expected,0xB68,4,Three);Put(expected,0xB6C,4,Five);
  var waiting=c.Invocation.RunToRandom();
  Need(waiting.AwaitingRandomObservation&&waiting.SavedDivisor==2000&&waiting.ResumeInstructionRva==0x3245A2&&waiting.CallerStoreInstructions==2&&waiting.DependencyStoreInstructions==0&&Same(state,expected),"observed3/5 and archived1000 reach external-call boundary without a fabricated random result");
  var result=c.Invocation.CompleteWithRandomObservation(new ItemReturnRandomObservation(true,1001));Put(expected,0xB64,4,4001);
  Need(result.ConditionalNormalReturn&&result.DependencyReturned&&result.ReturnedItemToken==Token&&result.ResumeInstructionRva==0x323A86&&result.CallSiteRva==null&&result.Status=="ConditionalItemReturn"&&!result.IsCompleteItemConstructor,"normal dependency return permits caller's original Item identity, with full compatibility still unclaimed");
  Need(result.StoredValue==4001&&result.Remainder==1001&&result.RandomEax==1001&&result.CallerStoreInstructions==2&&result.DependencyStoreInstructions==1&&Same(state,expected)&&SameArray(c.Fixture.Array,array),"whole Item and allocation oracle allows exactly two global copies plus finalB64 DWORD");
  Need(result.SlotReads==1&&result.GlobalFieldReads==2&&result.ItemFieldReads==3&&result.ScaleReads==2&&Object.ReferenceEquals(result.State,state)&&Object.ReferenceEquals(state.Actor,c.Fixture.Bridge.ActorCompletion.State),"complete path preserves shared Item/Actor and instruction-ordered observation counts");
  Need(ItemReturnNumericInputs.ArchivedScaleBits==Scale1000&&ItemReturnNumericInputs.WithArchivedScale().ScaleBits==Scale1000&&!ItemReturnNumericInputs.WithArchivedScale().NormalMaskedFpEnvironmentObserved,"archived operand does not automatically grant the unknown loaded FP environment");
  Need(c.Fields.ReadByte(8)==null&&c.Fields.ReadByte(11)==null&&state.ReadLittleEndian(0x2C,4)==0x89ABCDEFUL&&state.ReadLittleEndian(0x538,4)==0x89ABCDEFUL,"unused global padding and previous identity fields remain untouched");
 }
 static void BorrowedIdentityAndBothFieldLoadsAreLive(){
  var c=new Case(Three,Five);var state=c.Fixture.State;var before=Observe(state);
  Need(!c.Invocation.HasRun&&c.Invocation.Result.SlotReads==0&&c.Invocation.Result.BorrowedFields==null&&Same(state,before),"constructing the invocation neither borrows nor copies");
  var replacement=new ItemGameEngineFields(GlobalToken+0x10000,FieldBytes(Two,Six));c.Slot.Publish(replacement.Token,replacement);var begun=c.Invocation.Begin();
  Need(begun.BorrowedPointer==replacement.Token&&Object.ReferenceEquals(begun.BorrowedFields,replacement)&&begun.SlotReads==1&&Same(state,before),"Begin samples the actual slot after invocation creation");
  c.Slot.Publish(c.Fields.Token,c.Fields);replacement.Store(0,4,Three);c.Invocation.CopyFirstField();var expected=(byte?[])before.Clone();Put(expected,0xB68,4,Three);
  Need(Same(state,expected)&&c.Invocation.Result.GlobalFieldReads==1&&c.Invocation.Result.CallerStoreInstructions==1,"later slot replacement does not redirect the borrowed first field");
  replacement.Store(4,4,Five);c.Invocation.CopySecondField();Put(expected,0xB6C,4,Five);
  Need(Same(state,expected)&&c.Invocation.Result.SlotReads==1&&c.Invocation.Result.GlobalFieldReads==2&&c.Invocation.Result.Phase=="FieldsCopied","second field is read later from the same live borrowed object, not presnapshotted");
  var unknownSecond=new byte?[12];Put(unknownSecond,0,4,Three);var f=new Fixture();var fields=new ItemGameEngineFields(GlobalToken,unknownSecond);var invocation=new ItemConstructorReturnInvocation(f.Prior,new ItemGameEngineSlot(GlobalToken,fields),ItemReturnNumericInputs.WithArchivedScale(true));var firstExpected=Observe(f.State);Put(firstExpected,0xB68,4,Three);
  var stopped=invocation.RunToRandom();
  Need(stopped.Status=="Unsupported"&&stopped.Reason=="UnknownGameEngineField"&&stopped.ResumeInstructionRva==0x323A5C&&stopped.CallerStoreInstructions==1&&stopped.ItemFieldReads==0&&Same(f.State,firstExpected),"unknown second DWORD retains the first copy and does not start dependency arithmetic");
  var invalidated=new Case(Three,Five);invalidated.Invocation.Begin();invalidated.Invocation.CopyFirstField();var kept=Observe(invalidated.Fixture.State);invalidated.Fields.Invalidate();var invalid=invalidated.Invocation.CopySecondField();
  Need(invalid.Reason=="InvalidatedGameEngineFields"&&invalid.ResumeInstructionRva==0x323A5C&&invalid.CallerStoreInstructions==1&&Same(invalidated.Fixture.State,kept),"borrowed lifetime invalidation preserves the already copied first field");
 }
 static void MissingGlobalsNeverBecomeDefaults(){
  var f=new Fixture();var bytes=Observe(f.State);var fields=new ItemGameEngineFields(GlobalToken,FieldBytes(Three,Five));
  var unknown=new ItemConstructorReturnInvocation(f.Prior,new ItemGameEngineSlot(null,fields),ItemReturnNumericInputs.WithArchivedScale(true)).RunToRandom();
  Need(unknown.Reason=="UnknownGameEngineGlobalPointer"&&unknown.ResumeInstructionRva==0x323A49&&unknown.CallerStoreInstructions==0&&Same(f.State,bytes),"unknown slot is not constructor-default GameEngine data");
  var zero=new ItemConstructorReturnInvocation(f.Prior,new ItemGameEngineSlot(0,null),ItemReturnNumericInputs.WithArchivedScale(true)).RunToRandom();
  Need(zero.BorrowedPointer==0&&zero.Reason=="NullGameEngineGlobalPointer"&&zero.ResumeInstructionRva==0x323A50&&zero.SlotReads==1&&zero.GlobalFieldReads==1&&Same(f.State,bytes),"known null is borrowed then stops at the actual dereference frontier");
  var missing=new ItemConstructorReturnInvocation(f.Prior,new ItemGameEngineSlot(GlobalToken,null),ItemReturnNumericInputs.WithArchivedScale(true)).RunToRandom();
  Need(missing.Reason=="MissingBorrowedGameEngineFields"&&missing.CallerStoreInstructions==0&&Same(f.State,bytes),"known address alone does not supply missing fields");
  var mismatch=new ItemConstructorReturnInvocation(f.Prior,new ItemGameEngineSlot(GlobalToken+16,fields),ItemReturnNumericInputs.WithArchivedScale(true)).RunToRandom();
  Need(mismatch.Reason=="GameEngineBorrowedIdentityMismatch"&&mismatch.CallerStoreInstructions==0&&Same(f.State,bytes),"different pointer and backing identities are not synchronized by copying");
  var alias=new ItemGameEngineFields(Token-0x1B58UL,FieldBytes(Three,Five));var overlap=new ItemConstructorReturnInvocation(f.Prior,new ItemGameEngineSlot(alias.Token,alias),ItemReturnNumericInputs.WithArchivedScale(true)).RunToRandom();
  Need(overlap.Reason=="BorrowedFieldAliasingOutsideCoverage"&&overlap.ResumeInstructionRva==0x323A50&&overlap.CallerStoreInstructions==0&&Same(f.State,bytes),"independent global backing cannot pretend to alias the Item's actual bytes");
  for(int missingByte=0;missingByte<4;missingByte++){
   var partial=FieldBytes(Three,Five);partial[missingByte]=null;var source=new ItemGameEngineFields(GlobalToken,partial);var r=new ItemConstructorReturnInvocation(f.Prior,new ItemGameEngineSlot(GlobalToken,source),ItemReturnNumericInputs.WithArchivedScale(true)).RunToRandom();
   Need(r.Reason=="UnknownGameEngineField"&&r.ResumeInstructionRva==0x323A50&&r.CallerStoreInstructions==0&&Same(f.State,bytes),"a partial first DWORD cannot invent missing bits at byte"+missingByte);
  }
 }
 static void ZeroSpanCallsBeforeItsDelayedDivisionFault(){
  var c=new Case(Three,Three);var state=c.Fixture.State;state.Store(0xB64,4,0xAABBCCDDUL);var expected=Observe(state);Put(expected,0xB68,4,Three);Put(expected,0xB6C,4,Three);Put(expected,0xB64,4,3000);
  var waiting=c.Invocation.RunToRandom();
  Need(waiting.SavedDivisor==0&&waiting.StoredValue==3000&&waiting.DependencyStoreInstructions==1&&waiting.AwaitingRandomObservation&&waiting.ResumeInstructionRva==0x3245A2&&waiting.ScaleReads==2&&Same(state,expected),"zero span writesB64 then still awaits the external call, rather than returning successfully");
  var fault=c.Invocation.CompleteWithRandomObservation(new ItemReturnRandomObservation(true,13));
  Need(fault.Status=="NativeIntegerFaultFrontier"&&fault.Reason=="IntegerDivideByZero"&&fault.ResumeInstructionRva==0x3245B9&&!fault.DependencyReturned&&!fault.ConditionalNormalReturn&&fault.ReturnedItemToken==null,"normal external return reaches the saved zero IDIV without a repaired fallback");
  Need(fault.RandomEax==13&&fault.ItemFieldReads==3&&fault.ScaleReads==3&&fault.DependencyStoreInstructions==1&&Same(state,expected),"postcall B68 reread and multiply precede zero-divide stop and retain the earlier store");
  var badScale=new Case(Three,Three);badScale.Invocation.RunToRandom();var retained=Observe(badScale.Fixture.State);badScale.Numeric.ScaleBits=null;var earlier=badScale.Invocation.CompleteWithRandomObservation(new ItemReturnRandomObservation(true,7));
  Need(earlier.Reason=="UnknownScaleOperand"&&earlier.ResumeInstructionRva==0x3245B1&&earlier.RandomEax==7&&earlier.ItemFieldReads==3&&Same(badScale.Fixture.State,retained),"known zero divisor does not skip a preceding unresolved postcall scale read");
  var invalidScale=new Case(Three,Three);invalidScale.Invocation.RunToRandom();invalidScale.Numeric.ScaleBits=0x7FC00000U;var invalid=invalidScale.Invocation.CompleteWithRandomObservation(new ItemReturnRandomObservation(true,7));
  Need(invalid.Status=="Unsupported"&&invalid.ResumeInstructionRva==0x3245B1&&invalid.DependencyStoreInstructions==1,"postcall invalid floating operand also precedes the integer fault frontier");
 }
 static void MissingRandomObservationPreservesItsPrefix(){
  ItemReturnRandomObservation[] observations={null,new ItemReturnRandomObservation(false,17),new ItemReturnRandomObservation(true,null)};
  string[] reasons={"MissingRandomCallObservation","UnresolvedRandomCallReturn","UnknownRandomReturnEax"};
  for(int i=0;i<observations.Length;i++){
   var c=new Case(Three,Three);c.Invocation.RunToRandom();var before=Observe(c.Fixture.State);var result=c.Invocation.CompleteWithRandomObservation(observations[i]);
   Need(result.Reason==reasons[i]&&result.ResumeInstructionRva==0x3245A2&&result.ItemFieldReads==2&&result.ScaleReads==2&&result.DependencyStoreInstructions==1&&Same(c.Fixture.State,before),"missing external result does not resampleB68 or erase the zero-branch store for case"+i);
   Need(!result.AwaitingRandomObservation&&Invalid(delegate{c.Invocation.CompleteWithRandomObservation(new ItemReturnRandomObservation(true,1));})&&Same(c.Fixture.State,before),"stopped attempt cannot silently accept a later replacement observation");
  }
 }
 static void PostcallReadsSeeLiveItemAndScaleButNotNewDivisor(){
  var c=new Case(Two,Six);var waiting=c.Invocation.RunToRandom();
  Need(waiting.SavedDivisor==4000&&waiting.ScaleReads==1,"precall divisor is captured using the first live1000 operand");
  c.Fixture.State.Store(0xB68,4,0x41200000U);c.Fixture.State.Store(0xB6C,4,0x42C80000U);c.Numeric.ScaleBits=0x44FA0000U; // own observed10,100 and2000
  c.Fields.Store(0,4,0x42480000U);c.Slot.Publish(null,null);var expected=Observe(c.Fixture.State);Put(expected,0xB64,4,20005);
  var result=c.Invocation.CompleteWithRandomObservation(new ItemReturnRandomObservation(true,5));
  Need(result.ConditionalNormalReturn&&result.SavedDivisor==4000&&result.Remainder==5&&result.StoredValue==20005&&Same(c.Fixture.State,expected),"postcall lower and scale are reread, while mutated upper does not recompute savedEDI");
  Need(result.SlotReads==1&&result.GlobalFieldReads==2&&result.ItemFieldReads==3&&result.ScaleReads==2&&Object.ReferenceEquals(result.BorrowedFields,c.Fields),"postcall does not reread or redirect the previously borrowed global");
  c.Numeric.ScaleBits=null;c.Fixture.State.Store(0xB68,4,0);var before=Observe(c.Fixture.State);
  Need(Invalid(delegate{c.Invocation.CompleteWithRandomObservation(new ItemReturnRandomObservation(true,0));})&&Same(c.Fixture.State,before)&&result.StoredValue==20005,"completed result is not recalculated by changing its inputs and repeating a phase");
 }
 static void SignedRawRegisterEdgesAreNotRandRangeClaims(){
  int[] dividends={-5,int.MaxValue};int[] values={1995,5647};int[] remainders={-5,3647};
  for(int i=0;i<dividends.Length;i++){
   var c=new Case(Two,Six);c.Invocation.RunToRandom();var result=c.Invocation.CompleteWithRandomObservation(new ItemReturnRandomObservation(true,dividends[i]));
   Need(result.ConditionalNormalReturn&&result.Remainder==remainders[i]&&result.StoredValue==values[i],"own raw EAX uses signed remainder without inventing a CRT return-range clamp for case"+i);
  }
  var negative=new Case(Six,Two);negative.Invocation.RunToRandom();var normal=negative.Invocation.CompleteWithRandomObservation(new ItemReturnRandomObservation(true,5));
  Need(normal.SavedDivisor==-4000&&normal.Remainder==5&&normal.StoredValue==6005&&normal.ConditionalNormalReturn,"negative span remains a signed divisor, not unsigned modulus");
  // Alternative scale1 is a synthetic raw operand to isolate CPU IDIV overflow;
  // it is not the archived1000 constant nor a claim of real negative rand output.
  var overflow=new Case(One,Zero,One);overflow.Invocation.RunToRandom();var before=Observe(overflow.Fixture.State);var fault=overflow.Invocation.CompleteWithRandomObservation(new ItemReturnRandomObservation(true,int.MinValue));
  Need(fault.SavedDivisor==-1&&fault.Status=="NativeIntegerFaultFrontier"&&fault.Reason=="IntegerDivideOverflow"&&fault.ResumeInstructionRva==0x3245B9&&fault.DependencyStoreInstructions==0&&Same(overflow.Fixture.State,before),"INT_MIN divided by-1 stops before remainder/store without managed arithmetic repair");
 }
 static void NumericCoverageStopsBeforeInventingAResult(){
  var noEnvironment=new Case(Three,Five,Scale1000,false);var missing=noEnvironment.Invocation.RunToRandom();
  Need(missing.Reason=="MissingNormalMaskedFpEnvironment"&&missing.ResumeInstructionRva==0x324578&&missing.CallerStoreInstructions==2&&missing.DependencyStoreInstructions==0&&missing.ScaleReads==0,"archived scale cannot substitute for an observed normal FP environment");
  var noScale=new Case(Three,Five,null);var unknown=noScale.Invocation.RunToRandom();
  Need(unknown.Reason=="UnknownScaleOperand"&&unknown.ResumeInstructionRva==0x324580&&unknown.ScaleReads==1&&unknown.SavedDivisor==null,"unknown operand stops at its actual first multiply read");
  uint[] lower={Zero,1,0x00800000U,0x0D800000U,Zero,Zero,0x4F000000U};
  uint[] upper={0x7FC00000U,One,0x00800001U,One,One,0x4F000000U,0x4F000000U};
  uint[] scales={Scale1000,Scale1000,Scale1000,Scale1000,0x7F800000U,Scale1000,Scale1000};
  uint[] rvas={0x324578,0x324578,0x324578,0x324578,0x324580,0x324588,0x324598};
  string[] why={"NaN input","subnormal input","subnormal subtraction result","large exponent gap that double would lose","infinite scale","firstCVTT overflow","zero-span initialCVTT overflow"};
  for(int i=0;i<lower.Length;i++){
   var c=new Case(lower[i],upper[i],scales[i]);var expected=Observe(c.Fixture.State);Put(expected,0xB68,4,lower[i]);Put(expected,0xB6C,4,upper[i]);var r=c.Invocation.RunToRandom();
   Need(r.Status=="Unsupported"&&r.ResumeInstructionRva==rvas[i]&&!r.AwaitingRandomObservation&&r.DependencyStoreInstructions==0&&!r.DependencyReturned&&Same(c.Fixture.State,expected),why[i]+" remains a coverage frontier with only the two caller copies");
  }
  // Fractional but EXACT binary32 operations are allowed; CVTT itself truncates.
  var fraction=new Case(0x3FC00000U,0x40200000U,One);fraction.Invocation.RunToRandom();var trunc=fraction.Invocation.CompleteWithRandomObservation(new ItemReturnRandomObservation(true,0));
  Need(trunc.ConditionalNormalReturn&&trunc.SavedDivisor==1&&trunc.StoredValue==1,"synthetic exact1.5/2.5/scale1 demonstrates truncation toward zero without rounding arithmetic");
 }
 static void PostcallConversionAndAdditionBoundaries(){
  var remainder=new Case(Zero,0x47800000U);remainder.Invocation.RunToRandom();var before=Observe(remainder.Fixture.State);var lost=remainder.Invocation.CompleteWithRandomObservation(new ItemReturnRandomObservation(true,16777217));
  Need(lost.SavedDivisor==65536000&&lost.Remainder==16777217&&lost.Reason=="Binary32RoundingRequired"&&lost.ResumeInstructionRva==0x3245BF&&lost.DependencyStoreInstructions==0&&Same(remainder.Fixture.State,before),"unrepresentable remainder stops at CVTDQ2PS rather than rounding silently");
  var addition=new Case(Two,Six);addition.Invocation.RunToRandom();addition.Fixture.State.Store(0xB68,4,0x4B800000U);var addBefore=Observe(addition.Fixture.State);var rounded=addition.Invocation.CompleteWithRandomObservation(new ItemReturnRandomObservation(true,1));
  Need(rounded.ResumeInstructionRva==0x3245C2&&rounded.Reason=="Binary32RoundingRequired"&&rounded.DependencyStoreInstructions==0&&Same(addition.Fixture.State,addBefore),"exact scaled lower plus a too-small remainder stops at ADDSS without final store");
  var finalCvtt=new Case(Two,Six);finalCvtt.Invocation.RunToRandom();finalCvtt.Fixture.State.Store(0xB68,4,0x4F000000U);var finalBefore=Observe(finalCvtt.Fixture.State);var invalid=finalCvtt.Invocation.CompleteWithRandomObservation(new ItemReturnRandomObservation(true,0));
  Need(invalid.ResumeInstructionRva==0x3245C6&&invalid.Reason=="InvalidCvttss2siOutsideCoverage"&&invalid.DependencyStoreInstructions==0&&Same(finalCvtt.Fixture.State,finalBefore),"finite exact final arithmetic does not make out-of-range signed conversion valid");
 }
 static void PriorAndPhaseGuardsCannotBypassDependencies(){
  var c=new Case(Three,Five);var before=Observe(c.Fixture.State);
  Need(Invalid(delegate{c.Invocation.CopySecondField();})&&Invalid(delegate{c.Invocation.AdvanceToRandom();})&&Invalid(delegate{c.Invocation.CompleteWithRandomObservation(new ItemReturnRandomObservation(true,1));})&&!c.Invocation.HasRun&&Same(c.Fixture.State,before),"out-of-order own phases cannot manufacture copied fields or a dependency return");
  c.Invocation.Begin();Need(Invalid(delegate{c.Invocation.Begin();})&&c.Invocation.Result.SlotReads==1&&Same(c.Fixture.State,before),"Begin cannot silently recapture the global slot");
  c.Invocation.CopyFirstField();var afterFirst=Observe(c.Fixture.State);Need(Invalid(delegate{c.Invocation.CopyFirstField();})&&Same(c.Fixture.State,afterFirst),"field-copy phase cannot be replayed after its effect");
  var absent=new ItemConstructorReturnInvocation(null,c.Slot,c.Numeric).RunToRandom();Need(absent.Reason=="MissingPriorItemNamePrefix"&&absent.SlotReads==0&&!absent.ConditionalNormalReturn,"no Name result cannot authorize global access");
  var unrun=new Fixture(false);var unrunBefore=Observe(unrun.State);var pending=new ItemConstructorReturnInvocation(unrun.Prior,c.Slot,c.Numeric).RunToRandom();
  Need(pending.Reason=="PriorItemNamePrefixIncomplete"&&pending.SlotReads==0&&Same(unrun.State,unrunBefore),"known-looking bytes do not replace an executed Name continuation");
  var stopped=new Fixture(true,true);var stoppedBefore=Observe(stopped.State);var preserved=new ItemConstructorReturnInvocation(stopped.Prior,c.Slot,c.Numeric).RunToRandom();
  Need(preserved.Reason=="PriorItemNamePrefixIncomplete"&&preserved.ResumeInstructionRva==0x1655E&&preserved.SlotReads==0&&Same(stopped.State,stoppedBefore),"actual unresolved memcpy frontier remains intact and cannot be bypassed to Item return");
 }
 public static int Main(){
  Group("ArchivedScaleReturnsTheOriginalItem",ArchivedScaleReturnsTheOriginalItem);Group("BorrowedIdentityAndBothFieldLoadsAreLive",BorrowedIdentityAndBothFieldLoadsAreLive);
  Group("MissingGlobalsNeverBecomeDefaults",MissingGlobalsNeverBecomeDefaults);Group("ZeroSpanCallsBeforeItsDelayedDivisionFault",ZeroSpanCallsBeforeItsDelayedDivisionFault);Group("MissingRandomObservationPreservesItsPrefix",MissingRandomObservationPreservesItsPrefix);
  Group("PostcallReadsSeeLiveItemAndScaleButNotNewDivisor",PostcallReadsSeeLiveItemAndScaleButNotNewDivisor);Group("SignedRawRegisterEdgesAreNotRandRangeClaims",SignedRawRegisterEdgesAreNotRandRangeClaims);
  Group("NumericCoverageStopsBeforeInventingAResult",NumericCoverageStopsBeforeInventingAResult);Group("PostcallConversionAndAdditionBoundaries",PostcallConversionAndAdditionBoundaries);Group("PriorAndPhaseGuardsCannotBypassDependencies",PriorAndPhaseGuardsCannotBypassDependencies);
  Console.WriteLine("ItemConstructorReturnTests: "+assertions+" assertions, "+groups+" groups passed; own conditional normal return only");return 0;
 }
}
