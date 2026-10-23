using System;
using System.Text;
using GrimDynasty.TaskItems;
class RecordLoadFileTests {
 static int n;
 static void Need(bool value,string message){n++;if(!value)throw new Exception(message);}
 static byte[] B(string value){return Encoding.ASCII.GetBytes(value);}
 static bool Same(byte[] left,byte[] right){if(left==null||right==null)return left==right;if(left.Length!=right.Length)return false;for(int i=0;i<left.Length;i++)if(left[i]!=right[i])return false;return true;}
 static byte[] Repeated(int count,byte value){var bytes=new byte[count];for(int i=0;i<count;i++)bytes[i]=value;return bytes;}
 static ObjectNameString Name(byte[] bytes){return new ObjectNameString(bytes,(ulong)Math.Max(15,bytes.Length));}
 static RecordCacheEntry E(byte[] key,object table){return new RecordCacheEntry(key,table);}
 static RecordDepotState Depot(RecordCacheSnapshot cache,uint hits=0){return new RecordDepotState(new RegistrationGate(),cache,hits);}
 static bool ArgumentRejected(Action action){try{action();return false;}catch(ArgumentException){return true;}}
 static RegistrationIndex Index(params RegistrationEntry[] entries){return new RegistrationIndex(8,8,1.0f,entries);}
 static void HashGoldens(){
  var all=new byte[256];for(int i=0;i<all.Length;i++)all[i]=(byte)i;
  byte[][] inputs={new byte[0],B("A"),new byte[]{0x41,0,0x42},new byte[]{0},new byte[]{0x80},new byte[]{0xff},new byte[]{0x80,0xff,0,0x7f},all};
  // Independently generated with Node BigInt, retained in
  // docs/object-lifecycle/record-load-validation/hash-vectors.json.
  ulong[] expected={0xcbf29ce484222325UL,0xaf63fc4c860222ecUL,0xfb029919a1274bf2UL,0xaf63bd4c8601b7dfUL,0xaf643d4c8602915fUL,0xaf64724c8602eb6eUL,0x7d83f4abf79470edUL,0x4242dc5249c33625UL};
  for(int i=0;i<inputs.Length;i++)Need(RecordCacheSnapshot.HashBytes(inputs[i])==expected[i],"full-length unsigned-byte FNV64 literal vector "+i);
 }
 static void HitAndIsolation(){
  byte[] requestedBytes=B("Dir\\FILE.DBR"),original=(byte[])requestedBytes.Clone(),key=B("dir/file.dbr");
  var requested=new ObjectNameString(requestedBytes,63);var table=new object();var entry=E(key,table);var list=new[]{entry};var cache=new RecordCacheSnapshot(8,list);
  requestedBytes[0]=0;key[0]=0;list[0]=E(B("other"),new object());
  var depot=Depot(cache,17);var result=RecordLoadFile.Apply(depot,requested,RecordNameLocale.KnownC);
  Need(result.Status=="Known"&&result.CacheHit&&Object.ReferenceEquals(result.Table,table)&&Object.ReferenceEquals(result.Lookup.Entry,entry)&&result.Lookup.Visited==1&&result.Lookup.NodeIndex==0&&depot.HitCount54==18&&result.HitCountIncremented,"a normalized hit returns the exact table and counts once despite mutations to input arrays");
  Need(result.LockAcquired&&result.LockReleased&&result.LastCompletedStage=="LockReleased"&&result.MayHaveEffects&&!depot.Gate.IsHeldByCurrentThread&&result.LocalNameCapacity==15&&requested.Capacity==63&&Same(requested.CopyBytes(),original)&&Same(result.CopyNormalizedName(),B("dir/file.dbr")),"local normalization runs under the own gate and retains the requested name and its source capacity");
  var normalizedCopy=result.CopyNormalizedName();var keyCopy=entry.CopyKey();normalizedCopy[0]=0;keyCopy[0]=0;
  Need(Same(result.CopyNormalizedName(),B("dir/file.dbr"))&&Same(entry.CopyKey(),B("dir/file.dbr"))&&Object.ReferenceEquals(cache.Find(B("dir/file.dbr")).Entry,entry)&&Same(requested.CopyBytes(),original),"result and cache key copy getters do not expose mutable snapshot storage");
 }
 static void CacheTraversal(){
  var first=new object();var later=new object();var other=new object();var outside=new object();
  var cache=new RecordCacheSnapshot(8,new[]{E(B("a"),first),E(B("i"),other),E(B("a"),later),E(B("b"),outside)});
  var found=cache.Find(B("a"));
  Need(found.Found&&found.Bucket==4&&found.NodeIndex==0&&found.Visited==1&&Object.ReferenceEquals(found.Entry.Table,first),"lookup returns the first duplicate in forward list order");
  found=cache.Find(B("i"));
  Need(found.Found&&found.Bucket==4&&found.NodeIndex==1&&found.Visited==2&&Object.ReferenceEquals(found.Entry.Table,other),"a same-bucket collision advances to the second node");
  var miss=cache.Find(B("q"));var nextBucket=cache.Find(B("b"));var empty=cache.Find(B("c"));
  Need(!miss.Found&&miss.Bucket==4&&miss.Visited==3&&miss.NodeIndex==-1&&nextBucket.Found&&nextBucket.NodeIndex==3&&nextBucket.Visited==1&&Object.ReferenceEquals(nextBucket.Entry.Table,outside)&&!empty.Found&&empty.Visited==0,"bucket endpoints stop a collision miss before the next bucket and skip an empty bucket");
  Need(ArgumentRejected(delegate{new RecordCacheSnapshot(8,new[]{E(B("a"),first),E(B("b"),outside),E(B("i"),other)});}),"an explicit snapshot cannot repeat a bucket after another bucket segment");
 }
 static void LocaleBoundaries(){
  var depot=Depot(new RecordCacheSnapshot(1,new RecordCacheEntry[0]),9);
  foreach(byte b in new byte[]{0x41,0x2f,0}){
   var result=RecordLoadFile.Apply(depot,Name(new[]{b}),default(RecordNameLocale));
   Need(result.Status=="Unsupported"&&result.Reason=="UnknownCrtToLowerLocale"&&result.FailureStage=="NameProcessing"&&result.LastCompletedStage=="LocalNameCopied"&&result.LockAcquired&&result.LockReleased&&!result.CacheHit&&result.Lookup==null&&result.CopyNormalizedName()==null&&!result.MayHaveEffects&&depot.HitCount54==9&&!depot.Gate.IsHeldByCurrentThread,"unknown locale is not silently C even for an unchanged slash or embedded NUL byte "+b);
  }
  foreach(byte b in new byte[]{0x80,0xff}){
   var input=Name(new byte[]{0x5c,b});var result=RecordLoadFile.Apply(depot,input,RecordNameLocale.KnownC);
   Need(result.Status=="Unsupported"&&result.Reason=="SignedHighByteToLowerOutsideCoveredDomain"&&result.LockReleased&&result.Lookup==null&&result.CopyNormalizedName()==null&&!result.MayHaveEffects&&depot.HitCount54==9&&Same(input.CopyBytes(),new byte[]{0x5c,b}),"KnownC still does not cover signed high-byte tolower inputs, including FF "+b);
  }
  var emptyTable=new object();var slashTable=new object();depot=Depot(new RecordCacheSnapshot(1,new[]{E(new byte[0],emptyTable),E(B("//"),slashTable)}));
  byte[][] inputs={new byte[0],B("\\\\")};object[] tables={emptyTable,slashTable};
  for(int i=0;i<inputs.Length;i++){
   var result=RecordLoadFile.Apply(depot,Name(inputs[i]),RecordNameLocale.Unknown);
   Need(result.Status=="Known"&&result.CacheHit&&Object.ReferenceEquals(result.Table,tables[i])&&result.LocalNameCapacity==15&&result.LockReleased&&depot.HitCount54==(uint)(i+1)&&Same(result.CopyNormalizedName(),i==0?new byte[0]:B("//")),"empty or all-backslash names need no tolower locale "+i);
  }
 }
 static void FullLengthAndStoredKeys(){
  var table=new object();var cache=new RecordCacheSnapshot(1,new[]{E(B("a"),new object()),E(new byte[]{0x61,0},new object()),E(new byte[]{0x61,0,0x62},table)});
  var depot=Depot(cache);var result=RecordLoadFile.Apply(depot,Name(new byte[]{0x41,0,0x42}),RecordNameLocale.KnownC);
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Table,table)&&result.Lookup.NodeIndex==2&&result.Lookup.Visited==3&&Same(result.CopyNormalizedName(),new byte[]{0x61,0,0x62})&&depot.HitCount54==1,"normalization and equality retain embedded NUL and distinguish both shorter stored prefixes");
  byte[] stored=B("A\\B");cache=new RecordCacheSnapshot(1,new[]{E(stored,table)});depot=Depot(cache,7);result=RecordLoadFile.Apply(depot,Name(stored),RecordNameLocale.KnownC);
  Need(cache.Find(stored).Found&&result.Status=="Unsupported"&&result.Reason=="UnimplementedRecordCacheMissLoader"&&result.Lookup!=null&&!result.Lookup.Found&&Same(result.CopyNormalizedName(),B("a/b"))&&depot.HitCount54==7&&!result.MayHaveEffects,"cache snapshot keys are not normalized on the caller's behalf");
 }
 static void NullHitWrapAndMiss(){
  var depot=Depot(new RecordCacheSnapshot(1,new[]{E(B("a"),null)}),uint.MaxValue);
  var hit=RecordLoadFile.Apply(depot,Name(B("A")),RecordNameLocale.KnownC);
  Need(hit.Status=="Known"&&hit.CacheHit&&hit.Lookup.Found&&hit.Table==null&&hit.HitCountIncremented&&hit.MayHaveEffects&&depot.HitCount54==0&&hit.LockReleased,"a known null table is still a hit and the uint hit counter wraps from max to zero");
  var fields=new RegistrationObject(null,0);var registration=LoadedObjectRegistration.Apply(null,fields,77,hit.Table,null);
  Need(registration.Status=="Unsupported"&&registration.Reason=="NoKnownLoadedTable"&&!fields.Id2C.HasValue&&!registration.MayHaveEffects,"a known null cache hit cannot satisfy the separate nonnull loaded-table registration domain");
  var miss=RecordLoadFile.Apply(depot,Name(B("missing")),RecordNameLocale.KnownC);
  Need(miss.Status=="Unsupported"&&miss.Reason=="UnimplementedRecordCacheMissLoader"&&miss.FailureStage=="CacheMiss"&&miss.LastCompletedStage=="CacheLookupComplete"&&!miss.CacheHit&&miss.Lookup!=null&&!miss.Lookup.Found&&miss.Table==null&&!miss.HitCountIncremented&&!miss.MayHaveEffects&&depot.HitCount54==0&&miss.LockReleased&&Same(miss.CopyNormalizedName(),B("missing")),"a cache miss preserves its lookup result without pretending to load a table or count a hit");
 }
 static void CapacityAndBudget(){
  int[] lengths={15,16,31,32,65536};ulong[] capacities={15,31,31,47,65551};
  for(int i=0;i<lengths.Length;i++){
   byte[] bytes=Repeated(lengths[i],0x61);var table=new object();var depot=Depot(new RecordCacheSnapshot(1,new[]{E(bytes,table)}));
   var requested=new ObjectNameString(bytes,131073);var result=RecordLoadFile.Apply(depot,requested,RecordNameLocale.KnownC);
   Need(result.Status=="Known"&&result.CacheHit&&Object.ReferenceEquals(result.Table,table)&&result.LocalNameCapacity==capacities[i]&&Same(result.CopyNormalizedName(),bytes)&&requested.Capacity==131073&&depot.HitCount54==1,"a fresh local copy has the observed capacity at length "+lengths[i]);
  }
  var state=Depot(new RecordCacheSnapshot(1,new RecordCacheEntry[0]),37);byte[] excessive=Repeated(65537,0x61);
  var stopped=RecordLoadFile.Apply(state,Name(excessive),RecordNameLocale.KnownC);
  Need(stopped.Status=="Unsupported"&&stopped.Reason=="RecordNameOutsideCoveredDomain"&&!stopped.LockAcquired&&!stopped.LockReleased&&!stopped.MayHaveEffects&&stopped.LocalNameCapacity==0&&stopped.CopyNormalizedName()==null&&stopped.Lookup==null&&state.HitCount54==37,"65537 bytes exceed the own length coverage budget before acquiring the gate");
  Need(ArgumentRejected(delegate{RecordCacheSnapshot.HashBytes(excessive);})&&ArgumentRejected(delegate{new RecordCacheEntry(excessive,null);}),"direct hash and cache-entry input also enforce the explicit model length budget");
 }
 static void MissingInputs(){
  var requested=Name(B("A"));var cache=new RecordCacheSnapshot(1,new RecordCacheEntry[0]);
  var result=RecordLoadFile.Apply(null,requested,RecordNameLocale.KnownC);
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownRecordDepot"&&!result.LockAcquired&&!result.MayHaveEffects,"a missing depot is not an empty cache");
  var state=new RecordDepotState(null,cache,3);result=RecordLoadFile.Apply(state,requested,RecordNameLocale.KnownC);
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownRecordDepotGate"&&!result.LockAcquired&&result.LocalNameCapacity==0&&state.HitCount54==3,"missing synchronization is rejected before local copying");
  state=Depot(cache,4);result=RecordLoadFile.Apply(state,null,RecordNameLocale.KnownC);
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownRequestedRecordName"&&!result.LockAcquired&&!result.MayHaveEffects&&state.HitCount54==4,"a missing requested name does not become a known empty name");
  state=Depot(null,5);result=RecordLoadFile.Apply(state,requested,RecordNameLocale.KnownC);
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownRecordCacheSnapshot"&&result.FailureStage=="CacheLookup"&&result.LastCompletedStage=="NameProcessed"&&result.LockAcquired&&result.LockReleased&&!state.Gate.IsHeldByCurrentThread&&Same(result.CopyNormalizedName(),B("a"))&&result.Lookup==null&&!result.MayHaveEffects&&state.HitCount54==5,"unknown cache is reached only after copying and normalization, with the own gate released");
 }
 static void ConstructorNameRegistrationBoundary(){
  var value=ObjectBaseConstructor.Create();var name=value.Name;var fields=value.Fields;
  var assigned=ObjectNameAssignment.Apply(value,Name(B("A\\B")));uint? objectKey=fields.RawKey28;
  Need(assigned.Status=="Known"&&objectKey.HasValue&&!fields.Id2C.HasValue&&Object.ReferenceEquals(value.Name,name)&&Object.ReferenceEquals(value.Fields,fields),"base construction and name assignment leave registration ID unknown");
  var table=new object();var depot=Depot(new RecordCacheSnapshot(1,new[]{E(B("a/b"),table)}));
  var loaded=RecordLoadFile.Apply(depot,value.Name,RecordNameLocale.KnownC);
  Need(loaded.Status=="Known"&&loaded.CacheHit&&Object.ReferenceEquals(loaded.Table,table)&&depot.HitCount54==1&&loaded.LockReleased&&!depot.Gate.IsHeldByCurrentThread&&Same(value.CopyNameBytes(),B("A\\B"))&&fields.RawKey28==objectKey&&!fields.Id2C.HasValue,"LoadFile returns the exact supplied table without normalizing the object's stored name or name hash");
  const uint id=77;var manager=new ObjectRegistrationState(new RegistrationGate(),Index(new RegistrationEntry(id,new object())),Index(),Index());
  // No concrete native Read has been recovered. A cache hit does not supply
  // that missing callback or turn this into complete FinishPartial behavior.
  var registered=LoadedObjectRegistration.Apply(manager,fields,id,loaded.Table,null);
  Need(registered.Status=="Unsupported"&&registered.Reason=="UnknownOwnReadCallback"&&registered.FailureStage=="OwnRead"&&registered.LastCompletedStage=="LockReleased"&&registered.LockReleased&&!registered.ReadInvoked&&!manager.Gate.IsHeldByCurrentThread&&!depot.Gate.IsHeldByCurrentThread,"a real table identity crosses into registration but missing Read still stops after unlock");
  Need(registered.MayHaveEffects&&registered.PendingErase.RemovedCount==1&&registered.IdInsert.Inserted==true&&registered.RawKeyInsert.Inserted==true&&registered.RawKeyInsert.Node.Key==objectKey.Value&&Object.ReferenceEquals(registered.IdInsert.Value,fields)&&Object.ReferenceEquals(registered.RawKeyInsert.Value,fields)&&manager.Pending128.Count==0&&manager.ById30.Count==1&&manager.ByRawKey70.Count==1&&fields.Id2C==id&&fields.RawKey28==objectKey&&Object.ReferenceEquals(value.Name,name)&&depot.HitCount54==1,"the cache-hit count and completed registration prefix survive the unknown Read boundary");
 }
 static int Main(){try{HashGoldens();HitAndIsolation();CacheTraversal();LocaleBoundaries();FullLengthAndStoredKeys();NullHitWrapAndMiss();CapacityAndBudget();MissingInputs();ConstructorNameRegistrationBoundary();Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+n+",\"scope\":\"Own complete record-cache snapshots and bounded LoadFile hits with an explicit KnownC precondition; no runtime locale discovery, native miss loader, concrete Read or original execution.\"}");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
