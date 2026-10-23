using System;
using System.IO;
using System.Security.Cryptography;
using GrimDynasty.Resources;
using GrimDynasty.TaskItems;

// Two synthetic archives and an explicitly supplied target pool/map. These
// fixtures do not infer how native archive imports build or select the map.
class RecordLoadFromArzRemapTests {
 static int assertions,scenarios;
 const ulong Address=0x12345008;
 static void Need(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static byte[] B(string s){return ResourceUtil.Utf8.GetBytes(s);}
 static bool Same(byte[] a,byte[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
 static uint U32(byte[] b,int p){return (uint)b[p]|((uint)b[p+1]<<8)|((uint)b[p+2]<<16)|((uint)b[p+3]<<24);}
 static ObjectNameString Name(string s){byte[] b=B(s);return new ObjectNameString(b,(ulong)Math.Max(15,b.Length));}
 static byte[][] PoolA(){return new[]{B("rec-a"),B("Class"),B("Item"),B("raw"),B("Item")};}
 static byte[][] PoolB(){return new[]{B("Item"),B("raw"),B("rec-b"),B("Class"),B("Item")};}
 static TableString[] TargetPool(){return new[]{new TableString(B("Class")),new TableString(B("Item")),new TableString(B("raw")),
  new TableString(B("rec-a")),new TableString(B("rec-b")),new TableString(B("Item"))};}
 static byte[] BodyA(){return ArzRawFixture.Join(ArzRawFixture.Field(2,1,2,4),ArzRawFixture.Field(0,3,0xdeadbeef));}
 static byte[] BodyB(){return ArzRawFixture.Join(ArzRawFixture.Field(2,3,0,4),ArzRawFixture.Field(0,1,0xdeadbeef));}
 static uint[] Map(){return new uint[]{3,0,1,2,1,1,2,4,0,1};}
 static ArzRawLimits Limits(){return new ArzRawLimits(16,64,131072,65536,65536,131072,65536,1048576,4194304);}
 static RecordDepotState Depot(){return new RecordDepotState(new RegistrationGate(),new RecordCacheSnapshot(8,new RecordCacheEntry[0]),3,8,1,4,5,6);}
 static ArzRecordBinarySource Source(ResourceInput input,ArzRecordRemapContext context){
  return new ArzRecordBinarySource(input,Limits(),new RegistrationGate(),
   new BinaryPointerCacheSnapshot(0,0,new[]{new[]{new BinaryNameCacheEntry(Address,0)}}),
   new BinaryNameIdSnapshot(1,new[]{new BinaryNameIdEntry(B("Class"),0)}),10,15,
   ArzRawDecodeProfile.ObservedEngineOutputSize,context);
 }
 static RecordLoadFromSuppliedBinaryAttempt Attempt(RecordDepotState depot,ArzRecordBinarySource source,string name){
  return RecordLoadFromSuppliedBinaryAttempt.FromArzRecord(depot,Name(name),RecordNameLocale.KnownC,source);
 }
 static RecordTableGetterResult Get(object table){
  return RecordTableGetter.GetValue(table,B("Class\0"),new TableString(B("fallback")),Address);
 }
 static string Hash(string path){using(var f=File.OpenRead(path))using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(f));}
 static string WriteFixture(string name,ArzRawFixtureData fixture){
  string dir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"fixtures");Directory.CreateDirectory(dir);
  string file=Path.Combine(dir,name);
  using(var f=new FileStream(file,FileMode.CreateNew,FileAccess.Write,FileShare.None))f.Write(fixture.Bytes,0,fixture.Bytes.Length);
  return file;
 }
 static void TwoArchives(){
  byte[] first=BodyA(),second=BodyB();
  string fileA=WriteFixture("own-remap-a.arz",ArzRawFixture.Build(PoolA(),new ArzRawFixtureRecord(0,first)));
  string fileB=WriteFixture("own-remap-b.arz",ArzRawFixture.Build(PoolB(),new ArzRawFixtureRecord(2,second)));
  string hashA=Hash(fileA),hashB=Hash(fileB);
  var map=Map();var destination=TargetPool();var item=destination[1];
  var contextA=new ArzRecordRemapContext(new RecordStringRemapContext(true,map,10,0),destination);
  var contextB=new ArzRecordRemapContext(new RecordStringRemapContext(true,map,10,5),destination);
  map[1]=999;destination[1]=new TableString(B("changed-after-snapshot"));
  var depot=Depot();
  using(var inputA=new ResourceInput(new FileStream(fileA,FileMode.Open,FileAccess.Read,FileShare.Read)))
  using(var inputB=new ResourceInput(new FileStream(fileB,FileMode.Open,FileAccess.Read,FileShare.Read))){
   var sourceA=Source(inputA,contextA);var sourceB=Source(inputB,contextB);
   var a=Attempt(depot,sourceA,"REC-A");var b=Attempt(depot,sourceB,"REC-B");
   Need(!a.Attempted&&!b.Attempted&&Object.ReferenceEquals(sourceA.RemapContext,contextA),"explicit remap is deferred until a real miss");
   var ar=a.RunOnce();var br=b.RunOnce();
   Need(ar.Status=="Known"&&br.Status=="Known"&&!ar.CacheHit&&!br.CacheHit&&depot.Cache.Count==2,"two independently read archives reach the actual shared depot");
   Need(a.Trace.OwnRemapResult.Status==RecordStringRemapStatus.Completed&&b.Trace.OwnRemapResult.Status==RecordStringRemapStatus.Completed,"both checked remap traversals complete");
   Need(a.Trace.OwnRemapResult.FieldsProcessed==2&&a.Trace.OwnRemapResult.WordsWritten==4&&a.Trace.OwnRemapResult.Cursor==28,"two key and two string writes are counted");
   Need(Same(a.Trace.OwnArzRecord.CopyBodyBytes(),first)&&Same(b.Trace.OwnArzRecord.CopyBodyBytes(),second),"detached source record bodies remain the original local IDs");
   Need(a.Trace.OwnArzRecord.RecordNameStringId==0&&b.Trace.OwnArzRecord.RecordNameStringId==2,"record-name metadata is not passed through body remapping");
   byte[] transformed=a.Trace.CopyOwnRemapBody();
   Need(Same(transformed,b.Trace.CopyOwnRemapBody())&&U32(transformed,4)==0&&U32(transformed,8)==1&&U32(transformed,12)==1,"different source string orders converge through the supplied map, including duplicate strings");
   Need(U32(transformed,20)==2&&U32(transformed,24)==0xdeadbeef,"numeric key is remapped but its nonstring payload stays exact");
   Need(a.Trace.Parser.CompletedFields==2&&b.Trace.Parser.CompletedFields==2&&a.Trace.CacheCommitted&&b.Trace.CacheCommitted,"real mixed parsers and publication operate after remapping");
   var av=Get(ar.Table);var bv=Get(br.Table);
   Need(av.Status=="Known"&&bv.Status=="Known"&&!av.UsedDefault&&!bv.UsedDefault&&Object.ReferenceEquals(av.Value,item)&&Object.ReferenceEquals(bv.Value,item),"both actual getters resolve the same immutable target string identity");
   Need(Same(a.Trace.OwnArzRecord.CopyStringBytes(1),B("Class"))&&Same(b.Trace.OwnArzRecord.CopyStringBytes(1),B("raw"))&&contextA.TargetStringCount==6,"source pools remain distinct and target duplicates are retained");
   transformed[4]=99;
   Need(U32(a.Trace.CopyOwnRemapBody(),4)==0,"diagnostic body export cannot mutate the retained scratch");
   Need(a.Trace.OwnRemapMayHaveWrites&&!a.Trace.OwnRemapStateUncertain&&!a.Trace.NativeArchiveRead&&!sourceA.NativeArchiveFetchRecovered,"private remap writes are recorded without claiming native archive recovery");
   inputA.Dispose();inputB.Dispose();
   var ha=Attempt(depot,sourceA,"rec-a");var hb=Attempt(depot,sourceB,"rec-b");
   Need(ha.RunOnce().CacheHit&&hb.RunOnce().CacheHit&&!ha.Trace.OwnArzReadAttempted&&!hb.Trace.OwnRemapAttempted,"cached loads do not touch closed archives or repeat remapping");
   Need(Object.ReferenceEquals(a.RunOnce(),ar)&&Object.ReferenceEquals(b.RunOnce(),br)&&depot.LoadCount50==6&&depot.HitCount54==5,"attempt replay preserves actual miss/hit counters");
   Need(RecordLoadFile.Apply(depot,Name("rec-a"),RecordNameLocale.KnownC).CacheHit,"ordinary wrapper observes the remapped table");
  }
  Need(Hash(fileA)==hashA&&Hash(fileB)==hashB,"both physical archive files remain unchanged");
  scenarios++;
 }
 static void PartialFailure(byte[] body,RecordStringRemapContext mapping,int words,int cursor,uint firstValue){
  var fixture=ArzRawFixture.Build(PoolA(),new ArzRawFixtureRecord(0,body));
  using(var input=new ResourceInput(new MemoryStream(fixture.Bytes,false))){
   var depot=Depot();var old=depot.Cache;
   var attempt=Attempt(depot,Source(input,new ArzRecordRemapContext(mapping,TargetPool())),"rec-a");
   var result=attempt.RunOnce();var trace=attempt.Trace;
   Need(result.Status=="Unsupported"&&result.Reason.StartsWith("OwnArzRecordReadUnsupported:ArzRecordRemapUnsupported:",StringComparison.Ordinal),"checked remap guard is reported as own coverage, not a native rejection");
   Need(trace.OwnArzReadCompleted&&trace.OwnRemapAttempted&&trace.OwnRemapResult.Status==RecordStringRemapStatus.Unsupported&&trace.FailureStage=="OwnArzRecordRemap","failure retains acquisition and precise remap stage");
   Need(trace.OwnRemapResult.WordsWritten==words&&trace.OwnRemapResult.Cursor==cursor&&trace.OwnRemapResult.FieldsProcessed==0,"partial writes and cursor retain observed native operation order");
   byte[] partial=trace.CopyOwnRemapBody();
   Need(U32(partial,4)==0&&U32(partial,8)==firstValue&&Same(trace.OwnArzRecord.CopyBodyBytes(),body),"already written key/value remain visible while detached raw input is preserved");
   Need(trace.OwnRemapMayHaveWrites&&!trace.OwnRemapStateUncertain&&trace.Parser==null&&trace.Table==null&&trace.CallerPublication==null,"partial private scratch is not parsed or published");
   Need(Object.ReferenceEquals(old,depot.Cache)&&depot.Cache.Count==0&&!depot.RequiresResolution&&!result.MayHaveEffects&&depot.LoadCount50==4,"private scratch writes are separate from modeled parser/cache/depot mutation");
   input.Dispose();Need(Object.ReferenceEquals(attempt.RunOnce(),result)&&Same(partial,trace.CopyOwnRemapBody())&&result.LockReleased,"failed attempt is sticky and cannot reapply the additive remap");
  }
 }
 static void PartialStates(){
  PartialFailure(BodyA(),new RecordStringRemapContext(true,new uint[]{3,0},10,0),1,12,2);
  byte[] shortBody=ArzRawFixture.Field(2,1,2);ArzRawFixture.U16(shortBody,2,2);
  PartialFailure(shortBody,new RecordStringRemapContext(true,Map(),10,0),2,12,1);
  scenarios++;
 }
 static void OutOfPool(){
  byte[] body=ArzRawFixture.Join(ArzRawFixture.Field(2,1,uint.MaxValue,77),ArzRawFixture.Field(0,3,123));
  var fixture=ArzRawFixture.Build(PoolA(),new ArzRawFixtureRecord(0,body));
  using(var input=new ResourceInput(new MemoryStream(fixture.Bytes,false))){
   var attempt=Attempt(Depot(),Source(input,new ArzRecordRemapContext(new RecordStringRemapContext(true,Map(),10,0),TargetPool())),"rec-a");
   var result=attempt.RunOnce();byte[] mapped=attempt.Trace.CopyOwnRemapBody();
   Need(result.Status=="Known"&&attempt.Trace.OwnRemapResult.Status==RecordStringRemapStatus.Completed,"out-of-map IDs do not become invented remap errors");
   Need(U32(mapped,8)==uint.MaxValue&&U32(mapped,12)==77,"negative and beyond-count IDs retain their wrapped sums");
   var value=Get(result.Table);
   Need(value.Status=="Known"&&!value.UsedDefault&&value.Value==null&&value.Binary.StringIndex==-1,"existing getter, not remapper, returns known null for an out-of-pool index");
   Need(attempt.Trace.Parser.Record.CopyRawValues(2)[1]==77&&!attempt.Trace.NativeArchiveRead,"parser preserves a positive out-of-pool word too");
  }
  scenarios++;
 }
 static int Main(){
  try{TwoArchives();PartialStates();OutOfPool();
   Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+assertions+",\"scenarios\":"+scenarios+",\"scope\":\"two synthetic archives with explicit mapping/target pool through decoder, body remap, parser and cache; partial private scratch retained; no native archive merge or official compatibility claim\"}");return 0;
  }catch(Exception e){Console.Error.WriteLine(e);return 1;}
 }
}
