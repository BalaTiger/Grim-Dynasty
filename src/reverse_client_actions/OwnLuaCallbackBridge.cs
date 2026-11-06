using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace GrimDynasty.ReverseClient.Actions {
 public delegate void OwnLuaFixtureLogSink(WorldEngine engine,uint priority,byte[] bytes,bool rawNull);
 public enum OwnLuaCallbackFixture:uint { PrintOne=1,PrintTwo=2,PrintZero=3,ToStringNil=4,ToStringError=5,FirstNil=6,EmbeddedNul=7,PrintGrowth=8,ProtectedPanic=9,PanicNull=10,FailLuaGlueGlobal=11,FailDumpexGlobal=12,GcListCollect=13,GcObjectCollect=14,GcListClose=15,GcObjectClose=16,GcUnknownClose=17,GcNonemptyList=18,GcNonnullObject=19,GcForeignBuffer=20,FailLuaGlueFirstChunk=21,FailLuaGlueUnknownCallback=22,ObserveScriptRegistration=23,ObserveServiceRoles=24 }
 public sealed class OwnLuaRegistrySelection {
  public uint Version;public int RegistryIndex;public bool HasLuaGetGlobal,HasLuaJitSetmode;
  public readonly string Provenance="OWN actual-export classification followed by observed007FF0..008004 selector; native startup order unproved";
 }
 public sealed class OwnLuaCallbackContext {
  public readonly GameReachedGoalLuaService Service;
  public readonly Field<WorldEngine> EngineGlobal;
  public readonly Field<int> Registry422888=new Field<int>();
  // Shared by bindings using this context; unknown until explicit OWN export
  // classification publishes it. Consumers reread it rather than Selection.
  public readonly Field<uint> Version4217DC=new Field<uint>();
  public readonly Field<int> UpvalueBase422890=new Field<int>(); // Independent writer remains unproved.
  public readonly OwnLuaFixtureLogSink OwnSink;
  internal OwnLuaJit204Runtime SelectedRuntime;
  public OwnLuaRegistrySelection Selection{get;internal set;}
  public OwnLuaCallbackContext(GameReachedGoalLuaService service,Field<WorldEngine> engineGlobal,OwnLuaFixtureLogSink ownSink=null){
   if(service==null||engineGlobal==null)throw new ArgumentNullException("OWN callback context");Service=service;EngineGlobal=engineGlobal;OwnSink=ownSink;
  }
 }
 public sealed class OwnLuaCallbackResult {
  public uint BridgeStatus,StageRva,PublishedMask,CallbackCalls,LoggerCalls,ResolverCalls,Flags,FixtureId;
  public int LuaStatus,EntryTop,ExitTop,FailureLocalTop,HostStatus;
  public ulong ContextToken;
  public string HostFailure;internal Exception HostException;public uint HostFailureRva,EngineReadRva;
  public readonly bool NativeExceptionCleanupEquivalent=false;
  public bool OwnLoggerAdaptation;
 }
 // Only this fixed OWN artifact is admitted. BuildIdentity.cs is generated
 // from this batch's actual native DLL hash/size before compiling this assembly.
 // Neither a caller-selected path/hash nor a fabricated pending pin is accepted.
 internal sealed partial class OwnLuaCallbackBridge:IDisposable {
  const string FixedLibrary=@"C:\Users\zhuzi\Documents\Grim-Dynasty\.tooling\r1-provider-validation\own_luajit204_bridge.dll";
  const string Kernel=@"C:\Windows\System32\kernel32.dll";
  const uint Search=0x900,FromAddress=6;
  readonly FileStream file;readonly ModuleHandle module;
  readonly AttachFunction attach;readonly RunFunction publish;readonly FixtureFunction fixture;readonly BeginCloseFunction beginClose;readonly CompleteCloseFunction completeClose;
  int active;bool disposed;
  [StructLayout(LayoutKind.Sequential,Pack=8)] internal struct Result {
   internal uint Bytes,BridgeStatus;internal int LuaStatus;internal uint StageRva,PublishedMask,CallbackCalls,LoggerCalls,ResolverCalls;
   internal int EntryTop,ExitTop,FailureLocalTop,HostStatus;internal uint Flags,FixtureId;internal ulong Token;
  }
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int Resolver(IntPtr cookie,uint stage,uint flags,out IntPtr state,out int registry);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int Logger(IntPtr cookie,uint priority,IntPtr bytes,uint count,uint flags);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int AttachFunction(IntPtr luaModule,IntPtr state,Resolver resolver,Logger logger,IntPtr cookie,out ulong token);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int RunFunction(ulong token,out Result result);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int FixtureFunction(ulong token,uint fixtureId,out Result result);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int BeginCloseFunction(ulong token);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int CompleteCloseFunction(ulong token,out Result result);
  [DllImport(Kernel,ExactSpelling=true,CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr LoadLibraryExW(string path,IntPtr unused,uint flags);
  [DllImport(Kernel,ExactSpelling=true,CharSet=CharSet.Unicode)] static extern IntPtr GetModuleHandleW(string name);
  [DllImport(Kernel,ExactSpelling=true,CharSet=CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr module,string name);
  [DllImport(Kernel,ExactSpelling=true,CharSet=CharSet.Unicode)] static extern uint GetModuleFileNameW(IntPtr module,StringBuilder path,uint capacity);
  [DllImport(Kernel,ExactSpelling=true,CharSet=CharSet.Unicode)] static extern uint GetFinalPathNameByHandleW(SafeFileHandle file,StringBuilder path,uint capacity,uint flags);
  [DllImport(Kernel,ExactSpelling=true)] [return:MarshalAs(UnmanagedType.Bool)] static extern bool GetModuleHandleExW(uint flags,IntPtr address,out IntPtr module);
  [DllImport(Kernel,ExactSpelling=true)] [return:MarshalAs(UnmanagedType.Bool)] static extern bool FreeLibrary(IntPtr module);
  [DllImport(Kernel,ExactSpelling=true)] internal static extern uint GetCurrentThreadId();
  sealed class ModuleHandle:SafeHandleZeroOrMinusOneIsInvalid {
   internal bool Released;
   internal ModuleHandle(IntPtr pointer):base(true){SetHandle(pointer);}
   protected override bool ReleaseHandle(){Released=FreeLibrary(handle);return Released;}
  }
  static void Plain(string path){for(string p=Path.GetFullPath(path);p!=null;p=Path.GetDirectoryName(p))if((File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0)throw new MissingContract(0,"Redirected OWN bridge path");}
  static string Digest(FileStream stream){stream.Position=0;using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
  static string ModulePath(IntPtr pointer){var value=new StringBuilder(32768);uint n=GetModuleFileNameW(pointer,value,(uint)value.Capacity);if(n==0||n>=value.Capacity)throw new MissingContract(0,"Cannot verify OWN bridge module path");return Path.GetFullPath(value.ToString());}
  static void CheckPin(FileStream stream){if(stream.Length!=OwnLuaCallbackBridgeBuildIdentity.Bytes||!String.Equals(Digest(stream),OwnLuaCallbackBridgeBuildIdentity.Sha256,StringComparison.Ordinal))throw new MissingContract(0,"OWN bridge artifact fingerprint differs");}
  OwnLuaCallbackBridge(FileStream locked,ModuleHandle owned){
   file=locked;module=owned;if(Marshal.SizeOf(typeof(Result))!=64)throw new MissingContract(0,"OWN bridge result ABI size differs");
   attach=(AttachFunction)Bind("gd_lj204_attach",typeof(AttachFunction));publish=(RunFunction)Bind("gd_lj204_publish_prefix",typeof(RunFunction));fixture=(FixtureFunction)Bind("gd_lj204_execute_fixture",typeof(FixtureFunction));beginClose=(BeginCloseFunction)Bind("gd_lj204_begin_close",typeof(BeginCloseFunction));completeClose=(CompleteCloseFunction)Bind("gd_lj204_complete_close",typeof(CompleteCloseFunction));CheckPin(file);
   ValidateLuaGlueLayout();initializeLuaGlue=(LuaGlueFunction)Bind("gd_lj204_initialize_luaglue",typeof(LuaGlueFunction));
   ValidateRegistryLayout();publishRegistry=(RegistryFunction)Bind("gd_lj204_publish_luaglue_registry",typeof(RegistryFunction));readGcAudit=(GcAuditFunction)Bind("gd_lj204_read_gc_audit",typeof(GcAuditFunction));
  }
  Delegate Bind(string name,Type kind){
   IntPtr address=GetProcAddress(module.DangerousGetHandle(),name),owner;
   if(address==IntPtr.Zero||!GetModuleHandleExW(FromAddress,address,out owner)||owner!=module.DangerousGetHandle()||!String.Equals(ModulePath(owner),FixedLibrary,StringComparison.OrdinalIgnoreCase))throw new MissingContract(0,"Missing or foreign OWN bridge export: "+name);
   return Marshal.GetDelegateForFunctionPointer(address,kind);
  }
  internal static OwnLuaCallbackBridge Open(){
   if(IntPtr.Size!=8||!String.Equals(OwnLuaCallbackBridgeBuildIdentity.Library,FixedLibrary,StringComparison.OrdinalIgnoreCase)||OwnLuaCallbackBridgeBuildIdentity.Bytes<=0||OwnLuaCallbackBridgeBuildIdentity.Sha256==null||OwnLuaCallbackBridgeBuildIdentity.Sha256.Length!=64)throw new MissingContract(0,"OWN bridge build identity is unavailable or outside fixed profile");
   if(GetModuleHandleW("own_luajit204_bridge.dll")!=IntPtr.Zero)throw new MissingContract(0,"An existing bridge module is not adopted");
   Plain(FixedLibrary);FileStream locked=null;ModuleHandle owned=null;
   try{
    locked=new FileStream(FixedLibrary,FileMode.Open,FileAccess.Read,FileShare.Read);var path=new StringBuilder(32768);uint n=GetFinalPathNameByHandleW(locked.SafeFileHandle,path,(uint)path.Capacity,0);
    if(n==0||n>=path.Capacity||!String.Equals(path.ToString(),@"\\?\"+FixedLibrary,StringComparison.OrdinalIgnoreCase))throw new MissingContract(0,"Locked OWN bridge path differs");CheckPin(locked);
    IntPtr raw=LoadLibraryExW(FixedLibrary,IntPtr.Zero,Search);if(raw==IntPtr.Zero)throw new MissingContract(0,"OWN bridge load failed: "+Marshal.GetLastWin32Error());
    try{owned=new ModuleHandle(raw);}catch{FreeLibrary(raw);throw;}
    if(!String.Equals(ModulePath(raw),FixedLibrary,StringComparison.OrdinalIgnoreCase))throw new MissingContract(0,"Loaded OWN bridge path differs");
    var result=new OwnLuaCallbackBridge(locked,owned);locked=null;owned=null;return result;
   }finally{if(owned!=null)owned.Dispose();if(locked!=null)locked.Dispose();}
  }
  internal void Pin(){if(disposed)throw new MissingContract(0,"OWN bridge disposed");bool added=false;module.DangerousAddRef(ref added);if(!added)throw new MissingContract(0,"Cannot pin OWN bridge");active++;}
  internal void Unpin(){active--;module.DangerousRelease();}
  internal ulong Attach(IntPtr luaModule,IntPtr state,Resolver resolver,Logger logger,IntPtr cookie){ulong token;int status=attach(luaModule,state,resolver,logger,cookie,out token);if(status!=0||token==0)throw new MissingContract(0x0f2e5a,"OWN bridge attach failed: "+status);return token;}
  internal Result Publish(ulong token){Result result;int status=publish(token,out result);Validate(status,result);return result;}
  internal Result Execute(ulong token,OwnLuaCallbackFixture id){Result result;int status=fixture(token,(uint)id,out result);Validate(status,result);return result;}
  static void Validate(int status,Result result){if(result.Bytes!=64||status!=unchecked((int)result.BridgeStatus))throw new MissingContract(result.StageRva,"OWN bridge receipt ABI/status mismatch");}
  internal void BeginClose(ulong token){int status=beginClose(token);if(status!=0)throw new MissingContract(0,"OWN bridge close admission failed: "+status);}
  internal Result CompleteClose(ulong token){Result result;int status=completeClose(token,out result);if(status!=0||result.Bytes!=64)throw new MissingContract(0,"OWN bridge context release failed: "+status);return result;}
  public void Dispose(){if(disposed)return;if(active!=0)throw new InvalidOperationException("OWN bridge bindings remain live or uncertain");module.Dispose();disposed=true;try{if(!module.Released||GetModuleHandleW("own_luajit204_bridge.dll")!=IntPtr.Zero)throw new MissingContract(0,"OWN bridge unload not observed");}finally{file.Dispose();}}
 }
 // StateHandle owns this binding/delegates through actual lua_close. Cookie is
 // a monotonically assigned value, not a GCHandle or independent strong root.
 public sealed partial class OwnLuaCallbackBinding {
  static long nextCookie;
  readonly OwnLuaJit204Runtime runtime;readonly OwnLuaJit204State state;readonly OwnLuaCallbackBridge bridge;
  readonly OwnLuaCallbackBridge.Resolver resolver;readonly OwnLuaCallbackBridge.Logger logger;
  readonly IntPtr cookie;readonly uint ownerThread;
  bool pinned,closing,closed;uint closingThread;
  Exception hostException;uint hostRva,engineReadRva;
  public OwnLuaCallbackContext Context{get;private set;}
  public ulong Token{get;private set;}
  public bool IsClosed{get{return closed;}}
  public OwnLuaCallbackResult LastResult{get;private set;}
  public OwnLuaCallbackResult CloseResult{get;private set;}
  internal OwnLuaCallbackBinding(OwnLuaJit204Runtime owner,OwnLuaJit204State value,OwnLuaCallbackContext context,OwnLuaCallbackBridge module){
   runtime=owner;state=value;Context=context;bridge=module;ownerThread=OwnLuaCallbackBridge.GetCurrentThreadId();long id=Interlocked.Increment(ref nextCookie);if(id<=0)throw new MissingContract(0,"OWN callback cookie exhausted");cookie=new IntPtr(id);
   resolver=Resolve;logger=Log;bridge.Pin();pinned=true;
   try{Token=bridge.Attach(runtime.ModuleForBridge(0x0f2e5a),runtime.StateForBridge(state,0x0f2e5a),resolver,logger,cookie);}catch{bridge.Unpin();pinned=false;throw;}
  }
  internal void RequireReady(uint at){if(closed||closing)throw new MissingContract(at,"OWN callback binding is closed or closing");if(OwnLuaCallbackBridge.GetCurrentThreadId()!=ownerThread)throw new MissingContract(at,"OWN callback binding used by another thread");}
  void RequireHost(IntPtr received,uint at){if(received!=cookie||closed||Token==0||OwnLuaCallbackBridge.GetCurrentThreadId()!=(closing?closingThread:ownerThread))throw new MissingContract(at,"Unknown OWN callback thread/context");}
  void ClearHost(){hostException=null;hostRva=engineReadRva=0;}
  // No formatting, virtual calls or allocation in a reverse-P/Invoke catch.
  int Failed(Exception error,uint at){if(hostException==null){hostException=error;var missing=error as MissingContract;hostRva=missing==null?at:missing.Address;}return error is MissingContract?1:2;}
  string FormatHostFailure(){if(hostException==null)return null;try{return hostException.ToString();}catch{return "OWN host exception (diagnostic formatting failed)";}}
  int Resolve(IntPtr received,uint stage,uint flags,out IntPtr current,out int registry){
   current=IntPtr.Zero;registry=0;bool entered=false;
   try{
    RequireHost(received,stage);runtime.EnterHostCallback(stage);entered=true;
    if(flags==0||(flags&~3u)!=0)throw new MissingContract(stage,"Unknown native resolver request");
    if((flags&1)!=0){object value=Context.Service.Lua00.Read(stage);if(value==null)current=IntPtr.Zero;else{var lua=value as OwnLuaJit204State;if(lua==null)throw new MissingContract(stage,"Service Lua00 is not an OWN VM identity");current=runtime.StateForBridge(lua,stage);}}
    if((flags&2)!=0)registry=Context.Registry422888.Read(stage);
    return 0;
   }catch(Exception error){return Failed(error,stage);}
   finally{if(entered)runtime.ExitHostCallback();}
  }
  int Log(IntPtr received,uint priority,IntPtr bytes,uint count,uint flags){
   uint entry=priority==3?0x0f2d00u:0x0f2d20u;bool entered=false;
   try{
    RequireHost(received,entry);runtime.EnterHostCallback(entry);entered=true;
    if(priority!=3&&priority!=4)throw new MissingContract(entry,"Unknown message-hook priority");engineReadRva=entry;
    var engine=Context.EngineGlobal.Read(entry);uint dereference=priority==3?0x0f2d12u:0x0f2d32u;
    if(engine==null)throw new MissingContract(dereference,"Native message hook has no Engine null guard");
    if(Context.OwnSink==null){GameReachedGoalLogEngine.ForWorldEngine(engine).Virtual00.Read(dereference);throw new MissingContract(priority==3?0x0f2d15u:0x0f2d35u,"Actual Engine logger remains unsupported; no no-op sink selected");}
    if((flags&~1u)!=0||(bytes==IntPtr.Zero)!=((flags&1)!=0)||count>Int32.MaxValue||((flags&1)!=0&&count!=0))throw new MissingContract(entry,"Inconsistent borrowed message span");
    bool rawNull=(flags&1)!=0;byte[] copy=rawNull?null:new byte[(int)count];if(count!=0)Marshal.Copy(bytes,copy,0,(int)count);
    Context.OwnSink(engine,priority,copy,rawNull);return 0;
   }catch(Exception error){return Failed(error,entry);}
   finally{if(entered)runtime.ExitHostCallback();}
  }
  OwnLuaCallbackResult Receipt(OwnLuaCallbackBridge.Result value){return new OwnLuaCallbackResult{BridgeStatus=value.BridgeStatus,LuaStatus=value.LuaStatus,StageRva=value.StageRva,PublishedMask=value.PublishedMask,CallbackCalls=value.CallbackCalls,LoggerCalls=value.LoggerCalls,ResolverCalls=value.ResolverCalls,EntryTop=value.EntryTop,ExitTop=value.ExitTop,FailureLocalTop=value.FailureLocalTop,HostStatus=value.HostStatus,Flags=value.Flags,FixtureId=value.FixtureId,ContextToken=value.Token,HostFailure=FormatHostFailure(),HostException=hostException,HostFailureRva=hostRva,EngineReadRva=engineReadRva,OwnLoggerAdaptation=Context.OwnSink!=null};}
  public OwnLuaCallbackResult PublishPrefix(){return runtime.InvokeBridge(state,this,delegate{ClearHost();LastResult=Receipt(bridge.Publish(Token));return LastResult;},0x0f2e5a);}
  public OwnLuaCallbackResult ExecuteFixture(OwnLuaCallbackFixture id){if((uint)id<1||(uint)id>24)throw new MissingContract(0,"Unknown fixed OWN callback fixture");return runtime.InvokeBridge(state,this,delegate{ClearHost();LastResult=Receipt(bridge.Execute(Token,id));return LastResult;},0);}
  internal void BeginClose(){if(closed)return;if(closing){if(closingThread!=OwnLuaCallbackBridge.GetCurrentThreadId())throw new MissingContract(0,"OWN close already belongs to another thread");return;}bridge.BeginClose(Token);closing=true;closingThread=OwnLuaCallbackBridge.GetCurrentThreadId();ClearHost();}
  internal void CompleteClose(){if(!state.Handle.NativeClosed)throw new MissingContract(0,"OWN context release requires actual lua_close return");CaptureCloseGcAudit();CaptureCloseScriptAudit();var receipt=bridge.CompleteClose(Token);closed=true;if(pinned){bridge.Unpin();pinned=false;}CloseResult=Receipt(receipt);GC.KeepAlive(resolver);GC.KeepAlive(logger);GC.KeepAlive(scriptReader);}
 }
}
