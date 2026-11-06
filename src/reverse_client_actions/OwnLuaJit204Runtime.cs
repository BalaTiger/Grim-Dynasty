using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace GrimDynasty.ReverseClient.Actions {
 // OWN fixed-source runtime, not the original game's loaded Lua module or ABI.
 // Library entries are limited to the five observed by Engine25A030. Their
 // ordering and service-pointer reloads belong to the separate native contract.
 // Callback execution is confined to the separately pinned OWN native bridge;
 // neither its protected boundary nor publication implies service success.
 public sealed class OwnLuaJit204Runtime : IDisposable {
  public const string Library=@"C:\Users\zhuzi\Documents\Grim-Dynasty\.tooling\luajit204-build-run1\source\src\lua51.dll";
  public const string VerifiedSha256="95a6c84cfda2268d0d52967d6173d0b7c4ab609df06e06cdb1d6442059310010";
  public const long VerifiedBytes=436736;
  const string Kernel=@"C:\Windows\System32\kernel32.dll";
  const uint SearchDllDirectory=0x100,SearchSystem32=0x800,FromAddressUnchangedRefcount=6;
  static readonly object openGate=new object();
  static readonly List<StateHandle> uncertainCloseRoots=new List<StateHandle>();
  readonly object gate=new object();
  readonly FileStream lockedFile;
  readonly ModuleHandle module;
  readonly NewStateFunction newState;
  readonly GetTopFunction getTop;
  readonly CloseFunction close;
  readonly PushCClosureFunction pushCClosure;
  readonly PCallFunction pCall;
  readonly GetFieldFunction getField;
  readonly TypeFunction type;
  readonly SetTopFunction setTop;
  readonly Dictionary<string,long> exportRvas=new Dictionary<string,long>(StringComparer.Ordinal);
  readonly Dictionary<OwnLuaJit204Library,OwnLuaJit204Function> libraryFunctions=new Dictionary<OwnLuaJit204Library,OwnLuaJit204Function>();
  bool disposed,unloadObserved;
  OwnLuaCallbackBridge callbackBridge;
  int nativeCalls,hostCallbacks;
  string closeFailure;
  long liveStates,newStateCalls,getTopCalls,closeCalls,pushCClosureCalls,pCallZeroCalls,globalTypeReads;

  [DllImport(Kernel,ExactSpelling=true,CharSet=CharSet.Unicode,SetLastError=true)]
  static extern IntPtr LoadLibraryExW(string file,IntPtr reserved,uint flags);
  [DllImport(Kernel,ExactSpelling=true,CharSet=CharSet.Unicode,SetLastError=true)]
  static extern IntPtr GetModuleHandleW(string name);
  [DllImport(Kernel,ExactSpelling=true,CharSet=CharSet.Ansi,SetLastError=true)]
  static extern IntPtr GetProcAddress(IntPtr module,string name);
  [DllImport(Kernel,ExactSpelling=true,CharSet=CharSet.Unicode,SetLastError=true)]
  static extern uint GetModuleFileNameW(IntPtr module,StringBuilder path,uint capacity);
  [DllImport(Kernel,ExactSpelling=true,CharSet=CharSet.Unicode,SetLastError=true)]
  static extern uint GetFinalPathNameByHandleW(SafeFileHandle file,StringBuilder path,uint capacity,uint flags);
  [DllImport(Kernel,ExactSpelling=true,SetLastError=true)]
  [return:MarshalAs(UnmanagedType.Bool)]
  static extern bool GetModuleHandleExW(uint flags,IntPtr address,out IntPtr module);
  [DllImport(Kernel,ExactSpelling=true,SetLastError=true)]
  [return:MarshalAs(UnmanagedType.Bool)]
  static extern bool FreeLibrary(IntPtr module);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate IntPtr NewStateFunction();
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int GetTopFunction(IntPtr state);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void CloseFunction(IntPtr state);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void PushCClosureFunction(IntPtr state,IntPtr function,int upvalues);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int PCallFunction(IntPtr state,int arguments,int results,int errorFunction);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void GetFieldFunction(IntPtr state,int index,[MarshalAs(UnmanagedType.LPStr)]string name);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int TypeFunction(IntPtr state,int index);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void SetTopFunction(IntPtr state,int index);

  sealed class ModuleHandle:SafeHandleZeroOrMinusOneIsInvalid {
   internal bool ReleaseSucceeded;
   internal ModuleHandle(IntPtr value):base(true){SetHandle(value);}
   protected override bool ReleaseHandle(){ReleaseSucceeded=FreeLibrary(handle);return ReleaseSucceeded;}
  }
  // A state keeps the module's native reference alive even during finalization.
  // The runtime does not strongly retain states, so an abandoned wrapper can
  // still have its native state closed by SafeHandle finalization.
  internal sealed class StateHandle:SafeHandleZeroOrMinusOneIsInvalid {
   readonly OwnLuaJit204Runtime owner;
   bool moduleReference,registered;
   internal OwnLuaCallbackBinding CallbackBinding;
   internal bool NativeClosed;
   internal StateHandle(OwnLuaJit204Runtime runtime,IntPtr value):base(true){
    owner=runtime;bool added=false;
    try{owner.module.DangerousAddRef(ref added);moduleReference=added;SetHandle(value);}
    catch{if(added)owner.module.DangerousRelease();throw;}
   }
   internal void Register(){registered=true;}
   protected override bool ReleaseHandle(){
    bool released=owner.ReleaseState(this,handle,registered);
    if(released){if(moduleReference){moduleReference=false;owner.module.DangerousRelease();}}
    else{
     // Only uncertain cleanup is quarantined. Normal states have no external
     // GC root: an abandoned wrapper still reaches this critical finalizer.
     lock(uncertainCloseRoots){uncertainCloseRoots.Add(this);}
    }
    return released;
   }
  }

  OwnLuaJit204Runtime(FileStream file,ModuleHandle handle){
   lockedFile=file;module=handle;LoadedPath=ModulePath(handle.DangerousGetHandle(),0);
   if(!String.Equals(LoadedPath,Library,StringComparison.OrdinalIgnoreCase))throw new MissingContract(0,"Loaded OWN LuaJIT module path differs");
   ExportAddress("luaJIT_version_2_0_4");
   newState=(NewStateFunction)Bind("luaL_newstate",typeof(NewStateFunction));
   getTop=(GetTopFunction)Bind("lua_gettop",typeof(GetTopFunction));
   close=(CloseFunction)Bind("lua_close",typeof(CloseFunction));
   pushCClosure=(PushCClosureFunction)Bind("lua_pushcclosure",typeof(PushCClosureFunction));
   pCall=(PCallFunction)Bind("lua_pcall",typeof(PCallFunction));
   getField=(GetFieldFunction)Bind("lua_getfield",typeof(GetFieldFunction));
   type=(TypeFunction)Bind("lua_type",typeof(TypeFunction));
   setTop=(SetTopFunction)Bind("lua_settop",typeof(SetTopFunction));
   BindLibrary(OwnLuaJit204Library.Base,"luaopen_base","_G");
   BindLibrary(OwnLuaJit204Library.Math,"luaopen_math","math");
   BindLibrary(OwnLuaJit204Library.String,"luaopen_string","string");
   BindLibrary(OwnLuaJit204Library.Table,"luaopen_table","table");
   BindLibrary(OwnLuaJit204Library.Jit,"luaopen_jit","jit");
   RequireFingerprint(Digest(lockedFile),0);
  }
  public string LoadedPath{get;private set;}
  public bool IsDisposed{get{lock(gate){return disposed;}}}
  public bool ModuleUnloadObserved{get{lock(gate){return unloadObserved;}}}
  public long ActiveStateCount{get{lock(gate){return liveStates;}}}
  public long NativeNewStateCalls{get{lock(gate){return newStateCalls;}}}
  public long NativeGetTopCalls{get{lock(gate){return getTopCalls;}}}
  public long NativeCloseCalls{get{lock(gate){return closeCalls;}}}
  public long NativePushCClosureCalls{get{lock(gate){return pushCClosureCalls;}}}
  public long NativePCallZeroCalls{get{lock(gate){return pCallZeroCalls;}}}
  public long LibraryGlobalTypeReads{get{lock(gate){return globalTypeReads;}}}
  public string CloseFailure{get{lock(gate){return closeFailure;}}}
  public Dictionary<string,long> ExportRvas{get{lock(gate){return new Dictionary<string,long>(exportRvas,StringComparer.Ordinal);}}}

  static string ModulePath(IntPtr handle,uint at){
   var path=new StringBuilder(32768);uint count=GetModuleFileNameW(handle,path,(uint)path.Capacity);
   if(count==0||count>=path.Capacity)throw new MissingContract(at,"Cannot verify OWN LuaJIT loaded path");
   return Path.GetFullPath(path.ToString());
  }
  IntPtr ExportAddress(string name){
   IntPtr address=GetProcAddress(module.DangerousGetHandle(),name),owner;
   if(address==IntPtr.Zero)throw new MissingContract(0,"Fixed OWN LuaJIT export unavailable: "+name);
   if(!GetModuleHandleExW(FromAddressUnchangedRefcount,address,out owner)||owner!=module.DangerousGetHandle()||
      !String.Equals(ModulePath(owner,0),Library,StringComparison.OrdinalIgnoreCase))
    throw new MissingContract(0,"OWN LuaJIT export owner differs or is unknown: "+name);
   exportRvas.Add(name,checked(address.ToInt64()-owner.ToInt64()));return address;
  }
  Delegate Bind(string name,Type type){return Marshal.GetDelegateForFunctionPointer(ExportAddress(name),type);}
  void BindLibrary(OwnLuaJit204Library kind,string name,string global){
   libraryFunctions.Add(kind,new OwnLuaJit204Function(this,kind,name,global,ExportAddress(name)));
  }
  static void PlainAncestors(string path){
   for(string p=Path.GetFullPath(path);p!=null;p=Path.GetDirectoryName(p))
    if((File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0)throw new MissingContract(0,"Redirected OWN LuaJIT path is unsupported: "+p);
  }
  static string Digest(FileStream file){
   file.Position=0;using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(file)).Replace("-","").ToLowerInvariant();
  }
  public static void RequireFingerprint(string observed,uint at){
   if(!String.Equals(observed,VerifiedSha256,StringComparison.Ordinal))throw new MissingContract(at,"OWN LuaJIT fingerprint differs from the approved built artifact");
  }
  static void VerifyLockedPath(FileStream file){
   var path=new StringBuilder(32768);
   uint count=GetFinalPathNameByHandleW(file.SafeFileHandle,path,(uint)path.Capacity,0);
   if(count==0||count>=path.Capacity||!String.Equals(path.ToString(),@"\\?\"+Library,StringComparison.OrdinalIgnoreCase))
    throw new MissingContract(0,"Locked OWN LuaJIT file resolves outside the fixed path");
  }
  public static OwnLuaJit204Runtime OpenOwnProfile(){
   lock(openGate){
    if(System.Environment.OSVersion.Platform!=PlatformID.Win32NT||!System.Environment.Is64BitProcess||IntPtr.Size!=8||
       !String.Equals(System.Environment.SystemDirectory,@"C:\Windows\System32",StringComparison.OrdinalIgnoreCase))
     throw new MissingContract(0,"OWN LuaJIT profile requires the verified Windows x64 environment");
    var entry=Assembly.GetEntryAssembly();
    if(entry==null||String.IsNullOrEmpty(entry.Location))throw new MissingContract(0,"A regular OWN executable is required");
    string application=Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory),redirection=entry.Location+".local";
    PlainAncestors(application);
    if(File.Exists(redirection)||Directory.Exists(redirection)||File.Exists(Path.Combine(application,"kernel32.dll")))
     throw new MissingContract(0,"Application-local DLL redirection is outside the OWN LuaJIT profile");
    if(GetModuleHandleW("lua51.dll")!=IntPtr.Zero)throw new MissingContract(0,"A Lua51 module is already loaded; its identity is not adopted");
    FileStream file=null;ModuleHandle handle=null;
    try{
     PlainAncestors(Library);
     file=new FileStream(Library,FileMode.Open,FileAccess.Read,FileShare.Read);
     if(file.Length!=VerifiedBytes)throw new MissingContract(0,"OWN LuaJIT size differs");
     VerifyLockedPath(file);RequireFingerprint(Digest(file),0);
     IntPtr raw=LoadLibraryExW(Library,IntPtr.Zero,SearchDllDirectory|SearchSystem32);
     if(raw==IntPtr.Zero)throw new MissingContract(0,"Fixed OWN LuaJIT load failed: "+Marshal.GetLastWin32Error());
     try{handle=new ModuleHandle(raw);}catch{FreeLibrary(raw);throw;}
     var ready=new OwnLuaJit204Runtime(file,handle);file=null;handle=null;return ready;
    }catch(Exception error){
     if(error is MissingContract)throw;
     if(error is IOException||error is UnauthorizedAccessException||error is DllNotFoundException||error is EntryPointNotFoundException||error is BadImageFormatException||error is System.Security.SecurityException)
      throw new MissingContract(0,"OWN LuaJIT profile unavailable: "+error.GetType().Name,error);
     throw;
    }finally{if(handle!=null)handle.Dispose();if(file!=null)file.Dispose();}
   }
  }
  void Live(uint at){
   if(disposed||module.IsClosed||module.IsInvalid)throw new MissingContract(at,"OWN LuaJIT runtime has been disposed");
   if(closeFailure!=null)throw new MissingContract(at,"OWN LuaJIT state cleanup failed; no further calls accepted: "+closeFailure);
  }
  void RequireNativeEntry(uint at){
   if(hostCallbacks!=0||nativeCalls!=0)throw new MissingContract(at,"Reentrant OWN Lua native entry is outside the bridge boundary");
  }
  void RequireUnattached(OwnLuaJit204State state,uint at){
   if(state.Handle.CallbackBinding!=null)throw new MissingContract(at,"An attached OWN state may execute only through its protected callback bridge");
  }
  internal IntPtr ModuleForBridge(uint at){lock(gate){Live(at);return module.DangerousGetHandle();}}
  internal IntPtr StateForBridge(OwnLuaJit204State state,uint at){lock(gate){RequireState(state,at);return state.Handle.DangerousGetHandle();}}
  internal void EnterHostCallback(uint at){
   // Called synchronously while the runtime already holds gate. The native
   // bridge independently checks OS-thread/context identity before calling us.
   if(nativeCalls==0||hostCallbacks!=0)throw new MissingContract(at,"Unadmitted OWN host callback");
   hostCallbacks++;
  }
  // A reverse-P/Invoke finally must not throw across the C ABI.
  internal void ExitHostCallback(){if(hostCallbacks>0)hostCallbacks--;}
  internal OwnLuaCallbackResult InvokeBridge(OwnLuaJit204State state,OwnLuaCallbackBinding binding,Func<OwnLuaCallbackResult> call,uint at){
   lock(gate){
    RequireNativeEntry(at);RequireState(state,at);
    if(!Object.ReferenceEquals(state.Handle.CallbackBinding,binding))throw new MissingContract(at,"Unknown OWN state/bridge binding");
    binding.RequireReady(at);nativeCalls++;
    try{return call();}finally{nativeCalls--;GC.KeepAlive(state);GC.KeepAlive(binding);}
   }
  }
  bool OptionalExport(string name,uint at){
   IntPtr address=GetProcAddress(module.DangerousGetHandle(),name),owner;
   if(address==IntPtr.Zero)return false;
   if(!GetModuleHandleExW(FromAddressUnchangedRefcount,address,out owner)||owner!=module.DangerousGetHandle()||
      !String.Equals(ModulePath(owner,at),Library,StringComparison.OrdinalIgnoreCase))throw new MissingContract(at,"Optional OWN Lua export has a foreign or unknown owner: "+name);
   return true;
  }
  public OwnLuaRegistrySelection SelectOwnRegistry(OwnLuaCallbackContext context){
   lock(gate){
    RequireNativeEntry(0);Live(0);if(context==null)throw new ArgumentNullException("context");
    if(context.SelectedRuntime!=null)throw new InvalidOperationException("OWN registry classification was already selected");
    bool getGlobal=OptionalExport("lua_getglobal",0),jit=OptionalExport("luaJIT_setmode",0);
    uint version=getGlobal?2u:jit?3u:1u;int registry=version==2?-1001000:-10000;
    var selection=new OwnLuaRegistrySelection{Version=version,RegistryIndex=registry,HasLuaGetGlobal=getGlobal,HasLuaJitSetmode=jit};
    // Explicit OWN binding order, not proof of native startup ordering. Publish
    // the actual local classification; Selection is only a mutable report.
    context.Version4217DC.Write(version);context.Registry422888.Write(registry);context.Selection=selection;context.SelectedRuntime=this;return selection;
   }
  }
  public OwnLuaCallbackBinding AttachCallbacks(OwnLuaJit204State state,OwnLuaCallbackContext context,uint at){
   lock(gate){
    RequireNativeEntry(at);RequireState(state,at);
    if(context==null||!Object.ReferenceEquals(context.SelectedRuntime,this)||context.Selection==null)throw new MissingContract(at,"OWN callback registry has no actual module classification");
    if(state.Handle.CallbackBinding!=null)throw new MissingContract(at,"OWN state already has a callback owner");
    if(callbackBridge==null)callbackBridge=OwnLuaCallbackBridge.Open();
    var binding=new OwnLuaCallbackBinding(this,state,context,callbackBridge);state.Handle.CallbackBinding=binding;return binding;
   }
  }
  internal OwnLuaCallbackBinding CaptureServiceBinding(GameReachedGoalLuaService service,OwnLuaCallbackContext context,uint at){
   lock(gate){
    RequireNativeEntry(at);Live(at);
    if(service==null||context==null||!Object.ReferenceEquals(context.Service,service)||!Object.ReferenceEquals(context.SelectedRuntime,this))
     throw new MissingContract(at,"Unknown OWN service/global context for VM capture");
    // One read at the supplied native capture site. Later Script/Client/Server
    // captures can differ, but must retain this shared service/global context.
    var current=service.Lua00.Read(at) as OwnLuaJit204State;RequireState(current,at);
    var binding=current.Handle.CallbackBinding;
    if(binding==null||!Object.ReferenceEquals(binding.Context,context))
     throw new MissingContract(at,"Current service VM lacks this live shared OWN binding");
    binding.RequireReady(at);return binding;
   }
  }
  internal void RequireState(OwnLuaJit204State state,uint at){
   lock(gate){Live(at);if(state==null||!Object.ReferenceEquals(state.Owner,this)||state.Handle.IsClosed||state.Handle.IsInvalid)
    throw new MissingContract(at,"Unknown,foreign or released OWN Lua state identity");}
  }
  internal bool StateIsLive(OwnLuaJit204State state){
   lock(gate){return !disposed&&closeFailure==null&&!module.IsClosed&&!module.IsInvalid&&state!=null&&
    Object.ReferenceEquals(state.Owner,this)&&!state.Handle.IsClosed&&!state.Handle.IsInvalid;}
  }
  public OwnLuaJit204State NewState(uint at){
   lock(gate){
    RequireNativeEntry(at);Live(at);if(liveStates==Int64.MaxValue)throw new MissingContract(at,"OWN state counter exhausted");
    newStateCalls++;IntPtr pointer;nativeCalls++;try{pointer=newState();}finally{nativeCalls--;}
    if(pointer==IntPtr.Zero)return null; // Do not box zero as a false nonnull service value.
    StateHandle handle=null;
    try{handle=new StateHandle(this,pointer);liveStates++;handle.Register();return new OwnLuaJit204State(this,handle);}
    catch{if(handle!=null)handle.Dispose();else ReleaseState(null,pointer,false);throw;}
   }
  }
  internal int GetTop(OwnLuaJit204State state,uint at){
   lock(gate){
    RequireNativeEntry(at);RequireState(state,at);if(state.Handle.CallbackBinding!=null)state.Handle.CallbackBinding.RequireReady(at);getTopCalls++;nativeCalls++;
    try{return getTop(state.Handle.DangerousGetHandle());}
    finally{nativeCalls--;GC.KeepAlive(state);}
   }
  }
  void RequireFunction(OwnLuaJit204Function function,uint at){
   OwnLuaJit204Function canonical;
   if(function==null||!Object.ReferenceEquals(function.Owner,this)||
      !libraryFunctions.TryGetValue(function.Kind,out canonical)||!Object.ReferenceEquals(function,canonical))
    throw new MissingContract(at,"Unknown or foreign OWN Lua library function identity");
  }
  public OwnLuaJit204Function GetLibraryFunction(OwnLuaJit204Library kind,uint at){
   lock(gate){
    Live(at);OwnLuaJit204Function function;
    if(!libraryFunctions.TryGetValue(kind,out function))throw new MissingContract(at,"Library is outside the five observed Engine25A030 entries");
    return function;
   }
  }
  public void PushCClosure(OwnLuaJit204State state,OwnLuaJit204Function function,uint at){
   lock(gate){
    RequireNativeEntry(at);RequireState(state,at);RequireUnattached(state,at);RequireFunction(function,at);pushCClosureCalls++;nativeCalls++;
    try{pushCClosure(state.Handle.DangerousGetHandle(),function.Address,0);}
    finally{nativeCalls--;GC.KeepAlive(state);GC.KeepAlive(function);}
   }
  }
  public int PCallZero(OwnLuaJit204State state,uint at){
   lock(gate){
    RequireNativeEntry(at);RequireState(state,at);RequireUnattached(state,at);pCallZeroCalls++;nativeCalls++;
    try{return pCall(state.Handle.DangerousGetHandle(),0,0,0);}
    finally{nativeCalls--;GC.KeepAlive(state);}
   }
  }
  // OWN observation only, not an extra call attributed to Engine25A030.
  // All names are fixed ASCII. Restore the exact entry stack even when the
  // observed type is nil or unexpected; this does not turn pcall errors into success.
  public int ReadLibraryGlobalType(OwnLuaJit204State state,OwnLuaJit204Function function,uint at){
   lock(gate){
    RequireNativeEntry(at);RequireState(state,at);RequireUnattached(state,at);RequireFunction(function,at);nativeCalls++;
    try{
     IntPtr pointer=state.Handle.DangerousGetHandle();getTopCalls++;int top=getTop(pointer);
     try{globalTypeReads++;getField(pointer,-10002,function.GlobalName);return type(pointer,-1);}
     finally{setTop(pointer,top);}
    }finally{nativeCalls--;GC.KeepAlive(state);GC.KeepAlive(function);}
   }
  }
  bool ReleaseState(StateHandle state,IntPtr pointer,bool registered){
   lock(gate){
    var binding=state==null?null:state.CallbackBinding;
    try{
     RequireNativeEntry(0);
     if(binding!=null)binding.BeginClose();
     if(state==null||!state.NativeClosed){
      nativeCalls++;try{closeCalls++;close(pointer);}finally{nativeCalls--;}
      if(state!=null)state.NativeClosed=true;
      if(registered)liveStates--;
     }
     // A callback semantic error swallowed by Lua during close is kept in
     // CloseResult. It does not undo an actual close or trigger a second close.
     if(binding!=null){nativeCalls++;try{binding.CompleteClose();}finally{nativeCalls--;}}return true;
    }
    catch(Exception error){try{closeFailure=error.ToString();}catch{closeFailure="OWN Lua cleanup failed (formatting unavailable)";}return false;}
   }
  }
  internal void CloseState(OwnLuaJit204State state){
   lock(gate){
    RequireNativeEntry(0); // Before SafeHandle.Dispose irreversibly marks closed.
    if(state==null||!Object.ReferenceEquals(state.Owner,this))throw new InvalidOperationException("Foreign OWN Lua state");
    if(state.Handle.IsClosed){if(closeFailure!=null)throw new MissingContract(0,"Previous OWN Lua cleanup failed: "+closeFailure);return;}
    Live(0);if(state.Handle.CallbackBinding!=null)state.Handle.CallbackBinding.BeginClose();state.Handle.Dispose();
    if(closeFailure!=null)throw new MissingContract(0,"OWN Lua cleanup did not return normally: "+closeFailure);
   }
  }
  public void Dispose(){
   lock(gate){
    RequireNativeEntry(0);
    if(disposed)return;
    if(liveStates!=0)throw new InvalidOperationException("Close all OWN Lua states before disposing their runtime");
    Live(0);if(callbackBridge!=null)callbackBridge.Dispose();module.Dispose();disposed=true;
    try{unloadObserved=module.ReleaseSucceeded&&GetModuleHandleW("lua51.dll")==IntPtr.Zero;}
    finally{lockedFile.Dispose();}
    if(!unloadObserved)throw new MissingContract(0,"OWN LuaJIT reference released but module unload was not verified");
   }
  }
 }

 public enum OwnLuaJit204Library { Base, Math, String, Table, Jit }

 // Canonical module-owned address, never an arbitrary managed callback.
 // The runtime checks reference identity as well as owner and enum value.
 public sealed class OwnLuaJit204Function {
  internal readonly OwnLuaJit204Runtime Owner;
  internal readonly IntPtr Address;
  internal readonly string GlobalName;
  public OwnLuaJit204Library Kind{get;private set;}
  public string Name{get;private set;}
  internal OwnLuaJit204Function(OwnLuaJit204Runtime owner,OwnLuaJit204Library kind,string name,string globalName,IntPtr address){
   Owner=owner;Kind=kind;Name=name;GlobalName=globalName;Address=address;
  }
 }

 // Stable identity for exactly one real lua_State; no pointer setter or public
 // constructor can substitute a fixture object for native creation.
 public sealed class OwnLuaJit204State : IDisposable {
  internal readonly OwnLuaJit204Runtime Owner;
  internal readonly OwnLuaJit204Runtime.StateHandle Handle;
  internal OwnLuaJit204State(OwnLuaJit204Runtime owner,OwnLuaJit204Runtime.StateHandle handle){Owner=owner;Handle=handle;}
  public bool IsAlive{get{return Owner.StateIsLive(this);}}
  public OwnLuaCallbackBinding CallbackBinding{get{return Handle.CallbackBinding;}}
  public void RequireLive(uint at){Owner.RequireState(this,at);}
  public int GetTop(uint at){return Owner.GetTop(this,at);}
  public void Dispose(){Owner.CloseState(this);}
 }
}
