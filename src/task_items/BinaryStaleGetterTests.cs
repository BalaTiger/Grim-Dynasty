using System;
using System.Text;
using GrimDynasty.TaskItems;
class BinaryStaleGetterTests {
 static int n;
 const ulong Address=0x1008;
 static void Need(bool value,string message){n++;if(!value)throw new Exception(message);}
 static byte[] B(string value){return Encoding.ASCII.GetBytes(value);}
 static byte[] C(string value){return B(value+"\0");}
 static TableString S(string value){return new TableString(B(value));}
 static bool Same(byte[] left,byte[] right){if(left==null||right==null)return left==right;if(left.Length!=right.Length)return false;for(int i=0;i<left.Length;i++)if(left[i]!=right[i])return false;return true;}
 static bool Rejected(Action action){try{action();return false;}catch(ArgumentException){return true;}}
 static BinaryPointerCacheSnapshot Names(params BinaryNameCacheEntry[] entries){return new BinaryPointerCacheSnapshot(0,0,new[]{entries});}
 static BinaryFieldSnapshot Fields(params BinaryFieldEntry[] entries){return new BinaryFieldSnapshot(0,0,new[]{entries});}
 static BinaryFieldEntry Field(ulong key,int tag=2,uint slot=0){return new BinaryFieldEntry(key,new BinaryFieldValue(tag,slot));}
 static BinaryNameIdEntry Entry(string key,int id){return new BinaryNameIdEntry(B(key),id);}
 static BinaryNameIdSnapshot Map(params BinaryNameIdEntry[] entries){return new BinaryNameIdSnapshot(1,entries);}
 static LoadTableBinarySnapshot Table(BinaryPointerCacheSnapshot names,TableString[] pool,BinaryFieldSnapshot fields,int[] buffer,BinaryNameIdSnapshot map){return new LoadTableBinarySnapshot(new RegistrationGate(),true,new BinaryWarmState(names,pool,fields,buffer,map));}
 static RecordTableGetterResult Get(LoadTableBinarySnapshot table,byte[] query,TableString fallback,ulong? address=Address){return RecordTableGetter.GetValue(table,query,fallback,address);}
 static bool Released(RecordTableGetterResult result,RecordTableSnapshot table){return result.GateAcquired&&result.GateReleased&&!table.Gate.IsHeldByCurrentThread;}
 static void StaleThenWarm(){
  var pointer=new BinaryNameCacheEntry(Address,0);var mapping=Entry("New",1);var value=S("value");var field=Field(1);var pool=new[]{S("Old"),S("New"),value};var names=Names(pointer);var fields=Fields(field);var table=Table(names,pool,fields,new[]{2},Map(mapping));byte[] query=C("New");var before=(byte[])query.Clone();
  var result=Get(table,query,S("default"));
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Value,value)&&!result.UsedDefault&&Object.ReferenceEquals(result.Binary.PointerEntry,pointer)&&Object.ReferenceEquals(result.Binary.FieldEntry,field)&&Object.ReferenceEquals(result.Binary.NameLookup.Entry,mapping)&&result.Binary.NameLookup.Found&&result.Binary.NameId==1&&result.Binary.StringIndex==2&&Released(result,table),"stale name lookup updates and feeds the actual numeric getter using the supplied typed ID");
  Need(result.Binary.Stale&&result.KeyLength==3&&result.LocalKeyCapacity==15&&result.Binary.LocalNameCapacity==15&&result.Binary.NameTemporaryReleased&&result.Binary.CacheIdWritten&&result.Binary.CacheIdChanged&&result.Binary.PreviousCachedId==0&&result.Binary.WrittenCachedId==1&&pointer.CachedId==1&&result.CacheIdWritten&&result.CacheIdChanged&&result.MayHaveEffects&&result.Binary.MayHaveEffects&&Same(query,before),"the released temporary and completed qword write are reported through both result layers without changing the query");
  // Reuse the exact mutable node under the same gate; the second supplied
  // state omits name lookup because the first operation already made it warm.
  var warm=new LoadTableBinarySnapshot(table.Gate,true,new BinaryWarmState(names,pool,fields,new[]{2}));var again=Get(warm,C("New"),null);
  Need(again.Status=="Known"&&Object.ReferenceEquals(again.Value,value)&&Object.ReferenceEquals(again.Binary.PointerEntry,pointer)&&!again.Binary.Stale&&again.Binary.NameLookup==null&&!again.Binary.NameTemporaryReleased&&!again.LocalKeyCapacity.HasValue&&!again.CacheIdWritten&&!again.CacheIdChanged&&!again.MayHaveEffects&&pointer.CachedId==1&&Released(again,warm),"a subsequent call becomes warm without a name-ID snapshot and keeps the same cache-node identity");
  var sameId=new BinaryNameCacheEntry(Address,0);table=Table(Names(sameId),new[]{S("Old"),value},Fields(Field(0)),new[]{1},Map(Entry("New",0)));result=Get(table,C("New"),null);
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Value,value)&&result.CacheIdWritten&&!result.CacheIdChanged&&result.MayHaveEffects&&result.Binary.PreviousCachedId==0&&result.Binary.WrittenCachedId==0&&sameId.CachedId==0&&result.Binary.NameTemporaryReleased,"an executed store of the same ID is reported as written with effects, distinct from a changed ID");
 }
 static void MinusOneAndSignedIds(){
  var fallback=S("default");var value=S("minus-one-value");
  foreach(bool explicitMinusOne in new[]{false,true}){
   var pointer=new BinaryNameCacheEntry(Address,0);var entry=Entry("New",-1);var map=explicitMinusOne?Map(entry):Map();var minusOne=Field(ulong.MaxValue);var old=Field(0,1);var table=Table(Names(pointer),new[]{S("Old"),value},Fields(old,minusOne),new[]{1},map);var result=Get(table,C("New"),fallback);
   Need(result.Status=="Known"&&!result.UsedDefault&&Object.ReferenceEquals(result.Value,value)&&Object.ReferenceEquals(result.Binary.FieldEntry,minusOne)&&result.Binary.NameId==-1&&result.Binary.NameLookup.Found==explicitMinusOne&&Object.ReferenceEquals(result.Binary.NameLookup.Entry,explicitMinusOne?entry:null)&&result.Binary.NameTemporaryReleased&&pointer.CachedId==0&&!result.CacheIdWritten&&!result.MayHaveEffects&&Released(result,table),"name "+(explicitMinusOne?"entry ID -1":"miss")+" still enters numeric with signed -1 and leaves the previous cache ID untouched");
  }
  var absentPointer=new BinaryNameCacheEntry(Address,0);var absentTable=Table(Names(absentPointer),new[]{S("Old")},Fields(),null,Map());var absent=Get(absentTable,C("New"),fallback);
  Need(absent.Status=="Known"&&absent.UsedDefault&&Object.ReferenceEquals(absent.Value,fallback)&&absent.Binary.NameId==-1&&absent.Binary.FieldEntry==null&&absent.Binary.NameTemporaryReleased&&!absent.MayHaveEffects&&absentPointer.CachedId==0,"a name miss returns default only after numeric itself misses, not by bypassing that getter");
  const int negative=unchecked((int)0x80000001);const ulong signed=0xffffffff80000001;var pointerNegative=new BinaryNameCacheEntry(Address,0);var wanted=Field(signed);var tableNegative=Table(Names(pointerNegative),new[]{S("Old"),value},Fields(Field(0x80000001,1),wanted),new[]{1},Map(Entry("New",negative)));var negativeResult=Get(tableNegative,C("New"),fallback);
  Need(negativeResult.Status=="Known"&&Object.ReferenceEquals(negativeResult.Value,value)&&Object.ReferenceEquals(negativeResult.Binary.FieldEntry,wanted)&&negativeResult.Binary.NameId==negative&&pointerNegative.CachedId==signed&&negativeResult.Binary.WrittenCachedId==signed&&negativeResult.CacheIdChanged&&negativeResult.MayHaveEffects&&Released(negativeResult,tableNegative),"non--1 negative IDs are sign-extended into the entire cache qword and the numeric key before lookup");
  var next=Get(tableNegative,C("New"),fallback);
  Need(next.Status=="Unsupported"&&next.Reason=="CachedBinaryNameIndexOutsideCoveredDomain"&&next.FailureStage=="CachedNameRead"&&!next.CacheIdWritten&&!next.MayHaveEffects&&pointerNegative.CachedId==signed&&next.Binary.NameLookup==null&&Released(next,tableNegative),"only the following call rejects the now-out-of-pool cached name ID; the completed previous write is not rolled back");
 }
 static void PrefixAndFirstMatch(){
  byte[] raw={0x80,0xff},query={0x80,0xff,0,0x41};var first=new BinaryNameCacheEntry(Address,0);var later=new BinaryNameCacheEntry(Address,2);var full=new BinaryNameIdEntry(new byte[]{0x80,0xff,0,0x41},-2);var match=new BinaryNameIdEntry(raw,1);var duplicate=new BinaryNameIdEntry(raw,2);var value=S("raw-value");
  var table=Table(Names(first,later),new[]{S("Old"),new TableString(raw),value},Fields(Field(1)),new[]{2},Map(full,match,duplicate));var result=Get(table,query,null);
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Value,value)&&Object.ReferenceEquals(result.Binary.PointerEntry,first)&&Object.ReferenceEquals(result.Binary.NameLookup.Entry,match)&&result.Binary.NameLookup.Visited==2&&result.Binary.NameLookup.NodeIndex==1&&result.KeyLength==2&&first.CachedId==1&&later.CachedId==2&&Released(result,table),"stale lookup hashes the raw first-NUL prefix, retains complete stored-key length and chooses the first matching name node and pointer node");
  var next=Get(table,new byte[]{0x80,0xff,0,0x42},null);
  Need(next.Status=="Known"&&!next.Binary.Stale&&!next.MayHaveEffects&&next.Binary.NameLookup==null&&Object.ReferenceEquals(next.Value,value)&&first.CachedId==1&&later.CachedId==2,"the corrected high-byte name is warm and ignores only the unread suffix after equal NUL");
  var pointer=new BinaryNameCacheEntry(Address,0);table=Table(Names(pointer),new[]{S("Old")},Fields(),null,Map(Entry("new/path",1)));result=Get(table,C("New\\Path"),null);
  Need(result.Status=="Known"&&result.UsedDefault&&result.Value==null&&!result.Binary.NameLookup.Found&&result.Binary.NameId==-1&&!result.MayHaveEffects&&pointer.CachedId==0,"stale field-name lookup performs neither case folding nor slash normalization");
  pointer=new BinaryNameCacheEntry(Address,0);var a=Entry("A",1);var c=Entry("C",2);var b=Entry("B",3);table=Table(Names(pointer),new[]{S("Old")},Fields(),null,new BinaryNameIdSnapshot(2,new[]{a,c,b}));result=Get(table,C("B"),null);
  Need(result.Status=="Known"&&result.UsedDefault&&Object.ReferenceEquals(result.Binary.NameLookup.Entry,b)&&result.Binary.NameLookup.Bucket==1&&result.Binary.NameLookup.Visited==1&&result.Binary.NameLookup.NodeIndex==2&&pointer.CachedId==3,"name lookup starts at the selected bucket segment rather than walking unrelated list nodes");
  foreach(int length in new[]{15,16}){
   string text=new string('x',length);pointer=new BinaryNameCacheEntry(Address,0);table=Table(Names(pointer),new[]{S("Old"),S(text),value},Fields(Field(1)),new[]{2},Map(Entry(text,1)));result=Get(table,C(text),null);
   Need(result.Status=="Known"&&Object.ReferenceEquals(result.Value,value)&&result.KeyLength==length&&result.LocalKeyCapacity==(length==15?15UL:31UL)&&result.Binary.NameTemporaryReleased&&result.CacheIdWritten&&Released(result,table),"stale temporary uses its own inline/heap capacity at length "+length);
  }
 }
 static void UnsupportedPrefixes(){
  var pointer=new BinaryNameCacheEntry(Address,0);var mapping=Entry("New",1);var pool=new[]{S("Old"),S("New"),S("value")};var table=Table(Names(pointer),pool,null,null,Map(mapping));var result=Get(table,C("New"),null);
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownBinaryFieldSnapshot"&&result.LastCompletedStage=="CacheIdWritten"&&result.FailureStage=="NumericFieldLookup"&&result.CacheIdWritten&&result.CacheIdChanged&&result.MayHaveEffects&&result.Binary.MayHaveEffects&&result.Binary.NameTemporaryReleased&&pointer.CachedId==1&&Released(result,table),"unknown numeric state is checked after temporary release and cache mutation, preserving both the write and its effect flags");
  pointer=new BinaryNameCacheEntry(Address,0);table=Table(Names(pointer),pool,Fields(Field(1,2,9)),new int[0],Map(mapping));result=Get(table,C("New"),null);
  Need(result.Status=="Unsupported"&&result.Reason=="BinaryValueBufferReadOutsideCoveredDomain"&&result.FailureStage=="ValueBufferRead"&&result.CacheIdWritten&&result.MayHaveEffects&&result.Binary.FieldEntry.Key==1&&pointer.CachedId==1&&Released(result,table),"a later unchecked buffer boundary also preserves the earlier cache write");
  pointer=new BinaryNameCacheEntry(Address,0);table=Table(Names(pointer),pool,Fields(Field(1)),new[]{-1},Map(mapping));result=Get(table,C("New"),S("default"));
  Need(result.Status=="Known"&&result.Value==null&&!result.UsedDefault&&result.Binary.StringIndex==-1&&result.CacheIdWritten&&result.MayHaveEffects&&pointer.CachedId==1&&Released(result,table),"a native known-null string result does not erase a successful stale cache update");
  pointer=new BinaryNameCacheEntry(Address,0);table=Table(Names(pointer),pool,Fields(Field(1)),new[]{2},Map(mapping));result=Get(table,B("New"),null);
  Need(result.Status=="Unsupported"&&result.Reason=="BinaryStaleQueryReadOutsideCoveredDomain"&&result.FailureStage=="StaleQueryCStringRead"&&result.Binary.Stale&&result.Binary.NameLookup==null&&!result.Binary.NameTemporaryReleased&&!result.CacheIdWritten&&!result.MayHaveEffects&&pointer.CachedId==0&&Released(result,table),"an early mismatch still requires a complete C-string before stale lookup or mutation");
  result=Get(table,C("New"),null,Address+16);
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownBinaryPointerCacheInsertionStorage"&&result.Binary.PointerEntry==null&&Object.ReferenceEquals(result.Binary.NameLookup.Entry,mapping)&&result.Binary.Cold&&result.Binary.NameTemporaryReleased&&!result.Binary.Stale&&!result.CacheIdWritten&&!result.CacheNodeInserted&&!result.MayHaveEffects&&pointer.CachedId==0&&Released(result,table),"a known cold name completes lookup and release, then stops without inventing insertion metadata or turning another address into the old node");
  table=Table(Names(pointer),pool,Fields(Field(1)),new[]{2},null);result=Get(table,C("New"),null);
  Need(result.Status=="Unsupported"&&result.Reason=="UnimplementedBinaryStalePointerLookup"&&result.Binary.Stale&&result.Binary.NameLookup==null&&!result.CacheIdWritten&&!result.MayHaveEffects&&pointer.CachedId==0&&Released(result,table),"the existing no-name-map overload keeps its explicit stale boundary and performs no write");
 }
 static void TypedSnapshotCopies(){
  byte[] key=B("New");var entry=new BinaryNameIdEntry(key,1);var list=new[]{entry};var map=new BinaryNameIdSnapshot(1,list);key[0]=0;list[0]=Entry("Other",2);byte[] copy=entry.CopyKey();copy[0]=0;
  var pointer=new BinaryNameCacheEntry(Address,0);var value=S("value");var table=Table(Names(pointer),new[]{S("Old"),S("New"),value},Fields(Field(1)),new[]{2},map);var result=Get(table,C("New"),null);
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Value,value)&&Object.ReferenceEquals(result.Binary.NameLookup.Entry,entry)&&entry.Id==1&&Same(entry.CopyKey(),B("New"))&&pointer.CachedId==1,"typed name-ID snapshots copy mutable key and list inputs while retaining the exact immutable ID entry");
  Need(Rejected(delegate{new BinaryNameIdEntry(null,1);})&&Rejected(delegate{new BinaryNameIdSnapshot(1,null);})&&Rejected(delegate{Map(new BinaryNameIdEntry[]{null});}),"unknown key, list or node storage cannot stand in for a complete name-ID snapshot");
  Need(Rejected(delegate{new BinaryNameIdSnapshot(3,new BinaryNameIdEntry[0]);})&&Rejected(delegate{new BinaryNameIdSnapshot(2,new[]{Entry("A",1),Entry("B",2),Entry("C",3)});}),"name-ID snapshots enforce the explicit power-of-two/mask coverage contract and contiguous bucket segments");
  var large=new BinaryNameIdEntry(new byte[65536],1);var overBudget=new BinaryNameIdEntry[17];for(int i=0;i<overBudget.Length;i++)overBudget[i]=large;
  Need(Rejected(delegate{new BinaryNameIdSnapshot(1,overBudget);}),"the aggregate key budget counts repeated entry slots and rejects 17 shared 65536-byte keys");
 }
 static ObjectNameString Name(byte[] bytes){return new ObjectNameString(bytes,Math.Max(15UL,(ulong)bytes.Length));}
 static void CreateScenario(bool numericKnown){
  byte[] originalBytes=B("Records\\MiXeD\\Entry\0TAIL/AbC.dbr"),normalized=B("records/mixed/entry\0tail/abc.dbr");var pointer=new BinaryNameCacheEntry(Address,0);var classValue=S("Own\0Ignored");var mapEntry=Entry("Class",1);var table=Table(Names(pointer),new[]{S("Old"),S("Class"),classValue},numericKnown?Fields(Field(1)):null,numericKnown?new[]{2}:null,Map(mapEntry));
  var depot=new RecordDepotState(new RegistrationGate(),new RecordCacheSnapshot(1,new[]{new RecordCacheEntry(normalized,table)}),0);int constructors=0;bool unlocked=false,sawWrite=false;ObjectBaseState instance=null;
  var classes=new ClassInfoSnapshot(C("Own"),null,null,delegate{constructors++;unlocked=!table.Gate.IsHeldByCurrentThread&&!depot.Gate.IsHeldByCurrentThread;sawWrite=pointer.CachedId==1;instance=ObjectBaseConstructor.Create();return instance;});
  var created=CreatePartialObject.Apply(depot,Name(originalBytes),RecordNameLocale.KnownC,classes,Address);
  Need(created.Load.Status=="Known"&&created.Load.HitCountIncremented&&depot.HitCount54==1&&Object.ReferenceEquals(created.Load.Table,table)&&Same(created.Load.CopyNormalizedName(),normalized)&&!depot.Gate.IsHeldByCurrentThread,"CreatePartial first completes the actual normalized cache hit before entering stale Class lookup");
  Need(created.Getter.Binary.Stale&&Object.ReferenceEquals(created.Getter.Binary.NameLookup.Entry,mapEntry)&&created.Getter.Binary.NameTemporaryReleased&&created.Getter.Binary.PreviousCachedId==0&&created.Getter.Binary.WrittenCachedId==1&&pointer.CachedId==1&&created.Getter.CacheIdWritten&&created.Getter.MayHaveEffects&&created.MayHaveEffects&&created.Getter.GateReleased&&!table.Gate.IsHeldByCurrentThread,"the real caller retains completed cache-write effects and releases the table gate on either later outcome");
  if(numericKnown){
   Need(created.Status=="Known"&&created.LastCompletedStage=="NameAssigned"&&created.Getter.Status=="Known"&&Object.ReferenceEquals(created.Getter.Value,classValue)&&!created.Getter.UsedDefault&&created.Factory.CallbacksInvoked==1&&constructors==1&&unlocked&&sawWrite&&Object.ReferenceEquals(created.Instance,instance),"stale Class reaches its numeric value, then the first-NUL class dispatch invokes an own constructor outside both gates after the update");
   Need(created.NameAssignment.Status=="Known"&&instance.NameLength==32&&instance.NameCapacity==47&&instance.Fields.RawKey28==0xb0853751&&!instance.Fields.Id2C.HasValue&&Same(instance.CopyNameBytes(),originalBytes)&&!created.DiagnosticRequired,"the caller assigns the complete original NUL-containing name without allocating or registering an object ID");
   var warm=Get(table,C("Class"),null);Need(warm.Status=="Known"&&!warm.Binary.Stale&&warm.Binary.NameLookup==null&&!warm.MayHaveEffects&&Object.ReferenceEquals(warm.Value,classValue)&&pointer.CachedId==1&&constructors==1&&depot.HitCount54==1,"a later actual getter is warm and adds no construction or depot-load side effects");
  }else{
   Need(created.Status=="Unsupported"&&created.Reason=="ClassGetterUnsupported:UnknownBinaryFieldSnapshot"&&created.FailureStage=="ClassGetter"&&created.LastCompletedStage=="LoadFileComplete"&&created.Getter.LastCompletedStage=="CacheIdWritten"&&created.Factory==null&&created.NameAssignment==null&&created.Instance==null&&constructors==0&&!created.DiagnosticRequired,"unknown numeric metadata stops CreatePartial after the cache write, without invoking a factory or masking the effect as a no-op");
  }
 }
 static int Main(){try{StaleThenWarm();MinusOneAndSignedIds();PrefixAndFirstMatch();UnsupportedPrefixes();TypedSnapshotCopies();CreateScenario(true);CreateScenario(false);Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+n+",\"scenarios\":2,\"scope\":\"Own complete typed Binary stale name lookup, cache-ID writes and numeric/CreatePartial composition; no cold insertion, parsing, native allocation ABI, allocator connection or original execution.\"}");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
