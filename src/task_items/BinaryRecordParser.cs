using System;
namespace GrimDynasty.TaskItems {
 public sealed class BinaryRecordReader:IDisposable {
  public const int MaximumBytes=1048576;
  public readonly RegistrationGate Gate;
  readonly int limit;
  byte[] bytes;
  int position;
  bool disposed,consumed;
  public BinaryRecordReader(RegistrationGate ownerGate,byte[] bytes,int initialOffset,int byteLimit){
   if(ownerGate==null)throw new ArgumentNullException("ownerGate");
   if(bytes==null)throw new ArgumentNullException("bytes");
   if(bytes.Length>MaximumBytes)throw new ArgumentOutOfRangeException("bytes");
   if(initialOffset<0||byteLimit<initialOffset||byteLimit>bytes.Length)throw new ArgumentOutOfRangeException("byteLimit");
   Gate=ownerGate;this.bytes=(byte[])bytes.Clone();position=initialOffset;limit=byteLimit;
  }
  public int Position {get{Gate.Enter();try{return position;}finally{Gate.Exit();}}}
  public bool IsDisposed {get{Gate.Enter();try{return disposed;}finally{Gate.Exit();}}}
  public bool Consumed {get{Gate.Enter();try{return consumed;}finally{Gate.Exit();}}}
  public void Dispose(){Gate.Enter();try{disposed=true;bytes=null;}finally{Gate.Exit();}}
  // Parser access is exclusively under this same gate. Disposal retains the
  // logical cursor/consumption history; no native BinaryReader ABI is implied.
  internal byte[] Data {get{return bytes;}}
  internal int Limit {get{return limit;}}
  internal int Cursor {get{return position;}}
  internal bool Disposed {get{return disposed;}}
  internal bool WasConsumed {get{return consumed;}}
  internal void Move(int next){consumed=true;position=next;}
 }
 public sealed class BinaryRecordDescriptor {
  public readonly uint Key,BufferIndex,ElementCount;
  public readonly int TypeTag;
  internal BinaryRecordDescriptor(uint key,int typeTag,uint index,uint count){
   Key=key;TypeTag=typeTag;BufferIndex=index;ElementCount=count;
  }
 }
 public sealed class BinaryParsedRecord {
  public readonly RegistrationGate Gate;
  public readonly BinaryFieldSnapshot Fields;
  public readonly uint FieldBucketCount,FieldSlotCapacity,InitialSlotBytesCleared;
  public readonly uint BooleanCount;
  readonly uint[][] rawValues;
  readonly uint[] booleanWords;
  readonly BinaryRecordDescriptor[] descriptors;
  internal BinaryParsedRecord(RegistrationGate gate,BinaryFieldSnapshot fields,uint[][] rawValues,
   uint[] booleanWords,uint booleanCount,BinaryRecordDescriptor[] descriptors){
   Gate=gate;Fields=fields;this.rawValues=rawValues;this.descriptors=descriptors;
   this.booleanWords=booleanWords;BooleanCount=booleanCount;
   FieldBucketCount=fields.Mask+1;FieldSlotCapacity=(uint)descriptors.Length;
   InitialSlotBytesCleared=(uint)((descriptors.Length>>1)*24);
  }
  // Preserve all words without assigning numeric/float meaning to tag 0/1.
  // The existing getter consumes only tag 2 as signed string-pool indices.
  public uint[] CopyRawValues(int typeTag){
   if(typeTag<0||typeTag>2)throw new ArgumentOutOfRangeException("typeTag");
   return (uint[])rawValues[typeTag].Clone();
  }
  // Logical tag-3 bits, LSB first in DWORDs. Unused export bits are zero;
  // this interface does not expose native allocation capacity or padding.
  // Tag-3 wire words are converted by !=0 and are not a raw-value buffer.
  public uint[] CopyBooleanWords(){
   var copy=(uint[])booleanWords.Clone();int tail=(int)(BooleanCount&31U);
   if(tail!=0)copy[copy.Length-1]&=(1U<<tail)-1U;
   return copy;
  }
  public int[] CopyValues(){
   uint[] source=rawValues[2];var copy=new int[source.Length];
   for(int i=0;i<source.Length;i++)copy[i]=unchecked((int)source[i]);
   return copy;
  }
  public BinaryRecordDescriptor[] CopyDescriptors(){return (BinaryRecordDescriptor[])descriptors.Clone();}
  // These remain explicit archive inputs. No names, string pool, query
  // address, archive loader or concrete object Read is fabricated here.
  // Shared mutable names must use this record's gate or exclusive ownership.
  public BinaryWarmState ToWarmState(BinaryPointerCacheSnapshot names,TableString[] sharedPool,BinaryNameIdSnapshot nameIds){
   return new BinaryWarmState(names,sharedPool,Fields,CopyValues(),nameIds);
  }
  // The compact pool retains every original ID and its immutable owned wire.
  // Numeric fields are unchanged; only string materialization is deferred.
  public BinaryWarmState ToWarmStateWithPool(BinaryPointerCacheSnapshot names,BinaryStringPool sharedPool,BinaryNameIdSnapshot nameIds){
   if(sharedPool==null)throw new ArgumentNullException("sharedPool");
   return BinaryWarmState.FromPool(names,sharedPool,Fields,CopyValues(),nameIds);
  }
 }
 public sealed class BinaryRecordParseResult {
  public readonly string Status,Reason;
  public readonly BinaryParsedRecord Record;
  public readonly int? InitialPosition,FinalPosition,FirstPassEnd;
  public readonly bool GateAcquired,GateReleased,ReaderRewound,MayHaveEffects,DomainRejected;
  public readonly int CompletedFields;
  internal BinaryRecordParseResult(string reason,BinaryParsedRecord record,int? initial,int? final,int? firstEnd,
   bool acquired,bool released,bool rewound,int completed,bool effects,bool rejected){
   Status=reason==null?"Known":"Unsupported";Reason=reason;Record=reason==null?record:null;
   InitialPosition=initial;FinalPosition=final;FirstPassEnd=firstEnd;
   GateAcquired=acquired;GateReleased=released;ReaderRewound=rewound;
   CompletedFields=completed;MayHaveEffects=effects;DomainRejected=rejected;
  }
 }
 public static class BinaryRecordParser {
  // Covered normal-return domain of Engine 13F510, not a bool-returning
  // native parser: fresh typed storage, complete stable bytes, tags 0/1/2/3,
  // positive counts, 2..63 fields except 16/17/32/33, <=65536 total words,
  // no offset wrap. 63 is an own coverage bound, not a native maximum.
  //
  // Native normal allocation/copy/zeroing succeeds: 030D20 -> 02E0F0 ->
  // 022F00(m,4), zero-byte memmove from the fresh null vector, no old free;
  // 030D20 zeros the new values and publishes each vector's logical end.
  // Empty vectors receive zero-size no-op preparation. Positive bit storage:
  // 0C69F0 -> 047D30 -> 047F10 -> 048130 -> 02DE40 -> known 02E0F0.
  // Fresh word count is ceil(bits/32); 0483A0 fills all new DWORDs with
  // zero, 048130 publishes end, 047F8F writes the independent bit count.
  // 047D30 fills the valid bits, then 047EA0 writes an output iterator.
  // No nonfresh bit shifting/growth or native allocator/EH is modeled.
  // Saved rea-container-reserve/04..06 closes 022F00's count*size check,
  // small allocation and >=4096-byte aligned allocation paths; both remain
  // under the existing normal allocator-success contract. The new
  // rea-binary-parser-vector-growth/04..05 closes the fresh vector wiring.
  // 284080 receives req=fields>>1. Under the normal finite accurate-log
  // contract, req=1..7 yields four buckets, 9..15 eight, 17..31 sixteen.
  // The saved import/constant bytes prove CRT log * binary64 log2(e),
  // followed by truncation. req=8/16 remain excluded: target CRT return
  // bits and MXCSR are not recovered, so exact integer-boundary rounding
  // cannot be replaced with a host Math.Log call or mathematical log2.
  // Its operator_new/memset and descriptor allocation are success contracts,
  // not recovered allocator, OOM, exception or native memory-layout behavior.
  sealed class Run {
   internal int? Initial,Final,FirstEnd;
   internal bool Acquired,Released,Rewound,Effects,Rejected;
   internal int Completed;
   internal BinaryParsedRecord Record;
   internal BinaryRecordParseResult Result(string reason){
    return new BinaryRecordParseResult(reason,Record,Initial,Final,FirstEnd,Acquired,Released,Rewound,Completed,Effects,Rejected);
   }
  }
  static uint ReadWord(byte[] bytes,int offset){
   return (uint)bytes[offset]|((uint)bytes[offset+1]<<8)|((uint)bytes[offset+2]<<16)|((uint)bytes[offset+3]<<24);
  }
  static string Preflight(BinaryRecordReader reader){
   int cursor=reader.Cursor,fields=0,total=0;byte[] bytes=reader.Data;
   while(cursor<reader.Limit){
    int remaining=reader.Limit-cursor;
    if(remaining<8)return "BinaryRecordHeaderOutsideCoveredDomain";
    uint header=ReadWord(bytes,cursor);int count=(int)(header>>16);
    if((header&65535U)>3)return "BinaryRecordTagOutsideCoveredDomain";
    if(count==0)return "BinaryRecordZeroCountOutsideCoveredDomain";
    if(count*4>remaining-8)return "BinaryRecordPayloadOutsideCoveredDomain";
    if(count>BinaryWarmState.MaximumBufferValues-total)return "BinaryRecordValueBudgetExceeded";
    total+=count;fields++;
    if(fields>63)return "BinaryRecordFieldCountOutsideCoveredDomain";
    cursor+=8+count*4;
   }
   if(fields<2)return "BinaryRecordFieldCountOutsideCoveredDomain";
   int request=fields>>1;
   return request==8||request==16?"BinaryRecordBucketRoundingOutsideCoveredDomain":null;
  }
  static void ReplayFresh(RegistrationGate gate,BinaryRecordReader reader,Run run){
   byte[] bytes=reader.Data;int initial=reader.Cursor,fields=0;
   var totals=new int[4];
   // Preflight is an own coverage gate, not native rejection semantics.
   // Here the two real passes begin: each cursor write consumes the reader,
   // so even a later managed failure cannot make it reusable.
   while(reader.Cursor<reader.Limit){
    uint header=ReadWord(bytes,reader.Cursor);
    int count=(int)(header>>16),typeTag=(int)(header&65535U);
    reader.Move(reader.Cursor+8);run.Effects=true;
    totals[typeTag]+=count;fields++;reader.Move(reader.Cursor+count*4);
   }
   run.FirstEnd=reader.Cursor;
   var rawValues=new uint[3][];
   for(int i=0;i<rawValues.Length;i++)rawValues[i]=new uint[totals[i]];
   var booleanWords=new uint[(totals[3]+31)>>5];
   int bucketCount=fields<16?4:fields<32?8:16;
   uint mask=(uint)(bucketCount-1);
   var buckets=new BinaryFieldEntry[bucketCount][];
   for(int i=0;i<buckets.Length;i++)buckets[i]=new BinaryFieldEntry[0];
   var descriptors=new BinaryRecordDescriptor[fields];
   // Fresh ctor 13F381/38F gives index shift=0 and used=0. 284080
   // prepares the covered bucket count/capacity=fields and clears only (fields>>1)*24
   // bytes of native slots; own empty arrays do not claim full native
   // slot zeroing. Each subsequent insertion writes its complete node.
   reader.Move(initial);run.Rewound=true;
   var valueIndices=new int[4];
   while(reader.Cursor<reader.Limit){
    uint header=ReadWord(bytes,reader.Cursor);reader.Move(reader.Cursor+4);
    uint key=ReadWord(bytes,reader.Cursor);reader.Move(reader.Cursor+4);
    int count=(int)(header>>16),typeTag=(int)(header&65535U);
    int start=valueIndices[typeTag],source=reader.Cursor;
    if(typeTag==3){
     for(int i=0;i<count;i++){
      uint value=ReadWord(bytes,source+i*4);
      // 13F828/82C publish each DWORD read before updating its bit.
      reader.Move(source+(i+1)*4);
      int bit=start+i;uint bitMask=1U<<(bit&31);
      if(value!=0)booleanWords[bit>>5]|=bitMask;
      else booleanWords[bit>>5]&=~bitMask;
     }
    }else{
     for(int i=0;i<count;i++)rawValues[typeTag][start+i]=ReadWord(bytes,source+i*4);
     reader.Move(source+count*4);
    }
    valueIndices[typeTag]+=count;
    var descriptor=new BinaryRecordDescriptor(key,typeTag,(uint)start,(uint)count);
    descriptors[run.Completed]=descriptor;
    // MOV EDX,ESI zero-extends raw uint key. 284160 prepends even equal
    // keys, so a later duplicate is the first numeric getter match.
    var entry=new BinaryFieldEntry((ulong)key,new BinaryFieldValue(typeTag,(uint)start));
    int bucket=(int)(key&mask);var previous=buckets[bucket];
    var next=new BinaryFieldEntry[previous.Length+1];
    next[0]=entry;Array.Copy(previous,0,next,1,previous.Length);buckets[bucket]=next;
    run.Completed++;
   }
   run.Record=new BinaryParsedRecord(gate,new BinaryFieldSnapshot(0,mask,buckets),rawValues,
    booleanWords,(uint)totals[3],descriptors);
  }
  public static BinaryRecordParseResult DeserializeFresh(RegistrationGate tableGate,BinaryRecordReader reader){
   var run=new Run();
   if(reader!=null){run.Initial=reader.Position;run.Final=run.Initial;}
   string reason=null;
   if(tableGate==null)reason="UnknownBinaryRecordTableGate";
   else if(reader==null)reason="UnknownBinaryRecordReader";
   else if(!Object.ReferenceEquals(tableGate,reader.Gate))reason="BinaryRecordReaderGateMismatch";
   if(reason!=null){run.Rejected=true;return run.Result(reason);}
   try{
    tableGate.Enter();run.Acquired=true;run.Initial=reader.Cursor;run.Final=run.Initial;
    if(reader.Disposed)reason="BinaryRecordReaderDisposed";
    else if(reader.WasConsumed)reason="BinaryRecordReaderConsumed";
    else reason=Preflight(reader);
    if(reason!=null)run.Rejected=true;
    else ReplayFresh(tableGate,reader,run);
   }catch(Exception e){reason="OwnBinaryRecordParserOperationThrew:"+e.GetType().FullName;}
   finally{
    if(run.Acquired){
     run.Final=reader.Cursor;
     // Own Monitor cleanup on unsupported/managed failure, not native EH
     // recovery. Cursor changes and consumption are deliberately retained.
     try{tableGate.Exit();run.Released=true;}
     catch(Exception e){reason="OwnBinaryRecordParserUnlockThrew:"+e.GetType().FullName;}
    }
   }
   return run.Result(reason);
  }
 }
}
