using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 public sealed class GameDurationDamageManagerTable {
  public readonly uint Rva;
  GameDurationDamageManagerTable(uint rva){Rva=rva;}
  public static readonly GameDurationDamageManagerTable Observed75EF00=new GameDurationDamageManagerTable(0x75ef00);
 }
 public sealed class GameDurationSentinel {
  public readonly Field<GameDurationSentinel> Left00=new Field<GameDurationSentinel>(),Parent08=new Field<GameDurationSentinel>(),Right10=new Field<GameDurationSentinel>();
  public readonly Field<ushort> Flags18=new Field<ushort>();
  public readonly Field<byte>[] Unwritten1A27=new Field<byte>[14];
  internal GameDurationSentinel(){for(int n=0;n<Unwritten1A27.Length;n++)Unwritten1A27[n]=new Field<byte>();}
 }
 public sealed class GameDurationDamageRecord {
  public readonly Field<object> Virtual00=new Field<object>();
  public readonly Field<uint> Type0C=new Field<uint>();
 }
 // Fixed OWN storage for already-observed0x38 records. This is not native
 // vector allocation/growth or the unclosed matching-record slot30 body.
 public sealed class GameDurationDamageRecords {
  readonly GameDurationDamageRecord[] records;
  bool live=true;
  public long ByteLength{get{return (long)records.Length*0x38;}}
  public GameDurationDamageRecords(int count){if(count<0)throw new ArgumentOutOfRangeException("count");records=new GameDurationDamageRecord[count];for(int n=0;n<count;n++)records[n]=new GameDurationDamageRecord();}
  public void Invalidate(){live=false;}
  public void RequireLive(uint at){if(!live)throw new MissingContract(at,"Released OWN duration record storage");}
  public GameDurationDamageRecord Record(int index){RequireLive(0);return records[index];}
  public GameDurationDamagePointer Pointer(long byteOffset){return new GameDurationDamagePointer(this,byteOffset);}
  internal GameDurationDamageRecord At(long offset,uint at){RequireLive(at);if(offset<0||offset>=ByteLength||offset%0x38!=0)throw new MissingContract(at,"Duration record access outside bounded aligned OWN0x38 storage");return records[(int)(offset/0x38)];}
 }
 public sealed class GameDurationDamagePointer {
  public readonly GameDurationDamageRecords Storage;
  public readonly long ByteOffset;
  internal GameDurationDamagePointer(GameDurationDamageRecords storage,long offset){Storage=storage;ByteOffset=offset;}
 }
 public sealed class GameDurationDamageManagerState {
  static readonly ConditionalWeakTable<GamePlayerRunSpeedCapState,GameDurationDamageManagerState> states=new ConditionalWeakTable<GamePlayerRunSpeedCapState,GameDurationDamageManagerState>();
  public readonly GamePlayerRunSpeedCapState Cap;
  public readonly Field<GameDurationDamageManagerTable> Virtual00=new Field<GameDurationDamageManagerTable>();
  public readonly Field<ulong> Qword08=new Field<ulong>(),Qword18=new Field<ulong>(),Qword24=new Field<ulong>(),Size38=new Field<ulong>(),Qword48=new Field<ulong>(),Qword50=new Field<ulong>(),Qword58=new Field<ulong>(),Qword78=new Field<ulong>();
  public readonly Field<GameDurationSentinel> Head30=new Field<GameDurationSentinel>();
  public readonly Field<uint> Dword40=new Field<uint>(),Dword80=new Field<uint>();
  public readonly Field<GameDurationDamagePointer> Begin60=new Field<GameDurationDamagePointer>(),End68=new Field<GameDurationDamagePointer>(),Capacity70=new Field<GameDurationDamagePointer>();
  public readonly Field<ulong> Unwritten10=new Field<ulong>();
  public readonly Field<uint> Unwritten20=new Field<uint>(),Unwritten2C=new Field<uint>(),Unwritten44=new Field<uint>();
  GameDurationDamageManagerState(GamePlayerRunSpeedCapState cap){Cap=cap;}
  public static GameDurationDamageManagerState Bind(GamePlayerRunSpeedCapState cap){if(cap==null)throw new ArgumentNullException("cap");cap.RequireOwner(cap.Identity.Move,0);return states.GetValue(cap,delegate(GamePlayerRunSpeedCapState owner){return new GameDurationDamageManagerState(owner);});}
  public static GameDurationDamageManagerState ForBound(GamePlayerRunSpeedCapState cap,uint at){if(cap==null)throw new MissingContract(at,"Missing shared Player cap identity");cap.RequireOwner(cap.Identity.Move,at);GameDurationDamageManagerState state;if(!states.TryGetValue(cap,out state))throw new MissingContract(at,"Duration manager requires explicit same-owner binding");return state;}
  public void RequireOwner(MoveCharacter caller,uint at){Cap.RequireOwner(caller,at);}
 }
 public sealed class GameDurationWrite {
  public readonly uint Address,Offset;
  public readonly int Bytes;
  public GameDurationWrite(uint address,uint offset,int bytes){Address=address;Offset=offset;Bytes=bytes;}
 }
 public sealed class GameDurationSentinelInvocation {
  public readonly Invocation Native=new Invocation();
  public readonly List<GameDurationWrite> Writes=new List<GameDurationWrite>();
  public GameDurationSentinel Allocated,ReturnedRax;
  public bool OwnAllocationAttempted,OwnAllocationReturned,Returned;
  internal void Begin(){Native.At=0;Writes.Clear();Allocated=ReturnedRax=null;OwnAllocationAttempted=OwnAllocationReturned=Returned=false;}
 }
 public sealed class GameDurationDamageConstructorInvocation {
  public readonly Invocation Native=new Invocation();
  public readonly List<GameDurationWrite> Writes=new List<GameDurationWrite>();
  public readonly GameDurationSentinelInvocation Helper=new GameDurationSentinelInvocation();
  public GameDurationDamageManagerState ReturnedRax;
  public bool HelperReturned,ReturnIdentityCopied,Returned;
  internal void Begin(){Native.At=0;Writes.Clear();Helper.Begin();ReturnedRax=null;HelperReturned=ReturnIdentityCopied=Returned=false;}
 }
 public sealed class GameDurationDamageLookupInvocation {
  public readonly Invocation Native=new Invocation();
  public Field<GameDurationDamagePointer> CapturedBegin;
  public Field<uint> CapturedKey;
  public GameDurationDamageRecord MatchedRecord;
  public int EndCaptures,InitialBeginReads,LoopBeginCaptures,RecordComparisons;
  public uint Index;
  public long InitialCount,LatestCount;
  public bool Matched,Returned;
  public uint? ReturnedBits;
  internal void Begin(){Native.At=0;CapturedBegin=null;CapturedKey=null;MatchedRecord=null;EndCaptures=InitialBeginReads=LoopBeginCaptures=RecordComparisons=0;Index=0;InitialCount=LatestCount=0;Matched=Returned=false;ReturnedBits=null;}
 }
 public static class GameDurationDamageManagerContracts {
  static uint At(Invocation i,uint at){i.At=at;return at;}
  static void Store(GameDurationDamageConstructorInvocation c,uint at,uint offset,int bytes){c.Native.At=at;c.Writes.Add(new GameDurationWrite(at,offset,bytes));}
  static void NodeStore(GameDurationSentinelInvocation c,uint at,uint offset,int bytes){c.Native.At=at;c.Writes.Add(new GameDurationWrite(at,offset,bytes));}
  // Only normal OWN managed allocation is adapted. Native allocator5DA6C4,
  // NULL/OOM/EH behavior is unclosed; NULL is never fabricated as a sentinel.
  public static GameDurationSentinel AllocateSentinel019520(GameDurationSentinelInvocation c){
   if(c==null)throw new ArgumentNullException("invocation");c.Begin();At(c.Native,0x019524);At(c.Native,0x019529);c.OwnAllocationAttempted=true;
   var node=new GameDurationSentinel();c.Allocated=node;c.OwnAllocationReturned=true;
   NodeStore(c,0x019533,0,8);node.Left00.Write(node);NodeStore(c,0x01953f,8,8);node.Parent08.Write(node);NodeStore(c,0x01954b,0x10,8);node.Right10.Write(node);NodeStore(c,0x01954e,0x18,2);node.Flags18.Write(0x0101);
   c.ReturnedRax=node;c.Returned=true;At(c.Native,0x019558);return node;
  }
  public static GameDurationDamageManagerState Construct2790A0(GameDurationDamageManagerState state,MoveCharacter caller,GameDurationDamageConstructorInvocation c){
   if(c==null)throw new ArgumentNullException("invocation");c.Begin();At(c.Native,0x2790b5);if(state==null)throw new MissingContract(c.Native.At,"Missing same-owner duration manager");state.RequireOwner(caller,c.Native.At);
   Store(c,0x2790b5,8,8);state.Qword08.Write(0);Store(c,0x2790c0,0,8);state.Virtual00.Write(GameDurationDamageManagerTable.Observed75EF00);Store(c,0x2790c6,0x18,8);state.Qword18.Write(0);Store(c,0x2790ca,0x24,8);state.Qword24.Write(0);
   Store(c,0x2790d3,0x30,8);state.Head30.Write(null);Store(c,0x2790d6,0x38,8);state.Size38.Write(0);At(c.Native,0x2790da);var node=AllocateSentinel019520(c.Helper);c.HelperReturned=true;
   Store(c,0x2790df,0x30,8);state.Head30.Write(node);At(c.Native,0x2790e2);c.ReturnedRax=state;c.ReturnIdentityCopied=true;
   Store(c,0x2790ea,0x40,4);state.Dword40.Write(0);Store(c,0x2790ed,0x48,8);state.Qword48.Write(0);Store(c,0x2790f1,0x50,8);state.Qword50.Write(0);Store(c,0x2790f5,0x58,8);state.Qword58.Write(0);
   Store(c,0x2790f9,0x60,8);state.Begin60.Write(null);Store(c,0x2790fd,0x68,8);state.End68.Write(null);Store(c,0x279101,0x70,8);state.Capacity70.Write(null);Store(c,0x279105,0x78,8);state.Qword78.Write(0);Store(c,0x279109,0x80,4);state.Dword80.Write(0);
   c.Returned=true;At(c.Native,0x279119);return state;
  }
  static long Difference(GameDurationDamagePointer end,GameDurationDamagePointer begin,uint at){
   if(end==null&&begin==null)return 0;
   if(end==null||begin==null||!Object.ReferenceEquals(end.Storage,begin.Storage))throw new MissingContract(at,"Duration pointer difference lacks one explicit OWN storage identity");
   end.Storage.RequireLive(at);
   if(end.ByteOffset<0||end.ByteOffset>end.Storage.ByteLength||begin.ByteOffset<0||begin.ByteOffset>begin.Storage.ByteLength)throw new MissingContract(at,"Duration pointer outside bounded OWN storage");
   return end.ByteOffset-begin.ByteOffset;
  }
  static long Count(long bytes,uint at){if(bytes<0)throw new MissingContract(at,"Negative duration span outside bounded normal domain; not native empty");return bytes/0x38;}
  static float Zero(GameDurationDamageLookupInvocation c){At(c.Native,0x279d81);c.ReturnedBits=0;c.Returned=true;At(c.Native,0x279d8a);return 0.0f;}
  public static float GetFixedDamage279D10(GameDurationDamageManagerState state,MoveCharacter caller,Field<uint> index,GameDurationDamageLookupInvocation c){
   if(c==null)throw new ArgumentNullException("invocation");c.Begin();At(c.Native,0x279d16);if(state==null)throw new MissingContract(c.Native.At,"Missing same-owner duration manager");state.RequireOwner(caller,c.Native.At);
   var end=state.End68.Snapshot();c.EndCaptures++;At(c.Native,0x279d1a);if(index==null)throw new MissingContract(c.Native.At,"Missing index-register observation");c.CapturedKey=index.Snapshot();
   At(c.Native,0x279d1c);c.InitialBeginReads++;long difference=Difference(end.Read(c.Native.At),state.Begin60.Read(c.Native.At),c.Native.At);At(c.Native,0x279d30);c.InitialCount=Count(difference,c.Native.At);c.LatestCount=c.InitialCount;
   At(c.Native,0x279d44);if(c.InitialCount==0)return Zero(c);
   At(c.Native,0x279d46);c.CapturedBegin=state.Begin60.Snapshot();c.LoopBeginCaptures++;
   for(;;){
    At(c.Native,0x279d50);long offset=(long)c.Index*0x38;At(c.Native,0x279d54);var begin=c.CapturedBegin.Read(c.Native.At);if(begin==null)throw new MissingContract(c.Native.At,"Null captured duration record begin");var record=begin.Storage.At(begin.ByteOffset+offset,c.Native.At);c.RecordComparisons++;
    uint type=record.Type0C.Read(c.Native.At),wanted=c.CapturedKey.Read(c.Native.At);
    if(type==wanted){c.Matched=true;c.MatchedRecord=record;At(c.Native,0x279d95);var table=record.Virtual00.Snapshot();At(c.Native,0x279d9e);table.Read(c.Native.At);throw new MissingContract(c.Native.At,"Matching duration record slot30 target/body unclosed; same record tail-call not replaced by a callback");}
    At(c.Native,0x279d5b);end=state.End68.Snapshot();c.EndCaptures++;At(c.Native,0x279d5f);c.Index=unchecked(c.Index+1);At(c.Native,0x279d62);difference=Difference(end.Read(c.Native.At),begin,c.Native.At);At(c.Native,0x279d6b);c.LatestCount=Count(difference,c.Native.At);
    At(c.Native,0x279d7c);if((ulong)c.Index>=(ulong)c.LatestCount)return Zero(c);
   }
  }
 }
}
