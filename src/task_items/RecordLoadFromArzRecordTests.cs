using System;
using System.IO;
using System.Security.Cryptography;
using GrimDynasty.Resources;
using GrimDynasty.TaskItems;

// Complete synthetic ARZv3 containers only. No game archive or native execution.
class RecordLoadFromArzRecordTests {
 static int assertions,scenarios;
 const ulong QueryAddress=0x12345008;
 static void Need(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static byte[] B(string text){return ResourceUtil.Utf8.GetBytes(text);}
 static bool Same(byte[] a,byte[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
 static ObjectNameString Name(string text){byte[] bytes=B(text);return new ObjectNameString(bytes,(ulong)Math.Max(15,bytes.Length));}
 static TableString S(string text){return new TableString(B(text));}
 static ArzRawLimits Limits(int strings=64,int packed=65536,int body=1048576){
  return new ArzRawLimits(16,strings,131072,65536,65536,131072,packed,body,4194304);
 }
 static byte[] Body(){
  return ArzRawFixture.Join(ArzRawFixture.Field(2,1,2),
   ArzRawFixture.Field(0,3,0xdeadbeefU,0x80000000U));
 }
 static byte[][] Pool(string name){return new[]{B(name),B("Class"),B("Item"),B("raw"),B("Class"),new byte[0]};}
 static ArzRawFixtureData Fixture(string name){return ArzRawFixture.Build(Pool(name),new ArzRawFixtureRecord(0,Body()));}
 static RecordCacheSnapshot Empty(){return new RecordCacheSnapshot(8,new RecordCacheEntry[0]);}
 static RecordDepotState Depot(RecordCacheSnapshot cache,float load){
  return new RecordDepotState(new RegistrationGate(),cache,3,cache.BucketCount,load,4,5,6);
 }
 static ArzRecordBinarySource Source(ResourceInput input,ArzRawLimits limits,uint? start,uint? end){
  return new ArzRecordBinarySource(input,limits,new RegistrationGate(),
   new BinaryPointerCacheSnapshot(0,0,new[]{new[]{new BinaryNameCacheEntry(QueryAddress,1)}}),
   new BinaryNameIdSnapshot(1,new[]{new BinaryNameIdEntry(B("Class"),1)}),start,end);
 }
 static RecordLoadFromSuppliedBinaryAttempt Attempt(RecordDepotState depot,string name,ArzRecordBinarySource source){
  return RecordLoadFromSuppliedBinaryAttempt.FromArzRecord(depot,Name(name),RecordNameLocale.KnownC,source);
 }
 static RecordTableGetterResult Class(object table){
  return RecordTableGetter.GetValue(table,B("Class\0"),S("fallback"),QueryAddress);
 }
 sealed class ObservedStream:Stream {
  readonly Stream inner;
  readonly long payloadBegin,payloadEnd;
  public int Accesses,ReadCalls;
  public long PayloadBytesRead;
  public bool Closed;
  public ObservedStream(Stream stream,ArzRawFixtureData fixture){
   inner=stream;payloadBegin=fixture.PayloadOffsets[0];payloadEnd=fixture.RecordTableOffset;
  }
  public override bool CanRead{get{return inner.CanRead;}}
  public override bool CanSeek{get{return inner.CanSeek;}}
  public override bool CanWrite{get{return false;}}
  public override long Length{get{Accesses++;return inner.Length;}}
  public override long Position{get{Accesses++;return inner.Position;}set{Accesses++;inner.Position=value;}}
  public override int Read(byte[] bytes,int offset,int count){
   Accesses++;ReadCalls++;long start=inner.Position;int read=inner.Read(bytes,offset,count);
   PayloadBytesRead+=Math.Max(0,Math.Min(start+read,payloadEnd)-Math.Max(start,payloadBegin));
   return read;
  }
  public override long Seek(long offset,SeekOrigin origin){Accesses++;return inner.Seek(offset,origin);}
  public override void Flush(){}
  public override void SetLength(long value){throw new NotSupportedException();}
  public override void Write(byte[] bytes,int offset,int count){throw new NotSupportedException();}
  protected override void Dispose(bool disposing){Closed=true;if(disposing)inner.Dispose();base.Dispose(disposing);}
 }
 static ObservedStream Memory(ArzRawFixtureData fixture){return new ObservedStream(new MemoryStream(fixture.Bytes,false),fixture);}
 static string HashFile(string path){
  using(var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))
  using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(file)).Replace("-","");
 }
 static void FileToGetterAndDetachedHitScenario(){
  var fixture=Fixture("dir/file");
  string directory=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"fixtures");
  Directory.CreateDirectory(directory);
  string path=Path.Combine(directory,"own-two-field.arz");
  // The parent supplies a fresh output directory. Never replace a saved fixture.
  using(var output=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None))
   output.Write(fixture.Bytes,0,fixture.Bytes.Length);
  string before=HashFile(path);
  var stream=new ObservedStream(new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read),fixture);
  using(var input=new ResourceInput(stream)){
   var source=Source(input,Limits(),100,105);var depot=Depot(Empty(),1);
   var attempt=Attempt(depot,"Dir\\FILE",source);
   Need(!attempt.Attempted&&stream.Accesses==0&&!source.NativeArchiveFetchRecovered,"source/attempt construction performs no archive access and makes no native-fetch claim");
   var result=attempt.RunOnce();var trace=attempt.Trace;
   Need(result.Status=="Known"&&!result.CacheHit&&Object.ReferenceEquals(result.Table,trace.Table)&&trace.OwnArzReadAttempted&&trace.OwnArzReadCompleted,"real miss consumes the complete own ARZ and returns the parsed table identity");
   Need(trace.OwnArzRecord.RecordNameStringId==0&&trace.OwnArzRecord.StringCount==6&&Same(trace.OwnArzRecord.CopyStringBytes(1),trace.OwnArzRecord.CopyStringBytes(4))&&trace.OwnArzRecord.CopyStringBytes(5).Length==0,"projection retains every original pool ID including duplicate and empty slots");
   Need(Same(trace.OwnArzRecord.CopyBodyBytes(),Body())&&trace.Parser.Status=="Known"&&trace.Parser.CompletedFields==2&&trace.Parser.Record.CopyDescriptors()[0].Key==1&&trace.Parser.Record.CopyValues()[0]==2&&trace.Parser.Record.CopyRawValues(0)[1]==0x80000000U,"selected LZ4 bytes feed the real two-pass parser without reencoding fields or remapping IDs");
   Need(stream.PayloadBytesRead==fixture.RecordTableOffset-fixture.PayloadOffsets[0]&&!stream.Closed&&trace.ReaderDisposed&&trace.Reader.IsDisposed&&trace.Reader.Consumed,"only the actual packed block is read once while the borrowed source remains open");
   Need(trace.CacheCommitted&&trace.Publication.Status=="Known"&&depot.Cache.Count==1&&!depot.RequiresResolution&&depot.LoadCount50==5&&depot.HitCount54==3&&depot.ElapsedTotal5C==10&&depot.ElapsedMaximum60==6,"real publication and miss tail update the same depot once");
   var getter=Class(result.Table);
   Need(getter.Status=="Known"&&!getter.UsedDefault&&Same(getter.Value.CopyCStringBytes(),B("Item\0"))&&getter.GateReleased&&!trace.Table.Gate.IsHeldByCurrentThread,"actual Class getter follows parsed field ID 1 to original pool ID 2 outside the table gate");
   Need(!trace.NativeArchiveRead&&!trace.NativeFactoryRecovered&&result.LockReleased&&!depot.Gate.IsHeldByCurrentThread&&Same(result.CopyNormalizedName(),B("dir/file")),"normalization and own cleanup do not imply recovered native archive I/O or factory");
   input.Dispose();int access=stream.Accesses;
   Need(stream.Closed&&HashFile(path)==before,"caller closes the read-only source and fixture SHA256 remains unchanged");
   byte[] exported=trace.OwnArzRecord.CopyBodyBytes();exported[0]=0xff;
   Need(Object.ReferenceEquals(Class(result.Table).Value,getter.Value)&&Same(trace.OwnArzRecord.CopyBodyBytes(),Body()),"detached raw exports and actual getter storage survive closing the source");
   var hitAttempt=Attempt(depot,"DIR\\FILE",source);var hit=hitAttempt.RunOnce();
   Need(hit.Status=="Known"&&hit.CacheHit&&Object.ReferenceEquals(hit.Table,result.Table)&&!hitAttempt.Trace.Entered&&!hitAttempt.Trace.OwnArzReadAttempted&&stream.Accesses==access&&depot.HitCount54==4,"a new attempt hits the same table without even touching its disposed source");
   Need(Object.ReferenceEquals(attempt.RunOnce(),result)&&stream.Accesses==access&&depot.LoadCount50==5&&depot.HitCount54==4&&trace.Parser.CompletedFields==2,"replaying the original completed attempt performs neither read, parse nor counters");
   var ordinary=RecordLoadFile.Apply(depot,Name("dir/file"),RecordNameLocale.KnownC);
   Need(ordinary.Status=="Known"&&ordinary.CacheHit&&Object.ReferenceEquals(ordinary.Table,result.Table)&&ordinary.SuppliedMiss==null&&stream.Accesses==access&&depot.HitCount54==5,"ordinary cache-only LoadFile uses the actual newly published identity");
  }
  scenarios++;
 }
 static void RejectRead(ArzRawFixtureData fixture,string requested,ArzRawLimits limits,string reason,bool payloadRead){
  var stream=Memory(fixture);
  using(var input=new ResourceInput(stream)){
   var depot=Depot(Empty(),1);var old=depot.Cache;var attempt=Attempt(depot,requested,Source(input,limits,10,11));
   var result=attempt.RunOnce();var trace=attempt.Trace;
   Need(result.Status=="Unsupported"&&result.Reason=="OwnArzRecordReadUnsupported:"+reason&&trace.FailureStage=="OwnArzRecordRead"&&trace.OwnArzReadAttempted&&!trace.OwnArzReadCompleted,"own ARZ refusal remains explicit: "+reason);
   Need(trace.Parser==null&&trace.Reader==null&&trace.Table==null&&trace.Publication==null&&!trace.NativeArchiveRead&&!result.MayHaveEffects&&!depot.RequiresResolution&&Object.ReferenceEquals(depot.Cache,old)&&depot.LoadCount50==4&&depot.HitCount54==3,"failed raw acquisition publishes no table or modeled cache effects");
   Need((stream.PayloadBytesRead>0)==payloadRead&&result.LockReleased&&!depot.Gate.IsHeldByCurrentThread&&!stream.Closed,"only failures after decompression input can read the packed span; caller owns source lifetime");
   input.Dispose();int accesses=stream.Accesses;
   Need(Object.ReferenceEquals(attempt.RunOnce(),result)&&stream.Accesses==accesses&&trace.OwnArzRecord==null,"failed attempt is sticky even after source disposal");
  }
 }
 static void ExactNamesAndReadFailures(){
  RejectRead(Fixture("REC"),"rec",Limits(),"ArzRawRecordNotFound",false);
  RejectRead(Fixture("rec\0"),"rec",Limits(),"ArzRawRecordNotFound",false);
  RejectRead(Fixture("rec"),"rec\0tail",Limits(),"ArzRawRecordNotFound",false);
  RejectRead(Fixture("other"),"missing",Limits(),"ArzRawRecordNotFound",false);
  var bad=ArzRawFixture.Build(Pool("bad"),
   new ArzRawFixtureRecord(0,new byte[]{0x10,65,0,0},(uint)Body().Length,B("Item")));
  RejectRead(bad,"bad",Limits(),"match offset",true);
  RejectRead(Fixture("rec"),"rec",Limits(packed:1),"ArzRawPackedBytesBudget",false);
  RejectRead(Fixture("rec"),"rec",Limits(body:Body().Length-1),"ArzRawBodyBytesBudget",false);
  var exact=Fixture("rec\0tail");var stream=Memory(exact);
  using(var input=new ResourceInput(stream)){
   var depot=Depot(Empty(),1);var attempt=Attempt(depot,"REC\0TAIL",Source(input,Limits(),0,1));var result=attempt.RunOnce();
   Need(result.Status=="Known"&&Same(result.CopyNormalizedName(),B("rec\0tail"))&&Same(attempt.Trace.OwnArzRecord.CopyRecordNameBytes(),B("rec\0tail")),"a full stored NUL-containing name succeeds only when every normalized byte matches");
   Need(depot.Cache.Find(B("rec\0tail")).Found&&!depot.Cache.Find(B("rec")).Found&&Class(result.Table).Status=="Known","publication keeps the full NUL suffix instead of silently trimming it");
  }
 }
 static void SourceBudgetsAndLazyGuards(){
  var many=new byte[4140][];for(int i=0;i<many.Length;i++)many[i]=new byte[0];
  many[0]=B("rec");many[1]=B("Class");many[2]=B("Item");
  var fixture=ArzRawFixture.Build(many,new ArzRawFixtureRecord(0,Body()));var stream=Memory(fixture);
  using(var input=new ResourceInput(stream)){
   bool refused=false;try{Source(input,Limits(strings:4140),0,1);}catch(ArgumentOutOfRangeException){refused=true;}
   Need(refused&&stream.Accesses==0,"source rejects a 4140-string allowance before touching input, rather than truncating/reindexing an archive pool");
   var depot=Depot(Empty(),1);var attempt=Attempt(depot,"rec",Source(input,Limits(strings:4096),0,1));var result=attempt.RunOnce();
   Need(result.Status=="Unsupported"&&result.Reason=="OwnArzRecordReadUnsupported:ArzRawStringCountBudget"&&stream.PayloadBytesRead==0&&attempt.Trace.Parser==null&&depot.Cache.Count==0,"actual 4140-slot input also stops before reading any payload under the valid 4096 limit");
  }
  var large=ArzRawFixture.Build(Pool("large"),new ArzRawFixtureRecord(0,new byte[BinaryRecordReader.MaximumBytes+1]));
  stream=Memory(large);
  using(var input=new ResourceInput(stream)){
   bool refused=false;try{Source(input,Limits(packed:2097152,body:BinaryRecordReader.MaximumBytes+1),0,1);}catch(ArgumentOutOfRangeException){refused=true;}
   Need(refused&&stream.Accesses==0,"source rejects body coverage above 1MiB before reading or allocating a parser body");
   var depot=Depot(Empty(),1);var attempt=Attempt(depot,"large",Source(input,Limits(packed:2097152),0,1));var result=attempt.RunOnce();
   Need(result.Status=="Unsupported"&&result.Reason=="OwnArzRecordReadUnsupported:ArzRawBodyBytesBudget"&&stream.PayloadBytesRead==0&&attempt.Trace.Parser==null&&!depot.RequiresResolution,"a valid bounded source refuses an actual larger declared body before packed reads instead of widening parser coverage");
  }
  fixture=Fixture("rec");stream=Memory(fixture);
  using(var input=new ResourceInput(stream)){
   var depot=Depot(Empty(),1);var source=Source(input,Limits(),0,1);
   var unknown=RecordLoadFromSuppliedBinaryAttempt.FromArzRecord(depot,Name("rec"),RecordNameLocale.Unknown,source);var a=unknown.RunOnce();
   Need(a.Status=="Unsupported"&&a.Reason=="UnknownCrtToLowerLocale"&&!unknown.Trace.OwnArzReadAttempted&&stream.Accesses==0,"unknown normalizer locale stops before the optional own source");
   var empty=Attempt(depot,"",source);var b=empty.RunOnce();
   Need(b.Status=="Unsupported"&&b.Reason=="SuppliedBinaryMissEmptyNameTakesFallback"&&!empty.Trace.OwnArzReadAttempted&&stream.Accesses==0,"empty-name native fallback is not replaced by the own ARZ path");
   var ordinary=RecordLoadFile.Apply(depot,Name("rec"),RecordNameLocale.KnownC);
   Need(ordinary.Status=="Unsupported"&&ordinary.Reason=="UnimplementedRecordCacheMissLoader"&&stream.Accesses==0,"ordinary miss still refuses instead of silently adopting an external source");
  }
 }
 static void ParserRefusalRetainsRawScenario(){
  byte[] body=ArzRawFixture.Join(ArzRawFixture.Field(4,1,2),ArzRawFixture.Field(0,3,99));
  var fixture=ArzRawFixture.Build(Pool("badtag"),new ArzRawFixtureRecord(0,body));var stream=Memory(fixture);
  using(var input=new ResourceInput(stream)){
   var depot=Depot(Empty(),1);var attempt=Attempt(depot,"badtag",Source(input,Limits(),10,11));var result=attempt.RunOnce();var trace=attempt.Trace;
   Need(result.Status=="Unsupported"&&result.Reason=="BinaryRecordTagOutsideCoveredDomain"&&trace.FailureStage=="RecordParse"&&trace.OwnArzReadCompleted&&Same(trace.OwnArzRecord.CopyBodyBytes(),body),"valid ARZ/LZ4 retains its real raw body when the parser rejects unsupported tag 4");
   Need(trace.Parser.DomainRejected&&trace.Parser.Record==null&&!trace.Parser.MayHaveEffects&&trace.ReaderDisposed&&!trace.Reader.Consumed&&trace.Reader.IsDisposed&&trace.Reader.Position==0,"parser preflight stop neither consumes bytes nor fabricates parsed fields, while own reader cleanup runs");
   Need(trace.Table!=null&&result.Table==null&&trace.CallerPublication==null&&trace.TemporaryPair==null&&depot.Cache.Count==0&&!depot.RequiresResolution&&!result.MayHaveEffects,"fresh table preparation is not published as an empty successful parse");
   Need(depot.LoadCount50==4&&depot.HitCount54==3&&depot.ElapsedTotal5C==5&&result.LockReleased&&!depot.Gate.IsHeldByCurrentThread,"parser rejection retains counters and releases the outer gate");
   input.Dispose();int accesses=stream.Accesses;var raw=trace.OwnArzRecord;var parser=trace.Parser;
   Need(Object.ReferenceEquals(attempt.RunOnce(),result)&&Object.ReferenceEquals(trace.OwnArzRecord,raw)&&Object.ReferenceEquals(trace.Parser,parser)&&stream.Accesses==accesses,"once-only parser refusal retains diagnostic identities without rereading disposed input");
  }
  scenarios++;
 }
 static void GrowthPendingScenario(){
  var fixture=Fixture("new");var stream=Memory(fixture);
  using(var input=new ResourceInput(stream)){
   object oldTable=new object();var old=new RecordCacheSnapshot(4096,new[]{new RecordCacheEntry(B("old"),oldTable)});
   var depot=Depot(old,1.0f/4096.0f);var source=Source(input,Limits(),10,11);var attempt=Attempt(depot,"new",source);
   var result=attempt.RunOnce();var trace=attempt.Trace;
   Need(result.Status=="Unsupported"&&result.Reason=="RecordCacheGrowthBucketBudgetOutsideCoveredDomain"&&trace.FailureStage=="CallerPublication"&&trace.OwnArzReadCompleted&&trace.Parser.Status=="Known","real ARZ parse completes before the real publication growth boundary stops");
   Need(trace.Publication.CountIncremented&&trace.Publication.HeadPreviousWritten&&trace.Publication.PreviousNextWritten&&trace.CallerPublication.Count==2&&trace.TemporaryPair.Name.Length==0&&trace.ReaderDisposed,"growth stop retains caller links/count, source-name move and disposed-reader prefix");
   Need(depot.RequiresResolution&&depot.Cache==null&&Object.ReferenceEquals(depot.LastCompletedCache,old)&&Object.ReferenceEquals(depot.PendingMiss,trace)&&depot.CurrentCacheCount==2&&result.MayHaveEffects&&!trace.CacheCommitted,"unresolved graph cannot be replaced with the old completed cache");
   Need(Class(trace.Table).Status=="Known"&&depot.LoadCount50==4&&depot.HitCount54==3&&depot.ElapsedTotal5C==5&&!trace.ElapsedRecorded&&result.LockReleased,"retained parsed table is inspectable but publication failure runs no miss tail");
   input.Dispose();int accesses=stream.Accesses;var blocked=Attempt(depot,"old",source);var failedHit=blocked.RunOnce();
   Need(failedHit.Status=="Unsupported"&&failedHit.Reason=="RecordDepotMissRequiresResolution"&&!blocked.Trace.OwnArzReadAttempted&&stream.Accesses==accesses&&depot.HitCount54==3,"pending state blocks even an old-key hit without rereading the source");
   Need(Object.ReferenceEquals(attempt.RunOnce(),result)&&stream.Accesses==accesses&&trace.CallerPublication.Count==2&&depot.LoadCount50==4,"growth-stop replay neither rereads nor duplicates the linked candidate");
  }
  scenarios++;
 }
 static void UnknownEndPendingScenario(){
  var fixture=Fixture("end");var stream=Memory(fixture);
  using(var input=new ResourceInput(stream)){
   var old=Empty();var depot=Depot(old,1);var source=Source(input,Limits(),10,null);
   var attempt=Attempt(depot,"end",source);var result=attempt.RunOnce();var trace=attempt.Trace;
   Need(result.Status=="Unsupported"&&result.Reason=="UnknownConvertedMissEndTick"&&trace.FailureStage=="ConvertedMissEndTick"&&trace.OwnArzReadCompleted&&trace.Parser.Status=="Known","unknown injected end tick stops after actual archive decode and publication");
   Need(trace.CacheCommitted&&trace.LoadCountIncremented&&!trace.ElapsedRecorded&&!trace.Elapsed.HasValue&&depot.LoadCount50==5&&depot.ElapsedTotal5C==5&&depot.ElapsedMaximum60==6,"tail stop preserves load count without inventing elapsed time");
   Need(depot.RequiresResolution&&depot.Cache==null&&depot.LastCompletedCache.Count==1&&Object.ReferenceEquals(depot.LastCompletedCache.Find(B("end")).Entry.Table,trace.Table)&&old.Count==0,"completed-cache projection is retained diagnostically but is not exposed as live");
   Need(trace.ReaderDisposed&&trace.TemporaryPair.Name.Length==0&&Class(trace.Table).Status=="Known"&&result.Table==null&&result.MayHaveEffects&&result.LockReleased,"a valid retained table and own cleanup do not turn partial tail execution into outer success");
   input.Dispose();int accesses=stream.Accesses;var blocked=Attempt(depot,"end",source);var hit=blocked.RunOnce();
   Need(hit.Status=="Unsupported"&&hit.Reason=="RecordDepotMissRequiresResolution"&&!blocked.Trace.OwnArzReadAttempted&&stream.Accesses==accesses&&depot.HitCount54==3,"unresolved end tick prevents ordinary hit exposure without another file read");
   Need(Object.ReferenceEquals(attempt.RunOnce(),result)&&stream.Accesses==accesses&&depot.LoadCount50==5&&depot.LastCompletedCache.Count==1,"sticky end-tick failure repeats neither archive read nor already committed publication");
  }
  scenarios++;
 }
 static int Main(){
  try{
   FileToGetterAndDetachedHitScenario();ExactNamesAndReadFailures();SourceBudgetsAndLazyGuards();
   ParserRefusalRetainsRawScenario();GrowthPendingScenario();UnknownEndPendingScenario();
   Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+assertions+",\"scenarios\":"+scenarios+",\"scope\":\"complete own ARZv3 fixtures through raw LZ4, unchanged bounded parser, publication and real Class getter; detached cache hits and explicit failure prefixes; no native archive compatibility or original-game execution claim\"}");
   return 0;
  }catch(Exception error){Console.Error.WriteLine(error);return 1;}
 }
}
