using System;
using System.IO;
using System.Collections.Generic;
using GrimDynasty.Resources;

// Own complete ARZv3 fixtures only. No installation paths, files or game APIs.
class ArzRawRecordReaderTests {
 static int assertions,scenarios;
 static void Need(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static byte[] B(string value){return ResourceUtil.Utf8.GetBytes(value);}
 static bool Same(byte[] a,byte[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
 static ArzRawLimits Limits(int records=32,int strings=64,int stringTable=65536,int recordTable=65536,
  int stringBytes=32768,int pool=65536,int packed=65536,int body=1048576,long working=4194304){
  return new ArzRawLimits(records,strings,stringTable,recordTable,stringBytes,pool,packed,body,working);
 }
 static ArzRawFixtureData Basic(){
  return ArzRawFixture.Build(new[]{B("rec"),B("Class"),B("Item")},
   new ArzRawFixtureRecord(0,ArzRawFixture.Field(2,1,2)));
 }
 static ArzRawRecord Read(ArzRawFixtureData fixture,byte[] name,ArzRawLimits limits){
  using(var input=new ResourceInput(new MemoryStream(fixture.Bytes,false)))return ArzRawRecordReader.ReadExact(input,name,limits);
 }
 static void Reject(Action action,string reason,string why){
  bool rejected=false;
  try{action();}catch(InvalidDataException error){rejected=reason==null||error.Message==reason;}
  Need(rejected,why);
 }
 sealed class ObservedStream : Stream {
  readonly MemoryStream inner;
  readonly List<long[]> forbidden=new List<long[]>();
  public readonly List<long[]> Reads=new List<long[]>();
  public bool Closed;
  public int Fragment=int.MaxValue;
  public ObservedStream(byte[] bytes){inner=new MemoryStream(bytes,false);}
  public void Forbid(long begin,long length){forbidden.Add(new[]{begin,begin+length});}
  public long ReadBytes(long begin,long length){
   long sum=0,end=begin+length;
   foreach(long[] read in Reads)sum+=Math.Max(0,Math.Min(end,read[1])-Math.Max(begin,read[0]));
   return sum;
  }
  public override int Read(byte[] buffer,int offset,int count){
   long begin=Position,end=begin+count;
   foreach(long[] range in forbidden)if(begin<range[1]&&end>range[0])throw new Exception("forbidden payload/metadata read");
   int read=inner.Read(buffer,offset,Math.Min(count,Fragment));Reads.Add(new[]{begin,begin+read});return read;
  }
  public override bool CanRead{get{return !Closed;}}
  public override bool CanSeek{get{return !Closed;}}
  public override bool CanWrite{get{return false;}}
  public override long Length{get{return inner.Length;}}
  public override long Position{get{return inner.Position;}set{inner.Position=value;}}
  public override long Seek(long offset,SeekOrigin origin){return inner.Seek(offset,origin);}
  public override void Flush(){}
  public override void SetLength(long value){throw new NotSupportedException();}
  public override void Write(byte[] buffer,int offset,int count){throw new NotSupportedException();}
  protected override void Dispose(bool disposing){Closed=true;if(disposing)inner.Dispose();base.Dispose(disposing);}
 }
 static void RawPoolAndBodyScenario(){
  byte[][] pool={B("rec"),B("Class"),B("Item"),B("Class"),new byte[0],new byte[]{0xc3,0xa9,0,0x7f}};
  byte[] body=ArzRawFixture.Join(ArzRawFixture.Field(1,3,0x80000000U,0x7fc01234U),
   ArzRawFixture.Field(2,1,2),ArzRawFixture.Field(0,3,0xffffffffU));
  var fixture=ArzRawFixture.Build(pool,new ArzRawFixtureRecord(0,body));
  var stream=new ObservedStream(fixture.Bytes);var input=new ResourceInput(stream);
  byte[] query=B("rec");var record=ArzRawRecordReader.ReadExact(input,query,Limits());
  Need(record.StringCount==6&&record.RecordNameStringId==0,"pool IDs and duplicate stored strings are retained without dictionary normalization");
  Need(Same(record.CopyBodyBytes(),body),"raw mixed tags, duplicate keys, field order and every raw32 bit survive");
  Need(Same(record.CopyStringBytes(1),record.CopyStringBytes(3))&&Same(record.CopyStringBytes(5),pool[5])&&record.CopyStringBytes(4).Length==0,"UTF8 bytes, embedded NUL and empty pool slots are retained by ID");
  Need(Same(record.CopyRecordNameBytes(),B("rec"))&&Same(record.CopyClassBytes(),B("Item")),"name and class are independent raw byte projections");
  Need(record.DataOffset==24&&record.PackedBytes==fixture.RecordTableOffset-24&&record.BodyBytes==body.Length,"selected descriptor exposes its actual offset and packed/unpacked extents");
  Need(record.TimestampLow==0x89abcdefU&&record.TimestampHigh==0x01234567U,"timestamps preserve both unsigned words");
  query[0]=0;byte[] exported=record.CopyBodyBytes();exported[0]=99;
  byte[] name=record.CopyRecordNameBytes();name[0]=0;byte[] kind=record.CopyClassBytes();kind[0]=0;
  byte[] item=record.CopyStringBytes(2);item[0]=0;fixture.Bytes[fixture.PayloadOffsets[0]]=0;
  fixture.Bytes[fixture.StringTableOffset+8]=0;
  Need(Same(record.CopyBodyBytes(),body)&&Same(record.CopyStringBytes(2),B("Item")),"body and pool exports cannot mutate retained result storage");
  Need(Same(record.CopyRecordNameBytes(),B("rec"))&&Same(record.CopyClassBytes(),B("Item")),"query, input and exported arrays do not alias retained name/class");
  bool bounds=false;try{record.CopyStringBytes(6);}catch(ArgumentOutOfRangeException){bounds=true;}
  Need(bounds,"invalid pool ID is rejected rather than remapped");
  Need(!stream.Closed&&input.Length==fixture.Bytes.Length,"ReadExact borrows and leaves the caller-owned stream open");
  input.Dispose();Need(stream.Closed,"the caller can subsequently dispose the owned ResourceInput");
  scenarios++;
 }
 static void SelectedPayloadScenario(){
  byte[][] pool={B("left"),B("selected"),B("right")};
  byte[] selected=ArzRawFixture.Join(ArzRawFixture.Field(2,0,1),ArzRawFixture.Field(0,2,0xdeadbeefU));
  // The unselected compressed blocks are deliberately invalid, but their extents are valid.
  var fixture=ArzRawFixture.Build(pool,new ArzRawFixtureRecord(0,new byte[]{0xf0},20,B("Item")),
   new ArzRawFixtureRecord(1,selected),new ArzRawFixtureRecord(2,new byte[]{0x10,1,0},5,B("Item")));
  var stream=new ObservedStream(fixture.Bytes);stream.Fragment=3;
  stream.Forbid(fixture.PayloadOffsets[0],1);stream.Forbid(fixture.PayloadOffsets[2],3);
  using(var input=new ResourceInput(stream)){
   var first=ArzRawRecordReader.ReadExact(input,B("selected"),Limits());
   Need(Same(first.CopyBodyBytes(),selected)&&first.RecordNameStringId==1,"only the exact selected record is decoded");
   Need(stream.ReadBytes(fixture.PayloadOffsets[1],first.PackedBytes)==first.PackedBytes,"fragmented reads consume the selected compressed extent exactly once");
   Need(stream.ReadBytes(fixture.PayloadOffsets[0],1)==0&&stream.ReadBytes(fixture.PayloadOffsets[2],3)==0,"neither invalid unselected payload is read");
   int previousReads=stream.Reads.Count;
   var second=ArzRawRecordReader.ReadExact(input,B("selected"),Limits());
   Need(!Object.ReferenceEquals(first,second)&&Same(second.CopyBodyBytes(),selected)&&stream.Reads.Count>previousReads,"each stateless ReadExact independently reads its stable borrowed input");
   Need(stream.ReadBytes(fixture.PayloadOffsets[1],first.PackedBytes)==2L*first.PackedBytes&&!stream.Closed,"second read neither caches a hidden attempt nor takes stream ownership");
  }
  scenarios++;
 }
 static void ExactNames(){
  byte[][] names={B("A/B"),B("a/b"),B("A\\B"),new byte[]{65,47,66,0},new byte[]{65,0,66},new byte[]{0xc3,0xa9}};
  var records=new ArzRawFixtureRecord[names.Length];
  for(int i=0;i<records.Length;i++)records[i]=new ArzRawFixtureRecord(checked((uint)i),new[]{checked((byte)i)});
  var fixture=ArzRawFixture.Build(names,records);
  for(int i=0;i<names.Length;i++){
   var result=Read(fixture,names[i],Limits());
   Need(result.RecordNameStringId==(uint)i&&result.CopyBodyBytes()[0]==i&&Same(result.CopyRecordNameBytes(),names[i]),"record names match full raw byte length and content");
  }
  Reject(delegate{Read(fixture,B("absent"),Limits());},"ArzRawRecordNotFound","missing name does not fall back or normalize");
  var duplicate=ArzRawFixture.Build(new[]{B("same"),B("same"),B("other")},
   new ArzRawFixtureRecord(0,new byte[]{1}),new ArzRawFixtureRecord(1,new byte[]{2}),new ArzRawFixtureRecord(2,new byte[]{3}));
  var stream=new ObservedStream(duplicate.Bytes);stream.Forbid(24,duplicate.RecordTableOffset-24);
  using(var input=new ResourceInput(stream)){
   Reject(delegate{ArzRawRecordReader.ReadExact(input,B("same"),Limits());},"ArzRawDuplicateExactRecordName","all metadata is examined before a duplicate selected name can read either payload");
  }
  Need(Read(duplicate,B("other"),Limits()).CopyBodyBytes()[0]==3,"duplicates of a different exact name do not replace the selected record");
  var empty=ArzRawFixture.Build(new[]{new byte[0]},new ArzRawFixtureRecord(0,new byte[0]));
  Need(Read(empty,new byte[0],Limits()).CopyBodyBytes().Length==0,"archive raw-name selection permits an exact empty stored name without claiming LoadFile fallback equivalence");
 }
 static void ReadBeforeBudget(){
  // These limits must stop before metadata/payload allocation and corresponding reads.
  var fixture=Basic();
  ArzRawLimits[] headerLimits={Limits(records:0),Limits(recordTable:fixture.RecordTableSize-1),Limits(stringTable:fixture.StringTableSize-1)};
  foreach(var limits in headerLimits){
   var stream=new ObservedStream(fixture.Bytes);stream.Forbid(fixture.RecordTableOffset,fixture.RecordTableSize);stream.Forbid(fixture.StringTableOffset,fixture.StringTableSize);
   using(var input=new ResourceInput(stream))Reject(delegate{ArzRawRecordReader.ReadExact(input,B("rec"),limits);},null,"header budget rejects before either table is read");
  }
  var countStream=new ObservedStream(fixture.Bytes);countStream.Forbid(fixture.StringTableOffset+4,fixture.StringTableSize-4);countStream.Forbid(24,fixture.RecordTableOffset-24);
  using(var input=new ResourceInput(countStream)){
   Reject(delegate{ArzRawRecordReader.ReadExact(input,B("rec"),Limits(strings:2));},null,"pool-count limit rejects after count only, before reading the full pool blob");
   Need(countStream.ReadBytes(fixture.StringTableOffset,4)==4,"pool count is read as the bounded four-byte prefix");
  }
  // This fixture charges 3+24+4+24+28+32+12+4+13 = 144 array-payload bytes.
  ArzRawLimits[] payloadLimits={Limits(stringBytes:4),Limits(pool:11),Limits(packed:12),Limits(body:11),Limits(working:143)};
  foreach(var limits in payloadLimits){
   var stream=new ObservedStream(fixture.Bytes);stream.Forbid(24,fixture.RecordTableOffset-24);
   using(var input=new ResourceInput(stream))Reject(delegate{ArzRawRecordReader.ReadExact(input,B("rec"),limits);},null,"string, aggregate pool, packed, body or working budget rejects before selected payload read");
  }
  var exact=Read(fixture,B("rec"),Limits(records:1,strings:3,stringTable:fixture.StringTableSize,
   recordTable:fixture.RecordTableSize,stringBytes:5,pool:12,packed:13,body:12,working:144));
  Need(exact.BodyBytes==12&&exact.StringCount==3,"inclusive declared byte/count budgets admit their exact boundary");
  bool invalid=false;try{Limits(working:0);}catch(ArgumentException){invalid=true;}
  Need(invalid,"zero working budget is rejected as an own API configuration error");
  invalid=false;try{Limits(body:-1);}catch(ArgumentException){invalid=true;}
  Need(invalid,"negative read budget is rejected before opening any input");
 }
 static void MetadataFailures(){
  // Each mutation preserves the fixture provenance; no malformed original files are used.
  Action<ArzRawFixtureData>[] mutations={
   delegate(ArzRawFixtureData f){ArzRawFixture.U16(f.Bytes,0,1);},
   delegate(ArzRawFixtureData f){ArzRawFixture.U16(f.Bytes,2,4);},
   delegate(ArzRawFixtureData f){ArzRawFixture.U32(f.Bytes,4,0);},
   delegate(ArzRawFixtureData f){ArzRawFixture.U32(f.Bytes,16,0);},
   delegate(ArzRawFixtureData f){ArzRawFixture.U32(f.Bytes,16,checked((uint)f.RecordTableOffset));},
   delegate(ArzRawFixtureData f){ArzRawFixture.U32(f.Bytes,4,uint.MaxValue);},
   delegate(ArzRawFixtureData f){ArzRawFixture.U32(f.Bytes,8,uint.MaxValue);},
   delegate(ArzRawFixtureData f){ArzRawFixture.U32(f.Bytes,20,uint.MaxValue);},
   delegate(ArzRawFixtureData f){ArzRawFixture.U32(f.Bytes,12,2);},
   delegate(ArzRawFixtureData f){ArzRawFixture.U32(f.Bytes,8,checked((uint)(f.RecordTableSize-1)));},
   delegate(ArzRawFixtureData f){ArzRawFixture.U32(f.Bytes,20,checked((uint)(f.StringTableSize-1)));},
   delegate(ArzRawFixtureData f){ArzRawFixture.U32(f.Bytes,f.StringTableOffset,uint.MaxValue);},
   delegate(ArzRawFixtureData f){ArzRawFixture.U32(f.Bytes,f.StringTableOffset+4,uint.MaxValue);},
   delegate(ArzRawFixtureData f){ArzRawFixture.U32(f.Bytes,f.EntryOffsets[0],3);},
   delegate(ArzRawFixtureData f){ArzRawFixture.U32(f.Bytes,f.EntryOffsets[0]+4,uint.MaxValue);},
   delegate(ArzRawFixtureData f){f.Bytes[f.EntryOffsets[0]+8]=0x80;},
   delegate(ArzRawFixtureData f){ArzRawFixture.U32(f.Bytes,f.EntryOffsets[0]+12,uint.MaxValue);},
   delegate(ArzRawFixtureData f){ArzRawFixture.U32(f.Bytes,f.EntryOffsets[0]+12,checked((uint)(f.RecordTableOffset-24)));},
   delegate(ArzRawFixtureData f){ArzRawFixture.U32(f.Bytes,f.EntryOffsets[0]+16,14);},
   delegate(ArzRawFixtureData f){ArzRawFixture.U32(f.Bytes,f.EntryOffsets[0]+16,uint.MaxValue);},
   delegate(ArzRawFixtureData f){ArzRawFixture.U32(f.Bytes,f.EntryOffsets[0]+20,uint.MaxValue);},
   delegate(ArzRawFixtureData f){f.Bytes[f.StringTableOffset+8]=0xff;}
  };
  foreach(var mutate in mutations){
   var fixture=Basic();mutate(fixture);
   Reject(delegate{Read(fixture,B("rec"),Limits());},null,"malformed header/table/length/offset/name index or invalid UTF8 is rejected");
  }
  using(var input=new ResourceInput(new MemoryStream(new byte[23],false)))
   Reject(delegate{ArzRawRecordReader.ReadExact(input,B("rec"),Limits());},null,"truncated fixed header is rejected");
  // Even a non-selected record's descriptor must be bounded before selected bytes are read.
  var pair=ArzRawFixture.Build(new[]{B("selected"),B("other")},new ArzRawFixtureRecord(0,new byte[]{1}),new ArzRawFixtureRecord(1,new byte[]{2}));
  ArzRawFixture.U32(pair.Bytes,pair.EntryOffsets[1]+12,uint.MaxValue);
  var guarded=new ObservedStream(pair.Bytes);guarded.Forbid(24,pair.RecordTableOffset-24);
  using(var input=new ResourceInput(guarded))
   Reject(delegate{ArzRawRecordReader.ReadExact(input,B("selected"),Limits());},null,"invalid unselected metadata prevents selected payload reads");
 }
 static void Lz4ThroughArchive(){
  int[] lengths={0,1,14,15,16,270,271};
  foreach(int length in lengths){
   var body=new byte[length];for(int i=0;i<body.Length;i++)body[i]=unchecked((byte)(i*17));
   var fixture=ArzRawFixture.Build(new[]{B("rec")},new ArzRawFixtureRecord(0,body));
   Need(Same(Read(fixture,B("rec"),Limits()).CopyBodyBytes(),body),"literal lengths across nibble and extension boundaries decode exactly");
  }
  byte[][] packed={
   new byte[]{0x14,97,1,0,0x50,66,67,68,69,70},
   new byte[]{0x44,97,98,99,100,4,0,0x50,69,70,71,72,73},
   new byte[]{0x1f,97,1,0,5,0x50,66,67,68,69,70}
  };
  byte[][] expected={B("aaaaaaaaaBCDEF"),B("abcdabcdabcdEFGHI"),B(new string('a',25)+"BCDEF")};
  for(int i=0;i<packed.Length;i++){
   var fixture=ArzRawFixture.Build(new[]{B("rec")},new ArzRawFixtureRecord(0,packed[i],checked((uint)expected[i].Length),B("Item")));
   Need(Same(Read(fixture,B("rec"),Limits()).CopyBodyBytes(),expected[i]),"real match copy, overlap and extended match length decode through the archive reader");
  }
  byte[][] invalid={
   new byte[]{0xf0},new byte[]{0x20,97},new byte[]{0x10,97,1},
   new byte[]{0x10,97,0,0},new byte[]{0x10,97,2,0},new byte[]{0x14,97,1,0},
   new byte[]{0x1f,97,1,0},new byte[]{0x10,97},new byte[]{0x20,97,98}
  };
  uint[] unpacked={20,2,5,5,5,8,20,2,1};
  for(int i=0;i<invalid.Length;i++){
   var fixture=ArzRawFixture.Build(new[]{B("rec")},new ArzRawFixtureRecord(0,invalid[i],unpacked[i],B("Item")));
   Reject(delegate{Read(fixture,B("rec"),Limits());},null,"bad LZ4 length/literal/offset/match/output size is rejected without a fabricated raw record");
  }
 }
 public static int Main(){
  try{
   RawPoolAndBodyScenario();SelectedPayloadScenario();ExactNames();ReadBeforeBudget();MetadataFailures();Lz4ThroughArchive();
   Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+assertions+",\"scenarios\":"+scenarios+"}");return 0;
  }catch(Exception error){Console.Error.WriteLine(error.GetType().Name+": "+error.Message);return 1;}
 }
}
