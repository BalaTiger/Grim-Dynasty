using System;
using System.Collections.Generic;
using System.Text;
using GrimDynasty.TaskItems;

// Own wire fixtures and explicit observations only. No original archive, native
// getter, global publication, OS synchronization, rand or FP state is executed.
public static class ItemGameEngineFloatLoadTests {
 static int assertions,groups;
 const ulong MinAddress=0x800008UL,MaxAddress=0x800048UL,GlobalToken=0xD00000UL;
 const uint Two=0x40000000U,Three=0x40400000U,Four=0x40800000U,Five=0x40A00000U,Six=0x40C00000U;
 const string MinKey="itemSparkleMinTime",MaxKey="itemSparkleMaxTime";
 static void Need(bool ok,string why){assertions++;if(!ok)throw new Exception(why);}
 static void Group(string name,Action test){int before=assertions;test();groups++;Console.WriteLine("PASS "+name+": "+(assertions-before)+" assertions");}
 static byte[] B(string text){return Encoding.ASCII.GetBytes(text);}
 static byte[] C(string text){return B(text+"\0");}
 static TableString S(string text){return new TableString(B(text));}
 static byte?[] Fill(int size){var bytes=new byte?[size];for(int i=0;i<size;i++)bytes[i]=0xA5;return bytes;}
 static void Put(byte?[] bytes,int offset,int width,ulong value){for(int i=0;i<width;i++)bytes[offset+i]=unchecked((byte)(value>>(8*i)));}
 static void Word(List<byte> bytes,uint value){for(int i=0;i<4;i++)bytes.Add(unchecked((byte)(value>>(8*i))));}
 static byte[] Field(int tag,uint key,params uint[] values){var bytes=new List<byte>();Word(bytes,((uint)values.Length<<16)|(uint)tag);Word(bytes,key);foreach(uint value in values)Word(bytes,value);return bytes.ToArray();}
 static byte[] Join(params byte[][] pieces){var bytes=new List<byte>();foreach(var piece in pieces)bytes.AddRange(piece);return bytes.ToArray();}
 static BinaryParsedRecord Parse(RegistrationGate gate,params byte[][] pieces){
  byte[] wire=Join(pieces);var reader=new BinaryRecordReader(gate,wire,0,wire.Length);var parsed=BinaryRecordParser.DeserializeFresh(gate,reader);
  if(parsed.Status!="Known"||parsed.Record==null||!parsed.ReaderRewound||reader.Position!=wire.Length||!reader.Consumed||gate.IsHeldByCurrentThread)throw new Exception("Own literal record did not parse");
  reader.Dispose();return parsed.Record;
 }
 static BinaryPointerCacheSnapshot Empty(uint capacity){return new BinaryPointerCacheSnapshot(0,0,new[]{new BinaryNameCacheEntry[0]},0,capacity,new BinaryNameCacheEntry[(int)capacity]);}
 static BinaryPointerCacheSnapshot Warm(params BinaryNameCacheEntry[] entries){return new BinaryPointerCacheSnapshot(0,0,new[]{entries});}
 static BinaryNameIdSnapshot Map(params BinaryNameIdEntry[] entries){return new BinaryNameIdSnapshot(1,entries);}
 static BinaryNameIdSnapshot StandardMap(){return Map(new BinaryNameIdEntry(B(MinKey),0),new BinaryNameIdEntry(B(MaxKey),1));}
 static TableString[] Pool(){return new[]{S(MinKey),S(MaxKey),S("Other")};}
 static LoadTableBinarySnapshot Table(BinaryParsedRecord parsed,BinaryPointerCacheSnapshot names,BinaryNameIdSnapshot ids){return new LoadTableBinarySnapshot(parsed.Gate,true,parsed.ToWarmState(names,Pool(),ids));}
 static BinaryParsedRecord Pair(RegistrationGate gate,uint min=Two,uint max=Six){return Parse(gate,Field(1,0,min),Field(1,1,max));}
 static BinaryFloatTableGetterResult Get(LoadTableBinarySnapshot table,BinaryParsedRecord parsed,string key=MinKey,uint index=0,uint? fallback=Three,ulong? address=MinAddress){return BinaryFloatTableGetter.GetIndexedValue(table,parsed,C(key),index,fallback,address);}
 static bool Released(BinaryFloatTableGetterResult result,RegistrationGate gate){return result.GateAcquired&&result.GateReleased&&!gate.IsHeldByCurrentThread;}
 static byte?[] Observe(ItemGameEngineFields fields){var bytes=new byte?[12];for(int i=0;i<bytes.Length;i++)bytes[i]=fields.ReadByte(i);return bytes;}
 static byte?[] Observe(ItemConstructorStorage state){var bytes=new byte?[state.ByteCount];for(int i=0;i<bytes.Length;i++)bytes[i]=state.ReadByte(i);return bytes;}
 static bool Same(ItemGameEngineFields fields,byte?[] expected){for(int i=0;i<expected.Length;i++)if(fields.ReadByte(i)!=expected[i])return false;return true;}
 static bool Same(ItemConstructorStorage state,byte?[] expected){if(state.ByteCount!=expected.Length)return false;for(int i=0;i<expected.Length;i++)if(state.ReadByte(i)!=expected[i])return false;return true;}
 static bool Invalid(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}

 static void ParsedRawFloatsAndUnsignedClamp(){
  var gate=new RegistrationGate();var parsed=Parse(gate,Field(0,4,0xDEADBEEFU),Field(1,0,Two,0x80000000U,0x7FC12345U),Field(1,1,Six));
  var names=Warm(new BinaryNameCacheEntry(MinAddress,0),new BinaryNameCacheEntry(MaxAddress,1));var table=Table(parsed,names,null);
  uint[] requests={0,1,2,3,uint.MaxValue};uint[] expected={Two,0x80000000U,0x7FC12345U,0x7FC12345U,0x7FC12345U};
  for(int i=0;i<requests.Length;i++){
   var result=Get(table,parsed,MinKey,requests[i]);
   Need(result.Returned&&!result.UsedDefault&&result.ValueBits==expected[i]&&result.Numeric.SelectedElementIndex==(i<3?(uint)i:2U)&&result.Numeric.ElementCount==3&&Released(result,gate)&&!result.MayHaveEffects,"type1 raw bits and unsigned clamp for index "+requests[i]);
  }
  var second=Get(table,parsed,MaxKey,0,Five,MaxAddress);
  Need(second.Returned&&second.ValueBits==Six&&second.Numeric.BufferWordIndex==3&&second.Numeric.Descriptor.Key==1,"second float descriptor uses its own cumulative type1 offset after a multivalue field");
  var export=parsed.CopyRawValues(1);export[0]=0;var again=Get(table,parsed);
  Need(again.ValueBits==Two&&Object.ReferenceEquals(again.Numeric.Descriptor,parsed.CopyDescriptors()[1]),"export mutation cannot change stored raw floats or descriptor identity");
 }

 static void ColdWarmAndStaleUseTheSameCache(){
  var gate=new RegistrationGate();var parsed=Pair(gate);var names=Empty(1);var table=Table(parsed,names,StandardMap());var cold=Get(table,parsed);var node=names.GetSlot(0);
  Need(cold.Returned&&cold.ValueBits==Two&&cold.Name.Cold&&cold.Name.NameTemporaryReleased&&cold.CacheNodeInserted&&cold.CacheCountWritten&&cold.MayHaveEffects&&names.UsedCount==1&&node.Address==MinAddress&&node.CachedId==0&&Released(cold,gate),"cold name resolution inserts then reaches actual float descriptor");
  var warm=Get(table,parsed);
  Need(warm.Returned&&!warm.Name.Cold&&!warm.Name.Stale&&!warm.MayHaveEffects&&Object.ReferenceEquals(warm.Name.PointerEntry,node)&&names.UsedCount==1,"repeat same address and content is warm without another slot");
  var stale=Get(table,parsed,MaxKey,0,Five,MinAddress);
  Need(stale.Returned&&stale.ValueBits==Six&&stale.Name.Stale&&stale.CacheIdWritten&&stale.CacheIdChanged&&!stale.CacheNodeInserted&&node.CachedId==1&&names.UsedCount==1&&Released(stale,gate),"same address changed to Max rewrites its ID before numeric float lookup");
  var grown=Get(table,parsed,MinKey,0,Three,MaxAddress);
  Need(grown.Returned&&grown.ValueBits==Two&&grown.CacheStorageGrown&&grown.CacheNodeInserted&&names.Capacity==2&&names.UsedCount==2&&!Object.ReferenceEquals(names.GetSlot(0),node)&&node.CachedId==1,"a second address grows full cache through the shared existing primitive, leaving old node detached");
 }

 static void MissingWrongTagAndDuplicateOrder(){
  var gate=new RegistrationGate();var parsed=Parse(gate,Field(1,0,Two),Field(0,0,0xCAFEBABEU),Field(1,1,Six));var names=Warm(new BinaryNameCacheEntry(MinAddress,0));var table=Table(parsed,names,null);var wrong=Get(table,parsed);
  Need(wrong.Returned&&wrong.UsedDefault&&wrong.ValueBits==Three&&wrong.Numeric.Field.Value.TypeTag==0&&Released(wrong,gate),"newer duplicate nonfloat shadows old float and returns exact default");
  var reverse=Parse(gate,Field(0,0,0xCAFEBABEU),Field(1,0,Four));var reverseTable=Table(reverse,names,null);var latest=Get(reverseTable,reverse);
  Need(latest.Returned&&!latest.UsedDefault&&latest.ValueBits==Four&&latest.Numeric.Descriptor.TypeTag==1,"reverse duplicate ordering selects the newest float through actual parsed chain");
  var empty=Empty(0);var missingTable=Table(parsed,empty,Map());var missing=Get(missingTable,null,"Missing",0,0x80000000U);
  Need(missing.Returned&&missing.UsedDefault&&missing.ValueBits==0x80000000U&&missing.Name.NameId==-1&&!missing.MayHaveEffects&&empty.UsedCount==0&&Released(missing,gate),"known name miss reaches numeric default without parsed payload or zero-capacity insertion");
  var signed=Parse(gate,Field(1,uint.MaxValue,Two),Field(1,0x80000001U,Four));var signedNames=Empty(2);var signedMap=Map(new BinaryNameIdEntry(B("MinusOne"),-1),new BinaryNameIdEntry(B("Negative"),unchecked((int)0x80000001U)));var signedTable=Table(signed,signedNames,signedMap);
  var minus=Get(signedTable,signed,"MinusOne",0,Five);var negative=Get(signedTable,signed,"Negative",0,Three,MaxAddress);
  Need(minus.Returned&&minus.UsedDefault&&minus.ValueBits==Five&&!minus.MayHaveEffects&&minus.Name.NameId==-1,"signed ID-1 cannot match parser's zero-extended uintFFFFFFFF key");
  Need(negative.Returned&&negative.UsedDefault&&negative.ValueBits==Three&&negative.CacheNodeInserted&&signedNames.GetSlot(0).CachedId==0xFFFFFFFF80000001UL,"negative int ID sign-extends for lookup and cold cache write, rather than matching uint parser key");
  var invalidWarm=Get(signedTable,signed,"Negative",0,Three,MaxAddress);
  Need(!invalidWarm.Returned&&invalidWarm.Reason=="CachedBinaryNameIndexOutsideCoveredDomain"&&!invalidWarm.MayHaveEffects&&Released(invalidWarm,gate),"next warm read must validate full cachedQWORD against shared pool before low32 ID conversion");
 }

 static void UnknownNumericStateKeepsNameEffects(){
  var gate=new RegistrationGate();var parsed=Pair(gate);var names=Empty(2);var table=Table(parsed,names,StandardMap());var missing=Get(table,null);
  Need(!missing.Returned&&missing.ValueBits==null&&missing.CacheNodeInserted&&missing.CacheCountWritten&&missing.MayHaveEffects&&names.UsedCount==1&&Released(missing,gate),"unknown parsed payload cannot erase a completed cold name insertion or invent default");
  var other=Pair(gate,Four,Five);var mismatch=Get(table,other,MaxKey,0,Five,MinAddress);
  Need(!mismatch.Returned&&mismatch.CacheIdWritten&&mismatch.CacheIdChanged&&mismatch.MayHaveEffects&&names.GetSlot(0).CachedId==1&&Released(mismatch,gate),"mismatched descriptor identity rejects after preserving stale ID rewrite");
  var otherGate=Pair(new RegistrationGate());var wrongGate=Get(table,otherGate,MaxKey,0,Five,MinAddress);
  Need(!wrongGate.Returned&&!wrongGate.MayHaveEffects&&Released(wrongGate,gate),"wrong parsed gate is not repaired by copying descriptors");
  var absent=new LoadTableBinarySnapshot(gate,false);var noArchive=BinaryFloatTableGetter.GetValue(absent,null,null,0x7FC12345U,null);
  Need(noArchive.Returned&&noArchive.UsedDefault&&noArchive.ValueBits==0x7FC12345U&&noArchive.Name==null&&!noArchive.MayHaveEffects&&Released(noArchive,gate),"absent archive returns default rawbits without touching query/cache/parser");
  var unknownArchive=BinaryFloatTableGetter.GetValue(new LoadTableBinarySnapshot(gate,null),parsed,C(MinKey),Three,MinAddress);
  Need(!unknownArchive.Returned&&unknownArchive.Name==null&&!unknownArchive.MayHaveEffects&&Released(unknownArchive,gate),"unknown archive presence is not known absence");
 }

 sealed class Window {
  public readonly RegistrationGate Gate;
  public readonly BinaryParsedRecord Parsed;
  public readonly BinaryPointerCacheSnapshot Names;
  public readonly LoadTableBinarySnapshot Binary;
  public readonly ItemGameEngineFields Fields;
  public readonly ItemFloatVtableObservation Vtable;
  public readonly ItemFloatTableObservation TableView;
  public readonly ItemGameEngineFloatLoadInputs Inputs;
  public readonly ItemGameEngineFloatLoadInvocation Invocation;
  public Window(uint min=Two,uint max=Six){
   Gate=new RegistrationGate();Parsed=Pair(Gate,min,max);Names=Empty(2);Binary=Table(Parsed,Names,StandardMap());Fields=new ItemGameEngineFields(GlobalToken,Fill(12));
   Vtable=new ItemFloatVtableObservation(0x910000UL,0x13FD50,0x1402E0);TableView=new ItemFloatTableObservation(0x900000UL,Vtable.Token,Vtable,Binary,Parsed);
   Inputs=ItemGameEngineFloatLoadInputs.WithArchivedInputs(MinAddress,MaxAddress);Invocation=new ItemGameEngineFloatLoadInvocation(Fields,TableView,Inputs);
  }
  public ItemGameEngineFloatLoadResult Run(){Invocation.DispatchFirst();Invocation.CompleteFirst();Invocation.DispatchSecond();return Invocation.CompleteSecond();}
 }
 sealed class Checks950:IItem950CheckSource {
  public Item950CheckObservation Check(ItemConstructorStorage state,int index,ulong target){if(index<0||index>=10||target!=0x180016710UL)throw new Exception("Unexpected950 prerequisite");return new Item950CheckObservation(true);}
 }
 sealed class ChecksBE0:IItemBE0CheckSource {
  int count;
  public ItemBE0CheckObservation Check(ItemConstructorStorage state,ItemBE0ArrayStorage array,int index,ulong target){if(index!=count++||target!=0x1800EBEB0UL)throw new Exception("UnexpectedBE0 prerequisite");return new ItemBE0CheckObservation(true);}
 }
 sealed class ItemFixture {
  public readonly ItemConstructorStorage State;
  public readonly ItemConstructorNameResult Prior;
  public ItemFixture(){
   var bytes=Fill(0x530);Put(bytes,0x2C,4,0x89ABCDEFUL);State=ItemConstructorStorage.CreateForBE0Continuation(0x500000UL,new ActorConstructorPrefixState(bytes),Fill(0x735));
   var matrix=new ActorMatrixInitializationSource(ActorMatrixInitializationState.CreateArchivedInitialState(),ActorMatrixThreadState.CreateArchivedTemplateThread(),true,true);
   var bridge=new ItemConstructorBridgeInvocation(State).RunWithArchivedActorProfile(new Actor198ArrayStorage(0x100000UL,Fill(0x208)),new ActorConstructorStorage(0x200000UL,Fill(0x358)),new ActorConstructorStorage(0x300000UL,Fill(0x38)),ActorCompletionObservations.WithArchivedConstants(null,null,null),matrix,new ActorEngineSlot(new ActorEngineContext(0x600000UL,9,9,7)));
   var p950=new ItemConstructor950Invocation(bridge).Run(new Checks950());var ba8=new ItemConstructorBA8Invocation(p950).RunWithArchivedWorldVec3Body();
   var be0=new ItemConstructorBE0Invocation(ba8,new ItemBE0ArrayStorage(0x700000UL,Fill(0x108))).Run(new ChecksBE0());var identity=new ItemConstructorIdentityInvocation(be0).Run();
   Prior=new ItemConstructorNameInvocation(identity,new ItemNameMemory(State)).Run();if(!Prior.ReachedItem323A49)throw new Exception("Real own Item prior incomplete");
  }
 }
 static void SharedFieldsFeedTheRealItemReturn(){
  var w=new Window();var item=new ItemFixture();var slot=new ItemGameEngineSlot(GlobalToken,w.Fields);var returned=new ItemConstructorReturnInvocation(item.Prior,slot,ItemReturnNumericInputs.WithArchivedScale(true));
  var itemExpected=Observe(item.State);var globalExpected=Observe(w.Fields);returned.Begin();
  Need(Object.ReferenceEquals(returned.Result.BorrowedFields,w.Fields)&&Same(w.Fields,globalExpected)&&!w.Invocation.Result.MayHaveEffects,"Item borrows the exact writable global backing before local load starts, without a copied snapshot");
  var replacementFields=new ItemGameEngineFields(0xE00000UL,Fill(12));var replacementExpected=Observe(replacementFields);slot.Publish(replacementFields.Token,replacementFields);
  var dispatched=w.Invocation.DispatchFirst();
  Need(dispatched.Status=="AwaitingInnerDispatch"&&dispatched.VptrReads==1&&dispatched.DefaultReads==1&&dispatched.Slot48Reads==1&&dispatched.GetterCalls==0&&Same(w.Fields,globalExpected),"outer dispatch captures the first operands without prematurely calling getter or writing fields");
  var first=w.Invocation.CompleteFirst();Put(globalExpected,0,4,Two);
  Need(first.First.Getter.Returned&&first.First.Getter.ValueBits==Two&&first.First.CallerStored&&first.CallerStoreInstructions==1&&first.DefaultReads==1&&first.Second.Getter==null&&Same(w.Fields,globalExpected),"first raw DWORD is visible before second default or getter is read");
  w.Invocation.DispatchSecond();var final=w.Invocation.CompleteSecond();Put(globalExpected,4,4,Six);
  Need(final.ReachedWindowEnd&&final.ResumeInstructionRva==0x2CEC6E&&final.Status=="ReachedWindowEnd"&&final.CallerStoreInstructions==2&&final.VptrReads==4&&final.DefaultReads==2&&final.Slot48Reads==2&&final.Slot98Reads==2&&final.GetterCalls==2&&Same(w.Fields,globalExpected),"two scalar calls each reread vptr and preserve the complete twelve-byte global oracle");
  Need(!final.IsCompleteLoadFromDatabase&&!final.IsCompleteGameEngineConstructor&&!final.IsCompleteItemConstructor&&Object.ReferenceEquals(final.State,w.Fields)&&Object.ReferenceEquals(final.Table,w.TableView)&&final.ThisToken==GlobalToken&&final.TableToken==w.TableView.Token,"local caller window does not manufacture complete database loading or global publication");
  returned.CopyFirstField();returned.CopySecondField();var waiting=returned.AdvanceToRandom();
  Need(waiting.SavedDivisor==4000&&waiting.AwaitingRandomObservation&&waiting.BorrowedFields==w.Fields,"actual ItemReturn reads the freshly loaded2/6 through the shared object");
  var completed=returned.CompleteWithRandomObservation(new ItemReturnRandomObservation(true,5));Put(itemExpected,0xB68,4,Two);Put(itemExpected,0xB6C,4,Six);Put(itemExpected,0xB64,4,2005);
  Need(completed.ConditionalNormalReturn&&completed.ReturnedItemToken==item.State.Token&&completed.StoredValue==2005&&Same(item.State,itemExpected)&&Same(w.Fields,globalExpected),"real own parser/getter/window/ItemReturn chain changes exactly the expected Item bytes");
  Need(Object.ReferenceEquals(slot.Fields,replacementFields)&&Object.ReferenceEquals(completed.BorrowedFields,w.Fields)&&Same(replacementFields,replacementExpected),"republishing the source slot after borrowing does not redirect local writes or the already borrowed Item reads");
 }
 static void DispatchCapturesOperandsButRereadsInnerState(){
  var w=new Window();var capturedKey=w.Inputs.FirstKey;var oldTable=w.TableView.BinaryTable;w.Invocation.DispatchFirst();
  // This replacement would fail as a new outer target, but the already
  // selected13FD50 correctly reads only its current+98 target next.
  var replacement=new ItemFloatVtableObservation(0x920000UL,0x13CA00,0x1402E0);w.TableView.Vptr=replacement.Token;w.TableView.Vtable=replacement;
  var later=Pair(w.Gate,Four,Five);w.TableView.ParsedRecord=later;w.TableView.BinaryTable=Table(later,w.Names,StandardMap());
  capturedKey.ReplaceCString(C(MaxKey));w.Inputs.FirstKey=new ItemFloatQueryObservation(0x800088UL,C("Other"));w.Inputs.FirstDefaultBits=0xDEADBEEFU;
  var first=w.Invocation.CompleteFirst();
  Need(first.First.Getter.Returned&&first.First.Getter.ValueBits==Five&&first.First.DefaultBits==Three&&first.First.QueryAddress==MinAddress&&Object.ReferenceEquals(first.First.Key,capturedKey),"captured address/default survive input replacement while borrowed key bytes remain live");
  Need(Object.ReferenceEquals(first.First.OuterVtable,w.Vtable)&&Object.ReferenceEquals(first.First.InnerVtable,replacement)&&first.First.OuterTargetRva==0x13FD50&&first.First.InnerTargetRva==0x1402E0&&Object.ReferenceEquals(first.First.Getter.Table,w.TableView.BinaryTable)&&!Object.ReferenceEquals(oldTable,w.TableView.BinaryTable),"inner vptr and binary context are resampled at the real scalar-tail dispatch");
  var before=Observe(w.Fields);w.Inputs.SecondDefaultBits=null;var second=w.Invocation.DispatchSecond();
  Need(second.Status=="Unsupported"&&second.ResumeInstructionRva==0x2CEC50&&second.Reason=="UnknownCallerFloatDefaultBits"&&second.CallerStoreInstructions==1&&second.Slot48Reads==1&&second.GetterCalls==1&&Same(w.Fields,before),"second default is read after first store and before unsupported replacement outer slot");
  var absent=new Window();absent.TableView.BinaryTable=new LoadTableBinarySnapshot(absent.Gate,false);absent.TableView.ParsedRecord=null;
  absent.Invocation.DispatchFirst();absent.Inputs.FirstDefaultBits=Two;absent.Inputs.FirstKey.Invalidate();var absentFirst=absent.Invocation.CompleteFirst();
  Need(absentFirst.First.Getter.Returned&&absentFirst.First.Getter.UsedDefault&&absentFirst.First.Getter.ValueBits==Three&&absentFirst.First.Getter.Name==null,"captured first default and absent-archive shortcut do not depend on invalidated key bytes");
  absent.Inputs.SecondDefaultBits=0x7FC12345U;absent.Inputs.SecondKey=null;absent.Invocation.DispatchSecond();absent.Inputs.SecondDefaultBits=Five;var absentFinal=absent.Invocation.CompleteSecond();
  Need(absentFinal.ReachedWindowEnd&&absentFinal.Second.Getter.ValueBits==0x7FC12345U&&absentFinal.Second.DefaultBits==0x7FC12345U&&absent.Fields.ReadByte(4)==0x45&&absent.Fields.ReadByte(7)==0x7F,"second current default is captured independently and raw NaN payload survives later default changes");
 }
 static void StopsKeepCacheAndCallerPrefixes(){
  var w=new Window();var before=Observe(w.Fields);w.TableView.ParsedRecord=null;w.Invocation.DispatchFirst();var unknown=w.Invocation.CompleteFirst();
  Need(unknown.Status=="Unsupported"&&unknown.Reason=="MissingParsedFloatRecord"&&unknown.ResumeInstructionRva==0x140473&&unknown.First.Getter.CacheNodeInserted&&unknown.CallerStoreInstructions==0&&unknown.MayHaveEffects&&Same(w.Fields,before)&&w.Names.UsedCount==1,"first numeric frontier retains name insertion but no invented global default or caller store");
  w.TableView.ParsedRecord=w.Parsed;
  Need(Invalid(delegate{w.Invocation.CompleteFirst();})&&Invalid(delegate{w.Invocation.DispatchSecond();})&&w.Names.UsedCount==1&&Same(w.Fields,before),"repairing observations cannot silently retry a stopped effectful attempt");
  var second=new Window();second.Invocation.DispatchFirst();second.Invocation.CompleteFirst();var firstOnly=Observe(second.Fields);second.TableView.ParsedRecord=Pair(second.Gate,Four,Five);second.Invocation.DispatchSecond();var stopped=second.Invocation.CompleteSecond();
  Need(stopped.Reason=="ParsedFloatRecordFieldsMismatch"&&stopped.CallerStoreInstructions==1&&stopped.First.CallerStored&&!stopped.Second.CallerStored&&stopped.Second.Getter.CacheNodeInserted&&second.Names.UsedCount==2&&stopped.MayHaveEffects&&Same(second.Fields,firstOnly),"second mismatch preserves first DWORD and its own second-name insertion");
  var invalidTarget=new Window();invalidTarget.Invocation.DispatchFirst();invalidTarget.Fields.Invalidate();var targetStop=invalidTarget.Invocation.CompleteFirst();
  Need(targetStop.Reason=="InvalidatedGameEngineFields"&&targetStop.ResumeModule=="Game"&&targetStop.ResumeInstructionRva==0x2CEC44&&targetStop.First.Getter.Returned&&targetStop.First.Getter.CacheNodeInserted&&targetStop.CallerStoreInstructions==0&&targetStop.MayHaveEffects,"destination lifetime is checked at the actual post-getter store, preserving prior cache effects");
  var invalidTable=new Window();invalidTable.Invocation.DispatchFirst();invalidTable.Invocation.CompleteFirst();var saved=Observe(invalidTable.Fields);invalidTable.TableView.Invalidate();var invalid=invalidTable.Invocation.DispatchSecond();
  Need(invalid.Reason=="InvalidatedFloatTableObject"&&invalid.ResumeInstructionRva==0x2CEC4D&&invalid.GetterCalls==1&&invalid.CallerStoreInstructions==1&&Same(invalidTable.Fields,saved),"lost second table lifetime cannot roll back the completed first field");
 }
 static void RawBitsRemainRawBeforeItemNumericCoverage(){
  var w=new Window(0x80000000U,0x7FC12345U);var expected=Observe(w.Fields);Put(expected,0,4,0x80000000U);Put(expected,4,4,0x7FC12345U);var result=w.Run();
  Need(result.ReachedWindowEnd&&result.First.Getter.ValueBits==0x80000000U&&result.Second.Getter.ValueBits==0x7FC12345U&&Same(w.Fields,expected),"MOVSS caller writes preserve signed zero and noncanonical NaN payload without host float conversion");
  var item=new ItemFixture();var returned=new ItemConstructorReturnInvocation(item.Prior,new ItemGameEngineSlot(GlobalToken,w.Fields),ItemReturnNumericInputs.WithArchivedScale(true));var stopped=returned.RunToRandom();
  Need(stopped.Status=="Unsupported"&&stopped.ResumeInstructionRva==0x324578&&stopped.CallerStoreInstructions==2&&stopped.DependencyStoreInstructions==0&&item.State.ReadLittleEndian(0xB68,4)==0x80000000UL&&item.State.ReadLittleEndian(0xB6C,4)==0x7FC12345UL,"downstream exact FP coverage rejects NaN only after retaining both raw field copies");
 }
 static void UnknownDispatchAndOwnPhaseGuards(){
  var w=new Window();var before=Observe(w.Fields);
  Need(Invalid(delegate{w.Invocation.CompleteFirst();})&&Invalid(delegate{w.Invocation.DispatchSecond();})&&w.Invocation.Result.GetterCalls==0&&Same(w.Fields,before),"out-of-order inspection steps cannot bypass outer dispatch");
  w.TableView.Vptr=null;var unknown=w.Invocation.DispatchFirst();
  Need(unknown.Reason=="UnknownFloatTableVptr"&&unknown.ResumeInstructionRva==0x2CEC27&&unknown.DefaultReads==0&&unknown.CallerStoreInstructions==0&&!unknown.MayHaveEffects&&Same(w.Fields,before),"unknown vptr stops before default selection rather than inventing the Binary type");
  var zero=new Window();zero.TableView.Vptr=0;var nullPointer=zero.Invocation.DispatchFirst();
  Need(nullPointer.Reason=="NullFloatTableVptr"&&nullPointer.DefaultReads==1&&nullPointer.ResumeInstructionRva==0x2CEC41&&nullPointer.GetterCalls==0,"known null vptr does not skip the intervening scalar read before slot access");
  var wrong=new Window();wrong.Vtable.Slot48TargetRva=0x13CA00;var ordinary=wrong.Invocation.DispatchFirst();
  Need(ordinary.Reason=="UnsupportedOuterFloatTarget"&&ordinary.GetterCalls==0&&ordinary.First.OuterTargetRva==0x13CA00&&!ordinary.MayHaveEffects,"observed ordinary getter target cannot be substituted by the Binary implementation");
  var inner=new Window();inner.Invocation.DispatchFirst();inner.Vtable.Slot98TargetRva=null;var missing=inner.Invocation.CompleteFirst();
  Need(missing.Reason=="UnknownInnerFloatTarget"&&missing.ResumeModule=="Engine"&&missing.ResumeInstructionRva==0x13FD59&&missing.GetterCalls==0,"unknown second virtual slot remains an inner dispatch frontier");
  var completed=new Window();completed.Run();var saved=Observe(completed.Fields);
  Need(Invalid(delegate{completed.Invocation.DispatchFirst();})&&Invalid(delegate{completed.Invocation.CompleteSecond();})&&Same(completed.Fields,saved)&&completed.Names.UsedCount==2,"completed own invocation is not replayable or implicitly republished");
 }
 static void KnownAliasViewsStopWithoutInventingNativeBranches(){
  ulong first=GlobalToken+(ulong)ItemGameEngineFields.FirstOffset;
  var tableCase=new Window();var before=Observe(tableCase.Fields);
  var overlappingTable=new ItemFloatTableObservation(first-0xCFUL,tableCase.Vtable.Token,tableCase.Vtable,tableCase.Binary,tableCase.Parsed);
  var tableAttempt=new ItemGameEngineFloatLoadInvocation(tableCase.Fields,overlappingTable,tableCase.Inputs);var tableStop=tableAttempt.DispatchFirst();
  Need(tableStop.Reason=="AliasedGlobalAndTableViewsOutsideCoverage"&&tableStop.ResumeInstructionRva==0x2CEC27&&tableStop.VptrReads==0&&tableStop.GetterCalls==0&&!tableStop.MayHaveEffects&&Same(tableCase.Fields,before),"even a one-byte table/global overlap is an own coverage stop before consuming a fictitious separate vptr view");
  var vtableCase=new Window();var overlappingVtable=new ItemFloatVtableObservation(first-0x9FUL,0x13FD50,0x1402E0);vtableCase.TableView.Vptr=overlappingVtable.Token;vtableCase.TableView.Vtable=overlappingVtable;var vtableStop=vtableCase.Invocation.DispatchFirst();
  Need(vtableStop.Reason=="AliasedGlobalAndVtableViewsOutsideCoverage"&&vtableStop.ResumeInstructionRva==0x2CEC41&&vtableStop.DefaultReads==1&&vtableStop.Slot48Reads==0&&!vtableStop.MayHaveEffects,"vtable overlap is rejected at its slot access after the already observed default, not translated into native alias behavior");
  var queryCase=new Window();queryCase.Invocation.DispatchFirst();queryCase.Invocation.CompleteFirst();var firstOnly=Observe(queryCase.Fields);queryCase.Inputs.SecondKey=new ItemFloatQueryObservation(first+11UL,C(MaxKey));var queryStop=queryCase.Invocation.DispatchSecond();
  Need(queryStop.Reason=="AliasedGlobalAndQueryViewsOutsideCoverage"&&queryStop.ResumeInstructionRva==0x2CEC4D&&queryStop.CallerStoreInstructions==1&&queryStop.GetterCalls==1&&queryStop.MayHaveEffects&&Same(queryCase.Fields,firstOnly)&&queryCase.Names.UsedCount==1,"a later known query/global overlap preserves the first field and cache insertion without running the second getter");
 }
 public static int Main(){
  Group("ParsedRawFloatsAndUnsignedClamp",ParsedRawFloatsAndUnsignedClamp);Group("ColdWarmAndStaleUseTheSameCache",ColdWarmAndStaleUseTheSameCache);
  Group("MissingWrongTagAndDuplicateOrder",MissingWrongTagAndDuplicateOrder);Group("UnknownNumericStateKeepsNameEffects",UnknownNumericStateKeepsNameEffects);
  Group("SharedFieldsFeedTheRealItemReturn",SharedFieldsFeedTheRealItemReturn);Group("DispatchCapturesOperandsButRereadsInnerState",DispatchCapturesOperandsButRereadsInnerState);
  Group("StopsKeepCacheAndCallerPrefixes",StopsKeepCacheAndCallerPrefixes);Group("RawBitsRemainRawBeforeItemNumericCoverage",RawBitsRemainRawBeforeItemNumericCoverage);
  Group("UnknownDispatchAndOwnPhaseGuards",UnknownDispatchAndOwnPhaseGuards);
  Group("KnownAliasViewsStopWithoutInventingNativeBranches",KnownAliasViewsStopWithoutInventingNativeBranches);
  Console.WriteLine("ItemGameEngineFloatLoadTests: "+assertions+" assertions, "+groups+" groups passed; own Binary getter and local two-store window only");return 0;
 }
}
