using System;

namespace GrimDynasty.ReverseClient.Actions {
 // Explicit OWN views of the singleton's independently published words.
 // Allocation never implies that either BSS global, the OS lock or an
 // ObjectManager constructor ran. Instance4213A0 retains the caller's object.
 public sealed class GameReachedGoalCriticalState {
  public readonly Field<object> Native10=new Field<object>();
 }
 public sealed class GameReachedGoalLogEngine {
  public readonly Field<object> Virtual00=new Field<object>();
 }
 public sealed class GameReachedGoalEngineState {
  public readonly Field<GameReachedGoalCriticalState> Critical421398=new Field<GameReachedGoalCriticalState>();
  public readonly Field<object> Instance4213A0=new Field<object>();
  public readonly Field<object> LuaCall4219C8=new Field<object>();
  public readonly Field<GameReachedGoalLogEngine> Engine421428=new Field<GameReachedGoalLogEngine>();
 }
 public sealed class GameReachedGoalHookRecord {
  public readonly Field<object> Lua40=new Field<object>();
  public readonly Field<uint> Type60=new Field<uint>();
  public readonly Field<object> Name68=new Field<object>();
 }
 // The observed record stride is exactly0x80. This is stable, explicitly
 // allocated OWN typed storage; it is not an inferred native empty hook list.
 // uint event indices stay zero-extended when scaled to ulong byte offsets.
 public sealed class GameReachedGoalHookRecords {
  public const uint Stride=0x80;
  readonly GameReachedGoalHookRecord[] records;
  bool released;
  public int Count{get{return records.Length;}}
  public GameReachedGoalHookRecords(int count){
   if(count<0)throw new ArgumentOutOfRangeException("count");
   records=new GameReachedGoalHookRecord[count];for(int n=0;n<count;n++)records[n]=new GameReachedGoalHookRecord();
  }
  public GameReachedGoalHookRecord At(uint index,uint at){
   if(released)throw new MissingContract(at,"OWN hook-record allocation was explicitly released");
   ulong byteOffset=(ulong)index*Stride;
   if(byteOffset>=(ulong)records.Length*Stride)throw new MissingContract(at,"Hook event index lies outside the explicitly allocated stride80 OWN records");
   return records[(int)index];
  }
  public void MarkReleased(){released=true;}
 }
 public sealed class GameReachedGoalHookPackState {
  public readonly Field<byte> Initialized1C=new Field<byte>();
  public readonly Field<GameReachedGoalHookRecords> Records10=new Field<GameReachedGoalHookRecords>();
 }
 // Actual Engine01C040 and0EC010, r1-engine-reached-goal-dependencies-run1.
 // Stable OWN fields and sequential execution only: no racing globals,
 // arbitrary byte aliases, native heap, exception unwinding or ABI emulation.
 // No delegates can fabricate initialization, locking, Lua calls or logging.
 public static class GameReachedGoalEngineContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null OWN reached-goal Engine view");return value;}
  public static object GetObjectManager01C040(GameReachedGoalEngineState state,Invocation invocation){
   var i=Need(invocation,0x01c04f);i.At=0x01c04f;var criticalBits=Need(state,i.At).Critical421398.Snapshot();
   i.At=0x01c056;var critical=criticalBits.Read(i.At);
   if(critical==null){i.At=0x01c05e;throw new MissingContract(i.At,"Cold singleton critical-section allocation2DC398 is unclosed");}
   i.At=0x01c0a7;var instanceBits=state.Instance4213A0.Snapshot();
   i.At=0x01c0ae;object instance=instanceBits.Read(i.At);
   if(instance!=null){i.At=0x01c103;return instance;}
   // MOV reads the real +10 cell, preserving unknown bits as an opaque
   // argument. The unsupported Enter import is reached before any lock result
   // or a second instance-global read can be invented.
   i.At=0x01c0b3;critical.Native10.Snapshot();
   i.At=0x01c0b7;throw new MissingContract(i.At,"Cold singleton instance requires actual EnterCriticalSection302328 then allocation/construction; no success inferred");
  }
  static GameReachedGoalHookRecord Record(Field<GameReachedGoalHookRecords> pointer,uint index,uint at){return Need(pointer.Read(at),at).At(index,at);}
  static bool InvalidType(Field<uint> captured,uint at,Invocation i){i.At=at;return unchecked(captured.Read(at)+2u)<=1u;}
  // Native failure logging reloads both Engine and Records10. It does not use
  // the earlier record pointer for Name68. This arm has no supported route in
  // the stable hot subset: a valid Lua call stops before it, and repeated gate
  // reads cannot change in that subset. Keep its real read/call boundaries.
  static void LogFailedHook(GameReachedGoalHookPackState pack,uint index,GameReachedGoalEngineState state,Invocation i){
   i.At=0x0ec0a0;var engineBits=Need(state,i.At).Engine421428.Snapshot();
   i.At=0x0ec0ac;var recordsBits=pack.Records10.Snapshot();
   i.At=0x0ec0b0;var engine=Need(engineBits.Read(i.At),i.At);var table=engine.Virtual00.Snapshot();
   i.At=0x0ec0b3;Record(recordsBits,index,i.At).Name68.Snapshot();
   i.At=0x0ec0b8;table.Read(i.At);throw new MissingContract(i.At,"Engine failure-log virtual slot18 target remains unbound");
  }
  public static void ExecuteEventHook0EC010(GameReachedGoalHookPackState pack,uint eventIndex,GameReachedGoalEngineState state,Invocation invocation){
   var i=Need(invocation,0x0ec01f);i.At=0x0ec01f;
   if(Need(pack,i.At).Initialized1C.Read(i.At)==0){i.At=0x0ec02a;throw new MissingContract(i.At,"HookPack Initialize0EBAE0 remains unclosed; no empty-record fallback");}
   i.At=0x0ec032;var recordsBits=pack.Records10.Snapshot();
   i.At=0x0ec03d;var record=Record(recordsBits,eventIndex,i.At);
   if(record.Lua40.Read(i.At)==null){i.At=0x0ec0ca;return;}
   i.At=0x0ec044;var firstType=record.Type60.Snapshot();
   if(InvalidType(firstType,0x0ec047,i)){i.At=0x0ec0ca;return;}
   // The real body reads both Lua40 and Type60 again, rather than reusing the
   // first gate values. MOV snapshots defer unknown consumption until TEST/ADD.
   i.At=0x0ec04f;var luaAgain=record.Lua40.Snapshot();i.At=0x0ec053;
   if(luaAgain.Read(i.At)==null){LogFailedHook(pack,eventIndex,state,i);return;}
   i.At=0x0ec058;var secondType=record.Type60.Snapshot();
   if(InvalidType(secondType,0x0ec05b,i)){LogFailedHook(pack,eventIndex,state,i);return;}
   i.At=0x0ec06d;Need(state,i.At).LuaCall4219C8.Read(i.At);
   throw new MissingContract(i.At,"Hook Lua function pointer4219C8 target is unbound; later421A18/258100/log effects not fabricated");
  }
 }
}
