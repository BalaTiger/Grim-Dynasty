using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Declared OWN component inputs only. These tests do not initialize gEngine,
// create Lua, execute native constructors/CFG, or establish gameplay startup.
public static class GameHookInitializationTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action action,string why){try{action();}catch(MissingContract e){Eq(at,e.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> field){Missing(0,delegate{field.Read(0);},"Unknown remains Unknown");}
 static void Test(string name,Action action){try{action();scenarios++;}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 static GameReachedGoalHookPackState Cold(uint count){var p=new GameReachedGoalHookPackState();GameReachedGoalEngineContracts.ConstructOwnColdPack0EBDE0(p,count,new Invocation());return p;}
 // Explicit component fixture, not a production service or inferred Lua state.
 static GameReachedGoalEngineState Engine(object lua){var s=new GameReachedGoalEngineState();var e=new GameReachedGoalLogEngine();var service=new GameReachedGoalLuaService();service.Lua00.Write(lua);e.LuaService68.Write(service);s.Engine421428.Write(e);return s;}
 static void Init(GameReachedGoalHookPackState p,GameReachedGoalEngineState s,Invocation i){GameReachedGoalEngineContracts.Initialize0EBAE0(p,s,i);}
 public static void Run(){
  scenarios=assertions=0;
  Test("OWN count11 constructor publishes exact typed stores and remains cold",delegate{
   var p=new GameReachedGoalHookPackState();var i=new Invocation();var records=GameReachedGoalEngineContracts.ConstructOwnColdPack0EBDE0(p,11,i);
   Same(records,p.Records10.Read(0),"Only completed OWN records published");Same(GameReachedGoalHookPackTable.HookPack,p.Virtual00.Read(0),"Observed table identity");Eq(0x339b70u,GameReachedGoalHookPackTable.HookPack.Rva,"Table RVA");Eq(0u,p.Owner08.Read(0),"DWORD owner");Eq(11u,p.Count18.Read(0),"DWORD count");Eq((byte)0,p.Initialized1C.Read(0),"BYTE initialized");Eq(11,records.Count,"Separate exact allocation extent");Eq(0x0ebea4u,i.At,"Constructor normal return");
   for(uint n=0;n<11;n++){var r=records.At(n,0);Eq(15UL,r.LabelCapacity18.Read(0),"First string capacity");Eq(0UL,r.LabelLength10.Read(0),"First string length");Eq((byte)0,r.LabelFirst00.Read(0),"First byte");Eq(0UL,r.FunctionLength30.Read(0),"Second string length");Eq(15UL,r.FunctionCapacity38.Read(0),"Second string capacity");Eq((byte)0,r.FunctionFirst20.Read(0),"Second first byte");Same(null,r.Lua40.Read(0),"Ctor Lua null");Eq(0UL,r.Word48.Read(0),"48 word");Eq(0UL,r.Word50.Read(0),"50 word");Eq(0UL,r.Word58.Read(0),"58 word");Eq(0xfffffffeu,r.Type60.Read(0),"DWORD invalid type");Same(null,r.Name68.Read(0),"68 pointer");Eq(0UL,r.Word70.Read(0),"70 word");Eq(0UL,r.Word78.Read(0),"78 word");Unknown(r.FunctionHeap20);}
  });
  Test("OWN zero count and excessive allocation preserve real header prefix",delegate{
   var p=Cold(0);Eq(0,p.Records10.Read(0).Count,"Zero-element OWN allocation");Eq((byte)0,p.Initialized1C.Read(0),"Not initialized by allocation");var i=new Invocation();Missing(0x0ebe41,delegate{GameReachedGoalEngineContracts.ConstructOwnColdPack0EBDE0(p,UInt32.MaxValue,i);},"Bounded managed allocation");Eq(UInt32.MaxValue,p.Count18.Read(0),"No count truncation");Same(null,p.Records10.Read(0),"No partial publication");Eq((byte)0,p.Initialized1C.Read(0),"Header prefix preserved");Eq(0u,p.Owner08.Read(0),"DWORD owner prefix");
  });
  Test("Unknown execute gate does not speculate constructor or globals",delegate{
   var p=new GameReachedGoalHookPackState();var s=new GameReachedGoalEngineState();Missing(0x0ec01f,delegate{GameReachedGoalEngineContracts.ExecuteEventHook0EC010(p,9,s,new Invocation());},"Unknown initialization byte");Unknown(p.Count18);Unknown(p.Records10);Unknown(s.Engine421428);
  });
  Test("Unknown or null Engine fails before zero-count fast path",delegate{
   var p=Cold(0);var s=new GameReachedGoalEngineState();var i=new Invocation();Missing(0x0ebb15,delegate{Init(p,s,i);},"Global consumed at first dereference");Eq((byte)0,p.Initialized1C.Read(0),"Unknown Engine not successful");s.Engine421428.Write(null);Missing(0x0ebb15,delegate{Init(p,s,i);},"Known-null Engine is invalid even at count0");Eq((byte)0,p.Initialized1C.Read(0),"Null Engine not successful");
  });
  Test("Unknown or null service pointer fails at its actual dereference",delegate{
   var p=Cold(11);var s=new GameReachedGoalEngineState();var e=new GameReachedGoalLogEngine();s.Engine421428.Write(e);Missing(0x0ebb19,delegate{Init(p,s,new Invocation());},"Unknown +68");e.LuaService68.Write(null);Missing(0x0ebb19,delegate{Init(p,s,new Invocation());},"Null +68 is not null Lua state");Eq((byte)0,p.Initialized1C.Read(0),"No early initialized write");Unknown(e.Virtual00);
  });
  Test("Unknown service first word fails only at Lua TEST",delegate{
   var p=Cold(11);var s=new GameReachedGoalEngineState();var e=new GameReachedGoalLogEngine();var service=new GameReachedGoalLuaService();e.LuaService68.Write(service);s.Engine421428.Write(e);Missing(0x0ebb1c,delegate{Init(p,s,new Invocation());},"Opaque MOV then TEST");Eq((byte)0,p.Initialized1C.Read(0),"Unknown Lua not success");Same(service,e.LuaService68.Read(0),"Service identity retained");
  });
  Test("Explicit null Lua branch skips unknown count and storage",delegate{
   var p=new GameReachedGoalHookPackState();p.Initialized1C.Write(0);var i=new Invocation();var s=Engine(null);Init(p,s,i);Eq((byte)1,p.Initialized1C.Read(0),"Actual EBDBB store");Eq(0x0ebddfu,i.At,"Actual initializer return");Unknown(p.Count18);Unknown(p.Records10);Unknown(s.Engine421428.Read(0).Virtual00);Unknown(s.LuaCall4219C8);
  });
  Test("Explicit live Lua with zero count skips unknown record pointer",delegate{
   var p=new GameReachedGoalHookPackState();p.Initialized1C.Write(0);p.Count18.Write(0);var lua=new object();var s=Engine(lua);Init(p,s,new Invocation());Eq((byte)1,p.Initialized1C.Read(0),"Zero-count normal store");Unknown(p.Records10);Same(lua,s.Engine421428.Read(0).LuaService68.Read(0).Lua00.Read(0),"Captured source identity unchanged");
  });
  Test("Non-null Lua consumes unknown count before records",delegate{
   var p=new GameReachedGoalHookPackState();p.Initialized1C.Write(0);Missing(0x0ebb2a,delegate{Init(p,Engine(new object()),new Invocation());},"Count comparison");Eq((byte)0,p.Initialized1C.Read(0),"No initialized prefix");Unknown(p.Records10);
  });
  Test("Record address requires known live allocation and actual extent",delegate{
   var p=new GameReachedGoalHookPackState();p.Initialized1C.Write(0);p.Count18.Write(1);var s=Engine(new object());Missing(0x0ebb4b,delegate{Init(p,s,new Invocation());},"Unknown records consumed at length dereference");p.Records10.Write(null);Missing(0x0ebb4b,delegate{Init(p,s,new Invocation());},"Null records");p.Records10.Write(new GameReachedGoalHookRecords(0));Missing(0x0ebb4b,delegate{Init(p,s,new Invocation());},"Count does not create allocation");p.Records10.Write(new GameReachedGoalHookRecords(1));p.Records10.Read(0).MarkReleased();Missing(0x0ebb4b,delegate{Init(p,s,new Invocation());},"Released records");Eq((byte)0,p.Initialized1C.Read(0),"No success on bad pointer");
  });
  Test("All eleven observed empty records initialize then execute event9",delegate{
   var p=Cold(11);var s=Engine(new object());var records=p.Records10.Read(0);var i=new Invocation();GameReachedGoalEngineContracts.ExecuteEventHook0EC010(p,9,s,i);Eq((byte)1,p.Initialized1C.Read(0),"Initializer actually ran");Eq(0x0ec0cau,i.At,"Actual selected event returned");Same(records,p.Records10.Read(0),"Records are not copied");for(uint n=0;n<11;n++){Same(null,records.At(n,0).Lua40.Read(0),"Ctor values preserved");Eq(0xfffffffeu,records.At(n,0).Type60.Read(0),"No lookup fabricated");}Unknown(s.LuaCall4219C8);Unknown(s.Instance4213A0);
  });
  Test("Unknown later record stops before final initialized publication",delegate{
   var p=Cold(11);var records=p.Records10.Read(0);records.At(7,0).FunctionLength30.Forget();Missing(0x0ebb4b,delegate{Init(p,Engine(new object()),new Invocation());},"Seventh-prefix completed but unknown eighth length");Eq((byte)0,p.Initialized1C.Read(0),"Flag not published early");Same(records,p.Records10.Read(0),"Storage still original");Eq(0UL,records.At(6,0).FunctionLength30.Read(0),"Earlier record unmodified");Unknown(records.At(7,0).FunctionLength30);Eq(0UL,records.At(8,0).FunctionLength30.Read(0),"Later record untouched");
  });
  Test("Nonempty name consumes exact string capacity before unresolved lookup",delegate{
   var p=Cold(11);var r=p.Records10.Read(0).At(4,0);r.FunctionLength30.Write(1);r.FunctionCapacity38.Forget();Missing(0x0ebb5a,delegate{Init(p,Engine(new object()),new Invocation());},"Unknown SSO capacity");Eq((byte)0,p.Initialized1C.Read(0),"Initialization not published");Same(null,r.Lua40.Read(0),"No lookup side effect");Eq(0xfffffffeu,r.Type60.Read(0),"No type side effect");
  });
  Test("Inline and heap names stop at actual lookup without fake resource return",delegate{
   var p=Cold(11);var r=p.Records10.Read(0).At(9,0);r.FunctionLength30.Write(2);var s=Engine(new object());foreach(ulong capacity in new ulong[]{15,16,UInt64.MaxValue}){r.FunctionCapacity38.Write(capacity);Missing(0x0ebb70,delegate{Init(p,s,new Invocation());},"2577F0 call stays unimplemented");Eq((byte)0,p.Initialized1C.Read(0),"No early flag");Same(null,r.Lua40.Read(0),"No fake Lua acquisition");Eq(0xfffffffeu,r.Type60.Read(0),"Type unchanged");Unknown(r.FunctionHeap20);}r.FunctionHeap20.Write(new object());Missing(0x0ebb70,delegate{Init(p,s,new Invocation());},"Known heap pointer still not implementation");Unknown(s.Instance4213A0);Unknown(s.LuaCall4219C8);
  });
  Test("Empty length skips unrelated unknown fields and preserves preexisting Lua",delegate{
   var p=Cold(1);var r=p.Records10.Read(0).At(0,0);r.FunctionCapacity38.Forget();r.FunctionFirst20.Forget();var lua=new object();r.Lua40.Write(lua);r.Type60.Write(7);Init(p,Engine(new object()),new Invocation());Eq((byte)1,p.Initialized1C.Read(0),"Only zero length needed");Same(lua,r.Lua40.Read(0),"Initializer does not clear existing Lua");Eq(7u,r.Type60.Read(0),"Type preserved");Unknown(r.FunctionCapacity38);Unknown(r.FunctionFirst20);var s=new GameReachedGoalEngineState();Missing(0x0ec06d,delegate{GameReachedGoalEngineContracts.ExecuteEventHook0EC010(p,0,s,new Invocation());},"Hot valid Lua still cannot execute");
  });
  Test("Hot event gate leaves all added cold fields unknown",delegate{
   var p=new GameReachedGoalHookPackState();p.Initialized1C.Write(3);var records=new GameReachedGoalHookRecords(1);p.Records10.Write(records);records.At(0,0).Lua40.Write(null);var s=new GameReachedGoalEngineState();var i=new Invocation();GameReachedGoalEngineContracts.ExecuteEventHook0EC010(p,0,s,i);Eq(0x0ec0cau,i.At,"Any nonzero BYTE bypasses initialization");Eq((byte)3,p.Initialized1C.Read(0),"No normalization to1");Unknown(p.Count18);Unknown(records.At(0,0).FunctionLength30);Unknown(s.Engine421428);
  });
  Test("Count is authoritative and cannot be clamped to available records",delegate{
   var p=Cold(2);p.Count18.Write(3);Missing(0x0ebb4b,delegate{Init(p,Engine(new object()),new Invocation());},"Third record beyond typed allocation");Eq(3u,p.Count18.Read(0),"No count fixup");Eq((byte)0,p.Initialized1C.Read(0),"No success after two records");Eq(2,p.Records10.Read(0).Count,"No implicit growth");
  });
  Test("Cold event initializes before validating requested record",delegate{
   var p=Cold(0);var s=Engine(new object());Missing(0x0ec03d,delegate{GameReachedGoalEngineContracts.ExecuteEventHook0EC010(p,9,s,new Invocation());},"Event9 remains out of zero-count allocation");Eq((byte)1,p.Initialized1C.Read(0),"Real initializer return is retained before later failure");Eq(0,p.Records10.Read(0).Count,"No default event allocation");
  });
 }
}
