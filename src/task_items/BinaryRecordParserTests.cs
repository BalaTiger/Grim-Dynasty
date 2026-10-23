using System;
using System.Collections.Generic;
using System.Text;
using GrimDynasty.TaskItems;

class BinaryRecordParserTests {
 static int assertions;
 const ulong QueryAddress=0x12345008;
 static void Need(bool value,string message){assertions++;if(!value)throw new Exception(message);}
 static byte[] B(string text){return Encoding.ASCII.GetBytes(text);}
 static byte[] C(string text){return B(text+"\0");}
 static TableString S(string text){return new TableString(B(text));}
 static bool Same(int[] a,int[] b){if(a==null||b==null||a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
 static bool ThrowsArgument(Action action){try{action();return false;}catch(ArgumentException){return true;}}
 static BinaryPointerCacheSnapshot Names(ulong id){return new BinaryPointerCacheSnapshot(0,0,new[]{new[]{new BinaryNameCacheEntry(QueryAddress,id)}});}
 static BinaryNameIdSnapshot NameIds(string name,int id){return new BinaryNameIdSnapshot(1,new[]{new BinaryNameIdEntry(B(name),id)});}
 static RecordTableGetterResult Get(BinaryParsedRecord record,TableString[] pool,ulong nameId,TableString fallback){
  return RecordTableGetter.GetValue(new LoadTableBinarySnapshot(record.Gate,true,record.ToWarmState(Names(nameId),pool,null)),C("Class"),fallback,QueryAddress);
 }
 static bool Released(RecordTableGetterResult result,BinaryParsedRecord record){return result.GateAcquired&&result.GateReleased&&!record.Gate.IsHeldByCurrentThread;}
 static bool ParseKnown(BinaryRecordParseResult result,BinaryRecordReader reader,RegistrationGate gate,int start,int end,int fields){
  return result.Status=="Known"&&result.Reason==null&&result.Record!=null&&!result.DomainRejected&&result.GateAcquired&&result.GateReleased&&
   !gate.IsHeldByCurrentThread&&result.InitialPosition==start&&result.FinalPosition==end&&result.FirstPassEnd==end&&
   result.ReaderRewound&&result.CompletedFields==fields&&result.MayHaveEffects&&reader.Position==end&&reader.Consumed&&!reader.IsDisposed&&Object.ReferenceEquals(result.Record.Gate,gate);
 }
 static BinaryRecordParseResult Parse(byte[] bytes,out BinaryRecordReader reader,out RegistrationGate gate){
  gate=new RegistrationGate();reader=new BinaryRecordReader(gate,bytes,0,bytes.Length);return BinaryRecordParser.DeserializeFresh(gate,reader);
 }
 static void Word(List<byte> bytes,uint value){
  bytes.Add(unchecked((byte)value));bytes.Add(unchecked((byte)(value>>8)));bytes.Add(unchecked((byte)(value>>16)));bytes.Add(unchecked((byte)(value>>24)));
 }
 // Boundary inputs use an explicit little-endian wire encoder. The two
 // integration scenarios below instead start from independent literal bytes.
 static byte[] Field(uint key,int[] values,ushort type){
  if(values.Length>65535)throw new ArgumentException("Wire count is uint16");
  var bytes=new List<byte>();Word(bytes,((uint)values.Length<<16)|type);Word(bytes,key);
  foreach(int value in values)Word(bytes,unchecked((uint)value));return bytes.ToArray();
 }
 static byte[] Join(params byte[][] pieces){
  int count=0;foreach(byte[] piece in pieces)count+=piece.Length;
  var bytes=new byte[count];int at=0;foreach(byte[] piece in pieces){Array.Copy(piece,0,bytes,at,piece.Length);at+=piece.Length;}return bytes;
 }
 static int[] Repeat(int count,int value){var values=new int[count];for(int i=0;i<count;i++)values[i]=value;return values;}
 static byte[] Fields(int count){
  var pieces=new byte[count][];for(int i=0;i<count;i++)pieces[i]=Field((uint)i,new[]{i},2);return Join(pieces);
 }
 static byte[] Literal(){
  return new byte[]{
   0x02,0x00,0x01,0x00, 0x00,0x00,0x00,0x00, 0x01,0x00,0x00,0x00,
   0x02,0x00,0x01,0x00, 0x04,0x00,0x00,0x00, 0x02,0x00,0x00,0x00
  };
 }
 static void NeedRejected(byte[] bytes,int start,int limit,string message){
  var gate=new RegistrationGate();var reader=new BinaryRecordReader(gate,bytes,start,limit);
  var result=BinaryRecordParser.DeserializeFresh(gate,reader);
  Need(result.Status=="Unsupported"&&!String.IsNullOrEmpty(result.Reason)&&result.DomainRejected&&result.Record==null&&
   result.InitialPosition==start&&result.FinalPosition==start&&result.FirstPassEnd==null&&result.CompletedFields==0&&!result.ReaderRewound&&!result.MayHaveEffects&&
   result.GateAcquired&&result.GateReleased&&!gate.IsHeldByCurrentThread&&reader.Position==start&&!reader.Consumed&&!reader.IsDisposed,message);
 }
 static void LiteralClassScenario(){
  BinaryRecordReader reader;RegistrationGate gate;var parsed=Parse(Literal(),out reader,out gate);
  Need(ParseKnown(parsed,reader,gate,0,24,2),"literal two-field bytes complete both passes and release the owning gate");
  var record=parsed.Record;var descriptors=record.CopyDescriptors();
  Need(record.FieldBucketCount==4&&record.FieldSlotCapacity==2&&record.InitialSlotBytesCleared==24&&record.Fields.Shift==0&&record.Fields.Mask==3,
   "fresh two-field storage uses four buckets, two slots and only the recorded initial one-slot clear");
  Need(descriptors.Length==2&&descriptors[0].Key==0&&descriptors[0].TypeTag==2&&descriptors[0].BufferIndex==0&&descriptors[0].ElementCount==1&&
   descriptors[1].Key==4&&descriptors[1].TypeTag==2&&descriptors[1].BufferIndex==1&&descriptors[1].ElementCount==1,
   "descriptors derive their key, tag, element count and cumulative offset from actual record bytes");
  Need(Same(record.CopyValues(),new[]{1,2}),"literal payload words populate the actual numeric value buffer");
  var className=S("Class");var item=S("Item");var other=S("Other");var fallback=S("fallback");
  var pool=new[]{className,item,other,S("unused"),className};var result=Get(record,pool,0,fallback);
  Need(result.Status=="Known"&&!result.UsedDefault&&Object.ReferenceEquals(result.Value,item)&&result.Binary.FieldEntry.Key==0&&result.Binary.FieldEntry.Value.BufferIndex==0&&Released(result,record),
   "scenario one: parsed field zero reaches the real Class getter and returns the shared Item string identity");
  result=Get(record,pool,4,fallback);
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Value,other)&&result.Binary.FieldEntry.Key==4&&result.Binary.FieldEntry.Value.BufferIndex==1&&Released(result,record),
   "the second colliding parsed key remains reachable through the actual getter");
  Need(!result.MayHaveEffects&&reader.Position==24&&reader.Consumed&&!record.Gate.IsHeldByCurrentThread,
   "warm getter access neither rewrites the parsed reader nor introduces name-cache effects");
 }
 static void WindowDuplicateScenario(){
  // Poison bytes before/after the [3,31) reader window are not records.
  // Older key zero has two values; the newer duplicate must select offset 2.
  var bytes=new byte[]{
   0xff,0xff,0xff,
   0x02,0x00,0x02,0x00, 0x00,0x00,0x00,0x00,
   0x01,0x00,0x00,0x00, 0xff,0xff,0xff,0x7f,
   0x02,0x00,0x01,0x00, 0x00,0x00,0x00,0x00, 0x02,0x00,0x00,0x00,
   0x04,0x00,0x00,0x00,0xff
  };
  var gate=new RegistrationGate();var reader=new BinaryRecordReader(gate,bytes,3,31);
  bytes[3]=4;bytes[11]=127;bytes[27]=127;
  var parsed=BinaryRecordParser.DeserializeFresh(gate,reader);
  Need(ParseKnown(parsed,reader,gate,3,31,2),"a nonzero unaligned start and exclusive end limit parse only the cloned selected bytes");
  var record=parsed.Record;var descriptors=record.CopyDescriptors();
  Need(descriptors.Length==2&&descriptors[0].BufferIndex==0&&descriptors[0].ElementCount==2&&descriptors[1].BufferIndex==2&&descriptors[1].ElementCount==1,
   "multi-count payload advances the next descriptor by elements, not by one field or by bytes");
  Need(Same(record.CopyValues(),new[]{1,Int32.MaxValue,2}),"reader ownership clones input and preserves all payload bits, including an unused second element");
  var newest=new TableString(new byte[]{0,0x80,0xff});var pool=new[]{S("Class"),S("Old"),newest};
  var result=Get(record,pool,0,S("fallback"));
  Need(result.Status=="Known"&&!result.UsedDefault&&Object.ReferenceEquals(result.Value,newest)&&result.Binary.FieldEntry.Value.BufferIndex==2&&Released(result,record),
   "scenario two: duplicate parsed keys prepend so the real Class getter selects the newest full shared string, including its leading NUL");
  var exportedValues=record.CopyValues();exportedValues[2]=-1;descriptors[1]=null;
  Need(record.CopyDescriptors()[1]!=null&&record.CopyDescriptors()[1].BufferIndex==2&&Same(record.CopyValues(),new[]{1,Int32.MaxValue,2}),
   "mutating exported descriptor/value arrays cannot change the stored parsed record");
  result=Get(record,pool,0,null);
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Value,newest)&&result.Binary.StringIndex==2&&Released(result,record)&&reader.Position==31,
   "subsequent actual getter results survive exported-array mutation and do not inspect the trailing poison bytes");
 }
 static void ParsedBoundariesAndLookup(){
  foreach(int count in new[]{2,3,15}){
   BinaryRecordReader reader;RegistrationGate gate;var parsed=Parse(Fields(count),out reader,out gate);
   Need(ParseKnown(parsed,reader,gate,0,count*12,count)&&parsed.Record.CopyDescriptors().Length==count&&parsed.Record.FieldBucketCount==4&&
    parsed.Record.FieldSlotCapacity==(uint)count&&parsed.Record.InitialSlotBytesCleared==(uint)((count/2)*24),
    "fresh field-count boundary and partial initial clear are explicit: "+count);
  }
  BinaryRecordReader r;RegistrationGate g;
  var bits=Parse(Join(Field(UInt32.MaxValue,new[]{Int32.MinValue,-1},2),Field(0,new[]{1},2)),out r,out g);
  Need(ParseKnown(bits,r,g,0,28,2)&&bits.Record.CopyDescriptors()[0].Key==UInt32.MaxValue&&Same(bits.Record.CopyValues(),new[]{Int32.MinValue,-1,1}),
   "unsigned all-ones key and signed payload bit patterns survive actual little-endian parsing");
  var names=new BinaryPointerCacheSnapshot(0,0,new[]{new BinaryNameCacheEntry[0]});
  var warm=bits.Record.ToWarmState(names,new[]{S("Class"),S("Item")},NameIds("Missing",-1));var fallback=S("fallback");
  var result=RecordTableGetter.GetValue(new LoadTableBinarySnapshot(g,true,warm),C("Missing"),fallback,QueryAddress);
  Need(result.Status=="Known"&&result.UsedDefault&&Object.ReferenceEquals(result.Value,fallback)&&result.Binary.NameId==-1&&result.Binary.FieldEntry==null&&!result.MayHaveEffects&&Released(result,bits.Record),
   "numeric signed ID -1 does not match a parsed zero-extended uint key FFFFFFFF");
  var negative=Parse(Join(Field(0,new[]{-1},2),Field(4,new[]{Int32.MinValue},2)),out r,out g);var record=negative.Record;
  result=Get(record,new[]{S("Class"),S("Item"),S("unused"),S("unused"),S("Class")},0,fallback);
  Need(result.Status=="Known"&&!result.UsedDefault&&result.Value==null&&result.Binary.StringIndex==-1&&Released(result,record),
   "a parsed negative string-pool index produces the native known-null result, not the default");
  result=Get(record,new[]{S("Class"),S("Item"),S("unused"),S("unused"),S("Class")},4,fallback);
  Need(result.Status=="Known"&&!result.UsedDefault&&result.Value==null&&result.Binary.StringIndex==Int32.MinValue&&Released(result,record),
   "the full signed 32-bit payload is used by numeric bounds checks");
  result=RecordTableGetter.GetValue(new LoadTableBinarySnapshot(g,null,record.ToWarmState(Names(0),new[]{S("Class")},null)),C("Class"),fallback,QueryAddress);
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownBinaryArchivePresence"&&result.Binary==null&&Released(result,record),
   "parsing record bytes does not invent archive presence or a native archive object");
  result=RecordTableGetter.GetValue(new LoadTableBinarySnapshot(g,true,record.ToWarmState(null,new[]{S("Class")},null)),C("Class"),fallback,QueryAddress);
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownBinaryPointerCache"&&Released(result,record),
   "parsed numeric fields do not fabricate the independent name-pointer cache");
  var empty=Parse(Literal(),out r,out g).Record;var emptyValue=S("");
  result=Get(empty,new[]{S("Class"),emptyValue,S("Other")},0,fallback);
  Need(result.Status=="Known"&&!result.UsedDefault&&Object.ReferenceEquals(result.Value,emptyValue)&&Released(result,empty),
   "a parsed index returning an empty stored Binary string preserves its identity rather than selecting the default");
  var maximum=Join(Field(0,Repeat(65535,1),2),Field(4,new[]{2},2));
  var maximumResult=Parse(maximum,out r,out g);
  Need(ParseKnown(maximumResult,r,g,0,maximum.Length,2)&&maximumResult.Record.CopyValues().Length==65536&&
   maximumResult.Record.CopyDescriptors()[1].BufferIndex==65535&&maximumResult.Record.CopyValues()[65535]==2,
   "65536 total values are supported even though each wire element count is only uint16");
  var tooMany=Join(Field(0,Repeat(65535,1),2),Field(4,new[]{2,3},2));
  NeedRejected(tooMany,0,tooMany.Length,"65537 total values are rejected before cursor/effects instead of truncating or wrapping the count");
 }
 static void InputDomainRejection(){
  var literal=Literal();
  // Every short limit is checked against a physically complete backing array:
  // bytes outside byteLimit cannot rescue a truncated header or payload.
  for(int limit=0;limit<literal.Length;limit++)NeedRejected(literal,0,limit,"own domain rejects each shortened header/payload boundary without reading past limit: "+limit);
  foreach(ushort tag in new ushort[]{5,4,65534,65535}){
   var bytes=Join(Field(0,new[]{1},tag),Field(4,new[]{2},2));
   NeedRejected(bytes,0,bytes.Length,"unsupported tag is a pre-mutation own-domain stop, not emulated native malformed-input handling: "+tag);
  }
  var zero=Join(Field(0,new int[0],2),Field(4,new[]{1},2));
  NeedRejected(zero,0,zero.Length,"a zero element count is outside the selected positive-count success domain");
  var sixteen=Fields(16);NeedRejected(sixteen,0,sixteen.Length,"sixteen fields cannot silently use the 2..15 field storage formula");
  var lateBad=Join(Field(0,new[]{1},2),Field(4,new[]{2},4));
  NeedRejected(lateBad,0,lateBad.Length,"a later unsupported tag does not leave the earlier valid field or first-pass cursor published");
  var shifted=Join(new byte[]{0xff,0xff,0xff},Field(0,new[]{1},2),new byte[]{0x02,0x00});
  NeedRejected(shifted,3,shifted.Length,"nonzero-start truncation preserves the original absolute reader position");
 }
 static void OwnershipAndLifetime(){
  var gate=new RegistrationGate();var reader=new BinaryRecordReader(gate,Literal(),0,24);
  var other=new RegistrationGate();var wrong=BinaryRecordParser.DeserializeFresh(other,reader);
  Need(wrong.Status=="Unsupported"&&wrong.DomainRejected&&wrong.Record==null&&!wrong.GateAcquired&&!wrong.GateReleased&&!wrong.MayHaveEffects&&
   wrong.InitialPosition==0&&wrong.FinalPosition==0&&reader.Position==0&&!reader.Consumed&&!gate.IsHeldByCurrentThread&&!other.IsHeldByCurrentThread,
   "a mismatched gate cannot consume, publish or lock another owner's reader");
  var missingGate=BinaryRecordParser.DeserializeFresh(null,reader);
  Need(missingGate.Status=="Unsupported"&&missingGate.DomainRejected&&!missingGate.GateAcquired&&!missingGate.GateReleased&&missingGate.Record==null&&
   !missingGate.MayHaveEffects&&reader.Position==0&&!reader.Consumed,"a missing table gate is rejected without changing the reader");
  var missingReader=BinaryRecordParser.DeserializeFresh(gate,null);
  Need(missingReader.Status=="Unsupported"&&missingReader.DomainRejected&&missingReader.Record==null&&missingReader.InitialPosition==null&&missingReader.FinalPosition==null&&
   !missingReader.GateAcquired&&!missingReader.GateReleased&&!missingReader.MayHaveEffects&&!gate.IsHeldByCurrentThread,
   "a missing reader reports no invented positions and does not acquire the gate");
  var parsed=BinaryRecordParser.DeserializeFresh(gate,reader);
  Need(ParseKnown(parsed,reader,gate,0,24,2),"ownership rejections leave the original valid reader available to its true owner");
  var again=BinaryRecordParser.DeserializeFresh(gate,reader);
  Need(again.Status=="Unsupported"&&again.DomainRejected&&again.Record==null&&again.InitialPosition==24&&again.FinalPosition==24&&!again.MayHaveEffects&&
   again.GateAcquired&&again.GateReleased&&!gate.IsHeldByCurrentThread&&reader.Position==24&&reader.Consumed&&Same(parsed.Record.CopyValues(),new[]{1,2}),
   "a consumed reader cannot be replayed as a fresh table and does not corrupt the first parsed record");
  var disposed=new BinaryRecordReader(gate,Literal(),0,24);disposed.Dispose();
  var stopped=BinaryRecordParser.DeserializeFresh(gate,disposed);
  Need(stopped.Status=="Unsupported"&&stopped.DomainRejected&&stopped.Record==null&&stopped.InitialPosition==0&&stopped.FinalPosition==0&&!stopped.MayHaveEffects&&
   stopped.GateAcquired&&stopped.GateReleased&&!gate.IsHeldByCurrentThread&&disposed.Position==0&&disposed.IsDisposed&&!disposed.Consumed,
   "a disposed reader stops under its own gate without consuming or advancing");
  reader.Dispose();reader.Dispose();
  Need(reader.IsDisposed&&reader.Consumed&&reader.Position==24&&!gate.IsHeldByCurrentThread&&Same(parsed.Record.CopyValues(),new[]{1,2}),
   "idempotent disposal retains position metadata and cannot invalidate the detached parsed record");
  Need(ThrowsArgument(delegate{new BinaryRecordReader(null,Literal(),0,24);}),"reader construction requires explicit gate ownership");
  Need(ThrowsArgument(delegate{new BinaryRecordReader(gate,null,0,0);}),"unknown byte storage is not treated as an empty stream");
  Need(ThrowsArgument(delegate{new BinaryRecordReader(gate,Literal(),-1,24);}),"negative initial offset is rejected");
  Need(ThrowsArgument(delegate{new BinaryRecordReader(gate,Literal(),4,3);}),"an end limit before the initial offset is rejected");
  Need(ThrowsArgument(delegate{new BinaryRecordReader(gate,Literal(),0,25);}),"an end limit beyond the supplied byte storage is rejected");
 }
 static int Main(){
  try{
   LiteralClassScenario();WindowDuplicateScenario();ParsedBoundariesAndLookup();InputDomainRejection();OwnershipAndLifetime();
   Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+assertions+",\"scenarios\":2,\"scope\":\"Actual bounded fresh tag-2 record bytes through parser and Binary Class getter; own reader/gate and explicit shared archive snapshots, normal allocation contracts only. Domain rejection is not native malformed-input behavior. No original execution or native archive/ClassInfo/Read reconstruction.\"}");
   return 0;
  }catch(Exception error){Console.Error.WriteLine(error);return 1;}
 }
}
