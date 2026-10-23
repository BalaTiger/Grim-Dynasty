using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Six new Engine hot-helper scenarios only. No old Main, native program,
// automatic initialization, arrival completion or successful Lua callback.
public static class GameReachedGoalEngineTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action action,string why){try{action();}catch(MissingContract e){Eq(at,e.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> field){Missing(0,delegate{field.Read(0);},"Unknown remains Unknown");}
 static void Test(string name,Action action){try{action();scenarios++;}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 static GameReachedGoalHookPackState Pack(int count){var p=new GameReachedGoalHookPackState();p.Initialized1C.Write(1);p.Records10.Write(new GameReachedGoalHookRecords(count));return p;}
 public static void Run(){
  scenarios=assertions=0;
  Test("ObjectManager hot path returns the original object without lock pointee reads",delegate{
   var s=new GameReachedGoalEngineState();var c=new GameReachedGoalCriticalState();var manager=new object();s.Critical421398.Write(c);s.Instance4213A0.Write(manager);var i=new Invocation();
   Same(manager,GameReachedGoalEngineContracts.GetObjectManager01C040(s,i),"Exact explicitly published instance identity");Eq(0x01c103u,i.At,"Native hot return");Unknown(c.Native10);Same(c,s.Critical421398.Read(0),"Critical identity unchanged");Unknown(s.LuaCall4219C8);Unknown(s.Engine421428);
   s.Critical421398.Forget();Missing(0x01c056,delegate{GameReachedGoalEngineContracts.GetObjectManager01C040(s,i);},"Unknown critical consumed by TEST");s.Critical421398.Write(c);s.Instance4213A0.Forget();Missing(0x01c0ae,delegate{GameReachedGoalEngineContracts.GetObjectManager01C040(s,i);},"Unknown instance consumed by TEST");
  });
  Test("Cold singleton paths stop before allocation and lock effects",delegate{
   var s=new GameReachedGoalEngineState();s.Critical421398.Write(null);var i=new Invocation();Missing(0x01c05e,delegate{GameReachedGoalEngineContracts.GetObjectManager01C040(s,i);},"Cold critical allocator");Unknown(s.Instance4213A0);Eq(null,s.Critical421398.Read(0),"No fabricated critical allocation");
   var c=new GameReachedGoalCriticalState();s.Critical421398.Write(c);s.Instance4213A0.Write(null);Missing(0x01c0b7,delegate{GameReachedGoalEngineContracts.GetObjectManager01C040(s,i);},"Cold instance Enter after opaque actual Native10 MOV");Unknown(c.Native10);Eq(null,s.Instance4213A0.Read(0),"No fabricated manager");c.Native10.Write(new object());Missing(0x01c0b7,delegate{GameReachedGoalEngineContracts.GetObjectManager01C040(s,i);},"Known lock handle still does not fabricate Enter");
  });
  Test("Hook initialization and storage gates retain exact unsupported addresses",delegate{
   var p=new GameReachedGoalHookPackState();var i=new Invocation();Missing(0x0ec01f,delegate{GameReachedGoalEngineContracts.ExecuteEventHook0EC010(p,0,null,i);},"Unknown initialization byte");p.Initialized1C.Write(0);Missing(0x0ec02a,delegate{GameReachedGoalEngineContracts.ExecuteEventHook0EC010(p,0,null,i);},"Actual Initialize call boundary");Unknown(p.Records10);p.Initialized1C.Write(1);Missing(0x0ec03d,delegate{GameReachedGoalEngineContracts.ExecuteEventHook0EC010(p,0,null,i);},"Unknown record pointer delayed until dereference");p.Records10.Write(null);Missing(0x0ec03d,delegate{GameReachedGoalEngineContracts.ExecuteEventHook0EC010(p,0,null,i);},"Known-null records cannot imply empty hooks");p.Records10.Write(new GameReachedGoalHookRecords(0));Missing(0x0ec03d,delegate{GameReachedGoalEngineContracts.ExecuteEventHook0EC010(p,0,null,i);},"Explicit zero-length allocation still lacks record0");
  });
  Test("Known-null Lua uses exact selected stride80 record and skips unknown type",delegate{
   var p=Pack(2);var records=p.Records10.Read(0);var selected=records.At(1,0);selected.Lua40.Write(null);var i=new Invocation();GameReachedGoalEngineContracts.ExecuteEventHook0EC010(p,1,null,i);Eq(0x0ec0cau,i.At,"Null Lua returns normally");Unknown(selected.Type60);Unknown(selected.Name68);Unknown(records.At(0,0).Lua40);Same(selected,records.At(1,0),"Shared selected record identity");Eq(0x80u,GameReachedGoalHookRecords.Stride,"Observed stride");Missing(0x0ec03d,delegate{GameReachedGoalEngineContracts.ExecuteEventHook0EC010(p,UInt32.MaxValue,null,i);},"Zero-extended uint index does not wrap into valid record");
  });
  Test("Unsigned type plus2 rejects exactly minus2 and minus1",delegate{
   var p=Pack(1);var r=p.Records10.Read(0).At(0,0);var lua=new object();r.Lua40.Write(lua);var s=new GameReachedGoalEngineState();var i=new Invocation();
   foreach(uint t in new uint[]{0xfffffffeu,0xffffffffu}){r.Type60.Write(t);GameReachedGoalEngineContracts.ExecuteEventHook0EC010(p,0,s,i);Eq(0x0ec0cau,i.At,"Invalid Lua type returns normally");Same(lua,r.Lua40.Read(0),"Lua identity unchanged");Eq(t,r.Type60.Read(0),"Type unchanged");Unknown(s.LuaCall4219C8);Unknown(s.Engine421428);Unknown(r.Name68);}
   r.Type60.Forget();Missing(0x0ec047,delegate{GameReachedGoalEngineContracts.ExecuteEventHook0EC010(p,0,s,i);},"Unknown type consumed by ADD after MOV");
  });
  Test("Valid nonnull Lua stops at actual unbound function pointer after repeated gates",delegate{
   var p=Pack(1);var r=p.Records10.Read(0).At(0,0);var lua=new object();r.Lua40.Write(lua);var s=new GameReachedGoalEngineState();var i=new Invocation();
   foreach(uint t in new uint[]{0u,1u,0xfffffffdu}){r.Type60.Write(t);Missing(0x0ec06d,delegate{GameReachedGoalEngineContracts.ExecuteEventHook0EC010(p,0,s,i);},"Actual4219C8 call remains unbound");Eq(0x0ec06du,i.At,"Nested stop address");Same(lua,r.Lua40.Read(0),"No Lua callback side effects");Eq(t,r.Type60.Read(0),"No type mutation");Unknown(r.Name68);Unknown(s.Engine421428);}
   s.LuaCall4219C8.Write(new object());Missing(0x0ec06d,delegate{GameReachedGoalEngineContracts.ExecuteEventHook0EC010(p,0,s,i);},"Opaque function pointer is not executable implementation");Unknown(s.Instance4213A0);Unknown(s.Critical421398);
  });
 }
}
