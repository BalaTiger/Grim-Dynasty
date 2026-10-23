using System;
using System.Collections.Generic;
using System.Text;
using GrimDynasty.TaskItems;

// Independent own wire fixtures for the proposed bounded tag-3 projection.
// Success cases must not run until the native fresh-buffer initialization
// evidence is confirmed. No official payload, native call or allocator/EH
// simulation is used. Unused output bits are zero in this own export only.
class BinaryBooleanRecordParserTests {
 static int assertions,scenarios;
 const ulong Address=0x44556008;
 static void Need(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static TableString S(string text){return new TableString(Encoding.ASCII.GetBytes(text));}
 static TableString[] ClassPool(int nameId){
  var pool=new TableString[nameId+1];for(int i=0;i<pool.Length;i++)pool[i]=S("unused");
  pool[nameId]=S("Class");return pool;
 }
 static bool Same(uint[] actual,params uint[] expected){if(actual.Length!=expected.Length)return false;for(int i=0;i<actual.Length;i++)if(actual[i]!=expected[i])return false;return true;}
 static bool SameSigned(int[] actual,params int[] expected){if(actual.Length!=expected.Length)return false;for(int i=0;i<actual.Length;i++)if(actual[i]!=expected[i])return false;return true;}
 static bool AllWords(uint[] actual,int begin,int end,uint expected){for(int i=begin;i<end;i++)if(actual[i]!=expected)return false;return true;}
 static bool BadTag(Action action){try{action();return false;}catch(ArgumentOutOfRangeException){return true;}}
 static bool Known(BinaryRecordParseResult result,BinaryRecordReader reader,int start,int end,int fields){
  return result.Status=="Known"&&result.Reason==null&&!result.DomainRejected&&result.Record!=null&&
   result.InitialPosition==start&&result.FinalPosition==end&&result.FirstPassEnd==end&&result.ReaderRewound&&
   result.CompletedFields==fields&&result.MayHaveEffects&&result.GateAcquired&&result.GateReleased&&
   reader.Position==end&&reader.Consumed&&!reader.IsDisposed&&!reader.Gate.IsHeldByCurrentThread&&Object.ReferenceEquals(result.Record.Gate,reader.Gate);
 }
 static BinaryRecordParseResult Parse(byte[] bytes,out BinaryRecordReader reader){
  var gate=new RegistrationGate();reader=new BinaryRecordReader(gate,bytes,0,bytes.Length);return BinaryRecordParser.DeserializeFresh(gate,reader);
 }
 static BinaryPointerCacheSnapshot Names(ulong id){return new BinaryPointerCacheSnapshot(0,0,new[]{new[]{new BinaryNameCacheEntry(Address,id)}});}
 static RecordTableGetterResult Get(BinaryParsedRecord record,TableString[] pool,ulong nameId,TableString fallback){
  return RecordTableGetter.GetValue(new LoadTableBinarySnapshot(record.Gate,true,record.ToWarmState(Names(nameId),pool,null)),
   new byte[]{67,108,97,115,115,0},fallback,Address);
 }
 static bool Released(RecordTableGetterResult result,BinaryParsedRecord record){return result.GateAcquired&&result.GateReleased&&!record.Gate.IsHeldByCurrentThread;}
 static void Word(List<byte> bytes,uint value){
  bytes.Add(unchecked((byte)value));bytes.Add(unchecked((byte)(value>>8)));bytes.Add(unchecked((byte)(value>>16)));bytes.Add(unchecked((byte)(value>>24)));
 }
 // Encoder produces inputs only. Expected packed words and boundary bucket
 // metadata below are literal values, not another implementation of packing.
 static byte[] Field(ushort tag,uint key,params uint[] values){
  if(values.Length>65535)throw new ArgumentException("Wire count is uint16");
  var bytes=new List<byte>();Word(bytes,((uint)values.Length<<16)|tag);Word(bytes,key);
  foreach(uint value in values)Word(bytes,value);return bytes.ToArray();
 }
 static byte[] Join(params byte[][] pieces){
  int length=0;foreach(var piece in pieces)length+=piece.Length;
  var bytes=new byte[length];int at=0;foreach(var piece in pieces){Array.Copy(piece,0,bytes,at,piece.Length);at+=piece.Length;}return bytes;
 }
 static uint[] Repeat(int count,uint value){var values=new uint[count];for(int i=0;i<count;i++)values[i]=value;return values;}
 static byte[] MixedLiteral(){
  // Eight fields, tags 3/2/0/1/3/2/0/1. The seven boolean inputs
  // are 0,1,2,80000000,ffffffff,0,2: expected packed word is 0000005e.
  return new byte[]{
   0x03,0x00,0x05,0x00, 0x00,0x00,0x00,0x00,
   0x00,0x00,0x00,0x00, 0x01,0x00,0x00,0x00, 0x02,0x00,0x00,0x00,
   0x00,0x00,0x00,0x80, 0xff,0xff,0xff,0xff,
   0x02,0x00,0x02,0x00, 0x04,0x00,0x00,0x00, 0x01,0x00,0x00,0x00, 0x02,0x00,0x00,0x00,
   0x00,0x00,0x02,0x00, 0x08,0x00,0x00,0x00, 0x00,0x00,0x00,0x80, 0xff,0xff,0xff,0xff,
   0x01,0x00,0x02,0x00, 0x0c,0x00,0x00,0x00, 0x45,0x23,0xc1,0x7f, 0x00,0x00,0x00,0x80,
   0x03,0x00,0x02,0x00, 0x10,0x00,0x00,0x00, 0x00,0x00,0x00,0x00, 0x02,0x00,0x00,0x00,
   0x02,0x00,0x01,0x00, 0x14,0x00,0x00,0x00, 0x03,0x00,0x00,0x00,
   0x00,0x00,0x01,0x00, 0x18,0x00,0x00,0x00, 0x78,0x56,0x34,0x12,
   0x01,0x00,0x01,0x00, 0x1c,0x00,0x00,0x00, 0x00,0x00,0x80,0xff
  };
 }
 static void LiteralInterleavingAndIsolation(){
  byte[] literal=MixedLiteral();Need(literal.Length==128,"independent literal has the intended complete wire extent");
  byte[] window=Join(new byte[]{0xff,0xee,0xdd},literal,new byte[]{4,0,1,0,0xff});
  var gate=new RegistrationGate();var reader=new BinaryRecordReader(gate,window,3,131);
  window[3]=4;window[11]=1;window[87]=1;
  var parsed=BinaryRecordParser.DeserializeFresh(gate,reader);
  Need(Known(parsed,reader,3,131,8),"unaligned complete reader window ignores poison and retains its cloned original bytes");
  var record=parsed.Record;var descriptors=record.CopyDescriptors();
  int[] tags={3,2,0,1,3,2,0,1};uint[] keys={0,4,8,12,16,20,24,28};
  uint[] offsets={0,0,0,0,5,2,2,2},counts={5,2,2,2,2,1,1,1};
  Need(descriptors.Length==8,"all interleaved descriptors are retained");
  for(int i=0;i<8;i++)Need(descriptors[i].TypeTag==tags[i]&&descriptors[i].Key==keys[i]&&descriptors[i].BufferIndex==offsets[i]&&descriptors[i].ElementCount==counts[i],"independent typed offset and count for literal field "+i);
  Need(record.BooleanCount==7&&Same(record.CopyBooleanWords(),0x0000005eU),"zero is false and every other 32-bit input is true, packed least-significant-bit first");
  Need(Same(record.CopyRawValues(0),0x80000000U,0xffffffffU,0x12345678U)&&Same(record.CopyRawValues(1),0x7fc12345U,0x80000000U,0xff800000U),"boolean packing does not convert or overwrite integer and float raw words");
  Need(Same(record.CopyRawValues(2),1,2,3)&&SameSigned(record.CopyValues(),1,2,3),"tag-two raw and signed views retain their independent cumulative offsets");
  Need(record.FieldBucketCount==4&&record.FieldSlotCapacity==8&&record.InitialSlotBytesCleared==96&&record.Fields.Shift==0&&record.Fields.Mask==3,"eight fresh fields use the evidenced fixed bucket and initial-clear metadata");
  var item=S("Item");var other=S("Other");var fallback=S("fallback");var pool=new TableString[29];
  for(int i=0;i<pool.Length;i++)pool[i]=S("unused");
  foreach(int key in new[]{0,4,8,12,16,20,24,28})pool[key]=S("Class");pool[1]=item;pool[3]=other;
  var value=Get(record,pool,4,fallback);
  Need(value.Status=="Known"&&!value.UsedDefault&&Object.ReferenceEquals(value.Value,item)&&value.Binary.FieldEntry.Value.BufferIndex==0&&Released(value,record),"actual getter still resolves the first tag-two field after a boolean field");
  value=Get(record,pool,20,fallback);
  Need(value.Status=="Known"&&!value.UsedDefault&&Object.ReferenceEquals(value.Value,other)&&value.Binary.FieldEntry.Value.BufferIndex==2&&value.Binary.StringIndex==3&&Released(value,record),"actual getter uses string offset two, not the preceding boolean-bit or total-word count");
  foreach(ulong id in new ulong[]{0,16}){
   value=Get(record,pool,id,fallback);
   Need(value.Status=="Known"&&value.UsedDefault&&Object.ReferenceEquals(value.Value,fallback)&&value.Binary.FieldEntry.Value.TypeTag==3&&!value.Binary.StringIndex.HasValue&&!value.MayHaveEffects&&Released(value,record),"tag-three descriptor returns the caller default before accessing string storage: "+id);
  }
  var exported=record.CopyBooleanWords();exported[0]=uint.MaxValue;descriptors[4]=null;
  for(int tag=0;tag<3;tag++){var raw=record.CopyRawValues(tag);raw[0]=0;}
  var signed=record.CopyValues();signed[2]=-1;
  Need(record.BooleanCount==7&&Same(record.CopyBooleanWords(),0x5eU)&&record.CopyDescriptors()[4].BufferIndex==5,"boolean export and descriptor exports cannot change retained bit count, bits or bit offset");
  Need(Same(record.CopyRawValues(0),0x80000000U,0xffffffffU,0x12345678U)&&Same(record.CopyRawValues(1),0x7fc12345U,0x80000000U,0xff800000U)&&SameSigned(record.CopyValues(),1,2,3),"exported arrays cannot alias any other typed buffer");
  Need(BadTag(delegate{record.CopyRawValues(3);})&&BadTag(delegate{record.CopyRawValues(-1);})&&BadTag(delegate{record.CopyRawValues(4);}),"packed booleans are available only through their explicit view, never CopyRawValues(3)");
  reader.Dispose();value=Get(record,pool,20,null);
  Need(reader.IsDisposed&&reader.Consumed&&reader.Position==131&&Same(record.CopyBooleanWords(),0x5eU)&&Object.ReferenceEquals(value.Value,other)&&Released(value,record),"detached typed data and actual string getter survive reader disposal");
  BinaryRecordReader emptyReader;var noBoolean=Parse(Join(Field(0,0,1),Field(2,4,1)),out emptyReader);
  Need(Known(noBoolean,emptyReader,0,24,2)&&noBoolean.Record.BooleanCount==0&&noBoolean.Record.CopyBooleanWords().Length==0,"no tag-three field yields an explicit empty boolean view");
  var allFalse=Parse(Join(Field(3,0,0,0,0),Field(3,4,0)),out emptyReader);
  Need(Known(allFalse,emptyReader,0,32,2)&&allFalse.Record.BooleanCount==4&&Same(allFalse.Record.CopyBooleanWords(),0),"all-false input clears every valid bit and leaves own unused export bits zero");
  scenarios++;
 }
 static void CrossWordBitIndices(){
  var first=Repeat(30,0);first[0]=1;first[29]=2;
  var middle=Repeat(30,0);middle[0]=0xffffffffU;middle[29]=0x80000000U;
  byte[] bytes=Join(Field(3,0,first),Field(3,4,2,0,0x80000000U),Field(3,8,middle),Field(3,12,0xffffffffU,1,0),Field(3,16,2));
  BinaryRecordReader reader;var parsed=Parse(bytes,out reader);var record=parsed.Record;
  Need(Known(parsed,reader,0,308,5)&&record.BooleanCount==67,"five multi-value boolean fields consume 67 complete wire words");
  Need(Same(record.CopyBooleanWords(),0x60000001U,0xc0000003U,0x00000005U),"literal bitmap distinguishes bits 30/31/32 and 62/63/64/65/66 across both boundaries");
  var descriptors=record.CopyDescriptors();uint[] offsets={0,30,33,63,66},counts={30,3,30,3,1};
  for(int i=0;i<5;i++)Need(descriptors[i].TypeTag==3&&descriptors[i].BufferIndex==offsets[i]&&descriptors[i].ElementCount==counts[i],"descriptor index is a cumulative bit index rather than a packed-word index: "+i);
  Need(record.CopyValues().Length==0&&record.CopyRawValues(0).Length==0&&record.CopyRawValues(1).Length==0&&record.CopyRawValues(2).Length==0,"boolean-only input does not fabricate raw numeric or string words");
  var fallback=S("fallback");var value=Get(record,ClassPool(12),12,fallback);
  Need(value.Status=="Known"&&value.UsedDefault&&Object.ReferenceEquals(value.Value,fallback)&&value.Binary.FieldEntry.Value.BufferIndex==63&&value.Binary.FieldEntry.Value.TypeTag==3&&!value.Binary.StringIndex.HasValue&&Released(value,record),"real getter observes bit-index 63 but never treats it as a string-buffer offset");
  scenarios++;
 }
 static void DuplicateAndStringShadow(){
  BinaryRecordReader reader;var fallback=S("fallback");var oldItem=S("OldItem");var newItem=new TableString(new byte[]{0,0x80,0xff});var pool=new[]{S("Class"),oldItem,newItem};
  var duplicate=Parse(Join(Field(3,0,0,1),Field(3,0,2,0,1)),out reader);var record=duplicate.Record;
  Need(Known(duplicate,reader,0,36,2)&&record.BooleanCount==5&&Same(record.CopyBooleanWords(),0x16U)&&record.CopyDescriptors()[1].BufferIndex==2,"duplicate boolean keys retain both bit ranges and append by element count");
  var value=Get(record,pool,0,fallback);
  Need(value.Status=="Known"&&value.UsedDefault&&Object.ReferenceEquals(value.Value,fallback)&&value.Binary.FieldEntry.Value.TypeTag==3&&value.Binary.FieldEntry.Value.BufferIndex==2&&Released(value,record),"newest duplicate boolean descriptor shadows its older bit offset");
  var newerBoolean=Parse(Join(Field(2,0,1),Field(3,4,0,0xffffffffU),Field(3,0,2)),out reader);record=newerBoolean.Record;
  Need(Known(newerBoolean,reader,0,40,3)&&record.BooleanCount==3&&Same(record.CopyBooleanWords(),6)&&Same(record.CopyRawValues(2),1),"a later boolean field retains the earlier string payload but publishes its own descriptor");
  value=Get(record,pool,0,fallback);
  Need(value.Status=="Known"&&value.UsedDefault&&Object.ReferenceEquals(value.Value,fallback)&&value.Binary.FieldEntry.Value.TypeTag==3&&value.Binary.FieldEntry.Value.BufferIndex==2&&!value.Binary.StringIndex.HasValue&&Released(value,record),"latest tag-three Class shadows an older valid tag-two Class value");
  value=Get(record,pool,0,null);
  Need(value.Status=="Known"&&value.UsedDefault&&value.Value==null&&value.Binary.FieldEntry.Value.TypeTag==3&&!value.Binary.StringIndex.HasValue&&Released(value,record),"boolean shadow preserves a null caller default without searching older string descriptors");
  var newerString=Parse(Join(Field(3,0,0x80000000U,0),Field(3,4,2),Field(2,0,2)),out reader);record=newerString.Record;
  Need(Known(newerString,reader,0,40,3)&&record.BooleanCount==3&&Same(record.CopyBooleanWords(),5)&&SameSigned(record.CopyValues(),2),"reverse duplicate order preserves booleans while retaining an independent string offset zero");
  value=Get(record,pool,0,fallback);
  Need(value.Status=="Known"&&!value.UsedDefault&&Object.ReferenceEquals(value.Value,newItem)&&value.Binary.FieldEntry.Value.TypeTag==2&&value.Binary.FieldEntry.Value.BufferIndex==0&&value.Binary.StringIndex==2&&Released(value,record),"newest tag-two Class shadows the boolean and returns exact shared leading-NUL string identity");
  scenarios++;
 }
 sealed class BoundaryCase {
  public readonly int Fields;public readonly uint Buckets,Mask,Cleared,LastBit,LastKey;public readonly uint[] Words;
  public BoundaryCase(int fields,uint buckets,uint mask,uint cleared,uint lastBit,uint lastKey,params uint[] words){Fields=fields;Buckets=buckets;Mask=mask;Cleared=cleared;LastBit=lastBit;LastKey=lastKey;Words=words;}
 }
 static byte[] BooleanFields(int count){var fields=new byte[count][];for(int i=0;i<count;i++)fields[i]=Field(3,(uint)(i*16),2);return Join(fields);}
 static void Rejected(byte[] bytes,int start,int limit,string expectedReason,string why){
  var gate=new RegistrationGate();var reader=new BinaryRecordReader(gate,bytes,start,limit);var result=BinaryRecordParser.DeserializeFresh(gate,reader);
  Need(result.Status=="Unsupported"&&result.DomainRejected&&!String.IsNullOrEmpty(result.Reason)&&(expectedReason==null||result.Reason==expectedReason)&&result.Record==null&&
   result.InitialPosition==start&&result.FinalPosition==start&&result.FirstPassEnd==null&&!result.ReaderRewound&&result.CompletedFields==0&&!result.MayHaveEffects&&
   result.GateAcquired&&result.GateReleased&&!gate.IsHeldByCurrentThread&&reader.Position==start&&!reader.Consumed&&!reader.IsDisposed,why);
 }
 static void BucketCoverageBoundaries(){
  var cases=new[]{
   new BoundaryCase(2,4,3,24,1,16,0x00000003U),
   new BoundaryCase(15,4,3,168,14,224,0x00007fffU),
   new BoundaryCase(18,8,7,216,17,272,0x0003ffffU),
   new BoundaryCase(31,8,7,360,30,480,0x7fffffffU),
   new BoundaryCase(34,16,15,408,33,528,0xffffffffU,0x00000003U),
   new BoundaryCase(38,16,15,456,37,592,0xffffffffU,0x0000003fU),
   new BoundaryCase(63,16,15,744,62,992,0xffffffffU,0x7fffffffU)
  };
  foreach(var item in cases){
   byte[] bytes=BooleanFields(item.Fields);BinaryRecordReader reader;var parsed=Parse(bytes,out reader);var record=parsed.Record;
   Need(Known(parsed,reader,0,bytes.Length,item.Fields)&&record.FieldBucketCount==item.Buckets&&record.FieldSlotCapacity==(uint)item.Fields&&record.InitialSlotBytesCleared==item.Cleared&&record.Fields.Shift==0&&record.Fields.Mask==item.Mask,"literal fresh bucket/capacity/clear boundary: "+item.Fields);
   Need(record.BooleanCount==(uint)item.Fields&&Same(record.CopyBooleanWords(),item.Words)&&record.CopyDescriptors()[item.Fields-1].BufferIndex==item.LastBit,"literal packed boundary and final bit index: "+item.Fields);
   var fallback=S("fallback");var value=Get(record,ClassPool(checked((int)item.LastKey)),item.LastKey,fallback);
   Need(value.Status=="Known"&&value.UsedDefault&&Object.ReferenceEquals(value.Value,fallback)&&value.Binary.FieldEntry.Key==item.LastKey&&value.Binary.FieldEntry.Value.BufferIndex==item.LastBit&&Released(value,record),"last colliding boolean field remains reachable without fresh-storage growth: "+item.Fields);
  }
  foreach(int count in new[]{16,17,32,33}){
   byte[] bytes=BooleanFields(count);Rejected(bytes,0,bytes.Length,"BinaryRecordBucketRoundingOutsideCoveredDomain","unresolved bucket rounding rejects before any effects: "+count);
  }
  foreach(int count in new[]{64,65}){
   byte[] bytes=BooleanFields(count);Rejected(bytes,0,bytes.Length,"BinaryRecordFieldCountOutsideCoveredDomain","field-count coverage guard rejects before any effects: "+count);
  }
  scenarios++;
 }
 static void ReaderWindowsAndWordBudgets(){
  byte[] literal=MixedLiteral();var firstTwo=new byte[44];Array.Copy(literal,firstTwo,44);
  byte[] fullWindow=Join(new byte[]{0xff,0xee,0xdd},firstTwo,new byte[]{4,0,0,0,0xff});
  for(int length=0;length<44;length++)Rejected(fullWindow,3,3+length,null,"complete backing bytes cannot rescue a shortened two-field window: "+length);
  var gate=new RegistrationGate();var exactReader=new BinaryRecordReader(gate,fullWindow,3,47);var exact=BinaryRecordParser.DeserializeFresh(gate,exactReader);
  Need(Known(exact,exactReader,3,47,2)&&exact.Record.BooleanCount==5&&Same(exact.Record.CopyBooleanWords(),0x1eU)&&SameSigned(exact.Record.CopyValues(),1,2),"the exact full window succeeds with the literal noncanonical true values");
  byte[] maximumBoolean=Join(Field(3,0,Repeat(65535,2)),Field(3,4,0x80000000U));BinaryRecordReader reader;var parsed=Parse(maximumBoolean,out reader);var words=parsed.Record.CopyBooleanWords();
  Need(Known(parsed,reader,0,maximumBoolean.Length,2)&&parsed.Record.BooleanCount==65536&&words.Length==2048&&AllWords(words,0,2048,0xffffffffU)&&parsed.Record.CopyDescriptors()[1].BufferIndex==65535,"65536 boolean source words fit exactly and preserve the final cumulative bit index");
  byte[] maximumMixed=Join(Field(3,4,Repeat(65533,2)),Field(0,8,0xffffffffU),Field(1,12,0x7fc12345U),Field(2,0,1));
  parsed=Parse(maximumMixed,out reader);var record=parsed.Record;words=record.CopyBooleanWords();
  Need(Known(parsed,reader,0,maximumMixed.Length,4)&&record.BooleanCount==65533&&words.Length==2048&&AllWords(words,0,2047,0xffffffffU)&&words[2047]==0x1fffffffU,"65536-word shared budget counts every boolean input word before packing and exports zero invalid tail bits");
  Need(Same(record.CopyRawValues(0),0xffffffffU)&&Same(record.CopyRawValues(1),0x7fc12345U)&&SameSigned(record.CopyValues(),1)&&record.CopyDescriptors()[3].BufferIndex==0,"large boolean input cannot displace the three other typed offsets");
  var item=S("Item");var value=Get(record,new[]{S("Class"),item},0,null);
  Need(value.Status=="Known"&&!value.UsedDefault&&Object.ReferenceEquals(value.Value,item)&&value.Binary.StringIndex==1&&Released(value,record),"actual string getter remains usable at the combined source-word budget");
  byte[] tooManyBoolean=Join(Field(3,0,Repeat(65535,2)),Field(3,4,0,1));
  Rejected(tooManyBoolean,0,tooManyBoolean.Length,"BinaryRecordValueBudgetExceeded","65537 boolean source words are rejected despite a much smaller packed output");
  byte[] tooManyMixed=Join(Field(3,4,Repeat(65533,2)),Field(0,8,1),Field(1,12,1),Field(2,0,1,2));
  Rejected(tooManyMixed,0,tooManyMixed.Length,"BinaryRecordValueBudgetExceeded","65537 words across four tags reject with no partial first pass or publication");
  byte[] zero=Join(Field(3,0,new uint[0]),Field(2,4,1));
  Rejected(zero,0,zero.Length,"BinaryRecordZeroCountOutsideCoveredDomain","zero-count booleans remain outside the positive-count own domain");
  foreach(ushort tag in new ushort[]{4,5,65535}){
   byte[] unsupported=Join(Field(3,0,1),Field(tag,4,1));Rejected(unsupported,0,unsupported.Length,"BinaryRecordTagOutsideCoveredDomain","late unsupported tag cannot publish an earlier boolean field: "+tag);
  }
  byte[] single=Field(3,0,1);Rejected(single,0,single.Length,"BinaryRecordFieldCountOutsideCoveredDomain","one-field record cannot bypass fresh-table coverage");
  scenarios++;
 }
 static int Main(){
  try{
   LiteralInterleavingAndIsolation();CrossWordBitIndices();DuplicateAndStringShadow();BucketCoverageBoundaries();ReaderWindowsAndWordBudgets();
   Console.WriteLine("{\"suite\":\"BinaryBooleanRecordParserTests\",\"status\":\"passed\",\"fixed_assertions\":"+assertions+",\"scenarios\":"+scenarios+",\"scope\":\"Own literal and encoded four-tag records through bounded fresh parser and actual string getter; packed tag-3 bit indices, duplicate priority, bucket boundaries and source-word budgets. Normal storage and confirmed initialization contracts only; unused export bits are own projection; no official payload or original execution.\"}");return 0;
  }catch(Exception error){Console.Error.WriteLine(error);return 1;}
 }
}
