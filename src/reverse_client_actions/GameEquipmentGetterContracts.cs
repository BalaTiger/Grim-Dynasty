using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // OWN declared storage, not EquipManager/vector construction or native memory.
 // Every observed field starts Unknown. Other bytes of each 12-byte record are
 // deliberately absent: neither getter reads them. No equipment is inferred.
 public sealed class GameEquipmentRecord {
  public readonly Field<uint> Id00=new Field<uint>(),Location04=new Field<uint>();
  public readonly Field<byte> Byte09=new Field<byte>();
 }
 public sealed class GameEquipmentStorage {
  public const int MaximumRecords=4096;
  readonly GameEquipmentRecord[] records;bool released;
  public int Capacity{get{return records.Length;}}
  public GameEquipmentStorage(int capacity){
   if(capacity<0||capacity>MaximumRecords)throw new ArgumentOutOfRangeException("capacity","OWN bounded storage supports 0..4096 records");
   records=new GameEquipmentRecord[capacity];for(int n=0;n<capacity;n++)records[n]=new GameEquipmentRecord();
  }
  public GameEquipmentRecord Record(int index){if(index<0||index>=records.Length)throw new ArgumentOutOfRangeException("index");return records[index];}
  public void MarkReleased(){released=true;}
  internal void RequireLive(uint at){if(released)throw new MissingContract(at,"Equipment backing was explicitly released");}
  internal GameEquipmentRecord At(long offset,uint at){
   RequireLive(at);if(offset<0||offset%12!=0||offset/12>=records.Length)throw new MissingContract(at,"Equipment record access exceeds the declared aligned OWN extent");return records[(int)(offset/12)];
  }
 }
 public sealed class GameEquipmentPointer {
  public readonly GameEquipmentStorage Storage;public readonly long ByteOffset;
  public GameEquipmentPointer(GameEquipmentStorage storage,long byteOffset){if(storage==null)throw new ArgumentNullException("storage");Storage=storage;ByteOffset=byteOffset;}
 }
 public sealed class GameEquipmentManagerState {
  static readonly ConditionalWeakTable<GameMovementCharacterState,GameEquipmentManagerState> states=new ConditionalWeakTable<GameMovementCharacterState,GameEquipmentManagerState>();
  public readonly GameMovementCharacterState Owner;
  public readonly Field<GameEquipmentPointer> Begin08=new Field<GameEquipmentPointer>(),End10=new Field<GameEquipmentPointer>();
  GameEquipmentManagerState(GameMovementCharacterState owner){Owner=owner;}
  // This is the same owner's embedded +5C8 view, not an allocated second owner.
  public static GameEquipmentManagerState ForOwner(GameMovementCharacterState owner){if(owner==null)throw new ArgumentNullException("owner");return states.GetValue(owner,delegate(GameMovementCharacterState value){return new GameEquipmentManagerState(value);});}
 }
 public sealed class GameEquipmentGetterInvocation {
  public readonly Invocation Native=new Invocation();
  public readonly Field<GameEquipmentPointer> InitialEnd=new Field<GameEquipmentPointer>(),InitialBegin=new Field<GameEquipmentPointer>(),CapturedBegin=new Field<GameEquipmentPointer>();
  public readonly List<Field<GameEquipmentPointer>> FreshEnds=new List<Field<GameEquipmentPointer>>();
  public readonly Field<long> InitialCount=new Field<long>(),LastCount=new Field<long>();
  public readonly Field<uint> Result=new Field<uint>();
  public GameEquipmentManagerState Manager;public uint Index;public int VisitedRecords;public bool Used,Returned;
 }
 // Rea r1-player-equipment-getters-run1:284720/284670. Exclusive OWN storage,
 // same-backing signed pointer differences without signed overflow, finite
 // aligned accessed records. Pointer subtraction is not full vector validation;
 // unused records/fields and a sub-stride empty range are never prevalidated.
 // Native allocator/lifetime/concurrency/invalid-address behavior is unclaimed.
 public static class GameEquipmentGetterContracts {
  static uint At(GameEquipmentGetterInvocation c,uint at){c.Native.At=at;return at;}
  static long Difference(Field<GameEquipmentPointer> end,Field<GameEquipmentPointer> begin,uint at){
   var e=end.Read(at);var b=begin.Read(at);if(e==null&&b==null)return 0;
   if(e==null||b==null||!Object.ReferenceEquals(e.Storage,b.Storage))throw new MissingContract(at,"Pointer subtraction requires known same-backing equipment pointers or two known nulls");
   e.Storage.RequireLive(at);try{return checked(e.ByteOffset-b.ByteOffset);}catch(OverflowException error){throw new MissingContract(at,"Equipment pointer difference exceeds the supported signed range",error);}
  }
  static GameEquipmentRecord Record(GameEquipmentGetterInvocation c,uint at){
   At(c,at);var begin=c.CapturedBegin.Read(at);if(begin==null)throw new MissingContract(at,"Null equipment begin at record access");
   if(c.VisitedRecords>=GameEquipmentStorage.MaximumRecords)throw new MissingContract(at,"OWN finite traversal limit; no wrapped-index success inferred");
   long offset;try{offset=checked(begin.ByteOffset+(long)c.Index*12L);}catch(OverflowException error){throw new MissingContract(at,"Equipment record address exceeds the supported signed range",error);}
   var record=begin.Storage.At(offset,at);c.VisitedRecords++;return record;
  }
  static Field<uint> Return(GameEquipmentGetterInvocation c,Field<uint> result,uint at){c.Result.CopyFrom(result);At(c,at);c.Returned=true;return c.Result.Snapshot();}
  public static Field<uint> GetWeaponIdLeft284720(GameEquipmentManagerState manager,GameEquipmentGetterInvocation c){return Get(manager,c,true);}
  public static Field<uint> GetWeaponIdRight284670(GameEquipmentManagerState manager,GameEquipmentGetterInvocation c){return Get(manager,c,false);}
  static Field<uint> Get(GameEquipmentManagerState manager,GameEquipmentGetterInvocation c,bool left){
   if(c==null)throw new ArgumentNullException("invocation");if(c.Used)throw new InvalidOperationException("Use a fresh equipment getter invocation");c.Used=true;c.Manager=manager;
   At(c,left?0x284725u:0x284675u);if(manager==null)throw new MissingContract(c.Native.At,"Null embedded EquipManager view");manager.Owner.Entity.RequireLive(c.Native.At);c.InitialEnd.CopyFrom(manager.End10.Snapshot());
   At(c,left?0x284733u:0x284683u);c.InitialBegin.CopyFrom(manager.Begin08.Snapshot());long count=Difference(c.InitialEnd,c.InitialBegin,c.Native.At)/12L;
   At(c,left?0x28474du:0x28469du);c.InitialCount.Write(count);c.LastCount.Write(count);At(c,left?0x284750u:0x2846a0u);
   if(count==0)return Zero(c,left);
   At(c,left?0x284752u:0x2846a2u);c.CapturedBegin.CopyFrom(manager.Begin08.Snapshot());c.Index=0;
   while(true){
    var record=Record(c,left?0x284764u:0x2846b4u);
    var locationBits=record.Location04.Snapshot();At(c,left?0x284764u:0x2846b9u);uint location=locationBits.Read(c.Native.At);
    if(left){
     if(location==1){At(c,0x2847a0);if(record.Byte09.Read(c.Native.At)!=0)return Zero(c,true);At(c,0x2847a8);return Return(c,record.Id00.Snapshot(),0x2847b1);}
    }else{
     if(location==0){At(c,0x284714);return Return(c,record.Id00.Snapshot(),0x284718);}
     At(c,0x2846bd);if(location==1){At(c,0x2846c2);if(record.Byte09.Read(c.Native.At)!=0){At(c,0x2846fe);return Return(c,record.Id00.Snapshot(),0x284707);}}
    }
    At(c,left?0x28476cu:0x2846cau);var freshEnd=manager.End10.Snapshot();c.FreshEnds.Add(freshEnd);
    At(c,left?0x284770u:0x2846ceu);c.Index=unchecked(c.Index+1u);
    At(c,left?0x284773u:0x2846d1u);count=Difference(freshEnd,c.CapturedBegin,c.Native.At)/12L;
    At(c,left?0x284789u:0x2846e7u);c.LastCount.Write(count);At(c,left?0x28478cu:0x2846eau);
    if((ulong)c.Index>=unchecked((ulong)count))return Zero(c,left);
   }
  }
  static Field<uint> Zero(GameEquipmentGetterInvocation c,bool left){At(c,left?0x284791u:0x2846efu);return Return(c,Field<uint>.Of(0),left?0x284798u:0x2846f6u);}
 }
}
