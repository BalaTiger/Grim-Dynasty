using System;

namespace GrimDynasty.ReverseClient.Actions {
 // OWN resolutions of the five slots consumed by actual Engine25A030. Unknown,
 // known-null and live exported functions remain distinct; no fallback openlibs.
 public sealed class GameLuaLibraryBindings {
  public readonly Field<OwnLuaJit204Function> Base421908=new Field<OwnLuaJit204Function>();
  public readonly Field<OwnLuaJit204Function> Math421970=new Field<OwnLuaJit204Function>();
  public readonly Field<OwnLuaJit204Function> String421938=new Field<OwnLuaJit204Function>();
  public readonly Field<OwnLuaJit204Function> Table421950=new Field<OwnLuaJit204Function>();
  public readonly Field<OwnLuaJit204Function> Jit4218E0=new Field<OwnLuaJit204Function>();
  public static GameLuaLibraryBindings ResolveOwn(OwnLuaJit204Runtime runtime){
   if(runtime==null)throw new ArgumentNullException("runtime");
   var b=new GameLuaLibraryBindings();
   b.Base421908.Write(runtime.GetLibraryFunction(OwnLuaJit204Library.Base,0));
   b.Math421970.Write(runtime.GetLibraryFunction(OwnLuaJit204Library.Math,0));
   b.String421938.Write(runtime.GetLibraryFunction(OwnLuaJit204Library.String,0));
   b.Table421950.Write(runtime.GetLibraryFunction(OwnLuaJit204Library.Table,0));
   b.Jit4218E0.Write(runtime.GetLibraryFunction(OwnLuaJit204Library.Jit,0));
   return b;
  }
 }
 public sealed class GameLuaCallbackInitializationInvocation {
  public readonly Invocation Native=new Invocation();
  public OwnLuaCallbackBinding Binding;
  public OwnLuaCallbackResult Publication;
 }
 public sealed class GameLuaGlueInitializationInvocation {
  public readonly GameLuaCallbackInitializationInvocation CallbackPrefix=new GameLuaCallbackInitializationInvocation();
  public Invocation Native{get{return CallbackPrefix.Native;}}
  public OwnLuaCallbackBinding Binding{get{return CallbackPrefix.Binding;}}
  public OwnLuaGlueResult Glue;
 }
 public sealed class GameLuaRegistryInitializationInvocation {
  public readonly GameLuaGlueInitializationInvocation GluePrefix=new GameLuaGlueInitializationInvocation();
  public Invocation Native{get{return GluePrefix.Native;}}
  public OwnLuaCallbackBinding Binding{get{return GluePrefix.Binding;}}
  public OwnLuaRegistryResult Registry;
 }
 public sealed class GameLuaGlueCompletionInitializationInvocation {
  public readonly GameLuaCallbackInitializationInvocation CallbackPrefix=new GameLuaCallbackInitializationInvocation();
  public Invocation Native{get{return CallbackPrefix.Native;}}
  public OwnLuaCallbackBinding Binding{get{return CallbackPrefix.Binding;}}
  public OwnLuaGlueCompletionResult Completion;
 }
 public sealed class GameLuaScriptInitializationInvocation {
  public readonly GameLuaGlueCompletionInitializationInvocation GluePrefix=new GameLuaGlueCompletionInitializationInvocation();
  public Invocation Native{get{return GluePrefix.Native;}}
  public OwnLuaCallbackBinding ScriptBinding;
  public OwnLuaScriptResult Script;
 }
 public sealed class GameLuaServiceRolesInvocation {
  public readonly Invocation Native=new Invocation();
  public OwnLuaCallbackBinding ClientBinding,ServerBinding;
  public OwnLuaServiceRoleResult Client,Server;
  public bool Returned;
 }
 public sealed class GameLuaProviderInitializationInvocation {
  public readonly GameLuaScriptInitializationInvocation ScriptPrefix=new GameLuaScriptInitializationInvocation();
  public readonly GameLuaProviderLoopInvocation Providers=new GameLuaProviderLoopInvocation();
  public readonly GameLuaServiceRolesInvocation Roles=new GameLuaServiceRolesInvocation();
  public Invocation Native{get{return ScriptPrefix.Native;}}
 }
 public static class GameLuaServiceInitializationContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Missing OWN Lua initialization identity");return value;}
  // Observed0743C0 NEG DL;SBB EAX,EAX;AND EAX,7;DWORD store0743C7.
  // Explicit configuration of one shared global cell, not a native cold default
  // or a per-service flag. Nonzero low-byte input writes all three low bits.
  public static void SetLuaGlueLogging0743C0(Field<uint> sharedFlags4217D8,byte enabled,Invocation invocation){
   var i=Need(invocation,0x0743c0);Need(sharedFlags4217D8,i.At=0x0743c0);
   i.At=0x0743c7;sharedFlags4217D8.Write(enabled==0?0u:7u);
  }
  static OwnLuaJit204State LiveState(GameReachedGoalLuaService service,uint at){
   var state=Need(service,at).Lua00.Read(at) as OwnLuaJit204State;
   if(state==null)throw new MissingContract(at,"Lua00 is not a supported real OWN LuaJIT state");
   state.RequireLive(at);return state;
  }
  // Explicit own-storage adaptation of the represented stores on the cached
  // Engine074BF1..074C3F normal R14==0 allocation path. It is not native Engine
  // startup, a complete0x78 byte layout, or proof that initialization returned.
  // Unrepresented words/gaps remain outside this model. Publish last, retaining
  // the supplied WorldEngine identity and its one authoritative +68 field.
  public static GameReachedGoalLuaService PublishOwnColdService(WorldEngine engine,Invocation invocation){
   var i=Need(invocation,0x074bf1);Need(engine,i.At=0x074bf1);
   var service=new GameReachedGoalLuaService();
   service.Lua00.Write(null);service.Options08.Write(0x0101);
   i.At=0x074c23;service.Providers40.Begin00.Write(null);
   i.At=0x074c27;service.Providers40.End08.Write(null);
   i.At=0x074c2b;service.Providers40.Capacity10.Write(null);
   i.At=0x074c2f;service.Initialized58.Write(0);
   i.At=0x074c3f;GameReachedGoalLogEngine.ForWorldEngine(engine).LuaService68.Write(service);return service;
  }
  static void OpenOne(GameReachedGoalLuaService service,OwnLuaJit204Runtime runtime,
   Field<OwnLuaJit204Function> slot,uint read,uint push,uint call,uint failure,Invocation i){
   i.At=read;var function=slot.Read(i.At);
   if(function==null)return;
   i.At=push;Need(runtime,i.At).PushCClosure(LiveState(service,i.At),function,i.At);
   // The native body reloads service+00 after pushcclosure. Do not retain the
   // preceding state across a native callback or copy the service object.
   i.At=call;int status=runtime.PCallZero(LiveState(service,i.At),i.At);
   if(status!=0){i.At=failure;throw new MissingContract(i.At,"Actual lua_pcall error requires Engine25A150; error object and state are preserved");}
  }
  // Actual Rea pseudo/assembly at0x18025a030, no inferred luaL_openlibs call.
  // Error-handler25A150 is unclosed: reaching it is Unsupported, not false,
  // stack cleanup or success. Normal return writes AL=1; upper EAX is not a
  // proven boolean return. No Initialized58 store occurs in this helper.
  public static byte OpenCore25A030(GameReachedGoalLuaService service,GameLuaLibraryBindings bindings,OwnLuaJit204Runtime runtime,Invocation invocation){
   var i=Need(invocation,0x25a036);i.At=0x25a036;Need(bindings,i.At);
   OpenOne(service,runtime,bindings.Base421908,0x25a036,0x25a04b,0x25a05c,0x25a069,i);
   OpenOne(service,runtime,bindings.Math421970,0x25a06e,0x25a080,0x25a091,0x25a09e,i);
   OpenOne(service,runtime,bindings.String421938,0x25a0a3,0x25a0b5,0x25a0c6,0x25a0d3,i);
   OpenOne(service,runtime,bindings.Table421950,0x25a0d8,0x25a0ea,0x25a0fb,0x25a108,i);
   OpenOne(service,runtime,bindings.Jit4218E0,0x25a10d,0x25a11f,0x25a130,0x25a13d,i);
   i.At=0x25a142;return 1;
  }
  static bool CreateAndOpenCore(GameReachedGoalLuaService service,GameLuaLibraryBindings bindings,OwnLuaJit204Runtime runtime,Invocation invocation,out OwnLuaJit204State lua,out byte earlyReturn){
   var i=Need(invocation,0x0f2e28);Need(service,i.At=0x0f2e28);Need(runtime,i.At);
   earlyReturn=0;lua=runtime.NewState(i.At);
   i.At=0x0f2e2e;service.Lua00.Write(lua); // Real native null becomes managed null.
   if(lua==null){i.At=0x0f30fd;earlyReturn=service.Initialized58.Read(i.At);return false;}
   i.At=0x0f2e3a;service.Options08.Write(0x0101);
   i.At=0x0f2e43;byte core=OpenCore25A030(service,bindings,runtime,i);
   if(core==0){i.At=0x0f30fd;earlyReturn=service.Initialized58.Read(i.At);return false;}
   return true;
  }
  // Retained entry: its previously tested boundary does not move silently.
  public static byte InitializePrefix0F2E00(GameReachedGoalLuaService service,GameLuaLibraryBindings bindings,OwnLuaJit204Runtime runtime,Invocation invocation){
   OwnLuaJit204State lua;byte earlyReturn;var i=Need(invocation,0x0f2e28);
   if(!CreateAndOpenCore(service,bindings,runtime,i,out lua,out earlyReturn))return earlyReturn;
   i.At=0x0f2e5a;throw new MissingContract(i.At,"Core libraries returned; Engine0F2D20 callback identity/publication and remaining0F2E00 initialization are not implemented");
  }
  // Explicit OWN ABI/platform adaptation of only the evidenced publication
  // prefix. The same mutable service and VM are retained. Registry selection
  // is a prior explicit OWN operation, never a guessed native startup order.
  // A bridge failure preserves its executed stages and OWN cpcall receipt;
  // its unwound protected stack is NOT original native EH equivalence.
  static bool CreateAndPublishCallbacks(GameReachedGoalLuaService service,GameLuaLibraryBindings bindings,OwnLuaJit204Runtime runtime,OwnLuaCallbackContext context,GameLuaCallbackInitializationInvocation invocation,out byte earlyReturn){
   var call=Need(invocation,0x0f2e28);var i=call.Native;call.Binding=null;call.Publication=null;
   earlyReturn=0;
   if(context==null||!Object.ReferenceEquals(context.Service,service))throw new MissingContract(i.At=0x0f2e28,"OWN callback context must borrow this exact service");
   OwnLuaJit204State lua;
   if(!CreateAndOpenCore(service,bindings,runtime,i,out lua,out earlyReturn))return false;
   i.At=0x0f2e5a;call.Binding=runtime.AttachCallbacks(lua,context,i.At);
   call.Publication=call.Binding.PublishPrefix();
   if(call.Publication.BridgeStatus!=0){i.At=call.Publication.StageRva;throw new MissingContract(i.At,"Actual OWN protected callback publication stopped; receipt retains stage, mask and failure-local stack");}
   return true;
  }
  public static byte InitializeCallbackPrefix0F2E00(GameReachedGoalLuaService service,GameLuaLibraryBindings bindings,OwnLuaJit204Runtime runtime,OwnLuaCallbackContext context,GameLuaCallbackInitializationInvocation invocation){
   byte earlyReturn;if(!CreateAndPublishCallbacks(service,bindings,runtime,context,invocation,out earlyReturn))return earlyReturn;
   var i=invocation.Native;
   i.At=0x0f2ed2;throw new MissingContract(i.At,"Callback prefix returned; direct LuaGlue2568D0 and remaining service initialization are unsupported; Initialized58 is unchanged");
  }
  // Separate finite continuation. Do not catch the older entry's Unsupported
  // and continue. Only actual version/script effects are recorded by the bridge;
  // its supported prefix cannot write service readiness or a native return byte.
  static bool CreateAndRunLuaGlue(GameReachedGoalLuaService service,GameLuaLibraryBindings bindings,OwnLuaJit204Runtime runtime,OwnLuaCallbackContext context,GameLuaGlueInitializationInvocation invocation,out byte earlyReturn){
   var call=Need(invocation,0x0f2e28);call.Glue=null;
   if(!CreateAndPublishCallbacks(service,bindings,runtime,context,call.CallbackPrefix,out earlyReturn))return false;
   var i=call.Native;i.At=0x0f2ecf;call.Glue=call.Binding.InitializeLuaGlue();
   i.At=call.Glue.Call.StageRva;return true;
  }
  public static byte InitializeLuaGluePrefix0F2E00(GameReachedGoalLuaService service,GameLuaLibraryBindings bindings,OwnLuaJit204Runtime runtime,OwnLuaCallbackContext context,GameLuaGlueInitializationInvocation invocation){
   byte earlyReturn;if(!CreateAndRunLuaGlue(service,bindings,runtime,context,invocation,out earlyReturn))return earlyReturn;
   var call=invocation;var i=call.Native;
   if(call.Glue.Call.BridgeStatus!=0)throw new MissingContract(i.At,"OWN LuaGlue stopped at its observed phase; actual script prefix is preserved and Initialized58 is unchanged");
   // The fixed profile must stop before registry publication. A future changed
   // bridge cannot silently extend this service entry beyond its agreed scope.
   i.At=0x25762b;throw new MissingContract(i.At,"LuaGlue registry continuation is unsupported; no completed native return or service readiness is claimed");
  }
  // One explicit continuation of the same production call. The old public
  // wrapper keeps its stop; no catch-and-skip of Unsupported is involved here.
  public static byte InitializeLuaRegistryPrefix0F2E00(GameReachedGoalLuaService service,GameLuaLibraryBindings bindings,OwnLuaJit204Runtime runtime,OwnLuaCallbackContext context,GameLuaRegistryInitializationInvocation invocation){
   var call=Need(invocation,0x0f2e28);call.Registry=null;byte earlyReturn;
   if(!CreateAndRunLuaGlue(service,bindings,runtime,context,call.GluePrefix,out earlyReturn))return earlyReturn;
   var i=call.Native;var glue=call.GluePrefix.Glue;
   if(glue.Phase!=8||glue.Call.StageRva!=0x25762b||glue.Call.BridgeStatus!=7||glue.Call.LuaStatus!=0||glue.NativeReturnByte!=-1)
    throw new MissingContract(i.At,"Actual LuaGlue prefix did not reach the supported registry continuation");
   // Native additionally checks its own recorded qualification and retained VM.
   call.Registry=call.Binding.PublishLuaGlueRegistry();i.At=call.Registry.Call.StageRva;
   if(call.Registry.Call.BridgeStatus!=0)throw new MissingContract(i.At,"OWN registry prefix stopped; live4217D8 and remaining native initialization are unproved; Initialized58 is unchanged");
   i.At=0x257707;throw new MissingContract(i.At,"Registry prefix cannot select an unknown4217D8 flag or claim a native return");
  }
  // A fresh full LuaGlue call retains its own records until the evidenced tail.
  // It does not resume old prefix APIs whose OWN allocations were already freed.
  static bool CreateAndCompleteLuaGlue(GameReachedGoalLuaService service,GameLuaLibraryBindings bindings,OwnLuaJit204Runtime runtime,OwnLuaCallbackContext context,Field<uint> sharedFlags4217D8,GameLuaGlueCompletionInitializationInvocation invocation,out byte earlyReturn){
   var call=Need(invocation,0x0f2e28);call.Completion=null;
   if(!CreateAndPublishCallbacks(service,bindings,runtime,context,call.CallbackPrefix,out earlyReturn))return false;
   var i=call.Native;i.At=0x0f2ecf;call.Completion=call.Binding.InitializeLuaGlueCompletion(sharedFlags4217D8);
   var result=call.Completion;i.At=result.Call.StageRva;
   if(result.Glue.NativeReturnByte<0||result.Phase!=6)
    throw new MissingContract(i.At,"Actual LuaGlue completion stopped; OWN cleanup alone is not a native return");
   return true;
  }
  public static byte InitializeLuaGlueCompletionPrefix0F2E00(GameReachedGoalLuaService service,GameLuaLibraryBindings bindings,OwnLuaJit204Runtime runtime,OwnLuaCallbackContext context,Field<uint> sharedFlags4217D8,GameLuaGlueCompletionInitializationInvocation invocation){
   byte earlyReturn;if(!CreateAndCompleteLuaGlue(service,bindings,runtime,context,sharedFlags4217D8,invocation,out earlyReturn))return earlyReturn;
   var i=invocation.Native;
   //0F2ED2 ignores both normal0 and normal1.0EF8B0 would recapture current
   // service.Lua00; do not force the previous helper's VM into that next call.
   i.At=0x0f2eda;throw new MissingContract(i.At,"LuaGlue returned; Script0EF8B0 and remaining service initialization are unsupported; Initialized58 is unchanged");
  }
  // Direct observed Script function for the current service VM. Descriptor is an
  // explicit OWN byte observation/input; null remains unknown until its read.
  public static OwnLuaScriptResult InitializeScript0EF8B0(GameReachedGoalLuaService service,OwnLuaJit204Runtime runtime,OwnLuaCallbackContext context,Field<uint> sharedFlags4217D8,byte? ownDescriptor307E60,GameLuaScriptInitializationInvocation invocation){
   var call=Need(invocation,0x0ef8c4);call.Script=null;call.ScriptBinding=null;var i=call.Native;
   i.At=0x0ef8c4;call.ScriptBinding=Need(runtime,i.At).CaptureServiceBinding(service,context,i.At);
   call.Script=call.ScriptBinding.InitializeScriptCaptured(sharedFlags4217D8,ownDescriptor307E60);i.At=call.Script.Call.StageRva;
   if(call.Script.NormalReturned!=1)throw new MissingContract(i.At,"Script registration stopped; prior publications and OWN cleanup observations are preserved");
   i.At=0x0ef9e2;return call.Script;
  }
  public static byte InitializeScriptPrefix0F2E00(GameReachedGoalLuaService service,GameLuaLibraryBindings bindings,OwnLuaJit204Runtime runtime,OwnLuaCallbackContext context,Field<uint> sharedFlags4217D8,byte? ownDescriptor307E60,GameLuaScriptInitializationInvocation invocation){
   var call=Need(invocation,0x0f2e28);call.Script=null;call.ScriptBinding=null;byte earlyReturn;
   if(!CreateAndCompleteLuaGlue(service,bindings,runtime,context,sharedFlags4217D8,call.GluePrefix,out earlyReturn))return earlyReturn;
   call.Native.At=0x0f2eda;InitializeScript0EF8B0(service,runtime,context,sharedFlags4217D8,ownDescriptor307E60,call);
   call.Native.At=0x0f2edf;throw new MissingContract(call.Native.At,"Script registration returned; provider/client/server initialization remains unsupported; Initialized58 is unchanged");
  }
  // Direct, bounded branch contract only. A caller must separately prove the
  // provider loop completed; this helper cannot publish service readiness.
  public static void PublishRoleBranches0F2F25(GameReachedGoalLuaService service,OwnLuaJit204Runtime runtime,OwnLuaCallbackContext context,byte client,byte server,GameLuaServiceRolesInvocation invocation){
   var call=Need(invocation,0x0f2f25);var i=call.Native;
   call.Client=null;call.Server=null;call.ClientBinding=null;call.ServerBinding=null;call.Returned=false;
   i.At=0x0f2f25;
   if(client!=0){
    i.At=0x0f2f2e;call.ClientBinding=Need(runtime,i.At).CaptureServiceBinding(service,context,i.At);
    call.Client=call.ClientBinding.PublishServiceRoleCaptured(OwnLuaServiceRole.Client);i.At=call.Client.Call.StageRva;
    if(call.Client.NormalReturned!=1)throw new MissingContract(i.At,"Client global publication stopped; executed prefix and registry reference are preserved");
   }
   i.At=0x0f300f;
   if(server!=0){
    i.At=0x0f3018;call.ServerBinding=Need(runtime,i.At).CaptureServiceBinding(service,context,i.At);
    call.Server=call.ServerBinding.PublishServiceRoleCaptured(OwnLuaServiceRole.Server);i.At=call.Server.Call.StageRva;
    if(call.Server.NormalReturned!=1)throw new MissingContract(i.At,"Server global publication stopped; Client and executed Server prefix are preserved");
   }
   i.At=0x0f30f9;call.Returned=true;
  }
  // Conditional OWN continuation. Known empty fields must come from an explicit
  // cold construction/publication; no empty list is synthesized at this call.
  // Nonempty provider bodies remain a real stop, not a test callback adapter.
  public static byte InitializeProviderPrefix0F2E00(GameReachedGoalLuaService service,GameLuaLibraryBindings bindings,OwnLuaJit204Runtime runtime,OwnLuaCallbackContext context,Field<uint> sharedFlags4217D8,byte? ownDescriptor307E60,byte client,byte server,GameLuaProviderInitializationInvocation invocation){
   var call=Need(invocation,0x0f2e28);byte earlyReturn;
   call.ScriptPrefix.Script=null;call.ScriptPrefix.ScriptBinding=null;
   call.Providers.InitialCount=null;call.Providers.FirstProvider=null;call.Providers.FirstVirtualTable=null;call.Providers.ReachedRoleBranches=false;call.Providers.Native.At=0;
   call.Roles.Client=null;call.Roles.Server=null;call.Roles.ClientBinding=null;call.Roles.ServerBinding=null;call.Roles.Returned=false;call.Roles.Native.At=0;
   if(!CreateAndCompleteLuaGlue(service,bindings,runtime,context,sharedFlags4217D8,call.ScriptPrefix.GluePrefix,out earlyReturn))return earlyReturn;
   call.Native.At=0x0f2eda;InitializeScript0EF8B0(service,runtime,context,sharedFlags4217D8,ownDescriptor307E60,call.ScriptPrefix);
   try{GameLuaProviderContracts.InitializeProvidersPrefix0F2EDF(service,call.Providers);}
   finally{call.Native.At=call.Providers.Native.At;}
   try{PublishRoleBranches0F2F25(service,runtime,context,client,server,call.Roles);}
   finally{call.Native.At=call.Roles.Native.At;}
   throw new MissingContract(call.Native.At,"Conditional OWN provider/role prefix reached readiness store; original provider population and full service startup remain unproved, so this prefix does not publish Initialized58");
  }
 }
}
