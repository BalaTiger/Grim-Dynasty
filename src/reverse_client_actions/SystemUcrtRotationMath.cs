using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace GrimDynasty.ReverseClient.Actions {
 // OWN Windows x64 platform profile, not recovery of the game's loaded CRT.
 // This is intentionally separate from the old rewards SystemCrtMath profile.
 // Disk hash, actual module path and export ownership are checked separately.
 // A preloaded same-path image is NOT memory-hashed or proved byte-identical to
 // the locked disk file. No original runtime numeric equivalence is claimed.
 // All unmanaged calls below require formal build/test approval before use.
 public sealed class SystemUcrtRotationMath : IDisposable {
  public const string Library=@"C:\Windows\System32\ucrtbase.dll";
  public const string VerifiedSha256="dedb7611e1abc1ff3852dcac3d4c93a048d729e25b061973b9969f22698f14ca";
  public const string VerifiedFileVersion="10.0.26100.9711 (WinBuild.160101.0800)";
  public const long VerifiedBytes=1399656;
  const string Kernel=@"C:\Windows\System32\kernel32.dll";
  const uint SearchSystem32=0x800,FromAddressUnchangedRefcount=6;
  const uint RoundMask=0x300,DenormalMask=0x3000000,ExceptionMask=0x8001f,Ambiguous=0x80000000;
  [DllImport(Kernel,ExactSpelling=true,CharSet=CharSet.Unicode,SetLastError=true)]
  static extern IntPtr LoadLibraryExW(string file,IntPtr reserved,uint flags);
  [DllImport(Kernel,ExactSpelling=true,CharSet=CharSet.Ansi,SetLastError=true)]
  static extern IntPtr GetProcAddress(IntPtr module,string name);
  [DllImport(Kernel,ExactSpelling=true,CharSet=CharSet.Unicode,SetLastError=true)]
  static extern uint GetModuleFileNameW(IntPtr module,StringBuilder path,uint capacity);
  [DllImport(Kernel,ExactSpelling=true,SetLastError=true)]
  [return:MarshalAs(UnmanagedType.Bool)]
  static extern bool GetModuleHandleExW(uint flags,IntPtr address,out IntPtr module);
  [DllImport(Kernel,ExactSpelling=true,SetLastError=true)]
  [return:MarshalAs(UnmanagedType.Bool)]
  static extern bool FreeLibrary(IntPtr module);
  // Real C float(float), not double Math APIs or a user-provided callback.
  // On Windows x64 the first scalar float argument and result use XMM0.
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  [return:MarshalAs(UnmanagedType.R4)]delegate float Unary([MarshalAs(UnmanagedType.R4)]float value);
  // Prototyped Windows x64 float atan2f(float y,float x): XMM0/XMM1,
  // result XMM0. No conversion to double or division before the native call.
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  [return:MarshalAs(UnmanagedType.R4)]delegate float Binary([MarshalAs(UnmanagedType.R4)]float y,[MarshalAs(UnmanagedType.R4)]float x);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int QueryControl(out uint current,uint newValue,uint mask);
  sealed class ModuleHandle:SafeHandleZeroOrMinusOneIsInvalid {
   internal ModuleHandle(IntPtr value):base(true){SetHandle(value);}
   protected override bool ReleaseHandle(){return FreeLibrary(handle);}
  }
  readonly object gate=new object();
  readonly FileStream lockedFile;
  readonly ModuleHandle module;
  readonly Unary acos,cos,sin;
  Binary atan2;SystemUcrtCrowdMath crowd;
  Unary tan;SystemUcrtCameraMath camera;
  readonly QueryControl control;
  readonly Dictionary<string,long> exportRvas=new Dictionary<string,long>(StringComparer.Ordinal);
  bool disposed;long calls;
  public long NativeMathCalls{get{lock(gate){return calls;}}}
  public string LoadedPath{get;private set;}
  public Dictionary<string,long> ExportRvas{get{lock(gate){return new Dictionary<string,long>(exportRvas,StringComparer.Ordinal);}}}
  public bool IsDisposed{get{lock(gate){return disposed;}}}
  // Opt-in view of the same owned module, control contract and call counter.
  // Old rotation clients still bind exactly their original four exports.
  public SystemUcrtCrowdMath Crowd{get{lock(gate){Live(0);if(crowd==null){atan2=(Binary)Bind("atan2f",typeof(Binary));crowd=new SystemUcrtCrowdMath(this);}return crowd;}}}
  // Explicit Camera selection adds tanf only. The same locked system module,
  // ownership checks and floating-control contract remain authoritative.
  // Engine2EE09C's named disk import is evidenced, not loaded-game equivalence.
  public SystemUcrtCameraMath Camera{get{lock(gate){Live(0);if(camera==null){tan=(Unary)Bind("tanf",typeof(Unary));camera=new SystemUcrtCameraMath(this);}return camera;}}}
  SystemUcrtRotationMath(FileStream file,ModuleHandle handle){
   lockedFile=file;module=handle;LoadedPath=ModulePath(handle.DangerousGetHandle(),0);
   if(!String.Equals(LoadedPath,Library,StringComparison.OrdinalIgnoreCase))throw new MissingContract(0,"Loaded UCRT module path differs from fixed system profile");
   acos=(Unary)Bind("acosf",typeof(Unary));cos=(Unary)Bind("cosf",typeof(Unary));sin=(Unary)Bind("sinf",typeof(Unary));
   control=(QueryControl)Bind("_controlfp_s",typeof(QueryControl));
   Query(0);RequireFingerprint(Digest(lockedFile),0);
  }
  static string ModulePath(IntPtr handle,uint at){
   var path=new StringBuilder(32768);uint count=GetModuleFileNameW(handle,path,(uint)path.Capacity);
   if(count==0||count>=path.Capacity)throw new MissingContract(at,"Cannot verify loaded system module path");
   return Path.GetFullPath(path.ToString());
  }
  Delegate Bind(string name,Type type){
   IntPtr address=GetProcAddress(module.DangerousGetHandle(),name);
   if(address==IntPtr.Zero)throw new MissingContract(0,"Fixed UCRT export unavailable: "+name);
   IntPtr owner;
   // This query does not acquire a reference. Only our LoadLibraryEx reference
   // is released; forwarded exports into another module are outside the profile.
   if(!GetModuleHandleExW(FromAddressUnchangedRefcount,address,out owner)||owner!=module.DangerousGetHandle())
    throw new MissingContract(0,"Fixed UCRT export belongs to another or unknown module: "+name);
   if(!String.Equals(ModulePath(owner,0),Library,StringComparison.OrdinalIgnoreCase))throw new MissingContract(0,"Export owner path mismatch");
   exportRvas.Add(name,checked(address.ToInt64()-owner.ToInt64()));
   return Marshal.GetDelegateForFunctionPointer(address,type);
  }
  static void PlainAncestors(string path){
   for(string p=Path.GetFullPath(path);p!=null;p=Path.GetDirectoryName(p)){
    if((File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0)throw new MissingContract(0,"Redirected system/profile path is unsupported: "+p);
   }
  }
  static string Digest(FileStream file){
   file.Position=0;using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(file)).Replace("-","").ToLowerInvariant();
  }
  public static void RequireFingerprint(string observed,uint at){
   if(!String.Equals(observed,VerifiedSha256,StringComparison.Ordinal))throw new MissingContract(at,"System UCRT fingerprint is outside this own rotation profile");
  }
  // Pure checks are exposed for failed-gate tests without changing live FP state.
  public static void RequireControlProfile(uint word,uint at){
   if((word&RoundMask)!=0||(word&DenormalMask)!=0||(word&ExceptionMask)!=ExceptionMask||(word&Ambiguous)!=0)
    throw new MissingContract(at,"Own rotation profile requires nearest rounding, all exceptions masked and preserved denormals");
  }
  static bool Ordinary(float value){
   uint bits=BitConverter.ToUInt32(BitConverter.GetBytes(value),0),magnitude=bits&0x7fffffffu;
   return magnitude<0x7f800000u&&(magnitude==0||magnitude>=0x00800000u);
  }
  public static void RequireAcosInput(float value,uint at){
   if(!Ordinary(value)||value< -1f||value>1f)throw new MissingContract(at,"Own acosf domain is finite normal-or-zero [-1,1]; no NaN/infinity/subnormal handling");
  }
  public static void RequireAngleInput(float value,uint at){
   if(!Ordinary(value)||value< -4f||value>4f)throw new MissingContract(at,"Own sinf/cosf domain is finite normal-or-zero [-4,4] radians");
  }
  public static void RequireCrowdAngleInput(float value,uint at){
   if(!Ordinary(value)||value< -4f||value>9f)throw new MissingContract(at,"Own crowd sinf/cosf domain is finite normal-or-zero [-4,9] radians; no wider-angle or special-value fallback");
  }
  public static void RequireAtan2Inputs(float y,float x,uint at){
   if(!Ordinary(y)||!Ordinary(x))throw new MissingContract(at,"Own atan2f inputs require finite normal-or-zero y then x; no NaN/infinity/subnormal handling");
  }
  public static void RequireCameraTangentInput(float halfFov,uint at){
   if(!Ordinary(halfFov)||halfFov< -1.5f||halfFov>1.5f)
    throw new MissingContract(at,"Own camera tanf domain is finite normal-or-zero [-1.5,1.5] radians, bounded away from poles; not a native camera validation rule");
  }
  public static SystemUcrtRotationMath OpenOwnProfile(){
   if(System.Environment.OSVersion.Platform!=PlatformID.Win32NT||!System.Environment.Is64BitProcess||IntPtr.Size!=8)
    throw new MissingContract(0,"Own system UCRT profile requires Windows x64");
   if(!String.Equals(System.Environment.SystemDirectory,@"C:\Windows\System32",StringComparison.OrdinalIgnoreCase))
    throw new MissingContract(0,"Unexpected Windows system directory");
   // Explicitly reject application-local redirection candidates before loading.
   // No DLL search directories or other process-global settings are modified.
   string application=Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
   PlainAncestors(application);var entry=Assembly.GetEntryAssembly();
   if(entry==null||String.IsNullOrEmpty(entry.Location))throw new MissingContract(0,"A regular own executable is required");
   string redirection=entry.Location+".local";
   if(File.Exists(redirection)||Directory.Exists(redirection)||File.Exists(Path.Combine(application,"ucrtbase.dll"))||File.Exists(Path.Combine(application,"kernel32.dll")))
    throw new MissingContract(0,"Application-local system-DLL/redirection candidate is unsupported");
   FileStream file=null;ModuleHandle handle=null;
   try{
    PlainAncestors(Library);
    var info=new FileInfo(Library);if(info.Length!=VerifiedBytes)throw new MissingContract(0,"System UCRT size changed");
    if(FileVersionInfo.GetVersionInfo(Library).FileVersion!=VerifiedFileVersion)throw new MissingContract(0,"System UCRT version changed");
    // Keep a read-only handle without write/delete sharing for this owned
    // provider's lifetime. No replacement or mutable profile is accepted.
    file=new FileStream(Library,FileMode.Open,FileAccess.Read,FileShare.Read);
    RequireFingerprint(Digest(file),0);
    IntPtr raw=LoadLibraryExW(Library,IntPtr.Zero,SearchSystem32);
    if(raw==IntPtr.Zero)throw new MissingContract(0,"Fixed system UCRT load failed: "+Marshal.GetLastWin32Error());
    handle=new ModuleHandle(raw);
    var ready=new SystemUcrtRotationMath(file,handle);file=null;handle=null;return ready;
   }catch(Exception error){
    if(error is MissingContract)throw;
    if(error is IOException||error is UnauthorizedAccessException||error is DllNotFoundException||error is EntryPointNotFoundException||error is BadImageFormatException||error is System.Security.SecurityException)
     throw new MissingContract(0,"Own system UCRT profile unavailable: "+error.GetType().Name,error);
    throw;
   }finally{if(handle!=null)handle.Dispose();if(file!=null)file.Dispose();}
  }
  void Live(uint at){if(disposed||module.IsClosed||module.IsInvalid)throw new MissingContract(at,"Own system UCRT rotation provider is disposed");}
  uint Query(uint at){
   uint word;if(control(out word,0,0)!=0)throw new MissingContract(at,"Read-only UCRT floating control query failed");
   RequireControlProfile(word,at);return word;
  }
  public uint CheckControl(uint at){lock(gate){Live(at);return Query(at);}}
  public string VerifyLockedFingerprint(uint at){lock(gate){Live(at);string actual=Digest(lockedFile);RequireFingerprint(actual,at);return actual;}}
  float Invoke(Unary function,float value,bool inverse,uint at){
   lock(gate){
    Live(at);if(inverse)RequireAcosInput(value,at);else RequireAngleInput(value,at);
    uint before=Query(at);calls++;float result=function(value);uint after=Query(at);
    if(before!=after)throw new MissingContract(at,"System math call changed the queried floating control word");
    if(!Ordinary(result))throw new MissingContract(at,"System math result is outside own finite normal-or-zero output domain");
    // errno and sticky FP exception flags are not inspected, cleared or restored.
    // The model neither promises their preservation nor exposes them as success.
    return result;
   }
  }
  public float Acos(float value,uint at){return Invoke(acos,value,true,at);}
  public float Cos(float value,uint at){return Invoke(cos,value,false,at);}
  public float Sin(float value,uint at){return Invoke(sin,value,false,at);}
  internal float CrowdAtan2(float y,float x,uint at){
   lock(gate){
    Live(at);RequireAtan2Inputs(y,x,at);if(atan2==null)throw new MissingContract(at,"Crowd math profile has not been selected");
    uint before=Query(at);calls++;float result=atan2(y,x);uint after=Query(at);
    if(before!=after)throw new MissingContract(at,"System atan2f changed the queried floating control word");
    if(!Ordinary(result))throw new MissingContract(at,"System atan2f result is outside own finite normal-or-zero output domain");
    return result;
   }
  }
  internal float CrowdTrig(float angle,bool sine,uint at){
   lock(gate){
    Live(at);RequireCrowdAngleInput(angle,at);if(crowd==null)throw new MissingContract(at,"Crowd math profile has not been selected");
    uint before=Query(at);calls++;float result=sine?sin(angle):cos(angle);uint after=Query(at);
    if(before!=after)throw new MissingContract(at,"System crowd trig changed the queried floating control word");
    if(!Ordinary(result))throw new MissingContract(at,"System crowd trig result is outside own finite normal-or-zero output domain");
    return result;
   }
  }
  internal float CameraTan(float halfFov,uint at){
   lock(gate){
    Live(at);RequireCameraTangentInput(halfFov,at);if(camera==null||tan==null)throw new MissingContract(at,"Camera math profile has not been selected");
    uint before=Query(at);calls++;float result=tan(halfFov);uint after=Query(at);
    if(before!=after)throw new MissingContract(at,"System tanf changed the queried floating control word");
    if(!Ordinary(result))throw new MissingContract(at,"System camera tanf result is outside own finite normal-or-zero output domain");
    // This own profile does not inspect or restore errno or sticky FP flags.
    return result;
   }
  }
  public void Dispose(){lock(gate){if(disposed)return;disposed=true;module.Dispose();lockedFile.Dispose();}}
 }
 // Fixed concrete view; no public constructor, overridable math methods or
 // callback interface. Lifetime, module and counters remain in the one owner.
 public sealed class SystemUcrtCrowdMath {
  readonly SystemUcrtRotationMath owner;
  internal SystemUcrtCrowdMath(SystemUcrtRotationMath owner){this.owner=owner;}
  public float Atan2(float y,float x,uint at){return owner.CrowdAtan2(y,x,at);}
  public float Cos(float angle,uint at){return owner.CrowdTrig(angle,false,at);}
  public float Sin(float angle,uint at){return owner.CrowdTrig(angle,true,at);}
 }
 // No public constructor, delegate injection or independent module lifetime.
 // Production computes the half-FOV before this actual float(float) call.
 public sealed class SystemUcrtCameraMath {
  readonly SystemUcrtRotationMath owner;
  internal SystemUcrtCameraMath(SystemUcrtRotationMath owner){this.owner=owner;}
  public float Tan(float halfFov,uint at){return owner.CameraTan(halfFov,at);}
 }
}
