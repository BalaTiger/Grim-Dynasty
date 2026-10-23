using System;
using System.IO;
using System.Security.Cryptography;
using GrimDynasty.Resources;
using GrimDynasty.TaskItems;

// Own complete ARZ files only. No native binary invocation or official sample.
class RecordLoadFromArzProfileTests {
 static int assertions,scenarios;
 const ulong QueryAddress=0x12345008;
 static void Need(bool v,string why){assertions++;if(!v)throw new Exception(why);}
 static byte[] B(string s){return ResourceUtil.Utf8.GetBytes(s);}
 static bool Same(byte[] a,byte[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
 static ObjectNameString Name(string s){byte[] b=B(s);return new ObjectNameString(b,(ulong)Math.Max(15,b.Length));}
 static byte[] Body(){return ArzRawFixture.Join(ArzRawFixture.Field(2,1,2),ArzRawFixture.Field(0,3,0xdeadbeefU,0x80000000U));}
 static byte[][] Pool(){return new[]{B("rec"),B("Class"),B("Item"),B("raw"),B("Class"),new byte[0]};}
 static ArzRawLimits Limits(long working=4194304){return new ArzRawLimits(16,64,131072,65536,65536,131072,65536,1048576,working);}
 static ArzRawFixtureData Fixture(byte[] packed,uint bodyBytes){return ArzRawFixture.Build(Pool(),new ArzRawFixtureRecord(0,packed,bodyBytes,B("Item")));}
 static RecordDepotState Depot(){var cache=new RecordCacheSnapshot(8,new RecordCacheEntry[0]);return new RecordDepotState(new RegistrationGate(),cache,3,8,1,4,5,6);}
 static ArzRecordBinarySource Source(ResourceInput input,ArzRawDecodeProfile profile){
  return new ArzRecordBinarySource(input,Limits(),new RegistrationGate(),
   new BinaryPointerCacheSnapshot(0,0,new[]{new[]{new BinaryNameCacheEntry(QueryAddress,1)}}),
   new BinaryNameIdSnapshot(1,new[]{new BinaryNameIdEntry(B("Class"),1)}),10,15,profile);
 }
 static RecordLoadFromSuppliedBinaryAttempt Attempt(RecordDepotState depot,ArzRecordBinarySource source){
  return RecordLoadFromSuppliedBinaryAttempt.FromArzRecord(depot,Name("REC"),RecordNameLocale.KnownC,source);
 }
 static string Hash(string path){using(var f=File.OpenRead(path))using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(f));}
 static void ActualFileAndHit(){
  byte[] body=Body(),canonical=ArzRawFixture.LiteralBlock(body);
  byte[] packed=ArzRawFixture.Join(canonical,new byte[]{0xaa});
  var fixture=Fixture(packed,(uint)body.Length);
  string dir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"fixtures");Directory.CreateDirectory(dir);
  string file=Path.Combine(dir,"own-output-profile.arz");
  using(var output=new FileStream(file,FileMode.CreateNew,FileAccess.Write,FileShare.None))output.Write(fixture.Bytes,0,fixture.Bytes.Length);
  string before=Hash(file);
  using(var input=new ResourceInput(new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.Read))){
   bool refused=false;try{ArzRawRecordReader.ReadExact(input,B("rec"),Limits());}catch(InvalidDataException e){refused=e.Message=="truncated offset";}
   Need(refused,"the unchanged default input-bounded decoder still rejects the extra partial offset");
   var raw=ArzRawRecordReader.ReadExact(input,B("rec"),Limits(),ArzRawDecodeProfile.ObservedEngineOutputSize);
   Need(raw.DecodeProfile==ArzRawDecodeProfile.ObservedEngineOutputSize&&raw.PackedBytesConsumed==canonical.Length&&raw.PackedBytes==packed.Length,"explicit profile returns consumed prefix rather than pretending to consume every packed byte");
   Need(Same(raw.CopyBodyBytes(),body)&&raw.StringCount==6&&raw.CopyStringBytes(5).Length==0,"actual file profile retains raw words and unchanged complete string pool");
   var depot=Depot();var source=Source(input,ArzRawDecodeProfile.ObservedEngineOutputSize);var attempt=Attempt(depot,source);
   Need(source.DecodeProfile==ArzRawDecodeProfile.ObservedEngineOutputSize&&!source.NativeArchiveFetchRecovered&&!attempt.Attempted,"profile selection is explicit and does not claim a native archive");
   var result=attempt.RunOnce();var trace=attempt.Trace;
   Need(result.Status=="Known"&&!result.CacheHit&&trace.Parser.Status=="Known"&&trace.Parser.CompletedFields==2,"real output-profile bytes reach the real mixed parser");
   Need(trace.OwnArzRecord.DecodeProfile==ArzRawDecodeProfile.ObservedEngineOutputSize&&trace.OwnArzRecord.PackedBytesConsumed==canonical.Length&&Same(trace.OwnArzRecord.CopyBodyBytes(),body),"trace preserves profile and compressed input consumption");
   Need(trace.CacheCommitted&&depot.Cache.Count==1&&Object.ReferenceEquals(result.Table,trace.Table)&&!depot.RequiresResolution,"same table is published into the actual cache");
   var value=RecordTableGetter.GetValue(result.Table,B("Class\0"),new TableString(B("fallback")),QueryAddress);
   Need(value.Status=="Known"&&!value.UsedDefault&&Same(value.Value.CopyCStringBytes(),B("Item\0")),"real getter resolves the original pool ID");
   Need(depot.LoadCount50==5&&depot.ElapsedTotal5C==10&&depot.ElapsedMaximum60==6&&!trace.NativeArchiveRead&&result.LockReleased,"actual miss tail and lock release retain existing contracts");
   input.Dispose();
   var again=Attempt(depot,source);var hit=again.RunOnce();
   Need(hit.Status=="Known"&&hit.CacheHit&&Object.ReferenceEquals(hit.Table,result.Table)&&!again.Trace.OwnArzReadAttempted,"new attempt hits with a disposed source and performs no raw acquisition");
   Need(Object.ReferenceEquals(attempt.RunOnce(),result)&&depot.LoadCount50==5&&depot.HitCount54==4,"once replay changes neither decoder nor miss counters");
   var ordinary=RecordLoadFile.Apply(depot,Name("rec"),RecordNameLocale.KnownC);
   Need(ordinary.Status=="Known"&&ordinary.CacheHit&&Object.ReferenceEquals(ordinary.Table,result.Table),"ordinary cache-only wrapper sees the actual profile-created table");
  }
  Need(Hash(file)==before,"read-only file hash unchanged");
  scenarios++;
 }
 static void Failure(byte[] packed,uint bodyBytes,string expectedReason,bool exact){
  var fixture=Fixture(packed,bodyBytes);
  using(var input=new ResourceInput(new MemoryStream(fixture.Bytes,false))){
   var depot=Depot();var old=depot.Cache;var attempt=Attempt(depot,Source(input,ArzRawDecodeProfile.ObservedEngineOutputSize));
   var result=attempt.RunOnce();var trace=attempt.Trace;
   Need(result.Status=="Unsupported"&&(exact?result.Reason==expectedReason:result.Reason.StartsWith(expectedReason,StringComparison.Ordinal)),"decode failure category remains explicit");
   Need(trace.OwnArzReadAttempted&&!trace.OwnArzReadCompleted&&trace.Parser==null&&trace.Table==null&&trace.CallerPublication==null,"failed decoder cannot create a parsed or published table");
   Need(Object.ReferenceEquals(depot.Cache,old)&&depot.Cache.Count==0&&!depot.RequiresResolution&&!result.MayHaveEffects&&depot.LoadCount50==4&&depot.HitCount54==3,"own private decode failure does not mutate modeled cache; no claim about native scratch writes");
   input.Dispose();
   Need(Object.ReferenceEquals(attempt.RunOnce(),result)&&result.LockReleased&&!trace.NativeArchiveRead,"failed attempt remains sticky after closing source");
  }
 }
 static void ErrorsAndProfileBudgets(){
  Failure(new byte[]{0x10,65},2,"OwnArzRecordReadUnsupported:ArzRawOutputProfileLengthRejected:-2",true);
  Failure(new byte[]{0,0,0,0x90,65,66,67,68,69,70,71,72,73},13,"OwnArzRecordReadUnsupported:ArzRawOutputProfileUnsupported:",false);
  var body=Body();var packed=ArzRawFixture.LiteralBlock(body);var fixture=Fixture(packed,(uint)body.Length);
  long total=3+24+4+6*8+fixture.StringTableSize+fixture.RecordTableSize+2*body.Length+4+packed.Length;
  using(var input=new ResourceInput(new MemoryStream(fixture.Bytes,false))){
   bool budget=false;try{ArzRawRecordReader.ReadExact(input,B("rec"),Limits(total-1),ArzRawDecodeProfile.ObservedEngineOutputSize);}catch(InvalidDataException e){budget=e.Message=="ArzRawWorkingBudget";}
   Need(budget,"profile read budget includes both result-owned and detached output arrays");
   var raw=ArzRawRecordReader.ReadExact(input,B("rec"),Limits(total),ArzRawDecodeProfile.ObservedEngineOutputSize);
   Need(Same(raw.CopyBodyBytes(),body)&&raw.PackedBytesConsumed==packed.Length,"exact counted profile allocation budget succeeds");
   var legacy=ArzRawRecordReader.ReadExact(input,B("rec"),Limits(total-body.Length));
   Need(legacy.DecodeProfile==ArzRawDecodeProfile.BoundedInput&&legacy.PackedBytesConsumed==packed.Length,"legacy decoder keeps its original single-body budget and consumption contract");
   input.Dispose();
   bool readerRefused=false;try{ArzRawRecordReader.ReadExact(input,B("rec"),Limits(),(ArzRawDecodeProfile)99);}catch(ArgumentOutOfRangeException e){readerRefused=e.ParamName=="profile";}
   Need(readerRefused,"unknown raw profile rejected before touching disposed input");
   bool sourceRefused=false;try{Source(input,(ArzRawDecodeProfile)99);}catch(ArgumentOutOfRangeException e){sourceRefused=e.ParamName=="decodeProfile";}
   Need(sourceRefused,"unknown source profile rejected at construction without I/O");
  }
  scenarios++;
 }
 static int Main(){
  try{ActualFileAndHit();ErrorsAndProfileBudgets();
   Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+assertions+",\"scenarios\":"+scenarios+",\"scope\":\"own ARZ file with explicitly selected checked output-size decoder through actual parser/cache; default decoder unchanged, no native execution or official compatibility claim\"}");return 0;
  }catch(Exception e){Console.Error.WriteLine(e);return 1;}
 }
}
