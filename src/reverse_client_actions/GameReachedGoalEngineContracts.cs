using System;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // Explicit OWN views of the singleton's independently published words.
 // Allocation never implies that either BSS global, the OS lock or an
 // ObjectManager constructor ran. Instance4213A0 retains the caller's object.
 public sealed class GameReachedGoalCriticalState {
  public readonly Field<object> Native10=new Field<object>();
 }
 public sealed class GameReachedGoalLogEngine {
  static readonly ConditionalWeakTable<WorldEngine,GameReachedGoalLogEngine> engines=new ConditionalWeakTable<WorldEngine,GameReachedGoalLogEngine>();
  // Null only for the retained independent component view. A production
  // sidecar borrows one existing WorldEngine; it never constructs an Engine.
  public readonly WorldEngine WorldIdentity;
  public GameReachedGoalLogEngine(){}
  GameReachedGoalLogEngine(WorldEngine identity){WorldIdentity=identity;}
  public static GameReachedGoalLogEngine ForWorldEngine(WorldEngine identity){
   if(identity==null)throw new ArgumentNullException("identity");
   return engines.GetValue(identity,delegate(WorldEngine value){return new GameReachedGoalLogEngine(value);});
  }
  public readonly Field<object> Virtual00=new Field<object>();
  // The initializer dereferences this pointer before considering Hook count.
  // This is the same Engine421428 view used by failure logging, not a second
  // initialized Engine or an inferred null Lua service.
  public readonly Field<GameReachedGoalLuaService> LuaService68=new Field<GameReachedGoalLuaService>();
 }
 public sealed class GameReachedGoalLuaService {
  public readonly Field<object> Lua00=new Field<object>();
  public readonly Field<ushort> Options08=new Field<ushort>();
  // Same three independently known words consumed by the provider loop.
  // Merely creating this view does not initialize them or supply providers.
  public readonly NavigationCrowdPointerVector<GameLuaProvider> Providers40=new NavigationCrowdPointerVector<GameLuaProvider>();
  public readonly Field<byte> Initialized58=new Field<byte>();
 }
 public sealed class GameLuaProvider {
  public readonly Field<object> Virtual00=new Field<object>();
 }
 public sealed class GameReachedGoalEngineState {
  public readonly Field<GameReachedGoalCriticalState> Critical421398=new Field<GameReachedGoalCriticalState>();
  public readonly Field<object> Instance4213A0=new Field<object>();
  public readonly Field<object> LuaCall4219C8=new Field<object>();
  // Legacy independent-component cell only. After BorrowEngineGlobal this is
  // an inactive shadow: production contracts MUST NOT read it, and writing it
  // cannot replace or initialize the borrowed authoritative global.
  public readonly Field<GameReachedGoalLogEngine> Engine421428=new Field<GameReachedGoalLogEngine>();
  Field<WorldEngine> borrowedEngineGlobal;
  public Field<WorldEngine> BorrowedEngineGlobal{get{return borrowedEngineGlobal;}}
  public void BorrowEngineGlobal(Field<WorldEngine> global){
   if(global==null)throw new ArgumentNullException("global");
   if(borrowedEngineGlobal!=null){if(!Object.ReferenceEquals(borrowedEngineGlobal,global))throw new InvalidOperationException("Goal Engine global cannot be rebound to another cell");return;}
   // A known legacy value, even null, would introduce two competing sources.
   // Reject it during explicit OWN binding without consuming the new global.
   bool componentKnown;try{Engine421428.Read(0);componentKnown=true;}catch(MissingContract){componentKnown=false;}
   if(componentKnown)throw new InvalidOperationException("Known component Engine cell conflicts with authoritative shared-global binding");
   borrowedEngineGlobal=global;
  }
  public GameReachedGoalEngineSnapshot CaptureEngine421428(){
   return borrowedEngineGlobal==null?new GameReachedGoalEngineSnapshot(Engine421428.Snapshot()):new GameReachedGoalEngineSnapshot(borrowedEngineGlobal.Snapshot());
  }
 }
 // Captures pointer bits at the native MOV, not the service's later contents.
 // Unknown and null are retained until Read(at); a changed global cannot
 // replace a previously captured identity, while that identity's fields stay
 // shared and mutable. There is no per-context copy of a WorldEngine object.
 public sealed class GameReachedGoalEngineSnapshot {
  readonly Field<WorldEngine> world;
  readonly Field<GameReachedGoalLogEngine> component;
  internal GameReachedGoalEngineSnapshot(Field<WorldEngine> value){world=value;}
  internal GameReachedGoalEngineSnapshot(Field<GameReachedGoalLogEngine> value){component=value;}
  public GameReachedGoalLogEngine Read(uint at){
   if(component!=null)return component.Read(at);
   var identity=world.Read(at);return identity==null?null:GameReachedGoalLogEngine.ForWorldEngine(identity);
  }
 }
 public sealed class GameReachedGoalHookRecord {
  public readonly Field<byte> LabelFirst00=new Field<byte>(),FunctionFirst20=new Field<byte>();
  public readonly Field<ulong> LabelLength10=new Field<ulong>(),LabelCapacity18=new Field<ulong>();
  public readonly Field<ulong> FunctionLength30=new Field<ulong>(),FunctionCapacity38=new Field<ulong>();
  // This alternate interpretation of the string's union is only consumed on
  // the heap arm. No arbitrary aliasing between inline bytes and pointers is
  // claimed; the nonempty function call remains Unsupported.
  public readonly Field<object> FunctionHeap20=new Field<object>();
  public readonly Field<object> Lua40=new Field<object>();
  public readonly Field<ulong> Word48=new Field<ulong>(),Word50=new Field<ulong>(),Word58=new Field<ulong>();
  public readonly Field<uint> Type60=new Field<uint>();
  public readonly Field<object> Name68=new Field<object>();
  public readonly Field<ulong> Word70=new Field<ulong>(),Word78=new Field<ulong>();
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
  public readonly Field<object> Virtual00=new Field<object>();
  public readonly Field<uint> Owner08=new Field<uint>(),Count18=new Field<uint>();
  public readonly Field<byte> Initialized1C=new Field<byte>();
  public readonly Field<GameReachedGoalHookRecords> Records10=new Field<GameReachedGoalHookRecords>();
 }
 public sealed class GameReachedGoalHookPackTable {
  public readonly uint Rva;
  GameReachedGoalHookPackTable(uint rva){Rva=rva;}
  public static readonly GameReachedGoalHookPackTable HookPack=new GameReachedGoalHookPackTable(0x339b70);
 }
 // Actual Engine01C040 and0EC010, r1-engine-reached-goal-dependencies-run1.
 // Stable OWN fields and sequential execution only: no racing globals,
 // arbitrary byte aliases, native heap, exception unwinding or ABI emulation.
 // No delegates can fabricate initialization, locking, Lua calls or logging.
 public static class GameReachedGoalEngineContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null OWN reached-goal Engine view");return value;}
  // Explicit managed storage adaptation of the observed EBDE0/EBEB0 normal
  // writes. It does not execute native allocation, memset, CFG guard, iterator
  // unwinding or the complete Character constructor. The supported allocation
  // domain is count0..11, including Character's directly observed count11.
  // Header stores precede allocation; Records10 is published only after every
  // OWN element has received the evidenced constructor stores.
  public static GameReachedGoalHookRecords ConstructOwnColdPack0EBDE0(GameReachedGoalHookPackState pack,uint count,Invocation invocation){
   var i=Need(invocation,0x0ebe06);i.At=0x0ebe06;var p=Need(pack,i.At);p.Virtual00.Write(GameReachedGoalHookPackTable.HookPack);
   i.At=0x0ebe0b;p.Owner08.Write(0);i.At=0x0ebe0e;p.Records10.Write(null);i.At=0x0ebe12;p.Count18.Write(count);i.At=0x0ebe15;p.Initialized1C.Write(0);
   i.At=0x0ebe41;if(count>11)throw new MissingContract(i.At,"OWN Hook allocation is bounded to observed Character count11; native allocator is not executed");
   var records=new GameReachedGoalHookRecords((int)count);
   for(uint n=0;n<count;n++){
    var r=records.At(n,0x0ebeb0);
    i.At=0x0ebeb2;r.LabelCapacity18.Write(15);i.At=0x0ebeba;r.LabelLength10.Write(0);i.At=0x0ebebe;r.LabelFirst00.Write(0);
    i.At=0x0ebec0;r.FunctionLength30.Write(0);i.At=0x0ebec4;r.FunctionCapacity38.Write(15);i.At=0x0ebecc;r.FunctionFirst20.Write(0);
    i.At=0x0ebecf;r.Lua40.Write(null);i.At=0x0ebed3;r.Word48.Write(0);i.At=0x0ebed7;r.Word50.Write(0);i.At=0x0ebedb;r.Word58.Write(0);
    i.At=0x0ebedf;r.Type60.Write(0xfffffffeu);i.At=0x0ebee6;r.Name68.Write(null);i.At=0x0ebeea;r.Word70.Write(0);i.At=0x0ebeee;r.Word78.Write(0);
   }
   i.At=0x0ebe8b;p.Records10.Write(records);i.At=0x0ebea4;return records;
  }
  // Actual Engine0EBAE0, r1-engine-hook-initialize-run1. Only the observed
  // no-Lua-call arms are closed: null Lua state, zero count, or every visited
  // function length equal to zero. All three still require the real Engine
  // and service pointer dereferences. No global is initialized by this method.
  public static void Initialize0EBAE0(GameReachedGoalHookPackState pack,GameReachedGoalEngineState state,Invocation invocation){
   var i=Need(invocation,0x0ebb0e);i.At=0x0ebb0e;var engineBits=Need(state,i.At).CaptureEngine421428();
   i.At=0x0ebb15;var serviceBits=Need(engineBits.Read(i.At),i.At).LuaService68.Snapshot();
   i.At=0x0ebb19;var luaBits=Need(serviceBits.Read(i.At),i.At).Lua00.Snapshot();
   i.At=0x0ebb1c;object lua=luaBits.Read(i.At);
   // OWN lifetime guard: a released wrapper is not a live native Lua pointer.
   var ownLua=lua as OwnLuaJit204State;if(ownLua!=null)ownLua.RequireLive(i.At);
   if(lua==null){i.At=0x0ebdbb;Need(pack,i.At).Initialized1C.Write(1);i.At=0x0ebddf;return;}
   i.At=0x0ebb2a;var p=Need(pack,i.At);
   if(p.Count18.Read(i.At)==0){i.At=0x0ebdbb;p.Initialized1C.Write(1);i.At=0x0ebddf;return;}
   uint index=0;
   do{
    i.At=0x0ebb47;var recordsBits=p.Records10.Snapshot();
    i.At=0x0ebb4b;var r=Record(recordsBits,index,i.At);
    if(r.FunctionLength30.Read(i.At)!=0){
     i.At=0x0ebb5a;ulong capacity=r.FunctionCapacity38.Read(i.At);
     if(capacity>=16){i.At=0x0ebb61;r.FunctionHeap20.Snapshot();}else{i.At=0x0ebb66;}
     // R8 is an opaque captured heap pointer or this exact inline string.
     // Resolving/dereferencing it belongs to the still unimplemented callee.
     i.At=0x0ebb70;throw new MissingContract(i.At,"Hook name requires actual2577F0 Lua lookup; no return object or logging effects invented");
    }
    i.At=0x0ebda7;index=unchecked(index+1);i.At=0x0ebdaa;
   }while(index<p.Count18.Read(i.At));
   i.At=0x0ebdb4;p.Initialized1C.Write(1);i.At=0x0ebddf;
  }
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
   i.At=0x0ec0a0;var engineBits=Need(state,i.At).CaptureEngine421428();
   i.At=0x0ec0ac;var recordsBits=pack.Records10.Snapshot();
   i.At=0x0ec0b0;var engine=Need(engineBits.Read(i.At),i.At);var table=engine.Virtual00.Snapshot();
   i.At=0x0ec0b3;Record(recordsBits,index,i.At).Name68.Snapshot();
   i.At=0x0ec0b8;table.Read(i.At);throw new MissingContract(i.At,"Engine failure-log virtual slot18 target remains unbound");
  }
  public static void ExecuteEventHook0EC010(GameReachedGoalHookPackState pack,uint eventIndex,GameReachedGoalEngineState state,Invocation invocation){
   var i=Need(invocation,0x0ec01f);i.At=0x0ec01f;
   if(Need(pack,i.At).Initialized1C.Read(i.At)==0){i.At=0x0ec02a;Initialize0EBAE0(pack,state,i);}
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
