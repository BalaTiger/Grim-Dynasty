using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using GrimDynasty.ReverseClient.Actions;

// Real, pinned OWN LuaJIT and native bridge only. Logger captures below are
// explicit OWN platform adapters, not recovered Engine::Log implementations.
// Native protected-frame cleanup is not asserted to reproduce original EH.
public static class GameLuaCallbackPublicationTests {
 static int scenarios,assertions,olderAssertions;
 static readonly List<Dictionary<string,object>> checks=new List<Dictionary<string,object>>();
 static readonly List<object> receipts=new List<object>();
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Remember(OwnLuaCallbackResult r){
  // Keep the actual host Exception out of JSON. Its ToString/properties are not
  // trusted, including the deliberately broken diagnostic in the eighth case.
  receipts.Add(new Dictionary<string,object>{{"bridge_status",r.BridgeStatus},{"lua_status",r.LuaStatus},{"stage_rva",r.StageRva},{"published_mask",r.PublishedMask},{"callback_calls",r.CallbackCalls},{"logger_calls",r.LoggerCalls},{"resolver_calls",r.ResolverCalls},{"entry_top",r.EntryTop},{"exit_top",r.ExitTop},{"failure_local_top",r.FailureLocalTop},{"host_status",r.HostStatus},{"flags",r.Flags},{"fixture_id",r.FixtureId},{"context_token",r.ContextToken},{"host_failure",r.HostFailure},{"host_failure_rva",r.HostFailureRva},{"engine_read_rva",r.EngineReadRva},{"own_logger_adaptation",r.OwnLoggerAdaptation}});
 }
 static void Missing(uint expected,Action action,string why){try{action();}catch(MissingContract error){Eq(expected,error.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Rejected(Action action,string why){try{action();}catch(MissingContract){Check(true,why);return;}catch(InvalidOperationException){Check(true,why);return;}throw new Exception("Expected lifetime rejection: "+why);}
 static void Test(string name,Action action){int before=assertions;action();scenarios++;checks.Add(new Dictionary<string,object>{{"name",name},{"assertions",assertions-before}});}
 sealed class Log {
  internal WorldEngine Engine;internal uint Priority;internal byte[] Bytes;internal bool RawNull;
 }
 sealed class UnformattableSinkException:Exception {
  public override string ToString(){throw new InvalidOperationException("OWN diagnostic formatting failure");}
 }
 sealed class Scope:IDisposable {
  internal readonly OwnLuaJit204Runtime Runtime=OwnLuaJit204Runtime.OpenOwnProfile();
  internal readonly WorldEngine World=new WorldEngine();
  internal readonly Field<WorldEngine> EngineGlobal=new Field<WorldEngine>();
  internal readonly List<OwnLuaJit204State> States=new List<OwnLuaJit204State>();
  internal readonly List<Log> Logs=new List<Log>();
  internal readonly GameReachedGoalLuaService Service;
  internal readonly OwnLuaCallbackContext Context;
  internal OwnLuaCallbackBinding Binding;
  bool disposed;
  internal Scope(OwnLuaFixtureLogSink sink,bool omitSink){
   EngineGlobal.Write(World);Service=GameLuaServiceInitializationContracts.PublishOwnColdService(World,new Invocation());
   if(sink==null&&!omitSink)sink=delegate(WorldEngine e,uint p,byte[] b,bool n){Logs.Add(new Log{Engine=e,Priority=p,Bytes=b==null?null:(byte[])b.Clone(),RawNull=n});};
   Context=new OwnLuaCallbackContext(Service,EngineGlobal,sink);Runtime.SelectOwnRegistry(Context);
  }
  internal Scope():this(null,false){}
  internal OwnLuaJit204State Current {get{return Service.Lua00.Read(0) as OwnLuaJit204State;}}
  void Track(){var s=Current;if(s!=null&&!States.Contains(s))States.Add(s);}
  internal GameLuaCallbackInitializationInvocation Initialize(){
   var call=new GameLuaCallbackInitializationInvocation();
   try{Missing(0x0f2ed2,delegate{GameLuaServiceInitializationContracts.InitializeCallbackPrefix0F2E00(Service,GameLuaLibraryBindings.ResolveOwn(Runtime),Runtime,Context,call);},"Publication ends at actual LuaGlue call boundary");}
   finally{Track();Binding=call.Binding;}
   Check(call.Publication!=null,"Actual bridge publication receipt exists");Remember(call.Publication);
   Eq(0x0f2ed2u,call.Native.At,"Exact unimplemented continuation");Success(call.Publication,"Publication");
   Check(call.Publication.PublishedMask==127,"All seven native publication operations completed");
   Same(Binding,Current.CallbackBinding,"Same real state owns its callback binding");Same(Context,Binding.Context,"Same mutable callback context");
   Same(Service,GameReachedGoalLogEngine.ForWorldEngine(World).LuaService68.Read(0),"One published Engine service");return call;
  }
  internal OwnLuaCallbackResult Execute(int id){var r=Binding.ExecuteFixture((OwnLuaCallbackFixture)id);Remember(r);return r;}
  internal void CoreAndAttach(){var s=Runtime.NewState(0x0f2e28);Check(s!=null,"Actual state allocated");Service.Lua00.Write(s);Track();GameLuaServiceInitializationContracts.OpenCore25A030(Service,GameLuaLibraryBindings.ResolveOwn(Runtime),Runtime,new Invocation());Binding=Runtime.AttachCallbacks(s,Context,0x0f2e5a);}
  public void Dispose(){if(disposed)return;foreach(var s in States)s.Dispose();Runtime.Dispose();disposed=true;Check(Runtime.ActiveStateCount==0&&Runtime.CloseFailure==null,"All owned states closed normally");Check(Runtime.ModuleUnloadObserved,"Pinned OWN Lua module unload observed");}
 }
 static void Success(OwnLuaCallbackResult r,string why){Check(r.BridgeStatus==0&&r.LuaStatus==0,why+" genuinely returned through native protection");Check((r.Flags&3)==0,why+" has no protected unwind or error object");}
 static void Failure(OwnLuaCallbackResult r,uint stage,string why){Check(r.BridgeStatus!=0&&r.LuaStatus!=0,why+" is not success");Eq(stage,r.StageRva,why+" exact stage");Check((r.Flags&3)==3,why+" records OWN unwind and retained error object");Check(r.ExitTop==r.EntryTop+1,why+" protected outer stack retains one error, not original failed-frame stack");}
 static void Message(Scope s,int index,string text,uint priority){var log=s.Logs[index];Same(s.World,log.Engine,"OWN sink receives actual shared Engine");Eq(priority,log.Priority,"Observed leaf priority");Check(!log.RawNull,"Actual non-null borrowed message");Eq(text,Encoding.ASCII.GetString(log.Bytes),"Exact captured bytes in ASCII fixture");}

 // NoInlining ensures abandoned local binding/context/delegate roots do not
 // accidentally make the subsequent GC retention check pass.
 [MethodImpl(MethodImplOptions.NoInlining)]
 static OwnLuaJit204State PrepareGc(OwnLuaJit204Runtime runtime,GameReachedGoalLuaService service,Field<WorldEngine> engine,List<byte[]> messages,out WeakReference bindingWeak,out WeakReference contextWeak,out WeakReference sinkWeak){
  OwnLuaFixtureLogSink sink=delegate(WorldEngine e,uint p,byte[] b,bool n){messages.Add(b==null?null:(byte[])b.Clone());};
  var context=new OwnLuaCallbackContext(service,engine,sink);runtime.SelectOwnRegistry(context);var call=new GameLuaCallbackInitializationInvocation();
  Missing(0x0f2ed2,delegate{GameLuaServiceInitializationContracts.InitializeCallbackPrefix0F2E00(service,GameLuaLibraryBindings.ResolveOwn(runtime),runtime,context,call);},"GC fixture reaches actual publication boundary");
  Success(call.Publication,"GC fixture publication");bindingWeak=new WeakReference(call.Binding);contextWeak=new WeakReference(context);sinkWeak=new WeakReference(sink);return (OwnLuaJit204State)service.Lua00.Read(0);
 }
 public static Dictionary<string,object> Run(){
  scenarios=assertions=olderAssertions=0;checks.Clear();receipts.Clear();
  Test("Actual publication and byte-exact OWN callback consumption",delegate{
   using(var s=new Scope()){
    s.Service.Initialized58.Write(0x7f);s.Initialize();Eq((byte)0x7f,s.Service.Initialized58.Read(0),"Prefix preserves existing ready byte rather than writing success");Eq((ushort)0x0101,s.Service.Options08.Read(0),"Actual options word write");Eq(5L,s.Runtime.NativePCallZeroCalls,"Exactly five core library calls");
    int[] ids={1,2,7,8,9,6};string[] expected={"OWN callback","first second","A",new String('x',300),"OWN panic probe"," second"};
    for(int n=0;n<ids.Length;n++){var r=s.Execute(ids[n]);Success(r,"Fixed callback fixture "+ids[n]);Check(r.CallbackCalls==1&&r.LoggerCalls==1,"One actual callback and OWN sink dispatch");Check(r.EntryTop==r.ExitTop,"Protected successful fixture balances outer stack");Eq(n+1,s.Logs.Count,"Exactly one captured message");Message(s,n,expected[n],ids[n]==9?4u:3u);}
    var nullProbe=s.Execute(10);Success(nullProbe,"Protected panic-wrapper nil-input probe; not fatal panic recovery");Check(nullProbe.CallbackCalls==1&&nullProbe.LoggerCalls==1,"Actual protected nil panic-wrapper dispatch");Eq(ids.Length+1,s.Logs.Count,"Nil panic-wrapper produces one OWN sink observation");var nullLog=s.Logs[ids.Length];Same(s.World,nullLog.Engine,"Nil panic-wrapper borrows same Engine");Eq(4u,nullLog.Priority,"Panic leaf priority");Check(nullLog.RawNull&&nullLog.Bytes==null,"Actual null C-string is preserved, not replaced by an empty string");
    Eq((byte)0x7f,s.Service.Initialized58.Read(0),"Callbacks do not complete service initialization");
   }
  });
  Test("Reinitializing the same service preserves the overwritten live VM",delegate{
   using(var s=new Scope()){
    var firstCall=s.Initialize();var first=s.Current;var oldBinding=s.Binding;s.Initialize();var second=s.Current;
    Check(!Object.ReferenceEquals(first,second),"Second native allocation replaces Lua00 identity");Check(first.IsAlive&&second.IsAlive,"Overwritten old VM is not silently closed");Eq(2L,s.Runtime.ActiveStateCount,"Both real states remain owned");Eq(0L,s.Runtime.NativeCloseCalls,"Replacement is not teardown");Eq(0,first.GetTop(0),"Old VM remains actually callable");Eq((byte)0,s.Service.Initialized58.Read(0),"Cold service remains uninitialized");
    var oldCall=oldBinding.ExecuteFixture((OwnLuaCallbackFixture)1);Remember(oldCall);Success(oldCall,"Old callback still operates on its original live VM");Eq(firstCall.Publication.ContextToken,oldCall.ContextToken,"Old callback retains original native context token");Eq(0,first.GetTop(0),"Old callback balances its original VM stack");Eq(0,second.GetTop(0),"Old callback leaves new VM untouched; callback does not read service Lua00");
    var republish=oldBinding.PublishPrefix();Remember(republish);Failure(republish,0x0f2e5a,"Republishing old binding consumes changed service identity");Check(republish.PublishedMask==127,"Mask reports prior completed publication, not seven new operations");Eq(0,second.GetTop(0),"Rejected old publication does not switch to the new VM");
    Success(s.Execute(1),"Current same-service binding");Eq(2,s.Logs.Count,"Both live VM callbacks produced their own real dispatch");Same(second,s.Service.Lua00.Read(0),"Shared service retains new state");
   }
  });
  Test("State ownership retains native callbacks and OWN sink across GC",delegate{
   var runtime=OwnLuaJit204Runtime.OpenOwnProfile();OwnLuaJit204State lua=null;GameReachedGoalLuaService service=null;
   try{
    var world=new WorldEngine();var global=Field<WorldEngine>.Of(world);service=GameLuaServiceInitializationContracts.PublishOwnColdService(world,new Invocation());var messages=new List<byte[]>();WeakReference bw,cw,sw;
    lua=PrepareGc(runtime,service,global,messages,out bw,out cw,out sw);GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
    Check(bw.IsAlive&&cw.IsAlive&&sw.IsAlive,"State retains binding, context and sink after locals have left");Same(bw.Target,lua.CallbackBinding,"Actual state-owned binding survives");Same(lua,service.Lua00.Read(0),"GC preserves shared state identity");
    var r=lua.CallbackBinding.ExecuteFixture((OwnLuaCallbackFixture)1);Remember(r);Success(r,"Post-GC real callback");Eq(1,messages.Count,"One actual post-GC sink dispatch");Eq("OWN callback",Encoding.ASCII.GetString(messages[0]),"Post-GC native bytes");Eq((byte)0,service.Initialized58.Read(0),"No synthetic readiness");
   }finally{if(lua==null&&service!=null)lua=service.Lua00.Read(0) as OwnLuaJit204State;if(lua!=null)lua.Dispose();runtime.Dispose();}
   Check(runtime.ActiveStateCount==0&&runtime.CloseFailure==null&&runtime.ModuleUnloadObserved,"GC fixture shuts down owned state and module");
  });
  Test("Closed binding rejects reuse and module cannot unload with live states",delegate{
   using(var s=new Scope()){
    s.Initialize();var lua=s.Current;var binding=s.Binding;Rejected(delegate{s.Runtime.Dispose();},"Runtime unload while VM is live");Check(lua.IsAlive&&!binding.IsClosed,"Rejected unload keeps real VM and binding alive");
    long pushes=s.Runtime.NativePushCClosureCalls,pcalls=s.Runtime.NativePCallZeroCalls,observations=s.Runtime.LibraryGlobalTypeReads;var baseFunction=s.Runtime.GetLibraryFunction(OwnLuaJit204Library.Base,0);
    Missing(0x25a04b,delegate{s.Runtime.PushCClosure(lua,baseFunction,0x25a04b);},"Attached VM rejects unprotected legacy push");Missing(0x25a05c,delegate{s.Runtime.PCallZero(lua,0x25a05c);},"Attached VM rejects legacy call bypass");Missing(0,delegate{s.Runtime.ReadLibraryGlobalType(lua,baseFunction,0);},"Attached VM rejects legacy observation that may call Lua");
    Eq(pushes,s.Runtime.NativePushCClosureCalls,"Rejected push has no native side effect");Eq(pcalls,s.Runtime.NativePCallZeroCalls,"Rejected legacy call has no native side effect");Eq(observations,s.Runtime.LibraryGlobalTypeReads,"Rejected legacy lookup never enters Lua");Eq(0,lua.GetTop(0),"Pure guarded stack observation remains supported");Success(s.Execute(1),"Protected bridge remains usable after rejected bypasses");
    lua.Dispose();Check(binding.IsClosed&&!lua.IsAlive,"Normal Lua close invalidates callback binding");Check(binding.CloseResult!=null,"Actual bridge close receipt exists");Remember(binding.CloseResult);Eq(1L,s.Runtime.NativeCloseCalls,"Exactly one native close");Missing(0,delegate{binding.ExecuteFixture((OwnLuaCallbackFixture)1);},"Closed binding performs no native callback");lua.Dispose();Eq(1L,s.Runtime.NativeCloseCalls,"Repeated close remains idempotent");Same(lua,s.Service.Lua00.Read(0),"Closed identity remains in service, not replaced by null");
   }
  });
  Test("Callback reentry cannot close its state or dispose its runtime",delegate{
   Scope s=null;int invocations=0,closeRejected=0,disposeRejected=0;
   OwnLuaFixtureLogSink sink=delegate(WorldEngine engine,uint priority,byte[] bytes,bool rawNull){invocations++;try{s.Current.Dispose();}catch(MissingContract){closeRejected++;}catch(InvalidOperationException){closeRejected++;}try{s.Runtime.Dispose();}catch(MissingContract){disposeRejected++;}catch(InvalidOperationException){disposeRejected++;}};
   using(s=new Scope(sink,false)){
    s.Initialize();Success(s.Execute(1),"Callback after rejected reentrant lifecycle operations");Eq(1,invocations,"Actual sink entered");Eq(1,closeRejected,"Reentrant close rejected");Eq(1,disposeRejected,"Reentrant runtime Dispose rejected");Check(s.Current.IsAlive&&!s.Binding.IsClosed,"Callback owner remains live");Eq(0L,s.Runtime.NativeCloseCalls,"No premature lua_close");Eq(0,s.Current.GetTop(0),"Actual state remains usable after callback return");
   }
  });
  Test("Real tostring Lua error remains inside the native protected boundary",delegate{
   using(var s=new Scope()){
    s.Initialize();var r=s.Execute(5);Failure(r,0x259e33,"Actual tostring error");Check(r.BridgeStatus==6,"Actual Lua error, not a fabricated host status");Check(r.CallbackCalls==1&&r.LoggerCalls==0,"Print began but never reached logger");Eq(0,s.Logs.Count,"No synthetic message on error");Check(r.HostFailure==null,"No managed callback exception masquerades as Lua error");Check(s.Current.IsAlive,"Failed native call preserves live VM for explicit teardown");Eq((byte)0,s.Service.Initialized58.Read(0),"Failure does not mark service ready");
   }
  });
  Test("Zero and all-nil print keep uninitialized output explicitly unsupported",delegate{
   foreach(int id in new[]{3,4})using(var s=new Scope()){
    s.Initialize();var r=s.Execute(id);Check(r.BridgeStatus==7&&r.LuaStatus!=0,"Undefined native-buffer input is explicitly Unsupported");Check((r.Flags&3)==3&&(r.Flags&8)==0,"No initialized output invented; guard unwind is explicit");Check(r.CallbackCalls==1&&r.LoggerCalls==0,"No fake empty-string logger call");Eq(0,s.Logs.Count,"No output manufactured");Check(r.ExitTop==r.EntryTop+1,"Error object remains after OWN guard");Eq((byte)0,s.Service.Initialized58.Read(0),"Unsupported input preserves ready byte");
   }
  });
  Test("Unknown registry Engine and native logger preserve exact failure boundaries",delegate{
   using(var s=new Scope()){
    s.CoreAndAttach();s.Context.Registry422888.Forget();var r=s.Binding.PublishPrefix();Remember(r);Failure(r,0x0f2e67,"Unknown registry");Check(r.PublishedMask==1,"Actual first lightuserdata push precedes registry consumption");Check(r.FailureLocalTop>0,"Failed protected frame records real local stack");Check(r.LoggerCalls==0&&s.Logs.Count==0,"Unknown registry never reaches logger");Eq((byte)0,s.Service.Initialized58.Read(0),"Partial publication cannot mark service ready");
   }
   using(var s=new Scope()){
    s.Initialize();s.EngineGlobal.Forget();var r=s.Execute(1);Check(r.BridgeStatus!=0&&r.HostFailure!=null,"Unknown Engine is not defaulted");Check(r.EngineReadRva==0x0f2d00,"Print consumes live Engine at real leaf");Eq(0,s.Logs.Count,"Unknown Engine has no successful OWN sink dispatch");Check((r.Flags&3)==3,"Managed rejection returns before native error unwind");
   }
   using(var s=new Scope(null,true)){
    s.Initialize();var r=s.Execute(1);Check(r.BridgeStatus!=0&&r.HostFailure!=null,"Unknown native Engine logger cannot become empty success");Eq(0,s.Logs.Count,"No substitute logging output");Check((r.Flags&3)==3,"Unimplemented logger stays an explicit protected failure");
   }
   bool throwingSinkEntered=false;
   OwnLuaFixtureLogSink throwingSink=delegate(WorldEngine e,uint p,byte[] b,bool n){throwingSinkEntered=true;throw new UnformattableSinkException();};
   using(var s=new Scope(throwingSink,false)){
    s.Initialize();var r=s.Execute(1);Check(throwingSinkEntered,"Actual OWN logger threw the diagnostic-failing exception");Check(r.BridgeStatus==8&&r.HostStatus==2&&r.LuaStatus!=0,"Host exception safely became native HOST_ERROR after managed callback returned");Eq("OWN host exception (diagnostic formatting failed)",r.HostFailure,"Throwing diagnostic receives the agreed safe fallback after native return");Check(r.HostFailure!=null,"Formatting failure still leaves a bounded host diagnostic");Check((r.Flags&3)==3,"Host failure unwinds only inside native protected frame");Check(s.Current.IsAlive,"No managed exception escaped the ABI or destroyed the VM");
   }
  });
  int beforeLegacy=assertions;
  Test("Selected regression: legacy four-argument initializer keeps its old boundary",delegate{
   using(var s=new Scope()){
    s.Service.Initialized58.Write(0x5a);var i=new Invocation();
    try{Missing(0x0f2e5a,delegate{GameLuaServiceInitializationContracts.InitializePrefix0F2E00(s.Service,GameLuaLibraryBindings.ResolveOwn(s.Runtime),s.Runtime,i);},"Legacy overload retains its exact Unsupported");}
    finally{if(s.Current!=null)s.States.Add(s.Current);}
    Eq(0x0f2e5au,i.At,"Legacy stop address");Eq(5L,s.Runtime.NativePushCClosureCalls,"Five real legacy library pushes");Eq(5L,s.Runtime.NativePCallZeroCalls,"Five real legacy protected library calls");Check(s.Current!=null&&s.Current.IsAlive,"Legacy failure preserves actual live state");Same(null,s.Current.CallbackBinding,"Old entry does not silently publish new callbacks");Eq((byte)0x5a,s.Service.Initialized58.Read(0),"Legacy ready byte preserved");Eq(0,s.Current.GetTop(0),"Real legacy core stack balanced");
   }
  });
  olderAssertions=assertions-beforeLegacy;
  return new Dictionary<string,object>{{"status","passed-own-lua-callback-publication-prefix"},{"scenarios",scenarios},{"new_scenarios",scenarios-1},{"selected_older_regressions",1},{"assertions",assertions},{"new_assertions",assertions-olderAssertions},{"selected_older_assertions",olderAssertions},{"old_whole_suites_run",0},{"targeted_checks",checks},{"bridge_receipts",receipts},{"expected_unsupported",0x0f2ed2},{"native_service_initialization_completed",false},{"original_engine_logger_verified",false},{"own_sink_platform_adapter",true},{"original_exception_cleanup_equivalent",false},{"unprotected_panic_executed",false},{"original_executed",false},{"physical_mouse",false},{"native_decoding",false},{"native_playback",false},{"gameplay_accepted",false}};
 }
}
