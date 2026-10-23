using System;
using System.Collections.Generic;
using System.Text;
using GrimDynasty.TaskItems;

class BinaryMixedRecordParserTests {
 static int assertions;
 const ulong Address=0x33445008;
 static void Need(bool value,string message){assertions++;if(!value)throw new Exception(message);}
 static TableString S(string text){return new TableString(Encoding.ASCII.GetBytes(text));}
 static byte[] ClassKey(){return new byte[]{67,108,97,115,115,0};}
 static bool Same(uint[] actual,params uint[] expected){if(actual.Length!=expected.Length)return false;for(int i=0;i<actual.Length;i++)if(actual[i]!=expected[i])return false;return true;}
 static bool SameSigned(int[] actual,params int[] expected){if(actual.Length!=expected.Length)return false;for(int i=0;i<actual.Length;i++)if(actual[i]!=expected[i])return false;return true;}
 static bool BadTag(Action action){try{action();return false;}catch(ArgumentOutOfRangeException){return true;}}
 static BinaryPointerCacheSnapshot Names(ulong id){return new BinaryPointerCacheSnapshot(0,0,new[]{new[]{new BinaryNameCacheEntry(Address,id)}});}
 static RecordTableGetterResult Get(BinaryParsedRecord record,TableString[] pool,ulong nameId,TableString fallback){
  return RecordTableGetter.GetValue(new LoadTableBinarySnapshot(record.Gate,true,record.ToWarmState(Names(nameId),pool,null)),ClassKey(),fallback,Address);
 }
 static bool Released(RecordTableGetterResult result,BinaryParsedRecord record){return result.GateAcquired&&result.GateReleased&&!record.Gate.IsHeldByCurrentThread;}
 static bool Known(BinaryRecordParseResult result,BinaryRecordReader reader,int start,int end,int count){
  return result.Status=="Known"&&result.Reason==null&&!result.DomainRejected&&result.Record!=null&&
   result.InitialPosition==start&&result.FinalPosition==end&&result.FirstPassEnd==end&&result.ReaderRewound&&
   result.CompletedFields==count&&result.MayHaveEffects&&result.GateAcquired&&result.GateReleased&&
   reader.Position==end&&reader.Consumed&&!reader.IsDisposed&&!reader.Gate.IsHeldByCurrentThread&&Object.ReferenceEquals(result.Record.Gate,reader.Gate);
 }
 static BinaryRecordParseResult Parse(byte[] bytes,out BinaryRecordReader reader){
  var gate=new RegistrationGate();reader=new BinaryRecordReader(gate,bytes,0,bytes.Length);return BinaryRecordParser.DeserializeFresh(gate,reader);
 }
 static void Word(List<byte> output,uint value){
  output.Add(unchecked((byte)value));output.Add(unchecked((byte)(value>>8)));output.Add(unchecked((byte)(value>>16)));output.Add(unchecked((byte)(value>>24)));
 }
 static byte[] Field(ushort type,uint key,uint[] values){
  if(values.Length>65535)throw new ArgumentException("Wire count is uint16");
  var output=new List<byte>();Word(output,((uint)values.Length<<16)|type);Word(output,key);foreach(uint value in values)Word(output,value);return output.ToArray();
 }
 static byte[] Join(params byte[][] pieces){
  int size=0;foreach(var piece in pieces)size+=piece.Length;var result=new byte[size];int offset=0;
  foreach(var piece in pieces){Array.Copy(piece,0,result,offset,piece.Length);offset+=piece.Length;}return result;
 }
 static uint[] Repeat(int count,uint value){var result=new uint[count];for(int i=0;i<count;i++)result[i]=value;return result;}
 static byte[] MixedLiteral(){
  // Six independent wire fields: 2,0,1,2,0,1. Each type's offsets are 0,2.
  // Bits resembling NaN, negative zero and infinity remain opaque words.
  return new byte[]{
   0x02,0x00,0x02,0x00, 0x00,0x00,0x00,0x00, 0x01,0x00,0x00,0x00, 0x02,0x00,0x00,0x00,
   0x00,0x00,0x02,0x00, 0x04,0x00,0x00,0x00, 0x00,0x00,0x00,0x80, 0xff,0xff,0xff,0xff,
   0x01,0x00,0x02,0x00, 0x05,0x00,0x00,0x00, 0x45,0x23,0xc1,0x7f, 0x00,0x00,0x00,0x80,
   0x02,0x00,0x01,0x00, 0x08,0x00,0x00,0x00, 0x03,0x00,0x00,0x00,
   0x00,0x00,0x01,0x00, 0x0c,0x00,0x00,0x00, 0x78,0x56,0x34,0x12,
   0x01,0x00,0x01,0x00, 0x0d,0x00,0x00,0x00, 0x00,0x00,0x80,0xff
  };
 }
 static void InterleavedClassScenario(){
  var source=MixedLiteral();var gate=new RegistrationGate();var reader=new BinaryRecordReader(gate,source,0,source.Length);
  // Mutation after reader creation must not change any type's stored input.
  source[0]=3;source[24]=99;source[40]=99;source[56]=99;
  var parsed=BinaryRecordParser.DeserializeFresh(gate,reader);
  Need(Known(parsed,reader,0,84,6),"scenario one: literal mixed bytes complete both passes despite later caller input mutations");
  var record=parsed.Record;var descriptors=record.CopyDescriptors();
  int[] tags={2,0,1,2,0,1};uint[] keys={0,4,5,8,12,13};bool correct=descriptors.Length==6;
  for(int i=0;i<descriptors.Length;i++)correct&=descriptors[i].TypeTag==tags[i]&&descriptors[i].Key==keys[i]&&descriptors[i].BufferIndex==(i<3?0U:2U)&&descriptors[i].ElementCount==(i<3?2U:1U);
  Need(correct,"each mixed descriptor preserves its actual tag, raw key, count and independent per-type cumulative offset");
  Need(Same(record.CopyRawValues(0),0x80000000U,0xffffffffU,0x12345678U),"tag zero preserves all 32 bits without signed overflow or value conversion");
  Need(Same(record.CopyRawValues(1),0x7fc12345U,0x80000000U,0xff800000U),"tag one preserves noncanonical NaN-like bits, negative-zero bits and infinity-like bits exactly");
  Need(Same(record.CopyRawValues(2),1,2,3)&&SameSigned(record.CopyValues(),1,2,3),"CopyValues remains exactly the tag-two signed view rather than a merged typed buffer");
  Need(record.FieldBucketCount==4&&record.FieldSlotCapacity==6&&record.InitialSlotBytesCleared==72&&record.Fields.Shift==0&&record.Fields.Mask==3,
   "all six recognized fields share the fixed index while the partial native initial-clear metadata remains explicit");
  var className=S("Class");var item=S("Item");var other=S("Other");var fallback=S("fallback");
  var pool=new[]{className,item,S("unused"),other,className,className,S("unused"),S("unused"),className};
  var result=Get(record,pool,0,fallback);
  Need(result.Status=="Known"&&!result.UsedDefault&&Object.ReferenceEquals(result.Value,item)&&result.Binary.FieldEntry.Value.TypeTag==2&&result.Binary.FieldEntry.Value.BufferIndex==0&&Released(result,record),
   "a real Class getter traverses mixed collision nodes to return the first tag-two shared string identity");
  result=Get(record,pool,8,fallback);
  Need(result.Status=="Known"&&!result.UsedDefault&&Object.ReferenceEquals(result.Value,other)&&result.Binary.FieldEntry.Value.BufferIndex==2&&result.Binary.StringIndex==3&&Released(result,record),
   "tag-two offset after interleaved tag-zero and tag-one records is still two, not the total words seen");
  foreach(ulong id in new ulong[]{4,5}){
   result=Get(record,pool,id,fallback);
   Need(result.Status=="Known"&&result.UsedDefault&&Object.ReferenceEquals(result.Value,fallback)&&result.Binary.FieldEntry.Key==id&&
    result.Binary.FieldEntry.Value.TypeTag==(id==4?0:1)&&!result.Binary.StringIndex.HasValue&&!result.MayHaveEffects&&Released(result,record),
    "a real nonstring descriptor returns the same caller default before reading any string index: "+id);
  }
  for(int tag=0;tag<3;tag++){var copy=record.CopyRawValues(tag);copy[0]=0;}
  Need(Same(record.CopyRawValues(0),0x80000000U,0xffffffffU,0x12345678U)&&Same(record.CopyRawValues(1),0x7fc12345U,0x80000000U,0xff800000U)&&Same(record.CopyRawValues(2),1,2,3),
   "each exported raw typed array is an independent copy and cannot mutate its own or another stored buffer");
  var signedCopy=record.CopyValues();signedCopy[2]=-1;result=Get(record,pool,8,null);
  Need(SameSigned(record.CopyValues(),1,2,3)&&Object.ReferenceEquals(result.Value,other)&&Released(result,record),
   "mutating the legacy tag-two export cannot change a subsequent real Class getter result");
  Need(BadTag(delegate{record.CopyRawValues(-1);})&&BadTag(delegate{record.CopyRawValues(3);})&&BadTag(delegate{record.CopyRawValues(Int32.MaxValue);}),
   "the raw view rejects unsupported type selectors instead of aliasing a known buffer");
 }
 static void CrossTypeDuplicateScenario(){
  // Latest key zero has a nonstring descriptor and must shadow the older
  // tag-two entry even when that older entry could return a valid string.
  var newerNonString=new byte[]{
   0x02,0x00,0x01,0x00, 0x00,0x00,0x00,0x00, 0x01,0x00,0x00,0x00,
   0x01,0x00,0x01,0x00, 0x04,0x00,0x00,0x00, 0x45,0x23,0xc1,0x7f,
   0x00,0x00,0x01,0x00, 0x00,0x00,0x00,0x00, 0xff,0xff,0xff,0xff
  };
  var fallback=S("same default");var item=S("OldItem");var pool=new[]{S("Class"),item};
  foreach(byte newestTag in new byte[]{0,1}){
   var bytes=(byte[])newerNonString.Clone();bytes[24]=newestTag;BinaryRecordReader reader;var parsed=Parse(bytes,out reader);
   Need(Known(parsed,reader,0,36,3)&&parsed.Record.CopyDescriptors()[2].TypeTag==newestTag&&parsed.Record.CopyDescriptors()[2].BufferIndex==(newestTag==0?0U:1U),
    "scenario two: a later nonstring duplicate keeps its own typed offset: "+newestTag);
   var result=Get(parsed.Record,pool,0,fallback);
   Need(result.Status=="Known"&&result.UsedDefault&&Object.ReferenceEquals(result.Value,fallback)&&result.Binary.FieldEntry.Value.TypeTag==newestTag&&
    !result.Binary.StringIndex.HasValue&&Released(result,parsed.Record),"newest nonstring duplicate shadows an older usable tag-two Class value: "+newestTag);
   result=Get(parsed.Record,pool,0,null);
   Need(result.Status=="Known"&&result.UsedDefault&&result.Value==null&&result.Binary.FieldEntry.Value.TypeTag==newestTag&&Released(result,parsed.Record),
    "shadowed Class lookup preserves a known null default without falling back to the older string field: "+newestTag);
  }
  var newerString=new byte[]{
   0x00,0x00,0x01,0x00, 0x00,0x00,0x00,0x00, 0xff,0xff,0xff,0xff,
   0x01,0x00,0x01,0x00, 0x04,0x00,0x00,0x00, 0x00,0x00,0x00,0x80,
   0x02,0x00,0x01,0x00, 0x00,0x00,0x00,0x00, 0x02,0x00,0x00,0x00
  };
  BinaryRecordReader finalReader;var final=Parse(newerString,out finalReader);
  Need(Known(final,finalReader,0,36,3)&&Same(final.Record.CopyRawValues(0),0xffffffffU)&&Same(final.Record.CopyRawValues(1),0x80000000U)&&SameSigned(final.Record.CopyValues(),2),
   "reverse duplicate order preserves all typed payloads while publishing the latest tag-two descriptor");
  var newest=new TableString(new byte[]{0,0xff,0x80});var selected=Get(final.Record,new[]{S("Class"),item,newest},0,fallback);
  Need(selected.Status=="Known"&&!selected.UsedDefault&&Object.ReferenceEquals(selected.Value,newest)&&selected.Binary.FieldEntry.Value.TypeTag==2&&
   selected.Binary.FieldEntry.Value.BufferIndex==0&&selected.Binary.StringIndex==2&&Released(selected,final.Record),
   "latest tag-two duplicate restores Class selection and returns the exact shared leading-NUL value");
 }
 static void EmptyStringBufferAndBitViews(){
  var bytes=new byte[]{
   0x00,0x00,0x01,0x00, 0x00,0x00,0x00,0x00, 0x00,0x00,0x00,0x80,
   0x01,0x00,0x01,0x00, 0x04,0x00,0x00,0x00, 0x45,0x23,0xc1,0x7f
  };
  BinaryRecordReader reader;var parsed=Parse(bytes,out reader);var record=parsed.Record;
  Need(Known(parsed,reader,0,24,2)&&record.CopyValues().Length==0&&record.CopyRawValues(2).Length==0,
   "a valid all-nonstring record produces an explicitly empty tag-two buffer");
  Need(Same(record.CopyRawValues(0),0x80000000U)&&Same(record.CopyRawValues(1),0x7fc12345U),
   "empty tag-two storage does not drop the other two typed buffers");
  var fallback=S("fallback");var className=S("Class");var pool=new[]{className,S("unused"),S("unused"),S("unused"),className};
  foreach(ulong id in new ulong[]{0,4}){
   var result=Get(record,pool,id,fallback);
   Need(result.Status=="Known"&&result.UsedDefault&&Object.ReferenceEquals(result.Value,fallback)&&!result.Binary.StringIndex.HasValue&&Released(result,record),
    "real getter returns default from a parsed nonstring field without requiring a nonexistent tag-two element: "+id);
  }
  parsed=Parse(Join(Field(2,0,new uint[]{0x80000000U,0xffffffffU}),Field(1,4,new uint[]{0x7fa01234U})),out reader);
  Need(Known(parsed,reader,0,28,2)&&Same(parsed.Record.CopyRawValues(2),0x80000000U,0xffffffffU)&&SameSigned(parsed.Record.CopyValues(),Int32.MinValue,-1)&&
   Same(parsed.Record.CopyRawValues(1),0x7fa01234U),"tag-two signed compatibility is a bit-preserving view and does not canonicalize another type's payload");
 }
 static void NeedRejected(byte[] bytes,int limit,string message){
  var gate=new RegistrationGate();var reader=new BinaryRecordReader(gate,bytes,0,limit);var result=BinaryRecordParser.DeserializeFresh(gate,reader);
  Need(result.Status=="Unsupported"&&result.DomainRejected&&!String.IsNullOrEmpty(result.Reason)&&result.Record==null&&
   result.InitialPosition==0&&result.FinalPosition==0&&result.FirstPassEnd==null&&!result.ReaderRewound&&result.CompletedFields==0&&!result.MayHaveEffects&&
   result.GateAcquired&&result.GateReleased&&!gate.IsHeldByCurrentThread&&reader.Position==0&&!reader.Consumed&&!reader.IsDisposed,message);
 }
 static void CombinedBudgetAndRejections(){
  var maximum=Join(Field(0,4,Repeat(32768,0x80000000U)),Field(1,5,Repeat(32767,0x7fc12345U)),Field(2,0,new uint[]{1}));
  BinaryRecordReader reader;var parsed=Parse(maximum,out reader);var record=parsed.Record;
  Need(Known(parsed,reader,0,maximum.Length,3)&&record.CopyRawValues(0).Length==32768&&record.CopyRawValues(1).Length==32767&&record.CopyValues().Length==1&&
   record.CopyDescriptors()[2].BufferIndex==0,"the 65536-word budget is shared across types while every typed index starts independently");
  var item=S("Item");var result=Get(record,new[]{S("Class"),item},0,null);
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Value,item)&&result.Binary.FieldEntry.Value.BufferIndex==0&&Released(result,record),
   "a Class value remains readable after 65535 preceding words in the other two buffers");
  var tooMany=Join(Field(0,4,Repeat(32768,0)),Field(1,5,Repeat(32768,0)),Field(2,0,new uint[]{1}));
  NeedRejected(tooMany,tooMany.Length,"65537 words across types are rejected before cursor publication even though each individual buffer is within 65536");
  var literal=MixedLiteral();
  foreach(int limit in new[]{7,15,23,31,39,47,55,79,83})NeedRejected(literal,limit,"each selected mixed header/payload truncation respects byteLimit rather than readable backing bytes: "+limit);
  foreach(ushort tag in new ushort[]{5,4,65535}){
   var bytes=(byte[])literal.Clone();bytes[72]=unchecked((byte)tag);bytes[73]=unchecked((byte)(tag>>8));
   NeedRejected(bytes,bytes.Length,"a late unsupported type rejects the whole selected domain before any earlier mixed field is published: "+tag);
  }
  foreach(ushort tag in new ushort[]{0,1}){
   var bytes=Join(Field(2,0,new uint[]{1}),Field(tag,4,new uint[0]));
   NeedRejected(bytes,bytes.Length,"zero count remains outside the positive-count domain for newly supported type "+tag);
  }
 }
 static int Main(){
  try{
   InterleavedClassScenario();CrossTypeDuplicateScenario();EmptyStringBufferAndBitViews();CombinedBudgetAndRejections();
   Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+assertions+",\"scenarios\":2,\"scope\":\"Actual bounded fresh mixed tag-0/1/2 bytes through parser and real Binary Class getter, preserving raw typed words and cross-type duplicate priority; explicit archive snapshots and normal allocation contracts. No tag-3, native archive loading, allocator/EH ABI, concrete Read or original execution.\"}");
   return 0;
  }catch(Exception error){Console.Error.WriteLine(error);return 1;}
 }
}
