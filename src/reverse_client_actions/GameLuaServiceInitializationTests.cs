using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.ReverseClient.Actions;

// Loads only the fingerprint-pinned OWN LuaJIT artifact. These checks establish
// its actual library/state effects, not completion of native service startup.
public static class GameLuaServiceInitializationTests {
 static int scenarios,assertions;
 static readonly List<Dictionary<string,object>> checks=new List<Dictionary<string,object>>();
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action action,string why){try{action();}catch(MissingContract error){Eq(at,error.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Invalid(Action action,string why){try{action();}catch(InvalidOperationException){Check(true,why);return;}throw new Exception("Expected invalid lifetime: "+why);}
 static void Unknown<T>(Field<T> field,string why){Missing(0,delegate{field.Read(0);},why);}
 static void Test(string name,Action action){int before=assertions;try{action();scenarios++;checks.Add(new Dictionary<string,object>{{"name",name},{"scenarios",1},{"assertions",assertions-before}});}catch(Exception error){throw new Exception(name+": "+error.Message,error);}}
 static GameLuaLibraryBindings NullBindings(){var b=new GameLuaLibraryBindings();b.Base421908.Write(null);b.Math421970.Write(null);b.String421938.Write(null);b.Table421950.Write(null);b.Jit4218E0.Write(null);return b;}
 static GameReachedGoalHookPackState ColdPack(uint count){var p=new GameReachedGoalHookPackState();GameReachedGoalEngineContracts.ConstructOwnColdPack0EBDE0(p,count,new Invocation());return p;}
 static OwnLuaJit204State State(OwnLuaJit204Runtime runtime){var state=runtime.NewState(0x0f2e28);Check(state!=null,"Actual allocation returned a non-null state");return state;}
 static void ClosePublished(GameReachedGoalLuaService service){
  object value;try{value=service.Lua00.Read(0);}catch(MissingContract){return;}
  var state=value as OwnLuaJit204State;if(state!=null)state.Dispose();
 }
 public static Dictionary<string,object> Run(){
  scenarios=assertions=0;checks.Clear();int olderAssertions=0;
  var runtime=OwnLuaJit204Runtime.OpenOwnProfile();
  try{
   Test("Fixed module and canonical exports",delegate{
    Eq(OwnLuaJit204Runtime.Library,runtime.LoadedPath,"Only the pinned OWN artifact is loaded");
    Eq(0L,runtime.ActiveStateCount,"No VM created during module binding");
    Eq(14,runtime.ExportRvas.Count,"Version marker, eight API exports and five library functions");
    foreach(var entry in runtime.ExportRvas)Check(entry.Value>0,"Export RVA is inside the verified owner: "+entry.Key);
    foreach(OwnLuaJit204Library kind in Enum.GetValues(typeof(OwnLuaJit204Library))){var function=runtime.GetLibraryFunction(kind,0);Eq(kind,function.Kind,"Resolved library kind");Same(function,runtime.GetLibraryFunction(kind,0),"Canonical exported identity");}
    Missing(0x25a036,delegate{runtime.GetLibraryFunction((OwnLuaJit204Library)99,0x25a036);},"Unobserved library kind is not inferred");
    Missing(0x25a036,delegate{OwnLuaJit204Runtime.RequireFingerprint(new String('0',64),0x25a036);},"Mismatched fingerprint is rejected without loading another DLL");
   });
   Test("Actual five-library effects and balanced stack",delegate{
    using(var lua=State(runtime)){
     var service=new GameReachedGoalLuaService();service.Lua00.Write(lua);service.Initialized58.Write(0x7f);
     var bindings=GameLuaLibraryBindings.ResolveOwn(runtime);var invocation=new Invocation();
     var math=runtime.GetLibraryFunction(OwnLuaJit204Library.Math,0);
     Eq(0,lua.GetTop(0),"Fresh actual stack");Eq(0,runtime.ReadLibraryGlobalType(lua,math,0),"Math is nil before the observed opener");
     long pushes=runtime.NativePushCClosureCalls,calls=runtime.NativePCallZeroCalls;
     Eq((byte)1,GameLuaServiceInitializationContracts.OpenCore25A030(service,bindings,runtime,invocation),"Observed normal AL return");
     Eq(0x25a142u,invocation.At,"Normal return site");Eq(5L,runtime.NativePushCClosureCalls-pushes,"Five actual closure pushes");Eq(5L,runtime.NativePCallZeroCalls-calls,"Five actual protected calls");
     foreach(OwnLuaJit204Library kind in Enum.GetValues(typeof(OwnLuaJit204Library))){Eq(5,runtime.ReadLibraryGlobalType(lua,runtime.GetLibraryFunction(kind,0),0),"Library global is an actual Lua table: "+kind);Eq(0,lua.GetTop(0),"Global observation restores exact stack: "+kind);}
     Eq((byte)0x7f,service.Initialized58.Read(0),"Core helper never publishes ready");Same(lua,service.Lua00.Read(0),"Service retains actual VM identity");Unknown(service.Options08,"Core helper does not initialize options");
    }
   });
   Test("Core leaves unknown ready state unknown",delegate{
    using(var lua=State(runtime)){
     var service=new GameReachedGoalLuaService();service.Lua00.Write(lua);
     Eq((byte)1,GameLuaServiceInitializationContracts.OpenCore25A030(service,GameLuaLibraryBindings.ResolveOwn(runtime),runtime,new Invocation()),"Core returned normally");
     Unknown(service.Initialized58,"No synthesized ready byte");Unknown(service.Options08,"No synthesized options word");Eq(0,lua.GetTop(0),"Protected openers discard their results");
    }
   });
   Test("Same WorldEngine service reaches exact initialization boundary",delegate{
    var move=new MoveContext();var world=new WorldEngine();move.EngineGlobal.Write(world);
    var engineState=new GameReachedGoalEngineState();engineState.BorrowEngineGlobal(move.EngineGlobal);
    var captured=engineState.CaptureEngine421428();var publish=new Invocation();
    var service=GameLuaServiceInitializationContracts.PublishOwnColdService(world,publish);
    try{
     Eq(0x074c3fu,publish.At,"Cold profile publishes last");Same(world,captured.Read(0).WorldIdentity,"Same borrowed engine identity");Same(service,captured.Read(0).LuaService68.Read(0),"Captured engine sees the published service");
     Same(null,service.Lua00.Read(0),"Cold profile has explicitly known-null Lua");Eq((ushort)0x0101,service.Options08.Read(0),"Observed cold options width/value");Eq((byte)0,service.Initialized58.Read(0),"Cold ready remains zero");
     service.Options08.Write(0xbbaa);var invocation=new Invocation();long creations=runtime.NativeNewStateCalls;
     Missing(0x0f2e5a,delegate{GameLuaServiceInitializationContracts.InitializePrefix0F2E00(service,GameLuaLibraryBindings.ResolveOwn(runtime),runtime,invocation);},"Unknown callback publication is not replaced with success");
     Eq(0x0f2e5au,invocation.At,"Precise continuation boundary");Eq(1L,runtime.NativeNewStateCalls-creations,"Initializer made one actual VM");
     var lua=service.Lua00.Read(0) as OwnLuaJit204State;Check(lua!=null&&lua.IsAlive,"Failed prefix preserves the real live VM");
     Same(service,captured.Read(0).LuaService68.Read(0),"No cloned or second service");Same(service,GameReachedGoalLogEngine.ForWorldEngine(world).LuaService68.Read(0),"Canonical service remains authoritative");
     Eq((ushort)0x0101,service.Options08.Read(0),"Actual prefix overwrote one options word");Eq((byte)0,service.Initialized58.Read(0),"No native-ready claim at partial return");Eq(0,lua.GetTop(0),"Successful core leaves an empty stack");
     Eq(5,runtime.ReadLibraryGlobalType(lua,runtime.GetLibraryFunction(OwnLuaJit204Library.Jit,0),0),"Jit table exists in this same VM");Unknown(world.World50,"Lua publication does not initialize unrelated world data");
    }finally{ClosePublished(service);}
    var closed=service.Lua00.Read(0) as OwnLuaJit204State;Check(closed!=null&&!closed.IsAlive,"OWN teardown invalidates identity without rewriting native service storage");
   });
   Test("Known-null slots skip all service and runtime dereferences",delegate{
    var service=new GameReachedGoalLuaService();var bindings=NullBindings();var invocation=new Invocation();
    long calls=runtime.NativePCallZeroCalls,pushes=runtime.NativePushCClosureCalls;
    Eq((byte)1,GameLuaServiceInitializationContracts.OpenCore25A030(service,bindings,null,invocation),"No functions means no VM dereference");
    Eq(0x25a142u,invocation.At,"Skip path reaches actual AL return");Unknown(service.Lua00,"Unknown Lua is not consumed or filled");Unknown(service.Initialized58,"Unknown ready byte stays unknown");Unknown(service.Options08,"Unknown options remain unknown");
    Eq((byte)1,GameLuaServiceInitializationContracts.OpenCore25A030(null,bindings,null,new Invocation()),"All-null path does not dereference a null service");
    Eq(calls,runtime.NativePCallZeroCalls,"No native call");Eq(pushes,runtime.NativePushCClosureCalls,"No native push");
   });
   Test("Unknown binding preserves completed library prefix",delegate{
    using(var lua=State(runtime)){
     var service=new GameReachedGoalLuaService();service.Lua00.Write(lua);var bindings=new GameLuaLibraryBindings();var invocation=new Invocation();
     long calls=runtime.NativePCallZeroCalls;
     Missing(0x25a036,delegate{GameLuaServiceInitializationContracts.OpenCore25A030(service,bindings,runtime,invocation);},"Unknown first slot stops before any call");Eq(calls,runtime.NativePCallZeroCalls,"Unknown first binding performs no native call");
     bindings.Base421908.Write(runtime.GetLibraryFunction(OwnLuaJit204Library.Base,0));
     Missing(0x25a06e,delegate{GameLuaServiceInitializationContracts.OpenCore25A030(service,bindings,runtime,invocation);},"Unknown second slot retains base effect");
     Eq(calls+1,runtime.NativePCallZeroCalls,"Exactly base was invoked");Eq(5,runtime.ReadLibraryGlobalType(lua,runtime.GetLibraryFunction(OwnLuaJit204Library.Base,0),0),"Base table persists after stop");Eq(0,runtime.ReadLibraryGlobalType(lua,runtime.GetLibraryFunction(OwnLuaJit204Library.Math,0),0),"Math not silently opened");
     Eq(0,lua.GetTop(0),"Successful prefix stack retained");Same(lua,service.Lua00.Read(0),"Failure leaves same live identity");Unknown(service.Initialized58,"No ready write on binding failure");
    }
   });
   Test("Unknown nonnative null and released Lua fail before native push",delegate{
    var service=new GameReachedGoalLuaService();var bindings=GameLuaLibraryBindings.ResolveOwn(runtime);long pushes=runtime.NativePushCClosureCalls;
    Missing(0x25a04b,delegate{GameLuaServiceInitializationContracts.OpenCore25A030(service,bindings,runtime,new Invocation());},"Unknown Lua cannot become null");
    service.Lua00.Write(new object());Missing(0x25a04b,delegate{GameLuaServiceInitializationContracts.OpenCore25A030(service,bindings,runtime,new Invocation());},"Fixture object cannot masquerade as VM");
    service.Lua00.Write(null);Missing(0x25a04b,delegate{GameLuaServiceInitializationContracts.OpenCore25A030(service,bindings,runtime,new Invocation());},"Known-null VM cannot be invoked");
    var lua=State(runtime);try{service.Lua00.Write(lua);lua.Dispose();Missing(0x25a04b,delegate{GameLuaServiceInitializationContracts.OpenCore25A030(service,bindings,runtime,new Invocation());},"Released state cannot be invoked");Same(lua,service.Lua00.Read(0),"Release did not replace service pointer with synthetic null");}finally{lua.Dispose();}
    Eq(pushes,runtime.NativePushCClosureCalls,"All unsupported inputs stop before pushcclosure");
   });
   Test("Live Hook component borrows VM and release invalidates its consumer",delegate{
    var lua=State(runtime);long closes=runtime.NativeCloseCalls;
    try{
     Eq(1L,runtime.ActiveStateCount,"Exactly this state remains live");Invalid(delegate{runtime.Dispose();},"Cannot unload code while state owns it");Check(!runtime.IsDisposed&&lua.IsAlive,"Rejected unload preserves usable state");Eq(0,lua.GetTop(0),"State remains callable after rejected unload");
     var world=new WorldEngine();var state=new GameReachedGoalEngineState();var global=Field<WorldEngine>.Of(world);state.BorrowEngineGlobal(global);
     var service=new GameReachedGoalLuaService();service.Lua00.Write(lua);GameReachedGoalLogEngine.ForWorldEngine(world).LuaService68.Write(service);
     long calls=runtime.NativePCallZeroCalls,pushes=runtime.NativePushCClosureCalls;
     foreach(uint count in new uint[]{0,11}){var pack=ColdPack(count);var invocation=new Invocation();GameReachedGoalEngineContracts.Initialize0EBAE0(pack,state,invocation);Eq(0x0ebddfu,invocation.At,"Supported no-function Hook component returns");Eq((byte)1,pack.Initialized1C.Read(0),"Only Hook pack initialization is complete");Same(lua,service.Lua00.Read(0),"Hook consumes the same VM identity");}
     Unknown(service.Initialized58,"Hook result does not imply service startup ready");Eq(calls,runtime.NativePCallZeroCalls,"No-function Hook arm does not fake a Lua call");Eq(pushes,runtime.NativePushCClosureCalls,"No-function Hook arm pushes nothing");
     lua.Dispose();Eq(closes+1,runtime.NativeCloseCalls,"Native close once");Eq(0L,runtime.ActiveStateCount,"State ownership removed");Check(!lua.IsAlive,"Wrapper observes release");
     Missing(0x0ebb1c,delegate{lua.RequireLive(0x0ebb1c);},"Released state remains invalid at consumer boundary");
     var releasedPack=ColdPack(0);Missing(0x0ebb1c,delegate{GameReachedGoalEngineContracts.Initialize0EBAE0(releasedPack,state,new Invocation());},"Released OWN pointer cannot pass even zero-count Hook arm");Eq((byte)0,releasedPack.Initialized1C.Read(0),"Rejected consumed state leaves Hook uninitialized");
     lua.Dispose();Eq(closes+1,runtime.NativeCloseCalls,"Repeated OWN Dispose does not call native close twice");
    }finally{lua.Dispose();}
   });
   int beforeLegacy=assertions;
   Test("Legacy opaque and known-null Hook component arms remain unchanged",delegate{
    var world=new WorldEngine();var state=new GameReachedGoalEngineState();state.BorrowEngineGlobal(Field<WorldEngine>.Of(world));var service=new GameReachedGoalLuaService();GameReachedGoalLogEngine.ForWorldEngine(world).LuaService68.Write(service);
    long calls=runtime.NativePCallZeroCalls,pushes=runtime.NativePushCClosureCalls;
    foreach(object value in new object[]{null,new object()}){service.Lua00.Write(value);var pack=ColdPack(value==null?11u:0u);var invocation=new Invocation();GameReachedGoalEngineContracts.Initialize0EBAE0(pack,state,invocation);Eq(0x0ebddfu,invocation.At,"Previously supported legacy branch returns");Eq((byte)1,pack.Initialized1C.Read(0),"Legacy Hook flag retained");Same(value,service.Lua00.Read(0),"Opaque legacy identity not converted to OWN VM");}
    Unknown(service.Initialized58,"Legacy Hook never marks service ready");Eq(calls,runtime.NativePCallZeroCalls,"Legacy regression does not execute unobserved Lua");Eq(pushes,runtime.NativePushCClosureCalls,"Legacy regression has no actual closure push");
   });
   olderAssertions=assertions-beforeLegacy;
   Test("Module release is observed and rejects subsequent calls",delegate{
    Eq(0L,runtime.ActiveStateCount,"Every OWN test state has closed");Same(null,runtime.CloseFailure,"No native close failure");runtime.Dispose();Check(runtime.IsDisposed,"Runtime disposed");Check(runtime.ModuleUnloadObserved,"Module absence verified after reference release");
    Missing(0x0f2e28,delegate{runtime.NewState(0x0f2e28);},"Disposed runtime cannot create a state");runtime.Dispose();Check(runtime.ModuleUnloadObserved,"Repeated Dispose is harmless");
   });
   return new Dictionary<string,object>{
    {"status","passed-own-lua-service-prefix"},{"failure",null},{"scenarios",scenarios},{"new_scenarios",scenarios-1},{"selected_older_regressions",1},{"assertions",assertions},{"new_assertions",assertions-olderAssertions},{"selected_older_assertions",olderAssertions},{"old_whole_suites_run",0},{"targeted_checks",checks},
    {"expected_unsupported",0x0f2e5a},{"initialized_service",false},{"goal_tail_returned",false},{"stopped_frames_verified",false},{"original_executed",false},{"native_game_abi_verified",false},{"gameplay_accepted",false},{"module_unload_observed",runtime.ModuleUnloadObserved},{"active_states",runtime.ActiveStateCount},
    {"loaded_path",runtime.LoadedPath},{"dll_sha256",OwnLuaJit204Runtime.VerifiedSha256},{"export_rvas",runtime.ExportRvas},
    {"own_runtime_counters",new Dictionary<string,object>{{"newstate",runtime.NativeNewStateCalls},{"gettop",runtime.NativeGetTopCalls},{"close",runtime.NativeCloseCalls},{"pushcclosure",runtime.NativePushCClosureCalls},{"pcall_zero",runtime.NativePCallZeroCalls},{"global_type_observations",runtime.LibraryGlobalTypeReads}}}
   };
  }finally{runtime.Dispose();}
 }
}
public static class OwnLuaServiceValidationProgram {
 public static int Main(string[] args){
  try{if(args.Length!=0)throw new ArgumentException("Fixed test scope; no arguments");Console.WriteLine(new JavaScriptSerializer().Serialize(GameLuaServiceInitializationTests.Run()));return 0;}
  catch(Exception error){Console.WriteLine(new JavaScriptSerializer().Serialize(new Dictionary<string,object>{{"status","failed"},{"failure",error.ToString()},{"initialized_service",false},{"goal_tail_returned",false},{"stopped_frames_verified",false},{"gameplay_accepted",false}}));return 1;}
 }
}
