using System;
using System.Text;
using GrimDynasty.TaskItems;
class BinaryWarmGetterTests {
 static int n;
 const ulong Address=0x1008;
 const uint Id=77,FullNameKey=0xb0853751;
 static void Need(bool value,string message){n++;if(!value)throw new Exception(message);}
 static byte[] B(string value){return Encoding.ASCII.GetBytes(value);}
 static byte[] C(string value){return B(value+"\0");}
 static TableString S(string value){return new TableString(B(value));}
 static byte[] Repeated(int count,byte value){var bytes=new byte[count];for(int i=0;i<count;i++)bytes[i]=value;return bytes;}
 static bool Same(byte[] left,byte[] right){if(left==null||right==null)return left==right;if(left.Length!=right.Length)return false;for(int i=0;i<left.Length;i++)if(left[i]!=right[i])return false;return true;}
 static bool Rejected(Action action){try{action();return false;}catch(ArgumentException){return true;}}
 static BinaryPointerCacheSnapshot Names(params BinaryNameCacheEntry[] entries){return new BinaryPointerCacheSnapshot(0,0,new[]{entries});}
 static BinaryFieldSnapshot Fields(params BinaryFieldEntry[] entries){return new BinaryFieldSnapshot(0,0,new[]{entries});}
 static BinaryFieldEntry Field(ulong key,int tag=2,uint slot=0){return new BinaryFieldEntry(key,new BinaryFieldValue(tag,slot));}
 static LoadTableBinarySnapshot Table(BinaryPointerCacheSnapshot names,TableString[] pool,BinaryFieldSnapshot fields,int[] buffer){return new LoadTableBinarySnapshot(new RegistrationGate(),true,new BinaryWarmState(names,pool,fields,buffer));}
 static LoadTableBinarySnapshot Warm(TableString value){return Table(Names(new BinaryNameCacheEntry(Address,0)),new[]{S("Class"),value},Fields(Field(0)),new[]{1});}
 static RecordTableGetterResult Get(LoadTableBinarySnapshot table,byte[] query,TableString fallback,ulong? address=Address){return RecordTableGetter.GetValue(table,query,fallback,address);}
 static bool Released(RecordTableGetterResult result,RecordTableSnapshot table){return result.GateAcquired&&result.GateReleased&&!table.Gate.IsHeldByCurrentThread;}
 static void AddressAndChainIdentity(){
  var fallback=S("fallback");var value=S("Item");var pointer=new BinaryNameCacheEntry(Address,2);var field=Field(2);
  var table=Table(Names(new BinaryNameCacheEntry(0x1018,0),pointer),new[]{S("unused"),value,S("Class")},Fields(new BinaryFieldEntry(0x100000002, null),field),new[]{1});
  byte[] query=C("Class"),original=(byte[])query.Clone();var result=Get(table,query,fallback);
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Value,value)&&!result.UsedDefault&&result.MatchedField==null&&result.Binary.Status=="Known"&&Object.ReferenceEquals(result.Binary.PointerEntry,pointer)&&Object.ReferenceEquals(result.Binary.FieldEntry,field)&&result.Binary.NameId==2&&result.Binary.StringIndex==1&&result.KeyLength==5&&!result.LocalKeyCapacity.HasValue&&Released(result,table)&&Same(query,original),"real Binary getter traverses both collision chains, compares full qword field keys and returns shared-pool identity without copying the query");
  result=Get(table,C("Class"),fallback);
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Value,value)&&Object.ReferenceEquals(result.Binary.PointerEntry,pointer)&&Released(result,table),"a different managed byte array with the same explicit address and bytes remains the same warm query");
  result=Get(table,C("Class"),fallback,0x1028);
  Need(result.Status=="Unsupported"&&result.Reason=="UnimplementedBinaryPointerCacheMiss"&&result.FailureStage=="PointerLookup"&&result.Binary.PointerEntry==null&&result.Binary.FieldEntry==null&&!result.Binary.NameId.HasValue&&Released(result,table),"identical bytes at a different address do not create a pointer-cache hit");
  result=Get(table,C("Other"),fallback);
  Need(result.Status=="Unsupported"&&result.Reason=="UnimplementedBinaryStalePointerLookup"&&result.FailureStage=="NameContentCompare"&&Object.ReferenceEquals(result.Binary.PointerEntry,pointer)&&!result.Binary.NameId.HasValue&&result.Binary.FieldEntry==null&&Released(result,table),"an address hit with changed bytes stops at stale fallback before numeric lookup");
  result=Get(table,C("Class"),fallback);
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Value,value)&&Object.ReferenceEquals(result.Binary.PointerEntry,pointer)&&Object.ReferenceEquals(result.Binary.FieldEntry,field)&&pointer.CachedId==2&&field.Value.BufferIndex==0,"cold and stale attempts leave cache entries and numeric storage unchanged for the subsequent warm query");
  var first=new BinaryNameCacheEntry(Address,0);var later=new BinaryNameCacheEntry(Address,1);
  var duplicate=Table(Names(first,later),new[]{S("Other"),S("Class")},Fields(Field(1)),new[]{1});result=Get(duplicate,C("Class"),fallback);
  Need(result.Status=="Unsupported"&&result.Reason=="UnimplementedBinaryStalePointerLookup"&&Object.ReferenceEquals(result.Binary.PointerEntry,first)&&first.CachedId==0&&later.CachedId==1&&result.Binary.FieldEntry==null&&Released(result,duplicate),"the first equal pointer wins even when stale; a later equal pointer cannot rescue it");
  var firstField=Field(0,1,uint.MaxValue);var lastField=Field(0);duplicate=Table(Names(new BinaryNameCacheEntry(Address,0)),new[]{S("Class"),value},Fields(firstField,lastField),null);result=Get(duplicate,C("Class"),fallback);
  Need(result.Status=="Known"&&result.UsedDefault&&Object.ReferenceEquals(result.Value,fallback)&&Object.ReferenceEquals(result.Binary.FieldEntry,firstField)&&!result.Binary.StringIndex.HasValue&&Released(result,duplicate),"the first equal numeric key selects its nonstring tag without reading a later duplicate or the value buffer");
  var highId=new BinaryNameCacheEntry(Address,0x100000001);var outside=Table(Names(highId),new[]{S("unused"),S("Class")},null,null);result=Get(outside,C("Class"),fallback);
  Need(result.Status=="Unsupported"&&result.Reason=="CachedBinaryNameIndexOutsideCoveredDomain"&&result.FailureStage=="CachedNameRead"&&Object.ReferenceEquals(result.Binary.PointerEntry,highId)&&!result.Binary.NameId.HasValue&&result.Binary.FieldEntry==null&&Released(result,outside),"the full 64-bit cached ID is checked before its low 32 bits could select an apparently valid name");
 }
 static void HashAndCString(){
  var value=S("value");var pointer=new BinaryNameCacheEntry(0x0000000200000008,2);var field=Field(2);
  var names=new BinaryPointerCacheSnapshot(97,3,new[]{new BinaryNameCacheEntry[0],new[]{pointer},new BinaryNameCacheEntry[0],new BinaryNameCacheEntry[0]});
  var fields=new BinaryFieldSnapshot(97,3,new[]{new[]{field},new BinaryFieldEntry[0],new BinaryFieldEntry[0],new BinaryFieldEntry[0]});
  var table=Table(names,new[]{S("unused"),value,S("Class")},fields,new[]{1});var result=Get(table,C("Class"),null,pointer.Address);
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Value,value)&&Object.ReferenceEquals(result.Binary.PointerEntry,pointer)&&Object.ReferenceEquals(result.Binary.FieldEntry,field)&&Released(result,table),"shift 97 uses low six bits and shifts the complete address before truncation: pointer bucket 1, numeric bucket 0");
  pointer=new BinaryNameCacheEntry(0x8000000000000000,0);names=new BinaryPointerCacheSnapshot(127,1,new[]{new BinaryNameCacheEntry[0],new[]{pointer}});table=Table(names,new[]{S("Class"),value},Fields(Field(0)),new[]{1});result=Get(table,C("Class"),null,pointer.Address);
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Value,value)&&Released(result,table),"a high-bit address shifted by 127 uses a logical 64-bit shift by 63");
  var rawName=new TableString(new byte[]{0x80,0xff,0,0x41});var rawValue=new TableString(new byte[]{0,0xff});table=Table(Names(new BinaryNameCacheEntry(Address,0)),new[]{rawName,rawValue},Fields(Field(0)),new[]{1});
  result=Get(table,new byte[]{0x80,0xff,0,0x42},S("fallback"));
  Need(result.Status=="Known"&&result.KeyLength==2&&result.Binary.CStringLength==2&&Object.ReferenceEquals(result.Value,rawValue)&&!result.UsedDefault&&Same(result.Value.CopyCStringBytes(),new byte[]{0,0xff,0})&&Released(result,table),"high bytes compare exactly and equal NUL ends name validation while the returned value keeps its complete NUL-containing payload");
  result=Get(table,new byte[]{0x80,0xfe,0},null);
  Need(result.Status=="Unsupported"&&result.Reason=="UnimplementedBinaryStalePointerLookup"&&!result.KeyLength.HasValue&&Released(result,table),"high-byte mismatch is stale rather than locale conversion or a default result");
  table=Table(Names(new BinaryNameCacheEntry(Address,0)),new[]{S(""),value},Fields(Field(0)),new[]{1});result=Get(table,new byte[]{0,255},null);
  Need(result.Status=="Known"&&result.KeyLength==0&&Object.ReferenceEquals(result.Value,value)&&Released(result,table),"a warm empty name is valid and ignores bytes after the first NUL");
  var longest=new TableString(Repeated(65536,0x61));table=Table(Names(new BinaryNameCacheEntry(Address,0)),new[]{longest,value},Fields(Field(0)),new[]{1});result=Get(table,longest.CopyCStringBytes(),null);
  Need(result.Status=="Known"&&result.KeyLength==65536&&Object.ReferenceEquals(result.Value,value)&&Released(result,table),"the logical terminator at the maximum complete name length is readable");
  result=Get(table,Repeated(65536,0x61),null);
  Need(result.Status=="Unsupported"&&result.Reason=="BinaryQueryReadOutsideCoveredDomain"&&result.FailureStage=="NameContentCompare"&&!result.KeyLength.HasValue&&Released(result,table),"an otherwise matching query with no readable terminator remains unknown");
 }
 static void ResultsAndReachedGuards(){
  var fallback=S("fallback");var empty=S("");var table=Warm(empty);var result=Get(table,C("Class"),fallback);
  Need(result.Status=="Known"&&!result.UsedDefault&&Object.ReferenceEquals(result.Value,empty)&&result.Value.Length==0&&result.Binary.StringIndex==1&&Released(result,table),"Binary returns the actual empty stored string rather than the caller default");
  table=Table(Names(new BinaryNameCacheEntry(Address,0)),new[]{S("Class")},Fields(),null);result=Get(table,C("Class"),fallback);
  Need(result.Status=="Known"&&result.UsedDefault&&Object.ReferenceEquals(result.Value,fallback)&&result.Binary.FieldEntry==null&&!result.Binary.StringIndex.HasValue&&Released(result,table),"a numeric field miss returns exact default identity without requiring a value buffer");
  table=Table(Names(new BinaryNameCacheEntry(Address,0)),new[]{S("Class")},Fields(Field(0,7,uint.MaxValue)),null);result=Get(table,C("Class"),null);
  Need(result.Status=="Known"&&result.UsedDefault&&result.Value==null&&result.Binary.FieldEntry.Value.TypeTag==7&&!result.Binary.StringIndex.HasValue&&Released(result,table),"a nonstring field preserves a known null default without reading its buffer index");
  foreach(int index in new[]{-1,2}){
   table=Table(Names(new BinaryNameCacheEntry(Address,0)),new[]{S("Class"),S("value")},Fields(Field(0)),new[]{index});result=Get(table,C("Class"),fallback);
   Need(result.Status=="Known"&&!result.UsedDefault&&result.Value==null&&result.Binary.StringIndex==index&&result.Binary.LastCompletedStage=="NullStringSelected"&&Released(result,table),"a valid buffer read with negative or out-of-pool string index is known null: "+index);
  }
  table=Table(Names(new BinaryNameCacheEntry(Address,0)),new[]{S("Class")},Fields(Field(0,2,uint.MaxValue)),new int[0]);result=Get(table,C("Class"),fallback);
  Need(result.Status=="Unsupported"&&result.Reason=="BinaryValueBufferReadOutsideCoveredDomain"&&result.FailureStage=="ValueBufferRead"&&!result.Binary.StringIndex.HasValue&&result.Value==null&&!result.UsedDefault&&Released(result,table),"an unchecked native buffer access outside the supplied snapshot cannot become a known null string result");
  table=Table(Names(new BinaryNameCacheEntry(Address,0)),new[]{S("Class")},Fields(new BinaryFieldEntry(0,null)),null);result=Get(table,C("Class"),fallback);
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownBinaryFieldPayload"&&result.FailureStage=="FieldPayloadRead"&&result.Binary.FieldEntry!=null&&Released(result,table),"an existing node with unknown payload is not an absent field or a nonstring tag");
  var pointer=new BinaryNameCacheEntry(Address,0);table=Table(Names(pointer),null,null,null);result=Get(table,null,fallback);
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownBinarySharedStringPool"&&result.FailureStage=="CachedNameRead"&&Object.ReferenceEquals(result.Binary.PointerEntry,pointer)&&Released(result,table),"the cached-name pool becomes required before query text on a pointer hit");
  table=Table(Names(pointer),new[]{S("Class")},null,null);result=Get(table,C("Class"),fallback);
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownBinaryFieldSnapshot"&&result.KeyLength==5&&result.Binary.NameId==0&&Released(result,table),"unknown numeric cache is reported only after a complete warm name match");
  table=Table(Names(pointer),new[]{S("Class")},Fields(Field(0)),null);result=Get(table,C("Class"),fallback);
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownBinaryValueBuffer"&&result.FailureStage=="ValueBufferRead"&&Released(result,table),"a matched string tag requires its actual value buffer");
  table=Warm(S("Item"));result=RecordTableGetter.GetValue(table,C("Class"),fallback);
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownBinaryQueryAddress"&&result.FailureStage=="QueryAddress"&&result.Binary.PointerEntry==null&&Released(result,table),"the existing three-argument getter never invents a native query address");
  result=Get(table,C("Class"),fallback,0);
  Need(result.Status=="Unsupported"&&result.Reason=="NullBinaryQueryAddressOutsideCoveredDomain"&&result.Binary.PointerEntry==null&&Released(result,table),"explicit null pointer is not converted into managed byte-array identity");
  result=Get(table,null,fallback);
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownBinaryQueryCString"&&result.FailureStage=="NameContentCompare"&&result.Binary.PointerEntry!=null&&Released(result,table),"known pointer and pool cannot supply unknown query bytes");
  table=Table(new BinaryPointerCacheSnapshot(0,0,new BinaryNameCacheEntry[0][]),null,null,null);result=Get(table,null,fallback,null);
  Need(result.Status=="Unsupported"&&result.Reason=="UnimplementedBinaryPointerCacheMiss"&&result.FailureStage=="PointerCache"&&result.Binary.PointerEntry==null&&Released(result,table),"an explicitly absent bucket array reaches cold fallback before requiring an address or query");
  table=Table(null,null,null,null);result=Get(table,null,fallback,null);
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownBinaryPointerCache"&&result.FailureStage=="PointerCache"&&Released(result,table),"unknown cache storage is different from a known absent bucket array");
  table=new LoadTableBinarySnapshot(new RegistrationGate(),false,new BinaryWarmState(null,null,null,null));result=Get(table,null,null,null);
  Need(result.Status=="Known"&&result.Value==null&&result.UsedDefault&&result.Binary==null&&!result.KeyLength.HasValue&&Released(result,table),"the absent-archive shortcut still ignores all warm state and query metadata");
 }
 static void CopiesAndBudgets(){
  byte[] nameBytes=B("Class"),valueBytes=B("Item");var name=new TableString(nameBytes);var value=new TableString(valueBytes);var pointer=new BinaryNameCacheEntry(Address,0);var field=Field(0);
  var nameRow=new[]{pointer};var fieldRow=new[]{field};var nameRows=new[]{nameRow};var fieldRows=new[]{fieldRow};var pool=new[]{name,value};var buffer=new[]{1};
  var table=Table(new BinaryPointerCacheSnapshot(0,0,nameRows),pool,new BinaryFieldSnapshot(0,0,fieldRows),buffer);
  nameBytes[0]=0;valueBytes[0]=0;nameRow[0]=new BinaryNameCacheEntry(0,1);fieldRow[0]=Field(0,1);nameRows[0]=null;fieldRows[0]=null;pool[0]=S("Other");pool[1]=S("changed");buffer[0]=-1;
  var result=Get(table,C("Class"),null);
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Binary.PointerEntry,pointer)&&Object.ReferenceEquals(result.Binary.FieldEntry,field)&&Object.ReferenceEquals(result.Value,value)&&Same(result.Value.CopyCStringBytes(),C("Item"))&&Released(result,table),"all mutable input arrays are copied while immutable cache nodes and shared string identities are retained");
  byte[] exported=value.CopyCStringBytes();exported[0]=0;result=Get(table,C("Class"),null);
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Value,value)&&Same(value.CopyCStringBytes(),C("Item")),"a copied value view cannot alter later warm results");
  var many=new TableString[4096];for(int i=0;i<many.Length;i++)many[i]=name;table=Table(Names(new BinaryNameCacheEntry(Address,4095)),many,Fields(Field(4095)),new[]{4095});result=Get(table,C("Class"),null);
  Need(result.Status=="Known"&&result.Binary.NameId==4095&&result.Binary.StringIndex==4095&&Object.ReferenceEquals(result.Value,name)&&Rejected(delegate{new BinaryWarmState(null,new TableString[4097],null,null);}),"the 4096-string boundary supports actual warm lookup and rejects 4097 slots");
  var big=new TableString(Repeated(65536,0x61));var exactPool=new TableString[32];for(int i=0;i<exactPool.Length;i++)exactPool[i]=big;var overPool=new TableString[33];Array.Copy(exactPool,overPool,32);overPool[32]=S("x");table=Table(Names(new BinaryNameCacheEntry(Address,0)),exactPool,Fields(Field(0)),new[]{31});result=Get(table,big.CopyCStringBytes(),null);
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Value,big)&&result.KeyLength==65536&&Rejected(delegate{new BinaryWarmState(null,overPool,null,null);}),"the exact 2 MiB pool budget counts repeated storage slots and rejects one extra byte");
  var largeBuffer=new int[65536];largeBuffer[65535]=1;table=Table(Names(new BinaryNameCacheEntry(Address,0)),new[]{name,value},Fields(Field(0,2,65535)),largeBuffer);result=Get(table,C("Class"),null);
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Value,value)&&Rejected(delegate{new BinaryWarmState(null,null,null,new int[65537]);}),"the last of 65536 buffer values is usable and 65537 values exceed coverage");
  var rows=new BinaryNameCacheEntry[4096][];for(int i=0;i<rows.Length;i++)rows[i]=new BinaryNameCacheEntry[0];rows[8]=new[]{pointer};table=Table(new BinaryPointerCacheSnapshot(0,4095,rows),new[]{name,value},Fields(field),new[]{1});result=Get(table,C("Class"),null);
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Value,value)&&Rejected(delegate{new BinaryPointerCacheSnapshot(0,0,new BinaryNameCacheEntry[4097][]);})&&Rejected(delegate{new BinaryFieldSnapshot(0,0,new BinaryFieldEntry[4097][]);}),"4096 pointer buckets support a real hit and both cache types reject 4097 buckets");
  var nodes=new BinaryNameCacheEntry[4096];for(int i=0;i<nodes.Length;i++)nodes[i]=new BinaryNameCacheEntry((ulong)i+0x2000,0);nodes[4095]=pointer;table=Table(Names(nodes),new[]{name,value},Fields(field),new[]{1});result=Get(table,C("Class"),null);
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Binary.PointerEntry,pointer)&&Rejected(delegate{Names(new BinaryNameCacheEntry[4097]);})&&Rejected(delegate{Fields(new BinaryFieldEntry[4097]);}),"a bounded 4096-node chain reaches its final address while both caches reject 4097 nodes");
  Need(Rejected(delegate{new BinaryPointerCacheSnapshot(0,1,new[]{new[]{new BinaryNameCacheEntry(1,0)},new BinaryNameCacheEntry[0]});})&&Rejected(delegate{new BinaryFieldSnapshot(0,1,new[]{new[]{Field(1)},new BinaryFieldEntry[0]});}),"snapshots reject nodes placed in a bucket inconsistent with the actual shift and mask");
  Need(Rejected(delegate{new BinaryPointerCacheSnapshot(0,1,new[]{new BinaryNameCacheEntry[0]});})&&Rejected(delegate{new BinaryFieldSnapshot(0,0,new BinaryFieldEntry[1][]);})&&Rejected(delegate{Names(new BinaryNameCacheEntry[]{null});})&&Rejected(delegate{Fields(new BinaryFieldEntry[]{null});})&&Rejected(delegate{new BinaryWarmState(null,new TableString[]{null},null,null);}),"invalid masks, unknown chains, unknown nodes and unknown pool slots cannot masquerade as complete snapshots");
 }
 static ObjectNameString Name(byte[] bytes){return new ObjectNameString(bytes,Math.Max(15UL,(ulong)bytes.Length));}
 static RecordDepotState Depot(object table){return new RecordDepotState(new RegistrationGate(),new RecordCacheSnapshot(1,new[]{new RecordCacheEntry(B("item"),table)}),0);}
 static void CreateBoundaries(){
  var table=Warm(S("Own"));var depot=Depot(table);int calls=0;var classes=new ClassInfoSnapshot(C("Own"),null,null,delegate{calls++;return ObjectBaseConstructor.Create();});
  var result=CreatePartialObject.Apply(depot,Name(B("item")),RecordNameLocale.KnownC,classes);
  Need(result.Status=="Unsupported"&&result.Reason=="ClassGetterUnsupported:UnknownBinaryQueryAddress"&&result.FailureStage=="ClassGetter"&&result.Load.HitCountIncremented&&depot.HitCount54==1&&result.Factory==null&&result.NameAssignment==null&&calls==0&&result.Getter.GateReleased&&!table.Gate.IsHeldByCurrentThread,"CreatePartial's original overload retains the load hit but cannot manufacture the Class literal's address");
  foreach(bool stale in new[]{false,true}){
   table=Table(Names(new BinaryNameCacheEntry(Address,0)),new[]{S(stale?"Other":"Class"),S("Own")},Fields(Field(0)),new[]{1});depot=Depot(table);
   result=CreatePartialObject.Apply(depot,Name(B("item")),RecordNameLocale.KnownC,classes,stale?Address:Address+16);
   Need(result.Status=="Unsupported"&&result.Getter.Reason==(stale?"UnimplementedBinaryStalePointerLookup":"UnimplementedBinaryPointerCacheMiss")&&depot.HitCount54==1&&result.Factory==null&&result.NameAssignment==null&&calls==0&&result.MayHaveEffects&&result.Getter.GateReleased,"real CreatePartial preserves its counted load and stops before construction at "+(stale?"stale":"cold")+" lookup");
  }
  table=Table(Names(new BinaryNameCacheEntry(Address,0)),new[]{S("Class")},Fields(Field(0)),new[]{-1});depot=Depot(table);result=CreatePartialObject.Apply(depot,Name(B("item")),RecordNameLocale.KnownC,classes,Address);
  Need(result.Status=="Unsupported"&&result.Reason=="ClassFactoryUnsupported:UnknownRequestedClassName"&&result.Getter.Status=="Known"&&result.Getter.Value==null&&!result.Getter.UsedDefault&&result.Factory.CallbacksInvoked==0&&calls==0&&!result.DiagnosticRequired&&result.NameAssignment==null&&depot.HitCount54==1,"a known null binary string reaches the factory's null-query boundary rather than becoming the empty Class default");
  var empty=S("");table=Warm(empty);depot=Depot(table);int emptyCalls=0;result=CreatePartialObject.Apply(depot,Name(B("item")),RecordNameLocale.KnownC,new ClassInfoSnapshot(new byte[]{0},null,null,delegate{emptyCalls++;return null;}),Address);
  Need(result.Status=="Known"&&result.LastCompletedStage=="NullFactoryResult"&&Object.ReferenceEquals(result.Getter.Value,empty)&&!result.Getter.UsedDefault&&emptyCalls==1&&result.Instance==null&&!result.DiagnosticRequired&&result.NameAssignment==null,"a real empty binary string dispatches the empty class and preserves its known null result without a spawn diagnostic");
 }
 static RegistrationIndex Index(params RegistrationEntry[] entries){return new RegistrationIndex(8,8,1.0f,entries);}
 static RegistrationIndex FullIndex(){var entries=new RegistrationEntry[8];for(int i=0;i<entries.Length;i++)entries[i]=new RegistrationEntry((uint)i,new object());return Index(entries);}
 static bool Grown(RegistrationIndex index){return index.StateComplete&&index.Count==9&&index.BucketCount==64&&index.BucketCapacity==64;}
 static void WholeScenario(bool supplyOwnRead){
  byte[] originalBytes=B("Records\\MiXeD\\Entry\0TAIL/AbC.dbr"),fullKey=B("records/mixed/entry\0tail/abc.dbr"),prefix=B("Records\\MiXeD\\Entry"),prefixKey=B("records/mixed/entry");
  var classValue=S("Own\0Ignored");var pointer=new BinaryNameCacheEntry(Address,2);var field=Field(2);var creationTable=Table(Names(pointer),new[]{S("unused"),classValue,S("Class")},Fields(field),new[]{1});var finishTable=new object();
  var depot=new RecordDepotState(new RegistrationGate(),new RecordCacheSnapshot(1,new[]{new RecordCacheEntry(fullKey,creationTable),new RecordCacheEntry(prefixKey,finishTable)}),0);
  int constructors=0;bool unlocked=false,baseInitial=false;ObjectBaseState constructed=null;ObjectNameString initialName=null;RegistrationObject initialFields=null;
  var classes=new ClassInfoSnapshot(C("Own"),null,null,delegate{constructors++;unlocked=!depot.Gate.IsHeldByCurrentThread&&!creationTable.Gate.IsHeldByCurrentThread;constructed=ObjectBaseConstructor.Create();initialName=constructed.Name;initialFields=constructed.Fields;baseInitial=constructed.Dispatch==ObjectBaseDispatch.ObjectBase&&constructed.NameLength==0&&constructed.NameCapacity==15&&!initialFields.Id2C.HasValue&&!initialFields.RawKey28.HasValue;return constructed;});
  var original=Name(originalBytes);var created=CreatePartialObject.Apply(depot,original,RecordNameLocale.KnownC,classes,Address);var value=created.Instance as ObjectBaseState;
  Need(created.Status=="Known"&&value!=null&&Object.ReferenceEquals(value,constructed)&&constructors==1&&unlocked&&baseInitial&&created.Factory.CallbacksInvoked==1&&created.Getter.GateReleased&&depot.HitCount54==1,"warm Binary Class reaches one explicit own constructor after the load and table gates are released");
  Need(created.Getter.Status=="Known"&&!created.Getter.UsedDefault&&Object.ReferenceEquals(created.Getter.Value,classValue)&&Object.ReferenceEquals(created.Getter.Binary.PointerEntry,pointer)&&Object.ReferenceEquals(created.Getter.Binary.FieldEntry,field)&&created.Getter.Binary.NameId==2&&created.Getter.Binary.StringIndex==1&&created.Getter.KeyLength==5&&!created.Getter.LocalKeyCapacity.HasValue&&created.Getter.MatchedField==null,"the composition uses the actual warm pointer/shared-pool/numeric getter rather than a fabricated ordinary Class field");
  Need(created.NameAssignment.Status=="Known"&&created.NameAssignment.CapacityGrew&&Object.ReferenceEquals(value.Name,initialName)&&Object.ReferenceEquals(value.Fields,initialFields)&&value.NameLength==32&&value.NameCapacity==47&&value.Fields.RawKey28==FullNameKey&&!value.Fields.Id2C.HasValue&&Same(value.CopyNameBytes(),originalBytes)&&Same(created.Load.CopyNormalizedName(),fullKey),"construction is followed by full original NUL-containing assignment and signed-byte raw-key hashing");
  var fields=value.Fields;var ids=FullIndex();var raw=FullIndex();var manager=new ObjectRegistrationState(new RegistrationGate(),Index(new RegistrationEntry(Id,new object())),ids,raw);var oldId=ids.FirstInBucket(ids.BucketFor(0));var oldRaw=raw.FirstInBucket(raw.BucketFor(0));int reads=0;bool readComplete=false;Action<RegistrationObject,object> read=null;
  if(supplyOwnRead)read=delegate(RegistrationObject incoming,object loaded){reads++;readComplete=Object.ReferenceEquals(incoming,fields)&&Object.ReferenceEquals(loaded,finishTable)&&!Object.ReferenceEquals(loaded,creationTable)&&incoming.Id2C==Id&&incoming.RawKey28==FullNameKey&&manager.Pending128.Count==0&&Grown(ids)&&Grown(raw)&&!manager.Gate.IsHeldByCurrentThread&&!depot.Gate.IsHeldByCurrentThread&&!creationTable.Gate.IsHeldByCurrentThread;};
  var finished=FinishPartialObject.Apply(manager,depot,value,Id,RecordNameLocale.KnownC,read);
  Need(finished.Load.Status=="Known"&&finished.Load.CacheHit&&Object.ReferenceEquals(finished.Load.Table,finishTable)&&!Object.ReferenceEquals(finished.Load.Table,created.Load.Table)&&depot.HitCount54==2&&finished.CStringLength==19&&finished.TemporaryNameCapacity==31&&Same(finished.CopyRecordName(),prefix)&&Same(finished.Load.CopyNormalizedName(),prefixKey),"FinishPartial reloads the shorter C-string prefix and selects the distinct table identity");
  Need(Object.ReferenceEquals(finished.Instance,value)&&Object.ReferenceEquals(value.Name,initialName)&&Object.ReferenceEquals(value.Fields,initialFields)&&value.Fields.Id2C==Id&&value.Fields.RawKey28==FullNameKey&&Same(value.CopyNameBytes(),originalBytes)&&Same(original.CopyBytes(),originalBytes)&&constructors==1&&pointer.CachedId==2&&Object.ReferenceEquals(created.Getter.Value,classValue),"the prefix reload preserves full name, raw key, shared-pool storage and construction identity without another Class lookup");
  Need(finished.Registration.IdAssigned&&finished.Registration.PendingErase.Supported&&finished.Registration.PendingErase.RemovedCount==1&&manager.Pending128.Count==0&&finished.Registration.LockAcquired&&finished.Registration.LockReleased&&!manager.Gate.IsHeldByCurrentThread&&!depot.Gate.IsHeldByCurrentThread,"registration writes the supplied ID, erases pending and releases its independent registration gate");
  Need(Grown(ids)&&Grown(raw)&&finished.Registration.IdInsert.Inserted==true&&finished.Registration.RawKeyInsert.Inserted==true&&finished.Registration.IdInsert.Node.Key==Id&&finished.Registration.RawKeyInsert.Node.Key==FullNameKey&&Object.ReferenceEquals(finished.Registration.IdInsert.Value,fields)&&Object.ReferenceEquals(finished.Registration.RawKeyInsert.Value,fields),"both ninth insertions perform actual 8-to-64 growth and store the same constructed object under its ID and full-name key");
  var duplicateId=ids.Insert(0,new object());var duplicateRaw=raw.Insert(0,new object());
  Need(duplicateId.Supported&&duplicateRaw.Supported&&duplicateId.Inserted==false&&duplicateRaw.Inserted==false&&Object.ReferenceEquals(duplicateId.Node,oldId)&&Object.ReferenceEquals(duplicateRaw.Node,oldRaw)&&!duplicateId.MayHaveEffects&&!duplicateRaw.MayHaveEffects&&Grown(ids)&&Grown(raw),"both growth operations retain prior node identities and duplicate probes do not insert or grow");
  if(supplyOwnRead)Need(finished.Status=="Known"&&finished.LastCompletedStage=="ReadComplete"&&finished.Registration.ReadInvoked&&reads==1&&readComplete,"explicit own Read observes the prefix table after completed growth outside all gates");
  else Need(finished.Status=="Unsupported"&&finished.Reason=="RegistrationUnsupported:UnknownOwnReadCallback"&&finished.FailureStage=="Registration"&&finished.LastCompletedStage=="LoadFileComplete"&&finished.Registration.LastCompletedStage=="LockReleased"&&finished.Registration.FailureStage=="OwnRead"&&!finished.Registration.ReadInvoked&&reads==0&&!readComplete,"missing Read remains unsupported after the completed registration prefix rather than becoming an empty callback");
  Need(finished.MayHaveEffects&&finished.Load.HitCountIncremented&&finished.Registration.MayHaveEffects&&depot.HitCount54==2&&constructors==1&&fields.Id2C==Id&&fields.RawKey28==FullNameKey&&Grown(ids)&&Grown(raw)&&Same(value.CopyNameBytes(),originalBytes),"both Read outcomes preserve completed load, construction, name assignment and registration effects");
 }
 static int Main(){try{AddressAndChainIdentity();HashAndCString();ResultsAndReachedGuards();CopiesAndBudgets();CreateBoundaries();WholeScenario(true);WholeScenario(false);Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+n+",\"scenarios\":2,\"scope\":\"Own complete bounded Binary warm snapshots through the real table getter, CreatePartial and FinishPartial composition; explicit query addresses and own callbacks, no cold/stale updates, parsing, native binding, allocator connection or original execution.\"}");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
