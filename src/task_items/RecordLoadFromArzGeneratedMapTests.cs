using System;
using System.IO;
using System.Security.Cryptography;
using GrimDynasty.Resources;
using GrimDynasty.TaskItems;

// Own complete ARZv3 fixtures only. Maps and destination pools are produced
// by the checked archive-string importer, never supplied by this integration.
class RecordLoadFromArzGeneratedMapTests {
 static int assertions,scenarios;
 const ulong Address=0x12345008;
 static void Need(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static byte[] B(string text){return ResourceUtil.Utf8.GetBytes(text);}
 static bool Same(byte[] a,byte[] b){if(a==null||b==null)return a==b;if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
 static uint U32(byte[] bytes,int at){return (uint)bytes[at]|((uint)bytes[at+1]<<8)|((uint)bytes[at+2]<<16)|((uint)bytes[at+3]<<24);}
 static ObjectNameString Name(string text){byte[] bytes=B(text);return new ObjectNameString(bytes,(ulong)Math.Max(15,bytes.Length));}
 static byte[][] PoolA(){return new[]{B("rec-a"),B("Class"),B("Item"),B("raw")};}
 static byte[][] PoolB(){return new[]{B("Item"),B("raw"),B("rec-b"),B("Class"),B("Item")};}
 static byte[][] PoolC(){return new[]{B("rec-c"),B("Item"),B("Class"),B("raw"),B("Item")};}
 static byte[] BodyA(){return ArzRawFixture.Join(ArzRawFixture.Field(2,1,2,2),ArzRawFixture.Field(0,3,0xdeadbeefU));}
 static byte[] BodyB(){return ArzRawFixture.Join(ArzRawFixture.Field(2,3,0,4),ArzRawFixture.Field(0,1,0xdeadbeefU));}
 static byte[] BodyC(){return ArzRawFixture.Join(ArzRawFixture.Field(2,2,1,4),ArzRawFixture.Field(0,3,0xdeadbeefU));}
 static ArzRawFixtureData FixtureA(){return ArzRawFixture.Build(PoolA(),new ArzRawFixtureRecord(0,BodyA()));}
 static ArzRawFixtureData FixtureB(){return ArzRawFixture.Build(PoolB(),new ArzRawFixtureRecord(2,BodyB()));}
 static ArzRawFixtureData FixtureC(){return ArzRawFixture.Build(PoolC(),new ArzRawFixtureRecord(0,BodyC()));}
 static ArzRawLimits Limits(){return new ArzRawLimits(16,64,131072,65536,65536,131072,65536,1048576,4194304);}
 static RecordDepotState Depot(){return new RecordDepotState(new RegistrationGate(),new RecordCacheSnapshot(8,new RecordCacheEntry[0]),3,8,1,4,5,6);}
 static ArzRecordBinarySource Source(ResourceInput input,ArzRecordRemapContext context){
  return new ArzRecordBinarySource(input,Limits(),new RegistrationGate(),
   new BinaryPointerCacheSnapshot(0,0,new[]{new[]{new BinaryNameCacheEntry(Address,1)}}),
   new BinaryNameIdSnapshot(1,new[]{new BinaryNameIdEntry(B("Class"),1)}),10,15,
   ArzRawDecodeProfile.ObservedEngineOutputSize,context);
 }
 static RecordLoadFromSuppliedBinaryAttempt Attempt(RecordDepotState depot,ArzRecordBinarySource source,string name){
  return RecordLoadFromSuppliedBinaryAttempt.FromArzRecord(depot,Name(name),RecordNameLocale.KnownC,source);
 }
 static RecordTableGetterResult Get(object table){
  return RecordTableGetter.GetValue(table,B("Class\0"),new TableString(B("fallback")),Address);
 }
 static ArzRawRecord Read(ResourceInput input,string name){
  return ArzRawRecordReader.ReadExact(input,B(name),Limits(),ArzRawDecodeProfile.ObservedEngineOutputSize);
 }
 static ArzRawRecord ReadMemory(ArzRawFixtureData fixture,string name){
  using(var input=new ResourceInput(new MemoryStream(fixture.Bytes,false)))return Read(input,name);
 }
 static string WriteFixture(string name,ArzRawFixtureData fixture){
  string directory=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"fixtures");Directory.CreateDirectory(directory);
  string file=Path.Combine(directory,name);
  using(var output=new FileStream(file,FileMode.CreateNew,FileAccess.Write,FileShare.None))output.Write(fixture.Bytes,0,fixture.Bytes.Length);
  return file;
 }
 static string Hash(string path){using(var input=File.OpenRead(path))using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(input));}
 sealed class ObservedStream:Stream {
  readonly Stream inner;
  public int Accesses,ReadCalls;
  public bool Closed;
  public ObservedStream(Stream input){inner=input;}
  public override bool CanRead{get{return inner.CanRead;}}
  public override bool CanSeek{get{return inner.CanSeek;}}
  public override bool CanWrite{get{return false;}}
  public override long Length{get{Accesses++;return inner.Length;}}
  public override long Position{get{Accesses++;return inner.Position;}set{Accesses++;inner.Position=value;}}
  public override int Read(byte[] bytes,int offset,int count){Accesses++;ReadCalls++;return inner.Read(bytes,offset,count);}
  public override long Seek(long offset,SeekOrigin origin){Accesses++;return inner.Seek(offset,origin);}
  public override void Flush(){}
  public override void SetLength(long value){throw new NotSupportedException();}
  public override void Write(byte[] bytes,int offset,int count){throw new NotSupportedException();}
  protected override void Dispose(bool disposing){Closed=true;if(disposing)inner.Dispose();base.Dispose(disposing);}
 }
 static void CheckGeneratedState(ArchiveStringImportState state){
  Need(state.MappingCount==10&&state.TargetStringCount==7&&!state.RequiresResolution,"append counts use source slots while global strings deduplicate");
  uint[] selectors=state.CopySelectors();
  Need(selectors.Length==2&&selectors[0]==0&&selectors[1]==5,"append selectors preserve old mapping counts, independent of the initial target pool");
  byte[][] pool=state.CopyTargetStrings();
  string[] expected={"rec-a","Class","Item","raw","","rec-b","rec-c"};
  for(int i=0;i<expected.Length;i++)Need(Same(pool[i],B(expected[i])),"generated global pool preserves first occurrence at "+i);
  uint[] expectedMap={2,3,5,1,2,6,2,1,3,2};
  for(int i=0;i<expectedMap.Length;i++){uint value;Need(state.TryGetMapping(i,out value)&&value==expectedMap[i],"generated mapping cell "+i);}
  pool[2][0]=0;pool[0]=B("changed-export");
  Need(Same(state.CopyTargetStrings()[2],B("Item"))&&Same(state.CopyTargetStrings()[0],B("rec-a")),"pool inspection cannot rewrite live importer bytes");
 }
 static void ThreePhysicalArchives(){
  var fixtureA=FixtureA();var fixtureB=FixtureB();var fixtureC=FixtureC();
  string fileA=WriteFixture("own-generated-map-a.arz",fixtureA);
  string fileB=WriteFixture("own-generated-map-b.arz",fixtureB);
  string fileC=WriteFixture("own-generated-map-c.arz",fixtureC);
  string hashA=Hash(fileA),hashB=Hash(fileB),hashC=Hash(fileC);
  var streamA=new ObservedStream(new FileStream(fileA,FileMode.Open,FileAccess.Read,FileShare.Read));
  var streamB=new ObservedStream(new FileStream(fileB,FileMode.Open,FileAccess.Read,FileShare.Read));
  var streamC=new ObservedStream(new FileStream(fileC,FileMode.Open,FileAccess.Read,FileShare.Read));
  using(var inputA=new ResourceInput(streamA))
  using(var inputB=new ResourceInput(streamB))
  using(var inputC=new ResourceInput(streamC)){
   var rawA=Read(inputA,"rec-a");var rawB=Read(inputB,"rec-b");var rawC=Read(inputC,"rec-c");
   Need(rawA.DecodeProfile==ArzRawDecodeProfile.ObservedEngineOutputSize&&rawB.DecodeProfile==rawA.DecodeProfile&&rawC.DecodeProfile==rawA.DecodeProfile,"all complete physical fixtures use the observed output-size decoder");
   var state=new ArchiveStringImportState();
   var importedA=ArzRecordRemapContext.ImportChecked(state,rawA,false);
   var importedB=ArzRecordRemapContext.ImportChecked(state,rawB,true);
   var importedC=ArzRecordRemapContext.ImportChecked(state,rawC,true);
   Need(importedA.Status==ArchiveStringImportStatus.Completed&&importedB.Status==ArchiveStringImportStatus.Completed&&importedC.Status==ArchiveStringImportStatus.Completed,"initial and both appended raw pools import without a hand-built map");
   Need(!importedA.Append&&importedA.SourceIdsPreserved&&importedA.BaseId==0&&importedB.Append&&importedB.BaseId==0&&importedC.Append&&importedC.BaseId==5,"initial IDs stay local and append bases follow mapping storage, not target count");
   CheckGeneratedState(state);
   // Deliberately create every context only after all three imports.
   var contextA=ArzRecordRemapContext.FromGeneratedImport(state,importedA);
   var contextB=ArzRecordRemapContext.FromGeneratedImport(state,importedB);
   var contextC=ArzRecordRemapContext.FromGeneratedImport(state,importedC);
   Need(!contextA.Mapping.TablePresent&&contextA.Mapping.BaseId==0,"historical initial import never acquires an append mapping table");
   Need(contextB.Mapping.TablePresent&&contextC.Mapping.TablePresent&&contextB.Mapping.BaseId==0&&contextC.Mapping.BaseId==5&&contextB.Mapping.SignedMappingCount==10&&contextC.Mapping.SignedMappingCount==10,"historical append contexts snapshot the complete mapping with their original bases");
   Need(contextA.TargetStringCount==7&&contextB.TargetStringCount==7&&contextC.TargetStringCount==7,"all contexts snapshot the completed seven-string target pool");
   byte[] exportedSource=importedB.CopySourceString(4);exportedSource[0]=0;
   Need(Same(importedB.CopySourceString(4),B("Item")),"exporting imported source bytes cannot invalidate the generated source binding");
   var later=ReadMemory(ArzRawFixture.Build(new[]{B("rec-later"),B("Fresh")},new ArzRawFixtureRecord(0,ArzRawFixture.Field(2,1,1))),"rec-later");
   var laterImport=ArzRecordRemapContext.ImportChecked(state,later,true);
   Need(laterImport.Status==ArchiveStringImportStatus.Completed&&laterImport.BaseId==10&&state.MappingCount==12&&state.TargetStringCount==9,"a later import grows live mapping and pool after context creation");
   Need(contextA.TargetStringCount==7&&contextB.TargetStringCount==7&&contextC.TargetStringCount==7&&contextB.Mapping.SignedMappingCount==10&&contextC.Mapping.SignedMappingCount==10,"generated context snapshots do not track later importer growth");
   int readsA=streamA.ReadCalls,readsB=streamB.ReadCalls,readsC=streamC.ReadCalls;
   var sourceA=Source(inputA,contextA);var sourceB=Source(inputB,contextB);var sourceC=Source(inputC,contextC);
   var depot=Depot();
   var a=Attempt(depot,sourceA,"REC-A");var b=Attempt(depot,sourceB,"REC-B");var c=Attempt(depot,sourceC,"REC-C");
   Need(!a.Attempted&&!b.Attempted&&!c.Attempted&&streamA.ReadCalls==readsA&&streamB.ReadCalls==readsB&&streamC.ReadCalls==readsC,"source and attempt construction leave acquisition deferred");
   var ar=a.RunOnce();var br=b.RunOnce();var cr=c.RunOnce();
   Need(ar.Status=="Known"&&br.Status=="Known"&&cr.Status=="Known"&&!ar.CacheHit&&!br.CacheHit&&!cr.CacheHit&&depot.Cache.Count==3,"three real misses reach the actual shared depot");
   Need(streamA.ReadCalls>readsA&&streamB.ReadCalls>readsB&&streamC.ReadCalls>readsC,"each miss reacquires its own physical record through ResourceInput");
   var attempts=new[]{a,b,c};var results=new[]{ar,br,cr};
   byte[][] originalBodies={BodyA(),BodyB(),BodyC()};uint[] localNames={0,2,0};
   for(int i=0;i<attempts.Length;i++){
    var trace=attempts[i].Trace;
    Need(trace.OwnArzReadCompleted&&trace.OwnRemapAttempted&&trace.OwnRemapResult.Status==RecordStringRemapStatus.Completed,"generated source binding and body traversal complete for archive "+i);
    Need(Same(trace.OwnArzRecord.CopyBodyBytes(),originalBodies[i])&&trace.OwnArzRecord.RecordNameStringId==localNames[i],"raw body and record-name metadata retain local IDs for archive "+i);
    byte[] mapped=trace.CopyOwnRemapBody();
    Need(Same(mapped,BodyA())&&U32(mapped,4)==1&&U32(mapped,8)==2&&U32(mapped,12)==2&&U32(mapped,20)==3&&U32(mapped,24)==0xdeadbeefU,"different local body IDs converge to global Class/Item while numeric bits stay exact for archive "+i);
    Need(trace.Parser.Status=="Known"&&trace.Parser.CompletedFields==2&&trace.Parser.Record.CopyDescriptors()[0].Key==1&&trace.Parser.Record.CopyValues()[0]==2&&trace.CacheCommitted&&trace.Publication.Status=="Known","real parser and cache publication use global IDs for archive "+i);
    var value=Get(results[i].Table);
    Need(value.Status=="Known"&&!value.UsedDefault&&value.Value!=null&&Same(value.Value.CopyCStringBytes(),B("Item\0")),"actual getter resolves Class 1 to Item 2 for archive "+i);
    Need(results[i].LockReleased&&trace.ReaderDisposed&&!trace.NativeArchiveRead&&!trace.NativeFactoryRecovered&&!trace.OwnRemapStateUncertain,"successful own-file integration releases gates without a native-recovery claim for archive "+i);
   }
   Need(a.Trace.OwnRemapResult.WordsWritten==0&&a.Trace.OwnRemapResult.FieldsProcessed==0&&a.Trace.OwnRemapResult.Cursor==0&&!a.Trace.OwnRemapMayHaveWrites,"initial absent-table traversal preserves IDs without applying the later append map");
   Need(b.Trace.OwnRemapResult.WordsWritten==4&&c.Trace.OwnRemapResult.WordsWritten==4&&b.Trace.OwnRemapResult.FieldsProcessed==2&&c.Trace.OwnRemapResult.FieldsProcessed==2&&b.Trace.OwnRemapResult.Cursor==28&&c.Trace.OwnRemapResult.Cursor==28,"both appended records exercise the generated mapping traversal");
   Need(Same(a.Trace.OwnArzRecord.CopyStringBytes(1),B("Class"))&&Same(b.Trace.OwnArzRecord.CopyStringBytes(1),B("raw"))&&Same(c.Trace.OwnArzRecord.CopyStringBytes(1),B("Item")),"detached raw pools keep their distinct original orders");
   inputA.Dispose();inputB.Dispose();inputC.Dispose();
   int accessesA=streamA.Accesses,accessesB=streamB.Accesses,accessesC=streamC.Accesses;
   var ha=Attempt(depot,sourceA,"rec-a");var hb=Attempt(depot,sourceB,"rec-b");var hc=Attempt(depot,sourceC,"rec-c");
   var har=ha.RunOnce();var hbr=hb.RunOnce();var hcr=hc.RunOnce();
   Need(streamA.Closed&&streamB.Closed&&streamC.Closed&&har.CacheHit&&hbr.CacheHit&&hcr.CacheHit&&Object.ReferenceEquals(har.Table,ar.Table)&&Object.ReferenceEquals(hbr.Table,br.Table)&&Object.ReferenceEquals(hcr.Table,cr.Table),"cache hits retain all three actual tables after closing inputs");
   Need(!ha.Trace.OwnArzReadAttempted&&!hb.Trace.OwnArzReadAttempted&&!hc.Trace.OwnArzReadAttempted&&!ha.Trace.OwnRemapAttempted&&!hb.Trace.OwnRemapAttempted&&!hc.Trace.OwnRemapAttempted&&streamA.Accesses==accessesA&&streamB.Accesses==accessesB&&streamC.Accesses==accessesC,"cache hits neither access closed input nor repeat generated remapping");
   Need(Object.ReferenceEquals(a.RunOnce(),ar)&&Object.ReferenceEquals(b.RunOnce(),br)&&Object.ReferenceEquals(c.RunOnce(),cr)&&depot.LoadCount50==7&&depot.HitCount54==6,"replaying completed attempts does not repeat reads, parsers or counters");
   Need(state.MappingCount==12&&state.TargetStringCount==9&&state.CopySelectors().Length==3,"misses, hits and attempt replay do not regenerate or append the import map");
   Need(RecordLoadFile.Apply(depot,Name("rec-c"),RecordNameLocale.KnownC).CacheHit,"ordinary LoadFile sees the actual generated-map publication");
  }
  Need(Hash(fileA)==hashA&&Hash(fileB)==hashB&&Hash(fileC)==hashC,"all three original physical fixture hashes remain unchanged");
  scenarios++;
 }
 static void DuplicateInitialGap(){
  var fixture=ArzRawFixture.Build(new[]{B("rec-d"),B("Class"),B("Item"),B("Item")},new ArzRawFixtureRecord(0,ArzRawFixture.Field(2,1,2,3)));
  var raw=ReadMemory(fixture,"rec-d");var state=new ArchiveStringImportState();
  var imported=ArzRecordRemapContext.ImportChecked(state,raw,false);
  Need(imported.Status==ArchiveStringImportStatus.Completed&&!imported.SourceIdsPreserved&&!imported.Append,"deduplicating an initial source completes import but exposes loss of original IDs");
  Need(state.MappingCount==0&&state.CopySelectors().Length==0&&state.TargetStringCount==4&&!state.RequiresResolution,"initial deduplication does not invent a missing native initial remap table");
  bool rejected=false;try{ArzRecordRemapContext.FromGeneratedImport(state,imported);}catch(InvalidOperationException){rejected=true;}
  Need(rejected,"generated adapter refuses the unsupported initial duplicate-ID projection");
  Need(raw.StringCount==4&&Same(raw.CopyStringBytes(2),B("Item"))&&Same(raw.CopyStringBytes(3),B("Item"))&&state.MappingCount==0,"context rejection preserves the source duplicate slots and importer state");
  scenarios++;
 }
 static void RejectWrongBinding(ArzRawFixtureData fixture,string name,ArzRecordRemapContext context){
  var stream=new ObservedStream(new MemoryStream(fixture.Bytes,false));
  using(var input=new ResourceInput(stream)){
   var depot=Depot();var old=depot.Cache;
   var attempt=Attempt(depot,Source(input,context),name);var result=attempt.RunOnce();var trace=attempt.Trace;
   Need(result.Status=="Unsupported"&&trace.FailureStage=="OwnArzMappingSourceCheck"&&trace.OwnArzReadCompleted,"wrong generated source binding fails after own acquisition at its explicit guard");
   Need(!trace.OwnRemapAttempted&&trace.OwnRemapResult==null&&trace.CopyOwnRemapBody()==null&&!trace.OwnRemapMayHaveWrites,"source mismatch fails before allocating or writing remap scratch");
   Need(trace.Reader==null&&trace.Parser==null&&trace.Table==null&&trace.CallerPublication==null&&trace.Publication==null&&!trace.CacheCommitted,"wrong source bytes never reach parser or cache publication");
   Need(Object.ReferenceEquals(old,depot.Cache)&&depot.Cache.Count==0&&!depot.RequiresResolution&&!result.MayHaveEffects&&depot.LoadCount50==4&&result.LockReleased,"binding failure leaves the completed depot intact and releases the wrapper gate");
   input.Dispose();int accesses=stream.Accesses;
   Need(Object.ReferenceEquals(attempt.RunOnce(),result)&&stream.Accesses==accesses,"failed binding is sticky and cannot retry a disposed input");
  }
 }
 static void WrongSourceBinding(){
  var state=new ArchiveStringImportState();
  ArzRecordRemapContext.ImportChecked(state,ReadMemory(FixtureA(),"rec-a"),false);
  var importedB=ArzRecordRemapContext.ImportChecked(state,ReadMemory(FixtureB(),"rec-b"),true);
  ArzRecordRemapContext.ImportChecked(state,ReadMemory(FixtureC(),"rec-c"),true);
  var contextB=ArzRecordRemapContext.FromGeneratedImport(state,importedB);
  RejectWrongBinding(FixtureC(),"rec-c",contextB);
  byte[][] changed=PoolB();changed[4]=B("ITEM");
  RejectWrongBinding(ArzRawFixture.Build(changed,new ArzRawFixtureRecord(2,BodyB())),"rec-b",contextB);
  Need(state.MappingCount==10&&state.TargetStringCount==7&&!state.RequiresResolution,"same-count cross-archive and one-string byte mismatches do not alter the importer");
  scenarios++;
 }
 static int Main(){
  try{ThreePhysicalArchives();DuplicateInitialGap();WrongSourceBinding();
   Console.WriteLine("{\"suite\":\"RecordLoadFromArzGeneratedMapTests\",\"status\":\"passed\",\"fixed_assertions\":"+assertions+",\"scenarios\":"+scenarios+",\"scope\":\"three own physical archives through generated import maps, source binding, real parser/getter/cache and immutable snapshots; initial duplicate-ID gap remains explicit; no official samples or native compatibility claim\"}");return 0;
  }catch(Exception e){Console.Error.WriteLine(e);return 1;}
 }
}
