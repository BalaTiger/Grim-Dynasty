using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Shared OWN identity/global-cell integration only. Explicit test services are
// not native Engine initialization, Lua execution or a gameplay acceptance.
public static class GameGoalEngineSharingTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Different(object expected,object actual,string why){Check(!Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action action,string why){try{action();}catch(MissingContract e){Eq(at,e.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Invalid(Action action,string why){try{action();}catch(InvalidOperationException){assertions++;return;}throw new Exception("Expected invalid binding: "+why);}
 static void NullArgument(Action action,string why){try{action();}catch(ArgumentNullException){assertions++;return;}throw new Exception("Expected null argument: "+why);}
 static void Unknown<T>(Field<T> value){Missing(0,delegate{value.Read(0);},"Unknown remains Unknown");}
 static void Test(string name,Action action){try{action();scenarios++;}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 static GameReachedGoalHookPackState Cold(uint count){var p=new GameReachedGoalHookPackState();GameReachedGoalEngineContracts.ConstructOwnColdPack0EBDE0(p,count,new Invocation());return p;}
 static GameMovementIdentityBridge Bridge(MoveContext move){
  var character=new AddressSymbol<ClassRecord>(0xa5cba0);character.Write(new ClassRecord());var player=new AddressSymbol<ClassRecord>(0xa5cab0);player.Write(new ClassRecord());
  var callbacks=new GameMovementCallbackContext(new WorldCoordinateBoundaries(new WorldCoordinateState()),new EntityMovementRegistry(),character,player);move.CharacterClass.Write(character.Read(0));return new GameMovementIdentityBridge(callbacks);
 }
 static GameReachedGoalContext Context(MoveContext move,GameReachedGoalEngineState state){return new GameReachedGoalContext(Bridge(move),state,move);}
 public static void Run(){
  scenarios=assertions=0;
  Test("Canonical sidecar borrows one Engine and leaves service unknown",delegate{
   var a=new WorldEngine();var b=new WorldEngine();var av=GameReachedGoalLogEngine.ForWorldEngine(a);Same(av,GameReachedGoalLogEngine.ForWorldEngine(a),"Same object always same sidecar");Same(a,av.WorldIdentity,"Borrow original identity");var bv=GameReachedGoalLogEngine.ForWorldEngine(b);Different(av,bv,"Different Engine has different fields");Same(b,bv.WorldIdentity,"Second original identity");Unknown(a.World50);Unknown(b.World50);Unknown(av.LuaService68);Unknown(av.Virtual00);Unknown(bv.LuaService68);NullArgument(delegate{GameReachedGoalLogEngine.ForWorldEngine(null);},"Null Engine cannot create sidecar");
  });
  Test("Goal context automatically borrows Move global without reading it",delegate{
   var move=new MoveContext();var state=new GameReachedGoalEngineState();var context=Context(move,state);Same(move,context.Move,"Original Move context");Same(state,context.Engine,"Original helper state");Same(move.EngineGlobal,state.BorrowedEngineGlobal,"Automatic authoritative-cell binding");Unknown(move.EngineGlobal);Unknown(state.Engine421428);var capture=state.CaptureEngine421428();Missing(0x0ebb15,delegate{capture.Read(0x0ebb15);},"Unknown captured bits consume only at dereference");Unknown(state.Engine421428);Unknown(state.Critical421398);
  });
  Test("Two actual Goal contexts share Engine fields across one Move global",delegate{
   var move=new MoveContext();var bridge=Bridge(move);var left=new GameReachedGoalEngineState();var right=new GameReachedGoalEngineState();var lc=new GameReachedGoalContext(bridge,left,move);var rc=new GameReachedGoalContext(bridge,right,move);var world=new WorldEngine();move.EngineGlobal.Write(world);
   var lv=left.CaptureEngine421428().Read(0x0ebb15);var rv=right.CaptureEngine421428().Read(0x0ebb15);Same(lv,rv,"Canonical sidecar across helper states");Same(lv.LuaService68,rv.LuaService68,"One actual service cell");Same(lc.Move.EngineGlobal,rc.Engine.BorrowedEngineGlobal,"Contexts use same global cell");var service=new GameReachedGoalLuaService();lv.LuaService68.Write(service);Same(service,rv.LuaService68.Read(0),"Mutation visible without sync/copy");Unknown(service.Lua00);Unknown(left.Engine421428);Unknown(right.Engine421428);
  });
  Test("Production initializer follows unknown global service and Lua boundaries",delegate{
   var move=new MoveContext();var state=new GameReachedGoalEngineState();Context(move,state);var p=Cold(11);var i=new Invocation();Missing(0x0ebb15,delegate{GameReachedGoalEngineContracts.Initialize0EBAE0(p,state,i);},"Unknown shared global");Eq((byte)0,p.Initialized1C.Read(0),"Failed global leaves cold flag");var world=new WorldEngine();move.EngineGlobal.Write(world);var sidecar=GameReachedGoalLogEngine.ForWorldEngine(world);Missing(0x0ebb19,delegate{GameReachedGoalEngineContracts.Initialize0EBAE0(p,state,i);},"Known Engine does not infer +68");Eq((byte)0,p.Initialized1C.Read(0),"No service fallback");var service=new GameReachedGoalLuaService();sidecar.LuaService68.Write(service);Missing(0x0ebb1c,delegate{GameReachedGoalEngineContracts.Initialize0EBAE0(p,state,i);},"Known service still needs first word");Eq((byte)0,p.Initialized1C.Read(0),"No Lua fallback");Unknown(sidecar.Virtual00);Unknown(service.Lua00);Unknown(state.Engine421428);
  });
  Test("Fresh captures follow global replacement and never reuse old service",delegate{
   var global=new Field<WorldEngine>();var state=new GameReachedGoalEngineState();state.BorrowEngineGlobal(global);var a=new WorldEngine();var b=new WorldEngine();global.Write(a);var av=state.CaptureEngine421428().Read(0);var service=new GameReachedGoalLuaService();service.Lua00.Write(null);av.LuaService68.Write(service);var first=Cold(11);GameReachedGoalEngineContracts.Initialize0EBAE0(first,state,new Invocation());Eq((byte)1,first.Initialized1C.Read(0),"Explicit first service branch returns");global.Write(b);var bv=state.CaptureEngine421428().Read(0);Different(av,bv,"Replacement Engine selects distinct view");var second=Cold(11);Missing(0x0ebb19,delegate{GameReachedGoalEngineContracts.Initialize0EBAE0(second,state,new Invocation());},"Second Engine does not inherit first service");Eq((byte)0,second.Initialized1C.Read(0),"No copied success");Unknown(bv.LuaService68);global.Write(a);Same(av,state.CaptureEngine421428().Read(0),"Returning identity retains its own cells");Same(service,av.LuaService68.Read(0),"Original service retained");
  });
  Test("Captured global identity survives replacement while its fields stay live",delegate{
   var global=new Field<WorldEngine>();var state=new GameReachedGoalEngineState();state.BorrowEngineGlobal(global);var a=new WorldEngine();var b=new WorldEngine();global.Write(a);var captured=state.CaptureEngine421428();global.Write(b);var old=captured.Read(0x0ebb15);Same(a,old.WorldIdentity,"Native MOV captured old identity");Same(b,state.CaptureEngine421428().Read(0x0ebb15).WorldIdentity,"Later MOV captures replacement");var service=new GameReachedGoalLuaService();GameReachedGoalLogEngine.ForWorldEngine(a).LuaService68.Write(service);Same(service,captured.Read(0x0ebb15).LuaService68.Read(0),"Snapshot does not deep-copy later pointee fields");Unknown(GameReachedGoalLogEngine.ForWorldEngine(b).LuaService68);
  });
  Test("Unknown and null captured pointer states cannot be filled retroactively",delegate{
   var global=new Field<WorldEngine>();var state=new GameReachedGoalEngineState();state.BorrowEngineGlobal(global);var unknown=state.CaptureEngine421428();global.Write(new WorldEngine());Missing(0x0ec0b0,delegate{unknown.Read(0x0ec0b0);},"Unknown captured at logging MOV stays unknown");global.Write(null);var nil=state.CaptureEngine421428();global.Write(new WorldEngine());Same(null,nil.Read(0x0ec0b0),"Captured known-null stays null");Check(state.CaptureEngine421428().Read(0)!=null,"Fresh read sees published Engine");global.Forget();Missing(0x0ebb15,delegate{state.CaptureEngine421428().Read(0x0ebb15);},"Later Forget observed by new read");
  });
  Test("Binding is idempotent for the same cell and refuses alternate cells",delegate{
   var state=new GameReachedGoalEngineState();var global=new Field<WorldEngine>();state.BorrowEngineGlobal(global);state.BorrowEngineGlobal(global);Same(global,state.BorrowedEngineGlobal,"Same binding stable");Invalid(delegate{state.BorrowEngineGlobal(new Field<WorldEngine>());},"Cannot rebind even another Unknown field");Same(global,state.BorrowedEngineGlobal,"Failed rebind leaves authoritative cell");NullArgument(delegate{state.BorrowEngineGlobal(null);},"Null is not a global cell");Unknown(global);Unknown(state.Engine421428);
  });
  Test("Known component values conflict with initial shared binding",delegate{
   foreach(bool knownNull in new bool[]{false,true}){var state=new GameReachedGoalEngineState();var component=knownNull?null:new GameReachedGoalLogEngine();state.Engine421428.Write(component);var global=new Field<WorldEngine>();Invalid(delegate{state.BorrowEngineGlobal(global);},"Known component including null cannot become a second source");Same(null,state.BorrowedEngineGlobal,"Rejected binding is not published");Same(component,state.CaptureEngine421428().Read(0),"Component path retained after rejection");Unknown(global);}
   var move=new MoveContext();var s=new GameReachedGoalEngineState();s.Engine421428.Write(new GameReachedGoalLogEngine());Invalid(delegate{Context(move,s);},"Production context does not silently mask existing component value");Same(null,s.BorrowedEngineGlobal,"Rejected context leaves binding untouched");Unknown(move.EngineGlobal);
  });
  Test("Legacy shadow writes after binding cannot override authoritative global",delegate{
   var global=new Field<WorldEngine>();var state=new GameReachedGoalEngineState();state.BorrowEngineGlobal(global);var shadow=new GameReachedGoalLogEngine();var service=new GameReachedGoalLuaService();service.Lua00.Write(null);shadow.LuaService68.Write(service);state.Engine421428.Write(shadow);var p=Cold(0);Missing(0x0ebb15,delegate{GameReachedGoalEngineContracts.Initialize0EBAE0(p,state,new Invocation());},"Known shadow cannot fill unknown real global");global.Write(new WorldEngine());Missing(0x0ebb19,delegate{GameReachedGoalEngineContracts.Initialize0EBAE0(p,state,new Invocation());},"Shadow service cannot fill real +68");Eq((byte)0,p.Initialized1C.Read(0),"No fabricated initialization");state.BorrowEngineGlobal(global);Same(global,state.BorrowedEngineGlobal,"Idempotent bind does not adopt shadow");Different(shadow,state.CaptureEngine421428().Read(0),"Only canonical real Engine view selected");
  });
  Test("Hot event bypass does not consume shared global or cold service",delegate{
   var move=new MoveContext();var state=new GameReachedGoalEngineState();Context(move,state);var p=new GameReachedGoalHookPackState();p.Initialized1C.Write(7);p.Records10.Write(new GameReachedGoalHookRecords(1));p.Records10.Read(0).At(0,0).Lua40.Write(null);var i=new Invocation();GameReachedGoalEngineContracts.ExecuteEventHook0EC010(p,0,state,i);Eq(0x0ec0cau,i.At,"Unchanged hot return");Eq((byte)7,p.Initialized1C.Read(0),"Nonzero flag not rewritten");Unknown(move.EngineGlobal);Unknown(state.Engine421428);Unknown(p.Count18);Unknown(p.Records10.Read(0).At(0,0).FunctionLength30);
  });
  Test("Independent legacy component snapshots retain old supported interface",delegate{
   var state=new GameReachedGoalEngineState();var first=new GameReachedGoalLogEngine();var second=new GameReachedGoalLogEngine();state.Engine421428.Write(first);var captured=state.CaptureEngine421428();state.Engine421428.Write(second);Same(first,captured.Read(0x0ec0b0),"Legacy captured identity stable");Same(second,state.CaptureEngine421428().Read(0),"Fresh legacy capture sees change");Same(null,first.WorldIdentity,"Independent component does not invent WorldEngine");Same(null,state.BorrowedEngineGlobal,"No implicit shared binding outside context");var service=new GameReachedGoalLuaService();service.Lua00.Write(null);second.LuaService68.Write(service);var p=Cold(11);GameReachedGoalEngineContracts.ExecuteEventHook0EC010(p,9,state,new Invocation());Eq((byte)1,p.Initialized1C.Read(0),"Legacy explicit component cold arm remains supported");
  });
 }
}
